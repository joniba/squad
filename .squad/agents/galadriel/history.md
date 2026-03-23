# Project Context

- **Owner:** Jonathan
- **Project:** pa-squad ΓÇö a personal-assistant squad for everyday tasks, research, documentation, tool building, and livesite support
- **Stack:** General-purpose (not a single-stack project)
- **Role:** Reviewer ΓÇö quality gate for all PRs and deliverables
- **Created:** 2026-03-23
- **Hired by:** Gandalf (Lead), Issue #13

## Origin

Galadriel was hired based on Elrond's analysis of the coffee-ratings "Bobbie" charter (Issue #12). Key patterns adapted from Bobbie:

- **Ownership model:** Author fixes their own work, reviewer finds issues
- **PR Review Fix Workflow:** Structured 4-step process (read findings ΓåÆ fix ΓåÆ reply with SHA ΓåÆ signal completion)
- **Escalation timing:** 3+ review cycles ΓåÆ escalate to Lead
- **Evidence-driven voice:** Every finding cites file, line, and reasoning
- **Severity scale:** Critical/High/Medium/Low with clear action thresholds

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2025-07-10 — PR #15064785 (TiExpert agent skills and scripts)

**Tool chain for Sentinel-TiPipeline reviews:**
- `ado-search_code` returns infoCode 15 (0 results) for feature branches — branch not indexed by ADO search.
- File access pattern that works: `git fetch origin <branch>` then `git --no-pager show FETCH_HEAD:<path>` from the local clone at `C:\dev\ti\Sentinel-TiPipeline`.
- Always use `--no-pager` on git commands to avoid interactive pager blocking.

**Pattern: exit code vs return in PowerShell scripts:**
- `return` in a script exits the current scope with exit code 0 (success). Automation will not see the failure.
- `exit 1` sets process exit code and is the correct way to signal failure from a `.ps1` script invoked from CI or a parent script.
- Validate this across all scripts in a PR — inconsistency in one indicates risk in others.

**Pattern: polling loops must handle all terminal states, not just success:**
- A loop that only breaks on `Done` will silently exhaust retries on `Failed`. Always add explicit `Failed` early-exit to avoid multi-minute waits and lost failure context.

**Review scope discipline:**
- PR titles and descriptions are not always accurate — always check the full diff stat first (`git diff --name-status`). PRs can contain far more than advertised.
- Undescribed C# feature work in a "scripts only" PR is a scope flag for Gandalf.

**Security hygiene:**
- Hardcoded PPE subscription/workspace GUIDs in source are a Medium finding even in private repos. The pattern creates risk if prod values are later added to the same file. Flag consistently.
