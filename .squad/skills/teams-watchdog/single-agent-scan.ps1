<#
.SYNOPSIS
    Single-agent scan: filter+extract+format Teams messages in one copilot call.
.PARAMETER InputFile
    Raw messages file (from probe-messages.ps1).
.EXAMPLE
    .\single-agent-scan.ps1 -InputFile work\01-raw.txt > summary.md
#>
param([Parameter(Mandatory)][string]$InputFile, [int]$Hours = 24)
$ErrorActionPreference = "Stop"

if (-not (Test-Path $InputFile)) { Write-Error "Input not found: $InputFile"; exit 1 }

$messages = Get-Content $InputFile -Raw
if ([string]::IsNullOrWhiteSpace($messages)) {
    Write-Output "No Teams activity in the last $Hours hours."; exit 0
}

$prompt = @"
Below are raw Teams messages from the last $Hours hours.

--- BEGIN MESSAGES ---
$messages
--- END MESSAGES ---

From these messages, extract content in TWO directions:

1. Messages sent BY me (Jonathan): decisions, commitments, announcements
2. Messages sent TO me (Jonathan): replies to my messages, @-mentions, direct requests, action items assigned to me

Extract and format as markdown:

## Decisions
List 3-5 decisions that were made or announced (both by me and directed at me from teammates) (bullet points).

## Action Items
List 3-5 action items I committed to, assigned to others, OR that have been assigned to me by others (bullet points).

## Key Context
2-3 sentences summarizing important context, findings, or requests shared with or directed at me.

If no relevant messages, output:
"No Teams activity for Jonathan in the last $Hours hours."

Keep it concise. Focus on actionable content only.
"@

copilot -p $prompt --allow-tool='workiq'
