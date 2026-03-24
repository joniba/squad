# ICM Investigator

> Structured, evidence-based incident investigation using all available MCP tools.

## When to Use

Before any ICM investigation, Aragorn (or any agent doing livesite work) MUST read this skill. It defines the mandatory investigation pipeline, tool usage requirements, and output quality standards.

## Core Philosophy

**Investigation is iterative, context-driven, and evidence-based.**

| Avoid | Target |
|-------|--------|
| Listing queries for the user to run | **Running queries yourself** via MCP tools |
| Linking TSGs without reading them | **Fetching and extracting** actionable steps from TSGs |
| Reporting ICM metadata verbatim | **Synthesizing** RCA from multiple data sources |
| Single-pass shallow investigation | **Multi-stage** structured pipeline |
| Vague "possible causes" | **Evidence-backed hypotheses** with confidence levels |

---

## Investigation Pipeline

Follow these stages in order. Every investigation must complete at least Stages 1–3.

### Stage 1: Triage & Context (ALWAYS — 5–10 tool calls)

**Goal:** Understand the incident, classify it, assess real impact.

| # | Action | Tool | Notes |
|---|--------|------|-------|
| 1 | Get full incident metadata | `icm-get_incident_details_by_id` | Severity, status, owning team, timeline |
| 2 | Get AI summary | `icm-get_ai_summary` | Quick narrative overview |
| 3 | Get full context — discussions, monitor trigger, enrichment | `icm-get_incident_context` | **Extract the monitor trigger**: what metric fired, what threshold was violated, what values were evaluated. Look for enrichment tables with error breakdowns, stack traces, affected resources. |
| 4 | Get blast radius | `icm-get_impacted_services_regions_clouds` | Affected services, regions, clouds |
| 5 | Get formal impact metrics | `icm-get_incident_customer_impact` | Subscription counts, customer impact |
| 6 | Get support requests / CritSits | `icm-get_support_requests_crisit` | Customer escalations |
| 7 | Get incident location | `icm-get_incident_location` | Region, AZ, DC, cluster, node |
| 8 | Find similar incidents | `icm-get_similar_incidents` | Pattern matching, recurring issues |
| 9 | Get mitigation hints | `icm-get_mitigation_hints` | Historical probable causes and mitigations |

**After collecting:** Classify the incident:

- **True Positive** — real issue, requires investigation
- **False Positive** — alert fired incorrectly, no real issue
- **False Negative** — real issue but alert metadata is wrong
- **Noise** — known issue, should be suppressed

Assess **customer impact** (YES/NO with details) and **evaluate severity** (reported vs actual — flag mismatches).

---

### Stage 2: Data Enrichment (ALWAYS attempt — 3–8 tool calls)

**Goal:** Gather actual data — metrics, logs, TSG content, diagnostics.

#### 2a. Find and READ TSGs (not just link them)

```
enghub-search(query: "<service name> troubleshooting TSG <error pattern>")
enghub-fetch(url: "<TSG URL from search results>")
```

TSGs are the most valuable investigation resource. They contain:
- Known root causes and their symptoms
- Step-by-step diagnostic procedures
- **Specific Kusto queries** to run for diagnosis
- Mitigation steps and escalation paths

**You MUST fetch and read TSG content, not just link to it.**

#### 2b. Execute Kusto Queries

If the incident includes pre-built Kusto queries (in enrichment data, monitor trigger, or TSG):

```
azure-mcp-kusto → learn available commands first
azure-mcp-kusto → execute the query
```

**You MUST execute queries yourself.** Never list them for the user to run manually. The results tell you what operations are failing, how many subscriptions are affected, and whether failures are on the gateway side or the service side.

#### 2c. Query Geneva Metrics

The monitor that created the IcM fired on Geneva metrics. Query them directly to verify the anomaly:

```
geneva-mcp-server-metrics_handbook()              → learn query rules
geneva-mcp-server-list_metrics(account, namespace) → find the metric
geneva-mcp-server-list_metric_preaggregations(...)  → discover queryable dimensions
geneva-mcp-server-list_dimension_values(...)        → find dimension values for filtering
geneva-mcp-server-query_timeseries(...)             → get actual metric data
```

