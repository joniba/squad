<#
.SYNOPSIS
    Invokes Aragorn via copilot -p to scan IcM for active incidents.
.DESCRIPTION
    Watermark-driven IcM scan. Tracks last scan time and seen incident IDs to narrow
    query windows and skip already-processed incidents. Aragorn scans, prints a concise
    inline summary, and creates GitHub issues for each new incident. NO report files
    are created — scans trigger tasks, not documents.
.PARAMETER TeamId
    IcM team ID (default: 116041 = DAKOTA\ThreatIntelligence)
.PARAMETER Model
    LLM model to use (default: claude-sonnet-4.6)
.PARAMETER DryRun
    Print watermark state + prompt, then exit without invoking the LLM.
.PARAMETER Reset
    Delete the watermark file. Next run uses the 24h default window.
.EXAMPLE
    .\scripts\icm-scan.ps1
    .\scripts\icm-scan.ps1 -DryRun
    .\scripts\icm-scan.ps1 -Reset
#>
param(
    [string]$TeamId = "116041",
    [string]$Model = "claude-sonnet-4.6",
    [switch]$DryRun,
    [switch]$Reset
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$watermarkPath = Join-Path $root ".squad\icm-scan-watermark.json"

# --- Handle -Reset ---
if ($Reset) {
    if (Test-Path $watermarkPath) {
        Remove-Item $watermarkPath -Force
        Write-Host "🔄 Watermark reset — next scan uses 24h default window" -ForegroundColor Yellow
    } else {
        Write-Host "ℹ️  No watermark file to reset" -ForegroundColor DarkGray
    }
    exit 0
}

# --- Load watermark ---
$watermark = $null
$defaultHours = 24
$maxHours = 168  # 7 days cap

if (Test-Path $watermarkPath) {
    try {
        $watermark = Get-Content $watermarkPath -Raw | ConvertFrom-Json
        if (-not $watermark.version -or -not $watermark.lastScan) { throw "Invalid schema" }
    } catch {
        Write-Warning "⚠️  Watermark corrupt — deleting and starting fresh"
        Remove-Item $watermarkPath -Force
        $watermark = $null
    }
}

# --- Calculate lookback window ---
$now = [datetime]::UtcNow
$lookbackHours = $defaultHours
$lastScanDisplay = "never (first run)"

if ($watermark -and $watermark.lastScan) {
    # ConvertFrom-Json auto-parses ISO 8601 strings to DateTime objects
    $lastScan = [datetime]$watermark.lastScan
    $hoursSince = ($now - $lastScan).TotalHours + 1  # +1h buffer handles race conditions
    $lookbackHours = [Math]::Min([Math]::Ceiling($hoursSince), $maxHours)
    $lastScanDisplay = $lastScan.ToString("yyyy-MM-ddTHH:mm:ssZ")
}

$watermarkSeenIds = @()
if ($watermark -and $watermark.seenIds) { $watermarkSeenIds = @($watermark.seenIds) }

# --- Build known IDs list (3 sources merged) ---
$knownIds = [System.Collections.Generic.HashSet[string]]::new()

# Source 1: Watermark seenIds
foreach ($id in $watermarkSeenIds) { $knownIds.Add($id) | Out-Null }

# Source 2: docs/investigations/icm-* filenames
$investigationsDir = Join-Path $root "docs\investigations"
$existingReports = @()
if (Test-Path $investigationsDir) {
    $existingReports = @(Get-ChildItem -Path $investigationsDir -Filter "icm-*" -Name | ForEach-Object {
        if ($_ -match "icm-(\d+)") { $Matches[1] }
    })
    foreach ($id in $existingReports) { $knownIds.Add($id) | Out-Null }
}

# Source 3: GitHub issues with squad:aragorn label (titles like "ICM 12345: ...")
$existingIssues = gh issue list --label "squad,squad:aragorn" --state all --json title --limit 100 --repo jbenami_microsoft/ms-pa 2>$null | ConvertFrom-Json
$existingTitles = @()
if ($existingIssues) {
    $existingTitles = @($existingIssues | ForEach-Object { $_.title })
    $existingIssues | ForEach-Object {
        if ($_.title -match "ICM\s+(\d+)") { $knownIds.Add($Matches[1]) | Out-Null }
    }
}

$knownIdsList = @($knownIds)
$skipInstruction = if ($knownIdsList.Count -gt 0) {
    "Skip these IcM IDs (already processed — do NOT report them): $($knownIdsList -join ', ')"
} else { "" }

# --- Build prompt ---
$prompt = @"
You are Aragorn, the Livesite Responder for the pa-squad team.

## Task: IcM Scan — Team $TeamId (DAKOTA\ThreatIntelligence)

Use icm-search_incidents_by_owning_team_id with teamId $TeamId to retrieve all active
incidents. Filter client-side using the criteria below.

**Time window:** Only report incidents created or updated in the last $lookbackHours hours.
$skipInstruction

## Include

- Sev 0, 1, or 2 — any Incident Type, State == Active
- CRIs — Incident Type == "System/Customer Reported", any severity, State == Active
- Sev 2.5 candidates — Sev3 incidents with notes or tags recommending a Sev2 bump

## Exclude

- Bare Sev3 (not CRI, no Sev2 bump flag)
- Sev4 and Sev5
- State: Mitigated, Resolved, or False Positive

## Required Output Format

Print ONLY a concise summary. Do NOT write files. Do NOT create markdown reports.

**If incidents found, output EXACTLY this format:**

SCAN_RESULT|found|{total_count}
INCIDENT|{icmId}|Sev{severity}|{type}|{title}|{owningContact}|{createdDate}
INCIDENT|{icmId}|Sev{severity}|{type}|{title}|{owningContact}|{createdDate}
...
HIGHLIGHT|{one-line summary of most urgent item}

**If NO incidents match, output EXACTLY:**

SCAN_RESULT|clear|0

Example:
SCAN_RESULT|found|3
INCIDENT|766712513|Sev2|LiveSite|ARM Watchlist API Error Rates|jdoe|2026-03-20
INCIDENT|51000000954460|Sev3|CRI|Revoked TI Indicators Still Triggering|ssmith|2026-03-18
INCIDENT|21000000951041|Sev3|CRI|Azure Gov TAXII Ingestion Shortfall|mjones|2026-03-17
HIGHLIGHT|Sev2 766712513 is recurring (5th time in March) — needs root cause
"@

# --- Transparency output (shown for both normal and DryRun) ---
$sinceLine = if ($watermark) { "(since $lastScanDisplay)" } else { "(first run — 24h default)" }
Write-Host "🔍 IcM scan — team $TeamId" -ForegroundColor Cyan
Write-Host "   Window: last $($lookbackHours)h $sinceLine" -ForegroundColor DarkGray
Write-Host "   Known: $($knownIdsList.Count) IcM IDs already processed (skipped)" -ForegroundColor DarkGray
Write-Host "   Filter: Sev0-2 (any type) + CRIs (System/Customer Reported, any sev) + Sev2.5 candidates" -ForegroundColor DarkGray
Write-Host "   Exclude: bare Sev3, Sev4-5, Mitigated/Resolved/False Positive" -ForegroundColor DarkGray
Write-Host ""

if ($DryRun) {
    Write-Host "DRY RUN — would invoke: copilot -p with model $Model" -ForegroundColor Yellow
    Write-Host ""
    Write-Host $prompt
    exit 0
}

$output = copilot -p $prompt `
    --yolo `
    --no-ask-user `
    --model $Model `
    -s 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Error "copilot -p failed (exit $LASTEXITCODE)"
    exit $LASTEXITCODE
}

