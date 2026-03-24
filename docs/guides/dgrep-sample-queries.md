# DGrep Sample Queries

Ready-to-use query examples for common ICM investigation and debugging scenarios. Copy, paste, and customize.

> All examples assume you've configured defaults with `dgrep config set`. Replace `--endpoint`, `--namespace`, and `--event` with your values if not using defaults.

---

## Basic Search — Last Hour of Errors

The simplest starting point for any investigation:

```powershell
dgrep search \
  --endpoint https://production.diagnostics.monitoring.core.windows.net/ \
  --namespace MyServiceNamespace \
  --event MyEventTable \
  --from -1h \
  --query "source | where Level <= 1 | project PreciseTimeStamp, Level, Message"
```

With defaults configured, this simplifies to:

```powershell
dgrep search --event MyEventTable --from -1h \
  --query "source | where Level <= 1 | project PreciseTimeStamp, Level, Message"
```

---

## Scoped Search with Identity Columns

Identity columns filter at the blob level — dramatically faster than filtering in KQL. Always use them when you know the tenant, role, or environment.

```powershell
# Search specific tenant and role
dgrep search --event MyEventTable --from -2h \
  --identity "Tenant=WUS" \
  --identity "Role=Frontend" \
  --query "source | where Level <= 1 | project PreciseTimeStamp, Message, CorrelationId"

# Search across two tenants
dgrep search --event MyEventTable --from -1h \
  --identity "Tenant=WUS" \
  --identity "Tenant=EUS" \
  --query "source | where Level <= 2 | project PreciseTimeStamp, Tenant, Level, Message"

# Search by environment
dgrep search --event MyEventTable --from -4h \
  --identity "Environment=Production" \
  --query "source | where Message contains 'timeout' | project PreciseTimeStamp, Message"
```

---

## Using Saved Queries

Save queries you run frequently to avoid retyping:

### Save a parameterized query

```powershell
dgrep saved add find-errors \
  --query "source | where Level <= 1 | where Message contains '{{term}}' | project PreciseTimeStamp, Level, Message" \
  --description "Find errors containing a search term"

dgrep saved add errors-by-tenant \
  --query "source | where Level <= 1 | summarize count() by Tenant | order by count_ desc" \
  --description "Count errors per tenant"

dgrep saved add trace-correlation \
  --query "source | where CorrelationId == '{{id}}' | order by PreciseTimeStamp asc | project PreciseTimeStamp, Level, Message" \
  --description "Trace all events for a correlation ID"
```

### Run saved queries

```powershell
# Search for timeout errors
dgrep saved run find-errors --param term=timeout --event MyEventTable --from -1h

# Count errors by tenant
dgrep saved run errors-by-tenant --event MyEventTable --from -4h

# Trace a specific correlation ID
dgrep saved run trace-correlation --param id=abc-123-def-456 --event MyEventTable --from -6h
```

### List and manage saved queries

```powershell
dgrep saved list
dgrep saved show find-errors
dgrep saved remove old-query
```

---

## Piping Output to Other Tools

DGrep output formats (`json`, `jsonl`, `csv`) are designed for piping. Use them with standard tools:

### Pipe JSON to jq

```powershell
# Extract just the messages
dgrep search --event MyEventTable --from -1h \
  --query "source | where Level <= 1" --output json \
  | jq '.[].Message'

# Filter results further with jq
dgrep search --event MyEventTable --from -1h \
  --query "source | where Level <= 2" --output json \
  | jq '[.[] | select(.Message | test("timeout|connection"))]'

# Count results by level
dgrep search --event MyEventTable --from -1h \
  --query "source | where Level <= 2" --output json \
  | jq 'group_by(.Level) | map({Level: .[0].Level, Count: length})'
```

### Pipe to grep for quick filtering

```powershell
# Search for a pattern in results
dgrep search --event MyEventTable --from -1h \
  --query "source | where Level <= 2" --output jsonl \
  | Select-String "NullReference"

# Count matching lines
dgrep search --event MyEventTable --from -1h \
  --query "source | where Level <= 2" --output jsonl \
  | Select-String "timeout" | Measure-Object -Line
```

### Export to CSV for Excel analysis

```powershell
# Save to CSV file
dgrep search --event MyEventTable --from -4h \
  --query "source | where Level <= 2 | project PreciseTimeStamp, Level, Message, Tenant" \
  --output csv > errors.csv

# Open directly in Excel (Windows)
dgrep search --event MyEventTable --from -1h \
  --query "source | where Level <= 1" --output csv > errors.csv; Start-Process errors.csv
```

### JSONL for streaming pipelines

```powershell
# Stream results one per line — ideal for processing in real time
dgrep search --event MyEventTable --from -1h \
  --query "source | where Level <= 1" --output jsonl \
  | ForEach-Object { $obj = $_ | ConvertFrom-Json; Write-Host "$($obj.PreciseTimeStamp) $($obj.Message)" }
```

