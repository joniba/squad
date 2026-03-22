# Project Context

- **Owner:** Jonathan
- **Project:** pa-squad — a personal-assistant squad for everyday tasks, research, documentation, tool building, and livesite support
- **Stack:** General-purpose (not a single-stack project)
- **Created:** 2026-03-22

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-03-22: ICM 764634026 — MSPKI G1→G2 Root CA Migration (OceanView SR17)
- **Incident type:** AzRel Red Flag — cert migration enforcement. Severity 25, ACTIVE, Public cloud global impact.
- **Owning team:** AzRel Security Engineering (AzRel Red Flag Program)
- **Jonathan's scope:** Division=Microsoft Security, Org=MTP, Service=USX Threat Intelligence, items NOT starting with `usx-ta` (~15 items)
- **TSG source:** eng.ms OceanView SR17 TSG — comprehensive with Kusto queries, blocker decision tree, migration paths, verification commands
- **Key ICM tools used:** `get_incident_details_by_id`, `get_ai_summary`, `get_incident_context`, `get_mitigation_hints`, `get_similar_incidents`
- **Eng.ms tool used:** `enghub-fetch` to pull full TSG content
- **Resolution patterns from similar incidents:** Bulk close zero-resource tickets, safe deployment + rollback validation, cert status validation before closure
- **Deadlines:** April 10 (central), May 16 (self-migration). Post-deadline = enforced migration = potential outage.
- **Critical gotcha:** G2 certs do NOT have ClientAuth EKU — services using certs for client auth cannot use central migration
- **Delivered:** `docs/icm-764634026-resolution.md` — full walkthrough with queries, decision trees, verification commands, rollback procedures (PR #50, Issue #38)

### 2026-03-22: ICM 766712513 — ARM WATCHLISTS 5xx Errors (West Europe)
- **Incident type:** LiveSite — ARM detected increased HTTP 5xx error rates. Severity 2, ACTIVE, Public cloud West Europe.
- **Owning team:** Threat Intelligence (USX Threat Intelligence)
- **Affected endpoint:** `prd-weu-402.sentinel.microsoft.com` — MICROSOFT.SECURITYINSIGHTS/WATCHLISTS
- **Key finding:** `prd-weu-402` is a repeat offender — at least 4 past incidents reference this same endpoint (698715041, 698758328, 718932939, 757455363)
- **Most relevant precedent:** ICM 757455363 — same endpoint, same resource type (WATCHLISTS), caused by transient application timeouts, self-resolved
- **Recommended causes from mitigation hints:** Customer errors (500s), transient timeouts, service issues, throttling, certificate issues
- **Customer impact at time of investigation:** Zero formal impact (0 SRs, 0 CritSits, 0 impacted subscriptions), but 5xx errors are real
- **ICM tools used:** Full suite — `get_incident_details_by_id`, `get_ai_summary`, `get_incident_context`, `get_mitigation_hints`, `get_similar_incidents`, `get_incident_customer_impact`, `get_incident_location`, `get_impacted_services_regions_clouds`, `get_support_requests_crisit`
- **TSG sources:** Brain ARM RP Investigation TSG (most actionable), ARM Instance Error Rate Outlier Detection
- **Open items:** Kusto queries need to be run for actual impact; FCM change data needs checking; monitor if self-resolving
- **Delivered:** `docs/icm-766712513-investigation.md` — committed directly to main per Jonathan's directive
