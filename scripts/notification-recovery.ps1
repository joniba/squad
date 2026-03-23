<#
.SYNOPSIS
    Failure recovery for the notification system. Dead letter queue, retry scheduler,
    health check, and cleanup for failed Teams webhook deliveries.

.DESCRIPTION
    Three exported functions:
      - Write-DeadLetter:          Persist a failed notification to disk
      - Retry-FailedNotifications: Scan dead letter queue, retry with escalating backoff
      - Test-NotificationHealth:   Validate webhook + report queue depth + last success

    Dead letter directory: ~/.squad/notifications/dead-letter/
    Retry log:             ~/.squad/notifications/retry.log
    Last-success marker:   ~/.squad/notifications/last-success.json

    Integrates with scripts/notify.ps1 via the Send-TeamsWebhook failure path.

.PARAMETER DeadLetterDir
    Override dead letter directory (for testing).

.PARAMETER RetryLog
    Override retry log path (for testing).
#>

$ErrorActionPreference = "Stop"

# ── Defaults ──

$script:DefaultDeadLetterDir = Join-Path $HOME ".squad\notifications\dead-letter"
$script:DefaultRetryLog      = Join-Path $HOME ".squad\notifications\retry.log"
$script:DefaultLastSuccess   = Join-Path $HOME ".squad\notifications\last-success.json"
$script:MaxRetryCycles       = 3
$script:RetryDelaysMinutes   = @(5, 15, 60) # escalating backoff per cycle
$script:PermanentFailureAgeDays = 30

#region ── Dead Letter Queue ──

function Write-DeadLetter {
    <#
    .SYNOPSIS
        Write a failed notification to the dead letter queue as a JSON file.
    .PARAMETER Notification
        Hashtable with: Type, Event, Card, WebhookUrl, Error, Attempts.
    .PARAMETER DeadLetterDir
        Override default dead letter directory.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Notification,

        [string]$DeadLetterDir
    )

    if (-not $DeadLetterDir) { $DeadLetterDir = $script:DefaultDeadLetterDir }

    if (-not (Test-Path $DeadLetterDir)) {
        New-Item -Path $DeadLetterDir -ItemType Directory -Force | Out-Null
    }

    $timestamp = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
    $tier = if ($Notification.Type) { $Notification.Type } else { "unknown" }
    $filename = "${timestamp}_${tier}.json"
    $filepath = Join-Path $DeadLetterDir $filename

    $entry = @{
        id            = [guid]::NewGuid().ToString()
        createdAt     = (Get-Date -Format 'o')
        tier          = $tier
        eventId       = $Notification.Event.eventId
        event         = $Notification.Event
        card          = $Notification.Card
        webhookUrl    = $Notification.WebhookUrl
        originalError = "$($Notification.Error)"
        attempts      = if ($Notification.Attempts) { $Notification.Attempts } else { 4 }
        retryCycle    = 0
        lastRetryAt   = $null
        status        = "pending"  # pending | retrying | permanent-failure | succeeded
    }

    $entry | ConvertTo-Json -Depth 15 | Set-Content $filepath -Encoding UTF8
    Write-Verbose "Dead letter written: $filepath"
    return $filepath
}

function Get-DeadLetterItems {
    <#
    .SYNOPSIS
        Read all dead letter items from the queue directory.
    #>
    [CmdletBinding()]
    param(
        [string]$DeadLetterDir,
        [string[]]$StatusFilter = @("pending", "retrying")
    )

    if (-not $DeadLetterDir) { $DeadLetterDir = $script:DefaultDeadLetterDir }

    if (-not (Test-Path $DeadLetterDir)) { return @() }

    $items = [System.Collections.Generic.List[hashtable]]::new()
    foreach ($file in Get-ChildItem $DeadLetterDir -Filter "*.json" -File) {
        try {
            $item = Get-Content $file.FullName -Raw | ConvertFrom-Json -AsHashtable
            $item._filePath = $file.FullName
            if ($StatusFilter -and ($item.status -notin $StatusFilter)) { continue }
            $items.Add($item)
        } catch {
            Write-Warning "Corrupt dead letter file: $($file.Name) — $_"
        }
    }
    return , @($items)
}

