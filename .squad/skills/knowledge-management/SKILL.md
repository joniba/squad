# Knowledge Management Skill

**For:** Bilbo (Librarian & Documentarian)  
**Status:** Low Confidence (First Design)  
**Last Updated:** 2026-03-22

> This skill describes the organizational system that Bilbo implements and evolves. It's separate from Bilbo's charter because the system design may change — the charter is permanent identity, this is living architecture.

---

## Overview

The Knowledge Management Skill defines **how** Bilbo organizes squad knowledge:
- Folder structure and categories
- Tagging taxonomy
- Index formats and maintenance
- Document templates
- Naming conventions
- Quality standards

This skill allows Bilbo to evolve the system over time without freezing decisions in the permanent charter.

---

## Document Organization — Folder Hierarchy

All squad knowledge lives under `docs/` with this structure:

```
docs/
├── INDEX.md                         # Master index — all docs by category
├── TAGS.md                          # Reverse index — all docs by tag
├── RECENT.md                        # Chronological — last 20 additions
│
├── investigations/                  # ICM & livesite investigation reports
│   └── icm-{incident-id}.md
│
├── research/                        # Research reports, deep dives, analyses
│   └── {descriptive-slug}.md
│
├── decisions/                       # Architecture & design decision records
│   └── {descriptive-slug}.md
│
├── audits/                          # Audit reports, assessments, evaluations
│   └── {descriptive-slug}.md
│
├── guides/                          # How-to guides, runbooks, tutorials
│   └── {descriptive-slug}.md
│
├── catalogs/                        # Reference catalogs, inventories, listings
│   └── {descriptive-slug}.md
│
├── feedback/                        # Product feedback, proposals, RFCs
│   └── {descriptive-slug}.md
│
└── tools/                           # Tool documentation, capability specs
    └── {descriptive-slug}.md
```

### Category Definitions

| Category | Folder | What Goes Here | Examples |
|----------|--------|---------------|----------|
| **Investigation** | `investigations/` | ICM incident reports, livesite event analyses, outage post-mortems | `icm-766712513.md`, `icm-764634026.md` |
| **Research** | `research/` | Deep-dive analyses, technology evaluations, comparative studies | `worktree-parallelism-research.md`, `worktree-article-analysis.md` |
| **Decision** | `decisions/` | Architecture decisions, technology choices, process changes (ADR-style) | `knowledge-graph-db-evaluation.md`, `state-management-approach.md` |
| **Audit** | `audits/` | Systematic assessments of existing systems, gap analyses, compliance reviews | `squad-infra-audit.md` |
| **Guide** | `guides/` | Step-by-step how-tos, runbooks, onboarding docs, tutorials | `watchdog-pipeline-setup.md` |
| **Catalog** | `catalogs/` | Reference inventories, capability listings, tool directories | `squad-skills-catalog.md` |
| **Feedback** | `feedback/` | Product feedback, feature proposals, RFCs, user experience reports | `cli-draft-flag-feedback.md` |
| **Tool Doc** | `tools/` | Documentation for specific tools, scripts, and capabilities | `aragorn-icm-capability-upgrade.md` |

---

## Naming Conventions

- **Filenames:** lowercase, hyphen-separated, descriptive: `{descriptive-slug}.md`
- **Investigations:** always prefixed with `icm-{incident-id}.md`
- **No dates in filenames** — dates go in frontmatter (keeps filenames stable for linking)
- **No agent names in filenames** — author goes in frontmatter (keeps filenames reader-focused)
- **Slug should be self-explanatory** — a reader should know what the doc is about from the filename alone

---

## Tagging Taxonomy

Every document gets tags in its YAML frontmatter. Tags enable cross-referencing and the reverse index in `TAGS.md`.

### Tag Categories

#### Type Tags (one required — matches the folder)
`investigation` · `research` · `decision` · `audit` · `guide` · `catalog` · `feedback` · `tool-doc`

