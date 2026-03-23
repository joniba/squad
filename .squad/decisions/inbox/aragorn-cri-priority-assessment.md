---
title: "CRI Priority Assessment — Sev3 CRI Ranking"
author: aragorn
date: 2026-03-27
requested_by: Jonathan (Issue #88)
type: decision
tags:
  - cri
  - priority
  - triage
  - sev3
  - icm
---

# CRI Priority Assessment — Sev3 CRI Ranking

**Context:** Jonathan directed that "prioritization requires Aragorn's insights and isn't simply by severity. When there are multiple tickets in a category they need to be prioritized." This document records the reasoning behind the ranking applied to the five active Sev3 CRIs in TASK-INDEX.

---

## Ranking Summary

| Priority | ICM ID | Title | Rationale |
|----------|--------|-------|-----------|
| **P1** | 766937015 | MDTI Premium Connector Enablement Delays | Named commercial customers blocked; repeat incident; 30-min config fix available |
| **P1** | 51000000943039 | TI Upload API: pattern_type Override Bug | S500-tagged customer; detection analytics functionally broken; targeted code fix |
| **P2** | 51000000954460 | Revoked TI Indicators Still Triggering Alerts | Systemic rule gap across all TI templates; workaround self-serviceable; broader fix scope |
| **P2** | 21000000951041 | Azure Government TAXII Ingestion Shortfall | Gov cloud silent data loss; customer-side workaround available; multi-week engineering fix |
| **P3** | 21000000917983 | Deleted Watchlist Items Persist in _GetWatchlist | TSG-resolved; single non-S500 customer; architectural eventual consistency; no recurrence |

---

## Detailed Reasoning Per CRI

### #1 — ICM 766937015: MDTI Premium Connector (P1)

**Customer impact:** Two named customers with commercial consequences.
- **Post Holdings** — contract renewal is blocked pending connector enablement. This is a revenue-at-risk situation.
- **Metropolitan Police Service** — SOC capability and security investment blocked. This has operational security implications for a public institution.

**Recurrence:** REPEAT incident. ICM 692529114 (Oct 2025) was an exact recurrence of this same root cause — tenant ID missing from `SkuVerifier.cs` allowlist. Jonathan mitigated that one personally. The systemic issue (hardcoded tenant list, deprecated SKU) was not remediated after the first occurrence, making this a predictable repeat.

**Workaround availability:** YES and fast. Adding the tenant IDs to the `premiumSkuAllowListWorkspaces` app setting is a hot-config change — estimated 30 minutes, no code deploy required. This is the fastest path to customer unblocking of any CRI on the list.

**Engineering effort:** Immediate: config change (30 min). Short-term: PR to hardcoded list (2 hrs). Long-term: migrate to dynamic config (1-2 weeks) to prevent recurrence.

**Time sensitivity:** HIGH. Contract renewal blockage is a time-bound commercial event. The longer this sits, the greater the relationship and revenue risk.

**Assessment:** P1. Highest immediate business impact + repeat offense + fastest available fix. This should be actioned today, not next sprint.

---

### #2 — ICM 51000000943039: Upload API pattern_type Override Bug (P1)

**Customer impact:** S500-tagged customer — the highest customer tier in our portfolio. ICM metadata is understated (shows `isCustomerImpacting: false`) but the functional impact is clear: URL-type indicators submitted with `pattern_type: "stix"` are stored as `pattern_type: "https"`, causing detection analytics rules that filter on `pattern_type == "stix"` to miss these indicators entirely. The customer's TI workflow is functionally broken for a class of indicators.

**Recurrence:** No prior similar incidents found. However, the V2 Upload API is in Preview and growing adoption — if not fixed, the customer population experiencing this will expand.

**Workaround availability:** YES — the Upload STIX Objects API (separate endpoint) works correctly as an alternative. However, this requires the customer to change how they submit indicators, which is a meaningful behavior change and may not be immediately obvious to them.

**Engineering effort:** Once the root cause is confirmed in the V2 ingestion pipeline code, the fix is targeted: preserve the caller-provided `pattern_type` without overriding it from pattern content inference. Estimated 1-2 days coding + retroactive data fix for the customer's workspace.

**Time sensitivity:** MEDIUM-HIGH. S500 tag demands urgency. The customer has been experiencing broken TI detection since ICM creation (2026-03-11 — over 2 weeks old as of investigation). The window to retroactively fix the affected indicators in their workspace is still open.

**Assessment:** P1. S500 designation plus broken security detection capability. The detection gap is the most severe functional consequence of any CRI on the list — indicators that don't match represent threat coverage gaps.

---

### #3 — ICM 51000000954460: Revoked TI Indicators Triggering Alerts (P2)

**Customer impact:** Not S500 (M365DEN tag). 1 SR, 0 CritSits. The impact is alert fatigue and wasted SOC triage cycles from false-positive alerts on indicators the customer explicitly revoked. No data loss, no service disruption.

**Recurrence:** No similar incidents, but this is likely under-reported. Most customers delete indicators rather than revoke them — this is a STIX lifecycle feature that may not be widely used, but the defect affects anyone who does use revocation.

**Workaround availability:** YES, and self-serviceable:
1. Delete indicators instead of setting `Revoked = true`
2. Clone the built-in EmailUrlInfo TI rule and add `| where Revoked == false` to the KQL

**Engineering effort:** The fix is a KQL template change, but it must be applied across ALL built-in TI analytic rule templates (EmailUrlInfo, DNS, Network, Syslog, IP, domain, etc.) — potentially a dozen templates. Additionally, the deeper fix is to move revocation filtering upstream into the BBTI matching pipeline, which is a broader architectural change. This is more coordination effort than a simple point fix.

**Time sensitivity:** MEDIUM. Alert fatigue compounds over time but doesn't block security operations — alerts can be suppressed or triaged manually. Not contract-blocking or detection-gap-inducing.

**Assessment:** P2. Systemic design gap with real operational impact, but the workaround is self-serviceable and the fix requires broad template coordination rather than a single targeted change.

---

### #4 — ICM 21000000951041: Azure Gov TAXII Ingestion Shortfall (P2)

**Customer impact:** Azure Government (federal) customer. 1 SR, 0 CritSits. Non-S500. The impact is silent data loss — indicators sent by ThreatConnect are silently dropped because the Sentinel TAXII connector doesn't support GZIP encoding. This reduces threat detection coverage, which in a government context carries regulatory and operational security implications beyond typical commercial customers.

**Recurrence:** No similar incidents found, but the TSG exists for exactly this scenario (`TAXIIConnector_GzipIssue`) — meaning this is a documented known limitation, suggesting it has been seen before even if not formally tracked as similar ICMs.

**Workaround availability:** YES, but requires external coordination. The customer (or support) must configure ThreatConnect's TAXII server to respond with plain text (no GZIP compression). This is a customer-side action that doesn't require engineering changes to Sentinel, but it depends on ThreatConnect's configurability and customer cooperation.

**Engineering effort:** The fix — adding GZIP content encoding support to the TAXII connector — is a 2-4 week code change. There's no quick config-level mitigation on the Sentinel side.

**Time sensitivity:** MEDIUM. Silent data loss is ongoing but the workaround path (ThreatConnect config change) is actionable immediately with support engagement.

**Assessment:** P2. Government customer context elevates this above P3, but the customer-side workaround is actionable today, and the Sentinel-side fix is a multi-week engineering effort. Ranked below the revoked indicators CRI only because the workaround is immediately actionable (if ThreatConnect is cooperative) and the customer scope is currently 1 SR vs. a potentially broader defect.

---

### #5 — ICM 21000000917983: Deleted Watchlist Items (P3)

**Customer impact:** Single non-S500 customer (NotS500 tag). 1 SR, 0 CritSits. Stale deleted items appeared in `_GetWatchlist` API responses.

**Recurrence:** No similar incidents found. No recurrence pattern.

**Workaround availability:** The incident was marked `howFixed: "Fixed with TSG"` — it has already been resolved using existing troubleshooting guidance. The pipeline eventually caught up or was manually resolved.

**Engineering effort:** The underlying architecture (Watchlist API → CosmosDB → EventHub → Scuba → Log Analytics eventual consistency) is inherently eventual and the 5-minute SLA is documented behavior. The long-term fixes (Scuba deletion monitoring, CosmosDB direct query for `_GetWatchlist`) are improvements, not emergency fixes.

**Time sensitivity:** LOW. The incident appears to have been resolved (TSG fix). The remaining work is long-term pipeline improvement, not urgent customer unblocking.

**Assessment:** P3. Already resolved, non-recurring, no S500 impact, and the root cause is an architectural eventual consistency characteristic that is documented behavior. Lowest priority for active engineering investment.

---

## Priority Framework Applied

This ranking weights the following factors (in order):

1. **Commercial/operational blocking** — contract renewals, named customers, government SLAs
2. **Customer tier** — S500 > named non-S500 > generic customer
3. **Detection impact** — broken security analytics > false positives > noisy alerts
4. **Workaround speed** — immediate config fix > customer-side action > multi-week code change
5. **Recurrence** — repeat incidents get elevated priority (systemic failure, not one-off)
6. **Resolution status** — TSG-resolved incidents deprioritized

*This assessment informs TASK-INDEX.md ordering — updated by Aragorn on 2026-03-27.*
