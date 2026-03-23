---
title: "Threat Intelligence Pipeline Integration Guide"
category: guides
author: Bilbo (Documentarian)
requestedBy: Jonathan
date: 2026-03-24
status: active
tags:
  - guide
  - threat-intelligence
  - integration
  - ti-pipeline
  - tooling
  - investigations
---

# Threat Intelligence Pipeline Integration Guide

**Purpose:** Understand what Sagi's TiExpert PR brings to the squad, how to integrate it after merge, and which investigation types it accelerates.

**Source Material:**
- ADO PR #15064785 (`features/sagimarus/tiexpertagent` branch)
- Elrond's analysis: `.squad/decisions/inbox/elrond-ti-pipeline-integration.md`
- Aragorn's CRI mapping: `.squad/decisions/inbox/aragorn-ti-tools-assessment.md`
- Galadriel's review: `docs/reviews/pr-review-15064785-v2.md`

---

## 1. What's in Sagi's PR

The PR introduces **14 assets** (scripts, SKILL.md files, agent definition) designed to validate and reference the TI pipeline. All assets are in the `features/sagimarus/tiexpertagent` branch of `Sentinel-TiPipeline`.

### Agent Definition (1 file)
| File | Purpose | Status |
|------|---------|--------|
| `.github/agents/tiexpert.agent.md` | GitHub Copilot agent with YAML routing table, tool selection, and command flows for `stix-api`, `bulk-actions`, `file-import`, `ingestion-api`, `all` commands | 🟡 Known bug: BUG-2 |

### Configuration & Helpers (2 files)
| File | Purpose | Status |
|------|---------|--------|
| `.github/scripts/ti-config.ps1` | Shared config: PPE subscription ID, workspace ID, tenant ID, ingestion URL. Dot-sourced by all validation scripts. | 🟡 Known issue: SEC-1 (hardcoded PPE GUIDs) |
| `.github/scripts/ti-helpers.ps1` | Shared helper functions: `Poll-LAQuery` (Log Analytics polling with retry), `Get-ErrorDetail`, `Log-Result` (standardized output). | 🟡 Known bug: BUG-3 (silent exception swallowing) |

### Validation Scripts (6 files)
| File | What It Validates | Automation Risk |
|------|-------------------|-----------------|
| `.github/scripts/validate-stixapi.ps1` | STIX Object CRUD (create, read, update, delete each STIX type via ARM API) | 🔴 BUG-2 (token failure exit code) |
| `.github/scripts/validate-bulkactions.ps1` | Bulk Edit/Delete operations across 6 STIX types; polls for completion | 🔴 BUG-1 (Failed state polling loop) |
| `.github/scripts/validate-fileimport.ps1` | File Import API (STIX bundle JSON upload and ingestion validation) | 🟢 Safe |
| `.github/scripts/validate-stixwebapi.ps1` | STIX Web API endpoints (distinct from ARM-based STIX API) | 🟢 Safe |
| `.github/scripts/validate-ingestionapi.ps1` | TI indicator upload/ingestion pipeline endpoint validation | 🟢 Safe |
| `.github/scripts/validate-all.ps1` | Orchestrator: runs all 5 validators in parallel via `Start-Job`, aggregates results | 🟡 Inherits bugs from children |

### SKILL.md API Reference (6 files)
| File | Coverage | Review Quality |
|------|----------|-----------------|
| `.github/skills/stix-api-operations/SKILL.md` | STIX API operations (fields, validation rules, patterns) | 🟢 **Excellent** — Galadriel: "Comprehensive and clear, no issues found" |
| `.github/skills/bulk-actions-api/SKILL.md` | Bulk Actions API (mutator semantics, `SetTrue`/`SetFalse` behavior) | 🟡 DOC-2 (SetFalse/revoked inconsistency) |
| `.github/skills/file-import-api/SKILL.md` | File Import API (STIX bundle patterns, gotchas) | 🟡 DOC-3 (variable name mismatch in docs) |
| `.github/skills/ingestion-api/SKILL.md` | TI indicator ingestion API guide | 🟢 Safe |
| `.github/skills/stix-web-api/SKILL.md` | STIX Web API operations | 🟢 Safe |
| `.github/skills/validation-orchestration/SKILL.md` | How to run the validation suite | — |

---

## 2. Integration Plan

After the PR merges with Sagi's bug fixes, follow this sequence:

### Phase 1: Repository Setup (After Merge + Bug Fixes)

**1. Update `repo-map.json` in pa-squad**

Add `keyPaths` and investigation-use metadata to the Sentinel-TiPipeline entry:

```json
{
  "name": "Sentinel-TiPipeline",
  "path": "C:\\dev\\ti\\Sentinel-TiPipeline",
  "defaultBranch": "users/joniba/aspire-bdd",
  "description": "Customer TI Pipeline — code, tests, deployment. Also contains TiExpert agent (.github/agents/), 5 PowerShell validation scripts (.github/scripts/validate-*.ps1), 6 SKILL.md API references (.github/skills/), and shared TI config/helpers.",
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

**2. Fetch Sagi's branch in local Sentinel-TiPipeline clone**

```bash
cd C:\dev\ti\Sentinel-TiPipeline
git fetch origin features/sagimarus/tiexpertagent
```

This enables agents to read SKILL.md files from the branch without checking it out.

### Phase 2: Investigation Prompt Integration (P1)

**3. Reference SKILL.md files in Aragorn's TI investigation prompts**

When Aragorn investigates TI-related CRIs, his investigation prompt template should include:

```markdown
## TI API Reference

