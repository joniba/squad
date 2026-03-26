## Core Context

Archived history from gandalf. Preserved core metadata and most recent activity entry below. For full history, refer to git log.

---
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

### 2026-03-25 — IcM Scan Pipeline Fix (Bugs 1+2)
**Context:** Jonathan reported IcMs 768125338 and 768125136 were detected by scan but never investigated. Root cause: gh issue create errors silently swallowed (2>$null | Out-Null), notifications fired unconditionally, watermark updated before confirming issue creation, and no automation existed to trigger investigations or send completion notifications.

**Fixes Applied (commit a294fcb on squad/0-icm-scan-pipeline-fix):**
1. **Error handling:** Capture gh issue create output + check $LASTEXITCODE. Only proceed on success. Log failures visibly.
2. **Watermark timing:** Defer adding new IcM IDs to seenIds until issue creation confirmed. Prevents lost incidents on gh failures.
3. **Investigation trigger:** After confirmed issue creation, invoke copilot -p to trigger Aragorn investigation via ICM investigator skill.
4. **Completion notification:** After investigation completes, fire notify-investigation-complete.ps1 with full context.

**Learnings:**
- [HIGH] Never swallow command errors with 2>$null | Out-Null in automation scripts — always check exit codes
- [HIGH] Watermarks/state should only advance after the action they gate is confirmed successful
- [HIGH] End-to-end pipeline automation (scan→issue→investigate→notify) eliminates the "nobody picks it up" gap
- [MED] Pre-existing UTF-8 encoding issue in icm-scan.ps1 (em-dash chars) causes parse failures under powershell -File but works fine with dot-sourcing

### 2025-07-24 — Routing Enforcement Action Plan
**Context:** Elrond investigated why the coordinator bypasses worktree/PR/Galadriel review rules. Found 75%+ of main commits bypass the pipeline. Root cause: zero technical enforcement — all rules are instruction-layer only, which degrades under LLM context pressure. Investigation validated as accurate.

**Action Plan Delivered (commit b1489d4 on squad/0-routing-enforcement-investigation):**
1. **Immediate:** Pre-push hook + STOP-gate in squad.agent.md spawn template
2. **This week:** GitHub branch protection on main (P0 — eliminates 90% of violations), Ralph violation scan
3. **Structural:** Explicit exemption categories in routing.md, auto-PR on squad branch push
4. **Deferred as premature:** squad-git.ps1 wrapper, auto-review trigger, full automation suite

**Learnings:**
- [CRITICAL] Instruction-only enforcement is architecturally insufficient for stateless LLM agents — technical gates (branch protection) are mandatory
- [HIGH] The compliant path (6 steps) vs bypass path (2 steps) gap incentivizes violations — reduce friction on the compliant path
- [HIGH] Elrond's investigation quality was excellent — the three-layer enforcement model (technical → instruction → audit) is the right framework for agent governance
- [MED] Some recommendations in investigations are directionally correct but premature — Lead's job is to filter signal from enthusiasm and stage appropriately
- [MED] Governance rules that the orchestrator adds to itself without PR review are a meta-violation (rules about rules added without following the rules)

### 2025-07-25 — Enforcement V2 Design: Pre-Push Hook
**Context:** V1 enforcement (branch protection only) had a fatal flaw: bypass is all-or-nothing with no path-based exemption. Since Scribe must push .squad/ directly to main (Jonathan rejected Scribe PRs as board clutter) and all agents share one GitHub identity, branch protection can't distinguish Scribe from coordinator. Jonathan identified this kill shot during V1 review.

**Design Delivered (commit 9a34b8e on squad/0-enforcement-design-v2):**
- Pre-push hook at `scripts/hooks/pre-push` inspects diffs on pushes to main
- Rejects any push containing non-.squad/ files — tool-layer enforcement
- Scribe flow: unchanged, .squad/-only pushes pass validation
- Agent flow: worktree branches unaffected, accidental main pushes blocked
- Coordinator flow: local merges allowed but unpushable — PRs are only path for code to main
- Installation via `core.hooksPath` — one-time config, applies to all worktrees
- Branch protection recommended as complement (bypass list + hook = defense in depth)
- Migration: 3-phase rollout (hook day 1, CI audit day 2-3, branch protection day 3-5)

**Status:** Pending Boromir design review (routing.md Rule 13)

