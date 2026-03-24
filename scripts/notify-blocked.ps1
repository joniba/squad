<#
.SYNOPSIS
    Sends an IMMEDIATE "blocked on human" notification via the squad notification pipeline.
    Calls notify.ps1 directly with the urgent tier — no batching.

.DESCRIPTION
    Standalone caller script invoked when work is blocked waiting for Jonathan.
    Uses the 🔴 urgent tier for immediate delivery. Scenarios include:
      - Elrond exhausts research options
      - Gandalf rejects a fix twice
      - Agent retry still fails after fix
      - Aragorn blocked during active livesite
      - Any agent needs a human decision

    Does NOT touch send-teams-notification.ps1 or any old-system callers.

.PARAMETER Title
    What's blocked — shown as the card header (e.g., "Blocked: DGrep auth requires VPN").

.PARAMETER Reason
    Why it needs Jonathan's attention.

.PARAMETER ActionNeeded
    Specific action Jonathan should take.

.PARAMETER BlockerUrl
    Link to the issue, PR, config, or incident.

.PARAMETER BlockerLabel
    Button label for the action URL (default: "View Blocker").

.PARAMETER Agent
    Which agent is blocked (e.g., "Gimli", "Aragorn").

.PARAMETER Severity
    Urgency classification: "blocking-feature", "livesite", or "decision-needed".

.PARAMETER DryRun
    Build the card and print JSON but don't send to Teams.

.PARAMETER Force
    Skip dedup check — send even if recently notified.

.EXAMPLE
    .\scripts\notify-blocked.ps1 `
        -Title "Blocked: DGrep auth requires Geneva VPN access" `
        -Reason "SDK dSTS auth requires VPN tunnel to corp network." `
        -ActionNeeded "Confirm VPN access for the build machine." `
        -BlockerUrl "https://github.com/jbenami_microsoft/ms-pa/issues/42" `
        -Agent "Gimli" `
        -Severity "blocking-feature" `
        -DryRun -Force
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Title,

    [Parameter(Mandatory)]
    [string]$Reason,

    [Parameter(Mandatory)]
    [string]$ActionNeeded,

    [string]$BlockerUrl,

    [string]$BlockerLabel = "View Blocker",

    [string]$Agent,

    [ValidateSet("blocking-feature", "livesite", "decision-needed")]
    [string]$Severity = "blocking-feature",

    [switch]$DryRun,

    [switch]$Force
)

$ErrorActionPreference = "Stop"

# Resolve notify.ps1
$notifyScript = Join-Path $PSScriptRoot "notify.ps1"
if (-not (Test-Path $notifyScript)) {
    Write-Error "notify.ps1 not found at: $notifyScript"
    exit 1
}

# Build the event payload — urgent tier, immediate delivery
$agentName = if ($Agent) { $Agent } else { "unknown" }
$timestamp = Get-Date -Format 'yyyy-MM-ddTHH-mm-ss'

$event = @{
    eventId     = "blocked:${agentName}:${timestamp}"
    errorType   = "needs-human"
    title       = $Title
    reason      = @"
**Why:** $Reason

**Action needed:** $ActionNeeded

**Agent:** $agentName
**Severity:** $Severity
"@
    actionUrl   = $BlockerUrl
    actionLabel = $BlockerLabel
}

# Call notify.ps1 directly — urgent tier bypasses the scheduler
$notifyParams = @{
    Type  = "urgent"
    Event = $event
}
if ($DryRun) { $notifyParams.DryRun = $true }
if ($Force)  { $notifyParams.Force  = $true }

try {
    $output = & $notifyScript @notifyParams *>&1 | Out-String
    Write-Host $output.TrimEnd()
} catch {
    Write-Error "Failed to send blocked notification: $_"
    exit 1
}
