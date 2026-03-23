---
title: "Teams Notifications Webhook Setup & Usage"
date: 2024-12-19
author: Gimli
documentarian: Bilbo
category: guide
tags: [guide, tooling, teams, reusable, bilbo, final]
status: final
---

# Teams Notifications Webhook Setup & Usage

Send structured notifications from agents to a Microsoft Teams channel using Incoming Webhooks and Adaptive Cards.

## What It Does

Teams Notifications provide a one-way communication channel from agents to Jonathan (or the squad) via a Teams channel. Use this to:

- Alert on blocked issues or stale PRs
- Report daily squad summaries
- Notify on incident resolution
- Post PR merge confirmations
- Send agent errors or pipeline failures

This guide covers the setup (one-time), webhook storage (per machine), and scripted notification sending.

**Note:** This skill is **reusable for future squads**. The webhook setup, message format (Adaptive Cards), and scripts are standardized and portable.

## Prerequisites

- **PowerShell 7+** (Core or Desktop)
- **Microsoft Teams** with a channel you control (or access to create/configure)
- **Write access** to the channel webhooks configuration
- **Local file storage** for the webhook URL (`~/.squad/teams-webhook.url`)
- **`scripts/send-teams-notification.ps1`** available in the repo

## Part 1: One-Time Webhook Setup in Teams (~2 minutes)

### Step 1: Open the Target Channel

In Microsoft Teams, open the channel where you want notifications to arrive (e.g., `#squad-notifications`).

### Step 2: Configure Incoming Webhook

1. Click the **⋯ (three dots)** in the channel header
2. Select **Connectors → Incoming Webhook → Configure**
3. Name your webhook (e.g., `Squad Bot`, `pa-squad Notifications`)
4. Optionally upload an icon/image
5. Click **Create**

Teams generates a unique webhook URL and displays it on screen.

### Step 3: Copy the Webhook URL

The URL looks like:
```
https://outlook.webhook.office.com/webhookb2/xxxxx@xxxxx/IncomingWebhook/yyyyyyy/zzzzzzzzz
```

**Do NOT commit this to git.** It's a secret. Treat it like a password.

### Step 4: Store the URL Locally

Run this PowerShell command to save the URL securely on your machine:

```powershell
$webhookDir = Join-Path $HOME ".squad"
New-Item -Path $webhookDir -ItemType Directory -Force | Out-Null
Set-Content -Path (Join-Path $webhookDir "teams-webhook.url") -Value "<paste the webhook URL here>"
```

Verify it was saved:

```powershell
Get-Content ~/.squad/teams-webhook.url
```

The file is stored at:
- **Windows:** `C:\Users\<YourUsername>\.squad\teams-webhook.url`
- **macOS/Linux:** `~/.squad/teams-webhook.url`

**Security note:** This file contains your webhook secret. Keep it private. On Unix systems, you may want to set restrictive permissions:

```bash
chmod 600 ~/.squad/teams-webhook.url
```

## Part 2: Sending Notifications

### Basic Usage

Use the reusable script `scripts/send-teams-notification.ps1`:

```powershell
.\scripts\send-teams-notification.ps1 -Title "🚨 Blocked Issue" -Body "Issue #42 has been blocked for 18 hours."
```

The script automatically reads the webhook URL from `~/.squad/teams-webhook.url`.

### Custom Webhook File

If you store the webhook URL elsewhere:

```powershell
.\scripts\send-teams-notification.ps1 -Title "Daily Summary" -Body "3 blocked, 1 stale PR" -WebhookFile "C:\path\to\webhook.url"
```

### From Another Script

Call the notification script from your automation:

```powershell
# In your agent/orchestration script
& "$repoRoot\scripts\send-teams-notification.ps1" `
    -Title "🟢 Deployment Complete" `
    -Body "pa-squad v1.2.3 deployed to production." `
    -WebhookFile $webhookFile
```

### Markdown Content

The `Body` parameter accepts plain text and basic markdown formatting. Teams will render:

```powershell
.\scripts\send-teams-notification.ps1 `
    -Title "📊 Squad Status" `
    -Body @"
**Blocked Issues:** 2
- #42: Auth service migration
- #51: Storage quota limits

**Stale PRs (>24h):** 1
- #48: Refactor semantic model

**Action Items:**
- Aragorn: Investigate auth issue
- Gimli: Code review for PR #48
"@
```

## Part 3: Real-World Examples

### Daily Squad Summary

From `scripts/squad-daily-summary.ps1`:

```powershell
.\scripts\send-teams-notification.ps1 `
    -Title "📊 Daily Squad Summary" `
    -Body $summaryBody `
    -WebhookFile $webhookFile
```

(See `docs/guides/teams-watchdog-setup.md` for full daily summary logic.)

### Incident Alert

```powershell
.\scripts\send-teams-notification.ps1 `
    -Title "🚨 Sev-2 Incident: Authentication Service Down" `
    -Body @"
**Time:** 2024-12-19 14:35 UTC
**Impact:** Users cannot log in (estimated 2,500 affected)
**Status:** Investigating
**Owner:** Aragorn (on-call)
**Last Update:** 14:45 UTC - Rolled back to previous deployment. Service recovering.
"@
```

### PR Merge Notification

```powershell
.\scripts\send-teams-notification.ps1 `
    -Title "✅ PR Merged: Semantic Model Query Optimization" `
    -Body "PR #53 merged to main. 15% query latency improvement. Gimli: Great work on the benchmarks!"
```

### Agent Error/Pipeline Failure

```powershell
.\scripts\send-teams-notification.ps1 `
    -Title "⚠️ Agent Task Failed" `
    -Body @"
**Agent:** Gimli (tooling)
**Task:** Semantic model rebuild
**Error:** GitHub API rate limit exceeded
**Retry:** Auto-retry in 1 hour
**Logs:** ~/.copilot/logs/session-abc123.log
"@
```

