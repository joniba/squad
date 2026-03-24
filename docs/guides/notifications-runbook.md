---
title: Teams Notification System — Runbook & Integration Guide
author: Bilbo
date: 2026-01-24
category: guides
tags: [notifications, teams, integration, runbook, operations]
status: published
---

# Teams Notification System — Runbook & Integration Guide

> Send structured notifications to the squad's Microsoft Teams channel. This runbook covers configuration, event types, usage examples, and troubleshooting.

## Table of Contents

1. [Overview](#overview)
2. [Setup & Configuration](#setup--configuration)
3. [Notification Tiers](#notification-tiers)
4. [Event Types & Examples](#event-types--examples)
5. [Integration Guide](#integration-guide)
6. [Troubleshooting](#troubleshooting)

---

## Overview

The squad notification system delivers **three tiers of notifications** to Jonathan and the team via Microsoft Teams:

| Tier | Symbol | Delivery | Use Case |
|------|--------|----------|----------|
| **Urgent** | 🔴 | Immediate | Livesite issues, script failures, blockers |
| **Action** | 🟡 | Immediate | PR reviews, setup tasks, decisions needed |
| **Feature** | 🔵 | Batched (hourly or ≥5 items) | Feature completions, summaries |

### Key Concepts

- **Non-blocking**: Notification failures do NOT stop agent work. Failures are logged for retry.
- **Deduplication**: Repeated notifications are suppressed per tier (e.g., "don't re-notify about the same blocker for 6 hours").
- **Adaptive Cards**: All notifications use Teams Adaptive Card format for structured, rich presentation.
- **Webhook-based**: Uses Teams Incoming Webhooks — no bot tokens or OAuth needed.

---

## Setup & Configuration

### One-Time: Create the Webhook

1. **Open Microsoft Teams** → your squad's target channel (e.g., `#squad-notifications`)
2. **Click ⋯ (More) → Connectors → Incoming Webhook → Configure**
3. **Name it** (e.g., "Squad Bot") and optionally set an icon
4. **Click Create** and copy the generated URL
5. **Save locally** (never commit to git):

```powershell
$webhookDir = Join-Path $HOME ".squad"
New-Item -Path $webhookDir -ItemType Directory -Force | Out-Null
Set-Content -Path (Join-Path $webhookDir "teams-webhook.url") -Value "<paste URL>"
```

**Result**: Webhook URL stored at `~/.squad/teams-webhook.url` (one URL per line, no trailing whitespace).

### Webhook Security

- **NEVER commit the URL to git** — it's a shared secret
- **User-only permissions** — set file to read-only for your user
- **Rotatable** — if compromised, regenerate in Teams and update the local file
- **Rate limits** — 4 requests/second, 28 KB/message max

---

## Notification Tiers

### 🔴 Urgent (Immediate)

Sent immediately when work is blocked waiting for human input.

**Script**: `scripts/notify-blocked.ps1`

**Triggers**:
- Script failure or timeout
- Livesite incident (Sev2+)
- Agent exhausted all options
- Critical decision needed

**Deduplication**:
- Re-notify after **3+ consecutive errors** OR **6 hours of silence**
- Use `-Force` to skip dedup and send immediately

**Example**:
```powershell
.\scripts\notify-blocked.ps1 `
    -Title "🔴 Blocked: DGrep auth requires VPN" `
    -Reason "SDK dSTS auth requires corp tunnel." `
    -ActionNeeded "Confirm VPN access for the build machine." `
    -BlockerUrl "https://github.com/jbenami_microsoft/ms-pa/issues/42" `
    -Agent "Gimli" `
    -Severity "blocking-feature"
```

---

### 🟡 Action (Immediate)

Sent immediately for high-priority tasks requiring attention within hours.

**Script**: `scripts/notify.ps1 -Type action`

**Triggers**:
- Stale PR (no review activity >24h)
- Design review awaiting approval
- Setup or config task ready
- High-priority code review requested

**Deduplication**:
- Re-notify on **state change** (e.g., PR status changes)
- Re-notify after **48 hours of silence**

**Example**:
```powershell
.\scripts\notify.ps1 -Type action -Event @{
    eventId       = "pr-review:squad/123:2026-01-24"
    title         = "🟡 PR #123 stale — needs review"
    reason        = "No review activity for 36 hours. @Jonathan: 15-min review needed."
    currentState  = "waiting-for-review"
    estimatedTime = "~15 minutes"
    actionUrl     = "https://github.com/jbenami_microsoft/ms-pa/pull/123"
    actionLabel   = "Review PR"
}
```

---

### 🔵 Feature (Batched)

Queued and sent **hourly** or when **5+ items accumulate**.

**Script**: `scripts/notify-feature-complete.ps1`

**Triggers**:
- Feature merged and ready for testing
- Milestone completed
- Batch summary of changes

**Deduplication**:
- Never re-notify the same feature (one-time per feature)
- Queue flushes automatically after 1 hour or when 5 items queued

**Example**:
```powershell
.\scripts\notify-feature-complete.ps1 `
    -FeatureId "dgrep-cli-phase1" `
    -FeatureTitle "DGrep CLI — Phase 1 Foundation" `
    -Summary "CLI arg parsing, config management, and output formatters shipped." `
    -TestInstructions "cd tools/dgrep-cli && dotnet build && dotnet test" `
    -IssuesUrl "https://github.com/jbenami_microsoft/ms-pa/issues/101" `
    -PRList "#122, #125, #127"
```

---

## Event Types & Examples

### Blocked Notification (Urgent Tier)

When work cannot proceed without human decision or action.

```powershell
.\scripts\notify-squad-event.ps1 -Event blocked `
    -What "Elrond: VPN auth exhausted" `
    -Why "Tried 3 workarounds for dSTS; needs network admin approval." `
    -ActionNeeded "Confirm VPN routing for dSTS SDK." `
    -Link "https://github.com/jbenami_microsoft/ms-pa/issues/99" `
    -Urgency "blocking-feature" `
    -Agent "Elrond"
```

**Card Output**:
```
🔴 Elrond: VPN auth exhausted
Tried 3 workarounds for dSTS; needs network admin approval.
Action needed: Confirm VPN routing for dSTS SDK.
Agent: Elrond | Severity: blocking-feature
[View Blocker] →
```

---

### Feature Complete Notification (Feature Tier)

When a feature is ready for testing.

```powershell
.\scripts\notify-squad-event.ps1 -Event feature-complete `
    -FeatureName "Semantic Search Integration" `
    -Summary "Vector embedding pipeline and nearest-neighbor search ops completed." `
    -PRs "#88,#91,#94" `
    -DocLinks "https://docs/semantic-search.md"
```

**Card Output** (batched at bottom of feature summary):
```
🔵 Semantic Search Integration
Vector embedding pipeline and nearest-neighbor search ops completed.
Testing Instructions:
  1. npm run test:semantic
  2. Verify embeddings in /tmp/vectors
  3. Query: dgrep-semantic --query "auth"

PRs: #88, #91, #94
[View Issues] →
```

---

### Livesite Incident (Urgent Tier)

Critical production issue requiring immediate attention.

```powershell
.\scripts\notify-blocked.ps1 `
    -Title "🔴 Sev2: ms-pa builds failing in production" `
    -Reason "CI/CD pipeline down — blocks all deployments." `
    -ActionNeeded "Check GitHub Actions; roll back if necessary." `
    -BlockerUrl "https://github.com/jbenami_microsoft/ms-pa/actions/runs/98765" `
    -Agent "Aragorn" `
    -Severity "livesite" `
    -Force
```

---

### Stale PR (Action Tier)

High-value PR needs review.

```powershell
.\scripts\notify.ps1 -Type action -Event @{
    eventId       = "stale-pr:squad/120:2026-01-24"
    title         = "🟡 PR #120 stale — documentation runbook"
    reason        = "No review activity for 28 hours. Ready to merge after sign-off."
    currentState  = "awaiting-review"
    estimatedTime = "~20 minutes"
    actionUrl     = "https://github.com/jbenami_microsoft/ms-pa/pull/120"
    actionLabel   = "Review & Merge"
} -Force
```

---

## Integration Guide

### Using the Dispatcher (`notify-squad-event.ps1`)

The **dispatcher** routes all notification events to the correct tier and script. Use this for most integrations.

**Supported Events**:
- `feature-complete` — Feature shipped and ready for testing
- `blocked` — Work blocked, needs human attention

#### Parameter Translation (Dispatcher → Underlying Scripts)

The dispatcher renames parameters when calling underlying scripts for clarity. Use the dispatcher parameters in your integrations; the translation happens automatically.

| Event | Dispatcher Parameter | Underlying Script | Underlying Parameter | Notes |
|-------|----------------------|-------------------|----------------------|-------|
| `blocked` | `-What` | `notify-blocked.ps1` | `-Title` | Card header text |
| `blocked` | `-Why` | `notify-blocked.ps1` | `-Reason` | Explanation of blocker |
| `blocked` | `-ActionNeeded` | `notify-blocked.ps1` | `-ActionNeeded` | (unchanged) |
| `blocked` | `-Link` | `notify-blocked.ps1` | `-BlockerUrl` | Issue/PR URL |
| `blocked` | `-Urgency` | `notify-blocked.ps1` | `-Severity` | Severity classification |
| `blocked` | `-Agent` | `notify-blocked.ps1` | `-Agent` | (unchanged) |
| `feature-complete` | `-FeatureName` | `notify-feature-complete.ps1` | `-FeatureTitle` | Feature display name |
| `feature-complete` | `-PRs` | `notify-feature-complete.ps1` | `-PRList` | Normalized from "55,56" to "#55, #56" |
| `feature-complete` | `-DocLinks` | `notify-feature-complete.ps1` | `-IssuesUrl` | Only first link forwarded; multiple links logged as warning |
| `feature-complete` | `-Summary` | `notify-feature-complete.ps1` | `-Summary` | (unchanged) |
| `feature-complete` | `-TestInstructions` | `notify-feature-complete.ps1` | `-TestInstructions` | (unchanged) |
| `feature-complete` | `-FeatureId` | `notify-feature-complete.ps1` | `-FeatureId` | (unchanged) |

**Feature-Complete Example**:
```powershell
.\scripts\notify-squad-event.ps1 `
    -Event "feature-complete" `
    -FeatureName "Auth Flow" `
    -Summary "Token caching and refresh logic implemented." `
    -PRs "#55,#56" `
    -DocLinks "https://docs/auth.md" `
    -TestInstructions "cd auth && npm test"
```

**Blocked Example**:
```powershell
.\scripts\notify-squad-event.ps1 `
    -Event "blocked" `
    -What "dGrep SDK auth" `
    -Why "dSTS requires VPN; build machine has no network access." `
    -ActionNeeded "Provision VPN tunnel or use internal auth." `
    -Link "https://github.com/jbenami_microsoft/ms-pa/issues/88" `
    -Urgency "blocking-feature" `
    -Agent "Gimli"
```

### Direct Integration (Advanced)

For custom notification tiers, use `notify.ps1` directly.

```powershell
# Send to urgent tier with custom event data
.\scripts\notify.ps1 -Type urgent -Event @{
    eventId     = "custom:my-event:$(Get-Date -Format 'yyyy-MM-ddTHH-mm-ss')"
    title       = "🔴 Custom Alert"
    reason      = "Something important happened."
    actionUrl   = "https://link-to-context"
    actionLabel = "View Details"
}

# Dry-run mode (build card but don't send)
.\scripts\notify.ps1 -Type action -Event @{ ... } -DryRun

# Force immediate send (skip deduplication)
.\scripts\notify.ps1 -Type urgent -Event @{ ... } -Force
```

### Integration Points in Squad Scripts

**Existing callers**:
- `icm-scan.ps1` — Sends Sev2+ incident alerts (old system)
- `squad-daily-summary.ps1` — Sends daily status (old system)

**New callers** (pending implementation):
- `notification-recovery.ps1` — Retry/recovery for failed deliveries
- `notification-scheduler.ps1` — Scheduled batching of feature notifications

---

## Troubleshooting

### Webhook URL Not Found

**Symptom**: `Warning: Webhook URL file not found: ~/.squad/teams-webhook.url`

**Fix**:
1. Confirm the file exists: `Test-Path $HOME\.squad\teams-webhook.url`
2. If missing, regenerate via [Setup & Configuration](#setup--configuration)
3. Verify the URL is valid: `Get-Content $HOME\.squad\teams-webhook.url`

### Card Too Large (28 KB Limit)

**Symptom**: Notification payload exceeds 28 KB

**Fix**:
- Reduce test instructions or summary text
- Remove markdown formatting if possible
- Split large features into multiple notifications
- The system automatically sends a fallback ("Notification too large...") if limit exceeded

### Notification Not Appearing in Teams

**Checks**:
1. **Webhook URL valid?** Test with curl:
   ```powershell
   $url = Get-Content $HOME\.squad\teams-webhook.url
   $body = @{ text = "Test message" } | ConvertTo-Json
   Invoke-RestMethod -Uri $url -Method Post -Body $body -ContentType "application/json"
   ```

2. **Dry-run to inspect JSON**:
   ```powershell
   .\scripts\notify-blocked.ps1 -Title "Test" -Reason "Testing" -ActionNeeded "Check Teams" -DryRun
   ```

3. **Check error log**:
   ```powershell
   Get-Content .squad\notifications-errors.log -Tail 10
   ```

4. **Network issues?** Ensure build machine can reach `webhooks.office.com`

### Duplicate Notifications

**Symptom**: Same notification appears multiple times

**Fix**:
- This is expected behavior for urgent tier (re-notifies every 6h if issue persists)
- Use `-Force` only when you want to bypass dedup intentionally
- For one-time notifications, use unique `eventId` values

### Feature Queue Not Flushing

**Symptom**: Feature notification queued but not sent

**Checks**:
1. Queue only flushes when: **5+ items** OR **1 hour passed** OR **-Force flag**
2. Manually flush: `.\scripts\notify-feature-complete.ps1 ... -Force`
3. Check queue state: `Get-Content .squad\notifications-state.json | ConvertFrom-Json | Select-Object featureQueue`

### Script Failure After Notification

**Symptom**: "Notification delivery failed after X attempts"

**Expected behavior**: Notification failure does NOT block the calling script. Failed deliveries are logged to `.squad\notifications-errors.log` for manual retry.

**Manual retry**:
```powershell
# Check failed deliveries
Get-Content .squad\notifications-errors.log -Tail 5

# Retry manually with -Force flag
.\scripts\notify-blocked.ps1 -Title "..." -Reason "..." -ActionNeeded "..." -Force
```

---

## Reference

### Event Schema

#### Urgent Tier
```json
{
  "eventId": "blocked:agent-name:timestamp",
  "title": "Header text",
  "reason": "Why this needs attention",
  "actionUrl": "https://link",
  "actionLabel": "Button text"
}
```

#### Action Tier
```json
{
  "eventId": "action:type:timestamp",
  "title": "Header text",
  "reason": "Why action needed",
  "currentState": "current-state-name",
  "estimatedTime": "~X minutes",
  "actionUrl": "https://link",
  "actionLabel": "Button text"
}
```

#### Feature Tier
```json
{
  "eventId": "feature:id:timestamp",
  "featureTitle": "Feature name",
  "summary": "What shipped",
  "testInstructions": "How to verify",
  "issuesUrl": "https://github/issues",
  "nextAction": "What's next",
  "nextActionDue": "Due date (optional)"
}
```

### State File

Notifications maintain dedup state in `.squad/notifications-state.json`:

```json
{
  "version": 1,
  "lastUpdated": "2026-01-24T10:30:00Z",
  "events": {
    "blocked:gimli:2026-01-24T09-30-00": {
      "type": "urgent",
      "lastNotifiedAt": "2026-01-24T09:30:00Z",
      "errorCount": 1
    }
  },
  "featureQueue": [
    {
      "featureId": "auth-flow",
      "featureTitle": "Auth Flow",
      "summary": "Token caching shipped.",
      "queuedAt": "2026-01-24T10:00:00Z"
    }
  ],
  "lastFeatureBatchAt": "2026-01-24T09:00:00Z"
}
```

---

## See Also

- `.squad/skills/squad-notifications/SKILL.md` — Core system design
- `scripts/notify.ps1` — Central notification router
- `scripts/notify-squad-event.ps1` — Dispatcher for common events
- `scripts/notify-blocked.ps1` — Urgent tier (immediate blocked notifications)
- `scripts/notify-feature-complete.ps1` — Feature tier (batched feature completions)
- `scripts/notification-scheduler.ps1` — Scheduler and batching engine for feature notifications
- `scripts/notification-recovery.ps1` — Retry/recovery functions
- GitHub Actions: `.github/workflows/notifications.yml` (pending)
