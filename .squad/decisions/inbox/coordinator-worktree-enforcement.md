### 2026-03-23T23:35: Worktree enforcement moved from skill to governance

**By:** Jonathan Ben Ami (via Copilot Coordinator)
**Commit:** 7fa27e3 on main

---

## Problem

Agents spawned by the coordinator were running `git checkout -b` in the main checkout instead of using worktrees. This caused:

1. **Main checkout left on feature branches** — next session starts on wrong branch
2. **Cross-agent state destruction** — parallel agents clobber each other's uncommitted work
3. **Mixed commits on wrong branches** — Gandalf's #112 design commit ended up on Gimli's #110 branch

This happened despite having:
- A directive in `decisions.md`: "Every change must be associated with its own branch + worktree + PR + issue"
- A skill file at `.squad/skills/worktree-lifecycle/SKILL.md` with the full correct flow
- Scripts at `scripts/create-worktree.ps1`, `scripts/cleanup-worktree.ps1`, `scripts/cleanup-all-worktrees.ps1`
- 16 existing worktrees proving the pattern works

## Root Cause Analysis

The coordinator's spawn template in `squad.agent.md` — which is the ONLY thing a new session's coordinator reads before spawning — had zero worktree steps. The template is the path of least resistance. If it doesn't mention worktrees, they don't get used.

Skills are opt-in knowledge. The coordinator reads them "when relevant," but relevance detection itself is unreliable — especially on the first spawn of a new session when the coordinator hasn't loaded skill context yet.

**The failure mode:** Governance file says "spawn using this template" → template has no worktree steps → agent does `git checkout -b` → main checkout contaminated. The skill file documenting the correct flow was never consulted because it's not in the critical path.

## Design Decision

**Move worktree enforcement from opt-in skill to mandatory governance.**

Three surgical edits to `.github/agents/squad.agent.md`:

### Change 1: Worktree Gate (pre-spawn checklist)

Added before "How to Spawn an Agent" section. Four mandatory steps:
1. Verify main checkout is on `main` (switch back if not)
2. Create worktree via `create-worktree.ps1` or raw `git worktree add`
3. Pass `WORKTREE_PATH` in spawn prompt
4. Agents MUST NOT run `git checkout -b`

Explicit skip conditions: read-only queries, Scribe, tasks that don't touch git.

Fallback: if `create-worktree.ps1` doesn't exist, use raw git command.

### Change 2: Spawn Template — Worktree Isolation Block

Added to the main spawn template (the one every coordinator copies) between `TEAM ROOT` and the existing `Read .squad/...` lines:

```
### Worktree Isolation (CRITICAL)
WORKTREE_PATH: {worktree_path}
AGENT_BRANCH: squad/{issue}-{slug}
Rules:
1. cd into WORKTREE_PATH as FIRST action
2. Do NOT run git checkout or git checkout -b
3. READ .squad/ state from TEAM_ROOT
4. WRITE decision inbox to TEAM_ROOT
5. COMMIT and PUSH only from WORKTREE_PATH
```

Conditional: only included when worktree was created. Omitted for read-only/Scribe spawns.

### Change 3: Lightweight Spawn Template

Same worktree block added (compact form) for lightweight spawns that still create branches.

## Why This Should Work

1. **The template is the path of least resistance.** Every new session follows it. If the template says "create worktree first," the coordinator will create a worktree first. No discipline required.

2. **The gate is before the spawn, not after.** The coordinator must pass through the worktree gate before it can reach the spawn template. It can't skip it without skipping the entire spawn section.

3. **The agent gets explicit isolation rules.** The agent's prompt says "cd into WORKTREE_PATH" and "do NOT run git checkout." Even if the coordinator somehow skips the gate, the agent's prompt (if the block was included) prevents branch switching.

4. **Fallback exists.** If `create-worktree.ps1` is missing, the raw git command is documented inline. No dependency on the script existing.

## What Could Still Go Wrong

1. **Coordinator ignores the gate.** The gate is text in a prompt, not executable code. A sufficiently distracted coordinator could still skip it. Mitigation: the gate uses ⚠️ and "MANDATORY" markers, and is positioned as the FIRST thing in the "How to Spawn an Agent" section.

2. **Agent ignores WORKTREE_PATH.** The agent receives the isolation rules but doesn't cd into the worktree. Mitigation: "CRITICAL" marker, explicit "FIRST action" instruction, and "STOP and tell the Coordinator" if something seems wrong.

3. **Worktree already exists for the branch.** If a previous session created a worktree for the same issue and didn't clean up, `git worktree add` will fail. Mitigation: documented in the skill file's error handling table. The coordinator should `git worktree remove` first or reuse the existing worktree.

4. **Session starts on wrong branch (like this one).** If a previous session left the main checkout on a feature branch, the gate's step 1 catches it: "Verify main checkout is on main. If not, switch back." This is a repair step, not just a check.

5. **Read-only task that unexpectedly needs to commit.** If a task was classified as read-only (no worktree created) but the agent decides to write/commit, it'll commit to whatever branch main checkout is on. Mitigation: the gate says to verify main is on main, so worst case it commits to main (which is acceptable for non-code changes per decisions.md).

## Files Changed

- `.github/agents/squad.agent.md` — 36 lines added (3 locations)

## Files NOT Changed (and why)

- `.squad/skills/worktree-lifecycle/SKILL.md` — stays as-is. It's reference documentation for the full lifecycle (create → spawn → work → merge → cleanup). The governance file now enforces the critical path; the skill file documents the full flow.
- `scripts/create-worktree.ps1` — no changes needed. It already does the right thing.
- `.squad/decisions.md` — Scribe will merge this inbox entry.

## Status

- **Committed:** 7fa27e3 on main
- **Scope:** pa-squad repo only (this is a local squad.agent.md, not the upstream Squad product)
- **Next session test:** Start a new session, say "Ralph, go" or "Gimli, work on #101." Verify the coordinator creates a worktree before spawning.
