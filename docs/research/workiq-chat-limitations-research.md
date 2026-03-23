---
title: "WorkIQ Chat Message Retrieval — Capabilities, Limitations & Workarounds"
author: Elrond (Researcher)
date: 2026-03-23
status: complete
requested_by: Jonathan (HIGH PRIORITY)
triggered_by: "Aragorn failed to retrieve a specific Teams chat message via WorkIQ"
tags: [workiq, teams, chat, graph-api, mcp, limitations, workarounds]
---

# WorkIQ Chat Message Retrieval — Capabilities, Limitations & Workarounds

## Executive Summary

**WorkIQ can access Teams chat messages far more broadly than initially feared.** Live testing on 2026-03-23 confirms it can retrieve content from channel messages, group chats, meeting chats, 1:1 conversations, and even messages 7+ days old. The limitation Aragorn hit — failing to retrieve a *specific message by deep-link URL* — is a query-formulation issue, not a fundamental access restriction.

**Key finding:** WorkIQ cannot resolve Teams deep-link URLs to specific messages. It operates as a semantic search engine over M365 data, not a message-ID-based retrieval system. The workaround is to query by **meeting name + sender + time range + content snippet** instead of by URL.

---

## 1. WorkIQ Capabilities — What It CAN Access

All findings below are from **live testing** on 2026-03-23 using `workiq-ask_work_iq`.

### 1.1 Channel Messages ✅ CONFIRMED

**Test:** "List all Teams channel messages from the DK8S Clusters (Kubernetes) - Support channel from today with full message content"

**Result:** Returned **11 full channel messages** with verbatim content, sender names, thread context, and deep-link URLs. Included root messages and reply threads. Content was rich and detailed — multi-paragraph messages with code snippets, error messages, and discussion threads were fully reproduced.

**Evidence:** Full message content from Saar Salhov, George Sarji, Avital Lange, Yoav Saroya, Liad Livni, and others — all with correct deep-link citations.

### 1.2 Group Chat Messages ✅ CONFIRMED

**Test:** "What are all the Teams group chats I participated in today?"

**Result:** Returned **25 group chats** with participants, latest messages, and deep-link URLs. Included named group chats (e.g., "Flags Approvals") and unnamed chats (e.g., "Unnamed Group Chat with Hila Yehuda, Ligal Tuval, Yoni Nave").

### 1.3 Meeting Chat Messages ✅ CONFIRMED

**Test:** "Show me messages from meeting chats today"

**Result:** Returned **3 meeting chat conversations** (UTIP Sync, TI Analyzer agent, and another) with sender attribution, message content summaries, and deep-link URLs. Meeting chat thread IDs follow the `19:meeting_...@thread.v2` pattern.

### 1.4 1:1 (Private) Chat Messages ✅ CONFIRMED

**Test:** "Show me messages from private Teams channels I'm in from the last 24 hours"

**Result:** Returned 1:1 chats with Hila Yehuda, Ligal Tuval, Lama Saba, Ashish Syal, and Yoav Levy. Content included conversation summaries with attributed quotes.

**Nuance:** When asked specifically "Show me my 1:1 private chat messages with Hila Yehuda from today," WorkIQ correctly distinguished that today's messages with Hila were in *group* chats, not 1:1. This suggests accurate conversation-type classification.

### 1.5 Message Replies / Threads ✅ CONFIRMED

**Test:** "What replies have I received to my Teams messages in the last 24 hours"

**Result:** Returned reply chains across 4 conversation groups, attributing responses to specific people (Hila, Ligal, Dror, Yoni Nave) with content summaries. Deep-link URLs pointed to specific reply messages.

### 1.6 Specific Message Content by Context ✅ CONFIRMED

**Test:** "Show me the exact content of the message sent by Amir Skovronik in the UTIP Sync meeting chat about the PR being approved, from earlier today"

