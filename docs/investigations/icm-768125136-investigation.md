# ICM #768125136 — CosmosDbPublisher WEU Falling Behind
**Investigated by:** Aragorn  
**Requested by:** Jonathan Ben Ami  
**Investigation date:** 2026-03-25  
**Status at investigation time:** ACTIVE — unresolved  
**Severity:** 2 (LiveSite)  
**Classification:** ✅ TRUE POSITIVE — sustained pipeline failure, correctly alerted

---

## Executive Summary

On 2026-03-25 at 09:45 UTC, the CosmosDbPublisher Azure Function in West Europe began failing to process and checkpoint EventHub batches. By 10:08 UTC, the pipeline latency monitor fired: queue depth had reached **21.8 minutes** (1,306,470 ms) across multiple EventHub partitions. North Europe was also simultaneously affected.

The root cause is a known, recurring failure mode: **Cosmos DB HTTP 429 throttling** on legacy indicator upserts, combined with an **infinite retry policy** in the Azure Function that prevents EventHub checkpointing and creates a self-reinforcing retry storm. A specific high-volume TAXII connector feeding workspace `b6dfb36f-5727-4e9a-89ae-12df6706e278` was the trigger source, generating over 1,900 failing writes per minute at peak.

This is the **second occurrence** of this exact failure within 9 days — the previous incident (#763122287, 2026-03-16) was identical in title, root cause, and region, and self-resolved after ~3.7 hours without engineering action. As of this investigation (13:25 UTC), the current incident has been active for **3+ hours** with no sign of natural recovery and no mitigation applied.

---

## Stage 1: Triage & Context

### Incident Metadata

| Field | Value |
|-------|-------|
| Incident ID | 768125136 |
| Title | [TiPipeline] [CosmosDbPublisher] [Prod] [westeurope] Is falling behind |
| Severity | 2 |
| State | ACTIVE |
| Created | 2026-03-25T10:09:31 UTC |
| Impact start | 2026-03-25T10:08:49 UTC |
| Alert source | ADROCS (MDM-Ext-prod5-black) |
| Owning team | Threat Intelligence (USX Threat Intelligence) |
| Assigned to | jbenami |
| Acknowledged | ✅ 2026-03-25T11:26 UTC |
| TSG | [TiPipeline Latency Monitor TSG](https://eng.ms/docs/microsoft-security/microsoft-threat-protection-mtp/onesoc-1soc/usx-core/sentinel-us/ti-augusta/troubleshooting/monitors/tipipelinelatencymonitor) |

### Impact Assessment

| Dimension | Assessment |
|-----------|-----------|
| Customer-impacting (formal) | ❌ Not flagged in IcM |
| Support requests / CritSits | 0 |
| Impacted subscriptions | 0 (formally tracked) |
| Actual customer impact | ⚠️ YES — workspaces on affected EventHub partitions experience **delayed threat intelligence writes** to Cosmos DB in both WEU and NEU |
| Data pipeline health | ❌ Severely degraded — 86–93% error rate sustained across both regions for 3+ hours |

### Location

- **Primary region:** West Europe (PROD)
- **Secondary region:** North Europe — also affected (same pattern, same failure signature)
- **Instance:** `CosmosDbPublisher_westeurope_PROD`
- **EventHub:** `ti-prod-weu-normalized-eh-pair.servicebus.windows.net` → `normalizedeventhub`

### Classification

**TRUE POSITIVE.** The monitor correctly detected that the Azure Function had stalled and was not making progress on EventHub partitions. The evaluation value of 1,306,470 ms (~21.8 minutes of queue latency) reflects a genuine pipeline failure. No evidence of false alarm.

**Severity Assessment:** Sev2 is **correct**. Both WEU and NEU are degraded. Threat intelligence publishing pipeline is accumulating backlog. If the retry storm continues, the backlog may grow beyond recovery without intervention.

---

## Stage 2: Data Enrichment

### IcM Copilot Pre-Investigation (12:17–12:27 UTC)

The IcM Copilot Autopilot (v2.8.30) ran a 10-minute automated investigation. Key evidence it produced:

**MessageQueueDurationMs by partition (WEU, 09:30–10:30 UTC):**  
Queue duration rose from seconds to hundreds of thousands then over 1,000,000 ms on many partitions. Same pattern confirmed in NEU.

**Failure signature (MessageQueueOperation metric, multiple partitions):**
```
CollectionProcessingException([ItemProcessingTerminatingException(
  PipelineItemProcessingException(
    PublishLegacyIndicatorToCosmos(
      Upsert(TooManyRequests)
    )
  )
)...])
```

**ItemProcessingStatus — impacted workspace (09:45–09:50 UTC, WEU):**

| Time window | WorkspaceId | DataType | LastUpdateMethod | Status | Failures |
|-------------|-------------|----------|------------------|--------|----------|
| 09:45–09:46 | b6dfb36f-5727-4e9a-89ae-12df6706e278 | Indicator | TAXIIConnector | Fail | 1,906 |
| 09:46–09:47 | b6dfb36f-5727-4e9a-89ae-12df6706e278 | Indicator | TAXIIConnector | Fail | 1,443 |
| 09:47–09:48 | b6dfb36f-5727-4e9a-89ae-12df6706e278 | Indicator | TAXIIConnector | Fail | 1,074 |
| 09:48–09:49 | b6dfb36f-5727-4e9a-89ae-12df6706e278 | Indicator | TAXIIConnector | Fail | 1,037 |
| 09:49–09:50 | b6dfb36f-5727-4e9a-89ae-12df6706e278 | Indicator | TAXIIConnector | Fail | 652 |

This single workspace generated **~6,100 failures in 5 minutes** during the early phase of the incident.

### My Kusto Queries (ti-prod-kusto-cluster.northeurope.kusto.windows.net)

**CosmosDbPublisher WEU — Log error rate (last 4 hours at query time):**

| Time window | Total log lines | Error lines | Error rate |
|-------------|-----------------|-------------|------------|
| 09:30–10:00 | 46,713 | 40,415 | 86.5% |
| 10:00–10:30 | 57,783 | 53,532 | 92.6% |
| 10:30–11:00 | 58,164 | 53,919 | 92.7% |
| 11:00–11:30 | 56,341 | 52,134 | 92.5% |
| 11:30–12:00 | 61,208 | 56,839 | 92.9% |
| 12:00–12:30 | 55,311 | 50,851 | 91.9% |
| 12:30–13:00 | 59,389 | 54,859 | 92.4% |
| 13:00–13:30 | 51,858 | 47,406 | 91.4% |

**Assessment: The incident is STILL ACTIVE as of 13:25 UTC. No recovery trend visible.**

**CosmosDbPublisher NEU — Log error rate (last 4 hours at query time):**

| Time window | Total log lines | Error lines | Error rate |
|-------------|-----------------|-------------|------------|
| 09:30–10:00 | 30,659 | 26,769 | 87.3% |
| 10:00–10:30 | 32,740 | 30,103 | 91.9% |
| 10:30–11:00 | 40,765 | 37,932 | 93.1% |
| 11:00–11:30 | 45,427 | 42,166 | 92.8% |
| 11:30–12:00 | 45,334 | 42,273 | 93.3% |
| 12:00–12:30 | 41,545 | 38,670 | 93.1% |
| 12:30–13:00 | 40,931 | 38,084 | 93.1% |
| 13:00–13:30 | 36,452 | 33,398 | 91.6% |

**NEU is equally degraded — no recovery.**

### Prior Incident Comparison (ICM #763122287, 2026-03-16)

| Field | Prior (#763122287) | Current (#768125136) |
|-------|--------------------|----------------------|
| Title | [TiPipeline] [CosmosDbPublisher] [Prod] [westeurope] Is falling behind | Identical |
| Root cause | Cosmos DB 429 throttling on legacy indicator upserts | Identical |
| Failure code | TooManyRequests on PublishLegacyIndicatorToCosmos(Upsert) | Identical |
| Regions | WEU + NEU | Identical |
| Resolution | Auto-mitigated ("Transient"), 3h 39min duration | Still active at 3h 17min |
| Resolved by | healthmanagesvc (monitor auto-heal) | Not yet resolved |
| Fixed category | Transient | Not yet classified |
| Engineering fix | None shipped | None shipped |

**The identical recurrence 9 days later confirms: no engineering fix was applied after the March 16 incident.**

### Geneva Metrics (Augusta_PROD_MDM / TiPipeline namespace)

- **MessageQueueDurationMs** metric exists and is queryable — confirms the monitor signal is a real MDM metric
- The metric `By-ApplicationName-ApplicationRegion-Environment-FunctionName-OperationName-ResourceDivision-ResourceName-ResourceSubName-ResourceType-StatusCode` pre-aggregation supports partition-level drill-down
- The **ItemProcessingStatus** metric (also in this namespace) is the source for workspace-level failure attribution
- Geneva metric data points were confirmed but dimension value normalization requires exact casing (data enrichment was completed via Kusto)

---

## Stage 3: Root Cause Analysis

### Causal Chain (Confidence: HIGH)

```
TAXII Connector → b6dfb36f workspace
  ↓ high-volume indicator writes arrive on normalizedeventhub (WEU)
  ↓ CosmosDbPublisherAzf picks up batch
  ↓ Calls PublishLegacyIndicatorToCosmos → Cosmos DB upsert
  ↓ Cosmos DB responds: HTTP 429 TooManyRequests (RU limit exceeded)
  ↓ PublishLegacyIndicatorToCosmos.cs: non-success = Fail → BATCH TERMINATING
  ↓ Azure Function: [ExponentialBackoffRetry(-1, "00:00:01", "00:01:00")] = INFINITE RETRY
  ↓ Batch checkpoint NOT written → EventHub offset NOT advanced
  ↓ Same batch retried on next invocation → same 429 → same failure
  ↓ Retry storm: each retry consumes RU → amplifies throttling
  ↓ Multiple partitions stuck → MessageQueueDurationMs rises across all affected partitions
  ↓ NEU affected: same workspace likely ingested to NEU container, same throttling pattern
  ↓ At 10:08 UTC: monitor fires (latency = 1,306,470 ms / 21.8 minutes)
  ↓ Incident continues — as of 13:25 UTC: 3h 17min, error rate still 91-93%
```

### Hypotheses

| # | Hypothesis | Evidence | Confidence |
|---|-----------|----------|------------|
| H1 | **Cosmos DB RU exhaustion on legacy indicator container** for workspace b6dfb36f | Confirmed by ItemProcessingStatus metric (TooManyRequests), failure code in CollectionProcessingException chain, IcM Copilot evidence | **HIGH** |
| H2 | **Infinite retry policy amplifying RU consumption** (self-reinforcing) | Confirmed by code review: `ExponentialBackoffRetry(-1, "00:00:01", "00:01:00")` in CosmosDbPublisherAzf.cs | **HIGH** |
| H3 | **Batch-terminating on 429 prevents checkpointing** | Confirmed by code analysis of PublishLegacyIndicatorToCosmos.cs + TSG documentation | **HIGH** |
| H4 | **TAXII connector surge** triggered initial throttling threshold | Supported by high write volume (1,906/min) and TAXIIConnector as LastUpdateMethod; direct confirmation (e.g., connector restart event) not available | **MEDIUM** |
| H5 | **Code change** deployed shortly before incident | FCM flagged "Change data found" in IcM context; not investigated (no FCM query run) | **LOW — unverified** |

### Why This Is Recurring

The March 16 incident resolved naturally after ~3.7 hours — likely because the TAXII connector finished its bulk sync, Cosmos throttling ceased, and the retry storm subsided. **No engineering fix was applied.** The March 25 recurrence confirms the underlying defects remain:

1. Infinite retry policy — unchanged since March 16
2. Batch-terminating on 429 — unchanged since March 16
3. No per-item DLQ for throttling errors — still absent
4. No RU auto-scaling or ingestion-side rate limiting — still absent

---

## Stage 4: Remediation

### Immediate Actions (within 1 hour — Jonathan/on-call)

| Priority | Action | Rationale |
|----------|--------|-----------|
| 🔴 P0 | **Pause or throttle the TAXII connector** feeding workspace `b6dfb36f-5727-4e9a-89ae-12df6706e278` | Removes the source of throttling pressure. Allows backlog to drain and Cosmos to recover. |
| 🔴 P0 | **Increase Cosmos RU** for the legacy indicator container serving this workspace | Directly addresses the throttling root cause per prior incident TSG guidance. |
| 🟠 P1 | **Reduce `MaxEventsProcessedInParallel`** for CosmosDbPublisherAzf from 70 to 20–30 | Reduces concurrent retry amplification, allowing existing RU to serve fewer simultaneous requests |
| 🟠 P1 | **Restart CosmosDbPublisherAzf** (after RU increase) in WEU | Clears stuck in-flight batches and allows function to pick up fresh from committed EventHub offset |

### Short-Term Fixes (within 1 sprint — engineering)

| Priority | Action | Files affected |
|----------|--------|----------------|
| 🔴 P0 | **Replace infinite retry with finite retry** (e.g., 5 attempts) in CosmosDbPublisherAzf | `CosmosDbPublisherAzf.cs` — `ExponentialBackoffRetry(-1, ...)` |
| 🔴 P0 | **Change 429 handling in PublishLegacyIndicatorToCosmos** from batch-fail to per-item skip + DLQ | `PublishLegacyIndicatorToCosmos.cs` — non-success path |
| 🟠 P1 | **Add Cosmos 429 rate metric** to monitoring dashboards | Geneva / ADROCS dashboards |
| 🟠 P1 | **Alert on sustained 429 rate BEFORE** MessageQueueDurationMs spikes | Add early-warning Geneva alert at 30%+ 429 rate |
| 🟡 P2 | **Add FCM change data review step** to ICM investigation checklist | Skipped in both March 16 and March 25 investigations |
| 🟡 P2 | **Post-incident review for March 16 incident** — enforce engineering work items before closure | Process gap: incident closed as "Transient" without tracking fix |

### Long-Term Prevention (roadmap)

| Action | Impact |
|--------|--------|
| Auto-scale Cosmos RU for legacy indicator containers | Eliminates class of throttling incidents |
| Add ingestion-side rate limiting per workspace | Prevents one workspace starving others |
| Migrate `PublishLegacyIndicatorToCosmos` to modern batch pattern with backpressure | Architectural fix; eliminates legacy path issues |
| Add recurrence detection to ICM monitor — escalate if same resource fires within 14 days | Catches repeat incidents before they become third occurrences |

---

## Timeline of Events

| Time (UTC) | Event |
|------------|-------|
| 09:45 | CosmosDbPublisher WEU queue duration begins rising sharply |
| 09:45–10:30 | Sustained batch failures: `PublishLegacyIndicatorToCosmos(Upsert(TooManyRequests))` |
| 09:45–09:50 | Workspace b6dfb36f generates 6,100+ failures in 5 minutes |
| 09:45–10:30 | NEU shows same pattern — paired-region impact |
| 10:08 | IcM monitor fires — latency evaluated at 1,306,470 ms (21.8 min) |
| 10:09 | IcM #768125136 created |
| 10:27 | Keywords updated by first responder |
| 11:24 | Troubleshooting links added to discussion |
| 11:26 | Incident acknowledged by jbenami |
| 12:17 | IcM Copilot Autopilot investigation begins |
| 12:27 | IcM Copilot investigation completes — root cause confirmed |
| 13:25 | Aragorn investigation run — **incident still ACTIVE, no recovery trend** |

---

## Connections to Prior Work

- **IcM #763122287** (2026-03-16): Identical incident, same root cause, same region. Resolved as "Transient" in 3.7 hours with no engineering fix shipped. **This current incident is a direct recurrence.**
- **CosmosDbPublisher** appears in TI Kusto research (`Log` table, primary telemetry source). The Log table structure was documented in the 2026-03-25 Kusto guide (Issue #156).
- **TAXII connectors** (workspace b6dfb36f uses TAXIIConnector) were extensively studied in the MSPKI/G2 investigation (ICM #764634026). That investigation found SecEng-Augusta and SecEng-Interflow handle TAXII — the same connector family is generating the write surge here.
- **Kusto REST API workaround** used successfully here (MCP Kusto tool remains broken — FileNotFoundException).

---

## Open Questions

1. **What triggered the TAXII connector surge on March 25?** Was it a scheduled bulk sync, a new feed subscription, or a connector restart?
2. **Was there a code or config change** deployed before 09:45 UTC? (FCM "Change data found" flag in IcM — not investigated)
3. **Is the RU provisioned** for workspace b6dfb36f's Cosmos container the same as March 16, or was it already increased post-incident?
4. **Why is NEU also throttling** — is the Cosmos container shared between WEU and NEU, or are both regions receiving the same TAXII feed?

---

*Aragorn | ICM Investigation | 2026-03-25*  
*Evidence sources: IcM MCP tools (all 9 Stage 1 calls), IcM Copilot Autopilot v2.8.30 (pre-run), Kusto REST API (ti-prod-kusto-cluster.northeurope.kusto.windows.net), Geneva MCP (Augusta_PROD_MDM / TiPipeline namespace)*
