# Review: `squad/108-dgrep-error-handling`

**Reviewer:** Galadriel  
**Branch:** `squad/108-dgrep-error-handling`  
**Date:** 2026-03-24 (retroactive — branch closed without review)  
**Requested by:** Jonathan (via Ralph — protocol violation recovery)  
**Diff scope:** 10 files, 899 additions, 32 deletions (excluding `.squad/`)

---

## Summary

This branch adds three related features to the `dgrep-cli` tool:

1. **Retry policy** — exponential backoff with jitter, transient classification, configurable limits
2. **Error handling** — enriched exception hierarchy, actionable messages, new `QueryRateLimitException`
3. **Exit codes** — `DgrepExitCodes` constants (0 success / 1 user / 2 transient / 3 auth)

The design and implementation quality of the new components, taken in isolation, is excellent: the decorator pattern is clean, the backoff math is correct, and the test suite (41 new tests) is thorough and well-structured.

**However, two critical integration gaps mean the feature does not actually function end-to-end.** The retry decorator is never instantiated, and the exit code constants are never applied. See findings C1 and C2.

---

## Stage 1 — Correctness & Design

### ✅ Retry math is correct

`RetryPolicy.GetDelay` computes `BaseDelay × 2^(attempt−1)`, caps the exponent at 10 to avoid overflow, applies ±25% jitter, then caps at `MaxDelay`. The ordering (jitter before cap) means any pre-cap value is correctly bounded.

### ✅ Thread-safety of `Random`

`[ThreadStatic]` with `Guid.NewGuid().GetHashCode()` as seed is a well-understood pattern. Each thread gets its own independently seeded `Random`, avoiding lock contention.

### ✅ `OperationCanceledException` is correctly excluded from retry

The guard `when (ct.IsCancellationRequested)` ensures user-initiated cancellation propagates immediately. This is the standard pattern.

### ✅ Backward-compat alias for `ClusterUrl`

`public string ClusterUrl => EndpointUrl;` is the right way to rename a public property without breaking callers. The diff confirms the property rename is intentional.

### ✅ Decorator pattern for `RetryingQueryExecutor`

Wraps `IQueryExecutor` without modifying it. Injects `TextWriter` for stderr — correctly testable.

---

## Stage 2 — Findings

### 🔴 CRITICAL C1 — `RetryingQueryExecutor` is never instantiated (dead code)

**File:** `tools/dgrep-cli/src/DgrepCli/Program.cs` (unchanged by PR)  
**Evidence:**

```csharp
// RunQuery (Program.cs, unchanged)
var executor = new KustoQueryExecutor(authProvider);
var command = new QueryCommand(executor, configManager);

// RunSaved (Program.cs, unchanged)
var executor = new KustoQueryExecutor(authProvider);
var command = new SavedCommand(executor, configManager);
```

`Program.cs` creates `KustoQueryExecutor` directly in every command handler. `RetryingQueryExecutor` is never inserted into this chain. Retry logic is fully implemented but completely inactive at runtime.

**Required fix:** Wrap the executor before passing it to command handlers:
```csharp
var executor = new RetryingQueryExecutor(new KustoQueryExecutor(authProvider));
```

---

### 🔴 CRITICAL C2 — Exit code constants are never used (all failures return 1)

**File:** `tools/dgrep-cli/src/DgrepCli/Commands/QueryCommand.cs` (unchanged by PR)

Every catch block in `QueryCommand.Execute` hardcodes `return 1`:

```csharp
catch (QueryAuthException ex)    { _stderr.WriteLine(...); return 1; }  // should be DgrepExitCodes.AuthFailure (3)
catch (QueryConnectionException) { _stderr.WriteLine(...); return 1; }  // should be DgrepExitCodes.TransientFailure (2)
catch (QueryTimeoutException)    { _stderr.WriteLine(...); return 1; }  // should be DgrepExitCodes.TransientFailure (2)
catch (OperationCanceledException) { _stderr.WriteLine(...); return 1; } // should be DgrepExitCodes.TransientFailure (2)
```

`DgrepExitCodes` is defined in this PR but not applied. Automation scripts that check the exit code cannot distinguish auth failures from transient failures from user errors — all three return `1`.

