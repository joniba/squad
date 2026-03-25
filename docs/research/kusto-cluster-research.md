# TI Production Kusto Cluster — Research

> **Cluster:** `https://ti-prod-kusto-cluster.northeurope.kusto.windows.net`
> **Researched by:** Aragorn (Operator)
> **Date:** 2026-03-25
> **Purpose:** Raw research data for Bilbo to produce a polished Kusto guide

---

## 1. Cluster Overview

### Connection Details

| Property | Value |
|----------|-------|
| **Cluster URI** | `https://ti-prod-kusto-cluster.northeurope.kusto.windows.net` |
| **Database** | `prod` (only database discovered) |
| **Auth token resource** | `https://kusto.kusto.windows.net` (standard Kusto resource — NOT the cluster URI) |
| **Auth method** | `az account get-access-token --resource "https://kusto.kusto.windows.net"` |
| **Query endpoint** | `POST {clusterUri}/v1/rest/query` (KQL queries) |
| **Management endpoint** | `POST {clusterUri}/v1/rest/mgmt` (`.show` commands) |
| **Current subscription** | SentinelUS TI Matching Telemetry (`7f0d9100-9be3-4981-b127-611338694353`) |

### Access Notes

| Access Path | Status | Notes |
|-------------|--------|-------|
| Azure MCP Kusto tool | ❌ **BROKEN** | Returns `FileNotFoundException` for all operations. Appears to be an MCP configuration/auth issue, not a cluster permission issue. |
| Kusto REST API (direct) | ✅ **Works** | Using `az account get-access-token --resource https://kusto.kusto.windows.net` + `Invoke-RestMethod` |
| `.show databases` | ✅ Works | Via mgmt endpoint |
| `.show tables` | ✅ Works | Via mgmt endpoint |
| `.show database schema` | ✅ Works | Via mgmt endpoint |
| KQL queries | ✅ Works | Via query endpoint |

**CRITICAL: The token resource for this cluster is `https://kusto.kusto.windows.net`, NOT the cluster URI.** Using the cluster URI as the resource returns 401 Unauthorized.

---

## 2. Table Inventory

### Database: `prod`

The cluster has a single database (`prod`) containing 4 tables. No stored functions were discovered.

| Table | Row Count | Retention Window | Purpose |
|-------|-----------|-----------------|---------|
| **Log** | ~1.93B | ~7 days (Mar 18–25) | Primary application telemetry — covers all TI pipeline services |
| **SentinelLogEntry** | ~1.17B | ~7 days (Mar 18–25) | Sentinel service-level operational logs |
| **StixWebApiLogs** | ~104M | ~7 days (Mar 18–25) | STIX API request/response logs (indicator CRUD operations) |
| **TraceEvent** | ~247M | ~1 day (Mar 24–25) | Low-level trace events (TAXII actors, Service Fabric) |

---

## 3. Table Schemas

### 3.1 Log Table (Primary Application Telemetry)

The most general-purpose table. Contains logs from all TI pipeline applications.

**Key columns for investigation:**

| Column | Type | Purpose |
|--------|------|---------|
| `env_time` | datetime | Event timestamp (primary time filter) |
| `TIMESTAMP` / `PreciseTimeStamp` | datetime | Additional timestamps |
| `applicationName` | string | Service/app name (NormalizationService, CosmosDbPublisher, etc.) |
| `applicationRegion` | string | Azure region (EASTASIA, WESTEUROPE, etc.) |
| `severityText` | string | Log level: "Information" or "Error" |
| `severityNumber` | long | Numeric severity |
| `message` | string | Log message text |
| `exception` | string | Exception details (for errors) |
| `correlationVector` | string | CV for request tracing |
| `env_dt_traceId` / `env_dt_spanId` | string | Distributed tracing IDs |
| `StatusCode` | long | HTTP status code (when applicable) |
| `HttpMethod` | string | HTTP method |
| `Uri` | string | Request URI |
| `ElapsedMilliseconds` | real | Request duration |
| `Application` | string | Application identifier |
| `Service` | string | Service identifier |
| `Function` | string | Function identifier |
| `ScaleUnit` | string | Scale unit |

