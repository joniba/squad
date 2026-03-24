# Review: squad/134-notifications-coordinator-wiring

**Reviewer:** Galadriel  
**Requested by:** Jonathan (via Ralph — retroactive, branch closed without review)  
**Branch:** `squad/134-notifications-coordinator-wiring`  
**Date:** 2026-03-26  
**Files reviewed:** 6 total (2 production, 4 `.squad/`)  
**Diff size:** 553 additions, 11 deletions

---

## Scope

This branch delivers:

1. **`scripts/notify-squad-event.ps1`** — A new dispatcher that sits between the coordinator (AI agent) and the two MVP caller scripts (`notify-feature-complete.ps1`, `notify-blocked.ps1`). It translates from event-friendly parameters to each caller's native signature.
2. **`tests/notify-squad-event.Tests.ps1`** — 13 Pester integration tests for the dispatcher.
3. **`.squad/failure-recovery.md`** — Updated to document the new dispatch protocol for agents.
4. **`.squad/agents/gimli/history.md`** and **`.squad/agents/bilbo/history.md`** — Manifest-appended histories.
5. **`.squad/temp-commit-msg.txt`** — Scratch file, should not be here (see Finding L-1).

The branch depends on `squad/132-notifications-mvp-scripts` (which delivers `notify-feature-complete.ps1` and `notify-blocked.ps1`). Branch 132 exists but is not yet merged to main. The dispatcher correctly guards against the missing downstream scripts at runtime with a descriptive error.

---

## Stage 1 — Design Coherence

**Does the dispatcher pattern make sense?**  
Yes. Using a single entry-point dispatcher is the right abstraction layer. It means agents and the coordinator call one stable surface (`notify-squad-event.ps1 -Event "..."`) rather than remembering two different script signatures. If caller scripts ever change parameters, only the dispatcher needs updating. This directly addresses the gap Gandalf identified: "built but never wired."

**Does it match the failure-recovery.md intent?**  
Yes. The `failure-recovery.md` update correctly replaces the old "post to Teams webhook directly" instruction with a typed dispatcher call. Both the Elrond failure path and the general escalation path are updated consistently.

**Does it wire the coordinator?**  
The coordinator in this project is an AI agent, not a `coordinator.ps1` script. The "wiring" in this context means updating the team's protocol documents so the coordinator AI knows to use `notify-squad-event.ps1`. The `failure-recovery.md` change IS that wiring. This is understood and acceptable — the design is correct for the project structure.

---

## Stage 2 — Code Review

### Finding H-1 — Exit code from downstream scripts is not propagated (High)

**File:** `scripts/notify-squad-event.ps1`  
**Lines:** 155 (`& $callerScript @params` in `feature-complete` branch) and 190 (`& $callerScript @params` in `blocked` branch)

```powershell
Write-Host "📢 Dispatching feature-complete → notify-feature-complete.ps1"
& $callerScript @params
# ← nothing checks $LASTEXITCODE here
```

`$ErrorActionPreference = "Stop"` (line 91) does **not** convert a child process's non-zero exit code into a terminating error. It only converts PowerShell non-terminating errors (via `Write-Error`). If `notify-feature-complete.ps1` or `notify-blocked.ps1` exits with code 1 — a Teams delivery failure, a missing webhook URL, a malformed card — the dispatcher exits 0. Any caller (coordinator, CI step, a future orchestration script) will see success and log nothing.

**Required fix:**

```powershell
& $callerScript @params
if ($LASTEXITCODE -ne 0) {
    Write-Error "Downstream script '$callerScript' failed with exit code $LASTEXITCODE."
    exit $LASTEXITCODE
}
```

Apply the same fix to both switch arms (`feature-complete` and `blocked`).

---

### Finding M-1 — DocLinks truncation is silent (Medium)

**File:** `scripts/notify-squad-event.ps1`  
**Lines:** 134–136

```powershell
if ($DocLinks) {
    $params.IssuesUrl = ($DocLinks -split ',')[0].Trim()
}
```

The parameter `-DocLinks` accepts a comma-separated list (matching the `-PRs` convention), but only the **first** URL is forwarded to `notify-feature-complete.ps1`'s `$IssuesUrl`. The rest are silently discarded. A caller who passes `-DocLinks "https://a.md,https://b.md"` will see only `a.md` delivered — no warning, no error.

`notify-feature-complete.ps1` accepts only a single `$IssuesUrl` (a design limitation of the caller), so the dispatcher cannot forward all links. But the silent drop is a trap.

**Required fix (pick one):**

