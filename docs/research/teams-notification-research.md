---
title: "Teams Channel Notifications for Squad: Research & Recommendations"
date: 2025-01-16
author: Elrond
documentarian: bilbo
category: research
tags:
  - research-method
  - workflow
  - teams
  - squad-infra
  - tooling
status: final
---

# Teams Channel Notifications for Squad: Research & Recommendations

## Executive Summary

This document answers **6 key research questions** about enabling real-time squad notifications in Microsoft Teams channels for blocked tasks, stale items, and significant events. Based on investigation of Tamir's existing plugins, Microsoft Graph API capabilities, Teams webhooks, WorkIQ MCP, and Power Automate, the **recommended approach is a hybrid solution**: poll WorkIQ on a cadence, filter for actionable events, and post summaries to Teams via **Incoming Webhooks** (primary) or **Graph API** (future-proof alternative). This approach is production-ready, leverages existing squad infrastructure, and aligns with EMU/Microsoft-internal constraints.

---

## Research Methodology

This research synthesized evidence from:

1. **Existing Squad-Skills Plugins** (from tamirdresher/squad-skills):
   - `teams-monitor`: WorkIQ query patterns, Teams webhook delivery, security best practices
   - `news-broadcasting`: Adaptive Card formatting, webhook posting, team communication patterns
   - `teams-ui-automation`: UI automation constraints and layer-based approach

2. **Web Research** (verified via Microsoft documentation and third-party integration guides):
   - Teams Incoming Webhook setup and rate limits
   - Microsoft Graph API authentication flows and permissions
   - Power Automate triggers and Teams integration patterns
   - PAT vs. App Registration security models for EMU accounts

3. **Team Decisions** (from `.squad/decisions.md` and Elrond history):
   - WorkIQ is poll-based with indexing delay; recommended rate-limit: 1 query per agent cycle
   - Squad-skills plugin catalog documents proven patterns
   - Webhook delivery pattern is production-ready

---

## Q1: What Teams Integration Options Are Available?

### Answer

Microsoft provides **three primary channels** for posting to Teams:

| Option | API | Auth | Real-time? | Setup Complexity |
|--------|-----|------|-----------|-----------------|
| **Incoming Webhooks** | REST POST (app/json) | URL (secret, stored securely) | ~seconds | 2 minutes |
| **Microsoft Graph API** | `POST /teams/{id}/channels/{id}/messages` | OAuth 2.0 (delegated) + app registration | Immediate | 15+ minutes |
| **Power Automate Flows** | Hybrid (triggers + Graph/Webhooks) | Azure auth (managed by tenant) | ~seconds | Varies (UI-driven) |
| **Teams Native GitHub App** | GitHub webhook → Teams built-in | GitHub connection | Real-time | 1 minute |

### Details

**Incoming Webhooks** (✅ Recommended for squad)
- Setup: Connectors tab in Teams channel → "Incoming Webhook" → generates unique URL
- Payload: POST JSON with Adaptive Card format (`application/vnd.microsoft.card.adaptive`, v1.0+)
- Rate limits: 4 requests/sec, 28 KB message max
- Security: URL is secret; stored in `~\.squad\teams-webhook.url` (per news-broadcasting plugin)
- Delivery: PowerShell `Invoke-RestMethod` (proven in squad-skills)
- **Status:** Production-ready, currently used by squad

**Microsoft Graph API** (✅ Future-proof alternative)
- Endpoint: `POST https://graph.microsoft.com/v1.0/teams/{team-id}/channels/{channel-id}/messages`
- Permission: Delegated scope `ChannelMessage.Send` (least privileged for posting as user)
- ⚠️ Note: Application-only permissions (`Teamwork.Migrate.All`) exist but are for migration, not general posting
- Auth flow: OAuth 2.0 (requires app registration + admin consent in EMU tenant)
- Setup time: ~15 minutes (app registration + permission grant)
- **Status:** Production-ready, more scalable than webhooks, requires tenant admin coordination

**Power Automate** (⚠️ Feasible but adds abstraction layer)
- Trigger options: HTTP request, GitHub event (via connector), scheduled
- Action: "Post message to Teams" action with Adaptive Cards
- Integration: Bridges GitHub Actions workflows → Teams messages
- **Status:** Viable but adds operational overhead; webhooks are simpler for agents

