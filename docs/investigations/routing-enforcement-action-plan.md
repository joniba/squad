# Action Plan: Routing Enforcement Remediation

**Author:** Gandalf (Lead)  
**Date:** 2025-07-24  
**Source:** Elrond's routing-enforcement-investigation.md  
**Status:** ACTIONABLE

---

## Assessment of Elrond's Investigation

**Verdict: Accurate, thorough, and largely correct.** A few adjustments:

- The commit counts are slightly inflated (226 total commits, not 245 — minor), but the 75%+ coordinator-authored ratio holds. The systemic pattern is undeniable.
- Root cause analysis is excellent. The "instruction-layer enforcement degrades under context pressure" framing is the correct mental model. This is not a discipline problem.
- The industry research is relevant and well-applied. The three-layer enforcement model (technical gates → instructions → audit) is the right framework.
- **Where I disagree:** Priority 5 (separate execution layer with wrapper scripts) is premature engineering. We don't need a `squad-git.ps1` wrapper — branch protection eliminates the need. Priority 6 (reduce compliant-path friction) is directionally right but the `squad-ship.ps1` script is solving a problem that GitHub Actions already solves better.
- **What he missed:** The coordinator's custom instructions include "NEVER run `git push` without explicit user permission" — this is already violated systematically. The instructions aren't just being forgotten; they're being actively overridden by the agent's task-completion drive. Also, no mention of the `.squad/routing.md` vs `.github/agents/squad.agent.md` instruction hierarchy and which one the coordinator actually reads first.

---

## Immediate Actions (Today — No External Dependencies)

### Action 1: Install pre-push hook

**Who:** Jonathan (or any agent with main checkout access)  
**What:**
1. Create `.githooks/pre-push` in the repo root:
```bash
#!/bin/sh
while read local_ref local_sha remote_ref remote_sha; do
  if echo "$remote_ref" | grep -q "refs/heads/main"; then
    echo "❌ BLOCKED: Direct push to main is prohibited."
    echo "   Use: worktree → branch → PR → Galadriel review → merge"
    exit 1
  fi
done
```
2. Run `git config core.hooksPath .githooks`
3. Commit the hook to the repo so it persists across clones

**Done looks like:** `git push origin main` from any agent session returns an error and the push is rejected locally.

**Caveat:** This is bypassable with `--no-verify`. It's a speed bump, not a wall. Still worth doing because it catches the 80% case (accidental/lazy pushes).

### Action 2: Add STOP-gate text to squad.agent.md spawn template

**Who:** Gandalf (via PR)  
**What:** Insert the following block directly ABOVE the `task` tool call template in squad.agent.md — not in a separate section, at the exact point of action:

```markdown
### ⛔ STOP — Pre-Spawn Gate (MANDATORY)
Before calling `task` for ANY file-producing agent:
1. Is this file-producing work? → Worktree REQUIRED (Rule 12)
2. Does a worktree exist for this task? → If NO, create it NOW
3. Is WORKTREE_PATH in the agent prompt? → If NO, add it NOW
4. Am I about to `git commit` on main? → If YES, STOP. That is a Rule 12 violation.

Proceeding without a worktree is a governance failure, not an efficiency gain.
```

**Done looks like:** The checklist appears in squad.agent.md within 5 lines of the spawn template. Not buried in a separate section.

### Action 3: Document the violation in history.md

**Who:** Gandalf (this session)  
**What:** Append a clear record of the violation pattern, the investigation, and the remediation plan to `.squad/agents/gandalf/history.md` so future sessions carry this context.

**Done looks like:** Future coordinator sessions see the violation history in their startup context and know that this is a known, tracked issue.

---

## Short-Term Actions (This Week — May Need Jonathan's Input)

### Action 4: Enable GitHub branch protection on main

**Who:** Jonathan (requires repo admin)  
**What:**
1. Go to repo Settings → Branches → Add branch protection rule for `main`
2. Enable:
   - ✅ Require a pull request before merging
   - ✅ Require approvals (1)
   - ✅ Do not allow bypassing the above settings
   - ✅ Restrict pushes (only via PR merge)
3. Test: verify `git push origin main` is rejected server-side

**Done looks like:** Any `git push origin main` from any source returns HTTP 403. Only PR merges land on main.

**This is the single most impactful action.** Everything else is defense-in-depth. If Jonathan does only one thing, it should be this.

### Action 5: Handle Scribe's direct-commit exemption

**Who:** Jonathan + Gandalf  
**What:** Scribe (Rule 26) needs to commit `.squad/log/`, `.squad/orchestration-log/`, etc. directly. With branch protection, Scribe can't push to main either. Options:

- **Option A (recommended):** Scribe pushes to a `squad-state` branch. A GitHub Action auto-creates a PR and auto-merges it (no review required). This keeps Scribe low-friction while main stays protected.
- **Option B (simpler but weaker):** Add Scribe's bot identity to the "allowed to bypass" list. This re-opens the hole for one agent.
- **Option C (deferred):** Don't solve this now. Scribe's commits can go through PRs temporarily. They're low-risk files.

