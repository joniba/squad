---
title: "Upstream Inheritance & Org-Scale Knowledge Sharing"
author: "Elrond"
date: 2026-03-22
status: "complete"
tags:
  - squad
  - upstream-inheritance
  - org-knowledge-sharing
  - scalability
  - collective
version: 1.0
---

# Upstream Inheritance & Org-Scale Knowledge Sharing: Deep Research

## Executive Summary

**Upstream inheritance** is Squad's hierarchical knowledge-sharing mechanism that solves the critical scaling problem: enabling multiple autonomous teams across an organization to share decisions, skills, conventions, and policies without copy-paste duplication. Inspired by Tamir Dresher's "The Collective" concept, upstream inheritance flows knowledge **down** from org → team → repo levels, with **closest-wins resolution** allowing local override while maintaining organizational consistency.

**Key Finding:** This pattern directly addresses Jonathan's setup for ms-pa as a potential org-level upstream host. With proper structure, ms-pa can become the source of truth for org-wide Squad patterns, skills, and decisions that cascade to all team-level and repo-level squads.

---

## Part 1: The Collective — Problem Statement

### The Knowledge Silo Crisis

Tamir Dresher's blog post "We Are the Collective — Sharing Org Knowledge with Upstream Inheritance" (March 12, 2026) articulates the core problem: **isolated AI squads become organizational knowledge silos.**