**Teams GitHub App** (✅ Useful for GitHub-specific events)
- Setup: 1 command in Teams (`@github subscribe owner/repo`)
- Events: Push, PR, release, issues, discussions
- **Limitation:** Fixed message format; not customizable for squad-specific filtering
- **Status:** Good for repository notifications; insufficient for squad-wide task monitoring

### Evidence Source
- teams-monitor SKILL.md: Webhook delivery pattern documented
- news-broadcasting SKILL.md: Webhook POST via PowerShell, Adaptive Card formatting
- Microsoft Graph API documentation: Permission scopes and endpoints
- Power Automate docs: GitHub connector capabilities and Teams actions

---

## Q2: How Does WorkIQ Enable Task/Event Detection?

### Answer

**WorkIQ** is a Microsoft Graph-based MCP tool that agents use to query Teams messages, documents, and activities. It is **poll-based, not event-driven**, with typical indexing delay of minutes to hours.

### WorkIQ Query Patterns (from teams-monitor)

The teams-monitor skill demonstrates proven WorkIQ queries for identifying actionable content:

| Query Type | Example | Purpose |
|-----------|---------|---------|
| **Recent messages** | "What messages were sent in the last 24h related to DK8S, squad, or AI agents?" | Catch new requests |
| **Sender-specific** | "What did Tamir Dresher say recently about priorities or action items?" | Track directives |
| **Incident/urgent** | "Any recent Teams messages about outages, Sev2 incidents, or urgent Kubernetes issues?" | Escalations |
| **Meeting follow-ups** | "What action items came out of recent team meetings?" | Extract tasks |
| **Topic-based** | "Any recent Teams discussion about [current_topic_from_decisions.md]?" | Context-aware monitoring |

### Filtering Strategy (from teams-monitor)

**Actionable content includes:**
- Direct requests: "Can you look into...", "We need...", "Please investigate..."
- Questions awaiting answers: "Has anyone figured out...", "What's the status of..."
- Decisions/announcements affecting squad work
- Escalations or incidents

**Ignore:**
- Social/casual messages
- Already-processed items (check GitHub issues first)
- Automated notifications (already in GitHub/ADO)

### Rate-Limiting & Cadence

Per squad decisions:
- **Limit:** 1 WorkIQ query per agent cycle (to avoid rate-limiting)
- **Cadence:** Poll every 20 minutes (teams-monitor uses scheduler) or on-demand per agent session
- **Indexing delay:** Typically minutes; occasionally hours
- **Consequence:** Poll-based, not real-time; eventual consistency model

### WorkIQ Limitations

- Query freshness subject to indexing delay
- No thread-level precision (threads may be merged in results)
- Read-only; cannot post to Teams (responses go through GitHub issues/comments)
- Channel visibility limited to authenticated user's access
- First implementation (confidence: LOW in teams-monitor)

### Evidence Source
- teams-monitor SKILL.md: Query patterns, filtering heuristics, rate-limit guidance, limitations
- Elrond history: WorkIQ is poll-based with indexing delay; documented in squad decisions

---

## Q3: What Webhook Setup & Security Patterns Should We Follow?

### Answer

**Incoming Webhooks are the current squad pattern.** Setup is minimal; security requires careful secret management.

### Setup Steps

1. **In Teams:**
   - Go to desired channel → ⋮ (More options) → Connectors → Incoming Webhook
   - Click "Configure" → Enter name (e.g., "Squad Notifications") → Optionally upload icon
   - Copy generated webhook URL

2. **On Local Machine:**
   - Store URL securely: `C:\Users\[username]\.squad\teams-webhook.url`
   - Never commit to repository
   - Use `Get-Content` to read when posting

3. **In PowerShell:**
   ```powershell
   $webhookUrl = (Get-Content "$env:USERPROFILE\.squad\teams-webhook.url" -Raw).Trim()
   $body = @{ 
       text = "📢 **Blocked Task Alert**"
   } | ConvertTo-Json
   Invoke-RestMethod -Uri $webhookUrl -Method Post -ContentType "application/json" -Body $body
   ```

### Message Format (Adaptive Cards)

Teams webhooks support **Adaptive Card** payloads (v1.0+) for rich formatting:

```json
{
  "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
  "type": "AdaptiveCard",
  "version": "1.0",
  "body": [
    {
      "type": "TextBlock",
      "text": "🔴 BLOCKED: Parser refactor waiting on Config approval",
      "weight": "bolder",
      "size": "large"
    },
    {
      "type": "FactSet",
      "facts": [
        {"name": "Status:", "value": "Blocked on review"},
        {"name": "Since:", "value": "3 days"},
        {"name": "Assignee:", "value": "Jonathan"},
        {"name": "GitHub Issue:", "value": "[#42](https://github.com/...)"}
      ]
    }
  ]
}
```

### Rate Limits

- **Max rate:** 4 requests per second per webhook URL
- **Message size:** 28 KB max
- **Implication:** Batch notifications or spread large summaries across multiple cards

### Security Best Practices (from squad-skills)

✅ **DO:**
- Store webhook URL in secure file location (not git-tracked)
- Read from file in PowerShell; never hardcode
- Rotate URL periodically (delete/recreate connector)
- Treat webhook as sensitive as a password

❌ **DON'T:**
- Commit webhook URL to source control
- Share URL in chat/email
- Use same webhook across multiple tools (separate webhook per integration point)

### Alternative: Graph API Messages

For comparison, Graph API provides alternate posting method:

```powershell
# Requires OAuth 2.0 token with ChannelMessage.Send scope
$graphUrl = "https://graph.microsoft.com/v1.0/teams/{teamId}/channels/{channelId}/messages"
$body = @{
    body = @{
        content = "Message text"
        contentType = "text"
    }
} | ConvertTo-Json

Invoke-RestMethod -Uri $graphUrl -Method Post -Headers @{
    Authorization = "Bearer $token"
    "Content-Type" = "application/json"
} -Body $body
```

**Graph API advantage:** Native rich message types (no Adaptive Card JSON required).
**Webhook advantage:** Simpler setup; no OAuth tokens to manage.

### Evidence Source
- teams-monitor & news-broadcasting SKILL.md: Webhook file location, PowerShell invocation pattern, secret management
- Microsoft Teams documentation: Webhook setup UI, rate limits, Adaptive Card schema
- Web research: Security best practices for webhook URLs

---

## Q4: What Specific Event Types Should Trigger Notifications?

### Answer

**Recommended event types** based on squad use cases:

| Event Type | Source | Trigger Condition | Frequency |
|-----------|--------|-------------------|-----------|
| **Blocked Tasks** | GitHub issues + WorkIQ filter | Issue in "blocked" state for >12h | On state change + daily summary |
| **Stale Items** | GitHub issues + sprint board | No activity for >5 days | Daily summary |
| **Significant Merges** | GitHub commits/PRs | PR merged to main | Real-time (if via webhook) or summary |
| **Unreviewed PRs** | GitHub PRs | Open PR awaiting review >24h | Daily summary |
| **Critical Incidents** | Teams messages via WorkIQ | "Sev2", "outage", "blocking" keywords | On detection |
| **Decision Announcements** | Teams messages via WorkIQ | Mentions "decision", "approved", "change" | On detection |

### Implementation Strategy

**Hybrid approach** (WorkIQ + GitHub API):

1. **Scheduled Task** (every 20 min via squad scheduler):
   - Query WorkIQ for critical keywords (sev, outage, urgent, blocked)
   - Query GitHub API for recently-blocked issues, stale PRs
   - Filter out duplicates (check existing GitHub issues created in last 24h)

2. **Daily Summary** (e.g., 9 AM):
   - Poll for all events in past 24 hours
   - Aggregate by category (blocked, stale, merged, etc.)
   - Post single summary card to Teams

3. **Real-time Alerts** (optional, Phase 2):
   - GitHub webhook → PowerShell script → Teams webhook
   - Limited to events with immediate action (blockers, critical merges)

### Example: Blocked Task Detection

```powershell
# Query WorkIQ for recent mentions of blocking issues
$workIQResponse = workiq-ask_work_iq @{
    question = "What Teams messages were sent in the last 24 hours mentioning 'blocked', 'blocking', or 'can't proceed'?"
}

# Filter for actionable items (exclude info-only messages)
$actionable = $workIQResponse | Where-Object { 
    $_ -match "need\s+(to|your)\s+(help|action)" -or 
    $_ -match "urgent\s+(help|review)" 
}

# Post to Teams if found
foreach ($item in $actionable) {
    $body = @{
        text = "🔴 **BLOCKED**: $item"
    } | ConvertTo-Json
    Invoke-RestMethod -Uri $webhookUrl -Method Post -Body $body
}
```

