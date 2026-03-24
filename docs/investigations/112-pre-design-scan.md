# Pre-Design Scan: Issue #112 — Proactive Teams Notifications

**Date:** 2026-03-25  
**Author:** Gandalf (Lead)  
**Requested by:** Jonathan  
**Purpose:** Gap analysis before design work begins. This is NOT the design — it's the research that informs it.

---

## 1. Design Doc Status

### Does `docs/designs/human-attention-notifications.md` exist?

**No.** The file referenced in issue #112's deliverables (`docs/designs/human-attention-notifications.md`) was never created under that exact name.

### What DOES exist?

The design work was done — but under different filenames and spread across four documents:

| Document | Purpose | Lines |
|----------|---------|-------|
| `docs/designs/proactive-notifications.md` | Full design doc (three-tier system, event schemas, integration points, rollout phases) | ~420+ lines |
| `docs/designs/notifications-mvp-plan.md` | MVP plan with two triggers: feature-complete + blocked-on-human | ~414 lines |
| `docs/designs/notifications-status-assessment.md` | Honest gap analysis / post-mortem (2026-03-25) | ~257 lines |
| `docs/designs/notifications-tamir-xref.md` | Cross-reference validation against Tamir Dresher's patterns | ~205 lines |

**Gap #1:** Issue #112 says deliverable is `docs/designs/human-attention-notifications.md`. That file doesn't exist. The actual design is at `docs/designs/proactive-notifications.md`. This naming mismatch means anyone following #112 literally won't find the design doc.

### Design Doc Gaps

The design doc (`proactive-notifications.md`) is comprehensive and well-structured. However:

1. **No explicit migration plan for existing callers.** The design references `send-teams-notification.ps1` but never says "migrate icm-scan and squad-daily-summary to the new system." Integration points (lines 336–405) are all *new* callers, not migrations.
2. **Phase 0 was too large.** It should have been "build notify.ps1 AND wire one caller." Phase 0 as written was pure library work with no observable effect.
3. **Design status says "Pending Approval" (line 3)** but the implementation is already three PRs deep. Design was implemented without formal approval closure.
4. **Three-tier system (urgent/action/feature) may be over-scoped for MVP.** Jonathan's MVP definition only needs two triggers: feature-complete and blocked-on-human. The 🟡 Action tier (PRs awaiting review, design approvals, setup tasks) is explicitly NOT MVP.

---

## 2. Issue/PR Alignment Audit

### 2A. Merged PRs Related to Notifications

| PR | Title | Related Issue | Issue State |
|----|-------|---------------|-------------|
| #122 | `[teams-notifications] Phase 1.1: Webhook & message templates (#115)` | #115 | **OPEN** ⚠️ |
| #125 | `[notifications] Phase 1.2: Failure recovery integration (#116)` | #116 | **OPEN** ⚠️ |
| #127 | `[notifications] Phase 1.3: Scheduler integration (#117)` | #117 | **OPEN** ⚠️ |
| #113 | `Design: Proactive Teams Notifications System (#112)` | #112 | **OPEN** |
| #67 | `feat: Teams notification channel integration (#56)` | #56 (closed) | ✅ |

**Misalignment #1: Three merged PRs with open issues.** PRs #122, #125, #127 are merged but their corresponding issues #115, #116, #117 are still open. Either the issues should be closed (work was done) or the PRs didn't fully address the issues (which is actually the case — see below).

**Misalignment #2: PR titles vs issue titles mismatch.** The PRs say "Phase 1.1/1.2/1.3" but the actual work done was building library code (notify.ps1, notification-recovery.ps1, notification-scheduler.ps1) — not integration. The issue titles say "Integration with failure recovery" (#116) and "Integration with PR/review workflows" (#117), but the merged PRs built infrastructure, not integration. The issues are correctly still open because the integration work was never done.

### 2B. Closed Issues Related to Notifications