**Learnings:**
- [CRITICAL] Path-based enforcement requires tool-layer controls — GitHub branch protection is identity-only
- [HIGH] Client-side hooks are sufficient when threat model is context pressure, not adversarial actors
- [HIGH] Enforcement boundary belongs at the sharing point (push), not local state (commit)
- [MED] core.hooksPath applies to all worktrees from one config — critical advantage over per-worktree hook copies

### 2025-07-25 — Enforcement V2.1 Revisions (Boromir Review Response)

**Context:** Boromir REJECTED V2 design with 4 required revisions (2 bonus). All 4 required + both bonus items addressed in revised design doc.

**Revisions made (commit e39821f on squad/0-enforcement-design-v2):**

1. **Instruction-layer gap (Issue 1):** Documented evidence that spawn template WAS fixed (commit 7fa27e3, 2026-03-24) but 13+ direct-to-main violations continued after fix. Instruction-layer proven insufficient alone — this is the justification for the hook.

2. **Overclaiming strength (Issue 2):** Reframed entire design from "enforcement" to "guardrail." Removed "physically cannot push" claim from Section 1. Updated Sections 4, 7, 13, 15 with honest guardrail language. Design now correctly sets expectations about bypass resistance.

3. **Merge+Scribe trap (Issue 3):** Added two non-destructive recovery paths (cherry-pick and soft-reset) that preserve Scribe commits when local merge blocks push. Added prevention instruction for spawn template. Updated Section 9 Mode 4 to reference new recovery paths.

4. **Alternatives explored (Issue 4):** Genuine tradeoff analysis of Alternative A (crash recovery is fatal flaw — Scribe's durability contract broken by delayed push) and Alternative B (separate branch for .squad/ — breaking change across all agents, charters, scripts, wrong cost tradeoff). Alternative C (post-push audit) also addressed briefly.

5. **Positive confirmation (Issue 5, bonus):** Added breadcrumb mechanism — hook writes validation log to .squad/log/hook-validations.log. CI audit verifies breadcrumbs exist for recent pushes.

6. **Minimum viable guardrail (Issue 6, bonus):** Hook alone is the MVG. Build order: hook → templates → CI → branch protection.

**Status:** Re-submitted for Boromir review. Pushed to squad/0-enforcement-design-v2.

**Learnings:**
- [CRITICAL] Adversarial review exposes real gaps — Boromir's merge+Scribe trap was a genuine missed scenario, not a theoretical concern
- [HIGH] Honest language matters — calling a guardrail "enforcement" creates false confidence that leads to under-investment in complementary layers
- [HIGH] "Was the instruction fix tried?" is a necessary question before adding tool-layer controls. Evidence-based justification (13 post-fix violations) is stronger than assumption-based justification
- [MED] Alternative analysis is valuable even when alternatives are clearly worse — the analysis itself documents the decision rationale for future readers

### 2025-07-25 — Issue Triage (10 Open Issues)
**Context:** Jonathan requested triage of 10 open issues to separate stale from actionable. GitHub CLI was unreachable (underscore in org name `jbenami_microsoft/ms-pa`), so dependency status was inferred from project history and decisions.md.

**Verdicts:** 3 CLOSE, 7 KEEP.
- **Closed #89, #90:** External dependency chain (PR 15064785 in microsoft/Sentinel-TiPipeline) — repo unreachable, monitoring task is pure waste. Both issues dead-ended.
- **Closed #117:** P3-low, depends on unverified #113, notification architecture redesigned (#132-135 supersede Phase 1.x scheme).
- **Kept #159, #152, #123, #118, #116, #111, #107:** All have concrete remaining work, met dependencies, or active PRs.

**Decisions written to:** `.squad/decisions/inbox/gandalf-issue-triage.md`

## Learnings
- [HIGH] Monitoring issues for external repos you can't access are inherently stale — close them and rely on discovery through other channels
- [HIGH] When an architecture is redesigned (e.g., notifications #132-135), old phase-numbered issues from the prior architecture should be closed, not carried forward
- [MED] GitHub CLI failures due to repo naming (underscores vs hyphens) should be flagged for Jonathan to fix the remote — it blocks all automated issue management
- [MED] Dependency chain staleness is transitive — if a blocker is stale, everything blocked on it is also stale

- [MED] Alternative analysis is valuable even when alternatives are clearly worse — the analysis itself documents the decision rationale for future readers

