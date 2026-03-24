<#
.SYNOPSIS
    Central notification router for the squad. Classifies events by urgency tier,
    deduplicates via watermark state, formats Adaptive Cards, and delivers via
    Teams webhook with exponential backoff retry.

.DESCRIPTION
    Three tiers:
      🔴 urgent  — failures, livesite, blockers → immediate
      🟡 action  — PRs, setup, reviews → immediate
      🔵 feature — batched hourly summaries

    Follows the approved design at docs/designs/proactive-notifications.md.

.PARAMETER Type
    Notification tier: "urgent", "action", or "feature".

.PARAMETER Event
    Hashtable with tier-specific fields. See design doc for schema.

.PARAMETER StateFile
    Path to watermark/dedup state file. Default: .squad/notifications-state.json

.PARAMETER WebhookFile
    Path to file containing Teams webhook URL. Default: ~/.squad/teams-webhook.url

.PARAMETER Force
    Skip dedup check and send regardless.

.PARAMETER DryRun
    Build the card and print JSON but don't send.

.EXAMPLE
    .\scripts\notify.ps1 -Type urgent -Event @{
        eventId   = "script-failure:icm-scan:2026-03-24T14:32:00Z"
        errorType = "script-failure"
        title     = "icm-scan failed"
        reason    = "timeout querying incident #123456789"
        actionUrl = "https://github.com/org/repo/issues/42"
        actionLabel = "View Investigation"
    }

.EXAMPLE
    .\scripts\notify.ps1 -Type feature -Event @{
        eventId          = "feature:auth-flow:2026-03-24T15:00:00Z"
        featureTitle     = "Auth Flow"
        summary          = "Token caching implemented."
        testInstructions = "1. Run login\n2. Verify cache"
        issuesUrl        = "https://github.com/org/repo/issues"
    } -DryRun
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet("urgent", "action", "feature")]
    [string]$Type,

    [Parameter(Mandatory)]
    [hashtable]$Event,

    [string]$StateFile,

    [string]$WebhookFile = (Join-Path $HOME ".squad\teams-webhook.url"),

    [switch]$Force,

    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

# Dot-source failure recovery functions (dead letter queue, retry, health)
$recoveryScript = Join-Path $PSScriptRoot "notification-recovery.ps1"
if (Test-Path $recoveryScript) {
    . $recoveryScript
}

# Resolve state file relative to repo root
if (-not $StateFile) {
    $repoRoot = & git rev-parse --show-toplevel 2>$null
    if (-not $repoRoot) { $repoRoot = $PSScriptRoot | Split-Path }
    $StateFile = Join-Path $repoRoot ".squad\notifications-state.json"
}

#region ── Helpers ──

function Get-WebhookUrl {
    param([string]$Path)
    if (-not (Test-Path $Path)) {
        Write-Warning "Webhook URL file not found: $Path — notification will be skipped."
        return $null
    }
    $url = (Get-Content $Path -First 1).Trim()
    if ([string]::IsNullOrWhiteSpace($url)) {
        Write-Warning "Webhook URL file is empty: $Path"
        return $null
    }
    return $url
}

function Read-State {
    param([string]$Path)
    if (Test-Path $Path) {
        try {
            return Get-Content $Path -Raw | ConvertFrom-Json -AsHashtable
        } catch {
            Write-Warning "Corrupt state file, starting fresh: $_"
        }
    }
    return @{
        version          = 1
        lastUpdated      = (Get-Date -Format 'o')
        events           = @{}
        featureQueue     = @()
        lastFeatureBatchAt = $null
    }
}

function Save-State {
    param([hashtable]$State, [string]$Path)
    $State.lastUpdated = (Get-Date -Format 'o')
    $dir = Split-Path $Path
    if ($dir -and -not (Test-Path $dir)) {
        New-Item -Path $dir -ItemType Directory -Force | Out-Null
    }
    $State | ConvertTo-Json -Depth 10 | Set-Content $Path -Encoding UTF8
}

function Purge-ExpiredEvents {
    param([hashtable]$State)
    $cutoff = (Get-Date).AddDays(-7)
    $toRemove = @()
    foreach ($key in @($State.events.Keys)) {
        $entry = $State.events[$key]
        $lastNotified = $null
        if ($entry.lastNotifiedAt) {
            try { $lastNotified = [datetime]$entry.lastNotifiedAt } catch {}
        }
        if ($lastNotified -and $lastNotified -lt $cutoff) {
            $toRemove += $key
        }
    }
    foreach ($key in $toRemove) {
        $State.events.Remove($key)
    }
}

