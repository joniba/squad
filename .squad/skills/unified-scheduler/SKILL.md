# Unified Scheduler

> Single dispatcher for all recurring squad tasks. Config-driven, observable, composable.

## When to Use

| User says | Action |
|-----------|--------|
| "run the watchdog" | `.\scripts\squad-scheduler.ps1 -Once` |
| "run the watchdog, include icm scanning" | `.\scripts\squad-scheduler.ps1 -Once -Include icm-scan` |
| "run the watchdog, skip teams scan" | `.\scripts\squad-scheduler.ps1 -Once -Exclude teams-watchdog` |
| "only run icm scan" | `.\scripts\squad-scheduler.ps1 -Tasks icm-scan -Once` |
| "watchdog status" / "what's scheduled?" | `.\scripts\squad-scheduler.ps1 -DryRun` |
| "start the scheduler" | `.\scripts\squad-scheduler.ps1` (loops forever) |

## Configuration

Config lives at `.squad/scheduler.json`. Each task has:

```json
{
  "name": "teams-watchdog",
  "script": ".squad/skills/teams-watchdog/run-pipeline.ps1",
  "interval": "24h",
  "enabled": true,
  "condition": null
}
```

- **name** — unique task identifier
- **script** — path relative to repo root
- **interval** — how often to run (`4h`, `12h`, `24h`, `1d`)
- **enabled** — whether it runs by default (overridable with `-Include`/`-Exclude`)
- **condition** — `null` (always) or `"oncall"` (only when on-call is enabled)

### On-call configuration

```json
"oncall": {
  "enabled": false,
  "team_id": 116041,
  "check_alias": "jbenami"
}
```

Set `enabled: true` when Jonathan is on-call. Tasks with `"condition": "oncall"` only run when this is active.

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `-Once` | switch | Run all due tasks once and exit (for skill invocation) |
| `-DryRun` | switch | Show what would run without executing |
| `-Include` | string[] | Enable additional tasks for this run |
| `-Exclude` | string[] | Skip tasks for this run |
| `-Tasks` | string[] | Run ONLY these tasks (ignores enabled/disabled) |

## Adding a New Task

1. Create your script (e.g., `scripts/my-task.ps1`)
2. Add an entry to `.squad/scheduler.json`:
   ```json
   {
     "name": "my-task",
     "script": "scripts/my-task.ps1",
     "interval": "6h",
     "enabled": true,
     "condition": null
   }
   ```
3. Test: `.\scripts\squad-scheduler.ps1 -Tasks my-task -Once`

## Logs

All output is logged to `.squad/scheduler.log` with timestamps.
Last-run times are tracked in `.squad/scheduler-state.json` (git-ignored).

## Owner

Gimli (Tool Builder)