#### Domain Tags (one or more required)
| Tag | Covers |
|-----|--------|
| `icm` | ICM incidents, livesite events, incident management |
| `livesite` | Operational issues, monitoring, alerting, health |
| `architecture` | System design, patterns, structural decisions |
| `squad-infra` | Squad framework, governance, agent infrastructure |
| `tooling` | Tools, scripts, automation, CLI |
| `workflow` | Processes, pipelines, procedures |
| `security` | Security analysis, auth, secrets, access control |
| `research-method` | Research methodology, analysis techniques |
| `product` | Product feedback, feature requests, UX |
| `azure` | Azure services, ARM, resource providers |
| `teams` | Microsoft Teams, messaging, notifications |
| `git` | Git operations, branching, worktrees |

#### Agent Tags (one required — the author/owner)
`aragorn` · `elrond` · `bilbo` · `gimli` · `gandalf` · `galadriel`

#### Status Tags (one required)
| Tag | Meaning |
|-----|---------|
| `draft` | Work in progress, not yet reviewed |
| `final` | Complete and reviewed |
| `superseded` | Replaced by a newer document (link to replacement in frontmatter) |
| `archived` | No longer current but preserved for historical reference |

### Frontmatter Template

Every document MUST have this YAML frontmatter block:

```yaml
---
title: "Human-Readable Title"
date: 2026-03-22
author: aragorn            # Agent who produced the content
documentarian: bilbo       # Always bilbo (who formatted/indexed it)
category: investigation    # Matches the folder
tags:
  - investigation
  - icm
  - livesite
  - azure
  - aragorn
status: final
related_issues:            # GitHub issue numbers (optional)
  - 14
  - 15
related_docs:              # Paths to related docs (optional)
  - investigations/icm-764634026.md
superseded_by: null        # Path to replacement doc if superseded
---
```

### Tag Rules

1. **Every document gets at least one type tag, one domain tag, one agent tag, and one status tag**
2. **Tags are lowercase, hyphen-separated** — no spaces, no camelCase
3. **New tags require a decision record** — don't invent tags ad hoc; propose additions via `.squad/decisions/inbox/`
4. **Status transitions:** `draft` → `final` → `superseded` or `archived`
5. **The `superseded` status requires `superseded_by` to be set** — always link to the replacement

---

## The Index System

Three index files provide different views into the knowledge base. **All three must be updated whenever a document is added, modified, or removed.**

### `docs/INDEX.md` — Master Index by Category

The primary entry point. Groups all documents by category with one-line summaries.

```markdown
# Knowledge Base Index

> Last updated: 2026-03-22 | Total documents: 7

## Investigations
| Document | Date | Author | Status | Summary |
|----------|------|--------|--------|---------|
| [ICM 766712513](investigations/icm-766712513.md) | 2026-03-22 | Aragorn | Final | ARM error rates on SecurityInsights/Watchlists |

## Research
| Document | Date | Author | Status | Summary |
|----------|------|--------|--------|---------|
| [Worktree Parallelism](research/worktree-parallelism-research.md) | 2026-03-22 | Elrond | Final | Gap analysis of squad's worktree strategy |

[...repeat for each category...]
```

**Rules:**
- Categories appear in this fixed order: Investigations, Research, Decisions, Audits, Guides, Catalogs, Feedback, Tools
- Within each category, documents are sorted by date (newest first)
- Empty categories still show the header (with "No documents yet")
- Summary is ≤15 words

### `docs/TAGS.md` — Reverse Index by Tag

For each tag, lists every document that carries it. Enables cross-referencing.

```markdown
# Tag Index

> Last updated: 2026-03-22 | Total tags: 14

## icm
- [ICM 766712513](investigations/icm-766712513.md) — 2026-03-22
- [ICM 764634026](investigations/icm-764634026.md) — 2026-03-22

## architecture
- [Knowledge Graph DB Evaluation](decisions/knowledge-graph-db-evaluation.md) — 2026-03-22

[...repeat for each tag that has documents...]
```

**Rules:**
- Tags are sorted alphabetically
- Within each tag, documents are sorted by date (newest first)
- Tags with zero documents are omitted (unlike categories)
- Format: `- [Title](path) — YYYY-MM-DD`

### `docs/RECENT.md` — Chronological Recent Additions

The last 20 documents added or significantly updated, most recent first.

