<#
.SYNOPSIS
    Single entry-point dispatcher for all squad notification events.
    Routes to the correct caller script based on the -Event parameter.

.DESCRIPTION
    The coordinator, scheduler, and failure-recovery pipeline call this script
    instead of invoking notify-feature-complete.ps1 or notify-blocked.ps1 directly.
    This keeps the wiring stable — if caller scripts change names or parameters,
    only this dispatcher needs updating.

    Supported events:
      feature-complete  → scripts/notify-feature-complete.ps1
      blocked           → scripts/notify-blocked.ps1

    Does NOT replace old callers (send-teams-notification.ps1, squad-daily-summary.ps1).
    Those remain available for their existing consumers (icm-scan, daily summary).

.PARAMETER Event
    The event type to dispatch. Currently: "feature-complete" or "blocked".

.PARAMETER FeatureName
    [feature-complete] Human-readable feature name.

.PARAMETER Summary
    [feature-complete] What shipped — concise description.

.PARAMETER PRs
    [feature-complete] Comma-separated PR numbers (e.g., "#55,#56").

.PARAMETER DocLinks
    [feature-complete] Comma-separated documentation URLs.

.PARAMETER FeatureId
    [feature-complete] Optional unique ID. Defaults to a slug of FeatureName.

.PARAMETER TestInstructions
    [feature-complete] How to verify the feature works.

.PARAMETER What
    [blocked] What is blocked — shown as the card header.

.PARAMETER Why
    [blocked] Why it needs human attention.

.PARAMETER ActionNeeded
    [blocked] Specific action the human should take.

.PARAMETER Link
    [blocked] URL to the blocker (issue, PR, incident).

.PARAMETER Urgency
    [blocked] Severity: "blocking-feature", "livesite", or "decision-needed".

.PARAMETER Agent
    [blocked] Which agent is blocked.

.PARAMETER DryRun
    Build the card JSON but don't send to Teams.

.PARAMETER Force
    Skip dedup — send immediately.

.EXAMPLE
    .\scripts\notify-squad-event.ps1 -Event "feature-complete" `
        -FeatureName "Auth Flow" -Summary "Token caching shipped." `
        -PRs "#55,#56" -DocLinks "https://docs/auth.md"

.EXAMPLE
    .\scripts\notify-squad-event.ps1 -Event "blocked" `
        -What "DGrep auth needs VPN" -Why "dSTS requires corp tunnel" `
        -ActionNeeded "Enable VPN access" -Link "https://github.com/org/repo/issues/42" `
        -Urgency "blocking-feature"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet("feature-complete", "blocked")]
    [string]$Event,

    # --- feature-complete parameters ---
    [string]$FeatureName,
    [string]$Summary,
    [string]$PRs,
    [string]$DocLinks,
    [string]$FeatureId,
    [string]$TestInstructions,

    # --- blocked parameters ---
    [string]$What,
    [string]$Why,
    [string]$ActionNeeded,
    [string]$Link,
    [ValidateSet("blocking-feature", "livesite", "decision-needed", "")]
    [string]$Urgency = "blocking-feature",
    [string]$Agent,

    # --- common flags ---
    [switch]$DryRun,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Validate required parameters per event type
# ---------------------------------------------------------------------------
function Assert-Param {
    param([string]$Name, [string]$Value)
    if ([string]::IsNullOrWhiteSpace($Value)) {
        Write-Error "Parameter -$Name is required for event '$Event'."
        exit 1
    }
}

switch ($Event) {
    "feature-complete" {
        Assert-Param "FeatureName" $FeatureName
        Assert-Param "Summary"     $Summary
    }
    "blocked" {
        Assert-Param "What"         $What
        Assert-Param "Why"          $Why
        Assert-Param "ActionNeeded" $ActionNeeded
    }
}

# ---------------------------------------------------------------------------
# Resolve caller scripts relative to this script's directory
# ---------------------------------------------------------------------------
$scriptDir = $PSScriptRoot

switch ($Event) {
    # -------------------------------------------------------------------
    "feature-complete" {
        $callerScript = Join-Path $scriptDir "notify-feature-complete.ps1"
        if (-not (Test-Path $callerScript)) {
            Write-Error "Caller script not found: $callerScript (has squad/132 been merged?)"
            exit 1
        }

        # Build params — only pass non-empty values
        $params = @{
            FeatureTitle = $FeatureName
            Summary      = $Summary
        }

        # FeatureId: use explicit value or derive from FeatureName
        if ($FeatureId) {
            $params.FeatureId = $FeatureId
        } else {
            $slug = ($FeatureName -replace '[^a-zA-Z0-9\-]', '-').ToLower().TrimEnd('-')
            $params.FeatureId = $slug
        }

        if ($PRs) {
            # Normalise "55,56" → "#55, #56"
            $normalised = ($PRs -split ',') | ForEach-Object {
                $n = $_.Trim().TrimStart('#')
                "#$n"
            }
            $params.PRList = $normalised -join ', '
        }

        if ($DocLinks) {
            $params.IssuesUrl = ($DocLinks -split ',')[0].Trim()
        }

        if ($TestInstructions) {
            $params.TestInstructions = $TestInstructions
        }

        if ($DryRun) { $params.DryRun = $true }
        if ($Force)  { $params.Force  = $true }

        Write-Host "📢 Dispatching feature-complete → notify-feature-complete.ps1"
        & $callerScript @params
    }

    # -------------------------------------------------------------------
    "blocked" {
        $callerScript = Join-Path $scriptDir "notify-blocked.ps1"
        if (-not (Test-Path $callerScript)) {
            Write-Error "Caller script not found: $callerScript (has squad/132 been merged?)"
            exit 1
        }

        $params = @{
            Title        = $What
            Reason       = $Why
            ActionNeeded = $ActionNeeded
        }

        if ($Link)    { $params.BlockerUrl = $Link }
        if ($Urgency) { $params.Severity   = $Urgency }
        if ($Agent)   { $params.Agent      = $Agent }

        if ($DryRun) { $params.DryRun = $true }
        if ($Force)  { $params.Force  = $true }

        Write-Host "🚨 Dispatching blocked → notify-blocked.ps1"
        & $callerScript @params
    }
}