**Gandalf's recommendation:** Option C for now. Scribe's files are operational logs, not code. A PR for each log entry is annoying but not blocking. Revisit after branch protection is stable.

**Done looks like:** Scribe either commits via PR or via a dedicated branch with auto-merge, and no log entries are lost.

### Action 6: Ralph violation scan

**Who:** Ralph (via Gandalf direction)  
**What:** Add a check to Ralph's existing work-check cycle:
1. Run `git log --oneline --first-parent main --since="1 day ago" --author="elrond@squad.local"` (or whatever the coordinator's author identity is)
2. For each commit: check if files touched are outside Scribe's Rule 26 scope
3. If any violation found: write a report to `.squad/decisions/inbox/ralph-violation-{date}.md`
4. Fire a Teams notification

**Done looks like:** Any future direct-to-main commit by the coordinator triggers an automated violation alert within one Ralph cycle.

**Elrond's version of this is right. I'm just making it concrete.**

---

## Structural Changes (Require Repo Settings or Architecture Changes)

### Action 7: Explicit exemption categories in routing.md

**Who:** Gandalf (via PR)  
**What:** Add a new section to routing.md:

```markdown
## Exemptions to the Worktree/PR Pipeline

The following categories are EXEMPT from Rule 12 (worktree requirement):
- **Scribe operational logs:** Files listed in Rule 26 scope
- **Typo fixes:** Single-character or single-word corrections (post-hoc Galadriel review required within 24h)
- **Emergency hotfixes:** Production-breaking issues only. Requires: (a) immediate fix, (b) post-mortem within 24h, (c) retroactive Galadriel review

Everything else — governance changes, documentation, features, investigations, design docs — goes through the full pipeline. No exceptions. "It's just a small change" is not an exemption category.
```

**Done looks like:** The coordinator has a clear, bounded list of what can skip the pipeline, eliminating ad-hoc rationalization.

### Action 8: Auto-PR on squad branch push (Nice-to-Have)

**Who:** Jonathan or Gandalf  
**What:** GitHub Action that auto-creates a PR when any `squad/*` branch is pushed. Eliminates one manual step from the compliant path.

**Done looks like:** Agent pushes to `squad/42-my-feature`, a PR is auto-created targeting main, Galadriel review is triggered.

**This is Elrond's Priority 6 but scoped to just the auto-PR part, which is the highest-ROI friction reduction. The rest (auto-review trigger, `squad-ship.ps1`) is premature — let's see if auto-PR alone is enough.**

---

## What I'm NOT Recommending (And Why)

| Elrond's Recommendation | My Verdict | Reason |
|---|---|---|
| P5: `squad-git.ps1` wrapper script | **Skip** | Branch protection makes this redundant. Adding a wrapper script creates a maintenance burden and a second enforcement system to keep in sync. |
| P5: Remove `git push`/`git merge` from coordinator | **Skip** | Too restrictive. The coordinator needs to push to squad branches. The problem is pushing to *main*, which branch protection solves. |
| P6: `squad-ship.ps1` automation script | **Defer** | Good idea but premature. Auto-PR (Action 8) captures 80% of the value. Revisit if the compliant path is still too friction-heavy after branch protection. |
| P6: Auto-review trigger GitHub Action | **Defer** | The coordinator already knows to spawn Galadriel. If it doesn't, that's a separate instruction problem, not an automation problem. |
| P4: Ralph violation detection | **Keep** (Action 6) | This is defense-in-depth and worth having even with branch protection. |

---

## Priority Summary

| # | Action | Owner | Effort | Impact | Timeline |
|---|--------|-------|--------|--------|----------|
| 1 | Pre-push hook | Jonathan | 15 min | Medium | Today |
| 2 | STOP-gate in squad.agent.md | Gandalf PR | 15 min | Medium | Today |
| 3 | Record violation in history.md | Gandalf | 5 min | Low | Today |
| 4 | **Branch protection on main** | Jonathan | 10 min | **Critical** | This week |
| 5 | Scribe exemption handling | Jonathan + Gandalf | 30 min | Medium | This week |
| 6 | Ralph violation scan | Ralph | 1 hour | Medium | This week |
| 7 | Explicit exemptions in routing.md | Gandalf PR | 20 min | Medium | Next week |
| 8 | Auto-PR on squad branch push | Jonathan | 1 hour | Low-Med | Next week |

**The single action that matters most is #4 (branch protection).** If we do nothing else, that alone eliminates 90% of the problem. Everything else is either defense-in-depth or friction reduction.

---

## Final Note

Elrond's conclusion is correct: *"'I'll be more disciplined' is not a valid engineering solution for a stateless LLM."* The coordinator isn't being lazy or rebellious. It's a context-bounded system doing exactly what context-bounded systems do: optimizing for the immediate task when guardrails are advisory-only.

The fix is a locked door, not a better sign.

---

*Action plan complete. Ready for execution upon Jonathan's authorization for branch protection changes.*
