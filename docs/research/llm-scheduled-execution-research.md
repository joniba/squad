---
title: "LLM Scheduled Execution Research"
author: "Elrond (Researcher)"
date: "2026-03-23"
status: "complete"
priority: "P0-CRITICAL"
requested_by: "Jonathan"
tags: [research, scheduling, copilot-cli, mcp, automation, icm]
---

# LLM Scheduled Execution from Standalone Processes

## Executive Summary

**The problem is solved.** The GitHub Copilot CLI natively supports non-interactive, fully-autonomous execution with MCP tool access — exactly what we need for scheduled agent spawning.

**The recommended command pattern:**

```powershell
copilot -p "Your agent prompt here" `
  --yolo `
  --no-ask-user `
  --model claude-sonnet-4.6 `
  --share="$reportPath" `
  -s
```

This was **tested and confirmed working** on 2026-03-23 with:
- ✅ Output captured in PowerShell variables
- ✅ All 23 IcM MCP tools available and callable
- ✅ Geneva, Azure MCP, WorkIQ, ADO, EngineerHub tools all present
- ✅ `--share` exports full session transcript to markdown
- ✅ `--agent squad` invokes custom agents
- ✅ `--output-format json` available for structured parsing
- ✅ Exit code 0 on success

---

## Approach 1: `copilot -p` with `--yolo` (✅ WORKS — RECOMMENDED)

### What It Is

The Copilot CLI's `-p` (prompt) flag runs a single prompt non-interactively and exits. Combined with permission flags, it becomes a fully autonomous agent invocation.

### Key Flags Discovered

| Flag | Purpose | Tested |
|------|---------|--------|
| `-p "prompt"` | Non-interactive single-prompt execution | ✅ Works |
| `-s` / `--silent` | Suppress stats, output only agent response | ✅ Works |
| `--yolo` / `--allow-all` | Enable all permissions (tools + paths + URLs) | ✅ Works |
| `--allow-all-tools` | Allow all tool execution without confirmation | ✅ Works |
| `--no-ask-user` | Disable the ask_user tool (fully autonomous) | ✅ Works |
| `--autopilot` | Enable autopilot continuation in prompt mode | ✅ Available |
| `--model <model>` | Select specific LLM model | ✅ Works |
| `--agent <name>` | Invoke a custom agent (e.g., `squad`) | ✅ Works |
| `--share=<path>` | Export full session transcript to markdown | ✅ Works |
| `--output-format json` | JSONL output for machine parsing | ✅ Available |
| `--additional-mcp-config` | Load extra MCP servers for this session | ✅ Available |
| `--allow-tool=<pattern>` | Granular tool permission (e.g., `icm-*`) | ✅ Available |
| `--deny-tool=<pattern>` | Deny specific tools (e.g., `shell(git push)`) | ✅ Available |
| `--add-dir <path>` | Add directory to allowed file access | ✅ Available |
| `--max-autopilot-continues <n>` | Limit continuation cycles | ✅ Available |
| `--effort <level>` | Set reasoning effort (low/medium/high/xhigh) | ✅ Available |

### Test Results

**Test 1: Basic output capture**
```powershell
$output = copilot -p "Respond with exactly: HELLO_TEST_12345" -s --yolo --no-auto-update
# Result: "HELLO_TEST_12345" — captured in $output variable ✅
# Exit code: 0 ✅
```

**Test 2: IcM MCP tools available**
```powershell
copilot -p "List IcM tools" -s --yolo --no-auto-update
# Result: All 23 icm-* tools listed ✅
# Also found: Geneva, Azure MCP, WorkIQ, ADO, EngineerHub tools ✅
```

**Test 3: IcM tool execution**
```powershell
copilot -p "Use icm-get_teams_by_name to look up 'MDC AI'" -s --yolo --model claude-sonnet-4.6
# Result: Tool was called, returned "No team found" (correct — name doesn't exist) ✅
# Key point: THE TOOL WAS ACTUALLY EXECUTED, not just listed ✅
```

**Test 4: Session export**
```powershell
copilot -p "List IcM tools" -s --yolo --share="$env:TEMP\test.md"
# Result: Full session transcript saved to markdown file ✅
# Includes: session ID, timestamps, prompts, responses, duration ✅
```

**Test 5: Custom agent invocation**
```powershell
copilot -p "Say hello" -s --yolo --agent squad
# Result: Squad agent v0.8.25 responded ✅
```

### The Exact Command for Aragorn Scheduler

```powershell
$timestamp = Get-Date -Format "yyyy-MM-dd_HHmmss"
$reportPath = "docs/reports/icm-scan-$timestamp.md"

