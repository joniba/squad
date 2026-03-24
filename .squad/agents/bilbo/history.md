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

## 2026-03-24T23:59:00Z — Notifications Runbook & Architecture Overview (#120)
- **Status:** ✅ COMPLETED — 2 guides (runbook + overview), 6 closed issues total
- **Deliverables:**
  1. `notifications-runbook.md` — step-by-step operator procedures, Teams card validation, troubleshooting
  2. `notifications-overview.md` — architecture diagram, MVP caller patterns, integration points
- **Key Sections:**
  - Architecture: notify.ps1 foundation → notify-feature-complete.ps1 + notify-blocked.ps1 → dispatcher → Teams
  - Operations: Dry-run validation with -WhatIf, manual card formatting tests, failure recovery patterns
  - Integration: Coordinator wiring via notify-squad-event.ps1, Teams routing tables, expandability patterns
- **Cross-Agent Notes:** Built documentation after Gimli's coordinator wiring (#134); provides immediate reference for Teams notification operators
- **Prior session context:** Bilbo previously completed squad skills catalog, documentation index, and template guides; this closes Notifications epic

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

### User-Facing Feature Documentation — Three Shipped Feature Guides (2026-03-24)

**Deliverables:**
- `docs/guides/unified-scheduler-guide.md` (9,383 chars) — User guide for unified task scheduler covering quick start (dry run, run once, daemon modes), CLI flags reference, scheduler configuration, task management, and troubleshooting
- `docs/guides/email-watchdog-setup.md` (10,228 chars) — Complete setup guide for email watchdog (Outlook scanner) with prerequisites, quick start, CLI parameters, scheduler integration, Teams delivery, WorkIQ integration cost analysis, and troubleshooting
- `docs/guides/teams-knowledge-library-guide.md` (11,264 chars) — User guide to Teams Knowledge Library with library structure, browsing patterns using INDEX.md/TAGS.md, document creation workflow, document status tracking, tag taxonomy reference, and cross-referencing
- `docs/INDEX.md` (updated) — Added 3 new guide entries under Guides section (scheduler, email-watchdog, teams-knowledge-library); Guides section now has 5 total. Updated metadata: Last Updated → 2026-03-24, Total Documents → 18
- `docs/TAGS.md` (updated) — Added entries for all three guides under 6 tags (guide, bilbo, final, squad-infra, teams, tooling, workflow). Updated metadata: Last Updated → 2026-03-24, Total Tag Assignments → 73
- `docs/RECENT.md` (updated) — Added 3 new entries (all dated 2026-03-24) at top of list, removed oldest entry (#10 worktree-lifecycle) to maintain 10-item limit

**What was documented:**

*Unified Scheduler Guide:*
- Quick Start: Dry run mode, run-once mode, daemon mode with restart behavior
- CLI Flags: Complete reference table for all --flags (dry-run, once, help, config-path, etc.)
- Scheduler Configuration: .squad/scheduler.json structure and task definitions
- On-Call Setup: Integration with squad ceremonies for coverage rotation
- Troubleshooting: 6 scenarios (tasks not running, daemon crashes, wrong output, performance, logs, recovery)

*Email Watchdog Setup Guide:*
- Prerequisites: Outlook OAuth setup, Teams webhook registration, Kusto access prerequisites
- Quick Start: Single-command setup for first run with expected output
- CLI Parameters: Complete reference for all parameters (outlook-folders, teams-webhook, categories, etc.)
- Scheduler Integration: How to configure watchdog in .squad/scheduler.json for recurring runs
- Output Categories: Examples of Decisions, Action Items, Updates, Risks parsed from email
- WorkIQ Integration: Usage patterns, cost analysis (token budgeting), query patterns
- Troubleshooting: 7 scenarios (auth failures, empty results, Teams delivery issues, WorkIQ errors, etc.)

*Teams Knowledge Library Guide:*

## 2026-03-24T02:36:28Z — DGrep CLI Documentation Delivery (Bilbo)
**Task:** #133 + #109 — DGrep CLI documentation (README + Quickstart + Knowledge Library)  
**Status:** ✅ COMPLETED

**Deliverables:**
- `docs/dgrep-cli-guide.md` — Primary README (336 lines, 9.2 KB)
  - Overview, Installation, Core Concepts (Entity, Query, SDK)
  - CLI Quickstart examples
  - Configuration and Troubleshooting  
- `docs/dgrep-cli-quickstart.md` — Quick reference (107 lines, 3.1 KB)
  - Single-command examples for first-time users
  - Parameter cheat sheet for Diagnostics, LogEntry, Table schema
  - Common queries (logs, diagnostics, aggregations)
- `teams-knowledge/dgrep-endpoints.md` — POC validated test endpoint
  - Diagnostics PROD, AugustaPrdEus2 region verified
  - SDK authentication confirmed with `az cli`
  - 322 tests pass — foundation reliable

**Key Patterns:**
1. Documentation follows "discovery + reference" model: guide for learning, quickstart for copy-paste
2. Examples grounded in prod-ready endpoint (not mock data)
3. Troubleshooting section covers 3 high-impact scenarios: auth, query timeouts, schema mismatches
4. Clear prerequisites and validation step (322-test POC run)

**Issues Resolved:** #109, #133  
**Commit Context:** POC validation complete; documentation released with tested endpoint reference

**Next:** Merge to main, link from squad wiki for agent discovery

### DGrep Documentation Gaps — KQL Cheat Sheet, Troubleshooting, Sample Queries (2026-07-18)
**Deliverables:**
- `docs/guides/dgrep-kql-cheatsheet.md` (9.6 KB) — KQL patterns for ICM/Geneva investigation
- `docs/guides/dgrep-troubleshooting.md` (11.6 KB) — Common errors and fixes
- `docs/guides/dgrep-sample-queries.md` (9.9 KB) — Ready-to-use query examples with ICM workflow

**What was documented:**

*KQL Cheat Sheet:*
- Error severity levels, time range patterns, identity column filtering
- Aggregation patterns with the `summarize` partial-results pitfall documented
- Full string operations reference (contains, startswith, regex, comparisons)
- Complete DGrep-vs-Kusto pitfalls table (no `ago()`, no `let`, no `has`, no `join kind=leftouter`, spelling differences)
- Supported/unsupported operators and aggregations exhaustively listed
- Copy-paste patterns for ICM error investigation, correlation ID tracing, regex stack traces

*Troubleshooting:*
- Auth failures: az login, dSTS, certificate auth, MFA issues
- Network/endpoint issues: wrong MDS endpoint, proxy/firewall
- Rate limiting: 5 concurrent queries, orphaned query cleanup, query timeout
- Query syntax errors: KQL vs MQL confusion, top 5 common KQL mistakes
- "No results" systematic checklist: wrong namespace, time range, identity scoping, permissions
- Config file issues: location, JSON syntax, saved queries

*Sample Queries:*
- Basic search, scoped search with identity columns
- Saved query creation, parameterization, and execution
- Piping to jq, Select-String, CSV/Excel export
- Full real-world ICM investigation workflow (7-step scenario from alert to evidence export)
- Additional patterns: user lookup, slow requests, regex exceptions, cross-tenant comparison

**Source reference:** All content grounded in `docs/research/geneva-dgrep-research.md` sections 3-5 (KQL subset, auth, rate limits).

**Issues Resolved:** #119
**Branch:** squad/119-dgrep-docs-gaps
- Library Purpose: Feed-driven repository for decisions, action items, context, thematic insights
- Structure: decisions/, action-items/, context/, themes/ folders with naming conventions (YYYY-MM-DD-slug)
- Browsing Patterns: Using INDEX.md for category discovery, TAGS.md for tag-based lookup, RECENT.md for timeline view
- Document Creation: Manual document creation workflow with template frontmatter
- Document Status Tracking: How status field (active/archived/deprecated) controls discoverability
- Tag Taxonomy: Domain tags (teams, azure, tooling, architecture, etc.), project tags, type tags, status tags
- Cross-Referencing: How related_docs field creates discoverable connections

**Key design choices:**
1. **Consistent format across all three guides:** Quick start section for immediate usefulness, CLI/parameter reference for completeness, practical examples from actual workflows, troubleshooting sections addressing common issues, related documentation cross-references
2. **Production-ready documentation:** Each guide source-read from actual implementation (scheduler.json, .squad/skills/, email-watchdog scripts) to ensure technical accuracy
3. **Proper frontmatter & tagging:** All guides include YAML frontmatter with author (gimli for scheduler/watchdog features, bilbo for knowledge library), documentarian (bilbo), category (guide), tags (guide + domain-specific tags), status (final)
4. **Complete indexing:** Updated all three index files (INDEX.md, TAGS.md, RECENT.md) per knowledge-management/SKILL.md specification; guides now discoverable via category browsing, tag searching, and recent activity feed
5. **User-centric structure:** Each guide answers "What does this do?" (purpose), "How do I set it up?" (prerequisites + quick start), "What are all the options?" (CLI reference), "How do I troubleshoot?" (common issues + solutions)

**File references:**
- **Guides:** C:\dev\personal\pa-squad\docs\guides\unified-scheduler-guide.md, email-watchdog-setup.md, teams-knowledge-library-guide.md (30,875 chars total)
- **Indexes:** docs/INDEX.md (Guides section + metadata), docs/TAGS.md (6 updated tags + metadata), docs/RECENT.md (top 3 entries, 10-item limit maintained)
- **Commit:** cb92384 (6 files changed: 3 guides created, 3 indexes updated)

**Process notes:**
- Read source implementation files (scheduler.ps1, scheduler.json, email-watchdog production/POC scripts, teams-knowledge SKILL.md) to extract accurate technical details
- Structured each guide with consistent pattern: Purpose → Quick Start → Parameter Reference → Configuration → Examples → Troubleshooting → Related Docs
- Applied tagging taxonomy consistently: type:guide + domain tags (squad-infra, teams, tooling, workflow) + agent tags (bilbo) + status (final)
- Updated all three index files to maintain discovery surfaces: INDEX.md (category browsing), TAGS.md (tag search), RECENT.md (timeline view)
- Verified all frontmatter properly formatted for system parsing; all guides include related_docs cross-references for discoverability
- No blockers; all documentation complete, indexed, and committed to main branch

### Tools & Integrations Catalog (2026-03-23)
**Deliverable:** `docs/catalogs/tools-and-integrations.md` — Comprehensive inventory of every tool, integration, API, database, and reference source used by the pa-squad project.

**What was cataloged:**
- **8 MCP servers** with specific tool functions: IcM (19 tools), Geneva (5), EngHub (4), Azure (6), WorkIQ (2), ADO (5), GitHub (6), ConfigGen (available)
- **3 databases:** IcM Kusto cluster, Copilot session store (SQLite), semantic model (JSON)
- **4 direct APIs:** IcM REST, Kusto REST, Teams webhook, GitHub (via `gh`)
- **6 CLI tools:** `copilot`, `git`, `gh`, `az`, `py`, `squad-monitor`
- **5+ reference codebases:** Personal AI Companion, TI Pipeline repos (28), squad-skills, squad-monitor, squad-personal-demo
- **5 scheduled tasks:** teams-watchdog, daily-summary, semantic-refresh, email-watchdog, icm-scan (disabled)
- **2 notification channels:** Teams webhooks (Adaptive Cards), GitHub Issues
- **Authentication matrix:** 9 services with auth methods documented

**Research sources consulted:**
- `.squad/scheduler.json` — scheduled task definitions
- `.squad/agents/*/charter.md` — 7 agent charters for tool usage
- `.squad/skills/*/SKILL.md` — 12 skill definitions for integrations
- `scripts/*.ps1` — 10 scripts + 10 skill scripts for CLI/API usage
- `docs/research/*.md` — research docs for tool discoveries and auth patterns
- `docs/catalogs/reference-codebases.md` — reference repo inventory

**Process notes:**
- Ran 4 parallel explore agents to simultaneously research agents, skills, scripts, and docs
- Cross-referenced all sources to avoid duplicates and ensure complete coverage
- Organized into 8 categories with quick-reference tables per agent
- Documented as a living document with last-updated date for ongoing maintenance

## 2026-03-24T01:01:43Z — Notifications Documentation Completion (#120)
- **Task:** Notifications MVP documentation
- **Status:** ✅ COMPLETED
- **Deliverables:**
  1. Runbook: Comprehensive guide for Notifications system operation and maintenance
  2. Overview: High-level architecture and usage patterns
- **Issue Closed:** #120
- **Milestone:** All 4 MVP issues (#132-135) closed and E2E validated
- **Integration:** Documentation prepared for production release

### 2026-03-24 — Protocol Recovery Review Cycle (Bilbo Role)

**Context:** Retroactive review cycle for 8 branches. Bilbo authored/maintained documentation (#120). Full Cycle 1 review completed, fixes applied, Cycle 2 re-approved all.

**Key Validation:**
- **Documentation-Test Sync Validation:** Bilbo's Notifications documentation (#120) was produced based on outputs from Gimli's implementation track (#105, #106, #108, #134)
- Verified that documentation accurately reflects MVP deliverables: notify-feature-complete.ps1, notify-blocked.ps1, integration into coordinator, Teams card formatting
- Consistency check: Runbook procedures match actual scripts, setup steps validated against working implementation
- Pattern: Documentation must be validated against executable samples during review, not after

**Learning for Future Implementation:**
- [HIGH] Documentation-executable sync must be verified during review cycle, not treated as post-implementation polish
- [HIGH] When docs are generated from code outputs (like Gimli's MVP scripts), documentation reviewer must cross-reference actual production code
- [MED] Documentation completeness can mask implementation gaps — flag cases where docs describe features not yet delivered to production
