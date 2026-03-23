# Notification MVP Plan — Feature Complete + Blocked on Human

> **Status:** Approved for implementation  
> **Author:** Gandalf (Lead)  
> **Requested by:** Jonathan  
> **Date:** 2026-03-25

## Scope

Two notification triggers, additive only. The old system (`send-teams-notification.ps1` called by `icm-scan.ps1` and `squad-daily-summary.ps1`) stays untouched. These new triggers call `notify.ps1` exclusively.

| Trigger | Tier | Delivery | When |
|---------|------|----------|------|
| 🔵 Feature Complete | `feature` (batched) | Hourly batch or ≥5 items | Multi-PR feature track finishes |
| 🔴 Blocked on Human | `urgent` (immediate) | Instant | Work blocked waiting for Jonathan |

---

## Trigger 1: 🔵 Feature Complete

### What fires it

When a Galadriel PR review results in APPROVE and merge for the **last PR in a feature track**, the coordinator (or the script calling Galadriel) fires a feature-complete notification. A "feature track" is any set of PRs linked to the same parent GitHub issue.

### Integration point

**New script:** `scripts/notify-feature-complete.ps1`

This is a standalone caller script. It is invoked by the coordinator after Galadriel approves the final PR in a feature track, or manually by any agent/human who knows a feature shipped.

The coordinator's spawn prompt for Galadriel already includes post-review instructions. After a PR is approved and merged, the coordinator calls this script. The scheduler can also invoke it as a post-task hook.

**Who calls it:**
- The coordinator (squad.agent.md), after Galadriel's `task` tool returns with an APPROVE verdict and the PR is merged
- Any agent that completes a multi-step feature (e.g., Gimli finishing the last issue in a track)
- Jonathan manually, to test

### Event payload

```powershell
# scripts/notify-feature-complete.ps1
param(
    [Parameter(Mandatory)][string]$FeatureId,       # e.g., "dgrep-cli-phase1" or issue number "101"
    [Parameter(Mandatory)][string]$FeatureTitle,     # e.g., "DGrep CLI — Phase 1 Foundation"
    [Parameter(Mandatory)][string]$Summary,           # What shipped (markdown OK)
    [string]$TestInstructions,                        # How Jonathan can verify
    [string]$IssuesUrl,                               # Link to issues/PRs
    [string]$PRList,                                  # Comma-separated PR numbers
    [switch]$DryRun,
    [switch]$Force
)
```

The script dot-sources `notification-scheduler.ps1` and calls:

```powershell
. "$PSScriptRoot\notification-scheduler.ps1"
Initialize-DefaultTriggers

Invoke-ScheduledNotification -EventName "feature-complete" -EventData @{
    featureId        = $FeatureId
    featureTitle     = $FeatureTitle
    summary          = $Summary
    testInstructions = $TestInstructions
    issuesUrl        = $IssuesUrl
    nextAction       = if ($PRList) { "PRs: $PRList" } else { $null }
} -DryRun:$DryRun -Force:$Force
```

This flows through:
1. `notification-scheduler.ps1` → `Build-EventFromTemplate` with template `"feature-complete"` (line 311)
2. → `notify.ps1 -Type feature` → `Add-ToFeatureQueue` → batch or flush
3. → `Build-BatchFeatureCard` or `Build-FeatureCard` → Teams Adaptive Card

### Teams card output

```
🔵 Feature Summary (1 item)
━━━━━━━━━━━━━━━━━━━━━━━━━━
🔵 DGrep CLI — Phase 1 Foundation

CLI arg parsing, config management, and output formatters
shipped. Solution builds clean, 8 tests passing.

Testing Instructions:
  cd tools/dgrep-cli
  dotnet build
  dotnet test
  .\src\DgrepCli\bin\Debug\net472\dgrep.exe --help

[View Issues]
```

### Where the coordinator calls it

In the coordinator's spawn flow (`.squad/templates/squad.agent.md`), after a Galadriel review task returns successfully with an APPROVE verdict:

```
# Coordinator pseudo-logic (in the spawn prompt, not a script):
#
# 1. Spawn Galadriel to review PR
# 2. Galadriel returns: { verdict: "APPROVE", prNumber: 42, prTitle: "..." }
# 3. If this PR closes a parent issue (last PR in the track):
#      Run: scripts/notify-feature-complete.ps1 \
#           -FeatureId "issue-42" \
#           -FeatureTitle "Auth Flow Implementation" \
#           -Summary "Token caching and refresh implemented." \
#           -TestInstructions "Run login, verify cache persists across restarts" \
#           -IssuesUrl "https://github.com/jbenami_microsoft/ms-pa/issues/42" \
#           -PRList "#55, #56, #57"
```

