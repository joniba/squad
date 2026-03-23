<#
.SYNOPSIS
    Removes all squad worktrees whose branch has been merged or deleted.
.EXAMPLE
    .\scripts\cleanup-all-worktrees.ps1
#>
$ErrorActionPreference = "Stop"
$repoRoot = git rev-parse --show-toplevel
$worktreeDir = Join-Path $repoRoot "worktrees"

if (-not (Test-Path $worktreeDir)) {
    Write-Host "No worktrees directory found - nothing to clean."
    exit 0
}

$removed = 0
Get-ChildItem -Path $worktreeDir -Directory | Where-Object { $_.Name -match '^squad-\d+$' } | ForEach-Object {
    $path = $_.FullName
    $branch = git worktree list --porcelain | Select-String -Pattern "worktree $([regex]::Escape($path))" -Context 0,2 |
        ForEach-Object { ($_.Context.PostContext | Select-String 'branch refs/heads/(.+)').Matches.Groups[1].Value }
    if (-not $branch -or -not (git branch --list $branch)) {
        Write-Host "Removing orphaned worktree: $path"
        git worktree remove $path --force 2>$null
        $removed++
    }
}

git worktree prune
Write-Host "Done. Removed $removed worktree(s)."
