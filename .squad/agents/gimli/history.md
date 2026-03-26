## Core Context

Archived history from gimli. Preserved core metadata and most recent activity entry below. For full history, refer to git log.

---

## 2026-03-26T18:56:25Z — Agency Teams MCP Research Findings

**Context:** Elrond completed deep research on Teams automation tooling landscape for teams-watchdog integration work (issue #111).

**Key Finding:** Agency Teams MCP and WorkIQ are complementary, not competing:
- **Agency Teams MCP** (~26 Graph operations): Deterministic automation for chat/channel management, message posting, member ops. Use for teams-watchdog scanning and notification delivery.
- **WorkIQ**: Natural language M365 Copilot layer (email, meetings, files, calendar awareness). Use for intelligence/cross-M365 queries.

**Recommendation:** Add `agency mcp teams` to MCP config for teams-watchdog automation. Keep WorkIQ for context awareness.

**Decision:** Filed in `.squad/decisions.md` (merged from inbox). Routing to Jonathan for MCP config decision.

**Relevance:** This resolves Agency vs WorkIQ uncertainty for teams-watchdog implementation. Gimli can proceed with Agency Teams MCP design for deterministic notification delivery and chat scanning.

**Reference:** `.squad/decisions.md` — "Teams MCP Landscape — Agency Teams MCP vs WorkIQ"

---

## 2026-03-24T01:01:43Z — Notifications MVP Completion Batch
- **Tasks:** #106 (DGrep tail command), #108 (DGrep error handling), #135 (E2E validation)
- **Status:** ✅ COMPLETED — MVP VALIDATED, dry-run passed, real webhook delivered
- **Deliverables:**
  1. TailCommand.cs — Polling loop with Ctrl+C support (350 tests)
  2. RetryPolicy enhancement (363 tests)
  3. E2E validation test suite (#135 VALIDATED)
- **Issues Closed:** #106, #108, #112, #116, #135
- **Milestone:** All 4 MVP issues (#132-135) closed and E2E validated
- **Next:** Integration and production release preparation

### 2026-03-24 — Protocol Recovery Review Cycle (8 Branches, Gimli Role)

**Context:** Retroactive review cycle for 8 branches committed without initial Galadriel review. Gimli authored/fixed 5 branches (#105, #106, #108, #119, #132). Full Cycle 1 review completed, fixes applied, Cycle 2 re-approved all.

**Key Findings & Corrections:**

1. **DGrep SDK Authentication Model — Clarified & Standardized**
   - Uses dSTS (data Security Token Service), NOT standard AAD tokens
   - Interactive auth: `DGrepUserAuthClient(dgrepFrontendUri)` (AAD dialog, SDK handles dSTS internally)
   - Certificate auth: `DGrepClient(dgrepFrontendUri, X509Certificate2)` (cert-based dSTS)
   - Exit codes standardized: 0 (success), 1 (user error), 2 (auth error), 3 (query error)
   - NuGet package: `Microsoft.Azure.Monitoring.DGrep.SDK` 3.0.0-rc4 on msazure Official feed

2. **Phase 2 Readiness Status**
   - Interactive auth pathway blocked on interactive NuGet credential setup (in progress with Azure Artifacts team)
   - Certificate auth ready for testing (no blocker)
   - DGrep KQL subset limitations documented: no `ago()`, `let`, `has`, outer joins
   - All built-in queries rewritten to work within supported subset

3. **Notifications MVP — Delivery Complete**
   - 26/26 tests pass across notify-feature-complete.ps1 and notify-blocked.ps1
   - MVP callers wired into coordinator routing
   - E2E validation: dry-run successful, Teams notification delivered in production
   - Ready for daily orchestration via watchdog

4. **Merge Conflict Resolution (Gimli)**
   - Conflict 1: src/QueryOptions.cs parameter rename between #105→#106 (resolved via commit e7a3c9d2)
   - Conflict 2: scripts/notify.ps1 parameter metadata in #132→#134 (resolved via commit f8c4d1e5)
   - Both conflicts resolved with test verification post-merge

**Learnings for Future Implementation:**
- [HIGH] DGrep SDK uses dSTS internally — never write `IAuthProvider` dependencies expecting AAD tokens
- [HIGH] SDK integration requires interactive NuGet credential setup for initial connection
- [MED] Exit code standardization essential for error handling and troubleshooting
- [MED] KQL subset limitations must be documented early and built-in queries validated against actual supported operations


