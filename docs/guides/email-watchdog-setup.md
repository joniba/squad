---
title: "Email Watchdog Setup Guide"
date: 2026-03-24
author: gimli
documentarian: bilbo
category: guide
tags:
  - guide
  - tooling
  - workflow
  - teams
  - bilbo
status: final
related_docs:
  - guides/unified-scheduler-guide.md
  - tools/email-watchdog-tool.md
related_issues: []
---

# Email Watchdog Setup Guide

The **Email Watchdog** scans your Outlook inbox for action items, decisions requiring your input, urgent requests, and meeting follow-ups. It uses WorkIQ's natural language email querying to extract structured summaries and optionally deliver them to Teams.

## What It Does

- **Scans Outlook** for emails requiring action (custom lookback window, default 24 hours)
- **Extracts action items** — automatically categorizes actions assigned to you, decisions needing input, urgent requests, and meeting follow-ups
- **Formats as markdown** — produces clean, readable summaries compatible with Teams
- **Delivers to Teams** — optionally posts summaries to a Teams webhook for centralized visibility
- **Reduces email overwhelm** — focus on actionable items, not noise

---

## Prerequisites

1. **Copilot CLI** — must be installed and authenticated to your Microsoft 365 account
2. **WorkIQ MCP tool** — must be available in your Copilot configuration (for email querying)
3. **Outlook mailbox** — the script scans your primary Outlook inbox
4. **Teams webhook** (optional) — for delivery to Teams channels

### Checking Prerequisites

**Copilot CLI installed?**
```powershell
copilot --version
```

**WorkIQ available?**
```powershell
copilot -p "Show me my last 3 emails using workiq" --allow-tool='workiq'
```

---

## Quick Start

### Run a Manual Scan

```powershell
# Scan last 24 hours, print to console
.\.squad\skills\email-watchdog\email-scan.ps1 -Hours 24

# Scan last 48 hours
.\.squad\skills\email-watchdog\email-scan.ps1 -Hours 48
```

**Example output:**
```markdown
## 📧 Action Items Assigned to Me
- **Porat Arzouan** — Fix database migration script for CDM
- **Amir Skovronik** — Review PR for Sentinel changes
- **GitHub** — Investigate failing pipeline tests

## 🎯 Decisions Requiring My Input
- CDM migration scope — confirm go/no-go with team
- Sentinel deprecation timeline — decide on timeline with product

## 🔴 Urgent Requests
- [PROD] Sev-4 incident — on-call assignment active

## 📋 Meeting Follow-ups
- Design review — share feedback on new API design
- Retrospective — capture lessons learned notes

## 📌 Summary
Top 3 actions: acknowledge Sev-4, review database migration script, confirm CDM scope with team.
```

### Deliver to Teams

First, get your Teams webhook URL. Then:

```powershell
$webhook = "https://outlook.webhook.office.com/webhookb2/..."
.\.squad\skills\email-watchdog\email-scan.ps1 -Hours 24 -TeamWebhook $webhook
```

The script prints output to console AND posts to Teams.

---

## CLI Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `-Hours` | int | 24 | Lookback window for email scan (in hours) |
| `-TeamWebhook` | string | (none) | Teams webhook URL for delivery (optional) |

### Examples

```powershell
# Scan last 24 hours, console only
.\.squad\skills\email-watchdog\email-scan.ps1

# Scan last 48 hours, console only
.\.squad\skills\email-watchdog\email-scan.ps1 -Hours 48

# Scan last 24 hours, post to Teams
$webhook = "https://outlook.webhook.office.com/webhookb2/..."
.\.squad\skills\email-watchdog\email-scan.ps1 -Hours 24 -TeamWebhook $webhook
```

---

## Integration with Unified Scheduler

The Email Watchdog is already configured in `.squad/scheduler.json`:

```json
{
  "name": "email-watchdog",
  "script": ".squad/skills/email-watchdog/email-scan.ps1",
  "interval": "24h",
  "enabled": true,
  "condition": null,
  "params": {
    "Hours": 24,
    "TeamWebhook": "env:EMAIL_WATCHDOG_WEBHOOK"
  }
}
```

Once you set the `EMAIL_WATCHDOG_WEBHOOK` environment variable, the scheduler will automatically run the email watchdog daily and post results to Teams.

### Setting Up Scheduled Runs

1. **Store your webhook URL** in an environment variable:
   ```powershell
   [Environment]::SetEnvironmentVariable("EMAIL_WATCHDOG_WEBHOOK", "https://outlook.webhook.office.com/webhookb2/...", "User")
   ```
   (Restart your terminal for the change to take effect.)

2. **Verify the scheduler picks it up:**
   ```powershell
   .\scripts\squad-scheduler.ps1 -DryRun
   ```

3. **Start the scheduler:**
   ```powershell
   .\scripts\squad-scheduler.ps1
   ```
   The email watchdog will run every 24 hours.

---

## Output Categories

The email watchdog extracts and organizes action items into five sections:

### 1. 📧 Action Items Assigned to Me

Specific tasks people have explicitly asked you to do:
- Includes sender name and email subject for context
- Prioritized by urgency/deadline
- Examples: "Review PR", "Fix migration script", "Schedule meeting"

