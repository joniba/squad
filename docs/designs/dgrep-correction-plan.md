# DGrep CLI Correction Plan — Wrong SDK, Full Course Correction

**Author:** Gandalf (Lead)  
**Date:** 2026-03-28  
**Status:** Proposed  
**Triggered by:** Jonathan ("own the dgrep mess")  
**Context:** The DGrep CLI was built against `KustoQueryExecutor` / Kusto SDK concepts — but DGrep is NOT Kusto. It has its own SDK (`Microsoft.Azure.Monitoring.DGrep.SDK`). The entire query execution layer is built against the wrong product.

---

## 1. How Did This Happen?

### The Root Cause: Concept Confusion

DGrep uses a **subset of KQL** as its query language, and the Jarvis UI feels similar to Kusto Explorer. This surface-level similarity created a false equivalence: "DGrep speaks KQL → DGrep is Kusto." It is not. DGrep is a distributed brute-force log scanner over Geneva Logs. Kusto is an indexed analytics engine. They are different products with different SDKs, different auth, different APIs, and different connection models.

### The Timeline of the Error

| Date | Event | What Went Wrong |
|------|-------|-----------------|
| 2026-03-23 | Elrond's research (`geneva-dgrep-research.md`) | **Research was thorough and correct.** Clearly documented DGrep SDK, DGrep auth (dSTS), DGrep-specific types (`QueryInput`, `EventFilter`, `DGrepClient`). Named the SDK as `Microsoft.Geneva.DGrep.SDK` (wrong name, later corrected to `Microsoft.Azure.Monitoring.DGrep.SDK` by POC). |
| 2026-03-23 | PLAN.md v1 (TypeScript) | Plan correctly described DGrep concepts (endpoints, namespaces, events, MQL). No Kusto confusion yet. |
| 2026-03-24 | Pivot decision (TypeScript → C#) | Jonathan directed pivot. Gandalf created new issues #101–#109 for C# implementation. **The pivot introduced "Kusto" language:** issues reference "Kusto cluster," "database," and the query command help text says "Execute a KQL query against a Kusto cluster." |
| 2026-03-24 | Gimli scaffolds C# project | `KustoQueryExecutor.cs` created. `QueryOptions` has `Cluster` and `Database` properties (Kusto concepts, not DGrep concepts). DGrep uses `MdsEndpoint`, `Namespace`, `Event` — completely different connection model. |
| 2026-03-24–25 | Phases 1.1–2.2 built (#101–#105) | 6 PRs merged. `KustoQueryExecutor` stub references `Microsoft.Azure.Kusto.Data` in comments. Auth validates against `https://kusto.kusto.windows.net`. Built-in queries use Kusto-native syntax (`ago()`, `percentile()`) that DGrep doesn't support. |
| 2026-03-25 | Issue #110 — POC | POC confirmed the **DGrep SDK** works. But the POC was on a separate branch (`squad/dgrep-poc`), and its findings were never integrated back. The main codebase continued building against Kusto concepts. |

### Who Made the Wrong Assumption?

**Gandalf (me).** I own this.

When I created the C# pivot issues (#101–#109), I should have re-read Elrond's research and carried over the DGrep-specific concepts. Instead, I defaulted to the more familiar Kusto mental model. The issue descriptions for #103 (query execution) and #104 (auth) reference Kusto patterns, not DGrep patterns. Gimli built exactly what the issues described.

Elrond's research was **correct** — it clearly distinguishes DGrep from Kusto. The POC was **correct** — it used the DGrep SDK. The bug was in the translation layer: my issue descriptions for the C# pivot.

### Why Didn't the POC Catch This?

The POC (issue #110, branch `squad/dgrep-poc`) **did** prove the DGrep SDK works. But:

1. **The POC ran in parallel with Phase 1, not before it.** By the time POC results were available, Gimli had already built the CLI scaffolding, config management, and was deep into the query execution engine — all using Kusto concepts.
2. **POC findings weren't gated.** The PLAN.md said "Phase 2 is BLOCKED on Phase 0 spikes" but that was for the TypeScript plan. After the C# pivot, no one created an equivalent gate. Gimli started building immediately.
3. **POC was on a feature branch that was never merged.** The `squad/dgrep-poc` branch has `POC_RESULTS.md` documenting the correct SDK, correct types, correct package name — but this was never merged to main or referenced in the build issues.

### Why Didn't Galadriel (Reviewer) Catch This?

Galadriel reviews PRs for code quality, test coverage, and scope adherence — not for "is this the right SDK?" She has no Geneva/DGrep domain expertise. The code was well-structured, well-tested, and matched the issue descriptions. The issue descriptions were wrong, so the PRs were "correct" relative to their specifications.

This is a **design review gap**, not a **code review gap**. I (Gandalf) approved the design. The design was wrong.

---

## 2. What's Salvageable vs. What's Waste

### ✅ KEEP (Reusable As-Is or With Minor Changes)

| Component | Files | Why Reusable |
|-----------|-------|--------------|
| **CLI scaffolding** | `Program.cs`, `SearchOptions.cs`, `TailOptions.cs` | Arg parsing, help text, version handling — all SDK-agnostic. `SearchOptions` already has DGrep-correct flags (endpoint, namespace, event, identity, query-type). |
| **Output formatters** | `Formatters/*.cs` (Table, JSON, CSV) | Format `QueryResult` → stdout. SDK-agnostic. Work with any columnar result set. |
| **Config management** | `Config/*.cs` (ConfigManager, DgrepConfig, OptionResolver) | JSON config at `~/.dgrep/config.json`. DgrepConfig already has DGrep-correct fields (defaultNamespace, defaultEndpoint, defaultQueryType). |
| **Saved queries framework** | `SavedCommand.cs` (parameter substitution, template engine) | The `{{param}}` substitution, list/add/remove/show/run lifecycle — all SDK-agnostic. |
| **Exception hierarchy** | `QueryException.cs` | Base exception types. `QueryConnectionException`, `QueryTimeoutException`, `QueryAuthException` — reusable with DGrep-specific messages. |
| **Mock executor** | `MockQueryExecutor.cs` | SDK-agnostic test double. Works against `IQueryExecutor` interface. |
| **Project infrastructure** | `.sln`, `.csproj`, `.gitignore`, test project | Build system is fine. Just add the DGrep NuGet package. |

**Estimated reuse: ~60-65% of codebase.**

### 🔄 REWORK (Salvageable Core, Wrong Details)

| Component | Files | What's Wrong | Fix |
|-----------|-------|-------------|-----|
| **IQueryExecutor interface** | `IQueryExecutor.cs` | Signature is `ExecuteAsync(string query, QueryOptions options, ct)`. DGrep needs `QueryInput` (not a string query + options). | Redesign interface to accept DGrep `QueryInput` or an equivalent DTO. |
| **QueryOptions** | `QueryOptions.cs` | Has `Cluster` and `Database` (Kusto concepts). DGrep uses `MdsEndpoint`, `Namespace`, `Event`, `IdentityColumns`. | Replace with DGrep-specific options. |
| **QueryResult/ColumnDefinition** | `QueryResult.cs` | Structure is close but DGrep returns `IEnumerable<Dictionary<string, object>>`, not a columnar result set with separate schema. | Adapt to DGrep's result format. |
| **Auth providers** | `Auth/*.cs` | `AzCliAuthProvider` validates against `https://kusto.kusto.windows.net`. DGrep uses dSTS — the SDK handles auth internally via `DGrepUserAuthClient` or `DGrepClient(endpoint, cert)`. | Auth layer needs fundamental rethink. SDK handles auth; CLI just needs to choose interactive vs. cert mode. |
| **QueryCommand** | `QueryCommand.cs` | References `Cluster`, `Database`, Kusto connection strings. | Rewrite to use DGrep connection model (MdsEndpoint + namespace + event). |
| **QueryVerbOptions** | `QueryVerbOptions.cs` | `--cluster`, `--database` flags. Help text says "Kusto cluster." | Remove entirely or repurpose. The `search` verb already has the right DGrep flags. |
| **Built-in queries** | `BuiltInQueries.cs` | Uses `ago()`, `percentile()`, `exceptions`, `requests` — Kusto/App Insights syntax that DGrep doesn't support. | Rewrite with DGrep-compatible KQL subset (no `ago()`, no `let`, no `percentile()`). |

**Estimated rework: ~25-30% of codebase.**

### ❌ DELETE (Wrong Product, No Salvage Value)

| Component | Files | Why Delete |
|-----------|-------|-----------|
| **KustoQueryExecutor** | `KustoQueryExecutor.cs` | Built against `Microsoft.Azure.Kusto.Data` patterns. Comments reference `KustoConnectionStringBuilder`, `KustoClientFactory`, `ClientRequestProperties`. None of this applies to DGrep. |
| **`query` verb** | `QueryVerbOptions.cs`, `QueryCommand.cs` (in current form) | The entire "query" command is a Kusto query executor. DGrep queries go through the `search` verb with endpoint/namespace/event scoping. Having both `search` and `query` makes no sense for DGrep. |
| **Kusto-specific tests** | `ExecutionTests.cs` (KustoQueryExecutorTests) | Tests validate Kusto-specific behavior (cluster URLs, "kusto.windows.net" strings). |
| **Auth validation target** | `AzCliAuthProvider.ValidateAsync` hardcoded `https://kusto.kusto.windows.net` | Wrong resource. DGrep SDK handles its own auth. |

**Estimated waste: ~10-15% of codebase.**

### Test Assessment (274 test methods)

| Category | Estimated Count | Salvageable? |
|----------|----------------|-------------|
| Formatter tests (Table, JSON, CSV, Factory) | ~60 | ✅ Yes — SDK-agnostic |
| Config tests (ConfigManager, DgrepConfig, OptionResolver) | ~50 | ✅ Yes — SDK-agnostic |
| CLI option parsing tests (Search, Tail, Config, Saved options) | ~60 | ✅ Yes — SDK-agnostic |
| Auth provider tests (AzCli, Cert, MI, Factory, Config) | ~50 | 🔄 Partially — structure reusable, resource URLs/patterns need updating |
| Mock executor tests | ~10 | ✅ Yes — SDK-agnostic |
| Kusto executor tests | ~10 | ❌ No — test the wrong executor |
| Query command tests | ~20 | 🔄 Partially — test flow is right, Kusto-specific assertions need updating |
| Saved command tests | ~14 | 🔄 Partially — parameter substitution tests are fine, execution tests need updating |

**Estimated test salvage: ~200 of 274 tests (~73%) are SDK-agnostic and fully reusable.**

---

## 3. The Correct Architecture

### Connection Model: DGrep vs. Kusto

```
KUSTO (what we built):
  cluster URL + database name → KustoConnectionStringBuilder → query string

DGREP (what we need):
  MDS endpoint + namespace regex + event regex + identity columns → QueryInput → DGrepClient/DGrepUserAuthClient
```

### Auth Model: DGrep vs. Kusto

```
KUSTO (what we built):
  az login → AAD token → KustoConnectionStringBuilder.WithAadToken()
  Certificate → KustoConnectionStringBuilder.WithAadCertificate()

DGREP (what we need):
  Interactive → DGrepUserAuthClient(dgrepFrontendUri) → SDK handles dSTS internally
  Certificate → DGrepClient(dgrepFrontendUri, X509Certificate2) → SDK handles dSTS internally
  No az-login bridge (SDK doesn't accept AAD tokens directly — it does its own dSTS flow)
```

### Correct SDK Package

```xml
<PackageReference Include="Microsoft.Azure.Monitoring.DGrep.SDK" Version="3.0.0-rc4" />
```

**Feed:** `https://pkgs.dev.azure.com/msazure/_packaging/Official/nuget/v3/index.json`

**NOT** `Microsoft.Geneva.DGrep.SDK` (docs were wrong), **NOT** `Microsoft.Azure.Kusto.Data` (wrong product entirely).

---

## 4. Action Plan

### Phase A: Cleanup (Gimli, ~0.5 day)

1. **Delete `KustoQueryExecutor.cs`** — wrong SDK, no salvage value
2. **Delete the `query` verb** (`QueryVerbOptions.cs`, `QueryCommand.cs`) — Kusto-specific command that doesn't apply to DGrep. The `search` verb already has the correct DGrep flags.
3. **Delete `KustoQueryExecutorTests`** from `ExecutionTests.cs`
4. **Delete `BuiltInQueries.cs`** — Kusto-native syntax. Will rewrite with DGrep-compatible queries later.
5. **Remove Kusto references** from `Program.cs` help text (line 157: "Execute a KQL query against a Kusto cluster")
6. **Fix `AzCliAuthProvider.ValidateAsync`** — remove hardcoded `https://kusto.kusto.windows.net` resource
7. **Update `QueryOptions.cs`** — rename `Cluster`→`MdsEndpoint`, remove `Database`, add `Namespace`, `Event`, `IdentityColumns`

### Phase B: Correct POC Integration (Gimli, ~1 day)

1. **Add DGrep SDK NuGet reference** to `DgrepCli.csproj` (the commented-out reference currently says `Microsoft.Geneva.DGrep.SDK` — wrong package name)
2. **Create `DgrepQueryExecutor.cs`** implementing `IQueryExecutor` — wraps `DGrepUserAuthClient` or `DGrepClient`
3. **Run one real query** using the SDK from the CLI: `dgrep search --endpoint <endpoint> --namespace <ns> --event <event> --query "where Level <= 2" --from -1h`
4. **Validate result mapping** — DGrep returns `IEnumerable<Dictionary<string, object>>`; map to `QueryResult` for formatters
5. **Gate:** Do NOT proceed to Phase C until Phase B produces a working query with real results

### Phase C: Auth Rework (Gimli, ~1 day)

1. **Simplify auth layer** — DGrep SDK handles dSTS internally. The CLI's job is simpler:
   - Interactive mode: `new DGrepUserAuthClient(frontendUri)` — SDK pops AAD dialog
   - Certificate mode: `new DGrepClient(frontendUri, cert)` — SDK uses cert for dSTS
   - No `az login` bridge (SDK doesn't support it natively; investigate later)
2. **Update `AuthProviderFactory`** to create DGrep auth modes instead of AAD token providers
3. **Update `AuthCommand`** to test DGrep connectivity, not Kusto connectivity

### Phase D: Built-In Queries & Polish (Gimli, ~0.5 day)

1. **Rewrite built-in queries** with DGrep-compatible KQL subset (no `ago()`, `let`, `percentile()`, `mv-expand`)
2. **Update SavedCommand** to wire through `DgrepQueryExecutor` instead of `KustoQueryExecutor`
3. **Update tests** — fix the ~50 auth tests and ~34 command tests that reference Kusto-specific patterns

### Phase E: Documentation Update (Bilbo, ~0.5 day)

1. **Update PLAN.md** — remove TypeScript directory structure, update architecture decisions to reflect SDK wrapper
2. **Update README.md** — correct SDK references, installation instructions, auth documentation
3. **Fix package name** in all docs: `Microsoft.Geneva.DGrep.SDK` → `Microsoft.Azure.Monitoring.DGrep.SDK`

### New Issues to Create

| # | Title | Owner | Phase | Dependencies |
|---|-------|-------|-------|-------------|
| NEW-1 | DGrep cleanup: delete KustoQueryExecutor and Kusto-specific code | Gimli | A | None |
| NEW-2 | DGrep SDK integration: create DgrepQueryExecutor with real query | Gimli | B | NEW-1 |
| NEW-3 | DGrep auth rework: simplify to SDK-managed dSTS | Gimli | C | NEW-2 |
| NEW-4 | DGrep built-in queries: rewrite for DGrep KQL subset | Gimli | D | NEW-2 |
| NEW-5 | DGrep docs update: fix SDK references, auth docs, PLAN.md | Bilbo | E | NEW-2 |

### Timeline

| Phase | Duration | Can Start |
|-------|----------|-----------|
| A (Cleanup) | 0.5 day | Immediately |
| B (SDK Integration) | 1 day | After A |
| C (Auth Rework) | 1 day | After B |
| D (Built-In Queries) | 0.5 day | After B (parallel with C) |
| E (Docs) | 0.5 day | After B (parallel with C/D) |
| **Total** | **~3 days** | |

### Gate: Phase B Must Succeed Before C/D/E

Phase B is the critical proof point. If the DGrep SDK cannot produce a working query from the CLI, we stop and reassess. Do NOT repeat the mistake of building 5 phases of features before validating the core integration.

---

## 5. Existing Issues Status

Issues #101–#109 (C# DGrep CLI) and #110 (POC):
- **#101 (CLI arg parsing):** ✅ Done — KEEP. SearchOptions has correct DGrep flags.
- **#102 (Config management):** ✅ Done — KEEP. DgrepConfig has correct DGrep fields.
- **#103 (Query execution engine):** ✅ Done — **REDO.** KustoQueryExecutor is the wrong product.
- **#104 (Auth flow):** ✅ Done — **REDO.** Auth validates against Kusto, should use DGrep SDK auth.
- **#105 (Saved queries):** ✅ Done — **PARTIAL REDO.** Framework is reusable; built-in queries and executor wiring need replacement.
- **#106 (Tail/streaming):** Not started — will build against correct SDK.
- **#107 (Saved queries polish):** Not started — depends on corrected executor.
- **#108 (Error handling):** Not started — depends on corrected executor.
- **#109 (Documentation):** Not started — will write against corrected architecture.
- **#110 (POC):** ✅ Done — POC was correct but findings were never integrated.

---

## 6. Process Fixes

To prevent this pattern from recurring:

1. **POC gates are mandatory.** When a POC exists, its branch must merge (or its findings must be integrated) before feature work begins. Add this to Gandalf's charter.
2. **Design review after pivot.** When we pivot tech stack (TypeScript → C#), the pivot decision must include a design review of all new issues against the research. I skipped this step.
3. **Domain terms in issue descriptions.** Issue descriptions must use the correct domain terms. If the tool is "dgrep," the issues should say "DGrep endpoint," not "Kusto cluster." This is a signal check.
4. **Galadriel gets a domain context brief.** Before reviewing a track of PRs, Galadriel should receive a one-paragraph brief on what product the code targets. She can't catch "wrong SDK" without knowing what SDK is right.

---

## 7. Honest Assessment

**Work invested:** 6 PRs merged, 274 tests written, ~2000 lines of production code.  
**Work wasted:** ~35% needs deletion or significant rework (KustoQueryExecutor, query verb, auth validation, built-in queries).  
**Work salvaged:** ~65% is SDK-agnostic and fully reusable (CLI scaffolding, formatters, config, saved query framework, most tests).  
**Recovery cost:** ~3 developer-days to correct.  
**Could this have been prevented?** Yes. If the POC had been a gate (not a parallel track), or if I had written the C# pivot issues using DGrep terminology instead of Kusto terminology, Gimli would have built the right thing. This is a design failure, not an implementation failure.
