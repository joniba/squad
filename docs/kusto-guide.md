# TI Production Kusto Cluster Guide

**Target Audience:** Operators, incident investigators, platform engineers

**Cluster:** `https://ti-prod-kusto-cluster.northeurope.kusto.windows.net`  
**Database:** `prod`  
**Last Updated:** 2026-03-25  
**Documented by:** Bilbo (Librarian)  
**Research Credit:** Aragorn (Operator)

---

## 1. Quick Start

### Connect and Get a Token

```powershell
# Get an access token for Kusto
# ⚠️ CRITICAL: The resource is "https://kusto.kusto.windows.net" — NOT the cluster URI
$token = az account get-access-token --resource "https://kusto.kusto.windows.net" --query accessToken -o tsv

# Verify you have a token
if ($token) { Write-Host "✓ Token acquired" } else { Write-Host "✗ Failed to get token" }
```

### Run Your First Query

```powershell
# Example: Count errors in the last hour from StixWebApiLogs
$query = @"
StixWebApiLogs
| where env_time > ago(1h) and resultType == "Failure"
| summarize ErrorCount=count()
"@

$body = @{
    db = "prod"
    csl = $query
} | ConvertTo-Json

$response = Invoke-RestMethod `
    -Uri "https://ti-prod-kusto-cluster.northeurope.kusto.windows.net/v1/rest/query" `
    -Method Post `
    -Headers @{ 
        "Authorization" = "Bearer $token"
        "Content-Type" = "application/json"
    } `
    -Body $body

# Display results
$response.Tables[0].Rows | Format-Table
```

**Expected output:** A single row with the error count for the last hour.

---

## 2. Cluster Overview

### Architecture

The TI production Kusto cluster is a centralized telemetry hub for the threat intelligence pipeline. It collects logs from 7+ services across 10+ Azure regions via the Sentinel platform and TAXII feed processors.

| Aspect | Details |
|--------|---------|
| **Cluster URI** | `https://ti-prod-kusto-cluster.northeurope.kusto.windows.net` |
| **Region** | North Europe (primary) |
| **Database** | `prod` (only database) |
| **Tables** | 4 (Log, SentinelLogEntry, StixWebApiLogs, TraceEvent) |
| **Data retention** | ~7 days (TraceEvent: ~1 day) |
| **Ingestion rate** | ~10–15M rows/day (varies by feed activity) |
| **Auth resource** | `https://kusto.kusto.windows.net` |
| **Query endpoint** | `POST {clusterUri}/v1/rest/query` |
| **Management endpoint** | `POST {clusterUri}/v1/rest/mgmt` |

### Data Sources

| Source | Tables | Rows/Day | Purpose |
|--------|--------|----------|---------|
| **Log Table** | Log | ~1.93B (7-day retention) | All TI pipeline services (NormalizationService, CosmosDbPublisher, etc.) |
| **Sentinel Platform** | SentinelLogEntry | ~1.17B (7-day retention) | Sentinel service operational logs (ConnectorService, GatewayService, IngestionApi, etc.) |
| **STIX Web API** | StixWebApiLogs | ~104M (7-day retention) | Structured API operation logs (create, read, batch, upsert) |
| **TAXII Feeds & Service Fabric** | TraceEvent | ~247M (1-day retention) | Low-level trace events from feed processors and deployment infrastructure |

---

## 3. Table Reference

### 3.1 Log Table — Application Pipeline Telemetry

**Purpose:** Primary table for investigating the TI indicator processing pipeline. Contains logs from all services in the NormalizationService → CosmosDbPublisher → EventHub chain.

**When to use:**
- Tracking end-to-end pipeline throughput
- Investigating service-level error rates
- Regional health analysis
- Debugging CosmosDB or EventHub publishing issues

**Key columns:**

| Column | Type | Purpose | Example |
|--------|------|---------|---------|
| `env_time` | datetime | Event timestamp | 2026-03-25T14:30:45.123Z |
| `applicationName` | string | Service name | "NormalizationService", "CosmosDbPublisher" |
| `applicationRegion` | string | Deployment region | "WESTEUROPE", "EASTASIA" |
| `severityText` | string | Log level | "Information", "Error" |
| `message` | string | Log message | "Processed 1000 indicators" |
| `exception` | string | Stack trace (for errors) | Full exception details |
| `correlationVector` | string | Request trace ID | For correlating related events |
| `ElapsedMilliseconds` | real | Operation duration | Request latency |
| `StatusCode` | long | HTTP status | 200, 500, etc. (when applicable) |
| `Uri` | string | Request path | `/api/indicators` |

