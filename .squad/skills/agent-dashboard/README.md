# Agent Dashboard

Live terminal dashboard showing real-time agent activity and session task history.

## Quick Start

```powershell
# Launch dashboard (full mode)
.squad/skills/agent-dashboard/start-dashboard.ps1

# One-shot render (for scripts/CI)
.squad/skills/agent-dashboard/start-dashboard.ps1 -Once

# Offline mode (no GitHub API)
.squad/skills/agent-dashboard/start-dashboard.ps1 -NoGitHub

# Beta TUI mode with resizable panels
.squad/skills/agent-dashboard/start-dashboard.ps1 -SharpUI
```

## Architecture

Uses [tamirdresher/squad-monitor](https://github.com/tamirdresher/squad-monitor) as the rendering engine, with PowerShell adapter scripts that translate pa-squad data into squad-monitor's expected format.

```
pa-squad data sources          adapters              squad-monitor
─────────────────────     ───────────────────     ──────────────────
.squad/orchestration-log/ → write-orchestration  → Orchestration panel
  (pa-squad format)          -log.ps1               (squad-monitor fmt)

~/.squad/ralph-heartbeat  ─────────────────────→  Ralph Watch panel
  (already compatible)                              (reads directly)

~/.squad/ralph-watch.log  ─────────────────────→  Recent Rounds panel
  (already compatible)                              (reads directly)

GitHub (gh CLI)           ─────────────────────→  Issues + PRs panels
                                                    (reads directly)

session_store SQLite      → get-session-summary  → Terminal output
                             .ps1                   (standalone)
```

## Scripts

| Script | Purpose | Lines |
|--------|---------|-------|
| `start-dashboard.ps1` | Entry point — syncs data, launches squad-monitor | ~30 |
| `write-orchestration-log.ps1` | Converts pa-squad orchestration logs to squad-monitor format | ~40 |
| `get-session-summary.ps1` | Queries session_store for recent task history | ~45 |

## Prerequisites

- .NET 10 SDK
- `squad-monitor` global tool: `dotnet tool install -g squad-monitor`
- GitHub CLI (`gh`) for issue/PR panels
- SQLite3 or Python for session summary queries

## Dashboard Panels

| Panel | Data Source | Status |
|-------|-------------|--------|
| Ralph Watch Loop | `~/.squad/ralph-heartbeat.json` | ✅ Native |
| Ralph Recent Rounds | `~/.squad/ralph-watch.log` | ✅ Native |
| Token Usage | `~/.copilot/logs/` | ✅ Native |
| Live Agent Sessions | Process detection | ✅ Native |
| GitHub Issues | `gh issue list` | ✅ Native |
| GitHub PRs | `gh pr list` | ✅ Native |
| Orchestration Activity | `.squad/orchestration-log/` | 🔄 Adapter |
| Session Task History | `session_store.db` | 🔄 Adapter |

## Related

- [squad-monitor repo](https://github.com/tamirdresher/squad-monitor)
- [Issue #33](https://github.com/jbenami_microsoft/ms-pa/issues/33)
- [Issue #30](https://github.com/jbenami_microsoft/ms-pa/issues/30) — Squad Monitoring research