#endregion

#region ── Dedup Gate ──

function Test-ShouldNotify {
    param([string]$Type, [hashtable]$Event, [hashtable]$State)

    $eventId = $Event.eventId
    if (-not $eventId) { return $true } # no ID = always send

    $existing = $State.events[$eventId]
    if (-not $existing) { return $true } # never seen

    $lastNotified = $null
    if ($existing.lastNotifiedAt) {
        try { $lastNotified = [datetime]$existing.lastNotifiedAt } catch {}
    }

    switch ($Type) {
        "urgent" {
            # Re-notify after 3+ consecutive errors OR after 6h silence
            if ($existing.errorCount -ge 3) { return $true }
            if ($lastNotified -and $lastNotified -lt (Get-Date).AddHours(-6)) { return $true }
            return $false
        }
        "action" {
            # Re-notify on state change or after 48h silence
            if ($lastNotified -and $lastNotified -lt (Get-Date).AddHours(-48)) { return $true }
            $seenStates = $existing.seenStates
            $currentState = $Event.currentState
            if ($currentState -and $seenStates -and ($currentState -notin $seenStates)) { return $true }
            return $false
        }
        "feature" {
            # Feature: never re-notify same feature
            return $false
        }
    }
    return $true
}

function Update-StateAfterSend {
    param([string]$Type, [hashtable]$Event, [hashtable]$State)

    $eventId = $Event.eventId
    if (-not $eventId) { return }

    $now = Get-Date -Format 'o'
    $existing = $State.events[$eventId]

    switch ($Type) {
        "urgent" {
            $errorCount = if ($existing) { [int]$existing.errorCount + 1 } else { 1 }
            $State.events[$eventId] = @{
                type           = "urgent"
                lastNotifiedAt = $now
                retryCount     = 0
                errorCount     = $errorCount
            }
        }
        "action" {
            $seenStates = @()
            if ($existing -and $existing.seenStates) {
                $seenStates = @($existing.seenStates)
            }
            if ($Event.currentState -and ($Event.currentState -notin $seenStates)) {
                $seenStates += $Event.currentState
            }
            $State.events[$eventId] = @{
                type           = "action"
                lastNotifiedAt = $now
                retryCount     = 0
                seenStates     = $seenStates
            }
        }
        "feature" {
            $State.events[$eventId] = @{
                type           = "feature"
                lastNotifiedAt = $now
                retryCount     = 0
            }
        }
    }
}

#endregion

#region ── Adaptive Card Builders ──

function Build-UrgentCard {
    param([hashtable]$Event)
    $body = @(
        @{ type = "TextBlock"; size = "Large"; weight = "Bolder"; text = "🔴 $($Event.title)"; color = "attention" }
    )

    # Metadata: Agent and Severity as separate FactSet rows (never on one line)
    $facts = @()
    if ($Event.agent) {
        $facts += @{ title = "Agent"; value = "$($Event.agent)" }
    }
    if ($Event.severity) {
        $facts += @{ title = "Severity"; value = "$($Event.severity)" }
    }
    if ($facts.Count -gt 0) {
        $body += @{ type = "FactSet"; facts = $facts; spacing = "Small" }
    }

    # Per-issue rows (multi-issue mode)
    if ($Event.blockedIssues -and $Event.blockedIssues.Count -gt 0) {
        $body += @{ type = "TextBlock"; text = "**Blocked issues ($($Event.blockedIssues.Count)):**"; weight = "Bolder"; spacing = "Medium" }
        foreach ($issue in $Event.blockedIssues) {
            $linkText = "[#$($issue.number) — $($issue.title)]($($issue.url))"
            $body += @{ type = "TextBlock"; text = $linkText; wrap = $true; spacing = "Small" }
            if ($issue.reason) {
                $body += @{ type = "TextBlock"; text = "→ $($issue.reason)"; wrap = $true; spacing = "None"; isSubtle = $true; size = "Small" }
            }
        }
    } elseif ($Event.reason) {
        # Single-issue / legacy mode: show reason as a text block
        $body += @{ type = "TextBlock"; text = "$($Event.reason)"; wrap = $true; spacing = "Small" }
    }

    # Action needed
    if ($Event.actionNeeded) {
        $body += @{ type = "TextBlock"; text = "**Action needed:** $($Event.actionNeeded)"; wrap = $true; weight = "Bolder"; spacing = "Medium" }
    }

    # Action buttons — one per issue (Adaptive Cards supports up to 6)
    $actions = @()
    if ($Event.blockedIssues -and $Event.blockedIssues.Count -gt 0) {
        $maxButtons = [Math]::Min($Event.blockedIssues.Count, 6)
        for ($i = 0; $i -lt $maxButtons; $i++) {
            $issue = $Event.blockedIssues[$i]
            if ($issue.url) {
                $actions += @{ type = "Action.OpenUrl"; title = "View #$($issue.number)"; url = "$($issue.url)" }
            }
        }
    } elseif ($Event.actionUrl) {
        $label = if ($Event.actionLabel) { $Event.actionLabel } else { "View Details" }
        $actions += @{ type = "Action.OpenUrl"; title = $label; url = "$($Event.actionUrl)" }
    }
    return Build-CardEnvelope -Body $body -Actions $actions
}

