# DGrep CLI Troubleshooting

Common problems and solutions when using the DGrep CLI. Organized from most-common to least-common.

---

## Authentication Failures

### `az login` / Azure CLI Auth

**Symptom:** `Authentication failed` or `DefaultAzureCredential could not find credentials`

**Fix:**
1. Ensure you're logged in: `az login`
2. Verify your account: `az account show`
3. If using a specific tenant: `az login --tenant <tenant-id>`
4. Clear cached tokens and re-login: `az account clear && az login`

**Symptom:** `AADSTS50076: Due to a configuration change made by your administrator...` (MFA required)

**Fix:**
1. Use InPrivate/Incognito browser window for `az login`
2. Or try device code flow: `az login --use-device-code`

### dSTS (Distributed Security Token Service) Errors

**Symptom:** `dSTS authentication failed` or `Token exchange error`

DGrep uses dSTS internally (not standard AAD). The SDK handles the token exchange, but failures can occur when:

**Fix:**
1. **Cert auth:** Verify your certificate is registered in Geneva Account User Roles for the target namespace
2. **User auth:** Try the interactive SDK auth mode — it may need to open a browser dialog for dSTS consent
3. **Token expired:** Re-run `az login` to refresh tokens
4. **Sovereign cloud:** Ensure you're using the correct MDS endpoint for your environment (dSTS endpoints differ per cloud)

### Certificate Auth Errors

**Symptom:** `Certificate not found` or `Access denied with certificate`

**Fix:**
1. Verify the certificate is installed in your certificate store: `certutil -viewstore My`
2. Check that the certificate's Subject Alternative Name (SAN) is registered in Geneva Account User Roles
3. Ensure the user role has `TableRead` claims for the target namespace
4. Check certificate expiration: `certutil -verify <cert-path>`

**Symptom:** `The certificate chain could not be built to a trusted root`

**Fix:**
1. Install the issuing CA certificate in your Trusted Root store
2. For Microsoft internal certs, ensure the AME or MSit root CAs are trusted

---

## Network and Endpoint Issues

### Wrong or Unreachable Endpoint

**Symptom:** `Connection refused`, `Name resolution failed`, or `The remote name could not be resolved`

**Fix:**
1. Verify you're using the correct MDS endpoint for your environment:

| Environment | MDS Endpoint |
|-------------|-------------|
| FirstParty PROD | `https://firstparty.monitoring.windows.net/` |
| Diagnostics PROD | `https://production.diagnostics.monitoring.core.windows.net/` |
| BlackForest (Germany) | Sovereign-specific — check your team's docs |
| Fairfax (US Gov/GCC) | Sovereign-specific — check your team's docs |
| Mooncake (China) | Sovereign-specific — check your team's docs |

2. Check VPN/network connectivity — some endpoints require corpnet or VPN access
3. For the DGrep v2 frontend: `https://dgrepv2-frontend-prod.trafficmanager.net` — verify it resolves via `nslookup`

### Proxy or Firewall Issues

**Symptom:** `Connection timed out` or `407 Proxy Authentication Required`

**Fix:**
1. If behind a corporate proxy, set proxy environment variables:
   ```powershell
   $env:HTTP_PROXY = "http://your-proxy:8080"
   $env:HTTPS_PROXY = "http://your-proxy:8080"
   ```
2. Check that `*.monitoring.core.windows.net` and `*.trafficmanager.net` are not blocked by firewall rules
3. Test connectivity: `Test-NetConnection dgrepv2-frontend-prod.trafficmanager.net -Port 443`

---

## Rate Limiting

### Concurrent Query Limit

**Symptom:** `Quota exceeded` or `Too many concurrent requests` or queries hanging indefinitely

DGrep enforces a limit of **5 concurrent queries per user**. Orphaned queries (not properly closed) count against this limit.

