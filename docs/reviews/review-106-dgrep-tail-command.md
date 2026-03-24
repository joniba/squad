# Code Review — Issue #106: dgrep tail command with polling loop

**Branch:** `squad/106-dgrep-tail-command`  
**Reviewer:** Galadriel  
**Date:** 2025-07-10  
**Requested by:** Jonathan (via Ralph — retroactive review, protocol violation recovery)  
**Scope:** 4 files, 857 additions, 11 deletions  

---

## Files Reviewed

| File | Change |
|------|--------|
| `tools/dgrep-cli/src/DgrepCli/Commands/TailCommand.cs` | New — 151 lines |
| `tools/dgrep-cli/src/DgrepCli/Execution/MockQueryExecutor.cs` | Modified — +50 lines |
| `tools/dgrep-cli/src/DgrepCli/Program.cs` | Modified — +15/-11 lines |
| `tools/dgrep-cli/tests/DgrepCli.Tests/Commands/TailCommandTests.cs` | New — 641 lines |

---

## Stage 1: Design & Architecture

### Intent

The tail command implements a continuous polling loop that runs a DGrep query on a configurable interval, printing new results as they arrive and sliding the `from` timestamp forward after each poll. Ctrl+C is handled via `CancellationTokenSource` wired to the `Console.CancelKeyPress` event. The command delegates formatting to `FormatterFactory` (consistent with `QueryCommand`) and routes DGrep-specific fields through `QueryOptions.Parameters`.

### Design Observations

**Structural coherence:** The command follows the same constructor-injection pattern as `QueryCommand` — `TextWriter` injection for both stdout and stderr is the right call for testability. The `BuildQueryOptions` method being `internal static` makes it directly unit-testable, which pays off in the test file.

**Separation of concerns:** Status messages go to stderr (non-polluting to piped stdout). This is correct and thoughtful.

**Polling model:** Slide-forward by setting `currentFrom = DateTime.UtcNow` after each poll. Simple and legible. (See Finding M-1 for a timing gap concern.)

**Cancellation model:** Pre-cancel, cancel-during-delay, and cancel-during-query are all handled. Three explicit cancellation catch sites is thorough.

**Executor wiring:** `Program.cs` wires `TailCommand` using `new KustoQueryExecutor(authProvider)`. This follows the pattern of `QueryCommand` and `SavedCommand`. See Finding H-1 — this causes a production crash specific to the tail path.

---

## Stage 2: Implementation & Correctness

### 🔴 HIGH — H-1: Production crash: `KustoQueryExecutor` throws `ArgumentException` for every tail invocation

**File:** `tools/dgrep-cli/src/DgrepCli/Program.cs`, line 97 (new wiring block)  
**Also:** `tools/dgrep-cli/src/DgrepCli/Commands/TailCommand.cs`, `BuildQueryOptions` method  

`KustoQueryExecutor.ExecuteAsync` (line 30 of that file) contains:
```csharp
if (string.IsNullOrWhiteSpace(options.Database))
    throw new ArgumentException("Database must be specified.", nameof(options));
```

`TailCommand.BuildQueryOptions` correctly omits `Database` — DGrep has no database concept; it uses endpoint + namespace + event. But the injected executor requires it. The result: **every real `dgrep tail` invocation crashes with an `ArgumentException` before the first poll.**

This is the known architectural gap (DGrep-via-Kusto-executor, surfaced in decisions.md) made concrete. Other commands (`QueryCommand`, `SavedCommand`) work around it by having the user supply `--database`. The tail command has no `--database` option because DGrep doesn't use one — so there is no workaround for the user.

The comment in `BuildQueryOptions` itself acknowledges the situation: *"The real DgrepQueryExecutor reads these to construct QueryInput."* But no `DgrepQueryExecutor` exists in the codebase today. Until it does, the tail command cannot be exercised in production.

**Required action:** Either (a) add a `DgrepQueryExecutor` stub that reads from `Parameters` and does not require `Database`, wiring tail to it, OR (b) document the limitation explicitly in `Program.cs` with a TODO that prevents silent breakage from looking like user error. The current state is a crash with a confusing `ArgumentException` message that mentions `options` — not helpful to the end user.

---

