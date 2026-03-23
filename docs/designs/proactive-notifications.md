# Design: Proactive Teams Notifications System

**Status:** Design Review (Pending Approval)  
**Owner:** Gandalf (design) → Gimli (implementation)  
**Issue:** #112  
**Created:** 2026-03-24  

---

## Executive Summary

The squad currently waits passively for Jonathan to discover blocked work via GitHub. This design introduces a **proactive push notification system** that sends Teams webhooks when human intervention is needed, categorized by urgency (🔴 Urgent, 🟡 Action Needed, 🔵 Feature Complete). Notifications are immediate for blockers/failures, and batched hourly for feature completion summaries with testing instructions.

**Key principle:** The human never discovers blocked work by asking — the system pushes it.

---

## Problem Statement

### Current State
- **Reactive:** Jonathan must poll GitHub to discover blocked PRs, stale reviews, tool failures, and setup issues
- **Silent failures:** Script errors, agent blockers, and incomplete setup go unnoticed until Jonathan explicitly checks logs
- **No urgency signaling:** All work appears equal in the daily summary — no distinction between "livesite on fire" and "routine code review pending"
- **Scattered integration:** Notifications are ad-hoc (icm-scan posts incidents, squad-daily-summary batches daily updates) with no unified logic

### Impact
- **Development friction:** Debugging happens offline. Jonathan wastes time rediscovering the same issues.
- **SLA violations:** Livesite issues not detected immediately.
- **Stale reviews:** PRs languish waiting for human approval; agents can't proceed.
- **Setup delays:** Agents discover missing credentials/configs and block, but Jonathan doesn't know why work stopped.

---

## Requirements

### Functional

