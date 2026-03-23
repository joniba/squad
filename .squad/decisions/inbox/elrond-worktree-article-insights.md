# Elrond's Worktree Article Insights — Key Findings
**From:** Elrond (Researcher)  
**Re:** Tamir Dresher's Git Worktrees Article Analysis  
**Date:** 2025  
**Status:** Inbox — Ready for Team Decision

---

## EXECUTIVE SUMMARY

**The Problem:** Jonathan's team spawned 8 agents in parallel. Each agent did `git checkout -b` in the same working tree. Later agents' branch switches clobbered earlier agents' uncommitted work. Local state was destroyed (4 stashes, random branch, uncommitted files), though PRs did push successfully.

**Root Cause:** Single checkout + concurrent branch switches = state destruction.

**Tamir's Solution:** Git worktrees. Each parallel task gets its own directory with its own branch. No checkout switches in the main working tree.

**Squad's Aspirational Gap:** squad.agent.md documents worktree support (lines 562-600), but the feature is **not activated** in practice. Agents still use the old single-checkout pattern.

**The Fix:** Coordinator should create isolated worktrees before spawning agents. Agents work only in their assigned worktree. No branch switching needed.

---

## KEY INSIGHT #1: Tamir's Article Is Manual, Not Automated

Tamir describes a **manual VS Code workflow:**
1. Open Repositories view
2. Click UI to create worktree
3. Open worktree in new window
4. Launch AI agent manually in that window
5. Alt+Tab to supervise multiple windows

**This is NOT how Squad should work.** Squad agents are spawned programmatically. The coordinator needs to **automate** the worktree creation step before spawning agents.

---

## KEY INSIGHT #2: Squad's Worktree Documentation Is Designed but Dormant

**Lines 562-600 of squad.agent.md describe two strategies:**
1. `worktree-local` — Each worktree has isolated `.squad/` state (recommended for concurrent work)
2. `main-checkout` — All worktrees share `.squad/` state from main (not safe for concurrent sessions)

**The logic is documented in detail:**
- How to detect you're in a worktree
- How to resolve the team root
- How to handle state merges with `merge=union` driver in `.gitattributes`

**But it's never enforced.** Agents don't know they're in a worktree. The coordinator doesn't create worktrees before spawning. It's a design that was planned but not activated.

---

## KEY INSIGHT #3: The Fix Is Straightforward — Two Options

### Option A: Coordinator-Managed Worktrees (Recommended)

**Pre-spawn (Coordinator does this before spawning agents):**
```bash
git worktree add ./worktrees/squad.issue-1 -b squad/issue-1 main
git worktree add ./worktrees/squad.issue-2 -b squad/issue-2 main
...
```

**Agent Spawn Instructions:**
- Pass `WORKTREE_PATH` to each agent
- Instruct: "Work only in {WORKTREE_PATH}. Do NOT switch branches."
- Agents commit and push from that worktree

**Post-agent (Coordinator cleanup):**
```bash
git worktree remove ./worktrees/squad.issue-1
git worktree remove ./worktrees/squad.issue-2
...
```

**Advantages:**
- Deterministic setup/cleanup
- Agents are simpler (no worktree lifecycle logic)
- Coordinator has full visibility
- Scales to N agents easily

**Implementation effort:** ~200 lines in coordinator spawn logic

### Option B: Agent-Managed Worktrees

**Agents create their own worktrees:**
```bash
git worktree add {TEAM_ROOT}/worktrees/squad.{issue-id} -b squad/{issue-slug} main
# ... work ...
git worktree remove {TEAM_ROOT}/worktrees/squad.{issue-id}
```

**Advantages:**
- Agents are self-contained
- No pre-coordination needed
- Works for async/unplanned agents

**Disadvantages:**
- More complex agent logic
- Harder to debug if worktree creation fails
- Less visibility into parallel execution

---

## KEY INSIGHT #4: Why Jonathan's Problem Happened (And How It's Fixed)

