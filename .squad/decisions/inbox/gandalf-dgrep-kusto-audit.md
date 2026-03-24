### 2026-03-28: DGrep CLI — Kusto Contamination Audit

**Author:** Gandalf (Lead)  
**Context:** The DGrep CLI (`tools/dgrep-cli/`) queries Geneva DGrep endpoints, NOT Kusto clusters. KQL is a valid query language (DGrep supports KQL), but references to Kusto clusters, Kusto SDK, Kusto connection strings are wrong.

---

## Summary

**47 "Kusto" references** across 13 files. **48 `.kusto.windows.net` URLs** across 13 files (test data). Most are in tests using Kusto cluster URLs as fake connection targets when they should use DGrep MDS endpoints.

## Files with Kusto References — Cleanup Plan

### Source Files (MUST FIX)

| File | Kusto Refs | What's Wrong | Action |
|------|-----------|--------------|--------|
| `src/DgrepCli/Execution/KustoQueryExecutor.cs` | 9 | **Entire file is named wrong.** Class is `KustoQueryExecutor`, comments reference `Microsoft.Azure.Kusto.Data`, `KustoConnectionStringBuilder`, `KustoClientFactory`. | **RENAME** to `DgrepQueryExecutor.cs`, rename class to `DgrepQueryExecutor`. Replace all Kusto SDK comments with DGrep SDK equivalents. |
| `src/DgrepCli/Commands/QueryVerbOptions.cs` | 2 | Help text says "Execute a KQL query against a Kusto cluster" and "--cluster" help says "Kusto cluster URL". Example uses `help.kusto.windows.net`. | **REPLACE** help text: "Execute a query against a DGrep endpoint". Replace example URL with DGrep MDS endpoint format. |
| `src/DgrepCli/Program.cs` | 3 | Line 157: "Execute a KQL query against a Kusto cluster". Lines 113, 123: `new KustoQueryExecutor(...)`. | **UPDATE** help text to say "DGrep endpoint" not "Kusto cluster". Update class references after rename. |
| `src/DgrepCli/Execution/QueryOptions.cs` | 1 | XML comment: "Kusto cluster connection string or URL". | **REPLACE** with "DGrep MDS endpoint URL". |
| `src/DgrepCli/Execution/QueryException.cs` | 1 | XML comment: "pass through from Kusto", parameter named `kustoError`. | **RENAME** parameter to `serverError`. Update comment to "pass through from DGrep server". |
| `src/DgrepCli/Auth/IAuthProvider.cs` | 1 | XML comment: "obtain a bearer token for Kusto connections". | **REPLACE** with "obtain a bearer token for DGrep connections". |
| `src/DgrepCli/Auth/AzCliAuthProvider.cs` | 1 | Hardcoded `https://kusto.kusto.windows.net` as token resource URL. | **REPLACE** with correct DGrep/Geneva resource URL. |
| `src/DgrepCli/Commands/AuthCommand.cs` | 2 | Fallback cluster is `https://kusto.kusto.windows.net`, message says "Using Kusto resource URL". | **REPLACE** with DGrep endpoint and appropriate message. |

### Test Files (FIX — test data uses wrong URLs)

| File | Kusto Refs | Action |
|------|-----------|--------|
| `tests/DgrepCli.Tests/Execution/ExecutionTests.cs` | 5 | Rename `KustoQueryExecutorTests` class. Replace `.kusto.windows.net` URLs with DGrep endpoints. |
| `tests/DgrepCli.Tests/Auth/AuthExecutorIntegrationTests.cs` | 8 | Comment references `KustoQueryExecutor`. Replace all `.kusto.windows.net` URLs. |
| `tests/DgrepCli.Tests/Auth/AzCliAuthProviderTests.cs` | 9 | All test URLs use `kusto.kusto.windows.net`. Replace with DGrep resource URL. |
| `tests/DgrepCli.Tests/Auth/AuthCommandTests.cs` | 1 | Config uses `.kusto.windows.net`. Replace. |
| `tests/DgrepCli.Tests/Auth/CertificateAuthProviderTests.cs` | 2 | Token resource URLs. Replace. |
| `tests/DgrepCli.Tests/Auth/ManagedIdentityAuthProviderTests.cs` | 1 | Token resource URL. Replace. |
| `tests/DgrepCli.Tests/Commands/QueryCommandTests.cs` | 1 | Cluster URL in test data. Replace. |
| `tests/DgrepCli.Tests/Commands/SavedCommandTests.cs` | 17 | Heavy use of `.kusto.windows.net` in saved query tests. Bulk replace. |
| `tests/DgrepCli.Tests/Commands/SavedOptionsTests.cs` | 2 | Cluster URL in options tests. Replace. |

### Files That Are FINE (KQL references are correct)

| File | Why It's OK |
|------|-------------|
| `README.md` | Documents "DGrep KQL Pitfalls" — KQL is the query language, this is correct. Also has a ❌ Kusto / ✅ DGrep comparison table. |
| `PLAN.md` | References KQL validation/linting — correct (DGrep uses KQL). |
| `src/DgrepCli/Commands/SearchOptions.cs` | `--query-type kql` option — correct. |
| `src/DgrepCli/Commands/TailOptions.cs` | `--query-type kql` option — correct. |
| `src/DgrepCli/Commands/SavedOptions.cs` | "KQL query template" — correct. |
| `src/DgrepCli/Config/OptionResolver.cs` | Default query type is `kql` — correct. |
| `src/DgrepCli/Config/DgrepConfig.cs` | "Default query type: kql or mql" — correct. |
| `src/DgrepCli/Commands/OptionValidator.cs` | Validates `kql` / `mql` — correct. |
| `src/types/index.ts` | TypeScript types (legacy, pre-pivot) — `KQL` as query type is correct. |

## Key Decisions Needed

### 1. Should `KustoQueryExecutor.cs` become `DgrepQueryExecutor.cs`?

**YES.** This is a DGrep CLI. The executor connects to DGrep endpoints, not Kusto clusters. The DGrep SDK is `Microsoft.Azure.Monitoring.DGrep.SDK`, not `Microsoft.Azure.Kusto.Data`. Rename the file AND class.

### 2. Should the `query` verb be removed?

**NO — but its help text must change.** The `query` verb is fine for a CLI that runs queries. The problem is the help text ("Execute a KQL query against a Kusto cluster") and the example URL (`help.kusto.windows.net`). Fix: "Execute a query against a DGrep endpoint" with a Geneva MDS endpoint example.

The `query` verb is NOT Kusto-specific — it's a general concept. `dgrep query "..."` reads naturally for a DGrep CLI.

### 3. What about `--cluster` option name?

**RENAME to `--endpoint`.** DGrep doesn't have "clusters" — it has MDS endpoints/namespaces. The `--cluster` flag is a Kusto mental model leak. `--endpoint` is correct for DGrep.

### 4. What about `--database` option?

**RENAME to `--namespace` or `--event`.** DGrep uses namespace + event, not database. This is another Kusto concept leak.

## Effort Estimate

- **Source files:** ~2 hours (8 files, mostly find-and-replace + rename)
- **Test files:** ~3 hours (9 files, 46+ URL replacements, class renames, must verify tests still pass)
- **Total:** ~5 hours for Gimli, tracked as a single issue

## Recommendation

File one GitHub issue: "[dgrep-cli] Remove Kusto contamination — rename to DGrep concepts". Assign to Gimli. This is a straightforward bulk rename + find-replace. All 274 tests must still pass after.

---

*— Gandalf, Lead*
