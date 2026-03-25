# Review: TI Kusto Cluster Guide (Issue #156)

**Reviewer:** Galadriel  
**Branch:** `squad/156-kusto-guide`  
**Worktree:** `C:\dev\personal\pa-squad\worktrees\squad-156`  
**Date:** 2026-03-25  
**Deliverables reviewed:**
- `docs/kusto-guide.md` — Bilbo (Librarian)
- `docs/research/kusto-cluster-research.md` — Aragorn (Operator)

---

## Verdict: APPROVED ✅ (Cycle 2)

Both findings from Cycle 1 resolved by Bilbo in commit `0abe7ef`. Guide approved for merge.

---

### Cycle 1 Verdict: CHANGES_REQUESTED

Two findings. One medium (functional gap), one low (typo). Both fixable in minutes. The guide is otherwise excellent — approve once both are addressed.

---

## Checklist Results

| Check | Result | Notes |
|-------|--------|-------|
| Clear, copy-paste ready | ✅ Pass | Quick Start is immediate; all queries are runnable as-is with `<placeholder>` substitution clearly labeled |
| KQL syntax correctness | ✅ Pass | See detailed review below |
| Auth instructions accurate | ✅ Pass | Token resource gotcha documented twice (Quick Start + Known Issues §2) with both correct and incorrect examples |
| MCP Kusto bug workaround | ✅ Pass | Known Issues §1 — symptom, root cause, and REST API workaround with full code |
| Service health use case | ✅ Pass | Use Case 3 covers Sentinel service health; Section 3.2 covers ConnectorService/GatewayService/IngestionApi |
| Pipeline health use case | ✅ Pass | Use Case 2 covers end-to-end throughput, CosmosDB publish health |
| ICM lookup use case | ⚠️ Partial | ARM incident investigation (Use Case 1) is covered; but no KQL queries provided for `icmcluster.kusto.windows.net / IcMDataWarehouse` — see F1 |

---

## Findings

### F1 — Medium — ICM Data Warehouse: Referenced but No Queries Provided

**File:** `docs/kusto-guide.md`, Section 8 ("Next Steps & Related Resources")  
**Line:** 766

**Issue:**
The guide lists `icmcluster.kusto.windows.net / IcMDataWarehouse` as a related resource but provides zero KQL queries for it:

```
- **IcM data warehouse:** `icmcluster.kusto.windows.net` / `IcMDataWarehouse` (for incident history)
```

Aragorn's research confirms this cluster is **✅ Accessible** and used by `icm-scan.ps1` for `IncidentsSnapshotV2()` queries. If "ICM lookup" is a stated use case for this guide, operators need at least one working example.

**Why it matters:** When an operator is mid-incident and wants to look up an IcM by service or pull related incidents from the data warehouse, the guide offers no help. The pointer to the cluster without a query is a dead end under pressure.

**Required fix:**
Add a use case or sub-section — "IcM Data Warehouse Lookup" — with at minimum one copy-paste-ready query. Suggested content:

````markdown
### IcM Data Warehouse Lookup

The IcM cluster is separate from the TI Kusto cluster. Connect using the same auth pattern:

```powershell
$icmToken = az account get-access-token --resource "https://kusto.kusto.windows.net" --query accessToken -o tsv
$icmCluster = "https://icmcluster.kusto.windows.net"
```

```kql
// Look up recent active incidents for TI services
cluster("https://icmcluster.kusto.windows.net").database("IcMDataWarehouse").IncidentsSnapshotV2()
| where OwningServiceName has "ThreatIntelligence" or OwningServiceName has "SecurityInsights"
| where Status == "Active"
| project IncidentId, Title, Severity, OwningTeamName, CreateDate, Status
| order by CreateDate desc
| take 20
```

```kql
// Look up a specific incident by ID
cluster("https://icmcluster.kusto.windows.net").database("IcMDataWarehouse").IncidentsSnapshotV2()
| where IncidentId == <INCIDENT_ID>
| project IncidentId, Title, Severity, Status, ImpactStartDate, MitigateDate, OwningTeamName
```
````

Aragorn can supply the correct `IncidentsSnapshotV2()` schema and field names from `icm-scan.ps1` if the above query needs adjustment.

---

### F2 — Low — Typo: "ARM ARM" Duplicate

**File:** `docs/kusto-guide.md`, Section 3.3 (StixWebApiLogs table description)  
**Line:** 220

**Issue:**
```
- Cross-referencing API errors with ARM ARM watchlist incidents
```

"ARM ARM" is a double word. Should be "ARM watchlist incidents."

**Required fix:** Delete one "ARM".

---

## Detailed Review Notes

### KQL Syntax Assessment — PASS

Reviewed all queries in `docs/kusto-guide.md`. No syntax errors found. Specific patterns verified:

| Pattern | Status |
|---------|--------|
| `extract(@"\|(.+)$", 1, tagId)` — feed name extraction | ✅ Valid regex extract |
| `countif(resultType == "Failure")` | ✅ Valid KQL aggregation |
| `percentile(durationMs, 50)`, `P95`, `P99`, `P999` | ✅ Valid; all four in single summarize |
| `coalesce(IngestionErrors, 0)` | ✅ Valid null-coalesce after leftouter join |
| `between(datetime(<START>)..datetime(<END>))` | ✅ Valid template; placeholders clearly labeled |
| `round(100.0*Errors/Total, 2)` | ✅ Valid arithmetic extension |
| `bin(env_time, 5m)`, `bin(env_time, 1h)` | ✅ Valid time bucketing |
| `startswith "TAXIIActor:"` | ✅ Valid string predicate |
| Cross-table join on `env_time` | ✅ Correct join key for time-bucketed correlation |

