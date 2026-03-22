# Research Report: Tamir's Worktree/Parallelism Insights & Squad's Gap

**Date:** 2026-03-22  
**Researcher:** Elrond  
**Requested by:** Jonathan  
**Project:** pa-squad

---

## Executive Summary

Tamir Dresher's four-part blog series on scaling AI-native software engineering documents real distributed systems problems in agent teams—but notably, **`git worktree` is mentioned only once as a deployment target**, not as the primary solution to parallel branch management. Instead, Tamir built a sophisticated coordination layer using:

1. **Drop-box pattern** (inbox-based file merging with `merge=union`)
2. **Multi-layer locking** (named mutex + process scan + lockfile)
3. **Distributed task queues** (git-based, polling-driven)
4. **Cross-machine coordination** (task files, machine aliases, config whitelist)

Squad's governance template (`squad.agent.md`) documents worktree awareness and two strategies (**worktree-local** vs **main-checkout**), but **neither strategy is enforced in the agent spawn flow**. Jonathan's team hit a collision problem TODAY because:

- 8 parallel agents were spawned to the same working tree
- Each ran `git checkout -b squad/{issue}-{slug}` (a race condition)
- Branch state overwrote between agent tasks
- Workspace landed on `squad/4-format-summary` with stashes, uncommitted files, and no main branch restore

**The gap:** Squad documents the problem and solutions but doesn't bake worktree isolation into the agent spawn template. This is **not a squad framework bug**—it's a configuration decision that needs to be enforced per-project during squad initialization.

---

## Part 1: Tamir's Distributed Systems Insights

### 1.1 The Eight Ralphs Problem

Tamir's Part 4 blog post ("When Eight Ralphs Fight Over One Login") documents real collisions when running parallel agents across machines. The title refers to a Star Trek reference—multiple instances of the same agent (Ralph, the monitor) running simultaneously, competing for shared resources.

**Specific problems Tamir encountered:**

1. **Single-machine mutex collisions** — Multiple Ralph instances (one per machine) trying to run the same monitoring task concurrently, causing duplicate work or deadlocks
2. **State file write races** — Four agents finishing simultaneously, each trying to commit to `.squad/decisions.md`, resulting in git merge conflicts
3. **Distributed task ownership** — Cross-machine tasks were created but had no mechanism to prevent two machines from claiming the same task
4. **GitHub API rate limit thrashing** — 8 Ralphs × 12 rounds/hour × ~600 API calls per round = resource exhaustion
5. **Prompt serialization failures** — 7KB prompts passed through PowerShell's `Start-Process` were treated as command names, not arguments, causing 5 of 8 Ralphs to fail every round

### 1.2 Tamir's Solutions (Not Git Worktree)

#### Solution 1: Multi-Layer Locking (Named Mutex + Process Scan + Lockfile)

Tamir's `ralph-watch.ps1` (lines 35–71) uses **three complementary mechanisms**:

```powershell
$mutexName = "Global\RalphWatch_tamresearch1"
$mutex = New-Object System.Threading.Mutex($false, $mutexName)
$acquired = $mutex.WaitOne(0)  # Non-blocking attempt
if (-not $acquired) {
    Write-Host "Another Ralph is already running" -ForegroundColor Red
    exit 1
}
```

This prevents duplicate Ralphs on the same machine. But if Ralph crashes ungracefully:

1. **Process scan** — finds zombie Ralphs via `Get-CimInstance Win32_Process`
2. **Lockfile** — external tools can read status; cleaned up via `Register-EngineEvent PowerShell.Exiting`
3. **Distributed systems lesson** — "Lock files without health checks are lies." Real leader election (Chubby, ZooKeeper, etcd) requires liveness verification, not just mutex acquisition.

#### Solution 2: Drop-Box Pattern (Inbox Merging)

When multiple agents write decisions simultaneously:

**Old approach (BROKEN):** All agents write directly to `.squad/decisions.md` → merge conflicts

**New approach (WORKING):** 
- Agents write to individual inbox files: `.squad/decisions/inbox/{agent-name}-{slug}.md`
- Scribe (coordinator) merges inbox into canonical `.squad/decisions.md` after all agents finish
- All agents READ the merged snapshot at spawn time

