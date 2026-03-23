---
title: "SubSquads Architecture at Scale: Multi-Team Monorepo Patterns"
date: 2026-03-23
author: elrond
documentarian: bilbo
category: research
tags:
  - research
  - architecture
  - squad-infra
  - git
  - workflow
  - elrond
status: final
related_issues:
  - 24
related_docs: null
superseded_by: null
---

# SubSquads Architecture at Scale: Multi-Team Monorepo Patterns

## Executive Summary

SubSquads enable 2–5 autonomous teams to work in parallel within a single monorepo by combining **label-based issue routing**, **CODEOWNERS-driven review assignment**, **branch-per-issue discipline**, and **optional folder scoping**. The pattern has been proven effective in Tamir Dresher's Tetris experiment (3 teams, 9 issues closed in 2 hours) and is documented across Squad framework examples.

**Key findings:**
- **Label leakage prevention:** Strict label governance (centralized creation, team-prefixed labels, automated sync) is essential; without it, cross-team label conflicts rapidly degrade issue queue clarity.
- **CODEOWNERS integration:** File/directory ownership mapped to GitHub teams enables automated reviewer assignment and enforces code review gates, critical for multi-team quality control.
- **Scaling limits:** SubSquads scale linearly for issue throughput up to ~5 autonomous teams with well-scoped domains. Beyond 5–8 teams, cross-cutting changes (dependency upgrades, shared library updates) become bottlenecks unless complemented with advanced coordination tooling or explicit interface contracts.
- **Failure modes:** Shared code conflicts are the primary failure mode. Teams that violate branch-per-issue discipline or lack clear CODEOWNERS assignment experience frequent merge wars and reduced confidence in main branch stability.
- **ms-pa adoption:** SubSquads are **feasible and recommended for ms-pa** because: (1) the codebase already has domain-scoped folders (agents, skills, CLI), (2) team roles map naturally to SubSquads (frontend/UI, backend, infrastructure), and (3) the Squad framework is already in use.

---

## Context & Motivation

GitHub Issue #24 (ms-pa repository, jbenami_microsoft org) requests a deep research into SubSquads architecture to evaluate adoption for the ms-pa project. The research scope includes understanding label routing, CODEOWNERS integration, failure modes at scale, and specific feasibility within ms-pa's structure.

**Research sources:**
- Tamir Dresher's 4-part blog series on "Organized by AI" and multi-team monorepo workflows (Parts 0–3)
- Squad framework documentation and SubSquads examples
- GitHub best practices on label governance, CODEOWNERS, and multi-team workflows
- Industry research on monorepo scaling limits and branch strategies

---

## Methodology

This research synthesized evidence from:

1. **Blog Series & Real-World Case Study (Tamir Dresher)**
   - Part 0: Introduction to organizing by AI and SubSquads concept
   - Part 1: First team to production (single SubSquad setup)
   - Part 2: Organizational knowledge and collective patterns
   - Part 3: Multi-team streams and the Tetris experiment (3 teams, real results)

2. **Web Research & Best Practices**
   - GitHub label pollution prevention and centralized governance patterns
   - CODEOWNERS file setup, GitHub team assignment, and review automation
   - Monorepo branching strategies (branch-per-issue, trunk-based variants)
   - Scaling limits for multi-team workflows (2–8 teams documented)

3. **Squad Framework Examples**
   - SubSquads documentation in squad-skills and bradygaster/squad
   - Label-based issue routing patterns
   - Folder scope advisory vs. enforcement tradeoffs

---

## Section 1: SubSquads Architecture Overview

### What Are SubSquads?

A SubSquad is a semi-autonomous workstream within a single monorepo, defined by:
- **Label filter** (e.g., `team:ui`, `team:backend`): Scope of work assigned to the team
- **Folder scope** (advisory): Recommended directories the team owns (e.g., `src/ui/` for frontend)
- **Dedicated Codespace/environment**: Optional; allows team members to work in isolation
- **Branch-per-issue discipline**: Each issue gets a dedicated branch; PRs are the integration point

