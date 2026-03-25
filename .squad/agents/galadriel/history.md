# Project Context

- **Owner:** Jonathan
- **Project:** pa-squad — a personal-assistant squad for everyday tasks, research, documentation, tool building, and livesite support
- **Stack:** General-purpose (not a single-stack project)
- **Role:** Reviewer — quality gate for all PRs and deliverables
- **Created:** 2026-03-23
- **Hired by:** Gandalf (Lead), Issue #13

## Origin

Galadriel was hired based on Elrond's analysis of the coffee-ratings "Bobbie" charter (Issue #12). Key patterns adapted from Bobbie:

- **Ownership model:** Author fixes their own work, reviewer finds issues
- **PR Review Fix Workflow:** Structured 4-step process (read findings → fix → reply with SHA → signal completion)
- **Escalation timing:** 3+ review cycles → escalate to Lead
- **Evidence-driven voice:** Every finding cites file, line, and reasoning
- **Severity scale:** Critical/High/Medium/Low with clear action thresholds

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2025-07-10 — PR #15064785 (TiExpert agent skills and scripts)

**Tool chain for Sentinel-TiPipeline reviews:**
- `ado-search_code` returns infoCode 15 (0 results) for feature branches — branch not indexed by ADO search.
- File access pattern that works: `git fetch origin <branch>` then `git --no-pager show FETCH_HEAD:<path>` from the local clone at `C:\dev\ti\Sentinel-TiPipeline`.
- Always use `--no-pager` on git commands to avoid interactive pager blocking.

**Pattern: exit code vs return in PowerShell scripts:**
- `return` in a script exits the current scope with exit code 0 (success). Automation will not see the failure.
- `exit 1` sets process exit code and is the correct way to signal failure from a `.ps1` script invoked from CI or a parent script.
- Validate this across all scripts in a PR — inconsistency in one indicates risk in others.

**Pattern: polling loops must handle all terminal states, not just success:**
- A loop that only breaks on `Done` will silently exhaust retries on `Failed`. Always add explicit `Failed` early-exit to avoid multi-minute waits and lost failure context.

**Review scope discipline:**
- PR titles and descriptions are not always accurate — always check the full diff stat first (`git diff --name-status`). PRs can contain far more than advertised.
- Undescribed C# feature work in a "scripts only" PR is a scope flag for Gandalf.

**Security hygiene:**
- Hardcoded PPE subscription/workspace GUIDs in source are a Medium finding even in private repos. The pattern creates risk if prod values are later added to the same file. Flag consistently.

### 2026-03-24 — Protocol Recovery (8-Branch Review Cycle)

**Context:** Retroactive review of 8 branches (#105–#108, #119, #132, #134, plus #112 pre-approved) committed without full cycle review (issue #13). Conducted full Cycle 1 review on all 8, documented findings, authors fixed, Cycle 2 re-approved all. All merged.

**Findings & Patterns:**
1. **Domain terminology is a design signal — flag mismatches between issue description and code naming**
   - Example: DGrep issues described "Kusto cluster" and "Kusto database," but DGrep SDK uses MDS endpoints, namespaces, events. No flag from review despite 6 PRs merged against wrong model.
   - Fix: Add to charter — when tech terminology appears in issue descriptions, cross-check against original research and domain docs BEFORE implementing
   - Pattern: This same mismatch pattern appeared in notification track (calling it "integration wiring" when actually "missing callers")

2. **Reviewers need domain context to catch terminology mismatches**
   - Galadriel reviewed 6 DGrep PRs against issue descriptions and found no problems because the code matched the (incorrect) descriptions
   - Fix: Before multi-PR tracks on unfamiliar domains, provide one-paragraph domain brief to reviewer (e.g., "DGrep uses MDS endpoints and dSTS auth, not Kusto clusters and AAD tokens")
   - Pattern: "PR matches issue description" is necessary but not sufficient for quality gate

3. **Domain mismatches often cascade across multiple PRs in same track**
   - DGrep track: 6 PRs, all with same conceptual error (Kusto vs DGrep). Single-PR catch doesn't help if the mental model is baked into the track
   - Mitigation: Require team design review before issue creation when pivoting tech stacks (TypeScript → C#)

4. **Integration points are first-class deliverables**
   - Notification track: Built 3 PRs of notification infrastructure with zero production callers
   - DGrep track: Built query executor without end-to-end delivery validation
   - Fix pattern: For multi-PR tracks, every integration point must be tracked as separate issue + verified before feature complete
   - Reviewer signal: "Who calls this code outside tests?" must be answerable

5. **PR Gate Enforcement Now Wired:**
   - All future PRs require Galadriel review before merge (routing.md Rule 10 now enforced)
   - Issue routing: `squad:galadriel` added to routing table
   - Prevents repeat of protocol violation where branches were committed without review gate

**Learnings for Future Cycles:**
- [HIGH] Before implementing multi-PR tracks in unfamiliar domains, provide domain context brief
- [HIGH] Flag terminology mismatches between issue descriptions and actual domain naming
- [HIGH] Track integration points as first-class deliverables — each must be separate issue
- [MED] Tech stack pivots require design re-review (issue descriptions must be validated against original research)
- [MED] POC findings must gate feature work, not run in parallel


## 2026-03-25: Kusto Guide Issue #156 - Review Quality Gate (2 Cycles)

**Task:** 2-cycle review workflow on Kusto guide documentation (Bilbo-authored, Galadriel review, Bilbo fixes → re-review)

**Cycle 1 Review:** CHANGES_REQUESTED
- **F1:** Missing ICM queries — guide needs sample queries from past incidents for operational context
- **F2:** Typo in authentication section — token resource reference should be `https://kusto.kusto.windows.net

**Cycle 2 Review:** APPROVED
- Bilbo fixed both findings: added sample ICM queries, corrected token resource reference
- Publication-ready for merge

**Review Pattern Impact:**
- 2-cycle review on technical documentation caught both functional gaps (F1) and accuracy errors (F2)
- Operational context (ICM query examples) required for guide credibility — first draft lacked this
- Review gate prevented merge with functional errors that would have caused reader confusion

**Learnings:**
- [HIGH] For infrastructure documentation, sample real-world operational queries are mandatory—not optional polish
- [HIGH] 2-cycle review identifies gaps that single-pass review misses (especially for docs translated from research)
- [MED] Technical accuracy review (F2 token resource) must be separate concern from completeness review (F1 queries)
