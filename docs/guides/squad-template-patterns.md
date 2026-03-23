---
title: "Squad Template Patterns & Design Principles"
date: 2026-03-23
author: bilbo
documentarian: bilbo
category: guides
tags: [guide, squad-infra, patterns, workflow, bilbo, final]
status: final
---

# Squad Template Patterns & Design Principles

## Overview

This guide distills the architectural patterns that made the ms-pa squad effective, enabling future squads to bootstrap reliably. These patterns were validated through real-world use and are documented as essential vs. optional to guide customization.

**Audience:** Squad leads and documentarians setting up new squads.  
**Scope:** Core infrastructure patterns, governance structures, role interactions, and lessons learned.

---

## 1. Charter Structure Pattern

### The Foundational Pattern

Every squad role defines their scope through a **charter** — a single-source-of-truth document that maps identity to ownership to collaboration. The ms-pa squad template standardizes this structure:

| Section | Purpose | Example |
|---------|---------|---------|
| **Identity** | Who you are and your expertise | "Bilbo, Documentarian: Deep knowledge of squad infrastructure, patterns, and knowledge systems" |
| **What I Own** | Decision authority and accountabilities | "Own docs/ structure, INDEX.md synchronization, documentation taxonomy enforcement" |
| **How I Work** | Daily practices, communication style, automation | "Async-first, written decisions, automated index updates via scripts" |
| **Boundaries** | What you won't do or areas requiring escalation | "Won't write code in production; escalate authorization to Lead" |
| **Model** | Work patterns and response times | "Writes long-form docs (1-3h); reviews in async batches (24h)" |
| **Collaboration** | How you interact with other roles | "Consults with Gimli on tooling, Ralph on governance before deploying automation" |
| **Voice** | Your unique communication and contribution style | "Verbose, precise, document-centric; prefers written discourse to meetings" |
| **Customization** | How to adapt this charter for your squad | "Replace LotR names, update project context, adjust response times for timezone" |

### Key Design Principle: Template is Reusable

The charter template uses **placeholders** for project-specific content:
- Generic role names (Lead, Developer, Documentarian, etc.)
- `{ProjectName}` and `{Context}` placeholders
- LotR theme as example (teams choose their own theme)

**What's Essential:** Every role has a charter. Sections are mandatory.  
**What's Optional:** Specific names, theme, response time commitments.

---

## 2. Routing Table Pattern

### Problem it Solves

How does work get to the right person? The squad uses a **routing table** — a structured mapping of work types to responsible roles.

### The Pattern

```
Work Type                    → Primary Owner    → Escalation
----------------------------------------
Feature specification       → Lead             → Dev + Documentarian review
Code review                 → Reviewer         → Lead triage
Documentation               → Documentarian    → Lead if urgent
Incident response           → Lead             → All roles (standby)
Decision governance         → Lead (facilitator)→ Consultants per topic
```

### Features of Effective Routing

1. **Unambiguous mapping:** Each work type has a clear owner
2. **Escalation paths:** Clear escalation when primary can't take it
3. **Label-driven automation:** GitHub labels (`squad:lead`, `squad:dev`, etc.) trigger workflows
4. **@copilot integration:** Lead can route to copilot with assessment (🟢 good-fit, 🟡 needs-review, 🔴 not-suitable)

### How Labels Flow

```
Issue created with 'squad' label
    ↓
Lead triage (async, typically within 24h)
    ↓
Lead applies squad:{agent} label + optional assessment emoji
    ↓
Agent notified, work begins
    ↓
Copilot can pick up with 🟢 label if it's a good fit
```

**What's Essential:** Routing table, consistent labeling, clear escalation.  
**What's Optional:** Specific emoji choices, response time SLAs.

---

## 3. Ceremonies Pattern

### Problem it Solves

How does the squad stay aligned and learn? Automated ceremonies keep the squad synchronized without manual scheduling.

### The Two Core Ceremonies

#### Design Review (Before)
- **Trigger:** Multi-agent task or high-impact decision detected
- **Participants:** Lead (facilitator), relevant technical experts, Reviewer
- **Purpose:** Ensure design meets standards, identify risks, alignment check
- **Automation:** Can be auto-triggered by PR title patterns or labels
- **Outcomes:** Design approved, risks documented, concerns resolved

