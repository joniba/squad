# devops-ai Repository Analysis

**Repository:** https://github.com/kpiteira/devops-ai  
**Author:** Karl Piteira  
**Status:** Active (v0.4 completed, roadmap through 2026)  
**Analysis Date:** 2026

---

## What It Is

**devops-ai** is a development workflow automation framework combining **markdown-based AI skills** with a **Python CLI infrastructure tool** (kinfra). It standardizes how teams use AI coding assistants (Claude Code, GitHub Copilot CLI, OpenAI Codex) for design, planning, and implementation workflows. Skills are versioned, configuration-driven prompts following the [Agent Skills standard](https://agentskills.io); kinfra manages isolated development environments via git worktrees, Docker sandbox slots, and shared observability stacks. The entire system is symlink-installed globally, allowing all projects to pull upstream updates automatically.

---

## Key Capabilities

### AI Skills (Design-to-Implementation Pipeline)

- **`/kdesign`** — Collaborative feature design with scenario-based validation; produces DESIGN.md + ARCHITECTURE.md with milestone structure
- **`/kplan`** — Expands design milestones into granular, testable tasks with acceptance criteria and architecture alignment
- **`/kbuild`** — Executes tasks via TDD (red → green → refactor), maintaining handoff documents between tasks and generating completion reports
- **`/kissue`** — GitHub issue workflow: fetch issue, create branch, implement TDD, open PR with "Closes #N"
- **`/kreview`** — Critically assess PR review comments; implement fixes or push back with reasoning

### Infrastructure CLI (kinfra)

- **Git Worktrees** — Automatically creates isolated `spec/<feature>` and `impl/<feature>-<milestone>` branches
- **Docker Sandbox Slots** — Numbered slots (1–100) with port isolation; formula: `base_port + slot_id` prevents collisions across projects
- **Port Registry** — Global registry (`~/.devops-ai/registry.json`) tracks active slots; supports 100 concurrent development environments
- **Shared Observability** — Single Jaeger/Grafana/Prometheus stack (4xxxx ports) auto-connected to all sandboxes with project-scoped namespacing
- **Quality Artifacts** — Auto-generates Justfile, Makefile, pre-commit hooks, CI/CD workflows (GitHub Actions) with AI code review and security scanning
- **Phased Onboarding** — `/kinfra-onboard` skill analyzes projects, parameterizes Docker Compose, rewires OTEL endpoints, and verifies config

### Quality Infrastructure

- **Multi-tiered enforcement:** Claude hooks (lint ~2s) → pre-commit hooks (quality+tests ~30s) → CI (full+AI review ~2min)
- **Testing taxonomy rule** — Classifies tests as unit/integration/E2E; pytest conftest blocks `socket.connect()` in unit tests
- **AI-powered reviews** — GitHub Actions run AI code review on PRs + security scanning (CodeQL)
- **Re-init preservation** — Existing custom secrets, files, and env values are preserved when re-running `kinfra init`

### Configuration & Degradation

- **`.devops-ai/project.md`** — Single TOML+markdown config file specifying project language, test commands, infrastructure details, E2E catalog
- **Graceful fallback** — Skills ask for values on first use if config is missing; Docker features are entirely optional
- **Cross-tool portability** — Skills symlinked to `~/.claude/skills/`, `~/.codex/skills/`, `~/.copilot/skills/` via Agent Skills standard

---

## Architecture

### Conceptual Model

```
AI Coding Tool (Claude Code / Copilot CLI / Codex)
  ↓
  Skills (markdown prompts at ~/.*/skills/)
  ├── Read .devops-ai/project.md for context
  ├── Coordinate with AI assistant on design/plan/implement
  └── Hand off artifacts (DESIGN.md, PLAN.md, PR links)
  
kinfra CLI (Python, installed globally via uv)
  ├── Git Layer: create/list/destroy worktrees
  ├── Docker Layer: allocate slots, manage sandboxes, rewire OTEL
  ├── Port Layer: track allocations, detect conflicts, apply formulas
  ├── Observability Layer: Jaeger/Grafana/Prometheus lifecycle
  └── Agent-Deck Integration: optional `--session` flag for session tracking
```

### Data Flow: Feature Implementation

```
1. Developer runs: /kdesign feature: Add auth
   → Claude Code produces DESIGN.md + ARCHITECTURE.md + milestones
   
2. Developer runs: /kplan design: DESIGN.md arch: ARCHITECTURE.md
   → Milestones expanded to implementable tasks (M1.1, M1.2, ...)
   → Tasks include file list, test files, acceptance criteria
   
3. Developer runs: /kbuild impl: M1_auth.md
   → Creates impl/auth-M1 worktree + sandbox via kinfra
   → For each task: TDD cycle (write test → red → implement → green → refactor)
   → Produces handoff document (HANDOFF_M1_auth.md) with blockers, learnings
   
4. Developer creates PR
   → /kissue #42 workflow or manual PR
   → Pre-commit hook runs: make check (lint + tests ~30s)
   → GitHub Actions: CI (tests ~2min) + AI code review + security scan
   → Merge + close issue
```

### Technology Stack

- **Language:** Python 3.11+ (CLI only; skills are markdown)
- **CLI Framework:** Typer (command-line interface), Rich (terminal output)
- **Configuration:** ruamel.yaml (TOML/YAML parsing with comment preservation)
- **Dependency Management:** uv (ultra-fast Python installer; used for global kinfra install)
- **Testing:** pytest (unit + E2E), pytest-docker for E2E orchestration
- **Linting/Type-checking:** ruff, mypy
- **Container Orchestration:** Docker, Docker Compose (optional; port allocation formula pre-allocates ranges)
- **Observability Backend:** Jaeger (tracing), Prometheus (metrics), Grafana (dashboards)
- **Agent Skills:** Follows [agentskills.io spec](https://agentskills.io) — directory-based with `SKILL.md` entry point, YAML frontmatter

### Codebase Organization

```
src/devops_ai/
├── cli/main.py                 # Typer command entrypoint (kinfra)
├── worktree.py                 # Git worktree creation/tracking
├── sandbox.py                  # Docker sandbox generation (.env, compose overrides)
├── ports.py                    # Port allocation logic (base + slot_id)
├── registry.py                 # Global registry management (~/.devops-ai/registry.json)
├── config.py                   # infra.toml loader
├── compose.py                  # Docker Compose parameterization + OTEL rewiring
├── observability.py            # Jaeger/Grafana/Prometheus lifecycle
├── provision.py                # Sandbox provisioning (downloads, templating)
└── agent_deck.py               # Optional agent-deck session tracking
```

**Skills directory** (`skills/`):
```
skills/
├── kdesign/SKILL.md            # Design with scenario validation
├── kplan/SKILL.md              # Milestone → task expansion
├── kbuild/SKILL.md             # TDD task execution + orchestration
├── kissue/SKILL.md             # GitHub issue workflow
├── kreview/SKILL.md            # PR review assessment
├── kworktree/SKILL.md          # Worktree/sandbox UI skill
└── kinfra-onboard/SKILL.md     # Phased project onboarding
```

---

## Potential Value for PA-SQUAD

### Direct Applicability

1. **Standardized AI Workflow** — Our squad uses Claude Code, Copilot CLI, and hand-written agents. devops-ai's skill system + rules (symlinked `.claude/rules/`) provide a single source of truth for how we approach design, planning, and implementation. We could adopt kdesign → kplan → kbuild to ensure consistent quality across all projects.

2. **Port Isolation & Multi-Project Development** — kinfra's sandbox slot system directly solves our challenge of running multiple projects simultaneously without port conflicts. Instead of manual port management, we'd get deterministic allocation (e.g., slot 1 = ports 5001, 8001, 3001, etc.).

3. **Observability Story** — The shared Jaeger/Grafana/Prometheus stack means every project auto-connects to the same observability backend. Currently, we likely have per-project or missing observability; devops-ai unifies it.

4. **Quality Enforcement Automation** — devops-ai's tiered CI (Claude hooks → pre-commit → GitHub Actions) with AI-powered code review and security scanning is production-grade. We could adopt its Justfile/Makefile/CI templates immediately.

5. **Agent Collaboration Framework** — The AGENTS.md file in devops-ai itself models how humans and AI should work together (honesty, surface trade-offs, push back on disagreement). This aligns with our squad's collaborative ethos.

### Integration Opportunities

- **Custom E2E Test Designer Agent** — devops-ai has `/ke2e-test-designer`, `/ke2e-test-runner`, `/ke2e-test-scout` agents. We could extend these for our specific stacks.
- **Squad-Specific Rules** — Add pa-squad rules (`rules/testing-patterns.md`, `rules/architecture-principles.md`) symlinked to all squad projects via `install.sh --rules`.
- **Handoff Protocol** — devops-ai's HANDOFF_*.md documents could standardize how agents pass work between milestones in our projects.

### Limitations & Considerations

1. **Markdown-Only Skills** — Skills are prompts, not executable logic. Complex branching or conditional logic must be handled by the AI assistant, not the skill. This is intentional but requires disciplined prompt design.

2. **Docker Dependency (Optional)** — kinfra's sandbox and observability features require Docker. Non-containerized projects work but lose port isolation and observability.

3. **Project Config Burden** — `.devops-ai/project.md` must be maintained per-project. If config drifts or is wrong, skills degrade silently (asking values on-the-fly rather than failing loudly). We'd need discipline.

4. **Agent Skills Ecosystem Maturity** — The standard is young; tooling support (validation, debugging) is emerging. Codex CLI has a 500-char description limit; Copilot CLI compatibility is untested.

5. **Skill Update Frequency** — Symlinks point to the devops-ai repo. Frequent upstream changes could destabilize projects if skills regress. Pinning to a tag would require manual `install.sh --version` flag (not yet implemented).

---

## Limitations

### Known Gaps

1. **Multi-Language Agent Skills** — Rules are extracted to `.claude/rules/` but project-specific rules (e.g., Go testing patterns vs. Python patterns) must be hand-written. No templating or language detection.

2. **Async Observability Integration** — OTEL rewiring in Compose files is done via string substitution, not semantic parsing. Complex or non-standard Compose layouts may require manual fixes.

3. **No Rollback Mechanism** — `/kinfra done` cleans up a worktree/sandbox. Re-running `impl` on the same feature creates a fresh sandbox. No easy way to restore state (would need manual git recovery).

4. **Limited Error Diagnostics** — kinfra errors (port conflicts, Docker failures) report their cause but don't suggest remediation steps. Debugging requires manual inspection of logs or registry.

5. **Single Organization Assumption** — Registry and observability stack are per-user (`~/.devops-ai/`). Multi-organization or team-scoped registries would require extended config.

### By Design (Not Limitations)

- **No DSL** — Skills are plain markdown; there's no custom query language, no conditional syntax. Intentional simplicity for AI tool portability.
- **No Runtime Framework** — kinfra is a CLI, not a daemon or service. No persistent state or inter-process communication.
- **Config Is a Prompt** — `.devops-ai/project.md` is read by skills as text, not parsed as structured data. This allows flexibility but sacrifices validation.

---

## Technical Details

### Language & Platform

- **Primary Language:** Python 3.11+
- **Supported Platforms:** macOS, Linux (tested on 2024+ macOS; Windows requires WSL2 or native Python)
- **Package Manager:** uv (installs globally as `uv tool install -e ./` in editable mode)
- **Entry Point:** `kinfra` command (symlinked to `src/devops_ai/cli/main.py:main`)

### Dependencies

| Package | Purpose | Version |
|---------|---------|---------|
| `typer` | CLI framework | ≥0.9 |
| `rich` | Terminal formatting + tables | ≥13 |
| `ruamel.yaml` | TOML/YAML with comment preservation | ≥0.18 |
| `pytest` | Unit/E2E testing | ≥8 (dev) |
| `ruff` | Linting + formatting | ≥0.4 (dev) |
| `mypy` | Type checking | ≥1.10 (dev) |

### Skills Standards Compliance

- **Agent Skills Standard (v1):** [agentskills.io](https://agentskills.io) spec
- **Frontmatter:** YAML (name, description, version, metadata.model, etc.)
- **Entry Point:** Directory-based; `skills/<name>/SKILL.md` auto-discovered
- **Portability:** Symlinks to `~/.claude/skills/`, `~/.codex/skills/`, `~/.copilot/skills/`
- **Validation:** No runtime parser; skills are prompts. Validation would use `skills-ref validate` tool (not yet integrated).

### Installation & Upgrade

```bash
# Install
git clone https://github.com/kpiteira/devops-ai.git ~/Documents/dev/devops-ai
cd ~/Documents/dev/devops-ai
./install.sh

# Upgrade (symlinks are auto-updated)
cd ~/Documents/dev/devops-ai
git pull

# Install only for Claude Code
./install.sh --target claude

# Install rules into a specific project
./install.sh --rules /path/to/my-project
```

### Testing

- **Unit Tests:** 299+ tests using pytest
- **E2E Tests:** 8+ Docker-based tests for sandbox and observability
- **Test Markers:** `@pytest.mark.e2e` for Docker-dependent tests
- **Conftest Guards:** `tests/unit/conftest.py` blocks `socket.connect()` in unit tests (Python-only)

### Configuration Files

- **`.devops-ai/project.md`** — Per-project skill context (project name, language, test commands, E2E catalog, infrastructure)
- **`infra.toml`** — Generated by `kinfra init`, tracks parameterized Compose ports and sandbox config
- **`~/.devops-ai/registry.json`** — Global slot registry (shared across all projects), tracks active worktrees and sandbox allocations
- **`rules/*.md`** — Shared principles symlinked to `~/.claude/rules/` on install (~1,490 tokens always-on in Claude Code)

### Key Algorithms

**Port Allocation Formula:**
```python
allocated_port = base_port + slot_id
# Example: base=5000, slot=3 → port 5003
# Supports 100 slots per service per project
```

**Sandbox Slot Tracking:**
- Global registry (`~/.devops-ai/registry.json`) is JSON file listing active (project, slot_id, timestamp, ports)
- On `kinfra impl`, next available slot is allocated; on `kinfra done`, slot is released
- Conflict detection prevents same port from being used by two projects

**Observability Network:**
- Single Docker network: `devops-ai-observability`
- All sandboxes connect to it via compose override
- OTEL endpoints rewritten: `http://localhost:44317` → `http://devops-ai-observability:44317`

---

## Development Maturity & Roadmap

### Completed Versions

- **v0.1** — Skill porting from ktrdr, multi-tool install, config system
- **v0.2** — kinfra CLI with git worktrees, sandbox slots, observability stack (v0.2.5 is current stable)
- **v0.3** — Skills modernization for Claude Opus 4.6; reduced from 4,194 → 1,195 lines (71% reduction)
- **v0.4** — Quality infrastructure standard (Justfile, Makefile, CI/CD, AI review, security scanning)

### Backlog Priorities

1. **Dogfooding** — Run `/kdesign` → `/kplan` → `/kbuild` end-to-end on real features; capture friction points
2. **Test on other projects** — Validate skills on projects outside ktrdr (different stacks will expose assumptions)
3. **Agent teams prototype** — Parallel task execution within a milestone (blocked on Claude Code experimental API)
4. **Test with Codex & Copilot CLI** — Skills are symlinked but only tested with Claude Code

---

## Relationship to PA-SQUAD

### Alignment

- **Shared Values:** Craftsmanship over completion, honesty over confidence, decisions made together (matches our squad ethos from AGENTS.md)
- **Similar Problems:** Standardizing AI-assisted development workflows across projects, managing infrastructure complexity, TDD discipline
- **Tool Ecosystem:** We use Claude Code, GitHub Copilot CLI, and custom agents; devops-ai's skills are portable across all three

### Could Adopt

1. **Wholesale:** Use kdesign → kplan → kbuild for all new features across all projects; let devops-ai handle skill updates
2. **Selective:** Use kinfra for sandbox/port management only; keep our own design/planning approach
3. **Template:** Use devops-ai's install.sh, Justfile, Makefile as templates for our projects; maintain our own forks

### Should NOT Adopt (Yet)

- The exact skills verbatim without testing on our stack (TypeScript, Node.js primarily; devops-ai is Python-heavy)
- Observability without first evaluating if Jaeger/Grafana/Prometheus fit our monitoring needs
- Agent teams feature until Claude Code's experimental API stabilizes

---

## Summary for the Squad

**devops-ai is a production-grade, open-source framework for AI-assisted software engineering.** It provides:

1. **Reusable, portable skills** for design, planning, and implementation that work across Claude Code, Copilot CLI, and Codex
2. **Infrastructure automation** (git worktrees, Docker sandboxes, observability) that solves real multi-project development challenges
3. **Quality enforcement** through tiered CI/CD with AI-powered code review
4. **Collaborative human-AI model** (documented in AGENTS.md) that aligns with our squad's working agreement

The repo is **actively maintained** (last commit 2026-02-04), well-documented, and has concrete roadmap through mid-2026. Its architecture favors **simplicity over abstraction** (skills are prompts, not code), which makes it easy to understand, modify, and extend.

**Recommendation:** Pilot on one squad project (e.g., tools or teams-knowledge). Run `/kdesign` → `/kplan` → `/kbuild` on a real feature and capture friction points. If the workflow fits, adopt skills + kinfra for sandbox management. If skills need customization, fork them and maintain as squad templates.

---

## Reference Links

- **Repository:** https://github.com/kpiteira/devops-ai
- **Agent Skills Standard:** https://agentskills.io
- **Installation docs:** See README.md in repo
- **Design docs:** `docs/designs/` in repo (skill-generalization, kinfra-kworktree, etc.)