**Application names observed (last 1h):**

| Application | Count | Description |
|-------------|-------|-------------|
| NormalizationService | ~5.9M | TI indicator normalization pipeline |
| CosmosDbPublisher | ~620K | Publishes processed indicators to Cosmos DB |
| LogAEventHubPublisher | ~402K | Publishes to Log Analytics via Event Hub |
| ThreatIntelligenceBulkActionsExecutor | ~316K | Executes bulk operations on indicators |
| ThreatIntelligenceBulkActions | ~23K | Bulk action coordination |
| HuntingLogARepublisher | 1 | Re-publishes hunting data to LA |
| DetectionsLogARepublisher | 1 | Re-publishes detection data to LA |

**Severity distribution (last 1h):** Information: ~5.2M, Error: ~2.2M

**Regions observed:**
EASTASIA (3.2M), SOUTHEASTASIA (1.2M), WESTEUROPE (586K), EASTUS2 (298K), NORTHEUROPE (205K), UKWEST (202K), EASTUS (182K), CANADAEAST (163K), AUSTRALIASOUTHEAST (159K), WESTUS (148K)

### 3.2 SentinelLogEntry Table (Sentinel Service Logs)

Sentinel platform operational events. Uses Geneva/OTEL envelope schema.

**Key columns for investigation:**

| Column | Type | Purpose |
|--------|------|---------|
| `env_time` | datetime | Event timestamp |
| `ApplicationName` | string | Sentinel service name |
| `ApplicationRegion` | string | Azure region |
| `Message` | string | Log message |
| `LogType` | string | "Informational" or "Error" |
| `LogLevel` | long | 4 = Info, 2 = Error |
| `CorrelationVector` | string | CV for tracing |
| `MessageDetails` | string | Additional structured details |
| `CustomColumns` | string | Custom structured data (JSON) |
| `SourceLocation` | string | Source code location |
| `MemberName` | string | Source method name |
| `env_cloud_role` | string | Service Fabric role |
| `env_cloud_location` | string | Deployment location |

**Application names observed (last 1h):**

| Application | Count | Description |
|-------------|-------|-------------|
| ConnectorService | ~3.1M | Data connectors (ingestion from external sources) |
| GatewayService | ~2.9M | API gateway layer |
| IngestionApi | ~748K | Indicator ingestion API |
| StixApiService | ~133K | STIX 2.1 API service |
| FileImportsService | ~64K | File-based indicator import |
| ThreatIntelligenceManager | ~19K | TI management service |
| FileImportsApi | ~15K | File import API layer |

**Log level distribution:** Level 4 (Info): ~4.9M, Level 2 (Error): ~1.9M

### 3.3 StixWebApiLogs Table (STIX API Operations)

Structured request/response logs for the STIX Web API. Best table for investigating API-level issues.

**Key columns for investigation:**

| Column | Type | Purpose |
|--------|------|---------|
| `env_time` | datetime | Request timestamp |
| `operationName` | string | API operation (often empty — use `component` instead) |
| `operationType` | string | Operation type |
| `resultType` | string | "Success" or "Failure" |
| `resultSignature` | string | Error signature (often empty) |
| `resultDescription` | string | Error description |
| `durationMs` | long | Request duration in milliseconds |
| `callerIpAddress` | string | Client IP |
| `tenantId` | string | Azure tenant ID |
| `workspaceId` | string | Log Analytics workspace ID |
| `component` | string | API component (StixController.Create, StixStore, etc.) |
| `state` | string | Operation state |
| `message` | string | Log message |
| `dataCenter` | string | Data center name |
| `requestId` | string | Request identifier |
| `resourceId` | string | Azure resource ID |
| `resourceType` | string | Resource type |
| `internalRequestId` | string | Internal request ID |

