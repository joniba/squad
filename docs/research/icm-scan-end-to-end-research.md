---
title: "IcM Scan End-to-End Research: Bypassing MCP via Direct Kusto REST"
agent: elrond
date: 2025-07-16
priority: P0-CRITICAL
status: complete
sources:
  - MDC-AI-Shared/extensions/personal-ai/src/icm/client.ts
  - MDC-AI-Shared/extensions/personal-ai/src/icm/poller.ts
  - MDC-AI-Shared/extensions/personal-ai/src/icm/config.ts
  - MDC-AI-Shared/extensions/personal-ai/src/notifications/poller.ts
  - MDC-AI-Shared/extensions/personal-ai/src/notifications/service.ts
  - MDC-AI-Shared/extensions/personal-ai/.ai/plans/event-triggers/README.md
  - MDC-AI-Shared/extensions/personal-ai/.ai/plans/event-triggers/phase3-icm-poller.md
---

# IcM Scan End-to-End Research

## Executive Summary

The personal-ai VS Code extension's event triggers system **completely bypasses MCP tools** for IcM data access. Instead, it uses:

1. **`az account get-access-token`** — Azure CLI for authentication (no MCP consent dialogs)
2. **Direct HTTP POST to Kusto REST API** (`{cluster}/v1/rest/query`)
3. **KQL queries against `IncidentsSnapshotV2()`** — the IcM data warehouse

This means our standalone `icm-scan.ps1` can do the same thing in PowerShell — no Copilot session, no MCP tools, no permission dialogs, no human intervention.

---

## 1. The Key Finding: How Event Triggers Solve the MCP Permission Problem

**They don't use MCP at all.**

The entire IcM polling system in personal-ai uses two direct API patterns:

### Pattern A: Kusto REST API (primary — for listing/querying incidents)

```
Auth:   az account get-access-token --resource https://icmcluster.kusto.windows.net/ --query accessToken -o tsv
URL:    POST https://icmcluster.kusto.windows.net/v1/rest/query
Body:   { "db": "IcMDataWarehouse", "csl": "<KQL query>" }
Header: Authorization: Bearer <token>
```

**Source evidence** (`src/icm/config.ts:827-858`, `src/icm/config.ts:863-905`):
- Token fetched via `spawn('az', ['account', 'get-access-token', '--resource', resource, '--query', 'accessToken', '-o', 'tsv'], { shell: true })`
- Resource URL = Kusto cluster URL with trailing slash: `https://icmcluster.kusto.windows.net/`
- Token cached for 45 minutes
- Query sent as `{ db: database, csl: query }` to `{clusterUrl}/v1/rest/query`
- Response parsed: `data.Tables[0].Columns` (names) + `data.Tables[0].Rows` (values)

### Pattern B: IcM REST API (fallback — for restricted/redacted incidents)

```
Auth:   az account get-access-token --resource https://microsofticm.onmicrosoft.com/IcmAPI --query accessToken -o tsv
URL:    GET https://portal.microsofticm.com/api/incidents/{icmId}
Header: Authorization: Bearer <token>
```

**Source evidence** (`src/icm/client.ts:519-592`):
- Separate token with resource `https://microsofticm.onmicrosoft.com/IcmAPI`
- Used only when Kusto returns redacted fields (restricted incidents)
- Also fetches discussions: `GET {baseUrl}/incidents/{icmId}/discussion`

---

## 2. Architecture of the Event Triggers System

```
CronScheduler (every 5min)
    └── safePoll() — circuit breaker wrapper
        └── IcmPoller.poll()
            ├── IcmClient.executeKustoQuery() — Kusto REST
            ├── WatermarkStore.getWatermark() — last check time + seen IDs
            ├── Filter: only new incidents since last check
            └── NotificationService.emit('icm.new', incident)
                └── TriggerEngine.evaluate(conditions)
                    └── Execute actions (toast/command/skill)
```

### Circuit Breaker (`src/notifications/poller.ts`)
- States: CLOSED → OPEN (after 3 consecutive failures) → HALF-OPEN (after 15min cooldown)
- In HALF-OPEN: allows one probe poll; success → CLOSED, failure → OPEN again
- Prevents hammering a broken API

### Watermark Store (`src/notifications/watermarkStore.ts`)
- Persists `{ lastCheckTime: ISO8601, seenIds: number[] }` per queue
- Ensures no duplicate notifications across polls

### Trigger Engine (`src/notifications/service.ts`)
- 10+ condition operators: `equals`, `contains`, `matches` (regex), `gt`, `lt`, `gte`, `lte`, `in`, `exists`, `assignedToMe`
- All conditions in a trigger are AND-combined
- Actions: skill invocation, command execution, VS Code toast notification

---

## 3. Configuration Defaults