**Service names and what they do:**

| Application | Role | Error Baseline (per hour) |
|-------------|------|--------------------------|
| NormalizationService | Normalizes raw indicator data | ~1.9M events (mixed info/error) |
| CosmosDbPublisher | Writes to Cosmos DB | ~228K events, elevated errors indicate Cosmos DB write issues |
| LogAEventHubPublisher | Publishes to Log Analytics Event Hub | High throughput, watch for publish failures |
| ThreatIntelligenceBulkActionsExecutor | Executes bulk operations | Spiky pattern, errors correlate with bulk operation volume |

**Typical query patterns:**

```kql
// Error count by service (last hour)
Log
| where env_time > ago(1h) and severityText == "Error"
| summarize ErrorCount=count() by applicationName
| order by ErrorCount desc
```

```kql
// Service throughput over time (5-minute windows)
Log
| where env_time > ago(1h)
| summarize EventCount=count() by applicationName, bin(env_time, 5m)
| order by env_time desc, applicationName
```

```kql
// Regional error rates
Log
| where env_time > ago(1h)
| summarize Total=count(), Errors=countif(severityText == "Error")
  by applicationRegion
| extend ErrorRate=round(100.0*Errors/Total, 2)
| order by ErrorRate desc
```

---

### 3.2 SentinelLogEntry Table — Sentinel Platform Operations

**Purpose:** Operational logs from the Sentinel platform services (connectors, API gateway, ingestion pipeline).

**When to use:**
- Investigating Sentinel service availability or degradation
- Tracking data connector health (ConnectorService)
- Analyzing API gateway performance (GatewayService)
- Investigating indicator ingestion bottlenecks

**Key columns:**

| Column | Type | Purpose |
|--------|------|---------|
| `env_time` | datetime | Event timestamp |
| `ApplicationName` | string | Service name (ConnectorService, GatewayService, IngestionApi, etc.) |
| `ApplicationRegion` | string | Deployment region |
| `Message` | string | Log message |
| `LogLevel` | long | 4 = Info, 2 = Error |
| `LogType` | string | "Informational" or "Error" |
| `MessageDetails` | string | Structured error details |
| `CorrelationVector` | string | Request trace ID |
| `env_cloud_role` | string | Service Fabric role (deployment identity) |

**Application services:**

| Application | Typical Throughput (per hour) |
|-------------|-------------------------------|
| ConnectorService | ~3.1M events | Data connectors (external feed ingestion) |
| GatewayService | ~2.9M events | API gateway layer |
| IngestionApi | ~748K events | Indicator ingestion API |
| StixApiService | ~133K events | STIX 2.1 API service |
| FileImportsService | ~64K events | File-based indicator import |

**Typical query patterns:**

```kql
// Error rate by Sentinel service (last hour)
SentinelLogEntry
| where env_time > ago(1h)
| summarize Total=count(), Errors=countif(LogLevel == 2)
  by ApplicationName
| extend ErrorRate=round(100.0*Errors/Total, 2)
| order by ErrorRate desc
```

```kql
// Connector health drill-down
SentinelLogEntry
| where env_time > ago(1h)
  and ApplicationName == "ConnectorService"
  and LogLevel == 2
| project env_time, Message, MessageDetails, ApplicationRegion
| take 20
```

---

### 3.3 StixWebApiLogs Table — STIX API Operations

**Purpose:** Structured logs of the STIX 2.1 API. Best table for investigating individual API request failures, latency issues, or component-level debugging.

**When to use:**
- Diagnosing API request failures or timeouts
- Measuring API latency percentiles
- Identifying which API components are failing (StixController, StixStore, etc.)
- Tracking API success rates by data center
- Cross-referencing API errors with ARM ARM watchlist incidents

**Key columns:**

| Column | Type | Purpose |
|--------|------|---------|
| `env_time` | datetime | Request timestamp |
| `resultType` | string | "Success" or "Failure" |
| `resultDescription` | string | Error description (when Failure) |
| `durationMs` | long | Request latency in milliseconds |
| `component` | string | API component (StixController.Create, StixStore, etc.) |
| `dataCenter` | string | Data center name |
| `callerIpAddress` | string | Client IP address |
| `requestId` | string | Request identifier for tracing |
| `state` | string | Operation state |
| `message` | string | Log message |