The coordinator determines "last PR in track" by checking if the parent issue has any remaining open linked PRs. This is a judgment call the coordinator makes — not automated.

---

## Trigger 2: 🔴 Blocked on Human

### What fires it

When work is blocked waiting for Jonathan. This fires in three scenarios:

| Scenario | Source | Signal |
|----------|--------|--------|
| A. Elrond can't find a solution | Failure recovery pipeline | Elrond writes "No solution found — escalating to Jonathan" |
| B. Gandalf rejects Elrond's solution 2x | Failure recovery pipeline | Gandalf's second rejection |
| C. Fix implemented but retry still fails | Failure recovery pipeline | Original agent's retry fails |
| D. Aragorn fails during active livesite | Livesite exception | Aragorn signals immediately (bypasses pipeline) |
| E. Any agent needs human input/decision | Ad-hoc | Agent explicitly requests Jonathan's input |

### Integration point

**New script:** `scripts/notify-blocked.ps1`

Standalone caller script. Invoked by the coordinator when any of the above scenarios are detected.

**Who calls it:**
- The coordinator, when Gandalf's failure pipeline reaches an escalation trigger
- The coordinator, when any agent's `task` tool returns with a "blocked on human" signal
- Aragorn directly, during livesite incidents (immediate bypass)
- Any agent that needs a decision from Jonathan

### Event payload

```powershell
# scripts/notify-blocked.ps1
param(
    [Parameter(Mandatory)][string]$Title,            # e.g., "Blocked: DGrep auth requires Geneva VPN access"
    [Parameter(Mandatory)][string]$Reason,            # Why it needs Jonathan
    [Parameter(Mandatory)][string]$ActionNeeded,      # What Jonathan should do
    [string]$BlockerUrl,                              # Link to issue, PR, config, etc.
    [string]$BlockerLabel = "View Blocker",           # Button label
    [string]$Agent,                                   # Which agent is blocked
    [string]$Severity = "blocking-feature",           # blocking-feature | livesite | decision-needed
    [switch]$DryRun,
    [switch]$Force
)
```

The script calls `notify.ps1` directly (no scheduler indirection needed — urgent tier is always immediate):

```powershell
$notifyScript = Join-Path $PSScriptRoot "notify.ps1"

$event = @{
    eventId     = "blocked:$($Agent ?? 'unknown'):$(Get-Date -Format 'yyyy-MM-ddTHH-mm-ss')"
    errorType   = "needs-human"
    title       = "🔴 $Title"
    reason      = @"
**Why:** $Reason

**Action needed:** $ActionNeeded

**Agent:** $($Agent ?? 'unknown')
**Severity:** $Severity
"@
    actionUrl   = $BlockerUrl
    actionLabel = $BlockerLabel
}

& $notifyScript -Type "urgent" -Event $event -DryRun:$DryRun -Force:$Force
```

This flows through:
1. `notify.ps1 -Type urgent` → dedup check → `Build-UrgentCard`
2. → immediate `Send-TeamsWebhook` delivery
3. → exponential backoff retry on failure

### Teams card output

```
🔴 Blocked: DGrep auth requires Geneva VPN access
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Why: SDK's dSTS auth requires VPN tunnel to
corp network. Cannot acquire token from this machine.

Action needed: Confirm VPN access is available for the
build machine, or approve fallback to interactive auth.

Agent: Gimli
Severity: blocking-feature

[View Blocker]
```

### Where the coordinator calls it

**Scenario A — Elrond exhausts research:**
```
# Coordinator detects Elrond's task returned with "No solution found"
# in the output. Coordinator runs:
scripts/notify-blocked.ps1 \
    -Title "Research exhausted: $failureSlug" \
    -Reason "Elrond investigated and found no viable solution." \
    -ActionNeeded "Review Elrond's research at docs/research/$slug.md and provide direction." \
    -BlockerUrl "https://github.com/jbenami_microsoft/ms-pa/issues/$issueNum" \
    -Agent "Elrond" \
    -Severity "blocking-feature"
```