```markdown
# Recent Documents

> Last updated: 2026-03-22

| Date | Category | Document | Author | Tags |
|------|----------|----------|--------|------|
| 2026-03-22 | Investigation | [ICM 766712513](investigations/icm-766712513.md) | Aragorn | icm, livesite, azure |
| 2026-03-22 | Research | [Worktree Parallelism](research/worktree-parallelism-research.md) | Elrond | git, architecture |

[...up to 20 entries...]
```

**Rules:**
- Maximum 20 entries — oldest falls off when 21st is added
- "Significantly updated" means content changes, not typo fixes
- Tags shown are domain tags only (not type/agent/status tags)

---

## Document Structure Templates

### Investigation Report Template
```markdown
---
[frontmatter]
---
# ICM {id} — {title}

## 1. Executive Summary
[2-3 paragraphs: what happened, severity, impact, current state]

## 2. Timeline
| Time (UTC) | Event |
|---|---|
| ... | ... |

## 3. Impact Assessment
[Customer impact, subscription count, service/region affected]

## 4. Investigation
### 4.1 Data Gathered
[Kusto queries, metrics, TSG findings]
### 4.2 Hypotheses
[What could explain this? Evidence for/against each]
### 4.3 Root Cause
[Confirmed or suspected root cause with evidence]

## 5. Remediation
[Actions taken or recommended, with owner and timeline]

## 6. Classification
[True Positive / False Positive / Noise — with reasoning]
```

### Research Report Template
```markdown
---
[frontmatter]
---
# {Title}

## 1. Executive Summary
[What was researched, key findings, recommendation]

## 2. Context & Motivation
[Why this research was needed]

## 3. Methodology
[How the research was conducted]

## 4. Findings
[Detailed findings with evidence]

## 5. Analysis
[Interpretation of findings, implications]

## 6. Recommendations
[Actionable next steps with priority]
```

### Decision Record Template (ADR-style)
```markdown
---
[frontmatter]
---
# Decision: {Title}

## Status
[Proposed | Accepted | Superseded | Deprecated]

## Context
[What prompted this decision?]

## Decision
[What was decided and why]

## Consequences
[What changes as a result — positive and negative]

## Alternatives Considered
[What else was evaluated and why it was rejected]
```

### Audit Report Template
```markdown
---
[frontmatter]
---
# Audit: {Title}

## 1. Scope
[What was audited, boundaries, time period]

## 2. Methodology
[How the audit was conducted]

## 3. Findings
| # | Severity | Finding | Recommendation |
|---|----------|---------|----------------|
| 1 | High | ... | ... |

## 4. Summary
[Overall assessment, key themes]

## 5. Action Items
[Prioritized list of follow-ups with owners]
```

---

## Document Quality Standards

### Severity Scale for Documentation Issues

| Severity | Meaning | Action |
|----------|---------|--------|
| **Critical** | Missing frontmatter, wrong category, or not indexed | 🔴 Fix immediately — document is invisible to the system |
| **High** | Missing required tags, no related docs/issues linked | 🟠 Fix before marking `final` |
| **Medium** | Structure doesn't match template, summary too vague | 🟡 Fix when convenient |
| **Low** | Minor formatting, wording improvements | 🟢 Author decides |

### Pre-Commit Checklist

Before committing any document, verify:

- [ ] **Frontmatter complete** — title, date, author, documentarian, category, tags, status all present
- [ ] **Tags valid** — all tags exist in the taxonomy (no invented tags)
- [ ] **Correct folder** — document is in the folder matching its category
- [ ] **Filename follows convention** — lowercase, hyphen-separated, descriptive, no dates or agent names
- [ ] **Structure matches template** — uses the correct template for its category
- [ ] **INDEX.md updated** — entry added under correct category
- [ ] **TAGS.md updated** — entry added under each tag
- [ ] **RECENT.md updated** — entry added at top, oldest removed if >20
- [ ] **Related docs linked** — `related_docs` in frontmatter populated if relevant
- [ ] **Related issues linked** — `related_issues` in frontmatter populated if relevant

---

## Event Documentation Workflow

When a significant event produces knowledge, this is the pipeline:

### What Triggers Documentation