This follows **CRDT semantics** — G-Sets (grow-only sets) and append-only logs are conflict-free replication.

#### Solution 3: Append-Only Merge Driver

`.gitattributes` enables seamless merging of parallel work:

```
.squad/decisions.md merge=union
.squad/agents/*/history.md merge=union
.squad/log/** merge=union
.squad/orchestration-log/** merge=union
```

`merge=union` concatenates both sides (no conflicts) because these files are append-only. When two agents write decisions on different branches, merging keeps all lines from both sides.

#### Solution 4: Distributed Task Queue (Git-Based)

Multi-machine coordination uses **git as the transport**:

- `.squad/cross-machine/tasks/` — YAML files defining work (id, source_machine, target_machine, command, status)
- `.squad/cross-machine/config.json` — per-machine config with command whitelist and machine aliases
- `scripts/cross-machine-watcher.ps1` — polls every 5 minutes, pulls tasks, validates against whitelist, executes, pushes results
- Git merge conflicts are the tiebreaker for task ownership (first machine to push wins)

**Key insight:** Task YAML itself is the lock. Ralph claims a task by updating `status: pending` → `status: executing` (machine name), then pushes. Racing Ralphs hit git merge conflict; loser retries.

### 1.3 Does Tamir Mention Git Worktree?

**Answer:** Yes, once—as a deployment target, not as the parallel branching solution.

Tamir discusses **SubSquads** (not worktrees) for multi-team parallel work:

- Each SubSquad has a `labelFilter` (which issues to pick up)
- Each has a `folderScope` (advisory directory scoping)
- Branch names get **SubSquad prefixes**: `ui-team/issue-7-game-board` vs `backend-team/issue-14-state-sync`

**Worktree is mentioned only here:** Tamir ran the squad-tetris experiment in "three GitHub Codespaces, each with its own devcontainer" — these are three separate machines (not worktrees), each running its own Squad instance on the same repo.

### 1.4 Tamir's Actual Approach to Parallel Branch Work

**Tamir does NOT use `git worktree` to solve the "agents sharing one working tree" problem.**

Instead:

1. **SubSquad-based isolation** — Each team/agent gets its own branch with a prefixed name
2. **Append-only merge driver** — When branches merge, state files combine conflict-free
3. **Drop-box pattern** — Agents don't race on shared files; instead, they drop work in isolation zones
4. **Liveness-checked locks** — Named mutex + process scan for same-machine collisions
5. **Git-based task queue** — Cross-machine coordination with polling and conflict-based ownership

**Why not worktree?** Tamir's focus is multi-machine and cross-team scenarios. Worktrees are single-machine. Tamir chose abstractions that scale to enterprise (multiple repos, multiple machines, multiple teams). The pattern is: **branch-per-issue with isolation + append-only state + eventual consistency via git merge**.

---

## Part 2: Squad's Current State

### 2.1 What squad.agent.md Actually Says About Worktrees

The "Worktree Awareness" section (lines 562–600) documents two explicit strategies:

| Strategy | Team Root | State Scope | When to Use |
|----------|-----------|-------------|-----------|
| **worktree-local** | Current worktree root | Branch-local — each worktree has its own `.squad/` state | Feature branches that need isolated decisions and history |
| **main-checkout** | Main working tree root | Shared — all worktrees read/write the main checkout's `.squad/` | Single source of truth for memories, decisions, and logs across all branches |

**How Squad resolves the team root (coordinator logic, every session start):**

1. Run `git rev-parse --show-toplevel` to get current worktree root
2. Check if `.squad/` exists at that root
   - **Yes** → use **worktree-local** strategy
   - **No** → use **main-checkout** strategy
     - Run `git worktree list --porcelain` to find the main working tree
     - Use that path as team root
3. User may override the strategy at any time

**Passing team root to agents:**
- Coordinator includes `TEAM_ROOT: {resolved_path}` in every spawn prompt
- Agents resolve ALL `.squad/` paths from the provided team root
- Agents never discover the team root themselves

### 2.2 Is Git Worktree Usage Enforced, Recommended, or Just Documented?

**Status: Just documented, NOT enforced.**

The `.squad/templates/squad.agent.md` file explicitly states this is **awareness** guidance for Coordinators—the human or automated orchestrator spawning agents. It does NOT:

- Enforce worktree creation at agent spawn time
- Require agents to create their own worktree before checking out a branch
- Validate that agents are running in isolated worktrees
- Prevent multiple agents from running in the same tree

The documentation is **advisory only**. A Coordinator could spawn 8 agents to the same worktree and Squad would not intervene.

### 2.3 Merge Driver: Present and Correct

`.gitattributes` in pa-squad already has the union merge driver configured:

```
.squad/decisions.md merge=union
.squad/agents/*/history.md merge=union
.squad/log/** merge=union
.squad/orchestration-log/** merge=union
```

This is **properly set up**, following Tamir's pattern exactly. The drop-box pattern (decisions/inbox) is NOT yet implemented in pa-squad, but the foundation is there.

---

## Part 3: The Problem Jonathan's Team Hit

### 3.1 What Happened

**Time:** Today (2026-03-22)  
**Context:** 8 parallel agents spawned (Gimli, Elrond, Bilbo, Gandalf, Aragorn, etc.)

**Sequence:**

1. All 8 agents ran in the **same working tree** (no worktree isolation)
2. Each agent's instructions included: "Check out a new branch for this issue"
3. Each ran `git checkout -b squad/{issue}-{slug}` in sequence/parallel
4. **Race condition:** By the time agent #2 executed checkout, agent #1 had already changed the branch state
5. Agent #3 checked out while #2 was mid-operation, causing all previous work to be overwritten
6. Final state: workspace landed on `squad/4-format-summary`
7. **Artifacts left behind:**
   - 4 git stashes (one per agent that didn't push)
   - Uncommitted files scattered: `package.json`, `.vscode/settings.json`, `scripts/`
   - Main branch never restored
   - **BUT:** Work DID land on GitHub (PRs pushed successfully)

### 3.2 Why the Local Workspace Trashed But GitHub Work Survived

**Local workspace:** Each agent operated on the same tree, overwriting branches and leaving stashes.

**GitHub:** Each agent pushed its PR to a remote branch before the next agent checked out. Git push to remote is atomic and doesn't depend on the local working tree state. So:

- Agent A: `git checkout squad/1-issue && git commit && git push origin squad/1-issue` ✅
- Agent B: `git checkout squad/2-issue` (overwrites A's tree) && `git commit && git push origin squad/2-issue` ✅
- ...
- Agent 8: workspace is trash, but all 8 PRs landed on GitHub

**The lesson:** Remote operations (push) are resilient because git treats them atomically. Local operations (checkout) race because they share a single working tree state.

### 3.3 Symptoms vs Root Cause

**Symptoms:**
- Workspace on wrong branch
- Stashed work
- Uncommitted files
- Confusion about what's checked out

**Root cause:**
- 8 agents spawned without worktree isolation
- No guard in the spawn flow preventing this scenario
- squad.agent.md documents the *option* to use worktree-local but doesn't require it

---

## Part 4: Gap Analysis & Recommendations

### 4.1 Why Isn't Git Worktree Usage Baked Into the Agent Spawn Flow?

**Historical reason:** Squad's spawn template predates the multi-agent parallel work pattern. The template was designed for sequential agent work or loosely-coordinated background tasks. When only one or two agents run at a time, shared working tree races don't happen.

**Design choice (defensible):** Squad is framework-level, not project-level. Some projects may prefer the simpler **main-checkout** strategy for solo development. Squad documents both, lets Coordinators choose, doesn't mandate.

**The miss:** There is NO **coordinator-level guard** that prevents spawning multiple agents to the same tree. The template lacks:

1. **Pre-spawn validation** — "Are N agents already running in this tree?"
2. **Auto-worktree creation** — "This agent should run in its own worktree: `git worktree add squad/{issue}-{slug} main`"
3. **Post-spawn cleanup** — "Remove the worktree when done: `git worktree remove {path}`"

### 4.2 What a Proper Worktree-Based Parallel Workflow Looks Like

#### Pattern A: Worktree-Per-Agent (Recommended for High Parallelism)

```bash
# Coordinator decides: spawn agent for issue #42
# Before spawn: create a worktree for this agent
git worktree add .squad/worktrees/squad-42-format-summary main

# Pass to agent:
# TEAM_ROOT: /repo-root
# AGENT_WORKTREE: /repo-root/.squad/worktrees/squad-42-format-summary
# AGENT_ISSUE: 42

# Agent runs in AGENT_WORKTREE directory:
cd $AGENT_WORKTREE
git checkout -b squad/42-format-summary  # Safe: this worktree is isolated
# ... do work ...
git commit -m "feat: format summary (#42)"
git push origin squad/42-format-summary

# Post-spawn cleanup (after agent finishes):
cd $TEAM_ROOT
git worktree remove .squad/worktrees/squad-42-format-summary
```

**Advantages:**
- Zero race conditions on branch checkout
- Each agent's working tree is isolated on disk
- `.squad/` state can be branch-local (worktree-local strategy)
- Cleanup is explicit and fast

**Disadvantages:**
- O(N) disk usage for N parallel agents (worktrees clone the git object database)
- Setup/teardown overhead (~500ms per worktree)

#### Pattern B: Stash-Before-Switch (Lower Parallelism)

```bash
# Coordinator: before spawning agent #2, stash any uncommitted work
git stash push -m "agent-1-work"

# Agent #2 runs:
git checkout -b squad/2-...
# ... do work ...
git push origin squad/2-...

# Post-spawn cleanup:
git stash pop  # Restore agent #1's work
```

**Advantages:**
- Minimal disk overhead
- Simple bookkeeping

**Disadvantages:**
- Only works for 2–3 sequential agents
- Doesn't scale to parallel
- Still not thread-safe (stash pop can fail if conflicts)

#### Pattern C: Commit-Before-Switch (Journalistic)

```bash
# Before agent #2 checks out:
git commit -m "temp: work-in-progress for agent-1" --allow-empty

# Then checkout for agent #2:
git checkout -b squad/2-...

# Later: squash the temp commit into the real branch
```

**Advantages:**
- Preserves all intermediate work
- Auditable history

**Disadvantages:**
- Messy history
- Still not thread-safe

### 4.3 Recommended Changes to Squad

#### Change 1: Enforce Worktree-Local Strategy for Multi-Agent Spawns

In `.squad/templates/squad.agent.md`, add a new section "Parallel Agent Spawning":

```markdown
### Parallel Agent Spawning (When N > 1 agent runs concurrently)

When spawning multiple agents in the same session:

1. **Use worktree-local strategy exclusively.**
   - Set `STRATEGY: worktree-local` in spawn context
   - Coordinator creates a worktree per agent BEFORE spawn
   - Pass `AGENT_WORKTREE: {path}` in spawn prompt

2. **Worktree creation (Coordinator responsibility):**
   ```bash
   WORKTREE_NAME=squad-{issue}-{slug}
   WORKTREE_PATH=.squad/worktrees/$WORKTREE_NAME
   git worktree add $WORKTREE_PATH main
   ```

3. **Worktree cleanup (Post-spawn, after agent finishes):**
   ```bash
   git worktree remove .squad/worktrees/$WORKTREE_NAME
   ```

4. **Agent charter update** — Agents running in `AGENT_WORKTREE` must:
   - Resolve `.squad/` paths relative to `TEAM_ROOT` (not `AGENT_WORKTREE`)
   - Check out new branches only in `AGENT_WORKTREE`
   - Never modify the main checkout's working tree

5. **Append-only merge:** When multiple worktrees merge branches back to main,
   the union merge driver ensures `.squad/` files combine without conflicts.
```

#### Change 2: Add Drop-Box Pattern (Decisions/Inbox)

Currently, agents write decisions to `.squad/decisions.md` directly. With parallelism, this causes races. Implement:

```
.squad/decisions/inbox/{agent-name}-{slug}.md  # Agent writes here
.squad/decisions.md                              # Scribe merges inbox here
```

Agent instructions should include:

```markdown
- Write decisions to `.squad/decisions/inbox/{agent_name}-{issue_slug}.md`
- Scribe will merge your inbox entry into the canonical decisions.md
- All agents READ `.squad/decisions.md` at spawn time (merged snapshot)
```

#### Change 3: Add Orchestration Manifest Template

Coordinator passes a manifest to Scribe with spawn details:

```yaml
# .squad/orchestration-log/manifest-{session-id}.yaml
agents_spawned:
  - name: gimli
    issue: 42
    mode: background
    worktree: .squad/worktrees/squad-42-...
    spawned_at: "2026-03-22T14:30:00Z"
  - name: elrond
    issue: 43
    mode: background
    worktree: .squad/worktrees/squad-43-...
    spawned_at: "2026-03-22T14:30:00Z"
```

Scribe uses this to write orchestration log entries (already documented in `squad.agent.md`, line 552+).

#### Change 4: Update Squad Init Template

When initializing a squad, ask:

```
Q: Will this squad run multiple agents in parallel?
  - Yes (use worktree-local strategy, enforce isolation)
  - No (use main-checkout, simpler setup)
  - Auto-detect (spawn Coordinator as a trial to see)
```

Store the answer in `.squad/config.yml`:

```yaml
parallelism_strategy: worktree-local
max_concurrent_agents: 8
worktree_cleanup_on_finish: true
decision_inbox_pattern: .squad/decisions/inbox/{agent_name}-{slug}.md
```

### 4.4 Alternatives to Baking This In

**Option 1: Per-Project Wrapper**  
Create a wrapper script in pa-squad (e.g., `scripts/spawn-parallel-agents.ps1`) that:
- Takes N issues
- Creates N worktrees
- Spawns N agents
- Cleans up on finish

**Pros:** Zero changes to squad framework, encapsulates project-specific logic  
**Cons:** Rediscovered by every squad user, not shared

**Option 2: Squad Plugin**  
Create a plugin (`.squad/plugins/parallel-spawn.md`) with orchestration logic

**Pros:** Documented, reusable across projects  
**Cons:** Adds framework complexity

**Option 3: Scribe Feature (Recommended)**  
Have Scribe (the documentation/orchestration agent in Squad) automatically:
- Detect when N agents are spawned
- Create worktrees
- Pass AGENT_WORKTREE to each spawn
- Clean up on finish

**Pros:** Automatic, transparent to user, scalable  
**Cons:** Requires Scribe to have git scripting capability

### 4.5 Is This a Squad Framework Bug or a Per-Project Configuration Issue?

**Verdict: Per-project configuration issue with a framework-level gap.**

**Not a bug because:**
- Squad correctly documents both worktree strategies
- The framework doesn't prevent users from choosing main-checkout for solo work
- Append-only merge drivers are properly set up

**But a gap because:**
- Squad has no coordinator-level guard against multiple agents in one tree
- The spawn template doesn't mention worktree creation as a pre-spawn step
- New squads default to main-checkout without warning about parallelism risks

**Fix location:** 
- **Squad framework**: Add optional pre-spawn hooks for worktree creation
- **Squad template**: Add "Parallel Agent Spawning" section with clear guidance
- **This project (pa-squad)**: Configure `.squad/config.yml` with `parallelism_strategy: worktree-local` and implement the wrapper script

---

## Part 5: Tamir's Patterns vs Squad's Template

### Comparison Matrix

| Problem | Tamir's Solution | Squad's Template | pa-squad Gap |
|---------|------------------|------------------|--------------|
| Single-machine agent collisions | Named mutex + process scan + lockfile | (Not documented) | Implement ralph-watch.ps1 pattern or rely on platform (GitHub Actions) to run one instance per job |
| State file write races | Drop-box pattern + append-only merge | Documented: append-only merge with union driver | Inbox pattern not implemented; decisions.md might still race with many parallel agents |
| Distributed task ownership | Task YAML + polling + config whitelist | SubSquads (label filter, folder scope) | Not needed for single-machine; could adopt for cross-machine if scaling to multiple repos |
| Branch checkout collisions | SubSquad prefixes + append-only merge | Worktree-local strategy (documented, not enforced) | **TODAY'S PROBLEM:** No guard in spawn flow; agents don't auto-create worktrees |
| GitHub API rate limiting | Token bucket, exponential backoff, request coalescing | (Not documented) | Not a factor for 8 agents; would matter at 100+ scale |
| Prompt serialization failures | File-based task queue instead of command-line arguments | (Not relevant; uses task tool) | Copilot CLI uses proper argument passing; not a risk here |

### Key Insight: Tamir's Patterns Are CRDT-Based

All of Tamir's solutions follow **Conflict-free Replicated Data Types (CRDT)** principles:

1. **G-Sets (grow-only sets)** — decisions.md, history.md, logs are append-only; no deletes means no conflicts
2. **Last-write-wins (LWW)** — branch names have agent/SubSquad prefixes; two agents never overwrite the same branch
3. **Eventual consistency** — git merge is the consistency mechanism; branches eventually merge to main

Squad already uses CRDTs (append-only, union merge driver). **The missing piece is enforcement** — ensuring agents actually use this pattern instead of racing on shared state.

---

## Recommendations Summary

### Immediate (for pa-squad)

1. **Add `.squad/orchestration/spawn-parallel-agents.ps1`**
   - Takes list of issues
   - For each issue: `git worktree add .squad/worktrees/squad-{issue} main`
   - Spawns agents with `AGENT_WORKTREE` set
   - Post-spawn: removes worktrees

2. **Update `.squad/config.yml`**
   ```yaml
   parallelism_strategy: worktree-local
   max_concurrent_agents: 8
   ```

3. **Document in `.squad/decisions.md`**
   ```markdown
   ## Decision: Worktree-per-agent for parallel spawning
   - Date: 2026-03-22
   - Context: 8 parallel agents caused branch checkout collisions
   - Decision: Use worktree-local strategy; create worktree per agent at spawn time
   - Enforcement: spawn-parallel-agents.ps1 script (mandatory for N > 1)
   ```

### Short-term (for Squad framework)

1. **Update squad.agent.md "Parallel Agent Spawning" section** with:
   - Clear guidance on worktree-per-agent pattern
   - Pre-spawn and post-spawn checklist
   - Drop-box pattern for shared files

2. **Add `.squad/decisions/inbox/` to template**
   - Update agents to write to inbox, not directly to decisions.md
   - Update Scribe to merge inbox after batch

3. **Add parallel spawning validation**
   - Warn if N > 1 agents spawned without worktrees
   - Suggest `--parallel` flag that auto-creates worktrees

### Long-term (for Squad ecosystem)

1. **Orchestration plugin** — Reusable parallel spawning with worktree management
2. **Scribe feature** — Auto-detect multi-agent spawn, auto-create worktrees
3. **Observability** — Metrics on agent spawn patterns, worktree usage, branch conflicts

---

## Conclusion

Tamir Dresher solved the "multiple agents, one repo" problem with **CRDT-based patterns** (append-only logs, eventual consistency, conflict-free merge drivers) rather than git worktrees. Worktrees are a **deployment isolation mechanism** for his multi-machine setup, not the core solution.

Squad's template documents worktree awareness correctly but **doesn't enforce** it in the spawn flow. Jonathan's team hit a real collision when 8 agents raced on branch checkout—the expected outcome given no coordination.

**The fix is architectural:** Add pre-spawn worktree creation, implement inbox patterns for shared files, and update the spawn template to reflect parallel-agent patterns. This is **not a squad framework bug**—it's a missing integration point between coordination (Coordinator) and execution (agents).

The path forward is clear: adopt Tamir's CRDT principles systematically, make worktree-per-agent the default for multi-agent parallelism, and document the pattern so other squads don't rediscover this problem.

---

## References

- **Tamir Dresher's Blog Series**
  - Part 1: Resistance is Futile — Your First AI Engineering Team
  - Part 3: Unimatrix Zero — Many Teams, One Repo with SubSquads (worktree mention)
  - Part 4: When Eight Ralphs Fight Over One Login — Distributed Systems in AI Teams (mutex, locking, drop-box pattern, API rate limiting)

- **Squad Documentation**
  - `.squad/templates/squad.agent.md` — Worktree Awareness section (lines 562–600)
  - `.gitattributes` — Union merge driver (append-only files)

- **pa-squad State**
  - `.squad/decisions.md` — Active team decisions
  - `.squad/agents/elrond/history.md` — Researcher context

---

**Report compiled by:** Elrond, Researcher  
**For:** Jonathan, pa-squad Project Owner  
**Next step:** Review decision in `.squad/decisions/inbox/elrond-worktree-gap.md`
