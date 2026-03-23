---
title: "Agent Dashboard Live Terminal Display"
date: 2024-12-19
author: Gimli
documentarian: Bilbo
category: guide
tags: [guide, tooling, workflow, monitoring, bilbo, final]
status: final
---

# Agent Dashboard Live Terminal Display

A real-time terminal dashboard showing squad agent activity, session task history, and GitHub issues/PRs.

## What It Does

Agent Dashboard is a live monitoring interface that displays:

- **Ralph Watch Loop** — Current status of the Ralph watchdog
- **Agent Sessions** — Live processes running agents (Aragorn, Gandalf, Gimli, etc.)
- **GitHub Issues & PRs** — Open items in the `ms-pa` repo
- **Orchestration Activity** — Recent task executions from pa-squad logs
- **Token Usage** — Copilot CLI consumption metrics
- **Session Task History** — Recent tasks from the session database

Use this to:
- Monitor agent activity without looking at logs
- See what's currently running
- Track GitHub work at a glance
- Diagnose watchdog or agent failures visually

## Prerequisites

- **PowerShell 7+** (Core or Desktop)
- **.NET 10 SDK** or later
- **`squad-monitor` global tool:**
  ```powershell
  dotnet tool install -g squad-monitor
  ```
- **GitHub CLI (`gh`) installed and authenticated** (for issue/PR panels)
- **SQLite3** or **Python 3.6+** (for session summary queries)
- **Read access to `.squad/orchestration-log/`** directory

## Quick Start

### Launch the Dashboard

From the repo root:

```powershell
.\.squad\skills\agent-dashboard\start-dashboard.ps1
```

The dashboard opens in full live mode with a 5-second refresh interval. Press `Ctrl+C` to exit.

### One-Time Render (No Loop)

For scripts or CI/CD pipelines that need a snapshot:

```powershell
.\.squad\skills\agent-dashboard\start-dashboard.ps1 -Once
```

Output renders once and the script exits. Useful for:
- Health checks in deployment scripts
- Automated monitoring/alerting
- Cron jobs that should exit cleanly

### Offline Mode (No GitHub API)

If GitHub API is unavailable or rate-limited:

```powershell
.\.squad\skills\agent-dashboard\start-dashboard.ps1 -NoGitHub
```

GitHub panels (Issues, PRs) will show cached data or be disabled. All other panels function normally.

### Beta UI Mode

For advanced terminal formatting:

```powershell
.\.squad\skills\agent-dashboard\start-dashboard.ps1 -SharpUI
```

Enables the beta `SharpConsoleUI` mode with resizable panels. Experimental.

### Custom Refresh Interval

```powershell
# Refresh every 2 seconds (faster)
.\.squad\skills\agent-dashboard\start-dashboard.ps1 -Interval 2

# Refresh every 10 seconds (slower)
.\.squad\skills\agent-dashboard\start-dashboard.ps1 -Interval 10
```

## Dashboard Panels Explained

### Ralph Watch Loop

Shows the status of the Ralph (livesite monitoring) watchdog:
- Last heartbeat timestamp
- Current status (OK, running, failed)
- Process ID
- Next scheduled run

Data source: `~/.squad/ralph-heartbeat.json`

### Live Agent Sessions

Lists all running PowerShell processes that match agent patterns:
- Aragorn (operator)
- Gandalf (analyzer)
- Gimli (tooling)
- Elrond (researcher)
- Bilbo (documentarian)

Includes: PID, start time, CPU %, memory

### GitHub Issues & PRs

- **Open Issues** — Count and recent titles
- **Open PRs** — Author, base branch, status

Requires `gh` CLI. If rate-limited, GitHub panels go dark.

### Orchestration Activity

Recent task executions logged to `.squad/orchestration-log/`:
- Task name
- Agent who ran it
- Timestamp
- Status (queued, running, completed, failed)

Data source: `.squad/orchestration-log/` directory (auto-synced by the dashboard adapter)

### Token Usage

Current Copilot CLI token consumption:
- Premium requests (Copilot -p)
- Standard requests (gh copilot)
- Ratio vs daily/monthly quota

Data source: `~/.copilot/logs/`

### Session Task History

Recent tasks from the Copilot session database:
- Task name
- Agent involved
- Completion status
- Duration

Data source: Session store database (via `get-session-summary.ps1`)

## Architecture: How Data Flows

The dashboard uses **squad-monitor** as the rendering engine with PowerShell **adapter scripts** that translate pa-squad data into squad-monitor's format:

