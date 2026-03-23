<#
.SYNOPSIS
    Sends an Adaptive Card notification to a Microsoft Teams channel via Incoming Webhook.
.EXAMPLE
    .\scripts\send-teams-notification.ps1 -Title "🚨 Alert" -Body "Issue #42 is blocked."
.EXAMPLE
    .\scripts\send-teams-notification.ps1 -Title "Summary" -Body "All clear." -WebhookFile "C:\alt\webhook.url"
#>
param(
    [Parameter(Mandatory)][string]$Title,
    [Parameter(Mandatory)][string]$Body,
    [string]$WebhookFile = (Join-Path $HOME ".squad\teams-webhook.url")
)

$ErrorActionPreference = "Stop"

# Read webhook URL
if (-not (Test-Path $WebhookFile)) {
    Write-Error "Webhook URL file not found: $WebhookFile`nRun the setup steps in .squad/skills/squad-notifications/SKILL.md"
    exit 1
}
$webhookUrl = (Get-Content $WebhookFile -First 1).Trim()
if ([string]::IsNullOrWhiteSpace($webhookUrl)) {
    Write-Error "Webhook URL file is empty: $WebhookFile"
    exit 1
}

# Build Adaptive Card payload
$card = @{
    type = "message"
    attachments = @(@{
        contentType = "application/vnd.microsoft.card.adaptive"
        contentUrl  = $null
        content     = @{
            '$schema' = "http://adaptivecards.io/schemas/adaptive-card.json"
            type      = "AdaptiveCard"
            version   = "1.4"
            body      = @(
                @{ type = "TextBlock"; size = "Medium"; weight = "Bolder"; text = $Title }
                @{ type = "TextBlock"; text = $Body; wrap = $true }
            )
        }
    })
}

$json = $card | ConvertTo-Json -Depth 10 -Compress

# POST to Teams
try {
    Invoke-RestMethod -Uri $webhookUrl -Method Post -ContentType "application/json" -Body $json | Out-Null
    Write-Host "✅ Notification sent: $Title"
} catch {
    Write-Error "Failed to send notification: $_"
    exit 1
}
