---
title: "Aragorn ICM Investigation — Capability Upgrade Analysis"
date: 2026-03-22
author: Elrond
documentarian: bilbo
category: investigations
tags:
  - investigation
  - icm
  - architecture
  - aragorn
  - agent-capability
status: final
related_issues:
  - "766712513"
related_docs:
  - investigations/icm-766712513-summary.md
  - investigations/icm-766712513-full-report.md
---

# Aragorn ICM Investigation — Capability Upgrade Analysis

**Author:** Elrond (Researcher)  
**Date:** 2026-03-22  
**Requested by:** Jonathan (HIGH PRIORITY)  
**Context:** Jonathan is dissatisfied with the depth of Aragorn's ICM investigation. This report analyzes Jonathan's existing ICM Investigator skill, compares it against Aragorn's current capabilities, and provides a concrete upgrade plan.

---

## 1. ICM Investigator Skill — Full Capability Map

The existing ICM Investigator skill at `MDC-AI-Shared/extensions/personal-ai/skills/icm-investigator` is a **multi-stage, agent-driven investigation pipeline** with 6 specialized sub-agents, 3 commands, and extensive reference material. It is far more sophisticated than Aragorn's current ad-hoc approach.

### 1.1 Architecture Overview

| Component | Count | Purpose |
|-----------|-------|---------|
| **Sub-agents** | 6 | Specialized analysis at each pipeline stage |
| **Commands** | 3 | Start, continue, finalize investigations |
| **Reference docs** | 3 | Context spec, investigation template, quality criteria |
| **Pipeline stages** | 5+1 | Context → Enrichment → Triage → RCA → Remediation (+Postmortem) |
| **Max iterations** | 9 | Across all stages, configurable |

### 1.2 Pipeline Stages & Agent Responsibilities

#### Stage 1: Context Loading (`context-loader`, max 2 iterations)
**Purpose:** Load all available context before analysis begins.

**Key actions:**
1. Resolve queue attachments (workspaces, dashboards, data sources)
2. **Analyze the monitor trigger** — identified as "the single most important step"
   - Extract metric name, threshold, evaluated values, lookback window
   - Find error breakdown (categories, codes, counts)
   - Find affected resources, stack traces
   - Note monitor ID and links (Jarvis, BRAIN, Kusto dashboards)
3. **Load and study the TSG** (Troubleshooting Guide)
   - Analyze TSG URL path segments for issue category
   - Search for TSG content via documentation tools
   - Extract known root causes, diagnostic queries, mitigation steps
4. Load workspace documentation (memory bank, repos, architecture)
5. Get Azure service best practices for each involved service
6. Assess relevance of available repos, identify gaps

**Tools used:**
- `get_workspace_documentation`, `search_workspace_files`, `read_workspace_file`
- `get_icm_context`
- `search_documentation`, `explain_azure_resource`, `get_azure_bestpractices`

#### Stage 2: Enrichment (`enrichment-agent`, max 3 iterations)
**Purpose:** Build comprehensive evidence base via tool-based data gathering.

**Tiered approach (prioritized):**

| Tier | Actions | Tools |
|------|---------|-------|
| **Tier 1** (ALWAYS) | IcM AI summary, impacted services, full context, similar incidents | `get_icm_ai_summary`, `get_icm_impacted_services`, `get_icm_context`, `search_icm` |
| **Tier 2** (if TSG/monitor) | TSG deep-dive, verify metrics with Kusto | `search_documentation`, `query_kusto` |
| **Tier 3** (if context) | Data queries, code search, 1ES deployments | `query_kusto`, `search_workspace_code`, `query_1es` |
| **Tier 4** (time permitting) | Docs, WorkIQ, Azure best practices, extensions | `search_documentation`, `ask_workiq`, `get_azure_bestpractices` |

**Additional enrichment actions:**
- `git_log_recent` — find commits near incident time (24-48h window)
- `git_diff_stat` — which files changed in recent commits
- `query_1es` — ADO builds, deployments, PRs, pipeline status
- `ask_workiq` — escalation emails, Teams chats, deployment announcements
- `query_dashboard_widget` — execute KQL from attached dashboard widgets
- Auto-discovered extension tools (ES Chat, Azure diagnostics)

