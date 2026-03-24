# Review — Branch `squad/132-notifications-mvp-scripts`

**Reviewer:** Galadriel  
**Requested by:** Jonathan (via Ralph — retroactive review, protocol violation recovery)  
**Date:** 2025-07-10  
**Branch:** `squad/132-notifications-mvp-scripts`  
**Diff scope:** `git diff main...squad/132-notifications-mvp-scripts -- ':!.squad/'`  
**Files reviewed:** 4 new files, 551 additions  

| File | Lines |
|------|-------|
| `scripts/notify-blocked.ps1` | 122 |
| `scripts/notify-feature-complete.ps1` | 107 |
| `tests/notify-blocked.Tests.ps1` | 176 |
| `tests/notify-feature-complete.Tests.ps1` | 146 |

---

## Stage 1 — Scope & Design Integrity

### Scope check

This PR is exactly what it claims: two MVP caller scripts and their Pester test suites. No undocumented C# work, no infrastructure changes, no phantom files. Scope is clean.

This directly addresses the gap called out in the March 25 Pre-Design Scan: *"The MVP caller scripts don't exist and have no tracked issues."* Both scripts are now present. The integration-first delivery principle (each integration point is a tracked deliverable) is satisfied.

### Design alignment

**`notify-blocked.ps1`** correctly calls `notify.ps1` directly via the call operator (`& $notifyScript @notifyParams`), bypassing the scheduler. This is the right architecture — the urgent tier is designed for immediate delivery, not batching.

**`notify-feature-complete.ps1`** correctly dot-sources `notification-scheduler.ps1` and delegates through `Invoke-ScheduledNotification`. It calls `Initialize-DefaultTriggers` first (idempotent), then fires the `feature-complete` event. The scheduler confirms `feature-complete` is a registered built-in trigger with the `🔵 feature` (batched) tier.

Both scripts follow the "short and composable" directive — thin caller layers that build event payloads and delegate to the established pipeline.

---

## Stage 2 — Line-Level Findings

### Critical — 0 findings

### High — 0 findings

### Medium — 0 findings

### Low

---

#### LOW-1 — Dead test code in `tests/notify-feature-complete.Tests.ps1`

**File:** `tests/notify-feature-complete.Tests.ps1`, lines 17–25  
**Severity:** Low

The `BeforeAll` block defines two helper functions that are never called in any test case:

```powershell
function New-TempWebhookFile { ... }   # line 17
function New-TempStateFile { ... }     # line 23
```

These appear to be scaffolding that was intended for tests that exercise webhook/state isolation, but those tests were never written. The functions create files under `$TestDrive` but no test references them.

**Impact:** Dead code misleads future contributors into thinking test isolation is in place when it is not. If a future contributor relies on this scaffolding as a model, they may misunderstand the test contract.

**Fix:** Remove both functions. If webhook/state isolation tests are planned, file a follow-up issue and add them there.

---

#### LOW-2 — `notify-feature-complete.ps1` exits 0 on `send-error`; design intent undocumented

**File:** `scripts/notify-feature-complete.ps1`, lines 96–100  
**Severity:** Low

When `Invoke-ScheduledNotification` returns `@{ Sent = $false; Reason = "send-error" }` (e.g., Teams webhook unreachable), the script prints a warning and exits 0:

```powershell
} else {
    Write-Warning "Feature-complete notification not sent: $($result.Reason)"
}
# implicit exit 0
```

There is no `exit 1`. This means any calling automation will report success even when the notification pipeline failed silently.

**The design choice is defensible:** notification failure should not block feature completion reporting — coupling the feature completion signal to the notification infrastructure's health would be worse. But the intent should be explicit.

**Fix (choose one):**  
a) Add a comment documenting the deliberate choice: `# Intentional: notification failure is non-blocking; feature completion has already occurred.`  
b) Or add `exit 1` in the else branch if callers need error-level signal (requires team decision).

Note for comparison: `notify-blocked.ps1` correctly uses `exit 1` in its catch block — because a failed blocked-on-human notification IS a critical pipeline failure.

---

#### LOW-3 — Error path not covered in tests (dependency script missing)

**Files:** `tests/notify-blocked.Tests.ps1`, `tests/notify-feature-complete.Tests.ps1`  
**Severity:** Low

Both scripts have a guard at startup that exits 1 if their required dependency isn't found:

```powershell
# notify-blocked.ps1 line 78
if (-not (Test-Path $notifyScript)) {
    Write-Error "notify.ps1 not found at: $notifyScript"
    exit 1
}

# notify-feature-complete.ps1 line 74
if (-not (Test-Path $schedulerScript)) {
    Write-Error "notification-scheduler.ps1 not found at: $schedulerScript"
    exit 1
}
```

Neither test file exercises this path. For an internal integration test suite (no mocks) this is acceptable at MVP, but it means these guard clauses are untested.

**Fix:** Add a test in each suite that temporarily renames/moves the dependency, invokes the caller script, and asserts exit code 1 and error output. Label as a follow-up if it's out of scope for this branch.

---

## Stage 3 — Security & Team Convention Audit

