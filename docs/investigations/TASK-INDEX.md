---
title: "Investigations Task Index"
date: 2026-03-23
author: bilbo
documentarian: bilbo
category: investigations
tags:
  - index
  - task-tracking
  - priority
  - investigations
status: active
---

# Investigations Task Index

**Purpose:** Prioritized tracking of investigation tasks requiring action. Sorted by severity (Sev2 → Sev3), then by Aragorn's investigation-driven priority ranking (P1 → P2 → P3).

**Last Updated:** 2026-03-23  
**Maintenance:** Update checkbox status as actions are completed. Mark `[x]` when action resolved.

---

## 🔴 Sev 2: Needs Action (Highest Priority)

| Status | ICM ID | Title | Type | Links | Action Needed | Details |
|--------|--------|-------|------|-------|---------------|---------|
| 🔍 | 764634026 | AzRel Red Flag - Ocean View- SR17 - MSPKI Cert Attestation for Central or Self Migration | Security | [Report](icm-764634026/icm-764634026-investigation.md) · [Portal](https://portal.microsofticm.com/imp/v5/incidents/details/764634026/home) | **Investigate complete — awaiting remediation.** TI services confirmed using MSPKI certs for client auth (mTLS). Deadline: Apr 10 (central) / May 16 (self-migration) | HIGH confidence: TAXIIRequestSender.cs uses ClientCertificateOption.Manual. Two remediation paths documented. |
| ⬜ | 766712513 | ARM Watchlist API Error Rates (WEU-402) | LiveSite | [Report](icm-766712513-summary.md) · [Portal](https://portal.microsofticm.com/imp/v5/incidents/details/766712513/home) | **Run Kusto queries** to confirm subscription impact and failure causes (Gateway vs RP-side); check FCM change data; determine if self-resolving | Recurring pattern on `prd-weu-402` endpoint; Sev2 ACTIVE; likely single-tenant request flood or infrastructure issue; 5th recurrence in March 2026 |
| 🔁 | 767184571 | ARM Watchlist API Error Rates (WEU-402) — Recurrence | LiveSite | [Report](icm-767184571-investigation.md) · [Portal](https://portal.microsofticm.com/imp/v5/incidents/details/767184571/home) | **Confirmed recurrence of ICM 766712513.** Add ARM manifest throttling rules for Watchlist ops; investigate per-tenant rate limiting; ARM walkthrough included in report | 6th+ recurrence; same noisy-tenant pattern; P2 priority; zero customer impact; includes ARM throttling/timeout/message size configuration walkthrough |

---

## 🟠 Sev 3: Needs Action (Standard Priority)

*Ordered by Aragorn priority ranking — see [aragorn-cri-priority-assessment.md](./../.squad/decisions/inbox/aragorn-cri-priority-assessment.md) for full reasoning.*

| Status | Priority | ICM ID | Title | Type | Links | Action Needed | Details |
|--------|----------|--------|-------|------|-------|---------------|---------|
| ⬜ | **P1** | 766937015 | MDTI Premium Connector Enablement Delays | CRI | [Report](icm-766937015-investigation.md) · [Portal](https://portal.microsofticm.com/imp/v5/incidents/details/766937015/home) | **Get customer tenant IDs** → add to `premiumSkuAllowListWorkspaces` app setting or `SkuVerifier.cs` allowlist → deploy. Same fix as ICM 692529114 (Oct 2025) | Post Holdings + Metropolitan Police blocked; ~200 hardcoded tenant GUIDs in source; repeat incident; 30-min config fix available; needs migration to dynamic config |
| ⬜ | **P1** | 51000000943039 | TI Upload API: pattern_type Override Bug | CRI | [Report](icm-51000000943039-investigation.md) · [Portal](https://portal.microsofticm.com/imp/v5/incidents/details/51000000943039/home) | **Verify exact API request payload** from customer; execute Kusto to count affected indicators in workspace; confirm pattern_type being overridden | V2 Preview API ingesting `stix` pattern_type as `https`; blocks detection analytics; S500 tag; 1 SR linked |
| ⬜ | **P2** | 21000000951041 | Azure Government TAXII Ingestion Shortfall | CRI | [Report](icm-21000000951041-investigation.md) · [Portal](https://portal.microsofticm.com/imp/v5/incidents/details/21000000951041/home) | **Identify ThreatConnect TAXII server encoding** (GZIP vs plain-text); configure for plain-text response if needed; run TI ingestion baseline queries | Root cause: TAXII connector doesn't support GZIP encoding; indicators silently dropped; Azure Government cloud; 1 SR linked |
| ⬜ | **P3** | 21000000917983 | Deleted Watchlist Items Persist in _GetWatchlist | CRI | [Report](icm-21000000917983-investigation.md) · [Portal](https://portal.microsofticm.com/imp/v5/incidents/details/21000000917983/home) | **Run Kusto diagnostic queries** to verify deletion propagation timing; confirm Scuba health; determine if issue was transient or ongoing | Customer-reported stale data; resolved via TSG; eventual consistency by design (5-min SLA); non-S500; no recurrence |

---

## ✅ Resolved / Informational

| Status | Title | Type | Note |
|--------|-------|------|------|
| 🔄 TRANSFERRED | ICM 51000000954460 | Revoked TI Indicators Still Triggering Alerts | Transferred to different team (2026-03-27). Investigation complete. No longer owned by this team. |
| ℹ️ | [Aragorn ICM Investigation Capability Upgrade](aragorn-icm-capability-upgrade.md) | Analysis | Architectural analysis of Aragorn's investigation capabilities vs ICM Investigator skill; not an actionable incident |

---

## Priority Legend

| Priority | Meaning |
|----------|---------|
| 🔴 Sev2 | **Critical** — LiveSite incidents, active errors, immediate customer impact. Triage first. |
| 🟠 Sev3 | **Standard** — Customer-impacting or data-plane defects. Ordered by Aragorn's investigation-driven priority. |
| ℹ️ | **Informational** — Analysis, postmortems, architectural notes. No action required. |
| **P1** | Named commercial customers blocked OR S500 customer OR repeat incident with fast mitigation available |
| **P2** | Systemic defect with self-serviceable workaround, OR government cloud with customer-side mitigation |
| **P3** | TSG-resolved, single non-S500 customer, by-design behavior, no recurrence |

---

## Progress Summary

- **Total investigations:** 11 files
- **Needs action (Sev2):** 3 (ICM 764634026 + ICM 766712513 + ICM 767184571 recurrence)
- **Needs action (Sev3):** 5 distinct incidents
- **Informational:** 1
- **Completed:** 0/5 active

**Next step:** ICM 767184571 is a confirmed recurrence of 766712513 — implement ARM throttling rules for Watchlist operations (see walkthrough in 767184571 report). Start with Sev2 remediation actions.

---

*Maintained by Bilbo (Librarian) · Updated as investigations are resolved*
