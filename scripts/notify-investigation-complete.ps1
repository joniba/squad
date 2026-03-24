<#
.SYNOPSIS
    Sends an "investigation complete" notification via the squad notification pipeline.
    Calls notify.ps1 through the notification-scheduler for batched delivery.

.DESCRIPTION
    Standalone caller script invoked by the coordinator after an Aragorn investigation
    completes and the PR is merged. Uses the 🔍 feature tier (batched hourly or ≥5 items).

    Does NOT touch send-teams-notification.ps1 or any old-system callers.

.PARAMETER IcmNumber
    The incident management number (e.g., "123456789").

.PARAMETER Title
    The investigation title (e.g., "Investigate: Elevated error rates in region US-East").

.PARAMETER Conclusion
    The investigation verdict (e.g., "false positive", "remediation needed", "design flaw").

.PARAMETER ReportUrl
    GitHub permalink to the investigation report (e.g., "https://github.com/jbenami_microsoft/ms-pa/blob/main/docs/investigations/icm-123/report.md").

.PARAMETER IssueNumber
    GitHub issue number associated with the investigation (e.g., "42").

.PARAMETER DryRun
    Build the card and print JSON but don't send to Teams.

.PARAMETER Force
    Skip dedup and batching — send immediately.

.EXAMPLE
    .\scripts\notify-investigation-complete.ps1 `
        -IcmNumber "123456789" `
        -Title "Investigate: Elevated error rates in region US-East" `
        -Conclusion "False positive — DNS propagation delay, now resolved." `
        -ReportUrl "https://github.com/jbenami_microsoft/ms-pa/blob/main/docs/investigations/icm-123456789/report.md" `
        -IssueNumber "42" `
        -DryRun -Force
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$IcmNumber,

    [Parameter(Mandatory)]
    [string]$Title,

    [Parameter(Mandatory)]
    [string]$Conclusion,

    [Parameter(Mandatory)]
    [string]$ReportUrl,

    [Parameter(Mandatory)]
    [string]$IssueNumber,

    [switch]$DryRun,

    [switch]$Force
)

$ErrorActionPreference = "Stop"

# Dot-source the notification scheduler
$schedulerScript = Join-Path $PSScriptRoot "notification-scheduler.ps1"
if (-not (Test-Path $schedulerScript)) {
    Write-Error "notification-scheduler.ps1 not found at: $schedulerScript"
    exit 1
}
. $schedulerScript

# Ensure default triggers are registered (idempotent)
Initialize-DefaultTriggers

# Build the event data payload
$eventData = @{
    icmNumber    = $IcmNumber
    title        = $Title
    featureTitle = $Title  # For feature queue; same as title
    summary      = "IcM $IcmNumber - $Conclusion"  # For feature queue summary
    conclusion   = $Conclusion
    reportUrl    = $ReportUrl
    issueNumber  = $IssueNumber
}

# Fire through the scheduler pipeline
$result = Invoke-ScheduledNotification `
    -EventName "investigation-complete" `
    -EventData $eventData `
    -DryRun:$DryRun `
    -Force:$Force

if ($result.Sent) {
    Write-Host "✅ Investigation-complete notification dispatched: IcM $IcmNumber - $Title"
} elseif ($result.Reason -eq "disabled") {
    Write-Host "ℹ️ Investigation-complete trigger is disabled."
} elseif ($result.Reason -eq "unknown-trigger") {
    Write-Warning "Investigation-complete trigger not registered. Run Initialize-DefaultTriggers first."
} else {
    # Intentional: notification failure is non-blocking; investigation completion has already occurred.
    Write-Warning "Investigation-complete notification not sent: $($result.Reason)"
}