### Security

| Check | Result |
|-------|--------|
| Hardcoded secrets or credentials | ✅ None |
| Hardcoded webhook URLs | ✅ None — resolved from `~/.squad/teams-webhook.url` by `notify.ps1` |
| Hardcoded subscription/workspace GUIDs | ✅ None |
| URL parameters embedded in cards without validation | ⚠️ `BlockerUrl`/`IssuesUrl` accept any string; no `[ValidatePattern]` | 
| Secret exposure in DryRun output | ✅ DryRun prints card JSON only; no credentials in scope |

The URL validation gap (BlockerUrl, IssuesUrl) is informational — for an internal tool used by agents, not web users, injection risk is negligible.

### Team convention compliance

| Convention | Compliant? | Note |
|------------|------------|------|
| `exit 1` (not `return`) on script failure | ✅ | `notify-blocked.ps1` catch uses `exit 1`; `notify-feature-complete.ps1` gap noted in LOW-2 |
| `$ErrorActionPreference = "Stop"` | ✅ | Both scripts |
| `$PSScriptRoot` for relative paths | ✅ | Both scripts |
| `[CmdletBinding()]` + `[Parameter(Mandatory)]` | ✅ | Both scripts |
| `[ValidateSet]` where applicable | ✅ | `Severity` parameter in `notify-blocked.ps1` |
| No mocks — real scripts with DryRun | ✅ | All integration tests use `-DryRun -Force` |
| Scripts short and composable (< ~150 lines) | ✅ | 122 and 107 lines |
| `DryRun` and `Force` switches passed through | ✅ | Both scripts |
| No scope creep into action tier (#117) | ✅ | Only `urgent` and `feature` tiers used |

### `return` vs `exit` — confirmed correct

From team history (2025-07-10 learning): *"`return` exits current scope with exit code 0; `exit 1` is the correct way to signal failure from a .ps1 script invoked from CI."*

`notify-blocked.ps1` uses `exit 1` in its catch block — correct. The scheduler wrapper (`Invoke-ScheduledNotification`) in `notify-feature-complete.ps1` uses `return` internally since it's a function, not a script entry point — also correct.

### Polling loop check

Not applicable — no polling loops in any of the 4 files.

### Syntax validation

All 4 files parse clean via `[System.Management.Automation.Language.Parser]::ParseFile()`. Zero errors.

---

## Test Coverage Summary

| Suite | Describe blocks | Test cases | Modes covered |
|-------|----------------|------------|---------------|
| `notify-blocked.Tests.ps1` | 3 | 11 | Parameter validation, formatting, integration |
| `notify-feature-complete.Tests.ps1` | 3 | 9 | Parameter validation, formatting, integration |

**Covered:**
- All mandatory parameters validated as mandatory via attribute inspection
- `ValidateSet` values for `Severity` enumerated and individually exercised
- Default values (`Severity = "blocking-feature"`, `BlockerLabel = "View Blocker"`) verified
- All optional parameters verified to not crash when omitted
- Card formatting verified for: title, reason body, action button, custom labels
- Scheduler pipeline verified: queue + flush cycle with `Force`
- All three Severity values produce DryRun output without error

**Not covered:**
- Dependency script missing (LOW-3 above)
- `notify-feature-complete.ps1` `send-error` exit code path

---

## Verdict

## ⛔ CHANGES_REQUESTED

The code is well-structured, secure, and follows all team conventions. The notification pipeline wiring is correct and the 20 test cases are substantive (real integration tests with DryRun, no mocks). No critical or high findings.

Two changes are required before this is clean:

1. **Remove the two unused helper functions** from `tests/notify-feature-complete.Tests.ps1` (dead code, LOW-1)
2. **Document the exit-0 design choice** in `notify-feature-complete.ps1`'s else branch with an inline comment (LOW-2) — or file a team decision about whether notification failure should be a script failure

LOW-3 (error path coverage) is acceptable to defer as a follow-up issue.

**Fix author:** Gimli (original author, per Decision #48 — lockout rules removed, authors own their fixes)

---

*Galadriel — Quality Gate*  
*"Even the smallest person can change the course of the future — but not without a proper code review."*

---

## Cycle 2 Re-Review

**Re-reviewer:** Galadriel  
**Date:** 2025-07-10  
**Commit:** f5bb7d327243be1ee06b7bc01211313b4efd933a  

### Findings Verification

| Finding | Status | Evidence |
|---------|--------|----------|
| **L1** — Unused helper functions in test file | ✅ **FIXED** | `New-TempWebhookFile` and `New-TempStateFile` removed from `tests/notify-feature-complete.Tests.ps1` |
| **L2** — Missing comment on exit-0 design choice | ✅ **FIXED** | Inline comments added to `scripts/notify-feature-complete.ps1` else branch explaining deliberate non-blocking behavior |

### Verdict

## ✅ APPROVE

Both low-severity findings have been addressed correctly and completely. The code maintains all prior strengths (architecture, test coverage, security, team convention compliance) while resolving the review concerns. This branch is ready for merge.

**Next steps:** Merge to main and close issue #132.
