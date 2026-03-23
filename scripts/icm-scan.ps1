<#
.SYNOPSIS
    Scans IcM for active incidents matching configurable filters.
.DESCRIPTION
    Queries IcM for active incidents owned by the team. Default filter matches:
    Sev2, Sev2.5 (Sev3 recommended for bump), and CRIs (any severity).
    Filter is transparent — printed on every scan.
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

# Print the query transparently
Write-Host "ICM Scan — $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
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

# Build the prompt with explicit filter criteria
$filterDesc = @()
if ($filters -contains 'all') { $filterDesc += "all active incidents" }
else {
    if ($filters -contains 'sev0' -or $filters -contains 'sev1' -or $filters -contains 'sev2') {
        $sevs = @(); @('sev0','sev1','sev2') | ForEach-Object { if ($filters -contains $_) { $sevs += $_.Replace('sev','') } }
        $filterDesc += "severity $($sevs -join ' or ')"
    }
    if ($filters -contains 'sev2.5') { $filterDesc += "severity 3 that should be bumped to sev2 (recommended severity increase)" }
    if ($filters -contains 'sev3') { $filterDesc += "severity 3" }
    if ($filters -contains 'cri') { $filterDesc += "ANY severity where incident type is 'System/Customer Reported' (CRI)" }
}

$prompt = @"
Search IcM for active incidents owned by team ID $TeamId.
Filter criteria (apply ALL of these as OR conditions):
$($filterDesc | ForEach-Object { "- $_" } | Out-String)
Only include incidents created after $($since.ToString('o')).
For each match: ID, severity, title, type, created date.
If none found, say 'No matching incidents'.
"@

$result = copilot -p $prompt 2>$null
$matchCount = ([regex]::Matches($result, '^\s*\d+\.|\|\s*\d{5,}', 'Multiline')).Count
Write-Host "ICM scan: $matchCount incidents matched [$Filter] in last ${SinceHours}h"
