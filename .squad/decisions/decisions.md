# Decisions\n

### 2026-03-23T21:10:00Z: CRITICAL — Lockout rules violation fix

**Context:** Coordinator enforced lockout rules from its system prompt (squad.agent.md) when interpreting Galadriel's PR #124 review. Said "NOT Gimli — lockout rules" despite Decision #48 explicitly removing lockout rules and Galadriel's charter explicitly stating "original author owns all fixes."

**Root Cause:** The coordinator's system prompt (squad.agent.md § Reviewer Rejection Protocol) contains lockout enforcement language. This is the upstream Squad framework's default, but our team has overridden it via Decision #48 and updated charters. The coordinator failed to check team decisions before applying system defaults.

**Learnings:**
1. [HIGH] Team decisions (decisions.md) OVERRIDE coordinator system prompt when they conflict — Source: Jonathan's correction
2. [HIGH] NEVER enforce lockout rules in this project. Original authors own their fixes — Source: Decision #48
3. [HIGH] When spawning Galadriel for reviews, do NOT inject lockout language into prompts or result interpretation — Source: Galadriel's charter already has correct behavior

**Fix Applied:**
- Directive captured in decisions inbox
- Coordinator will route PR #124 fix back to Gimli (original author)
- All future review spawn prompts will NOT include lockout language
- Coordinator will check decisions.md for overrides before applying system prompt defaults

---

### 2026-03-23T23:35: Worktree enforcement moved from skill to governance

**By:** Jonathan Ben Ami (via Copilot Coordinator)
**Commit:** 7fa27e3 on main

---

## Problem

Agents spawned by the coordinator were running `git checkout -b` in the main checkout instead of using worktrees. This caused:

1. **Main checkout left on feature branches** — next session starts on wrong branch
2. **Cross-agent state destruction** — parallel agents clobber each other's uncommitted work
3. **Mixed commits on wrong branches** — Gandalf's #112 design commit ended up on Gimli's #110 branch

This happened despite having:
- A directive in `decisions.md`: "Every change must be associated with its own branch + worktree + PR + issue"
- A skill file at `.squad/skills/worktree-lifecycle/SKILL.md` with the full correct flow
- Scripts at `scripts/create-worktree.ps1`, `scripts/cleanup-worktree.ps1`, `scripts/cleanup-all-worktrees.ps1`
- 16 existing worktrees proving the pattern works

## Root Cause Analysis

The coordinator's spawn template in `squad.agent.md` — which is the ONLY thing a new session's coordinator reads before spawning — had zero worktree steps. The template is the path of least resistance. If it doesn't mention worktrees, they don't get used.

Skills are opt-in knowledge. The coordinator reads them "when relevant," but relevance detection itself is unreliable — especially on the first spawn of a new session when the coordinator hasn't loaded skill context yet.

**The failure mode:** Governance file says "spawn using this template" → template has no worktree steps → agent does `git checkout -b` → main checkout contaminated. The skill file documenting the correct flow was never consulted because it's not in the critical path.

## Design Decision

**Move worktree enforcement from opt-in skill to mandatory governance.**

Three surgical edits to `.github/agents/squad.agent.md`:

### Change 1: Worktree Gate (pre-spawn checklist)

Added before "How to Spawn an Agent" section. Four mandatory steps:
1. Verify main checkout is on `main` (switch back if not)
2. Create worktree via `create-worktree.ps1` or raw `git worktree add`
3. Pass `WORKTREE_PATH` in spawn prompt
4. Agents MUST NOT run `git checkout -b`

Explicit skip conditions: read-only queries, Scribe, tasks that don't touch git.

Fallback: if `create-worktree.ps1` doesn't exist, use raw git command.

### Change 2: Spawn Template — Worktree Isolation Block

Added to the main spawn template (the one every coordinator copies) between `TEAM ROOT` and the existing `Read .squad/...` lines:

```
### Worktree Isolation (CRITICAL)
WORKTREE_PATH: {worktree_path}
AGENT_BRANCH: squad/{issue}-{slug}
Rules:
1. cd into WORKTREE_PATH as FIRST action
2. Do NOT run git checkout or git checkout -b
3. READ .squad/ state from TEAM_ROOT
4. WRITE decision inbox to TEAM_ROOT
5. COMMIT and PUSH only from WORKTREE_PATH
```

Conditional: only included when worktree was created. Omitted for read-only/Scribe spawns.

### Change 3: Lightweight Spawn Template

Same worktree block added (compact form) for lightweight spawns that still create branches.

## Why This Should Work

1. **The template is the path of least resistance.** Every new session follows it. If the template says "create worktree first," the coordinator will create a worktree first. No discipline required.

2. **The gate is before the spawn, not after.** The coordinator must pass through the worktree gate before it can reach the spawn template. It can't skip it without skipping the entire spawn section.

3. **The agent gets explicit isolation rules.** The agent's prompt says "cd into WORKTREE_PATH" and "do NOT run git checkout." Even if the coordinator somehow skips the gate, the agent's prompt (if the block was included) prevents branch switching.

4. **Fallback exists.** If `create-worktree.ps1` is missing, the raw git command is documented inline. No dependency on the script existing.

## What Could Still Go Wrong

1. **Coordinator ignores the gate.** The gate is text in a prompt, not executable code. A sufficiently distracted coordinator could still skip it. Mitigation: the gate uses ⚠️ and "MANDATORY" markers, and is positioned as the FIRST thing in the "How to Spawn an Agent" section.

2. **Agent ignores WORKTREE_PATH.** The agent receives the isolation rules but doesn't cd into the worktree. Mitigation: "CRITICAL" marker, explicit "FIRST action" instruction, and "STOP and tell the Coordinator" if something seems wrong.

3. **Worktree already exists for the branch.** If a previous session created a worktree for the same issue and didn't clean up, `git worktree add` will fail. Mitigation: documented in the skill file's error handling table. The coordinator should `git worktree remove` first or reuse the existing worktree.

4. **Session starts on wrong branch (like this one).** If a previous session left the main checkout on a feature branch, the gate's step 1 catches it: "Verify main checkout is on main. If not, switch back." This is a repair step, not just a check.