This answers: "Is the issue still happening? When did it start? Is it recovering?"

#### 2d. Azure Diagnostics

| Tool | When to Use |
|------|-------------|
| `azure-mcp-applens` | AI-powered diagnostics — root cause detection, remediation suggestions. **Most directly applicable diagnostic tool.** |
| `azure-mcp-resourcehealth` | Resource availability status, service health events |
| `azure-mcp-monitor` | Azure Monitor logs and metrics |
| `azure-mcp-documentation` | Official Azure docs for error explanations |
| `azure-mcp-get_azure_bestpractices` | Azure best practices and troubleshooting guidance |

#### 2e. Threat Intelligence (TI) Investigations

**When the incident involves Threat Intelligence services** (e.g., STIX/TAXII ingestion, indicator management, pattern detection, bulk operations), **before running Kusto queries**, read the relevant Sentinel-TiPipeline SKILL.md files to understand API contracts and field semantics:

| SKILL.md File | Use When | Key Content |
|---|---|---|
| `stix-api-operations` | Incident involves STIX object creation, read, update, delete, or field semantics | STIX API operations, field validation rules, supported object types and properties |
| `bulk-actions-api` | Incident involves bulk indicator updates (SetTrue/SetFalse/Clear mutations) or indicator status changes | Bulk action contracts, mutator behavior (especially SetFalse for revoked indicators), transaction semantics |
| `file-import-api` | Incident involves STIX bundle import, file format validation, or ingestion errors | Bundle format requirements, validation rules, import pipeline error codes |
| `ingestion-api` | Incident involves TI indicator ingestion, TAXII server connectivity, or pipeline throughput | Ingestion endpoint contracts, TAXII collection handling, rate limits, failure modes |

**Example TI CRIs benefiting from these references:**
- **ICM 51000000954460** (revoked indicators): `bulk-actions-api` SKILL.md clarifies SetFalse behavior vs. deletion semantics
- **ICM 51000000943039** (pattern_type errors): `stix-api-operations` SKILL.md documents valid pattern_type field values
- **ICM 21000000951041** (TAXII ingestion): `ingestion-api` or `file-import-api` SKILL.md clarifies bundle format and endpoint requirements

**How to use:**
1. If the ICM title, enrichment data, or error messages mention STIX, TAXII, bulk updates, or indicator status, check the relevant SKILL.md in Sentinel-TiPipeline
2. Read the API contract documentation to understand field semantics, validation rules, and error codes **before** constructing Kusto queries
3. Use the API contract to interpret Kusto results (e.g., which error codes indicate validation failures vs. dependency issues)

#### 2f. Additional Context (when relevant)

| Tool | Purpose |
|------|---------|
| `icm-get_impacted_s500_customers` | Check S500 customer impact |
| `icm-get_impacted_ace_customers` | Check ACE customer impact |
| `icm-get_impacted_azure_priority0_customers` | Priority 0 / Life-and-Safety customers |
| `icm-get_outage_high_priority_events` | High-priority events for the incident |
| `icm-is_specific_customer_impacted` | Check if a specific named customer is impacted |
| `icm-get_impacted_subscription_count` | Count of impacted subscriptions |
| `icm-get_teams_by_name` / `icm-get_teams_by_public_id` | Team details, escalation contacts |
| `icm-get_on_call_schedule_by_team_id` | Who is on-call for the owning team |
| `icm-search_incidents_by_owning_team_id` | Team's recent incident history |
| `workiq-ask_work_iq` | Search emails, Teams chats, meetings for incident context |

---

### Stage 3: Root Cause Analysis

**Goal:** Evidence-based root cause determination with confidence level.

#### Process:

1. **Analyze the monitor trigger** — what metric exceeded what threshold? What error categories contributed?
2. **Cross-reference data sources** — Kusto results, Geneva metrics, TSG guidance, similar incidents
3. **Formulate hypotheses** — at least 2, with evidence for/against each
4. **Build causal chain:**
   ```
   Initial Trigger → Immediate Effect → Cascading Failures → User Impact
   ```
