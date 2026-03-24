# Notifications System — Task Plan

**Author:** Gandalf (Lead)  
**Requested by:** Jonathan  
**Date:** 2026-03-28  
**Scope:** End-to-end review + production-readiness plan

---

## 1. System Status

### What Exists

| Component | File | Status |
|-----------|------|--------|
| Core router | `scripts/notify.ps1` | ✅ Built — dedup, retry, Adaptive Cards, 3-tier routing |
| Recovery/DLQ | `scripts/notification-recovery.ps1` | ✅ Built — dead-letter queue, retry, health checks |
| Scheduler | `scripts/notification-scheduler.ps1` | ✅ Built — trigger registry, templates, hot-reload config |
| Blocked caller | `scripts/notify-blocked.ps1` | ✅ Built — urgent tier, immediate delivery |
| Feature-complete caller | `scripts/notify-feature-complete.ps1` | ✅ Built — feature tier, batched via scheduler |
| Dispatcher | `scripts/notify-squad-event.ps1` | ✅ Built — single entry point for coordinator |
| Old system | `scripts/send-teams-notification.ps1` | ✅ Active — 2 callers (icm-scan, daily-summary) |
| Tests | `tests/notify-*.Tests.ps1` (3 files) | ✅ 43/43 passing |
| Skill doc | `.squad/skills/squad-notifications/SKILL.md` | ✅ Documents both systems |
| Design doc | `docs/designs/human-attention-notifications.md` | ✅ Approved design |
| Routing rule | `.squad/routing.md` Rule 16 | ⚠️ Wrong params (fixed in this commit) |

### What Works

- All 3 caller scripts (`notify-blocked`, `notify-feature-complete`, `notify-squad-event`) produce valid Adaptive Cards in `-DryRun` mode
- Deduplication logic handles all 3 tiers (urgent: re-notify after 3 errors or 6h; action: state-change or 48h; feature: never re-send)
- Feature batching queues items and flushes at ≥5 items or ≥1h since last batch
- Exponential backoff retry (2s, 4s, 8s, 16s) with dead-letter fallback
- 28KB payload limit enforced with graceful fallback card
- Scheduler has 6 built-in trigger templates (icm-urgent, icm-action, pr-review, build-failure, feature-complete, ralph-round)

### What Does NOT Work (Production Gaps)

- **Zero production callers.** No script, agent, or coordinator actually calls `notify-squad-event.ps1` during real work. The new system has never sent a real Teams notification.
- **Rule 16 uses wrong params** (fixed below).
- **No `action` tier callers exist.** Design defines 🟡 Action Needed (PR reviews, design reviews) but no caller script or wiring exists.
- **IcM investigation completions not wired.** Aragorn's `icm-scan.ps1` still uses the old system. No bridge to the new scheduler triggers.
- **No "notify me about X" persistence.** Jonathan can't say "watch issue #42" and have it trigger a notification on state change.
- **Daily summary not migrated.** `squad-daily-summary.ps1` still calls old `send-teams-notification.ps1`.

---

## 2. Parameter Mismatch — Rule 16

### Problem

Rule 16 in `.squad/routing.md` tells the coordinator to call `notify-squad-event.ps1` with parameters `-What` and `-Why`:

```
-Event "feature-complete" -What "<summary>" -Why "<details>" -Link "<url>" -Agent "<agent>"
```

But `notify-squad-event.ps1` uses different parameters for `feature-complete`:
- `-FeatureName` (not `-What`)
- `-Summary` (not `-Why`)
- `-DocLinks` (not `-Link`)
- No `-Agent` parameter for feature-complete events

The `-What`/`-Why`/`-Link`/`-Agent` params are for the `blocked` event type, not `feature-complete`.

### Fix

Rule 16 updated to show correct params per event type with examples for both `feature-complete` and `blocked`. See the commit in this PR.

---

## 3. Gaps Found