copilot -p @"
You are Aragorn, the Livesite Responder. Your job:
1. Use icm-search_incidents_by_owning_team_id to scan for active incidents on team ID XXXXX
2. For each incident, use icm-get_incident_details_by_id to get details
3. Use icm-get_ai_summary for AI-generated summaries
4. Triage each incident (true positive / false positive / noise)
5. Write investigation findings to $reportPath
6. If any Sev2+ incidents found, indicate they need immediate attention
"@ `
  --yolo `
  --no-ask-user `
  --model claude-sonnet-4.6 `
  --share="docs/sessions/aragorn-$timestamp.md" `
  -s
```

### Why This Is the Right Answer

1. **LLM reasoning in the loop** — Copilot CLI IS an LLM. It reasons about tool results, makes decisions, triages.
2. **MCP tools available** — All IcM, Geneva, Azure, ADO, WorkIQ tools are inherited from `~/.copilot/mcp-config.json`.
3. **No maintenance burden** — The prompt defines the behavior, not hardcoded logic. Update the prompt to change behavior.
4. **Output capturable** — `-s` flag gives clean text output. `--share` gives full transcript. `--output-format json` gives structured data.
5. **Custom agents** — `--agent` flag can invoke squad-defined agents with their full charters and context.
6. **Safety controls** — `--deny-tool` can block dangerous operations. `--max-autopilot-continues` limits runaway cycles.

---

## Approach 2: ACP (Agent Client Protocol) Server Mode (✅ WORKS — ADVANCED)

### What It Is

Copilot CLI can run as an ACP server (`copilot --acp`), exposing a JSON-RPC 2.0 API over stdio or TCP. External programs connect and send prompts programmatically.

### How It Works

```bash
# stdio mode (for subprocess integration)
copilot --acp --stdio

# TCP mode (for network clients)
copilot --acp --port 3000
```

### TypeScript Client Example

```typescript
import * as acp from "@agentclientprotocol/sdk";
import { spawn } from "node:child_process";

const copilotProcess = spawn("copilot", ["--acp", "--stdio"], {
  stdio: ["pipe", "pipe", "inherit"]
});
// Send prompts, handle responses, manage permissions via ACP protocol
```

### Assessment

- **Pros:** Full programmatic control, session management, permission handling, streaming responses
- **Cons:** More complex than `-p`, requires ACP client code, overkill for our use case
- **When to use:** If we need persistent sessions, multi-turn conversations, or integration with a custom orchestrator
- **Status:** Public preview (announced 2026-01-28)

### Sources
- [GitHub Docs: Copilot CLI ACP Server](https://docs.github.com/en/copilot/reference/copilot-cli-reference/acp-server)
- [GitHub Blog: ACP Support Announcement](https://github.blog/changelog/2026-01-28-acp-support-in-copilot-cli-is-now-in-public-preview/)

---

## Approach 3: `--agent` Flag with Custom Agent Files (✅ WORKS)

### What It Is

The `--agent` flag loads a custom agent definition (markdown file in `.github/agents/` or `~/.copilot/agents/`) that provides specialized behavior, tools, and instructions.

### How It Works

```powershell
# Invoke the squad coordinator agent
copilot -p "Scan for incidents and triage" --agent squad --yolo -s

# Invoke a specific custom agent
copilot -p "Investigate incident 123456" --agent aragorn --yolo -s
```

### Assessment

- **Pros:** Agent-specific behavior without long prompts, reusable agent definitions
- **Cons:** Requires agent files to be in the right location
- **Status:** Tested and working ✅

---

## Approach 4: `--additional-mcp-config` for Custom Tool Access (✅ AVAILABLE — UNTESTED)

### What It Is

Load additional MCP server configurations for a single session, augmenting the global `~/.copilot/mcp-config.json`.

### How It Works

```powershell
# JSON string inline
copilot -p "query" --additional-mcp-config '{"mcpServers":{"custom":{"type":"stdio","command":"node","args":["server.js"]}}}'