**Major API components:**

| Component | Purpose | Typical Volume (per 24h) |
|-----------|---------|------------------------|
| StixController.Create | Create new indicators | ~4.9M |
| StixStore | Cosmos DB storage layer | ~4.6M |
| IStixContainer.UpsertIndicatorAsync | Upsert (update or insert) | ~2.5M |
| IStixContainer.GetIndicatorsAsync | Read indicators | ~2.1M |
| StixController.GetContainer | List collections | ~1.6M |
| IStixContainer.ChangeIndicatorStatusAsync | Revoke/change status | ~695K |
| StixController.Batch | Batch operations | ~553K |

**Typical query patterns:**

```kql
// API error rate by hour (primary health indicator)
StixWebApiLogs
| where env_time > ago(24h)
| summarize Total=count(), Failures=countif(resultType == "Failure")
  by bin(env_time, 1h)
| extend ErrorRate=round(100.0*Failures/Total, 2)
| order by env_time desc
```

**Expected results:** Baseline error rate is 3–5%. Spikes above 10% warrant investigation.

```kql
// Errors by component (what's failing?)
StixWebApiLogs
| where env_time > ago(1h) and resultType == "Failure"
| summarize FailureCount=count() by component
| order by FailureCount desc
```

```kql
// Latency percentiles (performance baseline)
StixWebApiLogs
| where env_time > ago(1h) and resultType == "Success"
| summarize P50=percentile(durationMs, 50),
    P95=percentile(durationMs, 95),
    P99=percentile(durationMs, 99),
    P999=percentile(durationMs, 99.9)
```

```kql
// Error rate by data center (regional issues?)
StixWebApiLogs
| where env_time > ago(1h)
| summarize Total=count(), Failures=countif(resultType == "Failure")
  by dataCenter
| extend ErrorRate=round(100.0*Failures/Total, 2)
| order by ErrorRate desc
```

---

### 3.4 TraceEvent Table — TAXII Feeds and Service Fabric Traces

**Purpose:** Low-level trace events from TAXII feed processors (Mandiant, SOCRadar, IBM X-Force, etc.) and Service Fabric deployment infrastructure.

**When to use:**
- Monitoring external TAXII feed health
- Investigating feed processing failures
- Tracking feed-level telemetry ingestion volume
- Debugging Service Fabric deployment issues

**Key columns:**

| Column | Type | Purpose |
|--------|------|---------|
| `env_time` | datetime | Event timestamp |
| `traceLevel` | long | Trace verbosity (lower = more severe) |
| `tagId` | string | Actor/component tag (contains feed name) |
| `message` | string | Trace message |
| `env_cloud_role` | string | Service Fabric role (fabric__dakotaappsprod{region}) |
| `env_cloud_location` | string | Deployment location |

**TAXII feed names (extracted from `tagId`):**

| Feed | Volume (per 1h) | Purpose |
|------|-----------------|---------|
| Accenture_Mandiant_CLOSINT_Feed | ~4.5M | Mandiant threat intelligence |
| SOCRadar-TI-Charles | ~1.1M | SOCRadar threat feeds |
| MSFeedRealTime:MSFeedPartitionHandler | ~1.0M | Microsoft internal feeds |
| ATIP_All_Collections | ~829K | ATIP collection |
| Threatviewio | ~782K | Threatview.io feed |
| IBMXF-ew_url | ~570K | IBM X-Force URLs |

**Service Fabric deployment regions (from `env_cloud_role`):**

| Fabric Role | Region | Volume (per 1h) |
|-------------|--------|-----------------|
| fabric__dakotaappsprodweu | West Europe | ~9.7M |
| fabric__dakotaappsprodsea | Southeast Asia | ~7.2M |
| fabric__dakotaappsproduks | UK South | ~3.1M |

**Typical query patterns:**

```kql
// TAXII feed activity (which feeds are actively polling?)
TraceEvent
| where env_time > ago(1h) and tagId startswith "TAXIIActor:"
| extend FeedName = extract(@"\|(.+)$", 1, tagId)
| summarize EventCount=count() by FeedName
| order by EventCount desc
```

```kql
// TAXII feed errors (feed failures?)
TraceEvent
| where env_time > ago(1h)
  and tagId startswith "TAXIIActor:"
  and traceLevel <= 2  // Error or lower
| extend FeedName = extract(@"\|(.+)$", 1, tagId)
| summarize ErrorCount=count() by FeedName
| order by ErrorCount desc
```