**Evidence format:** Each evidence item follows strict format:
```
**Evidence**: <short proof description> | Source: <tool> | Impact: <critical/high/medium/low>
```

#### Stage 3: Triage (`triage-agent`, max 2 iterations)
**Purpose:** Classify incident and set investigation direction.

**Critical outputs:**
1. **Incident classification:** True Positive / False Positive / False Negative / Noise
2. **Customer impact assessment:** YES/NO with affected users, symptoms, business impact
3. **Severity evaluation:** Reported vs Assessed severity with justification
4. **Action items:** If severity mismatch, classification issues, or IcM updates needed
5. **Investigation focus areas:** Prioritized recommendations for RCA

**Structured JSON output** parsed by orchestrator to populate investigation record.

#### Stage 4: Root Cause Analysis (`root-cause-analyzer`, max 3 iterations)
**Purpose:** Evidence-based root cause determination.

**Non-negotiable requirements:**
- Every hypothesis MUST be supported by evidence
- Code references MUST include `file:line` citations
- Log analysis MUST include actual log entries
- Metric analysis MUST include specific data points
- Root cause conclusion MUST explain the causal chain

**Process:**
1. Review triage findings + monitor trigger data
2. Analyze logs (error messages, stack traces, exception types, correlation IDs)
3. Review code (suspicious code paths, recent changes, error handling)
4. Analyze metrics (anomalies, correlations, resource exhaustion)
5. Build causal chain: `Initial Trigger → Immediate Effect → Cascading Failures → User Impact`
6. Formulate & test hypotheses (with evidence for/against each)
7. Determine root cause with confidence level (LOW/MEDIUM/HIGH)
8. Document full deduction process

**Structured JSON output** with rootCause, hypotheses, deductionProcess, codeReferences.

#### Stage 5: Remediation Planning (`remediation-planner`, max 2 iterations)
**Purpose:** Actionable fix and prevention plan.

**Output categories:**
- **Immediate actions** (< 1 hour) — stop active impact
- **Short-term fixes** (1-3 days) — address direct cause
- **Long-term solutions** (weeks/months) — prevent class of issues
- **Prevention measures** — process/system improvements

**Each action includes:**
- Specific code file references
- Effort estimates
- Impact rating
- Owner assignment
- Verification criteria

**Structured JSON output** with implementationPriority and successMetrics.

#### Stage 6 (Optional): Postmortem (`postmortem-writer`, max 1 iteration)
**Purpose:** Blameless postmortem document.

**Includes:**
- Executive summary, impact analysis (user/business/service)
- Complete timeline with T-relative timestamps
- Root cause with evidence chain
- Response analysis (what went well / could improve)
- Learnings (technical, process, organizational)
- Action items with owners, ETAs, status
- Investigation metrics (MTTD, MTTI, MTTM, MTTR)
- Related incidents and systemic patterns
- Appendix with queries, code refs, context used

### 1.3 Quality Criteria

Investigation is "completed" when:
- Root cause confidence is medium or high
- Analysis text is substantive (>100 chars)
- At least one evidence piece collected
- At least one action item or recommendation exists

Otherwise status is `requires-more-context`.

---

## 2. Available Tools Aragorn Should Be Using

Aragorn has access to an extensive set of MCP tools in this environment. Here is the full inventory relevant to ICM investigation:

### 2.1 ICM MCP Tools (`icm-*`)

| Tool | Purpose | Aragorn Uses? |
|------|---------|:---:|
| `icm-get_incident_details_by_id` | Full incident metadata | ✅ |
| `icm-get_ai_summary` | AI-generated incident summary | ✅ |
| `icm-get_incident_context` | Discussions, tags, custom fields, monitor trigger | ✅ Partial |
| `icm-get_impacted_services_regions_clouds` | Affected services, regions, clouds | ✅ |
| `icm-get_incident_customer_impact` | Overall customer impact assessment | ✅ |
| `icm-get_impacted_subscription_count` | Count of impacted subscriptions | ✅ |
| `icm-get_support_requests_crisit` | Support requests and CritSits | ✅ |
| `icm-get_incident_location` | Region, AZ, DC, cluster, node details | ✅ |
| `icm-get_similar_incidents` | Similar incidents for pattern matching | ✅ |
| `icm-get_mitigation_hints` | Probable causes and mitigations from history | ✅ |
| `icm-get_impacted_s500_customers` | S500 customer impact check | ❌ |
| `icm-get_impacted_ace_customers` | ACE customer impact check | ❌ |
| `icm-get_impacted_azure_priority0_customers` | Priority 0 / Life-and-Safety customers | ❌ |
| `icm-get_outage_high_priority_events` | High-priority events for the incident | ❌ |
| `icm-is_specific_customer_impacted` | Check if specific customer is impacted | ❌ |
| `icm-get_teams_by_name` / `get_teams_by_public_id` | Team details | ❌ |
| `icm-get_on_call_schedule_by_team_id` | On-call schedule | ❌ |
| `icm-search_incidents_by_owning_team_id` | Search team's incident history | ❌ |
| `icm-get_contact_by_alias` / `get_contact_by_id` | Contact details | ❌ |