```
pa-squad data              Adapter                Squad-monitor
──────────────             ───────                ──────────────
.squad/orchestration-log/  write-orchestration  Orchestration panel
                           -log.ps1               

~/.squad/ralph-heartbeat   (native format)        Ralph Watch panel

GitHub (gh CLI)            (native API)           Issues + PRs panels

session_store.db           get-session-summary    Terminal output
                           .ps1
```

## Scripts Included

| Script | Purpose |
|--------|---------|
| `start-dashboard.ps1` | Main entry point — syncs data and launches squad-monitor |
| `write-orchestration-log.ps1` | Converts pa-squad logs to squad-monitor format |
| `get-session-summary.ps1` | Queries session history from the SQLite session store |

## Troubleshooting

### "squad-monitor: The term 'squad-monitor' is not recognized"

The squad-monitor global tool isn't installed. Run:

```powershell
dotnet tool install -g squad-monitor
```

Then verify:

```powershell
squad-monitor --version
```

### Dashboard Shows Only Partial Data

Some panels may be unavailable if their data sources are missing:

| Panel | Requires | If Missing |
|-------|----------|-----------|
| Ralph Watch | `~/.squad/ralph-heartbeat.json` | Shows empty |
| Orchestration | `.squad/orchestration-log/` directory | Shows empty |
| GitHub Issues/PRs | `gh` CLI, auth, internet | Shows empty or cached data |
| Session History | Session database (`session_store.db`) | Shows message |

Check what's available:

```powershell
# Ralph heartbeat
Get-Content ~/.squad/ralph-heartbeat.json | ConvertFrom-Json

# Orchestration logs
Get-ChildItem .\.squad\orchestration-log\ | head -5

# Session database
sqlite3 ~/.copilot/session_store.db "SELECT COUNT(*) FROM turns;"

# GitHub CLI
gh issue list --repo jbenami_microsoft/ms-pa
```

### Dashboard Freezes or Is Unresponsive

The squad-monitor process may be stuck. Press `Ctrl+C` twice to force exit.

If this happens repeatedly:

1. Check for zombie squad-monitor processes:
   ```powershell
   Get-Process squad-monitor
   ```

2. Kill any stray processes:
   ```powershell
   Stop-Process -Name squad-monitor -Force
   ```

3. Rebuild the orchestration log adapter:
   ```powershell
   .\.squad\skills\agent-dashboard\write-orchestration-log.ps1
   ```

### GitHub API Rate Limit Hit

If the Issues/PRs panels stop updating, GitHub rate limiting is active. Run with `-NoGitHub`:

```powershell
.\.squad\skills\agent-dashboard\start-dashboard.ps1 -NoGitHub
```

Wait 1 hour, then try again without the flag.

### "Cannot find path to orchestration-log"

The `.squad/orchestration-log/` directory doesn't exist. Create it:

```powershell
New-Item -ItemType Directory -Path .\.squad\orchestration-log\ -Force | Out-Null
```

Then run the dashboard again.

## Advanced: Triggering from External Scripts

You can launch the dashboard from orchestration or monitoring scripts:

```powershell
# Spawn dashboard in a detached process
Start-Process pwsh -ArgumentList "-NoProfile", "-Command", ".\.squad\skills\agent-dashboard\start-dashboard.ps1"

# One-shot render in a CI pipeline
& .\.squad\skills\agent-dashboard\start-dashboard.ps1 -Once -NoGitHub
```

## Performance Notes

- Dashboard refresh interval: 5 seconds (default)
- Orchestration log sync: <1 second per run
- GitHub API queries: cached, ~10 seconds first run
- Session database queries: <500 ms (depends on size)

On a modern system, the dashboard uses ~5–10% CPU and ~30–50 MB memory when live.

## Related

- [squad-monitor repository](https://github.com/tamirdresher/squad-monitor) — The rendering engine
- `.squad/orchestration-log/` — Where orchestration logs are stored
- `~/.squad/ralph-heartbeat.json` — Ralph watchdog status
- `docs/guides/teams-watchdog-setup.md` — Teams monitoring (different tool)

## Next Steps

- **For Gimli (tooling):** Add custom panels for pa-squad-specific metrics (decision velocity, agent utilization)
- **For Jonathan (team lead):** Use the dashboard during daily standups to track squad activity
- **For agents:** Open this dashboard in a second terminal while working to see teammates' activity
