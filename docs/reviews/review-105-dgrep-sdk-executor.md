# Code Review: squad/105-dgrep-sdk-executor

**Reviewer:** Galadriel  
**Date:** 2025-07-19  
**Branch:** `squad/105-dgrep-sdk-executor`  
**Verdict:** ⚠️ CHANGES_REQUESTED  
**Files changed:** 30 files, +502 / −1124

---

## Summary

This PR migrates `dgrep-cli` from a Kusto-oriented query model to a DGrep SDK model. The data model changes are clean and architecturally sound — `Cluster + Database` → `Endpoint + Namespace + Event + QueryType` is the right direction. The config model, CLI options, and serialization all move together cleanly.

However, the PR has several issues that need addressing before merge:

1. The executor is a stub (throws `NotImplementedException`) — same as before — but the *context* of that stub has changed in a way that makes the auth test misleading.
2. `dgrep query` is removed entirely with no migration documentation.
3. One test class name is factually wrong and will mislead future reviewers.
4. Minor: built-in query `sample-events` has a required parameter with no documented default.

I'm not blocking on the stub-executor itself. `KustoQueryExecutor` was also a stub. Phase-1 model migration is a legitimate approach. I am blocking on the misleading auth test and the undocumented breaking change.

---

## Diff Overview

| Category | Files | Net Δ |
|---|---|---|
| Models / config | 5 | −85 |
| Commands | 4 | −362 |
| Execution | 3 | −2 |
| Auth | 2 | +6 |
| Tests | 8 | −363 |
| Config/infra | 4 | +22 |
| Squad decisions | 4 | +62 |

The deletions dominate because `QueryCommand.cs` (245 lines) and `QueryCommandTests.cs` (452 lines) are removed entirely. The additions are mostly DGrep-shaped replacements of removed code.

---

## Stage 1 — Correctness & Functional Behavior

### ✅ Model migration is correct

`QueryOptions`, `SavedQuery`, `DgrepConfig`, and `SavedOptions` all move together:

- `Cluster`/`Database` → `Endpoint`/`Namespace`  
- New fields: `Event`, `QueryType`, `IdentityColumns`
- `DefaultCluster`/`DefaultDatabase` removed from `DgrepConfig` and `ConfigManager` allowed-keys list
- The `--endpoint`, `--namespace`, `--event`, `--query-type` flags are wired correctly in `SavedOptions`

No mismatches found between model, serialization, CLI parsing, and command execution.

### ✅ SavedCommand absorbed QueryCommand's logic correctly

`ResolveTimeout` and `ResolveMaxRows` were moved from `QueryCommand` (deleted) to `SavedCommand` as `internal static`. `ParseParameters` was already on `SavedCommand`. The absorption is complete — no orphaned references.

### ❌ CHANGES_REQUESTED: `dgrep auth test` is now misleading

**File:** `src/DgrepCli/Auth/AzCliAuthProvider.cs`, `src/DgrepCli/Auth/IAuthProvider.cs`

The auth resource URL changes from `https://kusto.kusto.windows.net` to `https://management.azure.com/`. The comment in `IAuthProvider.cs` explains this:

> "DGrep SDK handles its own dSTS authentication internally — this provider is only used for CLI-level login validation."

The problem: `dgrep auth test` will succeed if the user has any valid `az login` session. But the DGrep SDK uses **dSTS** (device-side token service) via certificate/MSI, which is completely separate from Azure AD tokens. A user can pass `dgrep auth test` and still have `dgrep saved run` fail with an SDK auth error when Phase 2 lands.

This false-positive auth signal is worse than the previous behavior. With the Kusto resource, at least the auth test was testing the same identity surface the executor would use.

