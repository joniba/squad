# Failure Recovery Protocol

> When an agent fails a task, the squad self-heals. Jonathan is notified only if the squad can't fix it.

## The Pipeline

```
┌─────────────┐     FAIL     ┌──────────┐    research    ┌────────┐
│  Any Agent  │─────────────▶│  Gandalf │──────────────▶│ Elrond │
│ (e.g. Gala) │  notify lead  │  (Lead)  │  opus-4.6     │(Research│
└─────────────┘              └──────────┘               └────────┘
                                  ▲                         │
                                  │                         │ findings
                                  │    ┌────────────────────┘
                                  │    ▼
                              ┌──────────┐    triage     ┌────────┐
                              │  Gandalf │──────────────▶│ Ralph  │
                              │ (review) │  who builds?  │(triage)│
                              └──────────┘               └────────┘
                                                             │
                                                             │ assign
                                                             ▼
                                                     ┌──────────────┐
                                                     │ Gimli / orig │
                                                     │  (implement) │
                                                     └──────────────┘
                                                             │
                                                             │ done
                                                             ▼
                                                     ┌──────────────┐
                                                     │  Galadriel   │
                                                     │  (re-review) │
                                                     └──────────────┘
                                                             │
                                                             │ done
                                                             ▼
                                                     ┌──────────────┐
                                                     │ Original Agent│
                                                     │  (retry task) │
                                                     └──────────────┘
```

## Steps

| Step | Who | What | Output |
|------|-----|------|--------|
| **1. Detect** | Any agent | Agent hits a blocker — missing tool, permission denied, API failure, incomplete data | Failure signal |
| **2. Signal** | Failed agent | Write failure report to `.squad/decisions/inbox/{agent}-failure-{slug}.md` with: what failed, what was attempted, what's needed | Failure report |
| **3. Triage** | Gandalf | Read failure report. Decide: is this researchable or a known fix? Create GitHub issue if needed. | Issue created, Elrond tasked |
| **4. Research** | Elrond (opus-4.6) | Deep research on the failure. Check `docs/catalogs/reference-codebases.md` first. Produce tested solutions, not theory. | Research doc in `docs/research/` |
| **5. Review** | Gandalf | Review Elrond's findings. Pick the best approach. Approve or send back. | Design approved |
| **6. Route** | Ralph | Triage who implements: Gimli (new tool), original agent (config fix), or specific agent (domain fix) | Implementation assigned |
| **7. Build** | Gimli / assigned | Implement the solution. PR created. | Working fix |
| **8. Verify** | Galadriel | Review the fix PR. Merge on approval. | PR merged |
| **9. Retry** | Original agent | Re-run the original failed task with the fix in place. | Task completed |

## Notification Rules

| Condition | Notify Jonathan? |
|-----------|-----------------|
| Agent fails, pipeline starts | ❌ No — squad handles it |
| Elrond finds a solution | ❌ No — proceed to implementation |
| Elrond can't find a solution | ✅ **Yes** — Teams webhook + issue tagged `needs-human` |
| Gandalf rejects Elrond's solution 2x | ✅ **Yes** — escalate, something is fundamentally wrong |
| Fix implemented and original task retried successfully | ❌ No — unless the original task's output is for Jonathan |
| Fix implemented but retry still fails | ✅ **Yes** — the fix didn't work |
| **Aragorn fails during an active livesite incident** | ✅ **Yes** — operational urgency overrides the pipeline. Don't wait for Elrond. |

**How to notify:** Call the dispatcher script with the `blocked` event:

```powershell
# From the repo root:
.\scripts\notify-squad-event.ps1 -Event "blocked" `
    -What "<what is blocked>" `
    -Why "<why it needs Jonathan>" `
    -ActionNeeded "<specific action>" `
    -Link "<GitHub issue URL>" `
    -Urgency "<blocking-feature|livesite|decision-needed>" `
    -Agent "<agent name>"
```

This routes to `scripts/notify-blocked.ps1` → `scripts/notify.ps1` (urgent tier, immediate delivery) → Teams webhook. The old `send-teams-notification.ps1` is NOT used for failure escalation — use the dispatcher above.

Tag the GitHub issue `needs-human`. Include: what failed, what was tried, what's needed from Jonathan.

## Failure Report Format

When an agent fails, it writes this to `.squad/decisions/inbox/{agent}-failure-{slug}.md`:

