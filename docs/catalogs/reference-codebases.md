---
title: "Reference Codebases — Working Solutions Index"
date: 2026-03-23
author: bilbo
category: catalog
tags: [reference, codebase, personal-ai, event-triggers, icm, pr-review, skills, mcp, research-source]
status: final
related_docs:
  - docs/research/icm-scan-end-to-end-research.md
  - docs/research/ado-file-access-research.md
---

# Reference Codebases — Working Solutions Index

When researching solutions for the squad, **look here first**. These codebases contain tested, working implementations of features we need.

---

## 1. Personal AI Companion

| Field | Value |
|-------|-------|
| **Path** | `C:\dev\defender\MDC-AI-Shared\extensions\personal-ai\` |
| **What** | VS Code extension — full personal AI assistant with skills, agents, IcM, dashboards, engineering pipeline, notifications |
| **Why it matters** | This is the upstream system our squad is modeled after. Many problems we face are already solved here. |
| **Language** | TypeScript + PowerShell scripts |

### Directory Map

```
personal-ai/
├── .ai/
│   ├── plans/
│   │   ├── event-triggers/       ← HOW TO: scheduled polling + agent spawning
│   │   │   ├── phase1-notification-service.md
│   │   │   ├── phase2-trigger-engine.md
│   │   │   ├── phase3-icm-poller.md    ← IcM polling solution
│   │   │   ├── phase4-pr-new-poller.md
│   │   │   └── phase5-pr-comment-poller.md
│   │   ├── pr-review/            ← HOW TO: PR review orchestration
│   │   │   ├── phase1-submodule-agent-loader.md
│   │   │   ├── phase2-pr-review-tool-service.md
│   │   │   ├── phase3-orchestrator-core.md
│   │   │   ├── phase4-anti-drift-verification.md
│   │   │   └── phase5-report-publishing.md
│   │   └── teams-notification-integration.md
│   └── semantic-model/           ← 20-doc semantic graph (nodes, edges, schema)
│       └── 000-index.md ... 190-patterns.md
├── docs/                         ← 16 user/developer guides
│   ├── ICM-INVESTIGATIONS.md     ← 6-stage IcM pipeline
│   ├── TRIGGERED-ACTIONS.md      ← Event-driven automation
│   ├── CRON-AUTOMATION.md        ← Scheduling patterns
│   └── MCP-CONFIGURATION.md     ← MCP server setup
├── mcp-configs/                  ← 6 MCP server configs (ADO, Azure, GitHub, WorkIQ, 1ES, filesystem)
├── scripts/                      ← Utility scripts (Teams, screenshots)
├── skills/                       ← 19 AI skills
│   ├── icm-investigator/         ← FULL IcM pipeline (6 agents, 3 commands)
│   ├── pr-reviewer/              ← PR review pipeline (3 agents)
│   ├── work-item-engineer/       ← Code gen pipeline (6 agents)
│   ├── cloud-fleet-delegate/     ← Multi-repo delegation
│   ├── kusto-analyze/            ← Data analysis (6 agents)
│   ├── dashboard-creator/
│   ├── docs-manager/
│   └── ...
└── src/                          ← 23 TypeScript feature modules (149 files)
    ├── icm/                      ← IcM client, cache, investigation logic (20 files)
    ├── cron/                     ← CronJobManager, presets (10 files)
    ├── notifications/            ← Notification system (5 files)
    ├── skills/                   ← Skills framework (9 files)
    └── ...