### 🟡 MEDIUM — M-1: Misplaced `\r` corrupts terminal output

**File:** `tools/dgrep-cli/src/DgrepCli/Commands/TailCommand.cs`, line 88  

```csharp
_stderr.WriteLine($"Watching... (last check: {DateTime.Now:HH:mm:ss}, {totalResults} results so far\r)");
```

The `\r` sits **inside** the interpolated string, immediately before the closing `)`. With `WriteLine`, the emitted bytes are:
```
Watching... (last check: HH:mm:ss, N results so far CR ) LF
```
On a terminal this causes the cursor to jump to column 0 after `so far`, overwriting `W` with `)`. On any piped output (log file, `| tee`, CI stdout), it produces a literal CR in the line. On non-Windows systems, it displays as `^M` at end of line.

The intended overwrite-in-place pattern requires `\r` at the **start** and `Write` (not `WriteLine`):
```csharp
_stderr.Write($"\rWatching... (last check: {DateTime.Now:HH:mm:ss}, {totalResults} results so far  ");
```
Or, if scrolling output is acceptable, simply drop the `\r` entirely.

The test `Execute_EmptyResults_StillShowsStatusLine` passes because `StringWriter` doesn't render `\r` as a terminal overwrite — it stores it literally. The bug is invisible in tests.

**Required action:** Remove the `\r` or reimplement as a proper overwrite pattern.

---

### 🟡 MEDIUM — M-2: Time gap in `currentFrom` update — events can be missed

**File:** `tools/dgrep-cli/src/DgrepCli/Commands/TailCommand.cs`, lines 92–93  

```csharp
result = _executor.ExecuteAsync(opts.Query, queryOptions, ct).GetAwaiter().GetResult();
// ... process result ...
currentFrom = DateTime.UtcNow.ToString("o");  // <-- set AFTER query returns
```

The query was dispatched at time T₀. If any events arrive in the window [T₀, T₀+query_duration], they will have timestamps > T₀ but the next poll's `_from` will be set to T₀+query_duration+process_time. Events at T₀+epsilon could fall in either poll depending on DGrep's closed/open interval semantics, but there's no overlap: a strict `>` on the server side combined with this window means events can be silently dropped.

The robust pattern is to snapshot the time **before** the query is sent:
```csharp
var nextFrom = DateTime.UtcNow; // snapshot before dispatch
result = _executor.ExecuteAsync(...).GetAwaiter().GetResult();
// ...
currentFrom = nextFrom.ToString("o"); // use pre-query snapshot
```

**Required action:** Snapshot `DateTime.UtcNow` before `ExecuteAsync` call.

---

### 🟡 MEDIUM — M-3: `opts.To` is silently ignored

**File:** `tools/dgrep-cli/src/DgrepCli/Commands/TailCommand.cs`, `BuildQueryOptions` method  

`TailOptions` inherits `To` from its base class (or carries it as a field). It is present in `DefaultOpts()`'s comment in tests (`To = "now"`). But `BuildQueryOptions` hardcodes `["_to"] = "now"` and never reads `opts.To`.

If a user passes `--to 2024-01-01T12:00:00Z` to bound the tail session, it is silently discarded. The user sees no error, the parameter has no effect, and the command continues to poll indefinitely against `to=now`.

**Required action:** Either read `opts.To` (for the initial poll) or, if tail always uses `now`, remove `To` from `TailOptions` and document the behavior. Silent ignore is the worst outcome.

---

### 🟢 LOW — L-1: Indentation inconsistencies

**File:** `tools/dgrep-cli/src/DgrepCli/Commands/TailCommand.cs`, lines 87, 89, 126  

Three locations use 3-space or 7-space indentation while the surrounding code uses 4/8. Not breaking, but inconsistent with the project's style:

```csharp
               formatter.Format(result, _stdout);   // 15 spaces; should be 24
           }                                          // 11 spaces; should be 20
                   ["_to"] = "now",                  // 19 spaces; should be 20
```

`MockQueryExecutor.cs` line 116 has a pre-existing 10-space indentation on `LastQuery = query;` that was not fixed in this PR.

---

### 🟢 LOW — L-2: "1 results so far" — grammar error in status line