## Configuration: When to Notify

This table defines which agents own which notification triggers:

| Event | Trigger Condition | Owner | Frequency |
|-------|-------------------|-------|-----------|
| Daily summary | Scheduled (9 AM) | Ralph / Gandalf | Daily |
| Blocked issue alert | Issue labeled `blocked` for >12 hours | Ralph / Gandalf | On trigger |
| Stale PR alert | PR with no review activity for >24 hours | Ralph / Gandalf | On trigger |
| ICM incident (Sev 2+) | New Sev 2+ incident detected | Aragorn | On trigger |
| PR merged | PR completion on ms-pa | Any agent | On trigger |
| Agent error / pipeline failure | Script exits with non-zero code | Any agent | On trigger |

## Message Format: Adaptive Cards

All squad notifications use **Adaptive Cards** — a rich, structured format. The `send-teams-notification.ps1` script handles the format automatically, but here's what it sends:

```json
{
  "type": "message",
  "attachments": [
    {
      "contentType": "application/vnd.microsoft.card.adaptive",
      "content": {
        "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
        "type": "AdaptiveCard",
        "version": "1.4",
        "body": [
          {
            "type": "TextBlock",
            "size": "Medium",
            "weight": "Bolder",
            "text": "📊 Title Here"
          },
          {
            "type": "TextBlock",
            "text": "Body content here.",
            "wrap": true
          }
        ]
      }
    }
  ]
}
```

**Constraints:**
- Max payload size: 28 KB per message
- Rate limit: 4 requests/second
- All text blocks should have `wrap: true` to prevent clipping

## Security Rules

**IMPORTANT:** Follow these rules strictly:

1. **Never commit webhook URLs to git** — store locally at `~/.squad/teams-webhook.url`
2. **Never log or print webhook URLs** — treat as a secret credential
3. **Never include secrets in notification body** — no passwords, tokens, keys, credentials
4. **Rotate webhook URLs if compromised** — regenerate in Teams, update local file
5. **Restrict file permissions** on `~/.squad/teams-webhook.url` where possible (Unix: `chmod 600`)
6. **Use the reusable script** — don't implement webhook logic manually in multiple places

## Troubleshooting

### "Webhook URL file not found: ~/.squad/teams-webhook.url"

The webhook URL wasn't stored. Run the setup steps again:

```powershell
$webhookDir = Join-Path $HOME ".squad"
New-Item -Path $webhookDir -ItemType Directory -Force | Out-Null
Set-Content -Path (Join-Path $webhookDir "teams-webhook.url") -Value "<paste URL>"
```

### "Webhook URL file is empty"

The file exists but contains no data. Edit it:

```powershell
notepad ~/.squad/teams-webhook.url
```

Paste the webhook URL and save.

### Notification Fails to Send (HTTP Error)

1. **Verify the URL is correct:**
   ```powershell
   $url = Get-Content ~/.squad/teams-webhook.url
   $url.Length  # Should be 100+ characters
   ```

2. **Test the webhook directly:**
   ```powershell
   $url = (Get-Content ~/.squad/teams-webhook.url).Trim()
   $body = @{
       type = "message"
       text = "Test notification"
   } | ConvertTo-Json
   Invoke-RestMethod -Uri $url -Method Post -ContentType "application/json" -Body $body
   ```

3. **Check if the webhook was revoked:**
   - Go back to the Teams channel settings
   - Remove and recreate the webhook
   - Save the new URL to `~/.squad/teams-webhook.url`

### Rate Limit Exceeded (429 Errors)

Teams webhooks allow 4 requests/second per channel. If you hit this:

1. Add delays between notifications:
   ```powershell
   Start-Sleep -Seconds 1
   ```

2. Batch multiple items into a single notification

3. Check if another script/agent is also sending to the same webhook

### Notification Truncated or Formatting Broken

Teams has a 28 KB payload size limit. If messages are truncated:

1. Reduce the body content length
2. Remove unnecessary formatting
3. Split large messages across multiple notifications

## Advanced: Building Custom Notification Scripts

If you need notifications with more complex logic, use the `send-teams-notification.ps1` script as a template. Key points:

1. **Read the webhook URL securely:**
   ```powershell
   $webhookUrl = (Get-Content $WebhookFile -First 1).Trim()
   ```

2. **Validate before sending:**
   ```powershell
   if ([string]::IsNullOrWhiteSpace($webhookUrl)) {
       Write-Error "Webhook URL is empty"
       exit 1
   }
   ```

3. **Build Adaptive Card JSON** with your custom content

4. **POST via `Invoke-RestMethod`:**
   ```powershell
   Invoke-RestMethod -Uri $webhookUrl -Method Post -ContentType "application/json" -Body $json
   ```

5. **Handle errors gracefully:**
   ```powershell
   try {
       Invoke-RestMethod ... | Out-Null
       Write-Host "✅ Notification sent"
   } catch {
       Write-Error "Failed to send: $_"
       exit 1
   }
   ```

## Related

- `scripts/send-teams-notification.ps1` — The reusable notification script
- `scripts/squad-daily-summary.ps1` — Daily summary example (queries GitHub, sends Teams notification)
- `.squad/skills/squad-notifications/SKILL.md` — Architecture and constraints
- `docs/guides/teams-watchdog-setup.md` — Teams message monitoring (opposite direction)

## Next Steps

- **For Gimli (tooling):** Standardize notification templates for each event type (incidents, PRs, errors)
- **For Jonathan (team lead):** Tune the daily summary to include only the decisions/items you care about
- **For future squads:** This setup is fully reusable — just create a new webhook in your Teams channel and store the URL
