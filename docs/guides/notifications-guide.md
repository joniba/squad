---
title: "Notifications System Guide"
date: 2026-03-24
author: Bilbo
category: guides
tags: [notifications, teams, guide, user-facing, setup, troubleshooting]
---

# Notifications System Guide

A **proactive push notification system** for the PA Squad that sends real-time Teams messages when human intervention is needed, without requiring you to constantly check GitHub or logs.

---

## Overview

The notifications system classifies events into three urgency tiers and delivers them immediately to your Teams channel:

| Tier | Emoji | When | Delivery |
|------|-------|------|----------|
| **Urgent** | 🔴 | Livesite incidents, script failures, agent blockers, setup issues | Immediate |
| **Action** | 🟡 | PRs ready for review, design approvals needed, tasks requiring your input | Immediate |
| **Feature** | 🔵 | Features complete with test instructions | Batched (hourly or 5+ items) |

### Key Principle

**The system pushes work to you — you never discover blocked work by asking.**

---

## Setup

### 1. Configure Your Webhook URL

The notifications system sends messages via Teams webhooks. You need to provide the webhook URL **once**.

**Create the configuration directory and file:**

```powershell
# Create directory
$dir = Join-Path $HOME ".squad\notifications"
if (-not (Test-Path $dir)) { New-Item -Path $dir -ItemType Directory -Force | Out-Null }

# Save your webhook URL to this file (one line, no extra whitespace)
$webhookFile = Join-Path $HOME ".squad\teams-webhook.url"
$webhookUrl = "https://outlook.webhook.office.com/webhookb2/..."
$webhookUrl | Set-Content $webhookFile -Encoding UTF8
```

**Where to get your webhook URL:**
1. In Teams, open your squad channel
2. Click **⋮ More options** → **Connectors**
3. Search for **Incoming Webhook** and configure it
4. Copy the webhook URL from the connector settings