**Components observed (last 24h — ordered by frequency):**

| Component | Count | Purpose |
|-----------|-------|---------|
| StixController.Create | ~4.9M | Indicator creation |
| StixStore | ~4.6M | Cosmos DB storage operations |
| IStixContainer.UpsertIndicatorAsync | ~2.5M | Indicator upsert |
| IStixContainer.GetIndicatorsAsync | ~2.1M | Indicator retrieval |
| StixController.GetContainer | ~1.6M | Container/collection lookup |
| StixRequestContextParser | ~1.6M | Request parsing |
| StixContainer | ~1.3M | Container operations |
| StixBackend.SendDataToEventHub | ~1.1M | Event Hub publishing |
| IStixContainer.ChangeIndicatorStatusAsync | ~695K | Status changes (revoke, etc.) |
| StixController.Batch | ~553K | Batch operations |

**Data centers observed:**
West Europe (7.9M), Australia East (5.2M), West US 2 (5.1M), UK South (2.2M), Japan East (1.1M), Canada Central (140K), Korea Central (131K), Brazil South (131K), Italy North (66K)

**Result type distribution (last 24h):** Success: ~21.2M, Failure: ~688K (~3.1% error rate)

### 3.4 TraceEvent Table (Low-Level Traces)

Low-level Service Fabric and TAXII actor trace events. Shortest retention (~1 day).

**Key columns for investigation:**

| Column | Type | Purpose |
|--------|------|---------|
| `env_time` | datetime | Event timestamp |
| `traceLevel` | long | Trace verbosity level |
| `tagId` | string | Actor/component tag (e.g., "TAXIIActor:...") |
| `message` | string | Trace message |
| `env_cloud_role` | string | Service Fabric role (e.g., "fabric__dakotaappsprodweu") |
| `env_cloud_location` | string | Deployment location |
| `env_cloud_deploymentUnit` | string | Deployment unit |

**TAXII Actor tags observed (last 1h):**

| Tag ID | Count | External Feed |
|--------|-------|---------------|
| TAXIIActor:...\|Accenture_Mandiant_CLOSINT_Feed | ~4.5M | Mandiant threat intel |
| TAXIIActor:...\|SOCRadar-TI-Charles | ~1.1M | SOCRadar feed |
| MSFeedRealTime:MSFeedPartitionHandler | ~1.0M | Microsoft internal feed |
| TAXIIActor:...\|SOCRadar | ~919K | SOCRadar feed |
| TAXIIActor:...\|ATIP_All_Collections | ~829K | ATIP feed |
| TAXIIActor:...\|Threatviewio | ~782K | Threatview.io feed |
| TAXIIActor:...\|SOCRadarCollection_0001 | ~750K | SOCRadar collection |
| TAXIIActor:...\|SOCRadar-TI-Firehol | ~730K | SOCRadar/FireHOL feed |
| TAXIIActor:...\|IBMXF-ew_url | ~570K | IBM X-Force feed |

**Service Fabric roles (deployment regions):**

| Role | Count | Region |
|------|-------|--------|
| fabric__dakotaappsprodweu | ~9.7M | West Europe |
| fabric__dakotaappsprodsea | ~7.2M | Southeast Asia |
| fabric__dakotaappsproduks | ~3.1M | UK South |
| fabric__dakotaappsprodqac | ~1.3M | Qatar Central |
| fabric__dakotaappsproditn | ~886K | Italy North |
| fabric__dakotaappsprodjae | ~642K | Japan East |
| fabric__dakotaappsprodause | ~635K | Australia Southeast |
| fabric__dakotaappsprodcin | ~221K | Central India |
| fabric__dakotaappsprodfrc | ~220K | France Central |
| fabric__dakotaappsprodneu | ~147K | North Europe |

