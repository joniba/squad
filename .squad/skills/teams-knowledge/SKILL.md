# Teams Knowledge Library Skill

**For:** Bilbo (Librarian & Documentarian)  
**Status:** Low Confidence (First Design)  
**Last Updated:** 2026-03-23

> This skill describes the organizational system for the Teams Knowledge Library — a separate, feed-driven knowledge repository distinct from the general `docs/` library. It captures recurring themes, decisions, and action items from the daily Teams watchdog summary.

---

## Overview

The Teams Knowledge Library complements the general knowledge base by:
- Capturing **watchdog-driven insights** — daily Teams summaries feed recurring themes into organized knowledge
- **Reducing noise** — Not every Teams message becomes a document; only structured summaries (decisions, actions, context) are filed
- **Maintaining separation** — Exists at repo root (`teams-knowledge/`) separate from `docs/`, reflecting different cadence and purpose
- **Enabling pattern discovery** — Tags and cross-references surface recurring themes (e.g., "ARM watchlist", "TI Analyzer", "orchestration")

This skill evolves as patterns emerge. When the watchdog summary identifies new recurring themes, we may add new categories or tags.

---

## Document Organization — Folder Hierarchy

Teams Knowledge Library lives at repo root:

```
teams-knowledge/
├── INDEX.md                         # Master index — all items by category/date
├── TAGS.md                          # Reverse index — all items by tag
│
├── decisions/                       # Teams-originated decisions & escalations
│   └── {date-descriptive-slug}.md
│
├── action-items/                    # Action items requiring follow-up
│   └── {date-descriptive-slug}.md
│
├── context/                         # Key context, gaps, issues (watchdog observations)
│   └── {date-descriptive-slug}.md
│
└── themes/                          # Recurring theme analysis (auto-generated)
    └── {theme-name}.md
```

### Category Definitions

| Category | Folder | What Goes Here | Source |
|----------|--------|---------------|--------|
| **Decision** | `decisions/` | Decisions made by Yoni in watchdog summary; escalations, prioritizations | Daily watchdog summary "Decisions" section |
| **Action Item** | `action-items/` | Follow-up actions, sync requests, investigations to conduct | Daily watchdog summary "Action Items" section |
| **Context** | `context/` | Key context paragraphs, gaps identified, problems surfaced | Daily watchdog summary "Key Context" section |
| **Theme** | `themes/` | Recurring themes across multiple watchdog summaries (e.g., "orchestration gap") | Pattern analysis across watchdog history |

---

## Naming Conventions

- **Filenames:** lowercase, hyphenated, include date (YYYY-MM-DD) as prefix
- **Format:** `{YYYY-MM-DD-descriptive-slug}.md`
- **Examples:** `2026-03-23-arm-watchlist-priority.md`, `2026-03-23-git-worktree-orchestration-bug.md`
- **Slug should be self-explanatory** — reader knows what the item is about from filename alone

---

## Tagging Taxonomy

Teams Knowledge Library uses the same tagging system as the general knowledge base.

### Tag Categories

#### Type Tags (required — matches folder)
`decision` · `action-item` · `context` · `theme`

#### Domain Tags (one or more required)
- `teams` — Teams-originated, watchdog items
- `azure` — Azure services, ARM, resource providers
- `tooling` — Tools, scripts, automation, CLI
- `architecture` — System design, patterns, decisions
- `squad-infra` — Squad framework, governance, agent infrastructure
- `git` — Git operations, branching, worktrees
- `workflow` — Processes, pipelines, procedures
- `product` — Product feedback, feature requests

#### Project/Theme Tags (optional — recurring themes)
- `arm-watchlist` — ARM watchlist request failures, rate limiting
- `ti-analyzer` — TI Analyzer project, spec changes, timeline issues
- `orchestration` — Squad orchestration, parallel agent work, git worktrees
- `ai-squads` — AI Squads program, endorsements, direction

#### Agent Tags (required)
`yoni` · `bilbo` (as documentarian)

#### Status Tags (required)
`active` · `resolved` · `escalated` · `tracking`

### Frontmatter Template

Every document MUST have this YAML frontmatter block:

```yaml
---
title: "Human-Readable Title"
date: 2026-03-23
author: yoni                 # Watchdog originator
documentarian: bilbo        # Always bilbo
category: decision          # Matches folder
tags:
  - decision
  - teams
  - azure
  - yoni
  - active
related_docs: []            # Cross-references (optional)
status: active              # active | resolved | escalated | tracking
---
```

### Tag Rules

1. **Every document gets:** one type tag, one+ domain tags, one agent tag, one status tag
2. **Tags are lowercase, hyphen-separated** — no spaces
3. **Status progression:** `active` → `resolved` or `escalated` or `tracking`
4. **Project tags** (arm-watchlist, ti-analyzer, etc.) added as patterns emerge

---

## The Index System

Two index files provide views into the teams knowledge library. **Both must be updated when documents are added.**

### `teams-knowledge/INDEX.md` — Master Index by Date/Category

Groups all items by category with one-line summaries.

