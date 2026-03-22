# Squad Knowledge Base System

Welcome to the Squad knowledge base. This guide explains how it's organized so you can find what you need.

---

## Where to Find Things

Everything lives under `docs/` in one of these folders:

| Folder | What's There | Find By Searching For |
|--------|-------------|----------------------|
| **`investigations/`** | ICM incident reports and livesite analyses | Incident ID: `icm-766712513` |
| **`research/`** | Deep dives, analysis, technology evaluations | Topic: `worktree`, `architecture`, `security` |
| **`decisions/`** | Architecture choices, design decisions (ADR-style) | Decision: `state-management`, `database-choice` |
| **`audits/`** | System assessments, gap analyses, reviews | Audit type: `infra`, `security`, `process` |
| **`guides/`** | How-tos, runbooks, tutorials, step-by-step docs | Task: `setup`, `troubleshoot`, `deploy` |
| **`catalogs/`** | Inventories, capability listings, references | Catalog: `skills`, `tools`, `services` |
| **`feedback/`** | Feature requests, RFCs, user feedback | Product area: `cli`, `teams`, `messaging` |
| **`tools/`** | Documentation for specific tools and scripts | Tool name: `aragorn-icm`, `watchdog-pipeline` |

---

## Quick Navigation

**Start here to find what you need:**

1. **If you know the incident ID:** Look in `investigations/` for `icm-{id}.md`
2. **If you know the topic:** Check the [Tag Index](TAGS.md) — it cross-references all documents by subject
3. **If you want recent additions:** See [Recent Documents](RECENT.md) for the latest 20 entries
4. **If you want a complete listing:** Read the [Master Index](INDEX.md) organized by category

---

## How Documents Are Tagged

Every document carries tags that help you find related material:

- **Type tag** — what kind of document it is (`investigation`, `research`, `decision`, etc.)
- **Domain tags** — what it's about (`icm`, `architecture`, `tooling`, `security`, etc.)
- **Agent tag** — who created it (`aragorn`, `elrond`, `gandalf`, `gimli`, `bilbo`, `galadriel`)
- **Status** — where it stands (`draft`, `final`, `superseded`, `archived`)

Use the [Tag Index](TAGS.md) to find all documents tagged with a specific domain or agent.

---

## Anatomy of a Document

Every document has:

1. **YAML frontmatter** (at the top, between `---` lines)
   - Title, date, author, category, tags, status
   - Links to related documents and GitHub issues

2. **Human-readable content** (structured template for the document type)
   - Investigations have timelines and impact assessments
   - Research reports have methodology and findings
   - Decisions explain the context, choice, and consequences
   - Guides have step-by-step instructions

---

## Document Status

| Status | Meaning | Should I use it? |
|--------|---------|-----------------|
| `draft` | Work in progress, under review | Yes, but note that it may change |
| `final` | Complete and reviewed | Yes, this is stable |
| `superseded` | Replaced by a newer document | No — check the link to the replacement |
| `archived` | Historical record, no longer current | For reference only — check a newer doc |

---

## The Index Files

Three special index files help you navigate:

1. **[`INDEX.md`](INDEX.md)** — Master index, all documents grouped by category
   - Best for browsing by topic area
   - Updated whenever a new document is added

2. **[`TAGS.md`](TAGS.md)** — Reverse index, all documents grouped by tag
   - Best for finding documents about a specific subject
   - Links documents across categories

3. **[`RECENT.md`](RECENT.md)** — Chronological view of the latest 20 additions
   - Best for staying current with recent knowledge

---

## How to Contribute

If you have new knowledge to add:

1. **Determine the category** — does your content fit in investigations, research, decisions, audits, guides, catalogs, feedback, or tools?
2. **Use the right template** — each category has a structure template (see below)
3. **Add frontmatter** — include title, date, author, tags, and status
4. **Tag thoroughly** — at least one domain tag so it's discoverable
5. **Link related docs** — in the `related_docs` field, link to documents your content relates to
6. **Submit or commit** — follow the squad's process for new contributions

**Questions?** Ask Bilbo (the librarian) about the system or see `.squad/skills/knowledge-management/SKILL.md` for the complete system design.

---

## Quick Templates

### Investigation Report
```
# ICM {id} — {title}
## Executive Summary
## Timeline
## Impact Assessment
## Investigation
## Remediation
## Classification
```

### Research Report
```
# {Title}
## Executive Summary
## Context & Motivation
## Methodology
## Findings
## Analysis
## Recommendations
```

### Decision Record
```
# Decision: {Title}
## Status
## Context
## Decision
## Consequences
## Alternatives Considered
```

---

## Search Tips

- **By incident:** Search for `icm-{id}` in `investigations/`
- **By topic:** Use the [Tag Index](TAGS.md) and search for domain tags
- **By agent:** Use the [Tag Index](TAGS.md) and search for agent names
- **By recency:** Check [Recent Documents](RECENT.md)
- **Across documents:** Use GitHub's code search or grep for keywords

---

## Common Questions

**Q: I can't find a document I think exists.**  
A: Check the [Tag Index](TAGS.md) or [Master Index](INDEX.md). If it's not there, it may not have been added to the system yet. Ask Bilbo.

**Q: Can I update an existing document?**  
A: Yes. Update the `date` field to today and note the change in the document. If it's a major revision, consider marking the old version as `superseded`.

**Q: My document has a lot of tags. Is that OK?**  
A: Yes. More tags = better discoverability. Aim for 5–8 tags total (type, domain, agent, status, plus 1–4 domain tags).

**Q: Should I use a tag that doesn't exist yet?**  
A: No. Proposed new tags go to `.squad/decisions/inbox/` for Gandalf's approval. Use existing tags for now.

---

Last updated: 2026-03-22  
System design: `.squad/skills/knowledge-management/SKILL.md`
