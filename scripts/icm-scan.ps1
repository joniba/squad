<#
.SYNOPSIS
    Scans IcM for new customer-reported incidents on the configured team.
.DESCRIPTION
    Queries IcM for active incidents owned by the team, filters for customer-reported
    items created since the last check, and outputs a summary.
.EXAMPLE
    .\scripts\icm-scan.ps1
    .\scripts\icm-scan.ps1 -TeamId 116041 -SinceHours 4
#>
param(
    [int]$TeamId = 116041,
    [int]$SinceHours = 4
)
$ErrorActionPreference = "Stop"
$since = (Get-Date).AddHours(-$SinceHours).ToString("o")

Write-Host "Scanning IcM team $TeamId for incidents since $since..."

# Query IcM via MCP tool pattern — copilot -p invocation
$prompt = "Search IcM for active incidents owned by team ID $TeamId. Filter for customer-reported incidents created after $since. List each incident with: ID, severity, title, create date. If none found, say 'No new CRIs'."
$result = copilot -p $prompt 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "IcM query failed (exit $LASTEXITCODE)" -ForegroundColor Red
    exit 1
}

Write-Host $result
Write-Host "IcM scan complete — $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