**Required change:** Either:
  - Update the `dgrep auth test` output to explicitly say "Validates Azure CLI login only — DGrep SDK uses separate dSTS auth", or
  - Remove the auth command entirely from this PR (it's now testing something unrelated), or
  - Add a warning banner to the auth test output.

A one-liner in the success message is the lowest-friction fix.

### ❌ CHANGES_REQUESTED: Breaking `dgrep query` removal is undocumented

**Files:** `QueryCommand.cs`, `QueryVerbOptions.cs`, `BuiltInQueries.cs` (all deleted)

The entire `dgrep query` verb is removed. Any user running `dgrep query "some kql"` will now get a CommandLine parser error with no actionable message. This is a **user-visible breaking change**.

I understand this verb was a stub (NotImplemented). But users who discovered it, scripted against it, or built onboarding docs referencing it are now silently broken. The correct approach is to leave the verb in place with an explicit error:

```
error: 'dgrep query' has been replaced by 'dgrep saved run'. See 'dgrep saved --help'.
```

Or, at minimum, add a CHANGELOG / migration note in the PR description / README.

There is no `CHANGELOG.md` or docs update in this diff that acknowledges the removal.

### ⚠️ Medium: `DgrepQueryExecutor.ExecuteAsync` is not marked `async`

**File:** `src/DgrepCli/Execution/DgrepQueryExecutor.cs`

```csharp
public Task<QueryResult> ExecuteAsync(string query, QueryOptions options, CancellationToken cancellationToken)
{
    // ...validation...
    throw new NotImplementedException("DGrep SDK integration pending...");
}
```

Without `async`, this throws synchronously rather than returning a faulted `Task`. Tests pass because `xUnit`'s `ThrowsAsync` catches both. But this is inconsistent with the interface contract and `*Async` naming convention — when Phase 2 adds real SDK calls, a developer following this pattern will not think to add `async`. Suggest adding `async` + `await Task.FromException<QueryResult>(...)` or just `async` with the throw, which is valid.

### ⚠️ Medium: `IdentityColumns` is defined but never used

**File:** `src/DgrepCli/Execution/QueryOptions.cs`

```csharp
public Dictionary<string, string> IdentityColumns { get; set; } = new();
```

No CLI option surfaces it, no test sets it, no executor reads it. If it's a placeholder for Phase 2, add a `// Phase 2: SDK identity column mapping` comment so it doesn't look like dead code.

### ✅ nuget.config is appropriately documented

The commented-out `msazure-official` feed is a reasonable Phase 2 marker. The comment is clear about what to do. No issue here.

---

## Stage 2 — Security & Design

### ✅ Auth resource is not a security regression (by itself)

Requesting tokens for `management.azure.com` for a "login check" is harmless — it's a widely-used validation target. The concern is UX accuracy (see Stage 1), not security.

### ✅ Config deserialization of old keys

Existing config files with `defaultCluster`/`defaultDatabase` will silently ignore those fields on deserialization (System.Text.Json / Newtonsoft default behavior). This is a graceful forward-migration. Users won't get parse errors — they'll just find their cluster/database config has no effect. This is acceptable given the model is changing.

### ⚠️ Medium: Validation order in `DgrepQueryExecutor` differs from convention

Old executor checked: cluster → database → query.  
New executor checks: options null → query empty → endpoint → namespace.

Query-before-connection-params is unusual UX. A user who runs `dgrep saved run my-query` with no endpoint configured will see "Query cannot be empty" if they also have no query saved, rather than "Endpoint is required." Low impact for a stub, but worth fixing when Phase 2 lands.

### ⚠️ Low: `QueryConnectionException` used for missing parameter

Using a "connection" exception for a missing `Endpoint` parameter is semantically off — this is argument validation, not a connection failure. The old code did the same with cluster. Still, it means callers can't distinguish "couldn't connect" from "missing parameter" without inspecting the message string. Consider `ArgumentException` for null/empty `Endpoint` and `QueryConnectionException` only for actual connection failures.

### ✅ No new secrets or credential handling introduced

`nuget.config` uses public NuGet.org — no credentials. The msazure feed comment mentions credential provider setup but doesn't embed any tokens. Clean.

---

## Stage 3 — Test Coverage

### ❌ CHANGES_REQUESTED: `AuthExecutorIntegrationTests` class name is factually wrong

**File:** `tests/DgrepCli.Tests/Auth/AuthExecutorIntegrationTests.cs`

This class no longer tests auth integration — it tests input validation on `DgrepQueryExecutor`. The class header comment even says: "Note: The DGrep SDK handles its own auth internally."

A future reviewer looking at test failures in `AuthExecutorIntegrationTests` will be confused. The tests that actually remain:

- `ExecuteAsync_ValidInputs_ThrowsNotImplemented`
- `ExecuteAsync_NullOptions_ThrowsArgumentNull`
- `ExecuteAsync_EmptyEndpoint_ThrowsConnectionException`
- `ExecuteAsync_MissingNamespace_ThrowsArgumentException`
- `ExecuteAsync_EmptyQuery_ThrowsArgumentException`

These are executor input validation tests. They belong in `DgrepQueryExecutorTests` or similar. The file should be renamed/refactored.

### ✅ Executor validation tests are complete for a stub

The validation guards (`null options`, empty query, empty endpoint, empty namespace) are all tested. Given the executor is a stub, this is appropriate coverage for Phase 1.

### ⚠️ Medium: 452 test lines removed with no replacement for command-level tests

`QueryCommandTests.cs` is gone. The command it tested (`QueryCommand`) is also gone. This is internally consistent. But several of its test cases covered behavior that *still exists* in `SavedCommand`:

- `ResolveTimeout` unit tests
- `ResolveMaxRows` unit tests
- Config-default-applied-to-CLI-options tests
- Parameter parsing tests

These tests should exist (or do exist) in `SavedCommandTests.cs`. Looking at the diff, `SavedCommandTests.cs` does test `ResolveTimeout` and `ResolveMaxRows` — the coverage moved correctly. No issue.

### ⚠️ Low: `sample-events` built-in query has undefaulted parameter

```csharp
new SavedQuery
{
    Name = "sample-events",
    Query = "source | take {{count}}",
    // ...
}
```

The `{{count}}` parameter has no default in the SavedQuery model (which supports `Parameters` dictionary). A user running `dgrep saved run sample-events` will get a template substitution error. Consider either:
- Adding `Parameters = new() { ["count"] = "100" }` to the built-in definition, or
- Changing the query to `"source | take 100"` and removing the parameter.

### ✅ Gimli's auth findings acknowledged

The diff includes `.squad/decisions/inbox/gimli-dgrep-auth-findings.md` — Gimli already flagged auth concerns on this branch. My Stage 1 auth finding above aligns with Gimli's direction. No duplication issue; this review confirms and expands on that signal.

---

## Required Changes Before Merge

| # | Severity | Location | Action |
|---|---|---|---|
| 1 | **HIGH** | `AuthCommand.cs` or auth output | Warn users that `dgrep auth test` validates AZ CLI only, not DGrep SDK dSTS auth |
| 2 | **HIGH** | Anywhere (README/CHANGELOG) | Document `dgrep query` removal with migration path |
| 3 | **MEDIUM** | `AuthExecutorIntegrationTests.cs` | Rename class to reflect what it actually tests |

## Suggested Changes (Non-Blocking)

| # | Severity | Location | Suggestion |
|---|---|---|---|
| 4 | Medium | `DgrepQueryExecutor.cs` | Add `async` keyword to `ExecuteAsync` |
| 5 | Medium | `QueryOptions.cs` | Add Phase 2 comment to `IdentityColumns` |
| 6 | Low | `GetBuiltInQueries()` in `SavedCommand.cs` | Add default `count=100` to `sample-events` |
| 7 | Low | `DgrepQueryExecutor.cs` | Swap validation order to: endpoint → namespace → query |

---

## What Works Well

- The model migration is clean and complete — no dangling references to `Cluster`/`Database`.
- `nuget.config` is transparent about Phase 2 status.
- Built-in queries are simpler but functionally valid for DGrep KQL.
- `SavedCommandTests.cs` successfully absorbs the relevant QueryCommand test cases.
- Auth resource change is correctly explained in code comments.
- The squad decision inbox entry from Gimli shows good team communication about auth tradeoffs.

---

## Verdict

**⚠️ CHANGES_REQUESTED**

The structural migration is solid. The executor stub is acceptable as Phase 1. But items 1–3 (misleading auth signal, undocumented breaking change, wrong test class name) must be addressed before this merges to main. All three are low-effort fixes.

---

## Cycle 2 Re-Review

**Reviewer:** Galadriel  
**Date:** 2025-07-19  
**Fix commit:** `4e0078c` — *fix: address Galadriel review findings for #105*  
**Verdict:** ✅ APPROVED

### Finding Resolution

| # | Severity | Finding | Status |
|---|---|---|---|
| H1 | HIGH | `dgrep auth test` misleads users — no dSTS warning | ✅ Fixed |
| H2 | HIGH | `dgrep query` removal undocumented | ✅ Fixed |
| M1 | MEDIUM | `AuthExecutorIntegrationTests` class name factually wrong | ✅ Fixed |

#### H1 — dSTS warning in `dgrep auth test` output

**File:** `src/DgrepCli/Commands/AuthCommand.cs`

The fix adds a warning banner immediately after the successful auth confirmation:

```
⚠️  Note: This validates Azure CLI authentication only. DGrep SDK uses dSTS for
    authentication, which is validated at query time.
```

Placed correctly inside the success block (`return 0`), so it surfaces only when auth succeeds — exactly when a user might incorrectly assume they are "ready" to run queries. **Fully addresses H1.**

#### H2 — Migration note for `dgrep query` removal

**File:** `tools/dgrep-cli/CHANGELOG.md` (new file)

A `CHANGELOG.md` is created under the tool root with a clear breaking-change entry:

> **`dgrep query` verb removed.** The `dgrep query` command has been replaced by `dgrep saved run`. Update any scripts or workflows that invoke `dgrep query` to use `dgrep saved run <name>` instead.

The entry also documents the config-key migration (`defaultCluster`/`defaultDatabase` silently ignored) and the auth-resource change. This is more than the minimum required. **Fully addresses H2.**

#### M1 — Test class renamed

**File:** `tests/DgrepCli.Tests/Auth/AuthExecutorIntegrationTests.cs` → `DgrepQueryExecutorValidationTests.cs`

Both the file and the class name are updated to `DgrepQueryExecutorValidationTests`. The rename is a clean git rename (95% similarity), confirming no test logic was accidentally dropped. **Fully addresses M1.**

### Regression Check

The fix commit is surgical — three discrete changes with no unrelated modifications:

1. Three lines added to `AuthCommand.cs` (warning text only, no logic change).
2. New `CHANGELOG.md` created (docs only, no compilation impact).
3. File + class rename in test project (cosmetic, no test logic altered).

No regressions detected. The non-blocking suggestions from Cycle 1 (async keyword on executor, `IdentityColumns` comment, `sample-events` default count) remain open as tech debt; none were required for merge and their absence is acceptable.

### Final Verdict

**✅ APPROVED**

All three required changes are addressed correctly and with appropriate scope. The fix does not introduce regressions. This branch is clear to merge.
