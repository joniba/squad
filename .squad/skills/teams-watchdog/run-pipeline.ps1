<#
.SYNOPSIS
    Chains the Teams watchdog pipeline: probe → filter → extract → format.
.DESCRIPTION
    File-based I/O between steps. Stops on first failure.
.EXAMPLE
    .\run-pipeline.ps1
    .\run-pipeline.ps1 -Hours 24
#>
param([int]$Hours = 24)
$ErrorActionPreference = "Stop"
$dir = $PSScriptRoot
$work = Join-Path $dir "work"

# Clean slate for intermediate files
if (Test-Path $work) { Remove-Item $work -Recurse -Force }
New-Item -ItemType Directory -Path $work | Out-Null

$raw      = Join-Path $work "01-raw.txt"
$filtered = Join-Path $work "02-filtered.txt"
$insights = Join-Path $work "03-insights.txt"

function Invoke-Step($Name, $ScriptBlock) {
    Write-Host "[$((Get-Date).ToString('HH:mm:ss'))] $Name..." -ForegroundColor Cyan
    & $ScriptBlock
    if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) {
        Write-Host "FAILED at: $Name (exit $LASTEXITCODE)" -ForegroundColor Red; exit 1
    }
    Write-Host "[$((Get-Date).ToString('HH:mm:ss'))] $Name done" -ForegroundColor Green
}

# 24h scan window (NOT 48h — avoids duplicate summaries on daily runs)
Invoke-Step "probe"   { & "$dir\probe-messages.ps1" -Hours $Hours > $raw }
Invoke-Step "filter"  { & "$dir\filter-my-messages.ps1" -InputFile $raw > $filtered }
Invoke-Step "extract" { & "$dir\extract-insights.ps1" -InputFile $filtered > $insights }
Invoke-Step "format"  { & "$dir\format-summary.ps1" -InputFile $insights }

Write-Host "[$((Get-Date).ToString('HH:mm:ss'))] Pipeline complete." -ForegroundColor Green