function Build-ActionCard {
    param([hashtable]$Event)
    $body = @(
        @{ type = "TextBlock"; size = "Medium"; weight = "Bolder"; text = "🟡 $($Event.title)" }
        @{ type = "TextBlock"; text = "$($Event.reason)"; wrap = $true; spacing = "Small" }
    )
    if ($Event.estimatedTime) {
        $body += @{ type = "TextBlock"; text = "⏱️ $($Event.estimatedTime)"; isSubtle = $true; spacing = "Small" }
    }
    $actions = @()
    if ($Event.actionUrl) {
        $label = if ($Event.actionLabel) { $Event.actionLabel } else { "Take Action" }
        $actions += @{ type = "Action.OpenUrl"; title = $label; url = "$($Event.actionUrl)" }
    }
    return Build-CardEnvelope -Body $body -Actions $actions
}

function Build-FeatureCard {
    param([hashtable]$Event)
    $body = @(
        @{ type = "TextBlock"; size = "Medium"; weight = "Bolder"; text = "🔵 $($Event.featureTitle)" }
        @{ type = "TextBlock"; text = "$($Event.summary)"; wrap = $true; spacing = "Small" }
    )
    if ($Event.testInstructions) {
        $body += @{ type = "TextBlock"; text = "**Testing Instructions:**"; weight = "Bolder"; spacing = "Medium" }
        $body += @{ type = "TextBlock"; text = "$($Event.testInstructions)"; wrap = $true; fontType = "monospace"; size = "Small" }
    }
    if ($Event.nextAction) {
        $dueText = if ($Event.nextActionDue) { " (due $($Event.nextActionDue))" } else { "" }
        $body += @{ type = "TextBlock"; text = "$($Event.nextAction)$dueText"; isSubtle = $true; spacing = "Medium"; wrap = $true }
    }
    $actions = @()
    if ($Event.issuesUrl) {
        $actions += @{ type = "Action.OpenUrl"; title = "View Issues"; url = "$($Event.issuesUrl)" }
    }
    return Build-CardEnvelope -Body $body -Actions $actions
}

function Build-BatchFeatureCard {
    param([array]$Features)
    $body = @(
        @{ type = "TextBlock"; size = "Medium"; weight = "Bolder"; text = "🔵 Feature Summary ($($Features.Count) items)" }
    )
    foreach ($feat in $Features) {
        $body += @{ type = "ColumnSet"; separator = $true; columns = @(
            @{ type = "Column"; width = "stretch"; items = @(
                @{ type = "TextBlock"; weight = "Bolder"; text = "🔵 $($feat.featureTitle)"; wrap = $true }
                @{ type = "TextBlock"; text = "$($feat.summary)"; wrap = $true; spacing = "Small" }
            )}
        )}
        if ($feat.testInstructions) {
            $body += @{ type = "TextBlock"; text = "$($feat.testInstructions)"; wrap = $true; fontType = "monospace"; size = "Small"; spacing = "Small" }
        }
    }
    $actions = @()
    $firstUrl = ($Features | Where-Object { $_.issuesUrl } | Select-Object -First 1).issuesUrl
    if ($firstUrl) {
        $actions += @{ type = "Action.OpenUrl"; title = "View Issues"; url = "$firstUrl" }
    }
    return Build-CardEnvelope -Body $body -Actions $actions
}

