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

### Script task (default)

```json
{
  "name": "teams-watchdog",
  "script": ".squad/skills/teams-watchdog/run-pipeline.ps1",
  "interval": "24h",
  "enabled": true,
  "condition": null
}
```

### Agent task

```json
{
  "name": "icm-scan",
  "type": "agent",
  "agent": "aragorn",
  "prompt": "Scan IcM team 116041 for active incidents...",
  "interval": "4h",
  "enabled": false,
  "condition": "oncall"
}
```

**Fields common to all tasks:**

- **name** — unique task identifier
- **type** — `"script"` (default, may be omitted) or `"agent"`
- **interval** — how often to run (`4h`, `12h`, `24h`, `1d`)
- **enabled** — whether it runs by default (overridable with `-Include`/`-Exclude`)
- **condition** — `null` (always) or `"oncall"` (only when on-call is enabled)

**Script task fields** (`type: "script"` or no `type`):
- **script** — path to the PowerShell script, relative to repo root

**Agent task fields** (`type: "agent"`):
- **agent** — squad member to spawn (e.g., `"aragorn"`)
- **prompt** — full instruction passed to the agent

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

### Script task

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

### Agent task

1. Add an entry to `.squad/scheduler.json` with `"type": "agent"`:
   ```json
   {
     "name": "my-agent-task",
     "type": "agent",
     "agent": "aragorn",
     "prompt": "Full instructions for the agent...",
     "interval": "6h",
     "enabled": true,
     "condition": null
   }
   ```
2. Test (DryRun first): `.\scripts\squad-scheduler.ps1 -Tasks my-agent-task -DryRun`

## Task Types

### Script tasks (`type: "script"` or no `type`)

The default. The scheduler runs a PowerShell script directly. Suitable for tasks that don't need MCP tools.

```json
{ "name": "teams-watchdog", "script": ".squad/skills/teams-watchdog/run-pipeline.ps1", ... }
```

### Agent tasks (`type: "agent"`)

Used when the task requires MCP tools (IcM, Geneva, Kusto, etc.) that are only available in agent context. Instead of running a script, the scheduler emits a structured signal on stdout:

```
AGENT_TASK|{agent}|{prompt}
```

**Example output:**
```
AGENT_TASK|aragorn|Scan IcM team 116041 for active incidents...
```

The coordinator, which invokes the scheduler as a skill (`-Once`), reads this signal from stdout and spawns the named agent with the given prompt. The agent then executes the work using its full MCP tool context.

**Why not `copilot -p`?** Scripts cannot capture `copilot -p` output in variables (see team decisions). Agents have direct MCP context and are the correct mechanism for tool-calling work.

**Rule:** If a task needs MCP tools → `type: "agent"`. If it's a pure script → `type: "script"` (or omit `type`).

### Coordinator Handoff: AGENT_TASK → Agent Spawn

When the coordinator runs the scheduler (e.g., via `-Once`), it must watch stdout for `AGENT_TASK` lines. **This is a required handoff — not optional.**

**Protocol:**

1. Coordinator runs: `.\scripts\squad-scheduler.ps1 -Once` (or with `-Tasks`/`-Include`)
2. Scheduler prints to stdout: `AGENT_TASK|{agent}|{prompt}`
3. **Coordinator MUST spawn the named agent** with the given prompt as a subagent

**Example:**
```
AGENT_TASK|aragorn|Scan IcM team 116041 for active incidents...
```
→ Coordinator spawns Aragorn as a subagent with that exact prompt.

**Rules for the coordinator:**
- Parse each stdout line for the `AGENT_TASK|` prefix
- Split on `|` — field 1 is the agent name, field 2 is the full prompt
- Spawn immediately — do not defer or queue
- One `AGENT_TASK` line = one agent spawn
- The spawned agent runs independently; the coordinator does not wait for it

## Logs

All output is logged to `.squad/scheduler.log` with timestamps.
Last-run times are tracked in `.squad/scheduler-state.json` (git-ignored).

## Owner

Gimli (Tool Builder)
