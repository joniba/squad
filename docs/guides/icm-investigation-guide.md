---
title: "ICM Incident Investigation: Structured Root Cause Analysis"
date: 2024-12-19
author: Gimli
documentarian: Bilbo
category: guide
tags: [guide, livesite, icm, tooling, bilbo, final]
status: final
---

# ICM Incident Investigation: Structured Root Cause Analysis

Learn how Aragorn (and any agent doing incident response) conducts structured, evidence-based ICM incident investigations using MCP tools and proven methodology.

## What It Does

This guide teaches the mandatory incident investigation pipeline that turns raw ICM incident metadata into a **classified, evidenced, and actionable root cause analysis** with specific remediation steps.

The pipeline is:

1. **Stage 1: Triage & Context** — Understand the incident, classify it, assess impact
2. **Stage 2: Data Enrichment** — Execute queries, read TSGs, collect actual evidence
3. **Stage 3: Root Cause Analysis** — Build evidence-backed hypotheses with confidence levels
4. **Stage 4: Remediation** — Define specific, executable fixes

Use this to:
- Avoid vague incident reports
- Execute queries instead of listing them
- Ground conclusions in actual evidence
- Classify every incident (true positive, false positive, etc.)
- Provide leadership with confidence levels

## Prerequisites

- **Copilot CLI** with MCP tools enabled (icm, kusto, geneva, enghub, azure-mcp-*, workiq)
- **GitHub CLI (`gh`)** installed and authenticated
- **Azure subscription context** (for azure-mcp tools)
- **Incident ID** from ICM
- **Time:** Typically 30–60 minutes for a full investigation

## Understanding the Investigation Lifecycle

An incident goes through these phases:

| Phase | Who | Input | Output |
|-------|-----|-------|--------|
| **Detection** | Monitor | Threshold crossed | ICM incident created |
| **Triage** | On-call | Incident metadata | Classification (true/false positive) |
| **Investigation** | Aragorn (you) | ICM + data sources | Root cause + evidence |
| **Remediation** | Team lead + IC | RCA | Action plan, fixes |
| **Post-mortem** | Team | RCA + actions | Lessons learned |

**This guide covers the Investigation phase.**

## Part 1: Stage 1 — Triage & Context (ALWAYS)

### Step 1: Get Full Incident Metadata

```powershell
# Get the incident details
icm-get_incident_details_by_id -incidentId 626495494
```

**What to capture:**
- Severity (Sev1/2/3/etc.)
- Status (active, mitigated, resolved)
- Owning team
- Created/updated timestamps
- Affected service/component

### Step 2: Get AI Summary

```powershell
icm-get_ai_summary -incidentId 626495494
```

Quick narrative of what happened and what actions were taken. Use this as a starting point, but don't rely on it exclusively.

### Step 3: Get Full Incident Context

```powershell
icm-get_incident_context -incidentId 626495494
```

**What to extract:**
- **Monitor trigger**: What metric fired? What threshold was exceeded? What values were evaluated?
- **Enrichment tables**: Error breakdowns, stack traces, affected resource counts
- **Discussions**: Team notes, mitigations attempted, hypotheses discussed

**Critical:** Read the monitor trigger carefully. It tells you WHAT failed, not necessarily WHY.

### Step 4–8: Gather Impact & Scope Data

```powershell
# Blast radius
icm-get_impacted_services_regions_clouds -incidentId 626495494

# Customer impact
icm-get_incident_customer_impact -incidentId 626495494

# Support requests / escalations
icm-get_support_requests_crisit -incidentId 626495494

# Geographic impact
icm-get_incident_location -incidentId 626495494

# Similar incidents (pattern matching)
icm-get_similar_incidents -incidentId 626495494

# Historical mitigations
icm-get_mitigation_hints -incidentId 626495494
```

### Step 9: Classify the Incident

Based on the data above, classify:

| Classification | Definition | Example |
|---|---|---|
| **True Positive** | Real issue, genuine impact, requires investigation | Auth service down, customers can't log in |
| **False Positive** | Alert fired incorrectly, no real issue | Metric spike from probe restart (not user-facing) |
| **False Negative** | Real issue but alert metadata doesn't capture it | Latency spike but % error was low |
| **Noise** | Known/acceptable alert, should be suppressed | Expected load spike at 3 PM |

