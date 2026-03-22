# Decision Needed: Enforce Worktree-Per-Agent for Parallel Spawning

**Author:** Elrond (Researcher)  
**Date:** 2026-03-22  
**Status:** Proposed  
**Severity:** High (blocking concurrent agent work)

## Context

8 agents spawned in parallel today (Gimli, Elrond, Bilbo, Gandalf, Aragorn, etc.), all to the same working tree. Each ran `git checkout -b squad/{issue}-{slug}`, causing race conditions. Result:

- Workspace left on wrong branch (`squad/4-format-summary`)
- 4 git stashes from failed agents
- Uncommitted files scattered everywhere
- Main branch never restored
- **BUT:** All PRs successfully pushed to GitHub (remote operations are atomic)

Squad's governance template (`squad.agent.md`) documents two worktree strategies (worktree-local vs main-checkout) **but does not enforce worktree creation in the agent spawn flow**. This is the gap.

## The Problem

**Root cause:** No coordinator-level guard prevents multiple agents from running in the same working tree.

**Current behavior:**
- Coordinator spawns agents
- Each agent runs `git checkout -b ...` on the shared tree
- Branches overwrite; stashes accumulate
- Workspace becomes unusable

**Why it happened:**
- Squad's spawn template predates multi-agent parallelism patterns
- Documentation is advisory only, not enforced
- No pre-spawn validation, no auto-worktree creation, no post-spawn cleanup

## Decision Required

**Should pa-squad enforce worktree-per-agent isolation for parallel agent spawns?**

### Option A: Enforce Worktree-Per-Agent (Recommended)

**Action:**
1. Create `.squad/orchestration/spawn-parallel-agents.ps1` that:
   - Takes list of GitHub issue numbers
   - For each: `git worktree add .squad/worktrees/squad-{issue}-{slug} main`
   - Spawns agents with `AGENT_WORKTREE: {path}` in context
   - Post-spawn: removes all worktrees

2. Update `.squad/config.yml`:
   ```yaml
   parallelism_strategy: worktree-local
   max_concurrent_agents: 8
   ```

3. Mandate in Gandalf's (Coordinator) charter:
   - When spawning N > 1 agent concurrently, ALWAYS use `spawn-parallel-agents.ps1`
   - Never spawn multiple agents to the same tree without worktrees

**Pros:**
- Zero race conditions on branch checkout
- Each agent gets isolated disk state
- `.squad/` state merges cleanly via union driver
- Scales to 8+ parallel agents

**Cons:**
- O(N) disk overhead (worktrees clone git object database; ~100MB each)
- ~500ms setup/teardown per worktree
- Not suitable for large scale (100+ agents) without optimization

### Option B: Stash-Before-Switch (Lower Parallelism)

**Action:**
1. Create `.squad/orchestration/spawn-sequential-agents.ps1` that:
   - Spawns agent #1
   - When agent #1 finishes, stash its work: `git stash push -m "agent-1"`
   - Spawn agent #2
   - When agent #2 finishes, pop stash: `git stash pop`

**Pros:**
- Minimal disk overhead
- Simple logic

**Cons:**
- Only works for 2–3 agents sequentially
- Not parallel
- Stash pop can fail if conflicts
- Doesn't solve today's problem (8 agents spawned at once)

### Option C: Do Nothing (Status Quo)

**Action:** Keep current behavior; rely on users to manually manage worktrees

**Pros:**
- No code changes
- Maximum flexibility

**Cons:**
- Problem repeats every time someone tries to parallelize
- Workspace pollution, confusion, wasted time
- Remote PRs succeed but local state is trash

## Recommendation

**Adopt Option A: Enforce Worktree-Per-Agent**

**Reasoning:**
1. Tamir's multi-machine experiments (squad-tetris) used worktree isolation (3 separate Codespaces, each with own repo clone)
2. Squad's template already documents worktree-local strategy; we're just enforcing it
3. Disk overhead is acceptable for typical parallelism (8 agents = ~800MB, one-time, cleaned up after)
4. O(500ms) worktree overhead is faster than a full git clone and insignificant vs agent execution time
5. The gap between documentation and enforcement is exactly where bugs hide

