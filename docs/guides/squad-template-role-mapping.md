---
title: "Squad Template Role Mapping & Customization Guide"
date: 2026-03-23
author: bilbo
documentarian: bilbo
category: guides
tags: [guide, squad-infra, roles, workflow, bilbo, final]
status: final
---

# Squad Template Role Mapping & Customization Guide

## Overview

This guide explains how to map generic squad roles to your project's needs and cast names. It includes a reference table of roles validated in ms-pa, step-by-step guidance for choosing cast names, and worked examples showing how different customizations affect squad structure.

**Audience:** Squad leads customizing the template for their project.  
**Scope:** Role definitions, mapping strategies, cast theme selection, and customization rules.

---

## Part 1: Generic Roles Reference

### The Five Core Roles

Every squad needs these five roles. They may have different names in your project, but the responsibilities are essential:

#### 1. Lead (Decision-Maker & Facilitator)

**Responsibilities:**
- Break down projects into actionable tasks
- Route work to appropriate squad members
- Facilitate cross-role collaboration
- Make tiebreaker decisions
- Escalate policy violations or blockers
- Assess @copilot suitability (🟢🟡🔴)

**Expertise Required:**
- Project context and vision
- Understanding of each role's capabilities
- Communication across technical and non-technical stakeholders

**Interaction Pattern:**
- Async-first, but synchronous when needed for unblocking
- Responds within 24 hours typical SLA

**ms-pa Cast:** Gandalf (Lead Architect & Facilitator)

**Typical Workload:** 30-40% time spent facilitating, 20% decision-making, remaining delegated to @copilot or other roles

---

#### 2. Developer (Implementation & Code Quality)

**Responsibilities:**
- Implement features and fixes
- Write tests and ensure code quality
- Participate in design reviews
- Triage technical blockers
- Maintain code standards

**Expertise Required:**
- Deep codebase knowledge
- Testing practices and frameworks
- Performance and security considerations

**Interaction Pattern:**
- Works independently on assigned tasks
- Async code reviews (24-48h typical)
- Escalates design questions to Lead

**ms-pa Cast:** Elrond (Principal Developer & Code Keeper)

**Typical Workload:** 50-60% coding, 20% review, 10% design meetings, 10% unblocking

---

#### 3. Documentarian (Knowledge & Infrastructure)

**Responsibilities:**
- Maintain documentation structure and taxonomy
- Ensure README and decision logs are current
- Capture learnings and patterns
- Audit documentation for consistency
- Manage knowledge indexes

**Expertise Required:**
- Documentation systems and standards
- Project architecture and patterns
- Ability to extract patterns from code

**Interaction Pattern:**
- Writes async documentation
- Reviews docs alongside code reviews
- Batches feedback and updates

**ms-pa Cast:** Bilbo (Librarian & Documentarian)

**Typical Workload:** 30-40% writing, 20% review, 20% automation/auditing, remaining on meta-patterns

---

#### 4. Reviewer (Quality Gate & Standards)

**Responsibilities:**
- Review code for correctness, tests, performance
- Enforce coding standards and best practices
- Catch bugs before merging
- Approve or request changes on PRs
- Lead can override with documentation

**Expertise Required:**
- Strong coding fundamentals
- Knowledge of codebase patterns
- Attention to detail

**Interaction Pattern:**
- Responds to PR notifications
- Provides feedback within 24-48 hours
- Can be same person as Developer (small teams) or separate (larger teams)

**ms-pa Cast:** Gimli (Infrastructure Reviewer & Standards Keeper)

**Typical Workload:** 30-50% review, 20% standards documentation, remaining on tooling/automation

---

#### 5. Policy Enforcer (Governance & Automation)

**Responsibilities:**
- Enforce squad policies (commit message format, branch naming, etc.)
- Run automated checks (linting, security scans, build validation)
- Report on policy violations
- Suggest policy improvements

**Expertise Required:**
- CI/CD platforms and scripting
- Policy definition and enforcement
- Automation tools and bots