**Scenario D — Aragorn livesite bypass:**
```
# Coordinator detects Aragorn failed during active incident.
# Immediate notification — no pipeline:
scripts/notify-blocked.ps1 \
    -Title "LIVESITE: Aragorn investigation blocked" \
    -Reason "Active customer-impacting incident. Aragorn cannot proceed: $error" \
    -ActionNeeded "Take over incident investigation or provide missing access." \
    -BlockerUrl "$icmIncidentUrl" \
    -BlockerLabel "View Incident" \
    -Agent "Aragorn" \
    -Severity "livesite"
```

**Scenario E — Agent needs a decision:**
```
# Any agent returns "I need Jonathan's input on X"
scripts/notify-blocked.ps1 \
    -Title "Decision needed: $topic" \
    -Reason "$agentName needs your input: $question" \
    -ActionNeeded "$specificAction" \
    -BlockerUrl "$relevantUrl" \
    -Agent "$agentName" \
    -Severity "decision-needed"
```

---

## What NOT to touch

| Script | Current callers | Status |
|--------|----------------|--------|
| `scripts/send-teams-notification.ps1` | `icm-scan.ps1` (line 286), `squad-daily-summary.ps1` (line 56) | ❌ Do not modify |
| `scripts/icm-scan.ps1` | `squad-scheduler.ps1` | ❌ Do not modify |
| `scripts/squad-daily-summary.ps1` | `squad-scheduler.ps1` | ❌ Do not modify |

These continue using the old `send-teams-notification.ps1` path. Migration happens later, after the MVP proves out.

---

## Dry-run testing

Jonathan can test both triggers without sending real notifications:

### Test Feature Complete

```powershell
.\scripts\notify-feature-complete.ps1 `
    -FeatureId "test-feature-1" `
    -FeatureTitle "Test Feature — Notification MVP" `
    -Summary "Two new notification triggers: feature-complete and blocked-on-human." `
    -TestInstructions "1. Run this dry-run command`n2. Check the JSON output`n3. Verify card structure" `
    -IssuesUrl "https://github.com/jbenami_microsoft/ms-pa/issues" `
    -PRList "#122, #125, #127" `
    -DryRun -Force
```

Expected output: JSON Adaptive Card with 🔵 header, summary, test instructions, and "View Issues" button.

### Test Blocked on Human

```powershell
.\scripts\notify-blocked.ps1 `
    -Title "Test blocker — ignore this" `
    -Reason "Testing the blocked-on-human notification pipeline." `
    -ActionNeeded "No action needed — this is a dry run." `
    -BlockerUrl "https://github.com/jbenami_microsoft/ms-pa/issues" `
    -BlockerLabel "View Test Issue" `
    -Agent "Gandalf" `
    -Severity "decision-needed" `
    -DryRun -Force