5. **Read-only task that unexpectedly needs to commit.** If a task was classified as read-only (no worktree created) but the agent decides to write/commit, it'll commit to whatever branch main checkout is on. Mitigation: the gate says to verify main is on main, so worst case it commits to main (which is acceptable for non-code changes per decisions.md).

## Files Changed

- `.github/agents/squad.agent.md` — 36 lines added (3 locations)

## Files NOT Changed (and why)

- `.squad/skills/worktree-lifecycle/SKILL.md` — stays as-is. It's reference documentation for the full lifecycle (create → spawn → work → merge → cleanup). The governance file now enforces the critical path; the skill file documents the full flow.
- `scripts/create-worktree.ps1` — no changes needed. It already does the right thing.
- `.squad/decisions.md` — Scribe will merge this inbox entry.

## Status

- **Committed:** 7fa27e3 on main
- **Scope:** pa-squad repo only (this is a local squad.agent.md, not the upstream Squad product)
- **Next session test:** Start a new session, say "Ralph, go" or "Gimli, work on #101." Verify the coordinator creates a worktree before spawning.

---

### 2026-03-24T02-08-22: DGrep test endpoint
**By:** Jonathan (via Copilot)
**What:** Geneva DGrep portal works from this machine. Test endpoint details:
- Backend: DGrep
- Endpoint: Diagnostics PROD
- Namespaces: AugustaPrdEus2, AugustaPrdWeu, AugustaProdWUS2, TIAugustaDevWUS2, etc.
- Events: Log, SentinelLogEntry
- Example condition: AnyField contains 7296f6f9-2ff7-49e9-b118-a9e394518ef1
**Why:** Real test data for validating dgrep CLI Phase 2 (search execution)

---

### 2026-03-23T17:21: User directive
**By:** Jonathan (via Copilot)
**What:** Every change must be associated with its own branch + worktree + PR + issue. Gandalf orchestrates everything via GitHub issues. Unless the user explicitly names a squad member in the request, Gandalf owns the routing.
**Why:** User request — captured for team memory. Enforces proper git workflow discipline: no direct commits to main from agents. All work flows through the issue → branch → PR → merge lifecycle.

---

### 2026-03-23T17:23: User directive
**By:** Jonathan (via Copilot)
**What:** When Jonathan says "reflect", invoke the reflect skill. Insights from reflection must be persisted as directives (decisions inbox) or agent charter/instruction fixes — never left only in session memory.
**Why:** User request — ensures continuous improvement is durable, not ephemeral.

---

### 2026-03-23T19:46: User directive
**By:** Jonathan (via Copilot)
**What:** Never use haiku model for Aragorn. Always use standard tier (claude-sonnet-4.6) or higher.
**Why:** User request — Aragorn's investigation work requires higher quality reasoning. Haiku is insufficient for ICM analysis, cert investigations, and code-level RCA.

---

### 2026-03-23T19:50: Session reflection
**By:** Coordinator (reflect skill)
**What:** Clarification of branch-per-change directive based on session evidence:
- **Code changes** → branch + PR (mandatory)
- **Docs, designs, investigations, catalogs** → commit directly to main (practical)
- **When asking Jonathan to review** → merge to main first, don't ask him to switch branches
- **Don't duplicate work Jonathan already did** — check before spawning (e.g., "no triage needed, there's already a report", "just added it to the task-index")
**Why:** Multiple corrections during session. Jonathan merged PR #113 immediately when told to review on a branch ("not practical"). Investigation reports committed to main throughout without issue. Bilbo ran redundantly on TASK-INDEX work Jonathan had already done.

---

### 2026-03-23T20:32: User directives (super-duper yolo protocol)
**By:** Jonathan (via Copilot)
**What:**
1. ONLY Galadriel is authorized to close PRs. No one else except the coordinator (and only where human intervention is required).
2. Do NOT cut corners, do NOT be efficient. Be thorough. If stuck or have doubts, handoff to Elrond for research and Gandalf to orchestrate.
3. Super-duper yolo protocol: whenever human intervention is needed, the coordinator steps in autonomously.
**Why:** User request — Jonathan wants two major features (DGrep CLI + Teams notifications) built end-to-end with full quality gates.

---

### 2026-03-23T23:13:56Z: User directive
**By:** Jonathan (via Copilot)
**What:** Commits to main must always be pushed immediately after committing.
**Why:** User request — captured for team memory

---

### 2026-03-24T01-55-22: User directive
**By:** Jonathan (via Copilot)
**What:** All agents use claude-opus-4.6 for everything. No more sonnet for scans or any other work.
**Why:** User request — captured for team memory

---

### 2026-03-24T01-56-45: User directive
**By:** Jonathan (via Copilot)
**What:** Until the Teams notifications MVP is proven to work, do NOT replace existing Teams integrations. New notification system must coexist alongside current integrations.
**Why:** User request — risk mitigation, existing functionality must not regress during MVP validation

---

### 2026-03-24T02-03-01: User directives (batch)
**By:** Jonathan (via Copilot)
**What:**
1. YOLO MODE for dgrep: Coordinator acts as tester and handles all human interaction for the dgrep feature. Do not wait for Jonathan — make decisions autonomously.
2. DGrep naming: It's a DGREP CLI — remove any references to "Kusto" or "KQL" from the tool. If present, Gandalf should clean that up.
3. DGrep POC validation: Verify the dgrep POC works BEFORE continuing with further dgrep implementation phases.
4. All other features (non-notifications) must start NOW — don't hold them for the #112 cleanup.
**Why:** User request — maximize throughput, unblock all parallel work

---

### 2026-03-23T21:10:00Z: User directive — CRITICAL
**By:** Jonathan (via Copilot)
**What:** Lockout rules were removed from all charters per Decision #48. The coordinator MUST NOT enforce lockout rules. Original authors own their fixes. This was violated when coordinator cited "lockout rules" for PR #124 — Gimli should fix his own work.
**Why:** Decision #48 explicitly states "Reviewer lockout design is wrong — authors should own fixes." Galadriel's charter already says "original author owns all fixes." The coordinator's system prompt has stale lockout protocol that contradicts team decisions. Team decisions override stale system prompt rules.