**What Broke:**
```
Agent 1: git checkout -b squad/issue-1  ← Creates branch, works
Agent 2: git checkout -b squad/issue-2  ← Switches to new branch, clobbers Agent 1's uncommitted files
Agent 3-8: Each checkout destroys previous agent's state
Result: Workspace left on random branch with stashes and confusion
```

**How Worktrees Fix It:**
```
Coordinator: git worktree add worktrees/squad.issue-1 -b squad/issue-1 main
Coordinator: git worktree add worktrees/squad.issue-2 -b squad/issue-2 main
Coordinator: spawn Agent 1 in worktrees/squad.issue-1/
Coordinator: spawn Agent 2 in worktrees/squad.issue-2/

Agent 1: Works in worktrees/squad.issue-1/ — no branch switching needed
Agent 2: Works in worktrees/squad.issue-2/ — completely isolated
Agent 3-8: Each in their own worktree directory

Result: Clean parallel execution, no state collisions
```

---

## KEY INSIGHT #5: The Implementation Checklist

To activate worktree support in Squad (moving from aspirational to operational):

- [ ] **Coordinator:** Add pre-spawn worktree creation logic (Option A recommended)
- [ ] **Agent Spawn Prompt:** Pass `WORKTREE_PATH` and instruct agents not to switch branches
- [ ] **Coordinator:** Add post-agent worktree cleanup (after PR merged/abandoned)
- [ ] **Documentation:** Update squad.agent.md "Worktree Awareness" section from "designed" to "ACTIVE"
- [ ] **Error Handling:** Log failures if worktree creation or removal fails
- [ ] **Troubleshooting:** Document worktree lifecycle and common failures
- [ ] **Testing:** Run 8 agents in parallel to verify no state collisions

---

## KEY INSIGHT #6: Tamir's Core Contribution (For Squad's Adaptation)

Tamir's article proves that **worktrees are the right pattern for parallel AI work**. The approach scales naturally:
- 1 worktree = manual setup in VS Code
- 8 worktrees = coordinator batch setup
- 80 worktrees = same coordinator logic, just parameterized

The manual UI Tamir describes is just the **visible version** of what Squad can automate at the coordinator level.

---

## OPEN QUESTIONS FOR THE TEAM

1. **Which strategy should Squad use?** Option A (coordinator-managed) or Option B (agent-managed)?
   - Recommendation: Option A. Cleaner, more deterministic, less burden on agents.

2. **Should we activate `worktree-local` strategy immediately, or start with `main-checkout`?**
   - Recommendation: `worktree-local` is recommended by squad.agent.md and aligns with Tamir's approach. It's safe for concurrent work if each agent is isolated by worktree.

3. **How urgent is this?** Jonathan's team already pushed successful PRs—the issue was local state cleanup, not correctness.
   - Recommendation: Implement this before the next parallel agent spawning event to prevent repeated state confusion.

4. **Should agents be told they're in a worktree, or should they just work as normal?**
   - Recommendation: Agents should be told (`WORKTREE_PATH` in spawn prompt). Keeps agents honest about their working directory and makes debugging easier.

---

## NEXT STEPS

1. **Team decision:** Which approach (A or B)?
2. **Design review:** Coordinator pre-spawn and post-agent cleanup logic
3. **Agent spawn prompt update:** Include `WORKTREE_PATH` and worktree instructions
4. **Testing:** Parallel agent test with 8 agents to verify no state collisions
5. **Documentation update:** Mark squad.agent.md Worktree Awareness as "ACTIVE—Enforced by Coordinator"

---

## APPENDIX: Tamir's Exact Procedure (From Article)

**Phase 1:** Enable Repositories View in VS Code Source Control
**Phase 2:** Access Worktrees submenu from repository context menu
**Phase 3:** Create worktree (wizard: select branch, choose location, open in new window)
**Phase 4:** Repeat for each feature
**Phase 5:** Launch AI agent in each window
**Phase 6:** Alt+Tab between windows to supervise

**Key difference from Squad:** Tamir is manual. Squad should automate phases 1-3 at the coordinator level, leaving phases 5-6 for human supervision (or fully automated if desired).
