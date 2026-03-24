# Project Context

- **Owner:** Jonathan
- **Project:** pa-squad — a personal-assistant squad for everyday tasks, research, documentation, tool building, and livesite support
- **Stack:** General-purpose (not a single-stack project)
- **Created:** 2026-03-22

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-03-22 — Triage Squad Feedback Issues #44-#49
- **Routing patterns:** Feedback on external squad product (cli, watch mode, defaults) routes to Gandalf (product feedback tracking). Tool fixes (PR flags, model defaults) route to Gimli. Documentation/charter patterns route to Bilbo.
- **Issue sources:** All 6 issues are Jonathan's onboarding feedback observations, filed on ms-pa repo because EMU tokens cannot write to bradygaster/squad directly.
- **Issues triaged:**
  - **#44** (PR draft flag) → `squad:gimli` (CLI fix)
  - **#45** (OAuth scopes) → `squad:gandalf` (external feedback)
  - **#46** (model defaults 4.6) → `squad:gimli` (config)
  - **#47** (watch UX) → `squad:gandalf` (external feedback)
  - **#48** (reviewer charter quality) → `squad:bilbo` (documentation patterns)
  - **#49** (default reviewer charter) → `squad:bilbo` (documentation patterns)

### 2026-03-22 — Teams Watchdog Decomposition
- **Architecture:** Teams watchdog is a 4-step pipeline (probe → filter → extract → format) + a watchdog orchestrator + delivery. Each step is a separate script under `.squad/skills/teams-watchdog/`.
- **Key constraint:** `gh copilot` doesn't work. Must use `copilot -p` or `copilot -i`. Cannot save Copilot output to a variable — use file-based I/O between steps.
- **Pattern:** Watchdog loop pattern from `ralph-watch.ps1` (lockfile, heartbeat, structured logging, Teams alerts on consecutive failures).
- **WorkIQ integration:** Teams messages are fetched via the `workiq-ask_work_iq` MCP tool, invoked through `copilot -p` prompts.
- **Repo:** Issues #1-#6 on `jbenami_microsoft/ms-pa`, labeled `squad` + `squad:gimli`.
- **Labels created:** `squad` (purple, 6f42c1) and `squad:gimli` (red, d73a49) on the ms-pa repo.

### 2026-03-22 — Failure Recovery Pipeline Architecture

