---
title: Tools & Integrations Catalog
author: Bilbo
date: 2026-03-23
last_updated: 2026-03-23
tags: [catalog, tools, integrations, mcp, infrastructure, reference]
---

# Tools & Integrations Catalog

> Comprehensive inventory of every tool, integration, API, database, and reference source used by the pa-squad project.

**Last Updated:** 2026-03-23

---

## Table of Contents

1. [MCP Servers](#1-mcp-servers)
2. [Databases & Query Engines](#2-databases--query-engines)
3. [APIs (Direct, Non-MCP)](#3-apis-direct-non-mcp)
4. [CLI Tools](#4-cli-tools)
5. [Reference Codebases](#5-reference-codebases)
6. [Scheduled Automation](#6-scheduled-automation)
7. [Notification Channels](#7-notification-channels)
8. [Authentication & Access](#8-authentication--access)

---

## 1. MCP Servers

MCP (Model Context Protocol) servers provide tool functions to agents during Copilot CLI sessions. Configured globally via `~/.copilot/mcp-config.json`.

### 1.1 IcM (Incident Management)

**Type:** MCP Server  
**Used by:** Aragorn (Operator), icm-investigator skill, `icm-scan.ps1`  
**Managed in:** `.squad/agents/aragorn/charter.md`, `.squad/skills/icm-investigator/SKILL.md`

| MCP Tool | Purpose | Used By |
|----------|---------|---------|
| `icm-get_incident_details_by_id` | Full incident metadata (severity, owning team, status, timestamps) | Aragorn |
| `icm-get_ai_summary` | AI-generated incident summary | Aragorn |
| `icm-get_incident_context` | Discussions, monitor triggers, enrichment data | Aragorn |
| `icm-get_impacted_services_regions_clouds` | Blast radius — affected services, regions, clouds | Aragorn |
| `icm-get_incident_customer_impact` | Formal impact metrics | Aragorn |
| `icm-get_support_requests_crisit` | Customer escalations and CritSit links | Aragorn |
| `icm-get_incident_location` | Region, AZ, DC, cluster, node details | Aragorn |
| `icm-get_similar_incidents` | Historical pattern matching | Aragorn |
| `icm-get_mitigation_hints` | Historical mitigation steps | Aragorn |
| `icm-get_impacted_s500_customers` | S500 customer impact list | Aragorn |
| `icm-get_impacted_ace_customers` | ACE customer impact list | Aragorn |
| `icm-get_impacted_azure_priority0_customers` | Priority 0 / Life & Safety customer impact | Aragorn |
| `icm-get_outage_high_priority_events` | High priority outage events | Aragorn |
| `icm-is_specific_customer_impacted` | Check if a named customer is affected | Aragorn |
| `icm-get_impacted_subscription_count` | Impacted subscription count | Aragorn |
| `icm-get_teams_by_name` | Resolve team by name | Aragorn |
| `icm-get_teams_by_public_id` | Resolve team by public ID | Aragorn |
| `icm-get_on_call_schedule_by_team_id` | On-call schedule lookup | Aragorn |
| `icm-search_incidents_by_owning_team_id` | Query incidents by owning team | Aragorn, icm-scan.ps1 |

**Auth:** Azure AD token (automatic via corpnet/CLI session)

---

### 1.2 Geneva Metrics

**Type:** MCP Server  
**Used by:** Aragorn (Operator), icm-investigator skill  
**Managed in:** `.squad/skills/icm-investigator/SKILL.md`

| MCP Tool | Purpose | Used By |
|----------|---------|---------|
| `geneva-mcp-server-metrics_handbook` | Read metrics documentation before querying | Aragorn |
| `geneva-mcp-server-list_metrics` | List available metrics in a namespace | Aragorn |
| `geneva-mcp-server-list_metric_preaggregations` | Discover queryable dimension sets | Aragorn |
| `geneva-mcp-server-list_dimension_values` | List dimension values for filtering | Aragorn |
| `geneva-mcp-server-query_timeseries` | Query timeseries data for metric anomalies | Aragorn |

**Auth:** Azure AD token (automatic)

---

### 1.3 EngHub (Engineering Hub / eng.ms)

**Type:** MCP Server  
**Used by:** Aragorn (Operator), icm-investigator skill  
**Managed in:** `.squad/agents/aragorn/charter.md`, `.squad/skills/icm-investigator/SKILL.md`

| MCP Tool | Purpose | Used By |
|----------|---------|---------|
| `enghub-search` | Search eng.ms for TSGs, docs, onboarding guides | Aragorn |
| `enghub-fetch` | Retrieve full page content from eng.ms | Aragorn |
| `enghub-resolve_service` | Resolve service name to ServiceTree GUID | Aragorn |
| `enghub-get_service_nodes` | List content nodes for a ServiceTree service | Aragorn |

**Auth:** Azure AD / corpnet (automatic)

---

### 1.4 Azure (Multiple Sub-Servers)

**Type:** MCP Server collection  
**Used by:** Aragorn (Operator), icm-investigator skill  
**Managed in:** `.squad/skills/icm-investigator/SKILL.md`

| MCP Tool | Purpose | Used By |
|----------|---------|---------|
| `azure-mcp-kusto` | Execute KQL queries against Kusto clusters | Aragorn |
| `azure-mcp-applens` | AI-powered Azure resource diagnostics | Aragorn |
| `azure-mcp-resourcehealth` | Azure resource availability status | Aragorn |
| `azure-mcp-monitor` | Azure Monitor logs and metrics queries | Aragorn |
| `azure-mcp-documentation` | Azure documentation search | Aragorn |
| `azure-mcp-get_azure_bestpractices` | Azure best practices guidance | Aragorn |

**Auth:** Azure CLI credentials (automatic)

---

### 1.5 WorkIQ (Microsoft 365 Copilot)

**Type:** MCP Server  
**Used by:** Teams Watchdog, Email Watchdog, icm-investigator skill  
**Managed in:** `.squad/skills/workiq-patterns/SKILL.md`, `.squad/skills/teams-watchdog/SKILL.md`, `.squad/skills/email-watchdog/SKILL.md`

| MCP Tool | Purpose | Used By |
|----------|---------|---------|
| `workiq-ask_work_iq` | Natural-language query of M365 data (Teams, email, meetings, files) | Teams Watchdog, Email Watchdog, Aragorn |
| `workiq-accept_eula` | Accept EULA for WorkIQ access (one-time) | Setup |

**Key constraints:**
- Indexing delay: minutes to hours (not real-time)
- Max ~25 results per query (curated, not exhaustive)
- 1 query per agent action (squad rate-limit convention)
- Requires M365 subscription with Copilot license

**Auth:** M365 Copilot license + admin consent + EULA acceptance

---

### 1.6 Azure DevOps (ADO)

**Type:** MCP Server  
**Used by:** Galadriel (Reviewer)  
**Managed in:** `.squad/agents/galadriel/charter.md`

| MCP Tool | Purpose | Used By |
|----------|---------|---------|
| `ado-repo_get_pull_request_by_id` | PR metadata, title, description, branches | Galadriel |
| `ado-repo_list_pull_request_threads` | Review threads and inline comments | Galadriel |
| `ado-repo_list_pull_request_thread_comments` | Comments within review threads | Galadriel |
| `ado-search_code` | File contents via code search (primary file reader) | Galadriel |
| `ado-repo_search_commits` | Commit history for a repo | Galadriel |

**Known limitation:** ADO MCP lacks `get_file_contents` — code search is the workaround. See `docs/research/ado-file-access-research.md`.

**Auth:** Azure AD / PAT (via ADO MCP config)

---

### 1.7 GitHub

**Type:** MCP Server  
**Used by:** All agents (implicit via Copilot CLI session)  
**Managed in:** Global MCP config

| MCP Tool | Purpose | Used By |
|----------|---------|---------|
| `github-mcp-server-get_file_contents` | Read files from GitHub repos | Elrond, various |
| `github-mcp-server-list_issues` | List GitHub issues | Gandalf, Ralph |
| `github-mcp-server-search_issues` | Search issues with filters | Gandalf |
| `github-mcp-server-list_pull_requests` | List pull requests | Galadriel |
| `github-mcp-server-pull_request_read` | PR details, diffs, reviews | Galadriel |
| `github-mcp-server-search_code` | Code search across GitHub repos | Elrond |

**Auth:** GitHub token (via `gh auth`)

---

### 1.8 ConfigGen

**Type:** MCP Server  
**Used by:** Available in session but not primary to any agent  
**Managed in:** Global MCP config

Available tools include `yoni-configgen-mcp-*` for ConfigGen framework operations. Not a primary tool for any current squad workflow but available for Azure infrastructure work.

---

## 2. Databases & Query Engines

### 2.1 IcM Kusto (Data Warehouse)

| Property | Value |
|----------|-------|
| **Type** | Kusto (Azure Data Explorer) |
| **Cluster** | `https://icmcluster.kusto.windows.net` |
| **Database** | `IcMDataWarehouse` |
| **Key Table** | `IncidentsSnapshotV2()` |
| **Used by** | Aragorn, icm-scan.ps1 |
| **Access method** | Via `azure-mcp-kusto` MCP tool or Kusto REST API directly |
| **Auth** | `az account get-access-token --resource https://icmcluster.kusto.windows.net/` |
| **Token expiry** | 45 minutes |
| **Documented in** | `docs/research/icm-scan-end-to-end-research.md` |

### 2.2 Copilot Session Store (SQLite)

| Property | Value |
|----------|-------|
| **Type** | SQLite |
| **Location** | `~/.copilot/session-store.db` |
| **Used by** | `agent-dashboard/get-session-summary.ps1` |
| **Access method** | Python sqlite3 module |
| **Purpose** | Query past Copilot session history for dashboard summaries |

### 2.3 Semantic Model (JSON)

| Property | Value |
|----------|-------|
| **Type** | JSON graph file |
| **Location** | `.squad/semantic-model.json` |
| **Used by** | `scripts/semantic-index.ps1`, `scripts/semantic-query.ps1` |
| **Purpose** | Entity-relationship graph of squad structure (agents, skills, docs, issues, PRs, commits) |

---

## 3. APIs (Direct, Non-MCP)

### 3.1 IcM REST API

| Property | Value |
|----------|-------|
| **Type** | REST API |
| **Used by** | icm-scan.ps1 (fallback path) |
| **Auth resource** | `https://microsofticm.onmicrosoft.com/IcmAPI` |
| **Purpose** | Fallback for restricted incidents when MCP tools can't access them |
| **Documented in** | `docs/research/icm-scan-end-to-end-research.md` |

### 3.2 Kusto REST API

| Property | Value |
|----------|-------|
| **Endpoint** | `POST {clusterUrl}/v1/rest/query` |
| **Used by** | icm-scan.ps1 (direct KQL execution) |
| **Auth** | Bearer token via `az account get-access-token` |
| **Purpose** | Direct Kusto queries bypassing MCP when needed |
| **Documented in** | `docs/research/icm-scan-end-to-end-research.md` |

### 3.3 Microsoft Teams Incoming Webhook

| Property | Value |
|----------|-------|
| **Type** | REST webhook (POST) |
| **Used by** | `scripts/send-teams-notification.ps1`, email-watchdog, squad-daily-summary |
| **Format** | Adaptive Cards (JSON, v1.0+) |
| **Rate limit** | 4 requests/second, 28 KB/message max |
| **URL storage** | `~/.squad/teams-webhook.url` |
| **Documented in** | `docs/guides/teams-notifications-setup.md` |

### 3.4 GitHub API (via `gh` CLI)

| Property | Value |
|----------|-------|
| **Type** | REST API (accessed via CLI wrapper) |
| **Used by** | `icm-scan.ps1`, `squad-daily-summary.ps1`, `semantic-index.ps1` |
| **Operations** | Issue list/create, PR list, JSON filtering |
| **Auth** | `gh auth token --hostname github.com` |

---

## 4. CLI Tools

### 4.1 Copilot CLI (`copilot`)

| Property | Value |
|----------|-------|
| **Type** | CLI (LLM orchestration) |
| **Used by** | Teams Watchdog, Email Watchdog, ICM Scan, all agent sessions |
| **Key flags** | `-p` (prompt), `--yolo`, `--no-ask-user`, `--model`, `--allow-tool`, `--deny-tool`, `-s` (session), `--share`, `--agent` |
| **Models used** | `claude-sonnet-4.6` (default), `claude-opus-4.6` (research tasks per decision) |
| **Documented in** | `docs/research/llm-scheduled-execution-research.md` |

**⚠️ Important:** Do NOT use `gh copilot` — it doesn't work. Always use `copilot -p` (per team decision 2026-03-23T02:18:56Z).

### 4.2 Git

| Property | Value |
|----------|-------|
| **Type** | CLI (version control) |
| **Used by** | All agents, all worktree scripts, semantic-index, scheduler |
| **Key operations** | `worktree add/remove/list/prune`, `branch`, `log`, `diff-tree`, `rev-parse`, `checkout`, `push` |
| **Conventions** | Commit format: `{type}({scope}): {description}`. Push requires explicit user permission. |

### 4.3 GitHub CLI (`gh`)

| Property | Value |
|----------|-------|
| **Type** | CLI (GitHub API wrapper) |
| **Used by** | `icm-scan.ps1`, `squad-daily-summary.ps1`, `semantic-index.ps1` |
| **Key operations** | `issue list`, `issue create`, `pr list` (with `--json` and `jq` filtering) |
| **Auth** | OAuth token via `gh auth login` |

### 4.4 Azure CLI (`az`)

| Property | Value |
|----------|-------|
| **Type** | CLI (Azure management) |
| **Used by** | Galadriel (ADO file access fallback), token generation |
| **Key operations** | `az devops invoke --area git --resource items`, `az account get-access-token` |
| **Documented in** | `docs/research/ado-file-access-research.md` |

### 4.5 Python (`py`)

| Property | Value |
|----------|-------|
| **Type** | CLI (scripting runtime) |
| **Used by** | `agent-dashboard/get-session-summary.ps1` |
| **Purpose** | Embedded SQLite queries against Copilot session store |

### 4.6 squad-monitor

| Property | Value |
|----------|-------|
| **Type** | CLI (dashboard renderer) |
| **Source** | `https://github.com/tamirdresher/squad-monitor` |
| **Used by** | `agent-dashboard/start-dashboard.ps1` |
| **Purpose** | Real-time dashboard for squad agent activity |

---

## 5. Reference Codebases

These are not tools but context and reference sources used for research, patterns, and code understanding.

### 5.1 Personal AI Companion (Upstream Reference)

| Property | Value |
|----------|-------|
| **Path** | `C:\dev\defender\MDC-AI-Shared\extensions\personal-ai\` |
| **Owner** | Tamir Dresher |
| **Language** | TypeScript + PowerShell |
| **Purpose** | Upstream system the squad is modeled after |
| **Key areas** | IcM integration (`src/icm/`), PR review (`skills/pr-reviewer/`), MCP configs (`mcp-configs/`), cron system (`src/cron/`), notifications (`src/notifications/`) |
| **Used by** | Elrond (research), all agents (pattern reference) |
| **Documented in** | `docs/catalogs/reference-codebases.md` |

### 5.2 TI Pipeline Repositories

| Property | Value |
|----------|-------|
| **Path** | `C:\dev\ti\*` (28 repos) |
| **Key repos** | `Sentinel-TiPipeline`, `Sentinel-TiCommon`, `Sentinel-TiIngestion`, `Sentinel-TiPublishers`, `Sentinel-TiAutomation`, `Sentinel-TiAITools`, `Sentinel-TiActionPipeline`, `Sentinel-TiSharedInfra-Resources` |
| **Purpose** | Source code for RCA during incidents, PR review target repos |
| **Used by** | Aragorn (incident investigation), Galadriel (PR review) |
| **Documented in** | `docs/research/ado-file-access-research.md` |

### 5.3 Squad Skills Library (GitHub)

| Property | Value |
|----------|-------|
| **URL** | `https://github.com/tamirdresher/squad-skills` |
| **Content** | 20+ reusable skill plugins (teams-monitor, incident-response, restart-recovery, etc.) |
| **Purpose** | Plugin library for extending squad capabilities |
| **Documented in** | `docs/catalogs/squad-skills-catalog.md` |

### 5.4 Snap Squad (paulyuk/snap-squad)

| Property | Value |
|----------|-------|
| **URL** | `https://github.com/paulyuk/snap-squad` |
| **Content** | CLI tool for instant squad scaffolding with 4 preset architectures (default, fast, mentors, specialists) |
| **Purpose** | Reference for squad bootstrap patterns and multi-agent preset design |
| **Key areas** | Agent charters, routing rules, AGENTS.md pattern, preset architecture templates |
| **Documented in** | `docs/catalogs/reference-codebases.md` |

### 5.5 Other GitHub References

| Repository | Purpose |
|-----------|---------|
| `tamirdresher/squad-monitor` | Dashboard rendering engine |
| `tamirdresher/squad-personal-demo` | Full demo reference implementation |
| `tamirdresher/worktrees-example` | Git worktree pattern reference |
| `microsoft/work-iq-mcp` | WorkIQ MCP server source |
| `kacase/mcp-outlook` | Outlook MCP integration reference |

---

## 6. Scheduled Automation

The unified scheduler (`scripts/squad-scheduler.ps1`) dispatches tasks on configurable intervals. Configuration lives in `.squad/scheduler.json`.

### Active Scheduled Tasks

| Task | Script | Interval | Status | Tools Used |
|------|--------|----------|--------|------------|
| **teams-watchdog** | `.squad/skills/teams-watchdog/run-pipeline.ps1` | 24h | ✅ Enabled | WorkIQ, Copilot CLI |
| **daily-summary** | `scripts/squad-daily-summary.ps1` | 24h | ✅ Enabled | `gh` CLI, Teams webhook |
| **semantic-refresh** | `scripts/semantic-index.ps1` | 12h | ✅ Enabled | `git`, `gh` CLI |
| **email-watchdog** | `.squad/skills/email-watchdog/email-scan.ps1` | 24h | ✅ Enabled | WorkIQ, Copilot CLI, Teams webhook |
| **icm-scan** | `scripts/icm-scan.ps1` | 30m | ❌ Disabled | IcM MCP, Copilot CLI, `gh` CLI, Teams webhook |

### Scheduler Infrastructure

| Component | Location | Purpose |
|-----------|----------|---------|
| Scheduler script | `scripts/squad-scheduler.ps1` | Main dispatcher loop |
| Task config | `.squad/scheduler.json` | Task definitions, intervals, params |
| Run state | `.squad/scheduler-state.json` | Last-run timestamps (gitignored) |
| Execution log | `.squad/scheduler.log` | Structured log output |
| On-call config | `.squad/scheduler.json → oncall` | IcM on-call check (team 116041, disabled) |

### Scheduler Skill Documentation

Full design: `.squad/skills/unified-scheduler/SKILL.md`

---

## 7. Notification Channels

### 7.1 Microsoft Teams (Incoming Webhook)

| Property | Value |
|----------|-------|
| **Mechanism** | Incoming Webhook → Adaptive Card POST |
| **Webhook URL storage** | `~/.squad/teams-webhook.url` (per-machine, gitignored) |
| **Script** | `scripts/send-teams-notification.ps1` |
| **Rate limit** | 4 req/sec, 28 KB/message |
| **Used by** | Daily summary, IcM scan alerts, email watchdog |
| **Setup guide** | `docs/guides/teams-notifications-setup.md` |

### 7.2 GitHub Issues

| Property | Value |
|----------|-------|
| **Mechanism** | `gh issue create` via CLI |
| **Used by** | `icm-scan.ps1` (creates issues for new incidents) |
| **Labels** | `squad`, `squad:{agent}`, `icm`, `livesite` |

---

## 8. Authentication & Access

| Service | Auth Method | Token Command / Config |
|---------|------------|----------------------|
| **Azure / Kusto / Geneva** | Azure AD (corpnet) | `az account get-access-token --resource {url}` |
| **IcM Kusto** | Azure AD | `--resource https://icmcluster.kusto.windows.net/` |
| **IcM REST API** | Azure AD | `--resource https://microsofticm.onmicrosoft.com/IcmAPI` |
| **GitHub** | OAuth token | `gh auth login` / `gh auth token` |
| **ADO** | Azure AD / PAT | Configured in MCP server settings |
| **WorkIQ** | M365 Copilot license | EULA acceptance + admin consent |
| **Teams Webhook** | URL-as-secret | Stored in `~/.squad/teams-webhook.url` |
| **Copilot CLI** | GitHub auth | `copilot login` (one-time) |
| **EngHub** | Azure AD (corpnet) | Automatic with Azure session |

---

## Quick Reference: Tool by Agent

| Agent | Primary Tools |
|-------|--------------|
| **Aragorn** | IcM MCP (19 tools), Geneva MCP (5), EngHub MCP (4), Azure MCP (6), WorkIQ, grep/glob on TI repos |
| **Bilbo** | Git, file system, docs/ indexes |
| **Elrond** | GitHub MCP, reference codebases, web search |
| **Galadriel** | ADO MCP (5 tools), `az devops invoke`, git |
| **Gandalf** | GitHub issues, `.squad/decisions.md` |
| **Gimli** | PowerShell, Python, Node.js, npm, pip |
| **Scribe** | Git, file system (logs, decisions, history) |

---

*This is a living document. Update when new tools are adopted, integrations change, or scheduled tasks are modified.*
