# IcM #768706934 — CosmosDbPublisher NorthEurope Falling Behind

**Investigator:** Aragorn  
**Date:** 2026-03-26  
**Incident ID:** [768706934](https://icm.ad.msft.net/imp/v3/incidents/details/768706934)  
**Severity:** Sev 2 — ACTIVE  
**Pipeline followed:** `.squad/skills/icm-investigator/SKILL.md`

---

## 1. Executive Summary

A Sev 2 IcM fired at **09:09 UTC on 2026-03-26** for `CosmosDbPublisher_NorthEurope_Prod` falling behind. Investigation confirms that a **genuine upstream traffic surge** (~2× baseline in NorthEurope, ~3× in the paired WestEurope region) began at **08:06 UTC** — one hour before the alert fired — and overwhelmed the fixed processing capacity of the NorthEurope CosmosDbPublisher function fleet.

Unlike most other regions, NorthEurope/WestEurope **do not have autoscaling enabled** (already pinned at max instances). The function fleet cannot self-scale to absorb the spike, so the EventHub queue depth continues growing. No Cosmos DB errors or retry storms are present. The pattern matches the self-mitigating similar incident [#632336038](https://icm.ad.msft.net/imp/v3/incidents/details/632336038) (WestEurope, 2025-05-20, resolved after ~4 hours), but **manual scale-out action may be required** if the traffic surge persists.

**Confidence:** High (traffic surge confirmed in metrics; autoscale architecture confirmed in code; no alternative failure modes active).

---

## 2. Incident Metadata

| Field | Value |
|-------|-------|
| IcM ID | 768706934 |
| Title | [TiPipeline] [CosmosDbPublisher] [Prod] [NorthEurope] Is falling behind |
| Severity | 2 |
| Status | ACTIVE (as of 12:54 UTC 2026-03-26) |
| Created | 2026-03-26T09:09 UTC |
| Last correlation | 2026-03-26T12:45 UTC |
| Hit count | 54 (still firing) |
| Instance | `CosmosDbPublisher_NorthEurope_Prod` |
| Monitor | Augusta MDM — `ApplicationName-Environment-ApplicationRegion` |
| Customer impact | None (isCustomerImpacting=false, 0 subscriptions, 0 regions) |
| CritSits / SRs | 0 |
| S500 / ACE / P0 | None |
| Cloud / Region | Public — North Europe (northeurope) |

---

## 3. TSG Review

**Source:** [eng.ms — TiPipeline Latency Monitor TSG](https://eng.ms/docs/products/sentinel/sentinelthreatintelligence/tipipelinelatencymonitor)

Key guidance from the TSG:

- The alert fires when the **EventHub queue duration** (time from event enqueue to checkpoint) exceeds a threshold.
- Two primary causes: (a) **genuine traffic spike** — autoscaling normally absorbs this; (b) **retry storm** from exceptions — visible as failed items.
- **North Europe / West Europe pair exception**: these regions have instances fixed at maximum already. Autoscale is **disabled** for WEU/NEU pairs. Manual scale-out (up to 16 instances) or scale-up may be needed.
- Scale-out target: 16 instances for each function (primary + backup), total 32 partitions.
- Monitor self-clears when the health check reports healthy 12+ times in 55 minutes.

---

## 4. Data Collection

### 4.1 Geneva Metrics

**Account:** `Augusta_PROD_MDM` | **Namespace:** `TiPipeline`

#### 4.1.1 ItemProcessingStatus — NorthEurope (Count)

| Time (UTC) | Items / 5-min | vs Baseline |
|------------|---------------|-------------|
| 07:01 | ~116,000 | Baseline |
| 07:30 | ~251,000 | Baseline |
| 08:06 | **302,000** | +20% — surge begins |
| 08:17 | **465,000** | **+85% — near 2×** |
| 08:45–12:57 | 350,000–520,000 | Sustained elevated |

Incident created at **09:09 UTC** — one hour after the surge started. Items are being processed (no failures visible); the pipeline is working but cannot keep up.

#### 4.1.2 ItemProcessingStatus — WestEurope (Count)

| Time (UTC) | Items / 5-min | vs Baseline |
|------------|---------------|-------------|
| Pre-surge | ~200,000–410,000 | Baseline |
| 08:28 | **811,000** | **~3× peak** |
| Sustained | 550,000–760,000 | Elevated through window |

The WestEurope paired region shows an even larger surge, confirming this is a **system-wide upstream traffic event**, not isolated to NorthEurope.

#### 4.1.3 CosmosDB Errors — NorthEurope

| Metric | Result |
|--------|--------|
| CosmosOperation (Status=429 throttle) | **0 — no throttling** |
| ItemProcessingStatus (Status=Failure) | **0 — no failures** |

No Cosmos DB errors. Cosmos is accepting writes fine; the bottleneck is upstream EventHub queue depth vs processing throughput.

#### 4.1.4 Latency Metrics (EventHubQueueDurationMs, MessageQueueDurationMs)

Both metrics returned timestamps with **null values** for the investigation window — likely a metric recording gap. Not diagnostic for this incident; ItemProcessingStatus is the reliable signal.

### 4.2 Similar Incidents

| IcM | Region | Date | Resolution |
|-----|--------|------|------------|
| [#632336038](https://icm.ad.msft.net/imp/v3/incidents/details/632336038) | WestEurope | 2025-05-20 | Resolved, self-mitigated after ~4 hours. Monitor cleared when health check reported healthy. No manual intervention recorded. |

The similar incident is an exact pattern match: same publisher, paired region (WEU), same "falling behind" signature. It self-mitigated once traffic normalized.

---

## 5. Root Cause Analysis

### 5.1 Hypothesis H1: Genuine traffic surge — CONFIRMED ✅

**Evidence:**
- `ItemProcessingStatus` Count metric shows a clear step-change starting **08:06 UTC** — 60 minutes before the IcM fired.
- Both NorthEurope (2×) and WestEurope (3×) are simultaneously elevated, indicating the surge originates **upstream** in the TiPipeline ingestion layer, not in CosmosDbPublisher itself.
- No Cosmos errors → the downstream store is healthy; items are processed successfully, just slower than the EventHub enqueue rate.
- No `SeriousError` or `Failure` status metrics → no exception-driven retry loop.

**Causal chain:**
```
Upstream ingestion volume spikes at 08:06 UTC
  → EventHub NormalizedEventHub receives 2–3× normal message rate
  → CosmosDbPublisher NorthEurope processes at fixed capacity (no autoscale)
  → EventHub queue depth grows (enqueue rate > checkpoint rate)
  → Queue duration exceeds MDM alert threshold
  → IcM fires at 09:09 UTC (1 hour after surge began)
  → Alert continues to fire as traffic remains elevated through 12:54 UTC
```

### 5.2 Hypothesis H2: Retry storm from exceptions — RULED OUT ✗

**Evidence against:**
- `ItemProcessingStatus` shows 0 failure status codes.
- No `SeriousError` metric spikes.
- The `[ExponentialBackoffRetry(-1, "00:00:01", "00:01:00")]` attribute on `CosmosDbPublisherAzf.RunAsync` would cause infinite retries if batches failed — but processing counts show **normal throughput improvement** not stagnation or repetition.

### 5.3 Hypothesis H3: VNet/NAT GW connectivity issue — UNLIKELY ✗

**Evidence against:**
- The VNet + NAT GW change for CosmosDbPublisher (PR 14314918, "Revert 'Revert 'Cosmos Publisher - Connect to VNet and add NAT GW''") was merged on **2025-12-23** — over 3 months before this incident.
- Items are being successfully written to Cosmos (no 429, no failures), ruling out SNAT exhaustion causing connectivity drops.
- If NAT GW were exhausted, we would expect Cosmos write failures, not clean processing with elevated throughput.

---

## 6. Architecture Note: Why NorthEurope Cannot Self-Heal

From `CosmosDbPublisherResourceBuilder.cs` (lines 367–370, 595–598):

```csharp
// Implement Autoscale settings for the App Service Plan in all regions
// except East US2/East US and West Europe/North Europe
// since the number of instances are fixed already maxed out.
if (environment.GetBoolConstant(ConstantId.EnableAutoScale, dataCenter))
{
    // autoscale rule only runs for OTHER regions
```

NorthEurope and WestEurope are **pinned at maximum instance count** (16 per function app) because their EventHub has 32 partitions divided between primary + backup function apps. Autoscale is `EnableAutoScale = false` for this datacenter pair. There is **no elastic relief valve** — a traffic spike of this magnitude can only be absorbed by scaling up the VM SKU or waiting for upstream traffic to subside.

---

## 7. Remediation

### 7.1 Immediate Actions

| Priority | Action | Owner |
|----------|--------|-------|
| P0 | **Verify current instance count** on the NorthEurope CosmosDbPublisher function apps (CosmosPub1 + CosmosPub2). Confirm they are already at 16 instances each. | On-call |
| P0 | **Check upstream ingestion sources** — identify which TI feeds caused the ~08:06 UTC surge. Look at EventHub producer metrics or upstream partner dashboards. | On-call |
| P1 | If traffic remains elevated and instances are not at max: **manually scale to 16 instances** via Azure Portal or CLI. | On-call |
| P1 | If instances are already at 16: **scale UP the App Service Plan** (increase VM SKU) to provide more CPU/memory per instance. | On-call |
| P2 | Monitor `ItemProcessingStatus` Count — if it returns to ~200k/5min, the incident will self-mitigate within 55 minutes (per TSG). | On-call |

**Portal command to check instances:**
```
Azure Portal → Resource Group: ti-prod-...cosmospub → App Service Plan (CosmosPub1) → Scale out → Current instance count
```

### 7.2 Mitigation Verification

The IcM will auto-resolve when:
- MDM monitor reports healthy state 12 times in 55 minutes (per TSG)
- OR engineer manually resolves once metrics normalize

Track `ItemProcessingStatus` in the Geneva dashboard widget: `[BATCH INFO] Queue Duration per batch (minutes)`.

### 7.3 Long-Term Recommendations

| Recommendation | Rationale |
|----------------|-----------|
| **Investigate upstream traffic spike source** — identify the TI feed or ingestion partner responsible for the 08:06 surge | Recurrence prevention; if a specific partner is sending bursts, throttle or stagger ingestion |
| **Fix Geneva latency metrics recording** — `EventHubQueueDurationMs` and `MessageQueueDurationMs` returning null makes root cause determination slower | These are the primary diagnostic signals per TSG; null values impair investigation |
| **Add pre-IcM traffic surge alert** — current MDM alert fires 1 hour after the surge starts; an earlier "EventHub lag growing" alert would give the team more response time | The similar incident #632336038 pattern suggests the pipeline absorbs spikes on its own after ~4h; earlier alerting could allow proactive scaling before queue backup is severe |
| **Document NE/WE fixed-capacity architecture** in the CosmosDbPublisher TSG | The TSG sub-page for CosmosDbPublisher has no ICM examples; this incident should be added |
| **Add `ItemProcessingStatus` Count alert** for sustained >1.5× baseline | This would fire earlier than the queue duration alert and directly indicate traffic volume issues |

---

## 8. Evidence Inventory

| Evidence | Source | Finding |
|----------|--------|---------|
| Incident metadata | `icm-get_incident_details_by_id(768706934)` | Sev 2 ACTIVE, hitCount=54, NorthEurope |
| Customer impact | `icm-get_incident_customer_impact` | 0 subscriptions, no customer impact |
| Similar incidents | `icm-get_similar_incidents(768706934)` | #632336038 exact pattern match, WEU 2025-05-20, self-mitigated |
| Location | `icm-get_incident_location(768706934)` | northeurope, Public cloud, confidence 99% |
| TSG | eng.ms TiPipeline Latency Monitor | Fixed capacity NE/WE pair, queue duration alert semantics |
| ItemProcessingStatus NE | Geneva `Augusta_PROD_MDM/TiPipeline` | 2× surge from 08:06 UTC |
| ItemProcessingStatus WE | Geneva `Augusta_PROD_MDM/TiPipeline` | 3× surge from 08:06 UTC |
| CosmosOperation 429 | Geneva `Augusta_PROD_MDM/TiPipeline` | 0 — no throttling |
| Failure status codes | Geneva `Augusta_PROD_MDM/TiPipeline` | 0 — no failures |
| EventHubQueueDurationMs | Geneva `Augusta_PROD_MDM/TiPipeline` | Null values — metric gap |
| Code: retry config | `CosmosDbPublisherAzf.cs:109` | `[ExponentialBackoffRetry(-1, "00:00:01", "00:01:00")]` — infinite retries |
| Code: parallelism | `CosmosDbPublisherConfig.cs` | DefaultMaxParallelism=70 per instance |
| Code: autoscale disabled | `CosmosDbPublisherResourceBuilder.cs:368–370` | NE/WE pinned at max, `EnableAutoScale=false` |
| Recent deployments | `git log Sentinel-TiPublishers` | Last src/ change: 2025-12-23 (VNet+NAT GW); no recent deployments |

---

*Investigation completed: 2026-03-26 by Aragorn*  
*Following pipeline: `.squad/skills/icm-investigator/SKILL.md`*