---

## Real-World ICM Investigation Workflow

A step-by-step example of using DGrep during a live site incident.

### Scenario

You receive an ICM alert: **"High error rate in WUS Frontend — 5xx responses spiking"**

### Step 1: Get the lay of the land — recent errors

Start broad. See what's happening in the last 30 minutes:

```powershell
dgrep search --event WebRequestLog --from -30m \
  --identity "Tenant=WUS" --identity "Role=Frontend" \
  --query "source | where Level <= 1 | project PreciseTimeStamp, Level, Message | take 50" \
  --output table
```

### Step 2: Count errors by type

See which error messages are most common:

```powershell
dgrep search --event WebRequestLog --from -1h \
  --identity "Tenant=WUS" --identity "Role=Frontend" \
  --query "source | where Level <= 1 | extend ErrorType = substring(Message, 0, 80) | summarize count() by ErrorType | order by count_ desc | take 20" \
  --output table
```

### Step 3: Narrow to the specific error

You see `ConnectionRefused to downstream-api:443` is the top error. Dig deeper:

```powershell
dgrep search --event WebRequestLog --from -1h \
  --identity "Tenant=WUS" --identity "Role=Frontend" \
  --query "source | where Message contains 'ConnectionRefused' | project PreciseTimeStamp, RoleInstance, Message, CorrelationId" \
  --output json > connection_errors.json
```

### Step 4: Check if it's one instance or all instances

```powershell
dgrep search --event WebRequestLog --from -1h \
  --identity "Tenant=WUS" --identity "Role=Frontend" \
  --query "source | where Message contains 'ConnectionRefused' | summarize count() by RoleInstance | order by count_ desc" \
  --output table
```

### Step 5: Trace a specific request

Pick a CorrelationId from Step 3 and trace it end-to-end:

```powershell
dgrep search --event WebRequestLog --from -2h \
  --query "source | where CorrelationId == 'abc-123-def-456' | order by PreciseTimeStamp asc | project PreciseTimeStamp, Level, Message" \
  --output table
```

### Step 6: Check the downstream service

Switch to the downstream service's logs:

```powershell
dgrep search \
  --namespace DownstreamApiNamespace \
  --event DownstreamRequestLog \
  --from -1h \
  --identity "Tenant=WUS" \
  --query "source | where Level <= 1 | project PreciseTimeStamp, Level, Message | take 50" \
  --output table
```

### Step 7: Export evidence for the ICM

Save the key findings for the incident timeline:

```powershell
# Error counts for the timeline
dgrep search --event WebRequestLog --from -4h \
  --identity "Tenant=WUS" --identity "Role=Frontend" \
  --query "source | where Message contains 'ConnectionRefused' | summarize count() by bin(PreciseTimeStamp, 5m) | order by PreciseTimeStamp asc" \
  --output csv > icm_error_timeline.csv

# Sample errors for root cause
dgrep search --event WebRequestLog --from -1h \
  --identity "Tenant=WUS" --identity "Role=Frontend" \
  --query "source | where Message contains 'ConnectionRefused' | project PreciseTimeStamp, RoleInstance, Message | take 100" \
  --output json > icm_sample_errors.json
```

---

## More Patterns

### Find all events for a specific user/request

```powershell
dgrep search --event AuthLog --from -6h \
  --query "source | where UserId == 'user@microsoft.com' | order by PreciseTimeStamp desc | project PreciseTimeStamp, Action, Result"
```

### Search across multiple events (run separately)

DGrep doesn't support `union`, so query each event table separately:

```powershell
# Query event A
dgrep search --event RequestLog --from -1h \
  --query "source | where Level <= 1" --output json > request_errors.json

# Query event B
dgrep search --event BackgroundTaskLog --from -1h \
  --query "source | where Level <= 1" --output json > task_errors.json
```

### Find slow requests

```powershell
dgrep search --event WebRequestLog --from -1h \
  --query "source | where DurationMs > 5000 | project PreciseTimeStamp, RequestUrl, DurationMs, StatusCode | order by DurationMs desc | take 50" \
  --output table
```

### Regex search for exception types

```powershell
dgrep search --event MyEventTable --from -2h \
  --query "source | where Message matches regex 'System\\.(IO|Net|Threading)\\..*Exception' | project PreciseTimeStamp, Message | take 100"
```

### Compare error rates between tenants

```powershell
dgrep search --event MyEventTable --from -1h \
  --query "source | where Level <= 1 | summarize ErrorCount = count() by Tenant | order by ErrorCount desc" \
  --output table
```

---

## See Also

- [DGrep Quick Start](dgrep-quickstart.md) — Get from zero to first query in 5 minutes
- [DGrep KQL Cheat Sheet](dgrep-kql-cheatsheet.md) — Supported operators and common patterns
- [DGrep Troubleshooting](dgrep-troubleshooting.md) — Common errors and fixes
