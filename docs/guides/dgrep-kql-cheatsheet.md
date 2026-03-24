# DGrep KQL Cheat Sheet

Quick reference for writing KQL queries against Geneva DGrep. DGrep supports a **subset** of Kusto Query Language — this guide covers what works, what doesn't, and the patterns you'll use most during ICM investigations.

> **Prerequisite:** You should already have the CLI configured. See the [Quick Start](dgrep-quickstart.md) if not.

---

## Query Structure

Every DGrep KQL query starts with `source` as the table reference:

```kql
source
| where <condition>
| project <columns>
```

The `source` keyword refers to the event table specified by `--event`. You cannot reference other tables or use `union`.

---

## Find Errors by Severity Level

Geneva Logs typically use a `Level` column where lower numbers mean higher severity:

| Level | Meaning |
|-------|---------|
| 0 | Critical |
| 1 | Error |
| 2 | Warning |
| 3 | Info |
| 4 | Verbose / Debug |

```powershell
# Critical and error events (Level 0-1)
dgrep search --event MyEvent --from -1h \
  --query "source | where Level <= 1 | project PreciseTimeStamp, Level, Message"

# Warnings and above
dgrep search --event MyEvent --from -1h \
  --query "source | where Level <= 2"

# Only errors (exact match)
dgrep search --event MyEvent --from -1h \
  --query "source | where Level == 1"
```

---

## Search by Time Range

DGrep time ranges are set via CLI flags, **not** via KQL functions.

```powershell
# Last 30 minutes (relative)
dgrep search --event MyEvent --from -30m --query "source | where Level <= 2"

# Last 4 hours
dgrep search --event MyEvent --from -4h --query "source | where Level <= 2"

# Specific time window (UTC)
dgrep search --event MyEvent \
  --from "2026-03-23T10:00:00Z" --to "2026-03-23T11:00:00Z" \
  --query "source | where Level <= 2"
```

> **⚠️ `ago()` does not work.** Do not write `where PreciseTimeStamp > ago(1h)`. Use the `--from` flag instead. DGrep's time filtering happens at the blob selection layer, before your KQL runs.

---

## Filter by Identity Columns

Identity columns (e.g., `Tenant`, `Role`, `Environment`, `RoleInstance`) filter at the **blob level** before your query executes. This is the most powerful performance optimization in DGrep.

```powershell
# Scope to a specific tenant and role
dgrep search --event MyEvent --from -1h \
  --identity "Tenant=WUS" --identity "Role=Frontend" \
  --query "source | where Level <= 2"

# Multiple values for one identity dimension
dgrep search --event MyEvent --from -1h \
  --identity "Tenant=WUS" --identity "Tenant=EUS" \
  --query "source | where Level <= 2"
```

