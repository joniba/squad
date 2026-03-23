---
title: "Teams Knowledge Library Guide"
date: 2026-03-24
author: bilbo
documentarian: bilbo
category: guide
tags:
  - guide
  - squad-infra
  - teams
  - bilbo
status: final
related_docs:
  - guides/unified-scheduler-guide.md
related_issues: []
---

# Teams Knowledge Library Guide

The **Teams Knowledge Library** is a growing repository of structured knowledge extracted from daily Teams conversations. It captures decisions, action items, and important context — organized by date and topic — so patterns, recurring issues, and commitments stay visible and organized.

Unlike the general knowledge base (`docs/`), which preserves formal documentation and investigations, the Teams Knowledge Library is **feed-driven** and **watchdog-powered**: daily summaries from the Teams watchdog are parsed and filed into organized categories, creating a living record of what matters.

---

## What It Is

The Teams Knowledge Library:

- **Captures watchdog output** — daily Teams message summaries feed into organized knowledge
- **Organizes by category** — Decisions, Action Items, Context, and Themes
- **Reduces noise** — filters out routine messages; preserves only structured, actionable insights
- **Stays current** — dates, status tags, and cross-references keep everything fresh
- **Reveals patterns** — tags and themes surface recurring issues (e.g., "orchestration gap", "ARM watchlist")

It lives at `teams-knowledge/` (repo root) separate from `docs/`, reflecting its different cadence and purpose.

---

## How It Works

```
Daily Teams Watchdog Summary
           ↓
    Bilbo parses three sections:
    • Decisions Yoni made
    • Action items requiring follow-up
    • Key context/gaps identified
           ↓
    One document per distinct item
    (not per day — each decision/action
    gets its own file)
           ↓
    Filed in appropriate folder:
    decisions/ | action-items/ | context/
           ↓
    Tagged and indexed
    (INDEX.md, TAGS.md updated)
           ↓
    Tracked for patterns
    (recurring themes → themes/ folder)
```

When patterns emerge (e.g., "orchestration gap" appears in weeks 1, 2, and 3), a **theme document** is created to collect all related decisions, actions, and context in one place.

---

## Library Structure

```
teams-knowledge/
├── INDEX.md              # Master index by date & category
├── TAGS.md               # Reverse index by tag
│
├── decisions/            # Strategic decisions, prioritizations, escalations
│   ├── 2026-03-23-arm-watchlist-priority.md
│   ├── 2026-03-23-ti-analyzer-spec-hold.md
│   └── 2026-03-23-git-worktree-bug.md
│
├── action-items/         # Follow-ups, syncs, investigations
│   ├── 2026-03-23-sync-hila-ti-analyzer.md
│   ├── 2026-03-23-fix-service-tree-ownership.md
│   └── 2026-03-23-review-ti-analyzer-tasks.md
│
├── context/              # Key context, gaps, issues, observations
│   └── 2026-03-23-orchestration-gap.md
│
└── themes/               # Recurring themes (future)
    └── (generated as patterns emerge)
```

---

## Browsing the Library

### Start with the Index

**`teams-knowledge/INDEX.md`** — Master index grouped by category and date:

```markdown
## Decisions (4)
| Date | Title | Author | Status | Summary |
| 2026-03-23 | Prioritize SEV2 ARM Watchlist Rate-Limiting Issue | Yoni | Active | ... |
| 2026-03-23 | Freeze TI Analyzer Spec | Yoni | Active | ... |

## Action Items (4)
| 2026-03-23 | Sync with Hila on TI Analyzer | Yoni | Active | ... |
```

Browse by category, sorted newest first. One-line summaries help you decide what to read.

### Browse by Tag

**`teams-knowledge/TAGS.md`** — Reverse index groups related items across categories:

```markdown
## arm-watchlist
- [Prioritize SEV2 ARM Watchlist Rate-Limiting Issue](decisions/2026-03-23-arm-watchlist-priority.md) — 2026-03-23
- [Fix Service Tree Ownership](action-items/2026-03-23-fix-service-tree-ownership.md) — 2026-03-23

## orchestration
- [Orchestration Gap — Parallel Agents Blocked](context/2026-03-23-orchestration-gap.md) — 2026-03-23
```

Use tags to discover **related items** across categories. If you care about "orchestration", TAGS.md shows all decisions, actions, and context items tagged with it.

### Available Tags

| Tag | Meaning | Example Items |
|-----|---------|--------|
| `arm-watchlist` | ARM watchlist issues, rate limiting | Watchlist failures, service tree fixes |
| `orchestration` | Squad orchestration, parallel agents | Git worktree bugs, parallelism blockers |
| `ti-analyzer` | TI Analyzer project | Spec holds, timeline syncs |
| `ai-squads` | AI Squads program | Endorsements, direction shifts |
| `azure` | Azure-related | ARM, service tree, SME coordination |
| `teams` | Teams-originated insights | All watchdog items |
| `workflow` | Process/workflow issues | Scheduler, pipeline failures |

---

## Common Browsing Patterns

### "What decisions has Yoni made recently?"

1. Open `teams-knowledge/INDEX.md`
2. Look at **Decisions** section (sorted newest first)
3. Read summaries to find relevant decision, then click link

### "What's blocking us on orchestration?"

1. Open `teams-knowledge/TAGS.md`
2. Find `orchestration` tag section
3. See all related decisions, actions, context
4. Follow links to read full details

### "What do I need to do?"