| Issue | Title | Status |
|-------|-------|--------|
| #56 | Build: Teams notification channel integration | ✅ Closed (PR #67) |
| #55 | Design: Squad-to-Jonathan notification pipeline via Teams | ✅ Closed |
| #54 | Research: Teams channel integration for squad notifications | ✅ Closed |
| #65 | Research: Adapt Tamir's teams-monitor skill for daily watchdog | ✅ Closed |
| #64 | Build: Teams watchdog using Tamir's single-agent approach | ✅ Closed |
| #27 | [Deep Research] Teams & Outlook Integration (2-way) | ✅ Closed |

No conflicts with #112 scope. The closed issues (#54–#56) represent the original notification channel setup (send-teams-notification.ps1), which predates #112.

### 2C. Open Issues Related to Notifications

| Issue | Title | Owner | State |
|-------|-------|-------|-------|
| #112 | Design: Proactive Teams notifications for human-required actions | Gandalf | OPEN |
| #115 | [teams-notifications] Phase 1.1: Webhook & message templates | Gimli | OPEN |
| #116 | [teams-notifications] Phase 1.2: Integration with failure recovery | Gimli | OPEN |
| #117 | [teams-notifications] Phase 1.3: Integration with PR/review workflows | Gimli | OPEN |
| #120 | [teams-notifications] Documentation & runbooks | Bilbo | OPEN |

**Misalignment #3: No issues exist for the MVP-plan caller scripts.** The MVP plan (`notifications-mvp-plan.md`) defines four new issues:
1. Create `notify-feature-complete.ps1` — **no GitHub issue exists**
2. Create `notify-blocked.ps1` — **no GitHub issue exists**
3. Wire coordinator to call notification scripts — **no GitHub issue exists**
4. End-to-end validation — **no GitHub issue exists**

These were documented in the MVP plan but never filed as actual GitHub issues.

---

## 3. Codebase Scan — What Exists for Teams Notifications

### 3A. Scripts Inventory

#### Old System (WORKING — 2 production callers)

| Script | Lines | Callers | Status |
|--------|-------|---------|--------|
| `scripts/send-teams-notification.ps1` | 50 | `icm-scan.ps1` (line 286), `squad-daily-summary.ps1` (line 56) | ✅ Active, delivering notifications |

#### New System (ORPHANED — 0 production callers)

| Script | Lines | Callers | Status |
|--------|-------|---------|--------|
| `scripts/notify.ps1` | 490 | **None** | ❌ Dead code |
| `scripts/notification-recovery.ps1` | 364 | **None** | ❌ Dead code |
| `scripts/notification-scheduler.ps1` | 414 | **None** | ❌ Dead code |

**Total new system: 1,268 lines with zero production callers.**

#### MVP Caller Scripts (PLANNED but NOT BUILT)

| Script | Status |
|--------|--------|
| `scripts/notify-feature-complete.ps1` | ❌ Does not exist |
| `scripts/notify-blocked.ps1` | ❌ Does not exist |

### 3B. Test Files

| Test | Lines |
|------|-------|
| `tests/notify.Tests.ps1` | Exists |
| `tests/notification-recovery.Tests.ps1` | Exists |
| `tests/notification-scheduler.Tests.ps1` | Exists |

### 3C. Webhook Configuration

- **Storage:** `~/.squad/teams-webhook.url` (file-based, not git-tracked)
- **Convention:** One URL per line, no trailing whitespace
- **Env vars:** None. All scripts read from the file. No `TEAMS_WEBHOOK` env var pattern.
- **Security:** File-based, user-home directory, never committed to git

### 3D. Existing Notification Triggers in failure-recovery.md

`.squad/failure-recovery.md` (lines 57–67) defines notification rules:

| Condition | Notify Jonathan? |
|-----------|-----------------|
| Elrond can't find solution | ✅ Yes — Teams webhook + `needs-human` label |
| Gandalf rejects Elrond 2x | ✅ Yes — escalate |
| Fix implemented but retry fails | ✅ Yes |
| Aragorn blocked during livesite | ✅ Yes — immediate bypass |

**Gap:** The failure-recovery doc says "Post to Teams webhook" but doesn't specify WHICH script to call. It predates both `send-teams-notification.ps1` and `notify.ps1`. Nobody has wired these triggers to either notification system.

### 3E. Skill Files