```kql
// Regional deployment health
TraceEvent
| where env_time > ago(1h)
| summarize EventCount=count() by env_cloud_role, env_cloud_location
| order by EventCount desc
```

---

## 4. Sample Queries by Use Case

### Use Case 1: API Error Investigation (ARM Watchlist Incident)

When an ARM incident fires for the STIX API, follow this sequence:

**Step 1: Check API error rate in the impact window**

```kql
// Replace <START> and <END> with incident time window (e.g., 2026-03-24T21:00:00Z)
StixWebApiLogs
| where env_time between(datetime(<START>)..datetime(<END>))
| summarize Total=count(), Failures=countif(resultType == "Failure")
  by bin(env_time, 5m)
| extend ErrorRate=round(100.0*Failures/Total, 2)
| order by env_time desc
```

**Returns:** Error rate per 5-minute window. Compare to baseline (3–5%). Spikes >10% indicate an incident.

**Step 2: Identify affected components**

```kql
StixWebApiLogs
| where env_time between(datetime(<START>)..datetime(<END>))
| where resultType == "Failure"
| summarize FailureCount=count() by component
| order by FailureCount desc
```

**Returns:** Components ordered by failure count. High-volume failures in `StixStore` or `StixController.Create` indicate Cosmos DB or API controller issues.

**Step 3: Check if regional (data center specific)**

```kql
StixWebApiLogs
| where env_time between(datetime(<START>)..datetime(<END>))
| where resultType == "Failure"
| summarize FailureCount=count() by dataCenter
| order by FailureCount desc
```

**Returns:** Failures by data center. If concentrated in one region, indicates regional resource constraint or network issue.

---

### Use Case 2: Service Throughput and Health

**Monitor pipeline end-to-end throughput**

```kql
// Indicator processing throughput by service (5-minute windows)
Log
| where env_time > ago(24h)
| summarize EventCount=count() by applicationName, bin(env_time, 5m)
| order by env_time desc
```

**Returns:** Event count per service per 5-minute window. Helps identify processing bottlenecks.

**Track CosmosDB publish health**

```kql
// CosmosDB success rate (writer service)
Log
| where env_time > ago(24h) and applicationName == "CosmosDbPublisher"
| summarize Total=count(), Errors=countif(severityText == "Error")
  by bin(env_time, 1h)
| extend SuccessRate=round(100.0*(Total-Errors)/Total, 2)
| order by env_time desc
```

**Returns:** Success rate per hour. Sustained <98% may indicate Cosmos DB throttling or network issues.

---

### Use Case 3: Sentinel Service Health

**Track connector and ingestion API health**

```kql
// Sentinel service error rates
SentinelLogEntry
| where env_time > ago(1h)
| summarize Total=count(), Errors=countif(LogLevel == 2)
  by ApplicationName
| extend ErrorRate=round(100.0*Errors/Total, 2)
| order by ErrorRate desc
```

**Returns:** Error rate per Sentinel service. Compare to baseline:
- ConnectorService baseline: 3–5%
- IngestionApi baseline: 2–4%
- GatewayService baseline: 1–3%

---

### Use Case 4: External Feed Monitoring (TAXII)

**Monitor TAXII feed activity and errors**

```kql
// Feed volume and error rates
TraceEvent
| where env_time > ago(1h) and tagId startswith "TAXIIActor:"
| extend FeedName = extract(@"\|(.+)$", 1, tagId)
| summarize TotalEvents=count(),
    ErrorEvents=countif(traceLevel <= 2)
  by FeedName
| extend ErrorRate=round(100.0*ErrorEvents/TotalEvents, 2)
| order by TotalEvents desc
```

**Returns:** Feed volume and error rate. Sustained zero activity for a feed may indicate a connector failure.

**Alert on feed inactivity**

```kql
// Find feeds with no activity in last 30 minutes
TraceEvent
| where env_time > ago(2h) and tagId startswith "TAXIIActor:"
| extend FeedName = extract(@"\|(.+)$", 1, tagId)
| summarize MaxTime=max(env_time) by FeedName
| where MaxTime < ago(30m)
| project FeedName, LastSeen=MaxTime
```

**Returns:** Feeds that haven't polled in 30+ minutes. Silence on high-volume feeds (Mandiant, SOCRadar) is abnormal.

---

### Use Case 5: Performance Analysis and Baseline Extraction