# Parse structured output
$lines = $output -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ }
$scanResult = $lines | Where-Object { $_ -match "^SCAN_RESULT\|" } | Select-Object -First 1
$incidents  = $lines | Where-Object { $_ -match "^INCIDENT\|" }
$highlight  = $lines | Where-Object { $_ -match "^HIGHLIGHT\|" } | Select-Object -First 1

# --- Update watermark (always, even on clear result) ---
$newScanIds = @($incidents | ForEach-Object {
    $parts = $_ -split "\|"
    if ($parts.Count -ge 2 -and $parts[1]) { $parts[1] }
})

$mergedIds = [System.Collections.Generic.List[string]]::new()
foreach ($id in $watermarkSeenIds) { $mergedIds.Add($id) | Out-Null }
foreach ($id in $newScanIds) { if (-not $mergedIds.Contains($id)) { $mergedIds.Add($id) } }
while ($mergedIds.Count -gt 200) { $mergedIds.RemoveAt(0) }  # FIFO rotation

$newWatermark = [ordered]@{ version = 1; lastScan = $now.ToString("yyyy-MM-ddTHH:mm:ssZ"); seenIds = @($mergedIds) }
$newWatermark | ConvertTo-Json -Depth 5 | Set-Content $watermarkPath -Encoding UTF8
Write-Host "   💾 Watermark saved: lastScan=$($newWatermark.lastScan), seenIds=$($mergedIds.Count)" -ForegroundColor DarkGray
Write-Host ""

