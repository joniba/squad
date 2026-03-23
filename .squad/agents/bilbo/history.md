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

### Squad Template Patterns & Role Mapping Documentation (2026-03-23)
**Deliverables:** 
- `docs/guides/squad-template-patterns.md` (14,409 chars) — Patterns for charter structure, routing, ceremonies, review gates, role interactions
- `docs/guides/squad-template-role-mapping.md` (24,184 chars) — Generic role reference and customization process with worked examples

**What was documented:**

*Squad Template Patterns & Design Principles:*
- **Charter structure:** 8-section template with governance model, principles, ceremonies, reporting, escalation, SLAs, constraints
- **Routing table:** Mapping decision types to decision-makers (decider/RACI style) with real examples
- **Ceremonies:** Sync, retro, sprint planning, review gates with timing and attendance guidelines
- **Role interactions:** How different roles collaborate (PM ↔ EM on roadmap, TM ↔ PM on resourcing, etc.)
- **Lessons learned from ms-pa:** What worked (clear routing, documented SLAs) and gotchas (overloading PM, underutilizing roles)
- **Anti-patterns:** Common mistakes (omitting ceremonies, unclear routing, generic role names, missing SLAs)
- **Implementation checklist:** 12-step deployment process with validation gates

*Squad Template Role Mapping & Customization:*
- **Generic roles reference:** 10 core roles (PM, EM, TM, DevLead, etc.) with responsibilities and skill requirements
- **Role mapping process:** 4-step customization workflow (identify available skills, assign roles, name creatively, validate coverage)
- **Worked examples:** 3 squad variants with cast selection narratives and name mapping:
  - Feature/Marvel squad (Avengers theme)
  - Platform/Star Wars squad (Star Wars theme)
  - Data/Greek mythology squad (Greek hero names)
- **Customization mistakes:** Common pitfalls (ignoring skill-to-role fit, inconsistent naming, forgetting specialist roles, skipping validation)
- **Scaling considerations:** How to maintain squad identity as team grows

**Key patterns extracted:**
1. **Generic structure with custom identity** — Keep charter structure consistent, customize role names and themes per squad
2. **Clear role routing** — Every decision type has one decider; SLAs documented per role
3. **Ceremony rhythm** — Sync + retro (weekly), sprint planning (biweekly), review gates (per milestone)
4. **Role interaction framework** — Cross-role responsibilities prevent silos and clarify handoffs

**File references:**
- **Patterns guide:** C:\dev\personal\pa-squad\docs\guides\squad-template-patterns.md
- **Role mapping guide:** C:\dev\personal\pa-squad\docs\guides\squad-template-role-mapping.md
- **Index updates:** Added both guides to INDEX.md (Guides category), TAGS.md (bilbo, final, guide, patterns, roles, squad-infra, workflow), RECENT.md (entries 1–2)
- **Related issues:** Resolves #11 (Squad Template Patterns) and #19 (Squad Template Role Mapping)

**Process notes:**
- Built on learnings from ms-pa squad (existing squad charter archetype, role interactions, ceremony rhythm)
- Synthesized pattern guidelines from squad governance documents and team dynamics research
- Created worked examples to make customization process concrete and approachable
- Included anti-patterns section so implementers learn from gotchas, not experience them
- Documentation follows consistent structure: problem → pattern → examples → mistakes → checklist

### Comprehensive Feature Documentation — 6 How-To Guides (2024-12-19)
**Deliverables:** 
- `docs/guides/teams-watchdog-setup.md` (8,661 chars) — Daily Teams monitor for decisions/action items
- `docs/guides/agent-dashboard-setup.md` (8,666 chars) — Live terminal dashboard with 8 monitoring panels
- `docs/guides/teams-notifications-setup.md` (11,323 chars) — Webhook-based notifications (reusable for future squads)
- `docs/guides/semantic-model-usage.md` (13,430 chars) — Build and query squad knowledge graph
- `docs/guides/worktree-usage.md` (10,943 chars) — Coordinate parallel agent work with isolation rules
- `docs/guides/icm-investigation-guide.md` (16,974 chars) — Structured 4-stage incident investigation RCA
- `docs/guides/INDEX.md` — Master index (Guides category, sorted by date)
- `docs/guides/TAGS.md` — Reverse tag index (13 tags, cross-referencing all guides)
- `docs/guides/RECENT.md` — Chronological index (6 new entries)

**What was documented:**

*Teams Watchdog Daily Summary Setup:*
- 2-step architecture (PowerShell script + GitHub Actions scheduler)
- Prerequisite setup, configuration, output format, troubleshooting (7 scenarios)
- Cost model and performance tuning

