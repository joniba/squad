---
title: "Galadriel PR Review #143: Teams Notification System Runbook"
reviewer: Galadriel
pr: 143
date: 2026-03-24
verdict: CHANGES_REQUESTED
---

# Galadriel PR Review — PR #143: Teams Notification System Runbook

**Reviewer**: Galadriel (Code Review Agent)  
**PR**: jbenami_microsoft/ms-pa#143  
**Author**: Bilbo  
**Deliverable**: `docs/guides/notifications-runbook.md` (470 lines)  
**Verdict**: **CHANGES_REQUESTED**  
**Severity**: Medium (documentation inaccuracy + incomplete reference section)

---

## Executive Summary

PR #143 delivers a comprehensive Teams notification system runbook documenting the three-tier notification architecture (urgent, action, feature). **Overall quality is high: the documentation is well-structured, examples are operational, and core APIs are accurately documented.** However, **two issues prevent approval**:

1. **Parameter Mapping Inconsistency**: The `notify-squad-event.ps1` integration examples show event schema fields that don't align with the actual dispatcher parameter mappings (lines 143–170 of notify-squad-event.ps1).
2. **Incomplete Reference Section**: Documentation references `notification-scheduler.ps1` as a critical dependency but provides no reference entry for it, leaving readers unable to understand the scheduler contract.

Both issues are **low severity but require correction** to ensure operational accuracy and prevent future support burden.

---

## Verification Methodology (Galadriel Charter Stage 2 — Deep Analysis)

### Context Established ✅
- Notification system is **production-ready** (26/26 tests pass per .squad/decisions.md)
- Previous integration gaps resolved; system is fully wired
- Documentation claims to be "based on actual script analysis" (PR body)

### Files Analyzed

| File | Lines | Status | Notes |
|------|-------|--------|-------|
| `scripts/notify.ps1` | 563 | ✅ Verified | Central router; tier logic, dedup, card builders all match docs |
| `scripts/notify-blocked.ps1` | 123 | ✅ Verified | Urgent tier wrapper; APIs align with documentation |
| `scripts/notify-feature-complete.ps1` | 110 | ✅ Verified | Feature tier wrapper; calls notification-scheduler |
| `scripts/notify-squad-event.ps1` | 211 | ⚠️ Partial | Dispatcher routing verified; **parameter mapping discrepancy found** |
| `.squad/notifications-state.json` | 85 | ✅ Verified | State schema matches documented structure |

---

## Key Findings

### ✅ Verified Accurate

#### 1. Notification Tier Architecture (Section: Notification Tiers)
**Documentation**: Three tiers with defined delivery patterns  
**Implementation** (notify.ps1, lines 87–148):
- **Urgent**: Immediate; re-notify on 3+ errors OR 6h silence ✅
- **Action**: Immediate; re-notify on state change OR 48h silence ✅
- **Feature**: Batched ≥5 items or >1h; never re-notify same feature ✅

**Verdict**: Accurate

#### 2. Deduplication Logic (Section: Setup & Configuration, Notification Tiers)
**Documentation**: Re-notification windows per tier  
**Implementation** (notify.ps1, lines 150–232):
- Dedup tracked in `.squad/notifications-state.json` under `events` key ✅
- eventId format: `{type}:{context}:{timestamp}` ✅
- Expiry windows enforced via `lastNotifiedAt` and `errorCount` ✅

**Verdict**: Accurate

#### 3. Adaptive Card Format (Reference section, Section: Integration Guide)
**Documentation**: Schema 1.4, 28 KB limit, wrapped in attachment  
**Implementation** (notify.ps1, lines 235–336):
- Card schema: `http://adaptivecards.io/schemas/adaptive-card.json` ✅
- Payload limit enforced (line 360): `if ($payload.Length -gt 28KB)` ✅
- Fallback card generated on overflow ✅

**Verdict**: Accurate

#### 4. Configuration Paths (Section: Setup & Configuration)
**Documentation**:
- Webhook URL: `~/.squad/teams-webhook.url` ✅
- State file: `.squad/notifications-state.json` ✅
- Error log: `.squad/notifications-errors.log` ✅

**Verification**:
- Webhook file exists at documented location ✅
- State file verified with actual data (see state file review below) ✅

**Verdict**: Accurate

#### 5. State File Schema (Reference section, State File subsection)
**Documentation** schema:
```json
{
  "version": 1,
  "lastUpdated": "ISO-8601",
  "events": { "eventId": { "type", "lastNotifiedAt", "errorCount", "retryCount" } },
  "featureQueue": [ { "featureId", "featureTitle", "summary", "queuedAt" } ],
  "lastFeatureBatchAt": "ISO-8601"
}
```