5. **Assign confidence level:** LOW / MEDIUM / HIGH
6. **Document deduction process** — show reasoning, not just conclusions

#### Non-Negotiable Requirements:

- Every hypothesis MUST be supported by evidence (actual data, not metadata summaries)
- Root cause conclusion MUST explain the causal chain
- Confidence level MUST be assigned
- Alternative hypotheses MUST be considered and evaluated

---

### Stage 3b: Source Code Research (When RCA Points to a Code Issue)

**Goal:** Confirm or refute the RCA hypothesis by searching the team's service repositories for the affected component.

#### Process:

1. **Read the repo map** — load `.squad/skills/icm-investigator/repo-map.json` to identify which repos are relevant to the affected service or component.
2. **Identify search targets** — extract function names, class names, API endpoint paths, error strings, or config keys from the ICM data, Kusto results, or TSG content.
3. **Search repos with grep/glob** — use the `grep` and `glob` tools to find matching files across the relevant repo paths listed in `repo-map.json`:
   ```
   grep(pattern: "<error class or function name>", path: "<repo path from repo-map.json>")
   glob(pattern: "**/*<component>*", path: "<repo path from repo-map.json>")
   ```
4. **Read key files** — use the `view` tool to read the matching source files. Focus on:
   - The function or class that throws the error seen in the incident
   - Configuration files that control the affected behavior
   - Recent changes (check git log for the file if relevant)
5. **Check for existing fixes** — search for branches that reference the incident or the fix:
   ```powershell
   git -C <repo path> branch -a --list "*<incident keyword>*"
   git -C <repo path> log --oneline -10 -- <affected file>
   ```
6. **Include evidence in the report** — add file paths and code snippets to the RCA section:
   ```
   **Evidence**: Function `ProcessIndicator()` missing null check at line 142 | Source: Sentinel-TiPipeline/src/Pipeline/Processor.cs | Impact: high
   ```

#### When to Use:

- The RCA hypothesis points to a **code bug**, **missing validation**, **config error**, or **deployment issue**
- The ICM references a specific **API endpoint**, **function**, **pipeline step**, or **component name**
- Kusto/Geneva data shows errors originating from a **known service** in the repo map

#### When to Skip:

- The issue is purely infrastructure (Azure platform outage, capacity limits)
- The root cause is external (third-party dependency, network issue)
- No repos in the map are relevant to the affected service

---

### Stage 4: Remediation

**Goal:** Actionable fix and prevention plan.

| Category | Timeframe | Examples |
|----------|-----------|---------|
| **Immediate mitigations** | < 1 hour | Restart services, rollback deployment, increase limits, disable feature flag |
| **Short-term fixes** | 1–3 days | Code fix, config adjustment, add monitoring |
| **Long-term solutions** | Weeks/months | Architecture improvements, auto-scaling, capacity planning |
| **Prevention measures** | Ongoing | Process improvements, load testing, better alerting |

**Each action MUST include:**
- Specific steps (not vague recommendations)
- Effort estimate
- Owner assignment (team/role)
- Verification criteria (how to confirm it worked)

---

## Output Quality Standards

Every investigation report MUST include:

1. **Incident classification** (true positive / false positive / false negative / noise) with reasoning
2. **Customer impact assessment** (YES/NO with affected users, symptoms, impact level)
3. **Severity evaluation** (reported vs assessed — flag mismatches)
4. **Evidence collected** (actual data from tools, not just metadata summaries)
5. **Hypotheses** (multiple, with evidence for/against each)
6. **Root cause determination** with confidence level and causal chain
7. **Actionable remediation** with specific steps, effort estimates, and owners
8. **Open questions** with priority ranking

### Quality Gate — Verify Before Finalizing

- [ ] Incident classified (not just described)
- [ ] At least one Kusto query or metric query **executed** (not just listed)
- [ ] TSG content **read and applied** (not just linked)
- [ ] Root cause has confidence level (LOW/MEDIUM/HIGH)
- [ ] Remediation actions are specific and executable
- [ ] Causal chain documented