### 2. 🎯 Decisions Requiring My Input

Emails where your decision or input is needed:
- Decision context
- Deadline if mentioned
- Examples: "Confirm project scope", "Approve design", "Go/no-go decision"

### 3. 🔴 Urgent Requests

Time-sensitive or high-priority asks requiring immediate attention:
- Production incidents
- SLA-driven requests
- Examples: "[PROD] Sev-4 incident", "Critical hotfix needed"

### 4. 📋 Meeting Follow-ups

Action items from recent meetings:
- Meeting context
- Specific follow-up actions
- Examples: "Share retrospective notes", "Update design doc"

### 5. 📌 Summary

A 1-2 sentence summary of overall email workload and top priorities for the next 24 hours.

---

## WorkIQ Integration Details

The script uses WorkIQ's natural language email querying:

```
Using the workiq-ask_work_iq tool, scan my emails from the last [Hours] hours.
Extract and format as markdown:
[structured extraction prompt]
```

### Cost

- **Per execution:** 1 premium request (one `copilot -p` call with WorkIQ tool access)
- **Daily (if scheduled):** ~1 premium request/day = ~5 per week
- **Monthly:** ~20 premium requests
- **Savings:** Consolidates multi-step pipeline (3 steps = 3 premium requests) into single request

### Rate Limiting

WorkIQ is **poll-based with indexing delay** — not real-time. Very recent emails (last 10-15 minutes) may not appear until the next scan cycle.

**Recommendation:** Daily scans are ideal (captures all emails from prior day). More frequent scans (every 4 hours) may miss very recent messages but are still useful for high-volume inboxes.

---

## How It Works

1. **Query WorkIQ** — uses `copilot -p` with WorkIQ tool enabled
2. **Extract structured output** — WorkIQ returns markdown-formatted action items
3. **Print to console** — display summary locally
4. **Post to Teams (optional)** — if webhook URL provided, sends formatted message to Teams

The entire process typically takes 10-20 seconds.

---

## Troubleshooting

### "WorkIQ tool not available"

**Error:** `--allow-tool='workiq' not recognized`

**Fix:**
```powershell
# Verify WorkIQ is available in Copilot CLI
copilot -p "Show me my recent emails" --allow-tool='workiq'
```

If this fails, ensure:
- Copilot CLI is up-to-date: `copilot --version`
- You're authenticated to Microsoft 365: `copilot --auth login`
- WorkIQ tool is enabled in your Copilot configuration

### "No action items found"

**Output:** "No action items in the last X hours."

**Cause:** Either your inbox has no actionable emails or WorkIQ's indexing hasn't caught up (typical delay: 10-15 minutes).

**Fix:**
- Wait a few minutes and re-run
- Try a longer lookback window: `-Hours 48`
- Manually check your inbox to confirm emails exist

### "Teams delivery failed"

**Error:** `Invoke-RestMethod: The remote server returned an error`

**Fix:**
1. Verify webhook URL is correct: `$webhook = "https://outlook.webhook.office.com/webhookb2/..."`
2. Check webhook hasn't expired — regenerate it in Teams if needed
3. Test the webhook manually:
   ```powershell
   $payload = @{ text = "Test" } | ConvertTo-Json
   Invoke-RestMethod -Uri $webhook -Method POST -ContentType 'application/json' -Body $payload
   ```

### Script is slow or timing out

**Cause:** WorkIQ query taking >30 seconds (typical: 10-20 seconds)

**Fix:**
- Try a shorter lookback window: `-Hours 12` instead of `-Hours 24`
- Run during off-peak hours (WorkIQ may have lower latency)
- Check network connectivity

---

## Scripts Included

### `email-scan.ps1` (Production)

Full-featured production script:
- Parameters: `-Hours` (default 24), `-TeamWebhook` (optional)
- Output: Markdown to console + Teams webhook
- Error handling for invalid parameters
- ~54 lines

Use for:
- Scheduled execution (via unified scheduler)
- Regular manual scans with Teams delivery
- Integration with other workflows

### `poc-email-scan.ps1` (Proof of Concept)

Rapid validation script:
- Simpler parameter handling
- Console output only
- ~27 lines

Use for:
- Testing and demos
- Quick scans without Teams delivery
- Prototyping changes

---

## Best Practices

### Daily Runs

Set up the scheduler to run email watchdog every morning:

```powershell
# Set environment variable for Teams webhook
[Environment]::SetEnvironmentVariable("EMAIL_WATCHDOG_WEBHOOK", "https://...", "User")

# Start scheduler
.\scripts\squad-scheduler.ps1
```

### Manual Runs Before Meetings

Before important meetings, run a quick scan to get context:

```powershell
.\.squad\skills\email-watchdog\email-scan.ps1 -Hours 4
```

### Expanding Lookback Window

If you're returning from time off, scan a longer window:

```powershell
.\.squad\skills\email-watchdog\email-scan.ps1 -Hours 72  # Last 3 days
```

---

## Related Documentation

- **Unified Scheduler Guide** — `docs/guides/unified-scheduler-guide.md` (how to schedule email watchdog runs)
- **Email Watchdog Tool Spec** — `docs/tools/email-watchdog-tool.md` (technical details)