function Build-CardEnvelope {
    param([array]$Body, [array]$Actions)
    $content = @{
        '$schema' = "http://adaptivecards.io/schemas/adaptive-card.json"
        type      = "AdaptiveCard"
        version   = "1.4"
        body      = $Body
    }
    if ($Actions.Count -gt 0) {
        $content.actions = $Actions
    }
    return @{
        type        = "message"
        attachments = @(@{
            contentType = "application/vnd.microsoft.card.adaptive"
            contentUrl  = $null
            content     = $content
        })
    }
}

#endregion

#region ── Delivery ──

function Send-TeamsWebhook {
    param([hashtable]$Card, [string]$WebhookUrl, [string]$EventId)

    $json = $Card | ConvertTo-Json -Depth 15 -Compress

    # Enforce 28 KB limit
    if ($json.Length -gt 28672) {
        Write-Warning "Card payload exceeds 28 KB ($($json.Length) bytes). Truncating body."
        # Fallback: send a simplified card
        $json = (@{
            type = "message"
            attachments = @(@{
                contentType = "application/vnd.microsoft.card.adaptive"
                contentUrl = $null
                content = @{
                    '$schema' = "http://adaptivecards.io/schemas/adaptive-card.json"
                    type = "AdaptiveCard"; version = "1.4"
                    body = @(@{ type = "TextBlock"; text = "Notification too large. Check GitHub for details."; wrap = $true })
                }
            })
        } | ConvertTo-Json -Depth 10 -Compress)
    }

    # Exponential backoff: 2s, 4s, 8s, 16s
    $delays = @(2, 4, 8, 16)
    $maxRetries = 4
    $attempt = 0

    while ($attempt -lt $maxRetries) {
        try {
            Invoke-RestMethod -Uri $WebhookUrl -Method Post -ContentType "application/json" -Body $json | Out-Null
            return @{ Success = $true; Attempts = $attempt + 1 }
        } catch {
            $attempt++
            if ($attempt -lt $maxRetries) {
                $delay = $delays[$attempt - 1]
                Write-Verbose "Webhook attempt $attempt failed, retrying in ${delay}s: $_"
                Start-Sleep -Seconds $delay
            } else {
                # Log failure but don't block caller (fire-and-forget)
                $logEntry = "[$(Get-Date -Format 'o')] [$Type] [SEND_FAILED] [retries=$attempt] [eventId=$EventId] $_"
                $repoRoot2 = & git rev-parse --show-toplevel 2>$null
                if ($repoRoot2) {
                    $errorLog = Join-Path $repoRoot2 ".squad\notifications-errors.log"
                    $logDir = Split-Path $errorLog
                    if (-not (Test-Path $logDir)) { New-Item -Path $logDir -ItemType Directory -Force | Out-Null }
                    Add-Content -Path $errorLog -Value $logEntry
                }
                Write-Warning "Notification delivery failed after $maxRetries attempts. Error logged."
                return @{ Success = $false; Attempts = $attempt; Error = "$_" }
            }
        }
    }
}

#endregion

#region ── Feature Queue / Batching ──

function Add-ToFeatureQueue {
    param([hashtable]$Event, [hashtable]$State)
    $entry = @{
        featureId      = $Event.eventId
        featureTitle   = $Event.featureTitle
        summary        = $Event.summary
        testInstructions = $Event.testInstructions
        issuesUrl      = $Event.issuesUrl
        nextAction     = $Event.nextAction
        nextActionDue  = $Event.nextActionDue
        queuedAt       = (Get-Date -Format 'o')
    }
    if (-not $State.featureQueue) { $State.featureQueue = @() }
    $State.featureQueue = @($State.featureQueue) + @($entry)
}

function Test-ShouldFlushFeatureQueue {
    param([hashtable]$State)
    $queue = @($State.featureQueue)
    if ($queue.Count -eq 0) { return $false }

    # Flush if queue >= 5 items
    if ($queue.Count -ge 5) { return $true }

    # Flush if last batch was >1 hour ago (or never)
    if (-not $State.lastFeatureBatchAt) { return $true }
    try {
        $lastBatch = [datetime]$State.lastFeatureBatchAt
        if ($lastBatch -lt (Get-Date).AddHours(-1)) { return $true }
    } catch {
        return $true
    }
    return $false
}

