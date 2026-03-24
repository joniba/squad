# Design: Milestone-Based Feature Lifecycle

**Author:** Gandalf (Lead)  
**Date:** 2026-03-24  
**Status:** DRAFT — pending Boromir review  
**Issue:** [#141 — Design milestone-based feature lifecycle](https://github.com/jbenami_microsoft/ms-pa/issues/141)  
**Requested by:** Jonathan

---

## 1. Concept Definition

### What Is a "Feature"?

A **feature** is any unit of work that requires more than one task (GitHub issue) to complete.

| Concept | GitHub Primitive | Example |
|---------|-----------------|---------|
| Feature | Milestone | "DGrep CLI — Phase 1 Foundation" |
| Task | Issue | "#101 — CLI arg parsing" |
| Sub-task | Issue (child, via linked issues) | Rare — avoid nesting beyond one level |

**Threshold rule:** If Gandalf decomposes a request and it yields 2 or more issues, a milestone is created first and all issues are assigned to it. Single-issue requests do NOT get a milestone — they are standalone tasks.

### What a Milestone Is NOT

A milestone is not a sprint, not a time-box, and not a backlog category. It represents a coherent deliverable — something Jonathan would recognize as "the X feature shipped." Milestones should have names that make sense to a human: "Teams Notification MVP", "DGrep Phase 2 Transport Layer", "Failure Recovery Pipeline."

---

## 2. Lifecycle Flow

```
[Jonathan request]
        │
        ▼
[Gandalf: triage]
  ├─ 1 issue? → create issue, no milestone
  └─ 2+ issues? → create milestone → create issues (with milestone) → write design doc
                                                                          │
                                          ┌───────────────────────────────┘
                                          ▼
                              [Agents: execute tasks]
                              └── issues close via PR merge auto-close
                                                   │
                                 [Ralph: work-check scan]
                                   └── detects open_issues == 0
                                         && closed_issues > 0
                                               │
                                 [Ralph: closes milestone, writes trigger]
                                               │
                          ┌────────────────────┘
                          │    POST-COMPLETION PIPELINE (in order, each gates next)
                          │
                          ├─ Step 1: Gandalf review
                          │    └── Design vs. delivered? PASS / FAIL
                          │         └─ FAIL → create remediation issues → re-run from Step 1
                          │
                          ├─ Step 2: Galadriel E2E testing
                          │    └── Does the feature actually work? PASS / FAIL
                          │         └─ FAIL → create fix issues → re-run from Step 2
                          │
                          ├─ Step 3: Teams notification
                          │    └── notify-squad-event.ps1 -Event "feature-complete"
                          │
                          └─ Step 4: Bilbo documentation
                               └── Feature documented in docs/
```

---

## 3. GitHub Mechanics

### 3.1 Creating a Milestone

```powershell
# Gandalf creates the milestone at decomposition time
gh api repos/jbenami_microsoft/ms-pa/milestones --method POST `
  -f title="Feature Name" `
  -f description="One-sentence description. Design: docs/designs/feature-name.md"

# Response includes .number — save this for issue assignment
$milestoneNumber = (gh api repos/jbenami_microsoft/ms-pa/milestones --method POST `
  -f title="Feature Name" `
  -f description="..." | ConvertFrom-Json).number
```

### 3.2 Assigning Issues to a Milestone

Option A — at issue creation time (preferred):
```powershell
gh issue create `
  --title "Task title" `
  --body "Task description" `
  --label "squad" --label "squad:gimli" `
  --milestone "Feature Name"
```

Option B — after issue creation:
```powershell
gh api repos/jbenami_microsoft/ms-pa/issues/{issue_number} `
  --method PATCH `
  -F milestone={milestone_number}
```

### 3.3 Ralph: Detecting Milestone Completion

Ralph's work-check cycle adds a milestone scan. The detection condition is:
- `open_issues == 0`  
- `closed_issues > 0`  
- `state == "open"` (not already closed)

```powershell
# Ralph runs this in his work-check cycle
$milestones = gh api "repos/jbenami_microsoft/ms-pa/milestones?state=open&per_page=100" | ConvertFrom-Json

$completedMilestones = $milestones | Where-Object {
    $_.open_issues -eq 0 -and $_.closed_issues -gt 0
}

foreach ($m in $completedMilestones) {
    # Check for won't-fix closures (state_reason == "not_planned")
    $closedIssues = gh api "repos/jbenami_microsoft/ms-pa/issues?milestone=$($m.number)&state=closed&per_page=100" | ConvertFrom-Json
    $wontFix = $closedIssues | Where-Object { $_.state_reason -eq "not_planned" }

    # Close the milestone
    gh api "repos/jbenami_microsoft/ms-pa/milestones/$($m.number)" --method PATCH -f state=closed

    # Write completion trigger to decisions inbox
    $trigger = @{
        milestone_number = $m.number
        milestone_title  = $m.title
        milestone_url    = $m.html_url
        closed_issues    = $m.closed_issues
        wont_fix_count   = $wontFix.Count
        wont_fix_numbers = ($wontFix | ForEach-Object { $_.number }) -join ","
        triggered_at     = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
    }
    $trigger | ConvertTo-Json | Set-Content ".squad/decisions/inbox/ralph-milestone-complete-$($m.number).json"
}
```

### 3.4 Ralph: Coordinator Interaction

After writing the trigger file, Ralph adds a structured log entry in his work-check output:

```
[MILESTONE-COMPLETE] Milestone #N "Feature Name" — all N issues closed.
  Won't-fix: M issues. Trigger written to decisions inbox.
  Post-completion pipeline: Gandalf review → Galadriel E2E → Teams notify → Bilbo docs.
```

The coordinator reads the trigger and spawns the post-completion pipeline.

### 3.5 Closing the Milestone

Ralph closes the milestone **before** triggering the pipeline. Rationale: GitHub's milestone state is a display concern. The post-completion pipeline is the quality gate. Closing the milestone records that "all tasks are done" — the pipeline then verifies whether "done" actually means "working."

```powershell
gh api "repos/jbenami_microsoft/ms-pa/milestones/{number}" --method PATCH -f state=closed
```

### 3.6 Listing Closed Milestones (for audit/reference)

```powershell
gh api "repos/jbenami_microsoft/ms-pa/milestones?state=closed&per_page=100" | ConvertFrom-Json |
  Select-Object number, title, closed_at, closed_issues | Format-Table
```

---

## 4. Post-Completion Pipeline — Step-by-Step

### Step 1: Gandalf Feature Review

**Who:** Gandalf  
**When:** Immediately after Ralph detects milestone completion  
**Input:** Design doc (if exists), all closed issues, their linked PRs  
**Output:** Written review at `docs/reviews/milestone-{number}-{slug}-review.md`

**Process:**
1. Read the design doc (if one exists — see Edge Case 1 for no-design-doc handling)
2. Read each closed issue's title, body, and linked PRs
3. For each won't-fix issue: explicitly note in review (see Edge Case 2)
4. Answer: Did we build what we said we'd build? Any scope drift? Any missing pieces?
5. Verdict: **PASS** or **FAIL** with written rationale

**PASS criteria:**
- All issues in scope were either completed or explicitly deferred with documented rationale
- No silent scope drops (things that should have been built but weren't, without comment)
- Won't-fix closures are intentional and documented (not forgotten work)

**FAIL criteria:**
- Core acceptance criteria from the design were not met
- Issues were silently dropped without documented rationale
- Won't-fix closures were unintentional (missed work, not a deliberate choice)

**On FAIL:** Gandalf creates remediation issues (labeled `squad` + `squad:{agent}`), assigns them to the same milestone, re-opens the milestone, and the pipeline pauses until all remediation issues close.

```powershell
# Re-open a milestone (for remediation)
gh api "repos/jbenami_microsoft/ms-pa/milestones/{number}" --method PATCH -f state=open
```

### Step 2: Galadriel E2E Testing

**Who:** Galadriel  
**When:** After Gandalf's review returns PASS  
**Input:** Milestone description, design doc (if exists), closed issues  
**Output:** Written E2E test report at `docs/reviews/milestone-{number}-{slug}-e2e.md`

**What "E2E testing" means here:**
- This is NOT code review (Galadriel already reviewed each PR at merge time)
- This IS functional verification: does the feature work as a whole, end-to-end?
- Galadriel reads the design/issues to understand what the feature should DO, then verifies it actually does that
- For scripts: run them. For CLI tools: execute them. For documentation: verify it's accurate. For pure org/governance changes: verify the rules are in place and the coordinator would follow them.

**PASS criteria:**
- Feature is functional at the user level (not just "code merged")
- Integration between components works (e.g., if feature A calls feature B, that call works)
- No regressions in adjacent features

**On FAIL:** Galadriel creates fix issues (labeled `squad` + `squad:{agent}`), assigns to the milestone. Pipeline pauses until fix issues close, then re-runs from Step 2 (not Step 1).

### Step 3: Teams Notification

**Who:** Coordinator (automated)  
**When:** After Galadriel's E2E test returns PASS  
**Command:**

```powershell
.\scripts\notify-squad-event.ps1 `
  -Event "feature-complete" `
  -FeatureName "Feature Name" `
  -Summary "What shipped and its impact. N tasks completed." `
  -PRs "#55,#56,#57" `
  -DocLinks "docs/designs/feature-name.md,docs/reviews/milestone-N-slug-review.md" `
  -TestInstructions "How to verify: [brief description]"
```

**No notification on FAIL.** Jonathan gets one notification per feature, after it fully passes.

### Step 4: Bilbo Documentation

**Who:** Bilbo  
**When:** After Teams notification fires  
**Input:** Design doc, E2E test report, Gandalf's review, closed issues  
**Output:** Completed feature entry in `docs/` (specific path depends on feature type)

**What Bilbo documents:**
- Feature summary (what it does, when it shipped)
- Architecture decisions made during development
- How to use the feature (if user-facing)
- Cross-references to design doc, review, E2E report
- Index entries updated

---

## 5. Proposed Routing Rule Changes

These are the exact numbered rules to append to `routing.md → ## Rules`. They follow existing rules (currently 1–17), so new rules start at **18**.

---

**Rule 18 — Feature = Milestone:**
> A feature is any work that requires 2 or more tasks (GitHub issues). When Gandalf decomposes a request and yields 2+ issues, he MUST create a GitHub milestone before creating the issues: `gh api repos/jbenami_microsoft/ms-pa/milestones --method POST -f title="{Feature Name}" -f description="{One-line description}. Design: docs/designs/{slug}.md"`. Single-issue requests are standalone tasks and do NOT get a milestone.

**Rule 19 — Milestone assignment at creation time:**
> Every issue that is part of a multi-task feature MUST be assigned to the corresponding milestone at issue creation time using `--milestone "{Milestone Title}"` in `gh issue create`, or via `gh api` PATCH after creation. Issues floating without a milestone attachment when a milestone exists for their work are governance debt. Gandalf is responsible for ensuring assignment completeness at decomposition time.

**Rule 20 — Ralph: milestone completion scan:**
> Ralph's work-check cycle MUST scan all open milestones for completion using `gh api "repos/jbenami_microsoft/ms-pa/milestones?state=open&per_page=100"`. A milestone is complete when `open_issues == 0` AND `closed_issues > 0`. On detection: (1) check for won't-fix closures (state_reason == "not_planned"), (2) close the milestone via `gh api PATCH state=closed`, (3) write a completion trigger to `.squad/decisions/inbox/ralph-milestone-complete-{number}.json` with milestone_number, milestone_title, wont_fix_count, wont_fix_numbers, and triggered_at. Ralph MUST log `[MILESTONE-COMPLETE]` to his work-check output for every detected completion.

**Rule 21 — Post-completion pipeline (milestone lifecycle gate):**
> When the coordinator receives a `ralph-milestone-complete-{number}.json` trigger, it MUST run the post-completion pipeline in this exact order — each step gates the next:
> 1. **Gandalf review** (sync spawn): compare delivered work to design/issues. Writes review to `docs/reviews/milestone-{number}-{slug}-review.md`. Verdict: PASS or FAIL.
> 2. **Galadriel E2E test** (sync spawn, only if Step 1 = PASS): functional end-to-end verification. Writes report to `docs/reviews/milestone-{number}-{slug}-e2e.md`. Verdict: PASS or FAIL.
> 3. **Teams notification** (coordinator executes, only if Step 2 = PASS): `.\scripts\notify-squad-event.ps1 -Event "feature-complete"` with feature name, summary, PR list, doc links.
> 4. **Bilbo documentation** (sync spawn, only if Step 3 sent): document the completed feature.
> No step may be skipped. The pipeline does not send a Teams notification until both the review AND the E2E test pass.

**Rule 22 — Feature review failure (remediation loop):**
> If Gandalf's review (Rule 21, Step 1) returns FAIL: (1) Gandalf creates remediation issues labeled `squad` + `squad:{agent}`, assigned to the same milestone, (2) the coordinator re-opens the milestone via `gh api PATCH state=open`, (3) the pipeline pauses. When Ralph's next scan detects the milestone is complete again (all remediation issues closed), the full pipeline restarts from Step 1. If Galadriel's E2E test (Rule 21, Step 2) returns FAIL: (1) Galadriel creates fix issues assigned to the same milestone, (2) the coordinator re-opens the milestone, (3) the pipeline restarts from Step 2 (not Step 1) when Ralph detects completion again.

**Rule 23 — Won't-fix transparency:**
> When Ralph detects milestone completion and the trigger includes `wont_fix_count > 0`, Gandalf's review (Rule 21, Step 1) MUST explicitly address every won't-fix issue in the review document. For each: was this a deliberate deferral or a missed task? Unintentional won't-fix closures (missed work) are automatic FAIL criteria. Deliberate deferrals must be documented with rationale and, if significant, a follow-up issue must be created.

---

## 6. Proposed Charter Changes

### 6.1 Gandalf Charter Changes

**Add to "What I Own":**

```markdown
- **Milestone creation:** When decomposing multi-task work (2+ issues), I create the GitHub
  milestone first, then create issues assigned to it. I do not create floating issues for
  multi-task features — they must be anchored to a milestone.
- **Feature lifecycle review:** When the post-completion pipeline triggers, I review the
  milestone's delivered work against its design/issues and produce a PASS/FAIL verdict with
  written rationale at docs/reviews/milestone-{number}-{slug}-review.md.
```

**Add to "Delegation Model" table:**

```markdown
| **Milestone review** | Author review report (PASS/FAIL vs. design) | Galadriel gates E2E |
```

### 6.2 Ralph Charter Changes

Ralph does not currently have a charter.md at `.squad/agents/ralph/charter.md`. A charter must be created for Ralph as part of implementation. The charter must include the following section:

```markdown
## Milestone Scan (Work-Check Cycle Addition)

In every work-check cycle, after scanning the issue board, Ralph scans milestones:

1. Query: `gh api "repos/{owner}/{repo}/milestones?state=open&per_page=100"`
2. Detect complete milestones: open_issues == 0 AND closed_issues > 0
3. For each complete milestone:
   a. Check for won't-fix issues: query closed issues with state_reason == "not_planned"
   b. Close the milestone: `gh api PATCH milestones/{number} state=closed`
   c. Write trigger: `.squad/decisions/inbox/ralph-milestone-complete-{number}.json`
   d. Log: `[MILESTONE-COMPLETE] Milestone #N "Title" — N issues closed, M won't-fix.`
4. Ralph does NOT run the post-completion pipeline — that is the coordinator's job on reading the trigger.
```

### 6.3 Galadriel Charter Changes

**Add to "✅ I Handle":**

```markdown
- **Milestone E2E testing:** When the coordinator routes a completed milestone for
  end-to-end testing (post-completion pipeline Step 2), I perform functional verification
  of the feature as a whole. This is NOT code re-review — it's "does the feature work?"
  I produce a test report at docs/reviews/milestone-{number}-{slug}-e2e.md with PASS/FAIL.
```

**Add to "Collaboration Patterns" table:**

```markdown
| **Milestone pipeline** | After Gandalf's review passes, coordinator spawns me for E2E testing of the full feature. |
```

### 6.4 Bilbo Charter Changes

**Add to "Workflow Triggers":**

```markdown
- **A milestone post-completion pipeline reaches Step 4** → I document the completed feature.
  Input: design doc, Gandalf's review, Galadriel's E2E report, closed issues.
  Output: feature summary document in the appropriate docs/ category, indexes updated.
```

---

## 7. Proposed Ralph Integration (Scan Cycle Detail)

Ralph's current role is "work queue monitoring." His cycle checks the issue board for labeled, open issues and routes them to the appropriate agent.

The milestone scan is an **additive extension** to this cycle — it runs after the existing board scan, using the same polling interval.

### 7.1 Pseudocode for Ralph's Extended Cycle

```
EVERY CYCLE:

1. Board scan (existing behavior)
   - Query open issues with squad:{member} labels
   - Route unstarted issues to appropriate agents

2. Milestone scan (new behavior — Rule 20)
   - GET /milestones?state=open
   - For each milestone where open_issues == 0 AND closed_issues > 0:
     a. GET /issues?milestone={N}&state=closed → check state_reason
     b. PATCH /milestones/{N} → state=closed
     c. Write trigger JSON to .squad/decisions/inbox/
     d. Log [MILESTONE-COMPLETE]

3. Trigger processing check (new behavior)
   - Check .squad/decisions/inbox/ for unprocessed ralph-milestone-complete-*.json files
   - For each unprocessed trigger: report to coordinator
   - Coordinator handles pipeline spawning
```

### 7.2 Trigger File Format

```json
{
  "milestone_number": 3,
  "milestone_title": "Teams Notification MVP",
  "milestone_url": "https://github.com/jbenami_microsoft/ms-pa/milestone/3",
  "milestone_description": "Teams notification feature complete.",
  "design_doc": "docs/designs/notifications-mvp.md",
  "closed_issues": 8,
  "wont_fix_count": 1,
  "wont_fix_numbers": "117",
  "triggered_at": "2026-03-24T20:00:00Z",
  "pipeline_status": "pending"
}
```

### 7.3 Trigger State Management

The coordinator updates `pipeline_status` in the trigger file as the pipeline progresses:
- `"pending"` → pipeline not started
- `"step1-gandalf"` → Gandalf review in progress
- `"step2-galadriel"` → E2E testing in progress
- `"step3-notify"` → notification in progress
- `"step4-bilbo"` → documentation in progress
- `"complete"` → pipeline finished
- `"failed-step1"` / `"failed-step2"` → failure state, remediation in progress

Ralph skips already-processed triggers (pipeline_status != "pending").

---

## 8. Edge Cases

### Edge Case 1: Milestone Has No Design Doc

**When:** A feature was created before this lifecycle was adopted, or Gandalf decomposed multi-task work without producing a formal design doc.

**How Gandalf handles Step 1 review:**
- The GitHub issues themselves become the spec. Each issue's title + body = the stated requirement.
- Gandalf reviews: "Did each issue's stated requirement get met by its linked PR?"
- Gandalf notes explicitly in the review: "No design doc exists — review based on issue bodies."
- PASS is still achievable. The bar shifts from "design vs. delivered" to "stated requirements vs. delivered."

**Going forward:** The design doc requirement (Rule 13 Boromir gate) ensures any design-worthy feature gets a doc. For small features (2-3 issues, lightweight work), the issue bodies are sufficient spec.

### Edge Case 2: Some Issues Closed as Won't Fix

**Scenario:** A milestone has 6 issues. 5 are closed as "completed." 1 is closed as "not planned."

**Ralph's behavior:** Detects completion (open_issues == 0, closed_issues == 6). Includes `wont_fix_count: 1` and the issue number in the trigger.

**Gandalf's review (Rule 23):** MUST explicitly address the won't-fix issue.
- **Deliberate deferral** (correct): "Issue #X was won't-fixed because [reason]. This was a scope decision made during development. Follow-up tracked as #Y." → PASS if the deferral doesn't invalidate the feature.
- **Missed work** (incorrect): Issue was forgotten or someone hit close by mistake. → FAIL. Gandalf creates a remediation issue.

**Important:** Won't-fix does NOT automatically fail a milestone. It requires explicit Gandalf acknowledgment. Unacknowledged won't-fix = automatic FAIL.

### Edge Case 3: Feature Review Fails

**Scenario:** Gandalf's review returns FAIL. Some core work was missed.

**What happens:**
1. Gandalf creates 1+ remediation issues. Each issue is labeled `squad` + `squad:{agent}` and assigned to the same milestone number.
2. Coordinator re-opens the milestone.
3. Agents execute remediation issues as normal (branch → PR → Galadriel review → merge).
4. When all remediation issues are closed, Ralph's next scan detects completion again.
5. Coordinator runs the full pipeline from Step 1.

**Escape hatch:** If Gandalf determines that remediation is out of scope (e.g., the won't-fix work should actually be a new feature), he documents this in the review and marks it PASS with explicit documented rationale. The scope call is Gandalf's.

### Edge Case 4: Galadriel E2E Test Fails

**Scenario:** Gandalf's review PASSED, but Galadriel finds the feature doesn't actually work.

**What happens:**
1. Galadriel creates fix issues, labeled and assigned to the milestone.
2. Coordinator re-opens the milestone.
3. Fix issues follow normal workflow (branch → PR → Galadriel PR review → merge).
4. When fix issues close, Ralph detects completion again.
5. Pipeline restarts from **Step 2** (Galadriel E2E re-test) — Gandalf's design review does NOT re-run unless Galadriel flags a design-level issue.

### Edge Case 5: Jonathan Declares Feature "Done" Mid-Pipeline

**Scenario:** Jonathan says "ship it, we'll fix the rest later" before the pipeline completes.

**Protocol:** Jonathan's decision overrides the pipeline. Gandalf records in the review: "Jonathan explicitly approved early closure of milestone #N. Remaining gaps: [list]." A follow-up milestone or issues should be created to track the remaining work. The Teams notification fires with a note: "Feature shipped with known gaps — see review doc."

### Edge Case 6: Milestone Has Only 1 Issue

**Prevention:** Rule 18 prevents this — milestones only exist for 2+ issue work. If an issue is mistakenly assigned to a milestone when it's the only issue, Ralph's scan will still detect completion and trigger the pipeline. Gandalf's review will note the mismatch. The coordinator should remove the milestone assignment and close the orphan milestone.

### Edge Case 7: Feature Spans Multiple Design Doc Iterations

**Scenario:** The design was revised (Boromir REJECT → Gandalf revises) before implementation. Multiple design doc versions exist.

**Gandalf's review:** Use the FINAL approved design doc (the one that preceded implementation). Note the revision history if relevant. The key question is "does the implementation match what we committed to build?"

---

## 9. Implementation Sequencing

This design must go through Boromir review before any implementation begins (Rule 13). After approval, implementation consists of:

1. **Create Ralph charter** (`.squad/agents/ralph/charter.md`) — includes milestone scan section
2. **Update Gandalf charter** — add milestone creation and feature review sections
3. **Update Galadriel charter** — add E2E testing section
4. **Update Bilbo charter** — add milestone documentation trigger
5. **Add Rules 18–23 to routing.md**
6. **Build/update Ralph's scan script** (ralph-watch.ps1 or equivalent) — add milestone scan loop
7. **Create milestone trigger schema** (document the JSON format)
8. **Create PR** for all governance changes (Rules, charters) — Galadriel reviews, Gandalf oversees

Implementation issues should be filed after Boromir approval.

---

## 10. Summary Table

| Concept | Primitive | Owner | Tooling |
|---------|-----------|-------|---------|
| Feature | GitHub Milestone | Gandalf (creates) | `gh api /milestones` |
| Task | GitHub Issue | Gandalf (creates), agents (execute) | `gh issue create --milestone` |
| Completion detection | Milestone open_issues == 0 | Ralph | `gh api /milestones?state=open` |
| Design review | docs/reviews/milestone-N-*-review.md | Gandalf | Written report |
| E2E testing | docs/reviews/milestone-N-*-e2e.md | Galadriel | Written report |
| Teams notification | Teams card | Coordinator | `notify-squad-event.ps1 -Event feature-complete` |
| Feature documentation | docs/{category}/ | Bilbo | Written doc |
