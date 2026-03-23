<#
.SYNOPSIS
POC: Single-agent consolidated email scan+filter+extract+format

.DESCRIPTION
Replaces multi-step pipeline with single copilot -p call to WorkIQ.
Demonstrates reduced cost (1 premium request) while maintaining structured output.

.PARAMETER Hours
Lookback window for emails (default 24)

.EXAMPLE
& .\poc-email-scan.ps1 -Hours 24
#>

param(
    [int] $Hours = 24
)

$prompt = @"
Using the workiq-ask_work_iq tool, find all my emails from the last $Hours hours that require action or decision from me.

From those emails, extract and format as markdown:

## Action Items Assigned to Me
List 3-5 specific action items people have asked me to do (bullet points with email sender/context)

## Decisions Requiring My Input
List 2-3 emails where I need to make a decision or provide input (bullet points)

## Urgent Requests
List any time-sensitive or high-priority asks (bullet points)

## Meeting Follow-ups
List any action items from meetings in the last $Hours hours (bullet points)

Keep it concise. Focus on what I actually need to do. Include sender name and email subject for context.
"@

Write-Host "🔍 Running combined email scan+filter+extract+format agent..." -ForegroundColor Cyan
Write-Host "⏱️  Lookback: $Hours hours" -ForegroundColor Gray
Write-Host ""

copilot -p $prompt --allow-tool='workiq'

Write-Host ""
Write-Host "✅ POC complete. This output replaces multi-step pipeline." -ForegroundColor Green
