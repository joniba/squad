<#
.SYNOPSIS
    Event-driven notification scheduler. Maps squad events to notification triggers
    and fires them through the notify.ps1 pipeline.

.DESCRIPTION
    Four exported functions:
      - Register-NotificationTrigger:   Add event → notification mapping
      - Unregister-NotificationTrigger: Remove a trigger by name
      - Get-RegisteredTriggers:         List active triggers
      - Invoke-ScheduledNotification:   Fire a notification for a named event

    Trigger config: ~/.squad/notifications/triggers.json
    Hot-reloads config on every Invoke- call (no restart needed).

    Built-in event types:
      - icm-scan-urgent:     🔴 New sev2+ incidents from ICM scan
      - icm-scan-action:     🟡 Findings (non-urgent) from ICM scan
      - pr-review-complete:  🟡 PR review approved/rejected
      - build-test-failure:  🔴 Build or test failure
      - feature-complete:    🔵 Feature work finished (batched)
      - investigation-complete: 🔍 Investigation completed (batched)
      - ralph-round-complete: 🔵 Ralph round summary (batched)

.EXAMPLE
    . .\scripts\notification-scheduler.ps1
    Register-NotificationTrigger -Name "icm-scan-urgent" -EventType "icm-scan" `
        -Tier "urgent" -Template "icm-urgent" -Enabled $true
    Invoke-ScheduledNotification -EventName "icm-scan-urgent" -EventData @{ ... }
#>

$ErrorActionPreference = "Stop"

# ── Defaults ──

$script:DefaultTriggersFile = Join-Path $HOME ".squad\notifications\triggers.json"
$script:TriggersCache       = $null
$script:TriggersCacheTime   = $null

#region ── Config I/O ──

function Read-TriggersConfig {
    <#
    .SYNOPSIS
        Load triggers from JSON config. Hot-reloads if file changed since last read.
    .PARAMETER TriggersFile
        Path to triggers.json. Default: ~/.squad/notifications/triggers.json
    .PARAMETER Force
        Force reload even if cache is fresh.
    #>
    [CmdletBinding()]
    param(
        [string]$TriggersFile,
        [switch]$Force
    )

    if (-not $TriggersFile) { $TriggersFile = $script:DefaultTriggersFile }

    if (-not (Test-Path $TriggersFile)) {
        return @{
            version  = 1
            triggers = @{}
        }
    }

    # Hot-reload: check file timestamp
    $fileInfo = Get-Item $TriggersFile
    if (-not $Force -and $script:TriggersCache -and $script:TriggersCacheTime) {
        if ($fileInfo.LastWriteTimeUtc -le $script:TriggersCacheTime) {
            return $script:TriggersCache
        }
    }

    try {
        $config = Get-Content $TriggersFile -Raw | ConvertFrom-Json -AsHashtable
        if (-not $config.triggers) { $config.triggers = @{} }
        $script:TriggersCache     = $config
        $script:TriggersCacheTime = $fileInfo.LastWriteTimeUtc
        return $config
    } catch {
        Write-Warning "Corrupt triggers config, returning empty: $_"
        return @{ version = 1; triggers = @{} }
    }
}

function Save-TriggersConfig {
    <#
    .SYNOPSIS
        Persist triggers config to JSON file.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,

        [string]$TriggersFile
    )

    if (-not $TriggersFile) { $TriggersFile = $script:DefaultTriggersFile }

    $dir = Split-Path $TriggersFile
    if ($dir -and -not (Test-Path $dir)) {
        New-Item -Path $dir -ItemType Directory -Force | Out-Null
    }

    $Config | ConvertTo-Json -Depth 10 | Set-Content $TriggersFile -Encoding UTF8

    # Invalidate cache so next read picks up the write
    $script:TriggersCache     = $null
    $script:TriggersCacheTime = $null
}

#endregion

#region ── Trigger Management ──

function Register-NotificationTrigger {
    <#
    .SYNOPSIS
        Register an event → notification trigger mapping.
    .PARAMETER Name
        Unique trigger name (e.g., "icm-scan-urgent").
    .PARAMETER EventType
        Source event type (e.g., "icm-scan", "pr-review", "build", "feature", "ralph").
    .PARAMETER Tier
        Notification tier: "urgent", "action", or "feature".
    .PARAMETER Template
        Template name for card formatting. Maps to event data transformation.
    .PARAMETER Enabled
        Whether the trigger is active. Default: $true.
    .PARAMETER TriggersFile
        Override config file path (for testing).
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$EventType,

        [Parameter(Mandatory)]
        [ValidateSet("urgent", "action", "feature")]
        [string]$Tier,

        [Parameter(Mandatory)]
        [string]$Template,

        [bool]$Enabled = $true,

        [string]$TriggersFile
    )

    $config = Read-TriggersConfig -TriggersFile $TriggersFile -Force

    $config.triggers[$Name] = @{
        eventType  = $EventType
        tier       = $Tier
        template   = $Template
        enabled    = $Enabled
        createdAt  = (Get-Date -Format 'o')
    }

    Save-TriggersConfig -Config $config -TriggersFile $TriggersFile
    Write-Host "✅ Trigger registered: $Name → $Tier ($Template)"
    return $config.triggers[$Name]
}

function Unregister-NotificationTrigger {
    <#
    .SYNOPSIS
        Remove a trigger by name.
    .PARAMETER Name
        Trigger name to remove.
    .PARAMETER TriggersFile
        Override config file path.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [string]$TriggersFile
    )

    $config = Read-TriggersConfig -TriggersFile $TriggersFile -Force

    if (-not $config.triggers.ContainsKey($Name)) {
        Write-Warning "Trigger '$Name' not found."
        return $false
    }

    $config.triggers.Remove($Name)
    Save-TriggersConfig -Config $config -TriggersFile $TriggersFile
    Write-Host "🗑️ Trigger removed: $Name"
    return $true
}

function Get-RegisteredTriggers {
    <#
    .SYNOPSIS
        List all registered triggers with their configuration.
    .PARAMETER TriggersFile
        Override config file path.
    .PARAMETER EnabledOnly
        Only return enabled triggers.
    #>
    [CmdletBinding()]
    param(
        [string]$TriggersFile,
        [switch]$EnabledOnly
    )

    $config = Read-TriggersConfig -TriggersFile $TriggersFile

    $results = @()
    foreach ($name in $config.triggers.Keys) {
        $trigger = $config.triggers[$name]
        if ($EnabledOnly -and -not $trigger.enabled) { continue }
        $results += [PSCustomObject]@{
            Name      = $name
            EventType = $trigger.eventType
            Tier      = $trigger.tier
            Template  = $trigger.template
            Enabled   = $trigger.enabled
        }
    }
    return $results
}

#endregion

#region ── Event Templates ──

function Build-EventFromTemplate {
    <#
    .SYNOPSIS
        Transform raw event data into a notify.ps1-compatible Event hashtable
        using the trigger's template.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Template,

        [Parameter(Mandatory)]
        [string]$Tier,

        [Parameter(Mandatory)]
        [hashtable]$EventData
    )

    $now = Get-Date -Format 'o'

    switch ($Template) {
        "icm-urgent" {
            $count = $EventData.incidentCount ?? 1
            $highlight = $EventData.highlight ?? "New incidents detected"
            $incidents = $EventData.incidents ?? @()
            $incidentLines = @($incidents | ForEach-Object {
                "• **$($_.Sev)** [$($_.Type)] IcM#$($_.IcmId) — $($_.Title)"
            })
            $body = if ($incidentLines.Count -gt 0) {
                ($incidentLines -join "`n") + "`n`n$highlight"
            } else { $highlight }

            return @{
                eventId     = "icm-scan-urgent:$now"
                errorType   = "livesite"
                title       = "IcM Scan: $count new sev2+ incident(s)"
                reason      = $body
                actionUrl   = $EventData.actionUrl ?? ""
                actionLabel = $EventData.actionLabel ?? "View Incidents"
            }
        }
        "icm-action" {
            return @{
                eventId       = "icm-scan-action:$now"
                actionType    = "icm-findings"
                title         = "IcM Scan: $($EventData.findingCount ?? 0) finding(s)"
                reason        = $EventData.summary ?? "ICM scan completed with findings"
                actionUrl     = $EventData.actionUrl ?? ""
                actionLabel   = $EventData.actionLabel ?? "View Findings"
                estimatedTime = "~2 min review"
            }
        }
        "pr-review" {
            $status = $EventData.reviewStatus ?? "completed"
            $prNum  = $EventData.prNumber ?? "?"
            $prTitle = $EventData.prTitle ?? "Pull Request"
            return @{
                eventId       = "pr-review:$prNum`:$now"
                actionType    = "pr-review"
                title         = "PR #$prNum $status`: $prTitle"
                reason        = $EventData.summary ?? "Review $status by $($EventData.reviewer ?? 'reviewer')"
                actionUrl     = $EventData.actionUrl ?? ""
                actionLabel   = "Review PR"
                estimatedTime = $EventData.estimatedTime ?? "~5 min read"
                currentState  = $status
            }
        }
        "build-failure" {
            return @{
                eventId     = "build-failure:$($EventData.buildId ?? 'unknown'):$now"
                errorType   = "script-failure"
                title       = "Build/Test Failure: $($EventData.buildName ?? 'unknown')"
                reason      = $EventData.error ?? "Build or test failed"
                actionUrl   = $EventData.actionUrl ?? ""
                actionLabel = $EventData.actionLabel ?? "View Build Log"
            }
        }
        "feature-complete" {
            return @{
                eventId          = "feature:$($EventData.featureId ?? 'unknown'):$now"
                featureTitle     = $EventData.featureTitle ?? "Feature Complete"
                summary          = $EventData.summary ?? ""
                testInstructions = $EventData.testInstructions ?? ""
                issuesUrl        = $EventData.issuesUrl ?? ""
                nextAction       = $EventData.nextAction ?? ""
                nextActionDue    = $EventData.nextActionDue ?? ""
            }
        }
        "ralph-round" {
            $items = $EventData.workItems ?? @()
            $summaryLines = @($items | ForEach-Object {
                "• $($_.status ?? '✅') $($_.title ?? 'Task')"
            })
            $body = if ($summaryLines.Count -gt 0) {
                $summaryLines -join "`n"
            } else { $EventData.summary ?? "Ralph round completed" }

            return @{
                eventId          = "ralph-round:$($EventData.roundId ?? 'unknown'):$now"
                featureTitle     = "Ralph Round Complete"
                summary          = $body
                testInstructions = $EventData.testInstructions ?? ""
                issuesUrl        = $EventData.issuesUrl ?? ""
            }
        }
        "investigation-complete" {
            return @{
                eventId      = "investigation:$($EventData.icmNumber ?? 'unknown'):$now"
                title        = $EventData.title ?? "Investigation Complete"
                featureTitle = $EventData.featureTitle ?? $EventData.title ?? "Investigation Complete"
                summary      = $EventData.summary ?? "Investigation complete"
                icmNumber    = $EventData.icmNumber ?? ""
                conclusion   = $EventData.conclusion ?? ""
                reportUrl    = $EventData.reportUrl ?? ""
                issueNumber  = $EventData.issueNumber ?? ""
            }
        }
        default {
            # Pass through raw event data — caller is responsible for structure
            if (-not $EventData.eventId) {
                $EventData.eventId = "${Template}:$now"
            }
            return $EventData
        }
    }
}

