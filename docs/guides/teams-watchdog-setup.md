---
title: "Teams Watchdog Daily Summary Setup"
date: 2024-12-19
author: Gimli
documentarian: Bilbo
category: guide
tags: [guide, tooling, teams, workflow, bilbo, final]
status: final
---

# Teams Watchdog Daily Summary Setup

Learn how to deploy a daily Teams message monitor that extracts Jonathan's decisions and action items automatically.

## What It Does

Teams Watchdog is a lightweight daily monitor that scans Microsoft Teams messages over a 24-hour window, filters for Jonathan's activity, and produces a markdown summary of key decisions and action items. Use this to:

- Track what Jonathan decided without reading chat history
- Extract action items that need squad attention
- Maintain a searchable daily log of decisions
- Run on a schedule or execute on-demand

## Prerequisites

- **PowerShell 7+** (Core or Desktop)
- **GitHub CLI (`gh`)** installed and authenticated
- **Copilot CLI** installed with WorkIQ MCP tools enabled
- **`.squad/decisions.md`** in the repo root (for decision tracking)
- **Permission to run PowerShell scripts locally**

## How It Works: The 2-Step Pipeline

Teams Watchdog uses a file-based pipeline optimized for cost and reliability:

1. **Probe** (`probe-messages.ps1`) — Fetches raw Teams messages from the past 24 hours via the WorkIQ MCP tool
2. **Scan** (`single-agent-scan.ps1`) — Filters to Jonathan's messages, extracts decisions and action items, formats as markdown

Output: A dated markdown file at `.squad/skills/teams-watchdog/work/summary-YYYY-MM-DD.md`

## Step-by-Step Setup

### 1. Verify Script Location

The scripts are located at:
```
.squad/skills/teams-watchdog/
├── run-pipeline.ps1           # Orchestrator
├── probe-messages.ps1         # Step 1: fetch
├── single-agent-scan.ps1      # Step 2: scan + format
├── watchdog.ps1               # Daemon loop
└── work/                       # Output directory (auto-created)
```

### 2. Run the Pipeline Once (Test)

From the repo root, execute:

```powershell
.\.squad\skills\teams-watchdog\run-pipeline.ps1
```

Expected output:
```
[HH:mm:ss] probe... done
[HH:mm:ss] scan... done
[HH:mm:ss] Pipeline complete — .squad/skills/teams-watchdog/work/summary-YYYY-MM-DD.md
```

Check the generated markdown file in the `work/` directory.

### 3. Configure Daily Execution (Optional)

To run as a daily watchdog loop, use:

```powershell
.\.squad\skills\teams-watchdog\watchdog.ps1
```

This starts an infinite loop that:
- Runs the pipeline every 24 hours
- Maintains a lockfile to prevent duplicate instances
- Logs results to `watchdog.log` and `watchdog-heartbeat.json`
- Alerts after 3 consecutive failures

To stop, press `Ctrl+C` or kill the process.

### 4. Schedule as a Cron Job (Advanced)

**On macOS/Linux:**
```bash
crontab -e
# Add: 0 9 * * * /usr/bin/pwsh -NoProfile -Command ". ~/.squad/teams-watchdog/run-pipeline.ps1"
```

**On Windows Task Scheduler:**
1. Open Task Scheduler
2. Create Basic Task → Name it "Teams Watchdog"
3. Trigger: Daily at 9:00 AM
4. Action: Start a program
   - Program: `C:\Program Files\PowerShell\7\pwsh.exe`
   - Arguments: `-NoProfile -Command ".\.squad\skills\teams-watchdog\run-pipeline.ps1"`
   - Start in: `C:\path\to\repo` (the repo root)

## Configuration Options

### Custom Time Window

By default, the pipeline looks back 24 hours. To scan a different window:

```powershell
# Scan the last 12 hours
.\.squad\skills\teams-watchdog\run-pipeline.ps1 -Hours 12

# Scan the last 48 hours
.\.squad\skills\teams-watchdog\run-pipeline.ps1 -Hours 48
```

**Note:** The 24-hour default is intentional for daily runs — it prevents duplicate summaries on overlapping scan windows.

### Custom Watchdog Interval

To run the watchdog loop with a different interval:

```powershell
# Run every 12 hours
.\.squad\skills\teams-watchdog\watchdog.ps1 -IntervalHours 12

# Run every 6 hours
.\.squad\skills\teams-watchdog\watchdog.ps1 -IntervalHours 6
```

## Output Format: Reading the Summary

Each summary is a markdown file with this structure:

```markdown
# Teams Messages — 2024-12-19 (24h lookback)

## Jonathan's Decisions

- **[9:15 AM]** Decision: Escalate watchdog to daily cron
- **[2:30 PM]** Decision: Approve squad structure refresh

## Action Items

- [ ] Gimli: Review watchdog logs for errors
- [ ] Aragorn: Update livesite runbook with new escalation path

## Key Quotes

> "We need to reduce the daily summary to decisions only, not every message." — Jonathan

## Raw Message Count

- Total messages in window: 142
- Filtered to Jonathan: 8
- Decisions extracted: 3
- Action items extracted: 5
```

## Troubleshooting

### "No semantic model. Run semantic-index.ps1 first"

The semantic model isn't built yet. Run:
```powershell
.\scripts\semantic-index.ps1
```

Then try the pipeline again.

### "Webhook URL file not found" or WorkIQ Errors

The pipeline uses the WorkIQ MCP tool to fetch Teams messages. If you see errors:

1. Verify Copilot CLI is installed: `copilot --version`
2. Check WorkIQ is available: `copilot -p "list my Teams chats"`
3. Ensure you have Teams access in your Microsoft 365 account

If WorkIQ is unavailable in your environment, you can manually extract messages from Teams and save them to `work/01-raw.txt`, then run only the scan step:

```powershell
.\single-agent-scan.ps1 -InputFile work/01-raw.txt
```

### Pipeline Exits with "FAILED at: probe"

The probe step (WorkIQ fetch) failed. This typically means:
- WorkIQ MCP tool is not available
- You don't have permission to access Teams messages
- Your Microsoft 365 account doesn't have Teams enabled

Check by running:
```powershell
copilot -p --allow-tool=workiq "Summarize the last 3 messages in my Teams channels"
```

### Pipeline Exits with "FAILED at: scan"

The scan step (Copilot filter+extract) failed. This usually means:
- Copilot CLI crashed or is unavailable
- The `probe` output was malformed or empty
- `single-agent-scan.ps1` encountered a parse error

Check the raw probe output manually:
```powershell
.\probe-messages.ps1 | more
```

If raw output is empty, the issue is in the probe step (see above).

### Watchdog Logs Repeated Failures

If `watchdog.log` shows 3+ consecutive failures:

1. Check the most recent summary file:
   ```powershell
   Get-ChildItem .\.squad\skills\teams-watchdog\work\ | Sort-Object LastWriteTime -Descending | Select-Object -First 1
   ```

2. Run the pipeline manually to see the error:
   ```powershell
   .\.squad\skills\teams-watchdog\run-pipeline.ps1
   ```

3. If the manual run succeeds, the watchdog may have stale locks or temporary network issues. Check the lockfile:
   ```powershell
   Get-Content .\.squad\skills\teams-watchdog\.watchdog.lock
   ```

4. If the lockfile shows a dead process, remove it:
   ```powershell
   Remove-Item .\.squad\skills\teams-watchdog\.watchdog.lock -Force
   ```

## Cost & Performance

| Metric | Value |
|--------|-------|
| Cost per run | ~4–6 premium Copilot requests |
| Typical duration | 20–40 seconds |
| Messages scanned | ~100–300 per day |
| Summary size | ~2–5 KB markdown |

The single-agent scan approach (combined filter+extract+format in one call) reduced cost by ~50% compared to the earlier three-step pipeline.

## Advanced: Manual Workflow

If you want to inspect intermediate outputs, you can run each step separately:

```powershell
# Step 1: Fetch raw Teams messages (save to file)
.\probe-messages.ps1 -Hours 24 > work/01-raw.txt

# Inspect the raw output
Get-Content work/01-raw.txt | head -50

# Step 2: Filter, extract, and format
.\single-agent-scan.ps1 -InputFile work/01-raw.txt -Hours 24 > work/summary-YYYY-MM-DD.md

# View the result
Get-Content work/summary-YYYY-MM-DD.md
```

## Next Steps

- **For Gimli (tooling):** Monitor the watchdog logs for patterns — are certain decision types underrepresented? Should we add tagging?
- **For Jonathan (team lead):** Review generated summaries weekly — are they capturing the decisions you care about?
- **For the squad:** Store summaries in the knowledge base — link decisions to GitHub issues for traceability

## Related

- `.squad/decisions.md` — Team decision log (where watchdog decisions should be merged)
- `.squad/decisions/inbox/` — Where watchdog can write extracted decisions
- `docs/guides/teams-notifications-setup.md` — How to send notifications TO Teams
- `.squad/skills/teams-watchdog/SKILL.md` — Architectural details and cost analysis
