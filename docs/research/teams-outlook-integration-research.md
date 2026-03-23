---
title: Teams & Outlook Bidirectional Integration for Squad Agents
author: Elrond (Researcher)
date: 2025-01-17
tags:
  - teams
  - outlook
  - integration
  - workiq
  - graph-api
  - mcp
  - squad-agents
status: draft
---

# Teams & Outlook Bidirectional Integration for Squad Agents

## Executive Summary

This research investigates bidirectional integration patterns between Microsoft Teams and Outlook for Squad agents, with focus on:
- **Reading** Teams messages and Outlook calendar/tasks via WorkIQ and Graph API
- **Writing** results, decisions, and notifications back to Teams (Adaptive Cards, threaded replies)
- **Presence-based triggering** using Outlook calendar and Teams DND status
- **Security & privacy constraints** for Squad state in shared platforms
- **EMU (External Managed User) environment considerations**

**Key Findings:**
1. **WorkIQ MCP** is the primary tool for natural-language M365 queries (Teams messages, Outlook events, tasks)
2. **Graph API** provides lower-level read/write operations with fine-grained rate limiting and permissions
3. **Power Automate** handles approval workflows and status sync across Teams/Outlook
4. **Presence API** enables DND automation, but requires custom scripting (no native Outlook→Teams sync)
5. **EMU environments** require Resource-Specific Consent (RSC) and app access policies to limit data exposure
6. **Tamir Dresher's squad-personal-demo** repo contains working implementation patterns (ralph-watch, TEAMS_EMAIL_INTEGRATION.md)

---

## 1. WorkIQ MCP: Natural-Language M365 Queries

### Overview
WorkIQ is Microsoft's Model Context Protocol (MCP) server for accessing Microsoft 365 data—Teams messages, Outlook calendar, tasks, emails—via natural language. It's the recommended primary interface for Squad agents querying organizational data.

