# Squad Decisions

## User Directives

### 2026-03-23T14:42:40Z: Exhaust existing tools before researching alternatives
**By:** Jonathan (via Copilot)  
**What:** Before any agent declares a tool/capability is missing and starts researching workarounds, they MUST first inventory all available MCP tools and test them. Elrond researched ADO file access workarounds without checking that search_code and repo_get_pull_request_by_id already existed in the ADO MCP. Galadriel did the same. Rule: try what you have → then research what you don't.  
**Why:** Two agents wasted time researching workarounds for capabilities that already existed. The squad must be tool-aware before tool-seeking.

### 2026-03-23T14:16:25Z: Process decision — Galadriel did not prompt on missing integration
**By:** Jonathan (via Copilot)  
**What:** Galadriel reviewed ADO PR 15064785 but could not read file contents (ADO MCP lacks get_file_contents). She noted the limitation in her report but did NOT stop and prompt Jonathan. This violates the prerequisite check directive: "if a tool fails, STOP and report what's missing." The directive applies to ALL agents, not just Aragorn.  
**Why:** PR reviews without file contents are incomplete. Jonathan must be prompted so the integration can be fixed, not worked around silently.

### 2026-03-23T14:09:34Z: User directive — Investigation-informed prioritization
**By:** Jonathan (via Copilot)  
**What:** After ICM investigation completes, Aragorn must provide prioritization insights (not just severity). Bilbo must update the TASK-INDEX with Aragorn's recommended priority, especially when multiple tickets exist in the same category (e.g., multiple CRIs). Prioritization requires Aragorn's analysis — it's not a mechanical sort by severity.  
**Why:** Severity alone doesn't capture urgency. Aragorn sees customer impact, blast radius, fix complexity, and dependencies that affect real priority. Bilbo needs this input to maintain an accurate task index.

### 2026-03-23T13:56:51Z: User directive — Research tasks always use claude-opus-4.6
**By:** Jonathan (via Copilot)  
**What:** All research tasks (Elrond or any agent doing research/investigation/analysis) must use claude-opus-4.6. Never use haiku or sonnet for research. Research requires analytical reasoning, not cost optimization.  
**Why:** Haiku was being used for deep research tasks, producing shallow results. Research quality is non-negotiable — cost-first only applies to mechanical tasks.

### 2026-03-23T13:48:27Z: User directive — YOLO mode disabled
**By:** Jonathan (via Copilot)  
**What:** YOLO mode disabled. Jonathan is present — all design reviews, approvals, and decisions must be prompted to Jonathan. Do not auto-approve. Resume normal design-review gates (pending-design-review → Jonathan reviews → design-approved).  
**Why:** Jonathan is actively working and wants to review designs before implementation proceeds.

### 2026-03-23T13:01:34Z: User directive — No empty Teams notifications
**By:** Jonathan (via Copilot)  
**What:** Never send empty/zero-count notifications to Teams. Only notify when there's something actionable (blocked issues, stale PRs, new CRIs, etc.). Curated criteria list maintained in .squad/skills/squad-notifications/SKILL.md.  
**Why:** Empty notifications are noise and train Jonathan to ignore the channel.

### 2026-03-23T12:59:38Z: Process decision — Bilbo auto-docs enforcement
**By:** Coordinator (self-correction)  
**What:** The "Bilbo auto-documents completed features" directive was captured but never enforced. Root cause: the directive is a note, not a gate. Fix: after every PR merge or feature completion, the coordinator MUST spawn Bilbo (lightweight mode) to evaluate if user-facing docs are needed. This is the same pattern as spawning Galadriel for PR review — a mandatory post-completion step, not optional.  
**Enforcement rule:** After closing any issue that produced new scripts, tools, or user-facing capabilities:
1. Coordinator asks: "Does this feature need a user guide?"
2. If yes → spawn Bilbo to write it before marking the work complete
3. If no (pure research, internal refactoring, charter updates) → skip  
**Why this was missed:** The directive was in the decisions inbox but the coordinator's workflow didn't include a Bilbo checkpoint. Three features shipped without guides (scheduler, email watchdog, Teams knowledge library).

### 2026-03-23T02:49:15Z: User directive — No empty documentation folders
**By:** Jonathan (via Copilot)  
**What:** Do not create empty documentation category folders. Create folders on-demand only when placing a document in them. If a category has no docs yet, the folder shouldn't exist.  
**Why:** Empty folders are noise. The organizational structure should emerge from actual content, not pre-created scaffolding.

### 2026-03-23T02:40:55Z: User directive — Push after merge
**By:** Jonathan (via Copilot)  
**What:** Always push main to origin after a PR is approved and merged. Never let local main drift ahead of origin/main — it causes phantom diffs in future PRs.  
**Why:** 11 unpushed commits caused every PR to show 25+ phantom files in the GitHub diff, triggering false scope-violation rejections from Galadriel.

### 2026-03-23T02:22:11Z: User directive — Parallel Aragorn spawns for different ICMs
**By:** Jonathan (via Copilot)  
**What:** Multiple Aragorn agents may be spawned in parallel without regard to other running agents, as long as each Aragorn is working on a different ICM incident. No contention expected — each writes to a unique file (docs/investigations/icm-{id}.md). To avoid git index conflicts, Aragorn writes the file but does NOT commit — Scribe batch-commits after all agents finish.  
**Why:** ICM investigations are independent and time-sensitive. Parallelism is safe because outputs are unique files with no shared state.

### 2026-03-23T02:18:56Z: User directive — Watchdog strategy shift
**By:** Jonathan (via Copilot)  
**What:** Prefer Tamir's teams-monitor approach (single agent skill) over the 6-script composable pipeline. Reasons: simpler, cheaper (~6 req vs ~18 per run), will run more than once daily soon. IMPORTANT: do NOT use `gh copilot` — it doesn't work. Use `copilot -p` with `--allow-tool='workiq'`. Run POCs early to validate before building full solution. Prompt Jonathan for help if needed.  
**Why:** Cost matters at higher frequency. Simplicity preferred. Known `gh copilot` issue from previous project.

### 2026-03-23T02:18:56Z: User directive — Corrupted PRs: redo with full review cycle
**By:** Jonathan (via Copilot)  
**What:** Do NOT extract files from corrupted branches (Option C was wrong). Close corrupted PRs, create fresh branches from main, re-run each agent's task cleanly, open new PRs, run full Galadriel review cycle. New PRs may reference closed PRs for context. This applies to any future corrupted PRs as well.  
**Why:** Jonathan wants the full quality pipeline (branch → PR → review → merge) even if it's slower. Shortcuts compromise the review process.

## Active Decisions

### 2026-03-23T14:35:00Z: ICM Scan Watermark Design (Approved)
**Author:** Gandalf  
**Status:** Approved  
**Issue:** #92

**Context:** ICM scan currently queries ALL active incidents every time, wasting LLM calls on already-processed incidents. Needs watermark-driven query window to track scans and avoid reprocessing.

**Decision:**
- **Location:** `.squad/icm-scan-watermark.json` (gitignored, machine-local state)
- **Schema:** `{ lastScan (ISO 8601), seenIds (array, capped at 200), version: 1 }`
- **seenIds rotation:** FIFO cap at 200 entries (simple, keeps file ~7 KB)
- **Query window:** `now() - lastScan + 1h buffer`, capped at 7 days, 24h default on first run
- **Known IDs → LLM:** Merge watermark seenIds + investigation report IDs + GitHub issue IDs, pass to prompt to skip them
- **Corruption handling:** Log warning, delete corrupted file, start fresh (ephemeral state, safe to discard)
- **-Reset flag:** Clears watermark, next scan reverts to 24h default

**Rationale:**
- Gitignore: Machine-local tracking state, not shared config
- FIFO rotation: Simple, efficient, sufficient for deduplication window
- 1h buffer: Handles race conditions between scan cycles
- 7-day cap: Handles offline machines gracefully
- LLM-level dedup: Single source of truth in prompt prevents reprocessing

**Tracking:** Issue #92, assigned to Gimli (squad:gimli) for implementation, Galadriel for review.

### 2026-03-22T17:30:00Z: User directive — copilot CLI usage
**By:** Jonathan (via Copilot)  
**What:** `gh copilot` does not work. Must use vanilla `copilot -p` or `copilot -i` commands instead. Cannot save their result to a variable.  
**Why:** User request — captured for team memory

### 2026-03-22T17:29:59Z: User directive — scripting philosophy
**By:** Jonathan (via Copilot)  
**What:** Scripts should be short and concise and combined to create more complex functionality, rather than one big complex script. Build gradually, making sure each step works using real tests (no mocks) before adding additional complexity. Separate into small tasks.  
**Why:** User request — captured for team memory

### 2026-03-22T17:30:00Z: Teams Watchdog Architecture (Proposed)
**Author:** Gandalf  
**Status:** Proposed  

**Context:** Jonathan wants a daily Teams message watchdog that summarizes his decisions, action items, and important context from Teams conversations.

**Decision:** Decompose into a **6-step pipeline** of composable PowerShell scripts, not a monolithic watchdog:
1. **Probe** — Fetch raw Teams messages via WorkIQ MCP tool
2. **Filter** — Extract only Jonathan's sent messages
3. **Extract** — Identify decisions, action items, commitments
4. **Format** — Build a clean daily markdown summary
5. **Schedule** — Watchdog loop (based on ralph-watch.ps1 pattern)
6. **Deliver** — Post to Teams + archive to disk

Each step is a separate script under `.squad/skills/teams-watchdog/`, chained via file I/O (because `copilot -p` output cannot be captured in variables).

**Constraints Honored:**
- Scripts are short, composable, under 30-50 lines each
- Uses `copilot -p` (not `gh copilot`)
- File-based I/O between steps (no variable capture)
- Each step testable independently with real data

**Tracking:** Issues #1-#6 on `jbenami_microsoft/ms-pa`, labeled `squad` + `squad:gimli` (assigned to Gimli for implementation).

### 2026-03-22T17:35:00Z: Skills Scan: squad-skills Plugin Catalog (Proposed)
**Author:** Elrond (Researcher)  
**Status:** Proposed  