---

## 4. Query Patterns by Use Case

### 4.1 Error Rate Analysis (STIX API)

```kql
// Error rate by hour — primary health indicator
StixWebApiLogs
| where env_time > ago(24h)
| summarize Total=count(), Failures=countif(resultType == "Failure")
  by bin(env_time, 1h)
| extend ErrorRate=round(100.0*Failures/Total, 2)
| order by env_time desc
```

**Sample results (2026-03-25):**
- Baseline error rate: 3–5%
- Spike at 19:00 UTC: 6.94%
- Low point at 02:00 UTC: 0.81%

```kql
// Error rate by data center — identify regional issues
StixWebApiLogs
| where env_time > ago(1h) and resultType == "Failure"
| summarize Failures=count() by dataCenter
| order by Failures desc
```

```kql
// Failures by component — identify which API path is failing
StixWebApiLogs
| where env_time > ago(1h) and resultType == "Failure"
| summarize count() by component
| order by count_ desc
```

### 4.2 Application Error Analysis (Log Table)

```kql
// Error counts by application — which service is unhealthy?
Log
| where env_time > ago(1h) and severityText == "Error"
| summarize count() by applicationName
| order by count_ desc
```

**Observed pattern:** NormalizationService consistently produces the most errors (~1.9M/hr). CosmosDbPublisher errors (~228K/hr) may indicate Cosmos DB write issues.

```kql
// Error messages with stack traces — drill into specific app
Log
| where env_time > ago(1h)
  and severityText == "Error"
  and applicationName == "NormalizationService"
| project env_time, message, exception, applicationRegion
| take 10
```

```kql
// Error rate by region — identify region-specific issues
Log
| where env_time > ago(1h)
| summarize Total=count(), Errors=countif(severityText == "Error")
  by applicationRegion
| extend ErrorRate=round(100.0*Errors/Total, 2)
| order by ErrorRate desc
```

### 4.3 Sentinel Service Health (SentinelLogEntry)

```kql
// Service error rates — which Sentinel service has issues?
SentinelLogEntry
| where env_time > ago(1h)
| summarize Total=count(), Errors=countif(LogLevel == 2)
  by ApplicationName
| extend ErrorRate=round(100.0*Errors/Total, 2)
| order by ErrorRate desc
```

```kql
// Connector health — specific connector issues
SentinelLogEntry
| where env_time > ago(1h)
  and ApplicationName == "ConnectorService"
  and LogLevel == 2
| project env_time, Message, MessageDetails, ApplicationRegion
| take 20
```

```kql
// Ingestion API health
SentinelLogEntry
| where env_time > ago(1h)
  and ApplicationName == "IngestionApi"
| summarize Total=count(), Errors=countif(LogLevel == 2)
  by bin(env_time, 5m)
| extend ErrorRate=round(100.0*Errors/Total, 2)
| order by env_time desc
```

### 4.4 TAXII Feed Monitoring (TraceEvent)

```kql
// TAXII actor activity — which feeds are actively polling?
TraceEvent
| where env_time > ago(1h)
| where tagId startswith "TAXIIActor:"
| extend FeedName = extract(@"\|(.+)$", 1, tagId)
| summarize count() by FeedName
| order by count_ desc
```

```kql
// TAXII actor errors — feed ingestion failures
TraceEvent
| where env_time > ago(1h)
  and tagId startswith "TAXIIActor:"
  and traceLevel <= 2  // Error and below
| extend FeedName = extract(@"\|(.+)$", 1, tagId)
| summarize ErrorCount=count() by FeedName
| order by ErrorCount desc
```

```kql
// Regional deployment health (Service Fabric)
TraceEvent
| where env_time > ago(1h)
| summarize count() by env_cloud_role, env_cloud_location
| order by count_ desc
```

### 4.5 Indicator Pipeline Health

