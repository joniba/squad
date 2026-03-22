---
title: "Separate Bilbo's Charter from System Design"
date: 2026-03-22
author: gandalf
documentarian: bilbo
category: decision
tags:
  - decision
  - squad-infra
  - gandalf
status: final
related_docs:
  - .squad/agents/bilbo/charter.md
  - .squad/skills/knowledge-management/SKILL.md
  - docs/SYSTEM.md
---

# Decision: Separate Bilbo's Charter from System Design

## Status
**Accepted**

## Context

Bilbo's original charter was a single 536+ line document that mixed two distinct concerns:

1. **Permanent identity & principles** — WHO Bilbo is, what Bilbo does, how Bilbo thinks
   - Core principles (5 foundational values)
   - Workflow triggers and escalation rules
   - Voice and collaboration patterns
   - Delegation boundaries

2. **Organizational system design** — HOW Bilbo implements knowledge management
   - Folder hierarchy (8-category structure)
   - Tagging taxonomy (type/domain/agent/status with specific values)
   - Index system specifications (INDEX.md, TAGS.md, RECENT.md formats)
   - Document structure templates
   - Quality standards checklists
   - Naming conventions

The problem: **This mixed architecture freezes organizational decisions in the charter.** If Bilbo discovers a better categorization, tagging scheme, or index format, the charter must be rewritten — which implies changing Bilbo's permanent identity. This conflates two separate concerns and creates artificial resistance to improving the system.

## Decision

**Separate the charter from the system design:**

1. **Charter (`.squad/agents/bilbo/charter.md`)** — Identity only
   - Reduced from 536+ to ~250 lines
   - Keeps: identity table, core principles, ownership, workflow triggers, quality standards, delegation, escalation, collaboration, voice
   - Removes: all system design details
   - Adds: reference to `.squad/skills/knowledge-management/SKILL.md`
   - Frequency: Updated rarely (only when Bilbo's core purpose or principles change)

2. **Skill (`.squad/skills/knowledge-management/SKILL.md`)** — System design (new file)
   - ~400+ lines of comprehensive organizational design
   - Contains: folder hierarchy, category definitions, naming conventions, tagging taxonomy (with specific values), index specifications, document templates, quality standards, event documentation pipeline
   - Status: Marked "Low Confidence (First Design)" to signal that this is a starting point
   - Includes: Evolution Log section to track changes over time
   - Frequency: Evolves as Bilbo learns what works (referenced in decisions/inbox when Bilbo proposes improvements)

3. **Public Guide (`docs/SYSTEM.md`)** — Human-facing reference (new file)
   - ~150 lines, non-exhaustive, for humans and agents
   - Quick navigation guide, category explanations, how to find things, tagging overview
   - Links to `.squad/skills/knowledge-management/SKILL.md` for detailed spec

## Consequences

### Positive
- **Charter remains stable** — Bilbo's identity and principles don't require rewriting when the system evolves
- **System design is mutable** — Bilbo can propose improvements to organization, tagging, indexing without implying identity changes
- **Clear separation of concerns** — what Bilbo IS (charter) vs. what Bilbo DOES (skill) vs. how to USE it (public guide)
- **Reduced charter friction** — charter stays short (~250 lines) and focused; it's not a "system bible"
- **Better evolution story** — Evolution Log in SKILL.md documents how the system has changed and why

### Negative
- **Three files instead of one** — Maintenance burden slightly higher, but only when system design changes (rare) or charter principles change (also rare)
- **Coordination required** — If Bilbo's role or principles fundamentally shift, the charter AND the skill may need updates (but this is rare and intentional)
- **Skill is "low confidence"** — First iteration may need refinement based on real-world use (this is acknowledged in the SKILL.md frontmatter)

## Alternatives Considered

### 1. Keep the single charter, freeze the system design
**Rejected.** Keeping organizational design in the charter implies it's permanent identity. This creates artificial resistance to improving the system and makes the charter unwieldy (~536+ lines).

### 2. Move system design to a README in docs/
**Considered but rejected.** READMEs are for projects; system design belongs in the skill system (the `.squad/skills/` tree) so it can be versioned, evolved, and tracked alongside other agent capabilities.

### 3. Use a Git history / wiki approach instead
**Rejected.** Git history is hard to query; a wiki is external and harder to version-control. The skill system is the right place.

## Implementation

Three files created:

1. **`.squad/agents/bilbo/charter.md`** — Rewritten to focus on identity and principles only (~250 lines)
2. **`.squad/skills/knowledge-management/SKILL.md`** — New comprehensive system design file (~400+ lines)
3. **`docs/SYSTEM.md`** — New public-facing quick reference (~150 lines)

All three files linked and cross-referenced. No existing documents moved or restructured; this is purely architectural.

## Related Decisions

- TBD: How to handle legacy documents that predate this system (proposed future decision)
- TBD: Tagging standards for external agents' documents (proposed future decision)

---

**Approved by:** Gandalf  
**Decision ID:** gandalf-bilbo-charter-skill-separation  
**Date:** 2026-03-22