#### Retrospective (After)
- **Trigger:** Major incident, failed release, or manual scheduling
- **Participants:** All squad roles
- **Purpose:** Extract learnings, identify process improvements, celebrate wins
- **Automation:** Can be auto-triggered on build failures or incident closure
- **Outcomes:** Documented learnings, process improvements proposed, morale maintained

### Key Design Principle: Async-First

- Ceremonies use async write-ups before synchronous discussion
- Reduces meeting time, improves participation
- Documented in shared space (discussion thread, dedicated doc)

**What's Essential:** Both ceremonies, clear triggers, documented outcomes.  
**What's Optional:** Ceremony format (video + doc vs. pure async), frequency of scheduled retros.

---

## 4. Review Gates Pattern

### Problem it Solves

How does the squad maintain quality? Review gates embed quality checks into the workflow without slowing down work.

### The Pattern

Review gates exist at three levels:

1. **Code Review Gate** (every PR)
   - Reviewer checks: correctness, test coverage, docs, performance
   - Documentarian checks: README updated, decision logged if applicable
   - Lead can approve in urgent cases with documented rationale

2. **Design Review Gate** (before multi-agent work)
   - Ensures approach is sound, risks identified, dependencies clear
   - Prevents thrashing and rework

3. **Policy Gate** (governance decisions)
   - Ralph (policy bot) can enforce squad policies
   - Examples: commit message format, branch naming, mandatory docs

### Effective Gate Implementation

- Gates are **automated where possible** (linters, label checks, bots)
- Gate failures are **non-blocking with escalation** (Lead can override with rationale)
- Gate SLAs are **tracked and reviewed** in retrospectives

**What's Essential:** Clear gate criteria, automation, escalation paths.  
**What's Optional:** Specific tools used to implement gates.

---

## 5. Role Interactions & Dependency Pattern

### Problem it Solves

How do roles coordinate? The squad defines **interaction patterns** so roles know when/how to collaborate.

### Key Interaction Patterns

| Interaction | Pattern | Example |
|-------------|---------|---------|
| **Async escalation** | Role can't handle → Write to Lead in batched message → Lead responds within SLA | Dev hits blocker → Posts in #squad channel → Lead triages within 24h |
| **Consultation gate** | Some decisions require input from specific roles | Gimli consults Bilbo before deploying new tooling script |
| **Parallel work** | Multiple roles work independently on same feature | Dev codes, Documentarian writes docs, Reviewer pre-checks async |
| **Bottleneck detection** | If one role is consistently slower, it signals need for help | If Reviewer can't keep up with PRs → Lead considers adding second Reviewer |

### @copilot Routing Example

The squad can extend to @copilot agents:

```
Lead: This task is 🟢 good-fit for @copilot-dev
   → @copilot-dev picks it up
   → Implements, tests, opens PR
   → Reviewer and Documentarian review async

Lead: This task is 🟡 needs-review by human first
   → Task waits for human Developer analysis
   → Once understood, can route to @copilot

Lead: This task is 🔴 not-suitable (requires judgment, customer context, etc.)
   → Task must stay with human
```

**What's Essential:** Clear collaboration boundaries, escalation paths, @copilot assessment criteria.  
**What's Optional:** Specific emoji scheme (use whatever the team prefers).

---

## 6. Customization Guidance

### What Stays Generic

These elements should **not** be customized — they're part of the squad infrastructure:

- **Charter structure** (8-section format)
- **Routing table pattern** (work type → role → escalation)
- **Ceremony triggers** (before multi-agent work, after incidents)
- **Policy enforcement** (Ralph bots, governance)
- **Label naming** (`squad`, `squad:{agent}`)

### What Should Be Customized

These elements **must** be adapted for your project:

- **Agent names** (generic "Lead", "Developer" → your cast names)
- **Project context** in charters (replace {ProjectName} with actual project)
- **Theme** (LotR → Star Wars, Marvel, your company, etc.)
- **SLA expectations** (async/sync balance, response times)
- **Additional roles** (add/remove roles specific to your project)
- **Specific tools** (Ralph implementation, @copilot services)

### Judgment Calls

These can go either way; document the trade-off:

- **History format:** Append to single file vs. date-based entries?
- **Ceremony cadence:** Weekly standups vs. pure async?
- **Documentation level:** Lightweight README vs. comprehensive runbooks?
- **Skill structure:** Centralized vs. per-role skill folders?

---

## 7. Lessons Learned from ms-pa

### What Worked Well ✅