#endregion

#region ── Retry Scheduler ──

function Retry-FailedNotifications {
    <#
    .SYNOPSIS
        Scan dead letter queue, retry with escalating backoff (5m, 15m, 1h).
        Moves to permanent-failure after 3 retry cycles.
    .PARAMETER DeadLetterDir
        Override default dead letter directory.
    .PARAMETER RetryLog
        Override default retry log path.
    .PARAMETER WebhookUrl
        Webhook URL to use for retry. If not provided, reads from each dead letter item.
    .PARAMETER DryRun
        Log what would happen but don't actually send.
    #>
    [CmdletBinding()]
    param(
        [string]$DeadLetterDir,
        [string]$RetryLog,
        [string]$WebhookUrl,
        [switch]$DryRun
    )

    if (-not $DeadLetterDir) { $DeadLetterDir = $script:DefaultDeadLetterDir }
    if (-not $RetryLog) { $RetryLog = $script:DefaultRetryLog }

    $logDir = Split-Path $RetryLog
    if ($logDir -and -not (Test-Path $logDir)) {
        New-Item -Path $logDir -ItemType Directory -Force | Out-Null
    }

    $items = Get-DeadLetterItems -DeadLetterDir $DeadLetterDir -StatusFilter @("pending", "retrying")

    if ($items.Count -eq 0) {
        Write-Host "ℹ️ Dead letter queue is empty. Nothing to retry."
        return @{ Processed = 0; Succeeded = 0; Failed = 0; PermanentFailures = 0 }
    }

    $now = Get-Date
    $processed = 0
    $succeeded = 0
    $failed = 0
    $permanentFailures = 0

    foreach ($item in $items) {
        $cycle = [int]$item.retryCycle

        # Check if max retries exceeded → permanent failure
        if ($cycle -ge $script:MaxRetryCycles) {
            $item.status = "permanent-failure"
            $item | ConvertTo-Json -Depth 15 | Set-Content $item._filePath -Encoding UTF8
            $permanentFailures++
            Write-RetryLog -RetryLog $RetryLog -Message "PERMANENT_FAILURE" -EventId $item.eventId -Cycle $cycle
            continue
        }

        # Check backoff: don't retry too soon
        $requiredDelayMinutes = $script:RetryDelaysMinutes[$cycle]
        if ($item.lastRetryAt) {
            try {
                $lastRetry = [datetime]$item.lastRetryAt
                $nextAllowed = $lastRetry.AddMinutes($requiredDelayMinutes)
                if ($now -lt $nextAllowed) {
                    Write-Verbose "Skipping $($item.eventId): next retry at $nextAllowed"
                    continue
                }
            } catch {}
        }

        $processed++
        $targetUrl = if ($WebhookUrl) { $WebhookUrl } else { $item.webhookUrl }

        if ($DryRun) {
            Write-Host "[DRY RUN] Would retry: $($item.eventId) (cycle $cycle)"
            Write-RetryLog -RetryLog $RetryLog -Message "DRY_RUN_RETRY" -EventId $item.eventId -Cycle $cycle
            continue
        }

        # Attempt the send
        try {
            $json = $item.card | ConvertTo-Json -Depth 15 -Compress
            Invoke-RestMethod -Uri $targetUrl -Method Post -ContentType "application/json" -Body $json | Out-Null

            # Success! Remove from dead letter
            $item.status = "succeeded"
            $item | ConvertTo-Json -Depth 15 | Set-Content $item._filePath -Encoding UTF8
            $succeeded++

            Write-RetryLog -RetryLog $RetryLog -Message "RETRY_SUCCESS" -EventId $item.eventId -Cycle $cycle
            Update-LastSuccess
            Write-Host "✅ Retry succeeded: $($item.eventId)"
        } catch {
            $item.retryCycle = $cycle + 1
            $item.lastRetryAt = (Get-Date -Format 'o')
            $item.status = "retrying"
            $item | ConvertTo-Json -Depth 15 | Set-Content $item._filePath -Encoding UTF8
            $failed++

            Write-RetryLog -RetryLog $RetryLog -Message "RETRY_FAILED" -EventId $item.eventId -Cycle ($cycle + 1) -Error "$_"
            Write-Warning "Retry failed for $($item.eventId) (cycle $($cycle + 1)): $_"
        }
    }

    return @{
        Processed         = $processed
        Succeeded         = $succeeded
        Failed            = $failed
        PermanentFailures = $permanentFailures
    }
}

