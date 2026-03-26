## Core Context

Archived history from aragorn. Preserved core metadata and most recent activity entry below. For full history, refer to git log.

---
## 2026-03-25: IcM#768125338 — CosmosDbPublisher NorthEurope RU Throttling Investigation

**Requested by:** Jonathan Ben Ami  
**Incident:** [TiPipeline] [CosmosDbPublisher] [Prod] [NorthEurope] Is falling behind  
**Severity:** 2 | **State:** ACTIVE | **Customer Impact:** None  
**Report:** docs/investigations/icm-768125338-investigation.md

## Learnings

**[HIGH] CosmosDbPublisher 429/RU-throttling is a known, recurring pattern — 30 incidents since Feb 2025.**
- Seen across westeurope, eastus, northeurope. All Sev 2. None customer-impacting. All self-heal in 3–4 hours.
- IcM Copilot auto-enriches the keywords with root cause on every firing — this is a reliable first-triage signal.
- Mitigation pattern: health manager auto-resolves when watchdog reports healthy 12× over ~55 minutes.
- Root cause: high-volume batch of indicator upserts (specifically "legacy" upserts) exceeds Cosmos DB provisioned RU/s.
- Causal chain: write burst → 429 throttling → retry storm → EventHub consumer lag → pipeline "falls behind".

**[HIGH] Geneva CosmosRUUsage is the key diagnostic metric for this incident class.**
- Account: Augusta_PROD_MDM, Namespace: TIPipeline, Metric: CosmosRUUsage
- Query without dimension filters to get the total picture; the 4× RU spike is the fingerprint of this failure mode.
- CosmosOperation total count drops at throttling onset (fewer successful ops) — use as corroborating signal.

**[MED] Northeurope Kusto cluster returns HTTP 400 for REST API queries.**
- The ti-prod-kusto-cluster.northeurope.kusto.windows.net cluster rejected all REST API queries with 400 BadRequest.
- Previous research documented this workaround for a different cluster context. Northeurope may require different access.
- Geneva metrics were sufficient to confirm RCA without Kusto log evidence for this incident class.

**[MED] Dimension-filtered Geneva queries return empty for CosmosOperation by ApplicationName/ResponseCode.**
- Total (unfiltered) CosmosOperation and CosmosRUUsage return data; dimension-filtered queries return empty.
- Possible preaggregation mismatch: the pre-agg "By-ApplicationName-Operation-ResponseCode" may not populate for all regions.
- Always try total (no dimensions) first, then narrow down.

**[LOW] This incident should be downgraded or suppressed.**
- 30 identical incidents over 13 months, all transient, none customer-impacting — this is Sev 3 / Noise territory.
- Recommended: add 30-minute suppression window to monitor, or change to Sev 3 for first 2 hours.
- Formal recommendation written in decisions inbox.


### 2026-03-25: ICM #768125136 - CosmosDbPublisher WEU Falling Behind (Full Investigation)

