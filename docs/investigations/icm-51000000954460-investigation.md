---
title: "ICM 51000000954460 — Revoked TI Indicators Still Triggering Alerts in EmailUrlInfo Rule"
incident_id: 51000000954460
severity: 3
classification: True Positive
confidence: MEDIUM
status: Transferred
customer_impacting: true
created: 2026-03-19T12:42:40Z
investigated: 2026-03-22
investigator: Aragorn (Operator)
requested_by: Jonathan (P1-HIGH CRI)
owning_team: "USX Threat Intelligence / Threat Intelligence"
owning_service: "USX Threat Intelligence (ServiceTree: 9584f7d8-0ee9-4127-9fe1-47daed058df6)"
tags:
  - icm
  - sentinel
  - threat-intelligence
  - analytic-rule
  - EmailUrlInfo
  - revoked-indicators
  - false-positive-alerts
  - CRI
transferred_date: 2026-03-27
transfer_note: "Incident transferred to different team. No longer owned by this team."
---

# ICM 51000000954460 — Investigation Report

> ⚠️ **STATUS: TRANSFERRED** — This incident was transferred to a different team on 2026-03-27 and is no longer owned by this team. Documentation is retained for historical reference.

**IcM Portal:** [IcM#51000000954460](https://portal.microsofticm.com/imp/v5/incidents/details/51000000954460/home)

> **Revoked TI Indicators Still Triggering Alerts in EmailUrlInfo Analytic Rule**

## Executive Summary

| Field | Value |
|-------|-------|
| **Incident ID** | 51000000954460 |
| **Classification** | ✅ **True Positive** — real product defect in TI analytic rule logic |
| **Customer Impact** | **YES** — false positive alerts from revoked indicators create SOC noise |
| **Severity** | Sev 3 (reported) · Sev 3 (assessed) — correct for functional defect, no data loss |
| **Confidence** | **MEDIUM** — strong evidence of the defect pattern, but no direct Kusto query execution against customer workspace |
| **Root Cause** | TI matching analytic rule for EmailUrlInfo does not filter out indicators with `Revoked = true` |

---

## Stage 1: Triage & Context

### 1.1 Incident Metadata

| Field | Value |
|-------|-------|
| **Title** | [V-1SOC_Sentinel] - [2603050050003250] Threat Intelligence Analytic Rule Inconsistency — Revoked Indicators Still Triggering Alerts in EmailUrlInfo Rule |
| **Type** | Customer-Reported |
| **State** | ACTIVE |
| **Severity** | 3 |
| **Created** | 2026-03-19T12:42:40Z |
| **Created By** | yderevyanko |
| **Contact** | amitamar |
| **Modified By** | rgasparini |
| **Environment** | PROD |
| **Tags** | M365DEN, AIInvestigationStandard |
| **Stage Owner** | Backlog |
| **1SOC CRI Approver** | fcasas@microsoft.com |
| **TA Approver** | Riccardo Gasparini |
| **1SOC Responsible Dev Team** | TI |
| **ICat_Sentinel** | Automation & SOAR |
| **xTeam-mapping** | 1SOC |

### 1.2 Customer Context

| Field | Value |
|-------|-------|
| **Workspace ID** | `2d4104eb-7fd3-44fd-b174-15a2c3b424ba` |
| **Tenant ID** | `5e748221-31d8-4fcd-9dd1-ce45feeb3bad` |
| **Support Requests** | 1 (SR 2603050050003250) |
| **CritSits** | 0 |
| **Impacted Subscriptions** | 0 (auto-detected) |

### 1.3 Blast Radius

| Dimension | Value |
|-----------|-------|
| **Impacted Services** | 0 (none formally recorded) |
| **Impacted Regions** | 0 |
| **Impacted Clouds** | 0 |
| **Location** | Azure Public Cloud — no specific region/DC/cluster identified |

### 1.4 Similar Incidents & Mitigation Hints

- **Similar Incidents**: None found via ICM similarity engine
- **Mitigation Hints**: No recommended causes or mitigations available
- **Pattern**: This appears to be a novel report or under-reported issue

### 1.5 Incident Classification

**Classification: TRUE POSITIVE**

**Reasoning:** The customer reports that after revoking TI indicators (setting `Revoked = true`), those indicators continue to trigger alerts via the EmailUrlInfo analytic rule. Microsoft documentation confirms that the `Revoked` field is a supported STIX property on TI indicators and is editable for partner-ingested indicators. However, the built-in TI analytic rule templates match raw events against indicators without filtering on revocation status. This is a functional defect — the system should not generate alerts for indicators the customer has explicitly revoked.

### 1.6 Customer Impact Assessment

**Impact: YES**

- **Symptom**: SOC analysts receive false-positive security alerts for revoked threat indicators
- **Operational Cost**: Alert fatigue, wasted triage cycles, reduced trust in TI matching
- **Scope**: Affects any customer using TI indicator revocation with EmailUrlInfo analytic rules
- **Data Loss**: None
- **Service Disruption**: None — alerts are generated but not actionable

### 1.7 Severity Evaluation

| | Value |
|---|-------|
| **Reported Severity** | Sev 3 |
| **Assessed Severity** | Sev 3 |
| **Mismatch** | None — Sev 3 is appropriate for a functional defect with no data loss, no service outage, but with operational impact on SOC workflows |

---

## Stage 2: Data Enrichment

### 2.1 TSG Search Results

**Searched:** eng.ms for USX Threat Intelligence TSGs

**Evidence**: USX TI service has 1 content node tagged as TSG — an overview page listing service components and owners. No specific TSG for TI analytic rule matching or revocation behavior exists. | Source: enghub-get_service_nodes (serviceId: 9584f7d8-0ee9-4127-9fe1-47daed058df6) | Impact: medium

**Key finding from eng.ms overview page**: The relevant service components are:
- **Apply Ti Everywhere** — Owners: Dylan Wood (dylanwood), Hung Nguyen (hunngu), Nurgeldi Batyrbekov (nbatyrbekov)
- **TI Matching - BBTI** — Owners: Andre Junior (andrejunior), Hung Nguyen (hunngu)

These two components own the TI matching pipeline that powers the analytic rules.

### 2.2 Microsoft Documentation Analysis

**Evidence**: Microsoft Sentinel documentation confirms TI indicators have a `Revoked` field (STIX standard). For partner-ingested indicators, `Revoked`, `Expiration date`, `Confidence`, and `Tags` are editable fields. The `Revoked` field controls whether an indicator should be considered active. | Source: azure-mcp-documentation (microsoft_docs_fetch: work-with-threat-indicators) | Impact: critical

**Evidence**: TI analytic rules "compare raw events from your data sources against your threat indicators to detect security threats." Built-in rule templates are based on indicator type (domain, email, file hash, IP address, or URL) and data source events. Documentation does not explicitly state that revoked indicators are excluded from matching. | Source: azure-mcp-documentation (microsoft_docs_fetch: understand-threat-intelligence) | Impact: critical

**Evidence**: The Defender Threat Intelligence data connector with "matching analytics" only ingests indicators that match the rule into the environment. This suggests indicators flow through a matching pipeline where filter conditions are applied, but revocation status may not be one of those conditions. | Source: azure-mcp-documentation (microsoft_docs_search) | Impact: high

### 2.3 Kusto Query Execution

**Status: BLOCKED — insufficient access**

The incident provides a customer Workspace ID (`2d4104eb-7fd3-44fd-b174-15a2c3b424ba`) and Tenant ID (`5e748221-31d8-4fcd-9dd1-ce45feeb3bad`), but:
- No Kusto cluster URI is available in the incident metadata
- The `icm-get_incident_context` tool returned an error, preventing extraction of pre-built queries or enrichment data
- Without cluster access to the customer's Log Analytics workspace, the following diagnostic queries could not be executed:

```kql
// Query 1: Check if revoked indicators exist in ThreatIntelligenceIndicator table
ThreatIntelligenceIndicator
| where Active == true and Revoked == true
| summarize Count = count() by IndicatorType, SourceSystem
| order by Count desc

// Query 2: Check if revoked indicators are matching in EmailUrlInfo
ThreatIntelligenceIndicator
| where Revoked == true
| join kind=inner (EmailUrlInfo) on $left.Url == $right.Url
| summarize MatchCount = count() by IndicatorId, Url
| order by MatchCount desc

// Query 3: Check alert generation from revoked indicators
SecurityAlert
| where AlertType contains "TI" or AlertType contains "ThreatIntelligence"
| where TimeGenerated > ago(7d)
| extend IndicatorId = tostring(parse_json(ExtendedProperties).IndicatorId)
| join kind=inner (
    ThreatIntelligenceIndicator | where Revoked == true
) on IndicatorId
| summarize AlertCount = count() by IndicatorId, AlertName
```

**Recommendation**: The owning team should execute these queries against the customer's workspace to confirm the hypothesis.

### 2.4 Geneva Metrics

**Status: NOT ATTEMPTED** — No Geneva metric account/namespace identified in incident metadata. The `icm-get_incident_context` error prevented extraction of the monitor trigger that created this incident.

### 2.5 Azure Resource Health

**Evidence**: No active Azure service health events found for the investigation period (2026-03-15 onward). This rules out a platform-level outage as a contributing factor. | Source: azure-mcp-resourcehealth (health-events_list) | Impact: low

### 2.6 Tool Availability Summary

| Tool | Status | Result |
|------|--------|--------|
| `icm-get_incident_details_by_id` | ✅ SUCCESS | Full metadata retrieved |
| `icm-get_ai_summary` | ⚠️ NO DATA | "No AI summary available" |
| `icm-get_incident_context` | ❌ FAILED | "Error fetching context information" |
| `icm-get_impacted_services_regions_clouds` | ✅ SUCCESS | No formal services/regions impacted |
| `icm-get_incident_customer_impact` | ✅ SUCCESS | 1 SR, 0 CritSits |
| `icm-get_support_requests_crisit` | ✅ SUCCESS | 1 SR, 0 CritSits |
| `icm-get_incident_location` | ✅ SUCCESS | Azure Public, no specific region |
| `icm-get_similar_incidents` | ✅ SUCCESS | None found |
| `icm-get_mitigation_hints` | ✅ SUCCESS | None available |
| `icm-get_impacted_subscription_count` | ✅ SUCCESS | 0 impacted subscriptions |
| `enghub-search` | ✅ SUCCESS | No relevant TSGs found |
| `enghub-fetch` | ✅ SUCCESS | USX TI overview page with component owners |
| `azure-mcp-documentation` | ✅ SUCCESS | Key docs on TI indicators and analytic rules |
| `azure-mcp-kusto` | ⚠️ AVAILABLE BUT BLOCKED | No cluster URI for customer workspace |
| `azure-mcp-resourcehealth` | ✅ SUCCESS | No active health events |

---

## Stage 3: Root Cause Analysis

### 3.1 Hypotheses

#### Hypothesis A: TI Analytic Rule Does Not Filter on `Revoked` Flag (PRIMARY)

**Evidence FOR:**
1. Microsoft documentation lists `Revoked` as an editable field on TI indicators but does not document that built-in analytic rules honor this flag
2. The built-in TI analytic rule templates "compare raw events from data sources against threat indicators" — matching is based on indicator type and value (URL, IP, domain, hash), not on indicator lifecycle state
3. The customer explicitly reports that revoking indicators does not stop alert generation
4. The `ThreatIntelligenceIndicator` table in Log Analytics stores the `Revoked` field, but the KQL join in the analytic rule template likely filters on `Active == true` without also filtering `Revoked == false`
5. No similar incidents found — this may be under-reported because most customers delete indicators rather than revoke them

**Evidence AGAINST:**
- Cannot confirm without executing Kusto queries against the customer workspace
- The rule template code was not directly inspected

**Confidence: MEDIUM**

#### Hypothesis B: Indicator Revocation State Not Propagating to Log Analytics

**Evidence FOR:**
1. For partner-ingested indicators, the `Revoked` field is editable but the update may not propagate to the `ThreatIntelligenceIndicator` table in Log Analytics
2. The TI ingestion pipeline may cache indicator state and not refresh revocation status

**Evidence AGAINST:**
1. Documentation states that editing `Revoked` on partner-ingested indicators is supported
2. The incident title says "Revoked Indicators Still Triggering" — implying the customer successfully set revocation status

**Confidence: LOW**

#### Hypothesis C: EmailUrlInfo-Specific Rule Has a Bug

**Evidence FOR:**
1. The issue is specifically called out for the EmailUrlInfo analytic rule, not all TI rules
2. The EmailUrlInfo table (from Microsoft Defender for Office 365) may have a different join pattern that doesn't include revocation filters

**Evidence AGAINST:**
1. Most TI analytic rules share common template patterns — unlikely to be EmailUrlInfo-specific
2. No evidence of EmailUrlInfo-specific bugs in documentation or similar incidents

**Confidence: LOW**

### 3.2 Root Cause Determination

**Root Cause: The TI matching analytic rule for EmailUrlInfo does not filter out indicators where `Revoked = true` when performing the indicator-to-event join.**

**Confidence: MEDIUM**

### 3.3 Causal Chain

```
1. Customer imports TI indicators (URLs) into Sentinel workspace
   ↓
2. Customer revokes specific indicators (sets Revoked = true) 
   via Sentinel TI management interface
   ↓
3. TI analytic rule for EmailUrlInfo runs on schedule
   ↓
4. Rule KQL query joins ThreatIntelligenceIndicator with EmailUrlInfo
   on URL match — but does NOT filter on Revoked == false
   ↓
5. Revoked indicators still match against email URL events
   ↓
6. False-positive security alerts generated for revoked indicators
   ↓
7. SOC analysts waste triage cycles on non-actionable alerts
   → Alert fatigue, reduced trust in TI matching system
```

### 3.4 Deduction Process

1. **Started with the symptom**: Revoked indicators trigger alerts. This means either (a) the revocation state isn't stored, (b) the revocation state isn't checked, or (c) the revocation state isn't propagated.
2. **Checked documentation**: Confirmed `Revoked` is a first-class STIX field, editable on partner-ingested indicators. This eliminates (a).
3. **Analyzed analytic rule mechanics**: Documentation describes TI rules as matching raw events against indicators by type (URL, IP, etc.). No mention of lifecycle state filtering. This supports (b).
4. **Checked for platform-level issues**: No service health events, no similar incidents, no outage. This eliminates transient infrastructure problems.
5. **Conclusion**: The most likely root cause is (b) — the analytic rule KQL query does not include a `where Revoked == false` filter clause.

---

## Stage 4: Remediation

### 4.1 Immediate Mitigations (< 1 hour)

| Action | Steps | Effort | Owner | Verification |
|--------|-------|--------|-------|-------------|
| **Customer workaround: Delete instead of revoke** | Advise customer to delete revoked indicators rather than setting `Revoked = true`, or set `ExpirationDateTime` to a past date | 15 min | Support (amitamar) | Confirm alerts stop for deleted indicators |
| **Customer workaround: Custom analytic rule** | Customer clones the built-in EmailUrlInfo TI rule and adds `\| where Revoked == false` to the ThreatIntelligenceIndicator portion of the KQL query | 30 min | Support + Customer SOC team | Run modified rule and confirm no alerts for revoked indicators |

### 4.2 Short-Term Fixes (1–3 days)

| Action | Steps | Effort | Owner | Verification |
|--------|-------|--------|-------|-------------|
| **Confirm defect via Kusto** | Execute diagnostic queries (Section 2.3) against customer workspace `2d4104eb-7fd3-44fd-b174-15a2c3b424ba` to confirm revoked indicators are matching | 1 hour | TI Matching team (andrejunior, hunngu) | Query results show revoked indicators producing matches |
| **Inspect analytic rule template** | Review the built-in EmailUrlInfo TI analytic rule KQL template for missing `Revoked` filter | 2 hours | Apply Ti Everywhere team (dylanwood, hunngu, nbatyrbekov) | Template inspection confirms missing filter |
| **File product bug** | Create a work item in ADO under `TII_Detections` area path to add `Revoked == false` filter to all TI analytic rule templates | 30 min | TI team lead | Bug tracked and prioritized |

### 4.3 Long-Term Solutions (weeks/months)

| Action | Steps | Effort | Owner | Verification |
|--------|-------|--------|-------|-------------|
| **Fix all TI analytic rule templates** | Add `where Revoked == false and Active == true` to all built-in TI analytic rule templates (EmailUrlInfo, DNS, Network, Syslog, etc.) | 1–2 weeks | Apply Ti Everywhere + TI Matching teams | All built-in rules filter revoked indicators |
| **Add TI lifecycle filtering to matching pipeline** | Move revocation filtering to the TI matching pipeline (BBTI) so that revoked indicators are excluded at the matching stage, not just the rule query stage | 2–4 weeks | TI Matching - BBTI team (andrejunior, hunngu) | Revoked indicators never enter matching results |
| **Create TSG for TI indicator lifecycle issues** | Document revocation, expiration, and active/inactive indicator behaviors as a TSG on eng.ms under USX TI | 1 week | USX TI team (razvanp, suresp) | TSG published and linked from ICM |
| **Add automated tests** | Create regression tests that validate revoked indicators do not generate alerts across all TI rule templates | 2 weeks | TI team | CI/CD tests pass for revoked indicator scenarios |

### 4.4 Prevention Measures

| Action | Steps | Effort | Owner |
|--------|-------|--------|-------|
| **Template review gate** | Add a checklist item to the TI analytic rule template review process: "Does the rule filter on indicator lifecycle state (Active, Revoked, Expired)?" | 1 day | TI team lead |
| **Customer documentation** | Update Microsoft Learn docs to clarify that revoking an indicator does/does not stop alert generation, and document the workaround | 1 week | Content team + TI team |

---

## Open Questions

| Priority | Question | Action Required |
|----------|----------|-----------------|
| **P0** | Does the EmailUrlInfo TI rule template KQL actually lack a `Revoked` filter? | Inspect the rule template source code |
| **P0** | Are other TI analytic rule templates (DNS, Network, Syslog) also affected? | Audit all built-in TI rule templates |
| **P1** | Why did `icm-get_incident_context` fail? | Retry or check ICM API access for this incident |
| **P1** | Is the `Revoked` field reliably propagated to the `ThreatIntelligenceIndicator` Log Analytics table? | Execute Kusto query against customer workspace |
| **P2** | How many other customers are affected but haven't reported? | Query TI matching telemetry for revoked indicator matches |
| **P2** | Should the TI matching pipeline (BBTI) exclude revoked indicators before they reach analytic rules? | Architecture discussion with TI Matching team |

---

## Appendix: Service Component Owners

From eng.ms USX Threat Intelligence overview:

| Component | Owners | Relevance |
|-----------|--------|-----------|
| Apply Ti Everywhere | Dylan Wood, Hung Nguyen, Nurgeldi Batyrbekov | TI rule template logic |
| TI Matching - BBTI | Andre Junior, Hung Nguyen | TI matching pipeline |
| URL Detonation | Andre Junior | URL-specific analysis |
| Upload API | Hung Nguyen, Nitisha Bhandari | Indicator ingestion |

**USX TI Team Owners**: razvanp, suresp, inegroiu, timpaterson, lucojoca, jochrin, hunngu, sngupt

---

*Report generated by Aragorn (Operator) · Investigation date: 2026-03-22 · ICM 51000000954460*