- `.squad/skills/squad-notifications/SKILL.md` — Documents the notification channel, webhook setup, message format, when-to-notify criteria. References `scripts/send-teams-notification.ps1` (the old system). Does not mention `notify.ps1` (the new system).
- `.squad/skills/teams-watchdog/SKILL.md` — Teams watchdog skill (separate concern — reading Teams messages, not sending them).

### 3F. Documentation

- `docs/guides/notifications-guide.md` — User-facing guide for the new system (references `notify.ps1`)
- `docs/guides/teams-notifications-setup.md` — Setup guide for Teams webhook
- `NOTIFICATIONS_ANALYSIS_SUMMARY.txt` — Root-level summary of the cross-reference analysis

---

## 4. MVP Scope Assessment

### Jonathan's MVP Definition

| | In MVP | Not MVP |
|---|---|---|
| ✅ | Teams notifications for BIG FEATURES completed (concise summaries + links) | Everything else from #112 |
| ✅ | Teams notifications when ANYTHING is blocked and requires urgent human attention | |

### Does the current plan align?

**The MVP plan (`notifications-mvp-plan.md`) aligns well with Jonathan's definition.** It defines exactly two triggers:

1. 🔵 **Feature Complete** — batched hourly, concise summary + test instructions + links
2. 🔴 **Blocked on Human** — immediate, what's blocked + why + what Jonathan needs to do

### What's in scope that SHOULDN'T be (for MVP)?

The full design doc (`proactive-notifications.md`) includes a 🟡 **Action Needed** tier (PRs awaiting review, design approvals, setup tasks, ADO PR assignments). **This is NOT MVP per Jonathan's definition.** The MVP plan correctly excludes it, but the open issues don't reflect this:

- **Issue #117** (`Phase 1.3: Integration with PR/review workflows`) is 🟡 Action tier work. This should be labeled as post-MVP or deprioritized.
- **Issue #120** (`Documentation & runbooks`) includes documenting the full three-tier system. For MVP, documentation should cover only the two MVP triggers.

### What's MISSING from current issues for MVP?

| Missing Item | Why It Matters |
|--------------|----------------|
| GitHub issue for `notify-feature-complete.ps1` | No tracked work item for the first MVP caller script |
| GitHub issue for `notify-blocked.ps1` | No tracked work item for the second MVP caller script |
| GitHub issue for coordinator wiring | The coordinator template needs to be updated to call these scripts — this is the critical "last mile" that was missed last time |
| GitHub issue for E2E validation | The post-mortem lesson was "feature complete = user-observable effect." Validation should be a tracked deliverable |
| Migration plan for old callers | `icm-scan.ps1` and `squad-daily-summary.ps1` still call the old system. No tracked plan to migrate (though MVP plan says to leave them untouched for now, which is fine) |

### The Three-System Problem

There are now effectively THREE notification approaches:

1. **Old system** (`send-teams-notification.ps1`) — 50 lines, 2 callers, working
2. **New system** (`notify.ps1` + recovery + scheduler) — 1,268 lines, 0 callers, orphaned
3. **MVP plan** (`notify-feature-complete.ps1` + `notify-blocked.ps1`) — 0 lines, not built, calls new system

The MVP plan correctly builds ON TOP of the new system (the caller scripts invoke `notify.ps1`). But if the new system itself has issues, the MVP inherits them. The new system has never been tested in production.

---

## 5. Recommendations

### Before Design Work Starts

#### R1. Fix the Design Doc Naming (5 min)
Issue #112 says the deliverable is `docs/designs/human-attention-notifications.md`. The actual design is at `docs/designs/proactive-notifications.md`. Either:
- Rename the file to match the issue, OR
- Update issue #112's body to reference the correct filename