---

## Evidence Format

Each evidence item follows this format:

```
**Evidence**: <Short proof description — what this IS, not what it means> | Source: <tool used> | Impact: <critical/high/medium/low>
```

Good examples:
- **Evidence**: 5,200 AuthorizationDenied errors in SecurityEvents table (past 6h) | Source: azure-mcp-kusto | Impact: high
- **Evidence**: ErrorRate 20.4% exceeded 5% threshold in 60-minute lookback | Source: icm-get_incident_context (monitor trigger) | Impact: critical
- **Evidence**: TSG documents known root cause: HTTP 0 errors are ARM→RP timeouts | Source: enghub-fetch | Impact: high

Bad examples:
- ❌ "The investigation found that there were errors which indicates a potential issue..." (analysis, not evidence)
- ❌ Listing a Kusto query without running it (evidence requires actual results)

---

## Tool Quick Reference

### ICM Tools (Always Use)
| Tool | Purpose |
|------|---------|
| `icm-get_incident_details_by_id` | Full incident metadata |
| `icm-get_ai_summary` | AI-generated summary |
| `icm-get_incident_context` | Discussions, monitor trigger, enrichment data |
| `icm-get_impacted_services_regions_clouds` | Blast radius |
| `icm-get_incident_customer_impact` | Formal impact metrics |
| `icm-get_support_requests_crisit` | Customer escalations |
| `icm-get_incident_location` | Region, AZ, DC, cluster, node |
| `icm-get_similar_incidents` | Pattern matching |
| `icm-get_mitigation_hints` | Historical mitigations |

### Data & Metrics Tools (Attempt When Relevant)
| Tool | Purpose |
|------|---------|
| `azure-mcp-kusto` | Execute KQL queries — **run them, don't list them** |
| `geneva-mcp-server-query_timeseries` | Query metric timeseries data |
| `geneva-mcp-server-list_metrics` | Discover available metrics |
| `geneva-mcp-server-list_metric_preaggregations` | Discover queryable dimension sets |
| `geneva-mcp-server-list_dimension_values` | Find dimension values for filtering |
| `geneva-mcp-server-metrics_handbook` | Learn Geneva query rules before querying |

### Documentation & Diagnostics Tools
| Tool | Purpose |
|------|---------|
| `enghub-search` | Find TSGs and documentation on eng.ms |
| `enghub-fetch` | **Read** TSG content — extract actionable steps |
| `enghub-resolve_service` | Resolve service name to ServiceTree GUID |
| `enghub-get_service_nodes` | List content nodes for a service |
| `azure-mcp-applens` | AI-powered Azure diagnostics |
| `azure-mcp-resourcehealth` | Resource availability status |
| `azure-mcp-monitor` | Azure Monitor logs and metrics |
| `azure-mcp-documentation` | Official Azure documentation search |
| `azure-mcp-get_azure_bestpractices` | Best practices and troubleshooting guidance |

### Context & Communication Tools
| Tool | Purpose |
|------|---------|
| `workiq-ask_work_iq` | Search emails, Teams, meetings for context |
| `icm-get_impacted_s500_customers` | S500 customer impact |
| `icm-get_impacted_ace_customers` | ACE customer impact |
| `icm-get_impacted_azure_priority0_customers` | Priority 0 / Life-and-Safety customers |
| `icm-get_outage_high_priority_events` | High-priority events |
| `icm-is_specific_customer_impacted` | Check named customer impact |
| `icm-get_on_call_schedule_by_team_id` | On-call schedule |
| `icm-get_teams_by_name` | Team details |
| `icm-search_incidents_by_owning_team_id` | Team incident history |

---

---

## Scanning & Filtering

### ICM Incident Types

When scanning for specific incident categories, use the correct **Incident Type** field, not severity or tag heuristics. Always filter by the actual incident type value in ICM.

#### Common Incident Types
- **System/Customer Reported** — CRI (Customer Reported Incident). A customer-facing or customer-reported issue, not an automated alert.
- **Alert Derived** — Alert-triggered incident, automated detection.
- **Operational** — Internal operational incidents (on-call, runbook execution, etc.).