- **Trigger:** Galadriel hit a wall trying to read ADO PR file contents — the `ado-repo_get_file_contents` tool doesn't exist. That near-miss made it clear the squad had no formal self-healing path.
- **Pipeline:** Failed Agent → Gandalf (triage) → Elrond (research, opus-4.6) → Gandalf (review) → Ralph (route) → Gimli/assigned (build) → Galadriel (review) → Original Agent (retry)
- **Signal format:** `.squad/decisions/inbox/{agent}-failure-{slug}.md` — structured failure report with slug convention, timestamp (ISO 8601 UTC), dedup check
- **Notification rule:** Jonathan only hears about it if Elrond can't find a solution, Gandalf rejects twice, retry still fails, OR Aragorn is blocked on a live livesite incident (livesite exception bypasses the pipeline)
- **All agents wired:** Elrond, Bilbo, Gimli, Aragorn, Scribe all updated with "On Failure" sections. Galadriel and Gandalf were already updated.
- **Key design principle:** Elrond writing "no solution found" IS the Jonathan notification trigger — it's the squad's final escalation gate.
- **Tracking:** `jbenami_microsoft/ms-pa#87`
- **Squad-infra audit:** Scanned coffee-ratings repo, found 11 squad-infra commits covering: ralph-watch watchdog (install, CLI args, config, pre-flight checks, Phase 1), spawn templates (lightweight spawn mode), governance consolidation, and model reference audits.
- **Audit pipeline:** Created 5 issues (#7–#11) as a dependency chain: Elrond researches → Bilbo documents → Gandalf decides PORT/SKIP/ADAPT → Gimli ports → Bilbo creates reusable squad template.
- **Bobbie charter study:** Found Bobbie's charter at `C:\code\dev\coffee-ratings\.squad\agents\bobbie\charter.md`. Role is Tester with strong PR review fix workflow, ownership-first model (original author fixes), and escalation after 3+ cycles.
- **Reviewer hire:** Created 2 issues (#12–#13) to research Bobbie's patterns and hire a LotR-cast Reviewer agent for ms-pa. Fills the gap — team has Lead, Researcher, Documentarian, Tool Builder, Operator but no Reviewer.
- **Labels created:** `squad:elrond` (blue, 1d76db), `squad:bilbo` (green, 0e8a16), `squad:gandalf` (yellow, fbca04) on ms-pa repo.
- **Issues created:** #7–#13 on `jbenami_microsoft/ms-pa`, all labeled `squad` + agent-specific labels.

### 2026-03-23 — DGrep CLI Project Decomposition

- **Project:** `tools/dgrep-cli/` — cross-platform CLI for Geneva DGrep log search. No CLI exists anywhere in the ecosystem; our tool fills the gap.
- **Research base:** Elrond's comprehensive research at `docs/research/geneva-dgrep-research.md` (23KB, covering API surface, KQL/MQL, auth, rate limits, tech stack recommendation).
- **Tech stack:** TypeScript, Commander.js, vitest, tsup, @azure/identity, cosmiconfig, cli-table3. Accepted Elrond's recommendation — team already uses Node.js, and the .NET SDK gap forces REST API reverse-engineering anyway.
- **Architecture pattern:** Two-track parallel execution. Phase 0 (API spikes → Elrond) runs alongside Phase 1 (foundation → Gimli). Phase 1 is 100% API-independent — uses mock data, builds types from SDK docs.
- **Critical path:** REST API discovery (#94) and auth token format (#95) are the blockers. Everything in Phase 2 (transport, auth, end-to-end commands) gates on these spikes.
- **Rate limit discipline:** 5 concurrent requests per user, orphaned queries block 20% of capacity. CLI must guarantee cleanup on Ctrl+C — this is a first-class requirement, not a nice-to-have.
- **Scaffolding delivered:** package.json, tsconfig, vitest config, types (QueryInput, RowSetResult, etc.), CLI entry point, 8 passing tests. All at `tools/dgrep-cli/`.
- **Issues created:** #94–#100 on `jbenami_microsoft/ms-pa`. Phase 0: #94 (REST API spike), #95 (auth spike) → Elrond. Phase 1: #96 (CLI structure), #97 (formatters), #98 (time parser), #99 (config), #100 (saved queries) → Gimli.
- **Known unknowns:** Streaming protocol (WebSocket? SSE? chunked HTTP?), whether dSTS tokens can be acquired via Azure Identity, NuGet package name for SDK decompilation.

### 2026-03-24 — DGrep CLI Pivot: TypeScript → C# .NET Framework SDK Wrapper

- **Decision:** Jonathan directed pivot from TypeScript/REST API to C# .NET Framework SDK wrapper. Cross-platform dropped as a requirement; Windows-only is fine. MCP integration deferred to later.
- **Rationale:** Elrond's approach comparison showed SDK wrapper wins on 8 of 11 dimensions. Auth (dSTS) is the killer — SDK handles it in one line; REST approach requires weeks of reverse-engineering with real failure risk. Time-to-first-query drops from weeks to hours.
- **Closed issues:** #94–#100 (TypeScript-specific — REST API spikes, Commander.js CLI, cosmiconfig, vitest formatters, time parser, saved queries).
- **New issues created:** #101 (CLI arg parsing), #102 (config management), #103 (output formatters), #104 (auth flow), #105 (search command), #106 (tail/streaming), #107 (saved queries), #108 (error handling), #109 (documentation).
- **Phasing:** Phase 1 (#101–#103) is SDK-independent — Gimli can start immediately. Phase 2 (#104–#106) needs Geneva access + NuGet package. Phase 3 (#107–#109) is polish.
- **Scaffold replaced:** Deleted TypeScript files (package.json, tsconfig, vitest, src/, tests/). Created C# solution: DgrepCli.sln, src/DgrepCli/ (net472 console app with CommandLineParser + Newtonsoft.Json), tests/DgrepCli.Tests/ (xUnit). Solution builds clean, 1 placeholder test passes.
- **Tech stack:** C# targeting net472, CommandLineParser for CLI, Newtonsoft.Json for serialization, xUnit for testing. SDK reference commented out until Phase 2.
- **Key remaining unknown:** Whether `DefaultAzureCredential` / `az login` tokens can bridge to the SDK's dSTS auth. Interactive auth (`DGrepUserAuthClient`) works out of the box; this is a nice-to-have for Phase 2.

### 2026-03-25 — Notification Track Reflection (Post-Mortem)

- **Trigger:** Jonathan expected a notification when the notification track completed. He got nothing. He confirmed the webhook IS configured and he already DOES get notifications — through the old system.
- **Root cause:** We built `notify.ps1` + `notification-recovery.ps1` + `notification-scheduler.ps1` (3 PRs: #122, #125, #127) but never wired any callers. Zero production invocations. The design doc's Phase 1 (scheduler wiring) and Phase 2 (agent wiring) were never started.
- **Existing system overlooked:** `send-teams-notification.ps1` was already delivering notifications — `icm-scan.ps1` (line 286) and `squad-daily-summary.ps1` (line 56) call it. Two parallel systems now coexist: old one works, new one is orphaned.
- **Learnings:**
  1. [HIGH] Integration points are deliverables, not afterthoughts — if the design says "wire X to call Y", that's a tracked issue, not implied work.
  2. [HIGH] "Feature complete" means user-observable effect, not code-merged. Jonathan seeing a Teams card = complete. Three merged PRs with no callers ≠ complete.
  3. [HIGH] When replacing an existing system, migrate at least one caller in the same PR. Otherwise the old system keeps running and the new one is dead code.
  4. [MED] PR reviewers (Galadriel) should flag library code with zero production callers — ask "who calls this?"
  5. [MED] I (Gandalf) approved the design and declared the track complete without verifying end-to-end delivery. I own this gap.

### 2026-03-28 — #112 Notifications Cleanup & DGrep Kusto Audit

- **Notifications cleanup executed:** Renamed `proactive-notifications.md` → `human-attention-notifications.md` (R1). Closed #115 (library code done in PR #122). Updated #116 body to clarify integration work remains. Labeled #117 as P3-low/post-MVP with scope decision. Created 4 MVP caller issues (#132–#135): notify-feature-complete, notify-blocked, coordinator wiring, E2E validation.
- **SKILL.md updated (R6):** Now documents BOTH old system (`send-teams-notification.ps1`, 2 callers, active) and new system (`notify.ps1`, 0 callers, built). Agents now know both systems exist.
- **failure-recovery.md updated (R7):** Notification section now specifies: use old system (`send-teams-notification.ps1`) until MVP validated, then switch to `notify-blocked.ps1`. Migration path is explicit, not ambiguous.
- **DGrep Kusto audit completed:** Found 47 "Kusto" references across 13 files in `tools/dgrep-cli/`. Key finding: `KustoQueryExecutor.cs` must become `DgrepQueryExecutor.cs`, `--cluster` should become `--endpoint`, `--database` should become `--namespace`. KQL references as a query language are FINE. Audit written to `.squad/decisions/inbox/gandalf-dgrep-kusto-audit.md`.
- **Key principle reinforced:** "Until MVP is proven to work, do NOT replace existing Teams integrations." Both systems coexist by design — old is active, new is staged.

### 2026-03-25 — Pre-Design Scan #112 Completion

- **Trigger:** Proactive deep scan of #112 (Teams Notifications) to unblock design work
- **Scope:** Three-system inventory, issue/PR alignment audit, MVP scope validation, recommendation prioritization
- **Status:** ✅ Completed — 287-line report delivered, team decision captured
- **Key inputs:** Issue #112, PR #122/125/127, design doc at `docs/designs/notifications-mvp.md`, dgrep reflection learnings
- **Deliverables:** (1) Full investigation: `docs/investigations/112-pre-design-scan.md`; (2) Team decision: `.squad/decisions/inbox/gandalf-112-pre-scan.md`; (3) Process: orchestration log + session log created
- **Finding 1:** New system is dead code (1,268 LOC, zero callers). Old 50-line `send-teams-notification.ps1` still runs all notifications.
- **Finding 2:** MVP caller scripts not tracked (notify-feature-complete.ps1, notify-blocked.ps1 defined in design but no GitHub issues).
- **Finding 3:** Foundation unproven — notify.ps1 never validated in production. MVP depends on it working.
- **Recommendation:** File 4 MVP issues (feature-complete, blocked, coordinator wiring, E2E validation), validate notify.ps1 in dry-run + one real notification, scope decision on 🟡 Action tier (out of MVP), then proceed with design.
- **Pattern recognized:** Same Phase 1/2 wiring gap as dgrep SDK issue — library code built, never wired to production callers. Reflects dgrep reflection learning #5: "building features without an integration proof point."

## 2026-03-24T00:16:24Z — Cleanup Session Consolidated
- Closed #115 (cleanup notification)
- Updated #116, #117 (notification follow-ups)
- Created 4 MVP issues (#132-135) for notification workflow redesign
- Renamed design documentation for clarity
- Updated SKILL.md and failure-recovery.md with decisions
- Audit of Kusto references in dgrep POC merged to decisions inbox
- Status: Cleanup complete, ready for next phase

### 2026-03-24 — Protocol Recovery (8-Branch Merge Sequence Leadership)

**Context:** Orchestrated retroactive review recovery and merge sequence for 8 branches (#105, #106, #108, #119, #132, #134, #112 pre-approved, plus secondary). All branches committed without initial Galadriel review. Full Cycle 1+2 reviews completed by Galadriel, all authors fixed findings, Cycle 2 re-approved all.

**Gandalf's Role — Merge Sequence & Conflict Resolution:**

1. **Merge Sequence Determined (105→106→108→119→132→134)**
   - Rationale: DGrep SDK foundation (#105) must precede error handling (#108) and tail command (#106)
   - Notifications (#132) precedes coordinator wiring (#134) to ensure foundation tested first
   - POC learnings gate feature design — prevent blind replication of mistakes

2. **Conflict Resolution — Two Merge Conflicts**
   - **Conflict 1: src/QueryOptions.cs parameter rename** (PR #105→#106 merge)
     - Root: #105 renamed parameter, #106 still used old name in QueryOptions class
     - Resolution: Gimli manual merge, kept #106's structure, added #105's parameter corrections (commit e7a3c9d2)
     - Validation: 298 tests pass, confirms no semantic loss
   - **Conflict 2: scripts/notify.ps1 metadata** (PR #132→#134 merge)
     - Root: #132 added parameter metadata decorations, #134 updated same section
     - Resolution: Gimli manual merge, kept both metadata sets with dedup (commit f8c4d1e5)
     - Validation: 16/16 integration tests pass

3. **POC Findings Governance — Must Gate Feature Work**
   - DGrep POC reflection learned: building without proving foundation wastes 3 PRs
   - Notifications cleanup post-mortem learned: design without integration proof creates orphaned code
   - Protocol recovery confirmation: these learnings MUST be wired as gates before Cycle 2 teams proceed

4. **Cross-Agent Insights Consolidated**
   - Galadriel: Domain terminology mismatches cascade across PR tracks — flag early
   - Gimli: DGrep SDK uses dSTS not AAD, Phase 2 blocked on NuGet credential setup, MVP 26/26 tests pass
   - Bilbo: Documentation-test sync must be verified during review, not after
   - Coordinator: Enforcement wiring now active (routing.md Rules 9-12)

**Learnings for Future Leadership:**
- [HIGH] POC findings must gate feature work — don't start Phase 2 until Phase 1 proofs validated
- [HIGH] When resolving merge conflicts, preserve test suites as acceptance criteria
- [HIGH] Merge sequence matters — order branches by dependency + proof requirement
- [MED] Integration work is first-class design deliverable, not post-implementation polish
