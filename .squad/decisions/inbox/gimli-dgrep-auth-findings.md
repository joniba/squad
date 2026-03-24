# DGrep Auth & SDK Findings

**Author:** Gimli (Tool Builder)  
**Date:** 2025-07-15  
**Context:** Phase A+B of DGrep correction plan (issue #105)

## Key Findings

### 1. DGrep SDK Authentication Model

The DGrep SDK uses **dSTS (data Security Token Service)**, NOT standard AAD/Entra tokens.

- **Interactive auth:** `new DGrepUserAuthClient(dgrepFrontendUri)` — pops AAD dialog, SDK handles dSTS exchange internally
- **Certificate auth:** `new DGrepClient(dgrepFrontendUri, X509Certificate2)` — cert-based dSTS
- **No `az login` bridge** — the SDK does NOT accept AAD tokens directly

**Implication:** `DgrepQueryExecutor` takes NO `IAuthProvider` dependency. The existing `IAuthProvider`/`AzCliAuthProvider` infrastructure is retained for `dgrep auth status/test` (validating Azure credentials exist), but the actual DGrep query executor won't use it.

### 2. DGrep SDK NuGet Package

- **Correct package:** `Microsoft.Azure.Monitoring.DGrep.SDK` version `3.0.0-rc4`
- **Wrong name in old code:** `Microsoft.Geneva.DGrep.SDK` (does not exist)
- **Feed:** `https://pkgs.dev.azure.com/msazure/_packaging/Official/nuget/v3/index.json`
- **Problem:** Returns 401 Unauthorized without Azure Artifacts credential provider with interactive auth
- **Decision:** Executor is a validated stub; actual SDK integration requires interactive credential setup

### 3. DGrep Connection Model (vs Kusto)

| Concept | Kusto (WRONG) | DGrep (CORRECT) |
|---------|---------------|-----------------|
| Server | Cluster URL (`*.kusto.windows.net`) | MDS Endpoint (`*.monitoring.core.windows.net`) |
| Container | Database | Namespace (regex) + Event (regex) |
| Query lang | KQL (full) | KQL subset (no `ago()`, `let`, `has`) |
| Auth | AAD tokens | dSTS (SDK-internal) |
| Results | Columnar (`IDataReader`) | Row-per-dict (`RowSetResult.RowSet.Rows`) |

### 4. DGrep KQL Subset Limitations

These KQL features are **NOT supported** in DGrep:
- `ago()` — use explicit datetime literals
- `let` statements
- `mv-expand` — use `mvexpand` instead
- `has`, `has_any`, `has_all` — use `contains`
- `union`, outer joins

This is why all 3 original built-in queries were replaced — they all used `ago()`, `let`, and `has`.

## Decisions Made

1. **Property renames without migration:** Cluster→Endpoint, Database removed, Namespace/Event/QueryType added. No JSON migration needed (pre-release, no deployed configs).
2. **Stub executor:** `DgrepQueryExecutor` validates inputs, throws `NotImplementedException`. Actual SDK integration blocked on interactive NuGet auth.
3. **Built-in queries rewritten:** `recent-errors`, `top-messages`, `sample-events` use only supported KQL subset.
4. **`query` verb removed:** Only `search`, `tail`, `saved`, `config`, `auth` remain.
5. **Auth resource URL:** Changed from `kusto.kusto.windows.net` to `https://management.azure.com/` for generic Azure credential validation.

## What's Left for Phase 2

- Install Azure Artifacts credential provider (interactive)
- Add actual `Microsoft.Azure.Monitoring.DGrep.SDK` 3.0.0-rc4 package reference
- Implement `DgrepQueryExecutor.ExecuteAsync` using `DGrepUserAuthClient` + `QueryInput`
- Convert `RowSetResult` to existing `QueryResult` format at boundary
- Wire up DGrep frontend URI configuration
