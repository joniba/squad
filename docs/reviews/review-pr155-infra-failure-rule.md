# PR #155 Review: [governance] Add Rule 25: infrastructure failure notifications

**Reviewer:** Galadriel  
**PR:** #155  
**Branch:** squad-154  
**Date:** 2025  

**VERDICT:** ✅ **APPROVE**

---

## Review Checklist

### ✅ 1. PR Diff Review
**Status:** PASS

The PR adds Rule 25 to `.squad/routing.md` (line 25, after Rule 24). The diff shows:
- Single, focused commit
- Clean addition without modifying existing rules
- Line 25 added with proper numbering sequence
- No deletions or unintended side effects

### ✅ 2. Integration with Existing Rules
**Status:** PASS

Rule 25 integrates correctly with the governance framework:

- **Rule 15 (Teams notifications):** Rule 25 is orthogonal and complementary. Rule 15 covers three event categories (feature-complete, investigation-complete, blocked). Rule 25 adds a fourth mandatory category (infrastructure failures) and explicitly mandates IMMEDIATE notification. The two rules work in harmony: Rule 15 establishes the notification framework and script; Rule 25 defines a specific failure condition requiring immediate invocation of that framework.

- **Rule 23 (Post-merge issue verification):** No conflict. Rule 23 operates after merge; Rule 25 operates during coordinator operations. If a failure occurs during the Rule 23 workflow, Rule 25 triggers independently.

- **Rule 24 (Worktree cleanup):** No conflict. Rule 24 runs after merge; Rule 25 monitors operations throughout the workflow. If worktree corruption is detected during cleanup (Rule 24), Rule 25 triggers properly.

- **Rule 20 (Post-completion pipeline):** No conflict. Rule 25 is a safety gate across all coordinator operations, including the Rule 20 pipeline.

**Cross-reference audit:** No other rules reference rules 15, 23, or 24 in a way that would require updating due to Rule 25's addition. Rule 25 is self-contained.

### ✅ 3. Notification Command Format Verification
**Status:** PASS - Format matches script contract

The rule specifies:
```powershell
.\scripts\notify-squad-event.ps1 -Event "blocked" -What "Infrastructure failure: {category}" -Why "{error message}" -ActionNeeded "Investigate and resolve {category} failure" -Link "{relevant URL if any}" -Agent "coordinator"
```

**Script validation against `notify-squad-event.ps1`:**
- ✅ `-Event "blocked"` — Valid. Script accepts `[ValidateSet("feature-complete", "blocked", "investigation-complete")]` (line 111)
- ✅ `-What "{string}"` — Valid. Documented parameter for blocked event (line 42, synopsis)
- ✅ `-Why "{error message}"` — Valid. Documented parameter for blocked event (line 44)
- ✅ `-ActionNeeded "{string}"` — Valid. Documented parameter for blocked event (line 49)
- ✅ `-Link "{relevant URL if any}"` — Valid. Documented parameter for blocked event (line 58)
- ✅ `-Agent "coordinator"` — Valid. Documented parameter for blocked event (line 64)

All parameters match the `notify-blocked.ps1` single-issue mode documented in `notify-squad-event.ps1` (examples at lines 94-98).

**Format accuracy:** The command template is syntactically correct and will be accepted by the dispatcher without error.

### ✅ 4. Rule Numbering Verification
**Status:** PASS

- Rule 24 (worktree cleanup) exists on line 102 of routing.md
- Rule 25 (infrastructure failure notifications) added on line 103
- Sequence is correct: 22 → 23 → 24 → 25
- No numbering gaps or duplicates

### ✅ 5. Cross-References and Documentation Requirements
**Status:** PASS - No updates needed