#endregion

#region ── Invocation ──

function Invoke-ScheduledNotification {
    <#
    .SYNOPSIS
        Fire a notification for a named event. Looks up the trigger config,
        builds the event payload from template, and calls notify.ps1.
    .PARAMETER EventName
        The trigger name to fire (must be registered).
    .PARAMETER EventData
        Raw event data hashtable. Transformed via the trigger's template.
    .PARAMETER TriggersFile
        Override triggers config path.
    .PARAMETER NotifyScript
        Override path to notify.ps1.
    .PARAMETER DryRun
        Pass -DryRun to notify.ps1.
    .PARAMETER Force
        Pass -Force to notify.ps1 (skip dedup).
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$EventName,

        [Parameter(Mandatory)]
        [hashtable]$EventData,

        [string]$TriggersFile,
        [string]$NotifyScript,
        [switch]$DryRun,
        [switch]$Force
    )

    # Hot-reload config from disk
    $config = Read-TriggersConfig -TriggersFile $TriggersFile

    # Look up trigger
    if (-not $config.triggers.ContainsKey($EventName)) {
        Write-Warning "No trigger registered for event '$EventName'. Notification skipped."
        return @{ Sent = $false; Reason = "unknown-trigger" }
    }

    $trigger = $config.triggers[$EventName]

    # Check enabled flag
    if (-not $trigger.enabled) {
        Write-Host "ℹ️ Trigger '$EventName' is disabled. Notification skipped."
        return @{ Sent = $false; Reason = "disabled" }
    }

    # Build event payload from template
    $event = Build-EventFromTemplate -Template $trigger.template -Tier $trigger.tier -EventData $EventData

    # Resolve notify.ps1 path
    if (-not $NotifyScript) {
        $NotifyScript = Join-Path $PSScriptRoot "notify.ps1"
    }
    if (-not (Test-Path $NotifyScript)) {
        Write-Error "notify.ps1 not found at: $NotifyScript"
        return @{ Sent = $false; Reason = "notify-script-missing" }
    }

    # Build args
    $notifyParams = @{
        Type  = $trigger.tier
        Event = $event
    }
    if ($DryRun) { $notifyParams.DryRun = $true }
    if ($Force)  { $notifyParams.Force  = $true }

    # Fire notification
    try {
        $output = & $NotifyScript @notifyParams *>&1 | Out-String
        Write-Host $output.TrimEnd()
        return @{ Sent = $true; Tier = $trigger.tier; Template = $trigger.template; Output = $output }
    } catch {
        Write-Warning "Notification failed for '$EventName': $_"
        return @{ Sent = $false; Reason = "send-error"; Error = "$_" }
    }
}