**Real scenario from Tamir's experience:**
- ConfigurationGeneration repo (his team's .NET SDK) accumulated 70+ Architecture Decision Records, 8 context files, established conventions for error handling, PR format, and test structure.
- Provisioning Wizard repo (same team, different domain) got its own Squad instance with 3 skills but zero knowledge of ConfigurationGeneration's patterns.
- Result: Tamir had to re-explain the same org-wide conventions to every new Squad.

**The underlying organizational truth:** Real engineering orgs don't function at repo level—they function at layers:

| Layer | Scope | Examples | Enforceability |
|-------|-------|----------|---|
| **Organization** | Universal standards across all teams | "Use managed identity." "All APIs follow OpenAPI spec." "Security scanning on every build." | Non-negotiable defaults |
| **Team** | Domain expertise spanning multiple repos | ".NET SDK naming conventions." "ADRs go in docs/decisions/." "Go test patterns are table-driven." | Team-wide consensus |
| **Repository** | Implementation details & quirks | Local config, repo-specific decisions, integration patterns | Repo autonomy |

Without a mechanism to share this knowledge downward, each new Squad starts from scratch.

---

## Part 2: How Upstream Inheritance Works

### The Architecture: Org → Team → Repo Hierarchy

```
Organization Level (.squad/)
├── decisions.md (org-wide policies)
├── skills/ (universal patterns)
│   ├── error-handling/SKILL.md
│   ├── api-conventions/SKILL.md
│   └── security-scanning/SKILL.md
├── routing.md (default escalation paths)
└── casting/policy.json (standard agent roles)
    ↓ (flows down via upstream)
    
Team Level (.squad/)
├── decisions.md (team conventions)
├── skills/ (team-specific patterns)
│   └── dotnet-sdk-patterns/SKILL.md
├── routing.md (team-wide routing)
    ↓ (flows down via upstream)
    
Repository Level (.squad/)
├── decisions.md (repo-specific decisions)
├── agents/ (this repo's team cast)
├── upstream.json (declares upstreams)
```

### Connecting Repos to Upstreams

You connect a repo to its upstreams with the Squad CLI:

```bash
# Initialize Squad in a repo
squad init

# Add org-level upstream (e.g., from GitHub)
squad upstream add https://github.com/my-org/platform-squad.git --name org

# Add team-level upstream (e.g., local directory)
squad upstream add ../team-practices/.squad --name team

# View configured upstreams
squad upstream list

# Sync git-based upstreams (pull latest)
squad upstream sync
```

### Three Upstream Source Types

| Type | Example | Use Case | Sync Behavior |
|------|---------|----------|---|
| **git** | `https://github.com/acme/platform-squad.git` | Remote org/team repos | Cloned to `.squad/_upstream_repos/{name}`, updated via `squad upstream sync` |
| **local** | `../org-practices/.squad` | Sibling directory, monorepo package | Read live at session start; no sync needed |
| **export** | `./exports/squad-export.json` | Snapshot for offline use or version pinning | Validated at session start |

### Closest-Wins Resolution

When Squad starts a session in a repo, it reads:
1. **Org-level upstream** (if configured) → resolves its skills, decisions, routing, etc.
2. **Team-level upstream** (if configured) → resolves its context, **overriding org where conflicts exist**
3. **Repo-level `.squad/`** → resolves local context, **overriding team and org where conflicts exist**
4. **Agent instance** — inherits merged context

**The Resolution Rule: Later entries override earlier ones.**

```
upstream.json declares:
  1. org-upstream
  2. team-upstream
  3. (no entry for repo — repo's own .squad/ is implicit last)

Resolution order (closest wins):
  org-level     (lowest priority)
    ↓
  team-level    (overrides org)
    ↓
  repo-level    (overrides team and org)
    ↓
  agent instance (inherits merged context)
```

**Example conflict resolution:**
- Org says: "All TypeScript services must use Node 18+."
- Team says: "Our Go CLI tools run on Node 14."
- Repo says: (nothing; inherits team decision)
- **Outcome:** Go CLI uses Node 14; other TypeScript services use Node 18+.

**Important distinction:** Closest-wins is **not a security mechanism**. It's a **convenience mechanism** for layering policies. Org-level decisions can technically be overridden at repo level if the upstream ordering allows. True enforcement (org policies that cannot be overridden) requires additional governance—e.g., a policy that repo-level `.squad/` entries are never allowed to supersede org entries.

---

## Part 3: What Gets Inherited?

At session start, Squad reads these artifacts from each upstream's `.squad/` directory:

### ✅ Inherited (Shared Downward)

| Artifact | Location | What It Contains | Inheritance Behavior |
|----------|----------|------------------|---|
| **Skills** | `.squad/skills/*/SKILL.md` | Reusable patterns, conventions, confidence levels | All upstream skills available; repo can add local skills |
| **Decisions** | `.squad/decisions.md` | Policies, principles, conventions | Merged hierarchically; closest-wins |
| **Wisdom** | `.squad/identity/wisdom.md` | Accumulated lessons, domain knowledge | Merged hierarchically |
| **Casting Policy** | `.squad/casting/policy.json` | Default team shapes, agent roles | Merged; repo can override |
| **Routing Rules** | `.squad/routing.md` | Work distribution, escalation paths | Merged; repo can override |

### ❌ NOT Inherited (Stay Local)

| Artifact | Why It Stays Local |
|----------|---|
| **Agent Identities** | Each repo casts its own team (humans + AI agents). Can't inherit team membership. |
| **History** | Conversation history is repo-specific; doesn't make sense to share. |
| **Orchestration Logs** | Task state, artifacts, session data are repo-specific. |

**Analogy:** Upstream inheritance is like the Borg Collective—shared directives, tactics, and knowledge flow down. Individual drones have their own hardware and local task state.

---

## Part 4: Skills & the Confidence Lifecycle

### Skill Anatomy

A skill is a reusable pattern stored in `.squad/skills/{skill-name}/SKILL.md`:

```yaml
name: pr-format
description: Standard PR format for team repos
confidence: high
source: "Extracted from multiple high-functioning repos"
domain: code-review
steps:
  - title follows conventional commits format
  - description includes context, changes, testing sections
  - checklist includes review items (security, docs, tests)
context: |
  PR format is enforced at code review time. All team repos follow this pattern.
anti_patterns: |
  - Do not use narrative commit messages; use conventional commits
  - Do not skip the checklist
examples: |
  - [Example PR](link)
```

### Confidence Lifecycle: Low → Medium → High

Skills mature through **battle-testing** and **human validation**, not age:

| Confidence | Criteria | Examples | Action |
|-----------|----------|----------|--------|
| **Low** | First observation; single repo; pattern unvalidated | "This PR format seems to work well." | Document locally; test across more contexts |
| **Medium** | Used successfully in multiple contexts; looks like a real pattern | Pattern works in 3+ repos; team members acknowledge it | Discuss with team; consider promotion |
| **High** | Validated across repos; reviewed by humans; established practice | 6+ repos using it; zero failures; team consensus | **Promote upstream** → commit to team/org repo |

### Skill Promotion Workflow

```
Local Skill (confidence: low)
  ↓ (used successfully in multiple repos)
  ↓ (confidence → medium)
Validated Skill (confidence: medium)
  ↓ (human review & team sign-off)
  ↓ (confidence → high)
Battle-Tested Skill (confidence: high)
  ↓ (explicit promotion decision)
  ↓ (squad export --skill {name} --output {file})
Export Snapshot
  ↓ (commit to team/org upstream repo)
  ↓ (squad upstream sync in downstream repos)
Org-Level Skill (inherited by all downstream squads)
```

**Key insight:** Skill promotion is **one-directional: UP is manual, DOWN is automatic.**

- **Upward (manual):** Export a skill from repo → commit to upstream → explicit decision
- **Downward (automatic):** Squad reads upstream at session start → skills available immediately

This ensures upstream knowledge is **curated, not crowdsourced**. Promotion is like code review for organizational patterns.

---

## Part 5: Real Example — ConfigurationGeneration + Provisioning Wizard

Tamir's actual scenario illustrates the full workflow:

### Before Upstream Inheritance

**ConfigurationGeneration** (Team's .NET SDK):
- 70+ ADRs documenting decisions
- 8 context files explaining architecture, patterns, error handling
- Conventions for PR format, test structure, naming

**Provisioning Wizard** (Team's provisioning tool):
- 3 local skills (`docs-hygiene`, `pr-format`, `fabric-rti-mcp`)
- Zero awareness of ConfigurationGeneration's patterns
- Teams must re-explain conventions on every new repo

**Setup time:** Hours of manual context building for each new repo.

### After Upstream Inheritance

**Platform Squad** (Team-level upstream):
- Extracted `.squad/skills/pr-format/SKILL.md` (confidence: high)
- Extracted `.squad/skills/error-handling/SKILL.md` (confidence: high)
- Created `.squad/decisions.md` with team conventions
- Created `.squad/routing.md` with team escalation patterns

**New Repo Setup:**
```bash
squad init
squad upstream add https://github.com/my-org/team-squad.git --name team
squad upstream sync
```

**Result:** Every new repo inherits all team patterns instantly. No manual copy-paste. No re-explanation.

---

## Part 6: Policy Enforcement & Override Scenarios

### When Can You Override?

**Default behavior:** Closest-wins means repo CAN override team, team CAN override org.

**Scenarios:**

| Scenario | Resolution | Policy Implication |
|----------|-----------|---|
| Org says "TypeScript only"; Repo is Go. | Repo overrides (closest wins) | Org should establish why this exception is allowed or require pre-approval |
| Org says "ADRs mandatory"; Repo skips ADRs. | Repo overrides (closest wins) | Governance gap: either accept the override or enforce at tooling level |
| Team says "PR format A"; Org says "PR format B". | Team overrides (closest wins) | Team autonomy is honored; org should review if consistency is required |

### When Can You NOT Override?

**Limitation:** Squad's closest-wins is a **convenience mechanism, not a security mechanism**. There is no built-in "org policies cannot be overridden" enforcement.

**To enforce non-override org policies, you would need:**
1. **Configuration governance:** Org-level policy explicitly states "no repo-level `.squad/` entries may supersede org entries."
2. **Tooling enforcement:** CI/CD pre-merge checks that validate no overrides exist.
3. **Human governance:** Code review enforces the policy; maintainers catch violations.

**Practical implications for Jonathan's setup:**
- If ms-pa is the org upstream, it should contain **policies, not constraints**.
- Example org policies: "All PRs follow conventional commits", "Security scanning is mandatory", "ADRs live in docs/decisions/"
- Repo-level autonomy: Teams can override these IF the decision is documented and justified (in their own `.squad/decisions.md`).
- Non-overrideable org policies: Would require explicit governance agreements + CI enforcement.

---

## Part 7: Skills Sharing & Versioning Challenges

### Skill Versioning Problem

Skills don't have explicit versions today. When you promote a skill upstream and later improve it:

```
Team Squad
  .squad/skills/api-error-handling/SKILL.md (v1)
    ↓ export
    ↓ commit to org upstream
Org Squad
  .squad/skills/api-error-handling/SKILL.md (v1)
    ↓ (repos sync)
All Downstream Squads
  inherit v1
  ↓ (months later)
Team Squad improves to v2
  .squad/skills/api-error-handling/SKILL.md (v2)
    ↓ export
    ↓ commit to org upstream
Org Squad
  .squad/skills/api-error-handling/SKILL.md (v2)
    ↓ (repos sync)
All Downstream Squads
  inherit v2
```

**Gap:** No explicit versioning, backwards compatibility, or deprecation path.

**Current workaround:** Confidence levels (low → medium → high) + documentation serve as implicit versioning. Skills with `confidence: high` are stable; `medium` are evolving.

**Future enhancement (from Dresher's blog):** Auto-synced upstream that continuously updates downstream repos with latest skill versions, eliminating stale knowledge.

---

## Part 8: Practical Implications for Jonathan's ms-pa Setup

### Current State

Jonathan's project hierarchy:
```
personal-assistant-squad (ms-pa) ← TEAM-LEVEL repo today
  .squad/
    agents/ (LotR cast: Gandalf, Elrond, Bilbo, Gimli, etc.)
    decisions.md (squad governance, user directives)
    skills/ (teams-monitor, news-broadcasting, secrets-management, etc.)
    templates/ (squad.agent.md, spawn templates)
```

### Vision: ms-pa as Org-Level Upstream

**Question from issue:** Should ms-pa serve as org upstream for other squads?

**Answer: Yes, conditionally.**

**Proposed structure:**

```
Organization Level (ms-pa repo)
  .squad/
    decisions.md
      - All user directives (copilot CLI usage, scripting philosophy, work source policy, etc.)
      - Teams Watchdog Architecture (Gandalf decision)
      - Coffee-Ratings Squad-Infra Audit findings
      - Reviewer agent hiring patterns
    skills/
      - teams-monitor (proven WorkIQ patterns)
      - news-broadcasting (Teams webhook delivery)
      - secrets-management (Windows Credential Manager patterns)
      - squad-infra-bootstrap (from coffee-ratings audit)
      - icm-investigation (deep investigation methodology)
    routing.md (escalation patterns)
    casting/policy.json (LotR agent archetypes)
    identity/wisdom.md (lessons from squad-skills scan, worktree research, etc.)

Future Team-Level Squads
  .squad/upstream.json
    - references https://github.com/jbenami_microsoft/ms-pa.git --name org
    - inherits all org decisions, skills, routings
    - adds team-specific decisions, skills, routing overrides
```

### Benefits

1. **Knowledge consolidation:** Teams don't rediscover Squad patterns; they inherit ms-pa's accumulated wisdom.
2. **Standardized governance:** User directives (policy enforcement, scripting philosophy, work source policy) flow to all downstream squads.
3. **Skill reuse:** teams-monitor, news-broadcasting, secrets-management become available everywhere.
4. **Onboarding acceleration:** New team squads inherit the full ms-pa context on day one.
5. **Consistency with variation:** Team squads can override org decisions locally when needed (documented in their own decisions.md).

### Gotchas & Risks

1. **Scope creep:** ms-pa contains repo-specific skills (Gandalf's Teams watchdog orchestration) that shouldn't be org-level. Must cleanly separate org-level from repo-level.
2. **Agent identity sharing:** Org-level `.squad/casting/policy.json` should define archetypes (Lead, Code Expert, Researcher, Scribe, etc.), NOT specific named agents. Individual squads cast their own teams.
3. **Stale knowledge:** Without continuous upstream sync automation, downstream squads might inherit outdated skills. Versioning & deprecation paths needed.
4. **Override governance:** If ms-pa becomes org upstream, need explicit policy on what can/cannot be overridden at team/repo level.

---

## Part 9: Git-Based Upstreams Implementation Details

### How Git Upstreams Work Under the Hood

```bash
squad upstream add https://github.com/my-org/platform-squad.git --name org
```

**What happens:**

1. Squad clones repo to `.squad/_upstream_repos/org/`
2. Stores reference in `.squad/upstream.json`:
   ```json
   {
     "upstreams": [
       {
         "name": "org",
         "type": "git",
         "source": "https://github.com/my-org/platform-squad.git",
         "ref": "main",
         "added_at": "2026-03-22T12:00:00Z",
         "last_synced": null
       }
     ]
   }
   ```
3. Adds `.squad/_upstream_repos/` to `.gitignore` (already done by Squad)
4. On each `squad upstream sync org`: runs `git pull --ff-only` in the cached clone

**Sync behavior:**
- Manual: `squad upstream sync` or `squad upstream sync {name}`
- Fast-forward only (no rebases; safe to run anytime)
- Updates `last_synced` timestamp
- Errors if local edits exist (prevents data loss)

**Session start behavior:**
- Squad reads `.squad/upstream.json`
- For each git upstream, reads from `.squad/_upstream_repos/{name}/.squad/`
- Merges content using closest-wins hierarchy
- Passes inherited context to spawned agents

---

## Part 10: Org-Scale Knowledge Sharing Patterns

### Pattern 1: The Platform Team Model

**Structure:**
- Central platform team maintains org upstream (platform-squad repo)
- All product teams reference it as org upstream
- Each product team can add its own team-level upstream
- Individual repos inherit from both

**Knowledge flow:**
```
Platform Team (org upstream)
  ↓ decisions (shared standards)
  ↓ skills (common patterns)
  ↓ routing (escalation rules)
Product Team A (team upstream)
  ↓ team-specific decisions
Product Repo A1 (references both)
Product Repo A2 (references both)
Product Team B (team upstream)
Product Repo B1 (references both)
```

**Real example:** Infrastructure platform team defines "all services use managed identity", "APIs follow OpenAPI spec", "PR format is conventional commits". Every product repo inherits these on day one.

### Pattern 2: The Monorepo + Satellite Model

**Structure:**
- Monorepo contains org-level Squad config (`.squad/` at root)
- Satellite repos (separate teams/projects) add monorepo as local upstream
- Enables knowledge sharing across architectural boundaries

```
monorepo/
  .squad/ (org-level)
    skills/ (universal patterns)
    decisions.md (org-wide policies)
    
../satellite-repo-a/
  .squad/upstream.json → references ../monorepo/.squad
  
../satellite-repo-b/
  .squad/upstream.json → references ../monorepo/.squad
```

### Pattern 3: The Experience Base (Library of Patterns)

**Structure:**
- Dedicated "patterns repo" (e.g., squad-skills) contains only `.squad/skills/`
- No actual codebase; just documented patterns
- All teams reference it as a skills library

```
squad-skills-library/
  .squad/
    skills/
      error-handling/ (🟢 high confidence)
      api-design/ (🟡 medium confidence)
      testing-patterns/ (🟢 high confidence)
      docker-patterns/ (🟡 medium confidence)
    decisions.md (skill governance)

Every team repo:
  .squad/upstream.json → references squad-skills-library
```

---

## Part 11: Gaps, Gotchas, & Future Directions

### Known Limitations

1. **No explicit versioning:** Skills don't have version numbers; confidence levels serve as proxy.
2. **No deprecation path:** When a skill becomes obsolete, there's no mechanism to mark it deprecated or retire it.
3. **No auto-sync:** Upstream sync is manual today (`squad upstream sync`); no scheduled or event-driven sync.
4. **Weak enforcement:** Closest-wins is convenience, not security; org policies can be overridden without tooling enforcement.
5. **No conflict detection:** If two upstreams provide conflicting decisions, closest-wins wins silently; no warning.
6. **Limited export:** Export is snapshot-only today; no continuous feed or publication mechanism.

### Gaps for Jonathan's Use Case

1. **LotR agent archetypes:** ms-pa's casting/policy.json mixes specific agent names (Gandalf, Elrond, Bilbo) with archetypes (Coordinator, Researcher, Scribe). Org-level should define archetypes only.
2. **Teams watchdog infrastructure:** Specific to ms-pa's operations; shouldn't be org-level. Should be in repo-level `.squad/skills/`.
3. **Decision scope:** Need to separate decisions that flow down (user directives, governance) from those that don't (squad-infra audit findings).

### Future Enhancements (from Dresher's roadmap)

1. **Auto-sync upstream:** Continuous synchronization where upstream changes flow to downstream repos automatically.
2. **Versioning:** Explicit skill versions, deprecation markers, backwards compatibility guarantees.
3. **Conflict resolution UI:** Tools to visualize conflicts when multiple upstreams provide overlapping content.
4. **Policy enforcement:** Org-level policies marked as "non-overrideable" with CI validation.
5. **Upstream publication:** Skills promoted upstream automatically trigger notifications to all downstream squads.

---

## Part 12: Closest-Wins Resolution Algorithm (Deep Dive)

### Pseudocode

```
function resolveContext(repo):
  merged = {}
  
  # Read org upstream
  if upstream.org exists:
    merged.decisions = org.decisions.md
    merged.skills = org.skills/*
    merged.routing = org.routing.md
  
  # Read team upstream (overrides org)
  if upstream.team exists:
    merged.decisions = merge(merged.decisions, team.decisions.md)
    merged.skills = merge(merged.skills, team.skills/*)
    merged.routing = merge(merged.routing, team.routing.md)
  
  # Read repo local config (overrides team & org)
  merged.decisions = merge(merged.decisions, repo/.squad/decisions.md)
  merged.skills = merge(merged.skills, repo/.squad/skills/*)
  merged.routing = merge(merged.routing, repo/.squad/routing.md)
  
  return merged
```

### Merge Semantics

**For decisions.md (text documents):**
- Concatenate hierarchically
- Later entries shadow earlier ones by section
- Human review resolves conflicts

**For skills/ (directory of SKILL.md files):**
- Union all skills from all levels
- If same skill appears in org & repo: repo version wins
- Metadata (confidence, domain) also follow closest-wins

**For routing.md (routing rules):**
- Merge rule sets
- Later rules override earlier rules for same trigger
- If same trigger in org & repo: repo rule wins

---

## Summary: Decision Points for Jonathan

### Should ms-pa Become Org Upstream?

**Recommendation: Yes, with refactoring.**

1. **Extract org-level content** from `.squad/`:
   - User directives (copilot CLI, scripting philosophy, work source policy) → `decisions.md`
   - Reusable skills (teams-monitor, news-broadcasting, secrets-management) → `skills/`
   - LotR archetypes (Lead, Researcher, Scribe, Reviewer, etc.) → `casting/policy.json`
   - Accumulated lessons → `identity/wisdom.md`

2. **Keep repo-level content** in `.squad/`:
   - Specific agent castings (Gandalf, Elrond, Bilbo, etc.) → `agents/`
   - Teams Watchdog Architecture decision → `decisions.md` (repo-specific)
   - ms-pa-specific skills → `skills/`

3. **Establish governance** for downstream squads:
   - Document what can be overridden (team routings, local decisions)
   - Document what should not be overridden (user directives, org-level policies)
   - Create CI checks if strict enforcement needed

4. **Test with first downstream squad** (e.g., coffee-ratings port):
   - Add ms-pa as org upstream
   - Verify skills/decisions inherit correctly
   - Identify any missing or conflicting content
   - Iterate before rolling out to other teams

### Next Steps

1. **Elrond → Bilbo**: Document this research in `docs/upstream-inheritance.md` with architecture diagrams, decision matrices, and implementation checklist.
2. **Gandalf → Decision**: Add ADR to `.squad/decisions.md` proposing ms-pa as org upstream with governance boundaries.
3. **Squad**: Refactor `.squad/` to separate org-level from repo-level content.
4. **Gimli**: Implement org upstream setup (create `upstream.json` references in test squads).
5. **Test & iterate**: Run first downstream squad with ms-pa as upstream; gather feedback; refine.

---

## Sources & References

### Primary Sources

1. **Tamir Dresher Blog Series (Scaling AI-Native Software Engineering):**
   - Part 1: "Resistance is Futile — Your First AI Engineering Team" (March 11, 2026)
   - Part 2: "The Collective — Organizational Knowledge for AI Teams" (March 12, 2026)
   - Part 0: "Organized by AI — How Squad Changed My Daily Workflow" (March 10, 2026)
   - Series roadmap includes Part 3 (SubSquads) and Part 4 (Distributed Systems)

2. **Squad CLI Documentation:**
   - Upstream Inheritance feature docs: https://bradygaster.github.io/squad/docs/features/upstream-inheritance/
   - Skills system: https://bradygaster.github.io/squad/docs/features/skills/
   - CLI reference: https://bradygaster.github.io/squad/docs/reference/cli/

3. **GitHub Repositories:**
   - bradygaster/squad (Squad framework)
   - tamirdresher/squad-personal-demo (Tamir's demo repo)
   - tamirdresher/squad-skills (20+ plugin skills library)

### Secondary Sources

1. **Jonathan's squad decisions** (`.squad/decisions.md`):
   - User directives (copilot CLI, scripting philosophy, work source policy)
   - Teams Watchdog Architecture
   - Skills Scan findings
   - Coffee-Ratings Audit proposal
   - Reviewer agent hiring patterns

2. **Elrond's prior research** (`.squad/agents/elrond/history.md`):
   - Teams-monitor and news-broadcasting plugin patterns
   - WorkIQ polling characteristics
   - Squad infrastructure research findings

---

## Appendix A: Upstream Configuration Examples

### Example 1: Basic Two-Tier Setup

```bash
# In product repo
squad init
squad upstream add https://github.com/acme/org-platform-squad.git --name org
squad upstream list
```

Result in `.squad/upstream.json`:
```json
{
  "upstreams": [
    {
      "name": "org",
      "type": "git",
      "source": "https://github.com/acme/org-platform-squad.git",
      "ref": "main",
      "added_at": "2026-03-22T12:00:00Z",
      "last_synced": "2026-03-22T12:05:00Z"
    }
  ]
}
```

### Example 2: Three-Tier Setup (Org + Team)

```bash
# In product repo
squad init
squad upstream add https://github.com/acme/org-platform-squad.git --name org
squad upstream add https://github.com/acme/product-team-squad.git --name team
squad upstream sync
```

Result in `.squad/upstream.json`:
```json
{
  "upstreams": [
    {
      "name": "org",
      "type": "git",
      "source": "https://github.com/acme/org-platform-squad.git",
      "ref": "main"
    },
    {
      "name": "team",
      "type": "git",
      "source": "https://github.com/acme/product-team-squad.git",
      "ref": "main"
    }
  ]
}
```

### Example 3: Local Upstream (Monorepo)

```bash
# In satellite repo adjacent to monorepo
squad init
squad upstream add ../monorepo/.squad --name org-local
```

Result in `.squad/upstream.json`:
```json
{
  "upstreams": [
    {
      "name": "org-local",
      "type": "local",
      "source": "../monorepo/.squad",
      "added_at": "2026-03-22T12:00:00Z",
      "last_synced": null
    }
  ]
}
```

---

## Appendix B: Skill Promotion Checklist

Before promoting a skill from repo-level to org-level:

- [ ] Skill has `confidence: high`
- [ ] Skill has been tested in 3+ different contexts
- [ ] Skill has been used by 2+ different agents
- [ ] No known anti-patterns or edge cases
- [ ] Documentation is complete (context, steps, examples, anti-patterns)
- [ ] Team has reviewed and approved promotion
- [ ] Skill is exported: `squad export --skill {name} --output {file}`
- [ ] Exported skill is committed to org upstream repo
- [ ] Org upstream is synced: `squad upstream sync`
- [ ] Downstream repos run `squad upstream sync` and verify skill is inherited

---

**Research completed by:** Elrond, Researcher  
**Date:** 2026-03-22  
**Requested by:** Jonathan (via Ralph YOLO mode), GitHub Issue #25
