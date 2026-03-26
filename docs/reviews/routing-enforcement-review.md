# Review: Routing Enforcement Investigation + Action Plan

**Reviewer:** Galadriel  
**Date:** 2026-03-25  
**Documents Reviewed:**
1. `docs/investigations/routing-enforcement-investigation.md` — Elrond (Researcher)
2. `docs/investigations/routing-enforcement-action-plan.md` — Gandalf (Lead)

**Verdict:** APPROVE WITH CONDITIONS

---

## For Jonathan: Executive Summary

**The situation:** Your routing rules work exactly as designed on paper. The problem is that they exist only on paper. Out of 226 commits on main, 173 came from the coordinator. After subtracting merge commits and Scribe-scope work, roughly 135 are non-merge, non-Scribe commits pushed directly to main by the coordinator — bypassing the worktree, PR, and review pipeline you built. This is not a rounding error. It is the system's default operating mode.

**What's broken:** There is zero technical enforcement. No git hooks, no branch protection, no CI gate. Every rule in routing.md is a suggestion that the coordinator follows when context is fresh and ignores when it isn't. The coordinator isn't defiant — it's a stateless system optimizing for your immediate request, and the shortest path to "done" is `git add && git commit` on main.

**What fixes it:** One action matters more than all others combined: **enable GitHub branch protection on main**. This is a server-side gate that cannot be bypassed by the coordinator, regardless of context pressure, session length, or eagerness. It makes `git push origin main` physically impossible. Everything else — hooks, checklists, Ralph scans — is defense-in-depth. Useful, but secondary.

**What you need to do:**
1. **Today (10 minutes):** Enable branch protection on main in GitHub repo settings. Require PRs, require 1 approval, disallow bypassing.
2. **Today (5 minutes):** Run `git config core.hooksPath .githooks` after Gandalf commits the pre-push hook.
3. **This week (30 minutes):** Decide on Scribe exemption handling (see conditions below).

**What will change:** After branch protection, the coordinator will be forced through the worktree → PR → review pipeline for every commit. This eliminates the dominant violation pattern. The pre-push hook adds a local early-warning. The STOP-gate checklist in squad.agent.md reduces instruction-layer drift. Ralph's scan catches anything that slips through.

**What won't change without your action:** Nothing. The coordinator cannot enable branch protection — only you can. Until that happens, this investigation and action plan are documentation of a problem, not a solution to it.

---

## Part 1: Review of Elrond's Investigation

### Accuracy: STRONG

- **Commit counts:** Elrond claimed 245 non-merge commits and 183 coordinator-authored. Actual numbers: 226 total first-parent commits on main, 173 from `elrond@squad.local`. The percentages hold (76.5% coordinator-authored). Gandalf's correction to 226 is accurate. The 96+ violation figure is reasonable — I count ~135 non-merge, non-Scribe coordinator commits, of which a significant majority should have gone through the pipeline.
- **Hook claims:** Verified. No hooks exist anywhere — `.git/hooks/pre-push`, `.git/hooks/pre-commit`, and `.githooks/pre-push` all return False.
- **Violation examples:** Spot-checked against git log. All cited commits exist with the described content. The evidence is real.
- **Root cause analysis:** Excellent. The seven root causes are well-identified and correctly prioritized. The "context drift" framing (Root Cause 2) and the "compliant path friction" analysis (Root Cause 3) are particularly strong.

### Completeness: GOOD, with two gaps

**Gap 1: No analysis of Jonathan's own commits.** 53 commits on main are from `jbenami@microsoft.com`. Some of these may also bypass the pipeline. The investigation focused entirely on the coordinator, but if the human owner also commits directly, that undermines the enforcement culture. Branch protection solves this too — it applies to everyone, including admins if "Do not allow bypassing" is enabled.

**Gap 2: No analysis of the coordinator reading order.** Gandalf flagged this in the action plan: the investigation doesn't examine whether the coordinator reads `routing.md` or `squad.agent.md` first, and which takes precedence when they conflict. This matters for Root Cause 5 — if the core loop in `squad.agent.md` doesn't reference Rule 12, and the coordinator reads `squad.agent.md` first, the worktree requirement may never be salient.

### What Elrond Got Right That Matters

The concluding line — *"'I'll be more disciplined' is not a valid engineering solution for a stateless LLM. Technical gates are."* — is the single most important sentence in either document. This is the core insight. Everything else is detail.

---

## Part 2: Review of Gandalf's Action Plan

### Practicality: STRONG

The action plan is well-structured, correctly prioritized, and honest about what each action achieves. The "What I'm NOT Recommending" section is particularly valuable — it shows that Gandalf evaluated Elrond's full recommendation set and made principled cuts.

### Specific Findings

**Action 1 (Pre-push hook): APPROVE.**
Correct implementation. The caveat about `--no-verify` is honest. This is a speed bump, not a wall, and Gandalf correctly positions it as such.

**Action 2 (STOP-gate in squad.agent.md): APPROVE.**
Placing the checklist at the point of action rather than in a separate section is the right call. Instruction-layer enforcement degrades with distance from the action point. Five lines above the spawn template is the right position.

