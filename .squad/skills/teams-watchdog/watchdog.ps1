# Watchdog loop — runs the Teams pipeline daily with observability.
# Modeled on ralph-watch.ps1: lockfile, heartbeat, structured log.
# Usage: .\watchdog.ps1 [-IntervalHours 12]
param([int]$IntervalHours = 24)
$ErrorActionPreference = "Stop"

# Lockfile: prevent duplicate instances
$lockFile = Join-Path $PSScriptRoot ".watchdog.lock"
if (Test-Path $lockFile) {
    $lock = Get-Content $lockFile -Raw | ConvertFrom-Json -ErrorAction SilentlyContinue
    if ($lock.pid -and (Get-Process -Id $lock.pid -ErrorAction SilentlyContinue)) {
        Write-Host "Watchdog already running (PID $($lock.pid))" -ForegroundColor Red; exit 1
    }
    Remove-Item $lockFile -Force
}
@{ pid=$PID; started=(Get-Date -Format 'o') } | ConvertTo-Json | Out-File $lockFile -Encoding utf8
trap { Remove-Item $lockFile -Force -ErrorAction SilentlyContinue; break }

$logFile = Join-Path $PSScriptRoot "watchdog.log"
$hbFile = Join-Path $PSScriptRoot "watchdog-heartbeat.json"
$pipeline = Join-Path $PSScriptRoot "run-pipeline.ps1"
$round = 0; $failures = 0
Write-Host "Watchdog started — interval: ${IntervalHours}h, PID: $PID" -ForegroundColor Cyan

while ($true) {
    $round++; $start = Get-Date; $ts = $start.ToString('yyyy-MM-ddTHH:mm:ss')
    Write-Host "[$ts] Round $round..." -ForegroundColor Green
    try {
        & $pipeline -Hours 24  # 24h window — NOT 48h (avoids duplicate summaries)
        $ok = -not $LASTEXITCODE -or $LASTEXITCODE -eq 0
    } catch { $ok = $false }
    $dur = [math]::Round(((Get-Date) - $start).TotalSeconds, 1)
    if ($ok) {
        $failures = 0; $status = "OK"
        Write-Host "[$((Get-Date).ToString('HH:mm:ss'))] Round $round done (${dur}s)" -ForegroundColor Green
    } else {
        $failures++; $status = "FAIL"
        Write-Host "[$((Get-Date).ToString('HH:mm:ss'))] Round $round failed (${dur}s, streak: $failures)" -ForegroundColor Red
        if ($failures -ge 3) { Write-Host "ALERT: $failures consecutive failures!" -ForegroundColor Yellow }
    }
    "[$ts] round=$round status=$status duration=${dur}s failures=$failures" | Add-Content $logFile -Encoding utf8
    @{ lastRun=$ts; status=$status; round=$round; failures=$failures; pid=$PID } |
        ConvertTo-Json | Out-File $hbFile -Encoding utf8 -Force
    Write-Host "Next run in ${IntervalHours}h...`n" -ForegroundColor Gray
    Start-Sleep -Seconds ($IntervalHours * 3600)
}
