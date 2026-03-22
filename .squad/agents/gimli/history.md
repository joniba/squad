# Project Context

- **Owner:** Jonathan
- **Project:** pa-squad — a personal-assistant squad for everyday tasks, research, documentation, tool building, and livesite support
- **Stack:** General-purpose (not a single-stack project)
- **Created:** 2026-03-22

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

- **2026-07-22 — Watchdog scheduling (#5, PR #51):** Built `run-pipeline.ps1` (38 lines) and `watchdog.ps1` (46 lines) for daily Teams pipeline execution. Key patterns: file-based I/O between pipeline steps (redirect stdout with `>`), `Invoke-Step` helper for DRY error handling, lockfile with stale-lock detection (check PID), structured log as append-only text, heartbeat as overwritten JSON. Used 24h scan window (not 48h) to avoid duplicate summaries — this was a known issue from the issue body. The ralph-watch.ps1 reference pattern is at `tamirdresher/squad-skills/workshop/ralph-watch.ps1`.
- **Pipeline scripts live on separate branches** until merged: squad/1-teams-probe, squad/2-filter-messages, squad/3-extract-decisions, squad/4-format-summary. The orchestrator references them by relative path in the same directory.
- **2026-07-22 — ICM Investigator skill (#52, PR #53):** Adapted the MDC-AI-Shared ICM Investigator (6 sub-agents, 5+1 pipeline stages) into a single SKILL.md at `.squad/skills/icm-investigator/`. Key adaptation: the original uses VS Code extension APIs (workspace tools, dashboard widgets, 1ES queries); ours maps to Copilot CLI MCP tools (icm-*, azure-mcp-kusto, geneva-mcp-server-*, enghub-*, azure-mcp-applens/resourcehealth/monitor). Also upgraded Aragorn's charter with a mandatory 4-stage investigation checklist and hard rules (use tools don't list them, synthesize don't transcribe, classify every incident, confidence levels mandatory). The gap analysis at `docs/aragorn-icm-capability-upgrade.md` (Elrond's work) was the key input — it identified 7 critical gaps including "no data execution" and "no TSG content reading".
