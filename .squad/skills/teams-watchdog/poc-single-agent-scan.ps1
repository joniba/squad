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
Using the workiq-ask_work_iq tool, find Teams messages from the last $Hours hours in TWO directions:

1. Messages I sent (Jonathan): decisions, commitments, announcements
2. Messages I received (Jonathan): replies to my messages, @-mentions, direct requests, action items assigned to me

From those messages, extract and format as markdown:

## Decisions
List 3-5 decisions that were made or announced (both by me and directed at me from teammates) (bullet points)

## Action Items
List 3-5 action items I committed to, assigned to others, OR that have been assigned to me by others (bullet points)

## Key Context
Write 2-3 sentences summarizing important context, findings, or requests shared with or directed at me

If no messages found, output:
"No Teams activity for Jonathan in the last $Hours hours."

Keep it concise. Focus on actionable content only.
"@

Write-Host "🔍 Running combined scan+filter+extract+format agent..." -ForegroundColor Cyan
Write-Host "⏱️  Lookback: $Hours hours" -ForegroundColor Gray
Write-Host ""

copilot -p $prompt --allow-tool='workiq'

Write-Host ""
Write-Host "✅ POC complete. This output replaces filter→extract→format pipeline steps." -ForegroundColor Green
