---
title: "Unified Scheduler Guide"
date: 2026-03-24
author: gimli
documentarian: bilbo
category: guide
tags:
  - guide
  - tooling
  - workflow
  - squad-infra
  - bilbo
status: final
related_docs:
  - tools/squad-scheduler-tool.md
related_issues: []
---

# Unified Scheduler Guide

The **Unified Scheduler** is a single PowerShell dispatcher that runs all recurring squad tasks on a fixed schedule or on-demand. Instead of managing multiple watchdog scripts, you configure tasks once in `.squad/scheduler.json` and the scheduler handles execution, logging, and state tracking.

## What It Does

The scheduler:
- **Runs recurring tasks** — teams watchdog, email scanner, semantic index refresh, ICM scans, daily summaries
- **Tracks state** — remembers when each task last ran, respects intervals (24h, 12h, 4h, etc.)
- **Enforces conditions** — skips tasks during off-hours or when conditions aren't met
- **Logs everything** — timestamped output to `.squad/scheduler.log`
- **Offers toggles** — run specific tasks, skip others, check status without executing

---

## Quick Start

### See What's Scheduled (Dry Run)

```powershell
.\scripts\squad-scheduler.ps1 -DryRun
```

Shows all tasks, their status (due/not due), intervals, and conditions without executing anything.

**Example output:**
```
[DRY] teams-watchdog | 24h | DUE | condition=met
[DRY] icm-scan | 4h | not due | condition=unmet
[DRY] daily-summary | 24h | DUE | condition=met
[DRY] semantic-refresh | 12h | DUE | condition=met
[DRY] email-watchdog | 24h | DUE | condition=met
```

### Run All Due Tasks Once

```powershell
.\scripts\squad-scheduler.ps1 -Once
```

Executes all tasks that are due based on their interval and conditions. Useful for ad-hoc runs.

### Start the Scheduler Daemon

```powershell
.\scripts\squad-scheduler.ps1
```

Runs indefinitely, checking every 60 seconds for due tasks. Press Ctrl+C to stop. (For persistent background operation, use Windows Task Scheduler or a PowerShell job.)

---

## CLI Flags

| Flag | Type | Purpose | Example |
|------|------|---------|---------|
| `-DryRun` | switch | Show what would run without executing | `.\scripts\squad-scheduler.ps1 -DryRun` |
| `-Once` | switch | Run all due tasks once and exit | `.\scripts\squad-scheduler.ps1 -Once` |
| `-Include` | string[] | Enable additional tasks for this run (overrides `enabled: false`) | `.\scripts\squad-scheduler.ps1 -Once -Include icm-scan` |
| `-Exclude` | string[] | Skip tasks for this run | `.\scripts\squad-scheduler.ps1 -Once -Exclude teams-watchdog` |
| `-Tasks` | string[] | Run ONLY these tasks (ignores enabled/disabled) | `.\scripts\squad-scheduler.ps1 -Tasks icm-scan, daily-summary -Once` |

### Common Invocations

| You want to... | Command |
|----------------|---------|
| Run the watchdog now | `.\scripts\squad-scheduler.ps1 -Once` |
| Run watchdog + include ICM scanning | `.\scripts\squad-scheduler.ps1 -Once -Include icm-scan` |
| Run watchdog but skip Teams scan | `.\scripts\squad-scheduler.ps1 -Once -Exclude teams-watchdog` |
| Run only email watchdog | `.\scripts\squad-scheduler.ps1 -Tasks email-watchdog -Once` |
| Check what's scheduled and due | `.\scripts\squad-scheduler.ps1 -DryRun` |
| Start the scheduler as a daemon | `.\scripts\squad-scheduler.ps1` |

---

## Current Tasks

All tasks are configured in `.squad/scheduler.json`. Here's what's running by default:

| Task | Script | Interval | Enabled | Condition | Purpose |
|------|--------|----------|---------|-----------|---------|
| `teams-watchdog` | `.squad/skills/teams-watchdog/run-pipeline.ps1` | 24h | ✅ Yes | Always | Daily Teams message watchdog: summarizes decisions, actions, context |
| `daily-summary` | `scripts/squad-daily-summary.ps1` | 24h | ✅ Yes | Always | Generate daily summary of squad activities |
| `semantic-refresh` | `scripts/semantic-index.ps1` | 12h | ✅ Yes | Always | Rebuild semantic index for code search |
| `email-watchdog` | `.squad/skills/email-watchdog/email-scan.ps1` | 24h | ✅ Yes | Always | Scan Outlook for action items and deliver to Teams |
| `icm-scan` | `scripts/icm-scan.ps1` | 4h | ❌ No | On-call only | Scan ICM incidents (only when on-call is enabled) |

---

## Configuration

All tasks are defined in `.squad/scheduler.json`. Here's the structure:

```json
{
  "tasks": [
    {
      "name": "teams-watchdog",
      "script": ".squad/skills/teams-watchdog/run-pipeline.ps1",
      "interval": "24h",
      "enabled": true,
      "condition": null,
      "params": {}
    }
  ],
  "oncall": {
    "enabled": false,
    "team_id": 116041,
    "check_alias": "jbenami"
  },
  "log_file": ".squad/scheduler.log"
}
```