---

### 2026-03-28: DGrep CLI — Kusto Contamination Audit

**Author:** Gandalf (Lead)  
**Context:** The DGrep CLI (`tools/dgrep-cli/`) queries Geneva DGrep endpoints, NOT Kusto clusters. KQL is a valid query language (DGrep supports KQL), but references to Kusto clusters, Kusto SDK, Kusto connection strings are wrong.

---

## Summary

**47 "Kusto" references** across 13 files. **48 `.kusto.windows.net` URLs** across 13 files (test data). Most are in tests using Kusto cluster URLs as fake connection targets when they should use DGrep MDS endpoints.

## Files with Kusto References — Cleanup Plan

### Source Files (MUST FIX)

| File | Kusto Refs | What's Wrong | Action |
|------|-----------|--------------|--------|
| `src/DgrepCli/Execution/KustoQueryExecutor.cs` | 9 | **Entire file is named wrong.** Class is `KustoQueryExecutor`, comments reference `Microsoft.Azure.Kusto.Data`, `KustoConnectionStringBuilder`, `KustoClientFactory`. | **RENAME** to `DgrepQueryExecutor.cs`, rename class to `DgrepQueryExecutor`. Replace all Kusto SDK comments with DGrep SDK equivalents. |
| `src/DgrepCli/Commands/QueryVerbOptions.cs` | 2 | Help text says "Execute a KQL query against a Kusto cluster" and "--cluster" help says "Kusto cluster URL". Example uses `help.kusto.windows.net`. | **REPLACE** help text: "Execute a query against a DGrep endpoint". Replace example URL with DGrep MDS endpoint format. |
| `src/DgrepCli/Program.cs` | 3 | Line 157: "Execute a KQL query against a Kusto cluster". Lines 113, 123: `new KustoQueryExecutor(...)`. | **UPDATE** help text to say "DGrep endpoint" not "Kusto cluster". Update class references after rename. |
| `src/DgrepCli/Execution/QueryOptions.cs` | 1 | XML comment: "Kusto cluster connection string or URL". | **REPLACE** with "DGrep MDS endpoint URL". |
| `src/DgrepCli/Execution/QueryException.cs` | 1 | XML comment: "pass through from Kusto", parameter named `kustoError`. | **RENAME** parameter to `serverError`. Update comment to "pass through from DGrep server". |
| `src/DgrepCli/Auth/IAuthProvider.cs` | 1 | XML comment: "obtain a bearer token for Kusto connections". | **REPLACE** with "obtain a bearer token for DGrep connections". |
| `src/DgrepCli/Auth/AzCliAuthProvider.cs` | 1 | Hardcoded `https://kusto.kusto.windows.net` as token resource URL. | **REPLACE** with correct DGrep/Geneva resource URL. |
| `src/DgrepCli/Commands/AuthCommand.cs` | 2 | Fallback cluster is `https://kusto.kusto.windows.net`, message says "Using Kusto resource URL". | **REPLACE** with DGrep endpoint and appropriate message. |

### Test Files (FIX — test data uses wrong URLs)

| File | Kusto Refs | Action |
|------|-----------|--------|
| `tests/DgrepCli.Tests/Execution/ExecutionTests.cs` | 5 | Rename `KustoQueryExecutorTests` class. Replace `.kusto.windows.net` URLs with DGrep endpoints. |
| `tests/DgrepCli.Tests/Auth/AuthExecutorIntegrationTests.cs` | 8 | Comment references `KustoQueryExecutor`. Replace all `.kusto.windows.net` URLs. |
| `tests/DgrepCli.Tests/Auth/AzCliAuthProviderTests.cs` | 9 | All test URLs use `kusto.kusto.windows.net`. Replace with DGrep resource URL. |
| `tests/DgrepCli.Tests/Auth/AuthCommandTests.cs` | 1 | Config uses `.kusto.windows.net`. Replace. |
| `tests/DgrepCli.Tests/Auth/CertificateAuthProviderTests.cs` | 2 | Token resource URLs. Replace. |
| `tests/DgrepCli.Tests/Auth/ManagedIdentityAuthProviderTests.cs` | 1 | Token resource URL. Replace. |
| `tests/DgrepCli.Tests/Commands/QueryCommandTests.cs` | 1 | Cluster URL in test data. Replace. |
| `tests/DgrepCli.Tests/Commands/SavedCommandTests.cs` | 17 | Heavy use of `.kusto.windows.net` in saved query tests. Bulk replace. |
| `tests/DgrepCli.Tests/Commands/SavedOptionsTests.cs` | 2 | Cluster URL in options tests. Replace. |

### Files That Are FINE (KQL references are correct)

| File | Why It's OK |
|------|-------------|
| `README.md` | Documents "DGrep KQL Pitfalls" — KQL is the query language, this is correct. Also has a ❌ Kusto / ✅ DGrep comparison table. |
| `PLAN.md` | References KQL validation/linting — correct (DGrep uses KQL). |
| `src/DgrepCli/Commands/SearchOptions.cs` | `--query-type kql` option — correct. |
| `src/DgrepCli/Commands/TailOptions.cs` | `--query-type kql` option — correct. |
| `src/DgrepCli/Commands/SavedOptions.cs` | "KQL query template" — correct. |
| `src/DgrepCli/Config/OptionResolver.cs` | Default query type is `kql` — correct. |
| `src/DgrepCli/Config/DgrepConfig.cs` | "Default query type: kql or mql" — correct. |
| `src/DgrepCli/Commands/OptionValidator.cs` | Validates `kql` / `mql` — correct. |
| `src/types/index.ts` | TypeScript types (legacy, pre-pivot) — `KQL` as query type is correct. |

## Key Decisions Needed

### 1. Should `KustoQueryExecutor.cs` become `DgrepQueryExecutor.cs`?

**YES.** This is a DGrep CLI. The executor connects to DGrep endpoints, not Kusto clusters. The DGrep SDK is `Microsoft.Azure.Monitoring.DGrep.SDK`, not `Microsoft.Azure.Kusto.Data`. Rename the file AND class.

