---
title: "ICM 766937015 — MDTI Premium Sentinel Connector Enablement Delays"
incident_id: 766937015
severity_reported: 4
severity_assessed: 3
classification: True Positive
confidence: HIGH
customer_impact: YES
state: ACTIVE
owning_team: Threat Intelligence (USX Threat Intelligence)
created: 2026-03-23T09:33:37Z
investigated_by: Aragorn (Operator)
requested_by: Jonathan (HIGH PRIORITY — CRI, private channels)
tags:
  - CRI
  - MDTI
  - Premium-Connector
  - Sentinel
  - SKU-Allowlist
  - Enablement-Blocked
related_incidents:
  - 692529114
  - 740177161
---

# ICM 766937015 — MDTI Premium Sentinel Connector Enablement Delays

**IcM Portal:** [IcM#766937015](https://portal.microsofticm.com/imp/v5/incidents/details/766937015/home)

## Executive Summary

**Classification:** TRUE POSITIVE — real customer-blocking issue requiring manual intervention.

**Customer Impact:** YES — Two named customers blocked:
1. **Post Holdings** — contract renewal blocked pending connector enablement
2. **Metropolitan Police Service** — SOC capability enhancement and security investment blocked

**Root Cause (HIGH confidence):** Customer tenant IDs are missing from the PMDTI connector allowlist in `SkuVerifier.cs`. The MDTI API Access SKU is being deprecated, and the team maintains a static hardcoded tenant list. New customers cannot create PMDTI connectors without a code change + production deployment to add their tenant ID.

**Severity Mismatch:** Reported as Sev 4; assessed as **Sev 3** given contract-blocking impact on two named customers and exact match to precedent incident 692529114 (which was Sev 3).

---

## Stage 1: Triage & Context

### Incident Metadata

| Field | Value |
|-------|-------|
| **ID** | 766937015 |
| **Title** | Enable MDTI Premium Sentinel connector |
| **Severity** | 4 (reported) → **3 (assessed)** |
| **State** | ACTIVE |
| **Type** | LiveSite — CRI (IsCri: true) |
| **Owning Team** | Threat Intelligence (USX Threat Intelligence) |
| **Created** | 2026-03-23T09:33:37Z |
| **Created By** | prtanej |
| **TA Approver** | Riccardo Gasparini |
| **WS_ID** | PHI-AzureSentinel |
| **Environment** | PROD (Azure Public) |
| **Is Customer Impacting** | false (ICM metadata — **understated**, see analysis) |
| **Alert Source** | ICMPortal (manually filed) |

### Blast Radius

| Dimension | Value |
|-----------|-------|
| Impacted Services | 0 (not an outage — provisioning/config issue) |
| Impacted Regions | 0 (global service) |
| Impacted Clouds | 0 |
| Impacted Subscriptions | 0 (formal count) |
| Support Requests | 0 |
| CritSits | 0 |

**Note:** Formal impact metrics show zero because this is a provisioning request, not an active outage. Actual customer impact exists — see named customers above.

### Similar Incidents

| Incident | Title | State | Resolution |
|----------|-------|-------|------------|
| **692529114** | Enable the Premium MDTI feed via the Sentinel connector | RESOLVED | Added tenant ID to include-list + hotfix deployment. Mitigated by **jbenami** on 2025-10-29. |
| **740177161** | Premium License into Tenant | MITIGATED (Won't Fix) | Sales demo access request. Marked as noise. |

### Mitigation Hints (from ICM)

| Recommended Cause | Confidence | Reference |
|-------------------|------------|-----------|
| Tenant ID missing in Premium MDTI feed configuration | HIGH | ICM 692529114 |
| Integration issues with Premium license affecting access | MEDIUM | ICM 740177161 |

**Recommended Mitigation:** Add tenant ID to include-list and deploy hotfix (proven effective on ICM 692529114).

---

## Stage 2: Data Enrichment

### 2a. TSG Search (BLOCKED)

| Tool | Result |
|------|--------|
| `enghub-search` (query 1) | ❌ **Access denied** — "You may not have permission to access this resource" |
| `enghub-search` (query 2) | ❌ **Request timed out** |

**Impact:** Unable to retrieve TSG content from eng.ms. Investigation proceeds with code analysis and historical incident data.

### 2b. Kusto Queries

No Kusto queries available in this incident. The issue is a provisioning/configuration problem, not a metric anomaly. No monitor trigger — incident was manually filed via ICM Portal.

### 2c. Source Code Research

**Critical findings in `Sentinel-TiPipeline` repo:**

#### Evidence 1: SKU Verification Gate

**Evidence**: `PremiumMdtiValidator.ValidatePremiumMdtiAsync()` blocks connector creation when required SKUs are absent from tenant | Source: `Sentinel-TiPipeline/src/.../PremiumMdtiValidator.cs:32-48` | Impact: critical

The validator calls `tenantSkusVerifier.CheckSkusPresentAsync()` and throws `PaymentRequiredException` with message:
> "The connector does not fulfill the requirements to perform this action. Please make a payment for the MDTI API Access SKU to create this connector"

#### Evidence 2: Three-Tier Allowlist Bypass in SkuVerifier

**Evidence**: `SkuVerifier.CheckSkusPresentAsync()` has three bypass paths before hitting the SKU check, all based on static tenant ID lists | Source: `Sentinel-TiPipeline/src/.../SkuVerifier.cs:57-250+` | Impact: critical

The bypass hierarchy:
1. **Tenant+Workspace combo** (line 68): Checks `AllowedTenantIdWorkspaceIdCombinationForPremiumSku` — populated from `premiumSkuAllowListWorkspaces` app setting (environment variable).
2. **Hardcoded MOD tenant** (line 77): `8821753e-79eb-45f0-bb19-941abaa267b2` — always bypassed until correct SKU procured.
3. **Hardcoded Microsoft Corp tenant** (line 86): `72f988bf-86f1-41af-91ab-2d7cd011db47` — bypassed only when `HasSkusHardcoded` app setting is true.
4. **Static test/demo/paying-customer list** (line 92–250+): ~200+ hardcoded tenant GUIDs including test tenants and all paying customers as of July 2025.

#### Evidence 3: SKU Deprecation Comment

**Evidence**: Code comments state "There is a plan to deprecate the MDTI API sku" and the static list is "tenantIds who are active PMDTI sku paying customers as of July 2025" | Source: `Sentinel-TiPipeline/src/.../SkuVerifier.cs:113-115` | Impact: high

This confirms the systemic issue: the SKU validation system is being deprecated, but no automated replacement exists. Every new customer requires a manual code change to the allowlist.

#### Evidence 4: App Setting Configuration

**Evidence**: `PremiumSkuAllowListWorkspacesKey = "premiumSkuAllowListWorkspaces"` reads from environment variable, providing a deployment-time allowlist alternative | Source: `Sentinel-TiPipeline/src/.../ThreatIntelligenceConnectorServiceConfig.cs:62` | Impact: medium

This app setting accepts tenant+workspace tuples and is the preferred method for adding individual customers without a full code deployment.

---

## Stage 3: Root Cause Analysis

### Hypothesis 1: Customer Tenant IDs Not in PMDTI Allowlist (PRIMARY — HIGH confidence)

**Evidence FOR:**
- Exact match to precedent incident **692529114** — same symptom, same service, same resolution (tenant ID added to include-list)
- `SkuVerifier.cs` requires tenant IDs to be pre-registered in either the app setting or the hardcoded list
- No Kusto queries or metric anomalies — this is a provisioning gap, not a service failure
- Mitigation hints from ICM explicitly recommend "Add tenant ID to include-list and deploy hotfix"
- The MDTI API SKU is being deprecated, meaning new customers cannot procure the SKU through normal channels

**Evidence AGAINST:**
- None. All data points align with this hypothesis.

### Hypothesis 2: License/SKU Procurement Issue (SECONDARY — LOW confidence)

**Evidence FOR:**
- ICM 740177161 references "Premium license integration" issues
- The validator explicitly checks for MDTI API Access SKU

**Evidence AGAINST:**
- Code comments say the SKU is being deprecated — customers cannot/should not be asked to procure it
- The fix for 692529114 was adding to the allowlist, not procuring a SKU
- The static list was created specifically to bypass the SKU check for paying customers

### Causal Chain

```
Trigger: Customer requests MDTI Premium Sentinel connector enablement
    → Customer tenant ID not found in SkuVerifier allowlist (hardcoded list or app setting)
    → PremiumMdtiValidator.ValidatePremiumMdtiAsync() calls CheckSkusPresentAsync()
    → CheckSkusPresentAsync() checks all three bypass paths — none match
    → Customer likely doesn't have MDTI API Access SKU (being deprecated)
    → PaymentRequiredException thrown: "Please make a payment for the MDTI API Access SKU"
    → Connector creation blocked
    → CRI filed: contract renewal blocked (Post Holdings), SOC investment blocked (Metropolitan Police)
```

**Root Cause:** The PMDTI connector enablement process requires tenant IDs to be pre-registered in a hardcoded allowlist (`SkuVerifier.cs`) or app setting (`premiumSkuAllowListWorkspaces`). The customers' tenant IDs are not present in either location. The MDTI API Access SKU is being deprecated, so the normal procurement path is unavailable.

**Confidence:** HIGH — exact precedent exists (692529114), code analysis confirms mechanism, mitigation hints corroborate.

---

## Stage 4: Remediation

### Immediate Mitigation (< 1 hour)

| # | Action | Steps | Effort | Owner | Verification |
|---|--------|-------|--------|-------|-------------|
| 1 | **Add tenant IDs to `premiumSkuAllowListWorkspaces` app setting** | Get tenant IDs for Post Holdings and Metropolitan Police Service from the CRI contact (prtanej). Update the `premiumSkuAllowListWorkspaces` app setting in the Connector Service Function App to include the new tenant+workspace tuples. | 30 min | Threat Intelligence on-call | Customer can successfully create PMDTI connector via Sentinel UI |

### Short-Term Fix (1–3 days)

| # | Action | Steps | Effort | Owner | Verification |
|---|--------|-------|--------|-------|-------------|
| 2 | **Add tenant IDs to hardcoded allowlist** | Create PR to add both customer tenant IDs to `SkuVerifier.cs` static allowlist (below line ~110). Deploy to production. | 2 hours | Threat Intelligence dev (e.g., jbenami — did this for 692529114) | Tenant IDs present in deployed code; connector creation succeeds |
| 3 | **Update CRI severity** | Escalate from Sev 4 → Sev 3 to match precedent and actual customer impact (contract-blocking) | 5 min | prtanej / TA Approver | Severity reflects actual impact |

### Long-Term Solutions (Weeks/Months)

| # | Action | Steps | Effort | Owner | Verification |
|---|--------|-------|--------|-------|-------------|
| 4 | **Migrate allowlist to dynamic configuration** | Move the static tenant list from `SkuVerifier.cs` to an app setting, Cosmos DB config, or feature flag system. Eliminate need for code deployments to onboard customers. | 1–2 weeks | Threat Intelligence dev team | New customers onboarded without code change |
| 5 | **Complete MDTI API SKU deprecation** | Finish the SKU deprecation plan. Remove the `PremiumMdtiValidator` SKU check entirely once all customers are migrated to the new licensing model. | 1–3 months | TI Product + Engineering | `SkuVerifier` no longer contains hardcoded tenant lists |
| 6 | **Create self-service enablement workflow** | Build a portal or API that allows authorized users to request PMDTI connector enablement without filing a CRI. | 2–4 weeks | TI Engineering | CRI volume for "enable PMDTI" drops to zero |

---

## Open Questions

| Priority | Question | Why It Matters |
|----------|----------|----------------|
| **P0** | What are the tenant IDs for Post Holdings and Metropolitan Police Service? | Required to apply the immediate mitigation |
| **P1** | Is the `premiumSkuAllowListWorkspaces` app setting accessible to on-call for hot-add? | Determines whether immediate mitigation is possible without a code deploy |
| **P2** | What is the timeline for the MDTI API SKU deprecation? | Drives urgency of long-term fix #5 |
| **P2** | Are there other pending CRIs for the same issue? | Pattern suggests this is a recurring onboarding friction point |

---

## Tool Execution Summary

| Tool | Status | Key Finding |
|------|--------|------------|
| `icm-get_incident_details_by_id` | ✅ | CRI, Sev 4, ACTIVE, owned by Threat Intelligence |
| `icm-get_ai_summary` | ✅ | Two customers blocked: Post Holdings, Metropolitan Police Service |
| `icm-get_incident_context` | ✅ | Manually filed (ICMPortal), no monitor trigger, IsCri=true |
| `icm-get_impacted_services_regions_clouds` | ✅ | Zero formal impact (provisioning issue, not outage) |
| `icm-get_incident_customer_impact` | ✅ | Zero subscriptions/SRs/CritSits formally tracked |
| `icm-get_support_requests_crisit` | ✅ | Zero SRs, zero CritSits |
| `icm-get_incident_location` | ✅ | Azure Public cloud, no specific region |
| `icm-get_similar_incidents` | ✅ | No similar incidents found (auto-match) |
| `icm-get_mitigation_hints` | ✅ | **Key signal**: Recommends "add tenant ID to include-list", references ICM 692529114 |
| `icm-get_incident_details_by_id` (692529114) | ✅ | Exact precedent: resolved by jbenami, same root cause |
| `icm-get_incident_details_by_id` (740177161) | ✅ | Related but noise: sales demo request, won't fix |
| `icm-get_ai_summary` (692529114) | ✅ | Confirms: tenant ID missing → added to include-list → deployed hotfix |
| `icm-get_ai_summary` (740177161) | ✅ | Sales demo access, not real issue |
| `enghub-search` | ❌ | Access denied / timed out — blocked |
| `grep` (Sentinel-TiPipeline) | ✅ | Found PMDTI validator, SKU verifier, allowlist code |
| `grep` (Sentinel-TiIngestion) | ✅ | No matches (not relevant to this issue) |
| `grep` (Sentinel-TiPublishers) | ✅ | No matches (not relevant to this issue) |
| `view` (PremiumMdtiValidator.cs) | ✅ | SKU validation gate with PaymentRequiredException |
| `view` (SkuVerifier.cs) | ✅ | **Critical**: hardcoded tenant allowlist, SKU deprecation comments |
| `grep` (Config) | ✅ | `premiumSkuAllowListWorkspaces` app setting key identified |

---

## Appendix: Key Code References

| File | Path | Relevance |
|------|------|-----------|
| `PremiumMdtiValidator.cs` | `Sentinel-TiPipeline/src/ThreatIntelligenceConnectorService/Actions/V20240101/Specific/SpecificConnectorValidator/PremiumMdtiValidator.cs` | SKU validation gate — throws PaymentRequiredException |
| `SkuVerifier.cs` | `Sentinel-TiPipeline/src/ThreatIntelligenceConnectorService/Functions/SkuVerifier.cs` | **Root cause file** — contains hardcoded allowlist + bypass logic |
| `ThreatIntelligenceConnectorServiceConfig.cs` | `Sentinel-TiPipeline/src/ThreatIntelligenceConnectorService/Config/ThreatIntelligenceConnectorServiceConfig.cs` | App setting key `premiumSkuAllowListWorkspaces` |
| `SpecificConnectorValidator.cs` | `Sentinel-TiPipeline/src/ThreatIntelligenceConnectorService/Actions/V20240101/Specific/SpecificConnectorValidator/SpecificConnectorValidator.cs` | Dispatch: routes PremiumMdti connectors to PremiumMdtiValidator |