1. **Async-first design:** Squad stayed synchronized across timezones; reduced unnecessary meetings
2. **Clear routing table:** Eliminated ambiguity about "who takes this?"; reduced decision fatigue
3. **Charter-as-constitution:** Single source of truth; resolved conflicts quickly
4. **Automated ceremonies:** Design reviews and retros happened reliably without manual scheduling
5. **@copilot integration:** Extended capacity without adding headcount; freed humans for judgment calls
6. **Role-based labels:** GitHub workflow automatically routed work to right person
7. **Policy enforcement:** Ralph bots caught issues early; reduced manual PR comments

### What We'd Do Differently ⚠️

1. **Upfront theme decision:** Spent time deciding on LotR theme; pick theme earlier in setup
2. **Charter feedback cycle:** First drafts of charters were rough; build in feedback round before locking
3. **Ceremony documentation:** Early retros were ad-hoc; establish template earlier
4. **@copilot assessment criteria:** Took time to develop 🟢🟡🔴 scoring; document this upfront
5. **SLA tracking:** Didn't track response time SLAs; build into metrics from day one

### Anti-Patterns to Avoid ❌

1. **Over-customizing the charter:** Some roles added too many custom sections; stick to 8-section template
2. **Blurry role boundaries:** When roles overlap, conflicts increase; be precise about "What I Own"
3. **Manual ceremony scheduling:** If retros require calendar coordination, they'll be skipped; automate or define standing time
4. **Ungoverned customization:** Without policy enforcement (Ralph), squad rules slowly eroded; enforce standards from day one
5. **Copilot-first routing:** Don't route to copilot by default; use 🟢🟡🔴 to flag when it's appropriate
6. **Missing escalation paths:** If there's no clear "what do I do if this breaks?", work backs up; document escalation for every gate

---

## 8. Implementation Checklist

When setting up a new squad using these patterns:

- [ ] **Charters:** Write charter for each role using 8-section template
- [ ] **Routing table:** Define work types, owner roles, escalation paths
- [ ] **Labels:** Create `squad` and `squad:{agent}` labels on GitHub
- [ ] **Ceremonies:** Set up Design Review and Retrospective automation/scheduling
- [ ] **Review gates:** Configure code review, design review, policy checks
- [ ] **Documentation:** Document role interactions, @copilot assessment criteria, SLAs
- [ ] **First standup:** Verify all roles can execute end-to-end (from work creation to delivery)
- [ ] **Learning system:** Set up mechanism to capture learnings (Bilbo's history pattern)

---

## 9. Template Repository Structure

```
squad-template/
├── README.md                          # Overview and quick-start
├── cast.json                          # Team names and theme (customize)
├── .squad/
│   ├── policy.json                    # Governance rules
│   ├── decisions.md                   # Decision log template
│   ├── agents/
│   │   ├── lead/
│   │   │   └── charter.md            # Role charter template
│   │   ├── developer/
│   │   │   └── charter.md
│   │   ├── documentarian/
│   │   │   └── charter.md
│   │   ├── reviewer/
│   │   │   └── charter.md
│   │   ├── ralph/
│   │   │   └── policy.json           # Policy enforcement (keep generic)
│   │   └── [additional roles]/
│   │       └── charter.md
│   └── ceremonies/
│       ├── design-review.md          # Before-work ceremony
│       └── retrospective.md          # After-work ceremony
├── docs/
│   ├── INDEX.md                       # Documentation index
│   └── guides/
│       └── squad-setup-checklist.md  # This guide
└── github/
    ├── workflows/
    │   ├── design-review-trigger.yml # Ceremony automation
    │   └── routing-labels.yml         # Label-based routing
    └── issue-templates/
        └── epic-template.md           # Multi-agent work template
```

---

## 10. Related Documentation

- **Role Mapping Reference:** See `docs/guides/squad-template-role-mapping.md` for detailed role descriptions and customization guidance
- **Knowledge Management System:** See `.squad/skills/knowledge-management/SKILL.md` for documentation taxonomy
- **ms-pa Implementation:** See `.squad/agents/*/charter.md` for worked example of these patterns in practice

---

## References

- **GitHub Issues:** #10 (porting), #18 (template creation)
- **Gimli's Squad-Starter Template:** `templates/squad-starter/`
- **ms-pa Charter Collection:** `.squad/agents/*/charter.md`

---

**Author:** Bilbo (Librarian & Documentarian)  
**Last Updated:** 2026-03-23  
**Status:** Final — Ready for squad adoption