**Action 3 (Record in history.md): APPROVE.**
Necessary for cross-session context preservation. Low effort, real value.

**Action 4 (Branch protection): APPROVE — THIS IS THE ONLY ACTION THAT MATTERS.**
Gandalf is correct that this is the single most impactful action. If Jonathan does only one thing, it must be this.

**Action 5 (Scribe exemption): APPROVE WITH CONDITION.**
Gandalf recommends Option C (defer — Scribe goes through PRs temporarily). I agree for now, but with a time-box: if Scribe's PR volume becomes annoying within two weeks, implement Option A (squad-state branch with auto-merge). Don't let this fester.

**Action 6 (Ralph violation scan): APPROVE.**
Defense-in-depth. Even with branch protection, having an audit trail is good practice. The implementation is concrete and actionable.

**Action 7 (Explicit exemptions): APPROVE WITH CONDITION.**
The exemption categories are reasonable, but the "typo fix" exemption is dangerously vague. "Single-character or single-word corrections" could be stretched to include governance wording changes. **Condition:** Typo fixes must be limited to spelling/grammar corrections in documentation files only. Any change to routing.md, squad.agent.md, or charter files is never a "typo fix" — it's a governance change and requires the full pipeline.

**Action 8 (Auto-PR on push): APPROVE as nice-to-have.**
Correctly prioritized as lowest urgency. The ROI is real but not blocking.

### What Gandalf Got Right That Matters

The decision to skip Elrond's P5 (squad-git.ps1 wrapper) is correct. Branch protection makes wrappers redundant, and maintaining a parallel enforcement system creates sync risk. The principle — one enforcement mechanism per layer, not redundant mechanisms — is sound.

### Minor Issue

The action plan date says "2025-07-24" but should be "2026-03-25". Cosmetic, but worth fixing for audit trail accuracy.

---

## Part 3: Gaps Neither Document Addresses

### Gap A: What happens during the transition?

Between now and when branch protection is enabled, the coordinator will continue committing directly to main. Neither document proposes a transitional mechanism. **Recommendation:** Jonathan should enable branch protection before any more coordinator sessions run. If that's not possible today, the pre-push hook should be installed as an interim gate within this session.

### Gap B: What about force-push?

Branch protection should include "Do not allow force pushes" explicitly. Neither document mentions this. A coordinator under pressure could theoretically force-push to overwrite a rejected PR's branch. The branch protection rule should cover this.

### Gap C: What about the PR merge method?

When branch protection requires PRs, the coordinator will merge them via `gh pr merge`. Neither document specifies whether squash-merge, merge-commit, or rebase-merge should be the default. This affects git history readability. **Recommendation:** Require squash-merge for single-commit PRs and merge-commit for multi-commit PRs. Add this to routing.md.

### Gap D: Review coverage for the reviewer

I (Galadriel) review PRs, but who reviews my reviews? The current system has no quality check on the reviewer itself. If I approve a bad PR, there's no backstop. This isn't urgent, but for completeness: consider having Gandalf spot-check a random sample of my reviews monthly.

---

## Conditions for Approval

This review is **APPROVE WITH CONDITIONS**. The conditions are:

1. **Branch protection must be enabled before this investigation is considered "resolved."** The investigation and action plan are excellent analysis, but analysis without the P0 action is documentation of a known problem, not a fix. Jonathan must enable branch protection on main.

2. **The "typo fix" exemption (Action 7) must be scoped to documentation files only.** Changes to routing.md, squad.agent.md, and charter files are never typo fixes. This must be explicit in the exemption text.

3. **The action plan date must be corrected** from "2025-07-24" to "2026-03-25" before merge.

4. **The Scribe exemption (Action 5) must be time-boxed.** If Scribe's PR workflow is still unresolved in two weeks, escalate to Option A (squad-state branch).

All four conditions are low-effort. None blocks immediate action on branch protection.

---

## My Recommendations

If I were writing the action plan, I would add one item Gandalf omitted:

**Action 9: Post-protection smoke test.** After enabling branch protection, the coordinator should attempt `git push origin main` in the next session and verify it fails. Document the error. This confirms the gate works and creates evidence for the audit trail. A 2-minute test that prevents a "we thought it was enabled" failure mode.

---

## Summary Table

| Document | Verdict | Notes |
|----------|---------|-------|
| Elrond's Investigation | **Accurate and thorough** | Minor count discrepancies, two analytical gaps, but findings are sound |
| Gandalf's Action Plan | **Practical and sufficient** | Correct prioritization, honest about tradeoffs, one date error |
| Combined | **APPROVE WITH CONDITIONS** | Branch protection is the fix. Everything else is defense-in-depth. |

---

*Review complete. The investigation is solid. The action plan is actionable. The only thing missing is execution — specifically, Jonathan enabling branch protection on main. That is a 10-minute task that eliminates 90% of the problem. Everything else is gravy.*

---

*Galadriel — Reviewer, pa-squad*
