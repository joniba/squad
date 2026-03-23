<#
.SYNOPSIS
    Email watchdog: scan for action items and deliver to Teams webhook.

.DESCRIPTION
    Single-agent consolidation: queries WorkIQ for recent emails requiring action,
    extracts action items, decisions, urgent requests, meeting follow-ups.
    Outputs structured markdown compatible with Teams message formatting.

.PARAMETER Hours
    Lookback window for email scan (default 24)

.PARAMETER TeamWebhook
    Optional Teams webhook URL for delivery. If provided, posts summary to Teams.

.EXAMPLE
    .\email-scan.ps1 -Hours 24
    .\email-scan.ps1 -Hours 24 -TeamWebhook "https://outlook.webhook..."
#>

param(
    [int] $Hours = 24,
    [string] $TeamWebhook
)

$ErrorActionPreference = "Stop"

$prompt = @"
Using the workiq-ask_work_iq tool, scan my emails from the last $Hours hours.

Extract and format as markdown:

## 📧 Action Items Assigned to Me
List specific actions people have explicitly asked me to do (3-5 items)
- Include sender name and email subject
- Prioritize by urgency/deadline

## 🎯 Decisions Requiring My Input
List emails where my decision or input is needed (2-3 items)
- Include what decision is needed
- Note deadline if mentioned

## 🔴 Urgent Requests
List time-sensitive or high-priority asks requiring immediate attention (1-3 items)

## 📋 Meeting Follow-ups
List action items from recent meetings (2-3 items)
- Include meeting context

## 📌 Summary
Write 1-2 sentences about overall email workload and biggest priorities for next 24h.

Be concise. Focus only on what I need to actively do. If no actionable emails found, say "No action items in the last $Hours hours."
"@

Write-Host "📬 Scanning emails from last $Hours hours..." -ForegroundColor Cyan

$output = copilot -p $prompt --allow-tool='workiq'

Write-Host $output

if ($TeamWebhook) {
    Write-Host ""
    Write-Host "🚀 Delivering to Teams..." -ForegroundColor Cyan

    $payload = @{
        text = "**Email Watchdog Report ($Hours hours)**`n`n$output"
    } | ConvertTo-Json

    $params = @{
        Uri         = $TeamWebhook
        Method      = 'POST'
        ContentType = 'application/json'
        Body        = $payload
    }

    Invoke-RestMethod @params | Out-Null
    Write-Host "✅ Delivered to Teams" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "✅ Scan complete. To deliver to Teams, provide -TeamWebhook parameter." -ForegroundColor Green
}