**Document reasoning:**
- If true positive: "Customers reporting auth failures, 5,200 affected"
- If false positive: "Alert fired on internal metric spike, no customer impact reported"
- If false negative: "P99 latency jumped 5x but error rate stayed <0.1% — classic tail latency issue"

### Step 10: Assess Customer Impact

```
Impact: YES/NO
Affected users: <count>
Symptoms: <what did users see?>
Impact level: Critical / High / Medium / Low
```

**Examples:**
- ✅ "YES — 2,500 users cannot log in (auth unavailable)"
- ✅ "YES — 500 customers report 10–20 sec latency increase (P99 went 200ms → 2000ms)"
- ❌ "NO — Internal metric spike, no customer complaints, no latency increase observed"

## Part 2: Stage 2 — Data Enrichment (ATTEMPT ALL)

### Step 2a: Find and READ TSGs

Don't just link TSGs — fetch and read them:

```powershell
# Find TSGs related to the service
enghub-search -query "Azure Storage troubleshooting TSG timeout error"

# Read the full TSG content
enghub-fetch -url "https://eng.ms/docs/products/azure/storage/troubleshooting/timeout-tsgs"
```

**Extract from TSGs:**
- Known root causes matching your error pattern
- Step-by-step diagnostic procedures
- **Specific Kusto queries** to run
- Mitigation steps and escalation paths

### Step 2b: Execute Kusto Queries

If the incident enrichment data or TSG includes Kusto queries:

```powershell
# Learn Kusto query rules first
azure-mcp-kusto -command "learn_query_rules"

# Execute the query
azure-mcp-kusto -command "execute_query" -cluster "kusto-cluster" -database "db-name" -query "..."
```

**IMPORTANT:** You MUST execute queries yourself. Never list queries for the user to run manually. The results tell you:
- How many operations failed (not just that some failed)
- Error categories and their counts
- Whether it's a gateway issue or service-side issue
- When the failures started and stopped

### Step 2c: Query Geneva Metrics

The monitor that created the ICM fired on Geneva metrics. Verify the anomaly:

```powershell
# Learn Geneva query rules
geneva-mcp-server-metrics_handbook

# Find the metric
geneva-mcp-server-list_metrics -accountName "account" -namespaceName "namespace"

# Discover queryable dimensions
geneva-mcp-server-list_metric_preaggregations -accountName "account" -namespaceName "namespace" -query @{ metricName = "cpu:usage" }

# Query the metric data
geneva-mcp-server-query_timeseries -accountName "account" -namespaceName "namespace" -query @{
    seriesQueries = @( @{ metricName = "cpu:usage"; dimensions = @( @{ key = "host"; value = "prod-01" } ) } )
    startTimeUtc = "2024-12-19T10:00:00Z"
    endTimeUtc = "2024-12-19T11:00:00Z"
    samplingType = "Average"
    aggregationType = "Automatic"
}
```

**What to determine:**
- Is the anomaly still happening or has it recovered?
- When did it start and stop?
- What's the normal vs abnormal value?

### Step 2d: Azure Diagnostics

Depending on the service:

```powershell
# AI-powered diagnostics (BEST CHOICE for most issues)
azure-mcp-applens -intent "diagnose timeout errors in Azure Storage"

# Resource health status
azure-mcp-resourcehealth -command "get_resource_health" -resourceId "/subscriptions/.../resourceGroups/.../providers/Microsoft.Storage/storageAccounts/..."

# Azure Monitor logs/metrics
azure-mcp-monitor -command "query_logs" -query "errors | summarize count() by error_code"

# Official docs for error explanations
azure-mcp-documentation -intent "What does error code 0x80004002 mean in Azure Blob Storage?"
```

### Step 2e: Customer Impact Details (When Severity Warrants)

```powershell
# If high severity, check specific customer tiers
icm-get_impacted_s500_customers -incidentId 626495494
icm-get_impacted_ace_customers -incidentId 626495494
icm-get_impacted_azure_priority0_customers -incidentId 626495494

# If specific customer is mentioned
icm-is_specific_customer_impacted -incidentId 626495494 -customerName "Contoso"

# Subscription count
icm-get_impacted_subscription_count -incidentId 626495494
```

## Part 3: Stage 3 — Root Cause Analysis

### Step 1: Analyze the Monitor Trigger