**Fix:**
1. **Wait and retry** — existing queries may still be running. Wait a few minutes for them to complete.
2. **Close orphaned queries** — if you cancelled a CLI run (Ctrl+C), the server-side query may still be open. The CLI attempts cleanup on exit, but network interruptions can leave orphans. Orphans expire automatically (typically within 5 minutes).
3. **Don't run parallel queries** — avoid launching 5+ simultaneous `dgrep search` commands.
4. **Check the Jarvis portal** — you can see active queries at `portal.microsoftgeneva.com/logs/dgrep` and cancel them there.

### Query Timeout

**Symptom:** Query runs for a long time and returns no results or times out

**Fix:**
1. **Narrow the time range** — DGrep scan time is proportional to data volume. Try `-30m` instead of `-7d`.
2. **Add identity scoping** — `--identity` filters at the blob level, dramatically reducing scan volume.
3. **Simplify the query** — complex `where` clauses with regex increase per-row processing time.
4. Default timeout is 5 minutes. For large scans, you may need to narrow scope rather than increase timeout.

---

## Query Syntax Errors

### KQL vs. MQL Confusion

**Symptom:** `Query parse error` or unexpected results

The CLI defaults to KQL. If you're accidentally writing MQL syntax, you'll get parse errors.

| MQL Syntax (wrong for KQL mode) | KQL Equivalent |
|----------------------------------|----------------|
| `where Message.Contains("error")` | `where Message contains "error"` |
| `select PreciseTimeStamp, Message` | `project PreciseTimeStamp, Message` |
| `orderby TIMESTAMP desc` | `order by PreciseTimeStamp desc` |
| `groupby Tenant let count = Count()` | `summarize count() by Tenant` |
| `message ~= "pattern"` | `Message matches regex "pattern"` |

**Fix:** Check `--query-type` flag. Use `--query-type kql` (default) for KQL syntax, or `--query-type mql` if you specifically need MQL.

### Common KQL Mistakes in DGrep

**Problem:** Using `has` instead of `contains`

```
❌ source | where Message has "error"
✅ source | where Message contains "error"
```

**Problem:** Using `ago()` for time

```
❌ source | where PreciseTimeStamp > ago(1h)
✅ Use --from -1h flag instead (time is handled at the blob layer)
```

**Problem:** Using `let` for variables

```
❌ let threshold = 2; source | where Level <= threshold
✅ source | where Level <= 2
```

**Problem:** Using `mv-expand` with a hyphen

```
❌ source | mv-expand Tags
✅ source | mvexpand Tags
```

**Problem:** Using `iff` instead of `iif`

```
❌ source | extend IsError = iff(Level <= 1, "Yes", "No")
✅ source | extend IsError = iif(Level <= 1, "Yes", "No")
```

---

## "No Results" Troubleshooting

Getting zero results is the most common DGrep frustration. Work through this checklist:

### 1. Wrong Namespace or Event Name

Namespace and event are case-sensitive and support regex.

```powershell
# Exact match (use anchors)
--namespace "^MyServiceNamespace$" --event "^MyEvent$"

# Partial match (regex)
--namespace "MyService" --event "Error"

# List what's available — check the Jarvis portal:
# portal.microsoftgeneva.com → Logs → DGrep → browse namespaces
```

**Fix:** Open the Jarvis DGrep portal, browse to your MDS endpoint, and verify the exact namespace and event names.

### 2. Wrong Time Range

- DGrep has **~5 minute ingestion latency**. Logs from the last 5 minutes may not be queryable yet.
- Maximum query range is **7 days**. Queries beyond 7 days silently return nothing.
- Time is in **UTC**. If you specify absolute times, ensure they're UTC.

**Fix:**
```powershell
# Try a wider time range
--from -4h

# Use relative time instead of absolute
--from -1h   # not --from "2026-03-23T10:00:00Z"
```

### 3. Identity Scoping Too Narrow

If you specify `--identity` dimensions that don't match any blobs, you get zero results — no error message.

