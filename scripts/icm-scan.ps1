<#
.SYNOPSIS
    Invokes Aragorn via copilot -p to scan IcM for active incidents.
.DESCRIPTION
    Uses copilot -p --yolo --no-ask-user for LLM-in-the-loop IcM scanning with full
    MCP tool access. Aragorn scans, prints a concise inline summary, and creates
    GitHub issues for each incident needing investigation. NO report files are created
    — scans trigger tasks, not documents.
.PARAMETER TeamId
    IcM team ID (default: 116041 = DAKOTA\ThreatIntelligence)
.PARAMETER Model
    LLM model to use (default: claude-sonnet-4.6)
.PARAMETER DryRun
    Print the prompt and exit without invoking the LLM.
.EXAMPLE
    .\scripts\icm-scan.ps1
    .\scripts\icm-scan.ps1 -DryRun
#>
param(
    [string]$TeamId = "116041",
    [string]$Model = "claude-sonnet-4.6",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent

$prompt = @"
You are Aragorn, the Livesite Responder for the pa-squad team.

## Task: IcM Scan — Team $TeamId (DAKOTA\ThreatIntelligence)

Use icm-search_incidents_by_owning_team_id with teamId $TeamId to retrieve all active
incidents. Filter client-side using the criteria below.

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

if ($DryRun) {
    Write-Host "DRY RUN — would invoke: copilot -p with model $Model" -ForegroundColor Yellow
    Write-Host ""
    Write-Host $prompt
    exit 0
}

Write-Host "🔍 IcM scan — team $TeamId" -ForegroundColor Cyan
Write-Host "   Query: icm-search_incidents_by_owning_team_id (teamId=$TeamId)" -ForegroundColor DarkGray
Write-Host "   Filter: Sev0-2 (any type) + CRIs (System/Customer Reported, any sev) + Sev2.5 candidates" -ForegroundColor DarkGray
Write-Host "   Exclude: bare Sev3, Sev4-5, Mitigated/Resolved/False Positive" -ForegroundColor DarkGray
Write-Host ""

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
$incidents = $lines | Where-Object { $_ -match "^INCIDENT\|" }
$highlight = $lines | Where-Object { $_ -match "^HIGHLIGHT\|" } | Select-Object -First 1

if (-not $scanResult -or $scanResult -match "\|clear\|") {
    Write-Host "✅ No active incidents matching scan criteria" -ForegroundColor Green
    exit 0
}

# Print concise inline summary
$count = ($scanResult -split "\|")[2]
Write-Host "🚨 $count incident(s) found:" -ForegroundColor Yellow
Write-Host ""

$issuesCreated = @()
foreach ($inc in $incidents) {
    $parts = $inc -split "\|"
    if ($parts.Count -ge 6) {
        $icmId = $parts[1]
        $sev = $parts[2]
        $type = $parts[3]
        $title = $parts[4]
        $contact = $parts[5]
        Write-Host "  • $sev [$type] IcM#$icmId — $title ($contact)" -ForegroundColor White
        $issuesCreated += @{ IcmId=$icmId; Sev=$sev; Type=$type; Title=$title }
    }
}

if ($highlight) {
    $hlText = ($highlight -split "\|", 2)[1]
    Write-Host ""
    Write-Host "  ⚡ $hlText" -ForegroundColor Red
}

Write-Host ""

# Create GitHub issues for new incidents (dedup against existing issues + investigation reports)
$existingIssues = gh issue list --label "squad,squad:aragorn" --state all --json title --limit 100 --repo jbenami_microsoft/ms-pa 2>$null | ConvertFrom-Json
$existingTitles = $existingIssues | ForEach-Object { $_.title }

# Also check for existing investigation reports in docs/investigations/
$investigationsDir = Join-Path $root "docs\investigations"
$existingReports = @()
if (Test-Path $investigationsDir) {
    $existingReports = Get-ChildItem -Path $investigationsDir -Filter "icm-*" -Name | ForEach-Object {
        if ($_ -match "icm-(\d+)") { $Matches[1] }
    }
}

$created = 0
$notifyLines = @()
foreach ($inc in $issuesCreated) {
    $issueTitle = "ICM $($inc.IcmId): $($inc.Title)"
    $icmLink = "https://portal.microsofticm.com/imp/v5/incidents/details/$($inc.IcmId)/home"
    
    # Check 1: existing GitHub issue (any state — open or closed)
    $hasIssue = $existingTitles | Where-Object { $_ -match $inc.IcmId }
    
    # Check 2: existing investigation report
    $hasReport = $existingReports -contains $inc.IcmId
    
    if ($hasIssue) {
        Write-Host "  ⏭️  IcM#$($inc.IcmId) (issue exists on board)" -ForegroundColor DarkGray
        $notifyLines += "• **$($inc.Sev)** [$($inc.Type)] [IcM#$($inc.IcmId)]($icmLink) — $($inc.Title) _(tracked)_"
        continue
    }
    
    if ($hasReport) {
        Write-Host "  ⏭️  IcM#$($inc.IcmId) (investigation report exists)" -ForegroundColor DarkGray
        $notifyLines += "• **$($inc.Sev)** [$($inc.Type)] [IcM#$($inc.IcmId)]($icmLink) — $($inc.Title) _(investigated)_"
        continue
    }

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

# Send Teams notification with incident links + tracking status
$notifyScript = Join-Path $root "scripts\send-teams-notification.ps1"
if ((Test-Path $notifyScript) -and $notifyLines.Count -gt 0) {
    $notifyBody = ($notifyLines -join "`n")
    if ($created -gt 0) { $notifyBody += "`n`n📋 $created new task(s) → Aragorn investigating" }
    else { $notifyBody += "`n`n✅ All incidents already tracked" }
    & $notifyScript -Title "🚨 IcM Scan: $count incident(s)" -Body $notifyBody
}
