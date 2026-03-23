# Teams Watchdog

Daily Teams message scanner that summarizes Jonathan's decisions, action items, and key context.

## How It Works

Two-step pipeline using `copilot -p` and the WorkIQ MCP tool:

1. **probe-messages.ps1** — Fetches raw Teams messages via `workiq-ask_work_iq`
2. **single-agent-scan.ps1** — Filters to Jonathan's messages, extracts decisions/action items, formats as markdown (replaces old 3-step filter→extract→format pipeline)

Output: dated markdown summary at `work/summary-YYYY-MM-DD.md`.

## Usage

```powershell
# Run the pipeline once (24h lookback)
.\.squad\skills\teams-watchdog\run-pipeline.ps1

# Custom lookback window
.\.squad\skills\teams-watchdog\run-pipeline.ps1 -Hours 12

# Run as a daily watchdog loop
.\.squad\skills\teams-watchdog\watchdog.ps1 -IntervalHours 24
```

## Scripts

| Script | Purpose | Lines |
|--------|---------|-------|
| `run-pipeline.ps1` | Orchestrates probe → scan, file-based I/O | ~35 |
| `probe-messages.ps1` | Fetches raw Teams messages via WorkIQ | ~28 |
| `single-agent-scan.ps1` | Single-agent filter+extract+format | ~45 |
| `watchdog.ps1` | Loop with lockfile, heartbeat, structured log | ~46 |
| `poc-single-agent-scan.ps1` | Original POC (kept for reference) | ~48 |

## Cost Per Run

| Step | Premium Requests |
|------|-----------------|
| Probe (WorkIQ fetch) | ~2–3 |
| Scan (filter+extract+format) | ~2–3 |
| **Total** | **~4–6** |

The single-agent scan replaced three separate `copilot -p` calls (filter, extract, format), reducing cost by ~50%.

## Constraints

- Uses `copilot -p` (not `gh copilot`) — per team decision
- `--allow-tool='workiq'` grants non-interactive WorkIQ consent
- File-based I/O between steps (copilot output cannot be captured in variables)
- 24h scan window (not 48h) to avoid duplicate summaries on daily runs
- WorkIQ has 5–60 minute indexing delay — acceptable for daily summaries

## Architecture Decision

See `docs/research/teams-monitor-adaptation-research.md` for Elrond's analysis of Tamir's teams-monitor pattern and the rationale for the single-agent approach.
