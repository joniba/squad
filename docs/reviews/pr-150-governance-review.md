# PR #150 Review: Remove Rule 15 — Governance Through PR Review

**Reviewer:** 👑 Galadriel  
**Date:** 2026-03-29  
**PR:** #150 | Author: Yoni Ben-Ami (@jbenami_microsoft)  
**Scope:** Governance change — removing Rule 15 (direct-to-main bypass) and Rule 24 (governance closure exception); renumbering affected rules; updating all cross-references  
**Consequence Level:** ⚠️ **CRITICAL** — Fundamentally alters governance bypass authority; enforces PR review for ALL governance changes

---

## Executive Summary

**VERDICT: ⛔ CHANGES_REQUESTED**

This governance change is **well-intentioned and correctly targets the right rules**, but **contains multiple stale cross-references to removed Rules 15 and 24** across four key files. The references are in documentation and templates, not in enforcement code, but they **create ambiguity and risk misinterpretation** of the governance model. These must be resolved before merge.

**Critical issues identified:**
1. ✗ `.squad/routing.md` Rule 11 still references "Rule 24" by name
2. ✗ `.squad/routing.md` Rules 101–102 reference removed "Rule 15" and "Rule 24"
3. ✗ `.squad/routing.md` Rule 102 references "Rule 25" (should be "Rule 23")
4. ✗ `.squad/templates/issue-lifecycle.md` references removed rules across two rows
5. ✗ Decision document still frames governance rules around removed Rule 15

---

## Detailed Findings

### File 1: `.squad/routing.md`

#### Finding 1.1 — **Rule 11 (Issue Closure Restriction)** — Line 78
**Severity:** 🔴 BLOCKING  
**Location:** Line 78, exception (b) fallout text  

**Current text:**
```
**Exceptions:** (a) tracking/strategic issues and superseded issues may be closed with a comment, 
(b) governance work committed directly to main per Rule 15 — see Rule 24, 
(c) branches merged via direct `git merge` during recovery — see Rule 25.
```

**Problem:** 
- "per Rule 15" and "Rule 24" no longer exist
- These rules were **completely removed** in this PR
- Exception (b) **should be deleted entirely** since Rule 15 (direct-to-main governance) no longer exists
- Remaining exceptions should be re-lettered: (a) → (a), (b) [DELETED] → new (b) for recovery, re-lettered from (c)

**Required fix:**
```
**Exceptions:** (a) tracking/strategic issues and superseded issues may be closed with a comment, 
(b) branches merged via direct `git merge` during recovery — see Rule 23.
```

---

#### Finding 1.2 — **Rule 23 (Post-Merge Issue Verification)** — Line 102
**Severity:** 🔴 BLOCKING  
**Location:** Line 102, rule reference  

**Current text (from search output):**
```
...recovery merges and any non-standard merge path. After ANY merge to main — whether via `gh pr merge`, 
`git merge`, or direct commit — the coordinator MUST verify that all issues whose work was just landed are 
properly closed. If an issue remains open after its branch merged (e.g., direct `git merge` bypassed auto-close), 
close it with `gh issue close {N} --comment "Branch merged to main via direct merge. PR auto-close did not fire. 
Closing per Rule 25."` This catches recovery scenarios, manual conflict resolution, and any non-standard merge path.
```

**Problem:**
- References "Rule 25" but Rule 25 **no longer exists** (was renumbered to 23)
- The comment references "Rule 25" which will confuse operators

**Required fix:**
```
...close it with `gh issue close {N} --comment "Branch merged to main via direct merge. PR auto-close did not fire. 
Closing per Rule 23."` This catches recovery scenarios, manual conflict resolution, and any non-standard merge path.
```

---

#### Finding 1.3 — **Rules 23-24 context references** — Lines 101–102
**Severity:** 🟡 MODERATE  
**Location:** Lines 101–102, multi-line stale reference fallout  

**Current text (from search output):**
```
101: 24. **Governance issue closure** — after committing governance changes directly to main (per Rule 15), 
     the coordinator MUST close the tracking issue...This is the sole authorized use of `gh issue close` for 
     file-producing governance work. It exists because Rule 15's no-branch/no-PR path means Rule 11's 
     PR-based auto-close can never fire for governance issues...