**Security notes:**
- Never commit the webhook file to Git (it's in `.squad/`, which is gitignored)
- The URL contains sensitive information — keep it private
- Store only the URL, nothing else, in the file

---

## Sending Notifications

### Using `Send-Notification` Function

The main entry point is the `Send-Notification` PowerShell function (aliased from `scripts/notify.ps1`).

```powershell
.\scripts\notify.ps1 -Type <tier> -Event <hashtable> [options]
```

### Urgent Notifications (🔴)

**When to use:** Script failures, livesite incidents, agent blockers, critical setup issues.

**Example: Script Failure**

```powershell
$event = @{
    eventId    = "script-failure:icm-scan:$(Get-Date -u -Format 'o')"
    title      = "icm-scan failed"
    reason     = "Timeout querying incident #123456789 — Check incident details"
    actionUrl  = "https://github.com/jbenami_microsoft/ms-pa/issues/42"
    actionLabel = "View Investigation"
}
.\scripts\notify.ps1 -Type urgent -Event $event
```

**Example: Livesite Incident**

```powershell
$event = @{
    eventId    = "livesite:sev1:$(Get-Date -u -Format 'o')"
    title      = "🚨 Sev1 Incident Detected"
    reason     = "Incident #987654321 in production. DGrep CLI authentication unavailable."
    actionUrl  = "https://portal.microsofticm.com/imp/v5/incidents/details/987654321/home"
    actionLabel = "View in ICM"
}
.\scripts\notify.ps1 -Type urgent -Event $event
```

**Example: Agent Escalation**

```powershell
$event = @{
    eventId    = "escalation:$([guid]::NewGuid()):$(Get-Date -u -Format 'o')"
    title      = "Agent blocked: Elrond cannot proceed"
    reason     = "Missing NuGet feed credentials. Elrond investigated; no solution found."
    actionUrl  = "https://github.com/jbenami_microsoft/ms-pa/issues/99"
    actionLabel = "View Issue & Setup Steps"
}
.\scripts\notify.ps1 -Type urgent -Event $event
```

**Required fields:** `title`, `reason`  
**Optional fields:** `actionUrl`, `actionLabel`, `eventId`

### Action Notifications (🟡)

**When to use:** PRs ready for review, design approvals pending, setup tasks requiring your input.

**Example: PR Ready for Review**

```powershell
$event = @{
    eventId       = "pr-review:42:$(Get-Date -u -Format 'o')"
    title         = "PR #42: DGrep CLI Structure — Ready for Review"
    reason        = "Galadriel review complete; awaiting your approval"
    actionUrl     = "https://github.com/jbenami_microsoft/ms-pa/pull/42"
    actionLabel   = "Review PR"
    estimatedTime = "~5 min read"
}
.\scripts\notify.ps1 -Type action -Event $event
```

**Example: Design Approval Needed**

```powershell
$event = @{
    eventId       = "design-review:notifications:$(Get-Date -u -Format 'o')"
    title         = "Design Review: Proactive Notifications System"
    reason        = "Design doc ready. Gandalf flagged for your architectural approval."
    actionUrl     = "https://github.com/jbenami_microsoft/ms-pa/blob/main/docs/designs/proactive-notifications.md"
    actionLabel   = "Review Design"
    estimatedTime = "~15 min read"
}
.\scripts\notify.ps1 -Type action -Event $event
```

**Required fields:** `title`, `reason`  
**Optional fields:** `actionUrl`, `actionLabel`, `estimatedTime`, `eventId`

### Feature Notifications (🔵)

**When to use:** Features complete with testing instructions ready for manual validation.

**Example: Feature Complete**

```powershell
$event = @{
    eventId          = "feature:dgrep-auth-flow:$(Get-Date -u -Format 'o')"
    featureTitle     = "DGrep CLI: Auth Flow & Token Caching"
    summary          = "CLI now supports interactive auth (DGrepUserAuthClient) and token caching via DefaultAzureCredential."
    testInstructions = @"
1. dgrep search --interactive --query "Bing.Web"
2. Verify login prompt appears
3. Verify token cached in %APPDATA%\dgrep\tokens.json
4. dgrep search --query "Bing.Web" (no prompt expected)
"@
    issuesUrl        = "https://github.com/jbenami_microsoft/ms-pa/issues?q=label%3Adgrep-auth"
    nextAction       = "Waiting for customer POC feedback"
    nextActionDue    = "2026-03-26"
}
.\scripts\notify.ps1 -Type feature -Event $event
```

**Batching behavior:**
- Features are automatically queued
- Batch sends when: ≥5 items in queue OR 1+ hour since last batch
- Use `-Force` flag to send immediately (skip batching)

**Required fields:** `featureTitle`, `summary`  
**Optional fields:** `testInstructions`, `issuesUrl`, `nextAction`, `nextActionDue`, `eventId`

---

## Event Triggers & Integration Points

### Script Failures (Squad Scheduler)

When a scheduled task in `.squad/scheduler.json` exits non-zero, `squad-scheduler.ps1` automatically calls the notification system:

```powershell
# Automatically triggered on task failure
# scripts/squad-scheduler.ps1 → triggers notification
# Location: .squad/scheduler.json (monitor task configurations)
```

**Example integration in a custom script:**

```powershell
# In your scheduled task script:
try {
    # Your work here
    Do-SomeWork
} catch {
    $event = @{
        eventId    = "script-failure:my-task:$(Get-Date -u -Format 'o')"
        title      = "my-task failed"
        reason     = "Error: $_"
        actionUrl  = "https://github.com/jbenami_microsoft/ms-pa/issues/123"
        actionLabel = "View Issue"
    }
    & "$PSScriptRoot\..\notify.ps1" -Type urgent -Event $event
    throw $_  # Re-throw so scheduler logs it
}
```

### PR Review Completion (Galadriel Post-PR)

When a PR review completes:

```powershell
# Called after Galadriel finishes code review
# Triggers action notification with PR details
```

### Feature Completion (Agent Reporting)

When an agent reports a feature complete (issue closes, parent feature resolved):

```powershell
# Called after issue closes or feature milestone reached
# Queues feature notification for batching
```

### Health & Monitoring

**Built-in trigger table:**

| Event | Tier | When | Integration |
|-------|------|------|-------------|
| Script failure | Urgent | Exit code ≠ 0 | `squad-scheduler.ps1` |
| Livesite incident | Urgent | Sev1/Sev2 detected | `scripts/icm-scan.ps1` |
| Agent escalation | Urgent | Elrond blocks → Gandalf escalates | `.squad/failure-recovery.md` |
| PR ready for review | Action | Galadriel review done, state=Review | `galadriel` agent post-PR |
| Design approval needed | Action | Design doc flagged | Manual trigger via agent |
| Setup task assigned | Action | Agent needs credentials/config | Manual trigger or failure recovery |
| Feature complete | Feature | Issue closes (marked complete) | Manual trigger per feature |

---

## Failure Recovery

The notifications system includes automatic failure recovery. If a notification fails to send, it's queued for retry with exponential backoff.

### Dead Letter Queue

Failed notifications are persisted to disk at `~/.squad/notifications/dead-letter/` for later retry:

```
~/.squad/notifications/
├── dead-letter/
│   ├── 20260324-143200-500_urgent.json
│   ├── 20260324-143215-123_action.json
│   └── ...
├── retry.log                  # Retry attempt history
├── last-success.json          # Timestamp of last successful send
└── teams-webhook.url          # Your webhook URL (git-ignored)
```

### Retry Behavior

Failed notifications are automatically retried with escalating backoff:

- **Cycle 1:** 5 minutes after first failure
- **Cycle 2:** 15 minutes after Cycle 1
- **Cycle 3:** 1 hour after Cycle 2
- **Permanent failure:** After 3 cycles (>1.5 hours), moved to permanent-failure status

**Manual retry:**

```powershell
# Trigger retry of all pending notifications
.\scripts\notification-recovery.ps1 -Function Retry-FailedNotifications

# Dry run (see what would happen)
.\scripts\notification-recovery.ps1 -Function Retry-FailedNotifications -DryRun
```

### Health Check

Check notification system health at any time:

```powershell
# Load recovery module and run health check
. .\scripts\notification-recovery.ps1
$health = Test-NotificationHealth
$health | Select-Object timestamp, webhookReachable, deadLetterCount, pendingRetries, status
```

**Health status values:**
- **healthy** — Webhook reachable, no pending retries
- **degraded** — Webhook OK but items pending retry
- **unhealthy** — Webhook unreachable
- **unconfigured** — No webhook URL provided yet

### Cleanup

Automatic cleanup of the dead letter queue happens periodically:
- **Succeeded retries** — Removed immediately after successful send
- **Permanent failures** — Removed after 30 days

Manual cleanup:

```powershell
. .\scripts\notification-recovery.ps1
$result = Invoke-DeadLetterCleanup
Write-Host "Cleaned up $($result.Removed) old notifications"
```

---

## Configuration Reference

### Configuration Files

| File | Location | Purpose | Format |
|------|----------|---------|--------|
| Webhook URL | `~/.squad/teams-webhook.url` | Teams incoming webhook endpoint (git-ignored) | Plain text (one line) |
| Notification State | `.squad/notifications-state.json` | Dedup watermark & feature queue (git-ignored) | JSON |
| Dead Letter Queue | `~/.squad/notifications/dead-letter/` | Failed notifications awaiting retry | JSON files |
| Retry Log | `~/.squad/notifications/retry.log` | History of retry attempts (for debugging) | Text |
| Last Success | `~/.squad/notifications/last-success.json` | Timestamp of last successful send | JSON |
| Error Log | `.squad/notifications-errors.log` | Errors from delivery attempts | Text |

### Notification State Structure

Stored in `.squad/notifications-state.json`:

```json
{
  "version": 1,
  "lastUpdated": "2026-03-24T14:35:00Z",
  "events": {
    "script-failure:icm-scan:2026-03-24T14:32:00Z": {
      "type": "urgent",
      "lastNotifiedAt": "2026-03-24T14:32:15Z",
      "retryCount": 0,
      "errorCount": 1
    },
    "pr-review:42:2026-03-24T14:30:00Z": {
      "type": "action",
      "lastNotifiedAt": "2026-03-24T14:30:45Z",
      "retryCount": 0,
      "seenStates": ["Draft", "Review"]
    }
  },
  "featureQueue": [
    {
      "featureId": "feature:dgrep-auth:2026-03-24T15:00:00Z",
      "featureTitle": "Auth Flow & Token Caching",
      "summary": "CLI now supports...",
      "testInstructions": "1. dgrep search...",
      "queuedAt": "2026-03-24T15:00:10Z"
    }
  ],
  "lastFeatureBatchAt": "2026-03-24T14:00:00Z"
}
```

### Dead Letter Item Structure

Each failed notification in `~/.squad/notifications/dead-letter/*.json`:

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "createdAt": "2026-03-24T14:35:00Z",
  "tier": "urgent",
  "eventId": "script-failure:icm-scan:2026-03-24T14:32:00Z",
  "event": {
    "eventId": "script-failure:icm-scan:2026-03-24T14:32:00Z",
    "title": "icm-scan failed",
    "reason": "Timeout querying incident #123456789"
  },
  "card": { /* Adaptive Card JSON */ },
  "webhookUrl": "https://outlook.webhook.office.com/webhookb2/...",
  "originalError": "Connection timeout",
  "attempts": 4,
  "retryCycle": 0,
  "lastRetryAt": null,
  "status": "pending"
}
```

---

## Testing

### Dry Run Mode

Test notification formatting without sending:

```powershell
$event = @{
    eventId = "test:example:$(Get-Date -u -Format 'o')"
    title   = "Test Notification"
    reason  = "This is a dry run test"
}
.\scripts\notify.ps1 -Type urgent -Event $event -DryRun
```

Output shows the Adaptive Card JSON that *would* be sent, without actually calling the webhook.

### Force Skip Dedup

Send a notification even if dedup would normally suppress it:

```powershell
# Send even if same eventId was recently sent
.\scripts\notify.ps1 -Type urgent -Event $event -Force
```

### Flush Feature Queue Manually

Force batch send of queued features without waiting for 1 hour:

```powershell
# Add a feature to queue
$feature = @{
    eventId        = "feature:test:$(Get-Date -u -Format 'o')"
    featureTitle   = "Test Feature"
    summary        = "Test summary"
    testInstructions = "1. Test it\n2. Verify"
    issuesUrl      = "https://github.com/example"
}
.\scripts\notify.ps1 -Type feature -Event $feature