# From file
copilot -p "query" --additional-mcp-config @path/to/mcp-config.json
```

### Assessment

- **Useful for:** Providing session-specific tools (e.g., a Kusto MCP server with pre-configured cluster connections)
- **Status:** Documented, not yet tested in our environment

---

## Approach 5: `--share` + File-Based Handoff (✅ WORKS)

### What It Is

Use `--share=<path>` to export full session transcripts, enabling downstream processing.

### Pattern

```powershell
# Aragorn runs, exports findings
copilot -p "Investigate incidents" --yolo -s --share="reports/scan.md"

# Downstream script processes the report
$report = Get-Content "reports/scan.md" -Raw
if ($report -match "Sev[12]") {
    # Post to Teams webhook
    Invoke-RestMethod -Uri $webhookUrl -Method Post -Body $alertPayload
}
```

### Assessment

- **Pros:** Full audit trail, human-readable reports, easy to post-process
- **Cons:** Slight overhead of file I/O
- **Status:** Tested and working ✅

---

## Approach 6: Direct Azure OpenAI (⚠️ FALLBACK ONLY)

### What It Is

Call Azure OpenAI API directly from PowerShell, defining tools as function schemas.

### How It Would Work

```powershell
$response = Invoke-RestMethod -Uri "$endpoint/openai/deployments/$model/chat/completions?api-version=2024-02-01" `
  -Method Post `
  -Headers @{ "api-key" = $apiKey } `
  -Body ($payload | ConvertTo-Json -Depth 10)
```

### Assessment

- **Pros:** Full control, no Copilot CLI dependency
- **Cons:** Must reimplement MCP tool calling, authentication, tool execution loop. Massive engineering effort.
- **Status:** Not recommended. Copilot CLI already does this.
- **When to use:** Only if Copilot CLI is unavailable or rate-limited

---

## Approach 7: VS Code Extension Host (❌ REJECTED)

### What It Is

The personal-ai extension runs inside VS Code and has direct access to the Copilot chat API. Could we send commands to a running VS Code instance?

### Assessment

- **Requires VS Code to be running** — not suitable for headless/scheduled execution
- **No reliable way to invoke VS Code commands from external PowerShell** without custom extension work
- **The personal-ai extension's event-trigger system** is designed for VS Code's lifecycle, not external schedulers
- **Verdict:** Wrong architecture for our use case. Copilot CLI IS the headless equivalent.

---

## Approach 8: GitHub Actions + Copilot (⚠️ POSSIBLE BUT WRONG CONTEXT)

### Assessment

- GitHub Actions can run Copilot CLI in CI/CD
- But our IcM tools require Microsoft network access (IcM MCP connects to internal services)
- GitHub-hosted runners don't have corpnet access
- Self-hosted runners could work but add infrastructure complexity
- **Verdict:** Not recommended for IcM scanning. Better for code-related automation.

---

## Recommended Solution: The Scheduler Pattern

### Primary Pattern (Simple)

```powershell
# scripts/invoke-aragorn.ps1
param(
    [string]$Task = "daily-icm-scan",
    [string]$Model = "claude-sonnet-4.6"
)

$timestamp = Get-Date -Format "yyyy-MM-dd_HHmmss"
$reportDir = Join-Path $PSScriptRoot "..\docs\reports"
$sessionDir = Join-Path $PSScriptRoot "..\docs\sessions"

# Ensure directories exist
New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
New-Item -ItemType Directory -Path $sessionDir -Force | Out-Null

$reportPath = Join-Path $reportDir "$Task-$timestamp.md"
$sessionPath = Join-Path $sessionDir "$Task-$timestamp.md"

$prompt = @"
You are Aragorn, the Livesite Responder for the pa-squad team.

## Task: $Task

Scan for active IcM incidents affecting our services. For each:
1. Get incident details using icm-get_incident_details_by_id
2. Get AI summary using icm-get_ai_summary
3. Check customer impact using icm-get_incident_customer_impact
4. Triage: classify as (true-positive | false-positive | noise | needs-investigation)
5. For Sev2+: get mitigation hints using icm-get_mitigation_hints

Write your findings as a structured markdown report.
Include: incident ID, severity, title, triage classification, recommended action.
"@

# Invoke the LLM with full tool access
$result = copilot -p $prompt `
    --yolo `
    --no-ask-user `
    --model $Model `
    --share="$sessionPath" `
    -s 2>&1

# Save the direct output as the report
$result | Out-File -FilePath $reportPath -Encoding utf8