```kql
// End-to-end pipeline throughput
Log
| where env_time > ago(1h)
| summarize count() by applicationName, bin(env_time, 5m)
| order by env_time desc, count_ desc
```

```kql
// CosmosDB publish success vs failure
Log
| where env_time > ago(1h) and applicationName == "CosmosDbPublisher"
| summarize Total=count(), Errors=countif(severityText == "Error")
  by bin(env_time, 5m)
| extend SuccessRate=round(100.0*(Total-Errors)/Total, 2)
| order by env_time desc
```

```kql
// Event Hub publish health
Log
| where env_time > ago(1h) and applicationName == "LogAEventHubPublisher"
| summarize Total=count(), Errors=countif(severityText == "Error")
  by bin(env_time, 5m)
| extend SuccessRate=round(100.0*(Total-Errors)/Total, 2)
| order by env_time desc
```

```kql
// Bulk actions status
Log
| where env_time > ago(24h) and applicationName == "ThreatIntelligenceBulkActionsExecutor"
| summarize Total=count(), Errors=countif(severityText == "Error")
  by bin(env_time, 1h)
| extend ErrorRate=round(100.0*Errors/Total, 2)
| order by env_time desc
```

---

## 5. ICM Investigation Patterns

### 5.1 ARM Watchlist Error ICMs (767815474 / 767416366)

Both incidents reference the same pattern: ARM detects elevated error rates for `MICROSOFT.SECURITYINSIGHTS/WATCHLISTS` on endpoint `prd-weu-402.sentinel.microsoft.com`.

**Kusto queries extracted from ICM enrichment:**

```kql
// Top failing endpoints (from ARM-side — requires ARMProdEG macro-expand, not runnable from this cluster)
macro-expand isfuzzy=true ARMProdEG as X
(
X.database('Requests').HttpOutgoingRequests
| where TIMESTAMP between(datetime(2026-03-24T20:38:17.560Z)..datetime(2026-03-24T21:18:17.560Z))
| where (httpStatusCode >= 500 or httpStatusCode == 0)
| where TaskName == "HttpOutgoingRequestEndWithServerFailure"
| where targetResourceProvider == toupper("MICROSOFT.SECURITYINSIGHTS")
| summarize count() by targetUri, operationName
)
| order by count_ desc
| limit 10
```

**⚠️ NOTE:** The `macro-expand ARMProdEG` queries CANNOT be run against `ti-prod-kusto-cluster`. They target the ARM Production Event Group cluster. These require SAW access or DGrep.

**RP-side equivalent queries (CAN be run on `securityinsights.kusto.windows.net`):**

```kql
// Watchlist availability from RP side (from Watchlist TSG)
cluster("https://securityinsights.kusto.windows.net/").database("SecurityInsightsProd").ServiceFabricDynamicOEWithFilteredSubscriptions
| where env_time > ago(3d)
| where operationName has "WatchlistsController"
| extend totalFailures = iff(resultType == "Failure", 1, 0)
| summarize totalRequests = count(),
    failureCount = sum(todouble(totalFailures)),
    LatencyMsP50 = percentile(durationMs, 50),
    LatencyMsP95 = percentile(durationMs, 95),
    LatencyMsP99 = percentile(durationMs, 99),
    SLA = (1 - sum(todouble(totalFailures)) / count()) * 100
  by operationName
| sort by operationName asc
```

**RP-side equivalent on THIS cluster (if data is present):**

```kql
// STIX API errors during incident window
StixWebApiLogs
| where env_time between(datetime(2026-03-24T21:00:00Z)..datetime(2026-03-24T22:00:00Z))
| where resultType == "Failure"
| summarize count() by component, dataCenter
| order by count_ desc
```

### 5.2 Cross-Cluster Query Patterns

The TI ecosystem uses multiple Kusto clusters. Here's the landscape:

| Cluster | Database | Purpose | Access |
|---------|----------|---------|--------|
| `ti-prod-kusto-cluster.northeurope.kusto.windows.net` | `prod` | TI pipeline telemetry (this cluster) | ✅ Accessible |
| `sentinelwatchlistweu.westeurope.kusto.windows.net` | `SentinelWatchlistWEU` | Watchlist RP logs (WEU region) | ✅ Accessible (confirmed in past investigation) |
| `securityinsights.kusto.windows.net` | `SecurityInsightsProd` | Sentinel platform logs | ⚠️ Not tested this session |
| `icmcluster.kusto.windows.net` | `IcMDataWarehouse` | IcM incident data warehouse | ✅ Accessible (used by icm-scan.ps1) |
| ARMProdEG (macro-expand) | `Requests` | ARM-side request logs | ❌ Requires SAW/DGrep |
| Brain (slidata/healthevents) | Various | SLI/health data | ❌ Requires `brain-dashboard-sg` security group |

### 5.3 Investigation Template: ARM Error Rate ICM

When an ARM error rate ICM fires for TI services, follow this query sequence:

**Step 1: Check RP-side health on this cluster**
```kql
// Quick health check — are we seeing errors?
StixWebApiLogs
| where env_time between(datetime(<IMPACT_START>)..datetime(<IMPACT_END>))
| summarize Total=count(), Failures=countif(resultType == "Failure")
  by bin(env_time, 5m), dataCenter
| extend ErrorRate=round(100.0*Failures/Total, 2)
| order by env_time desc
```

**Step 2: Identify affected components**
```kql
StixWebApiLogs
| where env_time between(datetime(<IMPACT_START>)..datetime(<IMPACT_END>))
| where resultType == "Failure"
| summarize count() by component
| order by count_ desc
```

**Step 3: Check broader TI pipeline health**
```kql
Log
| where env_time between(datetime(<IMPACT_START>)..datetime(<IMPACT_END>))
| summarize Total=count(), Errors=countif(severityText == "Error")
  by applicationName, bin(env_time, 5m)
| extend ErrorRate=round(100.0*Errors/Total, 2)
| where ErrorRate > 10  // flag services with >10% error rate
| order by env_time desc
```

**Step 4: Check TAXII feed health (if connector-related)**
```kql
TraceEvent
| where env_time between(datetime(<IMPACT_START>)..datetime(<IMPACT_END>))
| where tagId startswith "TAXIIActor:"
| where traceLevel <= 2
| extend FeedName = extract(@"\|(.+)$", 1, tagId)
| summarize ErrorCount=count() by FeedName
| order by ErrorCount desc
```

---

## 6. Common Kusto Patterns for TI

### 6.1 Time-Range Filtering

```kql
// Last 1 hour
| where env_time > ago(1h)

// Last 24 hours
| where env_time > ago(24h)

// Last 7 days
| where env_time > ago(7d)

// Specific incident window
| where env_time between(datetime(2026-03-24T21:00:00Z)..datetime(2026-03-24T22:00:00Z))
```

### 6.2 Aggregation Patterns

```kql
// By region
| summarize count() by applicationRegion  // Log table
| summarize count() by dataCenter         // StixWebApiLogs

// By error type
| summarize count() by resultType         // StixWebApiLogs
| summarize count() by severityText       // Log table

// By time bucket
| summarize count() by bin(env_time, 5m)  // 5-minute windows
| summarize count() by bin(env_time, 1h)  // 1-hour windows

// Error rate calculation
| summarize Total=count(), Errors=countif(resultType == "Failure")
| extend ErrorRate=round(100.0*Errors/Total, 2)

// Percentile latencies
| summarize P50=percentile(durationMs, 50),
    P95=percentile(durationMs, 95),
    P99=percentile(durationMs, 99)
```

### 6.3 Join/Correlation Patterns