**Measure API latency percentiles (baseline for alerting)**

```kql
// STIX API latency percentiles by hour
StixWebApiLogs
| where env_time > ago(7d) and resultType == "Success"
| summarize P50=percentile(durationMs, 50),
    P95=percentile(durationMs, 95),
    P99=percentile(durationMs, 99),
    Count=count()
  by bin(env_time, 1h)
| order by env_time desc
```

**Returns:** Latency percentiles per hour. Use P95 and P99 to set alerting thresholds.

**Identify slow components**

```kql
// Find components with high latency
StixWebApiLogs
| where env_time > ago(1h) and resultType == "Success"
| summarize P95=percentile(durationMs, 95),
    P99=percentile(durationMs, 99),
    AvgDuration=avg(durationMs),
    Count=count()
  by component
| order by P99 desc
```

**Returns:** Components ranked by P99 latency. Helps identify performance regressions.

---

### Use Case 6: Correlation and Root Cause Analysis

**Correlate API failures with pipeline errors**

```kql
// Compare StixWebApiLogs failures with Log table errors (same time window)
StixWebApiLogs
| where env_time > ago(1h) and resultType == "Failure"
| summarize StixErrors=count() by bin(env_time, 5m)
| join kind=inner (
    Log
    | where env_time > ago(1h) and severityText == "Error"
    | summarize LogErrors=count() by bin(env_time, 5m)
) on env_time
| project env_time, StixErrors, LogErrors
| order by env_time desc
```

**Returns:** Failures correlated across two tables. Spikes that match suggest shared root cause (e.g., CosmosDB throttling).

**Correlate TAXII feed errors with Sentinel ingestion errors**

```kql
// Feed errors vs. ingestion API errors (same window)
TraceEvent
| where env_time > ago(1h)
  and tagId startswith "TAXIIActor:"
  and traceLevel <= 2
| summarize TAXIIErrors=count() by bin(env_time, 5m)
| join kind=leftouter (
    SentinelLogEntry
    | where env_time > ago(1h)
      and ApplicationName == "IngestionApi"
      and LogLevel == 2
    | summarize IngestionErrors=count() by bin(env_time, 5m)
) on env_time
| project env_time, TAXIIErrors, IngestionErrors=coalesce(IngestionErrors, 0)
```

**Returns:** Feed errors and ingestion errors side-by-side. Matching spikes suggest feed delivery issues.

---

## 5. Known Issues & Workarounds

### Issue 1: Azure MCP Kusto Tool is Broken ❌

**Symptom:** Calling the `azure-mcp-kusto` tool returns `FileNotFoundException` for all operations.

**Root cause:** MCP tool-level configuration issue (not a cluster access issue).

**Workaround:** Use the Kusto REST API directly via PowerShell (see **Quick Start** section above).

---

### Issue 2: Token Resource Gotcha

**Symptom:** Using the cluster URI as the token resource returns `401 Unauthorized`.

**Root cause:** The auth resource for **all** Kusto clusters is `https://kusto.kusto.windows.net` — not the individual cluster URI.

**Correct token command:**

```powershell
$token = az account get-access-token --resource "https://kusto.kusto.windows.net" --query accessToken -o tsv
```

**Incorrect (will fail):**

```powershell
# ❌ DO NOT DO THIS
$token = az account get-access-token --resource "https://ti-prod-kusto-cluster.northeurope.kusto.windows.net" --query accessToken -o tsv
```

---

### Issue 3: ARM Watchlist Queries Not Runnable from This Cluster

**Symptom:** Queries using `macro-expand ARMProdEG` return errors.

**Root cause:** ARM-side queries must run on the ARM cluster, not the TI cluster. They require SAW access or DGrep.

**Workaround:** Use RP-side (Sentinel) queries on `securityinsights.kusto.windows.net` or contact the incident team for ARM data.

---

### Issue 4: TraceEvent Table Has ~1 Day Retention

**Symptom:** Queries for TraceEvent data older than 1 day return zero rows.

**Root cause:** TraceEvent is low-level telemetry and has limited retention (~1 day vs. ~7 days for other tables).

**Workaround:** For long-term trend analysis, use Log or SentinelLogEntry tables instead.

---

## 6. Tips & Best Practices

### Time Range Best Practices

**Use relative time ranges for investigations:**