**Actual state file** (`.squad/notifications-state.json`):
- version: 1 ✅
- lastUpdated: "2026-03-24T20:26:26..." ✅
- events: keyed by eventId with type/lastNotifiedAt/errorCount/retryCount ✅
- featureQueue: array (currently empty) ✅
- lastFeatureBatchAt: populated ✅

**Verdict**: Accurate—schema matches implementation perfectly

#### 6. Event Type Schemas (Reference section, Event Schema subsection)
**Documented** Urgent event schema:
```json
{ "eventId", "title", "reason", "actionUrl", "actionLabel" }
```

**Implementation** (notify-blocked.ps1, lines 92–106):
```powershell
$event = @{
    eventId     = "blocked:${agentName}:${timestamp}"
    errorType   = "needs-human"  # [additional field]
    title       = $Title
    reason      = $Reason
    actionUrl   = $BlockerUrl
    actionLabel = $BlockerLabel
}
```

**Finding**: Documentation is **correct but incomplete** — implementation adds `errorType` field for tier-specific routing. This is informational and does not affect caller scripts (field is internal to notify.ps1).

**Verdict**: Acceptable (additional fields do not break contract)

---

### ⚠️ Issue #1: Parameter Mapping Inconsistency (notify-squad-event.ps1)

**Location**: Integration Guide section, "Dispatcher Entry Point" subsection (lines ~210–230 in PR diff)

**Documentation Example**:
```powershell
.\scripts\notify-squad-event.ps1 `
    -Event "blocked" `
    -What "dGrep SDK auth" `
    -Why "dSTS requires VPN..." `
    -ActionNeeded "Provision VPN..." `
    -Link "https://..." `
    -Urgency "blocking-feature" `
    -Agent "Gimli"