### 2.2 Geneva MCP Tools (`geneva-mcp-server-*`)

**These are entirely unused by Aragorn.** They provide direct access to Geneva metrics — the same monitoring system that creates IcM incidents.

| Tool | Purpose | Aragorn Uses? |
|------|---------|:---:|
| `geneva-mcp-server-metrics_handbook` | Learn how to query Geneva metrics correctly | ❌ |
| `geneva-mcp-server-list_metrics` | List available metrics in an account/namespace | ❌ |
| `geneva-mcp-server-list_metric_preaggregations` | Discover queryable dimension sets | ❌ |
| `geneva-mcp-server-list_dimension_values` | Find dimension values for filtering | ❌ |
| `geneva-mcp-server-query_timeseries` | **Query actual metric timeseries data** | ❌ |

**Impact of not using these:** Aragorn cannot verify metric anomalies, check if error rates are recovering, or produce metric-backed evidence. The monitor trigger fired on Geneva metrics — querying them directly is the single most valuable data-gathering action.

### 2.3 Azure Data Explorer / Kusto (`azure-mcp-kusto`)

| Tool | Purpose | Aragorn Uses? |
|------|---------|:---:|
| `azure-mcp-kusto` (hierarchical) | Run KQL queries, list clusters, databases, schemas | ❌ |

**Impact:** For ICM 766712513, the incident included 3 pre-built Kusto queries. Aragorn listed them in the report but **never executed them**. These queries would have revealed the actual failing operations, impacted subscriptions, and whether failures are ARM-side or RP-side.

### 2.4 Eng.ms / Engineering Hub (`enghub-*`)

| Tool | Purpose | Aragorn Uses? |
|------|---------|:---:|
| `enghub-search` | Search eng.ms docs, TSGs, knowledge articles | ✅ |
| `enghub-fetch` | **Fetch full page content from eng.ms** | ❌ |
| `enghub-resolve_service` | Resolve service name to ServiceTree GUID | ❌ |
| `enghub-get_service_nodes` | List content nodes for a service | ❌ |
| `enghub-get_node_tree` | Browse ServiceTree hierarchy | ❌ |
| `enghub-get_source_link` | Get source repo for an eng.ms article | ❌ |

**Impact:** Aragorn found TSGs via search but never fetched their content. The Brain ARM RP Investigation TSG contained step-by-step diagnostic procedures, specific Kusto queries, and known root causes that would have dramatically improved the investigation.

### 2.5 Azure Resource Health (`azure-mcp-resourcehealth`)

| Tool | Purpose | Aragorn Uses? |
|------|---------|:---:|
| `azure-mcp-resourcehealth` (hierarchical) | Resource availability status, service health events | ❌ |

### 2.6 Azure Monitor (`azure-mcp-monitor`)

| Tool | Purpose | Aragorn Uses? |
|------|---------|:---:|
| `azure-mcp-monitor` (hierarchical) | Query Azure Monitor logs and metrics | ❌ |

### 2.7 AppLens Diagnostics (`azure-mcp-applens`)

| Tool | Purpose | Aragorn Uses? |
|------|---------|:---:|
| `azure-mcp-applens` (hierarchical) | AI-powered diagnostics, root cause detection, remediation | ❌ |

**Impact:** AppLens is specifically designed for diagnosing Azure resource issues. It uses conversational AI to detect problems, identify root causes, and recommend remediation. This is the most directly applicable diagnostic tool and Aragorn doesn't use it.