From `src/icm/config.ts:91-96`:

| Parameter | Default Value |
|-----------|---------------|
| `kustoClusterUrl` | `https://icmcluster.kusto.windows.net` |
| `kustoDatabase` | `IcMDataWarehouse` |
| `queues` | `[]` (configured per user) |
| `userAlias` | Auto-detected via `az account show` |

---

## 4. KQL Query Patterns

### List Active Incidents (from `src/icm/client.ts:297-384`)

```kql
IncidentsSnapshotV2()
| where CreateDate > ago(30d)
| where not(IsRestricted)
| where Status in ("ACTIVE", "MITIGATED")
| where Severity in (0, 1, 2, 25)
| where OwningTeamName in ("DAKOTA\\ThreatIntelligence")
| project IcmId = IncidentId, Title, Severity, Status, CreateDate, ResolveDate,
          OwningTeamName, OwningTenantName, OwningContactAlias, Keywords, MonitorId
| order by CreateDate desc
| take 100
```

Key KQL notes:
- `IncidentsSnapshotV2()` is the main IcM function — contains latest state of all incidents
- Severity 25 = "Sev2.5" (recommended Sev2 bump)
- Filter by `OwningTeamName` for team queues, by `OwningContactAlias` for personal
- `IsRestricted` flag hides confidential incidents from Kusto (need REST API fallback)

### Detect User Queues (from `src/icm/config.ts:245-275`)

```kql
IncidentsSnapshotV2()
| where CreateDate > ago(90d)
| where OwningContactAlias == "jbenami"
| summarize count() by OwningTeamName
| order by count_ desc
| take 10
```

---

## 5. Designed Solution for pa-squad

### Overview

Replace the current `icm-scan.ps1` (config-display-only stub that delegates to Aragorn agent) with a **real standalone PowerShell scanner** that queries IcM Kusto directly.

### Composable Script Design

Following squad decisions (scripts short/composable, file-based I/O):

```
scripts/
  icm-scan.ps1          — Orchestrator: auth → query → filter → notify
  icm-kusto-query.ps1   — (helper) Execute a KQL query against Kusto REST API
```

The orchestrator does everything in one script (still short at ~120 lines) because the helper is a reusable Kusto query function. This follows the "composable" principle — the Kusto query helper can be reused for future Kusto-based scripts.

### icm-scan.ps1 — End-to-End Flow

```powershell
# 1. GET TOKEN
$token = az account get-access-token --resource "https://icmcluster.kusto.windows.net/" --query accessToken -o tsv
if (-not $token) { Write-Error "az login required"; exit 1 }

# 2. BUILD KQL (parameterized by team, severity, lookback window)
$kql = @"
IncidentsSnapshotV2()
| where CreateDate > ago({$SinceHours}h)
| where not(IsRestricted)
| where Status in ("ACTIVE", "MITIGATED")
| where Severity in (0, 1, 2, 25)
| where OwningTeamName has "{$TeamName}"
| project IcmId=IncidentId, Title, Severity, Status, CreateDate,
          OwningTeamName, OwningContactAlias, IncidentType, Keywords
| order by Severity asc, CreateDate desc
"@

# 3. QUERY KUSTO REST
$body = @{ db = "IcMDataWarehouse"; csl = $kql } | ConvertTo-Json
$resp = Invoke-RestMethod -Uri "https://icmcluster.kusto.windows.net/v1/rest/query" `
  -Method Post -Headers @{ Authorization = "Bearer $token" } `
  -ContentType "application/json" -Body $body

# 4. PARSE RESPONSE (Kusto table format → objects)
$columns = $resp.Tables[0].Columns | ForEach-Object { $_.ColumnName }
$incidents = $resp.Tables[0].Rows | ForEach-Object {
    $row = $_; $obj = @{}
    for ($i = 0; $i -lt $columns.Count; $i++) { $obj[$columns[$i]] = $row[$i] }
    [PSCustomObject]$obj
}

# 5. FILTER (Sev0-2 always, Sev25, CRIs regardless of sev)
$filtered = $incidents | Where-Object {
    $_.Severity -in @(0, 1, 2, 25) -or
    $_.IncidentType -in @("System Reported Incident", "Customer Reported Incident")
}

# 6. WATERMARK DEDUP (skip already-seen IcM IDs)
$watermarkPath = Join-Path $root ".squad/icm-scan-watermark.json"
$watermark = if (Test-Path $watermarkPath) { Get-Content $watermarkPath | ConvertFrom-Json } else { @{ seenIds = @() } }
$newIncidents = $filtered | Where-Object { $_.IcmId -notin $watermark.seenIds }

# 7. NOTIFY via Teams webhook (reuse existing send-teams-notification.ps1)
if ($newIncidents.Count -gt 0) {
    $body = ($newIncidents | ForEach-Object {
        "• **Sev$($_.Severity)** [IcM#$($_.IcmId)](https://portal.microsofticm.com/imp/v5/incidents/details/$($_.IcmId)/home) — $($_.Title)"
    }) -join "`n"
    & "$root/scripts/send-teams-notification.ps1" -Title "🚨 IcM Scan: $($newIncidents.Count) new incident(s)" -Body $body
}