### 2. Should the `query` verb be removed?

**NO — but its help text must change.** The `query` verb is fine for a CLI that runs queries. The problem is the help text ("Execute a KQL query against a Kusto cluster") and the example URL (`help.kusto.windows.net`). Fix: "Execute a query against a DGrep endpoint" with a Geneva MDS endpoint example.

The `query` verb is NOT Kusto-specific — it's a general concept. `dgrep query "..."` reads naturally for a DGrep CLI.

### 3. What about `--cluster` option name?

**RENAME to `--endpoint`.** DGrep doesn't have "clusters" — it has MDS endpoints/namespaces. The `--cluster` flag is a Kusto mental model leak. `--endpoint` is correct for DGrep.

### 4. What about `--database` option?

**RENAME to `--namespace` or `--event`.** DGrep uses namespace + event, not database. This is another Kusto concept leak.

## Effort Estimate

- **Source files:** ~2 hours (8 files, mostly find-and-replace + rename)
- **Test files:** ~3 hours (9 files, 46+ URL replacements, class renames, must verify tests still pass)
- **Total:** ~5 hours for Gimli, tracked as a single issue

## Recommendation

File one GitHub issue: "[dgrep-cli] Remove Kusto contamination — rename to DGrep concepts". Assign to Gimli. This is a straightforward bulk rename + find-replace. All 274 tests must still pass after.

---

*— Gandalf, Lead*

---

---
title: "DGrep CLI Reflection — Wrong SDK Built"
author: gandalf
date: 2026-03-28
type: reflection
tags:
  - dgrep
  - reflection
  - process-failure
  - sdk-mismatch
---

# DGrep CLI Reflection — Wrong SDK Built

## What Happened

We built a DGrep CLI tool with a `KustoQueryExecutor` that references `Microsoft.Azure.Kusto.Data` patterns. DGrep is not Kusto. It has its own SDK (`Microsoft.Azure.Monitoring.DGrep.SDK`), its own auth model (dSTS, not AAD tokens), and its own connection model (MDS endpoint + namespace + event, not cluster + database). 6 PRs merged, 274 tests written, all against the wrong product's concepts.

## Root Cause

**Gandalf (me) introduced Kusto terminology when creating the C# pivot issues (#101–#109).** Elrond's research was correct — it clearly distinguished DGrep from Kusto. The POC was correct — it used the DGrep SDK. But when I pivoted from TypeScript to C#, I wrote issue descriptions using Kusto mental models instead of re-reading the research. Gimli built exactly what I specified.

## Learnings

1. **[HIGH] POC results must gate feature work, not run in parallel.** The POC (`squad/dgrep-poc`) confirmed the correct SDK works, but its findings were never integrated. By the time POC results were available, 3 PRs had already merged against the wrong model. **Fix:** Add to Gandalf's charter — "When a POC exists, its findings must be reviewed and merged before feature work begins."

2. **[HIGH] Tech stack pivots require design re-review.** Pivoting from TypeScript to C# was the right call, but I created new issues without validating them against Elrond's research. The pivot created a translation gap where DGrep concepts got replaced with Kusto concepts. **Fix:** Add to team decisions — "After a tech stack pivot, all new issues must be reviewed against the original research document before implementation begins."

3. **[HIGH] Domain terminology is a design signal.** If a tool called "dgrep" has issues that say "Kusto cluster" and "Kusto database," that's a red flag. Nobody — Gandalf, Galadriel, or Gimli — caught the terminology mismatch. **Fix:** Add to Galadriel's charter — "Flag domain terminology mismatches between issue title/description and code naming."

4. **[MED] Reviewers need domain context.** Galadriel reviewed the PRs and found no issues because the code matched the issue descriptions. She had no way to know the issue descriptions were wrong. **Fix:** Before a multi-PR track, provide Galadriel a one-paragraph domain brief (e.g., "DGrep uses MDS endpoints, not Kusto clusters. Auth is dSTS via SDK, not AAD tokens via az-login.").

5. **[MED] This is the same pattern as the notification track.** We built library code without verifying end-to-end delivery. In the notification case, we built `notify.ps1` without wiring callers. In the DGrep case, we built a query executor without validating it against the real API. The common failure: building features without an integration proof point.

## Impact

- **Work invested:** 6 PRs, 274 tests, ~2000 LOC
- **Work wasted:** ~35% (KustoQueryExecutor, query verb, auth validation, built-in queries)
- **Work reusable:** ~65% (CLI scaffolding, formatters, config, saved queries, 200+ tests)
- **Recovery cost:** ~3 developer-days

## Action Items

Full correction plan at `docs/designs/dgrep-correction-plan.md`. Summary:

1. **Phase A:** Delete KustoQueryExecutor and all Kusto-specific code
2. **Phase B:** Add real DGrep SDK, create DgrepQueryExecutor, run one real query (GATE)
3. **Phase C:** Simplify auth to SDK-managed dSTS
4. **Phase D:** Rewrite built-in queries for DGrep KQL subset
5. **Phase E:** Update all documentation

5 new issues to create for Gimli (cleanup, SDK integration, auth rework, queries, docs).

## Process Fixes (Proposed Team Decisions)

1. **POC-gates-feature-work:** When a POC branch exists, its findings must be reviewed and integrated before feature development begins on the same track.
2. **Pivot-requires-design-review:** After any tech stack pivot, re-validate all new issues against the original research document.
3. **Domain-term-signal-check:** Galadriel should flag when code/issue terminology doesn't match the product domain.
4. **Reviewer-domain-brief:** Before a multi-PR feature track, provide the reviewer a one-paragraph domain context brief.

## Ownership

I (Gandalf) own this failure. Elrond's research was correct. The POC was correct. Gimli built what I specified. The bug was in my issue specifications. I'm correcting it now.

---

# Deep Audit: Teams Notifications Feature

**Date:** 2026-03-26
**Author:** Gandalf (Lead)
**Requested by:** Jonathan
**Type:** Gap analysis — what's implemented, what's pending, what to close/open

---

## Executive Summary