function Write-RetryLog {
    param(
        [string]$RetryLog,
        [string]$Message,
        [string]$EventId,
        [int]$Cycle,
        [string]$Error
    )
    if (-not $RetryLog) { $RetryLog = $script:DefaultRetryLog }
    $logDir = Split-Path $RetryLog
    if ($logDir -and -not (Test-Path $logDir)) {
        New-Item -Path $logDir -ItemType Directory -Force | Out-Null
    }
    $ts = Get-Date -Format 'o'
    $errPart = if ($Error) { " error=$Error" } else { "" }
    $line = "[$ts] [$Message] [eventId=$EventId] [cycle=$Cycle]$errPart"
    Add-Content -Path $RetryLog -Value $line
}

function Update-LastSuccess {
    param([string]$LastSuccessFile)
    if (-not $LastSuccessFile) { $LastSuccessFile = $script:DefaultLastSuccess }
    $dir = Split-Path $LastSuccessFile
    if ($dir -and -not (Test-Path $dir)) {
        New-Item -Path $dir -ItemType Directory -Force | Out-Null
    }
    @{ lastSuccessAt = (Get-Date -Format 'o') } |
        ConvertTo-Json | Set-Content $LastSuccessFile -Encoding UTF8
}

#endregion

#region ── Health Check ──

function Test-NotificationHealth {
    <#
    .SYNOPSIS
        Validate webhook reachability, report dead letter queue depth, and last
        successful send timestamp. Returns a structured health object.
    .PARAMETER WebhookUrl
        Webhook URL to check. If not provided, reads from ~/.squad/teams-webhook.url.
    .PARAMETER WebhookFile
        Path to file containing webhook URL.
    .PARAMETER DeadLetterDir
        Override dead letter directory.
    .PARAMETER LastSuccessFile
        Override last success marker file.
    #>
    [CmdletBinding()]
    param(
        [string]$WebhookUrl,
        [string]$WebhookFile = (Join-Path $HOME ".squad\teams-webhook.url"),
        [string]$DeadLetterDir,
        [string]$LastSuccessFile
    )

    if (-not $DeadLetterDir) { $DeadLetterDir = $script:DefaultDeadLetterDir }
    if (-not $LastSuccessFile) { $LastSuccessFile = $script:DefaultLastSuccess }

    $health = @{
        timestamp         = (Get-Date -Format 'o')
        webhookReachable  = $false
        webhookUrl        = $null
        deadLetterCount   = 0
        pendingRetries    = 0
        permanentFailures = 0
        lastSuccessAt     = $null
        status            = "unknown"
        details           = @()
    }

    # Resolve webhook URL
    if (-not $WebhookUrl -and (Test-Path $WebhookFile)) {
        $WebhookUrl = (Get-Content $WebhookFile -First 1).Trim()
    }

    # Dead letter queue depth (always check, regardless of webhook config)
    if (Test-Path $DeadLetterDir) {
        $allItems = Get-DeadLetterItems -DeadLetterDir $DeadLetterDir -StatusFilter $null
        $health.deadLetterCount = @($allItems).Count
        $health.pendingRetries = @($allItems | Where-Object { $_.status -in @("pending", "retrying") }).Count
        $health.permanentFailures = @($allItems | Where-Object { $_.status -eq "permanent-failure" }).Count
    }

    # Last success timestamp (always check)
    if (Test-Path $LastSuccessFile) {
        try {
            $lastSuccess = Get-Content $LastSuccessFile -Raw | ConvertFrom-Json
            $health.lastSuccessAt = $lastSuccess.lastSuccessAt
        } catch {
            $health.details += "Corrupt last-success file"
        }
    }

    if (-not $WebhookUrl) {
        $health.details += "No webhook URL configured"
        $health.status = if ($health.pendingRetries -gt 0) { "degraded" } else { "unconfigured" }
        return $health
    }

    # Mask URL for display (show domain only)
    try {
        $uri = [uri]$WebhookUrl
        $health.webhookUrl = "$($uri.Scheme)://$($uri.Host)/***"
    } catch {
        $health.webhookUrl = "invalid-url"
    }

    # Test webhook reachability (HEAD request, non-destructive)
    try {
        $response = Invoke-WebRequest -Uri $WebhookUrl -Method Head -TimeoutSec 10 -UseBasicParsing -ErrorAction Stop
        $health.webhookReachable = ($response.StatusCode -lt 500)
        $health.details += "Webhook responded with status $($response.StatusCode)"
    } catch {
        $statusCode = $null
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }
        # Some webhook endpoints reject HEAD but accept POST — 405 is OK
        if ($statusCode -eq 405 -or $statusCode -eq 400) {
            $health.webhookReachable = $true
            $health.details += "Webhook reachable (returned $statusCode to HEAD, expected for webhook endpoints)"
        } else {
            $health.webhookReachable = $false
            $health.details += "Webhook unreachable: $_"
        }
    }

    # Overall status
    if (-not $health.webhookReachable) {
        $health.status = "unhealthy"
    } elseif ($health.pendingRetries -gt 0) {
        $health.status = "degraded"
    } else {
        $health.status = "healthy"
    }

    return $health
}

