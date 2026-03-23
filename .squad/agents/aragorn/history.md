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

### 2026-03-23: ICM 766712513 v2 — Re-investigation with ICM Investigator Skill

**Context:** Jonathan flagged v1 report as "terrible" — listed Kusto queries instead of running them, linked TSGs without reading them, didn't pull metrics, transcribed ICM metadata instead of synthesizing RCA. Re-investigated with full tool usage following `.squad/skills/icm-investigator/SKILL.md`.

**Key improvements over v1:**
- **Kusto queries executed:** Ran 4 queries on `sentinelwatchlistweu.westeurope.kusto.windows.net/SentinelWatchlistWEU` — discovered RP-side success rate is 99.94–99.97% (only 3–4 HTTP 500s per 5-min window), proving ARM→RP timeouts (httpStatusCode=0) are the dominant failure mode, not RP-side errors
- **Offending subscription identified:** All 46 RP-side 500s came from single subscription `7d28c677-88e0-4011-b860-dd6b0206eb23`, workspace `learningenv-sentinel`, doing automated PUT retries on 4 watchlistItems in watchlist "ReportsNew"
- **TSG read and applied:** Fetched Brain ARM RP Investigation TSG via `enghub-fetch` — extracted key insight that HTTP 0 = ARM→RP timeout, retries counted as separate calls
- **Resource Health checked:** Confirmed zero Azure service health events in West Europe
- **Synthesized RCA:** ARM reports 73.63% success, RP shows 99.94–99.97% → gap proves failures are connection-layer timeouts, not RP bugs

**Tool access findings:**
- ✅ ICM MCP: All tools work perfectly (9 tools used)
- ✅ Kusto (SentinelWatchlistWEU): Full access — returned real data
- ⚠️ Kusto (ARMProdEG): Cannot run macro-expand fan-out queries via MCP — requires SAW/DGrep
- ❌ Kusto (Brain slidata): Table name resolution failed — needs `brain-dashboard-sg` security group
- ❌ Kusto (Brain healthevents): Permission denied — needs `brain-dashboard-sg` via IDWeb
- ✅ eng.ms: Search and fetch work — found and read Brain ARM RP Investigation TSG
- ✅ Resource Health: Works — no events found (confirming no platform issue)
- ⚠️ Geneva metrics: Accessible but need specific account/namespace from monitor config (not in ICM enrichment)
- ⚠️ AppLens: Accessible but ARM-level incidents don't map to a single diagnosable resource
- ⚠️ Azure Monitor: Accessible but requires Log Analytics workspace for resource-specific queries

