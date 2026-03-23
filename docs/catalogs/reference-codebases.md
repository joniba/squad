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

## 1. Personal AI Companion (Tamir Dresher)

| Field | Value |
|-------|-------|
| **Path** | `C:\dev\defender\MDC-AI-Shared\extensions\personal-ai\` |
| **What** | VS Code extension — full personal AI assistant with skills, agents, IcM, dashboards, engineering pipeline, notifications |
| **Why it matters** | This is the upstream system our squad is modeled after. Many problems we face are already solved here. |
| **Language** | TypeScript + PowerShell scripts |
| **Key author** | Tamir Dresher |

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