**File:** `tools/dgrep-cli/src/DgrepCli/Commands/TailCommand.cs`, line 88  

The status template always pluralizes: `"... {totalResults} results so far"`. When `totalResults == 1`, this prints "1 results so far." The fix is trivial:
```csharp
var resultWord = totalResults == 1 ? "result" : "results";
_stderr.WriteLine($"Watching... (last check: {DateTime.Now:HH:mm:ss}, {totalResults} {resultWord} so far)");
```

---

## Stage 3: Test Coverage

### Assessment

641 lines of tests for 151 lines of production code is a healthy ratio. The suite is structured well with clearly labelled regions. Coverage of the following scenarios is verified:

| Scenario | Covered |
|----------|---------|
| Single poll then cancel | ✅ |
| Pre-cancel (immediate) | ✅ |
| Cancel during delay | ✅ |
| Cancel during executor | ✅ |
| Empty results | ✅ |
| Multiple polls — from-time advancement | ✅ |
| Multiple polls — result accumulation | ✅ |
| Table / JSON / CSV output | ✅ |
| Invalid output format | ✅ |
| `QueryException` → exit 1 | ✅ |
| `QueryAuthException` → exit 1 + hint | ✅ |
| Null opts → `ArgumentNullException` | ✅ |
| `BuildQueryOptions` field mapping | ✅ |
| `BuildQueryOptions` optional version | ✅ |
| Identity and version parameters | ✅ |
| `MockQueryExecutor.WithResults` sequential | ✅ |
| `MockQueryExecutor.WithResultFactory` | ✅ |
| `MockQueryExecutor.AllQueries` tracking | ✅ |

### Missing coverage

**T-1: Unexpected exception propagation** — The inner catch handles `OperationCanceledException`, `QueryAuthException`, and `QueryException`. An unchecked exception (e.g., `InvalidOperationException`, `SocketException`) propagates and crashes the process with no cleanup output. A test that injects such an exception and verifies it propagates (rather than silently returns) would document the intended behavior.

**T-2: H-1 would never be caught by tests** — Because all tests use `MockQueryExecutor`, which has no `Database` requirement, the crash in Finding H-1 is completely invisible to the test suite. This is structurally expected for a mock but underscores that integration-level validation is needed once `DgrepQueryExecutor` exists.

### Timing test concern (informational)

`Execute_CustomInterval_RespectedBetweenPolls` uses wall-clock assertions with a 500ms lower bound on a 1-second interval. This is generally reliable but can be flaky under heavy CI load. The upper bound is unconstrained (3s tolerance comment), which is wise. Consider annotating with `[Trait("Category", "Timing")]` if the project has a mechanism to skip such tests.

---

## Summary of Findings

| ID | Severity | Description |
|----|----------|-------------|
| H-1 | **HIGH** | Production crash: `KustoQueryExecutor` throws `ArgumentException` (no Database) for every `dgrep tail` run |
| M-1 | Medium | `\r` inside `WriteLine` string corrupts terminal and piped output |
| M-2 | Medium | `currentFrom` set after poll — events in query-execution window can be missed |
| M-3 | Medium | `opts.To` silently ignored; user input has no effect |
| L-1 | Low | Indentation inconsistencies (3 sites) |
| L-2 | Low | "1 results so far" grammar error |
| T-1 | Low | No test for unexpected exception propagation path |

---

## Verdict

## ❌ CHANGES_REQUESTED

Two blockers prevent merge:

1. **H-1 (production crash):** Every real invocation of `dgrep tail` crashes before the first poll. Even if this is a known SDK-gap, the current state delivers a confusing `ArgumentException` with no user-readable explanation. Needs either a working stub path or an explicit, user-friendly error message explaining the limitation.

2. **M-1 (corrupted output):** The `\r` inside `WriteLine` is a visible bug on any real terminal or CI log. It's a one-line fix.

M-2 and M-3 should also be addressed in this PR — they are correctness and UX issues, not polish. L-1, L-2, and T-1 can be addressed in follow-up at the author's discretion.

**Author:** Fix H-1 and M-1 as priority, address M-2 and M-3, then reply with the fix SHA.

---

## Cycle 2 Re-Review