For API contract and expected behavior, read these files from Sentinel-TiPipeline (branch: features/sagimarus/tiexpertagent):

- `.github/skills/stix-api-operations/SKILL.md` — STIX API operations, field types, validation rules
- `.github/skills/bulk-actions-api/SKILL.md` — Bulk Edit/Delete operations, mutator semantics
- `.github/skills/file-import-api/SKILL.md` — STIX bundle import API patterns
- `.github/skills/ingestion-api/SKILL.md` — TI indicator ingestion pipeline API

These give you the authoritative API contract before diving into Kusto queries or source code research.
```

### Phase 3: Automated Investigation Use (P2 — After All Bugs Fixed)

**4. Adapt validation scripts for investigation reproduction**

Once all bugs are fixed, Aragorn can add a **PPE reproduction step** before Kusto queries:

```
Reproduce in PPE:
  - Run validate-stixapi.ps1 for API behavior tests
  - Run validate-bulkactions.ps1 for bulk operation validation
  - Use Poll-LAQuery to verify end-to-end data propagation

Then: Run Kusto queries to check prod behavior
```

This step isolates "PPE reproduces" vs. "prod-only issue" — a distinction that accelerates root cause analysis.

---

## 3. CRI → Investigation Tool Mapping

This table shows which CRI types benefit from Sagi's tools and **how much** they accelerate investigations.

| CRI Type | Relevant Tools | Investigation Impact | Why |
|----------|---|---|---|
| **ICM 51000000954460** — Revoked TI Indicators | `validate-bulkactions.ps1` + `bulk-actions-api/SKILL.md` + `Poll-LAQuery` | 🟢 **High** | Can reproduce revocation state behavior in PPE in ~5 minutes; SKILL.md clarifies expected vs. actual mutator semantics |
| **ICM 21000000951041** — TAXII Ingestion Failures | `validate-ingestionapi.ps1` + `validate-fileimport.ps1` + `ingestion-api/SKILL.md` | 🟡 **Medium** | File import serves as parity check for TAXII path; isolates bundle format issues from parser issues |
| **ICM 51000000943039** — Upload API pattern_type | `validate-stixapi.ps1` + `stix-api-operations/SKILL.md` | 🟢 **High** | Directly tests Upload API field handling; SKILL.md is authoritative source for accepted values |
| **ICM 21000000917983** — Deleted Watchlist Items | `Poll-LAQuery` in `ti-helpers.ps1` | 🟡 **Partial** | Confirms LA propagation timing post-delete, but watchlist-specific ARM operations not covered |
| **ICM 766937015** — MDTI Premium Connector | None (different layer) | 🔴 **Low** | Tools validate TI pipeline API layer, not data connector configuration |

**Bottom Line for Aragorn:** 3 of 5 active TI CRI types gain meaningful investigation acceleration (revocation, Upload API, TAXII). The tools give reproduction patterns that replace manual Kusto-only investigation.

---

## 4. Known Blockers (Must Fix Before Automated Use)

The PR has **3 blocking bugs** that must be fixed before incorporating scripts into automated investigation workflows. Galadriel's review flags these as `CHANGES_REQUESTED`.

### BUG-1: Poll Loop Never Exits on Failed State
**File:** `.github/scripts/validate-bulkactions.ps1`  
**Impact:** Failed bulk operations waste **~15 minutes** waiting on retry loop instead of failing fast  
**Fix:** Add early exit on `"Failed"` state (in addition to `"Done"`)  
**Before Automated Use:** 🔴 **Required**

### BUG-2: Token Failure Doesn't Set Exit Code
**Files:** `.github/agents/tiexpert.agent.md`, `.github/scripts/validate-stixapi.ps1`  
**Impact:** Authentication failure looks like success in CI/automation (exit code 0 instead of 1)  
**Fix:** Change `return` to `exit 1` on token acquisition failure  
**Before Automated Use:** 🔴 **Required** (silent failures in automation are unacceptable)

### BUG-3: Poll-LAQuery Silently Swallows Exceptions
**File:** `.github/scripts/ti-helpers.ps1`  
**Function:** `Poll-LAQuery`  
**Impact:** Non-transient errors (auth failure, wrong workspace) cause **7.5 minutes of silent waiting** before reporting failure  
**Fix:** Log exceptions; fast-fail on non-transient HTTP codes (401, 403, 404)  
**Before Automated Use:** 🟡 **Recommended** (still useful for read-only reference; fix before using in on-call investigation)

**Status:** All three are assigned to Sagi Marcus as blocking PR review items. Monitor PR for completion.

---

## 5. Safe to Use Now (Read-Only Reference)

These SKILL.md files contain **no bugs** and can be referenced immediately for investigation context — no automated execution needed. Elrond can cite these as the authoritative API contract documentation.

### Tier 1: No Issues Found
- ✅ `stix-api-operations/SKILL.md` — Comprehensive API reference; Galadriel verified clean
- ✅ `ingestion-api/SKILL.md` — No issues found
- ✅ `stix-web-api/SKILL.md` — No issues found
- ✅ `validate-all.ps1` — Read-only orchestration patterns are sound

### Tier 2: Safe with Caveats (Note Limitations When Citing)
- 🟡 `bulk-actions-api/SKILL.md` — DOC-2: `SetFalse` for `revoked` is ignored (limitation). Note this when researching revocation issues.
- 🟡 `file-import-api/SKILL.md` — DOC-3: Variable name mismatch in code example (`$array` vs `@($stixObjects)`). Verify logic against your use case.
- 🟡 `ti-config.ps1` — Safe to read for understanding PPE config topology; don't copy hardcoded values elsewhere

### For Immediate Use
Start citing SKILL.md files **today** in investigation prompts — they're the best TI API documentation available (better than scattered TSGs). Example for Aragorn's prompt:

> For Upload API field validation, read `Sentinel-TiPipeline/.github/skills/stix-api-operations/SKILL.md` (branch `features/sagimarus/tiexpertagent`). It documents expected field types and validation rules.

---

## 6. After PR Merge: Catalog & Automation Roadmap

### Immediate (Week 1 after merge)
1. ✅ Update `repo-map.json` with `keyPaths` and `investigationUse` (see Phase 1, #1)
2. ✅ Fetch Sagi's branch in local Sentinel-TiPipeline clone (see Phase 1, #2)
3. ✅ Add SKILL.md references to investigation prompt templates (see Phase 2, #3)

### Follow-Up (Week 2, after Sagi's bug fixes land)
1. Create reference catalog in `docs/catalogs/ti-validation-toolkit.md` documenting:
   - What each validation script tests
   - How to adapt them for investigation scenarios
   - Which scripts map to which CRI types
   - Expected output format (passCount/failCount, exit codes)

2. Publish "TI Investigation Reproduction Quick Start" in `docs/guides/` with examples:
   - How to run `validate-stixapi.ps1` locally to test Upload API behavior
   - How to use `Poll-LAQuery` to verify LA propagation timing
   - Common PPE reproduction patterns for each CRI type

3. Update `investigations/TASK-INDEX.md` with TI CRI status:
   - Add note to relevant CRIs: "Acceleration toolkit available: See `docs/guides/ti-pipeline-integration-guide.md#cri--investigation-tool-mapping`"

