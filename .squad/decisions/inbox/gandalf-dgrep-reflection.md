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