function Flush-FeatureQueue {
    param([hashtable]$State, [string]$WebhookUrl)

    $queue = @($State.featureQueue)
    if ($queue.Count -eq 0) { return }

    $card = Build-BatchFeatureCard -Features $queue

    if ($DryRun) {
        Write-Host "── DRY RUN: Batch Feature Card ($($queue.Count) items) ──"
        $card | ConvertTo-Json -Depth 15 | Write-Host
        Write-Host "── END DRY RUN ──"
    } else {
        $result = Send-TeamsWebhook -Card $card -WebhookUrl $WebhookUrl -EventId "feature-batch"
        if ($result.Success) {
            Write-Host "✅ Batch feature notification sent ($($queue.Count) items)"
        }
    }

    # Mark each feature in state so it won't re-send
    foreach ($feat in $queue) {
        $State.events[$feat.featureId] = @{
            type           = "feature"
            lastNotifiedAt = (Get-Date -Format 'o')
            retryCount     = 0
        }
    }
    $State.featureQueue = @()
    $State.lastFeatureBatchAt = (Get-Date -Format 'o')
}

#endregion

#region ── Main ──

# Validate event has required fields
$requiredFields = switch ($Type) {
    "urgent"  { @("title") }  # reason OR blockedIssues checked below
    "action"  { @("title", "reason") }
    "feature" { @("featureTitle", "summary") }
}
foreach ($field in $requiredFields) {
    if (-not $Event[$field]) {
        Write-Error "Event missing required field '$field' for type '$Type'"
        exit 1
    }
}
# Urgent: must have either 'reason' or 'blockedIssues'
if ($Type -eq "urgent" -and -not $Event.reason -and -not $Event.blockedIssues) {
    Write-Error "Urgent event requires either 'reason' or 'blockedIssues'"
    exit 1
}

# Load state
$state = Read-State -Path $StateFile
Purge-ExpiredEvents -State $state

# Dedup check
if (-not $Force -and -not (Test-ShouldNotify -Type $Type -Event $Event -State $state)) {
    Write-Host "ℹ️ Notification suppressed (dedup): $($Event.eventId ?? $Event.title)"
    Save-State -State $state -Path $StateFile
    exit 0
}

# Feature tier: enqueue and maybe batch
if ($Type -eq "feature") {
    Add-ToFeatureQueue -Event $Event -State $state
    Write-Host "ℹ️ Feature queued: $($Event.featureTitle)"

    if ($Force -or (Test-ShouldFlushFeatureQueue -State $state)) {
        if (-not $DryRun) {
            $webhookUrl = Get-WebhookUrl -Path $WebhookFile
            if (-not $webhookUrl) {
                Save-State -State $state -Path $StateFile
                exit 0
            }
        }
        Flush-FeatureQueue -State $state -WebhookUrl $webhookUrl
    }
    Save-State -State $state -Path $StateFile
    exit 0
}

# Build card for urgent/action
$card = switch ($Type) {
    "urgent"  { Build-UrgentCard -Event $Event }
    "action"  { Build-ActionCard -Event $Event }
}

if ($DryRun) {
    Write-Host "── DRY RUN: $Type Card ──"
    $card | ConvertTo-Json -Depth 15 | Write-Host
    Write-Host "── END DRY RUN ──"
    Update-StateAfterSend -Type $Type -Event $Event -State $state
    Save-State -State $state -Path $StateFile
    exit 0
}

# Read webhook URL
$webhookUrl = Get-WebhookUrl -Path $WebhookFile
if (-not $webhookUrl) {
    Save-State -State $state -Path $StateFile
    exit 0
}

# Send
$result = Send-TeamsWebhook -Card $card -WebhookUrl $webhookUrl -EventId ($Event.eventId ?? "unknown")
if ($result.Success) {
    Write-Host "✅ $Type notification sent: $($Event.title)"
    Update-StateAfterSend -Type $Type -Event $Event -State $state
    # Update last-success marker for health check
    if (Get-Command Update-LastSuccess -ErrorAction SilentlyContinue) {
        Update-LastSuccess
    }
} else {
    # Write to dead letter queue for persistent retry
    if (Get-Command Write-DeadLetter -ErrorAction SilentlyContinue) {
        $dlPath = Write-DeadLetter -Notification @{
            Type       = $Type
            Event      = $Event
            Card       = $card
            WebhookUrl = $webhookUrl
            Error      = $result.Error
            Attempts   = $result.Attempts
        }
        Write-Host "📬 Notification queued for retry: $($Event.eventId ?? $Event.title) → $dlPath"
    }
}

Save-State -State $state -Path $StateFile

#endregion