**Required fix:** Update each catch block in `QueryCommand.cs` (and `SavedCommand.cs`) to return the appropriate `DgrepExitCodes.*` constant.

---

### 🟠 HIGH H1 — `QueryCommand.cs` still uses deprecated `ex.ClusterUrl`

**File:** `tools/dgrep-cli/src/DgrepCli/Commands/QueryCommand.cs` (unchanged by PR)  
**Line:** Inside the `catch (QueryConnectionException ex)` block:

```csharp
_stderr.WriteLine($"Check that the cluster URL is correct: {ex.ClusterUrl}");
```

The PR renames `ClusterUrl` → `EndpointUrl` (keeping `ClusterUrl` as a compat alias). The `QueryCommand.cs` catch block was not updated to use the new canonical property. This is a maintenance signal that the integration wiring step was skipped entirely.

---

### 🟠 HIGH H2 — `QueryRateLimitException` has no dedicated catch branch in `QueryCommand.cs`

**File:** `tools/dgrep-cli/src/DgrepCli/Commands/QueryCommand.cs`

When retries are exhausted on a rate-limit failure, `QueryRateLimitException` (a subclass of `QueryException`) falls to the base `QueryException` catch and returns `1`. It should return `DgrepExitCodes.TransientFailure (2)`. This is entangled with C2, but also means the catch order would need careful attention: `QueryRateLimitException` should be caught before `QueryException`.

---

### 🟡 MEDIUM M1 — HTTP status detection from exception message strings is fragile

**File:** `tools/dgrep-cli/src/DgrepCli/Execution/RetryPolicy.cs`  
**Method:** `IsTransientHttpException`

```csharp
return message.Contains("429")
    || message.Contains("503")
    || message.Contains("502")
    || message.Contains("504");
```

A query syntax error like _"Error at line 503 of your query"_ or a timestamp _"Event logged at 2024-05-02T04:29:00Z"_ would be misclassified as transient and silently retried. The number `429` also appears in log lines, stack traces, and error payloads unrelated to HTTP status.

**Preferred fix:** Check for a structured `HttpStatusCode` property on the exception, or pattern-match on a more specific format like `"HTTP 429"` or `"status: 429"`.

---

### 🟡 MEDIUM M2 — Jitter range of ±25% is narrow for thundering herd prevention

**File:** `tools/dgrep-cli/src/DgrepCli/Execution/RetryPolicy.cs`

```csharp
var jitterFactor = 0.75 + (JitterRandom.NextDouble() * 0.5); // 0.75 to 1.25
```

AWS and Google's retry guidance recommends "full jitter" (0 to 100% of the computed delay) for service-level retries. ±25% means all concurrent callers retry within a tight window — 750ms–1250ms on the first retry — which can still cause a thundering herd on services with many CLI users. For a CLI tool (typically one-to-few concurrent users), this is low risk in practice, but the comment _"to avoid thundering herd"_ overstates the effectiveness.

---

### 🟡 MEDIUM M3 — No `InternalError` exit code

**File:** `tools/dgrep-cli/src/DgrepCli/Execution/DgrepExitCodes.cs`

The safety net in `RetryPolicy.ExecuteAsync` throws `new InvalidOperationException("Retry loop exited unexpectedly.")`, but there is no exit code defined for unexpected internal errors. When this surfaces to the command handler, it would fall through to a top-level unhandled exception or be caught by the base `Exception` handler (if one exists). Script authors have no way to distinguish a crash from a user error (both produce non-zero exit).

Suggest adding: `public const int InternalError = 4;`

---

### 🟢 LOW L1 — `GetDelay` cap test has a loose upper bound

**File:** `tools/dgrep-cli/tests/DgrepCli.Tests/Execution/RetryTests.cs`

```csharp
Assert.True(delay <= TimeSpan.FromMilliseconds(5000 * 1.25)); // Max with jitter overhead
```

Because jitter is applied _before_ the `Math.Min(delayMs, MaxDelay.TotalMilliseconds)` cap (in `RetryPolicy.GetDelay`), the result is always ≤ MaxDelay exactly — never above 5000ms. The test's upper bound of `5000 * 1.25 = 6250ms` is always trivially true. The test should assert `delay <= TimeSpan.FromSeconds(5)` to match the actual semantic guarantee.

