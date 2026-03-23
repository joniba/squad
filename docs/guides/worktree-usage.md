---
title: "Git Worktree Workflow for Parallel Agent Isolation"
date: 2024-12-19
author: Gimli
documentarian: Bilbo
category: guide
tags: [guide, workflow, git, tooling, bilbo, final]
status: final
---

# Git Worktree Workflow for Parallel Agent Isolation

Learn when and how to use git worktrees for parallel agent execution with isolated working directories.

## What It Does

Git worktrees allow multiple agents to work on different branches simultaneously without interfering with each other. Each agent gets its own isolated directory with its own branch, enabling:

- **Parallel execution:** 2+ agents working on different issues at the same time
- **Independent branches:** Each agent checks out a separate branch (squad/issue-slug)
- **No race conditions:** File system isolation prevents concurrent edits to the same files
- **Clean cleanup:** Simple commands to remove worktrees after work is done

## When to Use Worktrees vs `checkout -b`

### Use `git checkout -b` for:

- **Single sequential agent** working on one issue at a time
- **Quick feature branches** created for one-off work
- **Local-only development** not shared with other processes

Example:
```powershell
git checkout -b squad/42-fix-auth
# Work and commit
# Then merge/delete: git checkout main && git branch -D squad/42-fix-auth
```

### Use Git Worktrees for:

- **2 or more agents spawned in parallel** on different issues
- **Concurrent development** where agents push simultaneously
- **Coordinated orchestration** needing shared state (TEAM_ROOT)
- **Clean separation** of working directories

**Rule:** If you're spawning 2+ agents at once, use worktrees. Otherwise, `checkout -b` is fine.

## Prerequisites

- **PowerShell 7+** (Core or Desktop)
- **Git 2.17+** (worktrees available)
- **Scripts in `scripts/` directory:**
  - `create-worktree.ps1`
  - `cleanup-worktree.ps1`
  - `cleanup-all-worktrees.ps1`
- **Repo root** accessible (TEAM_ROOT)
- **Main branch** up to date

## Part 1: Coordinator Pre-Spawn Setup

Before spawning agents, the coordinator creates worktrees for each parallel task.

### Step 1: Create a Worktree for Each Agent

```powershell
# From TEAM_ROOT (the main checkout)
.\scripts\create-worktree.ps1 -IssueNumber 42 -Slug "fix-auth"
.\scripts\create-worktree.ps1 -IssueNumber 51 -Slug "refactor-storage"
```

Each creates:
- **Directory:** `./worktrees/squad-{issue}/`
- **Branch:** `squad/{issue}-{slug}` (based on main)

Expected output:
```
Creating worktree: ./worktrees/squad-42 (branch: squad/42-fix-auth, base: main)
Worktree ready:
  WORKTREE_PATH = C:\dev\pa-squad\worktrees\squad-42
  TEAM_ROOT     = C:\dev\pa-squad
  BRANCH        = squad/42-fix-auth
```

### Step 2: Verify Worktrees Were Created

```powershell
git worktree list
```

Output:
```
C:\dev\pa-squad                 (detached from 1a2b3c4)
C:\dev\pa-squad\worktrees\squad-42    squad/42-fix-auth
C:\dev\pa-squad\worktrees\squad-51    squad/51-refactor-storage
```

### Step 3: Get Paths for Agent Spawn

Extract the WORKTREE_PATH values:
- Worktree 1: `C:\dev\pa-squad\worktrees\squad-42`
- Worktree 2: `C:\dev\pa-squad\worktrees\squad-51`

## Part 2: Agent Isolation Rules

When agents are spawned into a worktree, they MUST follow these rules strictly:

### Rule 1: `cd` into WORKTREE_PATH

```powershell
cd $env:WORKTREE_PATH
# Now all work happens in this directory
```

### Rule 2: Never Create or Switch Branches

The branch is pre-configured. Do NOT run:
- ❌ `git checkout -b new-branch`
- ❌ `git checkout main`
- ❌ `git switch other-branch`