#endregion

#region ── Default Triggers ──

function Initialize-DefaultTriggers {
    <#
    .SYNOPSIS
        Register the built-in squad event triggers if not already present.
    .PARAMETER TriggersFile
        Override config file path.
    #>
    [CmdletBinding()]
    param(
        [string]$TriggersFile
    )

    $defaults = @(
        @{ Name = "icm-scan-urgent";       EventType = "icm-scan";    Tier = "urgent";  Template = "icm-urgent" }
        @{ Name = "icm-scan-action";       EventType = "icm-scan";    Tier = "action";  Template = "icm-action" }
        @{ Name = "pr-review-complete";    EventType = "pr-review";   Tier = "action";  Template = "pr-review" }
        @{ Name = "build-test-failure";    EventType = "build";       Tier = "urgent";  Template = "build-failure" }
        @{ Name = "feature-complete";      EventType = "feature";     Tier = "feature"; Template = "feature-complete" }
        @{ Name = "investigation-complete"; EventType = "investigation"; Tier = "feature"; Template = "investigation-complete" }
        @{ Name = "ralph-round-complete";  EventType = "ralph";       Tier = "feature"; Template = "ralph-round" }
    )

    $config = Read-TriggersConfig -TriggersFile $TriggersFile -Force

    $registered = 0
    foreach ($def in $defaults) {
        if (-not $config.triggers.ContainsKey($def.Name)) {
            $config.triggers[$def.Name] = @{
                eventType = $def.EventType
                tier      = $def.Tier
                template  = $def.Template
                enabled   = $true
                createdAt = (Get-Date -Format 'o')
            }
            $registered++
        }
    }

    if ($registered -gt 0) {
        Save-TriggersConfig -Config $config -TriggersFile $TriggersFile
        Write-Host "✅ $registered default trigger(s) registered"
    } else {
        Write-Host "ℹ️ All default triggers already registered"
    }
}

#endregion