### Evidence Source
- teams-monitor skill: Actionable content filtering, keyword detection patterns
- news-broadcasting skill: Daily briefing, breaking news, status flash formats
- Squad decisions: Priority given to real-time blockers and stale items

---

## Q5: Which Approach Is Best for Jonathan's EMU/Microsoft-Internal Environment?

### Answer

**Recommended: Hybrid webhook + WorkIQ polling approach.**

### Why This Approach

✅ **Aligns with squad capabilities:**
- WorkIQ is already available (used by teams-monitor)
- Squad scheduler is operational (runs tasks every 20 min)
- Teams webhook pattern is proven (news-broadcasting plugin)
- No additional infrastructure needed

✅ **Suited to EMU/Microsoft constraints:**
- Webhook approach doesn't require tenant admin approval (unlike app registration)
- No external API credentials needed (unlike Power Automate premium features)
- Works within standard Teams permissions
- Leverages existing Microsoft tooling (Graph, Teams, WorkIQ)

✅ **Low operational overhead:**
- Setup: ~15 minutes (webhook creation + PowerShell script)
- Maintenance: Update queries/filters as squad priorities shift
- Monitoring: Basic logging in agent sessions

### Phased Implementation

**Phase 1 (MVP - Week 1):**
- Create Teams incoming webhook for #squad-notifications channel
- Build simple polling script (WorkIQ + GitHub API)
- Filter for: blocked issues (>12h), critical Team mentions (sev2, outage)
- Post daily summary at 9 AM
- **Owner:** Squad Agent (Researcher or Infrastructure)
- **Output:** Skill in `.squad/skills/squad-notifications/`