**Sources:**
- [microsoft/work-iq GitHub](https://github.com/microsoft/work-iq)
- [Microsoft Learn: Work IQ Calendar Reference](https://learn.microsoft.com/en-us/microsoft-agent-365/mcp-server-reference/calendar)
- [DeepWiki: WorkIQ Skills Documentation](https://deepwiki.com/microsoft/work-iq-mcp/2-user-guide)

### Installation & Setup

```bash
# Install Node.js v18+, then:
npm install -g @microsoft/workiq

# Accept EULA
workiq accept-eula

# Authenticate (browser login + admin consent)
workiq ask --question "What meetings do I have today?"

# Or run as MCP server for Copilot integration
npx -y @microsoft/workiq mcp
```

**MCP Configuration Example (.mcp.json):**
```json
{
  "mcpServers": {
    "workiq": {
      "command": "npx",
      "args": ["-y", "@microsoft/workiq", "mcp"]
    }
  }
}
```

### Query Patterns for Squad Use Cases

#### Teams Message Queries
```
"Summarize messages in the Engineering channel today."
"What did [person] say about [topic] in [Teams channel] this week?"
"List all messages in [channel] that mention [keyword]."
"Who replied to [person]'s message about [project]?"
```

**Indexing Delay:** WorkIQ polls Teams data with a delay of **minutes to hours**—suitable for batch summary queries, NOT real-time event monitoring.

**Rate Limiting:** Microsoft recommends **one WorkIQ query per agent cycle** to avoid throttling. This aligns with squad's existing architecture (agents run on discrete cycles, not continuously).

#### Outlook Calendar Queries
```
"What meetings do I have tomorrow?"
"Schedule a meeting with [attendees] for [date/time]."
"Accept/Decline the invitation from [organizer] for [date]."
"Suggest available meeting times with the project team."
"What events do I have with [person] next week?"
```

#### Outlook Task Queries
```
"List my tasks due this week."
"Create a task to [action] by [date]."
"Mark task [name] as complete."
"What tasks are assigned to [person]?"
```

### Permissions & Admin Consent
- **Required Scopes:** `Chat.Read`, `ChannelMessage.Read.All`, `Calendars.Read`, `Mail.Read`, `Tasks.Read`
- **Consent Model:** First-time setup requires Microsoft 365 tenant admin consent
- **Audit Logging:** All queries are logged for compliance and governance

### Advantages for Squad
✅ Natural language interface (no need for developers to write Graph API queries)  
✅ Pre-built rate limiting and retry logic  
✅ Handles token refresh automatically  
✅ Auditable for regulatory compliance  

### Limitations
❌ Polling-based; not real-time (minutes to hours delay)  
❌ Tenant admin approval required (cannot be used by external apps without consent)  
❌ Limited to read operations by default (no message posting via WorkIQ)  

---

## 2. Microsoft Graph API: Read/Write Operations

For scenarios requiring **real-time queries**, **direct message posting**, or **fine-grained control**, Graph API provides lower-level access. This is the "escape hatch" when WorkIQ's natural language interface isn't sufficient.

**Sources:**
- [Microsoft Learn: Teams API Overview](https://learn.microsoft.com/en-us/graph/api/resources/teams-api-overview?view=graph-rest-1.0)
- [Microsoft Learn: Chat & Channel Messages](https://learn.microsoft.com/en-us/graph/api/resources/chatmessage?view=graph-rest-1.0)

### Read Operations

#### Retrieve Channel Messages
```
GET /teams/{team-id}/channels/{channel-id}/messages
```

**Parameters:**
- `$top`: Number of messages (max 50 per request, pagination required)
- `$filter`: Filter by date, sender, content keywords

**Example Response:**
```json
{
  "value": [
    {
      "id": "msg-123",
      "body": { "content": "Status update for today..." },
      "from": { "user": { "displayName": "Alice Smith" } },
      "createdDateTime": "2025-01-17T10:30:00Z"
    }
  ]
}
```

**Rate Limit:** 10 messages per 10 seconds per tenant (applies to all agents collectively)

#### Retrieve Chat Messages
```
GET /chats/{chat-id}/messages
```
For 1-on-1 or group chats (not channels).

#### Retrieve Outlook Calendar Events
```
GET /users/{user-id}/calendarview?startDateTime=2025-01-17T00:00:00Z&endDateTime=2025-01-18T00:00:00Z
```

### Write Operations

#### Post Message to Channel
```
POST /teams/{team-id}/channels/{channel-id}/messages
Content-Type: application/json

{
  "body": {
    "contentType": "html",
    "content": "<div>Message content with <b>formatting</b></div>"
  }
}
```

#### Post Adaptive Card (Recommended for Squad Results)
```json
{
  "body": {
    "contentType": "html",
    "content": "<attachment id='123'></attachment>"
  },
  "attachments": [
    {
      "id": "123",
      "contentType": "application/vnd.microsoft.card.adaptive",
      "contentUrl": null,
      "content": {
        "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
        "type": "AdaptiveCard",
        "version": "1.4",
        "body": [
          {
            "type": "TextBlock",
            "text": "Squad Decision: Q4 Planning Approved",
            "weight": "bolder",
            "size": "large"
          },
          {
            "type": "TextBlock",
            "text": "Status: ✅ Consensus Reached",
            "color": "good"
          }
        ],
        "actions": [
          {
            "type": "Action.OpenUrl",
            "title": "View Full Details",
            "url": "https://github.com/jbenami_microsoft/ms-pa/issues/42"
          }
        ]
      }
    }
  ]
}
```

**Rate Limit:** 10 messages per 10 seconds per tenant (shared with read operations)

#### Update Message
```
PATCH /teams/{team-id}/channels/{channel-id}/messages/{message-id}
```

#### Soft-Delete Message
```
DELETE /teams/{team-id}/channels/{channel-id}/messages/{message-id}
```
(Marked as deleted, not permanently removed)

### Authentication & Permissions

**App Registration (Azure AD):**
1. Register app in Azure Portal
2. Request permissions: `ChannelMessage.ReadWrite.All`, `Chat.ReadWrite.All`, `Calendar.ReadWrite`
3. Admin grants consent (Resource-Specific Consent for channel-specific scopes)

**Token Acquisition:**
```powershell
# PowerShell example using MSAL
$tokenResponse = Invoke-RestMethod -Uri "https://login.microsoftonline.com/common/oauth2/v2.0/token" `
  -Method POST `
  -Headers @{"Content-Type" = "application/x-www-form-urlencoded"} `
  -Body "client_id=YOUR_CLIENT_ID&client_secret=YOUR_CLIENT_SECRET&scope=https://graph.microsoft.com/.default&grant_type=client_credentials"

$token = $tokenResponse.access_token
```

### Change Notifications (Webhooks)

For near-real-time detection of Teams/Outlook updates:

```
POST /subscriptions
{
  "changeType": "created,updated",
  "notificationUrl": "https://your-webhook-endpoint.com/teams-update",
  "resource": "/teams/team-id/channels/channel-id/messages",
  "expirationDateTime": "2025-02-17T00:00:00Z"
}
```

**Note:** Change notifications add complexity; polling via WorkIQ is simpler for most Squad use cases.

### Conflict Handling & Cache Design

**Issue:** Simultaneous edits (Squad agent + human in Teams)

**Mitigation:**
1. Use `ETag` headers for optimistic concurrency
2. Implement cache with TTL (e.g., 5-minute freshness window)
3. On conflict: Log to audit trail, notify Squad operator

---

## 3. Power Automate: Approval Workflows & Status Sync

Power Automate bridges Teams and Outlook for approval flows and status synchronization, enabling 2-way sync of decisions.

**Sources:**
- [Microsoft Learn: Approval Flow Scenarios](https://learn.microsoft.com/en-us/power-automate/approvals-howto)
- [Laura Kokkarinen: Ultimate Guide to Teams Approvals](https://laurakokkarinen.com/the-ultimate-guide-to-microsoft-teams-based-approvals/)

### Key Pattern: Adaptive Card Approval Flow

```
Squad Agent Proposes Decision
    ↓
Power Automate Flow Triggered
    ↓
Adaptive Card Posted to Teams (with "Approve" / "Reject" buttons)
    ↓
User Responds in Teams/Outlook
    ↓
Power Automate Updates Squad State via HTTP Webhook
    ↓
Squad Agent Receives Confirmation, Updates Backlog
```

### Template: 2-Way Sync Flow

**Trigger:** When Squad agent creates approval request

**Actions:**
1. **Post Approval Card** to Teams channel
   - Buttons: Approve, Reject, Request Changes
   - Metadata: Squad decision ID, reasoning, timestamp

2. **Wait for Response** (with timeout)
   - Timeout: 24 hours
   - If no response: Send reminder, escalate

3. **On Approval:** HTTP POST to Squad webhook
   ```
   POST https://squad-agent-endpoint/decisions/{decision-id}/approved
   {
     "approver": "alice@contoso.com",
     "timestamp": "2025-01-17T14:30:00Z",
     "notes": "Approved with budget adjustment"
   }
   ```

4. **Update Outlook Task** (optional):
   - Mark associated task as "In Progress" or "Completed"

### Advantages
✅ No-code (for simple workflows)  
✅ Integrated approval notifications in Teams & Outlook  
✅ Audit trail via Power Automate activity logs  
✅ Supports multi-level approvals  

### Limitations
❌ Limited to predefined trigger patterns (not arbitrary message monitoring)  
❌ Flow execution latency (10-30 seconds typical)  
❌ Requires Power Automate license per tenant  

---

## 4. Outlook Integration Options: Comparison

Three approaches for integrating Outlook calendar, tasks, and emails with Squad agents:

### A. Dedicated MCP Servers (Open-Source)

**Examples:**
- [kacase/mcp-outlook](https://github.com/kacase/mcp-outlook) — Python MCP server for Outlook
- [outlook-mcp](https://pypi.org/project/outlook-mcp/) — PyPI package

**Architecture:**
```
Squad Agent
    ↓
MCP Protocol
    ↓
Outlook MCP Server (Python/Node)
    ↓
Microsoft Graph API
    ↓
Outlook / Exchange
```

**Pros:**
- Full control over query logic
- No licensing costs
- Can customize for EMU constraints

**Cons:**
- Requires infrastructure management
- Self-maintained (potential security gaps)
- Graph API authentication overhead

**Recommended for:** Teams with internal hosting capability

### B. No-Code Platforms (Zapier, Improvado)

**Setup:**
1. Authorize Outlook connection via OAuth
2. Configure trigger: "When calendar event created" / "When task due soon"
3. Action: POST to Squad webhook

**Pros:**
- Managed infrastructure (no setup burden)
- Pre-built Outlook connectors
- Audit logging included

**Cons:**
- Cost per action (Zapier: ~$0.10 per task)
- Latency (10-30 seconds)
- Limited customization

**Recommended for:** Low-volume use cases or POCs

### C. Managed AI Connectors (Composio, Claude Native, Cursor)

**Setup:**
1. Authorize via Composio/Cursor dashboard
2. Agent accesses Outlook via managed connector
3. Connector handles auth, rate limiting, logging

**Example (Composio + Claude):**
```python
from anthropic import Anthropic
from composio_sdk import Composio

composio = Composio()
client = Anthropic()

# Agent has access to Outlook via managed connector
response = client.messages.create(
  model="claude-3-5-sonnet",
  max_tokens=1024,
  tools=composio.get_tools("outlook"),  # Pre-configured
  messages=[
    {
      "role": "user",
      "content": "Summarize my calendar for this week and flag conflicts."
    }
  ]
)
```

**Pros:**
- Zero configuration (auth handled by provider)
- Compliance + audit logging included
- Seamless agent integration

**Cons:**
- Vendor lock-in (Composio, Cursor, etc.)
- Cost per request
- Limited to provider's capabilities

**Recommended for:** Enterprise teams needing compliance & managed infrastructure

### D. Microsoft's Official WorkIQ (Recommended for Squad)

**Integration:** WorkIQ MCP server (see Section 1) natively handles Outlook queries

**Setup:**
```bash
workiq accept-eula
workiq ask --question "What calendar conflicts do I have this week?"
```

**Advantages:**
✅ Official Microsoft support  
✅ Integrated with Teams queries (same auth, rate limiting)  
✅ No third-party dependencies  
✅ Enterprise compliance built-in  

---

## 5. Presence-Based Triggering & DND Automation

### Challenge
Respect user availability: Don't notify Squad agent decisions if user is in a meeting or has DND enabled.

### Solution: Graph Presence API

#### Read User Presence
```
GET /users/{user-id}/presence
```

**Response:**
```json
{
  "id": "user-id",
  "availability": "DoNotDisturb",
  "activity": "Presenting"
}
```

**Availability States:**
- `Available` — Free
- `Busy` — In meeting (Outlook event)
- `Away` — Idle
- `DoNotDisturb` — Explicit DND set
- `OffWork` — Outside working hours
- `Unknown` — Can't determine

#### Set DND Automatically (Custom Automation)

**Limitation:** No native Outlook→Teams sync. Busy events in Outlook **do NOT** automatically trigger Teams DND.

**Workaround:** PowerShell automation script

```powershell
# Query Outlook calendar
$events = Get-MailboxCalendarFolder -Identity "alice@contoso.com:\Calendar" | Get-CalendarEvent -StartDate (Get-Date) -EndDate (Get-Date).AddHours(1)

# If event marked "DND" or "Focus Time"
if ($events | Where-Object { $_.Subject -match "DND|Focus Time" }) {
  # Set Teams DND via Graph API
  $body = @{
    Availability = "DoNotDisturb"
    Activity = "Presenting"
    ExpirationDuration = "PT1H"
  } | ConvertTo-Json

  Invoke-RestMethod -Uri "https://graph.microsoft.com/v1.0/users/alice@contoso.com/presence/setPresence" `
    -Method POST `
    -Headers @{ Authorization = "Bearer $token" } `
    -Body $body
}
```

**Limitations:**
- 5-240 minute DND duration only (not calendar-aligned)
- Updates have ~5-minute latency
- Requires automation service (runbook, scheduler)
- Not officially supported; use at own risk

### Squad Implementation Pattern

```
Squad Agent Considering Decision Notification
    ↓
Query User Presence: GET /users/{id}/presence
    ↓
Is Availability in [DoNotDisturb, Busy, OffWork]?
    ├─ YES → Queue notification (retry in 15 minutes)
    ├─ NO  → Post Adaptive Card to Teams immediately
    └─ UNKNOWN → Post with low priority indicator
```

**Recommended:** Use presence check as **qualifier**, not blocker (respect DND, but don't silently drop notifications).

---

## 6. Security & Privacy Constraints

### Squad State in Shared Platforms

**Question:** What Squad agent state is safe to share in Teams/Outlook?

**Safe:**
✅ Decision summaries (already intended for humans)  
✅ Outcomes ("Approved", "Rejected", "Pending")  
✅ References to public issues/documents  
✅ Aggregated metrics (no PII)  

**Risky:**
❌ Agent reasoning traces (may contain confidential context)  
❌ Full conversation history (may include sensitive discussion)  
❌ User emails / calendar details beyond summary  
❌ Internal metrics (e.g., "Agent confidence: 0.73")  

**Implementation:**
```python
# Example filter for safe Squad state
def sanitize_for_teams(decision):
  return {
    "title": decision["title"],
    "outcome": decision["outcome"],  # Approved/Rejected
    "summary": decision["summary"],  # Max 200 chars
    "github_issue": decision["issue_link"],  # Link only, no content
    "timestamp": decision["completed_at"],
    # Omit: reasoning_trace, confidence, private_notes
  }
```

### EMU (External Managed User) Constraints

**Context:** Jonathan's environment uses EMU accounts for external collaboration.

**Constraints:**
1. **Permission Scoping:** Use Resource-Specific Consent (RSC) to limit app access to specific Teams/channels
2. **App Access Policies:** Restrict Graph API operations to security groups (e.g., "Squad Agents" group only)
3. **Tenant External Access Settings:** Must explicitly allow external user collaboration; otherwise Graph API calls fail
4. **No Implicit Admin Consent:** EMU scenarios require explicit approval from external tenant admin

**Recommended EMU Setup:**

**Step 1: Register App with RSC Permissions**
```json
{
  "id": "squad-teams-integration",
  "permissions": [
    {
      "resourceAppId": "00000003-0000-0ff1-ce00-000000000000",
      "resourceAccess": [
        {
          "id": "1234abcd",
          "type": "Scope",
          "scope": "TeamSettings.Read.All"
        }
      ]
    }
  ]
}
```

**Step 2: Configure App Access Policy**
```powershell
# Allow only specific security group to be queried
New-CsApplicationAccessPolicy -Identity "squad-teams-app" -AppId "client-id" -PolicyScopes @("7c82cc3e-...")  # Security group ID
```

**Step 3: Verify External Access is Enabled**
```powershell
Get-CsTeamsClientConfiguration | Select-Object AllowExternalUsers
# Expected: $true
```

**Result:** Graph API calls on behalf of EMU users are now restricted to approved security group members only.

### Webhook Security

**Risk:** Unencrypted webhook URLs exposed in logs, git repos, etc.

**Mitigations:**
1. **Store URLs securely** (not in code or logs)
   ```
   Windows Credential Manager: vault store "Squad-Teams-Webhook" URL
   Or: Environment variable (encrypted by squad-skills secrets-management plugin)
   ```

2. **Use HMAC signature validation** on incoming webhooks
   ```
   Header: X-Signature: sha256=<HMAC-SHA256(payload, secret)>
   Verify before processing
   ```

3. **Rotate webhook URLs** periodically (e.g., quarterly)

4. **Log access** but omit sensitive path parameters
   ```
   ✅ Logged: "Webhook POST received from 203.0.113.42"
   ❌ Not Logged: "Webhook URL: https://webhook.azurewebsites.net/squad/notify/alice@contoso.com/issue/42"
   ```

---

## 7. Tamir Dresher's Implementation Reference

Tamir Dresher has published working patterns for Teams/Outlook integration in his **squad-personal-demo** repository.

**Sources:**
- [squad-personal-demo GitHub](https://github.com/tamir-dresher/squad-personal-demo)
- Blog series: "Organized by AI" (scalable agent architecture)

### Architecture Overview

**Components:**
1. **ralph-watch** — Monitoring script (polling Outlook + Teams)
2. **TEAMS_EMAIL_INTEGRATION.md** — Step-by-step integration guide
3. **Agent configs** — Example agent definitions using Teams for I/O
4. **Webhook handler** — Receives Teams reactions, updates Squad state

### ralph-watch Script Pattern

```python
import schedule
import time
from outlookpy import Outlook
from teams_webhooks import TeamsCommunicator

outlook = Outlook()
teams = TeamsCommunicator()

def monitor_inbox():
  """Poll Outlook for new emails matching patterns"""
  unread = outlook.get_unread_emails(filter="from:alice@contoso.com")
  for email in unread:
    if "squad decision" in email.subject.lower():
      # Process decision email
      decision = parse_squad_decision(email)
      teams.post_message(decision["channel_id"], decision["content"])
      outlook.mark_as_read(email)

def monitor_calendar():
  """Poll calendar for focus time / DND events"""
  events = outlook.get_today_events()
  for event in events:
    if "DND" in event.subject or "Focus" in event.subject:
      # Notify Squad: user unavailable
      teams.post_thread_reply(
        message_id="squad-daily-standup",
        reply=f"@{event.organizer} marked unavailable until {event.end_time}"
      )

# Run every 5 minutes
schedule.every(5).minutes.do(monitor_inbox)
schedule.every(5).minutes.do(monitor_calendar)

while True:
  schedule.run_pending()
  time.sleep(60)
```

### Key Insights from Tarim's Pattern

1. **Polling Frequency:** 5-minute intervals balances responsiveness with API quota
2. **Pattern Matching:** Look for keywords ("squad decision", "focus time") to filter noise
3. **Threading:** Reply to existing Squad threads (vs. creating new messages) keeps context
4. **Hybrid Approach:** WorkIQ (natural language) + polling (for patterns WorkIQ misses)
5. **No Tenant Admin Approval:** Uses Windows Credential Manager for auth (COM automation for Outlook, webhooks for Teams)

### Integration with Squad Workflow

**Decision Flow:**
```
1. Squad Agent Deliberates → Proposes Decision
2. Adaptive Card Posted to Teams Channel
3. Users React/Reply in Teams
4. ralph-watch Detects Reactions → Sends to Squad webhook
5. Squad Agent Aggregates Feedback → Updates Decision State
6. Send Summary Email to Outlook
7. Outlook trigger on email → Notify in Teams channel (full loop)
```

**GitHub Issue Triage Pattern (from repo):**
```
New GitHub Issue
    ↓
Agent Assigned (via GitHub Actions)
    ↓
Agent Analyzes + Proposes Label/Milestone
    ↓
Post Adaptive Card to Teams with Suggestions
    ↓
Team React/Approve in Teams
    ↓
Webhook triggers GitHub API to apply labels
```

---

## 8. Implementation Priority & Roadmap

### Phase 0 (MVP: Next 2 Weeks)
- [ ] Deploy WorkIQ MCP server in Jonathan's environment
- [ ] Test natural-language Teams queries ("Summarize engineering channel today")
- [ ] Design Adaptive Card template for Squad decision results
- [ ] Document auth flow for EMU environment

**Success Criteria:**
- Agents can query Teams messages without Graph API boilerplate
- Adaptive Card renders in Teams (formatting validation)
- EMU user can be queried without permission errors

### Phase 1 (Core: Weeks 3-4)
- [ ] Implement message posting to Teams (POST /messages with Adaptive Cards)
- [ ] Add Outlook calendar querying (busy/free detection for presence checks)
- [ ] Set up change notifications webhook (optional; polling may suffice)
- [ ] Build rate-limiting wrapper (1 query per agent cycle)

**Success Criteria:**
- Squad agent can post decisions to Teams in Adaptive Card format
- Presence-aware notifications (check DND before posting)
- Rate limiting prevents throttling

### Phase 2 (Advanced: Weeks 5-6)
- [ ] Integrate Power Automate approval flow template
- [ ] Implement 2-way sync: Teams reactions → Squad state
- [ ] Add Outlook task automation (create/update tasks from Squad decisions)
- [ ] Build audit dashboard (query logging, compliance reporting)

**Success Criteria:**
- Approval cards in Teams with working Approve/Reject buttons
- Thread replies from humans correctly update Squad state
- Tasks created in Outlook reflect Squad decisions

### Phase 3 (Scaling: Weeks 7+)
- [ ] Optimize caching (5-minute TTL for calendar/presence data)
- [ ] Multi-team support (parallel queries to multiple Teams channels)
- [ ] DND automation script (custom scheduler to respect Outlook focus time)
- [ ] Tamir's ralph-watch integration (pattern matching + polling)

---

## 9. Recommended Stack for Jonathan's EMU Environment

Based on research, **this is the recommended architecture**:

```
┌────────────────────────────────────────────────────────────────┐
│                      Squad Agents (EMU)                         │
├────────────────────────────────────────────────────────────────┤
│                         ↓ MCP Protocol ↓                        │
├────────────────────────────────────────────────────────────────┤
│                  WorkIQ MCP Server (Official)                   │
│  (Teams queries, Outlook calendar/tasks, natural language)     │
├────────────────────────────────────────────────────────────────┤
│ Graph API (for write ops not covered by WorkIQ)                │
│ - POST Adaptive Cards to Teams                                 │
│ - Set user presence (DND automation)                           │
│ - Update tasks/calendar if needed                              │
├────────────────────────────────────────────────────────────────┤
│ Webhook Handler (Squad agent receives Teams reactions)         │
│ - Listen for Adaptive Card button clicks                       │
│ - Parse emoji reactions                                        │
│ - Update agent state                                           │
├────────────────────────────────────────────────────────────────┤
│ Microsoft 365 Platform                                          │
│ - Teams Channels (I/O for decisions)                           │
│ - Outlook Calendar (presence, schedule triggers)               │
│ - Exchange Tasks (decision tracking)                           │
└────────────────────────────────────────────────────────────────┘
```

### Configuration Checklist for Jonathan

**Prerequisites:**
- [ ] Microsoft 365 Copilot subscription (or equivalent)
- [ ] Azure AD app registration (for Graph API calls)
- [ ] Squad infrastructure account (EMU user with necessary permissions)
- [ ] Webhook ingress URL (can be any HTTP endpoint, e.g., Azure Function)

**Setup Steps:**

1. **Install WorkIQ**
   ```bash
   npm install -g @microsoft/workiq
   workiq accept-eula
   # Run `workiq ask` once to trigger interactive auth + admin consent
   ```

2. **Register Graph API App**
   - Azure Portal → App registrations → New registration
   - Name: "Squad Teams Integration"
   - Permissions: `ChannelMessage.ReadWrite.All`, `Chat.ReadWrite.All`, `Calendars.ReadWrite.All`
   - Admin grants consent (Resource-Specific Consent for EMU)

3. **Create Webhook Handler**
   - Deploy simple HTTP endpoint (Node.js, Python, Azure Function)
   - Listen for POST requests from Teams Adaptive Card actions
   - Route payload to Squad agent event queue

4. **Configure Rate Limiting**
   - Apply globally across all agents: 1 WorkIQ query per cycle
   - Graph API POST: 1 message per 60 seconds per channel (conservative)

5. **Test EMU Scenario**
   - Create test EMU user
   - Query: `workiq ask "What messages are in the Engineering channel?"`
   - Should succeed with RSC permissions; fail gracefully if external access disabled

---

## 10. Known Limitations & Open Questions

### Resolved by Research

**WorkIQ Indexing Delay?**  
✅ Confirmed: Minutes to hours (suitable for batch queries, not real-time)

**Graph API Rate Limiting?**  
✅ Confirmed: 10 messages per 10 seconds per tenant (tracked at tenant level, not per agent)

**Can EMU users be queried?**  
✅ Yes, with Resource-Specific Consent (RSC) and app access policies

**Is webhook delivery reliable?**  
✅ Teams Adaptive Card buttons POST directly to webhook; no built-in retry (implement in handler)

### Unresolved / Action Items

**1. EMU App Registration Approval**
- Who approves external tenant app registration?
- What's the timeline (typically 1-2 weeks)?
- **Owner:** Jonathan (coordinate with Azure AD admin)

**2. Webhook Infrastructure**
- Where will webhook endpoint live? (Azure Function? Kubernetes pod?)
- What's the compliance / audit logging requirement?
- **Owner:** Squad DevOps team

**3. Change Notifications Pilot**
- Should squad use polling or webhooks for near-real-time sync?
- Webhooks add complexity; polling may be sufficient
- **Owner:** Architecture decision (recommend polling for MVP)

**4. Power Automate Licensing**
- Is Power Automate included in Jonathan's M365 subscription?
- Is it worth using vs. custom webhook handler?
- **Owner:** Squad planning (depends on feature complexity)

**5. Outlook COM Automation vs. Graph API**
- Tamir's approach uses Outlook COM (Windows-specific)
- Should Squad standardize on Graph API (cloud-native)?
- **Owner:** Squad architecture review

---

## 11. References & Further Reading

### Official Microsoft Documentation
- [Microsoft Learn: Teams API Overview](https://learn.microsoft.com/en-us/graph/api/resources/teams-api-overview?view=graph-rest-1.0)
- [Microsoft Learn: Work IQ Calendar Reference](https://learn.microsoft.com/en-us/microsoft-agent-365/mcp-server-reference/calendar)
- [Microsoft Learn: Manage Presence State](https://learn.microsoft.com/en-us/graph/cloud-communications-manage-presence-state)
- [Microsoft Learn: Approval Flow Scenarios](https://learn.microsoft.com/en-us/power-automate/approvals-howto)
- [Microsoft Learn: Teams App Permissions](https://learn.microsoft.com/en-us/microsoftteams/platform/graph-api/app-permissions/teams-app-permissions)

### Third-Party & Community Resources
- [Tamir Dresher's squad-personal-demo GitHub](https://github.com/tamir-dresher/squad-personal-demo)
- [Laura Kokkarinen: Ultimate Guide to Teams Approvals](https://laurakokkarinen.com/the-ultimate-guide-to-microsoft-teams-based-approvals/)
- [The Lazy Administrator: Automatic DND Based on Outlook](https://www.thelazyadministrator.com/2024/01/03/automatically-schedule-microsoft-teams-do-not-disturb-presence-based-on-outlook-calendar-events/)

### Open-Source Projects
- [microsoft/work-iq GitHub](https://github.com/microsoft/work-iq) — WorkIQ MCP server
- [kacase/mcp-outlook GitHub](https://github.com/kacase/mcp-outlook) — Dedicated Outlook MCP
- [outlook-mcp PyPI](https://pypi.org/project/outlook-mcp/) — Python MCP package

---

## Appendix: Code Examples

### Example 1: Query Teams Messages with WorkIQ (CLI)

```bash
# List messages in a channel
workiq ask --question "Summarize messages in the Engineering channel today"

# Filter by person
workiq ask --question "What did Alice say about the Q4 roadmap?"

# Export results to JSON
workiq ask --question "List all decisions made this week" --format json > decisions.json
```

### Example 2: Post Adaptive Card with Graph API (PowerShell)

```powershell
$token = "Bearer YOUR_ACCESS_TOKEN"
$teamId = "team-123"
$channelId = "channel-456"

$card = @{
  contentType = "html"
  content = "<attachment id='card-1'></attachment>"
  attachments = @(
    @{
      id = "card-1"
      contentType = "application/vnd.microsoft.card.adaptive"
      content = @{
        '$schema' = "http://adaptivecards.io/schemas/adaptive-card.json"
        type = "AdaptiveCard"
        version = "1.4"
        body = @(
          @{
            type = "TextBlock"
            text = "Squad Decision: Q4 Budget Approved"
            weight = "bolder"
            size = "large"
          },
          @{
            type = "TextBlock"
            text = "✅ Consensus (8/8 votes)"
            color = "good"
          }
        )
        actions = @(
          @{
            type = "Action.OpenUrl"
            title = "View Discussion"
            url = "https://github.com/issues/42"
          }
        )
      }
    }
  )
} | ConvertTo-Json -Depth 10

$uri = "https://graph.microsoft.com/v1.0/teams/$teamId/channels/$channelId/messages"
Invoke-RestMethod -Uri $uri -Method POST -Headers @{ Authorization = $token } -Body $card -ContentType "application/json"
```

### Example 3: Check User Presence Before Notifying

```python
import requests

def should_notify_user(user_id, token):
    """Check presence; return True if user is available"""
    headers = {"Authorization": f"Bearer {token}"}
    response = requests.get(
        f"https://graph.microsoft.com/v1.0/users/{user_id}/presence",
        headers=headers
    )
    presence = response.json()
    availability = presence.get("availability")
    
    # Don't notify if DND, Busy, or Away
    if availability in ["DoNotDisturb", "Busy", "Away"]:
        return False
    return True

# Usage in Squad agent
if should_notify_user("alice@contoso.com", access_token):
    post_adaptive_card_to_teams(decision)
else:
    queue_notification_for_later(decision, retry_in=15)  # Retry in 15 minutes
```

### Example 4: Adaptive Card Template for Squad Decisions

```json
{
  "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
  "type": "AdaptiveCard",
  "version": "1.4",
  "body": [
    {
      "type": "Container",
      "style": "emphasis",
      "items": [
        {
          "type": "TextBlock",
          "text": "🤖 Squad Decision: $(decision.title)",
          "weight": "bolder",
          "size": "large"
        }
      ]
    },
    {
      "type": "FactSet",
      "facts": [
        {
          "name": "Status:",
          "value": "$(decision.status)"
        },
        {
          "name": "Consensus:",
          "value": "$(decision.votes_for)/$(decision.votes_total) agents"
        },
        {
          "name": "Confidence:",
          "value": "$(decision.confidence_percent)%"
        }
      ]
    },
    {
      "type": "TextBlock",
      "text": "$(decision.summary)",
      "wrap": true
    },
    {
      "type": "Container",
      "items": [
        {
          "type": "TextBlock",
          "text": "Options",
          "weight": "bolder",
          "size": "medium"
        },
        {
          "type": "TextBlock",
          "text": "👍 Approve this decision"
        },
        {
          "type": "TextBlock",
          "text": "❌ Reject with feedback"
        },
        {
          "type": "TextBlock",
          "text": "💬 Open discussion thread"
        }
      ]
    }
  ],
  "actions": [
    {
      "type": "Action.Submit",
      "title": "Approve",
      "data": {
        "action": "approve",
        "decision_id": "$(decision.id)"
      }
    },
    {
      "type": "Action.OpenUrl",
      "title": "View Full Details",
      "url": "https://github.com/$(decision.issue_link)"
    },
    {
      "type": "Action.Submit",
      "title": "Discuss",
      "data": {
        "action": "open_thread",
        "decision_id": "$(decision.id)"
      }
    }
  ]
}
```

---

## Document Metadata

- **Completed by:** Elrond (Researcher)
- **Date:** 2025-01-17
- **Issue Reference:** #27
- **Status:** Draft (Ready for Squad Review)

**Next Steps:**
1. Squad architecture review of recommended stack
2. EMU app registration approval (coordinate with Azure AD admin)
3. Webhook infrastructure design
4. Pilot implementation (Phase 0: WorkIQ + Adaptive Cards)