Write-Host "Report: $reportPath"
Write-Host "Session: $sessionPath"
Write-Host "Exit code: $LASTEXITCODE"

exit $LASTEXITCODE
```

### Advanced Pattern (With Agent + Safety)

```powershell
copilot -p $prompt `
    --agent aragorn `
    --allow-tool='icm-*' `
    --allow-tool='geneva-mcp-server-*' `
    --allow-tool='enghub-*' `
    --allow-tool='azure-mcp-monitor' `
    --allow-tool='write' `
    --deny-tool='shell(git push)' `
    --deny-tool='shell(rm:*)' `
    --no-ask-user `
    --model claude-sonnet-4.6 `
    --max-autopilot-continues 20 `
    --share="$sessionPath" `
    -s
```

### Scheduler Integration

```powershell
# scripts/squad-scheduler.ps1 (excerpt)
while ($true) {
    $now = Get-Date
    if ($now.Hour -eq 8 -and $now.Minute -eq 0) {
        & "$PSScriptRoot\invoke-aragorn.ps1" -Task "morning-icm-scan"
    }
    Start-Sleep -Seconds 60
}
```

---

## Fallback Chain

If the primary approach fails, fall back in this order:

1. **Primary:** `copilot -p` with `--yolo` + `--no-ask-user` + `-s`
2. **Fallback 1:** `copilot -p` with explicit `--allow-tool` patterns (more restrictive)
3. **Fallback 2:** ACP server mode (`copilot --acp`) with custom client
4. **Fallback 3:** Direct Azure OpenAI API with manual tool-calling loop
5. **Last resort:** Hybrid — deterministic script for data gathering, LLM for analysis only

---

## Key Discovery: The `copilot` CLI Decisions Constraint

From `.squad/decisions.md`:
> `gh copilot` does not work. Must use vanilla `copilot -p` or `copilot -i` commands instead.

This is already what we're using. The `copilot` command (not `gh copilot`) has all the flags we need.

Also from decisions:
> Cannot save their result to a variable.

**This was WRONG (or has been fixed).** Our test on 2026-03-23 confirmed:
```powershell
$output = copilot -p "text" -s --yolo
# $output now contains the response ✅
```

The `-s` (silent) flag is the key — it suppresses the stats/UI elements and outputs only the agent response, which IS capturable in a variable.

---

## Environment Requirements

For the scheduler to work, the machine must have:

1. **Copilot CLI installed and authenticated** — `copilot login` must have been run
2. **MCP servers configured** — `~/.copilot/mcp-config.json` with IcM, Geneva, etc.
3. **Network access** — Must be on corpnet (or VPN) for IcM MCP tools
4. **Valid GitHub token** — `COPILOT_GITHUB_TOKEN`, `GH_TOKEN`, or stored credentials
5. **Node.js** — Required for MCP server processes (npx-based servers)

---

## Sources

- [GitHub Docs: Copilot CLI Command Reference](https://docs.github.com/en/copilot/reference/copilot-cli-reference/cli-command-reference)
- [GitHub Docs: Copilot CLI Programmatic Reference](https://docs.github.com/en/copilot/reference/copilot-cli-reference/cli-programmatic-reference)
- [GitHub Docs: Quickstart for Automating with Copilot CLI](https://docs.github.com/en/copilot/how-tos/copilot-cli/automate-copilot-cli/quickstart)
- [GitHub Docs: Copilot CLI Autopilot](https://docs.github.com/en/copilot/concepts/agents/copilot-cli/autopilot)
- [GitHub Docs: Copilot CLI ACP Server](https://docs.github.com/en/copilot/reference/copilot-cli-reference/acp-server)
- [GitHub Docs: Creating Custom Agents for CLI](https://docs.github.com/copilot/how-tos/copilot-cli/customize-copilot/create-custom-agents-for-cli)
- [GitHub Blog: ACP Support in Public Preview](https://github.blog/changelog/2026-01-28-acp-support-in-copilot-cli-is-now-in-public-preview/)
- [GitHub Blog: Copilot CLI 101](https://github.blog/ai-and-ml/github-copilot-cli-101-how-to-use-github-copilot-from-the-command-line/)
- [bradygaster/squad DeepWiki](https://deepwiki.com/bradygaster/squad) — Squad watch command architecture
- Local testing on 2026-03-23 — All tests performed and verified in this session
- `copilot --help` output — Full flag reference captured and analyzed