```markdown
### {timestamp}: Agent Failure — {agent} on {task}
**Agent:** {name}
**Task:** {what was being done}
**Failed at:** {which step/tool/API}
**Error:** {exact error message or behavior}
**What I tried:** {list of approaches attempted}
**What I need:** {what would unblock this — tool, permission, API, workaround}
**Original request:** {link to issue or user request}
**Severity:** {blocking-feature | degraded-quality | cosmetic}
```

### Failure Slug Convention

The `{slug}` uniquely identifies the failure type. Format: `{tool-or-system}-{error-type}`. Keep it short and lowercase.

| Situation | Slug Example |
|-----------|-------------|
| ADO MCP tool missing | `ado-mcp-missing` |
| GitHub auth expired | `github-auth-expired` |
| File read permission denied | `file-permission-denied` |
| API timeout (Kusto) | `kusto-api-timeout` |
| Missing dependency | `missing-dependency-{name}` |
| Tool returns incomplete data | `incomplete-data-{tool}` |

If two failures of the same type occur simultaneously, append a counter: `ado-mcp-missing-2`.

**Deduplication rule:** Before writing a new failure report, check if a report for the same slug already exists in `.squad/decisions/inbox/`. If an identical pipeline is already running, append a note to the existing report instead of creating a duplicate.

### Timestamp Format

Use ISO 8601 UTC: `2026-03-22T17:30:00Z`

## Agent Responsibilities

### All Agents (new protocol)
- **NEVER silently skip a tool or capability.** If a tool fails or is missing, that's a failure — trigger this pipeline.
- **NEVER mark a task "done" if you couldn't complete it fully.** Partial completion with known gaps = failure signal.
- Write failure reports immediately — don't wait for the user to discover the problem.

### Gandalf (Lead)
- Receives all failure signals
- Creates issues for non-trivial failures
- Routes to Elrond with specific research questions
- Reviews Elrond's output before implementation proceeds
- On second rejection: immediately escalates to Jonathan — don't loop a third time
- Escalates to Jonathan only when the pipeline is stuck

### Elrond (Researcher)
- Always uses `claude-opus-4.6` for failure research
- Always checks reference codebases first (`docs/catalogs/reference-codebases.md`)
- Produces tested, working solutions — not theoretical suggestions
- **If no solution found:** Explicitly state "No solution found — escalating to Jonathan" with full reasoning. This triggers Gandalf to notify Jonathan immediately via the dispatcher:
  ```powershell
  .\scripts\notify-squad-event.ps1 -Event "blocked" `
      -What "Failure recovery exhausted: <task>" `
      -Why "Elrond found no viable solution after researching <topic>" `
      -ActionNeeded "<what Jonathan should do>" `
      -Link "<issue URL>" -Urgency "decision-needed" -Agent "Elrond"
  ```
  Also tag the GitHub issue `needs-human`.

### Ralph (Work Monitor)
- Triages implementation routing after Gandalf approves research
- Updates board with new issues/tasks
- Tracks the retry of the original task
- Closes the failure report issue once the original task retries successfully

### Galadriel (Reviewer)
- Reviews the fix PR the same as any other PR
- On approval and merge, signals the original agent to retry

### Scribe (Logger)
- Logs each pipeline activation in the orchestration log (`.squad/orchestration-log/`)
- Merges failure report inbox entries into `decisions.md` after pipeline completes
- Does NOT hold up the pipeline — logging is background work

## Examples

### Example 1: Galadriel can't read PR files (ADO MCP gap)
1. Galadriel writes failure: "Can't read file contents for PR review — ado-repo_get_file_contents doesn't exist"
2. Gandalf creates issue #86, tasks Elrond
3. Elrond researches → finds `az devops invoke` with `includeContent=true` works
4. Gandalf approves the approach
5. Ralph routes to Gimli: "Add az devops file read helper"
6. Gimli builds it, Galadriel reviews the PR
7. Galadriel retries the original PR review with the new tool

### Example 2: ICM scan doesn't execute
1. Scheduler prints AGENT_TASK but nobody spawns Aragorn
2. Gandalf creates issue, tasks Elrond
3. Elrond researches event-triggers implementation → finds direct IcM REST API approach
4. Gandalf approves
5. Ralph routes to Gimli: "Rewrite icm-scan.ps1 to query IcM directly"
6. Gimli builds, Galadriel reviews
7. ICM scan runs end-to-end in next scheduler tick