**Context:** Scanned all 20 plugins in tamirdresher/squad-skills to identify relevance to Teams message watchdog (Issues #1–#6).

**Finding — Directly Useful (🟢):**
1. **teams-monitor** — Demonstrates proven WorkIQ query patterns, filtering heuristics, and rate-limiting warnings. Critical reference for watchdog Probe/Filter steps.
2. **news-broadcasting** — Documents Teams webhook delivery pattern (URL at `~\.squad\teams-webhook.url`, POST via `Invoke-RestMethod`, Adaptive Cards formatting). Directly applicable to Deliver step.
3. **secrets-management** — Establishes security foundation: Windows Credential Manager for secrets, machine-local files (priority 2), `.env` (priority 3). Essential for production-ready watchdog scripts.

**Key Insight:** WorkIQ is poll-based with indexing delay—well-aligned with our daily summary design. Rate-limit to one WorkIQ query per agent cycle.

**Recommendation:** Install teams-monitor (P0), news-broadcasting (P0), secrets-management (P1) for watchdog work. Keep agency-optimal-config, teams-ui-automation, mail-mcp on radar for Phase 2.

**Tracking:** Findings documented in `docs/squad-skills-catalog.md` (Bilbo's catalog, 25.2 KB).

### 2026-03-22T17:40:00Z: User directive — work source policy
**By:** Jonathan (via Copilot)  
**What:** Only pull tasks from the board (GitHub issues), never from the chat. All work must be created as issues first.  
**Why:** User request — captured for team memory

### 2026-03-22T17:45:00Z: User directive — clean root chat
**By:** Jonathan (via Copilot)  
**What:** Whenever Jonathan mentions creating a task or mentions Gandalf, the Coordinator MUST pass the request to Gandalf via a subagent spawn to create an issue. The root chat must stay clean — only show handoffs to the squad, not planning or decomposition work.  
**Why:** User request — captured for team memory

### 2026-03-22T17:40:00Z: Coffee-Ratings Squad-Infra Audit & Port Plan (Proposed)
**Author:** Gandalf  
**Status:** Proposed  

**Context:** Jonathan requested an audit of coffee-ratings squad-infra work to identify reusable patterns for ms-pa, plus hiring a Reviewer agent based on the coffee-ratings "Bobbie" charter.

**Decision:** Two parallel initiatives tracked as dependent issue chains on `jbenami_microsoft/ms-pa`:

**Squad-Infra Audit (5-step pipeline, Issues #7–#11):**
1. **#7** — Elrond researches squad-infra git history + GitHub issues from coffee-ratings (11 known commits)
2. **#8** — Bilbo documents findings with links, diffs, and category groupings
3. **#9** — Gandalf decides PORT/SKIP/ADAPT for each item with reasoning
4. **#10** — Gimli ports approved changes to ms-pa
5. **#11** — Bilbo creates a reusable squad bootstrap template from ported patterns

**Reviewer Hire (2-step chain, Issues #12–#13):**
1. **#12** — Elrond studies Bobbie's charter and history, extracts generalizable patterns
2. **#13** — Gandalf hires new LotR-cast Reviewer agent adapted from Bobbie's patterns

**Reasoning:**
- Sequential dependency chain ensures full context before each step
- Bilbo's template (#11) is the most valuable long-term artifact
- Reviewer hire fills team gap (no dedicated quality gate) and applies proven patterns from mature squad

**Tracking:** Issues #7–#13 labeled with `squad` + agent-specific labels.



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


---

### 2026-03-22: ICM 764634026 Resolution Walkthrough — MSPKI G1→G2 Migration
**Author:** Aragorn (Operator)
**Status:** Proposed
**Issue:** #38 | **PR:** #50

**Context:** Jonathan's team has ~15 in-scope items under ICM 764634026 (OceanView SR17) requiring MSPKI G1→G2 root CA migration. Items were excluded from central migration due to safety concerns.

**Decision:** Built a comprehensive resolution doc at `docs/icm-764634026-resolution.md` covering:
- Blocker assessment decision tree (Pinning, Torus, ClientAuth, SDP, Rollback)
- Two migration paths: Central (April 10 deadline) vs Self-migration (May 16 deadline)
- Ready-to-paste Kusto queries for scoping, blocker ID, cert renewal, and validation
- Verification commands (az CLI, PowerShell, OpenSSL, browser)
- Rollback procedures (global and per-region)
- Exit criteria and Red Flag tag reference

**Key risk:** G2 certificates do NOT include ClientAuth EKU. Any service using MSPKI certs for client authentication must use self-migration path and address the ClientAuth blocker separately.

**Open items for Jonathan:**
1. Run the Kusto scoping queries to get exact item list
2. Review Ligal Tuval meeting recording for service-specific guidance
3. Decide central vs self-migration per item
4. Set ETA tags on each IcM

**Reasoning:** Combined ICM incident data (details, AI summary, context, mitigation hints, similar incidents) with the full OceanView SR17 TSG from eng.ms to produce a self-contained walkthrough that doesn't require re-reading the TSG.


---

# Assessment: Sagi's TI Pipeline Tools for ICM Investigation Integration

**Author:** Aragorn  
**Requested by:** Jonathan  
**Date:** 2026-03-23  
**Source:** Galadriel's review of ADO PR #15064785 (`docs/reviews/pr-review-15064785-v2.md`)  
**Context:** Evaluated against 5 TI-related CRIs from this session's investigations

---

## What Sagi Built

From Galadriel's review, the PR introduces:

**Validation scripts** (`.github/scripts/`):
- `validate-stixapi.ps1` — STIX Web API (create/read/update/delete indicators)
- `validate-bulkactions.ps1` — Bulk Edit/Delete across 6 STIX types
- `validate-fileimport.ps1` — File import API (STIX bundle upload)
- `validate-stixwebapi.ps1` — STIX Web API layer validation
- `validate-ingestionapi.ps1` — Ingestion pipeline API
- `validate-all.ps1` — Parallel orchestrator for all of the above
- `ti-config.ps1` — PPE environment config (subscription, workspace, tenant IDs)
- `ti-helpers.ps1` — Shared helpers including `Poll-LAQuery` (LA result polling)

**SKILL.md references** (`.github/skills/`):
- `bulk-actions-api/SKILL.md` — Mutator reference for Edit/Delete operations on indicators
- `file-import-api/SKILL.md` — STIX bundle import API reference
- `stix-api-operations/SKILL.md` — Full STIX API reference (Galadriel: "Comprehensive and clear, no issues found")

**Agent routing** (`.github/agents/`):
- `tiexpert.agent.md` — Agent definition routing `stix-api`, `bulk-actions`, `file-import`, `ingestion-api` commands

---

## CRI → Tool Mapping

### ICM 51000000954460 — Revoked TI Indicators

**What happened:** Indicators with `revoked=true` were behaving unexpectedly — either not propagating the revocation state downstream or not transitioning correctly.

**Relevant tools:**
- ✅ **`validate-bulkactions.ps1`** — directly tests Edit operations on indicators including the `revoked` field across all 6 STIX types. Could reproduce the revocation state issue against PPE in minutes.
- ✅ **`bulk-actions-api/SKILL.md`** — defines the mutator contract for `revoked`. Critically, it documents that `revoked` "accepts `SetFalse` but ignores it" — this exact limitation is what DOC-2 flags as contradictory in the Selection Guide. This would have been my first stop: read the SKILL.md to understand whether `SetFalse` for `revoked` is intentional or a bug.
- ✅ **`ti-helpers.ps1 Poll-LAQuery`** — after submitting a bulk Edit to set `revoked=true`, poll LA to confirm the indicator's state actually propagated. The 15-retry, 30-second sleep pattern would verify the pipeline's end-to-end revocation latency.

**Investigation impact:** Without these tools, I was working from ICM context + Kusto queries. With `validate-bulkactions.ps1` I could reproduce in PPE within 5 minutes, compare actual API response to expected, and confirm whether the issue is in the bulk-action layer or downstream propagation. The SKILL.md's `SetFalse`/`revoked` inconsistency (DOC-2) would immediately flag a known limitation as a suspect.

---

### ICM 21000000951041 — TAXII Ingestion Failures

**What happened:** TAXII-sourced STIX objects failing to ingest or partially ingesting with parsing errors.

**Relevant tools:**
- ✅ **`validate-ingestionapi.ps1`** — tests the ingestion pipeline API directly. TAXII pull is an ingestion vector; this script validates the same underlying pipeline.
- ✅ **`validate-fileimport.ps1`** — file-based STIX bundle import is functionally analogous to TAXII pull (both deliver STIX JSON bundles). Behavioral parity check: if file import works but TAXII doesn't, the issue is in the TAXII connector, not the parser.
- ✅ **`file-import-api/SKILL.md`** — documents STIX bundle format requirements and the `@($stixObjects)` array construction pattern. A TAXII-sourced bundle that violates these constraints would fail in the same way DOC-3 describes.
- ⚠️ **`ti-helpers.ps1 Poll-LAQuery`** (with caveat) — useful for verifying whether TAXII-ingested indicators appear in Log Analytics. BUT BUG-3 (silent exception swallowing) means a 401 or 404 would cause 7.5 minutes of silent waiting. Would still use it post-BUG-3 fix.

**Investigation impact:** The key diagnostic question for TAXII investigations is always: "Is the bundle malformed, or is the parser wrong?" `validate-fileimport.ps1` lets me answer that by testing known-good STIX bundles through the same pipeline. If file import works, TAXII connector is the suspect. That isolation step was manual and slow without these tools.

---

### ICM 51000000943039 — Upload API pattern_type Field

**What happened:** Indicators submitted via the Upload API with certain `pattern_type` values (e.g., `snort`, `yara`) being rejected or stored incorrectly.

**Relevant tools:**
- ✅ **`validate-stixapi.ps1`** — directly exercises the STIX Upload API (the endpoint implicated in this CRI). Would test `pattern_type` variants against PPE in an automated way.
- ✅ **`validate-stixwebapi.ps1`** — second layer of STIX API validation; useful for confirming whether the issue is in the API layer or the processing layer.
- ✅ **`stix-api-operations/SKILL.md`** — Galadriel rated this "Comprehensive and clear API reference; no issues found." It documents accepted field values and API contract. For a `pattern_type` mismatch issue, this is the authoritative source for what values the API claims to accept.

**Investigation impact:** This is the tightest fit. The CRI is literally about an API field, and Sagi's tools are API validation scripts. Without them, I had to trace the issue through ICM context + Kusto query results showing rejected payloads. With `validate-stixapi.ps1`, I could construct a minimal reproducing case in PPE with specific `pattern_type` values in under 10 minutes. The SKILL.md would tell me immediately whether those values are in the supported set or are an undocumented extension.

**Caveat:** BUG-2 (`return` instead of `exit 1` on token failure) means a CI run of `validate-stixapi.ps1` could silently succeed even if authentication fails. During a live investigation, that would be a dangerous false negative — I'd think the environment is healthy when it's just unauthenticated. This bug must be fixed before these scripts enter my investigation toolkit.

---

### ICM 21000000917983 — Deleted Watchlist Items

**What happened:** Watchlist items appearing as deleted in the API but remaining visible in some data paths, or vice versa.

**Relevant tools:**
- ⚠️ **`ti-helpers.ps1 Poll-LAQuery`** — the primary useful tool here. After a watchlist delete operation, polling LA to verify item removal is exactly what this function does. My ARM WATCHLISTS investigation (ICM 766712513) showed that RP-side success ≠ eventual consistency in LA. Poll-LAQuery would automate the reconciliation check I did manually via Kusto.
- ⚠️ **`validate-bulkactions.ps1`** — covers Delete operations on STIX objects. Watchlists are not a STIX type in the traditional sense, but if deleted watchlist items are backed by TI indicators in the pipeline, this is relevant.
- ❌ **No direct watchlist-specific validation script** — this is a gap. Sagi's tools are STIX-API-centric; watchlists sit at a different layer (ARM resource type `MICROSOFT.SECURITYINSIGHTS/WATCHLISTS`).

**Investigation impact:** Partial improvement. Poll-LAQuery post-delete would confirm whether the issue is at the API layer (item not actually deleted) or the propagation layer (deleted in API, not propagated to LA). But for watchlist-specific operations, Sagi's scripts don't cover the ARM WATCHLISTS resource type — the gap identified in ICM 766712513 remains.

---

### ICM 766937015 — MDTI Premium Connector

**What happened:** MDTI Premium connector configuration or authentication issue.

**Relevant tools:**
- ⚠️ **`ti-config.ps1`** — documents the PPE environment topology (subscription, workspace, tenant). Useful as reference for understanding which resources the connector is pointing at, but it's PPE-scoped. MDTI Premium connector issues typically involve prod credentials and partner API endpoints.
- ⚠️ **`tiexpert.agent.md`** — the agent routing definition. If extended to include a `connector` command flow, it could orchestrate connector health checks. As currently described in the PR, the routing covers `stix-api`, `bulk-actions`, `file-import`, and `ingestion-api` — no connector diagnostics route.
- ❌ **No connector-specific validation** — Sagi's tools are focused on the TI pipeline API layer, not on data connector configuration or MDTI partner API authentication.

**Investigation impact:** Minimal direct utility for connector CRIs. The toolset validates "what happens after data enters the pipeline" — it doesn't validate the connector's ability to pull data into the pipeline in the first place. This is the weakest fit of the five CRIs.

---

## Summary Matrix

| CRI | Incident | Tool Fit | Best Tool |
|-----|----------|----------|-----------|
| 51000000954460 | Revoked TI indicators | 🟢 High | `validate-bulkactions.ps1` + `bulk-actions-api/SKILL.md` |
| 21000000951041 | TAXII ingestion | 🟡 Medium | `validate-ingestionapi.ps1` + `validate-fileimport.ps1` |
| 51000000943039 | Upload API pattern_type | 🟢 High | `validate-stixapi.ps1` + `stix-api-operations/SKILL.md` |
| 21000000917983 | Deleted watchlist items | 🟡 Partial | `Poll-LAQuery` in `ti-helpers.ps1` |
| 766937015 | MDTI Premium connector | 🔴 Low | None — different layer |

---

## What I Would Add to My Investigation Pipeline

If these tools were available in my investigation environment:

### Step 0 (New): PPE Reproduction Before Kusto
Currently my pipeline goes: ICM context → Kusto queries → hypotheses. With Sagi's scripts, I'd insert a **reproduction step before Kusto**:

```
validate-stixapi.ps1    →  Can I reproduce the reported behavior in PPE?
validate-bulkactions.ps1 →  Does the bulk API handle edge cases correctly?
Poll-LAQuery            →  Does data appear in LA within expected latency?
```

This would catch "PPE reproduces, prod also reproducing" vs. "PPE works, prod-specific issue" — a distinction that would have accelerated CRI 51000000943039 significantly.

### Stage 2b Enhancement: LA-verified Kusto
After running Kusto queries showing error patterns, `Poll-LAQuery` would confirm whether affected indicators are actually missing from LA or just slow to appear. This disambiguation (pipeline lag vs. actual data loss) is critical for watchlist and indicator investigations.

### Stage 3b Enhancement: SKILL.md as API Contract Source
Currently in Stage 3b (source code research), I search repos for function names and class names. With `stix-api-operations/SKILL.md` and `bulk-actions-api/SKILL.md` as authoritative contract references, I'd read the SKILL.md first before going to source — it's faster than reading the C# and gives me the intended contract vs. the actual C# implementation.

### Prerequisite: Fix BUG-2 Before Trusting Script Results
BUG-2 (`return` vs `exit 1` on token failure) is a blocking concern for investigation use. If `validate-stixapi.ps1` exits 0 when unauthenticated, I'd think PPE is healthy when it's actually misconfigured. Every investigation step that relies on these scripts assumes BUG-2 is fixed.

---

## Repo-Map Assessment

**Current state:** Sentinel-TiPipeline **is already in `repo-map.json`**:

```json
{
  "name": "Sentinel-TiPipeline",
  "path": "C:\\dev\\ti\\Sentinel-TiPipeline",
  "defaultBranch": "users/joniba/aspire-bdd",
  "description": "Customer Threat Intelligence Pipeline — code, tests, and deployment scripts",
  "relevance": "service"
}
```

**What's missing:** The current entry treats Sentinel-TiPipeline as an opaque service repo. After this PR merges, it will also contain a structured investigation toolkit in `.github/`. The repo-map has no `keyPaths` field to surface this.

**Recommended update:** Add a `keyPaths` array to the Sentinel-TiPipeline entry:

```json
{
  "name": "Sentinel-TiPipeline",
  "path": "C:\\dev\\ti\\Sentinel-TiPipeline",
  "defaultBranch": "users/joniba/aspire-bdd",
  "description": "Customer Threat Intelligence Pipeline — code, tests, and deployment scripts",
  "relevance": "service",
  "keyPaths": [
    ".github/scripts/validate-stixapi.ps1",
    ".github/scripts/validate-bulkactions.ps1",
    ".github/scripts/validate-fileimport.ps1",
    ".github/scripts/validate-ingestionapi.ps1",
    ".github/scripts/validate-all.ps1",
    ".github/scripts/ti-config.ps1",
    ".github/scripts/ti-helpers.ps1",
    ".github/agents/tiexpert.agent.md",
    ".github/skills/stix-api-operations/SKILL.md",
    ".github/skills/bulk-actions-api/SKILL.md",
    ".github/skills/file-import-api/SKILL.md",
    "src/StixAPIs/"
  ],
  "investigationUse": "Validation scripts for STIX API, bulk actions, file import, and ingestion pipeline. Run against PPE for reproduction before Kusto investigation. Poll-LAQuery in ti-helpers.ps1 for LA-verified data confirmation."
}
```

**Rationale:** When I'm in Stage 3b (source code research) and the incident involves TI indicators, I need to find both the service code (`src/StixAPIs/`) and the validation scripts (`.github/scripts/`) quickly. Without `keyPaths`, I have to search the full repo tree. With it, I go directly to the right tools.

---

## Blocking Issues That Affect Investigation Use

Before integrating these scripts into my investigation workflow, two bugs from Galadriel's review must be fixed:

1. **BUG-2** — `validate-stixapi.ps1` exits 0 on auth failure. Investigation use would give false "environment is healthy" signals. **Must fix before using in on-call investigation.**

2. **BUG-3** — `Poll-LAQuery` silently swallows all exceptions for up to 7.5 minutes. During a live investigation, this is 7.5 minutes of no signal — unacceptable when the clock is ticking. **Must fix before using Poll-LAQuery for time-sensitive investigations.**

BUG-1 (polling loop never exits on `Failed` state) is also relevant — it would cause my automated validation runs to hang for 15 minutes before reporting failure. All three bugs should be resolved before PR merge per Galadriel's `CHANGES_REQUESTED` verdict.

---

## Recommendation

**Integrate after Sagi addresses Galadriel's blocking items.** The toolset is the right shape — validation scripts that match investigation reproduction workflows, SKILL.md files that serve as API contract references, and an LA polling utility for end-to-end confirmation. Three of the five CRI types (revoked indicators, Upload API, TAXII ingestion) would have meaningfully faster investigation paths with these tools available.

**Don't wait for BUG-3 to integrate Poll-LAQuery** — the fix is straightforward and the function is highly valuable for watchlist and indicator investigations. Flag Sagi on BUG-3 priority specifically for livesite use.

**Update repo-map.json** once the PR merges, adding `keyPaths` and `investigationUse` to the Sentinel-TiPipeline entry. This is a Stage 3b tooling improvement that benefits every future TI investigation.


---

# Decision: Upgrade Aragorn's ICM Investigation Capabilities

**Author:** Elrond (Researcher)  
**Date:** 2026-03-22  
**Status:** Proposed  
**Priority:** HIGH (Jonathan is unhappy with current quality)

## Context

Jonathan asked Aragorn to investigate ICM 766712513 (ARM Increased Error Rates on MICROSOFT.SECURITYINSIGHTS/WATCHLISTS). Aragorn produced a report that accurately transcribed ICM metadata but performed no original analysis — no Kusto queries were executed, no TSG content was read, no hypotheses were formed, no incident classification was made, and no actionable remediation was provided. The report describes the incident but does not investigate it.

I analyzed Jonathan's existing ICM Investigator skill (15 files, ~2,500 lines across 6 agents, 3 commands, and 3 reference docs). It implements a sophisticated 5-stage pipeline with evidence-based reasoning, hypothesis testing, metric verification, and structured remediation planning.

## Decision

**Upgrade Aragorn's charter with a structured investigation methodology and mandatory tool checklist.**

### Key Changes

1. **Add investigation pipeline** — 4 phases: Gather → Enrich → Analyze → Report (simplified from the skill's 5+1 stages)
2. **Add mandatory tool checklist** — Aragorn must attempt: Kusto queries, Geneva metrics, TSG content fetching, AppLens diagnostics, and incident classification
3. **Add output standards** — Every report must include: classification, customer impact assessment, severity evaluation, hypotheses with evidence, confidence level, and actionable remediation
4. **Add quality gate** — Self-check before finalizing: "Did I execute at least one query? Did I read the TSG? Did I form hypotheses?"

### Critical Gaps Addressed

| Gap | Fix |
|-----|-----|
| Never runs Kusto queries | Charter mandates `azure-mcp-kusto` execution |
| Never reads TSG content | Charter mandates `enghub-fetch` after `enghub-search` |
| No incident classification | Charter adds True Positive / False Positive / Noise framework |
| No hypothesis testing | Charter adds evidence-based reasoning requirement |
| No Geneva metric verification | Charter adds `geneva-mcp-server-query_timeseries` usage |
| No actionable remediation | Charter adds structured remediation with effort/owner/verification |

## Reasoning

The ICM Investigator skill is battle-tested and represents best practices from production incident response. Aragorn doesn't need the full 6-agent orchestrated pipeline — that level of complexity is appropriate for a VS Code extension, not a CLI agent. But the investigation methodology, tool usage patterns, and output quality standards should absolutely be ported.

The single highest-impact change is getting Aragorn to **execute Kusto queries** rather than just listing them. For ICM 766712513, three pre-built queries were provided in the incident enrichment. Running them would have revealed the actual failing operations, impacted subscriptions, and whether failures were ARM-side or RP-side — transforming a descriptive report into an investigative one.

## Impact

- **Full analysis:** `docs/aragorn-icm-capability-upgrade.md`
- **Aragorn charter update:** Section 4 of the analysis contains the exact text to add
- **No code changes needed** — this is a charter/methodology upgrade

## Next Steps

1. Gandalf reviews and approves this decision
2. Update `aragorn/charter.md` with the methodology, tool checklist, and output standards from the analysis
3. Re-investigate ICM 766712513 with upgraded methodology as validation test


---

---
title: "TI Pipeline Integration — Sagi's TiExpert PR Analysis"
author: Elrond (Researcher)
status: Proposed
date: 2025-07-10
pr: 15064785
repo: Sentinel-TiPipeline
branch: features/sagimarus/tiexpertagent
requested_by: Jonathan
tags:
  - ti-pipeline
  - integration
  - tooling
  - research
  - investigation
---

# Decision Proposal: Integrating Sagi's TiExpert Tools Into Our Squad

**Author:** Elrond (Researcher)  
**Status:** Proposed  
**Source:** ADO PR 15064785 on Sentinel-TiPipeline, reviewed by Galadriel (v2)

---

## 1. What the PR Introduces

Galadriel's review (docs/reviews/pr-review-15064785-v2.md) identified ~150+ files. The **agent/script layer** (our primary interest) consists of:

### Agent Definition
| File | Purpose |
|------|---------|
| `.github/agents/tiexpert.agent.md` | GitHub Copilot agent definition with YAML frontmatter, routing table, and tool selection (`shell`, `read`, `search`). Routes commands to validation scripts via dot-sourcing of `ti-config.ps1` and `ti-helpers.ps1`. |

### Configuration & Helpers (2 files)
| File | Purpose |
|------|---------|
| `.github/scripts/ti-config.ps1` | Shared configuration: PPE subscription ID, workspace ID, tenant ID, ingestion URL. Dot-sourced by all validation scripts. |
| `.github/scripts/ti-helpers.ps1` | Shared helper functions including `Poll-LAQuery` (Log Analytics polling with retry), `Get-ErrorDetail`, `Log-Result` (standardized pass/fail output). |

### Validation Scripts (6 files)
| File | What It Validates |
|------|-------------------|
| `.github/scripts/validate-stixapi.ps1` | STIX Object CRUD operations — creates, reads, updates, deletes each STIX type via ARM API. |
| `.github/scripts/validate-bulkactions.ps1` | Bulk Actions API — performs bulk edit/delete operations across 6 STIX types (indicators, threat actors, campaigns, etc.), polls for completion. |
| `.github/scripts/validate-fileimport.ps1` | File Import API — uploads STIX bundle JSON via the file import endpoint, validates ingestion into workspace. |
| `.github/scripts/validate-stixwebapi.ps1` | STIX Web API — tests the web-facing API endpoints (distinct from ARM-based STIX API). |
| `.github/scripts/validate-ingestionapi.ps1` | Ingestion API — validates the TI indicator upload/ingestion pipeline endpoint. |
| `.github/scripts/validate-all.ps1` | Orchestrator — runs all 5 validators in parallel via `Start-Job`, aggregates pass/fail counts, exits 0 (all pass) or 1 (any fail). |

### SKILL.md Files (6 files)
| File | Coverage |
|------|----------|
| `.github/skills/stix-api-operations/SKILL.md` | STIX API operations reference (Galadriel found no issues) |
| `.github/skills/bulk-actions-api/SKILL.md` | Bulk Actions API usage and mutator reference |
| `.github/skills/file-import-api/SKILL.md` | File Import API patterns and gotchas |
| `.github/skills/ingestion-api/SKILL.md` | TI indicator ingestion API guide |
| `.github/skills/stix-web-api/SKILL.md` | STIX Web API operations |
| `.github/skills/validation-orchestration/SKILL.md` | How to run the validation suite |

### C# (out of scope for our integration, but noted)
| Component | What It Does |
|-----------|-------------|
| `src/StixAPIs/Sightings/` | New STIX Sightings type implementation (~80 files) |
| `WorkspaceStatusUpdaterServices` | Cleanup and status change handling services |

---

## 2. Relevance to Elrond's Research Work

When I research TI-related topics, I need to understand how the APIs behave, what validation patterns exist, and what's documented. Here's how Sagi's tools map to my needs:

| My Research Need | Sagi's Tool | Relevance | Notes |
|-----------------|-------------|-----------|-------|
| Understanding STIX API behavior (types, fields, validation rules) | `stix-api-operations/SKILL.md` | 🟢 **HIGH** | Best reference I've seen for STIX API operations. Galadriel confirmed "comprehensive and clear, no issues found." |
| Understanding bulk action capabilities and limitations | `bulk-actions-api/SKILL.md` | 🟢 **HIGH** | Documents mutator semantics, `SetTrue`/`SetFalse` behavior, which fields support which operations. Critical for understanding indicator lifecycle (revocation, confidence updates). |
| Understanding file import flow | `file-import-api/SKILL.md` | 🟡 **MEDIUM** | Relevant when researching STIX bundle ingestion failures (like ICM 51000000943039 pattern_type bug). |
| Understanding ingestion pipeline | `ingestion-api/SKILL.md` + `validate-ingestionapi.ps1` | 🟢 **HIGH** | Directly relevant to TAXII ingestion research (ICM 21000000951041) and upload API bugs. |
| Verifying TI pipeline health in PPE | `validate-all.ps1` orchestrator | 🟡 **MEDIUM** | Could be adapted to verify pipeline behavior during research, but PPE-specific config limits immediate use. |
| Understanding STIX Sightings type (new) | `src/StixAPIs/Sightings/` | 🟡 **MEDIUM** | New STIX type — will matter when sightings-related CRIs arrive. |

**Bottom line for Elrond:** The **SKILL.md files are gold** for research. They're the best API documentation I've seen for TI pipeline operations, better than the scattered TSGs and eng.ms pages. I would reference these before doing any TI API research.

---

## 3. Relevance to Aragorn's Investigations

Cross-referencing against our active investigations (TASK-INDEX.md):

| Active Investigation | Sagi's Tool | How It Helps |
|---------------------|-------------|--------------|
| **ICM 51000000954460** — Revoked indicators triggering alerts | `bulk-actions-api/SKILL.md` | Documents the `revoked` field's `SetTrue`/`SetFalse` behavior — directly relevant to understanding why revoked indicators aren't properly handled. The DOC-2 bug (SetFalse ignored for revoked) is itself evidence of the revocation lifecycle gap. |
| **ICM 51000000943039** — Upload API pattern_type override | `validate-stixapi.ps1` + `stix-api-operations/SKILL.md` | The validation script creates/reads/updates STIX objects and could be adapted to reproduce the pattern_type override bug in PPE. The SKILL.md documents expected field behavior. |
| **ICM 21000000951041** — Azure Gov TAXII ingestion shortfall | `validate-ingestionapi.ps1` + `ingestion-api/SKILL.md` | The ingestion validator tests the upload pipeline endpoint; the SKILL.md documents expected behavior. Could help verify if the GZIP encoding issue is specific to TAXII or affects all ingestion paths. |
| **ICM 21000000917983** — Deleted watchlist items persist | `validate-bulkactions.ps1` | The bulk delete validation could be adapted to test deletion propagation timing in PPE, giving us baseline data for the "eventual consistency" hypothesis. |
| **ICM 766712513** — ARM Watchlist API errors (WEU-402) | Not directly applicable | Sagi's tools focus on TI STIX APIs, not Watchlist ARM APIs. Different service path. |

**Bottom line for Aragorn:** The validation scripts are **investigation accelerators** — they provide ready-made API interaction patterns that Aragorn could adapt for hypothesis testing. Currently, Aragorn lists queries but doesn't execute them (per my gap analysis). These scripts show exactly how to authenticate, call, poll, and validate TI APIs.

---

## 4. Integration Recommendations

### Option Analysis

| Option | Pros | Cons | Recommendation |
|--------|------|------|----------------|
| **A. Clone repo locally** | Already done (`C:\dev\ti\Sentinel-TiPipeline` exists in repo-map) | PR is on a feature branch, not merged yet | ✅ **Already available** — just need to fetch the branch |
| **B. Reference scripts by path in prompts** | Zero setup, agents can `read` files | Requires branch checkout; paths are fragile | ✅ **Do this** for SKILL.md files |
| **C. Copy validation scripts into squad toolkit** | Local control, can fix bugs ourselves | Drift from upstream; maintenance burden | ❌ **Don't do this** — Sagi's scripts will evolve |
| **D. Add/update repo in repo-map.json** | Already there | Entry exists but doesn't mention agent/skills content | ✅ **Update description** to note the TiExpert agent tools |

### Specific Recommendations

**R1. Update repo-map.json entry for Sentinel-TiPipeline (P0)**

The repo is already in `repo-map.json` (entry #18), but its description says "Customer Threat Intelligence Pipeline — code, tests, and deployment scripts." It should be updated to mention the TiExpert agent, validation scripts, and SKILL.md files so Aragorn and Elrond know to look there.

Proposed update:
```json
{
  "name": "Sentinel-TiPipeline",
  "path": "C:\\dev\\ti\\Sentinel-TiPipeline",
  "defaultBranch": "users/joniba/aspire-bdd",
  "description": "Customer TI Pipeline — code, tests, deployment. Also contains TiExpert agent (.github/agents/), 5 PowerShell validation scripts (.github/scripts/validate-*.ps1), 6 SKILL.md API references (.github/skills/), and shared TI config/helpers.",
  "relevance": "service",
  "agentBranch": "features/sagimarus/tiexpertagent",
  "agentNotes": "TiExpert tools on feature branch — not yet merged. Has known bugs (BUG-1 poll loop, BUG-2 exit code, BUG-3 silent swallow). SKILL.md files are safe to reference; scripts need bug fixes before automated use."
}
```

**R2. Fetch Sagi's branch for read-only reference (P0)**

Run `git fetch origin features/sagimarus/tiexpertagent` in the Sentinel-TiPipeline repo so agents can read the SKILL.md files without checking out the branch.

**R3. Reference SKILL.md files in Aragorn's investigation prompts (P1)**

When Aragorn investigates TI-related CRIs, his investigation prompt should include:
```
For TI API reference, read these files from Sentinel-TiPipeline (branch: features/sagimarus/tiexpertagent):
- .github/skills/stix-api-operations/SKILL.md — STIX API operations
- .github/skills/bulk-actions-api/SKILL.md — Bulk actions and mutators
- .github/skills/ingestion-api/SKILL.md — TI indicator ingestion
```

This gives Aragorn API context that currently only exists in scattered TSGs.

**R4. Do NOT automate execution of validation scripts yet (P2 — after bugs fixed)**

The scripts have 3 confirmed bugs and 1 security concern:
- BUG-1: Poll loop never exits on Failed state (wastes 15 min)
- BUG-2: Token failure doesn't set exit code (silent failures in automation)
- BUG-3: Poll-LAQuery silently swallows exceptions (7.5 min silent waits)
- SEC-1: Hardcoded PPE subscription/workspace IDs

Until Sagi fixes these and the PR merges, do NOT incorporate the scripts into automated workflows. Reading them as reference is safe; executing them as part of investigation automation is not.

**R5. Add TI validation patterns to Elrond's research toolkit (P2)**

After the PR merges with fixes, create a reference note in `docs/catalogs/` documenting:
- What each validation script tests
- How to adapt them for investigation scenarios
- Which scripts map to which CRI types

---

## 5. Risk Assessment

### Safe to Use Now (read-only reference)
| Asset | Risk Level | Notes |
|-------|-----------|-------|
| `stix-api-operations/SKILL.md` | 🟢 **Safe** | Galadriel: "comprehensive and clear, no issues found" |
| `bulk-actions-api/SKILL.md` | 🟡 **Safe with caveat** | DOC-2 inconsistency on `revoked`/`SetFalse` — note when referencing |
| `file-import-api/SKILL.md` | 🟡 **Safe with caveat** | DOC-3 variable name mismatch — note when referencing |
| `ingestion-api/SKILL.md` | 🟢 **Safe** | No issues found |
| `ti-config.ps1` | 🟢 **Safe to read** | PPE values — useful as config reference. Don't copy/commit elsewhere. |
| `ti-helpers.ps1` | 🟡 **Safe to read** | BUG-3 in `Poll-LAQuery` — good pattern but fix the error handling before adapting |

### NOT Safe for Automated Execution
| Asset | Risk Level | Why |
|-------|-----------|-----|
| `validate-bulkactions.ps1` | 🔴 **BUG-1** | Poll loop wastes 15 min on failure; masks real error state |
| `validate-stixapi.ps1` | 🔴 **BUG-2** | Token failure exits with code 0; automation thinks it succeeded |
| `tiexpert.agent.md` | 🔴 **BUG-2** | Same token failure issue |
| `validate-all.ps1` | 🟡 **Inherited** | Orchestrates buggy scripts; results unreliable if children have bugs |

### Not Assessed (Out of Scope)
| Asset | Notes |
|-------|-------|
| C# Sightings implementation | ~80 files, not described in PR description, spot-checked by Galadriel only |
| WorkspaceStatusUpdaterServices | Not described in PR description, no review coverage |
| Deployment templates/YAML | Infrastructure changes, not relevant to our agent workflows |

---

## 6. Summary of Actions

| # | Action | Owner | Priority | Depends On |
|---|--------|-------|----------|------------|
| 1 | Update Sentinel-TiPipeline entry in `repo-map.json` | Gimli | P0 | — |
| 2 | Fetch Sagi's branch in local Sentinel-TiPipeline clone | Gimli | P0 | — |
| 3 | Add SKILL.md references to Aragorn's TI investigation prompts | Gandalf | P1 | #2 |
| 4 | Wait for Sagi's bug fixes + PR merge | — | P2 | External |
| 5 | After merge: create TI validation reference catalog | Bilbo | P2 | #4 |
| 6 | After merge: evaluate validation scripts for automated investigation use | Elrond | P2 | #4, #5 |

---

*Proposed by Elrond (Researcher) · 2025-07-10 · Based on Galadriel's review of PR 15064785*


---

# Elrond's Worktree Article Insights — Key Findings
**From:** Elrond (Researcher)  
**Re:** Tamir Dresher's Git Worktrees Article Analysis  
**Date:** 2025  
**Status:** Inbox — Ready for Team Decision

---

## EXECUTIVE SUMMARY

**The Problem:** Jonathan's team spawned 8 agents in parallel. Each agent did `git checkout -b` in the same working tree. Later agents' branch switches clobbered earlier agents' uncommitted work. Local state was destroyed (4 stashes, random branch, uncommitted files), though PRs did push successfully.

**Root Cause:** Single checkout + concurrent branch switches = state destruction.

**Tamir's Solution:** Git worktrees. Each parallel task gets its own directory with its own branch. No checkout switches in the main working tree.

**Squad's Aspirational Gap:** squad.agent.md documents worktree support (lines 562-600), but the feature is **not activated** in practice. Agents still use the old single-checkout pattern.

**The Fix:** Coordinator should create isolated worktrees before spawning agents. Agents work only in their assigned worktree. No branch switching needed.

---

## KEY INSIGHT #1: Tamir's Article Is Manual, Not Automated

Tamir describes a **manual VS Code workflow:**
1. Open Repositories view
2. Click UI to create worktree
3. Open worktree in new window
4. Launch AI agent manually in that window
5. Alt+Tab to supervise multiple windows

**This is NOT how Squad should work.** Squad agents are spawned programmatically. The coordinator needs to **automate** the worktree creation step before spawning agents.

---

## KEY INSIGHT #2: Squad's Worktree Documentation Is Designed but Dormant

**Lines 562-600 of squad.agent.md describe two strategies:**
1. `worktree-local` — Each worktree has isolated `.squad/` state (recommended for concurrent work)
2. `main-checkout` — All worktrees share `.squad/` state from main (not safe for concurrent sessions)

**The logic is documented in detail:**
- How to detect you're in a worktree
- How to resolve the team root
- How to handle state merges with `merge=union` driver in `.gitattributes`

**But it's never enforced.** Agents don't know they're in a worktree. The coordinator doesn't create worktrees before spawning. It's a design that was planned but not activated.

---

## KEY INSIGHT #3: The Fix Is Straightforward — Two Options

### Option A: Coordinator-Managed Worktrees (Recommended)

**Pre-spawn (Coordinator does this before spawning agents):**
```bash
git worktree add ./worktrees/squad.issue-1 -b squad/issue-1 main
git worktree add ./worktrees/squad.issue-2 -b squad/issue-2 main
...
```

**Agent Spawn Instructions:**
- Pass `WORKTREE_PATH` to each agent
- Instruct: "Work only in {WORKTREE_PATH}. Do NOT switch branches."
- Agents commit and push from that worktree

**Post-agent (Coordinator cleanup):**
```bash
git worktree remove ./worktrees/squad.issue-1
git worktree remove ./worktrees/squad.issue-2
...
```

**Advantages:**
- Deterministic setup/cleanup
- Agents are simpler (no worktree lifecycle logic)
- Coordinator has full visibility
- Scales to N agents easily

**Implementation effort:** ~200 lines in coordinator spawn logic

### Option B: Agent-Managed Worktrees

**Agents create their own worktrees:**
```bash
git worktree add {TEAM_ROOT}/worktrees/squad.{issue-id} -b squad/{issue-slug} main
# ... work ...
git worktree remove {TEAM_ROOT}/worktrees/squad.{issue-id}
```

**Advantages:**
- Agents are self-contained
- No pre-coordination needed
- Works for async/unplanned agents

**Disadvantages:**
- More complex agent logic
- Harder to debug if worktree creation fails
- Less visibility into parallel execution

---

## KEY INSIGHT #4: Why Jonathan's Problem Happened (And How It's Fixed)

**What Broke:**
```
Agent 1: git checkout -b squad/issue-1  ← Creates branch, works
Agent 2: git checkout -b squad/issue-2  ← Switches to new branch, clobbers Agent 1's uncommitted files
Agent 3-8: Each checkout destroys previous agent's state
Result: Workspace left on random branch with stashes and confusion
```

**How Worktrees Fix It:**
```
Coordinator: git worktree add worktrees/squad.issue-1 -b squad/issue-1 main
Coordinator: git worktree add worktrees/squad.issue-2 -b squad/issue-2 main
Coordinator: spawn Agent 1 in worktrees/squad.issue-1/
Coordinator: spawn Agent 2 in worktrees/squad.issue-2/

Agent 1: Works in worktrees/squad.issue-1/ — no branch switching needed
Agent 2: Works in worktrees/squad.issue-2/ — completely isolated
Agent 3-8: Each in their own worktree directory

Result: Clean parallel execution, no state collisions
```

---

## KEY INSIGHT #5: The Implementation Checklist

To activate worktree support in Squad (moving from aspirational to operational):

- [ ] **Coordinator:** Add pre-spawn worktree creation logic (Option A recommended)
- [ ] **Agent Spawn Prompt:** Pass `WORKTREE_PATH` and instruct agents not to switch branches
- [ ] **Coordinator:** Add post-agent worktree cleanup (after PR merged/abandoned)
- [ ] **Documentation:** Update squad.agent.md "Worktree Awareness" section from "designed" to "ACTIVE"
- [ ] **Error Handling:** Log failures if worktree creation or removal fails
- [ ] **Troubleshooting:** Document worktree lifecycle and common failures
- [ ] **Testing:** Run 8 agents in parallel to verify no state collisions

---

## KEY INSIGHT #6: Tamir's Core Contribution (For Squad's Adaptation)

Tamir's article proves that **worktrees are the right pattern for parallel AI work**. The approach scales naturally:
- 1 worktree = manual setup in VS Code
- 8 worktrees = coordinator batch setup
- 80 worktrees = same coordinator logic, just parameterized

The manual UI Tamir describes is just the **visible version** of what Squad can automate at the coordinator level.

---

## OPEN QUESTIONS FOR THE TEAM

1. **Which strategy should Squad use?** Option A (coordinator-managed) or Option B (agent-managed)?
   - Recommendation: Option A. Cleaner, more deterministic, less burden on agents.

2. **Should we activate `worktree-local` strategy immediately, or start with `main-checkout`?**
   - Recommendation: `worktree-local` is recommended by squad.agent.md and aligns with Tamir's approach. It's safe for concurrent work if each agent is isolated by worktree.

3. **How urgent is this?** Jonathan's team already pushed successful PRs—the issue was local state cleanup, not correctness.
   - Recommendation: Implement this before the next parallel agent spawning event to prevent repeated state confusion.

4. **Should agents be told they're in a worktree, or should they just work as normal?**
   - Recommendation: Agents should be told (`WORKTREE_PATH` in spawn prompt). Keeps agents honest about their working directory and makes debugging easier.

---

## NEXT STEPS

1. **Team decision:** Which approach (A or B)?
2. **Design review:** Coordinator pre-spawn and post-agent cleanup logic
3. **Agent spawn prompt update:** Include `WORKTREE_PATH` and worktree instructions
4. **Testing:** Parallel agent test with 8 agents to verify no state collisions
5. **Documentation update:** Mark squad.agent.md Worktree Awareness as "ACTIVE—Enforced by Coordinator"

---

## APPENDIX: Tamir's Exact Procedure (From Article)

**Phase 1:** Enable Repositories View in VS Code Source Control
**Phase 2:** Access Worktrees submenu from repository context menu
**Phase 3:** Create worktree (wizard: select branch, choose location, open in new window)
**Phase 4:** Repeat for each feature
**Phase 5:** Launch AI agent in each window
**Phase 6:** Alt+Tab between windows to supervise

**Key difference from Squad:** Tamir is manual. Squad should automate phases 1-3 at the coordinator level, leaving phases 5-6 for human supervision (or fully automated if desired).


---

# Decision Needed: Enforce Worktree-Per-Agent for Parallel Spawning

**Author:** Elrond (Researcher)  
**Date:** 2026-03-22  
**Status:** Proposed  
**Severity:** High (blocking concurrent agent work)

## Context

8 agents spawned in parallel today (Gimli, Elrond, Bilbo, Gandalf, Aragorn, etc.), all to the same working tree. Each ran `git checkout -b squad/{issue}-{slug}`, causing race conditions. Result:

- Workspace left on wrong branch (`squad/4-format-summary`)
- 4 git stashes from failed agents
- Uncommitted files scattered everywhere
- Main branch never restored
- **BUT:** All PRs successfully pushed to GitHub (remote operations are atomic)

Squad's governance template (`squad.agent.md`) documents two worktree strategies (worktree-local vs main-checkout) **but does not enforce worktree creation in the agent spawn flow**. This is the gap.

## The Problem

**Root cause:** No coordinator-level guard prevents multiple agents from running in the same working tree.

**Current behavior:**
- Coordinator spawns agents
- Each agent runs `git checkout -b ...` on the shared tree
- Branches overwrite; stashes accumulate
- Workspace becomes unusable

**Why it happened:**
- Squad's spawn template predates multi-agent parallelism patterns
- Documentation is advisory only, not enforced
- No pre-spawn validation, no auto-worktree creation, no post-spawn cleanup

## Decision Required

**Should pa-squad enforce worktree-per-agent isolation for parallel agent spawns?**

### Option A: Enforce Worktree-Per-Agent (Recommended)

**Action:**
1. Create `.squad/orchestration/spawn-parallel-agents.ps1` that:
   - Takes list of GitHub issue numbers
   - For each: `git worktree add .squad/worktrees/squad-{issue}-{slug} main`
   - Spawns agents with `AGENT_WORKTREE: {path}` in context
   - Post-spawn: removes all worktrees

2. Update `.squad/config.yml`:
   ```yaml
   parallelism_strategy: worktree-local
   max_concurrent_agents: 8
   ```

3. Mandate in Gandalf's (Coordinator) charter:
   - When spawning N > 1 agent concurrently, ALWAYS use `spawn-parallel-agents.ps1`
   - Never spawn multiple agents to the same tree without worktrees

**Pros:**
- Zero race conditions on branch checkout
- Each agent gets isolated disk state
- `.squad/` state merges cleanly via union driver
- Scales to 8+ parallel agents

**Cons:**
- O(N) disk overhead (worktrees clone git object database; ~100MB each)
- ~500ms setup/teardown per worktree
- Not suitable for large scale (100+ agents) without optimization

### Option B: Stash-Before-Switch (Lower Parallelism)

**Action:**
1. Create `.squad/orchestration/spawn-sequential-agents.ps1` that:
   - Spawns agent #1
   - When agent #1 finishes, stash its work: `git stash push -m "agent-1"`
   - Spawn agent #2
   - When agent #2 finishes, pop stash: `git stash pop`

**Pros:**
- Minimal disk overhead
- Simple logic

**Cons:**
- Only works for 2–3 agents sequentially
- Not parallel
- Stash pop can fail if conflicts
- Doesn't solve today's problem (8 agents spawned at once)

### Option C: Do Nothing (Status Quo)

**Action:** Keep current behavior; rely on users to manually manage worktrees

**Pros:**
- No code changes
- Maximum flexibility

**Cons:**
- Problem repeats every time someone tries to parallelize
- Workspace pollution, confusion, wasted time
- Remote PRs succeed but local state is trash

## Recommendation

**Adopt Option A: Enforce Worktree-Per-Agent**

**Reasoning:**
1. Tamir's multi-machine experiments (squad-tetris) used worktree isolation (3 separate Codespaces, each with own repo clone)
2. Squad's template already documents worktree-local strategy; we're just enforcing it
3. Disk overhead is acceptable for typical parallelism (8 agents = ~800MB, one-time, cleaned up after)
4. O(500ms) worktree overhead is faster than a full git clone and insignificant vs agent execution time
5. The gap between documentation and enforcement is exactly where bugs hide

## Implementation Path

### Phase 1 (Immediate - This Session)

1. Create `.squad/orchestration/spawn-parallel-agents.ps1` with:
   - Pre-spawn worktree creation loop
   - Spawn manifest generation (for Scribe)
   - Post-spawn cleanup loop

2. Document in `.squad/decisions.md`:
   ```markdown
   ## Decision: Worktree-Per-Agent for Parallel Spawning
   - Adopted: 2026-03-22
   - Pattern: Each agent runs in .squad/worktrees/squad-{issue}-{slug}
   - Enforcement: spawn-parallel-agents.ps1 mandatory for N > 1
   - Cleanup: Automatic post-spawn
   ```

3. Update Gandalf's charter (`.squad/agents/gandalf/charter.md`):
   - Add: "When spawning N > 1 concurrent agents, use spawn-parallel-agents.ps1"

### Phase 2 (Short-term - Next Session)

1. Implement drop-box pattern for shared files:
   - Agents write decisions to `.squad/decisions/inbox/{agent}-{slug}.md`
   - Scribe merges inbox after batch → `.squad/decisions.md`

2. Update `.squad/config.yml` with:
   ```yaml
   parallelism_strategy: worktree-local
   max_concurrent_agents: 8
   decision_inbox_pattern: .squad/decisions/inbox/{agent}-{slug}.md
   ```

3. Document in README (if exists) or `.squad/README.md`:
   - "For parallel agent work, see `.squad/orchestration/spawn-parallel-agents.ps1`"

### Phase 3 (Framework - Future)

- Propose to Squad team: add "Parallel Agent Spawning" section to squad.agent.md template
- Create Scribe feature: auto-detect multi-agent spawn, auto-create worktrees
- Create orchestration plugin for reuse across squads

## Risk Assessment

**Implementation risk: Low**
- Pattern is well-documented (Tamir's work)
- Squad template already supports it
- Worktree setup/cleanup is standard git, no custom logic

**Adoption risk: Low**
- Mandatory only for multi-agent scenarios
- Solo agents (N=1) unaffected
- Backward compatible (main-checkout still option for sequential work)

**Operational risk: Medium**
- O(N) disk usage could be high for large N (mitigated by cleanup)
- Cross-platform worktree behavior differs (Windows vs Linux) — need testing
- Stale worktrees might accumulate if cleanup fails — need guard

## Acceptance Criteria

- [ ] spawn-parallel-agents.ps1 created and tested with 4 concurrent agents
- [ ] All 4 agents successfully check out branches without conflicts
- [ ] Workspace returns to main branch after spawn cleanup
- [ ] No stashes or uncommitted files left behind
- [ ] GitHub PRs merge cleanly with union merge driver
- [ ] Documentation added to `.squad/decisions.md` and Gandalf's charter
- [ ] Pattern tested end-to-end with real GitHub issues

## Open Questions

1. Should worktree cleanup be automatic or manual? (Recommend: automatic, via finally block in PowerShell)
2. Should agents be aware of their worktree path, or should it be transparent? (Recommend: transparent — they just see AGENT_WORKTREE in env)
3. Should we implement drop-box pattern (decisions/inbox) at the same time? (Recommend: yes, Phase 2)
4. Should this be enforced in squad.agent.md template globally? (Recommend: yes, but optional per-project)

## Decision

**Choose:** Option A: Enforce Worktree-Per-Agent

**Owner:** Gandalf (Coordinator) — responsible for enforcing spawn-parallel-agents.ps1 usage  
**Tracking:** Create GitHub issue #N on pa-squad: "Implement spawn-parallel-agents.ps1 worktree orchestration"

---

**Next:** Jonathan reviews, approves, and assigns implementation to Gimli (infrastructure) or another agent with scripting expertise.


---

---
title: "Separate Bilbo's Charter from System Design"
date: 2026-03-22
author: gandalf
documentarian: bilbo
category: decision
tags:
  - decision
  - squad-infra
  - gandalf
status: final
related_docs:
  - .squad/agents/bilbo/charter.md
  - .squad/skills/knowledge-management/SKILL.md
  - docs/SYSTEM.md
---

# Decision: Separate Bilbo's Charter from System Design

## Status
**Accepted**

## Context

Bilbo's original charter was a single 536+ line document that mixed two distinct concerns:

1. **Permanent identity & principles** — WHO Bilbo is, what Bilbo does, how Bilbo thinks
   - Core principles (5 foundational values)
   - Workflow triggers and escalation rules
   - Voice and collaboration patterns
   - Delegation boundaries

2. **Organizational system design** — HOW Bilbo implements knowledge management
   - Folder hierarchy (8-category structure)
   - Tagging taxonomy (type/domain/agent/status with specific values)
   - Index system specifications (INDEX.md, TAGS.md, RECENT.md formats)
   - Document structure templates
   - Quality standards checklists
   - Naming conventions

The problem: **This mixed architecture freezes organizational decisions in the charter.** If Bilbo discovers a better categorization, tagging scheme, or index format, the charter must be rewritten — which implies changing Bilbo's permanent identity. This conflates two separate concerns and creates artificial resistance to improving the system.

## Decision

**Separate the charter from the system design:**

1. **Charter (`.squad/agents/bilbo/charter.md`)** — Identity only
   - Reduced from 536+ to ~250 lines
   - Keeps: identity table, core principles, ownership, workflow triggers, quality standards, delegation, escalation, collaboration, voice
   - Removes: all system design details
   - Adds: reference to `.squad/skills/knowledge-management/SKILL.md`
   - Frequency: Updated rarely (only when Bilbo's core purpose or principles change)

2. **Skill (`.squad/skills/knowledge-management/SKILL.md`)** — System design (new file)
   - ~400+ lines of comprehensive organizational design
   - Contains: folder hierarchy, category definitions, naming conventions, tagging taxonomy (with specific values), index specifications, document templates, quality standards, event documentation pipeline
   - Status: Marked "Low Confidence (First Design)" to signal that this is a starting point
   - Includes: Evolution Log section to track changes over time
   - Frequency: Evolves as Bilbo learns what works (referenced in decisions/inbox when Bilbo proposes improvements)

3. **Public Guide (`docs/SYSTEM.md`)** — Human-facing reference (new file)
   - ~150 lines, non-exhaustive, for humans and agents
   - Quick navigation guide, category explanations, how to find things, tagging overview
   - Links to `.squad/skills/knowledge-management/SKILL.md` for detailed spec

## Consequences

### Positive
- **Charter remains stable** — Bilbo's identity and principles don't require rewriting when the system evolves
- **System design is mutable** — Bilbo can propose improvements to organization, tagging, indexing without implying identity changes
- **Clear separation of concerns** — what Bilbo IS (charter) vs. what Bilbo DOES (skill) vs. how to USE it (public guide)
- **Reduced charter friction** — charter stays short (~250 lines) and focused; it's not a "system bible"
- **Better evolution story** — Evolution Log in SKILL.md documents how the system has changed and why

### Negative
- **Three files instead of one** — Maintenance burden slightly higher, but only when system design changes (rare) or charter principles change (also rare)
- **Coordination required** — If Bilbo's role or principles fundamentally shift, the charter AND the skill may need updates (but this is rare and intentional)
- **Skill is "low confidence"** — First iteration may need refinement based on real-world use (this is acknowledged in the SKILL.md frontmatter)

## Alternatives Considered

### 1. Keep the single charter, freeze the system design
**Rejected.** Keeping organizational design in the charter implies it's permanent identity. This creates artificial resistance to improving the system and makes the charter unwieldy (~536+ lines).

### 2. Move system design to a README in docs/
**Considered but rejected.** READMEs are for projects; system design belongs in the skill system (the `.squad/skills/` tree) so it can be versioned, evolved, and tracked alongside other agent capabilities.

### 3. Use a Git history / wiki approach instead
**Rejected.** Git history is hard to query; a wiki is external and harder to version-control. The skill system is the right place.

## Implementation

Three files created:

1. **`.squad/agents/bilbo/charter.md`** — Rewritten to focus on identity and principles only (~250 lines)
2. **`.squad/skills/knowledge-management/SKILL.md`** — New comprehensive system design file (~400+ lines)
3. **`docs/SYSTEM.md`** — New public-facing quick reference (~150 lines)

All three files linked and cross-referenced. No existing documents moved or restructured; this is purely architectural.

## Related Decisions

- TBD: How to handle legacy documents that predate this system (proposed future decision)
- TBD: Tagging standards for external agents' documents (proposed future decision)

---

**Approved by:** Gandalf  
**Decision ID:** gandalf-bilbo-charter-skill-separation  
**Date:** 2026-03-22


---

# Decision: Bilbo Knowledge Architecture Upgrade

**Author:** Gandalf (Lead)  
**Date:** 2026-03-23  
**Status:** Accepted  
**Priority:** HIGH (Jonathan explicitly requested thorough charter before Bilbo starts work)

## Context

Jonathan wants Bilbo to evolve from a generic documentarian into a **knowledge architect**. The current `docs/` folder is a flat collection of 6 files with no organizational system — no frontmatter, no tags, no indexes, no naming conventions. Documents are hard to find, impossible to cross-reference, and disconnected from the issues and agents that produced them.

Existing documents span at least 5 distinct types:
- ICM investigation reports (2): `icm-766712513-investigation.md`, `icm-investigations/`
- Research reports (2): `worktree-article-analysis.md`, `worktree-parallelism-research.md`
- Capability analyses (1): `aragorn-icm-capability-upgrade.md`
- Reference catalogs (1): `squad-skills-catalog.md`
- Plus audit reports and decision records exist in concept but not yet as docs

Without structure, this will only get worse as the squad produces more knowledge artifacts.

## Decision

**Upgrade Bilbo's charter to embed a complete knowledge architecture system.** The charter now defines:

### 1. Folder Hierarchy (8 categories)

```
docs/
├── INDEX.md / TAGS.md / RECENT.md    # Three index files
├── investigations/                    # ICM & livesite reports
├── research/                          # Research & deep dives
├── decisions/                         # Architecture decisions (ADR-style)
├── audits/                            # Systematic assessments
├── guides/                            # How-tos & runbooks
├── catalogs/                          # Reference inventories
├── feedback/                          # Product feedback & RFCs
└── tools/                             # Tool documentation
```

### 2. Tagging Taxonomy (4 dimensions)

Every document gets tags in YAML frontmatter across 4 required dimensions:
- **Type** (1 required): matches the folder category
- **Domain** (1+ required): 12 domain tags covering `icm`, `livesite`, `architecture`, `squad-infra`, `tooling`, `workflow`, `security`, `research-method`, `product`, `azure`, `teams`, `git`
- **Agent** (1 required): which agent produced the content
- **Status** (1 required): `draft`, `final`, `superseded`, `archived`

New tags require a decision record — no ad-hoc tag invention.

### 3. Triple Index System

- **`docs/INDEX.md`** — Master index grouped by category, with date/author/status/summary per doc. Fixed category order, newest first within each.
- **`docs/TAGS.md`** — Reverse index by tag. For each tag, lists every document carrying it. Alphabetical tags, newest first within each.
- **`docs/RECENT.md`** — Last 20 documents by date. Rolling window. Oldest falls off at 21.

All three indexes are updated on every document add/modify/remove.

### 4. Event Documentation Pipeline

5-stage pipeline triggered when any agent produces knowledge:
1. **Receive** — content arrives from another agent
2. **Classify** — determine category, generate filename, apply frontmatter
3. **Tag** — apply type + domain + agent + status tags (minimum 4)
4. **Index** — update all three index files
5. **Commit** — standardized commit message: `docs({category}): {description}`

### 5. Document Templates

Structured templates for each document type:
- **Investigation**: Executive Summary → Timeline → Impact → Investigation (data, hypotheses, root cause) → Remediation → Classification
- **Research**: Executive Summary → Context → Methodology → Findings → Analysis → Recommendations
- **Decision**: Status → Context → Decision → Consequences → Alternatives
- **Audit**: Scope → Methodology → Findings table → Summary → Action Items

### 6. Quality Standards

Severity scale for documentation issues (Critical/High/Medium/Low) and a pre-commit checklist covering frontmatter, tags, folder placement, naming, structure, and index updates.

## Reasoning

**Why not a simpler system?** Jonathan explicitly asked for thorough design before Bilbo starts work. A simpler "just write docs" approach is what Bilbo had before — and it produced a flat, unorganized folder. The overhead of frontmatter + tags + indexes is minimal per-document but compounds into massive findability gains as the knowledge base grows.

**Why YAML frontmatter?** It's the standard for markdown-based knowledge systems (Jekyll, Hugo, VitePress, Obsidian). Machine-parseable, human-readable, and trivially extractable for index generation.

**Why three indexes instead of one?** Different access patterns: "What do we have?" (INDEX.md by category), "What relates to X?" (TAGS.md by tag), "What's new?" (RECENT.md by date). One index trying to serve all three is worse at each.

**Why embedded in the charter, not a separate spec?** Bilbo's charter IS the spec. Every time Bilbo spawns, the charter loads. If the knowledge architecture lived in a separate doc, Bilbo might not read it. The charter is the single source of truth for how Bilbo operates.

**Why not automate index generation?** We could build a script (Gimli's domain), but the system should work without tooling first. Manual index maintenance forces Bilbo to understand the system. Automation can come later as an optimization.

## Consequences

### Positive
- Every document is findable via three different paths (category, tag, recency)
- Cross-referencing via tags connects related knowledge across categories
- New agents can discover existing knowledge without tribal knowledge
- Status tracking prevents stale docs from being treated as current
- Templates ensure consistency across all document types
- Legacy docs can be migrated incrementally

### Negative
- Every document creation requires updating 3 index files (overhead)
- New tag additions require a decision record (friction by design)
- Existing 6 docs need migration to the new system (one-time cost)
- Frontmatter requirements mean Bilbo must post-process raw content from other agents

## Alternatives Considered

1. **Flat docs/ with just a README** — Rejected. This is what we have now. It doesn't scale.
2. **Wiki-style system (VitePress, Docusaurus)** — Rejected for now. Over-engineered for a team of agents. Can layer on later.
3. **Database-backed index (SQLite, JSON)** — Rejected. Markdown indexes are human-readable, git-diffable, and require no tooling.
4. **Tags in filenames** — Rejected. Makes filenames unreadable. Frontmatter is the right place.
5. **Single unified index** — Rejected. Trying to serve browse-by-category, find-by-tag, and see-recent in one file makes all three worse.

## Next Steps

1. ✅ Charter written and committed
2. Bilbo migrates existing 6 docs into the new folder structure (new issue)
3. Bilbo creates the three index files from scratch (same issue)
4. Bilbo documents this architecture decision as a `decisions/` doc (meta!)


---

### 2026-03-22T18:00:00Z: Failure Recovery Pipeline — Design Decision

**Author:** Gandalf (Lead)
**Status:** Active

---

## Context

Galadriel attempted a PR code review and could not read file contents — the `ado-repo_get_file_contents` tool does not exist in the ADO MCP. Rather than flagging this immediately, the gap would have gone unnoticed without this decision. That near-miss is the trigger for this decision.

The squad needs to self-correct. Jonathan should not have to discover broken features.

---

## Decision

The squad adopts a **Failure Recovery Pipeline** — a formal, self-healing protocol for handling agent failures without requiring Jonathan's involvement.

### Pipeline

```
Failed Agent → signal → Gandalf (triage) → Elrond (research, opus-4.6) → Gandalf (review)
  → Ralph (route implementation) → Gimli/assigned (build) → Galadriel (review)
  → Original Agent (retry original task)
```

### Signal Protocol

Every agent that fails a task writes a failure report to:
```
.squad/decisions/inbox/{agent}-failure-{slug}.md
```

Failure report format and slug convention are defined in `.squad/failure-recovery.md`.

### Notification Rules

Jonathan is only notified when:
1. **Elrond cannot find a solution** — the squad's research capability is exhausted. Post to Teams webhook, tag `needs-human`.
2. **Gandalf rejects Elrond's solution twice** — something is fundamentally wrong with the approach. Escalate immediately.
3. **Fix is implemented but the original task still fails** — the fix didn't work. Jonathan must know.
4. **Aragorn fails during an active livesite incident** — operational urgency overrides the pipeline. Notify immediately, don't wait for Elrond's research cycle.

The squad does NOT notify Jonathan for:
- Normal failure detection and pipeline activation
- Elrond finding a solution
- Successful fix + retry

### All Agents Wired

All agents now have an "On Failure" section in their charters:
- ✅ Galadriel (original trigger)
- ✅ Gandalf (owns the pipeline)
- ✅ Elrond (special case: "no solution found" IS the escalation)
- ✅ Bilbo (documentation failures)
- ✅ Gimli (build failures)
- ✅ Aragorn (investigation failures + livesite exception)
- ✅ Scribe (mechanical failures, typically trivial)

---

## Why This Exists

The Galadriel PR review failure exposed a systemic gap: when an agent can't do something, there was no formal path to resolution. The agent either silently failed, returned partial results, or Jonathan had to manually investigate.

This pipeline closes that gap. It's modeled on incident management patterns — detect, triage, research, fix, retry — and it keeps the squad's trust contract with Jonathan: **the work gets done, autonomously, or Jonathan is told exactly why not.**

---

## Tracking

- Pipeline spec: `.squad/failure-recovery.md`
- GitHub issue: created on `jbenami_microsoft/ms-pa`
- Related charters updated: all agents
- Routing verified: `.squad/routing.md` failure recovery section


---

# Decision: IcM Scanning Architecture — Agent-Driven, Not Script-Driven

**Date:** 2026-03-22  
**Author:** Gandalf (Lead)  
**Status:** Active  
**Decision ID:** `icm-scan-agent-architecture`

## Problem

The `scripts/icm-scan.ps1` script was using `copilot -p` to query IcM:
- **Cost:** 6 premium requests per scan
- **Permission issue:** Non-interactive mode can't grant MCP tool consent (permission denied)
- **Unnecessary:** IcM MCP tools are available directly to agents

## Decision

**ICM scanning is now agent-driven, not script-driven.**

When the scheduler needs to run an IcM scan:
1. Scheduler detects `icm-scan` task is due
2. Coordinator spawns **Aragorn** (incident response agent) with task context:
   - Team ID (116041)
   - Filter criteria (sev2, sev2.5, cri, etc.)
   - Since window (default: 4 hours)
3. Aragorn calls IcM MCP tools directly:
   - `icm-search_incidents_by_owning_team_id`
   - `icm-get_incident_details_by_id`
   - (any other IcM queries needed)
4. Results logged to `.squad/scheduler.log`

**Config transparency:** `scripts/icm-scan.ps1` remains as a config script only — it prints the scan parameters but delegates execution to the agent.

## Reasoning

### Why agents, not scripts, for MCP tasks?
- **Scripts:** No MCP tool context. Would need to shell out to `copilot -p`, which:
  - Costs premium requests
  - Can't get tool consent in non-interactive mode
  - Is indirection (script → copilot process)

- **Agents:** Direct MCP context. Tools are available natively:
  - No permission prompt needed
  - No cost (MCP tools are free to agents)
  - No indirection (agent calls tool directly)

### Pattern generalization
This applies to **any task that needs MCP tools**: teams, email, IcM, knowledge, etc.

**Rule:** If a task needs an MCP tool → make it an agent task, not a script task.

## Implementation

### Changed files
- `scripts/icm-scan.ps1` — Now config-only. Prints parameters, directs user to spawn Aragorn.
- `.squad/skills/unified-scheduler/SKILL.md` — Documents agent-driven vs. script-only task types.

### Next steps
1. Create Aragorn's `icm-scan` task handler (triggered by scheduler)
2. Update `.squad/scheduler.json` to point to agent spawn instead of script execution
3. Test with real IcM data

## Exceptions
None currently. All tasks requiring MCP tools should follow this pattern.

## History
- **2026-03-22T17:50:** Jonathan raised issue: IcM scan costs 6 premium requests + permission errors
- **2026-03-22T18:00:** Gandalf analyzed architecture and proposed agent-driven approach
- **2026-03-22T18:05:** Implemented decision


---

# Triage: Squad Product Feedback Issues #44-#49

**Date:** 2026-03-22  
**Triaged by:** Gandalf  
**Source:** Jonathan onboarding feedback (filed on ms-pa repo since EMU tokens cannot write to bradygaster/squad directly)

## Summary
All 6 issues are observations about the squad CLI product surface, filed as feedback to track improvements. Routed based on ownership pattern: tool fixes → Gimli, documentation/patterns → Bilbo, product feedback → Gandalf.

## Routing Decisions

| Issue | Title | Owner | Rationale |
|-------|-------|-------|-----------|
| #44 | PRs stuck in draft — missing --ready flag | **Gimli** | CLI tool fix (add --ready flag or default behavior) |
| #45 | Project board not updating — missing OAuth scopes | **Gandalf** | External product feedback, tracked for squad project reference |
| #46 | Model defaults stuck on 4.5 — should use 4.6 when cost is equal | **Gimli** | CLI configuration change (model version selection) |
| #47 | squad-cli watch — can't take actions, confusing | **Gandalf** | External product UX/design feedback, tracked for squad project |
| #48 | Reviewer lockout design is wrong — authors should own fixes | **Bilbo** | Documentation of charter patterns and best practices |
| #49 | Default reviewer charter quality too low — needs better patterns | **Bilbo** | Documentation of reviewer charter templates and quality standards |

## Key Insight
These issues represent high-level product feedback from Jonathan's onboarding experience. They are NOT implementation tasks for this squad—rather, reference tracking for the upstream squad-skills project. Gandalf owns the product feedback meta-thread; Gimli and Bilbo own any local adaptations we choose to make.

## Next Steps
- Gimli reviews tool fixes (#44, #46) for applicability to ms-pa's ralph-watch implementation
- Bilbo documents reviewer charter best practices (#48, #49) for local Galadriel charter reference
- Gandalf maintains external feedback thread for upstream product communication


---

# Design: ICM Scan Watermark

**Issue:** #92  
**Author:** Gandalf  
**Date:** 2026-03-23  
**Status:** Approved

## Decisions

### 1. Watermark Location
**Decision:** `.squad/icm-scan-watermark.json`, gitignored (machine-local state)

**Rationale:** This is ephemeral tracking state, not configuration or shared logic. Each machine maintains its own scan history independently. Committing it would pollute git history and create merge conflicts.

### 2. Schema
```json
{
  "lastScan": "2026-03-23T14:30:00Z",
  "seenIds": ["id1", "id2", "id3"],
  "version": 1
}
```

**Rationale:**
- `lastScan` (ISO 8601): Calculate query window from this timestamp
- `seenIds` (array of strings): IcM incident IDs already processed
- `version`: Future-proof for schema changes

### 3. seenIds Rotation
**Decision:** Cap at 200 entries. When exceeded, drop oldest entries to maintain size.

**Rationale:** 
- 200 IDs at ~36 bytes each = ~7.2 KB (negligible)
- No per-ID timestamps needed (adds complexity, minimal benefit)
- Simple FIFO rotation is sufficient for deduplication window

### 4. Query Window Calculation
**Decision:**
- **First scan:** 24 hours (default)
- **Subsequent scans:** `now() - lastScan + 1 hour buffer` (always look back at least 1h before last scan)
- **Cap:** 7 days maximum (prevents runaway lookback on stale watermarks)

**Rationale:**
- Buffer prevents race conditions (incidents updated between scan cycles)
- 7-day cap handles machines offline for extended periods
- Defaults to safe (wider window) on first run

### 5. Known IDs → LLM Prompt
**Decision:** Merge `seenIds` + investigation report IDs + GitHub issue IDs. Pass to LLM prompt as "skip these incident IDs."

**Rationale:** Single source of truth in the prompt prevents LLM reprocessing, even if it could theoretically access them again.

### 6. Corruption Handling
**Decision:** If watermark JSON parse fails:
1. Log warning
2. Delete corrupted file
3. Start fresh (next scan uses 24h default)

**Rationale:** Fail-safe: corrupt state is worse than lost history. Watermark is ephemeral—safe to discard.

### 7. `-Reset` Flag
**Decision:** Clears watermark on demand. Next scan reverts to 24h default.

**Rationale:** Simple manual recovery path for debugging and state resets.

---

## Implementation Notes
- Watermark is updated **after every scan**, regardless of whether incidents were found
- Use `[System.Collections.Generic.List[string]]` in PowerShell for seenIds management (preserves order, efficient rotation)
- Log all watermark reads/writes for observability


---

## WorkIQ Patterns Skill — Shared Knowledge Asset

**Author:** Gimli
**Date:** 2026-07-22
**Issue:** #85
**Artifact:** `.squad/skills/workiq-patterns/SKILL.md`

**Decision:** Created a general-purpose WorkIQ skill at `.squad/skills/workiq-patterns/` that any agent can read at spawn time. The skill is NOT tied to any specific agent protocol (e.g., Aragorn's ICM workflow) — it's a shared reference for all agents needing Teams message context.

**Key design choices:**
1. Used the standard `.squad/templates/skill.md` format with `tools:` metadata declaring `workiq-ask_work_iq`
2. Organized by "what works / what doesn't / patterns / anti-patterns" rather than by agent use case — keeps it agent-agnostic
3. Included the URL workaround pattern prominently since that was the original failure that triggered issue #85
4. Documented indexing delay and rate-limiting as operational constraints, not just limitations

**Team impact:** Any agent charter that references Teams data retrieval should include this skill in its spawn-time reading list.


## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
