<#
.SYNOPSIS
    Creates a git worktree for an agent working on a specific issue.
.EXAMPLE
    .\scripts\create-worktree.ps1 -IssueNumber 42 -Slug "fix-auth"
    # Creates ./worktrees/squad-42/ on branch squad/42-fix-auth
#>
param(
    [Parameter(Mandatory)][int]$IssueNumber,
    [Parameter(Mandatory)][string]$Slug,
    [string]$BaseBranch = "main"
)

$ErrorActionPreference = "Stop"
$repoRoot = git rev-parse --show-toplevel
$worktreePath = Join-Path (Join-Path $repoRoot "worktrees") "squad-$IssueNumber"
$branchName = "squad/$IssueNumber-$Slug"

# Clean up stale worktree if it exists
if (Test-Path $worktreePath) {
    Write-Host "Removing existing worktree at $worktreePath"
    git worktree remove $worktreePath --force 2>$null
}

# Clean up stale local branch if it exists
$ErrorActionPreference = "Continue"
git branch -D $branchName 2>$null
$ErrorActionPreference = "Stop"

# Create the worktree
Write-Host "Creating worktree: $worktreePath (branch: $branchName, base: $BaseBranch)"
git worktree add $worktreePath -b $branchName $BaseBranch
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create worktree"
    exit 1
}

# Validate
if (-not (Test-Path (Join-Path $worktreePath ".git"))) {
    Write-Error "Worktree created but .git marker missing - something went wrong"
    exit 1
}

Write-Host "Worktree ready:"
Write-Host "  WORKTREE_PATH = $worktreePath"
Write-Host "  TEAM_ROOT     = $repoRoot"
Write-Host "  BRANCH        = $branchName"