**Reviewer:** Galadriel  
**Date:** 2025-07-11  
**Fix commit:** `f59d9fe` — *"fix: address Galadriel review findings for #106"*  
**Requested by:** Jonathan (via Ralph)

---

### Finding-by-Finding Verification

#### H-1 — Production crash on every `dgrep tail` invocation

**Status: ✅ RESOLVED**

`Program.cs` `RunTail()` no longer instantiates `KustoQueryExecutor` or calls `TailCommand.Execute()`. Instead it returns immediately with three clear, user-facing lines on stderr:

```
Error: 'dgrep tail' is not yet available in this build.
The DGrep SDK executor required for real-time polling has not been implemented.
Use 'dgrep search' for one-shot DGrep queries in the meantime.
```

A `// TODO (#106)` comment documents exactly why (Database requirement mismatch) and what the forward path is. This is option (b) from my Cycle 1 required actions — explicit, user-readable gating — and it is correctly implemented. No crash; no confusing `ArgumentException`.

---

#### M-1 — Misplaced `\r` corrupts terminal output

**Status: ✅ RESOLVED (pre-commit)**

The fix commit's diff shows the `_stderr.WriteLine` status line as an unchanged context line, not a `-`/`+` pair. To understand why, I ran a byte-level hex dump of that line against **both** the original feature commit (`ffff4ec`) and the fix commit (`f59d9fe`). Both show identical bytes ending in:

```
20 73 6F 20 66 61 72 29 22 29 3B   →   " so far)");
```

No `0D` (CR) byte and no `\r` escape sequence (`5C 72`) is present in either commit. The `\r` was not in the committed code at any point — my Cycle 1 finding was apparently based on an intermediate working-copy state that predated the feature commit. The current state of the file is correct. No action needed; finding closed.

---

#### M-2 — Time gap: `currentFrom` set after poll, events can be missed

**Status: ✅ RESOLVED**

The fix introduces `var nextFrom = DateTime.UtcNow;` immediately before the `try { ExecuteAsync(...) }` block and uses `currentFrom = nextFrom.ToString("o");` after result processing. This is precisely the pre-dispatch snapshot pattern I prescribed. The existing test `Execute_MultiplePollCycles_UpdatesFromTime` exercises the from-time advancement path and continues to pass.

---

#### M-3 — `opts.To` silently ignored

**Status: ✅ RESOLVED**

`BuildQueryOptions` now reads:

```csharp
["_to"] = !string.IsNullOrEmpty(opts.To) ? opts.To : "now",
```

When the user supplies `--to`, it is passed through to the query parameter. When absent, it defaults to `"now"`. This is the correct fix. The existing test `BuildQueryOptions_MapsAllFields` asserts `_to == "now"` using a `DefaultOpts()` where `To = "now"`, which passes cleanly. 

**Minor observation (L-level, non-blocking):** No test was added for the non-default case (`opts.To = "2024-01-01T00:00:00Z"` → `_to = "2024-01-01T00:00:00Z"`). The fix logic is obviously correct, but a test would lock the behavior against future regressions. Can be addressed in follow-up.

---

### Regression Check

| Area | Concern | Result |
|------|---------|--------|
| H-1 gate removes CancellationTokenSource setup | Nothing to cancel; early return is correct | ✅ No regression |
| `nextFrom` declared before try-block | Still in scope after catch blocks; `currentFrom` unchanged on error paths (error returns before reaching it) | ✅ No regression |
| M-3: `_to` now conditional | When `opts.To = "now"` (existing tests), behaviour is unchanged | ✅ No regression |
| Test suite unchanged | No tests removed or disabled; 641-line suite still passes | ✅ No regression |

No regressions introduced by the fix commit.

---

### Carry-over Low Findings (Deferred per Cycle 1)

L-1 (indentation), L-2 ("1 results" grammar), and T-1 (unexpected-exception test) remain open. Per Cycle 1 verdict these were explicitly deferred to a follow-up at the author's discretion. No change in assessment.

---

## ✅ APPROVED

All four required findings (H-1, M-1, M-2, M-3) are resolved. No regressions detected. The one new observation (missing test for custom `opts.To` passthrough) is L-level and non-blocking. This branch is clear to merge.
