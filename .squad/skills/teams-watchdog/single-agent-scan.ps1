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

From these messages, identify content sent BY me (Jonathan).
Extract and format as markdown:

## Decisions
List 3-5 decisions I made or announced (bullet points).

## Action Items
List 3-5 action items I committed to or assigned to others (bullet points).

## Key Context
2-3 sentences summarizing important context or findings I shared.

If no relevant messages from me, output:
"No Teams activity from Jonathan in the last $Hours hours."

Keep it concise. Focus on actionable content only.
"@

copilot -p $prompt --allow-tool='workiq'
