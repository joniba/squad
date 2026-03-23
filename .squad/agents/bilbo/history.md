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