# Force immediate batch (don't wait for 1h or 5 items)
.\scripts\notify.ps1 -Type feature -Force
```

### Running Pester Tests

Unit tests for the notification system:

```powershell
# From repository root
Invoke-Pester -Path "tests/notifications/" -Verbose
```

Expected test coverage:
- Dedup logic (urgent, action, feature)
- Adaptive Card formatting
- State file I/O
- Webhook retry logic
- Dead letter queue persistence
- Health checks

---

## Troubleshooting

### Notification Not Sent

**Symptom:** You don't see a notification in Teams.

**Checklist:**

1. **Webhook URL configured?**
   ```powershell
   Test-Path $HOME\.squad\teams-webhook.url
   Get-Content $HOME\.squad\teams-webhook.url
   ```
   If missing, see [Setup](#setup).

2. **Webhook reachable?**
   ```powershell
   . .\scripts\notification-recovery.ps1
   $health = Test-NotificationHealth
   $health.webhookReachable
   ```
   If `$false`, check Teams connector settings.

3. **Suppressed by dedup?**
   ```powershell
   # Check notification state
   Get-Content .squad\notifications-state.json | ConvertFrom-Json | Select-Object -ExpandProperty events | 
     Where-Object { $_ -match "your-event-id" }
   ```
   Use `-Force` flag to override dedup.

4. **Check error log:**
   ```powershell
   Get-Content .squad\notifications-errors.log -Tail 20
   ```
   Look for webhook errors or payload size issues.

5. **Pending retry?**
   ```powershell
   Get-ChildItem $HOME\.squad\notifications\dead-letter\
   ```
   If files exist with `status: "pending"`, retry is scheduled.

### "Webhook URL file not found"

**Symptom:** Warning: "Webhook URL file not found: ~/.squad/teams-webhook.url — notification will be skipped."

**Solution:** Create the file (see [Setup](#setup)):

```powershell
$webhookUrl = "https://outlook.webhook.office.com/webhookb2/..."
$webhookUrl | Set-Content "$HOME\.squad\teams-webhook.url" -Encoding UTF8
```

### "Card payload exceeds 28 KB"

**Symptom:** Warning: "Card payload exceeds 28 KB. Truncating body."

**Cause:** Notification content too large for Adaptive Card limit.

**Solution:**
- Shorten `testInstructions` (link to docs instead of inline)
- Reduce number of items in feature batch (system auto-batches at 5 items)
- Simplify `reason` field (use actionUrl for details)

### Notification Repeatedly Failing

**Symptom:** Dead letter queue filling up with retries.

**Diagnosis:**

```powershell
. .\scripts\notification-recovery.ps1
$items = Get-DeadLetterItems
$items | Where-Object { $_.status -in @("pending", "retrying") } | 
  Select-Object eventId, retryCycle, originalError