```

### What's Solved Here (Solutions We Can Borrow)

| Problem We Have | Solution Location | Status |
|----------------|-------------------|--------|
| **ICM scanning from standalone process** | `.ai/plans/event-triggers/phase3-icm-poller.md` + `src/icm/` | ✅ Working |
| **PR review file access** | `.ai/plans/pr-review/` + `skills/pr-reviewer/` | ✅ Working |
| **Event-driven triggers** | `.ai/plans/event-triggers/` + `src/cron/` | ✅ Working |
| **Teams notifications** | `.ai/plans/teams-notification-integration.md` + `src/notifications/` | ✅ Working |
| **MCP tool configuration** | `mcp-configs/` (6 configs) | ✅ Working |
| **Multi-agent skill pipelines** | `skills/*/agents/*.md` (9 skills with agents) | ✅ Working |
| **Semantic model / knowledge graph** | `.ai/semantic-model/` | ✅ Working |
| **Dashboard creation** | `skills/dashboard-creator/` + `src/dashboard/` (31 files) | ✅ Working |
| **Cron scheduling patterns** | `docs/CRON-AUTOMATION.md` + `src/cron/` | ✅ Working |

### How to Use This Reference

**For Elrond (research):**
1. Read the relevant `.ai/plans/` phase docs first — they explain the design
2. Then find the implementation in `src/` or `skills/` — that's the working code
3. Extract the patterns that work and propose them for our squad

**For Gimli (implementation):**
1. Read Elrond's research referencing this codebase
2. Look at the actual TypeScript in `src/` for implementation patterns
3. Adapt to our PowerShell-based squad architecture

---

## 2. Snap Squad (paulyuk/snap-squad)

| Field | Value |
|-------|-------|
| **Repository** | [paulyuk/snap-squad](https://github.com/paulyuk/snap-squad) |
| **What** | CLI tool to instantly scaffold AI agent squads with pre-configured presets, eliminating cold-start setup friction |
| **Why it matters** | Demonstrates battle-tested preset architectures for multi-agent teams (default, fast, mentors, specialists). Reference for squad design patterns and team composition. |
| **Language** | TypeScript/Node.js |
| **Key author** | Paul Yuk |
| **Last reviewed** | 2026-03-23 |

### Purpose & Capability

Snap Squad solves the friction of manually setting up AI agent teams. Instead of answering interview questions or manually configuring agents, one command generates a complete squad with:

- Pre-written agent charters (Architect, Coder, Tester, DevRel, etc.)
- Routing rules and decision templates
- MCP tool configurations
- GitHub Copilot hook chain (AGENTS.md + CLAUDE.md + copilot-instructions.md)

### Relevance to Our Work

**Why Jonathan tracks this:**
- Our squad (`pa-squad`) is built on the same Squad runtime framework
- Snap Squad's preset architecture patterns inform how we structure teams
- It demonstrates successful multi-agent routing and charter design
- Reference for how to bootstrap new squads quickly

### Key Features

- **4 Preset Templates:** Default (generalist), Fast (speed), Mentors (learning), Specialists (precision)
- **One-command initialization:** `npx snap-squad init [preset]` with optional description
- **Smart routing:** Pre-baked rules for delegating work to the right agent
- **Safe regeneration:** `--force` preserves existing journals and decisions; `--reset-all` for clean slate
- **AI-aware instructions:** Generated AGENTS.md teaches any AI tool how to use the squad

### Directory Structure

```
snap-squad/
├── README.md              ← Quick start guide
├── SPEC.md                ← Design specification (warm-start architecture)
├── AGENTS.md              ← Instructions for AI agents using the squad
├── CLAUDE.md              ← Session memory for Claude/Copilot
├── CONTRIBUTING.md        ← Contribution guidelines
├── JOURNAL.md             ← Build journal (steering history)
├── docs/
│   └── presets/           ← Detailed preset architectures (default, fast, mentors, specialists)
├── src/                   ← TypeScript implementation
├── scripts/               ← Build and utility scripts
├── test/                  ← Test suite (vitest)
├── evals/                 ← Evaluation configurations
└── .squad/                ← Snap Squad's own team definition
```

### How to Use This Reference

**For Elrond (research):**
- Study the preset definitions in `docs/presets/` to understand agent role design
- Review `SPEC.md` for the "warm-start" architecture pattern
- Examine `AGENTS.md` for how to teach AI agents about squad structure

**For Gimli (implementation):**
- Reference the TypeScript patterns in `src/` for CLI argument parsing and file generation
- Study the template system for how charters are generated
- Look at the router configuration patterns for multi-agent delegation

**For Bilbo (documentation):**
- The README structure provides a good template for explaining CLI-driven tools
- The preset pages show how to document different agent roles and their responsibilities
- AGENTS.md demonstrates the clearest way to write AI-aware instructions

### What's Solved Here (Patterns We Can Borrow)

| Pattern | Location | Status |
|---------|----------|--------|
| **Agent charter templates** | `docs/presets/{preset}.md` | ✅ Documented |
| **Multi-agent routing** | SPEC.md + `src/` | ✅ Working |
| **Preset architecture composition** | `docs/presets/` (4 presets) | ✅ Proven |
| **AI-readable team instructions** | `AGENTS.md` | ✅ Pattern |
| **Safe config regeneration** | Implementation in `src/` | ✅ Working |
| **Squad CLI bootstrapping** | `package.json` + CLI command | ✅ Reference |

---

## 3. DevOps AI (kpiteira/devops-ai)

| Field | Value |
|-------|-------|
| **Repository** | [kpiteira/devops-ai](https://github.com/kpiteira/devops-ai) |
| **What** | Development workflow skills library + kinfra CLI for isolated development environments (git worktrees, Docker sandbox slots with port isolation, shared observability stack) |
| **Why it matters** | Reference architecture for AI-driven development workflows with design-to-implementation pipeline. Skills-based prompt system demonstrates portable AI-tool integration patterns. |
| **Language** | Python (kinfra CLI) + Markdown (skills) |
| **Key author** | Kpiteira |
| **Last reviewed** | 2026-03-24 |

### Purpose & Capability

DevOps AI provides a complete workflow for AI-assisted software engineering, split into two components:

1. **Skills** — Markdown prompts that guide AI tools (Claude Code, Codex CLI, GitHub Copilot CLI) through proven development workflows
2. **kinfra** — Python CLI managing git worktrees, Docker sandbox slots with port isolation, and shared observability (Jaeger/Grafana/Prometheus)

The typical workflow is: **Design** → **Plan** → **Build**, with each phase producing artifacts consumed by the next.

### Relevance to Our Work

**Why this matters for pa-squad:**
- Demonstrates AI skills design patterns (portable across Claude/Codex/Copilot via Agent Skills standard)
- Sandbox slot management with port isolation solves multi-project Docker coordination (squad could adopt this)
- Shared observability stack pattern for multi-agent systems
- Worktree conventions (`spec/<feature>`, `impl/<feature>-<milestone>`) applicable to our task workflow
- Skills-as-markdown approach is compatible with how we structure Copilot skills

### Key Features

| Feature | Location | Status |
|---------|----------|--------|
| **Design-to-implementation pipeline** | `skills/kdesign/`, `skills/kplan/`, `skills/kbuild/` | ✅ Working |
| **Git worktree lifecycle** | `kinfra impl/done/worktrees` commands | ✅ Working |
| **Docker sandbox slots** | `kinfra impl` with port isolation (slot-based formula) | ✅ Working |
| **Port allocation registry** | `~/.devops-ai/registry.json` (global, per-machine) | ✅ Working |
| **Shared observability** | Jaeger/Grafana/Prometheus on `4xxxx` ports | ✅ Working |
| **Issue workflow** | `/kissue 42` → fetch, branch, TDD, PR | ✅ Working |
| **PR review workflow** | `/kreview` → assess comments, implement fixes | ✅ Working |
| **Quality infrastructure** | Justfile, Makefile, `.githooks/pre-commit`, GitHub CI/CD | ✅ Working |

### Directory Structure

```
devops-ai/
├── src/devops_ai/           # kinfra CLI (Python)
│   ├── cli/                 # Typer command modules (init, spec, impl, done, status)
│   ├── worktree.py          # Git worktree lifecycle
│   ├── sandbox.py           # Docker sandbox file generation
│   ├── ports.py             # Port allocation with conflict detection
│   ├── registry.py          # Global slot registry
│   ├── observability.py     # Jaeger/Grafana/Prometheus management
│   └── agent_deck.py        # Optional agent-deck integration
├── skills/                  # AI tool skills (symlinked on install)
│   ├── kdesign/             # Design + validation workflow
│   ├── kplan/               # Task expansion and architecture alignment
│   ├── kbuild/              # TDD task execution and milestone orchestration
│   ├── kissue/              # GitHub issue → branch → TDD → PR
│   ├── kreview/             # PR review comment assessment and implementation
│   ├── kworktree/           # Worktree/sandbox management
│   └── kinfra-onboard/      # Project onboarding (analyze → propose → execute → verify)
├── rules/                   # Shared principles (auto-loaded to ~/.claude/rules/)
├── templates/               # Project config and observability templates
└── tests/                   # Unit and E2E tests
```

### How to Use This Reference

**For Elrond (research):**
- Study how skills are structured (`skills/*/SKILL.md`) — they're pure markdown prompts with conditional sections
- Examine `kinfra` command patterns to understand worktree and sandbox lifecycle
- Review `src/devops_ai/` for Python CLI best practices (Typer, structured state management)
- The port allocation strategy (`base_port + slot_id`) could solve our own multi-project Docker conflicts

**For Gimli (implementation):**
- Reference the kinfra CLI structure if we want to build similar sandbox/worktree tooling
- Study how skills parameterize project-specific values (via `.devops-ai/project.md`)
- The Docker Compose parameterization patterns in `compose.py` are directly reusable

**For Bilbo (documentation):**
- The skills-as-markdown pattern shows how to make AI instructions portable across tools
- Installation script pattern (`install.sh` → symlinks to `~/.claude/skills/`) is clean and upgradeable
- Onboarding skill design (analyze → propose → execute → verify) is a good UX template

### What's Solved Here (Patterns We Can Borrow)

| Pattern | Location | Status | Relevance |
|---------|----------|--------|-----------|
| **Workflo w orchestration via skills** | `skills/k{design,plan,build,issue,review}/` | ✅ Proven | Direct reference for AI skill design |
| **Git worktree conventions** | `kinfra impl/done` + `spec/`/`impl/` naming | ✅ Working | Applicable to our task workflow |
| **Docker sandbox port isolation** | `src/devops_ai/ports.py` + registry | ✅ Working | Could solve squad multi-project Docker conflicts |
| **Shared observability stack** | `kinfra observability up/down/status` | ✅ Working | Useful for squad observability scaling |
| **Skill portability (Agent Skills standard)** | Cross-tool via `agentskills.io` spec | ✅ Pattern | Ensures our skills work in Claude/Codex/Copilot CLI |
| **Quality infrastructure generation** | `kinfra init` produces Justfile/Makefile/CI/CD | ✅ Working | Template reference for new projects |
| **Project config as prompt** | `.devops-ai/project.md` read by skills (not parsed) | ✅ Pattern | Clean approach to parameterization |

---

## Research Workflow

When tackling a problem:

```
1. Check this catalog → is there a reference solution?
2. Read the plan docs (.ai/plans/) → understand the design
3. Read the implementation (src/, skills/) → see working code
4. Adapt to our squad → write research doc with specific recommendations
```

---

*Maintained by Bilbo. Update this catalog when new reference codebases are identified.*