---

### 🟢 LOW L2 — `QueryAuthException(string, Exception)` constructor: verify line wrapping

**File:** `tools/dgrep-cli/src/DgrepCli/Execution/QueryException.cs`

In the diff output, the inner-exception constructor shows a suspicious line break:
```
: base($"Authentication failed. ...", inner
r)
```
This is almost certainly a terminal line-wrapping artifact (the project reports 363 tests passing). No code fix needed, but the reviewer notes it for awareness.

---

## Stage 3 — Test Coverage Assessment

| Area | Coverage | Notes |
|------|----------|-------|
| `RetryPolicy` constructor/validation | ✅ Full | Positive, negative, zero, custom values |
| `RetryPolicy.IsTransient` classification | ✅ Full | All exception types, null, WebException variants, HTTP string messages, inner exceptions |
| `RetryPolicy.GetDelay` math | ✅ Good | Attempt 0, 1, 2, large (cap). Jitter bound is loose (L1) |
| `RetryPolicy.ExecuteAsync` behavior | ✅ Full | Success, retry-then-success, all-fail, non-transient, cancellation, zero-retry, callback params |
| `RetryingQueryExecutor` decorator | ✅ Good | Success, transient retry, rate limit, auth fail, syntax fail, exhausted, cancellation, null guard |
| `DgrepExitCodes` constant values | ⚠️ Trivial | Only tests numeric values, not that they're actually returned |
| Exit code returned from `QueryCommand` | ❌ Missing | No test verifying correct exit code per exception type |
| Retry wired in `Program.cs` | ❌ Missing | No integration test or even unit test confirming the executor chain |

**Total new tests:** 41 (all xUnit `[Fact]`s). Test quality in isolation is high.

---

## Summary Table

| # | Severity | Finding |
|---|----------|---------|
| C1 | 🔴 Critical | `RetryingQueryExecutor` never instantiated — retry is dead code |
| C2 | 🔴 Critical | `DgrepExitCodes` constants never used — all failures return exit code 1 |
| H1 | 🟠 High | `QueryCommand.cs` still uses deprecated `ex.ClusterUrl` |
| H2 | 🟠 High | `QueryRateLimitException` not caught explicitly; returns wrong exit code |
| M1 | 🟡 Medium | HTTP status detection by message string matching — fragile, false-positive risk |
| M2 | 🟡 Medium | Jitter range ±25% narrow; thundering-herd claim overstated |
| M3 | 🟡 Medium | No `InternalError` exit code for unexpected internal failures |
| L1 | 🟢 Low | `GetDelay` cap test asserts ≤ 6250ms but true bound is ≤ 5000ms |
| L2 | 🟢 Low | Line-wrap artifact in `QueryAuthException` diff (likely benign) |

---

## Verdict

**⛔ CHANGES_REQUESTED**

The architecture and component-level code are of high quality. The test suite for the new components is thorough. The critical gap is integration: the PR defines a retry decorator and exit codes but does not wire them into the command execution path. The feature does not deliver its stated purpose — callers see no retry behavior and always receive exit code 1 for all failures.

**Required before merge (Criticals and Highs):**

1. **C1** — Wire `RetryingQueryExecutor` in `Program.cs` for all command handlers that use `KustoQueryExecutor`
2. **C2** — Update catch blocks in `QueryCommand.cs` (and `SavedCommand.cs`) to return `DgrepExitCodes.*` constants
3. **H1** — Update `QueryCommand.cs` catch block to use `ex.EndpointUrl` (or leave `ClusterUrl` as-is — but the inconsistency should be intentional)
4. **H2** — Add explicit catch for `QueryRateLimitException` returning `DgrepExitCodes.TransientFailure`

**Recommended (Mediums):** M1 (message-string detection hardening), M3 (InternalError exit code)

---

*Galadriel — Reviewer, pa-squad*  
*"Even the smallest code ships in darkness will feel the weight of an untested integration path."*

---

## Cycle 2 Re-Review

**Reviewer:** Galadriel  
**Date:** 2026-03-24  
**Fix commit:** `b7eea10` — _"fix: address Galadriel review findings for #108"_  
**Requested by:** Jonathan (via Ralph)