### 2.8 WorkIQ / Microsoft 365 (`workiq-*`)

| Tool | Purpose | Aragorn Uses? |
|------|---------|:---:|
| `workiq-ask_work_iq` | Search emails, Teams chats, meetings, documents | ❌ |

**Impact:** Could find deployment announcements, team discussions about endpoint issues, or escalation context.

### 2.9 Other Relevant Azure Tools

| Tool | Purpose | Aragorn Uses? |
|------|---------|:---:|
| `azure-mcp-documentation` | Search official Azure documentation | ❌ |
| `azure-mcp-get_azure_bestpractices` | Azure best practices and troubleshooting guidance | ❌ |

---

## 3. Gap Analysis: Aragorn vs ICM Investigator

### 3.1 Capability Comparison Table

| Capability | ICM Investigator | Aragorn Current | Gap |
|---|---|---|---|
| **Multi-stage pipeline** | 5 stages with specialized agents, iterative refinement | Single-pass ad-hoc investigation | 🔴 **Critical** — No structured pipeline, no iteration |
| **Monitor trigger analysis** | Extracts metric name, threshold, values, error breakdown | Not performed | 🔴 **Critical** — Misses the WHY of the incident |
| **TSG deep-dive** | Fetches and analyzes TSG content, extracts diagnostic queries | Searches and links TSGs but never reads them | 🔴 **Critical** — Leaves most valuable resource unread |
| **Kusto query execution** | Runs KQL queries against configured data sources | Lists queries but never executes them | 🔴 **Critical** — No actual data analysis |
| **Geneva metric queries** | Queries timeseries data to verify anomalies | Not used at all | 🔴 **Critical** — Cannot verify metrics |
| **Incident classification** | True positive / false positive / false negative / noise | Not performed | 🟡 **High** — No actionable assessment |
| **Customer impact assessment** | Formal YES/NO with affected users, symptoms, business impact | Reports formal metrics only (all zeros) | 🟡 **High** — No real assessment |
| **Severity evaluation** | Compares reported vs assessed severity, flags mismatches | Reports reported severity only | 🟡 **High** — No independent evaluation |
| **Hypothesis formulation** | Multiple hypotheses with evidence for/against each | No hypotheses — just lists possible causes from ICM | 🔴 **Critical** — No analytical reasoning |
| **Causal chain mapping** | Step-by-step failure propagation path | Not performed | 🔴 **Critical** — No root cause tracing |
| **Evidence-based RCA** | Each conclusion supported by logs, metrics, code | Lists "recommended causes" from ICM history only | 🔴 **Critical** — No original analysis |
| **Code analysis** | Searches workspace code, references file:line | Not performed (no code context) | 🟡 **Medium** — Not always needed |
| **Deployment correlation** | Git history, 1ES builds, deployment timeline | Not performed | 🟡 **High** — Misses deployment-caused issues |
| **Communication discovery** | WorkIQ for emails, Teams, meetings | Not performed | 🟡 **Medium** — Misses team context |
| **Remediation planning** | Immediate / short-term / long-term / prevention with effort estimates | Lists recommendations from similar incidents only | 🔴 **Critical** — Not actionable |
| **Action items with owners** | Specific, executable steps with file refs, effort, owner, verification | None | 🔴 **Critical** — No next-steps ownership |
| **Postmortem generation** | Complete blameless postmortem with timeline, learnings, action items | Not offered | 🟡 **Medium** — Missing for resolved incidents |
| **Azure diagnostics** | AppLens, Resource Health, Azure Monitor, Azure best practices | Not used | 🟡 **High** — Misses Azure-native diagnostics |
| **Confidence level** | LOW/MEDIUM/HIGH on root cause determination | None | 🟡 **High** — No intellectual honesty about certainty |
| **Structured output** | JSON blocks parsed by orchestrator for automation | Markdown only | 🟢 **Low** — Functional but less actionable |
| **Quality gating** | Completion criteria: evidence, substantive RCA, action items | No quality gate | 🟡 **High** — Reports may be shallow |
| **Iterative refinement** | Up to 9 iterations across stages, re-runs with new context | Single pass, no iteration | 🔴 **Critical** — No depth escalation |

### 3.2 Summary of Critical Gaps

