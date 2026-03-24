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
      investigation-complete → scripts/notify-investigation-complete.ps1

    Does NOT replace old callers (send-teams-notification.ps1, squad-daily-summary.ps1).
    Those remain available for their existing consumers (icm-scan, daily summary).

.PARAMETER Event
    The event type to dispatch. Currently: "feature-complete", "blocked", or "investigation-complete".

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
    [blocked] Why it needs human attention (single-issue mode).
    Ignored when -Issues is provided.

.PARAMETER ActionNeeded
    [blocked] Specific action the human should take.

.PARAMETER Issues
    [blocked] JSON string with per-issue details. Array of objects, each with:
      number, title, url, reason.
    When provided, -Why and -Link are ignored; the card shows per-issue rows.

    Example JSON: '[{"number":89,"title":"Credential setup","url":"https://...","reason":"Need creds"}]'

.PARAMETER Link
    [blocked] URL to the blocker (single-issue mode). Ignored when -Issues is provided.

.PARAMETER Urgency
    [blocked] Severity: "blocking-feature", "livesite", or "decision-needed".

.PARAMETER Agent
    [blocked] Which agent is blocked.

.PARAMETER IcmNumber
    [investigation-complete] The incident management number (e.g., "123456789").

.PARAMETER Title
    [investigation-complete] The investigation title.

.PARAMETER Conclusion
    [investigation-complete] The investigation verdict (e.g., "false positive", "remediation needed", "design flaw").

.PARAMETER ReportUrl
    [investigation-complete] GitHub permalink to the investigation report (e.g., "https://github.com/org/repo/blob/main/docs/investigations/icm-123/report.md").

.PARAMETER IssueNumber
    [investigation-complete] GitHub issue number associated with the investigation.

.PARAMETER DryRun
    Build the card JSON but don't send to Teams.

.PARAMETER Force
    Skip dedup — send immediately.

.EXAMPLE
    .\scripts\notify-squad-event.ps1 -Event "feature-complete" `
        -FeatureName "Auth Flow" -Summary "Token caching shipped." `
        -PRs "#55,#56" -DocLinks "https://docs/auth.md"

.EXAMPLE
    # Single-issue blocked (legacy)
    .\scripts\notify-squad-event.ps1 -Event "blocked" `
        -What "DGrep auth needs VPN" -Why "dSTS requires corp tunnel" `
        -ActionNeeded "Enable VPN access" -Link "https://github.com/org/repo/issues/42" `
        -Urgency "blocking-feature"

.EXAMPLE
    # Multi-issue blocked (per-issue details)
    .\scripts\notify-squad-event.ps1 -Event "blocked" `
        -What "3 issues need credentials" `
        -ActionNeeded "Provide access credentials or unblock dependencies" `
        -Issues '[{"number":89,"title":"Credential setup","url":"https://github.com/jbenami_microsoft/ms-pa/issues/89","reason":"dSTS creds not configured"},{"number":90,"title":"VPN config","url":"https://github.com/jbenami_microsoft/ms-pa/issues/90","reason":"VPN access needed"}]' `
        -Agent "Gimli" -Urgency "blocking-feature" -DryRun
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet("feature-complete", "blocked", "investigation-complete")]
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
    [string]$Issues,
    [string]$Link,
    [ValidateSet("blocking-feature", "livesite", "decision-needed", "")]
    [string]$Urgency = "blocking-feature",
    [string]$Agent,

    # --- investigation-complete parameters ---
    [string]$IcmNumber,
    [string]$Title,
    [string]$Conclusion,
    [string]$ReportUrl,
    [string]$IssueNumber,

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
        Assert-Param "ActionNeeded" $ActionNeeded
        # Either -Issues (multi-issue) or -Why (single-issue) is required
        if ([string]::IsNullOrWhiteSpace($Issues) -and [string]::IsNullOrWhiteSpace($Why)) {
            Write-Error "Either -Issues (multi-issue mode) or -Why (single-issue mode) is required for event 'blocked'."
            exit 1
        }
    }
    "investigation-complete" {
        Assert-Param "IcmNumber"    $IcmNumber
        Assert-Param "Title"        $Title
        Assert-Param "Conclusion"   $Conclusion
        Assert-Param "ReportUrl"    $ReportUrl
        Assert-Param "IssueNumber"  $IssueNumber
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
            $links = @(($DocLinks -split ',') | ForEach-Object { $_.Trim() } | Where-Object { $_ })
            if ($links.Count -gt 1) {
                Write-Warning "-DocLinks received $($links.Count) values; only the first will be forwarded: $($links[0])"
            }
            $params.IssuesUrl = $links[0]
        }

        if ($TestInstructions) {
            $params.TestInstructions = $TestInstructions
        }

        if ($DryRun) { $params.DryRun = $true }
        if ($Force)  { $params.Force  = $true }

        Write-Host "📢 Dispatching feature-complete → notify-feature-complete.ps1"
        & $callerScript @params
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
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
            ActionNeeded = $ActionNeeded
        }

        # Multi-issue mode: parse JSON, pass as array
        if ($Issues) {
            try {
                $parsedIssues = $Issues | ConvertFrom-Json
                # Convert PSObjects to hashtables for downstream compatibility
                $issueArray = @($parsedIssues | ForEach-Object {
                    @{
                        Number = $_.number
                        Title  = $_.title
                        Url    = $_.url
                        Reason = $_.reason
                    }
                })
                $params.Issues = $issueArray
            } catch {
                Write-Error "Failed to parse -Issues JSON: $_"
                exit 1
            }
        } else {
            # Single-issue mode (legacy)
            $params.Reason = $Why
            if ($Link) { $params.BlockerUrl = $Link }
        }

        if ($Urgency) { $params.Severity   = $Urgency }
        if ($Agent)   { $params.Agent      = $Agent }

        if ($DryRun) { $params.DryRun = $true }
        if ($Force)  { $params.Force  = $true }

        Write-Host "🚨 Dispatching blocked → notify-blocked.ps1"
        & $callerScript @params
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }

    # -------------------------------------------------------------------
    "investigation-complete" {
        $callerScript = Join-Path $scriptDir "notify-investigation-complete.ps1"
        if (-not (Test-Path $callerScript)) {
            Write-Error "Caller script not found: $callerScript"
            exit 1
        }

        $params = @{
            IcmNumber   = $IcmNumber
            Title       = $Title
            Conclusion  = $Conclusion
            ReportUrl   = $ReportUrl
            IssueNumber = $IssueNumber
        }

        if ($DryRun) { $params.DryRun = $true }
        if ($Force)  { $params.Force  = $true }

        Write-Host "🔍 Dispatching investigation-complete → notify-investigation-complete.ps1"
        & $callerScript @params
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
}