```kql
// Correlate StixWebApiLogs failures with Log table errors
StixWebApiLogs
| where env_time > ago(1h) and resultType == "Failure"
| summarize FailedRequests=count() by bin(env_time, 5m)
| join kind=inner (
    Log
    | where env_time > ago(1h) and severityText == "Error"
    | summarize LogErrors=count() by bin(env_time, 5m)
) on env_time
| project env_time, FailedRequests, LogErrors
| order by env_time desc
```

```kql
// Cross-table correlation: TAXII errors vs. ingestion errors
TraceEvent
| where env_time > ago(1h) and tagId startswith "TAXIIActor:" and traceLevel <= 2
| summarize TAXIIErrors=count() by bin(env_time, 5m)
| join kind=leftouter (
    SentinelLogEntry
    | where env_time > ago(1h) and ApplicationName == "IngestionApi" and LogLevel == 2
    | summarize IngestionErrors=count() by bin(env_time, 5m)
) on env_time
| project env_time, TAXIIErrors, IngestionErrors=coalesce(IngestionErrors, 0)
```

### 6.4 Alert Threshold Patterns

Based on observed baselines:

| Metric | Baseline | Alert Threshold | Query |
|--------|----------|----------------|-------|
| STIX API error rate | 3–5% | >10% | `StixWebApiLogs \| ... \| where ErrorRate > 10` |
| NormalizationService errors/hr | ~1.9M | >3M | `Log \| where applicationName == "NormalizationService" and severityText == "Error" \| summarize count() by bin(env_time, 1h)` |
| TAXII feed inactivity | Continuous | No events for >30min | `TraceEvent \| where tagId startswith "TAXIIActor:" \| summarize MaxTime=max(env_time) by tagId \| where MaxTime < ago(30m)` |

---

## 7. Error Rate Snapshot (2026-03-24 to 2026-03-25)

### StixWebApiLogs hourly error rates:

| Time (UTC) | Total Requests | Failures | Error Rate |
|------------|---------------|----------|-----------|
| 25 Mar 12:00 | 33,104 | 667 | 2.01% |
| 25 Mar 11:00 | 479,622 | 20,490 | 4.27% |
| 25 Mar 10:00 | 625,547 | 28,306 | 4.52% |
| 25 Mar 09:00 | 633,676 | 25,211 | 3.98% |
| 25 Mar 08:00 | 624,222 | 22,373 | 3.58% |
| 25 Mar 07:00 | 708,956 | 31,720 | 4.47% |
| 25 Mar 06:00 | 879,975 | 32,306 | 3.67% |
| 25 Mar 05:00 | 812,310 | 31,475 | 3.87% |
| 25 Mar 04:00 | 801,735 | 27,206 | 3.39% |
| 25 Mar 03:00 | 512,830 | 29,247 | 5.70% |
| 25 Mar 02:00 | 3,323,374 | 26,786 | 0.81% |
| 25 Mar 01:00 | 2,645,239 | 31,274 | 1.18% |
| 25 Mar 00:00 | 1,122,309 | 29,554 | 2.63% |
| 24 Mar 23:00 | 557,612 | 29,232 | 5.24% |
| 24 Mar 22:00 | 682,909 | 30,735 | 4.50% |
| 24 Mar 21:00 | 529,096 | 30,762 | 5.81% |
| 24 Mar 20:00 | 582,914 | 28,721 | 4.93% |
| 24 Mar 19:00 | 424,974 | 29,494 | 6.94% |
| 24 Mar 18:00 | 595,258 | 30,728 | 5.16% |
| 24 Mar 17:00 | 682,823 | 30,443 | 4.46% |
| 24 Mar 16:00 | 1,104,895 | 28,314 | 2.56% |
| 24 Mar 15:00 | 888,212 | 30,649 | 3.45% |
| 24 Mar 14:00 | 861,644 | 27,217 | 3.16% |
| 24 Mar 13:00 | 946,276 | 31,572 | 3.34% |