### Key Components

#### 1. Label-Based Issue Routing
- **Purpose:** Isolate issue queues so each team sees only their own work
- **Implementation:** 
  - Create team-specific labels (e.g., `team:ui`, `team:backend`, `team:infra`)
  - GitHub automation or manual triage assigns labels to new issues
  - Team members filter their issue queue to `label:team:ui` (or equivalent)
- **Benefit:** Prevents context switching and reduces cognitive load on teams

#### 2. CODEOWNERS Integration
- **Purpose:** Automate code review assignment and enforce review gates
- **Implementation:**
  ```
  # .github/CODEOWNERS file
  /src/ui/              @org/team-ui
  /src/backend/         @org/team-backend
  /src/infra/           @org/team-infra
  /packages/shared/     @org/team-ui @org/team-backend
  ```
- **Behavior:** GitHub automatically adds CODEOWNERS as reviewers when PRs touch these paths
- **Enforcement:** Branch protection rules require CODEOWNERS approval before merge
- **Benefit:** Ensures code review from subject-matter experts; prevents unauthorized changes to critical code

#### 3. Branch-Per-Issue Discipline
- **Purpose:** Isolate work, minimize merge conflicts, enable parallel development
- **Pattern:** Each issue gets a dedicated branch named `{team/issue-number}` or similar
  - Example: `ui/issue-42`, `backend/issue-99`
- **Merge strategy:** Feature branch → PR → Code review → Merge to main
- **Benefit:** Clean PR history, traceable changes, easier to revert if needed

#### 4. Folder Scopes (Advisory)
- **Purpose:** Provide guidance on team ownership without enforcement
- **Constraint:** Folder scopes are **not** access control; any team member can edit any file
- **Cultural enforcement:** Code review (CODEOWNERS) + team communication ensure teams respect scopes
- **Shared folders:** Teams that need to touch shared libraries (e.g., `packages/shared/`) must coordinate via PR review and cross-team discussion

---

## Section 2: Label Leakage Prevention

### Problem: Label Pollution in Multi-Team Monorepos

Without strict governance, label pollution emerges quickly:
- **Too many labels:** Each team creates their own labels; repo ends up with 100+ labels that are overlapping or unclear
- **Inconsistent naming:** `urgent`, `critical`, `p0`, `high-priority` all mean different things to different teams
- **Scope creep:** Labels meant for one team get applied across the repo
- **Broken automation:** CI/CD pipelines that key off labels misfire when labels are inconsistent or polluted

### Prevention Mechanisms

#### 1. Centralized Label Governance
- **Owner:** Single team or working group (usually repo maintainers or DevOps) has exclusive permission to create/edit/delete labels
- **Documentation:** Maintain a labels specification (JSON or YAML) that defines all approved labels and their usage
- **Example structure:**
  ```json
  [
    {
      "name": "team:ui",
      "color": "0366d6",
      "description": "Work owned by the UI team"
    },
    {
      "name": "team:backend",
      "color": "2ecc71",
      "description": "Work owned by the backend team"
    }
  ]
  ```

#### 2. Team-Prefixed Labels
- **Pattern:** All team-specific labels use a consistent prefix (e.g., `team:`, `squad:`, `area:`)
- **Benefit:** Avoids collisions; makes automation easier (e.g., filter `label:team:*` for all team labels)
- **Example:** `team:ui`, `team:backend`, `team:infra`, not `frontend`, `Backend`, `infrastructure`

#### 3. Automated Label Synchronization
- **Tool:** `github-label-sync` (open source) or custom GitHub Actions workflow
- **Workflow:** 
  1. Maintain labels specification in source control (e.g., `.github/labels.json`)
  2. Run `github-label-sync` on every PR merge or on a schedule
  3. Tool automatically creates, updates, or deletes labels to match the spec
- **Benefit:** Prevents drift; label set always matches the specification

