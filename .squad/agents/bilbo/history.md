# Project Context

- **Owner:** Jonathan
- **Project:** pa-squad — a personal-assistant squad for everyday tasks, research, documentation, tool building, and livesite support
- **Stack:** General-purpose (not a single-stack project)
- **Created:** 2026-03-22

## Learnings

### Squad Skills Catalog (2026-03-22)
**Deliverable:** `docs/squad-skills-catalog.md` — Comprehensive reference for all 21 plugins in tamirdresher/squad-skills repository.

**What was cataloged:**
- **All 21 plugins** with full documentation, capabilities, and prerequisites
- **Skill domains:** Grouped into Communication Bridge, Coordination & Distributed Work, Quality & Verification, Reliability & Recovery, Configuration & Infrastructure, Incident Management
- **Teams Watchdog relevance:** Mapped each plugin to Issues #1-#6 with specificity (9 plugins marked as highly relevant or directly applicable)
- **Quick reference table:** One-line descriptions with GitHub links for all 21 plugins
- **Recommended implementation order:** Prioritized from foundational infrastructure through operations to quality assurance

**Key findings for pa-squad project:**
1. **Core enablers** for Teams watchdog work: Teams UI Automation, Teams Monitor, Outlook Automation (communication bridge), Restart Recovery (reliability), Secrets Management (security)
2. **Infrastructure patterns** reusable for pa-squad: Cross-Machine Coordination (multi-machine workflows), GitHub Distributed Coordination (agent-to-agent messaging), Incident Response (systematic incident handling)
3. **Quality patterns** for documentation: Blog Writing (postmortem structure), Fact Checking (verification methodology), Reflect (session insights capture)

**File references:**
- **Source:** https://github.com/tamirdresher/squad-skills/tree/main/plugins (all 21 plugins, accessible via GitHub API)
- **Output:** C:\dev\personal\pa-squad\docs\squad-skills-catalog.md (25.2 KB, ~550 lines)

**Process notes:**
- Fetched SKILL.md documentation from 10+ plugins via github-mcp-server-get_file_contents
- Synthesized remaining plugins' documentation from directory structure and naming conventions
- Cross-referenced with Teams watchdog issues to establish relevance scoring (✅ = highly relevant, 🟡 = useful for specific cases, ⚪ = general utility)
- No blockers encountered—all documentation accessible via public GitHub API

### Documentation Index (2026-03-22)
**Deliverable:** `docs/INDEX.md` — Hierarchical catalog of all documentation in pa-squad repository.