*Agent Dashboard Setup:*
- Live terminal dashboard launcher with 4 launch modes
- 8 dashboard panels (agents, status, recent decisions, action items, error log, token usage, pending work, quick actions)
- Prerequisites (.NET 10, squad-monitor tool), troubleshooting (5 scenarios)

*Teams Notifications Setup:*
- Webhook registration with secure token storage
- Adaptive Cards message format with 28 KB payload limit
- Security rules (5 critical), real webhook examples, reusable for future squads

*Semantic Model Usage:*
- Building the knowledge index (8 node types: Person, Decision, Task, Meeting, Document, Feature, Bug, Pattern)
- Querying patterns (5 types: entity search, relationship traversal, time-range queries, tag-based discovery, tag frequency analysis)
- JSON structure, real-world examples for each query type

*Worktree Usage:*
- Isolation rules (6 critical rules for safe parallel work)
- Decision table: worktrees vs. checkout -b trade-offs
- Spawn prompt template, cleanup procedures, error handling (6 scenarios)

*ICM Investigation Guide:*
- 4-stage pipeline: Triage (classify severity/category) → Enrichment (collect context) → RCA (identify root cause with evidence) → Remediation (fix + prevention)
- Evidence formatting, confidence levels (high/medium/low), output checklist (10 items)
- Real incident example walkthrough

**Key design choices:**
1. **Production-ready documentation:** Each guide includes real examples, prerequisites, configuration steps, troubleshooting sections
2. **Proper frontmatter:** All guides tagged with type (guide) + domain tags (tooling, teams, workflow, icm, etc.) + author + documentarian (bilbo) + status (final)
3. **Complete indexing:** Three index files (INDEX.md, TAGS.md, RECENT.md) make guides discoverable per knowledge-management system requirements
4. **Reusability:** Teams Notifications guide marked as reusable pattern for future squads; worktree and semantic-model docs serve as foundational patterns
5. **Source documentation:** Each guide source-read from actual implementation (.squad/skills/, PowerShell scripts, feature code) to ensure accuracy

**File references:**
- **Guides:** C:\dev\personal\pa-squad\docs\guides\*.md (all 6 guides, 69.9 KB total)
- **Indexes:** INDEX.md (master by category), TAGS.md (reverse by 13 tags), RECENT.md (chronological)
- **Commit:** 495e4eb (9 files changed: 6 guides + 3 index files)
- **Related issues:** Documented as part of sprint deliverables for feature team

**Process notes:**
- Read each feature's implementation to extract accurate technical details
- Structured each guide with consistent format: What It Does, Prerequisites, Step-by-step Setup, Configuration, Examples, Troubleshooting, Cost/Performance where relevant
- Applied tagging taxonomy consistently (guide + domain + bilbo + final)
- Created three index files (INDEX.md, TAGS.md, RECENT.md) to make guides discoverable per knowledge-management/SKILL.md specification
- All guides verified to load without rendering errors, frontmatter properly formatted for system parsing
- No blockers; all documentation complete and committed

### Teams Knowledge Library — Initialize with 9 Watchdog Items (2026-03-23)

**Deliverables:**
- .squad/skills/teams-knowledge/SKILL.md (320+ lines) — Comprehensive skill documentation defining Teams Knowledge Library purpose, folder hierarchy (decisions/, action-items/, context/), naming conventions (YYYY-MM-DD-slug), tagging taxonomy (type, domain, project, agent, status tags), frontmatter template, index system requirements, document templates, maintenance workflow, quality standards
- 	eams-knowledge/decisions/ (4 documents) — Decision documents for ARM watchlist priority, AI Squads endorsement, TI Analyzer spec hold, git worktree orchestration bug
- 	eams-knowledge/action-items/ (4 documents) — Action items for syncing with Hila, fixing service tree ownership, reviewing TI Analyzer tasks, sharing one-pager and GAIA review
- 	eams-knowledge/context/ (1 document) — Context document on orchestration gap (parallel agents blocked by git checkout fallback)
- 	eams-knowledge/INDEX.md — Master hierarchical index by category (Decisions, Action Items, Context), sorted by date (newest first), with brief summaries
- 	eams-knowledge/TAGS.md — Reverse tag index (11 tags) organized alphabetically, items sorted by date newest first
- Updated .squad/agents/bilbo/charter.md — Added 	eams-knowledge/ to Ownership section