### GAP-1: No Production Callers (Critical)
The new notification system has been built across 3 PRs (#122, #125, #127) plus caller scripts (#132, #134) but **zero** production code calls it. This is the same gap identified in the 2026-03-25 post-mortem.

### GAP-2: No Action-Tier Caller Script
The design defines 🟡 Action Needed notifications (PR reviews ready, design reviews pending, setup tasks). No caller script exists for this tier. The dispatcher only supports `feature-complete` and `blocked`.

### GAP-3: IcM Scan Still Uses Old System
`scripts/icm-scan.ps1` line 286 calls `send-teams-notification.ps1`. The scheduler has `icm-scan-urgent` and `icm-scan-action` trigger templates ready, but icm-scan has not been migrated.

### GAP-4: No Persistent Watch/Subscribe Mechanism
Jonathan cannot say "notify me when issue #42 changes" and have it persist. The system is event-push only — callers must know to fire notifications. No event-subscription model exists.

### GAP-5: Daily Summary Not Migrated
`scripts/squad-daily-summary.ps1` line 56 still calls old system. Not urgent — old system works — but creates tech debt.

### GAP-6: Coordinator Never Actually Calls notify-squad-event.ps1
Rule 16 tells the coordinator to call the dispatcher, but no enforcement mechanism ensures it happens. The coordinator is an LLM agent — it may or may not follow the rule. No integration test validates coordinator → dispatcher → Teams flow.

### GAP-7: Trigger Config Not Bootstrapped
`notification-scheduler.ps1` defines `Initialize-DefaultTriggers` to register 6 triggers, but it writes to `~/.squad/notifications/triggers.json` which doesn't exist until someone runs it. `notify-feature-complete.ps1` calls `Initialize-DefaultTriggers` on every invocation (good), but there's no setup validation that the config directory and file exist.

### GAP-8: No E2E Validation Test
No test sends a real (or webhook-validated) notification through the full pipeline: dispatcher → caller → scheduler → notify.ps1 → Teams. All tests use `-DryRun`.

---

## 4. Task Plan

### P0 — Blocking (Must fix before declaring production-ready)

| # | Task | Owner | Dependencies | Acceptance Criteria |
|---|------|-------|-------------|---------------------|
| T1 | **Fix Rule 16 params in routing.md** | Gandalf | None | Rule 16 shows correct `feature-complete` params (`-FeatureName`, `-Summary`, `-DocLinks`) and correct `blocked` params (`-What`, `-Why`, `-ActionNeeded`, `-Link`). Committed. |
| T2 | **E2E dry-run validation** | Gimli | T1 | A script that calls `notify-squad-event.ps1 -DryRun` for both event types and validates the output contains expected card structure. Run in CI or manually. Document in SKILL.md. |
| T3 | **Send one real notification** | Jonathan + Gimli | T2 | Remove `-DryRun`, send one `blocked` and one `feature-complete` notification to the real Teams channel. Confirm Jonathan sees both cards. Screenshot or confirm in Teams. |
| T4 | **Wire coordinator to actually call dispatcher** | Gandalf | T3 | Coordinator spawning logic includes a post-completion hook that invokes `notify-squad-event.ps1`. Verify by completing a feature and checking Teams. |

### P1 — Needed (Required for production quality)

| # | Task | Owner | Dependencies | Acceptance Criteria |
|---|------|-------|-------------|---------------------|
| T5 | **Create action-tier caller script** (`notify-action.ps1`) | Gimli | T3 | Script accepts PR number, title, reviewer, status. Routes through dispatcher as `action` event. Pester tests pass. |
| T6 | **Add `action` event type to dispatcher** | Gimli | T5 | `notify-squad-event.ps1` accepts `-Event "action"` and routes to `notify-action.ps1`. Tests cover param validation. |
| T7 | **Migrate icm-scan to new system** | Gimli | T3 | `icm-scan.ps1` calls `notify-squad-event.ps1 -Event "blocked"` (for sev2+) instead of `send-teams-notification.ps1`. Old call preserved behind a feature flag or removed with rollback plan. |
| T8 | **Wire Galadriel PR reviews to action notifications** | Gandalf | T6 | After Galadriel completes a review, coordinator fires `notify-squad-event.ps1 -Event "action"` with PR details. Jonathan sees 🟡 card in Teams. |
| T9 | **Batching validation** — confirm non-urgent events aggregate | Gimli | T3 | Test that 3 `feature-complete` calls within 1 minute produce ONE batched card (not 3 separate cards). Confirm with real Teams delivery. |
| T10 | **Update SKILL.md** — document new system as primary | Gandalf + Bilbo | T3 | SKILL.md updated to show new system as ACTIVE with callers listed. Old system marked as LEGACY. |

### P2 — Nice-to-Have (Post-MVP polish)

| # | Task | Owner | Dependencies | Acceptance Criteria |
|---|------|-------|-------------|---------------------|
| T11 | **Persistent watch/subscribe mechanism** | Gimli | T6 | Jonathan can run `.\scripts\notify-watch.ps1 -Issue 42` and get notified when issue state changes. Config persisted in `~/.squad/notifications/watches.json`. |
| T12 | **Migrate daily summary to new system** | Gimli | T7 | `squad-daily-summary.ps1` uses new `notify.ps1` instead of old `send-teams-notification.ps1`. Feature batching optional. |
| T13 | **Notification health dashboard** | Gimli | T3 | Script that reads `notifications-state.json` and `notifications-errors.log` and prints summary: last success, failure count, queue depth. |
| T14 | **IcM investigation completion notification** | Gimli + Aragorn | T7 | When Aragorn completes an IcM investigation, fire `notify-squad-event.ps1 -Event "feature-complete"` with investigation summary. |
| T15 | **Remove old system** | Gimli | T7, T12 | After all callers migrated and validated, remove `send-teams-notification.ps1`. Update all docs. |

---

## 5. Urgency Routing Summary

| Scenario | Tier | Delivery | Script |
|----------|------|----------|--------|
| Blocked on Jonathan | 🔴 Urgent | Immediate | `notify-blocked.ps1` ✅ |
| IcM investigation (sev2+) | 🔴 Urgent | Immediate | Scheduler template exists, **not wired** (GAP-3) |
| Jonathan asked to be notified | 🟡 Action | Immediate | **No mechanism** (GAP-4) |
| PR ready for review | 🟡 Action | Immediate | **No caller script** (GAP-2) |
| Feature completed | 🔵 Feature | Batched (hourly/≥5) | `notify-feature-complete.ps1` ✅ |
| Session summary | 🔵 Feature | Batched | Could use `ralph-round` template ✅ |
| Daily summary | 🔵 Feature | Daily | Old system only (GAP-5) |

---

## 6. Recommendation

**Start with T1–T4 (P0).** Fix the params, validate dry-run, send one real notification, then wire the coordinator. This proves the system works end-to-end.

Then T5–T10 (P1) in order — the action tier and icm-scan migration are the highest-impact items.

T11+ (P2) only after Jonathan confirms the P1 system works and he's satisfied with notification quality/frequency.

**Key principle from the post-mortem:** "Feature complete" means Jonathan sees a Teams card. Merged code with no callers ≠ complete.