#### 4. Audit & Cleanup
- **Periodic audits:** Query GitHub API to find unused labels (no issues with the label in the last 30 days)
- **Deprecation policy:** Mark unused labels as deprecated in the spec; warn teams in CONTRIBUTING.md
- **Automation:** GitHub Actions job that reports unused labels monthly; removes them after 60 days if not reinstated

### Real-World Impact

In Tamir's Tetris experiment, label governance was **strict**: each team had isolated labels (`team:ui`, `team:backend`, `team:infra`), and automation routed issues accordingly. Result: **no cross-team label interference**; each team's issue queue remained clean and focused.

---

## Section 3: CODEOWNERS Integration

### Setting Up CODEOWNERS

#### 1. Create `.github/CODEOWNERS` File

```
# Format: path (glob) → owner (GitHub team or user)

# UI Ownership
/src/ui/              @org/team-ui
/src/components/      @org/team-ui

# Backend Ownership
/src/backend/         @org/team-backend
/src/api/             @org/team-backend

# Infrastructure Ownership
/src/infra/           @org/team-infra
/.github/workflows/   @org/team-infra

# Shared Ownership (requires approval from both teams)
/packages/shared/     @org/team-ui @org/team-backend
/package.json         @org/team-infra @org/team-backend
```

#### 2. Key Rules

- **Glob patterns:** Paths are matched as globs; `*` matches one directory level, `**` matches any depth
- **Order matters:** First matching rule wins; more specific paths should come first
- **GitHub teams only (recommended):** Use `@org/team-name`, not individual user handles
  - Benefit: Prevents single-person bottlenecks; team changes don't break CODEOWNERS
- **Multiple owners (shared code):** Separate with spaces: `@org/team-a @org/team-b`

#### 3. Branch Protection & Review Requirements

Configure branch protection on `main`:
1. **Require pull request reviews before merging:** Yes
2. **Require review from code owners:** Yes
3. **Require status checks to pass:** Yes (CI/CD pipelines)
4. **Require branches to be up to date:** Yes (avoid stale PRs)

With these settings:
- Every PR automatically gets CODEOWNERS assigned as reviewers
- PR cannot be merged until CODEOWNERS approve
- Ensures expert review of all changes to protected code

### CODEOWNERS vs. Folder Scopes

| Aspect | CODEOWNERS | Folder Scope (Advisory) |
|--------|-----------|------------------------|
| **Enforcement** | Hard (GitHub-enforced, review gate) | Soft (cultural/process-enforced) |
| **Scope** | File/directory paths in `.github/CODEOWNERS` | Broad folders like `/src/ui/` |
| **Automation** | GitHub auto-assigns reviewers on PR | No automation; relies on discipline |
| **Cross-team changes** | Clear: PR goes to both teams' CODEOWNERS | Ambiguous: rely on PR description |
| **Scalability** | Works well for 2–5 teams | Scales better but requires trust |

**Best practice:** Use CODEOWNERS for sensitive files (API contracts, shared libs, CI/CD) and advisory folder scopes for general team areas.

---

## Section 4: Real-World Case Study — Tetris Experiment

### Setup

**Three teams, single monorepo (`tetris-monorepo`):**
- **UI Team:** React frontend, WebSocket client
- **Backend Team:** Node.js API server, game logic
- **Infrastructure Team:** Cloud deployment, CI/CD, monitoring

**SubSquads configuration:**
- Labels: `team:ui`, `team:backend`, `team:infra`
- CODEOWNERS: `/src/ui/` → `@org/team-ui`, `/src/backend/` → `@org/team-backend`, `/infra/` → `@org/team-infra`
- Branch discipline: `ui/{issue}`, `backend/{issue}`, `infra/{issue}`
- Shared folder: `/packages/shared/` (game rules, protocol definitions) → both `@org/team-ui` and `@org/team-backend`

### Results (Documented by Tamir)

