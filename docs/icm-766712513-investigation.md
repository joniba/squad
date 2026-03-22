# ICM 766712513 — Investigation Report

**Incident:** ARM Increased Error Rates on MICROSOFT.SECURITYINSIGHTS/WATCHLISTS  
**Severity:** 2 | **State:** ACTIVE | **Type:** LiveSite  
**Investigated by:** Aragorn (Operator) | **Requested by:** Jonathan  
**Report Date:** 2026-03-22

---

## 1. Executive Summary

ARM monitoring (ADROCS) detected increased HTTP 5xx error rates for the `MICROSOFT.SECURITYINSIGHTS/WATCHLISTS` resource provider on endpoint `prd-weu-402.sentinel.microsoft.com` in **West Europe (Public Cloud)**. The incident was auto-created at **22:43 UTC** on 2026-03-22 and acknowledged within 3 minutes by Jonathan (jbenami).

This is a **Severity 2, ACTIVE** incident assigned to the **Threat Intelligence** team under **USX Threat Intelligence**. It has fired 3 times (hitCount=3) with the last correlation at 22:50 UTC. As of investigation time, **no root cause has been identified** and **no mitigation has been applied**.

The incident is **not currently marked as customer-impacting** — no support requests, CritSits, or impacted subscriptions have been formally registered. However, the 5xx errors indicate real API failures that warrant investigation.

---

## 2. Timeline

| Time (UTC) | Event |
|---|---|
| 2026-03-22 22:43:21 | **Impact start** — ARM detects increased error rates |
| 2026-03-22 22:43:25 | **Incident created** — Auto-filed by MDM-Ext-southcentralus-ac (ADROCS) |
| 2026-03-22 22:46:00 | **Acknowledged** — by jbenami (within ~3 minutes) |
| 2026-03-22 22:46:14 | **FCM enrichment** — Change data investigation link provided |
| 2026-03-22 22:46:27 | **Last modified** — by healthmanagesvc |
| 2026-03-22 22:50:37 | **Last correlation** — 3 total hits |
| 2026-03-22 22:51:35 | **Location classified** — West Europe, Public Cloud, 90% confidence |

**Duration so far:** Active since 22:43 UTC — ongoing.

---

## 3. Impact Assessment

### Formal Impact (from ICM tools)

| Metric | Value |
|---|---|
| Customer Impacting | **No** (not yet flagged) |
| CritSit Count | 0 |
| Support Requests | 0 |
| Impacted Subscriptions | 0 (formal count) |
| Impacted Regions | 0 (formal count) |
| Impacted Services | 0 (formal count) |
| Impacted Clouds | 0 |
| Is Outage | No |
| Has Bridge | No |

### Actual Impact Indicators

| Indicator | Detail |
|---|---|
| Error Type | HTTP 5xx (server errors) and HTTP 0 (timeouts/connection failures) |
| Affected Endpoint | `prd-weu-402.sentinel.microsoft.com` |
| Affected Resource Type | `MICROSOFT.SECURITYINSIGHTS/WATCHLISTS` |
| Affected Region | West Europe (`westeurope`) |
| Cloud | Azure Public |
| Environment | PROD |
| Hit Count | 3 (multiple detections) |

> **Note:** The formal impact metrics show zero, but this is a Sev2 with confirmed 5xx errors. The Kusto queries provided with the incident (see Section 4) should be run to determine actual subscription-level impact.

---

## 4. Root Cause Analysis (RCA)

### Current Status
**No root cause has been determined.** The incident is in early triage. The AI summary and incident context confirm that no cause or mitigation steps have been identified yet.

### Recommended Causes (from similar incidents)

ICM's mitigation hints engine identified 5 probable causes based on historically similar incidents:

| # | Probable Cause | Reference ICM | Relevance |
|---|---|---|---|
| 1 | **Customer errors** causing many create indicator calls to return 500 | 718932939 | Direct match with symptoms and endpoint |
| 2 | **Transient application timeouts** causing increased error rates on WATCHLISTS | 757455363 | Same endpoint + resource type (WATCHLISTS on prd-weu-402) — **highest relevance** |
| 3 | **Service issues** impacting prd-weu-402 endpoint causing API failures | 698715041 | Same endpoint, ARM request failures |
| 4 | **Throttling** from high request volume from a single customer | 699318024 | Similar API failure and RP throttling |
| 5 | **Untrusted certificate issues** affecting API requests through ARM | 698758328 | API error rates linked to cert problems |