#endregion

#region ── Cleanup ──

function Invoke-DeadLetterCleanup {
    <#
    .SYNOPSIS
        Purge succeeded retries and age out permanent failures older than 30 days.
    .PARAMETER DeadLetterDir
        Override dead letter directory.
    .PARAMETER MaxAgeDays
        Days after which permanent failures are purged. Default: 30.
    #>
    [CmdletBinding()]
    param(
        [string]$DeadLetterDir,
        [int]$MaxAgeDays = $script:PermanentFailureAgeDays
    )

    if (-not $DeadLetterDir) { $DeadLetterDir = $script:DefaultDeadLetterDir }
    if (-not (Test-Path $DeadLetterDir)) { return @{ Removed = 0 } }

    $cutoff = (Get-Date).AddDays(-$MaxAgeDays)
    $removed = 0

    foreach ($file in Get-ChildItem $DeadLetterDir -Filter "*.json" -File) {
        try {
            $item = Get-Content $file.FullName -Raw | ConvertFrom-Json -AsHashtable

            # Remove succeeded retries immediately
            if ($item.status -eq "succeeded") {
                Remove-Item $file.FullName -Force
                $removed++
                continue
            }

            # Remove permanent failures older than MaxAgeDays
            if ($item.status -eq "permanent-failure" -and $item.createdAt) {
                try {
                    $created = [datetime]$item.createdAt
                    if ($created -lt $cutoff) {
                        Remove-Item $file.FullName -Force
                        $removed++
                    }
                } catch {}
            }
        } catch {
            Write-Warning "Error processing dead letter file $($file.Name): $_"
        }
    }

    return @{ Removed = $removed }
}

#endregion

# Export functions when loaded as a module (no-op when dot-sourced as a script)
if ($MyInvocation.MyCommand.ScriptBlock.Module) {
    Export-ModuleMember -Function Write-DeadLetter, Get-DeadLetterItems, Retry-FailedNotifications, Test-NotificationHealth, Invoke-DeadLetterCleanup, Update-LastSuccess
}