### Finding-by-Finding Verification

| # | Original Severity | Status | Notes |
|---|-------------------|--------|-------|
| C1 | 🔴 Critical | ✅ **Fixed** | `Program.cs` `RunQuery` and `RunSaved` both now wrap with `new RetryingQueryExecutor(new KustoQueryExecutor(authProvider))`. Retry is live. |
| C2 | 🔴 Critical | ✅ **Fixed** | All exception catch blocks in `QueryCommand.Execute` and `SavedCommand.ExecuteRun` now return `DgrepExitCodes.*` constants. No hardcoded `return 1` remains in catch blocks. |
| H1 | 🟠 High | ✅ **Fixed** | `QueryCommand.cs` catch uses `ex.EndpointUrl`. |
| H2 | 🟠 High | ✅ **Fixed** | `QueryRateLimitException` added as an explicit catch before the base `QueryException` catch in **both** `QueryCommand.cs` and `SavedCommand.cs`, returning `DgrepExitCodes.TransientFailure`. Catch order is correct (specific before general). |
| M1 | 🟡 Medium | ✅ **Fixed** | `IsTransientHttpException` now matches `"HTTP 429"` / `"HTTP 503"` / etc. and `message.StartsWith("NNN ")`. False-positive risk from arbitrary number strings is substantially reduced. |
| M2 | 🟡 Medium | ⬜ **Not addressed** | ±25% jitter range unchanged. This was recommended, not required — acceptable. |
| M3 | 🟡 Medium | ✅ **Fixed** | `DgrepExitCodes.InternalError = 4` added. Used in `NotImplementedException` handlers in both commands, and test asserts `Assert.Equal(4, DgrepExitCodes.InternalError)`. |
| L1 | 🟢 Low | ✅ **Fixed** | Cap test now asserts `delay <= TimeSpan.FromSeconds(5)` with explanatory comment. |
| L2 | 🟢 Low | ✅ **N/A** | Was a terminal line-wrap artifact; no code fix needed. |

### Regression Check

No regressions introduced. Confirmed:

- **Validation `return 1` paths** (QueryCommand.cs lines 56, 63, 77, 82; SavedCommand.cs validation branches): These are input-validation user-error paths that pre-date this PR. They return `1` which equals `DgrepExitCodes.UserError`. Numerically correct; the inconsistency with named constants is cosmetic, not behavioral.
- **SavedCommand.cs `catch (ArgumentException)`** (line 163): Pre-existing catch, returns `1` (== `UserError`). Not in scope of C2, numerically correct.
- **Catch order in `QueryCommand.cs`**: `QueryRateLimitException` → `QueryConnectionException` → `QueryAuthException` → `QueryTimeoutException` → `QuerySyntaxException` → `QueryException`. All specific-before-general. Correct.
- **M1 `StartsWith` variants**: `message.StartsWith("429 ")` could theoretically match a non-HTTP message starting with those digits, but this is an edge case far less likely than the original bare `Contains("429")`. The improvement is significant.
- **Tests updated**: `QueryCommandTests.cs` and `SavedCommandTests.cs` now assert named exit code constants instead of hardcoded `1`. `RetryTests.cs` cap assertion tightened. All consistent with the production code changes.

### New Low-Severity Observation (non-blocking)

**NF1 🟢 LOW** — Validation-path `return 1` not converted to `DgrepExitCodes.UserError`. The validation branches in both command files still use the literal `1` rather than the named constant. Since `UserError == 1`, there is zero behavioral impact. Worth a follow-up tidy but does not warrant blocking this PR.

### Verdict

**✅ APPROVE**

All four required findings (C1, C2, H1, H2) are fully and correctly addressed. Both recommended mediums (M1, M3) and the low L1 were also fixed — Gimli went above and beyond the minimum. The retry decorator is now live in the execution pipeline, exit codes are semantically correct across all exception handlers, and the test suite has been updated to match. The one outstanding item (M2 jitter) was optional and the one new observation (NF1) is cosmetic. This branch is ready to merge.

---

*Galadriel — Reviewer, pa-squad*  
*"The work is mended. The light that was hidden now flows through the whole of the pipeline."*