The branch is already checked out and ready to use.

### Rule 3: Read Shared State from TEAM_ROOT

If you need squad state (decisions, routing, decisions inbox):

```powershell
# Read from TEAM_ROOT, not from worktree
$decisions = Get-Content "$env:TEAM_ROOT\.squad\decisions.md" -Raw

# Do NOT read from $pwd\.squad\decisions.md
# (It won't exist or will be outdated)
```

### Rule 4: Write Decision Inbox to TEAM_ROOT

When writing decisions, write to the shared directory:

```powershell
$outbox = Join-Path $env:TEAM_ROOT ".squad\decisions\inbox\gimli-my-decision.md"
"# My Decision`nContext here..." | Set-Content $outbox

# Do NOT write to $pwd\.squad\decisions\inbox\...
# (Will be lost when the worktree is cleaned up)
```

### Rule 5: Commit and Push from WORKTREE_PATH

All commits and pushes happen in the worktree:

```powershell
cd $env:WORKTREE_PATH
git add .
git commit -m "feat: fix auth issue #42"
git push origin squad/42-fix-auth
```

### Rule 6: If Branch Setup Seems Wrong, STOP

If when the agent starts:
- The branch is incorrect
- `git status` shows unexpected files
- `.git` is missing

**Stop immediately and notify the coordinator.** Do not proceed.

## Part 3: Spawn Prompt Template

When spawning an agent into a worktree, include this block in the prompt:

```markdown
### Worktree Isolation (CRITICAL)

Environment:
- TEAM_ROOT = C:\dev\pa-squad           # Main checkout — where .squad/ state lives
- WORKTREE_PATH = C:\dev\pa-squad\worktrees\squad-42   # Your isolated checkout
- AGENT_BRANCH = squad/42-fix-auth      # Already set up — do NOT create or switch branches

Rules:
1. READ shared state from TEAM_ROOT: `$env:TEAM_ROOT\.squad\decisions.md`
2. WORK in WORKTREE_PATH: `cd $env:WORKTREE_PATH`
3. DO NOT create or switch branches — your branch is already checked out
4. WRITE decision inbox to TEAM_ROOT: `$env:TEAM_ROOT\.squad\decisions\inbox\{name}.md`
5. COMMIT and PUSH from WORKTREE_PATH

If the branch setup seems wrong, STOP and tell the Coordinator.
```

## Part 4: Post-Merge Cleanup

After a PR is merged into main, clean up the worktree.

### Option A: Clean a Single Worktree

```powershell
# From TEAM_ROOT
.\scripts\cleanup-worktree.ps1 -IssueNumber 42
```

Expected output:
```
Removing worktree: C:\dev\pa-squad\worktrees\squad-42
Cleanup complete for issue #42
```

### Option B: Clean All Merged Worktrees

After batch PRs are merged, clean all at once:

```powershell
# From TEAM_ROOT
.\scripts\cleanup-all-worktrees.ps1
```

This automatically detects which worktrees' branches have been merged or deleted, and removes them.

Expected output:
```
Removing orphaned worktree: C:\dev\pa-squad\worktrees\squad-42
Done. Removed 2 worktree(s).
```

### Verify Cleanup

```powershell
git worktree list
# Should show only TEAM_ROOT
```

## Real-World Example: Parallel Agent Workflow

### Scenario

Jonathan spawns Gimli and Aragorn to work on two issues in parallel.

### Coordinator Workflow

```powershell
# From TEAM_ROOT: C:\dev\pa-squad

# 1. Create worktrees for each agent
.\scripts\create-worktree.ps1 -IssueNumber 42 -Slug "fix-auth"
.\scripts\create-worktree.ps1 -IssueNumber 51 -Slug "refactor-storage"

# 2. Verify
git worktree list

# 3. Spawn Gimli into worktree 1
# [Spawn with WORKTREE_PATH = C:\dev\pa-squad\worktrees\squad-42]

# 4. Spawn Aragorn into worktree 2
# [Spawn with WORKTREE_PATH = C:\dev\pa-squad\worktrees\squad-51]