- **Time:** 2 hours of parallel development
- **Throughput:** 9 issues closed (3 per team)
- **Merge quality:** Clean PRs, minimal conflicts on main branch
- **Lesson learned:** Label isolation and CODEOWNERS assignment prevented teams from stepping on each other; branch-per-issue kept commits traceable and merge conflicts localized to shared code (which was coordinated via PR review)

### Key Insight

The experiment validated that **label routing + CODEOWNERS + branch discipline = predictable, scalable multi-team development**. No coordination overhead; each team moved at its own pace. Conflicts only surfaced at merge time, not during day-to-day work.

---

## Section 5: Scaling Limits & Failure Modes

### Scalability by Team Count

| Team Count | Status | Notes |
|------------|--------|-------|
| 2–3 | ✅ Ideal | Minimal coordination overhead; label isolation effective |
| 4–5 | ✅ Good | Still manageable; cross-team PRs increase but remain tractable |
| 5–8 | ⚠️ Caution | Coordination overhead rises; shared code conflicts become frequent; need dashboards |
| >8 | ❌ Challenging | Cross-cutting changes (dependency upgrades, infra changes) create bottlenecks; SubSquads alone insufficient |

### Failure Mode 1: Branch Conflicts in Shared Code

**Problem:** Multiple teams touch the same shared file (e.g., `packages/shared/types.ts`). When several PRs merge in quick succession, later PRs get merge conflicts.

**Why:** Folder scopes are advisory; teams can access shared code. Without strict coordination, conflicts accumulate.

**Mitigation:**
- **Explicit ownership:** Use CODEOWNERS to require review from **both** teams for shared files
- **API contracts:** Document stable interfaces in `packages/shared/`; teams agree not to break them
- **Staggered merges:** If conflicts are frequent, coordinate merge order via team chat or sprint planning
- **Branch-per-issue (rigorously enforced):** Ensures rebasing is clean and conflicts are caught early

### Failure Mode 2: Label Routing Breaks Down

**Problem:** Teams add their own labels without coordination. Automation routed based on labels misfires. CI/CD jobs key off labels and fail unpredictably.

**Why:** No centralized label governance; team isolation breaks down.

**Mitigation:**
- **Centralized governance:** Only repo admins can create labels; teams request via issue or PR
- **Automated sync:** Use `github-label-sync` to ensure label set always matches spec
- **Documentation:** CONTRIBUTING.md clearly states label policy and team prefixes
- **Audits:** Quarterly cleanup of orphaned labels; remove unused labels after 60 days

### Failure Mode 3: CODEOWNERS Assignment Misses Cross-Team Code

**Problem:** New shared code added (e.g., new file in `/packages/shared/`) but CODEOWNERS not updated. PR is reviewed by only one team; other team unaware of changes.

**Why:** CODEOWNERS file can become stale; processes for updating it are informal.

**Mitigation:**
- **Include CODEOWNERS updates in PR template:** "Does this PR require CODEOWNERS changes?"
- **Automated checks:** GitHub Actions job validates CODEOWNERS paths exist and are covered by glob patterns
- **Team communication:** Slack notification when CODEOWNERS changes; teams confirm they're happy with new ownership
- **Regular audits:** Quarterly review of CODEOWNERS; teams confirm they still own the paths assigned

### Failure Mode 4: Branch-Per-Issue Discipline Collapses

**Problem:** Team members commit directly to `main` or use long-lived branches (`develop`, `staging`) that accumulate changes. Merge conflicts explode; blame history becomes unreadable.

**Why:** Discipline is cultural; no technical enforcement.

**Mitigation:**
- **Branch protection:** Use GitHub branch protection rules to disallow direct commits to `main`
- **PR review gate:** Every commit must go through PR + review + CODEOWNERS approval
- **PR templates:** Remind teams to use `team/issue-{num}` branch naming
- **CI/CD checks:** Fail builds if PR is from a branch not following the naming pattern (warning-level initially)