```

Expected output: JSON Adaptive Card with 🔴 header, reason, action needed, and "View Test Issue" button.

### Send a real notification (after verifying dry-run)

Remove `-DryRun` from either command above. Requires `~/.squad/teams-webhook.url` to contain a valid Teams webhook URL (already configured).

---

## Implementation issues for Gimli

### Issue 1: Create `notify-feature-complete.ps1`

**Title:** `feat(notifications): add notify-feature-complete.ps1 caller script`  
**Labels:** `squad`, `squad:gimli`  
**Body:**
> Create `scripts/notify-feature-complete.ps1` that accepts feature metadata and calls `notify.ps1` via the notification-scheduler.
>
> **Parameters:** FeatureId (mandatory), FeatureTitle (mandatory), Summary (mandatory), TestInstructions, IssuesUrl, PRList, DryRun, Force
>
> **Behavior:**
> 1. Dot-source `notification-scheduler.ps1`
> 2. Call `Initialize-DefaultTriggers` (idempotent)
> 3. Call `Invoke-ScheduledNotification -EventName "feature-complete"` with mapped EventData
> 4. Support `-DryRun` and `-Force` passthrough
>
> **Acceptance criteria:**
> - `.\scripts\notify-feature-complete.ps1 -FeatureId test -FeatureTitle "Test" -Summary "Test" -DryRun -Force` prints valid Adaptive Card JSON
> - Card includes 🔵 header, summary, test instructions (if provided), View Issues button (if URL provided)
> - Does NOT touch `send-teams-notification.ps1`, `icm-scan.ps1`, or `squad-daily-summary.ps1`

### Issue 2: Create `notify-blocked.ps1`

**Title:** `feat(notifications): add notify-blocked.ps1 caller script`  
**Labels:** `squad`, `squad:gimli`  
**Body:**
> Create `scripts/notify-blocked.ps1` that accepts blocker details and calls `notify.ps1` directly with tier `urgent`.
>
> **Parameters:** Title (mandatory), Reason (mandatory), ActionNeeded (mandatory), BlockerUrl, BlockerLabel (default: "View Blocker"), Agent, Severity (default: "blocking-feature"), DryRun, Force
>
> **Behavior:**
> 1. Build event hashtable with `eventId`, `errorType = "needs-human"`, formatted reason (includes Why, Action needed, Agent, Severity)
> 2. Call `notify.ps1 -Type urgent -Event $event`
> 3. Support `-DryRun` and `-Force` passthrough
>
> **Acceptance criteria:**
> - `.\scripts\notify-blocked.ps1 -Title "Test" -Reason "Test" -ActionNeeded "None" -DryRun -Force` prints valid Adaptive Card JSON
> - Card includes 🔴 header, reason block, action URL button
> - Does NOT touch `send-teams-notification.ps1`, `icm-scan.ps1`, or `squad-daily-summary.ps1`

### Issue 3: Wire coordinator to call notification scripts

**Title:** `feat(notifications): wire coordinator spawn flow to call notification scripts`  
**Labels:** `squad`, `squad:gandalf`  
**Body:**
> Update coordinator behavior documentation to include notification calls at the right points:
>
> **Feature Complete trigger points:**
> 1. After Galadriel's `task` tool returns APPROVE + PR is merged, and this is the last PR in a feature track → call `notify-feature-complete.ps1`
> 2. After any agent completes the last issue in a multi-issue feature → call `notify-feature-complete.ps1`
>
> **Blocked on Human trigger points:**
> 1. When Elrond's research task returns "No solution found" → call `notify-blocked.ps1` with severity `blocking-feature`
> 2. When Gandalf rejects Elrond's solution a second time → call `notify-blocked.ps1`
> 3. When any agent's task returns indicating it needs Jonathan's input → call `notify-blocked.ps1` with severity `decision-needed`
> 4. When Aragorn fails during active livesite → call `notify-blocked.ps1` with severity `livesite` (immediate, bypass pipeline)
>
> **This is a documentation + behavior change** — the coordinator reads its template on every session start, so updating the template IS the wiring.

### Issue 4: End-to-end validation

**Title:** `test(notifications): end-to-end dry-run validation of MVP triggers`  
**Labels:** `squad`, `squad:gimli`  
**Depends on:** Issues 1, 2  
**Body:**
> Validate the full notification pipeline works end-to-end:
>
> 1. Run both dry-run commands from the plan doc — verify valid JSON output
> 2. Run both with `-Force` (no `-DryRun`) — verify Teams cards arrive in the channel
> 3. Run feature-complete twice with same FeatureId — verify dedup suppresses the second
> 4. Run blocked twice with same eventId — verify re-notification after 6h rule works
> 5. Verify `icm-scan.ps1` and `squad-daily-summary.ps1` still work (no regression)
>
> **Acceptance criteria:** All 5 checks pass. Screenshot of received Teams cards attached to issue.

---

## Architecture summary

```
┌─────────────────────────────────────────────────────┐
│                  OLD SYSTEM (untouched)              │
│                                                     │
│  icm-scan.ps1 ──→ send-teams-notification.ps1 ──→ Teams
│  squad-daily-summary.ps1 ──→ send-teams-notification.ps1 ──→ Teams
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│                  NEW MVP (additive)                  │
│                                                     │
│  notify-feature-complete.ps1                        │
│    └─→ notification-scheduler.ps1                   │
│          └─→ notify.ps1 (feature tier, batched)     │
│                └─→ Teams webhook                    │
│                                                     │
│  notify-blocked.ps1                                 │
│    └─→ notify.ps1 (urgent tier, immediate)          │
│          └─→ Teams webhook                          │
│                                                     │
│  Coordinator (squad.agent.md behavior):             │
│    • After Galadriel APPROVE + merge → feature-complete
│    • After Elrond "no solution" → blocked            │
│    • After agent "needs input" → blocked             │
│    • After Aragorn livesite fail → blocked           │
└─────────────────────────────────────────────────────┘
```

## Key learning applied

From the 2026-03-25 post-mortem: **"Feature complete means user-observable effect, not code-merged."** This plan delivers observable effect by defining exactly who calls what, when, and with what payload. The dry-run commands let Jonathan verify the cards before going live. Issue 4 (E2E validation) gates the track — it's not done until Jonathan sees the card.
