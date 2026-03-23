<#
.SYNOPSIS
POC: Single-agent consolidated Teams message scan+filter+extract+format

.DESCRIPTION
Replaces 3-step filter→extract→format pipeline with single copilot -p call.
Demonstrates reduced cost (1 premium request) while maintaining structured output.

.PARAMETER Hours
Lookback window for Teams messages (default 24)

.EXAMPLE
& .\poc-single-agent-scan.ps1 -Hours 24
#>

param(
    [int] $Hours = 24
)

$prompt = @"
Using the workiq-ask_work_iq tool, find all my Teams messages sent in the last $Hours hours.

From those messages, extract and format as markdown:

## Decisions
List 3-5 decisions I made or announced to teams (bullet points)

## Action Items
List 3-5 action items I committed to or identified for others (bullet points)

## Key Context
Write 2-3 sentences summarizing important context or findings I shared

If no messages found, output:
"No Teams activity in the last $Hours hours."

Keep it concise. Focus on actionable content only.
"@

Write-Host "🔍 Running combined scan+filter+extract+format agent..." -ForegroundColor Cyan
Write-Host "⏱️  Lookback: $Hours hours" -ForegroundColor Gray
Write-Host ""

copilot -p $prompt --allow-tool='workiq'

Write-Host ""
Write-Host "✅ POC complete. This output replaces filter→extract→format pipeline steps." -ForegroundColor Green