1. **No data execution** — Aragorn has access to Kusto and Geneva but never queries actual data
2. **No TSG content reading** — Finds TSGs but never reads them (the most valuable investigation resource)
3. **No analytical framework** — No hypotheses, no causal chains, no evidence-based reasoning
4. **No structured pipeline** — Ad-hoc investigation without systematic stages
5. **No actionable output** — Recommendations are vague, no owners, no effort estimates
6. **No incident classification** — Doesn't assess whether the incident is real, noise, or misclassified
7. **No metric verification** — Cannot confirm or deny the anomaly that triggered the incident

---

## 4. Recommended Aragorn Charter Upgrade

### 4.1 Additions to `charter.md`

Add the following sections to Aragorn's charter:

```markdown
## Investigation Methodology

I follow a structured, evidence-based investigation pipeline:

### Stage 1: Context & Triage (ALWAYS)
1. Get full incident details, context, and AI summary
2. **Analyze the monitor trigger** — understand WHY the monitor fired (metric, threshold, values)
3. **Fetch and read TSG content** — don't just link, actually read and extract diagnostic steps
4. Classify the incident: True Positive / False Positive / False Negative / Noise
5. Assess customer impact (YES/NO with details) and evaluate severity (reported vs actual)

### Stage 2: Data Enrichment (ALWAYS attempt)
1. **Run Kusto queries** — execute any pre-built queries from incident enrichment
2. **Query Geneva metrics** — verify the metric anomaly that triggered the incident
3. Check similar incidents and mitigation hints
4. Search eng.ms for TSGs and fetch their content
5. Check Azure Resource Health and AppLens diagnostics
6. Search WorkIQ for related communications

### Stage 3: Root Cause Analysis
1. Formulate hypotheses based on evidence
2. Build causal chain: Trigger → Effect → Cascade → Impact
3. Assign confidence level (LOW/MEDIUM/HIGH)
4. Document deduction process — show reasoning, not just conclusions

### Stage 4: Remediation
1. Immediate mitigations (< 1 hour)
2. Short-term fixes (1-3 days)
3. Long-term prevention (weeks/months)
4. Each action: specific steps, effort estimate, owner, verification criteria

## Tool Usage — Mandatory Checklist

For every ICM investigation, I MUST use these tools:

### Always Use:
- `icm-get_incident_details_by_id` — full incident metadata
- `icm-get_ai_summary` — AI summary
- `icm-get_incident_context` — discussions, monitor trigger, enrichment data
- `icm-get_impacted_services_regions_clouds` — blast radius
- `icm-get_incident_customer_impact` — formal impact metrics
- `icm-get_similar_incidents` — pattern matching
- `icm-get_mitigation_hints` — historical mitigations
- `icm-get_support_requests_crisit` — customer escalations
- `enghub-search` — find TSGs and documentation
- `enghub-fetch` — **read** TSG content, not just link to it

### Attempt When Relevant:
- `azure-mcp-kusto` — execute Kusto queries (especially pre-built ones from incidents)
- `geneva-mcp-server-query_timeseries` — verify metric anomalies
- `geneva-mcp-server-list_metrics` + `list_dimension_values` — discover available metrics
- `azure-mcp-applens` — AI-powered Azure diagnostics
- `azure-mcp-resourcehealth` — resource availability status
- `azure-mcp-monitor` — Azure Monitor logs and metrics
- `workiq-ask_work_iq` — search emails/Teams/meetings for context
- `icm-get_impacted_s500_customers` — check S500 customer impact
- `icm-get_outage_high_priority_events` — high-priority events

## Output Standards

Every investigation report MUST include:
1. **Incident classification** (true positive / false positive / false negative / noise) with reasoning
2. **Customer impact assessment** (YES/NO with affected users, symptoms, impact level)
3. **Severity evaluation** (reported vs assessed, flag mismatches)
4. **Evidence collected** (actual data, not just metadata summaries)
5. **Hypotheses** (multiple, with evidence for/against each)
6. **Root cause determination** with confidence level and causal chain
7. **Actionable remediation** with specific steps, effort estimates, and owners
8. **Open questions** with priority ranking

## Quality Gate

Before finalizing a report, verify:
- [ ] Incident classified (not just described)
- [ ] At least one Kusto query or metric query executed (not just listed)
- [ ] TSG content read and applied (not just linked)
- [ ] Root cause has confidence level
- [ ] Remediation actions are specific and executable
- [ ] Causal chain documented
```