```

**Common causes:**
- **Webhook URL invalid or revoked** → Regenerate webhook in Teams connector
- **Network issues** → Check connectivity to `outlook.webhook.office.com`
- **Teams connector disabled** → Re-enable in Teams connector settings

### Dedup Too Aggressive (Not Sending Real Updates)

**Symptom:** New notifications for the same event suppressed incorrectly.

**For urgent tier:** Resends after 3+ consecutive errors OR 6 hours of silence (by design).

**For action tier:** Resends on state change OR after 48 hours (by design).

**For feature tier:** Never resends same feature (by design).

**To force send:**

```powershell
# Add a unique suffix to eventId each time
$event = @{
    eventId = "pr-review:42:$(Get-Date -u -Format 'o')"  # Includes timestamp
    title   = "PR #42 Updated"
    reason  = "New changes pushed"
}
.\scripts\notify.ps1 -Type action -Event $event
```

### "Corrupt state file"

**Symptom:** Warning: "Corrupt state file, starting fresh"

**Cause:** `.squad/notifications-state.json` is malformed JSON.

**Solution:**

```powershell
# Backup corrupted file
Move-Item .squad\notifications-state.json .squad\notifications-state.json.bak

# System will recreate it on next notification
.\scripts\notify.ps1 -Type feature -Event @{ featureTitle = "Test"; summary = "Test" }
```

---

## Reference: Adaptive Cards

Notifications are formatted as Microsoft Adaptive Cards, a standardized JSON format for interactive content in Teams.

**Card payload constraints:**
- Max 28 KB per card
- Max actions: 5 per card
- Max text length: ~1,000 chars per TextBlock

The notification system handles this automatically:
- Oversized payloads truncated with fallback message
- Feature batches limited to 5 items per card
- Action links required (no orphaned content)

For details on Adaptive Card formatting, see: https://adaptivecards.io/

---

## Next Steps

### Common Integration Points

**Add notifications to your scripts:**

```powershell
# 1. On script failure
try {
    $result = Do-ImportantWork
} catch {
    & ".\scripts\notify.ps1" -Type urgent -Event @{
        eventId = "my-script:failure:$(Get-Date -u -Format 'o')"
        title = "My script failed"
        reason = "$_"
        actionUrl = "https://github.com/org/repo/issues/123"
        actionLabel = "View Issue"
    }
    throw
}