The notifications feature has strong infrastructure (1,475 lines of PowerShell across 3 scripts) but **zero production callers**. The old system (`send-teams-notification.ps1`, 56 lines) continues to be the only thing delivering Teams messages. Three PRs (#122, #125, #127) merged successfully, each delivering library code with no wiring. The MVP plan (dated 2026-03-25) designed two caller scripts (`notify-feature-complete.ps1` and `notify-blocked.ps1`) — neither exists.

**Bottom line:** The engine was built. Nobody turned the key.

---

## Issue-by-Issue Analysis

### Issue #112 — Design: Proactive Teams notifications
**State:** OPEN
**PR:** #113 (MERGED — delivered design doc)
**Completion:** 100%

**What was delivered:**
- Full design doc at `docs/designs/proactive-notifications.md` (~26KB)
- Three-tier notification architecture (urgent/action/feature)
- Integration points defined for scheduler, failure recovery, agents
- Phased rollout plan (Phase 0–3)
- Cross-referenced against Tamir Dresher's patterns (validated, no conflicts)

**What's missing:** Nothing. This is a design issue. The design is delivered and sound.

**Recommendation:** ✅ **CLOSE immediately.** The design was delivered via PR #113. All subsequent work is tracked by #115–#117 and #120. Keeping this open serves no purpose — it's done.

---

### Issue #115 — Phase 1.1: Webhook & message templates
**State:** OPEN
**PR:** #122 (MERGED — delivered `notify.ps1`)
**Completion:** 95%

**What was delivered:**
- `scripts/notify.ps1` (563 lines) — full notification router
  - Three-tier classification: `urgent`, `action`, `feature`
  - Four Adaptive Card builders: `Build-UrgentCard`, `Build-ActionCard`, `Build-FeatureCard`, `Build-BatchFeatureCard`
  - Watermark dedup: state file at `.squad/notifications-state.json`, per-tier re-notification rules (urgent: 6h/3-error, action: 48h/state-change, feature: never)
  - Feature batching: queue ≥5 items OR hourly flush
  - Webhook delivery: exponential backoff (2s, 4s, 8s, 16s), 28KB payload guard
  - DryRun + Force flags
- `tests/notify.Tests.ps1` — unit tests for formatting, dedup, state

**What's missing (minor):**
- Issue says "Env var validation for TEAMS_WEBHOOK_URL" — implementation uses file-based `~/.squad/teams-webhook.url` instead (better approach, aligned with existing system). Not a real gap.
- Issue says "All 5 message types have templates" — `notify.ps1` has 3 tiers with 4 card builders. The scheduler (PR #127) adds 6 event templates. This exceeds the requirement.

**Recommendation:** ✅ **CLOSE immediately.** All acceptance criteria met. The env var vs file-based difference is an improvement, not a gap.

---

### Issue #116 — Phase 1.2: Integration with failure recovery
**State:** OPEN
**PR:** #125 (MERGED — delivered `notification-recovery.ps1`)
**Completion:** 40%

**What was delivered:**
- `scripts/notification-recovery.ps1` (429 lines) — dead letter queue + retry
  - `Write-DeadLetter`: persist failed notifications to `~/.squad/notifications/dead-letter/`
  - `Retry-FailedNotifications`: escalating backoff (5m, 15m, 1h), permanent failure after 3 cycles
  - `Test-NotificationHealth`: webhook reachability, queue depth, last-success timestamp
  - `Invoke-DeadLetterCleanup`: purge succeeded/aged items
- `tests/notification-recovery.Tests.ps1` — unit tests
- `notify.ps1` wires to recovery functions (Write-DeadLetter on send failure, Update-LastSuccess on success)

**What's NOT delivered (the actual issue scope):**

| Task from issue | Status | Details |
|-----------------|--------|---------|
| Update `.squad/failure-recovery.md` to call notification on escalation | ❌ NOT DONE | Line 69 still says generic "Post to Teams webhook" — no reference to `notify.ps1` or `notify-blocked.ps1` |
| Create notification template for 'agent escalation' type | ⚠️ PARTIAL | Scheduler has `build-failure` template but no `agent-escalation` template. MVP plan designs `notify-blocked.ps1` but script doesn't exist |
| Tests for escalation → notification flow | ❌ NOT DONE | Tests cover recovery infrastructure only, not the escalation→notification integration |
| When agent escalates, notification is sent immediately | ❌ NOT DONE | No caller exists. Zero production invocations |
| Notification includes agent name, blocker description, link | ⚠️ DESIGNED ONLY | MVP plan specifies exact payload for `notify-blocked.ps1` — but the script was never created |

**Root cause:** PR #125 built the recovery *infrastructure* (dead letter queue, retry, health check) but not the *integration* (wiring escalation triggers to call `notify.ps1`). The issue title says "Integration with failure recovery" — what was delivered is "recovery infrastructure for the notification system." Those are different things.

**Recommendation:** ⚠️ **KEEP OPEN — rework scope.** Rewrite the remaining checklist to:
1. Create `notify-blocked.ps1` (fully designed in MVP plan)
2. Update `.squad/failure-recovery.md` to reference `notify-blocked.ps1` at each escalation trigger
3. Test: manually trigger an escalation and verify Teams card arrives

---

### Issue #117 — Phase 1.3: Integration with PR/review workflows
**State:** OPEN
**PR:** #127 (MERGED — delivered `notification-scheduler.ps1`)
**Completion:** 30%

**What was delivered:**
- `scripts/notification-scheduler.ps1` (483 lines) — event-driven trigger system
  - Register/unregister/list triggers
  - 6 built-in event templates: `icm-urgent`, `icm-action`, `pr-review`, `build-failure`, `feature-complete`, `ralph-round`
  - Hot-reload config from `~/.squad/notifications/triggers.json`
  - `Build-EventFromTemplate`: transforms raw event data into `notify.ps1`-compatible payloads
  - `Invoke-ScheduledNotification`: look up trigger, build payload, call `notify.ps1`
  - `Initialize-DefaultTriggers`: idempotent registration of all 6 defaults
- `tests/notification-scheduler.Tests.ps1` — unit tests

**What's NOT delivered (the actual issue scope):**

| Task from issue | Status | Details |
|-----------------|--------|---------|
| Hook into GitHub PR review completion events | ❌ NOT DONE | `pr-review` template exists in scheduler but nothing fires it. No GitHub webhook handler, no polling, no integration with Galadriel's review flow |
| Detect when PR is blocked on Jonathan's approval | ❌ NOT DONE | No detection logic exists anywhere |
| Send notification with PR link and summary | ⚠️ TEMPLATE ONLY | Template at `Build-EventFromTemplate "pr-review"` produces correct payload, but nobody calls it |

**Root cause:** Same pattern as #116 — infrastructure built, integration missing. PR #127 delivered a scheduler *framework* but not the PR/review *integration*. The issue title says "Integration with PR/review workflows."

**Recommendation:** ⚠️ **KEEP OPEN.** This is legitimately incomplete. The remaining work is:
1. Wire Galadriel's review completion to fire `Invoke-ScheduledNotification -EventName "pr-review-complete"`
2. Wire the coordinator to detect "PR awaiting Jonathan's approval" and fire the trigger
3. Test: Galadriel approves a PR → Jonathan gets a Teams card

---

### Issue #120 — Documentation & runbooks
**State:** OPEN
**Completion:** 50%

**What exists:**
- `docs/guides/notifications-guide.md` — user-facing guide (setup, usage, troubleshooting)
- `docs/designs/proactive-notifications.md` — full design doc
- `docs/designs/notifications-status-assessment.md` — honest gap analysis
- `docs/designs/notifications-tamir-xref.md` — cross-reference validation
- `docs/designs/notifications-mvp-plan.md` — MVP implementation plan with dry-run commands

**What's missing:**
- Runbook for webhook setup (partially in notifications-guide.md)
- Admin guide for managing triggers
- Troubleshooting guide for common failures
- The documentation describes a system nobody uses yet — hard to write troubleshooting for a system with zero production traffic

**Recommendation:** ⚠️ **KEEP OPEN but deprioritize.** Documentation for an unwired system is premature. Complete after at least one production caller is wired. The existing notifications-guide.md is adequate for now.

---

## Cross-Cutting Gaps (Not Covered by Any Issue)

### Gap 1: `notify-feature-complete.ps1` doesn't exist
**Designed:** Yes — fully specified in `notifications-mvp-plan.md` with exact parameter signatures, invocation examples, and dry-run commands.
**Built:** No.
**Impact:** The coordinator cannot notify Jonathan when a feature track completes. This was the #1 trigger in the original ask ("I expected a notification when the notification track completed").

**Recommendation:** Create new issue: "Create `notify-feature-complete.ps1` caller script" (Gimli, P0)

### Gap 2: `notify-blocked.ps1` doesn't exist
**Designed:** Yes — fully specified in MVP plan with 5 escalation scenarios and exact payload mappings.
**Built:** No.
**Impact:** The failure recovery pipeline has no notification path. Jonathan won't know when work is blocked.

**Recommendation:** This falls under #116's scope (reworked). No new issue needed if #116 is reworked.

### Gap 3: Zero production callers for notify.ps1
**Current state:** `icm-scan.ps1` (line 286) calls `send-teams-notification.ps1`. `squad-daily-summary.ps1` (line 56) calls `send-teams-notification.ps1`. Nothing calls `notify.ps1`.
**Impact:** 1,475 lines of dead code. Two notification systems coexist; only the 56-line one works.

**Recommendation:** Create new issue: "Migrate `icm-scan.ps1` to call `notify.ps1`" (Gimli, P0). This is the single most valuable action — it proves the pipeline end-to-end with a real caller.

### Gap 4: Coordinator not wired to fire notifications
**Designed:** Yes — MVP plan Issue 3 specifies exactly when the coordinator should call `notify-feature-complete.ps1` and `notify-blocked.ps1`.
**Built:** No. The coordinator template (squad.agent.md) has no notification hooks.

**Recommendation:** Create new issue: "Wire coordinator to call notification scripts" (Gandalf, P1). This is the behavioral wiring — updating the coordinator prompt to include notification calls.

### Gap 5: No end-to-end validation
**Designed:** Yes — MVP plan Issue 4 specifies 5 validation checks.
**Built:** No.
**Impact:** Nobody has verified the full pipeline works from trigger to Teams card.

**Recommendation:** Create new issue: "End-to-end notification validation" (Gimli, P1). Gate the entire track on this.

### Gap 6: No standalone Teams message template files
**Designed:** Issue #112 deliverables say "Teams message templates for each notification type."
**Built:** Templates are embedded as functions in `notify.ps1` (`Build-UrgentCard`, etc.) and as event transformers in `notification-scheduler.ps1` (`Build-EventFromTemplate`). No standalone template files.
**Impact:** Low. The embedded approach is actually better — templates are co-located with the code that uses them.

**Recommendation:** No action. Embedded templates are fine. This deliverable is satisfied in spirit.

---

## Recommended Actions (Priority Order)

| # | Action | Issue | Owner | Priority |
|---|--------|-------|-------|----------|
| 1 | Close #112 (design delivered) | #112 | Gandalf | Now |
| 2 | Close #115 (webhook + templates delivered) | #115 | Gandalf | Now |
| 3 | Create `notify-feature-complete.ps1` | NEW | Gimli | P0 |
| 4 | Rework #116 → create `notify-blocked.ps1` + update failure-recovery.md | #116 | Gimli | P0 |
| 5 | Create issue: migrate `icm-scan.ps1` to notify.ps1 | NEW | Gimli | P0 |
| 6 | Create issue: wire coordinator to call notification scripts | NEW | Gandalf | P1 |
| 7 | Create issue: end-to-end validation | NEW | Gimli | P1 |
| 8 | Keep #117 open (PR/review integration genuinely incomplete) | #117 | Gimli | P2 |
| 9 | Keep #120 open, deprioritize (docs for unwired system) | #120 | Bilbo | P3 |

---

## Architecture Status Map

```
WHAT EXISTS (built, tested, no callers):
  notify.ps1               ████████████████████ 100% (563 lines)
  notification-recovery.ps1 ████████████████████ 100% (429 lines)
  notification-scheduler.ps1 ████████████████████ 100% (483 lines)
  Tests (3 files)          ████████████████████ 100%
  Design docs (4 files)    ████████████████████ 100%

WHAT'S MISSING (designed but not built):
  notify-feature-complete.ps1  ░░░░░░░░░░░░░░░░░░░░  0%
  notify-blocked.ps1           ░░░░░░░░░░░░░░░░░░░░  0%
  Coordinator wiring           ░░░░░░░░░░░░░░░░░░░░  0%
  icm-scan.ps1 migration       ░░░░░░░░░░░░░░░░░░░░  0%
  PR/review integration        ░░░░░░░░░░░░░░░░░░░░  0%
  End-to-end validation        ░░░░░░░░░░░░░░░░░░░░  0%

WHAT WORKS (old system, production traffic):
  send-teams-notification.ps1  ████████████████████ 100% (56 lines, 2 callers)
```

---

## Lessons Reinforced

This audit confirms the 2026-03-25 post-mortem findings. Three additional observations:

1. **PR→Issue mapping was misleading.** PR #125 closed against #116 ("failure recovery integration"), but delivered recovery infrastructure, not failure recovery integration. PR #127 closed against #117 ("PR/review integration"), but delivered a scheduler framework, not PR/review integration. The issue titles promised integration; the PRs delivered building blocks.

2. **The MVP plan was the right correction.** The `notifications-mvp-plan.md` correctly identifies the gap and designs two caller scripts. But it was never executed — it's a plan document, not tracked issues.

3. **Issue lifecycle discipline matters.** Issues #115 and #116 have PRs merged against them but remain open because nobody checked the acceptance criteria against the actual deliverables. If the acceptance criteria had been checked, we'd have caught the gap immediately.

---

## Decision

- **Close #112 and #115** — deliverables complete.
- **Rework #116** — narrow to: create `notify-blocked.ps1`, update `failure-recovery.md`, test escalation flow.
- **Keep #117 and #120 open** — genuinely incomplete.
- **Create 3–4 new issues** for gaps not covered: `notify-feature-complete.ps1`, `icm-scan.ps1` migration, coordinator wiring, end-to-end validation.
- **The critical path is:** `notify-feature-complete.ps1` + `notify-blocked.ps1` → migrate one caller → validate end-to-end. Estimated effort: 4–6 hours of Gimli's time.

— Gandalf

---

# Reflection: Notification Track — Built the Plumbing, Never Connected It

**Date:** 2026-03-25  
**By:** Gandalf (Lead)  
**Trigger:** Jonathan expected a notification that the notification track was complete. He didn't get one. He pointed out the webhook IS configured and notifications already work — so what broke?  
**Severity:** HIGH — this is a process failure, not a tooling failure.

---

## Phase 1: Learning Target

**Scope:** Team-wide  
**What happened:** We delivered 3 PRs (#122, #125, #127) building a sophisticated notification pipeline (`notify.ps1`, `notification-recovery.ps1`, `notification-scheduler.ps1`). The code is merged, tested, documented. But nobody — no agent, no script, no coordinator — ever *calls* `notify.ps1`. The plumbing is connected to nothing.

Meanwhile, the **existing** notification system (`send-teams-notification.ps1`) has been working this whole time. `icm-scan.ps1` calls it (line 286). `squad-daily-summary.ps1` calls it (line 56). Jonathan already gets Teams notifications through this system. We knew it existed — the design doc *references it* at line 675.

---

## Phase 2: Analysis — What Signals Did We Miss?

### Signal 1: The design doc explicitly listed integration work as Phases 1–2

The approved design (`docs/designs/proactive-notifications.md`, lines 619–635) clearly separates:
- **Phase 0:** Build `notify.ps1` ← ✅ We did this (PR #122)
- **Phase 1:** Wire `squad-scheduler.ps1` to call `notify.ps1` on task failure ← ❌ Never done
- **Phase 2:** Update Galadriel spawn, failure recovery pipeline ← ❌ Never done

We shipped Phase 0 three times over (webhook+templates, failure recovery, scheduler) but **never started Phase 1 or Phase 2**. The integration points at lines 336–405 are code samples showing *exactly* how callers should invoke `notify.ps1`. Nobody implemented them.

### Signal 2: `squad-scheduler.ps1` has zero notification integration

The scheduler (lines 57–77) runs tasks and logs success/failure. On failure (line 75), it logs `FAIL` and moves on. The design doc (line 627) explicitly says: "Wire `squad-scheduler.ps1` to call `notify.ps1` on task failure." This wiring was never added. The scheduler doesn't import, reference, or call `notify.ps1` anywhere.

### Signal 3: The existing system was never migrated

`send-teams-notification.ps1` is a 56-line script that already does webhook delivery with Adaptive Cards. It reads from the same `~/.squad/teams-webhook.url` file. Two systems now exist:

| System | Script | Callers | Status |
|--------|--------|---------|--------|
| **Old (working)** | `send-teams-notification.ps1` | `icm-scan.ps1`, `squad-daily-summary.ps1` | Active, delivering notifications |
| **New (orphaned)** | `notify.ps1` + `notification-recovery.ps1` + `notification-scheduler.ps1` | Nothing. Zero callers. | Merged, tested, never invoked |

The design doc even says at line 675: "Existing notification script: `scripts/send-teams-notification.ps1`" — acknowledging its existence. But the rollout plan never includes a migration step. The old callers were never updated to use the new system.

### Signal 4: No "feature-complete" notification was ever wired

The design doc (lines 387–404) shows exactly how to fire a "feature complete" notification:
```powershell
& "$scriptDir\notify.ps1" -Type "feature" -Event @{
    eventId = "feature:dgrep-cli-auth:..."
    featureTitle = "DGrep CLI: Auth Flow"
    ...
}
```

The irony: the notification track itself completing should have triggered a "feature complete" notification. But the code that would fire that notification... was the very code that had no callers.

### Signal 5: We tested the scripts in isolation, not end-to-end

Tests exist (`tests/notify.Tests.ps1`, `tests/notification-scheduler.Tests.ps1`). They test card formatting, dedup, retry, batching — all internal mechanics. No test validates that an actual caller (scheduler, agent, coordinator) invokes the pipeline. We tested the engine but never checked if anyone turns the key.

---

## Phase 3: Proposed Learnings

### [HIGH] Integration points are first-class deliverables, not afterthoughts

**Source:** Jonathan: "I would have thought I'd get a notification about that :D" — then: "the webhook url was configured long ago and I already do get notifications, so what's wrong?"  
**Target:** Team directive (all agents)

The design doc had 4 explicit integration points with code samples. We built the library but treated the wiring as "someone else's job." In a squad with no persistent memory between sessions, **if integration isn't in the PR, it doesn't exist.** The design's Phase 1 and Phase 2 should have been tracked as separate issues with clear ownership.

**Proposed directive:** When a design doc specifies integration points, each integration point MUST be tracked as a separate issue. The track is not "complete" until callers are wired, not just when the library ships.

### [HIGH] Existing systems must be explicitly migrated or superseded

**Source:** Two parallel notification systems now coexist — old one works, new one is orphaned.  
**Target:** Team directive (all agents)

We built `notify.ps1` without migrating the callers of `send-teams-notification.ps1`. The old script is still the one doing real work. The new system adds dedup, retry, tiered routing, batching, dead letter queues — but none of that matters because nothing calls it.

**Proposed directive:** When building a replacement for an existing system, the PR MUST include migration of at least one existing caller to the new system. If migration is deferred, a tracking issue MUST be created and the track is NOT marked complete.

### [HIGH] "Feature complete" ≠ "code merged" — it means the user can observe it

**Source:** Jonathan expected a notification. He got nothing. From his perspective, the feature doesn't exist.  
**Target:** Team directive (all agents)

We told Jonathan the notification track was complete based on PRs being merged. But "complete" means Jonathan can observe the system working. If the feature has no visible effect, it's not complete — it's just code in the repo.

**Proposed directive:** A feature track is "complete" only when the user can observe its effect. For infrastructure tracks (like notification plumbing), this means at least one end-to-end path must be working: event → pipeline → delivery → user sees it.

### [MED] Don't build new infrastructure without surveying what already works

**Source:** `send-teams-notification.ps1` was already delivering notifications. `icm-scan.ps1` already calls it. Jonathan was already getting Teams cards.  
**Target:** Gandalf history (I approved this design without catching the gap)

The design doc acknowledges the existing script but doesn't address: "Why not just enhance `send-teams-notification.ps1` with retry and dedup?" We built a second, more sophisticated system alongside the first. The sophistication is justified (tiered routing, batching, dead letters) — but we should have started by wiring it to the existing callers.

### [MED] PR review (Galadriel) should check for orphaned code

**Source:** Three PRs were reviewed and approved. None of them had a single caller outside test files.  
**Target:** Galadriel charter improvement

A PR that adds 500+ lines of library code with zero production callers should be flagged. The reviewer should ask: "Who calls this? Where's the integration?" This isn't about blocking the PR — it's about ensuring the next PR (the wiring) is tracked.

---

## Phase 4: Persistence

### Immediate Actions Required

1. **Create tracking issues** for the missing integration work:
   - Wire `squad-scheduler.ps1` to call `notify.ps1` on task failure (design Phase 1)
   - Wire `icm-scan.ps1` to use `notify.ps1` instead of `send-teams-notification.ps1` (migration)
   - Wire `squad-daily-summary.ps1` to use `notify.ps1` (migration)
   - Wire agent spawn (coordinator/Ralph) to call `notify.ps1` on feature-complete
   - Wire failure recovery pipeline to call `notify.ps1` on escalation

2. **Fire a manual notification NOW** to prove the system works end-to-end:
   ```powershell
   .\scripts\notify.ps1 -Type feature -Event @{
       eventId      = "feature:proactive-notifications:2026-03-25"
       featureTitle = "Proactive Notifications Pipeline"
       summary      = "Three-tier notification system (urgent/action/feature) with dedup, retry, dead letter queue, and event-driven scheduler. PRs #122, #125, #127 merged."
       issuesUrl    = "https://github.com/jbenami_microsoft/ms-pa/pulls?q=is%3Apr+notification"
   } -Force
   ```

### Decision Inbox Entry (for Scribe)

**New team directive — Integration-first delivery:**
When a design specifies integration points (callers that must invoke new code), each integration point is a separate deliverable. The track is not complete until at least one production caller is wired. PR reviewers must verify: "Who calls this code outside tests?"

### Gandalf History Entry

```
### 2026-03-25 — Notification Track Reflection (Post-Mortem)

**Context:** Jonathan expected a notification that the notification track completed. He got nothing despite the webhook being configured and notifications working through the OLD system.

**Learnings:**
1. [HIGH] Built notify.ps1 (3 PRs, 500+ lines) but never wired any callers — zero production invocations
2. [HIGH] Existing system (send-teams-notification.ps1) was already working — icm-scan.ps1 and squad-daily-summary.ps1 use it
3. [HIGH] Design doc explicitly listed Phase 1 (scheduler wiring) and Phase 2 (agent wiring) — neither was done
4. [HIGH] "Feature complete" was declared based on code-merged, not user-observable-effect
5. [MED] Two parallel notification systems now coexist — old one works, new one is orphaned
6. [MED] All 3 PRs passed review without anyone asking "who calls this?"
```

---

## Root Cause Summary

**We built a library and called it a feature.** The notification pipeline is well-designed and well-tested — but it's a library with no callers. The design doc explicitly separated "build the engine" (Phase 0) from "wire the engine" (Phases 1–2), and we stopped after Phase 0. Three PRs shipped, three reviews passed, and nobody noticed that the output of all that work is a set of PowerShell functions that nothing invokes.

Jonathan's existing notification system (`send-teams-notification.ps1`) kept working the whole time, which made the gap invisible to us — notifications *were* going out, just not through our new pipeline.

This is not a code bug. It's a **delivery gap**. We confused "code in the repo" with "feature in production."

---