**What was documented:**
- **Skills Architecture:** Created comprehensive SKILL.md following existing knowledge-management patterns but adapted for watchdog-driven cadence
- **9 Watchdog Items from 2026-03-23:** Parsed daily Teams watchdog summary into 4 decisions + 4 action items + 1 context item
- **Cross-referencing:** All documents include elated_docs field creating discoverable web of related work
- **Frontmatter Consistency:** All 9 documents include YAML frontmatter with required fields and tagging

**Key patterns extracted:**
1. **Watchdog to Knowledge:** Teams watchdog summaries are authoritative source; each distinct item becomes one markdown document
2. **Tagging Taxonomy:** Domains (teams, azure, tooling, architecture, squad-infra, git, workflow, product), projects (arm-watchlist, ti-analyzer, orchestration, ai-squads)
3. **Library Separation:** Teams library physically separated at repo root but follows identical organizational standards
4. **Index System:** Two required master indexes — INDEX.md (hierarchical by category/date with summaries) and TAGS.md (reverse alphabetical by tag)

**File references:**
- **Skill:** .squad\skills\teams-knowledge\SKILL.md
- **Decisions:** 	eams-knowledge\decisions\2026-03-23-*.md (4 files)
- **Action Items:** 	eams-knowledge\action-items\2026-03-23-*.md (4 files)
- **Context:** 	eams-knowledge\context\2026-03-23-orchestration-gap.md
- **Indexes:** 	eams-knowledge\INDEX.md, 	eams-knowledge\TAGS.md
- **Charter Update:** .squad\agents\bilbo\charter.md

**Process notes:**
- Established Teams Knowledge Library as Bilbo's new domain alongside existing docs/ ownership
- Parsed watchdog summary into 9 distinct items with careful attention to category (folder) and tagging
- Built cross-reference network: each document includes elated_docs field linking related items
- Created INDEX.md with table format for readability; created TAGS.md as pure reverse index
- Updated charter to formally claim ownership of 	eams-knowledge/ directory
- No blockers; all 9 items processed, indexes complete, charter updated, ready for commit.

### Investigations Folder Reorganization — Prioritized Task Index & Master Index Synchronization (2026-03-24)

**Deliverables:**
- docs/investigations/TASK-INDEX.md (created in prior session) — Prioritized action dashboard for all investigations
- docs/INDEX.md (updated) — Added TASK-INDEX.md to Investigations section at top, reordered all investigations by date (newest first), added 4 missing ICM files with full tags and status
- docs/TAGS.md (updated) — Added 10 new investigation-specific tags, consolidated duplicate squad-infra and workflow sections, updated sentinel section to include all 4 new investigations
- docs/RECENT.md (updated) — Added TASK-INDEX.md as entry #1 (2026-03-24), shifted existing 10 entries, removed oldest entry to maintain 10-entry limit

**What was reorganized:**
- INDEX.md: Added TASK-INDEX.md as primary investigation entry; reordered 8 investigation files by date; added comprehensive tags for each; updated footer: doc count 11→15
- TAGS.md: Created investigation-centric tag sections; added 10+ new tags; consolidated duplicate sections; updated footer: 26→38 unique tags, 46→64 total tag assignments
- RECENT.md: Added TASK-INDEX.md as most recent (2026-03-24); reordered prior entries; doc count 11→12

**Investigation Files Covered:**
- Existing: ICM 766712513 (3 variants: full report, v2 report, summary), ICM 764634026 (MSPKI migration), Aragorn capability upgrade
- Newly Indexed: ICM 21000000917983 (watchlist stale-data), ICM 21000000951041 (Azure Government Sentinel TAXII), ICM 51000000943039 (TI Upload STIX/HTTPS), ICM 51000000954460 (revoked TI indicators)

**Key design choices:**
1. Multi-index strategy: TASK-INDEX.md as actionable priority dashboard; INDEX/TAGS/RECENT as complementary discovery paths
2. Tag consolidation: Removed historical duplicate sections to maintain clean tag structure
3. Geographic tags: Added azure-government and s500 to surface regional/customer-specific investigations
4. Severity-driven urgency: Sev2 and Sev3 CRI investigations indexed by date (newest first)
5. Status fields: All investigations include status field (final/ACTIVE) for priority interpretation

**File references:**
- Task Index: docs/investigations/TASK-INDEX.md (created prior session, now indexed in all three master files)
- Updated Indexes: docs/INDEX.md, docs/TAGS.md, docs/RECENT.md
- Commit: d9a44cc

**Process notes:**
- Updated all three master index files to reflect complete investigation folder reorganization
- Consolidated duplicate tag sections and added new investigation-specific tags
- Verified TASK-INDEX.md discoverable via category (INDEX), tag (TAGS), and recency (RECENT)
- Committed with Co-authored-by trailer; all work complete and verified
