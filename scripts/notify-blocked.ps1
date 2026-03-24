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

    Supports two modes:
      1. SINGLE-ISSUE (legacy): -Title, -Reason, -ActionNeeded, -BlockerUrl
      2. MULTI-ISSUE:  -Title, -ActionNeeded, -Issues (array of per-issue details)

    When -Issues is provided, the card shows each issue on its own line with a
    clickable link, per-issue blocker reason, and one action button per issue.

    Does NOT touch send-teams-notification.ps1 or any old-system callers.

.PARAMETER Title
    What's blocked — shown as the card header (e.g., "Blocked: DGrep auth requires VPN").

.PARAMETER Reason
    Why it needs Jonathan's attention (single-issue mode).
    Ignored when -Issues is provided.

.PARAMETER ActionNeeded
    Specific action Jonathan should take.

.PARAMETER Issues
    Array of hashtables for per-issue details. Each entry:
      @{ Number=89; Title="Issue title"; Url="https://..."; Reason="Why this is blocked" }
    When provided, -Reason and -BlockerUrl are ignored.

.PARAMETER BlockerUrl
    Link to the issue, PR, config, or incident (single-issue mode).
    Ignored when -Issues is provided.

.PARAMETER BlockerLabel
    Button label for the action URL (default: "View Blocker"). Single-issue mode only.

.PARAMETER Agent
    Which agent is blocked (e.g., "Gimli", "Aragorn").

.PARAMETER Severity
    Urgency classification: "blocking-feature", "livesite", or "decision-needed".

.PARAMETER DryRun
    Build the card and print JSON but don't send to Teams.

.PARAMETER Force
    Skip dedup check — send even if recently notified.

.EXAMPLE
    # Single-issue mode (backward compatible)
    .\scripts\notify-blocked.ps1 `
        -Title "Blocked: DGrep auth requires Geneva VPN access" `
        -Reason "SDK dSTS auth requires VPN tunnel to corp network." `
        -ActionNeeded "Confirm VPN access for the build machine." `
        -BlockerUrl "https://github.com/jbenami_microsoft/ms-pa/issues/42" `
        -Agent "Gimli" `
        -Severity "blocking-feature" `
        -DryRun -Force

.EXAMPLE
    # Multi-issue mode
    .\scripts\notify-blocked.ps1 `
        -Title "Blocked: 3 issues need credentials / access" `
        -ActionNeeded "Provide access credentials or unblock dependencies." `
        -Issues @(
            @{ Number=89;  Title="Credential setup for Geneva"; Url="https://github.com/jbenami_microsoft/ms-pa/issues/89"; Reason="dSTS credentials not configured" },
            @{ Number=90;  Title="VPN tunnel config";           Url="https://github.com/jbenami_microsoft/ms-pa/issues/90"; Reason="VPN access needed for build machine" }
        ) `
        -Agent "Gimli" `
        -Severity "blocking-feature" `
        -DryRun
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Title,

    [string]$Reason,

    [Parameter(Mandatory)]
    [string]$ActionNeeded,

    [array]$Issues,

    [string]$BlockerUrl,

    [string]$BlockerLabel = "View Blocker",

    [string]$Agent,

    [ValidateSet("blocking-feature", "livesite", "decision-needed")]
    [string]$Severity = "blocking-feature",

    [switch]$DryRun,

    [switch]$Force
)

$ErrorActionPreference = "Stop"

# Validate: either -Issues or -Reason must be provided
if (-not $Issues -and [string]::IsNullOrWhiteSpace($Reason)) {
    Write-Error "Either -Issues (multi-issue mode) or -Reason (single-issue mode) is required."
    exit 1
}

# Resolve notify.ps1
$notifyScript = Join-Path $PSScriptRoot "notify.ps1"
if (-not (Test-Path $notifyScript)) {
    Write-Error "notify.ps1 not found at: $notifyScript"
    exit 1
}

# Build the event payload — urgent tier, immediate delivery
$agentName = if ($Agent) { $Agent } else { "unknown" }
$timestamp = Get-Date -Format 'yyyy-MM-ddTHH-mm-ss'

if ($Issues -and $Issues.Count -gt 0) {
    # ── Multi-issue mode ──
    $event = @{
        eventId       = "blocked:${agentName}:${timestamp}"
        errorType     = "needs-human"
        title         = $Title
        actionNeeded  = $ActionNeeded
        agent         = $agentName
        severity      = $Severity
        blockedIssues = @($Issues | ForEach-Object {
            @{
                number = [int]$_.Number
                title  = "$($_.Title)"
                url    = "$($_.Url)"
                reason = "$($_.Reason)"
            }
        })
        # First issue URL as primary action (backward compat with notify.ps1 actions)
        actionUrl   = "$($Issues[0].Url)"
        actionLabel = "View #$($Issues[0].Number)"
    }
} else {
    # ── Single-issue mode (legacy) ──
    $event = @{
        eventId      = "blocked:${agentName}:${timestamp}"
        errorType    = "needs-human"
        title        = $Title
        actionNeeded = $ActionNeeded
        agent        = $agentName
        severity     = $Severity
        reason       = $Reason
        actionUrl    = $BlockerUrl
        actionLabel  = $BlockerLabel
    }
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