```

**Problem:**
- Entire Rule 24 definition references removed Rule 15
- Rule 24 **no longer exists in this PR**, so these lines should not exist post-merge
- **Verification needed:** Is Rule 24 actually deleted in the diff, or is this an artifact of the old version before PR changes?

**Required fix:**
- If Rule 24 remains in the file: it must be completely rewritten or deleted
- If Rule 24 is already marked for deletion in the diff: this is RESOLVED

---

### File 2: `.squad/templates/issue-lifecycle.md`

#### Finding 2.1 — **Issue Closure Rules table** — Line 46
**Severity:** 🔴 BLOCKING  
**Location:** Line 46, table row  

**Current text (from view output):**
```
| Governance (Rule 15) | Coordinator closes with `gh issue close` + comment after commit to main (Rule 24) |
```

**Problem:**
- References **two removed rules:** Rule 15 and Rule 24
- Since Rule 15 no longer allows direct-to-main commits, this entire row is **obsolete**
- Must be deleted to reflect new governance model

**Required fix:**
```
DELETE this entire row. Rule 15 and Rule 24 no longer exist. No governance exception to PR-gating.
```

---

#### Finding 2.2 — **Never use gh issue close** — Line 49
**Severity:** 🔴 BLOCKING  
**Location:** Line 49, footer text  

**Current text (from view output):**
```
**Never use `gh issue close` for any issue that produced files — EXCEPT** governance work committed directly 
to main (Rule 24) and branches merged via direct `git merge` during recovery (Rule 25). These are the only two 
authorized exceptions. The comment must cite the applicable rule.
```

**Problem:**
- References **both removed rules:** Rule 24 (governance) and wrong number for recovery rule
- After this PR, recovery rule is **Rule 23**, not Rule 25
- Governance exception no longer exists at all

**Required fix:**
```
**Never use `gh issue close` for any issue that produced files — EXCEPT** branches merged via direct `git merge` 
during recovery (Rule 23). This is the only authorized exception. The comment must cite the rule.
```

---

### File 3: `.squad/agents/gandalf/charter.md`

#### Finding 3.1 — **Charter section title and content**
**Severity:** 🟢 RESOLVED  
**Location:** Lines 66–68  

**Observation:**
The charter section heading changed from "Squad Governance Authority" to "Squad Governance Ownership" ✓  
The text was updated to state governance changes "follow the standard PR lifecycle" ✓  
**This is correct and properly reflects the removal of Rule 15's direct-to-main bypass.**

**Status:** ✅ No changes required

---

### File 4: `.squad/decisions/inbox/gandalf-issue-hygiene.md`

#### Finding 4.1 — **Decision documentation outdated**
**Severity:** 🟡 MODERATE  
**Location:** Lines 12, 20  

**Current text:**
```
1. **Rule 24 — Governance issue closure:** After committing governance changes to main (Rule 15), 
   the coordinator closes the tracking issue with `gh issue close` and a comment citing the commit SHA and Rule 15. 
   This is an authorized exception to Rule 11.