Audit of references to Rules 15, 23, 24:
- Rule 20 (post-completion pipeline): References Rule 20 (step numbers), not 15/23/24 — no impact
- Rule 21 (feature review failure): References Rule 20 — no impact
- Rule 22 (won't-fix transparency): References Rule 20 — no impact
- Failure recovery pipeline description: No references to 15, 23, 24 by rule number — no impact

**Finding:** Rule 25 is sufficiently self-contained that no existing rules require updates or cross-references. The rule stands independently as a monitoring gate.

### ✅ 6. Meta-Failure Exception Review
**Status:** PASS - Exception is sound and properly documented

The rule states:
> **Meta-failure exception:** if the notification script itself fails, log to console with `[CRITICAL]` prefix — this is the only case where silent handling is permitted (because the notification system itself is broken).

**Rationale assessment:**
- ✅ Exception is **narrowly scoped** — applies only to the notification script, not to the underlying failure being reported
- ✅ Exception is **documented with justification** — "the notification system itself is broken"
- ✅ Exception includes **alternative action** — console logging with `[CRITICAL]` tag ensures the failure is NOT silent; it's logged visibly
- ✅ Exception is **not a governance loophole** — the coordinator can still inspect console logs and escalate manually if needed
- ✅ Exception prevents **cascading failures** — avoids the pathological case where a broken notification system causes a failure to hide other failures

This exception is reasonable and prevents infinite error-handling loops.

---

## Failure Category Coverage Audit

Rule 25 specifies these failure categories:
1. **Git operations** (permissions, auth, push/pull failures) — ✅ Common and correctible
2. **GitHub CLI errors** (`gh` command failures) — ✅ Common and correctible  
3. **MCP tool disconnections or errors** — ✅ Common in multi-agent systems
4. **Notification script failures** — ✅ Covered by meta-failure exception
5. **Worktree corruption** — ✅ Critical infrastructure state
6. **Branch operations failures** — ✅ Foundational to workflow
7. **Merge conflicts that block automation** — ✅ Prevents silent stalling

**Coverage assessment:** The list covers the primary failure modes for a coordinator managing git, GitHub CLI, MCP tools, and local worktree infrastructure. Edge cases (network timeouts, Teams API failures) fall under meta-failure exception or are treated as "notification script failures." No significant governance gaps detected.

---

## Immediate Application Scenarios

Examples where Rule 25 would correctly fire:

| Scenario | Trigger | Action |
|----------|---------|--------|
| `git push` auth fails on feature branch | Git failure during merge prep | Notify immediately: "Infrastructure failure: git push auth" |
| `gh pr create` returns 403 Forbidden | GitHub CLI failure | Notify immediately: "Infrastructure failure: gh command" |
| MCP server disconnects mid-review | MCP tool failure | Notify immediately: "Infrastructure failure: MCP disconnection" |
| Worktree `git rebase` exits with merge conflict | Branch operation failure | Notify immediately: "Infrastructure failure: merge conflict blocks automation" |
| `.\scripts\notify-squad-event.ps1` script file missing | Notification script failure (meta) | Log to console: `[CRITICAL] Notification system broken: script missing` (no Teams card) |

---

## Rule Interactions: Post-Merge Scenario

**Scenario:** A worktree contains a merged branch. Rule 24 cleanup runs `git worktree remove`, which fails due to filesystem corruption.

**Expected execution:**
1. Rule 24 cleanup detects failure (line: "If the worktree contains uncommitted changes, the coordinator MUST warn and skip removal")
2. Rule 25 triggers on detection (infrastructure failure: "worktree corruption")
3. Teams notification fires immediately with category and error message
4. Coordinator inspects, escalates to Jonathan if needed
5. Manual cleanup follows (outside automation)

**Verdict:** Rules interact correctly. No priority conflicts.

---

## Code Quality and Clarity

✅ **Rule is precise:** Clear failure categories, explicit timing ("IMMEDIATELY"), no ambiguity  
✅ **Rule is actionable:** Coordinator knows exactly which command to run and what parameters to pass  
✅ **Rule is defensive:** Meta-failure exception prevents cascading errors  
✅ **Rule is complete:** All required elements present (categories, command format, timing, exception)  

---

## Summary of Findings

| Criterion | Status | Notes |
|-----------|--------|-------|
| Numbering | ✅ PASS | Correct sequence after Rule 24 |
| Integration | ✅ PASS | No conflicts; complementary to Rule 15 |
| Script compatibility | ✅ PASS | All parameters valid; format correct |
| Failure coverage | ✅ PASS | Comprehensive; no gaps |
| Meta-failure exception | ✅ PASS | Reasonable scope; prevents loops |
| Cross-references | ✅ PASS | No updates needed to existing rules |
| Clarity | ✅ PASS | Precise, actionable, complete |

---

## VERDICT: ✅ APPROVE

**Rule 25 correctly mandates infrastructure failure notifications with:**
- Clear failure category definitions
- Exact notification command template matching the existing script contract
- IMMEDIATE firing (no batching/deferral)
- Properly scoped meta-failure exception
- No conflicts with existing rules
- Complete governance coverage of coordinator infrastructure failure modes

**No findings. Ready for merge.**

---

**Review completed:** 2025  
**Reviewer:** Galadriel  
**Signature:** ✅ APPROVE
