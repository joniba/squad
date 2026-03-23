<#
.SYNOPSIS
    Removes a worktree created for a specific issue.
.EXAMPLE
    .\scripts\cleanup-worktree.ps1 -IssueNumber 42
#>
param(
    [Parameter(Mandatory)][int]$IssueNumber
)

$ErrorActionPreference = "Stop"
$repoRoot = git rev-parse --show-toplevel
$worktreePath = Join-Path (Join-Path $repoRoot "worktrees") "squad-$IssueNumber"

if (Test-Path $worktreePath) {
    Write-Host "Removing worktree: $worktreePath"
    git worktree remove $worktreePath --force
} else {
    Write-Host "Worktree not found: $worktreePath (already cleaned?)"
}

git worktree prune
Write-Host "Cleanup complete for issue #$IssueNumber"