```kql
// Standard ranges
| where env_time > ago(1h)   // Last 1 hour
| where env_time > ago(6h)   // Last 6 hours
| where env_time > ago(24h)  // Last 24 hours
| where env_time > ago(7d)   // Last 7 days (full retention)
```

**Use absolute ranges for incident post-mortems:**

```kql
// Incident window: 2026-03-24 21:00 UTC to 22:00 UTC
| where env_time between(datetime(2026-03-24T21:00:00Z)..datetime(2026-03-24T22:00:00Z))
```

---

### Performance Tips

**Aggregate early, fetch fewer rows:**

```kql
// ✓ Good: Aggregate before project
Log
| where env_time > ago(1h) and severityText == "Error"
| summarize count() by applicationName  // Aggregated
| order by count_ desc

// ✗ Avoid: Project all rows then aggregate
Log
| where env_time > ago(1h) and severityText == "Error"
| project applicationName
| summarize count() by applicationName  // Still works, less efficient
```

**Filter early and often:**

```kql
// ✓ Good: Filter before operations
StixWebApiLogs
| where env_time > ago(1h)
| where resultType == "Failure"      // Filter early
| summarize count() by component

// ✗ Avoid: Large result sets before filtering
StixWebApiLogs
| where env_time > ago(1h)
| summarize count() by component
| where component has "Error"        // Filtering a summary (ineffective)
```

**Use `take` to limit results during exploration:**

```kql
// Preview before running full query
Log
| where env_time > ago(1h) and applicationName == "NormalizationService"
| take 10  // Fetch just 10 rows for inspection
```

---

### Common Pitfalls

**Pitfall 1: Forgetting the auth resource**

```powershell
# ❌ WRONG
$token = az account get-access-token --query accessToken -o tsv

# ✓ CORRECT
$token = az account get-access-token --resource "https://kusto.kusto.windows.net" --query accessToken -o tsv
```

**Pitfall 2: Using text operators instead of structured fields**

```kql
// ❌ Inefficient: Text search
Log
| where message contains "NormalizationService"

// ✓ Better: Use the applicationName field
Log
| where applicationName == "NormalizationService"
```

**Pitfall 3: Querying beyond retention window**

```kql
// ❌ No results: Data older than 7 days
Log
| where env_time > ago(10d)

// ✓ Within retention
Log
| where env_time > ago(3d)
```

**Pitfall 4: Case sensitivity in filters**

```kql
// ❌ May miss rows
Log
| where severityText == "error"  // Actual value is "Error"

// ✓ Use exact casing
Log
| where severityText == "Error"
```

---

## 7. Operational Baselines & Alert Thresholds

Use these baselines to calibrate monitoring and alerting:

| Metric | Baseline | Alert Threshold | Query |
|--------|----------|-----------------|-------|
| **STIX API error rate** | 3–5% | >10% for sustained >5 min | `StixWebApiLogs \| ... \| where ErrorRate > 10` |
| **NormalizationService errors/hr** | ~1.9M | >3M for 2+ hours | `Log \| where applicationName == "NormalizationService" and severityText == "Error"` |
| **CosmosDB publish success rate** | >98% | <95% | `Log \| where applicationName == "CosmosDbPublisher" \| ... SuccessRate < 95` |
| **STIX API P99 latency** | ~50–100ms | >200ms for >10 min | `StixWebApiLogs \| ... \| where P99 > 200` |
| **TAXII feed inactivity** | Continuous | No events for >30 min (high-volume feeds only) | `TraceEvent \| where tagId startswith "TAXIIActor:"` |
| **Sentinel connector error rate** | 3–5% | >10% | `SentinelLogEntry \| where ApplicationName == "ConnectorService"` |

---

## 8. Next Steps & Related Resources

**Related clusters and tools:**

- **RP-side queries:** `securityinsights.kusto.windows.net` / `SecurityInsightsProd` (for Watchlist RP data)
- **IcM data warehouse:** `icmcluster.kusto.windows.net` / `IcMDataWarehouse` (for incident history)
- **Watchlist incidents:** ICM 767815474, 767416366 (ARM watchlist errors — cross-reference with this cluster)

**Contact:**

- **Questions about this guide?** Reach out to Bilbo (Librarian)
- **Cluster access issues?** Contact the TI platform team
- **New data sources?** Suggest additions via squad decisions

---

**Document version:** 1.0  
**Last verified:** 2026-03-25  
**Authored by:** Bilbo, pa-squad Librarian  
**Research by:** Aragorn, pa-squad Operator