**Fix:**
```powershell
# Remove identity filters and query without them first
dgrep search --event MyEvent --from -1h \
  --query "source | take 10"

# Then progressively add identity filters to narrow down
dgrep search --event MyEvent --from -1h \
  --identity "Tenant=WUS" \
  --query "source | take 10"
```

### 4. Query Filter Too Restrictive

Your `where` clause may be filtering out all rows.

**Fix:**
```powershell
# Start with no filter — just get some rows
dgrep search --event MyEvent --from -1h \
  --query "source | take 10" --output table

# Look at the actual column names and values, then build your filter
```

### 5. Wrong MDS Endpoint

Different environments (prod, int, sovereign) use different endpoints. If you're querying the wrong endpoint, you'll get zero results.

**Fix:** Verify which endpoint your team's Geneva account uses. Check your team's Geneva onboarding docs or the Jarvis portal.

### 6. Permissions Issue (Silent Failure)

Some namespace/event combinations require specific user role claims. Without them, DGrep may return empty results rather than an explicit auth error.

**Fix:** Verify your access in the Geneva portal → Account → User Roles. Ensure you have `TableRead` claims for the target namespace.

---

## Config File Issues

### Config File Location

The DGrep CLI stores configuration in `~/.dgrep/config.json` (typically `C:\Users\<you>\.dgrep\config.json`).

### Config File Won't Load

**Symptom:** `Invalid config file` or settings not being applied

**Fix:**
1. Check JSON syntax: `Get-Content ~/.dgrep/config.json | ConvertFrom-Json`
2. Ensure all values are strings (not numbers or booleans where strings are expected)
3. Delete and recreate: `Remove-Item ~/.dgrep/config.json` then re-run `dgrep config set ...`

### Saved Queries Not Found

**Symptom:** `Saved query 'X' not found`

**Fix:**
1. List available saved queries: `dgrep saved list`
2. Query names are case-sensitive
3. Saved queries are stored in the same config file — check if the file is intact

### Config Settings Not Taking Effect

**Symptom:** Default endpoint/namespace not being used

**Fix:**
1. Verify config: `dgrep config list`
2. Explicit CLI flags always override config defaults
3. Check that the setting name matches exactly: `defaultEndpoint`, `defaultNamespace`, `defaultQueryType`

---

## Build and Installation Issues

### Build Fails

**Symptom:** `dotnet build` fails with errors

**Fix:**
1. Ensure .NET Framework 4.7.2 Developer Pack is installed (not just the runtime)
2. Check that you have Visual Studio 2022 build tools
3. Restore NuGet packages: `dotnet restore src\DgrepCli\DgrepCli.csproj`
4. Clean and rebuild: `dotnet clean && dotnet build`

### `dgrep` Not Found

**Symptom:** `dgrep: The term 'dgrep' is not recognized`

**Fix:**
1. The built executable is at `tools\dgrep-cli\src\DgrepCli\bin\Debug\net472\DgrepCli.exe`
2. Either use the full path or add it to your PATH:
   ```powershell
   $env:PATH += ";C:\dev\personal\pa-squad\tools\dgrep-cli\src\DgrepCli\bin\Debug\net472"
   ```
3. Or create an alias: `Set-Alias dgrep "C:\path\to\DgrepCli.exe"`

---

## Still Stuck?

1. **Check the Jarvis DGrep portal** — run your query in the web UI first to verify it works: `portal.microsoftgeneva.com/logs/dgrep`
2. **Check Geneva docs** — [DGrep KQL Reference](https://eng.ms/docs/products/geneva/logs/references/dgrepquerylanguage/kql)
3. **File an issue** — if the CLI has a bug, file it on the pa-squad repo

---

## See Also

- [DGrep Quick Start](dgrep-quickstart.md) *(coming soon)* — Get from zero to first query in 5 minutes
- [DGrep KQL Cheat Sheet](dgrep-kql-cheatsheet.md) — Supported operators and patterns
- [DGrep Sample Queries](dgrep-sample-queries.md) — Ready-to-use query examples
