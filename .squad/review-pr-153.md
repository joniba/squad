# PR #153 Review: [notifications] Investigation completion event

**Reviewer:** Galadriel (Code Reviewer)  
**PR:** #153 | Investigation completion event  
**Verdict:** ✅ **APPROVE**  
**Date:** 2026-03-24

---

## Executive Summary

PR #153 successfully adds a new "investigation-complete" event type to the squad notification pipeline, implementing the third event category as required by issue #151. All acceptance criteria are met:

✅ Investigation-complete added as new event type  
✅ Dual-action card (View Report + View Issue buttons)  
✅ Proper routing through the 3-tier notification pipeline  
✅ GitHub permalink format compliance (remote URL per charter spec)  
✅ Cross-file consistency verified  
✅ PowerShell syntax valid across all 6 modified files  

---

## Findings Table

| Severity | Finding | File | Line(s) | Status |
|----------|---------|------|---------|--------|
| ✅ PASS | PowerShell syntax valid (all files) | All scripts | All | No issues |
| ✅ PASS | Event routing chain complete | notify-squad-event.ps1, notify-investigation-complete.ps1, notification-scheduler.ps1, notify.ps1 | 111, 168-175, 306-315, 337-350 | Correct dispatcher → caller → scheduler flow |
| ✅ PASS | Dual-action card rendering | notify.ps1: Build-InvestigationCompleteCard | 330-357 | View Report + View Issue buttons implemented |
| ✅ PASS | Mixed batch handling | notify.ps1: Build-BatchFeatureCard | 359-412 | Correctly detects investigation events via icmNumber field and branches rendering/actions |
| ✅ PASS | GitHub permalink format compliance | aragorn/charter.md, notify-investigation-complete.ps1 | charter line 150, examples lines 36-38 | Remote URL format enforced: https://github.com/jbenami_microsoft/ms-pa/blob/main/docs/investigations/icm-{number}/{filename} |
| ✅ PASS | Parameter validation | notify-squad-event.ps1 | 168-175 | All 5 mandatory params (IcmNumber, Title, Conclusion, ReportUrl, IssueNumber) validated via Assert-Param |
| ✅ PASS | Field mapping consistency | notify.ps1: Add-ToFeatureQueue | 501-517 | Investigation fields (icmNumber, conclusion, reportUrl, issueNumber) properly passed through feature queue |
| ✅ PASS | Coalescing operator usage | notify.ps1: Add-ToFeatureQueue, Build-EventFromTemplate | 505-506, 341-345 | Correct fallback logic: featureTitle uses ?? $Event.title for backward compatibility |
| ✅ PASS | Event ID generation | notification-scheduler.ps1: Build-EventFromTemplate | 341 | Format: investigation:{IcmNumber}:{timestamp} correctly structured |
| ✅ PASS | Trigger registration | notification-scheduler.ps1: Initialize-DefaultTriggers | 468 | investigation-complete registered with Tier="feature" (hourly batching or ≥5 items) |
| ✅ PASS | Documentation completeness | aragorn/charter.md, routing.md | charter 139-150, routing 82-83 | Spec includes parameters, URL format requirement, example command, dual-button description |
| ✅ PASS | Routing update | routing.md | Line 82 | Event count updated: "two categories" → "three categories" |

---

## Detailed Analysis

### 1. Event Routing Architecture ✅

The PR implements proper integration with the 3-tier notification pipeline:

**Tier 1: Dispatcher** (notify-squad-event.ps1)
- ValidateSet updated: "feature-complete", "blocked", "investigation-complete" (line 111) ✅
- Parameter block added for 5 mandatory params (lines 132-138) ✅
- Routing case added: dispatcher validates params and calls notify-investigation-complete.ps1 (lines 306-315) ✅