**Interaction Pattern:**
- Mostly automated (bot-driven)
- Human oversight for policy exceptions
- Lead decides on policy violations

**ms-pa Cast:** Ralph (Policy & Governance Bot)

**Typical Workload:** 80% automated, 20% manual override decisions

---

### Optional Roles (Project-Specific)

These roles are optional; add them if your project needs specialized expertise:

| Role | When to Add | ms-pa Example |
|------|------------|---------------|
| **Liaison/Community** | If squad interacts with external stakeholders | Legolas (Community & External Relations) |
| **Infrastructure** | If squad manages deployments or systems | Treebeard (Infrastructure & DevOps) |
| **Analytics/Data** | If squad generates or analyzes metrics | Lorien (Analytics & Learnings) |
| **Compliance** | If squad works under regulatory requirements | Galadriel (Compliance & Risk) |

---

## Part 2: Role Mapping Reference Table

### How to Use This Table

1. **Identify your project's focus** (feature dev, infrastructure, data platform, etc.)
2. **Look at the ms-pa mapping** for how a similar project structured roles
3. **Adapt to your context** (keep responsibilities, change names/theme)
4. **Add optional roles** only if your project truly needs specialized expertise

### Master Mapping Table

| Generic Role | ms-pa Cast | Theme | Name Rationale | Your Project |
|--------------|-----------|-------|-----------------|--------------|
| **Lead** | Gandalf | Wizard/Wise One | "Gandalf the Grey" — Gray areas, navigates complexity | `[Your Lead]` |
| **Developer** | Elrond | Elf/Maker | Elves built Rivendell — creators and makers of things | `[Your Dev]` |
| **Documentarian** | Bilbo | Burglar/Chronicler | Bilbo chronicled the quest — captures and preserves knowledge | `[Your Docs]` |
| **Reviewer** | Gimli | Dwarf/Quality | Dwarves known for craftsmanship and quality (not quantity) | `[Your Reviewer]` |
| **Policy Enforcer** | Ralph | Bot/Guardian | Ralph keeps the squad on policy rails (themed after Thorin's Erebor rules) | `[Your Policy Bot]` |

### Alternative Theme Examples

**Marvel Theme (Tech Squad)**
- Lead → Iron Man (Tony Stark)
- Developer → Wanda (Makes complex systems work)
- Documentarian → Vision (Sees the whole system)
- Reviewer → Black Widow (Precision and attention to detail)
- Policy Enforcer → JARVIS/FRIDAY (The system itself)

**Star Wars Theme (Platform Team)**
- Lead → Leia (Leadership, strategy)
- Developer → Luke (Builder of new systems)
- Documentarian → C-3PO (Protocol & knowledge)
- Reviewer → Yoda (Master of craft)
- Policy Enforcer → The Force (System of governance)

**Greek Mythology (Data Team)**
- Lead → Zeus (Authority)
- Developer → Hephaestus (Creator)
- Documentarian → Hermes (Messenger, knowledge keeper)
- Reviewer → Athena (Wisdom, attention to detail)
- Policy Enforcer → The Fates (Governance)

---

## Part 3: Step-by-Step: Choosing Your Cast Names

### Step 1: Decide on a Theme

Pick a theme that resonates with your team and project:

- **LotR (Tolkien):** Works well for engineering teams; rich character development
- **Marvel/DC:** Modern, familiar, personality-based
- **Star Wars:** Epic, approachable, good for platforms
- **Greek Mythology:** Classical, symbolic roles
- **Your Company Culture:** Inside jokes, company values, history
- **No Theme:** Just use generic names (Lead, Dev, Docs, Reviewer, Ralph)

**Recommendation:** Pick a theme that has at least 5-10 distinct characters so you can add specialized roles without breaking the theme.

### Step 2: Map Roles to Theme Characters

For each core role, pick a character that embodies the responsibility:

| Generic Role | Looking For | Theme Character | Why This Works |
|--------------|-------------|-----------------|-----------------|
| **Lead** | Wise, strategic, makes hard calls | Gandalf (LotR) / Leia (SW) / Zeus (Greek) | Has authority; others trust their judgment |
| **Developer** | Creates things, problem-solver | Elrond (LotR) / Luke (SW) / Hephaestus (Greek) | Builders; focused on making things work |
| **Documentarian** | Preserves knowledge, storyteller | Bilbo (LotR) / C-3PO (SW) / Hermes (Greek) | Keep records; pass knowledge forward |
| **Reviewer** | Detail-oriented, high standards | Gimli (LotR) / Yoda (SW) / Athena (Greek) | Craftsmanship; precision over speed |
| **Policy Enforcer** | Impartial, rule-enforcer, system-level | Ralph (bot) / JARVIS (SW) / The Fates (Greek) | Automated; enforces rules without emotion |

### Step 3: Validate Against Project Needs

Ask these questions:

- ✅ Does each character feel right for the responsibility?
- ✅ Are there at least 2-3 more characters in the theme for future optional roles?
- ✅ Will your team remember the mapping without frequent reference?
- ✅ Does the theme resonate with your project culture?

### Step 4: Create Your cast.json

```json
{
  "theme": "Lord of the Rings",
  "roles": {
    "lead": {
      "cast": "Gandalf",
      "title": "Lead Architect & Facilitator",
      "description": "Wise decision-maker; navigates complexity and conflict"
    },
    "developer": {
      "cast": "Elrond",
      "title": "Principal Developer & Code Keeper",
      "description": "Creator and keeper of the realm; builds with intention"
    },
    "documentarian": {
      "cast": "Bilbo",
      "title": "Librarian & Documentarian",
      "description": "Chronicler of quests; preserves knowledge for future generations"
    },
    "reviewer": {
      "cast": "Gimli",
      "title": "Infrastructure Reviewer & Standards Keeper",
      "description": "Master of craft; knows quality and attention to detail"
    },
    "policy": {
      "cast": "Ralph",
      "title": "Policy & Governance Bot",
      "description": "Impartial enforcer of squad rules"
    },
    "optional_future": [
      {"cast": "Legolas", "role": "Community & External Relations"},
      {"cast": "Treebeard", "role": "Infrastructure & DevOps"},
      {"cast": "Lorien", "role": "Analytics & Learnings"}
    ]
  }
}
```

### Step 5: Document in Charter Customization Section

Each role's charter includes a "Customization" section explaining how to adapt for your project:

```markdown
## Customization

For your squad:
1. Replace "Gandalf" with your chosen Lead character name
2. Update project context ({ProjectName} → your actual project)
3. Adjust response time SLAs if your timezone/async model differs
4. Add optional roles following the same charter template
```

---

## Part 4: What to Customize vs. Keep Generic

### Keep These Generic (Don't Change)

| Element | Why | Impact of Changing |
|---------|-----|-------------------|
| **8-Section Charter Template** | Proven structure; changing loses consistency | Hard to onboard new agents; inconsistent role definitions |
| **Routing Table Pattern** | Work needs a path; ambiguity causes delays | Work gets lost or routed wrong repeatedly |
| **Ceremony Triggers** | "Before multi-agent" and "After incident" are universal | Ceremonies become ad-hoc; team loses learnings |
| **Policy Enforcement** | Rules need to be checked; changing means rules break | Governance erodes over time |
| **Label Naming (`squad`, `squad:{agent}`)** | Automation depends on consistency | GitHub workflows break; routing fails |

### Customize These (Should Be Different Per Project)

| Element | Why | How to Customize |
|---------|-----|-----------------|
| **Cast Names** | Reflect your team and culture | Pick theme that resonates; map each role to a character |
| **Project Context** | Each project is unique | Replace {ProjectName} placeholders; add project-specific examples |
| **Optional Roles** | Projects have different needs | Add Infrastructure, Analytics, Compliance, Community roles as needed |
| **SLA Expectations** | Teams have different timezones and workloads | Adjust response times, async/sync balance per team |
| **Specific Tools** | Use tools your team knows | Ralph implementation, @copilot services, CI/CD platform |
| **Process Details** | Organizational practices vary | Weekly standup cadence, ceremony format (video vs. async), etc. |

### These Are Judgment Calls (Document Your Choice)

| Element | Option A | Option B | Decision Process |
|---------|----------|----------|------------------|
| **History Format** | Single file, append entries | Date-based folders, one entry per file | Weigh ease-of-search (folders) vs. discoverability (single file); document choice |
| **Ceremony Frequency** | Auto-triggered on events | Standing time (weekly, biweekly) | Async projects may auto-trigger; sync teams may prefer regular time |
| **Skill Structure** | Centralized folder | Per-role skill folders | Depends on how skills are reused and who maintains them |
| **Documentation Level** | Lightweight READMEs | Comprehensive runbooks | Risk level and team size; higher risk → more comprehensive docs |

---

## Part 5: Worked Example — Three Squad Variants

### Example 1: Feature Squad (Web Team)

**Context:** Small team building user-facing features on a web platform

**Customization:**

```json
{
  "theme": "Marvel",
  "lead": "Iron Man (Strategic, visionary)",
  "developer": "Wanda (Makes complex systems work)",
  "documentarian": "Vision (Sees the whole system)",
  "reviewer": "Black Widow (Precision, detail-oriented)",
  "policy": "JARVIS (System rules)"
}
```

**Structure:**

```
web-squad/
├── .squad/
│   ├── agents/
│   │   ├── iron-man/       # Lead
│   │   ├── wanda/          # Developer (usually 2+ developers for larger team)
│   │   ├── vision/         # Documentarian
│   │   ├── black-widow/    # Reviewer
│   │   └── jarvis/         # Policy bot
```

**Customizations Made:**
- ✅ Cast names from Marvel
- ✅ Added two Wanda instances (web-frontend, web-backend)
- ⚠️ Kept all 5 core roles (no optional roles needed)
- ✅ Daily standups (web team prefers sync check-ins)

---

### Example 2: Platform Squad (Infrastructure Team)

**Context:** Team maintaining shared infrastructure for multiple dependent teams

**Customization:**

```json
{
  "theme": "Star Wars",
  "lead": "Leia (Strategy, alignment with dependent teams)",
  "developer": "Luke (Builder of systems)",
  "documentarian": "C-3PO (Protocol, knowledge keeper)",
  "reviewer": "Yoda (Master of infrastructure craft)",
  "policy": "The Force (System governance)",
  "optional_roles": [
    "Chewbacca - DevOps & Deployments",
    "R2D2 - Monitoring & Alerts"
  ]
}
```

**Structure:**

```
platform-squad/
├── .squad/
│   ├── agents/
│   │   ├── leia/           # Lead
│   │   ├── luke/           # Developer
│   │   ├── c3po/           # Documentarian
│   │   ├── yoda/           # Reviewer
│   │   ├── the-force/      # Policy bot
│   │   ├── chewbacca/      # DevOps (optional)
│   │   └── r2d2/           # Monitoring (optional)
```

**Customizations Made:**
- ✅ Cast names from Star Wars
- ✅ Added two optional roles (DevOps, Monitoring)
- ⚠️ Async-first (dependent teams across timezones)
- ✅ Weekly retros (infrastructure changes need reflection)

---

### Example 3: Data Squad (Analytics Platform)

**Context:** Team building data pipelines and analytics infrastructure

**Customization:**

```json
{
  "theme": "Greek Mythology",
  "lead": "Zeus (Authority, tiebreaker decisions)",
  "developer": "Hephaestus (Creator, builder)",
  "documentarian": "Hermes (Messenger, knowledge flow)",
  "reviewer": "Athena (Wisdom, pattern recognition)",
  "policy": "The Fates (Governance, threading data rules)",
  "optional_roles": [
    "Hestia - Data Quality & Testing",
    "Apollo - Analytics & Metrics"
  ]
}
```

**Structure:**

```
data-squad/
├── .squad/
│   ├── agents/
│   │   ├── zeus/           # Lead
│   │   ├── hephaestus/     # Developer (pipeline builders)
│   │   ├── hermes/         # Documentarian
│   │   ├── athena/         # Reviewer (data quality reviewer)
│   │   ├── fates/          # Policy bot
│   │   ├── hestia/         # Data Quality (optional)
│   │   └── apollo/         # Analytics (optional)
```

**Customizations Made:**
- ✅ Cast names from Greek Mythology
- ✅ Gimli-equivalent (Athena) focused on data quality, not code quality
- ✅ Added two optional roles (Quality, Analytics)
- ⚠️ Governance focused on data policies (retention, privacy, lineage)

---

## Part 6: Common Customization Mistakes to Avoid

### ❌ Don't: Over-customize the Charter

**Mistake:** Adding 15 new sections to the charter template

**Impact:** Inconsistency between roles; hard to onboard new agents; loses the benefit of a standard template

**Fix:** Use the 8-section template as-is. If you need to add project-specific info, do it in a separate "Project Context" section at the end.

---

### ❌ Don't: Blur Role Boundaries

**Mistake:** Having Lead also be Documentarian (or similar overlaps) without explicitly documenting the handoff

**Impact:** When role-holder is busy, neither responsibility gets done; unclear who owns decisions

**Fix:** If combining roles (small teams), explicitly document this in the charter. Example:

```markdown
## Blended Roles

In this squad, Gandalf serves as both Lead and part-time Reviewer.
Boundary: Gandalf makes decisions; Gimli does final review before merge.
If Gandalf is unavailable, decisions escalate to Elrond (Developer Lead).
```

---

### ❌ Don't: Skip Optional Roles Then Regret It

**Mistake:** Starting with 5 core roles, then 6 months later adding a 6th role without updating charters, routing table, or documentation

**Impact:** New role flies under the radar; not properly integrated; causes confusion

**Fix:** Decide upfront if you'll need optional roles. Pre-fill charter templates for likely future roles (even if unused). When you activate a role, follow the same process as core roles.

---

### ❌ Don't: Use a Theme You Don't Know

**Mistake:** Picking LotR theme but team doesn't know the characters; can't remember who Treebeard is

**Impact:** Cast names become meaningless; team reverts to generic names anyway

**Fix:** Pick a theme your team knows and enjoys. If LotR doesn't work for your team, try Marvel, Star Wars, or your company culture.

---

### ❌ Don't: Change Role Names Mid-Stream

**Mistake:** Starting with "Reviewer" then changing to "Quality Assurance Lead" 3 months in

**Impact:** Docstrings, labels, charters all have outdated names; confusion about what changed

**Fix:** Get role names right upfront (theme + charter). If you must change, do it as a documented decision with migration plan (old labels → new labels, etc.).

---

## Part 7: Role Interaction Patterns

### How Roles Actually Collaborate

#### Developer ↔ Reviewer

```
Developer opens PR
    ↓ (notifies Reviewer)
Reviewer checks: correctness, tests, docs, performance
    ↓
  🟢 Approved
    OR
  🔴 Changes Requested
    ↓
Developer responds (async, within 24-48h)
    ↓
Reviewer checks again or Lead overrides with rationale
```

**SLA:** Reviewer responds within 24-48 hours; Developer responds within same window

---

#### Lead ↔ Developer ↔ Documentarian

```
Lead receives feature request
    ↓ (routes to Developer via 'squad:developer' label)
Developer breaks down into tasks, shares design with Lead
    ↓
Lead approves design, Documentarian added to review
    ↓
Documentarian reviews for docs requirements
    ↓ (Developer implements)
Developer opens PR with docs
    ↓
Reviewer + Documentarian review; Reviewer approves code, Documentarian approves docs
    ↓
Documentarian updates INDEX.md, TAGS.md; Lead announces completion
```

**Key:** No one person owns everything; each role has a clear touch-point

---

#### All Roles ↔ Policy Enforcer (Ralph)

```
Developer commits work
    ↓
Ralph (bot) runs checks: commit message format, branch naming, linting
    ↓
  ✅ All checks pass → PR allowed
    OR
  ❌ Violations detected → PR blocked with clear message
    ↓ (Lead can override with 'policy-override' + rationale)
Ralph logs override for retrospective review
```

**Key:** Policy is automated; Lead decides exceptions.

---

#### @copilot ↔ Lead ↔ Reviewer

```
Lead assesses task suitability
    ↓
  🟢 Good fit → 'good-for-copilot' label
    ↓
@copilot picks up task
    ↓
@copilot implements, tests, opens PR
    ↓
Reviewer reviews exactly as they would for human-written code
    ↓
  🟢 Approved → Merge and celebrate
  OR
  🔴 Changes Requested → @copilot iterates
```

**Assessment Criteria:**
- 🟢 **Good fit:** Clear spec, objective success criteria, isolated to one area, no judgment calls needed
- 🟡 **Needs review:** Good candidate but might benefit from human pre-analysis or clarification
- 🔴 **Not suitable:** Requires customer judgment, architectural decision, cross-team alignment, or significant refactoring

---

## Part 8: Customization Checklist

Before launching your squad:

- [ ] **Theme chosen:** Do all team members know and like the theme?
- [ ] **Cast names mapped:** Each role has a character; team agrees it's appropriate
- [ ] **Charters written:** 8-section template filled for each role, customized for project
- [ ] **Routing table defined:** Every work type has an owner and escalation path
- [ ] **Labels created:** GitHub labels for `squad` and `squad:{agent}` match your naming
- [ ] **Policy decided:** What squad policies will Ralph enforce? (commit messages, branch naming, etc.)
- [ ] **Optional roles decided:** Do you need Infrastructure? Analytics? Community? Decide now.
- [ ] **SLAs defined:** What are response time expectations? Async-first or sync-heavy?
- [ ] **Theme consistency:** Are all optional roles' names consistent with theme?
- [ ] **Documentation complete:** Is customization documented so next person can understand why choices were made?

---

## Part 9: Scaling Considerations

### Small Squad (1-3 people per role)

**Customization:** Keep close to template; minimal optional roles

**Example:**
```
Lead: You
Developer: Bob (does 80% of coding)
Documentarian: Carol (part-time, handles docs + some coding)
Reviewer: You (Lead also reviews)
Policy: Ralph (bot)
```

---

### Medium Squad (3-5 per role)

**Customization:** Can add optional roles; may need multiple developers in different areas

**Example:**
```
Lead: Alice
Developers: Bob (backend), Carol (frontend), Dave (infrastructure)
Documentarian: Eve
Reviewer: Frank (code quality) + Grace (infrastructure)
Policy: Ralph
Optional: Hans (DevOps), Iris (Analytics)
```

---

### Large Squad (5+ per role)

**Customization:** Multiple sub-teams; each may have mini-lead; need clear escalation paths

**Example:**
```
Lead: Executive Lead (Alice) → Sub-leads (Backend, Frontend, Infrastructure)
Developers: 10+ across multiple areas
Documentarian: Eve + Jack (assistant)
Reviewers: Multiple (by area of expertise)
Policy: Ralph + custom policies per area
Optional: Full infrastructure, analytics, compliance teams
```

---

## Part 10: References & Related Docs

- **Squad Template Patterns Guide:** See `docs/guides/squad-template-patterns.md` for ceremony structure, review gates, and interaction patterns
- **ms-pa Implementation:** See `.squad/agents/*/charter.md` for worked examples of each role in practice
- **Charter Template:** See `squad-template/agents/[role]/charter.md` for 8-section template
- **cast.json Format:** See `squad-template/cast.json` for JSON schema

---

**Author:** Bilbo (Librarian & Documentarian)  
**Last Updated:** 2026-03-23  
**Status:** Final — Ready for squad customization
