<#
.SYNOPSIS
    Filters Teams messages to only Jonathan's sent messages.
.DESCRIPTION
    Takes raw Teams message output (from probe-messages.ps1) and uses
    copilot -p to extract only messages authored by Jonathan.
    Preserves timestamp and conversation context.
.EXAMPLE
    .\probe-messages.ps1 > raw.txt
    .\filter-my-messages.ps1 raw.txt
    .\filter-my-messages.ps1 raw.txt > filtered.txt
#>
param(
    [Parameter(Mandatory)]
    [string]$InputFile
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $InputFile)) {
    Write-Error "File not found: $InputFile"
    exit 1
}

$fullPath = (Resolve-Path $InputFile).Path

$prompt = @"
Read the file at $fullPath. It contains Teams messages.
Extract ONLY messages sent by Jonathan / Yoni Ben-Ami (me).
For each message, preserve: conversation/channel name, timestamp, and message content.
Exclude all messages from other people.
Output as plain text grouped by conversation.
If none found, say 'No messages from Jonathan found.'
"@

copilot -p $prompt
