---
name: "workiq-patterns"
description: "Query patterns, capabilities, and workarounds for retrieving Teams messages via WorkIQ"
domain: "teams, workiq, m365, chat-retrieval"
confidence: "high"
source: "earned — live-tested 2026-03-23 (11 tests, documented in docs/research/workiq-chat-limitations-research.md)"
tools:
  - name: "workiq-ask_work_iq"
    description: "Natural-language query against M365 Copilot data (emails, meetings, Teams, files, people)"
    when: "Any time you need Teams message content, meeting info, email context, or people lookup"
---

## Context

WorkIQ is a semantic search engine over M365 data, accessed via the `workiq-ask_work_iq` MCP tool. It proxies through Microsoft 365 Copilot's orchestration layer — it does NOT call Graph API directly. This means it excels at natural-language retrieval but cannot do ID-based lookups.

Any agent needing Teams message context (watchdog, ICM investigator, daily summary, ad-hoc lookup) should read this skill before querying.

## What Works

### Channel messages ✅
Query: `"List all Teams channel messages from the {channel-name} channel from today with full message content"`
Returns: Full verbatim content, sender names, thread context, deep-link URLs.

### Group chats ✅
Query: `"What are all the Teams group chats I participated in today?"`
Returns: Chat names, participants, latest messages, deep-link URLs.

### Meeting chats ✅
Query: `"Show me messages from meeting chats today"`
Returns: Meeting chat conversations with sender attribution, content summaries, deep-link URLs.

### 1:1 (private) chats ✅
Query: `"Show me messages from private Teams channels I'm in from the last 24 hours"`
Returns: 1:1 conversations with content summaries and attributed quotes. WorkIQ correctly distinguishes 1:1 from group chats.

### Replies and threads ✅
Query: `"What replies have I received to my Teams messages in the last 24 hours"`
Returns: Reply chains across conversations with sender attribution and deep-links to specific replies.

### Specific message by context ✅
Query: `"Show me the exact content of the message sent by {sender} in the {meeting-name} meeting chat about {topic}, from {time-range}"`
Returns: Verbatim message content when enough context parameters are combined.

### Historical messages (7+ days) ✅
Query: `"Show me Teams messages from 7 days ago"`
Returns: Messages from a week prior. No evidence of a time-based access cutoff.

### Action item extraction ✅
Query: `"What Teams chat messages have I received today that mention action items?"`
Returns: AI-extracted action items with source attribution.

## What Doesn't Work

### ❌ URL-based retrieval
WorkIQ **cannot** resolve Teams deep-link URLs to specific messages. Queries like "retrieve the message at https://teams.microsoft.com/l/message/..." will fail. The search index does not index message IDs as queryable fields.

### ❌ Message ID lookup
Message IDs in Teams URLs (e.g., `1774247320795`) are epoch-millisecond timestamps. WorkIQ cannot query by these IDs.

### ❌ Private vs. public channel distinction
WorkIQ cannot reliably classify whether a channel is private or public. It returns all matching messages regardless.

### ⚠️ Content is summarized by default
Bulk queries return AI-summarized content, not verbatim text. To get exact wording, ask explicitly: "Show me the **exact content** of the message..."

### ⚠️ No pagination / no exhaustive enumeration
Results are curated (typically ~25 per query). There is no way to paginate or guarantee completeness.

## Patterns

### Pattern 1: Find a specific message (URL workaround)

When someone gives you a Teams URL and you need the message content, **do not pass the URL to WorkIQ**. Instead, extract context from the URL or surrounding conversation and query by attributes:

```
workiq-ask_work_iq:
  question: "Show me the exact content of the message sent by {sender} in the {meeting-or-chat-name} about {topic}, from {time-range}"
```

Combine **2-3 parameters minimum** for reliable results:
| Parameter       | Example                              |
|-----------------|--------------------------------------|
| Meeting/chat    | "in the UTIP Sync meeting"           |
| Sender          | "sent by Amir Skovronik"             |
| Time range      | "from earlier today" / "from Monday" |
| Content snippet | "about the PR being approved"        |
| Topic           | "about certificate rotation"         |

### Pattern 2: Daily message scan

For batch retrieval of the day's messages (watchdog, daily summary):

```
workiq-ask_work_iq:
  question: "What Teams chat messages have I received today that mention action items?"

workiq-ask_work_iq:
  question: "What replies have I received to my Teams messages in the last 24 hours?"

workiq-ask_work_iq:
  question: "What decisions were made in my Teams conversations today?"

workiq-ask_work_iq:
  question: "Show me messages from meeting chats today"
```

### Pattern 3: Scan a specific channel

```
workiq-ask_work_iq:
  question: "List all Teams channel messages from the {channel-name} channel from today with full message content"
```

This returns verbatim multi-paragraph content including code snippets and error messages.

### Pattern 4: Find messages from a specific person

```
workiq-ask_work_iq:
  question: "Show me all Teams messages from {person-name} in the last {time-range}"
```

### Pattern 5: Historical lookup

```
workiq-ask_work_iq:
  question: "Show me Teams messages from {N} days ago about {topic}"
```

Works reliably for messages 7+ days old. No known time-based cutoff.

## Indexing Delay

WorkIQ has a **search indexing delay** between when a message is sent and when it becomes queryable:

- Queries for "last 2 hours" returned **empty** despite messages existing in that window
- Queries for "today" returned results (same messages)
- **Estimated delay: minutes to a few hours** — exact latency varies
- **Implication:** Do not use WorkIQ for near-real-time message retrieval. It is designed for batch/summary use cases with tolerance for lag.

**Rule of thumb:** Query for "today" or "last 24 hours" — avoid sub-hour time windows.

## Rate Limiting

Rate-limit to **one WorkIQ query per agent action**. Rapid-fire queries may be throttled. If you need multiple data points, combine them into a single well-scoped query rather than sending many narrow ones.

## Anti-Patterns

- ❌ **Passing a Teams URL directly** — WorkIQ will fail. Always decompose into sender + chat name + time + topic.
- ❌ **Querying with sub-hour time windows** — Indexing delay makes this unreliable. Use "today" or "last 24 hours" instead.
- ❌ **Expecting exhaustive results** — WorkIQ returns curated top results (~25), not all matching messages. Don't assume completeness.
- ❌ **Assuming verbatim content** — Bulk queries return summaries. Explicitly ask for "exact content" when you need the literal text.
- ❌ **Rapid-fire queries** — Space out requests. Combine related questions into a single query when possible.

## Tool Reference

### `workiq-ask_work_iq`

**Invocation:**
```json
{
  "question": "your natural-language question here"
}
```

**Scope:** Searches across all M365 data the authenticated user can access — Teams messages, emails, meetings, files, people.

**Returns:** AI-synthesized answer with citations (deep-link URLs). Not raw data.

**Prerequisites:** M365 subscription with Copilot license, admin consent granted, WorkIQ EULA accepted.

## Fallback: Graph API

When WorkIQ cannot retrieve a specific message (e.g., from a URL with a known message ID), the Graph API endpoint `GET /chats/{chat-id}/messages/{message-id}` is the direct alternative. This requires a separate authenticated Graph API client — it is not available through WorkIQ. See `docs/research/workiq-chat-limitations-research.md` §6.3 for implementation details.
