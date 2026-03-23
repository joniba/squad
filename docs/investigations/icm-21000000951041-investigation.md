---
title: "ICM 21000000951041 — Azure Government Sentinel TI TAXII Ingestion Shortfall"
incident_id: 21000000951041
severity: 3
classification: True Positive
confidence: MEDIUM
cloud: Azure Government (Federal)
service: USX Threat Intelligence
owning_team: Threat Intelligence
status: ACTIVE
customer_impacting: true
created: 2026-03-17
investigated: 2026-06-20
investigator: Aragorn (Operator)
requested_by: Jonathan (P1-HIGH CRI)
tags:
  - icm
  - sentinel
  - taxii
  - threat-intelligence
  - azure-government
  - ingestion
  - data-loss
---

# ICM 21000000951041 — Azure Government Sentinel TI TAXII Ingestion Shortfall

**IcM Portal:** [IcM#21000000951041](https://portal.microsofticm.com/imp/v5/incidents/details/21000000951041/home)

> **Incident:** `[Azure Government] Sentinel TI TAXII Ingestion count lower than whats being sent from threat connect`
> **Classification:** True Positive | **Confidence:** MEDIUM | **Severity:** 3 (reported) → 3 (assessed, appropriate)

---

## 1. Executive Summary

A customer on Azure Government reports that the number of Threat Intelligence (TI) indicators ingested into Microsoft Sentinel via the TAXII connector is **lower than the count being sent from ThreatConnect**. The USX Threat Intelligence team's own TSG documents a **known platform limitation**: the Sentinel TAXII connector code **only supports plain-text responses and does not handle GZIP or other content encoding**. If ThreatConnect's TAXII server responds with GZIP-encoded content (common for large indicator batches), the connector silently fails to parse those responses, resulting in indicator loss. A secondary factor involves CDN vs. direct/VPN access paths to the TAXII server, which can alter response encoding behavior.

---

## 2. Incident Metadata

| Field | Value |
|-------|-------|
| **ICM ID** | 21000000951041 |
| **Title** | [Azure Government] Sentinel TI TAXII Ingestion count lower than whats being sent from threat connect |
| **Severity** | 3 |
| **State** | ACTIVE |
| **Type** | CustomerReported |
| **Created** | 2026-03-17T00:49:26Z |
| **Created By** | nikkiwilson |
| **Modified By** | rgasparini |
| **TA Approver** | Riccardo Gasparini |
| **Owning Team** | Threat Intelligence (USX Threat Intelligence) |
| **Cloud** | Federal (Azure Government) |
| **Environment** | PROD |
| **Support Requests** | 1 |
| **CritSit Count** | 0 |
| **Tags** | M365DEN, AIInvestigationStandard |
| **How Fixed** | External |
| **Is Support Engagement** | Yes |
| **Is Customer Impacting (ICM)** | No (see assessment below) |

---

## 3. Incident Classification

### Classification: **True Positive**

**Reasoning:** The customer reports a genuine discrepancy between indicators sent by ThreatConnect and indicators ingested by Sentinel. The USX Threat Intelligence team maintains a TSG (`TAXIIConnector_GzipIssue`) documenting this exact failure mode as a **known platform limitation**. This is not a false alarm — it is a real data ingestion shortfall caused by an encoding incompatibility in the TAXII connector.

### Customer Impact Assessment: **YES**

Despite the ICM `isCustomerImpacting` flag being set to `false`, this incident **does** impact the customer:
- **Symptom:** Fewer threat intelligence indicators appear in Sentinel than what ThreatConnect sends
- **Impact:** Reduced threat detection coverage — missing indicators mean potential threats go undetected
- **Scope:** Azure Government customer(s) using ThreatConnect → Sentinel TAXII integration
- **Data Table Affected:** `ThreatIntelligenceIndicator` in Log Analytics

### Severity Evaluation: **Appropriate (Sev 3)**

Severity 3 is appropriate for a data ingestion shortfall that degrades security coverage but does not cause an outage. However, for a government customer relying on TI feeds for threat detection, the security implications elevate the urgency beyond typical Sev 3.

---

## 4. Evidence Collected

### Evidence 1: Known GZIP Encoding Limitation (TSG)
**Evidence:** USX TI team TSG (`TAXIIConnector_GzipIssue`) states: *"Our current code only supports plain text and does not handle GZIP or any other encoding. If an external TAXII server only supports GZIP, it will inevitably lead to ingestion failures."* | Source: `enghub-fetch` (eng.ms TSG) | Impact: **critical**

### Evidence 2: CDN Access Path Affects Behavior
**Evidence:** Same TSG warns: *"It's important to confirm whether the TAXII server is accessed via a Content Delivery Network (CDN). When customers try to reproduce the issue locally, they need to ensure that they are accessing the TAXII server through the CDN, rather than using internal VPN connections, for accurate results."* | Source: `enghub-fetch` (eng.ms TSG) | Impact: **high**

### Evidence 3: Incident Location — Azure Government (Federal)
**Evidence:** Incident location classified as `ms-loc://az/federal` — Azure Government cloud. Government environments may have additional network constraints (firewalls, proxies) that influence TAXII server response encoding. | Source: `icm-get_incident_location` | Impact: **medium**

### Evidence 4: Single Support Request Linked
**Evidence:** 1 support request is associated with this CRI. 0 formally impacted subscriptions detected via auto-detection. | Source: `icm-get_support_requests_crisit`, `icm-get_impacted_subscription_count` | Impact: **medium**

### Evidence 5: No Similar Incidents or Mitigation History
**Evidence:** No similar incidents found and no recommended mitigations available in IcM. | Source: `icm-get_similar_incidents`, `icm-get_mitigation_hints` | Impact: **low** (absence of data is itself a signal — this may be under-reported)

### Evidence 6: TAXII Connector Architecture (Microsoft Docs)
**Evidence:** Microsoft docs confirm the Sentinel TAXII connector uses a built-in TAXII client polling TAXII 2.0/2.1 servers. Data lands in `ThreatIntelligenceIndicator` table. Azure Government availability noted with reference to feature availability docs. IP allowlisting may be required for some TAXII servers. | Source: `azure-mcp-documentation` (learn.microsoft.com) | Impact: **medium**

---

## 5. Data Enrichment Results

### 5a. TSG Content (Read and Applied)

**TSG:** [CRI - issue with TAXII connector (TAXIIConnector_GzipIssue)](https://eng.ms/docs/microsoft-security/microsoft-threat-protection-mtp/onesoc-1soc/siem/usx-threat-intelligence/overview/troubleshooting/tsgs/taxiiconnector_gzipissue)

**Key Findings from TSG:**
1. The TAXII connector **only supports plain text** response encoding
2. GZIP-encoded responses from TAXII servers cause **silent ingestion failures**
3. CDN routing vs. VPN/direct routing changes encoding behavior
4. When reproducing, customers must access via CDN (not internal VPN) for accurate results

**TSG Overview Page:** [USX Threat Intelligence TSGs Overview](https://eng.ms/docs/microsoft-security/microsoft-threat-protection-mtp/onesoc-1soc/siem/usx-threat-intelligence/overview/troubleshooting/tsgs/overview) — confirms two documented TSGs: CosmosDB Publisher throughput and TAXII Connector Gzip. This incident matches the latter.

### 5b. Kusto Queries

**Status: NOT EXECUTED** — `icm-get_incident_context` returned an error, so no pre-built Kusto queries were available from the incident enrichment data. Without workspace ID or cluster details, direct Kusto execution was not possible.

### 5c. Geneva Metrics

**Status: NOT EXECUTED** — No monitor trigger data was available (incident is `CustomerReported`, not monitor-triggered). No Geneva account/namespace identifiers to query against.

### 5d. Azure Diagnostics

**Status: NOT EXECUTED** — No specific resource IDs (workspace, subscription) were provided in the incident metadata. The `WS_ID` custom field contains only `-`.

---

## 6. Root Cause Analysis

### Hypothesis 1: GZIP Encoding Incompatibility (PRIMARY)

| Aspect | Detail |
|--------|--------|
| **Hypothesis** | ThreatConnect's TAXII server responds with GZIP-encoded content. The Sentinel TAXII connector cannot parse GZIP, causing silent data loss. |
| **Evidence For** | TSG explicitly documents this as a known limitation. ThreatConnect is a large TI platform likely using GZIP for performance. Government environments may enforce compression policies. |
| **Evidence Against** | No direct confirmation from logs that GZIP is being used in this specific case (context data unavailable). |
| **Confidence** | **MEDIUM** |

### Hypothesis 2: CDN/Network Path Routing Issue

| Aspect | Detail |
|--------|--------|
| **Hypothesis** | Azure Government network topology routes TAXII requests differently (e.g., through a proxy or gateway that modifies `Accept-Encoding` headers), causing the TAXII server to respond with GZIP even if it supports plain text. |
| **Evidence For** | TSG warns about CDN vs. VPN access paths affecting behavior. Government clouds have additional network restrictions. |
| **Evidence Against** | No direct network trace data. This is a secondary factor more likely than a standalone root cause. |
| **Confidence** | **LOW** |

### Hypothesis 3: TAXII Server Pagination / Rate Limiting

| Aspect | Detail |
|--------|--------|
| **Hypothesis** | ThreatConnect's TAXII server paginates large collections, and the Sentinel connector drops indicators during pagination (e.g., timeout, partial page reads). |
| **Evidence For** | Large TI feeds commonly paginate. Count discrepancy could indicate missed pages. |
| **Evidence Against** | TSG does not mention pagination as a known issue. The GZIP issue is documented as the primary failure mode for ingestion shortfalls. |
| **Confidence** | **LOW** |

### Causal Chain (Primary Hypothesis)

```
ThreatConnect TAXII Server responds with GZIP-encoded content
    → Sentinel TAXII connector receives GZIP payload
    → Connector code expects plain text, cannot decompress GZIP
    → Response parsing fails silently (no error surfaced to customer)
    → Affected indicators are dropped / not ingested
    → ThreatIntelligenceIndicator table in Sentinel has fewer records
    → Customer observes count discrepancy between ThreatConnect sent count and Sentinel ingested count
    → Reduced threat detection coverage in Azure Government Sentinel workspace
```

### Root Cause Determination

**Primary Root Cause:** The Sentinel TAXII connector's lack of GZIP content encoding support causes silent ingestion failures when the upstream ThreatConnect TAXII server responds with compressed payloads.

**Confidence Level:** **MEDIUM** — The TSG documents this exact failure mode, and the symptoms match perfectly. Confidence is not HIGH because we could not access incident context/logs to confirm GZIP is specifically involved in this instance.

---

## 7. Remediation

### Immediate Mitigations (< 1 hour)

| # | Action | Steps | Effort | Owner | Verification |
|---|--------|-------|--------|-------|-------------|
| 1 | **Configure ThreatConnect to send plain text** | Work with customer to configure ThreatConnect TAXII server to respond with `Content-Type: application/json` without GZIP. Check ThreatConnect server settings for `Accept-Encoding` negotiation. | 30 min | Customer + Support Engineer | Re-poll TAXII connector and compare indicator counts |
| 2 | **Verify CDN access path** | Confirm the Sentinel TAXII connector is accessing ThreatConnect via CDN (not direct/VPN). Check if Azure Government network policies alter the request path. | 30 min | Support Engineer | Compare response headers from CDN vs. direct access |

### Short-Term Fixes (1–3 days)

| # | Action | Steps | Effort | Owner | Verification |
|---|--------|-------|--------|-------|-------------|
| 3 | **Query ThreatIntelligenceIndicator table** | Run KQL query on customer workspace: `ThreatIntelligenceIndicator \| where TimeGenerated > ago(7d) \| summarize count() by SourceSystem, bin(TimeGenerated, 1h)` to establish baseline ingestion rate and identify exact drop-off windows. | 1 hour | Support Engineer | Identify specific time windows with zero/low ingestion |
| 4 | **Enable TAXII connector diagnostics** | Enable diagnostic logging on the TAXII connector to capture HTTP response headers and error details during polling. | 2 hours | Threat Intelligence team | Diagnostic logs show `Content-Encoding: gzip` or parsing errors |

### Long-Term Solutions (Weeks/Months)

| # | Action | Steps | Effort | Owner | Verification |
|---|--------|-------|--------|-------|-------------|
| 5 | **Add GZIP support to TAXII connector** | Engineering fix: Update the Sentinel TAXII connector code to handle GZIP (and other common encodings like deflate, br). This is the definitive fix. | 2–4 weeks | USX Threat Intelligence Engineering | Automated tests with GZIP-encoded TAXII responses pass; indicator counts match |
| 6 | **Add ingestion count validation** | Implement a monitoring check that compares "indicators received from TAXII server" vs. "indicators written to ThreatIntelligenceIndicator table" and alerts on discrepancy. | 1–2 weeks | USX Threat Intelligence Engineering | Alert fires when >5% of indicators are dropped |
| 7 | **Update TSG with diagnostic KQL** | Add specific Kusto queries to the `TAXIIConnector_GzipIssue` TSG for support engineers to run during triage. | 2 hours | TSG Owners (razvanp, suresp, inegroiu) | TSG contains runnable diagnostic queries |

---

## 8. Open Questions

| Priority | Question | Why It Matters |
|----------|----------|----------------|
| **P0** | What specific `Content-Encoding` does ThreatConnect's TAXII server return for this customer? | Confirms/denies GZIP hypothesis definitively |
| **P0** | What is the customer's workspace ID and subscription ID? | Required for Kusto queries and Azure diagnostics — `WS_ID` field is empty |
| **P1** | Are there TAXII connector diagnostic logs available for the affected workspace? | Would show HTTP response details and parsing errors |
| **P1** | Is this the same issue as previous CRIs referenced in the TSG title? | Pattern detection — may be a repeat of a known unresolved limitation |
| **P2** | Is GZIP support on the TAXII connector engineering backlog? | Determines whether the long-term fix is already planned |
| **P2** | How many Azure Government customers use ThreatConnect + Sentinel TAXII integration? | Scope of potential impact beyond this single customer |

---

## 9. Tool Execution Summary

| Tool | Status | Result |
|------|--------|--------|
| `icm-get_incident_details_by_id` | ✅ Success | Full metadata retrieved |
| `icm-get_ai_summary` | ⚠️ No data | "No AI summary available" |
| `icm-get_incident_context` | ❌ Failed | "Error fetching context information" |
| `icm-get_mitigation_hints` | ✅ Success | No recommendations available |
| `icm-get_similar_incidents` | ✅ Success | No similar incidents found |
| `icm-get_incident_customer_impact` | ✅ Success | 1 SR, 0 formal impact |
| `icm-get_incident_location` | ✅ Success | Federal cloud |
| `icm-get_support_requests_crisit` | ✅ Success | 1 SR, 0 CritSit |
| `icm-get_impacted_services_regions_clouds` | ✅ Success | No formal services impacted |
| `icm-get_impacted_subscription_count` | ✅ Success | 0 subscriptions |
| `enghub-search` | ✅ Success | Found 2 relevant TSGs |
| `enghub-fetch` (TSG Overview) | ✅ Success | Read full content |
| `enghub-fetch` (GzipIssue TSG) | ✅ Success | **Key evidence extracted** |
| `azure-mcp-documentation` (3 queries) | ✅ Success | TAXII connector docs retrieved |
| `azure-mcp-kusto` | ⏭️ Skipped | No workspace/cluster details available |
| `geneva-mcp-server` | ⏭️ Skipped | No monitor trigger (customer-reported) |
| `azure-mcp-applens` | ⏭️ Skipped | No resource IDs available |

---

## 10. References

- **TSG:** [CRI - issue with TAXII connector](https://eng.ms/docs/microsoft-security/microsoft-threat-protection-mtp/onesoc-1soc/siem/usx-threat-intelligence/overview/troubleshooting/tsgs/taxiiconnector_gzipissue)
- **TSG Overview:** [USX TI Troubleshooting Guides](https://eng.ms/docs/microsoft-security/microsoft-threat-protection-mtp/onesoc-1soc/siem/usx-threat-intelligence/overview/troubleshooting/tsgs/overview)
- **Azure Docs:** [Use STIX/TAXII to import and export threat intelligence in Microsoft Sentinel](https://learn.microsoft.com/azure/sentinel/connect-threat-intelligence-taxii)
- **Azure Docs:** [Threat intelligence in Microsoft Sentinel](https://learn.microsoft.com/azure/sentinel/understand-threat-intelligence)
- **Feature Availability:** [Cloud feature availability for US Government customers](https://learn.microsoft.com/azure/security/fundamentals/feature-availability)

---

*Investigation performed by Aragorn (Operator) on 2026-06-20. Report generated from ICM data, eng.ms TSGs, and Microsoft Learn documentation.*
