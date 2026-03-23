<#
.SYNOPSIS
    Config transparency for IcM scans. Actual scanning is agent-driven.
.DESCRIPTION
    Prints the scan parameters (team, filter, since) for transparency.
    The actual IcM scan is performed by Aragorn (agent) using IcM MCP tools directly.
    
    This script serves as config documentation only. To run the actual scan:
      coordinator, spawn aragorn for icm-scan
    Or via scheduler:
      .\scripts\squad-scheduler.ps1 -Tasks icm-scan -Once
.PARAMETER TeamId
    IcM team ID (default: 116041 = DAKOTA\ThreatIntelligence)
.PARAMETER SinceHours
    Lookback window in hours (default: 4)
.PARAMETER Filter
    Configurable filter string. Default: "sev2,sev2.5,cri"
    Options: sev0, sev1, sev2, sev2.5, sev3, cri, all
.EXAMPLE
    .\scripts\icm-scan.ps1
    .\scripts\icm-scan.ps1 -Filter "sev2,cri"
    .\scripts\icm-scan.ps1 -Filter "all" -SinceHours 24
#>
param(
    [int]$TeamId = 116041,
    [int]$SinceHours = 4,
    [string]$Filter = "sev2,sev2.5,cri"
)
$ErrorActionPreference = "Stop"
$since = (Get-Date).AddHours(-$SinceHours)
$filters = $Filter -split ',' | ForEach-Object { $_.Trim().ToLower() }

# Print the query transparently (config only)
Write-Host "ICM Scan Configuration — $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
Write-Host "  Team:    $TeamId"
Write-Host "  Since:   $($since.ToString('yyyy-MM-dd HH:mm')) ($SinceHours h)"
Write-Host "  Filter:  $Filter"
Write-Host "  Matching: $(if ($filters -contains 'all') { 'ALL active incidents' } else {
    $parts = @()
    if ($filters -contains 'sev0') { $parts += 'Sev0' }
    if ($filters -contains 'sev1') { $parts += 'Sev1' }
    if ($filters -contains 'sev2') { $parts += 'Sev2' }
    if ($filters -contains 'sev2.5') { $parts += 'Sev3 (recommended Sev2 bump)' }
    if ($filters -contains 'sev3') { $parts += 'Sev3' }
    if ($filters -contains 'cri') { $parts += 'CRIs (any sev, type=System/Customer Reported)' }
    $parts -join ' + '
})"
Write-Host ""
Write-Host "To execute the scan: coordinator, spawn aragorn for icm-scan" -ForegroundColor Yellow
Write-Host "Or via scheduler: .\scripts\squad-scheduler.ps1 -Tasks icm-scan -Once" -ForegroundColor Yellow
Write-Host ""
