# Geneva DGrep Research — CLI Tool Feasibility & Architecture

**Researcher:** Elrond  
**Date:** 2026-03-23  
**Status:** Complete  
**Sources:** Geneva product docs (eng.ms), DGrep SDK docs, IcM Agent Studio connector docs, team TSGs from Xbox, Designer, Office.com, CDS Telemetry, Remote Assistance Service  

---

## 1. What Is DGrep?

**DGrep (Distributed Grep)** is Geneva's highly parallelized brute-force log search engine. It is the **unindexed/partially indexed** log search offering, enabled by default on all logs ingested into Geneva Logs — no additional onboarding required.

### Key characteristics:
- **Brute-force scan** — searches raw log blobs in Azure Storage, parallelized across a distributed backend
- **Real-time results** — streams results back as they are found; usable interactively
- **Free on any Geneva Logs stream** — no provisioning, no compute allocation needed (unlike Kusto, which requires teams to provide their own compute)
- **~5 minute ingestion latency** — logs become queryable approximately 5 minutes after emission
- **7-day maximum query range** — queries are limited to a 7-day window
- **1M row server-side limit** — server query returns at most 1,000,000 rows
- **500K default / 1M max row count** — SDK default is 500,000 rows; max configurable to 1,000,000

### DGrep vs. Kusto:
| Aspect | DGrep | Kusto |
|--------|-------|-------|
| Setup | Zero — enabled on all Geneva Logs | Requires dedicated compute cluster |
| Cost | Free (included with Geneva Logs) | Teams pay for compute |
| Query latency | Proportional to time range & data volume | Fast (indexed) |
| Query language | KQL subset + MQL | Full KQL |
| Retention | Geneva Logs retention (varies) | Configurable |
| Best for | Ad-hoc debugging, incident triage | Analytics, dashboards, long-term queries |

**Common use cases:**
1. **Incident triage** — searching logs during live site incidents when Kusto is unavailable or too slow to onboard
2. **Ad-hoc debugging** — finding specific correlation IDs, error messages, request traces
3. **Sovereign/air-gapped environments** — querying logs in Germany, GCC, China, ITAR where Kusto may not be available
4. **Int/EDog environments** — many teams disable Kusto for non-prod to save cost; DGrep is the only option
5. **Incident automation** — IcM workflows query DGrep to enrich, route, and correlate incidents

---

## Approach Comparison: REST API vs .NET Framework SDK

> **Added 2026-03-24** — In response to Jonathan's challenge of the REST API approach. This section provides an honest, side-by-side comparison of both implementation strategies now that **cross-platform is confirmed as NOT a requirement** (Windows-only is acceptable).

### Background: What Is ".NET Framework" and Why Does It Matter?

**.NET Framework** is Microsoft's original managed runtime for Windows. Key facts:

| Aspect | .NET Framework | .NET Core / Modern .NET (.NET 8+) |
|--------|----------------|-------------------------------------|
| **Platform** | Windows-only | Cross-platform (Windows, Linux, macOS) |
| **Ships with Windows?** | Yes — .NET Framework 4.8 is pre-installed on Windows 10/11 | No — requires separate install or self-contained publish |
| **Current version** | 4.8.1 (maintenance mode) | .NET 9 (active development) |
| **Project target** | `net472`, `net48` | `net8.0`, `net9.0` |
| **Status in 2026** | Security patches via Windows Update; no new features | Where all new investment goes |
| **Developer tooling** | Visual Studio 2022 fully supports it; `dotnet` CLI can build it | Full `dotnet` CLI and VS Code support |

**Why the DGrep SDK doesn't work on .NET Core / .NET 8:**

The `Microsoft.Geneva.DGrep.SDK` NuGet package has two client classes:
- **`DGrepClient`** — certificate-based auth. Takes `(endpoint, X509Certificate2)`. **May work on .NET Core** (no UI dependency).
- **`DGrepUserAuthClient`** — interactive user auth. Opens an AAD login dialog window. **Does NOT work on .NET Core** because it uses WinForms/WPF for the authentication dialog — UI frameworks that are Windows/.NET Framework-only.

The specific incompatibility is the interactive auth dialog. The SDK references `System.Windows.Forms` or similar Windows-only UI assemblies to pop a browser-based login window. .NET Core doesn't include these assemblies in its cross-platform runtime (they exist in `Microsoft.WindowsDesktop.App` on Windows, but this is a Windows-only workload).