### 4.2 Investigation Runbook

The following runbook should be embedded in Aragorn's operational knowledge:

```markdown
## ICM Investigation Runbook

### Phase 1: Gather (5-10 tool calls)
1. `icm-get_incident_details_by_id(incidentId)` — base facts
2. `icm-get_ai_summary(incidentId)` — AI overview
3. `icm-get_incident_context(incidentId)` — monitor trigger, discussions
4. `icm-get_impacted_services_regions_clouds(incidentId)` — blast radius
5. `icm-get_incident_customer_impact(incidentId)` — impact metrics
6. `icm-get_support_requests_crisit(incidentId)` — SRs/CritSits
7. `icm-get_similar_incidents(incidentId)` — patterns
8. `icm-get_mitigation_hints(incidentId)` — historical mitigations
9. `enghub-search(query: "<service name> troubleshooting TSG")` — find TSGs
10. `enghub-fetch(url: "<TSG URL from search>")` — read TSG content

### Phase 2: Enrich (3-8 tool calls)
11. Execute pre-built Kusto queries from incident enrichment via `azure-mcp-kusto`
12. `geneva-mcp-server-query_timeseries` — verify the metric anomaly
13. `azure-mcp-applens` — run AppLens diagnostics
14. `azure-mcp-resourcehealth` — check resource health
15. `icm-get_impacted_s500_customers(incidentId)` — S500 check
16. `icm-get_outage_high_priority_events(incidentId)` — priority events
17. `workiq-ask_work_iq` — search for related communications
18. `azure-mcp-monitor` — query Azure Monitor data

### Phase 3: Analyze
- From monitor trigger: What metric fired? What threshold was violated?
- From Kusto results: What operations are failing? How many subscriptions affected?
- From TSG: Does this match a known root cause?
- From similar incidents: What was the resolution?
- From Geneva metrics: Is the issue recovering or worsening?
- Formulate 2-3 hypotheses, evaluate evidence for/against each

### Phase 4: Report
- Classify incident, assess customer impact, evaluate severity
- Present root cause with confidence level and causal chain
- Provide actionable remediation with effort estimates and owners
- List open questions with priority
```

### 4.3 Output Format Improvements

The report format should be upgraded to include:

1. **Classification block** at the top (before executive summary)
2. **Evidence section** with actual data points, not just references
3. **Hypothesis evaluation** — structured for/against analysis
4. **Causal chain** — visual failure propagation
5. **Remediation** with owner, effort, verification for each action
6. **Confidence level** on the overall analysis

---

## 5. Example: How Aragorn Should Have Investigated ICM 766712513

### What Aragorn Actually Did