From `icm-get_incident_context`:

```
Trigger: ErrorRate > 5% (24h lookback)
Current value: 18.2%
Threshold: 5%
Error breakdown:
  - Timeout: 45% (5,200 errors)
  - Unauthorized: 30% (3,400 errors)
  - NotFound: 25% (2,800 errors)
```

**Ask:**
- What error categories dominate? (Timeouts? Authentication? Availability?)
- Is this a service issue or a dependency issue?
- Did a deployment happen before the spike?

### Step 2: Cross-Reference Data Sources

Build a fact table:

| Source | Finding | Confidence |
|--------|---------|------------|
| Monitor trigger | 18.2% error rate (was 2%) at 14:35 UTC | High |
| Kusto query | 5,200 timeout errors in past 6h | High |
| Geneva metrics | CPU on prod cluster went 40% → 95% at 14:33 | High |
| TSG | High CPU can cause connection pool exhaustion | Medium |
| Recent PR | PR #123 merged 14:20 UTC (15 min before spike) | N/A |
| Deployment log | Deployment #456 rolled out to prod at 14:25 UTC | High |

### Step 3: Formulate Multiple Hypotheses

Each hypothesis needs evidence for AND against:

**Hypothesis 1: PR #123 introduced a resource leak**
- ✅ For: PR #123 merged 15 min before spike; CPU jumped at same time
- ❌ Against: PR changed only logging code, not resource allocation; similar spikes happened before
- **Confidence: MEDIUM**

**Hypothesis 2: External dependency (database) is slow**
- ✅ For: Database latency increased at 14:33 (captured in dependency traces)
- ❌ Against: Database team reports no issues; other services connecting to same DB are fine
- **Confidence: LOW**

**Hypothesis 3: Incoming traffic spike**
- ✅ For: RPS increased 3x at 14:30
- ❌ Against: RPS is expected for this time of day (pattern from past 4 weeks)
- **Confidence: MEDIUM**

### Step 4: Build the Causal Chain

```
Causal Chain: PR #123 merged → resource leak in connection pooling
                 ↓
                 Connection pool exhausted
                 ↓
                 New requests get connection timeout
                 ↓
                 Error rate jumps from 2% to 18%
                 ↓
                 Customer impact: 5,000 users see "Service Unavailable"
```

### Step 5: Assign Confidence Level

**HIGH** (90%+)
- Multiple independent evidence sources agree
- Alternative hypotheses are ruled out
- Causal chain is complete and logical

**MEDIUM** (50–89%)
- Evidence suggests a cause but not conclusive
- Some alternative hypotheses remain plausible
- Causal chain has reasonable gaps

**LOW** (<50%)
- Evidence is circumstantial
- Many alternative hypotheses remain plausible
- Causal chain is speculative

For the PR #123 hypothesis: **MEDIUM** — timeline matches but cause-effect isn't 100% certain.

## Part 4: Stage 4 — Remediation

### Immediate Mitigations (< 1 hour)

Stop active impact:

```
Action: Rollback PR #123
Steps:
  1. git revert <PR123-commit-hash>
  2. kubectl deploy -f rollback-manifest.yaml
  3. Monitor error rate for 5 minutes
  4. If error rate drops below 3%, rollback successful
Effort: 10 minutes
Owner: Gimli (on-call)
Verification: Error rate < 3% sustained for 10 minutes
```

### Short-Term Fixes (1–3 days)

Address the direct cause:

```
Action: Review and fix resource leak in PR #123
Steps:
  1. Code review of the connection pool changes
  2. Identify the leak (likely missing close() call)
  3. Fix the code + add unit tests
  4. Deploy to staging, run load test
  5. Deploy to prod during maintenance window
Effort: 4 hours development + testing
Owner: Gimli
Verification: Load test shows stable connection pool, no leak over 1 hour
```

### Long-Term Prevention (Weeks/Months)

Prevent recurrence:

```
Action: Add connection pool monitoring and alerting
Steps:
  1. Add Geneva metric for pool capacity/utilization
  2. Alert if pool utilization > 80%
  3. Add connection pool load testing to CI/CD
  4. Document resource limit best practices
Effort: 2 days
Owner: Gimli
Verification: Monitoring dashboard shows pool utilization, alert triggers on test load spike
```