You can also filter identity columns in KQL (but it's slower — the full blob is scanned first):

```kql
source | where Tenant == "WUS" and Role == "Frontend"
```

**Always prefer `--identity` flags over KQL `where` clauses for identity columns.** The `--identity` approach skips entire blobs that don't match, dramatically reducing scan time.

---

## Aggregate Counts by Field

```kql
-- Count events per level
source | summarize count() by Level

-- Count distinct tenants with errors
source | where Level <= 2 | summarize dcount(Tenant)

-- Count errors per role instance
source | where Level <= 1 | summarize count() by RoleInstance | order by count_ desc

-- Count with a condition
source | summarize ErrorCount = countif(Level <= 1), TotalCount = count() by Tenant
```

### ⚠️ The `summarize` Pitfall

**`summarize` in a server query gives partial results, not global aggregates.** DGrep processes each blob independently. Each blob produces its own aggregation, and results from different blobs are concatenated — not merged.

To get correct totals, you need a client query that re-aggregates the server results. This is the CLI's `--client-query` flag (when SDK integration is complete). For now, pipe to `jq` for post-processing:

```powershell
dgrep search --event MyEvent --from -1h \
  --query "source | summarize count() by Level" --output json \
  | jq 'group_by(.Level) | map({Level: .[0].Level, TotalCount: (map(.count_) | add)})'
```

---

## String Operations

### `contains` — Case-insensitive substring match (most common)

```kql
source | where Message contains "timeout"
source | where Message contains "connection refused"
```

### `contains_cs` — Case-sensitive substring match

```kql
source | where Message contains_cs "NullReferenceException"
```

### `startswith` / `endswith`

```kql
source | where RequestUrl startswith "https://api.example.com/v2/"
source | where FileName endswith ".json"
```

### `matches regex` — Regular expression match

```kql
source | where Message matches regex "error.*timeout|timeout.*error"
source | where CorrelationId matches regex "[0-9a-f]{8}-[0-9a-f]{4}"
```

### String comparisons

```kql
-- Case-insensitive equals
source | where Status =~ "failed"

-- Case-sensitive equals
source | where Status == "Failed"

-- Not equals
source | where Status != "Success"

-- In list
source | where Status in ("Failed", "Timeout", "Cancelled")
```

---

## Projection and Computed Columns

### `project` — Select specific columns

```kql
source | project PreciseTimeStamp, Level, Message, CorrelationId
```

### `project-away` — Remove columns

```kql
source | project-away TIMESTAMP, PartitionKey
```

### `extend` — Add computed columns

```kql
source | extend DurationSec = Duration / 1000.0
source | extend IsError = iif(Level <= 1, "Yes", "No")
source | extend ShortMessage = substring(Message, 0, 100)
```

### `parse` — Extract fields from strings

```kql
source | parse Message with "Request " RequestId " took " DurationMs " ms"
```

---

## Sorting and Limiting

```kql
-- Most recent first
source | order by PreciseTimeStamp desc

-- Top 100 errors
source | where Level <= 1 | take 100

-- Top 10 noisiest role instances
source | summarize count() by RoleInstance | order by count_ desc | take 10
```

---

## DGrep-Specific Gotchas

Things that work in Kusto but **do NOT work** in DGrep:

| ❌ Does NOT Work | ✅ Use Instead | Why |
|------------------|----------------|-----|
| `ago(1h)` | `--from -1h` CLI flag | Time filtering is at the blob layer, not KQL |
| `let x = ...; x \| where ...` | Inline the expression | `let` statements not supported |
| `has "term"` | `contains "term"` | `has` operator not implemented |
| `has_any("a","b")` | `contains "a" or contains "b"` | `has_any` not implemented |
| `join kind=leftouter` | ❌ Not available | Only `inner join` (client-side only) |
| `union Table1, Table2` | Run separate queries | `union` not supported |
| `mv-expand` | `mvexpand` (no hyphen) | Different spelling |
| `column_ifexists` | `columnifexists` (no underscore) | Different spelling |
| `iff(cond, a, b)` | `iif(cond, a, b)` (double-i) | Different function name |
| `extract_all` | `extractall` (no underscore) | Different spelling |
| `percentile(X, 95)` | ❌ Listed in docs but unreliable in server queries | Re-aggregate client-side |
| `arg_max` / `arg_min` | ❌ Not available | Use `order by` + `take 1` |
| `make_list` / `make_bag` | ❌ Not available | Use `makeset` for unique values |
| `stdev` / `variance` | ❌ Not available | Compute client-side |

### Supported Aggregations

These work: `avg`, `count`, `countif`, `dcount`, `dcountif`, `makeset`, `max`, `min`, `sum`, `any` (single arg only).

These do NOT work: `maxif`, `minif`, `sumif`, `make_list`, `make_bag`, `arg_max`, `arg_min`, `stdev`, `variance`, `any` (multi-arg).

### Supported Tabular Operators

These work: `extend`, `limit`/`take`, `mvexpand`, `order`/`sort`, `project`, `print`, `summarize`, `where`, `parse`, `project-away`, `project-rename`, `columnifexists`, `categorize` (DGrep-only), `join` (inner only, client-side).

These do NOT work: `let`, `union`, any `join` kind other than `inner`.

### Supported String Operators

These work: `==`, `!=`, `=~`, `!~`, `contains`/`!contains`, `contains_cs`/`!contains_cs`, `startswith`/`!startswith`, `startswith_cs`/`!startswith_cs`, `endswith`/`!endswith`, `endswith_cs`/`!endswith_cs`, `matches regex`, `in`/`!in`.

---

## Quick Copy-Paste Patterns

### ICM Error Investigation Starter

```powershell
dgrep search --event MyEvent --from -1h \
  --identity "Tenant=WUS" \
  --query "source | where Level <= 1 | project PreciseTimeStamp, Level, Message, CorrelationId" \
  --output table
```

### Count Errors by Type

```powershell
dgrep search --event MyEvent --from -4h \
  --query "source | where Level <= 1 | extend ErrorType = substring(Message, 0, 80) | summarize count() by ErrorType | order by count_ desc | take 20" \
  --output table
```

### Find Specific Correlation ID

```powershell
dgrep search --event MyEvent --from -6h \
  --query "source | where CorrelationId == 'abc-123-def-456' | order by PreciseTimeStamp asc" \
  --output table
```

### Regex Search for Stack Traces

```powershell
dgrep search --event MyEvent --from -2h \
  --query "source | where Message matches regex 'System\\..*Exception' | project PreciseTimeStamp, Message | take 50" \
  --output table
```

---

## See Also

- [DGrep Quick Start](dgrep-quickstart.md) — Get from zero to first query in 5 minutes
- [DGrep Troubleshooting](dgrep-troubleshooting.md) — Common errors and fixes
- [DGrep Sample Queries](dgrep-sample-queries.md) — Ready-to-use query examples
- [Geneva DGrep KQL Reference](https://eng.ms/docs/products/geneva/logs/references/dgrepquerylanguage/kql) — Official supported-operator list