**Note on research vs. guide style:** Aragorn's research uses implicit column name `count_` in several queries (e.g., `| order by count_ desc`). Bilbo correctly renamed all aggregations to explicit names (`ErrorCount`, `FailureCount`, `TotalEvents`). This is the right call — named columns are required for the guide to be self-explanatory.

---

### Auth Instructions — PASS

The token resource gotcha is documented **twice** with appropriate weight:

1. **Quick Start** (line 19): Inline comment in the code block: `# ⚠️ CRITICAL: The resource is "https://kusto.kusto.windows.net" — NOT the cluster URI`
2. **Known Issues §2** (lines 588–603): Full section with both correct and explicitly-labeled incorrect examples

This is exactly right. Operators who skip the Quick Start will still hit the Known Issues. ✅

---

### MCP Kusto Bug Workaround — PASS

Known Issues §1 ("Azure MCP Kusto Tool is Broken ❌") covers:
- Symptom (`FileNotFoundException`)
- Root cause (tool-level config issue, not cluster access)
- Full workaround (REST API via PowerShell, both query and mgmt endpoints)

The workaround code in §1 is also the primary usage pattern shown in Quick Start — which means the guide naturally teaches the workaround first. Good design. ✅

---

### Use Case Coverage — MOSTLY PASS

| Requested Use Case | Guide Section | Status |
|--------------------|--------------|--------|
| ARM incident error investigation | Use Case 1 — 4-step query sequence | ✅ Covered |
| Service health (Sentinel) | Use Case 3 + Section 3.2 + Baselines table | ✅ Covered |
| Pipeline health (CosmosDB, EventHub, NormalizationService) | Use Case 2 + Section 3.1 + Baselines table | ✅ Covered |
| TAXII feed monitoring | Use Case 4 + Section 3.4 | ✅ Covered |
| API latency / performance | Use Case 5 | ✅ Covered |
| Correlation across tables | Use Case 6 | ✅ Covered |
| IcM data warehouse lookup | Section 8 (pointer only) | ⚠️ Partial — see F1 |

---

### Research Quality Assessment — PASS

Aragorn's `docs/research/kusto-cluster-research.md` is thorough:

- ✅ All 4 tables inventoried with schemas, row counts, retention windows, and observed data distributions
- ✅ Application-level breakdowns with counts (NormalizationService ~5.9M/hr, etc.)
- ✅ TAXII feed enumeration with feed names and volumes
- ✅ Cross-cluster landscape documented (5 clusters, access status for each)
- ✅ Error rate snapshot data (24-hour hourly breakdown with a key observation: "failure counts remain constant at ~27–32K/hr while volume varies 425K–3.3M/hr")
- ✅ ICM investigation patterns with specific incident context (ICMs 767815474, 767416366)
- ✅ ARM-side limitation clearly flagged (`macro-expand ARMProdEG` not runnable from TI cluster)
- ✅ MCP tool status honestly documented
- ✅ Alert threshold table (Section 6.4)

The observation about failure counts being load-independent is particularly valuable — it points toward a fixed-rate background error source, not a capacity-correlated issue. This was correctly surfaced in both the research and the guide. Good analysis from Aragorn.

---

### One Optional Improvement (Non-Blocking)

**File:** `docs/kusto-guide.md`, Use Case 1  
The `macro-expand ARMProdEG` query block appears inside the "Use Case 1: API Error Investigation" section before the warning that it cannot be run from this cluster. An operator following the numbered steps might attempt to run it before reading the warning. Consider either:
1. Moving the ARM query block *after* the warning (swap order), or
2. Adding a `⚠️ NOT RUNNABLE FROM THIS CLUSTER` comment inside the code block itself

This is a UX suggestion, not a blocking finding. Bilbo's judgment call.

---

## Summary

Aragorn's research is solid. Bilbo's guide is well-structured, copy-paste ready, and correctly documents the critical auth and workaround gotchas. The operational baselines table is production-quality.

Two issues stand between this and approval:
- **F1 (Medium):** Add IcM warehouse queries — referenced cluster, zero queries
- **F2 (Low):** Delete duplicate "ARM" on line 220

**Return to Bilbo with these two findings. Re-submit for Cycle 2 when fixed.**

---

## Cycle 2 Review — 2026-03-25

**Reviewer:** Galadriel  
**Commit reviewed:** `0abe7ef`

### F1 — ICM Queries ✅ RESOLVED

Section 8 now contains a full "ICM Incident Lookup via Kusto" sub-section (lines 769–800) with three copy-paste-ready KQL queries:
- Lookup by incident ID
- Recent high-severity incidents filtered by team name
- 30-day incident trend bucketed by day and severity

Bilbo used `IncidentHistory` as the table name (rather than the `IncidentsSnapshotV2()` suggested in the finding) and added an explicit schema-verification warning: *"Table and field names are templates — verify against the actual schema before first use."* This is the responsible call given schema uncertainty. The gap is closed.

### F2 — Typo ✅ RESOLVED

Line 220 now reads: `Cross-referencing API errors with ARM watchlist incidents`. Duplicate "ARM" removed.

### New Issues

None. No regressions observed; no new problems introduced.

### Cycle 2 Verdict: **APPROVED ✅**

Guide is ready to merge.