**Tier 2: Caller** (notify-investigation-complete.ps1 — NEW)
- 104-line standalone script properly structured ✅
- Parameter validation: All 5 params marked [Parameter(Mandatory)] ✅
- Error handling: $ErrorActionPreference = "Stop", proper exit codes ✅
- Dot-sources notification-scheduler.ps1 correctly (line 69) ✅
- Invokes Invoke-ScheduledNotification with event data (lines 83-89) ✅
- Non-blocking failure handling: notification failure does not block investigation completion (line 101) ✅

**Tier 3: Scheduler** (notification-scheduler.ps1)
- Build-EventFromTemplate case for investigation-complete added (lines 339-352) ✅
  - eventId format: investigation:{IcmNumber}:{timestamp} ✅
  - Field mapping: icmNumber, conclusion, reportUrl, issueNumber all passed through ✅
  - Coalescing operators used for optional field fallbacks (featureTitle, summary) ✅
- Trigger registered in Initialize-DefaultTriggers (line 468) ✅
  - EventType: "investigation" (matches case name pattern) ✅
  - Tier: "feature" (hourly batching or ≥5 items, consistent with feature-complete) ✅

### 2. Card Rendering (Dual-Action) ✅

**Build-InvestigationCompleteCard** (NEW, lines 330-357 in notify.ps1)
- Header: 🔍 icon + title (consistent with charter spec) ✅
- Facts block: IcM number + conclusion displayed (lines 344-346) ✅
- Actions: Two buttons rendered conditionally (lines 348-353):
  - "View Report" → ReportUrl ✅
  - "View Issue" → GitHub issues URL derived from IssueNumber ✅

**Build-BatchFeatureCard** (MODIFIED, lines 359-412)
- Event detection: if ($feat.icmNumber) branch logic correctly identifies investigation events (line 363) ✅
  - Investigation events don't have testInstructions; feature events do — good implicit discriminator ✅
- Investigation rendering branch (lines 364-373):
  - Renders 🔍 icon instead of 🔵 ✅
  - Displays FactSet with IcM + Verdict ✅
- Feature rendering branch (lines 374-380): unchanged legacy logic ✅
- Action building (lines 382-402):
  - Investigation-first logic: Builds View Report + View Issue buttons (lines 386-392) ✅
  - Feature fallback: Builds View Issues/Details buttons (lines 393-402) ✅
  - Proper fallback chain: issuesUrl → reportUrl → none ✅

### 3. GitHub Permalink Format Compliance ✅

**Charter Specification** (aragorn/charter.md, line 150):
```
Format: https://github.com/jbenami_microsoft/ms-pa/blob/main/docs/investigations/icm-{number}/{report-filename}
```

**Compliance Verification:**
- Spec explicitly states: "GitHub permalink to the remote (not a local file path)" ✅
- Example in charter (line 148): Full GitHub URL with blob/main path ✅
- Example in caller documentation (notify-investigation-complete.ps1, line 37): Same format ✅
- Dispatcher documentation (notify-squad-event.ps1, line 76): Format documented ✅
- Routing documentation (routing.md, line 83): Format documented as "<GitHub permalink to investigation doc>" ✅

No local paths observed in diff. ReportUrl is passed through as-is from caller to card rendering (no path manipulation). ✅

### 4. PowerShell Syntax Validation ✅

All 6 modified files pass syntax validation:
```
✅ notify-squad-event.ps1 — valid
✅ notify-investigation-complete.ps1 — valid
✅ notification-scheduler.ps1 — valid
✅ notify.ps1 — valid
✅ aragorn/charter.md — (documentation, no syntax check)
✅ routing.md — (documentation, no syntax check)
```

Key patterns verified:
- Parameter binding: Mandatory params marked correctly (all 4 scripts) ✅
- ValidateSet attribute: Updated to include investigation-complete ✅
- Backtick continuation: Used correctly for multi-line statements ✅
- Hashtable construction: @{ key = value } syntax consistent ✅
- Coalescing operators: ?? used for optional field fallbacks ✅
- Dot-sourcing: . $schedulerScript pattern correct ✅