**Context:** Jonathan assigned Aragorn to investigate this Sev2 livesite. Second occurrence of identical failure (prior: ICM #763122287, 2026-03-16). Investigation run at 13:25 UTC while incident still ACTIVE.

**Root Cause Confirmed (HIGH confidence):**
- Cosmos DB HTTP 429 throttling on legacy indicator upserts (PublishLegacyIndicatorToCosmos)
- Workspace b6dfb36f-5727-4e9a-89ae-12df6706e278 (TAXIIConnector) generated 1,906+ failures/min at peak
- Infinite retry policy ExponentialBackoffRetry(-1) in CosmosDbPublisherAzf.cs creates retry storm
- Batch-terminating Fail on 429 prevents EventHub checkpoint -> queue grows indefinitely
- Both WEU and NEU affected (91-93% error rate sustained 4+ hours)

**Key Technical Findings:**
1. Kusto REST API confirmed working - Log table error rates show incident still active at 13:25 UTC
2. Geneva metrics (Augusta_PROD_MDM / TiPipeline namespace) confirmed accessible
3. Recurrence pattern: This is EXACTLY ICM #763122287 (March 16) - same title, root cause, region. Prior closed as Transient with no fix.
4. IcM Copilot Autopilot v2.8.30 ran a pre-investigation with actual metrics tables

**Engineering Fix Required (P0):**
- Replace infinite retry with finite (5 attempts) in CosmosDbPublisherAzf.cs
- Change 429 handling to per-item DLQ in PublishLegacyIndicatorToCosmos.cs

**Process Gap:** Prior incident closed Transient without engineering tracking -> direct cause of recurrence

**Delivered:** docs/investigations/icm-768125136-investigation.md, .squad/decisions/inbox/aragorn-icm-768125136.md

### 2026-03-25: ICM #768125136 Report Enrichment — Inline Citations + MaxEventsProcessedInParallel Walkthrough

**Context:** Jonathan requested two enrichments to the investigation report: (1) a step-by-step walkthrough for the MaxEventsProcessedInParallel recommendation, and (2) inline source citations throughout the entire report.

**Source Code Findings (Sentinel-TiPublishers repo):**
- **Function App naming:** `ti-prod-{geo}{index}-{region}-cosmospub-fa` (source: `CosmosDbPublisherResourceBuilder.cs:303–311`)
- **MaxEventsProcessedInParallel:** App setting, not host.json. Set to `70` via `.AddCustomAppSetting()` in topology code, read by `CosmosDbPublisherConfig.cs:50–55` from env var, fallback `DefaultMaxParallelism=70` at line 19
- **Deployment mechanism:** EV2 rollout → ARM templates → generated parameter files. Portal override possible for emergency but overwritten on next EV2 deploy
- **host.json:** Contains EventHub trigger tuning (maxEventBatchSize=2000, prefetchCount=20000) but NOT MaxEventsProcessedInParallel
- **Error handling:** `PublishLegacyIndicatorToCosmos.cs:171–180` throws `CreateFail()` on non-success (including 429). Only CMK errors (lines 160–169) get `CreateSkip()` treatment

**Citation Strategy Applied:**
- 48 inline citations added across all sections
- Citation types: `(source: file.cs:line)`, `(per IcM tool_name)`, `(per Kusto query)`, `(per Geneva metric)`, `(engineering judgment: reasoning)`
- No `## References` section added (Jonathan explicitly rejected that pattern)
- Walkthrough covers: what the app is, where the setting lives, two change paths (portal vs EV2 PR), recommended value with reasoning, rollback plan

**Key Learning:** Always search deployment topology code (not just runtime code) to find where config values originate. App settings flow: topology C# → EV2 ARM params → Azure Function env vars → runtime config class.
### 2026-03-26: ICM #768693081 — CosmosDbPublisher WEU Falling Behind (3rd Recurrence)

**Context:** Jonathan assigned Aragorn to investigate this Sev2 livesite. Third occurrence of identical failure in 10 days (prior: IcM#763122287 2026-03-16, IcM#768125136 2026-03-25). Confirmed recurrence, not new investigation.

**Key Findings:**
1. **Identical root cause confirmed** — same title, monitor, region, correlation pattern across all three incidents
2. **P0 fix NOT deployed** — bounded retry + per-item DLQ recommended yesterday was not shipped. Only 9 hours between previous mitigation and this recurrence.
3. **Accelerating frequency** — gap shrank from 9 days to 22 hours. Duration increasing: 3.7h → 13.6h → still active at investigation time.
4. **Fleet-wide scope** — IcM mitigation hints reveal 18 similar incidents across 10+ regions (westus, eastus2, eastasia, canadaeast, etc.)
5. **Recommended Sev1 escalation** — 3 occurrences of known defect with known fix meets mandatory repair bar

**Learnings:**

**[HIGH] Recurrence investigation should be fast — reuse prior analysis, don't re-investigate from scratch.**
- Prior investigation (IcM#768125136) already identified root cause with full source code citations. This investigation confirmed recurrence in ~15 minutes vs ~2 hours for the original.
- Key technique: cross-reference IcM timestamps (mitigateTime of prior vs impactStartTime of current) to prove gap and acceleration pattern.

**[HIGH] Closing IcMs as "Transient" without tracking engineering work creates predictable recurrence.**
- IcM#763122287 closed as "Transient" on March 16 → recurred March 25 → recurred March 26.
- Policy decision written: no more "Transient" closures for recurring defects with identified root cause.

**[MED] IcM mitigation hints reveal fleet-wide scope that individual incident investigation misses.**
- The `get_mitigation_hints` tool returned 18 similar incidents across 10+ regions — critical for escalation justification.
- Always check mitigation hints even on confirmed recurrences to assess blast radius.

**Delivered:** docs/investigations/icm-768693081-investigation.md, .squad/decisions/inbox/aragorn-icm-768693081.md