#### 1. Urgent Notifications (🔴 Immediate)
**When:**
- **Livesite incident detected** (Sev1, Sev2 via icm-scan → Aragorn)
- **Script/scheduled task failure** (squad-scheduler, ralph-watch, icm-scan exits non-zero)
- **Agent escalation** (failure recovery pipeline → Elrond can't solve → Gandalf escalates to Jonathan)
- **Critical setup blocker** (e.g., DGrep agent needs internal NuGet feed, env var missing, secrets not available)

**Content:**
- Title with emoji + event type (e.g., "🔴 Scheduled Task Failed: icm-scan")
- Reason (e.g., "query timeout on incident #123456789")
- What failed / what's needed
- Actionable link (GitHub issue, ICM incident, runbook, or config doc)
- Immediate action (restart command, fix link, etc.)

**Dedup:**
- Watermark file: `.squad/notifications-state.json` (gitignored)
- Per-event tracking: `{ eventType, eventId, lastNotifiedAt, errorCount }`
- Threshold: Notify only on first failure OR after 3 consecutive failures (prevents notification spam on recurring issues)

---

#### 2. Action Needed Notifications (🟡 Immediate)
**When:**
- **PR ready for review** (Galadriel completes review → PR in "Changes Requested" or "Needs Review" state)
- **PR assigned to Jonathan** (GitHub event or ADO PR assignment)
- **Design review pending** (Gandalf flags design as "awaiting-human-approval")
- **Setup task discovered** (agent needs Jonathan to provide credentials, create tokens, etc.)

**Content:**
- Title with emoji + action (e.g., "🟡 PR Ready for Review: #42")
- Link to PR / issue / design doc
- Summary of what was done (1-2 lines)
- Expected time to review ("~5 min read")

**Dedup:**
- Watermark: `{ eventId, lastNotifiedAt }`
- Notify once per state change (e.g., notify when PR moves from Draft to Review, but not again until it moves states)
- Renotify after 48h of inactivity (reminder)

---

#### 3. Feature Complete Notifications (🔵 Batched, Max 1/hour)
**When:**
- **Multi-issue feature closes** (parent issue closed OR all child issues labeled `is-part-of:X` move to Done)
- **Includes testing instructions** (linked docs or short inline tests)

**Content:**
- Title: "🔵 Feature Complete: X"
- Summary of what shipped (2-3 lines)
- Testing instructions (short! link to README if long)
- Link to merged PR(s) or issues
- Next action if any (e.g., "Waiting for customer feedback by 2026-03-26")

**Batching Logic:**
- Collect all feature completions in a queue per hour (`.squad/feature-queue.json`, gitignored)
- At the top of each hour (or when queue exceeds 5 items), send one batched notification
- Format: "\n\n🔵 Feature 1\n...\n\n🔵 Feature 2\n..."
- Max payload 28 KB (Adaptive Card limit); if queue exceeds limit, split into two notifications

**Dedup:**
- Track `{ featureId, lastNotified }` in state
- Never notify the same feature twice
- Expired entries (>7 days old) purged from state

---

### Non-Functional

- **Latency:** Immediate notifications must send within 2 seconds of trigger
- **Reliability:** Use exponential backoff on webhook failures (2s, 4s, 8s, 16s); log to `.squad/notifications-errors.log` after 4 retries
- **No false positives:** Only notify on confirmed action (not on transient state)
- **No credentials in notifications:** Never include tokens, URLs with secrets, or sensitive config in message body
- **Graceful degradation:** If webhook fails, log error but don't block calling code (fire-and-forget)

---

## Architecture

### Overview

```
Events (trigger sources)
    ↓
Notification Router (classify → tier)
    ↓
Deduplication Gate (check watermark)
    ↓
Format Handler (build Adaptive Card)
    ↓
Delivery (send via webhook with retry)
    ↓
State Update (record in watermark)
```

### Components

#### 1. **Notification Router** (`scripts/notify.ps1`)
Central entry point for all notification types. Classifies event and routes to appropriate tier.

**Signature:**
```powershell
.\scripts\notify.ps1 `
  -Type <"urgent" | "action" | "feature"> `
  -Event <hashtable> `
  [-StateFile "path/to/state.json"] `
  [-WebhookFile "path/to/webhook.url"]
```

**Input `$Event` structure (varies by type):**

*Urgent:*
```powershell
@{
  eventId         = "script-failure:icm-scan:2026-03-24T14:32:00Z"
  errorType       = "script-failure" | "escalation" | "livesite" | "setup-blocker"
  title           = "icm-scan failed"
  reason          = "timeout querying incident #123456789"
  actionUrl       = "https://github.com/jbenami_microsoft/ms-pa/issues/42"
  actionLabel     = "View Investigation"
  urgency         = 1 # or 2 or 3 (1=immediate, 2=soon, 3=info)
}
```

*Action:*
```powershell
@{
  eventId         = "pr-review:42:2026-03-24T14:30:00Z"
  actionType      = "pr-review" | "setup-task" | "design-review" | "assignment"
  title           = "PR #42: DGrep CLI Structure — Ready for Review"
  reason          = "Galadriel review complete; awaiting approval"
  actionUrl       = "https://github.com/jbenami_microsoft/ms-pa/pull/42"
  actionLabel     = "Review PR"
  estimatedTime   = "~5 min read"
}
```

*Feature:*
```powershell
@{
  eventId         = "feature:dgrep-cli-auth-flow:2026-03-24T15:00:00Z"
  featureTitle    = "DGrep CLI: Auth Flow & Token Refresh"
  summary         = "CLI now supports interactive auth (DGrepUserAuthClient) and token caching via DefaultAzureCredential."
  testInstructions = @"
1. dgrep search --interactive --query "Bing.Web"
2. Verify login prompt appears
3. Verify token cached in %APPDATA%\dgrep\tokens.json
4. dgrep search --query "Bing.Web" (no prompt)
"@
  issuesUrl       = "https://github.com/jbenami_microsoft/ms-pa/issues?q=is%3Aissue+is%3Aclosed+label%3Adgrep-auth"
  nextAction      = "Waiting for customer POC feedback"
  nextActionDue   = "2026-03-26"
}
```

#### 2. **Deduplication Gate** (embedded in `notify.ps1`)
Reads watermark file, checks if event is new or a repeat. Updates watermark after successful send.

**Watermark structure** (`.squad/notifications-state.json`, gitignored):
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
      "featureId": "dgrep-cli-auth:2026-03-24T15:00:00Z",
      "queuedAt": "2026-03-24T15:00:10Z",
      "lastHourBatchId": null
    }
  ],
  "lastFeatureBatchAt": "2026-03-24T14:00:00Z"
}
```

**Dedup Logic:**
- **Urgent:** Send if `eventId` not in watermark OR `errorCount >= 3` OR `lastNotifiedAt` older than 6h (reminder)
- **Action:** Send if `eventId` not in watermark OR state changed (detect by comparing `seenStates`) OR `lastNotifiedAt` older than 48h (reminder)
- **Feature:** Enqueue; batch send once per hour (top of hour or when queue ≥ 5 items)

---

#### 3. **Format Handler** (functions in `notify.ps1`)
Builds Adaptive Card payloads per tier. Ensures consistency and compliance with Teams constraints.

**Urgent Card Template:**
```json
{
  "type": "message",
  "attachments": [{
    "contentType": "application/vnd.microsoft.card.adaptive",
    "content": {
      "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
      "type": "AdaptiveCard",
      "version": "1.4",
      "body": [
        { "type": "TextBlock", "size": "Large", "weight": "Bolder", "text": "🔴 {title}", "color": "attention" },
        { "type": "TextBlock", "text": "{reason}", "wrap": true, "spacing": "Small" },
        { "type": "TextBlock", "text": "**What to do:**", "weight": "Bolder", "spacing": "Medium" },
        { "type": "TextBlock", "text": "{actionLabel}", "wrap": true }
      ],
      "actions": [
        {
          "type": "Action.OpenUrl",
          "title": "{actionLabel}",
          "url": "{actionUrl}"
        }
      ]
    }
  }]
}
```

**Action Card Template:**
```json
{
  "type": "message",
  "attachments": [{
    "contentType": "application/vnd.microsoft.card.adaptive",
    "content": {
      "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
      "type": "AdaptiveCard",
      "version": "1.4",
      "body": [
        { "type": "TextBlock", "size": "Medium", "weight": "Bolder", "text": "🟡 {title}" },
        { "type": "TextBlock", "text": "{reason}", "wrap": true, "spacing": "Small" },
        { "type": "TextBlock", "text": "⏱️ {estimatedTime}", "isSubtle": true, "spacing": "Small" }
      ],
      "actions": [
        {
          "type": "Action.OpenUrl",
          "title": "{actionLabel}",
          "url": "{actionUrl}"
        }
      ]
    }
  }]
}
```

**Feature Card Template:**
```json
{
  "type": "message",
  "attachments": [{
    "contentType": "application/vnd.microsoft.card.adaptive",
    "content": {
      "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
      "type": "AdaptiveCard",
      "version": "1.4",
      "body": [
        { "type": "TextBlock", "size": "Medium", "weight": "Bolder", "text": "🔵 {featureTitle}" },
        { "type": "TextBlock", "text": "{summary}", "wrap": true, "spacing": "Small" },
        { "type": "TextBlock", "text": "**Testing Instructions:**", "weight": "Bolder", "spacing": "Medium" },
        { "type": "TextBlock", "text": "{testInstructions}", "wrap": true, "family": "monospace", "size": "Small" },
        { "type": "TextBlock", "text": "{nextAction} (due {nextActionDue})", "isSubtle": true, "spacing": "Medium", "wrap": true }
      ],
      "actions": [
        {
          "type": "Action.OpenUrl",
          "title": "View Issues",
          "url": "{issuesUrl}"
        }
      ]
    }
  }]
}
```

---

#### 4. **Delivery** (in `notify.ps1`)
Sends card via Teams webhook with retry logic. Fire-and-forget; failures logged but don't block caller.

**Retry Strategy:**
- Exponential backoff: 2s, 4s, 8s, 16s (max 4 attempts)
- After 4 failures, log to `.squad/notifications-errors.log` and return success anyway
- Log format: `[TIMESTAMP] [TYPE] [ERROR] [RETRY_COUNT] [EVENT_ID]`

**Security:**
- Never log webhook URL
- Never include sensitive data in retry logs
- Webhook URL read from `~/.squad/teams-webhook.url` (user home, not repo)

---

### Integration Points

#### From **Squad Scheduler** (`squad-scheduler.ps1`)
When a scheduled task exits non-zero:

```powershell
# On task failure
$event = @{
  eventId      = "script-failure:$taskName:$(Get-Date -u -Format 'yyyy-MM-ddTHH:mm:ssZ')"
  errorType    = "script-failure"
  title        = "$taskName failed at $(Get-Date -Format 'HH:mm:ss')"
  reason       = "Exit code $exitCode; see log at $logPath"
  actionUrl    = "file:///$logPath"  # or GitHub Actions link
  actionLabel  = "View Log"
}
& "$scriptDir\notify.ps1" -Type "urgent" -Event $event
```

#### From **Failure Recovery Pipeline** (`.squad/failure-recovery.md`)
When Elrond escalates to Jonathan:

```powershell
# After Elrond reports "no solution found"
$event = @{
  eventId      = "escalation:$(New-Guid):$(Get-Date -u -Format 'yyyy-MM-ddTHH:mm:ssZ')"
  errorType    = "escalation"
  title        = "Agent blocked and escalated: $agentName / $taskTitle"
  reason       = "Blocker: $blockerDescription. Elrond researched and found no solution."
  actionUrl    = "https://github.com/jbenami_microsoft/ms-pa/issues/$issueNumber"
  actionLabel  = "View Issue & Investigation"
}
& "$scriptDir\notify.ps1" -Type "urgent" -Event $event
```

#### From **Galadriel PR Review** (agent spawn post-PR)
When PR review completes:

```powershell
# After Galadriel completes review and PR is in "Review" state
$event = @{
  eventId         = "pr-review:$prNumber:$(Get-Date -u -Format 'yyyy-MM-ddTHH:mm:ssZ')"
  actionType      = "pr-review"
  title           = "PR #$prNumber: $prTitle — Ready for Review"
  reason          = "Galadriel review complete; awaiting your approval"
  actionUrl       = "https://github.com/jbenami_microsoft/ms-pa/pull/$prNumber"
  actionLabel     = "Review PR"
  estimatedTime   = "~5 min read"
}
& "$scriptDir\notify.ps1" -Type "action" -Event $event
```

#### From **Agent Spawn** (when agent reports feature complete)
When an issue closes and marks a feature complete:

```powershell
# After issue #105 closes (DGrep CLI auth)
$event = @{
  eventId              = "feature:dgrep-cli-auth:2026-03-24T15:00:00Z"
  featureTitle         = "DGrep CLI: Auth Flow & Token Refresh"
  summary              = "CLI now supports interactive auth (DGrepUserAuthClient) and token caching."
  testInstructions     = @"
1. dgrep search --interactive --query "Bing.Web"
2. Verify login prompt, then token cache at %APPDATA%\dgrep\tokens.json
"@
  issuesUrl            = "https://github.com/jbenami_microsoft/ms-pa/issues?q=label%3Adgrep-auth"
  nextAction           = "Waiting for customer POC validation"
  nextActionDue        = "2026-03-26"
}
& "$scriptDir\notify.ps1" -Type "feature" -Event $event
```

#### From **ADO PR Assignment** (if monitoring ADO)
When a PR is assigned to Jonathan in Azure DevOps:

```powershell
# On ADO PR assignment (via Ralph or Aragorn monitoring)
$event = @{
  eventId         = "ado-pr-assignment:$prId:$(Get-Date -u -Format 'yyyy-MM-ddTHH:mm:ssZ')"
  actionType      = "assignment"
  title           = "ADO PR Assigned: $prTitle (PR $prId)"
  reason          = "Assigned to you in Azure DevOps"
  actionUrl       = "https://dev.azure.com/{{org}}/{{project}}/_git/{{repo}}/pullrequest/$prId"
  actionLabel     = "View PR"
  estimatedTime   = "~5 min review"
}
& "$scriptDir\notify.ps1" -Type "action" -Event $event
```

---

### Webhook Payload Format (Examples)

#### Example 1: Urgent — Script Failure
```json
{
  "type": "message",
  "attachments": [{
    "contentType": "application/vnd.microsoft.card.adaptive",
    "content": {
      "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
      "type": "AdaptiveCard",
      "version": "1.4",
      "body": [
        { "type": "TextBlock", "size": "Large", "weight": "Bolder", "text": "🔴 icm-scan failed at 14:32:15", "color": "attention" },
        { "type": "TextBlock", "text": "Exit code 124 (timeout querying incident #123456789)", "wrap": true, "spacing": "Small" },
        { "type": "TextBlock", "text": "**What to do:** Check the logs and retry manually or wait for next scheduled run (15:00).", "wrap": true, "spacing": "Medium" }
      ],
      "actions": [
        { "type": "Action.OpenUrl", "title": "View Logs", "url": "file:///C:/dev/personal/pa-squad/.squad/logs/icm-scan-2026-03-24.log" }
      ]
    }
  }]
}
```

#### Example 2: Action — PR Review
```json
{
  "type": "message",
  "attachments": [{
    "contentType": "application/vnd.microsoft.card.adaptive",
    "content": {
      "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
      "type": "AdaptiveCard",
      "version": "1.4",
      "body": [
        { "type": "TextBlock", "size": "Medium", "weight": "Bolder", "text": "🟡 PR #105: DGrep CLI Auth Flow — Ready for Review" },
        { "type": "TextBlock", "text": "Galadriel review complete; awaiting your approval", "wrap": true, "spacing": "Small" },
        { "type": "TextBlock", "text": "⏱️ ~5 min read", "isSubtle": true, "spacing": "Small" }
      ],
      "actions": [
        { "type": "Action.OpenUrl", "title": "Review PR", "url": "https://github.com/jbenami_microsoft/ms-pa/pull/105" }
      ]
    }
  }]
}
```

#### Example 3: Feature Complete (Batched)
```json
{
  "type": "message",
  "attachments": [{
    "contentType": "application/vnd.microsoft.card.adaptive",
    "content": {
      "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
      "type": "AdaptiveCard",
      "version": "1.4",
      "body": [
        { "type": "TextBlock", "size": "Medium", "weight": "Bolder", "text": "🔵 Feature Complete: DGrep CLI Auth Flow & Token Refresh" },
        { "type": "TextBlock", "text": "CLI now supports interactive auth (DGrepUserAuthClient) and token caching via DefaultAzureCredential. Config saved to %APPDATA%\\dgrep\\tokens.json.", "wrap": true, "spacing": "Small" },
        { "type": "TextBlock", "text": "**Testing Instructions:**", "weight": "Bolder", "spacing": "Medium" },
        { "type": "TextBlock", "text": "1. dgrep search --interactive --query \"Bing.Web\"\n2. Verify login prompt, then token cache\n3. dgrep search --query \"Bing.Web\" (no prompt)", "wrap": true, "family": "monospace", "size": "Small" },
        { "type": "TextBlock", "text": "**Next:** Waiting for customer POC validation (due 2026-03-26)", "isSubtle": true, "spacing": "Medium", "wrap": true }
      ],
      "actions": [
        { "type": "Action.OpenUrl", "title": "View Issues", "url": "https://github.com/jbenami_microsoft/ms-pa/issues?q=label%3Adgrep-auth" }
      ]
    }
  }]
}
```

---

## Batching Logic (Feature Complete)

### Hourly Batch Window
- **Window:** Top of every hour (00:00, 01:00, etc.) ± 5 minutes (scheduler triggers every 5 min; batch on first trigger ≥ HH:00)
- **Queue:** `.squad/feature-queue.json` (gitignored)
- **Trigger 1 (scheduled):** `squad-scheduler.ps1` calls `notify.ps1 -Type feature -Batch` at the top of each hour
- **Trigger 2 (manual):** Batch immediately if queue item count ≥ 5 (max-payload safety)

### Queue Format
```json
{
  "version": 1,
  "items": [
    {
      "featureId": "dgrep-cli-auth:2026-03-24T15:00:00Z",
      "featureTitle": "DGrep CLI: Auth Flow & Token Refresh",
      "summary": "...",
      "testInstructions": "...",
      "issuesUrl": "...",
      "nextAction": "...",
      "nextActionDue": "...",
      "queuedAt": "2026-03-24T15:00:10Z"
    }
  ],
  "lastBatchAt": "2026-03-24T14:00:00Z"
}
```

### Batch Notification
- Collect all items in queue
- Build multi-feature card (up to 28 KB)
- Send one notification with all features
- Clear queue
- Update `lastBatchAt` in watermark
- Move processed items to state watermark (for dedup memory)

### Payload Size Safety
- Each feature ≈ 500–700 bytes (card header + summary + instructions)
- ~40 features per 28 KB limit
- If queue ≥ 5 items, trigger immediate batch (don't wait for hour boundary)
- If batch would exceed 28 KB, split into two notifications (batch 1 & batch 2 sent seconds apart)

---

## State Management

### Files (All Gitignored)
- `.squad/notifications-state.json` — event dedup + feature queue index
- `.squad/feature-queue.json` — feature complete items pending batch send
- `.squad/notifications-errors.log` — retry failures & diagnostic info
- `~/.squad/teams-webhook.url` — webhook URL (outside repo, user home)

### Rotation & Cleanup
- **Watermark state:** Entries older than 7 days auto-purged
- **Error log:** Rotated weekly (archive to `.squad/logs/notifications-errors-{date}.log`)
- **Feature queue:** Cleared after each batch send
- **Stale events:** If webhook is down for >12h, oldest watermark entries pruned to prevent unbounded growth

---

## Error Handling

### Webhook Failures
1. **Transient failure (1st–3rd attempt):** Retry with exponential backoff
2. **Persistent failure (4th attempt):** Log to `.squad/notifications-errors.log` and return success (don't block calling code)
3. **URL misconfigured:** Fail loudly with clear error message; require user to fix `.squad/teams-webhook.url`

### Malformed Event
- Validate required fields in `$Event` per type
- If missing, throw error with clear message (e.g., "`-Event` must include `actionUrl` for action-type notifications")
- Return early; don't attempt send

### Queue Corruption
- On JSON parse error in state file: log warning, delete file, start fresh
- On JSON parse error in feature queue: log warning, skip corrupted items, process remainder

---

## Scalability & Performance

### Concurrency
- **Fire-and-forget:** Calling code does not wait for notification to send (non-blocking)
- **Parallel sends:** Multiple agents can call `notify.ps1` simultaneously; no contention (async sends via Jobs)

### Rate Limiting
- Teams webhook: max 4 req/sec (built-in rate limit)
- Our buffer: Space sends ~100 ms apart to stay well under limit
- Queue discipline: Never send >10 notifications per minute (scheduler batch only)

### Cost
- 1 notification per urgent event (varies)
- 1 notification per action (varies)
- 1 notification per hour (feature batch)
- Estimated: 2–5 webhooks/day steady state; 10–15 on incident days

---

## Testing & Validation

### Unit Tests
- [notify.ps1] Dedup logic: verify watermark updates correctly
- [notify.ps1] Card formatting: verify JSON structure matches Adaptive Card schema
- [notify.ps1] Retry logic: mock webhook failures, verify exponential backoff
- [feature queue] Batch logic: verify queue collects, sorts, sends, and clears

### Integration Tests
1. Send urgent notification → verify Teams message appears within 2s
2. Send duplicate event → verify second send is skipped (watermark gate)
3. PR review trigger → send action notification → verify link works
4. Feature complete batch → queue 5 features → verify one batched notification (not 5)

### Manual Validation
- Post test notification to teams channel
- Verify card renders correctly (colors, links, wrapping)
- Click action links → verify they work

---

## Rollout Plan

### Phase 0: Foundation (Week 1)
- Implement `scripts/notify.ps1` (router + format handlers + dedup + delivery)
- Implement watermark persistence & cleanup
- Unit tests for routing and formatting

### Phase 1: Integration (Week 2)
- Wire `squad-scheduler.ps1` to call `notify.ps1` on task failure
- Wire `notify.ps1` to support hourly feature batching
- Test feature queue and batch send

### Phase 2: Agent Integration (Week 3)
- Update Galadriel spawn (PR review complete → notify)
- Update failure recovery pipeline (escalation → notify)
- Add support for ADO PR assignment (optional, depends on Ralph)

### Phase 3: Documentation & Launch (Week 4)
- User guide: "Setting up notifications" (webhook setup steps)
- Runbook: "Noise control & silent mode" (how to suppress notifications if needed)
- Deploy to prod + announce in squad

---

## Future Enhancements (Out of Scope)

- **Email notifications** — if need offline delivery
- **Slack webhook** — if team uses Slack
- **Notification preferences** — allow Jonathan to filter by urgency
- **Notification history** — searchable archive of past notifications
- **Custom rules** — e.g., "batch all Gimli issues, send Aragorn urgent only"
- **Mobile app push** — if critical incident needs immediate alert on phone

---

## FAQ

**Q: What if the webhook URL changes?**  
A: User rotates webhook in Teams → updates `~/.squad/teams-webhook.url` → next notification uses new URL. Old URL becomes invalid; Teams will bounce future posts to it. No code changes needed.

**Q: Can we suppress notifications temporarily?**  
A: Yes — Gandalf can set `.squad/notifications-state.json` with `"paused": true` to silence everything. Resume by removing the flag or setting `"paused": false`.

**Q: What if feature complete is detected but testing instructions are missing?**  
A: Send notification anyway, but omit testing section. Include link to README if available. Format: "See README for testing steps" + link.

**Q: How do we know if a notification failed?**  
A: Check `.squad/notifications-errors.log`. Failed events are logged with timestamp, event ID, and error. Log is human-readable for debugging.

**Q: What happens if Teams webhook is down for days?**  
A: Notifications will fail and be logged, but won't block calling code. Once webhook is restored, notifications resume. Old events in watermark (>7 days) are purged on next `notify.ps1` call to prevent unbounded growth.

---

## References

- **Existing notification script:** `scripts/send-teams-notification.ps1`
- **Daily summary:** `scripts/squad-daily-summary.ps1`
- **Notification skill:** `.squad/skills/squad-notifications/SKILL.md`
- **Failure recovery pipeline:** `.squad/failure-recovery.md`
- **Adaptive Card docs:** https://adaptivecards.io/explorer/
- **Teams webhooks:** https://docs.microsoft.com/en-us/microsoftteams/platform/webhooks-and-connectors/how-to/connectors-using

---

## Sign-Off

**Design Status:** Pending Gandalf Sign-Off & Jonathan Approval  
**Implementation Owner:** Gimli  
**Review Owner:** Galadriel  
**Estimated Implementation:** 3 weeks (Phase 0–3)