1. Open `teams-knowledge/INDEX.md`
2. Scroll to **Action Items** section
3. See your assignments with context and status

### "What's the context on the ARM watchlist issue?"

1. Search TAGS.md for `arm-watchlist`
2. See all related decisions, actions, context
3. Follow links to understand full picture

---

## Document Status

Each document carries a **status tag** that tracks its lifecycle:

| Status | Meaning | Example |
|--------|---------|---------|
| `active` | Ongoing, needs attention | "Sync with Hila on TI Analyzer" (pending on-call) |
| `resolved` | Completed or resolved | (none yet) |
| `escalated` | Escalated to leadership | (none yet) |
| `tracking` | Monitoring situation | "Orchestration Gap" (ongoing issue, being tracked) |

When browsing, **focus on `active` items** — those need attention. `tracking` items are important context but may not need immediate action.

---

## Adding Items Manually

If a decision or action item doesn't get captured in the daily watchdog, you can add it manually:

### Create a Decision Document

File: `teams-knowledge/decisions/{YYYY-MM-DD-descriptive-slug}.md`

```markdown
---
title: "Your Decision Title"
date: 2026-03-24
author: yoni
documentarian: bilbo
category: decision
tags:
  - decision
  - teams
  - azure
  - yoni
  - active
status: active
---

# Your Decision Title

## Decision

> Clear statement of the decision made

## Rationale

- Key reasoning
- Any constraints

## Implications

- What this means for current work
- Who needs to know
- Next steps
```

### Create an Action Item Document

File: `teams-knowledge/action-items/{YYYY-MM-DD-descriptive-slug}.md`

```markdown
---
title: "Your Action Item"
date: 2026-03-24
author: yoni
documentarian: bilbo
category: action-item
tags:
  - action-item
  - teams
  - [domain tags...]
  - yoni
  - active
status: active
---

# Your Action Item

## Action

> What needs to be done

## Context

- Why this matters
- Any dependencies

## Owner & Timeline

- Owner: [person name]
- Target date: [if known]
```

### Create a Context Document

File: `teams-knowledge/context/{YYYY-MM-DD-descriptive-slug}.md`

```markdown
---
title: "Context Item Title"
date: 2026-03-24
author: yoni
documentarian: bilbo
category: context
tags:
  - context
  - teams
  - [domain tags...]
  - yoni
  - tracking
status: tracking
---

# Context Item Title

## Situation

> Description of context or gap

## Why It Matters

- Impact or risk
- Who's affected

## Related Items

- Cross-references to decisions/actions
```

### Update the Indexes

After creating a new document, update both indexes:

1. **`teams-knowledge/INDEX.md`** — Add entry under the correct category (sorted by date, newest first)
2. **`teams-knowledge/TAGS.md`** — Add entry under each tag the document carries

---

## Updating Status

As items resolve or progress, update their status:

```yaml
# Change this:
status: active

# To this:
status: resolved
```

Then update INDEX.md to reflect the status change. Items can progress:
- `active` → `resolved` (action completed, decision implemented)
- `active` → `escalated` (problem needs leadership attention)
- `active` → `tracking` (ongoing situation, lower priority)

---

## Separating Teams Knowledge from General Knowledge

The Teams Knowledge Library is **intentionally separate** from `docs/`:

### Teams Knowledge Library (`teams-knowledge/`)
- **Feed-driven** — watchdog generates daily items
- **Current/active** — focuses on recent decisions and actions
- **Status tags** — tracks progress (active, resolved, escalated)
- **Short shelf-life** — items move from active → resolved or tracking

### General Knowledge Base (`docs/`)
- **Event-driven** — created when significant work completes
- **Permanent records** — investigations, research, decisions
- **Status frozen** — draft → final → superseded (rarely change)
- **Long shelf-life** — investigations, learnings, architecture decisions

### When to Cross-Reference

Link to `docs/` when:
- A Teams action item becomes an investigation → `docs/investigations/`
- A decision resolves → becomes a research document or audit
- A pattern merits formal documentation → becomes a guide or decision record

---

## Tags Overview

### Type Tags (Required)

One per document:
- `decision` — Strategic decisions, prioritizations
- `action-item` — Follow-ups, investigations
- `context` — Observations, gaps, issues
- `theme` — Recurring patterns (future)

### Domain Tags (One or More Required)

- `teams` — Teams-originated watchdog items
- `azure` — Azure services, ARM, resources
- `architecture` — System design decisions
- `squad-infra` — Squad governance, agents
- `tooling` — Tools, scripts, automation
- `workflow` — Processes, pipelines
- `git` — Git, worktrees, version control
- `product` — Product feedback, features

### Project/Theme Tags (Optional)

As recurring patterns emerge, new tags are created:
- `arm-watchlist` — ARM rate-limiting issues
- `orchestration` — Squad orchestration, parallelism
- `ti-analyzer` — TI Analyzer project
- `ai-squads` — AI Squads program

### Status Tags (Required)

One per document:
- `active` — Ongoing, needs attention
- `resolved` — Completed
- `escalated` — Elevated to leadership
- `tracking` — Monitoring, lower priority

---

## Related Documentation

- **Unified Scheduler Guide** — `docs/guides/unified-scheduler-guide.md` (how the watchdog runs)
- **Knowledge Management System** — `.squad/skills/knowledge-management/SKILL.md` (general library design)
- **Teams Knowledge Library Skill** — `.squad/skills/teams-knowledge/SKILL.md` (technical details)