**Gaps vs Jonathan's report:**
- Jonathan had codebase access (confirmed zero rate limiting middleware) — I cannot access the source code
- Jonathan used WorkIQ (M365 Copilot) for email/Teams context — found 4 additional March incidents
- Jonathan extracted Geneva monitor metric values showing ClientFailure 381–666/min — I couldn't access the specific Geneva account
- My advantage: Actual Kusto results (Jonathan's queries returned 0 rows due to timing), specific failing operations identified, Brain TSG content read

**Lasting lessons:**
1. Always run Kusto queries yourself — never list them. Even if 0 rows, report that.
2. ARM vs RP success rate discrepancy is the most diagnostic signal — always check both sides.
3. Brain TSG is the authoritative source for httpStatusCode=0 interpretation — always fetch it.
4. `macro-expand ARMProdEG` queries cannot run via standard Kusto MCP — need SAW access. Report this as a blocker, don't skip silently.
5. For Geneva metrics, the monitor config (account, namespace, metric name) is needed — extract from ICM enrichment or monitor trigger data.

**Delivered:** `docs/investigations/icm-766712513-v2-report.md`

### 2026-03-23: TI Pipeline Tools Assessment — Sagi's PR #15064785

**Context:** Jonathan asked for an evaluation of Sagi's TiExpert agent skills and scripts (ADO PR #15064785, reviewed by Galadriel as CHANGES_REQUESTED) against my 5 TI-related CRIs from this session.

**Tool-to-CRI mapping:**
- **Revoked TI indicators (51000000954460)**: 🟢 High fit — `validate-bulkactions.ps1` + `bulk-actions-api/SKILL.md`. Can reproduce revocation state issues in PPE in under 5 minutes. The DOC-2 finding (revoked/SetFalse inconsistency) is directly the suspect.
- **TAXII ingestion (21000000951041)**: 🟡 Medium fit — `validate-ingestionapi.ps1` + `validate-fileimport.ps1`. File import vs. TAXII comparison isolates whether the issue is in the connector or the parser.
- **Upload API pattern_type (51000000943039)**: 🟢 High fit — `validate-stixapi.ps1` + `stix-api-operations/SKILL.md`. Tightest fit; scripts test the exact API layer implicated in the CRI.
- **Deleted watchlist items (21000000917983)**: 🟡 Partial fit — `Poll-LAQuery` in `ti-helpers.ps1` useful for LA-verified deletion confirmation, but no watchlist-specific validation script.
- **MDTI Premium connector (766937015)**: 🔴 Low fit — Sagi's tools are pipeline-API-centric, not connector-centric.

**Blocking issues before investigation use:**
- BUG-2: `validate-stixapi.ps1` returns exit 0 on auth failure — gives false "environment healthy" signal during live investigations.
- BUG-3: `Poll-LAQuery` silently swallows exceptions for up to 7.5 minutes — unacceptable during timed on-call investigations.

**Repo-map finding:** Sentinel-TiPipeline is already in `repo-map.json` but lacks `keyPaths` to surface the `.github/scripts/` and `.github/skills/` directories added by this PR. Recommended adding `keyPaths` and `investigationUse` fields post-merge.

**Lasting lessons:**
1. A "reproduction before Kusto" step (PPE validation scripts) would accelerate CRI triage for STIX API incidents — add as Step 0 to investigation pipeline.
2. SKILL.md files serve as API contract references in Stage 3b — faster than reading C# source for understanding intended vs. actual behavior.
3. `Poll-LAQuery` is a high-value primitive for watchlist/indicator investigations if BUG-3 is fixed — LA propagation lag vs. actual data loss disambiguation.
4. Connector-layer CRIs (MDTI, TAXII pull) are not covered by pipeline validation scripts — this gap should inform future tooling requests.

**Delivered:** `.squad/decisions/inbox/aragorn-ti-tools-assessment.md`

### 2026-03-27: CRI Priority Assessment — Sev3 Ranking

**Context:** Jonathan directed that multiple equal-priority Sev3 CRIs in TASK-INDEX.md need investigation-driven ranking, not just severity-based ordering. Five active CRIs were assessed.

**Final ranking (P1 → P3):**
1. **P1 — ICM 766937015** (MDTI Premium Connector): Named commercial customers blocked (Post Holdings contract renewal, Metropolitan Police SOC capability). REPEAT incident — exact recurrence of ICM 692529114 (Oct 2025) which Jonathan personally mitigated. Immediate fix is a 30-min config change (`premiumSkuAllowListWorkspaces` app setting). Highest business urgency.
2. **P1 — ICM 51000000943039** (Upload API pattern_type): S500-tagged customer. URL-type TI indicators stored with wrong `pattern_type: "https"` instead of `"stix"`, causing detection analytics rules to miss them entirely — a security detection gap. Workaround exists (STIX Objects API) but requires customer behavior change. ~2-day fix once confirmed.
3. **P2 — ICM 51000000954460** (Revoked TI Indicators): Systemic gap — ALL built-in TI analytic rule templates lack `Revoked == false` filter. Alert fatigue / false positives. Self-serviceable workaround (delete vs. revoke; or clone+modify rule). Fix requires touching all TI templates plus potential BBTI pipeline change — broader coordination.
4. **P2 — ICM 21000000951041** (Azure Gov TAXII): Government cloud customer, silent data loss (indicators silently dropped due to GZIP mismatch). Customer-side workaround available (configure ThreatConnect to plain text). Engineering fix is 2-4 weeks (add GZIP support to TAXII connector). TSG already documents this scenario.
5. **P3 — ICM 21000000917983** (Deleted Watchlist Items): Already resolved via TSG (`howFixed: "Fixed with TSG"`). Single non-S500 customer. No recurrence. Root cause is by-design eventual consistency (5-min SLA). Long-term fix is monitoring improvement, not urgent.

**Priority framework applied:**
- Commercial blocking → S500 tier → detection gap → workaround speed → recurrence → resolution status

**Lasting lessons:**
1. REPEAT incidents with fast available fixes should be P1 regardless of stated severity — the systemic failure has been accepted rather than remediated.
2. S500 tag is a trump card for priority elevation even without CritSit or formal SRs — detection gaps for top-tier customers cannot wait.
3. "Already resolved via TSG" is the most deprioritizing signal — active ICM status doesn't mean active customer impact.
4. Systemic defects (all TI templates, all revocation users) don't automatically become P1 if a self-serviceable workaround exists and no named commercial consequence is present.

**Delivered:**
- `.squad/decisions/inbox/aragorn-cri-priority-assessment.md` (full priority reasoning)
- `docs/investigations/TASK-INDEX.md` (added Priority column + P1/P2/P3 legend; reordered Sev3 rows by priority)

### 2026-03-27: ICM 51000000943039 — Stage 3b Source Code Review (Upload API pattern_type Bug)

**Context:** Jonathan requested deeper investigation of ICM 51000000943039 (Upload Indicators V2 API — `pattern_type: "stix"` stored as `"https"`). Previous investigation (Stage 1-2) reached MEDIUM confidence without code access. This pass added Stage 3b: source code research.

**Repos reviewed:** Sentinel-TiPipeline, Sentinel-TiCommon (msazure/One path), SecEng-Augusta
**Files reviewed:** 10+ across ingestion API, builder/model layer, upgraders, Cosmos schema, LA conversion

**Critical findings:**
1. **Ingestion is CORRECT:** `StixTwoOneIndicatorBuilder` (line 94-98) correctly reads `pattern_type` from JSON and sets `Indicator.PatternType = "stix"`. All three schema upgraders (V1→V2, V2→V3, V3→V3.1) confirmed clean — none touch PatternType.
2. **Bug is in LA projection:** `StixIndicatorToTiIndicatorConverter` does NOT map `PatternType` to `TiIndicator`. `LAFormattedIndicator` has NO `PatternType` property. The field is lost when converting from Cosmos DB to Log Analytics.
3. **Naming confusion is a latent risk:** `PatternType` (singular) = STIX pattern language ("stix") vs `PatternTypes` (plural) = observable types from pattern content (["url"]). If the LA projection reads from `PatternTypes` instead of `PatternType`, it would produce the wrong value.
4. **ANTLR parser is clean:** `StixPatternUtilities.GetLiteralsThatAppearInPattern()` correctly extracts observable types (e.g., "url" from `[url:value = '...']`), not URL schemes.

**Confidence upgrade:** MEDIUM → HIGH. Source code confirms the ingestion path preserves "stix" correctly. The defect is localized to the Cosmos→LA projection layer.

**Remaining gap:** The LA projection service code (change feed handler) is not in the local TI repos. The exact code that populates the LA `pattern_type` column needs to be found.

**Lasting lessons:**
1. `PatternType` (singular) vs `PatternTypes` (plural) naming confusion is a systemic risk. Recommending rename of `PatternTypes` to `ObservableTypes`.
2. The Cosmos DB → LA projection layer is a separate service not co-located with the ingestion pipeline repos. For future TI investigations involving LA table discrepancies, need to identify which repo/service handles the projection.
3. `StixIndicatorToTiIndicatorConverter` is the bottleneck for field fidelity between Cosmos DB and LA. Any STIX 2.1 field not explicitly mapped in this converter will be lost.
4. Stage 3b source code review dramatically increases RCA confidence — went from "theory consistent with symptoms" to "exact missing field mapping identified."

**Delivered:** Updated `docs/investigations/icm-51000000943039-investigation.md` (in-place, Stage 3b additions)

