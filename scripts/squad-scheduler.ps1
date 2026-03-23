# squad-scheduler.ps1 — Unified dispatcher for recurring tasks (reads .squad/scheduler.json)
param([string[]]$Include, [string[]]$Exclude, [string[]]$Tasks, [switch]$DryRun, [switch]$Once)
$ErrorActionPreference = "Stop"
$root = (git rev-parse --show-toplevel 2>$null) -replace '/','\'; if (-not $root) { $root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot) }
$config = Get-Content (Join-Path $root ".squad\scheduler.json") -Raw | ConvertFrom-Json
$statePath = Join-Path $root ".squad\scheduler-state.json"
$logFile = Join-Path $root $config.log_file
$state = if (Test-Path $statePath) { Get-Content $statePath -Raw | ConvertFrom-Json } else { @{} }

function Write-Log($msg) {
    $line = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') $msg"
    Write-Host $line; Add-Content -Path $logFile -Value $line -EA SilentlyContinue
}
function ConvertTo-Seconds($i) {
    if ($i -match '^(\d+)h$') { return [int]$Matches[1]*3600 }
    if ($i -match '^(\d+)m$') { return [int]$Matches[1]*60 }
    if ($i -match '^(\d+)d$') { return [int]$Matches[1]*86400 }
    return 3600
}
function Test-Condition($task) {
    if (-not $task.condition) { return $true }
    if ($task.condition -eq "oncall") { return [bool]$config.oncall.enabled }
    return $true
}
function Get-TaskList {
    $all = $config.tasks
    if ($Tasks) { return $all | Where-Object { $Tasks -contains $_.name } }
    $list = $all | Where-Object { $_.enabled }
    if ($Include) { $list = @($list) + @($all | Where-Object { $Include -contains $_.name -and -not $_.enabled }) }
    if ($Exclude) { $list = $list | Where-Object { $Exclude -notcontains $_.name } }
    return $list
}
function Test-Due($task) {
    $last = $state.PSObject.Properties[$task.name]
    if (-not $last) { return $true }
    return ((Get-Date) - [datetime]$last.Value).TotalSeconds -ge (ConvertTo-Seconds $task.interval)
}
function Invoke-Task($task, [switch]$ForceCondition) {
    $script = Join-Path $root $task.script
    if (-not (Test-Path $script)) { Write-Log "  SKIP $($task.name) -- script not found"; return }
    if (-not $ForceCondition -and -not (Test-Condition $task)) { Write-Log "  SKIP $($task.name) -- condition not met"; return }
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        Write-Log "  RUN  $($task.name)"; & $script; $sw.Stop()
        Write-Log "  DONE $($task.name) ($([math]::Round($sw.Elapsed.TotalSeconds,1))s)"
        $state | Add-Member -NotePropertyName $task.name -NotePropertyValue (Get-Date -Format "o") -Force
    } catch {
        $sw.Stop(); Write-Log "  FAIL $($task.name) ($([math]::Round($sw.Elapsed.TotalSeconds,1))s) -- $_"
    }
}

do {
    Write-Log "--- scheduler tick ---"
    foreach ($t in (Get-TaskList)) {
        if ($DryRun) {
            $due = if (Test-Due $t) { "DUE" } else { "not due" }
            Write-Log "  [DRY] $($t.name) | $($t.interval) | $due | condition=$(if (Test-Condition $t) {'met'} else {'unmet'})"
        } elseif (Test-Due $t) {
            $force = ($Include -and $Include -contains $t.name) -or ($Tasks -and $Tasks -contains $t.name)
            Invoke-Task $t -ForceCondition:$force
        }
    }
    if (-not $DryRun) { $state | ConvertTo-Json | Set-Content $statePath -Encoding UTF8 }
    if (-not $Once -and -not $DryRun) { Write-Log "sleeping 60s..."; Start-Sleep -Seconds 60 }
} while (-not $Once -and -not $DryRun)