if (-not $scanResult -or $scanResult -match "\|clear\|") {
    Write-Host "✅ No active incidents matching scan criteria" -ForegroundColor Green
    exit 0
}

# Print concise inline summary
$count = ($scanResult -split "\|")[2]

$newIncidents = @()
$skippedCount = 0
foreach ($inc in $incidents) {
    $parts = $inc -split "\|"
    if ($parts.Count -ge 6) {
        $icmId   = $parts[1]
        $sev     = $parts[2]
        $type    = $parts[3]
        $title   = $parts[4]
        $contact = $parts[5]
        
        $hasIssue  = $existingTitles | Where-Object { $_ -match $icmId }
        $hasReport = $existingReports -contains $icmId
        $inKnown   = $knownIdsList -contains $icmId
        
        if ($hasIssue -or $hasReport -or $inKnown) {
            $skippedCount++
            continue
        }
        
        $newIncidents += @{ IcmId=$icmId; Sev=$sev; Type=$type; Title=$title; Contact=$contact }
    }
}

if ($skippedCount -gt 0) {
    Write-Host "  ⏭️  $skippedCount known incident(s) skipped" -ForegroundColor DarkGray
}

if ($newIncidents.Count -eq 0) {
    Write-Host "✅ No new incidents" -ForegroundColor Green
    exit 0
}

Write-Host "🚨 $($newIncidents.Count) new incident(s):" -ForegroundColor Yellow
Write-Host ""

foreach ($inc in $newIncidents) {
    Write-Host "  • $($inc.Sev) [$($inc.Type)] IcM#$($inc.IcmId) — $($inc.Title) ($($inc.Contact))" -ForegroundColor White
}

if ($highlight) {
    $hlText = ($highlight -split "\|", 2)[1]
    Write-Host ""
    Write-Host "  ⚡ $hlText" -ForegroundColor Red
}

Write-Host ""

# Create GitHub issues for new incidents
$created = 0
$notifyLines = @()
foreach ($inc in $newIncidents) {
    $issueTitle = "ICM $($inc.IcmId): $($inc.Title)"
    $icmLink    = "https://portal.microsofticm.com/imp/v5/incidents/details/$($inc.IcmId)/home"

    $body = "## IcM Investigation Task`n`n" +
        "**IcM ID:** [$($inc.IcmId)]($icmLink)`n" +
        "**Severity:** $($inc.Sev)`n" +
        "**Type:** $($inc.Type)`n`n" +
        "Aragorn: investigate this incident using the ICM investigator skill.`n" +
        "Follow the pipeline in ``.squad/skills/icm-investigator/SKILL.md``."

    gh issue create --title $issueTitle --body $body --label "squad,squad:aragorn" --repo jbenami_microsoft/ms-pa 2>$null | Out-Null
    Write-Host "  📋 Created: $issueTitle" -ForegroundColor Green
    $notifyLines += "• **$($inc.Sev)** [$($inc.Type)] [IcM#$($inc.IcmId)]($icmLink) — $($inc.Title) ⚡ **NEW — Aragorn assigned**"
    $created++
}

if ($created -gt 0) {
    Write-Host ""
    Write-Host "✅ $created new investigation task(s) created on board" -ForegroundColor Green
}

# Send Teams notification ONLY for new incidents (not already-investigated ones)
$notifyScript = Join-Path $root "scripts\send-teams-notification.ps1"
if ((Test-Path $notifyScript) -and $notifyLines.Count -gt 0) {
    $notifyBody = ($notifyLines -join "`n")
    $notifyBody += "`n`n📋 $created new task(s) → Aragorn investigating"
    & $notifyScript -Title "🚨 IcM Scan: $created new incident(s)" -Body $notifyBody
}