### 5. Parameter Consistency ✅

Parameter naming and passing verified across pipeline:

| Parameter | Caller | Dispatcher | Scheduler | Card |
|-----------|--------|-----------|-----------|------|
| IcmNumber | ✅ | ✅ | ✅ (icmNumber in data) | ✅ |
| Title | ✅ | ✅ | ✅ (title in data) | ✅ |
| Conclusion | ✅ | ✅ | ✅ (conclusion in data) | ✅ |
| ReportUrl | ✅ | ✅ | ✅ (reportUrl in data) | ✅ (card action) |
| IssueNumber | ✅ | ✅ | ✅ (issueNumber in data) | ✅ (derived URL) |

Add-ToFeatureQueue field mapping (lines 501-517): All investigation fields properly included in queue entry ✅

### 6. Cross-File Consistency ✅

- Routing documentation updated: "two categories" → "three categories" (routing.md, line 82) ✅
- Event count in both aragorn/charter.md and routing.md consistent ✅
- Parameter names use consistent casing: PascalCase in parameter blocks, camelCase in data dictionaries ✅
- Icon consistency: 🔍 used for investigation events everywhere (charter, code, routing) ✅
- Tier consistency: investigation-complete uses "feature" tier (hourly batching) like feature-complete ✅

### 7. Backward Compatibility ✅

- No breaking changes to existing feature-complete or blocked event types ✅
- Add-ToFeatureQueue uses coalescing operators (??) to support both title and featureTitle ✅
- Build-BatchFeatureCard branching doesn't affect feature-complete event rendering ✅
- Old scripts (send-teams-notification.ps1, squad-daily-summary.ps1) remain untouched ✅

---

## Acceptance Criteria Checklist

From issue #151 and PR #153 requirements:

- [x] **Investigation-complete added as 3rd event type** — Implemented in all 4 scripts (dispatcher, caller, scheduler, templates)
- [x] **Dual-action card (View Report + View Issue)** — Build-InvestigationCompleteCard and Build-BatchFeatureCard branching working
- [x] **IcM number passed through pipeline** — Validated at dispatcher, routed through scheduler, rendered in facts block
- [x] **Conclusion displayed in card** — Rendered as "Verdict" fact in card
- [x] **GitHub permalink format compliance** — Remote URL enforced per charter spec (not local paths)
- [x] **Proper routing integration** — All 3 tiers of pipeline correctly chain investigation-complete events
- [x] **PowerShell syntax valid** — All scripts pass parse validation
- [x] **No breaking changes** — Existing event types unaffected, backward compatibility maintained
- [x] **Documentation complete** — Charter, routing.md, and inline examples updated

---

## Risk Assessment

**Critical Issues:** None  
**High Issues:** None  
**Medium Issues:** None  
**Low Issues:** None  

All findings are passing/clean. PR is ready to merge.

---

## Verdict

### ✅ **APPROVE**

This PR successfully implements the investigation-complete event as the third notification category, with proper dual-action card rendering, GitHub permalink format compliance, and full integration into the 3-tier notification pipeline. All acceptance criteria are met. No critical or high-severity findings. Code is syntactically valid, cross-file consistency verified, and backward compatibility maintained.

**Confidence:** High (12/12 findings passing, comprehensive review of all 6 changed files, event chain traced end-to-end)

---

## Post-Merge Action Items (for Coordinator)

1. Merge PR #153 to main (Gandalf merge gate applies: consult Gandalf for merge ordering if other PRs pending)
2. After merge, fire test investigation-complete notification to verify card renders correctly in Teams
3. Close issue #151 (Wire Teams notification for investigation completion)

---

**Reviewed by:** Galadriel  
**Methodology:** Context Loading → Deep Analysis (event routing chain, card rendering, parameter consistency, GitHub URL format) → Structured Reporting  
**Review Date:** 2026-03-24  
**PR Status:** Clean merge, ready for approval
