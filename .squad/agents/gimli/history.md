# Project Context

- **Owner:** Jonathan
- **Project:** pa-squad — a personal-assistant squad for everyday tasks, research, documentation, tool building, and livesite support
- **Stack:** General-purpose (not a single-stack project)
- **Created:** 2026-03-22

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

- **2026-03-23 — ICM scan watermark (#92, commit e222b57):** Rewrote `scripts/icm-scan.ps1` to be watermark-driven. Watermark at `.squad/icm-scan-watermark.json` (gitignored). Key patterns: (1) `ConvertFrom-Json` auto-parses ISO 8601 strings to DateTime objects — use `[datetime]$watermark.lastScan` not `[datetime]::Parse(..., RoundtripKind)`; (2) Known IDs built from 3 sources merged into a HashSet: watermark seenIds + `docs/investigations/icm-*` filenames + GitHub issue titles matching `ICM (\d+):`; (3) Fetch GitHub issues once early (for knownIds) and reuse the result for dedup check later — avoids double API call; (4) Watermark updated after every scan (even "clear" result) so window always narrows; (5) FIFO cap at 200 via `List.RemoveAt(0)` loop. -DryRun shows watermark state + prompt. -Reset deletes file and exits cleanly.

- **2026-07-22 — Watchdog scheduling (#5, PR #51):** Built `run-pipeline.ps1` (38 lines) and `watchdog.ps1` (46 lines) for daily Teams pipeline execution. Key patterns: file-based I/O between pipeline steps (redirect stdout with `>`), `Invoke-Step` helper for DRY error handling, lockfile with stale-lock detection (check PID), structured log as append-only text, heartbeat as overwritten JSON. Used 24h scan window (not 48h) to avoid duplicate summaries — this was a known issue from the issue body. The ralph-watch.ps1 reference pattern is at `tamirdresher/squad-skills/workshop/ralph-watch.ps1`.
- **Pipeline scripts live on separate branches** until merged: squad/1-teams-probe, squad/2-filter-messages, squad/3-extract-decisions, squad/4-format-summary. The orchestrator references them by relative path in the same directory.
- **2026-07-22 — ICM Investigator skill (#52, PR #53):** Adapted the MDC-AI-Shared ICM Investigator (6 sub-agents, 5+1 pipeline stages) into a single SKILL.md at `.squad/skills/icm-investigator/`. Key adaptation: the original uses VS Code extension APIs (workspace tools, dashboard widgets, 1ES queries); ours maps to Copilot CLI MCP tools (icm-*, azure-mcp-kusto, geneva-mcp-server-*, enghub-*, azure-mcp-applens/resourcehealth/monitor). Also upgraded Aragorn's charter with a mandatory 4-stage investigation checklist and hard rules (use tools don't list them, synthesize don't transcribe, classify every incident, confidence levels mandatory). The gap analysis at `docs/aragorn-icm-capability-upgrade.md` (Elrond's work) was the key input — it identified 7 critical gaps including "no data execution" and "no TSG content reading".
- **2026-03-23 — icm-scan.ps1 rewrite (LLM-in-the-loop via copilot -p):** Replaced the broken AGENT_TASK signal pattern in `scripts/icm-scan.ps1` with a real `copilot -p --yolo --no-ask-user --model claude-sonnet-4.6 --share=<reportPath> -s` invocation. Elrond's research (`docs/research/llm-scheduled-execution-research.md`) confirmed output IS capturable in a variable with `-s`. Key decisions: (1) `--share` saves full session transcript to `docs/investigations/icm-scan-<timestamp>.md`; (2) `$output` from `-s` checked for incident patterns to trigger Teams notification via `send-teams-notification.ps1`; (3) `-DryRun` flag prints prompt and exits for safe testing. Also updated `.squad/scheduler.json`: icm-scan changed from `type: "agent"` (AGENT_TASK stdout signal nobody reads) to `type: "script"` with `"script": "scripts/icm-scan.ps1"` — the scheduler's existing script-task path handles execution with no further changes. Scheduler confirmed via `squad-scheduler.ps1 -Tasks icm-scan -DryRun`: shows `script | 4h | condition=unmet` (correctly blocked until on-call enabled). Acceptance: `-DryRun` works, prompt expands correctly with TeamId and timestamp, scheduler integration correct.
- **2026-07-22 — WorkIQ patterns skill (#85):** Created `.squad/skills/workiq-patterns/SKILL.md` — a general-purpose skill documenting WorkIQ query patterns for Teams message retrieval. Built from Elrond's live-tested research (11 tests). Key patterns: (1) WorkIQ is a semantic search engine, not an ID-based retrieval system — URLs don't work, but sender + chat name + time + topic reliably finds specific messages; (2) Indexing delay means sub-hour time windows are unreliable — use "today" or "last 24 hours"; (3) Results are curated (~25 max), AI-summarized by default — ask for "exact content" when verbatim text is needed; (4) Rate-limit to one query per agent action. Skill follows `.squad/templates/skill.md` format with tools metadata declaring `workiq-ask_work_iq`. Any agent needing Teams context reads this at spawn time — not tied to any specific protocol.

- **2026-03-25 — #112 Pre-Design Scan Complete (Gandalf findings):** Gandalf completed deep scan of Teams Notifications (#112). Three unresolved gaps found: (1) new notify.ps1 system built but zero production callers (1,268 LOC dead code); (2) MVP caller scripts not tracked as GitHub issues (notify-feature-complete.ps1, notify-blocked.ps1 defined in design but not filed); (3) foundation unproven (notify.ps1 never validated in production). Recommendation: file 4 MVP issues, validate notify.ps1 dry-run + real notification, scope decision on 🟡 Action tier, then proceed with design. Report at `docs/investigations/112-pre-design-scan.md`. Gimli's action items: (1) Create GitHub issue #??? for `notify-feature-complete.ps1` MVP caller (Gimli owner, depends on notify.ps1 validation); (2) Create GitHub issue #??? for `notify-blocked.ps1` MVP caller (Gimli owner, same dependency); (3) Create GitHub issue #??? for coordinator wiring (how does coordinator invoke notify-feature-complete.ps1? depends on #??? and #???); (4) Create GitHub issue #??? for E2E validation test (once MVP callers exist, Gimli writes integration test that calls notify-feature-complete.ps1 and verifies a Teams card arrived).

## 2026-03-24T00:16:24Z — DGrep POC Validation & Issue Resolution
- DGrep POC build passes (322 tests)
- Authentication verified with az cli
- SDK types compile successfully
- 6 closed issues: #101, #102, #103, #104, #107, #110
- Test endpoint saved: Diagnostics PROD, AugustaPrdEus2, Log/SentinelLogEntry
- Interactive dSTS query hung (investigating)
- Directives enforced: YOLO mode active, Kusto removed from CLI, Teams integration deferred until MVP
- Status: POC validation complete, ready for next phase

## 2026-03-24T02:36:28Z — Notifications MVP Scripts Delivery (Gimli)
**Task:** #132 + #133 — Notifications MVP scripts  
**Status:** ✅ COMPLETED

**Deliverables:**
- `scripts/notify-feature-complete.ps1` — MVP caller notifying feature completion to Teams
- `scripts/notify-blocked.ps1` — MVP caller notifying work blocked to Teams
- `tests/notifications/notify-feature-complete.Tests.ps1` — 13 Pester tests
- `tests/notifications/notify-blocked.Tests.ps1` — 13 Pester tests
- **Total:** 26/26 tests pass

**Key Patterns:**
1. Both callers wrap `scripts/notify.ps1` (production foundation system)
2. Standardized parameter handling: `FeatureName`, `Owner`, `Teams`, `Message`
3. Error handling: non-blocking exceptions logged; notification delivery verified in tests
4. Tests verify: parameter validation, JSON structure, Teams card formatting

**Commit:** 846d753 on squad/132-notifications-mvp-scripts

**Next:** Merge to main, wire into coordinator.ps1 for E2E orchestration

## 2026-03-24T23:59:00Z — Notifications Coordinator Wiring & DGrep SDK Stabilization Batch
- **Tasks:** #105 (DGrep SDK), #108 (DGrep error handling), #134 (Coordinator wiring)
- **Status:** ✅ COMPLETED — 381 tests passing, 3 issues closed
- **Deliverables:**
  1. DGrep SDK: Removed Kusto refs, built DgrepQueryExecutor (298 tests, -622 LOC)
  2. DGrep Error Handling: RetryPolicy + RetryingQueryExecutor with exponential backoff (363 tests, +41 new)
  3. Coordinator Wiring: notify-squad-event.ps1 dispatcher + failure-recovery.md (16/16 integration tests)
- **Key Patterns:**
  1. QueryExecutor abstraction encapsulates KQL execution + Azure auth; enables swapping implementations
  2. RetryPolicy framework separates retry logic (exponential backoff, jitter) from query execution
  3. Coordinator dispatcher routes events → notify-feature-complete.ps1 or notify-blocked.ps1 → Teams card
  4. Failure recovery: Non-blocking exception handling ensures partial failures don't break event stream
- **Cross-Agent:** Bilbo completed Notifications documentation (#120) using outputs from this batch
- **Boromir:** Hired (committed separately); next task is on-call integration with Notifications MVP