## Implementation Path

### Phase 1 (Immediate - This Session)

1. Create `.squad/orchestration/spawn-parallel-agents.ps1` with:
   - Pre-spawn worktree creation loop
   - Spawn manifest generation (for Scribe)
   - Post-spawn cleanup loop

2. Document in `.squad/decisions.md`:
   ```markdown
   ## Decision: Worktree-Per-Agent for Parallel Spawning
   - Adopted: 2026-03-22
   - Pattern: Each agent runs in .squad/worktrees/squad-{issue}-{slug}
   - Enforcement: spawn-parallel-agents.ps1 mandatory for N > 1
   - Cleanup: Automatic post-spawn
   ```

3. Update Gandalf's charter (`.squad/agents/gandalf/charter.md`):
   - Add: "When spawning N > 1 concurrent agents, use spawn-parallel-agents.ps1"

### Phase 2 (Short-term - Next Session)

1. Implement drop-box pattern for shared files:
   - Agents write decisions to `.squad/decisions/inbox/{agent}-{slug}.md`
   - Scribe merges inbox after batch → `.squad/decisions.md`

2. Update `.squad/config.yml` with:
   ```yaml
   parallelism_strategy: worktree-local
   max_concurrent_agents: 8
   decision_inbox_pattern: .squad/decisions/inbox/{agent}-{slug}.md
   ```

3. Document in README (if exists) or `.squad/README.md`:
   - "For parallel agent work, see `.squad/orchestration/spawn-parallel-agents.ps1`"

### Phase 3 (Framework - Future)

- Propose to Squad team: add "Parallel Agent Spawning" section to squad.agent.md template
- Create Scribe feature: auto-detect multi-agent spawn, auto-create worktrees
- Create orchestration plugin for reuse across squads

## Risk Assessment

**Implementation risk: Low**
- Pattern is well-documented (Tamir's work)
- Squad template already supports it
- Worktree setup/cleanup is standard git, no custom logic

**Adoption risk: Low**
- Mandatory only for multi-agent scenarios
- Solo agents (N=1) unaffected
- Backward compatible (main-checkout still option for sequential work)

**Operational risk: Medium**
- O(N) disk usage could be high for large N (mitigated by cleanup)
- Cross-platform worktree behavior differs (Windows vs Linux) — need testing
- Stale worktrees might accumulate if cleanup fails — need guard

## Acceptance Criteria

- [ ] spawn-parallel-agents.ps1 created and tested with 4 concurrent agents
- [ ] All 4 agents successfully check out branches without conflicts
- [ ] Workspace returns to main branch after spawn cleanup
- [ ] No stashes or uncommitted files left behind
- [ ] GitHub PRs merge cleanly with union merge driver
- [ ] Documentation added to `.squad/decisions.md` and Gandalf's charter
- [ ] Pattern tested end-to-end with real GitHub issues

## Open Questions

1. Should worktree cleanup be automatic or manual? (Recommend: automatic, via finally block in PowerShell)
2. Should agents be aware of their worktree path, or should it be transparent? (Recommend: transparent — they just see AGENT_WORKTREE in env)
3. Should we implement drop-box pattern (decisions/inbox) at the same time? (Recommend: yes, Phase 2)
4. Should this be enforced in squad.agent.md template globally? (Recommend: yes, but optional per-project)

## Decision

**Choose:** Option A: Enforce Worktree-Per-Agent

**Owner:** Gandalf (Coordinator) — responsible for enforcing spawn-parallel-agents.ps1 usage  
**Tracking:** Create GitHub issue #N on pa-squad: "Implement spawn-parallel-agents.ps1 worktree orchestration"

---

**Next:** Jonathan reviews, approves, and assigns implementation to Gimli (infrastructure) or another agent with scripting expertise.