...
- Rule 24: Governance work should be tracked (Rule 17) but has no PR path (Rule 15). Explicit closure step fills the gap.
```

**Problem:**
- Decision document still **frames governance rules around removed Rule 15**
- This is a **governance record**, so it should be updated to reflect the new state
- Does not cause operational issues but creates historical ambiguity

**Required fix:**
```
Update decision context to note that this decision (from #142) was **superseded by PR #150**, which removed 
Rules 15 and 24 and enforced PR review for all governance changes. Add comment: "Superseded by #150 - governance 
changes now follow PR review workflow per Rule 15 (formerly Rule 16: Teams notifications)."
```

---

## Cross-Reference Audit Summary

| File | Rule References | Status |
|------|-----------------|--------|
| routing.md | Rule 11 (exception b) | ✗ Stale — references removed Rule 15/24 |
| routing.md | Rule 23 comment | ✗ Wrong number — says "Rule 25" (should be "Rule 23") |
| routing.md | Rule 24 (lines 101–102) | ⚠️ VERIFY — appears to still exist; if so, references removed Rule 15 |
| issue-lifecycle.md | Table row "Governance (Rule 15)" | ✗ Stale — both rules removed |
| issue-lifecycle.md | Footer exception text | ✗ Stale + wrong number — Rule 24 removed, Rule 25 → 23 |
| gandalf/charter.md | "Governance Ownership" | ✅ Correct — properly updated |
| gandalf-issue-hygiene.md | Decision context | 🟡 Outdated — should note supersession by #150 |

---

## Renumbering Verification

**Before PR (old scheme):**
- Rule 15: Squad governance via Gandalf (direct-to-main)
- Rule 16: Teams notifications
- Rule 24: Governance issue closure
- Rule 25: Post-merge issue verification
- Rule 26: Worktree cleanup

**After PR (expected new scheme):**
- Rule 15: Teams notifications (was 16) ✓
- Rule 23: Post-merge issue verification (was 25) ✓
- Rule 24: Worktree cleanup (was 26) ✓

**Issue:** Rule 24 renumbering is correct, but the new Rule 24 (Worktree cleanup, formerly Rule 26) is **NOT** the governance exception. Old Rule 24 is **deleted entirely**. The exception text still references the old Rule 24, causing ambiguity.

---

## Verdict: ⛔ CHANGES_REQUESTED

### Blocking Issues (must fix before merge):
1. **Rule 11 exception (b):** Delete or rewrite to remove references to removed Rule 15 and Rule 24
2. **Rule 23 comment:** Change "Rule 25" to "Rule 23"
3. **issue-lifecycle.md table row:** Delete entire "Governance (Rule 15)" row
4. **issue-lifecycle.md footer:** Rewrite to remove Rule 24 exception and correct Rule 25 → Rule 23

### Non-blocking (but recommended before merge):
5. **gandalf-issue-hygiene.md:** Add note that decision was superseded by PR #150

### Root Cause Analysis

The PR author performed cross-reference audits, but the **audit missed exception clauses and templated guidance** that reference the removed rules. These are secondary references (not the rule definitions themselves), which can be easy to miss in a comprehensive renumbering.

---

## What This Change Means for the Squad

✅ **After approval:**
- Governance changes (charters, routing.md, team.md, ceremonies.md) **no longer have a direct-to-main bypass**
- All governance modifications **follow the standard PR lifecycle:** branch → PR → Galadriel review → merge
- Gandalf remains the designated author for governance files, but now must create PRs like everyone else
- Governance work **must be tracked as GitHub issues** (Rule 17) and close via PR auto-close (Rule 11)
- This enforces **peer review and traceability** for structural changes to the squad itself

---

## Recommendation

**Request changes from author.** The fixes are surgical and well-defined:

1. Open `.squad/routing.md`:
   - Rule 11, line ~78: Update exception text to remove references to Rules 15/24
   - Rule 23, line ~102: Change "Rule 25" → "Rule 23"
   - Rule 24 definition (lines 101–102): Verify if this rule still exists; if so, rewrite to remove Rule 15 references

2. Open `.squad/templates/issue-lifecycle.md`:
   - Delete entire table row for "Governance (Rule 15)"
   - Rewrite footer text to remove old Rule 24 exception reference; fix Rule 25 → Rule 23

3. (Optional) `.squad/decisions/inbox/gandalf-issue-hygiene.md`:
   - Add note that PR #150 supersedes this decision

After fixes are committed and pushed, I will review the updated PR and render a APPROVE verdict.

---

**Reviewer Confidence:** 🟢 HIGH  
All issues are well-defined, localized, and solvable with small targeted edits. The core intent of the PR (removing Rule 15's direct-to-main bypass) is sound and correctly implemented in the charter and main rules; only secondary references need cleanup.

---

**Posted by:** 👑 Galadriel  
**Charter:** `.squad/agents/galadriel/charter.md`  
**Authority:** PR review gate (Rule 10)