**Result:** Successfully returned the **exact message content**: `"The pr was approved"` — with sender, meeting name, and contextual information about surrounding messages.

**This is the critical finding:** WorkIQ *can* retrieve specific messages when given enough context (sender + meeting + topic + time). It just can't do it by URL.

### 1.7 Messages from 7+ Days Ago ✅ CONFIRMED

**Test:** "Show me Teams messages from 7 days ago"

**Result:** Returned **25 messages/conversations** from the previous Monday, including 1:1 chats, team channels, meeting chats, and community channels. No evidence of a time-based access cutoff.

### 1.8 Action Item Extraction ✅ CONFIRMED

**Test:** "What Teams chat messages have I received today that mention action items?"

**Result:** Correctly identified the DK8S certificate rotation thread and extracted 5 specific action items (acknowledge IcM, confirm ownership, rotate cert, validate, resolve IcM).

---

## 2. WorkIQ Limitations — What It CANNOT Do

### 2.1 ❌ Retrieve Messages by Deep-Link URL

**Test:** "Retrieve the specific Teams chat message at this URL: https://teams.microsoft.com/l/message/19:meeting_NWRkZDk0MDkt..."

**Result:** WorkIQ explicitly stated: *"I can't directly resolve a specific message by deep-link URL alone via search."* It attempted to search by thread ID but could not match to the exact message ID (`1774247320795`).

**Root cause:** WorkIQ uses M365 Copilot's semantic search index, not the Graph API message-by-ID endpoint. Teams deep-link URLs contain a `messageId` (epoch timestamp) that the search index doesn't index as a queryable field.

**Workaround:** Provide meeting name + sender + time + content snippet instead of URL.

### 2.2 ❌ Retrieve Messages by Message ID

The message ID in Teams URLs (e.g., `1774247320795`) is an epoch-millisecond timestamp. WorkIQ's search layer doesn't expose this as a filterable field. You cannot query "get message ID 1774247320795."

### 2.3 ❌ Distinguish Private Channels Reliably

**Test:** "Show me messages from private Teams channels"

**Result:** WorkIQ acknowledged: *"Teams message data doesn't reliably label whether a channel is 'private' in the search results."* It returned all messages without confidently classifying private vs. public channels.

### 2.4 ⚠️ Indexing Delay (Unquantified)

**Test:** "Show me the most recent messages in my Teams chats from the last 2 hours"

**Result:** Returned **no results** despite messages existing within that window (confirmed by other queries returning "today's" messages). This suggests:
- There IS an indexing delay between message creation and WorkIQ searchability
- The delay is likely 2-4 hours based on the test pattern (2h query = empty, "today" query = populated)
- This aligns with prior knowledge: *"WorkIQ is poll-based with indexing delay"* (from teams-monitor skill analysis)

### 2.5 ⚠️ Content is Summarized, Not Always Verbatim

For most queries, WorkIQ returns **AI-summarized** content rather than raw message text. The one exception was when explicitly asked for "exact content" of a specific message — it returned the verbatim quote. For bulk queries, expect summaries with occasional direct quotes.

### 2.6 ⚠️ No Pagination or Exhaustive Enumeration

WorkIQ returns a curated selection of results. There's no way to paginate through all messages in a time range or guarantee completeness. For the "7 days ago" query, it returned 25 results — likely the top-ranked, not all.

---

## 3. WorkIQ Architecture & Documentation

### 3.1 What WorkIQ Is (Official Documentation)

Source: [Microsoft Learn — Work IQ MCP overview](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/workiq-overview)

- **CLI + MCP Server** connecting AI assistants to M365 Copilot data
- Queries emails, meetings, documents, Teams messages, people info via natural language
- Two modes: CLI (`workiq ask`) and MCP server (`workiq mcp`)
- **Public preview** — features and APIs may change
- **Prerequisites:** Node.js, M365 subscription with Copilot license, admin consent
- **Security model:** Inherits M365 Copilot data protections, respects permissions, no data storage