## Evidence Formatting (Critical)

Each evidence item follows this format:

```
**Evidence**: <Short proof description — what this IS, not what it means>
| Source: <tool used> | Impact: critical/high/medium/low
```

**Good examples:**
- **Evidence**: 5,200 timeout errors in SecurityEvents table (past 6h) | Source: azure-mcp-kusto | Impact: high
- **Evidence**: Error rate 18.4% exceeded 5% threshold in 60-minute lookback | Source: icm-get_incident_context | Impact: critical
- **Evidence**: TSG documents connection pool exhaustion causes timeout errors | Source: enghub-fetch | Impact: high
- **Evidence**: PR #123 (connection pool changes) merged at 14:20, error spike at 14:35 | Source: git log + icm timeline | Impact: medium

**Bad examples:**
- ❌ "The investigation found errors which indicates an issue..." (analysis, not evidence)
- ❌ "We should run this Kusto query..." (not executed, not evidence)
- ❌ "Errors happened" (too vague, not evidence)

## Output Checklist

Every investigation report MUST include:

- [ ] **Incident classification** (true positive / false positive / false negative / noise) with reasoning
- [ ] **Customer impact assessment** (YES/NO with affected users, symptoms, level)
- [ ] **Severity evaluation** (reported vs assessed — flag mismatches)
- [ ] **Evidence collected** (at least 3 independent sources)
- [ ] **Multiple hypotheses** (at least 2, with evidence for/against)
- [ ] **Root cause determination** with confidence level (LOW/MEDIUM/HIGH)
- [ ] **Causal chain** (trigger → effect → cascade → impact)
- [ ] **Actionable remediation** (immediate/short-term/long-term with effort estimates)
- [ ] **Open questions** with priority ranking
- [ ] **Verification plan** (how to confirm fixes worked)

## Real-World Example: Complete Investigation

### Incident #626495494: Auth Service Timeout

**Stage 1 Findings:**
- Reported as Sev2, created 2024-12-19 14:35 UTC
- Error rate jumped from 2% → 18% (timeout errors)
- 5,200 affected requests, ~2,500 users

**Stage 2 Data:**
- Kusto query: 5,200 "connection timeout" errors in past hour
- Geneva metrics: Auth service CPU 40% → 95%, memory 60% → 92%
- Recent PR #123 merged 14:20 (connection pool refactor)
- TSG doc: "Exhausted connection pool causes timeout cascades"

**Stage 3 RCA:**
- Classification: **True Positive** — real customer impact confirmed
- Hypothesis: PR #123 introduced connection pool leak → pool exhausted → timeout cascade
- Confidence: **MEDIUM** (timeline matches, but leak not yet proven in code)

**Stage 4 Remediation:**
- Immediate: Rollback PR #123 (10 min, verify error rate drops)
- Short-term: Fix leak in PR #123 code, redeploy (4 hours)
- Long-term: Add pool monitoring, load testing (2 days)

## Troubleshooting

### "I Don't Have Access to This Tool"

If an MCP tool is unavailable:
- Check that Copilot CLI is installed: `copilot --version`
- Verify tool is available: `copilot -p "list available tools"`
- Escalate to platform team if core tools are missing

### "Query Returned Unexpected Results"

Verify the query:
1. Check the time range (is it the right window?)
2. Check filters (are you filtering for the right service/region?)
3. Ask the tool for schema/dimension documentation
4. Run a simpler query first to validate access

### "Confidence Level is Too Low"

If you reach Stage 3 and confidence is below MEDIUM:
1. Go back to Stage 2 and execute more queries
2. Find TSG docs specific to the error pattern
3. Check if similar incidents provide clues
4. If still LOW, escalate: "Root cause uncertain, requires expert review"

## Related

- `.squad/skills/icm-investigator/SKILL.md` — Authoritative pipeline definition
- `.squad/agents/aragorn/charter.md` — Aragorn's charter and investigation workflow
- `docs/guides/teams-notifications-setup.md` — How to notify teams of investigation results
- `.squad/decisions.md` — Team decisions on investigation standards

## Next Steps

- **For Aragorn (operator):** Use this pipeline for every Sev2+ incident
- **For Gimli (tooling):** Enhance Kusto query templates for common error patterns
- **For Jonathan (team lead):** Review investigation reports for thoroughness and confidence levels