| Event | Trigger Source | Document Type |
|-------|---------------|---------------|
| ICM incident investigated | Aragorn completes investigation | Investigation report |
| Research completed | Elrond delivers findings | Research report |
| Architecture decision made | Gandalf decides | Decision record |
| Audit completed | Any agent completes systematic review | Audit report |
| Tool built or upgraded | Gimli ships, or capability analysis done | Tool documentation |
| Product feedback captured | Gandalf triages external feedback | Feedback record |

### The Documentation Pipeline

**Stage 1 — Receive Content**
1. Another agent produces raw content (investigation report, research findings, etc.)
2. Content arrives as either: a document in `docs/` (unstructured), a decision inbox item, or raw output from a spawn

**Stage 2 — Classify & Structure**
1. Determine the **category** — which folder does this belong in?
2. Generate a **descriptive filename** following naming conventions
3. Apply the **frontmatter template** with all required metadata
4. Ensure the document follows the **structure template** for its category
5. Cross-reference: identify **related docs** and **related issues**

**Stage 3 — Tag**
1. Apply **type tag** (matches category)
2. Apply **domain tags** (what is this about?)
3. Apply **agent tag** (who produced the content?)
4. Set **status tag** (`draft` if review needed, `final` if complete)
5. Verify: does this document carry at least 4 tags (type + domain + agent + status)?

**Stage 4 — Index**
1. Add entry to `docs/INDEX.md` under the correct category
2. Add entry to `docs/TAGS.md` under each tag the document carries
3. Add entry to `docs/RECENT.md` at the top (remove oldest if >20)
4. Update the "Last updated" date and document count in each index

**Stage 5 — Commit**
1. Stage the document and all three index files
2. Commit with message: `docs({category}): {brief description}`
3. Example: `docs(investigation): add ICM 766712513 ARM error rates report`

---

## Handling Legacy Documents

The repo has existing documents that predate this system. When encountering them:

1. **Do not move files without explicit approval** — other agents may reference current paths
2. **Add frontmatter** to existing docs without restructuring content
3. **Create index entries** for existing docs in their current locations
4. **Propose migration** via `.squad/decisions/inbox/bilbo-docs-migration.md` for Gandalf's approval
5. **Track migration status** — note which docs have been brought into the system

---

## Evolution Log

### Purpose
This log tracks changes to the Knowledge Management system design over time. It helps answer: "Why is the system like this now? What changed and when?"

### When to Update
- New category added
- Tag taxonomy expanded or restructured
- Index format changed
- Template modified
- Major organizational pattern discovered

### Format

| Date | Change | Rationale | Confidence |
|------|--------|-----------|------------|
| 2026-03-23 | Hierarchical subfolder grouping for related documents | When 2+ documents share a topic and form a conceptual cluster, create a subfolder to group them. Preserves git history by using `git mv` instead of delete/recreate. Update `related_docs` cross-references when paths change. Applied successfully to `research/worktrees/` (3 related worktree research documents). Improves findability and signals conceptual relationships. | High |
| 2026-03-22 | Initial system design | Separation of charter (identity) from skill (design) allows evolution without freezing architecture. See decision: gandalf-bilbo-charter-skill-separation | Low |

### Future Changes to Track
- Merging categories if they blur
- Retiring tags that aren't used
- Adding new status transitions (e.g., `reviewing` between draft/final)
- Changes to index frequency or refresh strategy

---

## Notes for Future Iterations

**Low Confidence Areas** (may need revision based on real use):
1. **Category boundaries** — Do 8 categories work? Will they blur over time? How often will reorganization be needed?
2. **Tag taxonomy** — Are the domain tags comprehensive? Will we need to invent new ones? Should we use a hierarchical tag system instead of flat?
3. **Index maintenance** — Manually updating three indexes is prone to drift. Consider automation (scripts to regenerate from source)?
4. **Template rigidity** — Are the document structure templates flexible enough for edge cases?

**Feedback Welcome**
- From Bilbo: What's working? What's friction?
- From other agents: Is findability good? Are docs organized intuitively?
- From Gandalf: Should categories or tags change based on how the squad evolves?

This skill is living. Propose improvements via `.squad/decisions/inbox/`.