### Task Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | ✅ Yes | Unique task identifier (used in `-Include`, `-Exclude`, `-Tasks`) |
| `script` | string | ✅ Yes | Path to script, relative to repo root |
| `interval` | string | ✅ Yes | How often to run: `4h`, `6h`, `12h`, `24h`, `1d` |
| `enabled` | boolean | ✅ Yes | Default on/off state (overridable with `-Include`/`-Exclude`) |
| `condition` | string | ✅ Yes | `null` (always run) or `"oncall"` (only when on-call enabled) |
| `params` | object | ❌ No | Script-specific parameters (passed to the script) |

### Interval Format

- `4h`, `6h`, `12h` — hours
- `24h`, `1d` — full day
- Intervals are checked against last-run time in `.squad/scheduler-state.json`

### On-Call Configuration

By default, on-call is **disabled**. Tasks with `"condition": "oncall"` only run when you enable it:

```json
"oncall": {
  "enabled": true,
  "team_id": 116041,
  "check_alias": "jbenami"
}
```

Enable this when Jonathan is on-call, so ICM scanning and other on-call tasks automatically activate.

---

## Adding a New Task

1. **Create your script** — e.g., `scripts/my-new-task.ps1`

2. **Add to `.squad/scheduler.json`:**
   ```json
   {
     "name": "my-new-task",
     "script": "scripts/my-new-task.ps1",
     "interval": "6h",
     "enabled": true,
     "condition": null
   }
   ```

3. **Test it:**
   ```powershell
   # Check if it's recognized
   .\scripts\squad-scheduler.ps1 -DryRun
   
   # Run it once
   .\scripts\squad-scheduler.ps1 -Tasks my-new-task -Once
   ```

4. **If the task needs parameters:**
   ```json
   {
     "name": "my-task-with-params",
     "script": "scripts/my-task.ps1",
     "interval": "12h",
     "enabled": true,
     "condition": null,
     "params": {
       "Hours": 24,
       "Verbose": true
     }
   }
   ```
   (Your script must accept these as named parameters.)

---

## Logging

All scheduler output is logged to `.squad/scheduler.log` with timestamps:

```
2026-03-24 09:00:15 --- scheduler tick ---
2026-03-24 09:00:15   RUN  teams-watchdog
2026-03-24 09:02:30   DONE teams-watchdog (135.2s)
2026-03-24 09:02:30   RUN  semantic-refresh
2026-03-24 09:03:15   DONE semantic-refresh (45.1s)
2026-03-24 09:03:15 sleeping 60s...
```

**Last-run state** is tracked in `.squad/scheduler-state.json` (git-ignored):

```json
{
  "teams-watchdog": "2026-03-24T09:02:30.0000000Z",
  "daily-summary": "2026-03-24T09:03:00.0000000Z"
}
```

---

## From Copilot Sessions

When using the Copilot CLI, you can invoke the scheduler with natural commands:

**In a Copilot session:**
```
@bilbo run the watchdog
@gimli run the watchdog, include icm scanning
@aragorn what's scheduled?
```

These get translated to scheduler invocations:
```powershell
# "run the watchdog"
.\scripts\squad-scheduler.ps1 -Once

# "run the watchdog, include icm scanning"
.\scripts\squad-scheduler.ps1 -Once -Include icm-scan

# "what's scheduled?"
.\scripts\squad-scheduler.ps1 -DryRun
```

---

## Troubleshooting

### A task isn't running even though it should be due

1. **Check dry run:**
   ```powershell
   .\scripts\squad-scheduler.ps1 -DryRun
   ```
   Look for `[DRY]` line with the task name. If it says "not due", the interval hasn't elapsed since last run.

2. **Check last-run state:**
   ```powershell
   Get-Content .squad/scheduler-state.json | ConvertFrom-Json
   ```

3. **Check the condition:**
   If the task has `"condition": "oncall"`, verify that on-call is enabled in `.squad/scheduler.json`.

### A task is failing

1. **Check the log:**
   ```powershell
   Get-Content .squad/scheduler.log -Tail 50
   ```
   Look for `FAIL` lines with error messages.

2. **Run the task manually:**
   ```powershell
   & ".squad/skills/teams-watchdog/run-pipeline.ps1"
   ```
   This helps isolate whether the issue is the scheduler or the script itself.

3. **Verify the script path:**
   Paths in `scheduler.json` are relative to the repo root. Check that the file exists.

### The scheduler process is stuck

```powershell
# Find the PowerShell process
Get-Process powershell

# Kill it (replace PID with the actual process ID)
Stop-Process -Id <PID>
```

---

## Related Documentation

- **Email Watchdog Guide** — `docs/guides/email-watchdog-setup.md`
- **Teams Knowledge Library** — `docs/guides/teams-knowledge-library-guide.md`
- **Scheduler Tool Spec** — `docs/tools/squad-scheduler-tool.md`