| Step | Action | Result |
|------|--------|--------|
| 1 | Got incident details | ✅ Good |
| 2 | Got AI summary | ✅ Good |
| 3 | Got impact metrics | ✅ Good (but all zeros — didn't investigate further) |
| 4 | Got similar incidents | ✅ Good |
| 5 | Got mitigation hints | ✅ Good |
| 6 | Got support requests | ✅ Good |
| 7 | Searched eng.ms | ✅ Found TSGs |
| 8 | Listed pre-built Kusto queries | ❌ Listed but never ran them |
| 9 | Noted FCM change data link | ❌ Noted but never checked it |
| 10 | Wrote report | ❌ Described the incident but didn't investigate it |

### What Aragorn SHOULD Have Done

#### Step 1: Context Loading (same as current — keep this)
```
icm-get_incident_details_by_id(766712513) — ✅ Already doing this
icm-get_ai_summary("766712513") — ✅ Already doing this
```

#### Step 2: Deep Context (NEW — currently missing)
```
icm-get_incident_context("766712513")
  → Look for monitor trigger/watchdog report in discussion entries
  → Extract: What metric fired? What threshold? What values?
  → Extract: Error categories, stack traces, affected resources
  → Extract: Embedded Kusto query links, Jarvis health page, BRAIN links
```

**Expected finding:** The ADROCS monitor detected HTTP 5xx error rates exceeding threshold on `MICROSOFT.SECURITYINSIGHTS/WATCHLISTS` for endpoint `prd-weu-402.sentinel.microsoft.com`. Specific error categories and counts would be in the enrichment tables.

#### Step 3: Read the TSGs (NEW — currently missing)
```
enghub-search("ARM RP troubleshooting 5xx error rate resource provider")
enghub-fetch("https://eng.ms/docs/products/arm/troubleshooting/livesites/tsgs/rps/overview")
enghub-fetch("https://eng.ms/docs/products/brain/brain-detection/arm-rp-investigation-tsg")
```

**Expected finding:** The Brain ARM RP Investigation TSG contains:
- Specific KQL queries for error counts, failure causes
- How to distinguish Gateway (ARM-side) vs Service (RP-side) failures
- Known root causes for RP error rate spikes
- HTTP 0 errors are timeouts from ARM→RP (RP's responsibility)
- Step-by-step investigation procedure

#### Step 4: Execute the Kusto Queries (NEW — THE BIGGEST GAP)

The incident included 3 pre-built queries. Aragorn should have run them:

```
azure-mcp-kusto: "learn" → discover available commands
azure-mcp-kusto: Execute Query 1 — Top Failing Endpoints
  → Filter: httpStatusCode >= 500 or == 0, RP = MICROSOFT.SECURITYINSIGHTS
  → Time: 2026-03-22T22:09:02Z to 2026-03-22T22:49:02Z
  → Result: Which specific operations are failing (GET/PUT/DELETE on WATCHLISTS?)
  
azure-mcp-kusto: Execute Query 2 — Sample Correlation IDs
  → Filter: prd-weu-402.sentinel.microsoft.com hostname
  → Result: Specific failing request IDs for deep-dive

azure-mcp-kusto: Execute Query 3 — Impacted Subscriptions
  → Filter: same hostname + RP
  → Result: How many subscriptions are actually affected (vs the 0 in formal metrics)
```

**Expected outcome:** Actual data about which operations fail, how many subscriptions are hit, whether failures are Gateway or Service side. This transforms the investigation from "we know there are errors" to "we know exactly what's failing and who's affected."

#### Step 5: Query Geneva Metrics (NEW — currently missing)

```
geneva-mcp-server-metrics_handbook() → learn query rules
geneva-mcp-server-list_metrics(accountName: "ADROCS related account", namespaceName: "ARM RP metrics")
  → Find the metric that triggered the alert

geneva-mcp-server-query_timeseries(
  accountName: "<ADROCS account>",
  namespaceName: "<ARM namespace>",
  query: {
    duration: "2h",
    seriesQueries: [{ metricName: "ErrorRate", dimensions: [
      { key: "ResourceProvider", value: "MICROSOFT.SECURITYINSIGHTS" },
      { key: "ResourceType", value: "WATCHLISTS" }
    ]}]
  }
)
```

**Expected outcome:** Actual metric timeseries showing when errors started, peak error rate, and whether the issue is recovering. This is the definitive answer to "is this still happening?"

#### Step 6: Azure Diagnostics (NEW — currently missing)

```
azure-mcp-applens: Diagnose the Azure Sentinel resource in West Europe
  → AI-powered root cause detection
  → Remediation recommendations

azure-mcp-resourcehealth: Check resource health for Sentinel in westeurope
  → Current availability status
  → Recent health events
```

#### Step 7: Check for High-Priority Impact (NEW — currently missing)

```
icm-get_impacted_s500_customers(766712513) → Any S500 customers affected?
icm-get_impacted_azure_priority0_customers(766712513) → Any P0/Life-Safety customers?
icm-get_outage_high_priority_events(766712513) → Any high-priority events?
```

#### Step 8: Search Communications (NEW — currently missing)

```
workiq-ask_work_iq("recent issues with prd-weu-402 sentinel watchlists endpoint West Europe")
  → Find deployment announcements, team discussions, or escalation emails
```

#### Step 9: Classify and Analyze (NEW — currently missing)

Based on all collected evidence, Aragorn should have produced:

**Incident Classification:** True Positive — confirmed 5xx error spike on production Sentinel endpoint affecting customer-facing WATCHLISTS API.

**Customer Impact Assessment:**
- **Customer Impact:** YES (likely)
- **Symptoms:** Customers calling WATCHLISTS API in West Europe receiving 5xx errors
- **Affected Users:** Unknown until Kusto query results, but endpoint is customer-facing
- **Impact Level:** Significant (production API failures)

**Severity Evaluation:**
- **Reported:** Sev 2
- **Assessed:** Sev 2 (correct — degraded API, not full outage; endpoint-specific, not region-wide)

**Hypotheses:**
1. **Transient application timeouts** — Evidence: ICM 757455363 (identical endpoint + resource type) resolved as transient timeouts. Confidence: HIGH.
2. **Customer-driven load spike** — Evidence: ICM 699318024 showed single-customer throttling pattern. Need Kusto data to verify.
3. **Backend infrastructure issue** — Evidence: Cosmos DB instability pattern from ICM 598566983. Need Kusto data to verify.

**Root Cause (Preliminary):** Most likely transient application timeouts on `prd-weu-402.sentinel.microsoft.com` affecting WATCHLISTS API, matching historical pattern from ICM 757455363. Confidence: MEDIUM (pending Kusto query results).

**Causal Chain:**
```
Transient timeout on Sentinel backend (prd-weu-402)
  → WATCHLISTS API requests to ARM return HTTP 5xx/0
  → ADROCS monitor detects error rate threshold violation
  → IcM auto-created at Sev 2
  → 3 correlations in 7 minutes (hitCount=3)
```

#### Step 10: Actionable Remediation (NEW — currently missing)

**Immediate Actions:**
1. **Run the Kusto queries** to determine actual impact scope — Effort: 5 min — Owner: On-call
2. **Check if errors are self-resolving** by querying current metric values — Effort: 5 min — Owner: On-call
3. **Monitor hit count trend** — if stabilizing, likely transient — Effort: ongoing — Owner: On-call

**If Errors Persist (> 30 min):**
4. **Investigate Sentinel backend health** for prd-weu-402 — Effort: 30 min — Owner: Sentinel team
5. **Check for recent deployments** to prd-weu-402 via FCM change data — Effort: 10 min — Owner: On-call
6. **Engage Sentinel platform team** if backend issue confirmed — Effort: N/A — Owner: On-call → Sentinel

**Long-Term (if pattern confirmed):**
7. **Investigate systemic reliability** of prd-weu-402 endpoint (4+ past incidents) — Owner: Sentinel platform team
8. **Consider redundancy/failover** for West Europe WATCHLISTS traffic — Owner: Sentinel architecture team

### Report Quality Comparison

| Quality Dimension | Aragorn's Report | Ideal Report |
|---|---|---|
| Length | ~220 lines | ~350+ lines |
| Data points cited | 0 (all from ICM metadata) | 10+ (Kusto results, metrics, TSG findings) |
| Original analysis | None (transcribes ICM tools) | Hypotheses, causal chain, classification |
| Actionable items | "Run the Kusto queries" (vague) | Specific queries with expected outcomes |
| Confidence level | None stated | MEDIUM with justification |
| TSG application | Linked but unread | Read, summarized, applied to case |
| Classification | Not performed | True Positive with reasoning |
| Impact assessment | Reports formal zeros | Assesses likely real impact |
| Severity evaluation | Reports as-is | Evaluates independently |

---

## 6. Implementation Priority

| Priority | Change | Impact |
|----------|--------|--------|
| 🔴 P0 | Add Kusto query execution to Aragorn's investigation workflow | Transforms from metadata-reader to actual investigator |
| 🔴 P0 | Add `enghub-fetch` for TSG content reading | Applies domain expertise from TSGs |
| 🔴 P0 | Add incident classification and hypothesis framework | Enables analytical reasoning |
| 🟡 P1 | Add Geneva metric querying | Verifies anomalies from source-of-truth |
| 🟡 P1 | Add AppLens/Resource Health diagnostics | Azure-native diagnostic capability |
| 🟡 P1 | Add structured investigation pipeline in charter | Prevents shallow single-pass investigations |
| 🟢 P2 | Add WorkIQ communication discovery | Finds team context and deployment info |
| 🟢 P2 | Add postmortem generation capability | Completes the investigation lifecycle |
| 🟢 P2 | Add S500/ACE/P0 customer impact checks | Catches high-profile customer exposure |

---

*Report prepared by Elrond (Researcher) for pa-squad. Based on exhaustive analysis of 15 files totaling ~2,500 lines in the ICM Investigator skill.*