### Diagnostic Kusto Queries (from incident enrichment)

The incident includes 3 pre-built Kusto queries for investigation. These should be run against `ARMProdEG` clusters:

1. **Top Failing Endpoints** — Shows which `targetUri` + `operationName` combos are failing most
   - Time window: `2026-03-22T22:09:02Z` to `2026-03-22T22:49:02Z`
   - Filter: `httpStatusCode >= 500 or == 0`, RP = `MICROSOFT.SECURITYINSIGHTS`

2. **Sample Correlation IDs** — Gets specific failing request correlation IDs for deep-dive
   - Filters on `prd-weu-402.sentinel.microsoft.com` hostname

3. **Impacted Subscriptions** — Counts distinct affected subscription IDs
   - Same hostname + RP filter

### FCM Change Data
An FCM enrichment was provided indicating change data may be available:
- **Link:** [Investigate changes in IcM](https://portal.microsofticm.com/imp/v5/incidents/details/766712513/troubleshooting?tab=changeData)
- This should be checked for any recent deployments or config changes that correlate with the error spike.

---

## 5. Remediation Actions

### Completed
- ✅ Incident acknowledged (within 3 minutes of creation)
- ✅ Enrichments and diagnostic links provided by ARM monitoring
- ✅ Assigned to Threat Intelligence team for triage

### Recommended Mitigations (from similar incidents)

| # | Mitigation | Reference ICM | Rationale |
|---|---|---|---|
| 1 | Stop or reduce erroneous customer requests causing 500 errors | 718932939 | Resolved by identifying and stopping bad customer calls |
| 2 | Fix application timeout spikes to restore API success rates | 757455363 | Timeout fix restored normal success rates |
| 3 | Apply targeted fixes to affected Sentinel infrastructure endpoints | 698715041 | Targeted fixes improved availability |
| 4 | Throttle or limit high request volumes from single customers | 699318024 | Stopped high request volume to mitigate |
| 5 | Restart functions, rotate certificates, clear caches | 698758328 | Certificate rotation + restarts resolved errors |

### Recommended Next Steps
1. **Run the Kusto queries** (Section 4) to determine actual subscription impact and failing operations
2. **Check FCM change data** for recent deployments to `prd-weu-402`
3. **Determine if this is self-resolving** — similar incident 757455363 (same WATCHLISTS endpoint) was caused by transient timeouts
4. **If errors persist**, investigate Sentinel backend health for the West Europe instance
5. **Update ICM** with findings after Kusto analysis

---

## 6. Similar Incidents

### High-Confidence Matches (from ICM similarity engine)

| ICM ID | Title | Severity | State | Resolution | Team |
|---|---|---|---|---|---|
| [598566983](https://portal.microsofticm.com/imp/v5/incidents/details/598566983) | THREATINTELLIGENCE errors on eus.rp.asi.azure.com | 2 | RESOLVED | **External** — Sev1 outage in T1 DC for Norway East; Cosmos returning 500. Resolved 2025-02-20. | Threat Intelligence |
| [599126313](https://portal.microsofticm.com/imp/v5/incidents/details/599126313) | THREATINTELLIGENCE errors on eus.rp.asi.azure.com | 2 | RESOLVED | **Other** — Errors stopped. Planned work to address incorrect error codes. Resolved 2025-02-25. | Threat Intelligence |

### Mitigated Similar Incidents (from mitigation hints)

| ICM ID | Title | Key Finding |
|---|---|---|
| [757455363](https://portal.microsofticm.com/imp/v5/incidents/details/757455363) | **WATCHLISTS errors on prd-weu-402** (same endpoint + resource type!) | Transient application timeouts |
| [718932939](https://portal.microsofticm.com/imp/v5/incidents/details/718932939) | THREATINTELLIGENCE errors on prd-weu-402 | Customer errors causing 500s |
| [699318024](https://portal.microsofticm.com/imp/v5/incidents/details/699318024) | ENRICHMENT errors on prd-eus-402 | Throttling from single customer |
| [698758328](https://portal.microsofticm.com/imp/v5/incidents/details/698758328) | THREATINTELLIGENCE errors on prd-weu-402 | Untrusted certificate issues |
| [698715041](https://portal.microsofticm.com/imp/v5/incidents/details/698715041) | THREATINTELLIGENCE errors on prd-weu-402 | Service issues on endpoint |

### Pattern Analysis

**Key pattern:** The `prd-weu-402.sentinel.microsoft.com` endpoint is a **repeat offender**. At least 4 of the 5 mitigated similar incidents involve this same West Europe Sentinel instance. The most relevant precedent is **ICM 757455363** — identical resource type (WATCHLISTS), identical endpoint, caused by transient timeouts that self-resolved.

The broader pattern across similar incidents suggests this endpoint has recurring reliability issues, potentially related to:
- Cosmos DB backend instability
- Customer-driven load spikes (single-customer throttling)
- Transient infrastructure issues in the West Europe region

---

## 7. Support Requests / CritSits

| Metric | Count |
|---|---|
| Support Requests (SRs) | **0** |
| SevA / CritSit | **0** |

No customer escalations have been filed for this incident. This aligns with the "not customer impacting" flag, though the absence of SRs does not guarantee zero customer impact — it may simply mean customers haven't reported the issue yet.

---

## 8. TSG References

### Directly Linked (from incident)
| TSG | URL |
|---|---|
| ARM RP Troubleshooting Overview | [eng.ms/docs/products/arm/troubleshooting/livesites/tsgs/rps/overview](https://eng.ms/docs/products/arm/troubleshooting/livesites/tsgs/rps/overview) |

### From eng.ms Search (relevant to this incident type)
| TSG | Service | URL |
|---|---|---|
| **Brain ARM RP Investigation TSG** — Comprehensive guide for investigating ARM RP SLI detections with Kusto queries for error counts, failure causes, and impacted resources | Brain | [eng.ms/.../arm-rp-investigation-tsg](https://eng.ms/docs/products/brain/brain-detection/arm-rp-investigation-tsg) |
| ARM Instance Error Rate — Outlier Detection | Azure Resource Manager | [eng.ms/.../instance_error_rate_outlier_detection](https://eng.ms/docs/products/arm/troubleshooting/livesites/tsgs/frontdoor/instance_error_rate_outlier_detection) |
| High 5xx Errors Frontend Troubleshooting | Hybrid Resource Provider | [eng.ms/.../high-5xx-errors-frontend](https://eng.ms/docs/cloud-ai-platform/azure-core/azure-cloud-native-and-management-platform/control-plane-bburns/hybrid-resource-provider/azure-arc-for-servers/managedops/troubleshooting/monitors/high-5xx-errors-frontend) |

### Key Investigation Steps (from Brain TSG)
The Brain ARM RP Investigation TSG provides the most actionable guidance:
1. Run **Error Count** queries against `ARMProdEG` clusters filtering on `MICROSOFT.SECURITYINSIGHTS`, `WATCHLISTS`, West Europe
2. Run **Failure Cause** queries to determine if failures are Gateway (ARM-side) vs Service (RP-side)
3. Check **Impacted Resources** via Brain telemetry in `brainintuse2kc` cluster
4. Note: HTTP 0s are timeouts from ARM→RP and are the RP's responsibility to investigate

### IcM Direct Links
- [Incident Details](https://portal.microsofticm.com/imp/v5/incidents/details/766712513)
- [Change Data Investigation](https://portal.microsofticm.com/imp/v5/incidents/details/766712513/troubleshooting?tab=changeData)

---

## 9. Open Questions

1. **🔴 Run Kusto queries** — The pre-built queries from the incident enrichment need to be executed to determine:
   - Which specific operations are failing (GET, PUT, DELETE on WATCHLISTS?)
   - How many subscriptions are actually affected
   - Whether failures are Gateway (ARM) or Service (Sentinel RP) side

2. **🟡 Check FCM change data** — Were there recent deployments or configuration changes to the `prd-weu-402` Sentinel instance that correlate with the error spike?

3. **🟡 Is this self-resolving?** — The most similar precedent (ICM 757455363) was transient. Monitor whether the hit count stabilizes or continues growing.

4. **🟡 Recurring endpoint issues** — `prd-weu-402.sentinel.microsoft.com` appears in multiple past incidents. Is there a systemic reliability issue with this instance that needs escalation to the Sentinel platform team?

5. **🟢 Customer impact verification** — Despite 0 formal impact, should we proactively check if any customer workloads depend on WATCHLISTS in West Europe?

---

*Report generated by Aragorn (Operator) for pa-squad. Data sourced from ICM tools and eng.ms.*