### 3.2 How WorkIQ Accesses Data

WorkIQ does NOT call Microsoft Graph API directly. It proxies through **Microsoft 365 Copilot's orchestration layer**, which:
1. Receives the natural-language query
2. Determines relevant data sources (mail, calendar, Teams, files, people)
3. Queries the **Microsoft Search index** (which indexes Teams messages, emails, etc.)
4. Returns AI-synthesized results

This is why:
- It can find messages by semantic meaning but not by ID
- Results are summarized, not raw
- There's an indexing delay (Search index lags behind real-time)
- It respects the user's existing M365 permissions

### 3.3 Required Permissions

From the WorkIQ GitHub repo and web research:
- `Chat.Read` — Read user's chats
- `ChannelMessage.Read.All` — Read channel messages
- Admin consent required for tenant-level access
- All access is scoped to authenticated user's permissions

### 3.4 Available Plugins

Source: [GitHub — microsoft/work-iq-mcp](https://github.com/microsoft/work-iq-mcp)

| Plugin | Description |
|--------|-------------|
| **workiq** | Core M365 data querying (emails, meetings, docs, Teams, people) |
| **microsoft-365-agents-toolkit** | Toolkit for building M365 Copilot declarative agents |
| **workiq-productivity** | Read-only productivity insights (email triage, meeting costs, org charts) |

---

## 4. Microsoft Graph API — The Direct Alternative

### 4.1 Relevant Endpoints

| Endpoint | Description |
|----------|-------------|
| `GET /chats/{chat-id}/messages` | List messages in a specific chat |
| `GET /chats/{chat-id}/messages/{message-id}` | Get a specific message by ID |
| `GET /teams/{team-id}/channels/{channel-id}/messages` | List channel messages |
| `GET /me/chats` | List all user's chats |
| `GET /chats/{chat-id}/messages?$filter=from/user/id eq '{user-id}'` | Filter by sender |

Source: [Microsoft Learn — List messages in a chat](https://learn.microsoft.com/en-us/graph/api/chat-list-messages?view=graph-rest-1.0)

### 4.2 Permissions Required

**Delegated (work/school account):**
- `Chat.Read` — Read user's chat messages
- `Chat.ReadWrite` — Read and write chat messages

**Application permissions:**
- `ChatMessage.Read.Chat` — Resource-specific consent (per-chat)
- `Chat.Read.All` — Read all chats in tenant (requires admin consent)
- `Chat.ReadWrite.All` — Read/write all chats in tenant

### 4.3 Graph API vs WorkIQ — Comparison

| Capability | WorkIQ | Graph API |
|-----------|--------|-----------|
| Retrieve by message ID | ❌ | ✅ |
| Retrieve by chat ID | ❌ (semantic search only) | ✅ |
| Semantic search | ✅ | ❌ (keyword only via `$search`) |
| Real-time access | ❌ (indexing delay) | ✅ (near real-time) |
| Bulk enumeration | ❌ (curated results) | ✅ (pagination support) |
| Action item extraction | ✅ (AI-powered) | ❌ (raw data only) |
| Authentication | M365 Copilot license + admin consent | App registration + delegated/app permissions |
| Setup complexity | Low (npm install) | Medium (app reg, consent, token management) |

### 4.4 Can We Use Graph API as Fallback?

**Yes, but with significant effort.** Graph API requires:
1. Azure AD app registration
2. API permissions granted by admin
3. OAuth2 token acquisition flow
4. Direct HTTP calls to Graph endpoints

This is a fundamentally different integration pattern from WorkIQ's "ask a question" approach. It would be a separate tool/script, not a simple fallback within the same pipeline.

---

## 5. Real-Time Alternatives — Webhooks & Subscriptions

### 5.1 Microsoft Graph Change Notifications

Source: [Microsoft Learn — Change notifications for chat messages](https://learn.microsoft.com/en-us/graph/teams-changenotifications-chatmessage)

Graph API supports **webhook subscriptions** for real-time chat message notifications:

- **Subscribe to all channel messages:** `POST /subscriptions` with resource `/teams/getAllMessages`
- **Subscribe to all chat messages:** `POST /subscriptions` with resource `/chats/getAllMessages`
- Supports `includeResourceData` for message content in notifications
- Delivery via webhook, Azure Event Hubs, or Azure Event Grid
- **Requires application permissions** (not delegated)
- Subscriptions expire and need renewal (max ~1 hour without lifecycle notification URL)
- **Licensing/payment implications** for production use of `/getAllMessages` endpoints

### 5.2 Power Automate Triggers

| Trigger | Channels | Private/Group Chats | Automatic |
|---------|----------|---------------------|-----------|
| "When a new channel message is added" | ✅ | ❌ | ✅ |
| "For a selected message" | ✅ | ✅ | ❌ (manual) |
| Custom (Graph API polling) | ✅ | ✅ | ✅ |

**Key limitation:** There is **no built-in Power Automate trigger for private/group chat messages**. Only channel messages have an automatic trigger. For chat messages, you must use Graph API directly.

---

## 6. Workarounds & Recommendations

### 6.1 For the Teams Watchdog — WorkIQ IS Sufficient

**Finding:** WorkIQ can retrieve the data needed for a daily Teams message watchdog:
- ✅ "What Teams messages have I received today that mention action items?" → Works
- ✅ "What replies have I received in the last 24 hours?" → Works
- ✅ "Show me messages from meeting chats today" → Works
- ✅ "What decisions were made in my Teams conversations today?" → Works

**The daily batch-summary use case aligns perfectly with WorkIQ's strengths** (semantic search, AI summarization) and tolerates its weaknesses (indexing delay, non-exhaustive results).

### 6.2 Query Formulation Best Practices

When WorkIQ fails to find a specific message, **reformulate the query** using these parameters instead of URLs:

| Parameter | Example |
|-----------|---------|
| Meeting/chat name | "in the UTIP Sync meeting" |
| Sender | "sent by Amir Skovronik" |
| Time range | "from earlier today" / "from yesterday" |
| Content snippet | "about the PR being approved" |
| Topic | "about certificate rotation" |

**Combine 2-3 parameters** for best results. Single-parameter queries may return too many results or miss the target.

### 6.3 For Specific Message Retrieval — Graph API Fallback

When you need to retrieve a *specific* message (e.g., from a Teams URL):

1. **Parse the URL** to extract `threadId` and `messageId`
2. **Use Graph API** `GET /chats/{threadId}/messages/{messageId}` with `Chat.Read` permission
3. This requires a separate authenticated Graph API client (not WorkIQ)

**Recommendation:** Build a small PowerShell script (`scripts/get-teams-message.ps1`) that:
- Accepts a Teams deep-link URL
- Parses out the thread ID and message ID
- Calls Graph API to retrieve the exact message
- Returns the content

This would complement WorkIQ for the edge case Aragorn hit.

### 6.4 Meeting Transcript as Fallback

When meeting chat messages can't be retrieved, **meeting transcripts** are an alternative:
- WorkIQ confirmed it can find meetings and transcripts
- Transcripts capture spoken content, not chat messages
- Useful for: meeting decisions, action items, discussion summaries
- Not useful for: links shared in chat, code snippets, async pre/post-meeting chat

### 6.5 Tamir's teams-ui-automation Plugin

From the squad-skills catalog, `teams-ui-automation` provides browser automation for Teams. This could:
- Navigate to a specific Teams URL
- Extract message content from the rendered page
- Bypass API limitations entirely

**Tradeoff:** Fragile (depends on Teams web UI structure), slow, requires browser session. Best as a last-resort fallback, not a primary data source.

### 6.6 Recommended Architecture for Watchdog

```
┌──────────────────────────────────────────────────┐
│              Teams Watchdog Pipeline              │
├──────────────────────────────────────────────────┤
│                                                  │
│  Primary: WorkIQ (daily batch queries)           │
│  ├─ "Action items from today's messages"         │
│  ├─ "Replies to my messages in 24h"              │
│  ├─ "Decisions in meeting chats today"           │
│  └─ "Messages mentioning me today"               │
│                                                  │
│  Fallback: Graph API (specific messages)         │
│  └─ When URL-based retrieval is needed           │
│                                                  │
│  Future: Graph Webhooks (real-time)              │
│  └─ Subscribe to /chats/getAllMessages            │
│     for near-instant notification                │
│                                                  │
└──────────────────────────────────────────────────┘
```

---

## 7. Eng.ms Search Results

**Note:** `enghub-search` returned "Access denied" for all queries attempted:
- "WorkIQ MCP tool capabilities limitations"
- "Microsoft Graph API chat messages Teams retrieval"
- "work-iq MCP M365 Copilot tool chat access"

This may indicate the current user's eng.ms permissions don't cover the relevant service nodes. Documentation was sourced from Microsoft Learn, the WorkIQ GitHub repo, and web search instead.

---

## 8. Open Questions for Further Investigation

1. **Exact indexing delay:** What is the precise lag between a Teams message being sent and it appearing in WorkIQ results? Testing suggests 2-4 hours, but this needs more data points.

2. **Result completeness:** Does WorkIQ return ALL matching messages or a ranked subset? The 25-result cap in several tests suggests a limit.

3. **Graph API integration feasibility:** Can we register an Azure AD app for the squad's Graph API fallback script? What permissions would Jonathan's admin need to grant?

4. **Webhook subscription costs:** The `/chats/getAllMessages` and `/teams/getAllMessages` subscription endpoints may have licensing/payment implications. What's the cost model?

5. **WorkIQ rate limits:** The teams-monitor skill documents "rate-limit to one WorkIQ query per agent cycle." What's the actual throttling behavior?

---

## Appendix: Raw Test Results

### Test 1: "Show me the most recent messages in my Teams chats from the last 2 hours"
**Result:** No results found. WorkIQ suggested expanding time range or checking channel posts.

### Test 2: "What Teams chat messages have I received today that mention action items?"
**Result:** 1 conversation (DK8S Clusters) with 5 extracted action items. Included deep-link URLs.

### Test 3: "Show me messages from meeting chats today"
**Result:** 3 meeting chats (UTIP Sync, TI Analyzer, another). Content summaries with sender attribution.

### Test 4: "What replies have I received to my Teams messages in the last 24 hours"
**Result:** 4 conversation groups with 7 deep-link citations. Included 1:1s, group chats, channels.

### Test 5: "Retrieve specific message by URL"
**Result:** Failed. WorkIQ cannot resolve deep-link URLs to specific messages.

### Test 6: "Show me my 1:1 private chat messages with Hila Yehuda from today"
**Result:** Correctly identified no 1:1 messages today (all were group chats). Demonstrates accurate classification.

### Test 7: "Show me messages from private Teams channels"
**Result:** Returned all recent messages but couldn't reliably distinguish private vs. public channels.

### Test 8: "Show me Teams messages from 7 days ago"
**Result:** 25 messages/conversations from the previous Monday. No evidence of time-based cutoff.

### Test 9: "What are all Teams group chats I participated in today?"
**Result:** 25 group chats with participants and latest messages. Comprehensive coverage.

### Test 10: "Exact content of Amir's PR approval message in UTIP Sync"
**Result:** Verbatim quote: "The pr was approved" — with meeting context and surrounding messages.

### Test 11: "DK8S channel messages with full content"
**Result:** 11 full channel messages with verbatim multi-paragraph content, error messages, and code.