```

**Implementation** (notify-squad-event.ps1, lines 186–209):
```powershell
# Actual parameter mapping:
$params = @{
    Title        = $What             # ✅ Matches docs
    Reason       = $Why              # ✅ Matches docs
    ActionNeeded = $ActionNeeded     # ✅ Matches docs
}
if ($Link)    { $params.BlockerUrl = $Link }        # ⚠️ Docs show "Link" → script maps to "BlockerUrl"
if ($Urgency) { $params.Severity   = $Urgency }    # ⚠️ Docs show "Urgency" → script maps to "Severity"
if ($Agent)   { $params.Agent      = $Agent }      # ✅ Matches
```

**Issue**: Documentation parameter names (-Link, -Urgency) are **public-facing dispatcher parameters**, but the docs show them flowing directly to notify-blocked.ps1 **without renaming**. In reality, the dispatcher **renames** them:
- `-Link` → `-BlockerUrl` (notify-blocked.ps1 param)
- `-Urgency` → `-Severity` (notify-blocked.ps1 param)

**Impact**: 
- If a user copies the docs example exactly, it **still works** because notify-squad-event.ps1 is the entry point
- However, **direct calls to notify-blocked.ps1** would fail if user swaps parameter names (e.g., calling notify-blocked.ps1 with `-Urgency` instead of `-Severity`)
- Docs do show both calling patterns (dispatcher and direct), creating **ambiguity**

**Evidence**:
- Documentation line: ".-Urgency..." (dispatcher param)
- Implementation line 200: `$params.Severity = $Urgency` (actual notify-blocked.ps1 param)

**Recommendation**: Clarify in the Integration Guide that:
1. When using the **dispatcher** (notify-squad-event.ps1): use `-Urgency` and `-Link`
2. When calling **directly** (notify-blocked.ps1): use `-Severity` and `-BlockerUrl`
3. Add a **Parameter Translation Table** showing the dispatcher→caller mappings

---

### ⚠️ Issue #2: Incomplete Reference Section (notification-scheduler.ps1)

**Location**: Reference section, "See Also" subsection (bottom of file)

**Documentation**:
```
- `.squad/skills/squad-notifications/SKILL.md` — Core system design
- `scripts/notify.ps1` — Central notification router
- `scripts/notify-squad-event.ps1` — Dispatcher for common events
- `scripts/notification-recovery.ps1` — Retry/recovery functions
```

**Problem**: Documentation mentions **notification-scheduler.ps1** as a critical dependency in TWO places:

1. **notify-feature-complete.ps1 section** (line ~4 of script):
   > "Calls notify.ps1 through the notification-scheduler for batched delivery"

2. **Feature notification deduplication** (Notification Tiers section):
   > "Feature tier uses the scheduler pipeline"

**But**: The Reference section **does NOT list notify-scheduler.ps1**, leaving readers unable to:
- Understand the scheduler contract
- Debug batching failures
- Verify feature queue logic

**Impact**: Medium — incomplete reference documentation creates a knowledge gap for operational support

**Recommendation**: Add to Reference section:
```
- `scripts/notification-scheduler.ps1` — Batching engine for feature notifications; manages queue flush logic and trigger registration
```

---

## Completeness Assessment

### Coverage Checklist

| Topic | Coverage | Status |
|-------|----------|--------|
| Configuration setup | Complete | ✅ |
| Three notification tiers | Complete | ✅ |
| Event type schemas | Complete (minor omission: errorType field) | ✅ |
| Integration patterns | Complete but ambiguous on parameter mapping | ⚠️ |
| Troubleshooting guide | Comprehensive (7 scenarios) | ✅ |
| State file schema | Complete | ✅ |
| Security guidance | Present (webhook URL safety) | ✅ |
| Non-blocking semantics | Clearly documented | ✅ |
| Reference section | Missing notification-scheduler.ps1 | ⚠️ |

### Troubleshooting Guide Verification

Documentation covers:
1. ✅ Webhook URL not found — Fix clear and actionable
2. ✅ Card too large (28 KB) — Fix clear and actionable
3. ✅ Notification not appearing — 4-step diagnostic (webhook validation, dry-run, error log, network)
4. ✅ Duplicate notifications — Explains re-notify behavior correctly
5. ✅ Feature queue not flushing — Correct flush conditions documented
6. ✅ Script failure after notification — Correctly states non-blocking semantics

**Verdict**: Troubleshooting guide is **comprehensive and accurate**. All major failure modes are covered.

---

## Operational Usability Assessment

✅ **Target audience**: Squad developers and on-call engineers  
✅ **Examples are runnable**: All PowerShell examples follow correct syntax  
✅ **Configuration is clear**: Webhook setup is step-by-step and safe  
✅ **Failure modes are explained**: Readers understand non-blocking semantics  
⚠️ **Parameter mapping needs clarification**: Could confuse users choosing between dispatcher vs. direct calls  

---

## Risk Assessment

| Risk | Severity | Mitigation |
|------|----------|-----------|
| Parameter mapping ambiguity (Issue #1) | Medium | Add parameter translation table to Integration Guide |
| Missing notification-scheduler.ps1 reference (Issue #2) | Medium | Add to Reference section |
| Additional errorType field in events | Low | Not exposed to callers; internal only |

**Overall Risk**: Low-to-Medium. Issues are documentation gaps, not code bugs. Failures would surface quickly in testing/operations.

---

## Summary Table

| Criterion | Status | Notes |
|-----------|--------|-------|
| **Factual Accuracy** | ⚠️ Mostly Accurate | Two parameter mapping ambiguities; one missing reference |
| **Completeness** | ⚠️ Mostly Complete | notification-scheduler.ps1 missing from reference |
| **Operational Clarity** | ✅ Clear | Examples are correct; troubleshooting is comprehensive |
| **Code Examples** | ✅ Verified | All scripts exist; examples match implementations |
| **State Schema** | ✅ Accurate | Verified against actual .squad/notifications-state.json |
| **Non-blocking Semantics** | ✅ Correct | Accurately documented |

---

## Verdict: CHANGES_REQUESTED

**Reason**: Documentation is **high quality and operationally useful**, but **two factual inaccuracies** prevent approval:

1. **Issue #1** (Parameter Mapping): The dispatcher parameter names (-Urgency, -Link) are not clearly distinguished from the underlying script parameters (-Severity, -BlockerUrl). This creates ambiguity for users choosing between calling the dispatcher vs. calling notify-blocked.ps1 directly.

2. **Issue #2** (Missing Reference): The documentation references notification-scheduler.ps1 as a critical dependency but does not document it in the Reference section, creating a knowledge gap for operational support.

**Required Changes**:
1. Add a "Parameter Translation" subsection to the Integration Guide clarifying dispatcher params vs. caller script params
2. Add `scripts/notification-scheduler.ps1` to the Reference section with a brief description of its role in batching

**Not Required But Recommended**:
- Consider documenting the additional `errorType` field in the Event Schema section (currently omitted but harmless)

---

## Reviewer Notes

- ✅ All four notification scripts exist and function as documented
- ✅ State file schema matches documentation perfectly
- ✅ Dedup logic is correctly explained
- ✅ Troubleshooting guide is comprehensive and accurate
- ✅ Non-blocking semantics are clearly communicated
- ⚠️ Integration section needs parameter mapping clarification
- ⚠️ Reference section is incomplete (notification-scheduler.ps1 missing)

---

**Approval Path**: Resubmit with changes to Issue #1 and Issue #2 above.

**Co-authored-by**: Copilot <223556219+Copilot@users.noreply.github.com>
