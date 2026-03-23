<#
.SYNOPSIS
    Invokes Aragorn via copilot -p to scan IcM for active incidents.
.DESCRIPTION
    Uses copilot -p --yolo --no-ask-user for LLM-in-the-loop IcM scanning with full
    MCP tool access. Aragorn reasons about the results and produces a structured markdown
    report saved via --share to docs/investigations/. Sends a Teams notification if
    incidents are found.
.PARAMETER TeamId
    IcM team ID (default: 116041 = DAKOTA\ThreatIntelligence)
.PARAMETER Model
    LLM model to use (default: claude-sonnet-4.6; use claude-opus-4.5 for deep investigation)
.PARAMETER DryRun
    Print the prompt and exit without invoking the LLM.
.EXAMPLE
    .\scripts\icm-scan.ps1
    .\scripts\icm-scan.ps1 -DryRun
    .\scripts\icm-scan.ps1 -TeamId "116041" -Model "claude-opus-4.5"
#>
param(
    [string]$TeamId = "116041",
    [string]$Model = "claude-sonnet-4.6",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$reportPath = Join-Path $root "docs\investigations\icm-scan-$timestamp.md"
New-Item -ItemType Directory -Path (Split-Path $reportPath -Parent) -Force | Out-Null

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

## For Each Matching Incident, Report

IcM ID | Severity | Title | Incident Type | Created Date | Owning Contact | State

## Required Output

Produce a structured markdown report:

### IcM Scan — $(Get-Date -Format "yyyy-MM-dd HH:mm") UTC

**Team:** $TeamId (DAKOTA\ThreatIntelligence)
**Filter:** Sev0–2 (all types) + CRIs (any sev) + Sev2.5 candidates

#### Summary
Total: N | Sev0: N | Sev1: N | Sev2: N | CRIs: N | Sev2.5: N

#### Active Incidents
[Markdown table with all matching incidents]

#### Highlights
[One-line callout for any Sev0/Sev1 or high-impact CRIs needing immediate attention]

If NO incidents match, output exactly: No active incidents matching scan criteria.
"@

if ($DryRun) {
    Write-Host "DRY RUN — would invoke: copilot -p with model $Model" -ForegroundColor Yellow
    Write-Host "Report path: $reportPath" -ForegroundColor Yellow
    Write-Host ""
    Write-Host $prompt
    exit 0
}

Write-Host "🔍 IcM scan starting — team $TeamId | model $Model" -ForegroundColor Cyan
Write-Host "   Report: $reportPath"

$output = copilot -p $prompt `
    --yolo `
    --no-ask-user `
    --model $Model `
    --share="$reportPath" `
    -s 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Error "copilot -p failed (exit $LASTEXITCODE)"
    exit $LASTEXITCODE
}

Write-Host "✅ Scan complete" -ForegroundColor Green
Write-Host "   Report: $reportPath"

# Send Teams notification if incidents were found
$foundIncidents = ($output -match "Sev[0-2]|IcM#|System/Customer Reported") -and
                  ($output -notmatch "No active incidents matching scan criteria")
if ($foundIncidents) {
    $notifyScript = Join-Path $root "scripts\send-teams-notification.ps1"
    if (Test-Path $notifyScript) {
        & $notifyScript -Title "🚨 IcM Scan — Incidents Found" `
            -Body "Active incidents detected for team $TeamId. Report: $reportPath"
    }
}