---

## 7. Timeline and Ownership

| Phase | Deliverable | Owner | Timeline | Depends On |
|-------|-------------|-------|----------|-----------|
| **Pre-Merge** | All 3 bugs fixed; PR passes Galadriel's review | Sagi Marcus | Before merge | — |
| **Phase 1** | repo-map.json updated; Sentinel-TiPipeline branch fetched | Gimli (Tool Builder) | Week 1 after merge | PR merge |
| **Phase 2** | SKILL.md refs added to investigation prompts | Gandalf (Lead) | Week 1 after merge | Phase 1 complete |
| **Phase 3** | TI validation toolkit catalog created | Bilbo (Documentarian) | Week 2 after merge | Phase 2 complete, bugs fixed |
| **Ongoing** | Reference SKILL.md in TI investigations | Elrond, Aragorn | Continuous | Phase 2 complete |

---

## Quick Reference: When to Use Each Asset

**Use validation scripts for:**
- Reproducing reported API behavior in PPE before Kusto investigation
- Creating minimal test cases for specific field values or mutator operations
- Verifying pipeline health as baseline for comparison

**Use SKILL.md files for:**
- Understanding API contract (accepted field values, types, validation rules)
- Documenting expected behavior in investigation reports
- Clarifying mutator semantics when researching bulk operations
- Providing authoritative API reference better than TSGs or source code

**Use Poll-LAQuery for:**
- Confirming end-to-end data propagation timing to Log Analytics
- Reconciling "API success" vs. "data appears in queries"
- Distinguishing eventual consistency delays from actual data loss

**Skip (not applicable to investigations):**
- MDTI Premium Connector issues (different service layer)
- Watchlist-specific ARM operations (use ARM WATCHLISTS API, not STIX APIs)

---

## See Also

- [Sentinel-TiPipeline in repo-map.json](../../repo-map.json) — Configuration and keyPaths
- [PR Review: TiExpert PR #15064785](../reviews/pr-review-15064785-v2.md) — Full bug list and recommendations
- [Elrond's TI Pipeline Integration Analysis](.squad/decisions/inbox/elrond-ti-pipeline-integration.md) — Research rationale
- [Aragorn's TI Tools CRI Mapping](.squad/decisions/inbox/aragorn-ti-tools-assessment.md) — Investigation impact analysis

---

**Document Metadata:**
- **Author:** Bilbo (Documentarian)
- **Status:** Active (awaiting PR merge and bug fixes)
- **Last Updated:** 2026-03-24
- **Review Cycle:** Post-merge, then quarterly