### Common Scan Patterns

Use these filter patterns with `icm-search_incidents_by_owning_team_id`. After retrieving results, filter client-side by the target field:

| Scan Goal | Filter Pattern |
|-----------|---|
| **CRIs (Customer Reported)** | `Incident Type == System/Customer Reported`, `State == Active` |
| **Sev 0-2 (High Priority)** | `Severity <= 2`, `State == Active` |
| **Red Flags (AzRF)** | `Tags contains "AzRF"` |
| **Unmitigated Incidents** | `State == Active`, no mitigation applied |
| **High Customer Impact** | `ImpactedSubscriptionCount > 100` or `HasS500Customer == true` |
| **Aged Incidents** | `CreatedTime < now - 24h`, `State == Active` |

### Scanning Instructions for Agents

**When asked to scan for X:**

1. **Determine the correct ICM field** — don't guess. Reference the table above.
2. **Don't use heuristics.** For example:
   - ❌ "High severity = CRI" — CRIs are identified by **Incident Type == System/Customer Reported**, not severity
   - ❌ "Old incidents are sev-2+" — severity is independent of age
   - ✅ "Filter Incident Type field directly"
   - ✅ "Filter State and CreatedTime for aged incidents"

3. **Use `icm-search_incidents_by_owning_team_id`** to fetch broad results, then filter by:
   - `Incident Type` (System/Customer Reported, Alert Derived, Operational)
   - `Severity` (numeric: 0-5)
   - `State` (Active, Mitigated, Resolved, False Positive)
   - `Tags` (string array)
   - `ImpactedSubscriptionCount` (numeric)
   - `HasS500Customer` / `HasACECustomer` / `HasPriority0Customer` (boolean)
   - `CreatedTime` / `LastModifiedTime` (timestamp)

4. **If unsure about the correct field:** Scan broadly first (all incidents), then progressively filter by the dimension that matches the scan goal.

---

## Post-Investigation

After an investigation completes, the report MUST include a `## Priority Assessment` section that documents investigation-informed prioritization:

### Priority Assessment Format

```markdown
## Priority Assessment

**Recommended Priority:** P1 (high)

**Rationale:**
- Customer Impact Scope: Affects 2,400 subscriptions, 12 S500 customers
- Blast Radius: 3 downstream services, Azure Portal + REST API
- Fix Complexity: Medium (config change, requires validation, no code changes)
- Dependencies: Waiting on team X for DNS TTL reduction (3 days)
- Workaround: Temporary rate-limit increase available for affected customers
- Deadline Pressure: Recurring issue; SLA breach if not mitigated within 48h

**Relative Priority:** If other investigations exist:
- vs. Investigation #456 (CRI, Sev 0): This is P2 — more customers in #456, quicker fix path
- vs. Investigation #457 (Sev 2, single customer): This is P1 — broader scope, more S500 impact

**Decision:** P1 because high subscription count, recurring nature, and tight SLA deadline outweigh slightly higher complexity vs. other open items.
```

### Handoff to Bilbo

The Priority Assessment directly informs `docs/investigations/TASK-INDEX.md` ordering:

- **Bilbo reads** the Priority Assessment after Aragorn completes the investigation
- **Bilbo updates TASK-INDEX** with Aragorn's recommended priority (P0–P3), not severity-based sorting alone
- **Bilbo uses relative priority** to break ties within the same category (e.g., multiple CRIs)
- **Bilbo documents divergence** if Aragorn's assessment differs from severity-based ordering

This ensures prioritization is evidence-driven, not just alert-driven.

---

## Adapted From

This skill is adapted from the ICM Investigator at `MDC-AI-Shared/extensions/personal-ai/skills/icm-investigator/`, a multi-stage agent-driven investigation pipeline with 6 specialized sub-agents, 3 commands, and extensive reference material. The original uses VS Code extension APIs (workspace tools, 1ES queries, dashboard widgets). This adaptation targets the Copilot CLI environment with MCP tools available in the session.
