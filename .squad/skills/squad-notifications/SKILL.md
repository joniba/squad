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

Use the reusable script at `scripts/send-teams-notification.ps1`:

```powershell
# Basic usage (reads webhook URL from default location)
.\scripts\send-teams-notification.ps1 -Title "🚨 Blocked Issue" -Body "Issue #42 has been blocked for 18 hours."

# Custom webhook file
.\scripts\send-teams-notification.ps1 -Title "Daily Summary" -Body "3 blocked, 1 stale PR" -WebhookFile "C:\path\to\webhook.url"
```

For the daily summary specifically, use `scripts/squad-daily-summary.ps1` which queries GitHub and calls `send-teams-notification.ps1` automatically.

## Security Rules

- **NEVER** commit the webhook URL to version control.
- **NEVER** include secrets, tokens, or credentials in notification body text.
- Webhook URLs are rotatable — if compromised, regenerate in Teams and update the file.
- The URL file should have user-only read permissions where possible.

## Rate Limiting

- Teams webhooks: max 4 req/sec, 28 KB/message.
- WorkIQ: max 1 query per agent cycle (squad standard).
- Daily summary: 1 notification/day to avoid fatigue.
