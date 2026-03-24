# Squad Notifications

> Send structured notifications to a Microsoft Teams channel via Incoming Webhooks.

## When to Use

Any agent that needs to notify Jonathan or the squad of an event — blocked items, stale PRs, ICM incidents, PR merges, daily summaries — uses this skill. The notification channel is one-way: agents post, Jonathan reads in Teams.

## Webhook Setup (One-Time, ~2 Minutes)

1. In Microsoft Teams, open the target channel (e.g. `#squad-notifications`).
2. Click **⋯ → Connectors → Incoming Webhook → Configure**.
3. Name it (e.g. `Squad Bot`), optionally set an icon, click **Create**.
4. Copy the generated URL.
5. Save it to a local file — **never commit this to git**:
   ```powershell
   $webhookDir = Join-Path $HOME ".squad"
   New-Item -Path $webhookDir -ItemType Directory -Force | Out-Null
   Set-Content -Path (Join-Path $webhookDir "teams-webhook.url") -Value "<paste URL>"
   ```

The webhook URL is stored at `~/.squad/teams-webhook.url` by convention (one URL per line, no trailing whitespace).

## Message Format: Adaptive Cards

Teams webhooks accept **Adaptive Card** payloads. All squad notifications MUST use this format:

```json
{
  "type": "message",
  "attachments": [
    {
      "contentType": "application/vnd.microsoft.card.adaptive",
      "contentUrl": null,
      "content": {
        "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
        "type": "AdaptiveCard",
        "version": "1.4",
        "body": [
          { "type": "TextBlock", "size": "Medium", "weight": "Bolder", "text": "📊 Title Here" },
          { "type": "TextBlock", "text": "Body content here.", "wrap": true }
        ]
      }
    }
  ]
}
```

**Constraints:**
- Max payload: 28 KB
- Rate limit: 4 requests/sec
- Use `wrap: true` on all TextBlocks to prevent clipping

## When to Notify

| Event | Trigger | Agent |
|-------|---------|-------|
| Daily summary | Scheduled (9 AM) | Any / cron |
| Blocked issue detected | Issue labeled `blocked` for >12h | Ralph / Gandalf |
| Stale PR | PR with no review activity for >24h | Ralph / Gandalf |
| ICM incident (Sev2+) | New Sev2 or higher incident | Aragorn |
| PR merged | PR completion on ms-pa | Any |
| Agent error / pipeline failure | Script exits non-zero | Ralph |

## How to Send a Notification

There are TWO notification systems. The old system is active; the new system is built but has no callers yet.

### Old System (ACTIVE — 2 callers)

`scripts/send-teams-notification.ps1` — simple Title+Body notification sender.

**Callers:**
- `scripts/icm-scan.ps1` (line 286) — ICM incident alerts
- `scripts/squad-daily-summary.ps1` (line 56) — daily summary

```powershell
# Basic usage (reads webhook URL from default location)
.\scripts\send-teams-notification.ps1 -Title "🚨 Blocked Issue" -Body "Issue #42 has been blocked for 18 hours."

# Custom webhook file
.\scripts\send-teams-notification.ps1 -Title "Daily Summary" -Body "3 blocked, 1 stale PR" -WebhookFile "C:\path\to\webhook.url"
```

> ⚠️ **Do NOT remove or replace the old system until the new system's MVP callers are validated end-to-end.**

### New System (BUILT — 0 callers yet)

`scripts/notify.ps1` — structured notification router with event schemas, deduplication, and recovery.

**Supporting scripts:**
- `scripts/notification-recovery.ps1` — retry/recovery for failed deliveries
- `scripts/notification-scheduler.ps1` — scheduled/batched notification delivery

**Planned MVP callers (not yet built — see issues #132–#135):**
- `scripts/notify-feature-complete.ps1` — batched hourly, concise summary of big completed features
- `scripts/notify-blocked.ps1` — immediate notification when anything requires Jonathan's urgent attention

```powershell
# New system dry-run example
.\scripts\notify.ps1 -Type "urgent" -Event @{
    eventId = "test:$(Get-Date -Format 'yyyy-MM-ddTHH-mm-ss')"
    title = "🔴 Blocked — agent needs human input"
    reason = "Elrond can't find solution"
    actionUrl = "https://github.com/jbenami_microsoft/ms-pa/issues/999"
    actionLabel = "View Issue"
} -DryRun
```

For the daily summary specifically, use `scripts/squad-daily-summary.ps1` which queries GitHub and calls `send-teams-notification.ps1` (old system) automatically.

## Security Rules

- **NEVER** commit the webhook URL to version control.
- **NEVER** include secrets, tokens, or credentials in notification body text.
- Webhook URLs are rotatable — if compromised, regenerate in Teams and update the file.
- The URL file should have user-only read permissions where possible.

## Rate Limiting

- Teams webhooks: max 4 req/sec, 28 KB/message.
- WorkIQ: max 1 query per agent cycle (squad standard).
- Daily summary: 1 notification/day to avoid fatigue.

## Notification Criteria

Only send notifications when there is actionable information. **Never send empty updates.**

### Always Notify
| Condition | Why |
|-----------|-----|
| Blocked issues (>0) | Something is stuck, needs attention |
| Stale PRs (no update >24h) | Work is languishing |
| CRI detected (new, unacknowledged) | Customer impact |
| Sev2 incident (new or unmitigated) | Urgent livesite |
| Design review awaiting approval (>24h) | Blocking downstream work |
| Agent failed repeatedly (3+ failures) | System health |

### Never Notify
| Condition | Why |
|-----------|-----|
| Board is clear, no blocked items | Empty update = noise |
| All PRs recently updated | Things are moving, no action needed |
| Routine task completed | Not urgent, visible in logs |

### Evolving This List
Add new criteria to the tables above. The daily summary script checks these conditions — if none are true, it exits silently.
