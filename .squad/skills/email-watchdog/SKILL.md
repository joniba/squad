# Email Watchdog Skill

**Status:** Production-ready (POC validated 2025-01-XX)

## Overview

The Email Watchdog scans your Outlook inbox for action items, decisions requiring input, urgent requests, and meeting follow-ups from the last 24 hours (or custom window). It uses WorkIQ's natural language email querying to extract structured, actionable summaries and optionally delivers them to a Teams webhook.

**Key Features:**
- Single-agent consolidation (1 premium request instead of 3-step pipeline)
- Structured markdown output (compatible with Teams formatting)
- Optional Teams webhook integration for automated delivery
- Customizable lookback window (default: 24 hours)
- Prioritized action items with sender context

## Architecture

### Why This Pattern?

Email watchdogs traditionally need a multi-step pipeline:
1. **Filter:** Query WorkIQ for recent emails requiring action
2. **Extract:** Parse unstructured results for action items
3. **Format:** Convert to Teams-compatible markdown
4. **Deliver:** POST to Teams webhook

This skill consolidates steps 1-3 into a single copilot -p call with WorkIQ, reducing cost (1 premium request vs 3) while maintaining output fidelity. Only step 4 (Teams delivery) runs separately if a webhook is provided.

### Scripts

**poc-email-scan.ps1** (27 lines)
- Rapid validation script (POC only)
- Single 24-hour scan with structured output
- Use for testing and demos
- Exit: Markdown summary to stdout

**email-scan.ps1** (54 lines)
- Production script with optional Teams delivery
- Parameters: `-Hours` (default 24), `-TeamWebhook` (optional)
- Output categories:
  - Action Items Assigned to Me
  - Decisions Requiring Input
  - Urgent Requests (🔴 priority flag)
  - Meeting Follow-ups
  - Summary (1-2 sentences)
- Exit: Markdown to stdout + Teams webhook if URL provided

## Usage

### Quick Scan (POC)
```powershell
cd .squad/skills/email-watchdog
.\poc-email-scan.ps1
.\poc-email-scan.ps1 -Hours 48  # Custom lookback window
```

### Production Scan
```powershell
# Stdout only
.\email-scan.ps1 -Hours 24

# With Teams delivery
$webhook = "https://outlook.webhook.office.com/webhookb2/..."
.\email-scan.ps1 -Hours 24 -TeamWebhook $webhook
```

### Scheduled Execution (in .squad/scheduler.json)
```json
{
  "tasks": [
    {
      "name": "email-scan",
      "script": ".squad/skills/email-watchdog/email-scan.ps1",
      "schedule": "0 9 * * MON-FRI",  // 9 AM, weekdays
      "params": {
        "Hours": 24,
        "TeamWebhook": "env:EMAIL_WATCHDOG_WEBHOOK"
      },
      "enabled": true
    }
  ]
}
```

## WorkIQ Integration

### Query Pattern
```
Using workiq-ask_work_iq tool, scan emails from last [Hours] hours.
Extract:
- Action items assigned to me (with sender/subject)
- Decisions requiring my input
- Urgent/time-sensitive requests
- Meeting follow-ups
- Summary of workload/priorities
```

**Response Format:** Structured markdown with priorities, sender names, email subjects, and context.

### Rate Limiting
- WorkIQ is **poll-based with indexing delay** (not real-time)
- Documented in elrond/history.md as: "Well-suited for daily batch summary design"
- **Rate limit:** 1 query per agent cycle (typically daily or per shift)
- Scheduler config enforces this automatically

### Cost Estimate
- **Per execution:** 1 premium request (copilot -p with WorkIQ tool access)
- **Daily (9 AM weekdays):** ~5 premium requests/week
- **Monthly:** ~20 premium requests
- **Baseline:** Significant cost reduction vs multi-step pipeline (3x savings)

## Output Examples

### High-Priority Email Window
```markdown
## 📧 Action Items Assigned to Me
- **Porat Arzouan** — *RE: CDP Usop test migration* — Provide status update on CMK Sentinel onboarding
- **Amir Skovronik** — *[PROD] Sev 4: Enable MDTI Premium Sentinel* — Acknowledge on-call assignment; be ready for engineering action
- **GitHub** — *Squad pipeline failures* — Investigate systemic CI/automation breakage (Heartbeat, Triage)

## 🎯 Decisions Requiring My Input
- **CMK Sentinel:** Confirm completion status or flag blockers
- **Squad Workflows:** Decide whether to fix, rollback, or silence failing automation

## 🔴 Urgent Requests
- [PROD] Sev-4 MDTI incident (active, production, on-call)
- CMK onboarding status (blocking downstream progress)

## 📋 Meeting Follow-ups
(None in last 24 hours)

## 📌 Summary
Your top 3 actions: acknowledge Sev-4 incident, reply to Porat with CMK status, investigate squad automation failures.
```

## Limitations & Known Issues

1. **Indexing Delay:** WorkIQ emails are poll-based, not real-time. Very recent emails (last 10-15 min) may not appear until next cycle.
2. **Unread Flag:** WorkIQ can filter "unread emails" but may include "implicitly read" messages.
3. **Meeting Context:** WorkIQ extracts action items from email threads but doesn't have direct access to Teams meeting notes.
4. **Attachment Filtering:** WorkIQ can mention attachments but doesn't parse attachment contents.

## Integration with Unified Scheduler

This skill depends on **Issue #75** (unified scheduler). Once integrated:
- Email watchdog runs on schedule (default: 9 AM, weekdays)
- Results can feed into task management system
- Teams webhook enables real-time delivery to monitored channels

## Related Documentation

- **Teams Watchdog Pattern:** `.squad/skills/teams-watchdog/README.md`
- **Research:** `docs/research/teams-outlook-integration-research.md`
- **Squad Decisions:** `.squad/decisions.md` (copilot -p policy, composable scripts philosophy)
- **Elrond History:** `.squad/agents/elrond/history.md` (WorkIQ learnings, rate-limit guidance)

## Testing Checklist

- [x] POC script validates WorkIQ email query (3 test queries passed)
- [x] Structured markdown output matches Teams webhook requirements
- [x] Production script handles optional webhook parameter correctly
- [x] Error handling for invalid email lookback windows
- [ ] Scheduler integration test (pending Issue #75 completion)
- [ ] Full end-to-end Teams delivery test (pending webhook URL)

## Future Enhancements

1. **AI Triage:** Automatically classify action items by priority/project
2. **Integration with unified scheduler:** Auto-create tasks for extracted action items
3. **Attachment Summary:** Extract and summarize action items from common attachment types (meeting notes, PDFs)
4. **Multi-account support:** Scan shared mailboxes or delegate accounts
5. **Custom filters:** "Only show actions assigned to me," "Hide emails from distribution lists," etc.
