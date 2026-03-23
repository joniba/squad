# Worktree Lifecycle — Parallel Agent Isolation

> Teaches the coordinator the create → spawn → work → merge → cleanup flow for parallel agent execution.

## When to Use Worktrees vs `checkout -b`

| Scenario | Method | Why |
|----------|--------|-----|
| Single agent (sequential work) | `git checkout -b squad/{issue}-{slug}` | Fast, no cleanup, simple |
| Multiple agents (parallel spawns) | `git worktree add` per agent | Isolated directories, no race conditions |

**Rule:** If you're spawning 2+ agents at once, use worktrees. Otherwise, `checkout -b` is fine.

## Pre-Spawn: Coordinator Creates Worktrees

Before spawning each parallel agent, the coordinator runs:

```powershell
# From TEAM_ROOT (main checkout)
.\scripts\create-worktree.ps1 -IssueNumber 42 -Slug "fix-auth"
```

This creates:
- **Directory:** `./worktrees/squad-42/`
- **Branch:** `squad/42-fix-auth` (based on main)

Repeat for each parallel agent with its own issue number.

## Spawn Prompt: Add WORKTREE_PATH

When spawning an agent into a worktree, add this block to the spawn prompt:

```markdown
### Worktree Isolation (CRITICAL)

Environment:
- TEAM_ROOT = {team_root}           # Main checkout — where .squad/ state lives
- WORKTREE_PATH = {worktree_path}   # Your isolated checkout
- AGENT_BRANCH = squad/{issue}-{slug}  # Already set up — do NOT create or switch branches

Rules:
1. READ shared state from TEAM_ROOT: `$TEAM_ROOT/.squad/decisions.md`
2. WORK in WORKTREE_PATH: `cd $WORKTREE_PATH`
3. DO NOT create or switch branches — your branch is already checked out
4. WRITE inbox to TEAM_ROOT: `$TEAM_ROOT/.squad/decisions/inbox/{agent}-{slug}.md`
5. COMMIT and PUSH from WORKTREE_PATH

If the branch setup seems wrong, STOP and tell the Coordinator.
```

## Agent Isolation Rules

Each agent in a worktree must:

1. **`cd` into WORKTREE_PATH** as the first action
2. **Never run `git checkout`** — the branch is pre-configured
3. **Read `.squad/` state from TEAM_ROOT**, not from their worktree
4. **Write decision inbox to TEAM_ROOT** (shared across all agents)
5. **Commit and push only from WORKTREE_PATH**

The Scribe stays on the main checkout (TEAM_ROOT) and never needs its own worktree.

## Post-Merge: Cleanup

After a PR is merged, clean up the worktree:

```powershell
# Single worktree
.\scripts\cleanup-worktree.ps1 -IssueNumber 42

# All merged/orphaned worktrees
.\scripts\cleanup-all-worktrees.ps1
```

## Error Handling

| Error | Cause | Fix |
|-------|-------|-----|
| `fatal: '{path}' is already checked out` | Worktree already exists for this path | Remove first: `git worktree remove {path} --force` |
| `fatal: a branch named '{name}' already exists` | Branch from a previous run | Delete: `git branch -D {name}` |
| `worktree remove` fails with lock | Stale lock file | Force: `git worktree remove {path} --force` |
| Cleanup fails entirely | Corrupt worktree state | Fallback: `git worktree prune` clears stale references |
| Agent tries to switch branches | Spawn prompt missing WORKTREE_PATH block | Re-spawn with correct prompt — agent should never create branches in worktree mode |

## Quick Reference

```powershell
# Create
.\scripts\create-worktree.ps1 -IssueNumber 42 -Slug "fix-auth"

# List
git worktree list

# Cleanup one
.\scripts\cleanup-worktree.ps1 -IssueNumber 42

# Cleanup all merged
.\scripts\cleanup-all-worktrees.ps1
```
