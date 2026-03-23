<#
.SYNOPSIS
    Formats extracted insights into a daily markdown summary (summaries/YYYY-MM-DD.md).
.EXAMPLE
    .\format-summary.ps1 insights.txt
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
$date = Get-Date -Format 'yyyy-MM-dd'
$outputDir = Join-Path $PSScriptRoot "summaries"
$outputFile = Join-Path $outputDir "$date.md"

if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

$prompt = @"
Read the file at $fullPath. It contains extracted insights from Teams messages.
Reformat into a clean daily summary in markdown. Requirements:
- Title: '# Teams Summary ΓÇö $date'
- Sections: ## Decisions, ## Action Items, ## Commitments, ## FYIs
- Each item as a bullet with [channel, time] prefix
- OMIT any section that has no items or says 'None found' ΓÇö do not include it at all
- If the file is empty or has no content, output only '# Teams Summary ΓÇö $date' and 'No activity.'
- Output clean markdown only, no commentary
"@

copilot -p $prompt > $outputFile
Write-Host "Summary saved to: $outputFile"