**Observation:** Failure counts remain roughly constant (~27–32K/hr) while total volume fluctuates dramatically (425K–3.3M/hr). This suggests a consistent background failure rate, NOT correlated with load.

---

## 8. Related Kusto Clusters (For Cross-Reference)

| Cluster | Access | Past Use |
|---------|--------|----------|
| `sentinelwatchlistweu.westeurope.kusto.windows.net` / `SentinelWatchlistWEU` | ✅ Confirmed | ICM 766712513 — RP-side success rate analysis. Showed 99.94–99.97% RP success while ARM showed 73.63%. |
| `icmcluster.kusto.windows.net` / `IcMDataWarehouse` | ✅ Confirmed | Used by `icm-scan.ps1` for `IncidentsSnapshotV2()` queries |
| `securityinsights.kusto.windows.net` / `SecurityInsightsProd` | ⚠️ Not tested | Referenced in Watchlist TSGs for `ServiceFabricDynamicOEWithFilteredSubscriptions` table |
| `ti-kusto-cluster.westus3.kusto.windows.net` / `prod` | ⚠️ Not tested | Referenced in IcM Copilot investigation for ICM 767416366 |

---

## 9. Access Issues & Workarounds

### Azure MCP Kusto Tool (BROKEN)

The `azure-mcp-kusto` MCP tool returns `FileNotFoundException` for all operations (query, list databases, list tables, etc.). This is a tool-level configuration issue, not a cluster access issue.

**Workaround:** Use the Kusto REST API directly via PowerShell:

```powershell
# Get token
$token = az account get-access-token --resource "https://kusto.kusto.windows.net" --query accessToken -o tsv

# Run a KQL query
$body = @{ db = "prod"; csl = "<KQL QUERY>" } | ConvertTo-Json
$response = Invoke-RestMethod `
    -Uri "https://ti-prod-kusto-cluster.northeurope.kusto.windows.net/v1/rest/query" `
    -Method Post `
    -Headers @{ "Authorization" = "Bearer $token"; "Content-Type" = "application/json" } `
    -Body $body

# Run a management command (.show, etc.)
$response = Invoke-RestMethod `
    -Uri "https://ti-prod-kusto-cluster.northeurope.kusto.windows.net/v1/rest/mgmt" `
    -Method Post `
    -Headers @{ "Authorization" = "Bearer $token"; "Content-Type" = "application/json" } `
    -Body $body
```

### Token Resource Gotcha

The authentication resource for Kusto tokens is **always** `https://kusto.kusto.windows.net` — NOT the individual cluster URI. Using the cluster URI as the resource results in a 401 Unauthorized.

---

## 10. Summary of Key Findings

1. **Single database, 4 tables:** The cluster is simpler than expected. `prod` is the only database with `Log`, `SentinelLogEntry`, `StixWebApiLogs`, and `TraceEvent` tables.

2. **~7-day retention:** All tables retain approximately 7 days of data (except TraceEvent which appears to be ~1 day).

3. **StixWebApiLogs is the best table for API-level investigation:** It has structured fields for operation results, components, durations, and data centers.

4. **Log table is the most comprehensive:** 1.93B rows covering all TI pipeline services with application-level telemetry.

5. **TraceEvent reveals TAXII feed activity:** Shows real-time TAXII actor polling from external feeds (Mandiant, SOCRadar, IBM X-Force, etc.) across 10+ Service Fabric deployment regions.

6. **Baseline error rate is 3–5%** for the STIX API, with failure counts constant regardless of load volume.

7. **NormalizationService generates the most errors** (~1.9M errors/hr out of ~5.9M total events), warranting dedicated monitoring.

8. **Azure MCP Kusto tool is broken** — use REST API workaround. The tool team should be notified.

9. **ARM-side queries (macro-expand ARMProdEG) cannot be run from this cluster.** They require SAW access or DGrep. This is a fundamental gap for incident investigation.