**Is building a .NET Framework console app reasonable in 2026?** Yes, with caveats:
- ✅ .NET Framework 4.8 ships with every Windows 10/11 machine — zero install required
- ✅ Visual Studio 2022 and `dotnet` CLI fully support .NET Framework projects
- ✅ Many internal Microsoft tools still target .NET Framework (it's not exotic)
- ✅ Security patches continue indefinitely through Windows servicing
- ⚠️ No new features or performance improvements will come to .NET Framework
- ⚠️ It's Windows-only (but this is explicitly acceptable for our use case)
- ⚠️ Some newer NuGet packages may drop .NET Framework support over time

**Bottom line:** .NET Framework is not dead — it's in stable maintenance mode. Building a console app targeting `net472` is a perfectly normal thing to do in 2026 on Windows, especially when wrapping a .NET Framework-only SDK.

---

### Approach A: Wrap the .NET Framework SDK

Build a C# console app targeting .NET Framework 4.7.2+, reference the `Microsoft.Geneva.DGrep.SDK` NuGet package, and ship it as a Windows `.exe`.

### Approach B: Reverse-Engineer the REST API

Capture the HTTP traffic the SDK generates, reverse-engineer the protocol, and build a standalone CLI (TypeScript, Go, or Python) that talks to the DGrep v2 frontend REST endpoint directly — no .NET dependency.

---

### Head-to-Head Comparison

#### 1. Time to First Working Query

| | Approach A (SDK Wrapper) | Approach B (REST API) |
|---|---|---|
| **Estimate** | **Hours to 1 day** | **Days to weeks** |
| **Why** | Create project → `dotnet add package Microsoft.Geneva.DGrep.SDK` → copy 15-line sample from docs → build → run. The SDK handles serialization, auth, connection management, result parsing — all of it. | Must first capture HTTP traffic with Fiddler/mitmproxy, decode the request/response format, understand the streaming protocol, figure out the auth token format, implement all of it from scratch. |
| **First milestone** | Working query returning real results on day 1 | Possibly a raw HTTP request that gets a 200 back after several days of protocol analysis |

**Verdict: Approach A wins decisively.** The SDK exists precisely so you don't have to do protocol work.

#### 2. Auth Complexity

| | Approach A (SDK Wrapper) | Approach B (REST API) |
|---|---|---|
| **Certificate auth** | `new DGrepClient(endpoint, cert)` — one line. SDK handles dSTS token exchange. | Must reverse-engineer the dSTS token acquisition flow. Geneva uses dSTS (distributed Security Token Service), not standard AAD tokens. This is the single hardest part of the REST approach. |
| **Interactive user auth** | `new DGrepUserAuthClient(endpoint)` — pops AAD login dialog. Works out of the box on .NET Framework. | Must implement AAD device code flow or browser-based PKCE flow, then figure out how to exchange the AAD token for a dSTS token the DGrep frontend accepts. May require undocumented token exchange. |
| **`az login` / DefaultAzureCredential** | Not natively supported by SDK, but could acquire a token via Azure Identity and pass it to the SDK (needs investigation). | Same challenge — even with an AAD token, unclear if the DGrep frontend accepts it directly or requires dSTS. |
| **Risk** | Low — SDK handles auth internally. | **High** — dSTS auth is the #1 risk. If we can't figure out the token format, the entire approach fails. |

**Verdict: Approach A wins.** Auth is the hardest part of any Geneva integration. The SDK abstracts all of it.

#### 3. Maintenance Burden

| | Approach A (SDK Wrapper) | Approach B (REST API) |
|---|---|---|
| **When Geneva updates their API** | Update the NuGet package version. The SDK team maintains backward compatibility. | Our tool breaks silently. No changelog, no migration guide, no heads-up — the API is undocumented and internal. We discover the breakage when users report errors. |
| **When Geneva adds features** | Update NuGet package, expose new parameters in CLI. | Must re-capture traffic, discover new endpoints/parameters, implement from scratch. |
| **Ongoing cost** | Low — thin CLI wrapper over a maintained SDK. | **High** — we own the entire protocol layer. Every Geneva release is a potential breaking change. |

**Verdict: Approach A wins.** Maintaining a wrapper around a supported SDK is fundamentally less work than maintaining a reverse-engineered protocol implementation.

#### 4. Feature Coverage

| | Approach A (SDK Wrapper) | Approach B (REST API) |
|---|---|---|
| **Day 1 features** | Everything the SDK supports: query, stream, CSV export, client queries, server queries, all auth modes | Only what we reverse-engineer and implement |
| **Streaming results** | `RunQueryAsync` + `IDGrepQuery` — streaming is built into the SDK | Must discover and implement the streaming protocol (WebSocket? SSE? Chunked HTTP?) |
| **CSV export** | `GetCsvResultAsync` — returns SAS URI | Must discover the CSV export endpoint |
| **Quota management** | `IDGrepQuery.CloseAsync()` — SDK enforces resource cleanup | Must implement query lifecycle management ourselves |
| **Sovereign clouds** | SDK supports all MDS endpoints | Must test each endpoint independently |

**Verdict: Approach A wins.** 100% feature coverage vs. whatever subset we manage to reverse-engineer.

#### 5. Dependencies

| | Approach A (SDK Wrapper) | Approach B (REST API) |
|---|---|---|
| **Runtime** | .NET Framework 4.7.2+ (pre-installed on Windows 10/11) | Node.js 18+ (requires install), OR Go (single binary), OR Python 3.10+ (requires install) |
| **User install experience** | Download `.exe`, run it. Nothing else needed. | Install Node.js → `npm install -g dgrep-cli`. Or download Go binary. |
| **Size** | Small (SDK + app, maybe 5-10 MB) | Varies: Node.js is ~80MB if bundled, Go binary ~15MB, Python needs venv |

**Verdict: Approach A wins.** Zero-dependency on Windows is hard to beat. .NET Framework is already there.

#### 6. Testability

| | Approach A (SDK Wrapper) | Approach B (REST API) |
|---|---|---|
| **Unit testing** | Mock `DGrepClient` interface, test CLI argument parsing, output formatting. Standard C# testing with xUnit/NUnit. | Mock HTTP layer, test request construction, response parsing, output formatting. Standard testing in chosen language. |
| **Integration testing** | Run against real DGrep with test namespace (requires Geneva access). | Same — need real DGrep access for integration tests. |
| **Test confidence** | High — SDK handles protocol correctness; we test our layer. | Lower — we must test protocol correctness ourselves too. |

**Verdict: Roughly equal for unit tests. Approach A wins on total test burden** because we don't need to test the protocol layer.

#### 7. Distribution

| | Approach A (SDK Wrapper) | Approach B (REST API) |
|---|---|---|
| **Binary** | `dotnet publish` → single folder with `.exe` + DLLs. Can use ILMerge or `PublishSingleFile` for one `.exe`. | Go: single binary. Node.js: npm package or `pkg` bundled binary. Python: `pyinstaller` or pip install. |
| **Install method** | Download from GitHub Releases, internal NuGet feed, or file share. | npm registry, GitHub Releases, or Go binary download. |
| **Update mechanism** | Download new version. Could add self-update later. | npm update, or download new binary. |

**Verdict: Approach A has a slight edge** (single .exe with no runtime needed), but both approaches can produce downloadable binaries.

#### 8. Risk Assessment

| Risk | Approach A (SDK Wrapper) | Approach B (REST API) |
|------|---|---|
| **"Will it work at all?"** | **Very low** — the SDK is the documented, supported way to use DGrep. | **Medium-high** — undocumented protocol, undocumented auth flow. Feasibility is unproven. |
| **Auth failure** | Very low — SDK handles it. | **HIGH** — dSTS token acquisition is complex and undocumented. This is a potential showstopper. |
| **API breaking changes** | Low — NuGet package updates. | **High** — no notice of changes. |
| **Geneva team support** | Can file bugs against the SDK. They support it. | No support — we're using an internal protocol they don't publish. |
| **"Can't do X" discovery** | Found early — SDK docs list all capabilities. | Found late — after weeks of reverse-engineering, discover a feature isn't feasible over REST. |
| **Abandonment cost** | Low — small codebase, easy to walk away. | High — weeks of protocol reverse-engineering investment before first working query. |

**Verdict: Approach A is dramatically lower risk.** Approach B has a plausible failure mode (auth) that could kill the entire project.

---

### Summary Table

| Dimension | Approach A (SDK) | Approach B (REST) | Winner |
|-----------|:---:|:---:|:---:|
| Time to first query | Hours | Days–weeks | **A** |
| Auth complexity | Handled by SDK | Must reverse-engineer dSTS | **A** |
| Maintenance burden | NuGet update | Own the protocol | **A** |
| Feature coverage | 100% | Subset | **A** |
| Dependencies | Zero (Windows) | Runtime install | **A** |
| Testability | Test CLI layer only | Test CLI + protocol | **A** |
| Distribution | .exe download | Binary/npm | Tie |
| Risk | Very low | Medium-high | **A** |
| Cross-platform | ❌ Windows only | ✅ Any OS | **B** |
| Language flexibility | C# only | Any language | **B** |
| Future-proofing | .NET Framework (maintenance) | Modern runtime | **B** |

---

### Recommendation

**Use Approach A: Wrap the .NET Framework SDK.**

**Reasoning:**

1. **Cross-platform was the original reason for Approach B — and that requirement is gone.** Jonathan confirmed Windows-only is acceptable. With that constraint removed, the REST API approach loses its primary justification.

2. **Auth is the killer.** Geneva uses dSTS, not standard AAD tokens. The SDK abstracts this entirely. Reverse-engineering dSTS token acquisition is weeks of work with a real chance of failure. The SDK gives us working auth in one line of code.

3. **Time-to-value is not close.** A working DGrep CLI with the SDK wrapper can be demoed in a day. The REST approach requires a multi-week spike just to prove feasibility.

4. **Maintenance cost is not close.** Wrapping a supported SDK means Geneva's team handles protocol changes. Owning a reverse-engineered protocol means we absorb every internal change.

5. **The downsides of Approach A are manageable:**
   - "C# only" — For a CLI tool that wraps an SDK, C# is the natural choice. We're not building a web app.
   - ".NET Framework maintenance mode" — The Framework isn't going away. It ships with Windows and gets security patches. Our tool doesn't need the latest .NET performance features.
   - "Windows-only" — Explicitly acceptable per Jonathan.

6. **Approach B is not ruled out forever.** If we later discover the SDK has a deal-breaking limitation (e.g., cannot support `az login`-based auth), we can pivot to REST. But we should try the simpler approach first.

**Proposed implementation plan:**

1. **Day 1:** Create .NET Framework 4.7.2 console project, add DGrep SDK NuGet, run sample query with cert or user auth
2. **Day 2-3:** Build CLI argument parser (namespace, event, time range, query, output format)
3. **Day 4-5:** Add output formatting (table, JSON, CSV, JSONL), streaming support
4. **Week 2:** Add config file support, saved queries, error handling, polish
5. **Week 2+:** Investigate `DefaultAzureCredential` integration (can we get Azure Identity tokens working with the SDK?)

**What to investigate first (before committing):**

Before fully committing to Approach A, do a 2-hour spike:
1. Can we create a .NET Framework console project and reference the DGrep SDK without errors?
2. Does `DGrepUserAuthClient` work for interactive auth on a developer machine?
3. Does `DGrepClient` with a certificate work for automation scenarios?

If all three work, proceed with Approach A. If any fail for unexpected reasons, revisit.

---

## 2. API Surface

### 2.1 DGrep Frontend Endpoint

**Production endpoint:** `https://dgrepv2-frontend-prod.trafficmanager.net`

This is the DGrep v2 frontend that the SDK communicates with. The web UI is accessed via the Jarvis portal at `https://portal.microsoftgeneva.com/logs/dgrep` (aka `aka.ms/jarvis`).

### 2.2 MDS (Microsoft Diagnostics Service) Endpoints

The MDS endpoint specifies where logs are stored. Different environments use different endpoints:

| Environment | MDS Endpoint |
|-------------|-------------|
| FirstParty PROD | `https://firstparty.monitoring.windows.net/` |
| Diagnostics PROD | `https://production.diagnostics.monitoring.core.windows.net/` |
| CA BlackForest (Germany) | Sovereign-specific endpoint |
| CA Fairfax (US Gov/GCC) | Sovereign-specific endpoint |
| CA Mooncake (China) | Sovereign-specific endpoint |

### 2.3 DGrep SDK (.NET)

The official programmatic API is the **DGrep SDK**, a .NET NuGet package. This is the recommended way to interact with DGrep programmatically.

#### Core types:

```csharp
// Query input definition
var input = new QueryInput {
    MdsEndpoint = new Uri("https://production.diagnostics.monitoring.core.windows.net/"),
    EventFilters = new List<EventFilter> {
        new EventFilter {
            NamespaceRegex = "^MyNamespace$",
            NameRegex = "^MyEvent$",
            VersionRegex = "^(Ver2v0|Ver3v0)$"   // optional
        }
    },
    IdentityColumns = new Dictionary<string, List<string>> {
        { "Tenant", new List<string> { "WUS" } },
        { "Role", new List<string> { "Worker", "Frontend" } }
    },
    StartTime = DateTimeOffset.Now.AddMinutes(-30),
    EndTime = DateTimeOffset.Now,
    ServerQuery = "source | where Level <= 2",  // KQL
    ServerQueryType = QueryType.KQL,
    MaxRowCount = 750000  // default 500,000; max 1,000,000
};
```

#### Client classes:
- **`DGrepClient`** — certificate-based authentication. Takes `(DGrepEndpoint, X509Certificate2)`.
- **`DGrepUserAuthClient`** — user/interactive authentication. Takes `(DGrepEndpoint)`. Opens auth dialog. **Not supported in .NET Core.**

#### Key methods:
| Method | Description |
|--------|-------------|
| `GetRowSetResultAsync(input, cancel)` | Run query, return all rows as `RowSetResult` |
| `GetRowSetResultAsync(input, clientQuery, cancel)` | Run server + client query |
| `GetCsvResultAsync(input, cancel)` | Run query, save to blob as CSV ZIP, return SAS URI (valid 6 hours) |
| `RunQueryAsync(input, cancel)` | Start query, return `IDGrepQuery` for streaming/multiple client queries |
| `IDGrepQuery.GetResultAsRowSetAsync(clientQuery, cancel)` | Run client query on existing server results |
| `IDGrepQuery.CloseAsync(cancel)` | **Must call** — releases server resources, frees quota |

#### Result structure:
- `RowSetResult.RowSet.Rows` — `List<Dictionary<string, object>>` (column name → value)
- `RowSetResult.QueryStatus.Status` — completion status
- `CsvResult.CsvUri` — SAS URI to ZIP containing CSV (valid 6 hours)

### 2.4 IcM Agent Studio Connector (REST-like)

The IcM Agent Studio DGrep connector provides a higher-level interface used in incident automation workflows. Its inputs map directly to the DGrep API:

| Parameter | Description |
|-----------|-------------|
| Endpoint | Dropdown matching the Geneva environment |
| Namespace | Regex matching one or more namespaces |
| Event name | Regex matching event names |
| Reference time | UTC start time (`2018-01-12T22:00:00Z`) |
| Time range query direction | `+`, `-`, or `+-` |
| Reference time query range | Number (limited to 7 days total) |
| Time range query unit | Seconds, Minutes, Hours, Days |
| Filtering condition | Server-side filter (KQL or MQL) |
| Query language | `MQL` (default) or `KQL` |
| Client query | Client-side post-processing |
| Scoping condition | Pre-filter dimensions (role, environment, etc.) |
| Max results | Deprecated; defaults to 500,000 |

---

## 3. Query Language

DGrep supports **two query languages**: KQL (recommended, actively developed) and MQL (legacy, maintained but not enhanced).

### 3.1 KQL (Kusto Query Language) — Recommended

DGrep supports a **subset of KQL**. The Geneva team is adopting KQL as the main query language going forward.

#### Supported tabular operators:
`extend`, `limit/take`, `mvexpand` (not `mv-expand`), `order/sort`, `project`, `print`, `summarize`, `where`, `parse`, `project-away`, `project-rename`, `columnifexists` (not `column_ifexists`), `categorize` (DGrep-only), `join` (inner only, client-side only)

#### NOT supported:
`let` statements, `union`, `leftouter`/`rightouter`/`fullouter` join kinds, `mv-expand` (use `mvexpand`), `has`/`has_any`/`has_all` (use `contains`), `in~`

#### Supported string operators:
`==`, `!=`, `=~`, `!~`, `contains`/`!contains`, `contains_cs`/`!contains_cs`, `startswith`/`!startswith`, `startswith_cs`/`!startswith_cs`, `endswith`/`!endswith`, `endswith_cs`/`!endswith_cs`, `matches regex`, `in`/`!in`

#### Supported aggregation functions:
`avg`, `count`, `countif`, `dcount` (always 100% accurate, no Accuracy param), `dcountif`, `makeset`, `max`, `min`, `percentile`, `sum`, `any` (single argument only)

#### NOT supported aggregations:
`maxif`/`minif`/`sumif`, `make_list`/`make_bag`, `arg_max`/`arg_min`, `stdev`/`variance`, `any` with multiple arguments

#### Key DGrep-vs-Kusto pitfalls:
| Kusto | DGrep Equivalent |
|-------|-----------------|
| `iff(cond, a, b)` | `iif(cond, a, b)` |
| `has "term"` | `contains "term"` |
| `has_any("a","b")` | `contains "a" or contains "b"` |
| `mv-expand` | `mvexpand` |
| `column_ifexists` | `columnifexists` |
| `extract_all` | `extractall` |
| `base64_encode_tostring` | `base64_encodestring` |
| `join kind=leftouter` | ❌ Not available |
| `let x = ...;` | ❌ Not available; use inline |

### 3.2 MQL (Geneva's Custom Language)

MQL is a SQL/C#-inspired language created by the Geneva team. It remains supported but is **not recommended for new development**.

#### Key MQL clauses:
- `where` — boolean filter (`where Level <= 2 and Tenant.Contains("test")`)
- `select` — projection (`select timestamp, message`)
- `let` — define/redefine columns (`let TotalDurationSec = TotalDuration / 1000`)
- `orderby` — sorting (`orderby TIMESTAMP desc`)
- `take` / `skip` — pagination
- `distinct` — deduplication
- `groupby` — aggregation (`groupby Tenant let count = Count()`)
- `var` / `join` — nested queries and joins (client-side only)

#### MQL-specific features:
- C# method invocation on strings: `message.Contains("error")`, `field.ToLower()`
- Regex: `message.IsMatch("pattern")`, `message ~= "pattern"` (case-insensitive)
- `it.Any("foo")` — search all columns for substring
- `Regex.Match()` for extraction
- Full `System.String`, `System.DateTime`, `System.Math` method access

### 3.3 Server Query vs. Client Query (Critical Architecture)

**Server Query** — Executed in a distributed fashion close to the data (at blob level). This is the heavy lifting. Results are streamed back.

**Client Query** — Executed on the DGrep backend over the results of the server query. Runs entirely in-memory, so it's fast.

⚠️ **Known limitation:** `summarize`/`groupby` in server queries produces **partial aggregations** because each blob is processed independently. You must re-summarize in the client query to get correct results. This is the #1 source of confusion/bugs.

---

## 4. Authentication Patterns

### 4.1 Certificate-based (Service/Automation)
```csharp
using (var client = new DGrepClient(DGrepEndpoint, ClientCertificate))
```
- Certificate must be authorized via Geneva Account User Roles
- Add cert SAN to user role, then add table read claims for required namespaces
- Most common for automation, CI/CD, incident workflows

### 4.2 User-based (Interactive)
```csharp
using (var client = new DGrepUserAuthClient(DGrepEndpoint))
```
- Opens authentication dialog (AAD)
- ⚠️ **Not supported in .NET Core** — only .NET Framework
- Used for ad-hoc queries from developer machines
- MFA may need to be triggered via InPrivate browsing + DSTS auth

### 4.3 For a CLI tool (Recommended approach):
1. **Azure CLI auth (`az login`)** — leverage existing Azure identity tokens
2. **Managed Identity** — for automation scenarios running in Azure
3. **Certificate** — for service-to-service scenarios
4. **Device code flow** — for interactive CLI use where browser auth isn't possible

The Geneva ecosystem uses **dSTS (distributed Security Token Service)** for authentication. The Jarvis portal uses AAD/Active Directory auth. For a CLI, the recommended pattern would be to use the Azure Identity library's `DefaultAzureCredential` chain, which supports `az login`, managed identity, and environment-based credentials.

---

## 5. Rate Limits & Quotas

| Limit | Value | Source |
|-------|-------|--------|
| Concurrent requests per user | **5** | DGrep quota docs, IcM connector docs |
| Server query max rows | **1,000,000** | Office.com DRI handbook |
| Default max row count | **500,000** | SDK docs, IcM connector |
| Max row count (configurable) | **1,000,000** | SDK docs |
| Query time range | **7 days maximum** | IcM connector docs |
| Ingestion latency | **~5 minutes** | IcM connector docs, Geneva overview |
| CSV export SAS URI validity | **6 hours** | SDK docs |
| Query timeout (configurable) | **5 minutes default** | SRE Agent config |

⚠️ **Critical:** `IDGrepQuery.CloseAsync()` must be called to release query resources. Orphaned queries count against the 5-concurrent-request quota.

---

## 6. Existing Tools & SDKs Landscape

### What exists today:

| Tool | Type | Gap |
|------|------|-----|
| **Jarvis Portal (Web UI)** | Web app at `portal.microsoftgeneva.com/logs/dgrep` | No CLI, no scriptability, requires browser |
| **DGrep SDK (.NET)** | NuGet package (`DGrepClient`, `DGrepUserAuthClient`) | .NET only, `DGrepUserAuthClient` doesn't work on .NET Core |
| **IcM Agent Studio Connector** | Workflow integration | Not a general-purpose tool; tied to IcM Logic Apps |
| **SRE Agent `PerformDgrepSearch`** | MCP tool for AI agents | Requires Azure Agent Space provisioning |
| **Geneva Actions** | REST gateway for production operations | Not designed for log search; requires C#/swagger extension authoring |
| **MdsDataAccessClient** | .NET library for direct blob access | Client-side processing (not distributed); different API |

### What's missing (the gap our CLI would fill):

1. **No CLI tool exists** — there is no `dgrep` command-line tool. Every interaction requires either the web UI or writing C# code against the SDK.
2. **No cross-platform tool** — the DGrep SDK is .NET-only and `DGrepUserAuthClient` doesn't support .NET Core.
3. **No scriptable interface** — shell scripts, Python scripts, and automation pipelines cannot query DGrep without writing a .NET wrapper.
4. **No output format control** — the web UI dumps results into a table; there's no JSON/CSV/TSV streaming output for piping.
5. **No saved query management** — queries are saved per-user in the Jarvis portal; there's no file-based query management.
6. **No integration with modern auth** — `az login` / `DefaultAzureCredential` aren't natively supported.

---

## 7. Recommended Architecture for Our CLI

### 7.1 High-Level Design

```
┌─────────────────────────────────────────────────────┐
│                   dgrep CLI                          │
├─────────────────────────────────────────────────────┤
│  Commands:                                           │
│  • dgrep search    — run a query                     │
│  • dgrep stream    — stream results as they arrive   │
│  • dgrep export    — export results to CSV           │
│  • dgrep config    — manage endpoints/namespaces     │
│  • dgrep saved     — manage saved queries            │
│  • dgrep endpoints — list known MDS endpoints        │
├─────────────────────────────────────────────────────┤
│  Query Layer:                                        │
│  • Parse KQL/MQL input                               │
│  • Build QueryInput from CLI flags + query text      │
│  • Handle server query + client query split          │
├─────────────────────────────────────────────────────┤
│  Auth Layer:                                         │
│  • Azure Identity (DefaultAzureCredential)           │
│  • Certificate-based (--cert flag)                   │
│  • Device code flow (--device-code flag)             │
├─────────────────────────────────────────────────────┤
│  Transport Layer:                                    │
│  • DGrep v2 Frontend REST API                        │
│  • HTTP/2 streaming for real-time results            │
├─────────────────────────────────────────────────────┤
│  Output Layer:                                       │
│  • Table (default, like Azure CLI)                   │
│  • JSON (--output json)                              │
│  • CSV (--output csv)                                │
│  • TSV (--output tsv)                                │
│  • JSONL for streaming (--output jsonl)              │
└─────────────────────────────────────────────────────┘
```

### 7.2 Core CLI Experience

```bash
# Basic search
dgrep search --endpoint diag-prod --namespace MyNamespace --event MyEvent \
  --from "2026-03-23T10:00:00Z" --to "2026-03-23T11:00:00Z" \
  --query "source | where Level <= 2 | project PreciseTimeStamp, Message"

# With identity scoping
dgrep search --endpoint diag-prod --namespace MyNamespace --event MyEvent \
  --from -30m --identity "Tenant=WUS" --identity "Role=Frontend" \
  --query "source | where Message contains 'error'"

# Stream results in real-time
dgrep stream --endpoint diag-prod --namespace MyNamespace --event MyEvent \
  --from -1h --query "source | where Level <= 2" --output jsonl

# Export to CSV
dgrep export --endpoint diag-prod --namespace MyNamespace --event MyEvent \
  --from -4h --query "source | where Status == 'Failed'" --output results.csv

# Use saved query
dgrep saved run my-error-query --from -1h

# Pipe to other tools
dgrep search ... --output json | jq '.[] | .Message'
```

### 7.3 Tech Stack Recommendation

> ⚠️ **UPDATE (2026-03-24):** This section was written assuming cross-platform was a requirement. Jonathan has since confirmed **Windows-only is acceptable**. See the **"Approach Comparison: REST API vs .NET Framework SDK"** section near the top of this document for the updated recommendation: **wrap the .NET Framework SDK in a C# console app** instead of reverse-engineering the REST API. The TypeScript recommendation below is preserved for historical context.

**Primary: TypeScript/Node.js** *(original recommendation — superseded by SDK wrapper approach)*

| Factor | Recommendation | Rationale |
|--------|---------------|-----------|
| Language | **TypeScript** | Cross-platform, fast iteration, good CLI ecosystem |
| CLI framework | **Commander.js** or **oclif** | Mature, well-documented |
| HTTP client | **node-fetch** or **undici** | Streaming support for real-time results |
| Auth | **@azure/identity** | `DefaultAzureCredential`, device code, managed identity |
| Output | **cli-table3** + custom formatters | Table, JSON, CSV, TSV, JSONL |
| Config | **cosmiconfig** | `.dgreprc`, `dgrep.config.json`, etc. |
| Packaging | **npm** + **pkg** (standalone binary) | Easy distribution |

**Why TypeScript over C# / .NET:**
1. The DGrep SDK's `DGrepUserAuthClient` doesn't work on .NET Core — we'd need to reverse-engineer the REST API anyway
2. TypeScript gives us cross-platform (Linux/Mac/Windows) with a single build
3. The Azure Identity JS SDK has full parity with the .NET version
4. Faster iteration cycle; Jonathan's team already uses Node.js tooling
5. Can be published to npm for easy installation

**Why not Python:**
- Python CLI tools have dependency management overhead (venv, pip)
- Type safety matters for API contracts
- Node.js has better streaming/async patterns for real-time result delivery

**Alternative: Go**
- Excellent for CLI tools (single binary, fast startup)
- But Azure Identity Go SDK is less mature
- Team would need Go expertise

### 7.4 API Integration Strategy

Since the DGrep SDK is .NET-only, our CLI must interface with the DGrep v2 frontend **REST API** directly. The key is reverse-engineering the HTTP protocol used by the SDK.

**Approach:**
1. **Phase 1:** Use the DGrep SDK docs + Fiddler/mitmproxy to capture the exact HTTP requests the SDK makes to `dgrepv2-frontend-prod.trafficmanager.net`
2. **Phase 2:** Implement the core query/result/streaming protocol in TypeScript
3. **Phase 3:** Add output formatting, config management, saved queries

**Known API patterns from SDK analysis:**
- `QueryInput` is serialized to JSON and POSTed to the DGrep frontend
- Results stream back (the SDK supports batched retrieval and interactive streaming)
- CSV export returns a SAS URI to a blob
- `IDGrepQuery` represents a stateful server-side query session that must be closed

---

## 8. Key Risks & Unknowns

### High Risk:
1. **REST API is undocumented** — The DGrep SDK abstracts the HTTP protocol. We need to reverse-engineer it. The SDK is the only documented interface.
2. **Authentication complexity** — Geneva uses dSTS, not standard AAD tokens. We need to determine if `@azure/identity` can produce tokens accepted by the DGrep frontend.
3. **Streaming protocol unknown** — How does the DGrep frontend stream results back? WebSocket? Server-Sent Events? Chunked HTTP? This is critical for the `stream` command.

### Medium Risk:
4. **Rate limiting enforcement** — With 5 concurrent requests per user, a CLI that doesn't properly close queries will quickly hit quota.
5. **Server query distributed nature** — Users will hit the summarize-duplication bug. CLI needs clear documentation and possibly automatic re-summarization.
6. **.NET Core gap in DGrepUserAuthClient** — If the REST API requires a specific auth flow that's only implemented in .NET Framework, we may have trouble.

### Low Risk:
7. **KQL subset differences** — CLI needs a cheat sheet or validation layer for DGrep-specific KQL quirks.
8. **Environment discovery** — Users need to know their MDS endpoint, namespace, and event names. CLI could help with discovery.

### Key Unknowns (need investigation):
- [ ] Exact HTTP protocol between DGrep SDK and DGrep v2 frontend
- [ ] Authentication token format and acquisition flow
- [ ] Whether `@azure/identity` tokens work with the DGrep frontend
- [ ] Streaming result delivery mechanism
- [ ] Whether there's an OpenAPI/Swagger spec for the DGrep API
- [ ] NuGet package name for the DGrep SDK (to inspect via decompilation)

---

## 9. Next Steps for Implementation

1. **Spike: Capture DGrep REST API** — Use the .NET SDK + Fiddler to capture HTTP traffic and document the REST protocol
2. **Spike: Auth token format** — Determine if `DefaultAzureCredential` tokens work or if custom dSTS integration is needed
3. **Design: CLI command structure** — Define the full command tree, flags, and config format
4. **Implement: Core query flow** — `QueryInput` → HTTP POST → stream results → format output
5. **Implement: Auth layer** — Support `az login`, certificate, device code
6. **Implement: Output formats** — Table, JSON, CSV, TSV, JSONL
7. **Implement: Config & saved queries** — Persistent endpoints, namespaces, saved query files
8. **Test: Cross-platform** — Verify on Windows, Linux, macOS

---

## Sources

| Source | URL | Content |
|--------|-----|---------|
| Geneva Monitoring Overview | eng.ms/docs/products/geneva/getting_started/monitoringyourdataingeneva | DGrep description, architecture |
| DGrep SDK Samples | eng.ms/docs/products/geneva/logs/references/dgrepsdk/firstsdklogsearch | SDK API, QueryInput, DGrepClient |
| DGrep KQL Reference | eng.ms/docs/products/geneva/logs/references/dgrepquerylanguage/kql | Supported KQL subset |
| DGrep MQL Reference | eng.ms/docs/products/geneva/logs/references/dgrepquerylanguage/mql | MQL language spec |
| DGrep KQL Quick Reference | eng.ms/docs/.../remote-assistance-service-wiki/.../geneva-dgrep-kql-reference | Pitfall table, migration patterns |
| IcM DGrep Connector | eng.ms/docs/products/icm/agentstudio/references/dgrepconnector | Connector inputs, quota, best practices |
| SRE Agent DGrep Config | eng.ms/docs/.../sre-agent/.../usedgreplogs | DGrep v2 endpoint, MDS endpoints, cert auth |
| Office.com DRI Handbook | eng.ms/docs/.../officecom/dri-handbook/geneva/dgrep | UI walkthrough, limitations, auth issues |
| Xbox DGrep Guide | eng.ms/docs/.../xbox-insider-services/.../how-to-use-geneva-dgrep | UI overview, saved queries, tips |
| Designer TSGs | eng.ms/docs/.../designer-tsgs/.../howto-query-logs-using-d-grep | Environment endpoints, namespace patterns |
| DGrep Summarize Bug | eng.ms/docs/.../dgrep-kql-summarize-query-return-not-aggregated-properly | Known limitation documentation |