# 5. Wait for both agents to finish and push

# 6. Both PRs merge into main

# 7. Clean up both worktrees
.\scripts\cleanup-all-worktrees.ps1
```

### Agent 1 (Gimli) Workflow

```powershell
# 1. Start in worktree
cd C:\dev\pa-squad\worktrees\squad-42

# 2. Verify branch
git status
# On branch squad/42-fix-auth

# 3. Read shared decisions
$decisions = Get-Content "C:\dev\pa-squad\.squad\decisions.md" -Raw

# 4. Do work
# ... edit files, test, debug

# 5. Commit
git add .
git commit -m "fix(auth): resolve token expiration issue #42"

# 6. Push
git push origin squad/42-fix-auth

# 7. When done, await merge
```

### Agent 2 (Aragorn) Workflow

Same isolation rules apply — both agents work independently, each in their own worktree.

## Error Handling

### "fatal: '{path}' is already checked out"

A worktree already exists for this path. Remove it first:

```powershell
git worktree remove C:\dev\pa-squad\worktrees\squad-42 --force
```

Then recreate:

```powershell
.\scripts\create-worktree.ps1 -IssueNumber 42 -Slug "fix-auth"
```

### "fatal: a branch named '{name}' already exists"

A local branch from a previous run still exists. Delete it:

```powershell
git branch -D squad/42-fix-auth
```

Then retry:

```powershell
.\scripts\create-worktree.ps1 -IssueNumber 42 -Slug "fix-auth"
```

### "worktree remove" Fails with Lock

Stale lock file is preventing removal. Force it:

```powershell
git worktree remove C:\dev\pa-squad\worktrees\squad-42 --force
```

If it still fails, use `git worktree prune`:

```powershell
git worktree prune
```

Then verify:

```powershell
git worktree list
```

### Cleanup Fails Entirely

Corrupt worktree state. Fallback:

```powershell
# Nuclear option: remove the directory manually
Remove-Item C:\dev\pa-squad\worktrees\squad-42 -Recurse -Force

# Then clean up git's references
git worktree prune

# Verify
git worktree list
```

### Agent Tries to Switch Branches

If an agent accidentally runs `git checkout main`:

1. The worktree branch becomes detached
2. Subsequent pushes will fail
3. **Stop and notify the coordinator**

The coordinator can fix it:

```powershell
git -C C:\dev\pa-squad\worktrees\squad-42 checkout squad/42-fix-auth
```

## Quick Reference

```powershell
# Create
.\scripts\create-worktree.ps1 -IssueNumber 42 -Slug "fix-auth"

# List all
git worktree list

# Remove one
.\scripts\cleanup-worktree.ps1 -IssueNumber 42

# Remove all merged
.\scripts\cleanup-all-worktrees.ps1

# Prune stale references
git worktree prune

# Force remove if cleanup fails
git worktree remove C:\dev\pa-squad\worktrees\squad-42 --force
```

## Advanced: Custom Base Branch

By default, worktrees are created from `main`. To use a different base:

```powershell
.\scripts\create-worktree.ps1 -IssueNumber 42 -Slug "fix-auth" -BaseBranch "develop"
```

This creates the worktree branch from `develop` instead of `main`.

## Performance Notes

- **Worktree creation:** <1 second
- **Worktree cleanup:** <1 second
- **No additional git overhead** during agent work (it's a normal git checkout)

Worktrees are extremely lightweight.

## Related

- `scripts/create-worktree.ps1` — Create worktrees
- `scripts/cleanup-worktree.ps1` — Remove one worktree
- `scripts/cleanup-all-worktrees.ps1` — Remove all merged
- `.squad/skills/worktree-lifecycle/SKILL.md` — Architecture details

## Next Steps

- **For coordinators:** Document your spawn prompts with the worktree isolation block
- **For agents:** Add the worktree paths to your initial checklist
- **For teams:** Make worktrees your default for any 2+ parallel agent work
