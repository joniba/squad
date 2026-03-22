# Decision: Bilbo Knowledge Architecture Upgrade

**Author:** Gandalf (Lead)  
**Date:** 2026-03-23  
**Status:** Accepted  
**Priority:** HIGH (Jonathan explicitly requested thorough charter before Bilbo starts work)

## Context

Jonathan wants Bilbo to evolve from a generic documentarian into a **knowledge architect**. The current `docs/` folder is a flat collection of 6 files with no organizational system — no frontmatter, no tags, no indexes, no naming conventions. Documents are hard to find, impossible to cross-reference, and disconnected from the issues and agents that produced them.

Existing documents span at least 5 distinct types:
- ICM investigation reports (2): `icm-766712513-investigation.md`, `icm-investigations/`
- Research reports (2): `worktree-article-analysis.md`, `worktree-parallelism-research.md`
- Capability analyses (1): `aragorn-icm-capability-upgrade.md`
- Reference catalogs (1): `squad-skills-catalog.md`
- Plus audit reports and decision records exist in concept but not yet as docs

Without structure, this will only get worse as the squad produces more knowledge artifacts.

## Decision

**Upgrade Bilbo's charter to embed a complete knowledge architecture system.** The charter now defines:

### 1. Folder Hierarchy (8 categories)

```
docs/
├── INDEX.md / TAGS.md / RECENT.md    # Three index files
├── investigations/                    # ICM & livesite reports
├── research/                          # Research & deep dives
├── decisions/                         # Architecture decisions (ADR-style)
├── audits/                            # Systematic assessments
├── guides/                            # How-tos & runbooks
├── catalogs/                          # Reference inventories
├── feedback/                          # Product feedback & RFCs
└── tools/                             # Tool documentation
```

### 2. Tagging Taxonomy (4 dimensions)

Every document gets tags in YAML frontmatter across 4 required dimensions:
- **Type** (1 required): matches the folder category
- **Domain** (1+ required): 12 domain tags covering `icm`, `livesite`, `architecture`, `squad-infra`, `tooling`, `workflow`, `security`, `research-method`, `product`, `azure`, `teams`, `git`
- **Agent** (1 required): which agent produced the content
- **Status** (1 required): `draft`, `final`, `superseded`, `archived`

New tags require a decision record — no ad-hoc tag invention.

### 3. Triple Index System

- **`docs/INDEX.md`** — Master index grouped by category, with date/author/status/summary per doc. Fixed category order, newest first within each.
- **`docs/TAGS.md`** — Reverse index by tag. For each tag, lists every document carrying it. Alphabetical tags, newest first within each.
- **`docs/RECENT.md`** — Last 20 documents by date. Rolling window. Oldest falls off at 21.

All three indexes are updated on every document add/modify/remove.

### 4. Event Documentation Pipeline

5-stage pipeline triggered when any agent produces knowledge:
1. **Receive** — content arrives from another agent
2. **Classify** — determine category, generate filename, apply frontmatter
3. **Tag** — apply type + domain + agent + status tags (minimum 4)
4. **Index** — update all three index files
5. **Commit** — standardized commit message: `docs({category}): {description}`

### 5. Document Templates

Structured templates for each document type:
- **Investigation**: Executive Summary → Timeline → Impact → Investigation (data, hypotheses, root cause) → Remediation → Classification
- **Research**: Executive Summary → Context → Methodology → Findings → Analysis → Recommendations
- **Decision**: Status → Context → Decision → Consequences → Alternatives
- **Audit**: Scope → Methodology → Findings table → Summary → Action Items

### 6. Quality Standards

Severity scale for documentation issues (Critical/High/Medium/Low) and a pre-commit checklist covering frontmatter, tags, folder placement, naming, structure, and index updates.

## Reasoning

**Why not a simpler system?** Jonathan explicitly asked for thorough design before Bilbo starts work. A simpler "just write docs" approach is what Bilbo had before — and it produced a flat, unorganized folder. The overhead of frontmatter + tags + indexes is minimal per-document but compounds into massive findability gains as the knowledge base grows.

**Why YAML frontmatter?** It's the standard for markdown-based knowledge systems (Jekyll, Hugo, VitePress, Obsidian). Machine-parseable, human-readable, and trivially extractable for index generation.

**Why three indexes instead of one?** Different access patterns: "What do we have?" (INDEX.md by category), "What relates to X?" (TAGS.md by tag), "What's new?" (RECENT.md by date). One index trying to serve all three is worse at each.

**Why embedded in the charter, not a separate spec?** Bilbo's charter IS the spec. Every time Bilbo spawns, the charter loads. If the knowledge architecture lived in a separate doc, Bilbo might not read it. The charter is the single source of truth for how Bilbo operates.

**Why not automate index generation?** We could build a script (Gimli's domain), but the system should work without tooling first. Manual index maintenance forces Bilbo to understand the system. Automation can come later as an optimization.

## Consequences

### Positive
- Every document is findable via three different paths (category, tag, recency)
- Cross-referencing via tags connects related knowledge across categories
- New agents can discover existing knowledge without tribal knowledge
- Status tracking prevents stale docs from being treated as current
- Templates ensure consistency across all document types
- Legacy docs can be migrated incrementally

### Negative
- Every document creation requires updating 3 index files (overhead)
- New tag additions require a decision record (friction by design)
- Existing 6 docs need migration to the new system (one-time cost)
- Frontmatter requirements mean Bilbo must post-process raw content from other agents

## Alternatives Considered

1. **Flat docs/ with just a README** — Rejected. This is what we have now. It doesn't scale.
2. **Wiki-style system (VitePress, Docusaurus)** — Rejected for now. Over-engineered for a team of agents. Can layer on later.
3. **Database-backed index (SQLite, JSON)** — Rejected. Markdown indexes are human-readable, git-diffable, and require no tooling.
4. **Tags in filenames** — Rejected. Makes filenames unreadable. Frontmatter is the right place.
5. **Single unified index** — Rejected. Trying to serve browse-by-category, find-by-tag, and see-recent in one file makes all three worse.

## Next Steps

1. ✅ Charter written and committed
2. Bilbo migrates existing 6 docs into the new folder structure (new issue)
3. Bilbo creates the three index files from scratch (same issue)
4. Bilbo documents this architecture decision as a `decisions/` doc (meta!)
