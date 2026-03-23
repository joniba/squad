<#
.SYNOPSIS
  Launches squad-monitor dashboard with pa-squad data adapters.
.DESCRIPTION
  Runs orchestration log adapter to sync data, then launches squad-monitor.
  Supports all squad-monitor flags passthrough.
.PARAMETER Once
  Render once and exit (passes --once to squad-monitor).
.PARAMETER NoGitHub
  Skip GitHub API calls (passes --no-github to squad-monitor).
.PARAMETER Interval
  Refresh interval in seconds. Default: 5.
.PARAMETER SharpUI
  Use beta SharpConsoleUI mode.
#>
param(
    [switch]$Once,
    [switch]$NoGitHub,
    [int]$Interval = 5,
    [switch]$SharpUI
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$teamRoot = (Get-Item "$scriptDir\..\..\..").FullName

# Sync orchestration log to squad-monitor format
& "$scriptDir\write-orchestration-log.ps1" -TeamRoot $teamRoot

# Build squad-monitor args
$args = @("--interval", $Interval)
if ($Once)     { $args += "--once" }
if ($NoGitHub) { $args += "--no-github" }
if ($SharpUI)  { $args += "--sharp-ui" }

# Launch squad-monitor from team root
Push-Location $teamRoot
try {
    & squad-monitor @args
} finally {
    Pop-Location
}
