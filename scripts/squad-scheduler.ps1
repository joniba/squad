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
function Write-LogOnly($msg) {
    Add-Content -Path $logFile -Value "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') $msg" -EA SilentlyContinue
}
function ConvertTo-Seconds($i) {
    if ($i -match '^(\d+)h$') { return [int]$Matches[1]*3600 }
    if ($i -match '^(\d+)m$') { return [int]$Matches[1]*60 }
    if ($i -match '^(\d+)d$') { return [int]$Matches[1]*86400 }
    return 3600
}
function Format-Remaining([int]$seconds) {
    if ($seconds -le 0) { return "now" }
    $h = [math]::Floor($seconds / 3600)
    $m = [math]::Floor(($seconds % 3600) / 60)
    $s = $seconds % 60
    if ($h -gt 0) { return "${h}h ${m}m" }
    if ($m -gt 0) { return "${m}m" }
    return "${s}s"
}
function Get-Remaining($task) {
    $last = $state.PSObject.Properties[$task.name]
    if (-not $last) { return 0 }
    $elapsed = ((Get-Date) - [datetime]$last.Value).TotalSeconds
    $interval = ConvertTo-Seconds $task.interval
    return [math]::Max(0, [int]($interval - $elapsed))
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
    if ($task.type -eq "agent") {
        if (-not $ForceCondition -and -not (Test-Condition $task)) { Write-Log "  SKIP $($task.name) -- condition not met"; return }
        Write-Log "  AGENT $($task.name) --> $($task.agent)"
        # Signal line: coordinator reads this from stdout and spawns the named agent with the given prompt
        Write-Host "AGENT_TASK|$($task.agent)|$($task.prompt)"
        $state | Add-Member -NotePropertyName $task.name -NotePropertyValue (Get-Date -Format "o") -Force
        return
    }
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
function Start-SpinnerSleep([int]$totalSeconds, $taskList) {
    $spinChars = '|', '/', '-', '\'
    $spinIdx = 0
    $w = [Console]::WindowWidth - 1
    for ($i = $totalSeconds; $i -gt 0; $i--) {
        $spin = $spinChars[$spinIdx % 4]; $spinIdx++
        $parts = @(foreach ($t in $taskList) {
            if (-not (Test-Condition $t)) { continue }
            "$($t.name) in $(Format-Remaining (Get-Remaining $t))"
        })
        $line = "⏳ $spin $($parts -join ' | ')"
        if ($line.Length -gt $w) { $line = $line.Substring(0, $w) }
        Write-Host -NoNewline "`r$($line.PadRight($w))"
        Start-Sleep -Seconds 1
    }
    Write-Host -NoNewline "`r$(' ' * $w)`r"
}

do {
    $taskList = Get-TaskList
    if ($DryRun) { Write-Log "--- scheduler tick ---" } else { Write-LogOnly "--- scheduler tick ---" }
    foreach ($t in $taskList) {
        if ($DryRun) {
            $due = if (Test-Due $t) { "DUE" } else { "not due" }
            $type = if ($t.type) { $t.type } else { "script" }
            Write-Log "  [DRY] $($t.name) | $type | $($t.interval) | $due | condition=$(if (Test-Condition $t) {'met'} else {'unmet'})"
        } else {
            $force = $Tasks -and $Tasks -contains $t.name
            if ($force -or (Test-Due $t)) { Invoke-Task $t -ForceCondition:$force }
            else { Write-LogOnly "  SKIP $($t.name) -- not due (last: $($state.PSObject.Properties[$t.name].Value))" }
        }
    }
    if (-not $DryRun) { $state | ConvertTo-Json | Set-Content $statePath -Encoding UTF8 }
    if (-not $Once -and -not $DryRun) { Start-SpinnerSleep -totalSeconds 60 -taskList $taskList }
} while (-not $Once -and -not $DryRun)