# 8. UPDATE WATERMARK
$watermark.seenIds = @($filtered | ForEach-Object { $_.IcmId })
$watermark.lastCheck = (Get-Date -Format "o")
$watermark | ConvertTo-Json | Set-Content $watermarkPath
```

### Scheduler Integration

Update `.squad/scheduler.json` to change `icm-scan` from agent type to script type:

```json
{
  "name": "icm-scan",
  "script": "scripts/icm-scan.ps1",
  "interval": "4h",
  "enabled": true,
  "condition": "oncall"
}
```

This removes the dependency on spawning Aragorn (agent) and eliminates the MCP permission problem entirely.

### Configuration

All configuration should be in a JSON config file at `.squad/icm-scan-config.json`:

```json
{
  "kustoClusterUrl": "https://icmcluster.kusto.windows.net",
  "kustoDatabase": "IcMDataWarehouse",
  "teamName": "DAKOTA\\ThreatIntelligence",
  "teamId": 116041,
  "userAlias": "jbenami",
  "severities": [0, 1, 2, 25],
  "sinceHours": 4,
  "includeCRIs": true,
  "maxResults": 100
}
```

---

## 6. Prerequisites

1. **Azure CLI** (`az`) must be installed and logged in (`az login`)
2. **Kusto access** — user must have read permissions on `IcMDataWarehouse`
3. **Teams webhook** — already configured at `~/.squad/teams-webhook.url` (existing)
4. **No Copilot/MCP dependency** — runs in any PowerShell terminal

---

## 7. Error Handling (From Event Triggers Patterns)

Patterns to adopt from the personal-ai implementation:

| Pattern | Source | PowerShell Equivalent |
|---------|--------|-----------------------|
| Circuit breaker | `poller.ts:20-70` | Track consecutive failures in watermark file; skip after 3 failures for 15min |
| Token caching | `client.ts:635-665` | Cache token in `$env:TEMP/icm-kusto-token.txt` with timestamp; reuse if < 45min old |
| Watermark dedup | `watermarkStore.ts` | JSON file with `lastCheck` + `seenIds[]` |
| Graceful fallback | `client.ts:557-592` | If Kusto fails, try IcM REST API with different token |
| Timeout | `config.ts:853-856` | PowerShell `-TimeoutSec 30` on `Invoke-RestMethod` |

---

## 8. Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| `az` CLI token expired | Query fails silently | Check `$LASTEXITCODE` after `az` call; log clear error |
| Kusto cluster throttling | 429 rate limit | Circuit breaker + exponential backoff (not needed at 4h interval) |
| Restricted incidents hidden | Missing CRIs | Optional IcM REST API fallback (Pattern B above) |
| Watermark file corruption | Duplicate or missed notifications | JSON schema validation; reset on parse failure |
| Teams webhook URL missing | Silent notification failure | Pre-check file exists; log warning |

---

## 9. Unresolved Questions

1. **Kusto access verification** — Can Jonathan's `az` identity query `IcMDataWarehouse`? Should be yes (same identity used in personal-ai), but needs verification with a test query.
2. **IcM REST API necessity** — Do we need the restricted-incident fallback (Pattern B), or is Kusto sufficient for the DAKOTA team's incidents?
3. **CRI detection** — The KQL column for incident type may be `IncidentType` or `SourceType` — needs verification against actual Kusto schema.

---

## 10. References

| File | Lines | What It Shows |
|------|-------|---------------|
| `src/icm/client.ts` | 632-762 | Core Kusto query execution + token management |
| `src/icm/client.ts` | 297-384 | KQL query builder for incident listing |
| `src/icm/client.ts` | 517-592 | IcM REST API fallback for restricted incidents |
| `src/icm/config.ts` | 86-117 | Default config (cluster URL, database name) |
| `src/icm/config.ts` | 827-905 | Kusto token + query execution (duplicated from client) |
| `src/icm/poller.ts` | 1-167 | IcmPoller: poll logic, watermark, event emission |
| `src/notifications/poller.ts` | 1-109 | Circuit breaker state machine |
| `src/notifications/service.ts` | 1-334 | Trigger evaluation with condition operators |
| `scheduler.json` | 12-19 | Current icm-scan task config (agent type, disabled) |
| `scripts/icm-scan.ps1` | 1-52 | Current stub (config display only) |
