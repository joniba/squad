# Project Context

- **Owner:** Jonathan
- **Project:** pa-squad — a personal-assistant squad for everyday tasks, research, documentation, tool building, and livesite support
- **Stack:** General-purpose (not a single-stack project)
- **Created:** 2026-03-22

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-03-22 — Teams Watchdog Decomposition
- **Architecture:** Teams watchdog is a 4-step pipeline (probe → filter → extract → format) + a watchdog orchestrator + delivery. Each step is a separate script under `.squad/skills/teams-watchdog/`.
- **Key constraint:** `gh copilot` doesn't work. Must use `copilot -p` or `copilot -i`. Cannot save Copilot output to a variable — use file-based I/O between steps.
- **Pattern:** Watchdog loop pattern from `ralph-watch.ps1` (lockfile, heartbeat, structured logging, Teams alerts on consecutive failures).
- **WorkIQ integration:** Teams messages are fetched via the `workiq-ask_work_iq` MCP tool, invoked through `copilot -p` prompts.
- **Repo:** Issues #1-#6 on `jbenami_microsoft/ms-pa`, labeled `squad` + `squad:gimli`.
- **Labels created:** `squad` (purple, 6f42c1) and `squad:gimli` (red, d73a49) on the ms-pa repo.