#### R2. Close or Relabel Issues #115, #116, #117 (10 min)
These issues say "Phase 1.1/1.2/1.3" but the PRs that reference them (#122, #125, #127) only built library code, not integration. Options:
- **Close** #115 as "completed" (webhook + templates ARE built) and create a NEW issue for wiring callers
- **Relabel** #116 and #117 to clarify they need integration work, not just library code
- **Keep #117 open but deprioritize** — it's 🟡 Action tier work, not MVP

#### R3. Create GitHub Issues for MVP Caller Scripts (15 min)
File the four issues defined in `notifications-mvp-plan.md`:
1. `feat(notifications): add notify-feature-complete.ps1`
2. `feat(notifications): add notify-blocked.ps1`
3. `feat(notifications): wire coordinator to call notification scripts`
4. `test(notifications): end-to-end dry-run validation`

These are the ACTUAL MVP deliverables. Without tracked issues, they'll fall through the cracks — exactly what happened with Phase 1/2 last time.

#### R4. Validate the New System Works at All (30 min)
`notify.ps1` has never been called in production. Before building caller scripts that depend on it, someone should:
```powershell
# Dry-run test
.\scripts\notify.ps1 -Type "urgent" -Event @{
    eventId = "test:gandalf:$(Get-Date -Format 'yyyy-MM-ddTHH-mm-ss')"
    errorType = "test"
    title = "🔴 Test notification — ignore"
    reason = "Validating notify.ps1 works before MVP build"
    actionUrl = "https://github.com/jbenami_microsoft/ms-pa/issues/112"
    actionLabel = "View Issue"
} -DryRun -Force
```
If dry-run works, send one real notification to confirm webhook delivery.

#### R5. Scope Decision: 🟡 Action Tier is NOT MVP (5 min)
Explicitly label issue #117 (`PR/review workflows`) as post-MVP. Jonathan's MVP is two triggers only: feature-complete and blocked-on-human. Don't let scope creep pull in the Action tier.

#### R6. Update `.squad/skills/squad-notifications/SKILL.md` (10 min)
The skill file only references `send-teams-notification.ps1` (old system). It should also reference `notify.ps1` (new system) and the planned MVP caller scripts. Agents reading this skill will only know about the old system otherwise.

#### R7. Wire `failure-recovery.md` to Specific Script (5 min)
`.squad/failure-recovery.md` line 69 says "Post to Teams webhook" but doesn't specify which script. Update to reference `notify-blocked.ps1` (once built) for escalation scenarios.

### Priority Order

| # | Action | Effort | Impact |
|---|--------|--------|--------|
| 1 | R3: Create MVP issues | 15 min | HIGH — prevents the same "unwired" failure |
| 2 | R4: Validate notify.ps1 works | 30 min | HIGH — confirms foundation before building on it |
| 3 | R5: Scope decision on Action tier | 5 min | MED — prevents scope creep |
| 4 | R1: Fix design doc naming | 5 min | LOW — housekeeping |
| 5 | R2: Close/relabel phase issues | 10 min | MED — reduces confusion |
| 6 | R6: Update SKILL.md | 10 min | MED — agent awareness |
| 7 | R7: Wire failure-recovery.md | 5 min | LOW — can wait for MVP build |

---

## Appendix: File Index

| File | Role |
|------|------|
| `docs/designs/proactive-notifications.md` | Full design doc (the actual #112 deliverable) |
| `docs/designs/notifications-mvp-plan.md` | MVP plan with two triggers |
| `docs/designs/notifications-status-assessment.md` | Post-mortem gap analysis |
| `docs/designs/notifications-tamir-xref.md` | Design validation against Tamir's patterns |
| `scripts/send-teams-notification.ps1` | Old system (50 lines, 2 callers, working) |
| `scripts/notify.ps1` | New system router (490 lines, 0 callers) |
| `scripts/notification-recovery.ps1` | New system recovery (364 lines, 0 callers) |
| `scripts/notification-scheduler.ps1` | New system scheduler (414 lines, 0 callers) |
| `scripts/icm-scan.ps1:286` | Calls old system for ICM notifications |
| `scripts/squad-daily-summary.ps1:56` | Calls old system for daily summary |
| `.squad/failure-recovery.md:57-67` | Notification rules (not wired to any script) |
| `.squad/skills/squad-notifications/SKILL.md` | Skill file (references old system only) |
| `tests/notify.Tests.ps1` | Tests for new system |
| `tests/notification-recovery.Tests.ps1` | Tests for recovery module |
| `tests/notification-scheduler.Tests.ps1` | Tests for scheduler module |

---

*— Gandalf, Lead*
