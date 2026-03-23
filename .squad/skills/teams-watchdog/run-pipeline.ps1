<#
.SYNOPSIS
    Chains the Teams watchdog pipeline: probe → single-agent scan → summary.
.DESCRIPTION
    2-step pipeline using file-based I/O. Stops on first failure.
    Step 1: probe-messages.ps1 fetches raw Teams messages via WorkIQ.
    Step 2: single-agent-scan.ps1 filters, extracts, and formats in one call.
    Output: dated markdown summary in the work directory.
.EXAMPLE
    .\run-pipeline.ps1
    .\run-pipeline.ps1 -Hours 24
#>
param([int]$Hours = 24)
$ErrorActionPreference = "Stop"
$dir = $PSScriptRoot
$work = Join-Path $dir "work"

if (Test-Path $work) { Remove-Item $work -Recurse -Force }
New-Item -ItemType Directory -Path $work | Out-Null

$raw     = Join-Path $work "01-raw.txt"
$date    = (Get-Date).ToString('yyyy-MM-dd')
$summary = Join-Path $work "summary-$date.md"

function Invoke-Step($Name, $ScriptBlock) {
    Write-Host "[$((Get-Date).ToString('HH:mm:ss'))] $Name..." -ForegroundColor Cyan
    & $ScriptBlock
    if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) {
        Write-Host "FAILED at: $Name (exit $LASTEXITCODE)" -ForegroundColor Red; exit 1
    }
    Write-Host "[$((Get-Date).ToString('HH:mm:ss'))] $Name done" -ForegroundColor Green
}

# 24h scan window (NOT 48h — avoids duplicate summaries on daily runs)
Invoke-Step "probe" { & "$dir\probe-messages.ps1" -Hours $Hours > $raw }
Invoke-Step "scan"  { & "$dir\single-agent-scan.ps1" -InputFile $raw -Hours $Hours > $summary }

Write-Host "[$((Get-Date).ToString('HH:mm:ss'))] Pipeline complete — $summary" -ForegroundColor Green
