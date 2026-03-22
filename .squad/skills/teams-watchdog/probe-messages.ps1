<#
.SYNOPSIS
    Probes Teams messages via WorkIQ MCP tool through Copilot CLI.
.DESCRIPTION
    Fetches recent Teams messages from the last 24 hours using copilot -p
    and the workiq-ask_work_iq MCP tool. Output is plain text to stdout.
.EXAMPLE
    .\.squad\skills\teams-watchdog\probe-messages.ps1
    .\.squad\skills\teams-watchdog\probe-messages.ps1 -Hours 48
    .\.squad\skills\teams-watchdog\probe-messages.ps1 > messages.txt
#>
param(
    [int]$Hours = 24
)

$ErrorActionPreference = "Stop"

$prompt = @"
Use the workiq-ask_work_iq tool to answer this question:
What are my Teams messages from the last $Hours hours?
List each message with: sender name, time, and message content.
Group by conversation. Keep it concise plain text, no markdown links.
If there are no messages, say 'No Teams messages found in the last $Hours hours.'
"@

# --allow-tool grants non-interactive consent for the WorkIQ MCP tool.
# Caller can redirect to a file: .\probe-messages.ps1 > output.txt
copilot -p $prompt --allow-tool='workiq'