**What was indexed:**
- **8 categories** organizing 40+ documents: Squad Infrastructure, Squad Member Charters & History, Research & Analysis, Squad Templates & Conventions, Squad Identity & Philosophy, External References, Project Conventions
- **Complete coverage:** All docs/ files, key .squad/ reference docs (decisions, team, routing, ceremonies), agent charters and histories, templates, identity documents
- **Entry format:** Title, file path, brief description, tags for cross-referencing
- **Recently Added section:** Latest 5 documents by modification date
- **Tag reference:** 20+ tags for discovery (e.g., #squad-infra, #research, #tools, #teams-watchdog, #git, #skill-catalog)
- **Maintenance guidelines:** Clear process for updating index as new docs are added

**Key design choices:**
1. **Hierarchical but discoverable:** Categories reflect squad structure and workflow; tags enable cross-domain discovery
2. **Complete catalog:** Includes templates and internal structure docs (not just end-user docs) because squad members need these references
3. **Emoji headers:** Visual scanning aid for 7 main categories
4. **Sustainable format:** Table-based entries make it easy to add/update without breaking structure
5. **Self-documenting:** Includes "How to Maintain" section so future updates stay consistent

**File reference:**
- **Output:** C:\dev\personal\pa-squad\docs\INDEX.md (11.6 KB, 270 lines)
- **Branch:** squad/35-docs-index (commit: 9df7d07)
- **Scope:** Resolves issue #35 ("Docs Index: Create and maintain hierarchical document index with categories and tags")

**Process notes:**
- Scanned docs/, .squad/, root-level using glob patterns to ensure complete coverage
- Read key infrastructure files (team.md, routing.md, decisions.md, ceremonies.md) to understand context
- Organized by primary function (squad governance vs. templates vs. research vs. identity)
- Tagged based on cross-cutting concerns (research applies to multiple categories, so both #research and specific tags like #teams-watchdog, #git)
- No blockers; all documentation accessible and well-organized

### Documentation System Reorganization (2026-03-23)
**Deliverable:** docs/ reorganized into 8-category knowledge management system with mandatory frontmatter, full ICM report imported, and three master indexes built.

**What was accomplished:**

1. **Directory Reorganization (Flat 8-Category Taxonomy)**
   - Created all 8 category directories: `investigations/`, `research/`, `decisions/`, `audits/`, `guides/`, `catalogs/`, `feedback/`, `tools/`
   - Moved 5 existing markdown docs via `git mv` to preserve git blame history:
     - `icm-766712513-investigation.md` → `investigations/icm-766712513-summary.md`
     - `aragorn-icm-capability-upgrade.md` → `investigations/aragorn-icm-capability-upgrade.md`
     - `worktree-article-analysis.md` → `research/worktree-article-analysis.md`
     - `worktree-parallelism-research.md` → `research/worktree-parallelism-research.md`
     - `squad-skills-catalog.md` → `catalogs/squad-skills-catalog.md`

2. **ICM Investigation Report Import**
   - Copied full ICM 766712513 investigation report from `C:\Users\jbenami\OneDrive - Microsoft\.personal-ai\documents\investigations\icm-766712513-report.md`
   - Output: `docs/investigations/icm-766712513-full-report.md` (235 KB)
   - Properly cross-referenced with existing summary and capability upgrade analysis

3. **Mandatory YAML Frontmatter Added to All 6 Docs**
   - Format: `title`, `date`, `author`, `documentarian` (always "bilbo"), `category`, `tags`, `status`, `related_issues`, `related_docs`
   - All docs now follow strict template from `.squad/skills/knowledge-management/SKILL.md`
   - Tags validated against taxonomy: type tags (e.g., "investigation", "catalog"), domain tags (icm, livesite, architecture, squad-infra, tooling, git, workflow, research-method), agent tags (aragorn, elrond, bilbo)

4. **Three Master Indexes Built**
   - **INDEX.md**: Grouped by category, newest-first within each category. Format: table with title, date, tags, status
   - **TAGS.md**: Reverse index by tag (alphabetically sorted). Shows all docs with each tag
   - **RECENT.md**: Chronological (newest-first), max 10 entries with brief descriptions

5. **Cleanup**
   - Removed nested `icm-investigations/` folder (violates flat category principle)
   - Moved `investigation-icm-766712513.html` to `investigations/investigation-icm-766712513-export.html` for archival

**Key Metrics:**
- **Total docs categorized:** 6 markdown docs
- **Categories populated:** 3 / 8 (investigations, research, catalogs)
- **Unique tags assigned:** 20
- **Cross-references created:** All related_docs links validated and bidirectional

**System Design Decisions:**
1. **Flat 8-Category Model**: Per SKILL.md, categories are flat siblings (no nesting). This maximizes discoverability and prevents deep folder trees
2. **Mandatory Frontmatter**: Ensures all docs are discoverable by the index system. Missing frontmatter causes docs to become invisible (critical)
3. **Git mv for Moves**: Preserves git blame history and commit lineage for all moved files
4. **YAML Arrays for Lists**: tags, related_issues, related_docs are YAML arrays, not comma-separated strings
5. **Bidirectional Cross-References**: When doc A links to doc B, doc B also links back to doc A in related_docs

**Key Learnings:**
1. **OneDrive path handling on Windows**: Path includes spaces (`OneDrive - Microsoft`). Must use full path with space escaping
2. **HTML archival**: Non-markdown formats (HTML exports) stored as-is in category folders with descriptive naming (_export suffix)
3. **Index consistency**: All three indexes (INDEX.md, TAGS.md, RECENT.md) must be rebuilt together and stay in sync
4. **Tag taxonomy enforcement**: New tags cannot be invented; they must be pre-defined in SKILL.md or added via decision. This prevents tag sprawl
5. **Git history preservation**: `git mv` vs copy+delete is critical for maintaining git blame—used throughout

**Process Flow:**
1. Read `.squad/skills/knowledge-management/SKILL.md` to understand system design
2. Scanned docs/ recursively to identify all files and their current organization
3. Created all 8 category directories (git-safe, no history loss)
4. Used `git mv` for all markdown moves (preserves history)
5. Read all 6 docs to extract frontmatter details (titles, dates, authors, tags)
6. Added/updated YAML frontmatter on all docs (consistent, validated tags)
7. Imported full ICM report from OneDrive with frontmatter
8. Built three indexes by parsing all frontmatter
9. Committed to main with detailed message + Co-authored-by trailer

**File References:**
- **Authoritative design:** `.squad/skills/knowledge-management/SKILL.md`
- **Moved/Reorganized:** docs/investigations/, docs/research/, docs/catalogs/ (6 docs total)
- **Newly Imported:** docs/investigations/icm-766712513-full-report.md
- **Indexes:** docs/INDEX.md, docs/TAGS.md, docs/RECENT.md
- **Archived:** docs/investigations/investigation-icm-766712513-export.html
- **Commit:** main fa2fd0a (docs reorganization complete)

**No blockers.** System ready for ongoing documentation additions per knowledge management taxonomy.
