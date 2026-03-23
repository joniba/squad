# Notifications Status Assessment — Honest Gap Analysis

**Date:** 2026-03-25
**Author:** Gandalf (Lead)
**Requested by:** Jonathan
**Trigger:** "I do not understand the current status. Did we build something we shouldn't have? Is the design faulty? What's wrong with it? What did we miss? Start from the beginning."

---

## 1. What Existed Before We Started

Jonathan already had a working notification system. It's simple and effective:

**`scripts/send-teams-notification.ps1`** — a 56-line script that:
- Takes `-Title` and `-Body` parameters
- Reads a webhook URL from `~/.squad/teams-webhook.url`
- Builds an Adaptive Card with a title and body text block
- POSTs it to Teams via `Invoke-RestMethod`

**Active callers (delivering notifications today):**
- `scripts/icm-scan.ps1` (line 286) — calls `send-teams-notification.ps1` when ICM incidents are found
- `scripts/squad-daily-summary.ps1` (line 56) — calls `send-teams-notification.ps1` with a daily squad summary

**Status: Working.** Jonathan was already getting Teams notifications. The webhook was configured. Cards were being delivered. This was not broken.

---

## 2. What the Design Doc Says We Should Build

The approved design (`docs/designs/proactive-notifications.md`) describes a sophisticated upgrade to that simple system:

### Core Value Proposition
> "The human never discovers blocked work by asking — the system pushes it."

### Three-Tier Notification System
- 🔴 **Urgent** — livesite, script failures, agent escalations → immediate
- 🟡 **Action** — PRs to review, design approvals, setup tasks → immediate
- 🔵 **Feature** — feature complete summaries → batched hourly

### Key Capabilities Beyond the Old System
1. **Event classification** — route by urgency tier
2. **Deduplication** — watermark state file, don't spam the same event
3. **Feature batching** — collect feature completions, send as hourly digest
4. **Retry with backoff** — exponential backoff on webhook failures (2s, 4s, 8s, 16s)
5. **Structured event schema** — typed events with standard fields per tier

### Rollout Plan (explicitly stated in the design doc)
| Phase | Scope | Status |
|-------|-------|--------|
| **Phase 0** | Build `notify.ps1` — router, formatting, dedup, delivery | ✅ Done |
| **Phase 1** | Wire `squad-scheduler.ps1` to call `notify.ps1` on task failure | ❌ Never started |
| **Phase 2** | Wire Galadriel (PR review), failure recovery (escalation), ADO PR assignment | ❌ Never started |
| **Phase 3** | Documentation, user guide, launch | ⚠️ Partially done (guide written, no production use) |

---

## 3. What We Actually Built