# 2. On task completion
& ".\scripts\notify.ps1" -Type feature -Event @{
    eventId = "feature:my-task:$(Get-Date -u -Format 'o')"
    featureTitle = "Task Complete"
    summary = "Description of what was done"
    testInstructions = "How to verify the work"
    issuesUrl = "https://github.com/org/repo/issues"
}
```

**Monitor notification health:**

```powershell
# Add to your monitoring/health check scripts
. .\scripts\notification-recovery.ps1
$health = Test-NotificationHealth
if ($health.status -ne "healthy") {
    Write-Warning "Notification system unhealthy: $($health.status)"
}
```

**Integrate with scheduled tasks:**

```powershell
# Add to .squad/scheduler.json to trigger task failure notifications
# See squad-scheduler.ps1 for integration points
```

---

## Support

For issues, questions, or feedback about the notifications system:

1. Check **Troubleshooting** above
2. Run `.\scripts\notification-recovery.ps1 -Function Test-NotificationHealth` for diagnostics
3. Review `.squad/notifications-errors.log` for detailed error messages
4. File an issue: https://github.com/jbenami_microsoft/ms-pa/issues (label: `team:infrastructure`)

---

**Last updated:** 2026-03-24  
**System version:** 1 (Stable — 3 phases merged)  
**Owned by:** Bilbo (Documentation)
