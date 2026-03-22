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