```markdown
# Teams Knowledge Library Index

> Last updated: 2026-03-23 | Total items: 9

## Decisions (4)
| Date | Title | Author | Status | Summary |
|------|-------|--------|--------|---------|
| 2026-03-23 | [ARM Watchlist Priority](decisions/2026-03-23-arm-watchlist-priority.md) | Yoni | Active | Escalate ARM watchlist request failures as SEV2 |

## Action Items (4)
| Date | Title | Author | Status | Summary |
|------|-------|--------|--------|---------|
| 2026-03-23 | [Sync with Hila](action-items/2026-03-23-sync-hila-ti-analyzer.md) | Yoni | Active | Discuss EE and timeline constraints |

## Context (1)
| Date | Title | Author | Status | Summary |
|------|-------|--------|--------|---------|
| 2026-03-23 | [Orchestration Gap](context/2026-03-23-orchestration-gap.md) | Yoni | Tracking | Parallel agents blocked by git checkout fallback |

## Themes (0)
No recurring themes yet.
```

**Rules:**
- Categories appear in order: Decisions, Action Items, Context, Themes
- Within each, sorted by date (newest first)
- Empty categories show header with "No items yet"
- Summary ≤20 words

### `teams-knowledge/TAGS.md` — Reverse Index by Tag

For each tag, lists items that carry it.

```markdown
# Tag Index

> Last updated: 2026-03-23 | Total tags: 12

## arm-watchlist
- [ARM Watchlist Priority](decisions/2026-03-23-arm-watchlist-priority.md) — 2026-03-23
- [Fix Service Tree Ownership](action-items/2026-03-23-fix-service-tree-ownership.md) — 2026-03-23

## orchestration
- [Orchestration Gap](context/2026-03-23-orchestration-gap.md) — 2026-03-23
- [Git Worktree Orchestration Bug](decisions/2026-03-23-git-worktree-bug.md) — 2026-03-23

[...repeat for each tag...]
```

**Rules:**
- Tags sorted alphabetically
- Within each tag, items sorted by date (newest first)
- Tags with zero items omitted
- Format: `- [Title](path) — YYYY-MM-DD`

---

## Document Structure

### Decision Document Template
```markdown
---
title: "{Decision Title}"
date: 2026-03-23
author: yoni
documentarian: bilbo
category: decision
tags:
  - decision
  - teams
  - [domain tags...]
  - yoni
  - active
status: active
---

# {Decision Title}

## Decision

> Clear statement of the decision made

## Rationale

- Key reasoning or concern that prompted this decision
- Any constraints or context

## Implications

- What this means for current work
- Who needs to know
- Next steps (if any)
```

### Action Item Document Template
```markdown
---
title: "{Action Item Title}"
date: 2026-03-23
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

# {Action Item Title}

## Action

> What needs to be done

## Context

- Why this matters
- Any dependencies or constraints

## Owner & Timeline

- Owner: [person name]
- Target date: [if known]
- Dependencies: [if any]
```

### Context Document Template
```markdown
---
title: "{Context Title}"
date: 2026-03-23
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

# {Context Title}

## Situation

> Description of the context or gap identified

## Why It Matters

- Impact or risk this presents
- Who's affected
- Current state

## Related Items

- Other decisions or actions in response (if any)
- Cross-references to general knowledge base
```

---

## Maintenance

### When to Add Documents

1. **Daily watchdog summary arrives** (summary-{YYYY-MM-DD}.md)
2. Parse the three sections: Decisions, Action Items, Key Context
3. Create one document per distinct item (not per day)
4. Use the date from the watchdog summary in filename and frontmatter

### When to Retire Documents

- **Action Item Resolved** → Change status to `resolved`, mark with completion date
- **Decision Superseded** → Create new decision doc with reference, update status to `tracking`
- **Context Resolved** → Change status to `resolved` with resolution note

### Theme Documents (Future)

When the same issue appears in multiple watchdog summaries (e.g., "orchestration gap" in weeks 1, 2, 3):
1. Create a theme document at `themes/{theme-name}.md`
2. Link all related decisions/actions/context to it
3. Use status tags to track: `active` (ongoing), `resolved` (fixed), `escalated` (to leadership)

---

## Quality Standards

Every document must have:
- **Proper metadata** — date, author (yoni), documentarian (bilbo), category, tags
- **Correct frontmatter** — YAML block with all required fields
- **Correct folder** — matching document type
- **Concise content** — clear decision/action/context, no padding
- **Index entries** — in INDEX.md and TAGS.md immediately after creation

---

## Integration with General Knowledge Base

The Teams Knowledge Library **complements** but remains separate from `docs/`:

- **When to cross-reference:** A decision or action item that resolves becomes an investigation, research document, or guide in `docs/`
- **Ownership:** Bilbo maintains both libraries; watchdog items feed the teams library, other sources feed the general library
- **Tagging:** The `teams` domain tag helps identify teams-originated documents that may graduate to general knowledge

---

## Versioning

- **Skill Version:** 1.0 (2026-03-23) — initial design, watchdog-driven structure
- **Next Review:** After 2–3 weeks of watchdog summaries, when patterns emerge
- **Expected Changes:** Additional project tags, theme documents, possible category expansions as watchdog summaries reveal recurring patterns