---

## Section 6: Gaps & Limitations in Tamir's Documentation

Tamir's blog series (Parts 0–3) provides excellent high-level patterns and real-world validation but leaves some gaps:

### Gap 1: Label Governance Specifics
Tamir's blog mentions label isolation but doesn't document the **specific tools and policies** needed to prevent label pollution. This research filled in that gap with `github-label-sync`, centralized label ownership, and audit procedures.

### Gap 2: CODEOWNERS Advanced Patterns
Tamir's examples show simple CODEOWNERS assignments but don't document:
- Multi-team ownership of shared code (when to require both teams' approval)
- Fallback patterns (if a team is off-hours, what happens to a PR?)
- Escalation procedures for urgent changes to CODEOWNERS-protected code

### Gap 3: Failure Modes & Recovery
Tamir focuses on the happy path (Tetris experiment success). This research identified and documented failure modes:
- Branch conflicts in shared code
- Label pollution breakdown
- CODEOWNERS staleness
- Branch discipline collapse

### Gap 4: Scaling Beyond 5 Teams
Tamir's experiment was 3 teams; industry research suggests SubSquads work best up to 5–8 teams. This research documented the scaling limits and identified what's needed beyond 8 teams (dashboards, advanced coordination tooling, explicit interface contracts).

### Gap 5: ms-pa Specific Applicability
Tamir's blog doesn't address ms-pa specifically. This research provides a feasibility assessment and adoption roadmap for ms-pa's structure.

---

## Section 7: Feasibility & Adoption for ms-pa

### ms-pa Repository Structure

Current structure (from exploration):
```
pa-squad/
├── docs/              # Documentation (research, decisions, guides)
├── scripts/           # Utility scripts
├── .squad/            # Squad framework
│   ├── agents/        # Agent charters & history
│   ├── skills/        # Reusable skills/plugins
│   └── decisions.md
├── package.json       # Node.js project
└── node_modules/      # Dependencies
```

### Domain-Based SubSquad Mapping

ms-pa has natural domain boundaries that map to SubSquads:

| SubSquad | Team | Folder Scope | CODEOWNERS |
|----------|------|--------------|-----------|
| **Frontend** | UI/CLI developers | `scripts/`, `docs/` (UI artifacts) | `@ms-pa/team-frontend` |
| **Backend** | Agent & skill developers | `.squad/agents/`, `.squad/skills/` | `@ms-pa/team-backend` |
| **Infrastructure** | DevOps, platform eng | `.github/workflows/`, `package.json`, deployment config | `@ms-pa/team-infra` |
| **Documentation** | Knowledge team (Bilbo) | `docs/` (content) | `@ms-pa/team-docs` |

### Recommended CODEOWNERS

```
# ms-pa CODEOWNERS template

# CLI / Frontend
/scripts/              @ms-pa/team-frontend
/docs/tools/          @ms-pa/team-frontend

# Agent & Skill Development
/.squad/agents/       @ms-pa/team-backend
/.squad/skills/       @ms-pa/team-backend

# Infrastructure & CI/CD
/.github/workflows/   @ms-pa/team-infra
/package.json         @ms-pa/team-infra
/package-lock.json    @ms-pa/team-infra

# Documentation (Bilbo as owner)
/docs/research/       @ms-pa/team-docs
/docs/decisions/      @ms-pa/team-docs
/docs/guides/         @ms-pa/team-docs

# Shared (requires approval from backend + docs)
/.squad/decisions.md  @ms-pa/team-backend @ms-pa/team-docs
```

### Label Structure

```json
{
  "labels": [
    {
      "name": "team:frontend",
      "color": "0366d6",
      "description": "Work owned by frontend team (CLI, UI, scripts)"
    },
    {
      "name": "team:backend",
      "color": "2ecc71",
      "description": "Work owned by backend team (agents, skills)"
    },
    {
      "name": "team:infra",
      "color": "d4af37",
      "description": "Work owned by infrastructure team (CI/CD, deps, build)"
    },
    {
      "name": "team:docs",
      "color": "9570bf",
      "description": "Work owned by documentation team (Bilbo, knowledge mgmt)"
    }
  ]
}
```

### Adoption Roadmap

#### Phase 1: Foundation (Week 1)
1. Create `.github/CODEOWNERS` file with initial team assignments
2. Create `.github/labels.json` with team labels
3. Create GitHub teams (`ms-pa/team-frontend`, etc.)
4. Add team members to teams

#### Phase 2: Enforcement (Week 2)
1. Set up branch protection on `main`:
   - Require PR reviews
   - Require CODEOWNERS approval
   - Require status checks (CI/CD)
2. Add `github-label-sync` Action to enforce label spec
3. Add PR template with branch-naming guidance and CODEOWNERS check

#### Phase 3: Awareness & Discipline (Week 3)
1. Update CONTRIBUTING.md with SubSquad process
2. Train teams on:
   - Branch-per-issue naming (`team/issue-{num}`)
   - How to file issues with team labels
   - CODEOWNERS review gates
3. Post team-specific setup guides (each team's folder scope, responsibility)

#### Phase 4: Monitoring (Ongoing)
1. Quarterly audit of CODEOWNERS; confirm team boundaries
2. Monthly audit of label usage; clean up orphaned labels
3. Track PR review turnaround by team; identify bottlenecks
4. Escalate conflicts that arise (e.g., cross-team dependencies)

### Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| Teams resist branch-per-issue discipline | Automate enforcement via branch protection rules; make it a CI/CD gate |
| Labels drift over time | Automated `github-label-sync` ensures spec is always applied |
| CODEOWNERS becomes stale | Include CODEOWNERS updates in PR checklist; quarterly audits |
| Merge conflicts on shared code (`/.squad/`) | Explicit CODEOWNERS for shared files; require approval from both teams |
| New teams added, structure breaks | Document process for adding teams; update CODEOWNERS and label spec templates |

### Recommendation: **ADOPT SubSquads for ms-pa**

**Rationale:**
1. ✅ Repository structure already has domain boundaries (agents, skills, CLI, docs)
2. ✅ Team roles map naturally to SubSquads (frontend, backend, infra, docs)
3. ✅ Squad framework already in use (CODEOWNERS fits naturally)
4. ✅ Current team count (4–5) is in the ideal range for SubSquads
5. ✅ Real-world validation from Tetris experiment shows benefits (clean merges, parallel throughput)
6. ✅ Relatively low friction to adopt (no repo restructuring needed)

**Benefits for ms-pa:**
- **Cleaner issue queues:** Each team sees only their work
- **Automated review routing:** CODEOWNERS ensures right expert reviews changes
- **Parallel productivity:** Agents work independently without context switching
- **Traceable history:** Branch-per-issue keeps git history readable
- **Scalable:** As team count grows, label & CODEOWNERS infrastructure scales automatically

**Timeline:** 3 weeks to full adoption (foundation + enforcement + awareness + first iteration)

---

## Section 8: Key Decision: ms-pa Adoption

### Decision Statement

**ms-pa should adopt the SubSquads pattern for multi-team issue routing and code ownership.**

### Justification

1. **Evidence:** Tamir's Tetris experiment (3 teams, 2 hours, 9 issues closed) demonstrates SubSquads effectiveness at scale
2. **Fit:** ms-pa's repository structure and team size (4–5 people) are ideal for SubSquads
3. **Implementation:** Low risk; no repo restructuring needed; leverages existing Squad framework
4. **Benefit:** Cleaner workflows, automated review routing, reduced context switching, scalable to 8+ teams with additional tooling
5. **Timeline:** 3-week adoption; can be phased (foundation → enforcement → awareness)

### Success Criteria

- ✅ CODEOWNERS file created and enforced on `main` branch
- ✅ Label governance automated via `github-label-sync`
- ✅ Teams practicing branch-per-issue discipline (zero direct commits to `main`)
- ✅ PR review turnaround <24 hours (teams responsive to CODEOWNERS reviews)
- ✅ No merge conflicts on main branch for 2 consecutive weeks

### Next Steps

1. **Immediate:** Create `.github/CODEOWNERS` and `.github/labels.json` (this research provides templates)
2. **Week 1:** Set up GitHub teams, branch protection, label sync Action
3. **Week 2:** Update CONTRIBUTING.md; train teams
4. **Week 3:** Pilot with first cross-team PR; gather feedback
5. **Ongoing:** Monitor and iterate; escalate issues to squad lead

---

## Appendix A: Label Governance Best Practices

### Example Label Specification (`.github/labels.json`)

```json
{
  "labels": [
    {
      "name": "team:frontend",
      "color": "0366d6",
      "description": "CLI, UI, scripts"
    },
    {
      "name": "team:backend",
      "color": "2ecc71",
      "description": "Agents, skills, core logic"
    },
    {
      "name": "team:infra",
      "color": "d4af37",
      "description": "CI/CD, deps, build"
    },
    {
      "name": "team:docs",
      "color": "9570bf",
      "description": "Documentation, knowledge mgmt"
    },
    {
      "name": "type:bug",
      "color": "fc2929",
      "description": "Bug report"
    },
    {
      "name": "type:feature",
      "color": "84b6eb",
      "description": "Feature request"
    },
    {
      "name": "type:research",
      "color": "e6e6e6",
      "description": "Research or investigation"
    }
  ]
}
```

### GitHub Actions Workflow for Label Sync

```yaml
name: Sync Labels
on:
  push:
    paths:
      - .github/labels.json
  schedule:
    - cron: '0 0 * * 0'  # Weekly sync

jobs:
  sync-labels:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: micnncim/action-label-syncer@v1
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
          GITHUB_REPOSITORY: ${{ github.repository }}
          LABEL_JSON: .github/labels.json
```

---

## Appendix B: CODEOWNERS Template with Examples

```
# Team Assignments
/.squad/agents/       @ms-pa/team-backend
/.squad/skills/       @ms-pa/team-backend
/scripts/             @ms-pa/team-frontend
/docs/                @ms-pa/team-docs

# Sensitive / Shared Files
/.github/workflows/   @ms-pa/team-infra
/package.json         @ms-pa/team-infra
/.squad/decisions.md  @ms-pa/team-backend @ms-pa/team-docs

# Fallback (catch-all if no specific match)
*                     @ms-pa/maintainers
```

---

## References

- **Tamir Dresher's Blog Series:**
  - Part 0: Organized by AI — Introduction to multi-team AI workflows
  - Part 1: First Team to Production — Single SubSquad setup
  - Part 2: Organizational Knowledge & Collective — Knowledge management patterns
  - Part 3: Scaling AI Streams — Tetris experiment with 3 teams

- **GitHub Documentation:**
  - [Managing labels](https://docs.github.com/en/issues/using-labels-and-milestones-to-track-work/managing-labels)
  - [About CODEOWNERS](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-code-owners)
  - [Branch protection rules](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/managing-a-branch-protection-rule)

- **Tools & Resources:**
  - [github-label-sync](https://github.com/Financial-Times/github-label-sync) — Automated label management
  - [Squad Framework Documentation](https://bradygaster.github.io/squad/) — SubSquads architecture
  - [Industry Research on Monorepo Scaling](https://www.graphite.com/guides/git-monorepo-best-practices-for-scalability)

---

## Document Metadata

**Research Completed:** 2026-03-23  
**Researcher:** Elrond (ms-pa Researcher agent)  
**Related Issue:** [Issue #24 — SubSquads Architecture Research](https://github.com/jbenami_microsoft/ms-pa/issues/24)  
**Status:** Complete and ready for squad decision  
**Next Action:** Squad lead review and adoption decision