Option A — Warn on truncation:
```powershell
$links = ($DocLinks -split ',') | ForEach-Object { $_.Trim() }
$params.IssuesUrl = $links[0]
if ($links.Count -gt 1) {
    Write-Warning "notify-squad-event: only the first DocLink is forwarded ($($links[0])). $($links.Count - 1) link(s) dropped."
}
```

Option B — Document the single-link limit in the `.PARAMETER DocLinks` block.

I prefer Option A — a runtime warning is harder to miss than a comment.

---

### Finding L-1 — Temp file committed (Low)

**File:** `.squad/temp-commit-msg.txt`

A scratch commit-message draft was committed to the branch. It has no operational value and should not be tracked.

**Required fix:** Delete the file in a follow-up commit on this branch before merge.

```powershell
git rm .squad/temp-commit-msg.txt
git commit -m "chore: remove temp commit message artifact"
```

---

### Finding L-2 — Slug can produce consecutive hyphens (Low)

**File:** `scripts/notify-squad-event.ps1`  
**Line:** 127

```powershell
$slug = ($FeatureName -replace '[^a-zA-Z0-9\-]', '-').ToLower().TrimEnd('-')
```

Input `"Auth  Flow"` (double space) produces `"auth--flow"`. Input `"DGrep CLI: Phase 1!"` produces `"dgrep-cli--phase-1-"` (the colon and space become `--`, and the trailing `!` becomes a trailing `-` which is then trimmed — but the internal `--` is not). Not broken, but aesthetically inconsistent.

**Suggested fix:**

```powershell
$slug = ($FeatureName -replace '[^a-zA-Z0-9]', '-') -replace '-{2,}', '-'
$slug = $slug.ToLower().Trim('-')
```

Not blocking merge but worth addressing.

---

### Finding L-3 — `BlockerLabel` not exposed (Low)

**File:** `scripts/notify-squad-event.ps1` (design gap)

`notify-blocked.ps1` accepts a `-BlockerLabel` parameter (the Teams button label, defaults to `"View Blocker"`). The dispatcher has no `-Label` or `-BlockerLabel` pass-through. Callers using the dispatcher cannot customize the button label. Not critical (the default is sensible), but worth noting for completeness.

**Recommendation:** Add `-Label [string]` to the dispatcher's `blocked` parameter set and forward it as `$params.BlockerLabel = $Label` when non-empty. If intentionally omitted, document it.

---

## Stage 3 — Test Review

The test file is well-structured. The mock-via-copy-to-TestDrive pattern (copying the dispatcher alongside a mock downstream script) correctly exercises `$PSScriptRoot` resolution. The use of JSON capture files for param assertion is sound.

**Strengths:**
- 13 tests covering validation, routing, slug generation, PR normalization, and error cases.
- Separate `BeforeAll` isolation per context block.
- Both `DryRun` and `Force` flags verified for `feature-complete`.
- Missing downstream script error cases covered.

**Gaps (not blocking, but noted):**

| Gap | Impact |
|-----|--------|
| No test for `$LASTEXITCODE` propagation (H-1) | High finding has no test — the bug is invisible until the downstream fails in production |
| No test for DocLinks with multiple URLs (M-1) | The truncation behavior is unverified |
| No test for `TestInstructions` forwarding | Parameter is plumbed but not asserted |
| No test for DryRun/Force flags in `blocked` path | Covered for `feature-complete` only |
| Slug consecutive-hyphen case not tested | `"Auth  Flow"` slug not asserted |

A test for H-1 should be added as part of the H-1 fix:

```powershell
It "propagates non-zero exit code from downstream" {
    # Create a mock that exits 1
    $failCaller = "param([string]$FeatureTitle, [string]$Summary, [string]$FeatureId, [switch]$DryRun) exit 1"
    Set-Content (Join-Path $mockDir "notify-feature-complete.ps1") $failCaller
    Copy-Item $dispatcherPath (Join-Path $mockDir "notify-squad-event.ps1")
    { & $mockDispatcher -Event "feature-complete" -FeatureName "X" -Summary "Y" -DryRun } |
        Should -Throw
}
```

---

## Summary of Findings

| ID | Severity | File | Finding |
|----|----------|------|---------|
| H-1 | **High** | `scripts/notify-squad-event.ps1:155,190` | Downstream exit code not checked — silent failure on notification errors |
| M-1 | **Medium** | `scripts/notify-squad-event.ps1:134–136` | Multiple DocLinks silently truncated to first; no warning |
| L-1 | **Low** | `.squad/temp-commit-msg.txt` | Scratch file committed; delete before merge |
| L-2 | **Low** | `scripts/notify-squad-event.ps1:127` | Slug allows consecutive hyphens |
| L-3 | **Low** | Design gap | `BlockerLabel` not exposed through dispatcher |
| — | Note | `tests/notify-squad-event.Tests.ps1` | Add test for H-1 failure propagation and M-1 truncation |