**Phase 2 (Enhanced - Week 2-3):**
- Add real-time alerts for critical events (not batched)
- Implement deduplication (don't post same event twice)
- Adaptive Cards with richer formatting (color-coded by urgency)
- Add unreviewed PR detection
- **Owner:** Squad Agent + Jonathan feedback loop

**Phase 3 (Future - Optional):**
- Graph API alternative (if webhook rate-limits become issue)
- Custom WorkIQ queries per agent (each agent gets filtered digest)
- Power Automate triggers for GitHub Actions (if GitHub integration expands)

### Alternative Approaches (Why Not)

| Approach | Pros | Cons | Verdict |
|----------|------|------|---------|
| **Pure Graph API** | Scalable, rich types, auth granular | Requires app registration + tenant admin approval; 15+ min setup | Too heavyweight for MVP |
| **Pure Power Automate** | Visual UI, audit trail, enterprise-grade | Premium features may require licenses; operational overhead | Save for Phase 3 if needed |
| **GitHub → Power Automate → Teams** | Integrates GitHub Actions directly | Adds abstraction layer; harder to customize for squad-specific filters | Overkill for current scope |
| **Teams Native GitHub App** | Zero-config for GitHub events | Can't filter for custom squad events; fixed format | Complements webhook but insufficient alone |

### EMU-Specific Considerations

1. **Authentication:** Webhook approach requires no EMU-specific auth (URL is self-contained).
2. **Permissions:** User running script needs Teams channel access (normal permissions).
3. **Audit:** Webhook posts appear as "Posted via Incoming Webhook"; audit trails are clean.
4. **Compliance:** Webhook URLs are temporary (can be rotated); no persistent tokens.

### Evidence Source
- Squad decisions: WorkIQ is poll-based; rate-limit to 1 query per cycle
- Elrond history: Webhook delivery pattern documented in squad-skills
- Web research: Teams webhooks vs. Graph API trade-offs for EMU environments
- teams-monitor & news-broadcasting: Proven implementation patterns

---

## Q6: What Are the Implementation Steps & Next Actions?

### Answer

### Detailed Implementation Steps

**Step 1: Create Teams Webhook (5 min)**

1. Open Teams channel: #squad-notifications (or create if not exists)
2. Click ⋮ → "Manage channel"
3. Go to "Connectors" → Search "Incoming Webhook"
4. Click "Configure" → Enter name "Squad Alerts" → Copy webhook URL
5. Save URL to `C:\Users\[username]\.squad\teams-webhook.url`
6. Test:
   ```powershell
   $url = Get-Content "$env:USERPROFILE\.squad\teams-webhook.url"
   $body = @{ text = "🧪 Test: Webhook is working" } | ConvertTo-Json
   Invoke-RestMethod -Uri $url -Method Post -ContentType "application/json" -Body $body
   ```

**Step 2: Create Squad Notifications Skill (30 min)**

Create `.squad/skills/squad-notifications/SKILL.md`:

```markdown
---
name: squad-notifications
description: "Monitor blocked/stale squad items and post daily summaries to Teams"
domain: monitoring, communication
confidence: medium
---

## Trigger
- Scheduled: Nightly at 9 AM via squad scheduler
- Manual: "Check squad status and post to Teams"

## Workflow
1. Query WorkIQ for recent messages (24h window)
   - Filter for keywords: blocked, urgent, sev2, can't proceed, help needed
2. Query GitHub API for:
   - Issues in "blocked" state (>12h)
   - PRs awaiting review (>24h)
   - Stale issues (no activity >5d)
3. Deduplicate against last 24h of Issues created/updated
4. Format as Adaptive Card with sections per category
5. POST to Teams via webhook

## Query Examples

### WorkIQ Queries
- "What Teams messages in the last 24 hours mention 'blocked' or 'urgent'?"
- "Any messages from Tamir or squad team mentioning Sev2 incidents?"

### GitHub API Queries
- `GET /repos/owner/repo/issues?state=closed&labels=blocked&sort=updated&direction=desc`
- `GET /repos/owner/repo/pulls?state=open&sort=updated&direction=desc`
```

Create `.squad/skills/squad-notifications/index.js` or PowerShell script:

```powershell
# squad-notifications.ps1
param(
    [string]$WeekHookUrl = (Get-Content "$env:USERPROFILE\.squad\teams-webhook.url" -Raw).Trim()
)

# Query WorkIQ
$workIQResponse = workiq-ask_work_iq @{
    question = "What Teams messages were sent in the last 24 hours mentioning blocked, urgent, or Sev2 issues?"
}

# Query GitHub (example - requires gh CLI)
$blockedIssues = gh issue list --state closed --label blocked --limit 10 --json title,url,updatedAt

# Format as Adaptive Card
$card = @{
    '$schema' = "http://adaptivecards.io/schemas/adaptive-card.json"
    'type' = "AdaptiveCard"
    'version' = "1.0"
    'body' = @(
        @{
            'type' = "TextBlock"
            'text' = "📊 Daily Squad Status"
            'size' = "large"
            'weight' = "bolder"
        },
        @{
            'type' = "FactSet"
            'facts' = @(
                @{ 'name' = "Blocked Issues"; 'value' = ($blockedIssues | Measure-Object).Count.ToString() },
                @{ 'name' = "Critical Messages"; 'value' = "See details below" }
            )
        }
    )
} | ConvertTo-Json -Depth 10

# POST to Teams
Invoke-RestMethod -Uri $webhookUrl -Method Post -ContentType "application/json" -Body $card
```

**Step 3: Register with Squad Scheduler (15 min)**

Add to `.squad/schedule.json`:

```json
{
  "tasks": [
    {
      "id": "squad-notifications-daily",
      "description": "Post daily squad status to Teams",
      "script": ".squad/skills/squad-notifications/squad-notifications.ps1",
      "schedule": "0 9 * * *",
      "enabled": true
    }
  ]
}
```

**Step 4: Test & Iterate (10 min)**

```powershell
# Manual test
& ".squad/skills/squad-notifications/squad-notifications.ps1"

# Check Teams channel for message
# Verify format and completeness
```

### Next Actions (Priority Order)

1. ✅ **Jonathan approves approach** (verbal/email) — Confirm webhook approach is acceptable
2. ⚠️ **Create webhook & store URL securely** — DO NOT commit to git
3. ✅ **Build polling script** — Start with simple WorkIQ + GitHub queries
4. ✅ **Test in #squad-notifications channel** — Verify formatting and timing
5. ⚠️ **Document query patterns** — Update SKILL.md with squad-specific queries
6. ✅ **Add to squad scheduler** — Set nightly cadence
7. ✅ **Gather feedback** — Iterate on event filters, formatting, frequency after 1 week
8. ⏩ **Phase 2: Real-time alerts** — Add critical-only notifications outside daily summary
9. ⏩ **Phase 3: Graph API / Power Automate** — Evaluate if webhook hits rate limits

### Success Criteria

- ✅ Daily notification posts to Teams channel every morning at 9 AM
- ✅ Notifications include blocked issues and critical Teams messages
- ✅ No duplicate notifications (deduplication working)
- ✅ Format is readable (Adaptive Cards render correctly)
- ✅ Webhook URL remains secure (not in git, only in `~/.squad/`)
- ✅ Queries complete within 30 seconds
- ✅ Team engagement: Jonathan/Tamir confirm notifications are useful within 1 week

### Evidence Source
- teams-monitor & news-broadcasting: Proven implementation patterns from squad-skills
- Elrond charter: Research-to-implementation handoff; iterative refinement approach
- Squad decisions: Operational standards for scheduling, scripting, security

---

## Summary Table: Research Questions & Answers

| Q | Topic | Answer | Status |
|---|-------|--------|--------|
| **Q1** | Integration options | Webhooks (primary), Graph API (future), Power Automate (optional) | ✅ Ready |
| **Q2** | WorkIQ for detection | Poll-based; proven query patterns in teams-monitor; 20-min cadence | ✅ Ready |
| **Q3** | Webhook setup & security | Minimize setup; store URL securely in `~\.squad\teams-webhook.url` | ✅ Ready |
| **Q4** | Event types | Blocked tasks, stale items, critical incidents, significant merges | ✅ Ready |
| **Q5** | Best for EMU | Hybrid webhook + WorkIQ polling; low overhead, no admin approval needed | ✅ Recommended |
| **Q6** | Implementation steps | 4 steps: create webhook, build skill, register scheduler, test & iterate | ✅ Detailed |

---

## Appendix: Resource References

### Tamir's Squad-Skills Plugins (Primary Evidence)

1. **teams-monitor** (SKILL.md)
   - WorkIQ query patterns (lines 28-52)
   - Actionable content filtering (lines 54-70)
   - Rate-limit guidance (line 36: "Don't run queries more than once per session")
   - Webhook URL location: `C:\Users\[username]\.squad\teams-webhook.url`

2. **news-broadcasting** (SKILL.md)
   - Webhook POST via PowerShell (lines 21-27)
   - Adaptive Card formatting guidelines
   - Message style and humor guidelines

3. **teams-ui-automation** (SKILL.md)
   - Layer-based automation approach (Playwright → Keyboard → UIA)
   - Applicable for future UI-based integrations (e.g., connector setup)

### Microsoft Documentation

- [Send Incoming Webhook Messages to Teams](https://learn.microsoft.com/en-us/microsoftteams/platform/webhooks-and-connectors/how-to/connectors-using)
- [Microsoft Graph API: Post Channel Messages](https://learn.microsoft.com/en-us/graph/api/chatmessage-post?view=graph-rest-1.0)
- [Adaptive Cards for Teams](https://adaptivecards.io/)

### Squad Infrastructure

- `.squad/decisions.md` — WorkIQ rate-limit guidance, plugin findings
- `.squad/agents/elrond/charter.md` — Researcher role and methodology
- `.squad/agents/elrond/history.md` — Prior research learnings

---

## Conclusion

**Real-time squad notifications in Teams are achievable** using a hybrid approach of WorkIQ polling + Incoming Webhooks. This approach is:

- ✅ **Production-ready:** Leverages existing squad infrastructure
- ✅ **Secure:** No new authentication mechanisms; URL-based webhooks with secure storage
- ✅ **Scalable:** Poll cadence and filtering are easily tunable
- ✅ **EMU-compatible:** No tenant admin approval required; works within standard Teams permissions
- ✅ **Iterative:** Phase 1 (MVP) can launch within days; Phase 2+ refinements follow based on usage

The research questions have been answered with concrete implementation guidance. Jonathan and team can proceed with Phase 1 implementation using the steps outlined in Q6.

---

**Document Version:** 1.0  
**Research Completed:** January 16, 2025  
**Next Review:** After Phase 1 implementation (1 week)  
**Owner:** Elrond (Researcher)  
**Stakeholders:** Jonathan (Product), Tamir (Architecture Reference)