Three PRs merged (#122, #125, #127):

### `scripts/notify.ps1` (563 lines) — PR #122
Central notification router. Classifies events by tier, deduplicates via watermark state, formats tier-specific Adaptive Cards, delivers via webhook with exponential backoff. Supports `-DryRun` and `-Force` flags. Well-structured, well-commented.

### `scripts/notification-recovery.ps1` (429 lines) — PR #125
Dead letter queue, retry scheduler, health check, cleanup. Persists failed notifications to `~/.squad/notifications/dead-letter/`, retries with escalating backoff (5m, 15m, 1h), marks permanent failures after 3 cycles, provides `Test-NotificationHealth` for diagnostics.

### `scripts/notification-scheduler.ps1` (483 lines) — PR #127
Event-driven trigger system. Maps event names to notification triggers with templates (ICM urgent, PR review, build failure, feature complete, Ralph round). Hot-reloads config, transforms raw event data into `notify.ps1`-compatible payloads.

### Supporting Artifacts
- `tests/notify.Tests.ps1` — unit tests for card formatting, dedup, state management, batching, validation
- `tests/notification-scheduler.Tests.ps1` — unit tests for triggers, config, template pipeline
- `docs/guides/notifications-guide.md` — user-facing guide by Bilbo (setup, usage, troubleshooting)
- `docs/designs/notifications-tamir-xref.md` — cross-reference analysis validating the design against Tamir Dresher's patterns

**Total: ~1,475 lines of PowerShell + ~20KB of documentation.**

**Number of production callers: zero.**

---

## 4. The Gap

### What was delivered vs. what was needed

```
DESIGN DOC SAYS                          REALITY
─────────────────────────────────────    ─────────────────────────────────────
Phase 0: Build notify.ps1               ✅ Built (and then some)
Phase 1: Wire squad-scheduler           ❌ Zero lines of integration code
Phase 2: Wire agents & pipelines        ❌ Zero lines of integration code
Phase 3: Documentation & launch         ⚠️ Docs written, but for a system
                                            nobody uses

Migration of existing callers:
  icm-scan.ps1 → notify.ps1             ❌ Still calls send-teams-notification.ps1
  squad-daily-summary.ps1 → notify.ps1  ❌ Still calls send-teams-notification.ps1
```

### Two parallel systems now coexist

| | Old System | New System |
|---|---|---|
| **Script** | `send-teams-notification.ps1` | `notify.ps1` + recovery + scheduler |
| **Lines of code** | 56 | ~1,475 |
| **Production callers** | 2 (`icm-scan`, `squad-daily-summary`) | 0 |
| **Actually delivering notifications** | Yes | No |
| **Dedup, retry, tiering** | No | Yes (but nobody uses it) |

---

## 5. Root Cause Analysis — Where Did We Go Wrong?

### Root Cause #1: We confused building a library with delivering a feature

We shipped Phase 0 three times over — the core router, then recovery infrastructure, then a scheduler framework — but never started the one thing that makes it real: wiring a caller. We over-invested in sophistication (dead letter queues, health checks, trigger registries) and under-invested in the minimum viable integration: making `icm-scan.ps1` call `notify.ps1` instead of `send-teams-notification.ps1`.

**This is the primary failure.** Everything else is downstream.

### Root Cause #2: The design doc was good, but no one tracked the phases as separate deliverables

The design doc clearly labels Phase 0, Phase 1, Phase 2, Phase 3 with specific integration points and even code samples. But these were never turned into tracked issues. Gimli built Phase 0, declared success, and moved on. Nobody asked "what about Phase 1?" because nobody was tracking it.

### Root Cause #3: I (Gandalf) declared the track complete without verifying end-to-end delivery

I approved the design. I reviewed the PRs. I should have asked: "Did Jonathan get a notification from this new system?" I didn't. I accepted PR-merged as feature-complete. That's on me.

### Root Cause #4: PR review didn't catch the orphaned code

Three PRs were reviewed and approved — each adding hundreds of lines of library code with zero production callers. The reviewer (Galadriel) should have asked: "Who calls this?" Library code with no callers outside test files is dead code until proven otherwise.

### Root Cause #5: The existing system's existence masked the problem

Because `send-teams-notification.ps1` was already working and Jonathan was already getting notifications, nobody noticed the new system wasn't connected. The old system silently covered for the new system's absence. If there had been NO existing notification system, the gap would have been immediately obvious.

---

## 6. Is the Design Faulty?

**No. The design is sound.** Let me be specific:

### What's good about the design:
- **Three-tier classification** — real value over the flat old system
- **Dedup with watermark** — prevents notification spam, critical for automated senders
- **Feature batching** — smart for high-volume environments
- **Exponential backoff** — proper resilience pattern
- **Structured event schema** — enables type-safe integration across agents
- **Cross-referenced against Tamir's patterns** — validated against production experience

### What the design got wrong (minor):
- **No explicit migration plan for existing callers.** The design references `send-teams-notification.ps1` (line 675) but never says "migrate icm-scan and squad-daily-summary to the new system in Phase 1." The integration points at lines 336–405 are all *new* callers, not migrations of existing ones.
- **Phase 0 was too large.** It should have been "Build `notify.ps1` AND wire one caller" — proving end-to-end in the same phase. Phase 0 as written was pure library work with no observable effect.

### Was notify.ps1 redundant?

Partially. The old system (`send-teams-notification.ps1`) could have been *extended* rather than replaced:
- Add a `-Type` parameter for tier classification
- Add dedup via a state file
- Add retry logic

That would have been 100 lines of changes to an existing, working script, vs. 1,475 lines of new code with zero callers.

**But** — the new system genuinely provides capabilities the old one can't (tiered routing, batching, dead letter queues, event-driven scheduling). The question isn't whether the new system is better (it is), but whether we needed all of it before wiring a single caller (we didn't).

---

## 7. What Should We Do Now?

### Option A: Wire the New System (Recommended)

**What:** Keep `notify.ps1` and its infrastructure. Migrate existing callers and add new ones.

**Concrete steps:**
1. Migrate `icm-scan.ps1` from `send-teams-notification.ps1` → `notify.ps1` (use `urgent` tier for sev2+, `action` tier for findings)
2. Migrate `squad-daily-summary.ps1` → `notify.ps1` (use `feature` tier for daily summaries)
3. Wire `squad-scheduler.ps1` to call `notify.ps1` on task failure (the design's Phase 1)
4. Fire a test notification to prove end-to-end delivery
5. Deprecate `send-teams-notification.ps1` (keep for 2 weeks, then remove)

**Effort:** ~2-3 hours of Gimli's time. The integration code samples are already in the design doc.

**Pros:** We get value from the 1,475 lines we already wrote. Dedup, retry, tiering all become real.

**Cons:** We carry the complexity of the new system (dead letter queues, trigger registry, health checks) that we may never need at current scale.

### Option B: Simplify and Extend the Old System

**What:** Scrap `notify.ps1`, `notification-recovery.ps1`, `notification-scheduler.ps1`. Add dedup and retry to `send-teams-notification.ps1`. Add a `-Type` parameter for tier classification.

**Effort:** ~2 hours to add features to the old script. Another hour to delete the new scripts and their tests.

**Pros:** Simpler. One system instead of two. No orphaned code.

**Cons:** Loses batching, dead letter queues, event scheduling, health checks. These are real capabilities we might want as the squad grows. Also wastes the 3 PRs of work we already did.

### Option C: Hybrid — Wire Minimum, Defer the Rest

**What:** Migrate just `icm-scan.ps1` to use `notify.ps1`. Leave `squad-daily-summary.ps1` on the old system for now. Don't wire the scheduler or agents yet.

**Effort:** ~1 hour.

**Pros:** Fastest path to proving the new system works. One migration demonstrates value. The rest can follow incrementally.

**Cons:** Still two parallel systems. But at least the new one has a real caller.

---

## 8. My Recommendation

**Option C (Hybrid), then evolve toward Option A.**

Here's why:
1. The fastest way to fix this is to wire ONE caller and prove it works end-to-end. `icm-scan.ps1` is the best candidate — it already generates urgent events (new incidents) that are a perfect fit for the `urgent` tier.
2. Once Jonathan sees a real 🔴 notification from the new system, we've proven the pipeline. Then we migrate `squad-daily-summary.ps1` and add new callers incrementally.
3. We don't delete the sophisticated infrastructure (recovery, scheduler) — we let it earn its keep over time as more callers are added.
4. We track each remaining integration point as a separate issue so this gap never happens again.

---

## 9. Concrete Next Steps

| # | Action | Owner | Priority |
|---|--------|-------|----------|
| 1 | Migrate `icm-scan.ps1` to call `notify.ps1` with `urgent` tier for sev2+ incidents | Gimli | P0 — do this first |
| 2 | Fire a manual test notification to prove end-to-end delivery | Gandalf | P0 — do immediately after #1 |
| 3 | Create tracking issues for remaining integration points (scheduler, agents, daily summary) | Gandalf | P1 |
| 4 | Migrate `squad-daily-summary.ps1` to `notify.ps1` | Gimli | P1 |
| 5 | Wire `squad-scheduler.ps1` failure path to `notify.ps1` (design Phase 1) | Gimli | P2 |
| 6 | Wire agent spawn / coordinator to fire `feature-complete` notifications (design Phase 2) | Gimli + Ralph | P2 |
| 7 | Deprecate and remove `send-teams-notification.ps1` after all callers migrated | Gandalf | P3 |

---

## 10. Lessons for the Team

1. **A feature isn't done until a user can see it work.** Three merged PRs with zero callers is a library, not a feature.
2. **When a design has phases, each phase is a tracked deliverable.** Don't let Phase 1 fall through the cracks.
3. **When replacing an existing system, migrate at least one caller in the same PR.** Otherwise the old system keeps running and the new one is dead code.
4. **PR reviewers: ask "who calls this?"** Library code with no production callers should be flagged, not blocked — but the reviewer should ensure the wiring PR is tracked.
5. **I (Gandalf) need to verify end-to-end before declaring a track complete.** "Did Jonathan get a notification?" is the acceptance test. Not "did the PR merge?"

---

## Summary for Jonathan

**The design is good. The implementation is good. What's missing is the wiring.**

We built a 1,475-line notification pipeline with dedup, retry, tiered routing, dead letter queues, and event scheduling. It's tested and documented. But nobody calls it. Meanwhile, your existing 56-line script (`send-teams-notification.ps1`) keeps doing the actual work.

We didn't build something we shouldn't have — the new system is genuinely better. But we stopped one step short of making it real. The design doc explicitly said "Phase 1: wire the callers." We never did Phase 1.

The fix is straightforward: migrate `icm-scan.ps1` to call the new system, prove it works, then migrate the rest incrementally. Estimated effort: a few hours.

I own this gap. I approved the design, reviewed the PRs, and declared the track complete without verifying that you, Jonathan, could actually see it work. That won't happen again.

— Gandalf
