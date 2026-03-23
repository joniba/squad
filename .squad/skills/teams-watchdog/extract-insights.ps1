<#
.SYNOPSIS
    Extracts decisions, action items, and commitments from filtered Teams messages.
.DESCRIPTION
    Takes filtered Teams message output (from filter-my-messages.ps1) and uses
    copilot -p to identify structured insights: decisions made, action items,
    commitments given, and important FYIs. Each item includes source context.
.EXAMPLE
    .\filter-my-messages.ps1 raw.txt > filtered.txt
    .\extract-insights.ps1 filtered.txt
    .\extract-insights.ps1 filtered.txt > insights.txt
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
Read the file at $fullPath. It contains Teams messages sent by Jonathan / Yoni Ben-Ami.
Extract structured insights into these categories:
- DECISIONS: Things decided or agreed to
- ACTION ITEMS: Tasks committed to or assigned
- COMMITMENTS: Promises, deadlines, deliverables mentioned
- FYIs: Important context, updates, information shared
For each item include: [conversation/channel, time] one-line summary.
Use plain text with ## headings per category. If a category has no items, write 'None found.'
If the file is empty or has no meaningful content, output 'No items found.'
"@

copilot -p $prompt
