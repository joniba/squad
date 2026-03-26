# ICM #768693081 — CosmosDbPublisher WEU Falling Behind (3rd Recurrence)

**Investigated by:** Aragorn
**Requested by:** Jonathan Ben Ami
**Investigation date:** 2026-03-26
**Status at investigation time:** ACTIVE — unresolved
**Severity:** 2 (LiveSite)
**Classification:** ✅ CONFIRMED RECURRENCE — identical root cause to IcM#768125136 and IcM#763122287

---

## Executive Summary

On 2026-03-26 at 08:33 UTC, the CosmosDbPublisher Azure Function in West Europe began falling behind again (per IcM `get_incident_details_by_id`: impactStartTime `2026-03-26T08:33:56.987Z`, created by `MDM-Ext-prod5-black` monitor). This is the **third occurrence of the identical failure in 10 days**, and it fired only **~9 hours** after the previous incident (IcM#768125136) was auto-mitigated by the health monitor at 23:44 UTC on March 25 (per IcM `get_incident_details_by_id` for #768125136: mitigateTime `2026-03-25T23:44:42.7Z`).

**This is not a new issue.** This is the same production defect identified and documented in yesterday's investigation: the infinite retry policy (`ExponentialBackoffRetry(-1, ...)` in `CosmosDbPublisherAzf.cs:108–123`, per source code review on 2026-03-25) converts transient Cosmos DB 429 throttling into a sustained multi-hour retry storm that prevents EventHub checkpointing (per investigation report `docs/investigations/icm-768125136-investigation.md`).

**The recommended P0 fix (bounded retry + per-item DLQ) has not been deployed** (engineering judgment: no code changes shipped since yesterday's investigation, confirmed by the fact that the identical failure pattern recurred within hours). The recurrence frequency is **accelerating** — from a 9-day gap to a 22-hour gap — and the recommended fix from yesterday's investigation remains unimplemented.

---

## Recurrence Timeline

| # | IcM ID | Date | Duration | Gap from Prior | Resolution |
|---|--------|------|----------|----------------|------------|
| 1 | [#763122287](https://portal.microsofticm.com/imp/v5/incidents/details/763122287) | 2026-03-16 10:28 UTC | ~3.7 hours | — | Closed as **"Transient"** by amitamar, no fix shipped (per IcM `get_incident_details_by_id`: `howFixed: "Transient"`, `resolvedBy: "amitamar"`) |
| 2 | [#768125136](https://portal.microsofticm.com/imp/v5/incidents/details/768125136) | 2026-03-25 10:08 UTC | ~13.6 hours | 9 days | Auto-mitigated by health monitor (per IcM: `mitigatedBy: "healthmanagesvc"`). Full investigation performed, P0 fix recommended. |
| 3 | [#768693081](https://portal.microsofticm.com/imp/v5/incidents/details/768693081) | 2026-03-26 08:33 UTC | **ACTIVE** (60 hits at investigation time) | **~9 hours** | Under investigation |

**Pattern:** Duration is increasing (3.7h → 13.6h → still active). Frequency is accelerating (9-day gap → 22-hour gap). Both are indicators of a worsening systemic issue, not transient noise (engineering judgment: the retry storm compounds over time as un-checkpointed EventHub partitions grow).

---

## Key Question 1: Is This the Same Root Cause?

**Answer: Yes — confirmed identical.**

Evidence:
- **Same title:** `[TiPipeline] [CosmosDbPublisher] [Prod] [westeurope] Is falling behind` — character-for-character match across all three incidents (per IcM `get_incident_details_by_id` for all three)
- **Same monitor:** All three fired from `MDM-Ext-prod5-black` with routing `ADROCS://Recovery/Augusta_PROD_MDM` targeting resource `CosmosDbPublisher_westeurope_Prod` (per IcM incident context: `routingId` and `correlationId` fields)
- **Same location:** West Europe, PROD, instance `CosmosDbPublisher_westeurope_Prod` (per IcM `get_incident_location`: `armRegion: "westeurope"`, confidence 99%)
- **Same owning team:** Threat Intelligence (team ID 116041) under USX Threat Intelligence tenant (per IcM `get_incident_details_by_id`)
- **IcM similar incidents confirms linkage:** IcM's mitigation hints directly reference prior CosmosDbPublisher incidents including IcM#694601823 (same title, WEU) and recommends "Increase Cosmos DB request units" and "monitoring with watchdogs" — the same self-heal-and-pray pattern that has failed to prevent recurrence (per IcM `get_mitigation_hints`)
- **No code changes deployed:** The infinite retry policy (`ExponentialBackoffRetry(-1, ...)` at `CosmosDbPublisherAzf.cs:108–123`) and the batch-terminating 429 handling (`CreateFail()` at `PublishLegacyIndicatorToCosmos.cs:171–180`) remain unchanged since yesterday's investigation (engineering judgment: the fix was recommended <24 hours ago with no deployment window between mitigation and recurrence)

The root cause chain is identical to what was documented in `docs/investigations/icm-768125136-investigation.md`:
1. Cosmos DB returns HTTP 429 (TooManyRequests) on legacy indicator upserts
2. `PublishLegacyIndicatorToCosmos.cs:171–180` throws `CreateFail()` for non-success responses (per source code review, 2026-03-25)
3. `CosmosDbPublisherAzf.cs:108–123` catches the exception and retries infinitely (`-1` = no limit) (per source code review, 2026-03-25)
4. Retry storm prevents EventHub checkpoint → queue depth grows → monitor fires
5. Self-reinforcing: more retries → more 429s → more retries

---

## Key Question 2: Was the Fix Deployed?

**Answer: No.**

The P0 fix recommended in yesterday's investigation — bounded retry count (5 attempts) and per-item DLQ for TooManyRequests — was **not deployed** (engineering judgment: the decision was documented in `.squad/decisions.md` on 2026-03-25 and routed to Jonathan for review and Gimli for implementation, but no code change has been shipped). The timeline makes this clear:

- **IcM#768125136 mitigated:** 2026-03-25 at 23:44 UTC (per IcM data)
- **IcM#768693081 fired:** 2026-03-26 at 08:33 UTC (per IcM data)
- **Gap:** ~9 hours — insufficient time for a code review + deployment cycle

This confirms the concern raised in yesterday's investigation: closing the previous incident (IcM#763122287) as "Transient" without tracking an engineering work item was a **process gap** that directly caused recurrence (per investigation report `docs/investigations/icm-768125136-investigation.md`, "Process Gap" section).

---

## Key Question 3: Should This Be Escalated?

**Answer: Yes — recommend escalation to Sev1 or formal repair item.**

**Escalation criteria met** (engineering judgment based on standard livesite escalation guidelines):

| Criterion | Assessment |
|-----------|------------|
| Recurrence count | 3 occurrences in 10 days |
| Frequency trend | **Accelerating** — 9 days → 22 hours between incidents |
| Duration trend | **Increasing** — 3.7h → 13.6h → still active |
| Fix status | P0 fix identified but **not deployed** |
| Customer impact | Marked as non-customer-impacting (per IcM: `isCustomerImpacting: false`), but pipeline latency affects TI data freshness downstream (engineering judgment) |
| Self-heal reliability | Auto-mitigation takes 4–14 hours; not a sustainable pattern |
| Hit count | 60 hits in first 4 hours of current incident (per IcM: `hitCount: 60`) vs 160 hits over 13.6 hours for IcM#768125136 (per IcM: `hitCount: 160`) — similar rate |

**Broader context:** IcM's mitigation hints reveal **18 similar incidents** across multiple regions (westus, westus2, eastus2, eastasia, canadaeast, canadacentral, francecentral, qatarcentral, ukwest, australiasoutheast) (per IcM `get_mitigation_hints`: `MitigatedSimilarIncidents` list). This is not a WEU-specific problem — it's a **fleet-wide architectural defect** in the CosmosDbPublisher retry handling.

---

## Recommendations

### Immediate (Today)

1. **Deploy emergency config change:** Reduce `MaxEventsProcessedInParallel` from 70 to 20 on the WEU Function App via Azure Portal to reduce concurrent pressure on Cosmos DB (engineering judgment: this buys time by reducing write concurrency; documented in yesterday's investigation report, Section 5 "Immediate Hotfix" walkthrough). This is a portal-only change and can be done without an EV2 deploy.

2. **Create a tracked Sev1 repair item:** The P0 fix (bounded retry + per-item DLQ) must be tracked as a formal repair item, not a discretionary backlog item (engineering judgment: 3 occurrences in 10 days with accelerating frequency meets the bar for mandated repair).

### Short-Term (This Week)

3. **Ship the bounded retry fix:** Replace `ExponentialBackoffRetry(-1, ...)` with `ExponentialBackoffRetry(5, ...)` in `CosmosDbPublisherAzf.cs:108–123` and add per-item `CreateSkip()` handling for HTTP 429 in `PublishLegacyIndicatorToCosmos.cs:171–180`, matching the existing pattern for CMK errors at lines 160–169 (per source code review, 2026-03-25). This was the P0 recommendation from yesterday.

4. **Evaluate Cosmos DB RU capacity for WEU:** IcM mitigation hints repeatedly recommend increasing RUs (per IcM `get_mitigation_hints`: "Increasing RUs helped pipeline catch up in prior incidents", referencing IcM#694601823 and IcM#721123894). If the WEU region is under-provisioned relative to the TAXIIConnector write volume, a capacity increase would reduce the frequency of 429s even before the retry fix ships.

### Medium-Term

5. **Fleet-wide audit:** 18 similar incidents across multiple regions (per IcM `get_mitigation_hints`) indicate this retry defect affects the entire CosmosDbPublisher fleet. The bounded retry fix must be deployed to all regions, not just WEU.

6. **Add 429-specific alerting:** Current monitor only detects "falling behind" (latency). Add a specific alert for sustained 429 error rates > 50% over 5 minutes, which would catch the retry storm before it cascades to pipeline lag (engineering judgment: early detection would cut MTTR significantly).

---

## Severity Assessment

| Factor | IcM#763122287 (Mar 16) | IcM#768125136 (Mar 25) | IcM#768693081 (Mar 26) |
|--------|------------------------|------------------------|------------------------|
| Duration | ~3.7h | ~13.6h | **Still active** |
| Severity | Sev2 | Sev2 | **Sev2 → recommend Sev1** |
| Resolution | Transient (no fix) | Auto-mitigated (fix recommended) | **Pending** |
| Hit count | 39 | 160 | 60+ (and climbing) |
| Root cause addressed? | ❌ No | ❌ No (recommended only) | ❌ No |

**Verdict:** This pattern now justifies **Sev1 treatment** — not because a single occurrence is Sev1, but because a known defect with a known fix is being allowed to recur at accelerating frequency. Each recurrence wastes engineering on-call time and degrades TI pipeline freshness for an increasing duration (engineering judgment: the escalation is about the unresolved defect, not the individual alert).

---

## Related Artifacts

- Prior investigation: `docs/investigations/icm-768125136-investigation.md`
- TSG: [TIPipeline Latency Monitor](https://eng.ms/docs/microsoft-security/microsoft-threat-protection-mtp/onesoc-1soc/usx-core/sentinel-us/ti-augusta/troubleshooting/monitors/tipipelinelatencymonitor) (per IcM `tsgLink`)
- Source code: `CosmosDbPublisherAzf.cs:108–123` (retry policy), `PublishLegacyIndicatorToCosmos.cs:160–180` (error handling), `CosmosDbPublisherConfig.cs:50–55` (MaxEventsProcessedInParallel)
- Decision log: `.squad/decisions.md` (2026-03-25 entry)
- GitHub issue: #158
