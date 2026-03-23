<#
.SYNOPSIS
    Queries GitHub for blocked issues and stale PRs, then sends a daily summary to Teams.
.EXAMPLE
    .\scripts\squad-daily-summary.ps1
.EXAMPLE
    .\scripts\squad-daily-summary.ps1 -Repo "jbenami_microsoft/ms-pa" -WebhookFile "C:\alt\webhook.url"
#>
param(
    [string]$Repo = "jbenami_microsoft/ms-pa",
    [string]$WebhookFile = (Join-Path $HOME ".squad\teams-webhook.url")
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Query GitHub for blocked issues (open, labeled "blocked")
$blocked = gh issue list --repo $Repo --label "blocked" --state open --json number,title --jq '.[] | "#\(.number) \(.title)"' 2>$null
$blockedList = if ($blocked) { $blocked -split "`n" | Where-Object { $_ } } else { @() }

# Query GitHub for stale PRs (open, no update in 24h)
$since = (Get-Date).AddHours(-24).ToString("yyyy-MM-ddTHH:mm:ssZ")
$allPrs = gh pr list --repo $Repo --state open --json number,title,updatedAt | ConvertFrom-Json
$stalePrs = $allPrs | Where-Object { $_.updatedAt -lt $since }
$staleList = $stalePrs | ForEach-Object { "#$($_.number) $($_.title)" }

# Query open issues per agent label
$agentCounts = @()
foreach ($agent in @("gandalf","aragorn","gimli","elrond","bilbo")) {
    $count = (gh issue list --repo $Repo --label "squad:$agent" --state open --json number 2>$null | ConvertFrom-Json).Count
    if ($count -gt 0) { $agentCounts += "$agent`: $count" }
}

# Build summary body
$lines = @("📊 **Daily Squad Status** — $(Get-Date -Format 'yyyy-MM-dd')")
$lines += ""
$lines += "**Blocked Issues:** $($blockedList.Count)"
$blockedList | ForEach-Object { $lines += "  - $_" }
$lines += "**Stale PRs (>24h):** $(($staleList | Measure-Object).Count)"
$staleList | ForEach-Object { $lines += "  - $_" }
if ($agentCounts) { $lines += "**Open per agent:** $($agentCounts -join ', ')" }
$body = $lines -join "`n"

# Send via notification script
& "$scriptDir\send-teams-notification.ps1" -Title "📊 Daily Squad Summary" -Body $body -WebhookFile $WebhookFile
