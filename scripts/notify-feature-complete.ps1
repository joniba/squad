<#
.SYNOPSIS
    Sends a "feature complete" notification via the squad notification pipeline.
    Calls notify.ps1 through the notification-scheduler for batched delivery.

.DESCRIPTION
    Standalone caller script invoked by the coordinator after the last PR in a
    feature track is merged, or by any agent that completes a multi-step feature.
    Uses the 🔵 feature tier (batched hourly or ≥5 items).

    Does NOT touch send-teams-notification.ps1 or any old-system callers.

.PARAMETER FeatureId
    Unique identifier for the feature (e.g., "dgrep-cli-phase1" or issue number).

.PARAMETER FeatureTitle
    Human-readable feature name shown in the card header.

.PARAMETER Summary
    What shipped — concise description (markdown OK).

.PARAMETER TestInstructions
    How Jonathan can verify the feature works.

.PARAMETER IssuesUrl
    Link to the GitHub issue(s) or project board.

.PARAMETER PRList
    Comma-separated PR numbers (e.g., "#55, #56, #57").

.PARAMETER DryRun
    Build the card and print JSON but don't send to Teams.

.PARAMETER Force
    Skip dedup and batching — send immediately.

.EXAMPLE
    .\scripts\notify-feature-complete.ps1 `
        -FeatureId "dgrep-cli-phase1" `
        -FeatureTitle "DGrep CLI — Phase 1 Foundation" `
        -Summary "CLI arg parsing, config management, and output formatters shipped." `
        -TestInstructions "cd tools/dgrep-cli && dotnet build && dotnet test" `
        -IssuesUrl "https://github.com/jbenami_microsoft/ms-pa/issues/101" `
        -PRList "#122, #125, #127" `
        -DryRun -Force
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$FeatureId,

    [Parameter(Mandatory)]
    [string]$FeatureTitle,

    [Parameter(Mandatory)]
    [string]$Summary,

    [string]$TestInstructions,

    [string]$IssuesUrl,

    [string]$PRList,

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
    featureId        = $FeatureId
    featureTitle     = $FeatureTitle
    summary          = $Summary
    testInstructions = $TestInstructions
    issuesUrl        = $IssuesUrl
    nextAction       = if ($PRList) { "PRs: $PRList" } else { $null }
}

# Fire through the scheduler pipeline
$result = Invoke-ScheduledNotification `
    -EventName "feature-complete" `
    -EventData $eventData `
    -DryRun:$DryRun `
    -Force:$Force

if ($result.Sent) {
    Write-Host "✅ Feature-complete notification dispatched: $FeatureTitle"
} elseif ($result.Reason -eq "disabled") {
    Write-Host "ℹ️ Feature-complete trigger is disabled."
} elseif ($result.Reason -eq "unknown-trigger") {
    Write-Warning "Feature-complete trigger not registered. Run Initialize-DefaultTriggers first."
} else {
    Write-Warning "Feature-complete notification not sent: $($result.Reason)"
}
