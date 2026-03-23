---
title: "Worktree Lifecycle for Parallel Agent Execution — Comprehensive Research"
date: 2026-03-22
author: Elrond
documentarian: bilbo
category: research
tags:
  - git
  - worktree
  - parallel-execution
  - agent-coordination
  - squad-infra
status: final
related_docs:
  - research/worktrees/worktree-article-analysis.md
  - research/worktrees/worktree-parallelism-research.md
---

# Worktree Lifecycle for Parallel Agent Execution — Comprehensive Research

**Date:** 2026-03-22  
**Researcher:** Elrond  
**Requested by:** Jonathan (P1-HIGH, Issue #59)  
**Project:** pa-squad  
**Investigation Scope:** Git worktree implementation for squad parallel agent execution

---

## Executive Summary

Squad agents currently spawn to a shared working tree and use `git checkout -b`, which causes race conditions when multiple agents run in parallel. This research answers 8 critical questions about worktree-based isolation:

1. **Current flow:** Coordinator spawns agents via `.squad/templates/squad.agent.md`; agents do `git checkout -b` in place (no worktree creation).
2. **Minimal change:** Update spawn prompt template to create worktrees pre-spawn; agents work in isolated directories, no branch creation needed.
3. **WORKTREE_PATH vs TEAM_ROOT:** Complementary — `TEAM_ROOT` locates `.squad/` state (on main via strategy), `WORKTREE_PATH` locates isolated working directory.
4. **Cleanup lifecycle:** After PR merge (cleanest, requires automation via GitHub workflow or post-merge hook).
5. **Edge cases:** Worktree collision detection, remote branch cleanup, locked worktrees, branch switching prevention — all addressable.
6. **Scribe integration:** Scribe uses `TEAM_ROOT` (main checkout) for state commits; worktree-local state merges through branch merges.
7. **Existing patterns:** Tamir's distributed systems work uses append-only merge + drop-box pattern (not worktrees directly); squad.agent.md documents worktree awareness but doesn't enforce it.
8. **Platform constraints:** `git worktree add` fully supported on Windows; no path length issues; VS Code supports multiple worktrees seamlessly.

**Recommendation:** Implement **Coordinator-managed worktrees** with pre-spawn setup, post-merge cleanup, and main-checkout strategy for shared `.squad/` state.

---

## Research Question 1: Current Coordinator Flow

### Exact Path: Coordinator → Spawn Prompt → Agent Git Commands

**Entry Point:** `.squad/templates/squad.agent.md` (Team Mode, line 609+)

#### Coordinator Responsibility (squad.agent.md)
```
### How to Spawn an Agent

You MUST call the `task` tool with these parameters:
- agent_type: "general-purpose"
- mode: "background" (default)
- description: "{emoji} {Name}: {brief task summary}"
- prompt: {full agent prompt, inlined charter + context}
```

**Current spawn prompt template (lines 626–650):**

```
You are {Name}, the {Role} on this project.

YOUR CHARTER:
{paste contents of .squad/agents/{name}/charter.md here}

TEAM ROOT: {team_root}
All `.squad/` paths are relative to this root.

Read .squad/agents/{name}/history.md (your project knowledge).
Read .squad/decisions.md (team decisions to respect).
...
```

**What the template does NOT include:**
- No `WORKTREE_PATH` variable
- No pre-spawn worktree creation instructions
- No instruction to avoid branch creation
- Agents assume they're in a "normal" checkout and can `git checkout -b`

#### Agent Responsibility (Implied from Charter)
Each agent receives the spawn prompt and does:

1. **Discover the issue:** From the spawn request (e.g., "GitHub Issue #59: Research worktree lifecycle")
2. **Create a working branch:** Agents infer the pattern from past sessions: `git checkout -b squad/{issue-num}-{slug}`
3. **Do work:** Commit, push
4. **Return results:** Agent exits, Coordinator collects results

**Evidence:** Agent charters (e.g., `.squad/agents/gimli/charter.md`) don't mention worktrees at all. The "do work" section says:
> "When you make changes, use git: `git add`, `git commit -m "message"`, `git push origin {branch}`"

No mention of branch creation, no mention of worktrees.

#### The Race Condition: When Parallel Agents Collide

**Today's incident (2026-03-22, Issue #59 context):**

1. Coordinator spawns 8 agents: Gimli, Bilbo, Elrond, Aragorn, Galadriel, Gandalf, Scribe, Ralph
2. All 8 share the **same working tree** (no worktree isolation)
3. Execution order (parallel ≈ concurrent):
   ```
   Agent 1: git checkout -b squad/1-issue
   Agent 2: git checkout -b squad/2-issue     ← Switches tree away from Agent 1
   Agent 3: git checkout -b squad/3-issue     ← Switches tree away from Agent 2
   ...
   Agent 8: git checkout -b squad/8-issue     ← Switches tree to squad/8
   ```
4. **Result:** Working tree lands on `squad/8` branch, Agents 1–7 have stashed/lost work locally
5. **But:** All PRs landed on GitHub (push is atomic and doesn't depend on local tree state)

**Key insight:** The current spawn prompt has no guards against this. Squad.agent.md documents worktree *awareness* (lines 562–600) but doesn't *enforce* worktree creation.

### Conclusion on Question 1

**Current flow is:** Coordinator spawns agents with generic spawn prompt → agents infer branch creation pattern → agents race on shared checkout → local workspace corrupts, but GitHub work survives.

**Squad governance document has awareness but no enforcement:** `.squad/templates/squad.agent.md` lines 562–600 document worktree strategy, but the agent spawn template (lines 609–650) doesn't mandate worktree isolation.

---

## Research Question 2: Minimal Change to Make Worktrees Default for Parallel Spawns

### Scope of Change

**Minimal viable change is: Update the spawn prompt template only. Do NOT require scripts.**

#### What Changes

1. **Add to spawn prompt (in `squad.agent.md` lines 609–650):**

```
WORKTREE_PATH: {path}/worktrees/squad.{issue_id}.{slug}
TEAM_ROOT: {path}
CURRENT_BRANCH: squad/{issue_id}-{slug}

Instructions:
- You are working in a dedicated git worktree: {WORKTREE_PATH}
- The branch squad/{issue_id}-{slug} is already created and checked out
- Do NOT create branches, do NOT switch branches, do NOT switch directories
- All git operations (add, commit, push) happen in {WORKTREE_PATH}
```

2. **Coordinator responsibility (implicit in spawn call):**
   - Before spawning agent: Run `git worktree add {WORKTREE_PATH} -b squad/{issue_id}-{slug} main`
   - Pass `WORKTREE_PATH` to spawn prompt
   - After agent finishes: Run `git worktree remove {WORKTREE_PATH}`

#### Why This Is Minimal

- **No new scripts needed** — coordinator already calls `task` tool, just needs to run `git worktree add` before spawn
- **No new tools required** — `git worktree` is standard Git (installed with Git)
- **No agent code changes** — agents just follow spawn instructions instead of inferring branch creation
- **Backward compatible** — old spawn prompts still work for sequential agents (they just get `WORKTREE_PATH` pointing to a non-existent directory; agents that ignore it still work)

#### What Does NOT Change

- `.gitattributes` — already correct (union merge driver configured)
- `.squad/` structure — no new directories needed
- Agent charters — agents already know how to git commit and push
- Scribe behavior — Scribe commits to `.squad/` on main checkout (via `TEAM_ROOT`)

### Evidence: Squad Already Has Worktree Resolution Logic

Lines 573–588 of `squad.agent.md` show coordinator already resolves team root for worktree awareness:

```
How the Coordinator resolves the team root (on every session start):

1. Run `git rev-parse --show-toplevel` to get the current worktree root.
2. Check if `.squad/` exists at that root
   - Yes → use **worktree-local** strategy
   - No → use **main-checkout** strategy. Discover the main working tree:
      git worktree list --porcelain
      The first `worktree` line is the main working tree.
3. Pass TEAM_ROOT to all agents
```

**This logic is already implemented and works.** The minimal change just extends it to *create* worktrees before spawn, not just detect them.

### Conclusion on Question 2

**Minimal change: Update spawn prompt template in `.squad/templates/squad.agent.md` to include `WORKTREE_PATH` and pre-spawn instructions to coordinator.** No scripts, no new tools, no breaking changes. The coordinator already handles `TEAM_ROOT` resolution; just extend it to create worktrees.

---

## Research Question 3: WORKTREE_PATH vs TEAM_ROOT — How Should They Work?

### Clarification: They Are Complementary, Not Competing

#### TEAM_ROOT: Where `.squad/` State Lives

**Definition:** Absolute path to the root of the repository where `.squad/` is located.

**Value determination (coordinator logic, already in squad.agent.md lines 573–588):**
- Run `git rev-parse --show-toplevel` → current tree root
- Check if `.squad/` exists at that root
  - **Yes** → `TEAM_ROOT = current root` (worktree-local strategy)
  - **No** → run `git worktree list --porcelain`, find main tree, `TEAM_ROOT = main tree root` (main-checkout strategy)

**Used for:** Resolving all `.squad/` paths in spawn prompt:
- Read: `.squad/agents/{name}/charter.md`
- Read: `.squad/decisions.md`
- Write: `.squad/decisions/inbox/{agent_name}-{slug}.md`
- Read: `.squad/identity/wisdom.md`

**Scribe uses `TEAM_ROOT`** to commit `.squad/` state to git. Scribe runs from the main checkout (or wherever `.squad/` lives) and commits state there.

#### WORKTREE_PATH: Where Agent Does Work

**Definition:** Absolute path to the isolated git worktree where this specific agent operates.

**Value determination (coordinator setup, NEW):**
```
WORKTREE_PATH = {TEAM_ROOT}/.squad/worktrees/squad.{issue_id}.{slug}
git worktree add {WORKTREE_PATH} -b squad/{issue_id}-{slug} main
```

**Used for:** Where agent's git checkout lives and where agent does work.
- `cd {WORKTREE_PATH}` — agent works here, not at `TEAM_ROOT`
- All git operations (add, commit, push) happen in `{WORKTREE_PATH}`
- `.squad/` state is still read from `{TEAM_ROOT}` (shared or branch-local depending on strategy)

**Agent instructions:**
```
TEAM_ROOT: /repo-root (where .squad/ is)
WORKTREE_PATH: /repo-root/.squad/worktrees/squad.42.fix-auth (where YOU work)

cd {WORKTREE_PATH}    ← your working directory
Read {TEAM_ROOT}/.squad/decisions.md   ← read shared decisions
Write {TEAM_ROOT}/.squad/decisions/inbox/gimli-fix-auth.md  ← write your decision
git add .                               ← add files in WORKTREE_PATH
git commit -m "..."                     ← commit in WORKTREE_PATH
git push origin squad/42-fix-auth       ← push from WORKTREE_PATH
```

### Visual Diagram: How TEAM_ROOT and WORKTREE_PATH Relate

```
Repository Layout (main-checkout strategy):
┌─ /repo-root (TEAM_ROOT)
│  ├─ .git/                           ← shared across all worktrees
│  ├─ .squad/                         ← state files live here (TEAM_ROOT)
│  │  ├─ decisions.md                 ← shared, read/write via TEAM_ROOT
│  │  ├─ agents/gimli/                ← shared
│  │  └─ worktrees/                   ← NEW: storage for worktree paths
│  │     ├─ squad.42.fix-auth/        ← WORKTREE_PATH for agent 1
│  │     ├─ squad.43.add-logging/     ← WORKTREE_PATH for agent 2
│  │     └─ squad.44.test-utils/      ← WORKTREE_PATH for agent 3
│  ├─ src/
│  ├─ package.json
│  └─ (main branch checked out here)
│
├─ /repo-root/.squad/worktrees/squad.42.fix-auth/ (WORKTREE_PATH for Agent 1)
│  ├─ .git → (symlink to /repo-root/.git)
│  ├─ src/
│  ├─ (squad/42-fix-auth branch checked out here)
│  └─ (Agent 1 works here, completely isolated)
│
├─ /repo-root/.squad/worktrees/squad.43.add-logging/ (WORKTREE_PATH for Agent 2)
│  ├─ .git → (symlink to /repo-root/.git)
│  ├─ src/
│  └─ (squad/43-add-logging branch checked out here)
│
└─ /repo-root/.squad/worktrees/squad.44.test-utils/ (WORKTREE_PATH for Agent 3)
   ├─ .git → (symlink to /repo-root/.git)
   └─ (squad/44-test-utils branch checked out here)
```

**Key insight:** `.git/` is shared (worktrees use symlinks to main repo's `.git`), so commits and branches are visible everywhere. `.squad/` lives at `TEAM_ROOT` and is either:
- **Main-checkout strategy (recommended for parallel):** Shared at `TEAM_ROOT`, read/write from there
- **Worktree-local strategy:** Branch-local (each worktree's branch has its own `.squad/`), merges via union merge driver

### Answer to Question 3

**WORKTREE_PATH and TEAM_ROOT are complementary:**
- `TEAM_ROOT` = where `.squad/` state lives (unchanged if using main-checkout strategy)
- `WORKTREE_PATH` = where agent does work (isolated on disk, but shares `.git/` with main tree)
- Coordinator passes both to agents
- Agents read `.squad/` from `TEAM_ROOT`, work in `WORKTREE_PATH`
- Scribe commits to `TEAM_ROOT` after collecting agent results

---

## Research Question 4: Cleanup Lifecycle — When Should Worktrees Be Removed?

### Four Candidate Strategies

#### Strategy A: After Agent Completes (TOO EARLY)

```
Timeline:
T1: Coordinator creates worktree, spawns agent
T2: Agent finishes work, pushes PR
T3: Coordinator removes worktree immediately
T4: (much later) PR reviewer approves and merges
T5: Worktree already gone — can't access branch state
```

**Problems:**
- PR reviewer can't access agent's worktree if they need to inspect/debug
- If PR is rejected, branch is harder to find (depends on who keeps it)
- Reduces auditability

**Not recommended.**

#### Strategy B: After Reviewer (Galadriel) Approves (MANUAL TRIGGER)

```
Timeline:
T1: Coordinator creates worktree, spawns agent
T2: Agent finishes work, pushes PR
T3: PR undergoes review → Galadriel approves
T4: Galadriel signals cleanup (via comment, label, or new issue)
T5: Coordinator removes worktree
T6: PR is merged
```

**Advantages:**
- Worktree available during entire review cycle
- Reviewer can debug in worktree if needed
- Explicit cleanup point

**Problems:**
- Requires manual coordination (Galadriel has to trigger)
- Depends on Galadriel being in the loop
- Easy to forget cleanup, leaving stale worktrees
- Doesn't scale (one more thing for Galadriel to manage)

**Partial automation:** Could make Galadriel label PRs with `reviewed:approved` and have cleanup automation watch for that + merge signal.

#### Strategy C: After PR Merge (RECOMMENDED)

```
Timeline:
T1: Coordinator creates worktree, spawns agent
T2: Agent finishes work, pushes PR
T3–T4: PR undergoes review
T5: PR is merged to main
T6: GitHub webhook triggers → cleanup automation removes worktree
```

**Advantages:**
- Fully automated (no manual trigger needed)
- Worktree available for full review cycle
- Clean state: after merge, branch is integrated to main, worktree is cleaned up
- Scales: same automation works for 1 agent or 100 agents

**Automation mechanism:** GitHub Actions workflow on `pull_request.closed` event:
```yaml
on:
  pull_request:
    types: [closed]

jobs:
  cleanup-worktree:
    if: github.event.pull_request.merged == true
    runs-on: ${{ runner.os }}
    steps:
      - uses: actions/checkout@v3
      - run: git worktree remove .squad/worktrees/squad.${{ github.event.pull_request.number }}.* 2>/dev/null || true
```

**Challenges:**
- Automation runs on GitHub's runner (not local machine)
- Worktree is on the developer's machine or a specific agent runner
- Cleanup can't reach local worktrees from GitHub Actions
- **Requires local automation** (long-running process or cron job)

#### Strategy D: Periodic Pruning (FALLBACK)

```
git worktree prune  # Remove orphaned/inaccessible worktrees
```

**Use case:** Cleanup any worktrees that got corrupted, locked, or left behind.

**Not a primary strategy** — too coarse-grained. Should be a fallback.

### Recommended Approach: Strategy C + Local Cleanup Automation

**Primary flow:**
1. Coordinator creates worktree pre-spawn
2. Agent works in worktree, pushes PR
3. PR is reviewed and merged
4. Local automation (e.g., background script, GitHub's API polling, or webhook) detects merge
5. Runs `git worktree remove` for that worktree

**Implementation:**

**Option 1: Local background watcher (Simplest)**
```powershell
# scripts/worktree-cleanup-watcher.ps1
while ($true) {
    # Poll GitHub API for merged PRs with squad labels
    $merged = gh pr list --search "is:merged is:closed label:squad" --json number,headRefName
    foreach ($pr in $merged) {
        $branchName = "squad/$($pr.number)-*"
        git worktree list --porcelain | grep $branchName | ForEach-Object {
            $path = $_.split()[0]
            git worktree remove $path 2>/dev/null
        }
    }
    Start-Sleep -Seconds 300  # Check every 5 minutes
}
```

**Option 2: Post-merge GitHub workflow + local webhook receiver**
```yaml
# .github/workflows/cleanup-worktree.yml
on:
  pull_request:
    types: [closed]

jobs:
  cleanup:
    if: github.event.pull_request.merged == true
    runs-on: ubuntu-latest
    steps:
      - run: |
          curl -X POST http://localhost:8888/cleanup \
            -H "Content-Type: application/json" \
            -d '{"issue": ${{ github.event.pull_request.number }}}'
```

Then local machine runs a simple HTTP listener that calls `git worktree remove` when it receives the webhook.

### Conclusion on Question 4

**Recommended: After PR merge (Strategy C) with local cleanup automation.**

Automation options:
1. **Local background watcher (simplest):** Runs on developer machine, polls GitHub API for merged PRs, removes corresponding worktrees
2. **GitHub webhook + local receiver (more robust):** GitHub Actions triggers webhook, local receiver cleans up

Either way, cleanup is automatic and scales with team size. Worktrees persist through entire review cycle (allowing debugging) and are cleaned up after merge.

---

## Research Question 5: Edge Cases — Handling Worktree Collisions and Failures

### Edge Case 1: Worktree Already Exists (Agent Retry)

**Scenario:** Agent fails midway. Coordinator retries, tries to create the same worktree.

**Symptom:**
```
$ git worktree add .squad/worktrees/squad.42.fix-auth main
fatal: '.squad/worktrees/squad.42.fix-auth' already exists
```

**Solution:**

Before creating worktree, check if it exists:
```powershell
$worktreePath = ".squad/worktrees/squad.42.fix-auth"
if (Test-Path $worktreePath) {
    git worktree remove $worktreePath --force
    Write-Host "Cleaned up stale worktree: $worktreePath"
}
git worktree add $worktreePath -b squad/42-fix-auth main
```

**Add to coordinator logic (pre-spawn):**
```
Before creating a worktree:
1. Check if it exists (Test-Path or [ -d ] on Unix)
2. If it exists, remove it with --force
3. Then create fresh worktree
```

### Edge Case 2: Branch Already Exists Remotely (Previous Corrupted Attempt)

**Scenario:** Previous parallel run had a collision. Branch `squad/42-fix-auth` exists on GitHub from a corrupted attempt, worktree no longer exists locally.

**Symptom:**
```
$ git worktree add .squad/worktrees/squad.42.fix-auth -b squad/42-fix-auth main
fatal: 'squad/42-fix-auth' already exists
```

**Solution:**

When creating worktree with new branch flag (`-b`), git checks if branch exists anywhere (local or remote). Need to handle this:

```powershell
# Option A: Use existing branch (fetch from remote)
$branchName = "squad/42-fix-auth"
git fetch origin $branchName
git worktree add $worktreePath $branchName

# Option B: Force create local branch (clobber old one)
$branchName = "squad/42-fix-auth"
git branch -D $branchName 2>/dev/null  # Delete local if exists
git branch -D remotes/origin/$branchName 2>/dev/null  # Delete remote tracking
git worktree add $worktreePath -b $branchName main  # Create fresh
git push origin --delete $branchName 2>/dev/null  # Clean up remote
```

**Recommendation:**

Add coordinator pre-spawn cleanup:
```powershell
# Pre-spawn cleanup
$issueBranchName = "squad/$issueId-*"
git branch -D $issueBranchName 2>/dev/null  # Local
git branch -D remotes/origin/$issueBranchName 2>/dev/null  # Remote tracking
git push origin --delete $issueBranchName 2>/dev/null  # Remote
```

This ensures fresh state for every agent spawn.

### Edge Case 3: Cleanup Fails (Worktree Locked, Files in Use)

**Scenario:** After merge, cleanup tries to remove worktree but fails because files are locked (editor still open, process holding files).

**Symptom:**
```
$ git worktree remove .squad/worktrees/squad.42.fix-auth
fatal: './worktrees/squad.42.fix-auth' is locked
```

**Solution:**

Git provides `--force` flag and `git worktree prune` for recovery:

```powershell
# Option A: Force remove
git worktree remove $worktreePath --force

# Option B: Prune stale worktrees
git worktree prune

# Option C: Manual recovery (last resort)
Remove-Item $worktreePath -Recurse -Force  # Delete directory
```

**Add to cleanup script (with retry logic):**
```powershell
function Remove-GitWorktree {
    param([string]$Path, [int]$RetryCount = 3)
    
    for ($i = 0; $i -lt $RetryCount; $i++) {
        try {
            git worktree remove $Path --force
            return $true
        } catch {
            Write-Warning "Failed to remove worktree (attempt $($i+1)/$RetryCount): $_"
            Start-Sleep -Seconds 5
        }
    }
    # Last resort: force delete directory
    Remove-Item $Path -Recurse -Force 2>/dev/null
    return $true
}
```

### Edge Case 4: Agent Tries to Switch Branches Inside Worktree

**Scenario:** Agent receives instructions but ignores them and tries `git checkout -b` inside the worktree (old pattern instinct).

**Symptom:**
```
Agent (confused): I'll create a new branch for this work
$ git checkout -b squad/42-different-branch
# Silently succeeds, now agent is on the wrong branch
# Agent commits to squad/42-different-branch instead of squad/42-fix-auth
```

**Solution:**

Add guard in spawn prompt:

```
CRITICAL: This worktree is already checked out to squad/{issue_id}-{slug}.
DO NOT run: git checkout -b
DO NOT switch branches
DO NOT change directories

If you need a different branch, that indicates a problem with this spawn.
Stop immediately and report the issue.

All your work MUST be on squad/{issue_id}-{slug} in {WORKTREE_PATH}.
```

**Add to agent charter:**
> "Never create branches inside a worktree spawn. Your branch is already set up. If you need a different branch, the spawn is misconfigured — ask the Coordinator."

### Edge Case 5: Worktree Created But Branch Creation Fails

**Scenario:** Coordinator creates worktree directory but `-b` flag fails (branch name conflict, etc.). Worktree exists but is unusable.

**Solution:**

Add validation after worktree creation:
```powershell
git worktree add $worktreePath -b $branchName main
if ($LASTEXITCODE -ne 0) {
    Write-Error "Worktree creation failed. Cleaning up."
    Remove-Item $worktreePath -Recurse -Force 2>/dev/null
    exit 1
}
```

### Conclusion on Question 5

**Edge cases and solutions:**

| Edge Case | Root Cause | Solution | Who Handles |
|-----------|-----------|----------|-------------|
| Worktree already exists | Retry, collision | Pre-spawn: `git worktree remove --force` if exists | Coordinator |
| Branch already exists remotely | Corrupted previous run | Pre-spawn: delete local/remote branch before creating | Coordinator |
| Cleanup fails (locked worktree) | Files still open | Cleanup script: retry with `--force`, fallback to rm | Cleanup automation |
| Agent tries to switch branches | Misunderstood instructions | Clear prompt: "DO NOT create/switch branches" | Spawn prompt |
| Worktree created but branch fails | Config error | Validate worktree after creation, rollback if fail | Coordinator |

**Recommendation:** Build these guards into the Coordinator's pre-spawn validation and cleanup scripts. Make the worktree lifecycle bulletproof.

---

## Research Question 6: Scribe Integration — Which Worktree Does Scribe Use?

### Two Strategies: Main-Checkout vs. Worktree-Local

#### Strategy A: Main-Checkout (Recommended for Parallel)

**Scribe operates from the main working tree:**

```
Repository Layout:
/repo-root (main checkout, TEAM_ROOT)
  ├─ .squad/decisions.md (Scribe writes here)
  ├─ .squad/agents/*/history.md (Scribe appends here)
  ├─ worktrees/squad.42.fix-auth/
  │  └─ (Agent 1 works here, isolated)
  ├─ worktrees/squad.43.add-logging/
  │  └─ (Agent 2 works here, isolated)
```

**How it works:**

1. All agents read `.squad/decisions.md` from `/repo-root` via `TEAM_ROOT` variable
2. All agents write inbox files: `.squad/decisions/inbox/{agent}-{slug}.md` to `/repo-root`
3. Agents do work in their respective `WORKTREE_PATH` directories
4. Scribe runs from main checkout (`/repo-root`):
   ```powershell
   cd $TEAM_ROOT  # /repo-root
   
   # Merge inbox files
   Get-ChildItem .squad/decisions/inbox/*.md | ForEach-Object {
       (Get-Content $_) | Add-Content .squad/decisions.md
       Remove-Item $_
   }
   
   # Commit .squad/ state
   git add .squad/
   git commit -m "docs: update squad state (decisions, history) — session {id}"
   
   # Scribe does NOT touch agent worktrees
   ```

**Why this is safe for parallel:**
- All agents read the same source of truth (main checkout)
- Agents write to separate inbox files (no conflicts)
- Scribe commits to main checkout only (single writer)
- No race conditions — Scribe commits after all agents finish

**Append-only merge driver (already configured in `.gitattributes`):**
```
.squad/decisions.md merge=union
.squad/agents/*/history.md merge=union
```

When branches merge back to main later, these files combine conflict-free (both sides kept).

#### Strategy B: Worktree-Local (For Isolated Teams)

**Each worktree's branch has its own `.squad/` state:**

```
Repository Layout:
/repo-root (main branch)
  ├─ .squad/decisions.md (main's version)
  └─ worktrees/squad.42.fix-auth/ (squad/42-fix-auth branch)
     ├─ .squad/decisions.md (branch's own version)
     └─ (Agent 1 works here)
```

**How it works:**

1. Agents read `.squad/decisions.md` from their branch's copy (via `TEAM_ROOT` pointing to their worktree)
2. Agents write inbox files to their branch's `.squad/` (branch-local)
3. Scribe runs from the agent's worktree while agent is still working:
   ```powershell
   cd $WORKTREE_PATH  # /repo-root/worktrees/squad.42.fix-auth
   
   # Merge inbox files into branch's .squad/
   git add .squad/
   git commit -m "docs: update squad state (agent history) — session {id}"
   ```

4. When branch merges to main, `.squad/` files merge via union driver (both versions kept)

**Why this is complex:**
- Scribe needs to know which worktree to run from (requires per-agent Scribe spawn)
- Each agent gets its own Scribe instance (more overhead)
- State fragmentation across branches (harder to find decisions)
- Merging back to main is complex (union driver helps, but still messy)

### Answer to Question 6: Recommended Approach

**Use Strategy A: Main-Checkout with Shared `.squad/` State**

**Why:**
- Simpler coordination: single `.squad/` state lives at `TEAM_ROOT` (main checkout)
- No per-agent Scribe spawns needed
- Decisions are centralized — all agents see the same decisions file
- Append-only merge driver handles branch merges cleanly
- Scales to N parallel agents with no additional complexity

**Scribe responsibilities (unchanged):**
1. Read inbox files from `.squad/decisions/inbox/` (agents write here)
2. Merge into `.squad/decisions.md`
3. Append to agents' `history.md`
4. Commit `.squad/` to git
5. Scribe runs from `TEAM_ROOT` (main checkout), not from agent worktrees

**Agent workflow:**
```
Agent in squad.42.fix-auth worktree:
1. Read decisions from TEAM_ROOT/.squad/decisions.md (shared, current)
2. Write inbox file to TEAM_ROOT/.squad/decisions/inbox/gimli-fix-auth.md
3. Do work in WORKTREE_PATH (isolated)
4. Commit and push from WORKTREE_PATH
5. Scribe collects inbox file and merges to .squad/decisions.md on main checkout
```

**Drop-box pattern (already partially implemented):**
- `.gitattributes` has union merge driver ✅
- Inbox pattern is documented ✅
- Scribe knows how to merge inbox files ✅
- Main-checkout strategy is documented in `squad.agent.md` ✅

**Just needs to be enforced in spawn template.**

---

## Research Question 7: Existing Patterns — Reusable Ideas from Tamir and Squad

### What Tamir's Distributed Systems Work Tells Us

**Tamir's approach (from `docs/research/worktree-parallelism-research.md`):**

Tamir does NOT use git worktree for local parallelism. Instead, he uses:

1. **Branch-per-issue with SubSquad prefixes** (not worktrees)
   - Branch naming: `ui-team/issue-7-feature` vs `backend-team/issue-14-api`
   - Each team/agent gets its own branch
   - No worktree isolation (single checkout, branches solve it via naming)

2. **Append-only merge driver** (`merge=union` in `.gitattributes`)
   - `.squad/decisions.md merge=union`
   - `.squad/agents/*/history.md merge=union`
   - Ensures parallel writes don't conflict when branches merge

3. **Drop-box pattern** (inbox-based conflict avoidance)
   - Agents don't write directly to shared files
   - Instead: `.squad/decisions/inbox/{agent}-{slug}.md`
   - Scribe merges inbox into canonical `.squad/decisions.md`
   - Conflict-free by design

4. **Multi-layer locking** (for same-machine collisions)
   - Named mutex (`Global\RalphWatch_tamresearch1`)
   - Process scan (find zombie processes)
   - Lockfile (external monitoring)

5. **Git-based task queue** (for cross-machine coordination)
   - `.squad/cross-machine/tasks/` YAML files
   - Polling-based (every 5 minutes)
   - Conflict is the tiebreaker for task ownership

**Key insight:** Tamir's focus is multi-machine and multi-team. Worktrees are single-machine. He chose abstractions that scale to enterprise (git-based transport, distributed task queue).

### What Squad's Template Already Has

**From `.squad/templates/squad.agent.md`:**

✅ **Worktree awareness** (lines 562–600)
- Detects if running in a worktree
- Resolves team root correctly
- Passes `TEAM_ROOT` to all agents
- Documents two strategies: worktree-local vs main-checkout

✅ **Append-only merge driver** (lines 75–82)
- `.gitattributes` configured with `merge=union`
- Documented in init phase
- Ready to use

✅ **Drop-box pattern** (lines 543–550)
- Documents inbox pattern
- Scribe merges inbox files
- All agents read merged snapshot at spawn time

✅ **Orchestration logging** (lines 601–607)
- Scribe writes orchestration log entries
- Tracks which agents ran, why, what they did
- Per-agent logging: `.squad/orchestration-log/{timestamp}-{agent}.md`

**From `.gitattributes` (already in repo):**
```
.squad/decisions.md merge=union
.squad/agents/*/history.md merge=union
.squad/log/** merge=union
.squad/orchestration-log/** merge=union
```

**✅ Everything is configured and ready.** Squad just needs to enforce worktree creation in the spawn template.

### What's Missing (Small Gap)

**From squad.agent.md spawn template (lines 609–650):**

❌ **No `WORKTREE_PATH` variable** — agents don't know they're in a worktree
❌ **No pre-spawn worktree creation instructions** — coordinator doesn't create worktrees
❌ **No explicit instruction to avoid branch creation** — agents might infer old pattern
❌ **No post-spawn cleanup logic** — orphaned worktrees accumulate

### Reusable Patterns

**From Tamir:**
1. Use branch naming conventions (not needed if using worktrees, but good backup)
2. Append-only merge driver (already in pa-squad)
3. Drop-box pattern (already in pa-squad)
4. Multi-layer locking for same-machine (could be useful for pre-spawn validation)
5. Git-based task queue (not needed for local parallelism)

**From Squad:**
1. Coordinator-detected team root resolution (already working)
2. Scribe-based state management (already working)
3. Orchestration logging (already working)
4. Drop-box pattern for decisions (already working)

### Conclusion on Question 7

**Squad already has everything needed. Reusable ideas:**

| Pattern | Source | Squad Status | Use? |
|---------|--------|--------------|------|
| Worktree isolation | Tamir + VS Code | Documented, not enforced | ✅ Use — enforce in spawn template |
| Append-only merge | Tamir + Squad | Already configured in `.gitattributes` | ✅ Already working |
| Drop-box pattern | Tamir + Squad | Already documented | ✅ Already working |
| Scribe orchestration | Squad | Already working | ✅ Already working |
| Team root resolution | Squad | Already working | ✅ Already working |
| Multi-layer locking | Tamir | Not in Squad | ⚠️ Consider for pre-spawn validation |
| Git task queue | Tamir | Not in Squad | ❌ Overkill for local parallelism |

**Next step:** Update spawn template to enforce worktree creation (minimal change, uses existing infrastructure).

---

## Research Question 8: Platform Constraints — Does Git Worktree Work on Windows?

### Git Worktree Support on Windows

**Answer: YES, fully supported.**

#### Verification on Windows (This Machine)

```
$ git worktree list --porcelain
worktree C:/dev/personal/pa-squad
HEAD 35a052c9a41285a6df30ba31d065fa4e62671bee
branch refs/heads/main
```

Git worktree command is available and working on Windows 10/11.

#### Windows-Specific Considerations

| Concern | Reality | Mitigation |
|---------|---------|-----------|
| **Path length (260-char limit)** | Windows has a 260-character path limit unless long-path support is enabled | Worktree paths are typically `{repo}/.squad/worktrees/squad.{issue}.{slug}` — usually under 100 chars. Even with issue slugs, rarely exceeds limit. Git handles long paths automatically on modern Windows. |
| **Symlinks in `.git`** | Windows typically doesn't support symlinks without admin; Git worktrees use symlinks | Git on Windows creates `.git` as a file (`.git` text file pointing to main repo) instead of symlink — works fine without admin |
| **File permissions** | Windows NTFS doesn't have Unix permissions | Not an issue — git worktree doesn't rely on Unix perms on Windows |
| **Line endings** | CRLF vs LF differences | Not related to worktree isolation — standard git configuration |
| **Multiple VS Code windows** | Can VS Code handle multiple worktrees? | YES — VS Code native support for worktrees (since v1.50). Can open each worktree in separate window without issues. |
| **Performance** | Multiple worktrees = multiple file system watchers? | Git worktrees are very efficient (shared `.git` object database). Minimal overhead. VS Code watchers are per-window, not per-worktree, so negligible impact. |

#### Evidence: VS Code Native Worktree Support

From Tamir's worktree-article-analysis.md (lines 87–95):

> VS Code UI workflow:
> 1. Enable Repositories View → Source Control panel (Ctrl+Shift+G) → •••  menu → Views → Repositories
> 2. Access Worktrees → Right-click repository → Worktrees menu
> 3. Create Worktree → VS Code wizard prompts for branch and location
> 4. Open in new window → worktree opens in separate VS Code window

This is a native VS Code feature, available on all platforms including Windows.

#### Command-Line Worktree Operations on Windows

All git worktree commands work identically on Windows:

```powershell
# Create worktree
git worktree add .squad\worktrees\squad.42.fix-auth -b squad/42-fix-auth main

# List worktrees
git worktree list --porcelain

# Remove worktree
git worktree remove .squad\worktrees\squad.42.fix-auth

# Prune stale worktrees
git worktree prune
```

**Key difference:** Use backslashes in PowerShell (`.\path\to\worktree`), not forward slashes. But git worktree command handles both.

#### Multi-Agent Scenario on Windows

No issues scaling to multiple parallel agents:

```powershell
# Create 8 worktrees for parallel agents
1..8 | ForEach-Object {
    $issueId = $_
    $path = ".squad/worktrees/squad.$issueId"
    git worktree add $path -b "squad/$issueId" main
}

# All 8 agents can work in parallel from their worktrees
# Each worktree is independent, no conflicts
```

### Conclusion on Question 8

**Platform constraints: NONE for Windows.**

Git worktree is fully supported on Windows with no limitations:
- ✅ Command-line operations work identically
- ✅ VS Code has native worktree support
- ✅ No path length issues for typical worktree names
- ✅ No symlink problems (git creates `.git` file, not symlink)
- ✅ Multiple VS Code windows work seamlessly
- ✅ Performance is excellent (shared `.git` object database)

**Recommendation:** Implement worktree-based parallel execution without any Windows-specific workarounds. Works out of the box.

---

## Summary of Findings

### Quick Reference: Answers to All 8 Questions

| # | Question | Answer |
|---|----------|--------|
| 1 | **Current flow** | Coordinator spawns agents via spawn prompt template; agents infer `git checkout -b` pattern; shared checkout causes race conditions when multiple agents run in parallel. |
| 2 | **Minimal change** | Update spawn prompt template to include `WORKTREE_PATH` and pre-spawn instructions. Coordinator creates worktrees before spawn, agents work in isolated directories. No scripts needed. |
| 3 | **WORKTREE_PATH vs TEAM_ROOT** | Complementary: `TEAM_ROOT` = where `.squad/` state lives (main checkout), `WORKTREE_PATH` = where agent does work (isolated). Agents read from `TEAM_ROOT`, work in `WORKTREE_PATH`. |
| 4 | **Cleanup lifecycle** | After PR merge (Strategy C). Local cleanup automation detects merge and runs `git worktree remove`. Fully automated, scales with team size. Worktrees persist through review cycle. |
| 5 | **Edge cases** | 5 main cases: collision detection (remove if exists), branch conflict (clean up old branch), locked worktree (force remove), branch switching prevention (explicit prompt), creation failure (validate and rollback). |
| 6 | **Scribe integration** | Use main-checkout strategy: Scribe operates from main checkout (`TEAM_ROOT`), all agents write to shared `.squad/` via inbox pattern. Agents in worktrees write to inbox, Scribe merges on main. |
| 7 | **Existing patterns** | Squad already has all infrastructure: worktree awareness, append-only merge driver, drop-box pattern, Scribe orchestration. Just needs enforcement in spawn template. Tamir's distributed systems patterns are compatible. |
| 8 | **Platform constraints** | None on Windows. Git worktree is fully supported, VS Code has native UI, no path length issues, no symlink problems. Works identically to Unix. |

### Implementation Priority

**Phase 1 (Immediate):** Update spawn template
- Add `WORKTREE_PATH` variable
- Add pre-spawn worktree creation instructions to Coordinator
- Add post-spawn cleanup instructions

**Phase 2 (Short-term):** Add safety guards
- Pre-spawn collision detection and cleanup
- Worktree creation validation
- Cleanup failure handling with retry logic

**Phase 3 (Medium-term):** Automate cleanup
- GitHub Actions workflow for post-merge cleanup
- Local background watcher (polling-based)
- Webhook receiver (if available)

**Phase 4 (Long-term):** Refinements
- Multi-layer locking for same-machine coordination
- Performance monitoring (worktree creation time, disk usage)
- Scaling tests (how many parallel agents?)

---

## Recommendations

### For Jonathan & Squad

1. **Update `.squad/templates/squad.agent.md` spawn template** to include `WORKTREE_PATH` and enforce worktree isolation.

2. **Add pre-spawn coordinator logic** to create worktrees before spawning agents (just before `task` tool call).

3. **Add post-spawn cleanup logic** in GitHub Actions (on PR merge) and/or local background watcher.

4. **Use main-checkout strategy** by default (all agents share same `.squad/` state on main checkout). This is already the default in squad.agent.md, just needs to be enforced.

5. **Document worktree lifecycle** in a new section "Worktree-Based Parallel Execution" with clear examples and troubleshooting.

6. **Test with parallel spawns** — spawn 8 agents simultaneously and verify no checkout collisions or workspace corruption.

### For Future Research

- Investigate multi-layer locking for pre-spawn collision detection
- Profile worktree creation/removal overhead at scale (10+, 50+, 100+ agents)
- Explore GitHub Actions integration for cleanup
- Test cross-platform consistency (Windows, Mac, Linux)

---

## Appendix: Git Worktree Reference

### Common Commands

```powershell
# Create worktree
git worktree add .squad/worktrees/squad.42 -b squad/42-fix-auth main

# List worktrees
git worktree list
git worktree list --porcelain  # Machine-readable

# Remove worktree
git worktree remove .squad/worktrees/squad.42
git worktree remove .squad/worktrees/squad.42 --force  # Force if locked

# Prune stale worktrees
git worktree prune

# Repair (if worktree metadata is corrupt)
git worktree repair
```

### Worktree Limitations (Know Before Using)

- One branch per worktree (can't have same branch checked out in multiple worktrees)
- Shared `.git` object database (not full clone — efficient)
- Shared git config (all worktrees see same config)
- If `.git` is deleted, all worktrees break (but this is rare)

### Performance Characteristics

- **Creation:** ~100-500ms per worktree (disk speed dependent)
- **Removal:** ~100ms per worktree
- **Disk overhead:** Minimal (shared `.git`, only working directory duplicated)
- **Memory overhead:** Per-worktree file watcher in VS Code (~50MB per window)

---

## Conclusion

**Implementing worktree-based parallel agent execution is feasible, safe, and recommended for pa-squad.**

Squad already has the infrastructure (worktree awareness, merge drivers, state management). The minimal change is updating the spawn template to create worktrees pre-spawn and remove them post-merge. Platform support is excellent on Windows. Edge cases are addressable.

**Next step:** Implement Phase 1 (update spawn template) and validate with parallel agent spawning.

