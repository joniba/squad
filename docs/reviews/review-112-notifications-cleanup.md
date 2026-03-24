# Review: squad/112-notifications-cleanup

**Reviewer:** Galadriel  
**Reviewed:** 2026-03-28  
**Branch:** squad/112-notifications-cleanup  
**Commits:** 1 (1f57f06)  
**Files Changed:** 3 (1 rename, 2 updates)  
**Lines:** +40 insertions, -3 deletions

---

## Scope Summary

This is organizational housekeeping for issue #112 (Proactive Teams Notifications):

1. **Rename** design doc: `docs/designs/proactive-notifications.md` → `docs/designs/human-attention-notifications.md`
2. **Update SKILL.md** to document both notification systems (old active + new built)
3. **Update failure-recovery.md** to provide explicit migration path for notifications

---

## Detailed Findings

### ✅ File Rename (docs/designs)

**Finding:** Rename is correct and matches issue #112 specification.

- **Old path:** `docs/designs/proactive-notifications.md`
- **New path:** `docs/designs/human-attention-notifications.md`
- **Reasoning (from commit):** "match what issue #112 specifies as the deliverable filename"
- **Correctness check:** Confirmed by commit message R1 and verified against history.md decision #112.

**Verdict:** ✅ CORRECT

---

### ✅ SKILL.md Updates

**Finding:** Additions accurately document both systems and provide clarity on status.

**Key changes:**
1. Added "TWO notification systems" header (line 67) — clarifies context immediately
2. Added "Old System (ACTIVE — 2 callers)" section with:
   - Script location and description
   - Explicit caller citations:
     - `scripts/icm-scan.ps1` (line 286) — ICM incident alerts
     - `scripts/squad-daily-summary.ps1` (line 56) — daily summary
   - Warning block: "Do NOT remove or replace the old system until the new system's MVP callers are validated end-to-end."

3. Added "New System (BUILT — 0 callers yet)" section with:
   - Script location and purpose
   - Supporting scripts listed (notification-recovery.ps1, notification-scheduler.ps1)
   - Planned MVP callers marked as "not yet built — see issues #132–#135"
   - Dry-run example showing `-DryRun` flag and structured `-Event` object

4. Clarified daily summary routing (line 108): "uses `send-teams-notification.ps1` (old system) automatically"

**Accuracy check:**
- Caller citations accurate per commit message (R6: "Old system: active, 2 callers")
- MVP issues referenced correctly (#132–#135 per commit narrative)
- Schema example matches new system's parameters (Type, Event object with eventId, title, reason, actionUrl, actionLabel, DryRun flag)
- Warning is appropriately strong: prevents premature switchover before validation

**Verdict:** ✅ CORRECT — Provides clear dual-system documentation with proper guardrails.

---

### ✅ failure-recovery.md Updates

**Finding:** Migration path is explicit and removes ambiguity.

**Key changes:**
1. Replaced vague instruction ("Post to Teams webhook") with structured guidance:
   - **Current:** Call `scripts/send-teams-notification.ps1` (proven delivery)
   - **Target:** Call `scripts/notify-blocked.ps1` (once MVP validated)
   - **Condition:** Only after end-to-end validation (issue #135)
   - **Duration:** Until then, use old system

2. Added explicit migration plan language: "Switch to `notify-blocked.ps1` ONLY after end-to-end validation"

3. Preserved tagging requirement: "Tag the GitHub issue `needs-human`"

**Accuracy check:**
- Script names match SKILL.md (send-teams-notification.ps1 current, notify-blocked.ps1 target)
- Validation gate (#135) matches MVP planning in decisions.md (pre-design scan recommends validation before expansion)
- Language is appropriately cautious: no ambiguous "migrate when ready" — explicit validation checkpoint

**Verdict:** ✅ CORRECT — Removes ambiguity and provides guardrail for controlled migration.

---

## Cross-Reference Validation

**Against decisions.md (Pre-Design Scan, 2026-03-25):**
- ✅ Issue #112 finding #1: "New notification system has zero production callers" — SKILL.md now documents this explicitly ("0 callers yet")
- ✅ Recommendation: "Validate notify.ps1 works (dry-run + one real notification)" — SKILL.md now includes dry-run example
- ✅ Recommendation: "Do not start new implementation until foundation is proven" — failure-recovery.md now explicitly gates switchover on #135 validation

**Against commit message narrative:**
- ✅ R1 (rename): Verified as correct
- ✅ R6 (SKILL.md): Both systems documented with caller counts and status
- ✅ R7 (failure-recovery.md): Explicit migration path with validation gate and issue reference

---

## Risk Assessment

### Security
- ✅ No new secrets or credentials exposed
- ✅ Webhook URL handling unchanged (still reads from default location)
- ✅ No auth changes

### Maintainability
- ✅ Dual-system documentation is clear and prevents confusion
- ✅ MVP caller issue references (#132–#135) provide traceability
- ✅ Warning blocks prevent premature system switchover

### Scope Creep
- ✅ Commit scope is documentation only — no new functionality
- ✅ No untracked changes (all items from commit message accounted for)
- ✅ GitHub issue actions (closed #115, updated #116, labeled #117, created #132–#135) documented in commit message but not in commit diff — correct (team actions, not code changes)

---

## Summary

**Branch:** squad/112-notifications-cleanup  
**Changes:** 1 rename, 2 documentation updates  
**Quality:** Correct, clear, well-reasoned  
**Risk:** Minimal (documentation only)  
**Compliance:** Follows team decisions (#112 pre-design scan recommendations)

All findings are accurate and appropriate. No corrections needed.

---

## VERDICT

### ✅ **APPROVE**

**Rationale:**
1. File rename correctly implements #112 specification
2. SKILL.md accurately documents both systems with proper guardrails
3. failure-recovery.md removes ambiguity with explicit migration path and validation gate
4. All changes align with team decisions (pre-design scan, MVP planning)
5. Documentation is clear, maintainable, and prevents operational confusion
6. Scope is controlled; no unrelated changes

**Ready to merge to main.**