---

## Verdict

### ❌ CHANGES_REQUESTED

H-1 is a correctness defect: the dispatcher is the coordinator's only visibility into whether a notification succeeded. A silent exit-0 on downstream failure breaks the entire signal chain. The fix is 4 lines, but it must be there before merge.

M-1 and L-1 are required for merge. L-2 and L-3 can follow in a cleanup issue.

**Merge-blocking fixes required from Gimli (original author):**

1. **H-1:** After each `& $callerScript @params`, check `$LASTEXITCODE` and propagate failure.
2. **M-1:** Warn (or document clearly) when DocLinks are truncated.
3. **L-1:** Delete `.squad/temp-commit-msg.txt`.
4. **Test for H-1:** Add a test that verifies a failing downstream causes the dispatcher to throw.

Reply with the fix commit SHA when complete.

---

*Galadriel — Reviewer*  
*"The world is changed. I feel it in the code. I sense it in the tests. Much that once was documented is lost, for none now live who remember it."*

---

## Cycle 2 Re-Review

**Reviewer:** Galadriel  
**Requested by:** Jonathan (via Ralph — re-review after fixes)  
**Date:** 2026-03-27  
**Fix commit:** `3b75209` — "fix: address Galadriel review findings for #134"  
**Diff reviewed:** `git show squad/134-notifications-coordinator-wiring -- ':!.squad/'`

---

### H-1 — Exit code propagation ✅ RESOLVED

Both switch arms (`feature-complete` and `blocked`) now carry:

```powershell
& $callerScript @params
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
```

This is exactly what was required. The signal chain from downstream script to coordinator is now intact: a Teams delivery failure will surface as a non-zero exit from the dispatcher.

---

### M-1 — Multiple DocLinks ✅ RESOLVED

Gimli chose **Option A** (warn on truncation):

```powershell
$links = @(($DocLinks -split ',') | ForEach-Object { $_.Trim() } | Where-Object { $_ })
if ($links.Count -gt 1) {
    Write-Warning "-DocLinks received $($links.Count) values; only the first will be forwarded: $($links[0])"
}
$params.IssuesUrl = $links[0]
```

A bonus robustness improvement: the `Where-Object { $_ }` filter strips empty entries that would result from trailing commas (e.g., `"url1,url2,"`). The warning message names both the count and the forwarded URL — exactly the right runtime signal. Well done.

---

### L-1 — Temp file removed ✅ RESOLVED

`git ls-tree -r squad/134-notifications-coordinator-wiring --name-only` returns no match for `temp-commit-msg`. The file is gone from the branch tree.

---

### Test for H-1 exit-code propagation ✅ ADDED

A new `Context "exit-code propagation"` block is present in `tests/notify-squad-event.Tests.ps1`. The mock downstream script exits with code `2` unconditionally; the test then asserts:

```powershell
& $script:exitCodeDispatcher -Event "feature-complete" -FeatureName "X" -Summary "Y" -DryRun
$LASTEXITCODE | Should -Be 2
```

This is structurally sound. In PowerShell, `exit N` inside a `.ps1` script called with `&` exits the *script* (not the host process) and sets `$LASTEXITCODE` in the calling scope, so Pester continues normally and the assertion runs correctly.

---

### Remaining low-severity findings (L-2, L-3)

These were marked non-blocking in Cycle 1 and were not required for this fix cycle. They remain open:

| ID | Status | Note |
|----|--------|------|
| L-2 | Open | Slug consecutive-hyphen edge case — carry to cleanup issue |
| L-3 | Open | `BlockerLabel` not exposed through dispatcher — carry to cleanup issue |

Both are acceptable to defer. They do not affect correctness of the dispatcher or signal chain.

---

### Verdict

#### ✅ APPROVE

All four merge-blocking items from Cycle 1 are resolved:

1. **H-1** — `$LASTEXITCODE` checked and propagated in both dispatch arms. ✅  
2. **M-1** — Multiple DocLinks warned at runtime; first link forwarded. ✅  
3. **L-1** — Temp scratch file deleted from branch. ✅  
4. **Test** — Exit-code propagation test added with correct structure. ✅  

The dispatcher is ready to merge. Open a follow-up issue for L-2 and L-3 before this branch's pattern is replicated elsewhere.

---

*Galadriel — Reviewer, Cycle 2*  
*"Even the smallest fix can change the course of the future."*
