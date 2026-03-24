# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Output Location | Examples |
|-----------|----------|-----------------|----------|
| General tasks, triage, coordination | 🏗️ Gandalf | — | "What should I work on?", prioritize tasks, general questions |
| Research, investigation, analysis | 🔍 Elrond | `docs/research/` | "Research X", "What's the state of Y?", deep dives, competitive analysis |
| Documentation, reports, summaries | 📝 Bilbo | `docs/` (category-specific) | "Write a doc for X", "Summarize this", create guides, READMEs |
| Scripts, tools, automation | 🔧 Gimli | — | "Build a script to do X", "Automate Y", create utilities |
| PR code review | 👑 Galadriel | `docs/reviews/` | "Review PR #42", code quality verification, finding reports |
| Livesite, incidents, Azure ops | ⚙️ Aragorn | `docs/investigations/` | "Investigate IcM #123", "Check service health", troubleshoot |
| TI domain backend code (C#, RP, STIX APIs) | 🗡️ Frodo | External repos (Sentinel-TiPipeline, SecurityInsights RP) | "Implement subscription filter", "Fix RP code", TI pipeline changes |
| Design review (adversarial) | 💀 Boromir | — | EVERY design Gandalf produces gets Boromir review. Always rejects first draft on concept/approach. |
| Scope & priorities | 🏗️ Gandalf | — | What to work on next, trade-offs, decisions |
| Significant feature review | 🏗️ Gandalf | — | After a significant feature is completed and merged, review that it actually works end-to-end (not just "code merged") |
| Session logging | 📋 Scribe | — | Automatic — never needs routing |
| Work queue monitoring | 🔄 Ralph | — | "Ralph, go", "What's on the board?", backlog status |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, assign `squad:{member}` label | 🏗️ Gandalf |
| `squad:gandalf` | General tasks, coordination | 🏗️ Gandalf |
| `squad:elrond` | Research, analysis, investigation | 🔍 Elrond |
| `squad:bilbo` | Documentation, reports, summaries | 📝 Bilbo |
| `squad:gimli` | Scripts, tools, automation | 🔧 Gimli |
| `squad:aragorn` | Livesite, incidents, ops | ⚙️ Aragorn |
| `squad:galadriel` | PR code review, quality gates | 👑 Galadriel |
| `squad:frodo` | TI domain backend code, RP changes | 🗡️ Frodo |

### How Issue Assignment Works

1. When a GitHub issue gets the `squad` label, the **Lead** triages it — analyzing content, evaluating @copilot's capability profile, assigning the right `squad:{member}` label, and commenting with triage notes.
2. **@copilot evaluation:** The Lead checks if the issue matches @copilot's capability profile (🟢 good fit / 🟡 needs review / 🔴 not suitable). If it's a good fit, the Lead may route to `squad:copilot` instead of a squad member.
3. When a `squad:{member}` label is applied, that member picks up the issue in their next session.
4. When `squad:copilot` is applied and auto-assign is enabled, `@copilot` is assigned on the issue and picks it up autonomously.
5. Members can reassign by removing their label and adding another member's label.
6. The `squad` label is the "inbox" — untriaged issues waiting for Lead review.

### Lead Triage Guidance for @copilot

When triaging, the Lead should ask:

1. **Is this well-defined?** Clear title, reproduction steps or acceptance criteria, bounded scope → likely 🟢
2. **Does it follow existing patterns?** Adding a test, fixing a known bug, updating a dependency → likely 🟢
3. **Does it need design judgment?** Architecture, API design, UX decisions → likely 🔴
4. **Is it security-sensitive?** Auth, encryption, access control → always 🔴
5. **Is it medium complexity with specs?** Feature with clear requirements, refactoring with tests → likely 🟡

## Failure Recovery

When any agent fails a task, the **failure recovery pipeline** activates automatically (see `.squad/failure-recovery.md`):

```
Failed Agent → Gandalf (triage) → Elrond (research, opus) → Gandalf (review)
  → Ralph (route implementation) → Gimli/assigned (build) → Galadriel (review)
  → Original Agent (retry)
```

**Key rule:** Jonathan is only notified if Elrond can't find a solution or the fix doesn't work after implementation. The squad self-heals autonomously.

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream work.
2. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
3. **Quick facts → coordinator answers directly.** Don't spawn an agent for "what port does the server run on?"
4. **When two agents could handle it**, pick the one whose domain is the primary concern.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
6. **Anticipate downstream work.** If a feature is being built, spawn the tester to write test cases from requirements simultaneously.
7. **Issue-labeled work** — when a `squad:{member}` label is applied to an issue, route to that member. The Lead handles all `squad` (base label) triage.
8. **@copilot routing** — when evaluating issues, check @copilot's capability profile in `team.md`. Route 🟢 good-fit tasks to `squad:copilot`. Flag 🟡 needs-review tasks for PR review. Keep 🔴 not-suitable tasks with squad members.
9. **Issue lifecycle enforcement** — all issue-linked work follows the lifecycle in `.squad/templates/issue-lifecycle.md`. The coordinator adds the ISSUE CONTEXT block to spawn prompts and follows the post-work steps (verify push → verify PR → route to reviewer → merge on approval). Read `issue-lifecycle.md` before spawning any agent for issue work.
10. **Galadriel PR Gate** — every PR created by any agent MUST be reviewed by Galadriel before merge. The coordinator spawns Galadriel (sync) with the PR diff after the author pushes and creates the PR. On REJECT, the original author addresses feedback. On APPROVE, the coordinator merges via `gh pr merge`. No PR merges without Galadriel's approval.
11. **Issue closure restriction** — issues that produced files (code, docs, scripts, designs, tests) close ONLY via PR merge auto-close ("Closes #N" in PR body). Never use `gh issue close` for file-producing work. **Exceptions:** (a) tracking/strategic issues and superseded issues may be closed with a comment, (b) branches merged via direct `git merge` during recovery — see Rule 23.
12. **Worktree for all file-producing work** — every task that creates or modifies files (including documentation) requires a worktree. Exceptions: read-only queries, Scribe (.squad/ state), pure analysis producing no files.
13. **Boromir Design Gate** — every design doc Gandalf produces MUST be reviewed by Boromir before implementation begins. The coordinator spawns Boromir (sync) with the design after Gandalf completes it. Boromir always rejects the first draft on concept/approach — he demands alternative approaches considered and rejected, evidence backing the chosen approach, and answers to "why not do this completely differently?" On REJECT, Gandalf revises and resubmits. Repeat from step 4 (re-review) until Boromir approves. Implementation is BLOCKED until Boromir approves. Gandalf has authority to override Boromir after genuinely considering his objections, but must document the override rationale in the decisions inbox.
14. **Merge ordering via Gandalf** — when multiple PRs are pending merge (2+ approved PRs from the same work batch or related issues), the coordinator MUST consult Gandalf on merge order before merging. Gandalf determines sequencing based on dependency order, conflict risk, and logical progression. Single isolated PRs skip this gate and merge directly after Galadriel approval.
15. **Teams notifications** — The coordinator MUST fire notifications via `.\scripts\notify-squad-event.ps1` for three categories of events. FAILURE: if notification fails, log it but don't block the workflow.
    - **Significant completion** (after feature completion, multi-branch merge, investigation conclusion, or PR merge producing committed artifacts): `.\scripts\notify-squad-event.ps1 -Event "feature-complete" -FeatureName "<feature name>" -Summary "<what shipped and impact>" -PRs "<#55,#56>" -DocLinks "<relevant URL>"`
    - **Investigation complete** (after an IcM investigation PR merges): `.\scripts\notify-squad-event.ps1 -Event "investigation-complete" -IcmNumber "<incident number>" -Title "<investigation title>" -Conclusion "<verdict (e.g., 'false positive', 'remediation needed')>" -ReportUrl "<GitHub permalink to investigation doc>" -IssueNumber "<GitHub issue number>"`. Card displays 🔍 header with IcM number, conclusion text, "View Report" button, and "View Issue" button.
    - **Blocked on human/external** (IMMEDIATELY when detected — by coordinator or Ralph — that work is blocked on external dependencies, human decisions, or external system access). Two modes:
      - **Multi-issue (preferred):** `.\scripts\notify-squad-event.ps1 -Event "blocked" -What "<header>" -ActionNeeded "<specific action>" -Issues '<JSON array>' -Agent "<agent>"`. JSON format: `[{"number":89,"title":"Issue title","url":"https://github.com/.../issues/89","reason":"Why blocked"}]`. Each issue gets its own card row with a clickable link and per-issue reason.
      - **Single-issue (legacy):** `.\scripts\notify-squad-event.ps1 -Event "blocked" -What "<what is blocked>" -Why "<why it needs Jonathan>" -ActionNeeded "<specific action>" -Link "<issue/PR URL>" -Agent "<agent>"`.
      When multiple issues share a root cause, send ONE aggregated notification using multi-issue mode. Ralph's scan cycle MUST check for newly-detected blockers and fire notifications.
16. **Issue-tracked work** — when the user names a squad member in their request (except Ralph and Scribe), the coordinator MUST create a GitHub issue BEFORE spawning the agent. Flow: (1) `gh issue create --title "{task summary}" --body "{user's request context}" --label "squad" --label "squad:{member-name}"`, (2) note the issue number, (3) spawn the agent with ISSUE CONTEXT block per `issue-lifecycle.md`. Exempt: Ralph (monitor), Scribe (infrastructure), and pure read-only queries that produce no artifacts (e.g., "Elrond, what does this function do?"). Research tasks that produce investigation docs ARE tracked (e.g., "Aragorn, investigate IcM 123"). When the user says "team, do X", treat as "Gandalf, analyze and decompose X" — create ONE issue for Gandalf; Gandalf creates sub-issues for each agent via `gh issue create`. This ensures ALL squad work appears on the GitHub board and Ralph can drive it.
17. **Feature = Milestone** — a feature is any work that requires 2 or more tasks (GitHub issues). When Gandalf decomposes a request and yields 2+ issues, he MUST create a GitHub milestone before creating the issues: `gh api repos/jbenami_microsoft/ms-pa/milestones --method POST -f title="{Feature Name}" -f description="{One-line description}. Design: docs/designs/{slug}.md"`. Single-issue requests are standalone tasks and do NOT get a milestone.
18. **Milestone assignment at creation time** — every issue that is part of a multi-task feature MUST be assigned to the corresponding milestone at issue creation time using `--milestone "{Milestone Title}"` in `gh issue create`, or via `gh api` PATCH after creation. Issues floating without a milestone attachment when a milestone exists for their work are governance debt. Gandalf is responsible for ensuring assignment completeness at decomposition time.
19. **Ralph: milestone completion scan** — Ralph's work-check cycle MUST scan all open milestones for completion using `gh api "repos/jbenami_microsoft/ms-pa/milestones?state=open&per_page=100"`. A milestone is complete when `open_issues == 0` AND `closed_issues > 0`. On detection: (1) check for won't-fix closures (state_reason == "not_planned"), (2) close the milestone via `gh api PATCH state=closed`, (3) write a completion trigger to `.squad/decisions/inbox/ralph-milestone-complete-{number}.json` with milestone_number, milestone_title, wont_fix_count, wont_fix_numbers, and triggered_at. Ralph MUST log `[MILESTONE-COMPLETE]` to his work-check output for every detected completion.
20. **Post-completion pipeline (milestone lifecycle gate)** — when the coordinator receives a `ralph-milestone-complete-{number}.json` trigger, it MUST run the post-completion pipeline in this exact order — each step gates the next:
    1. **Gandalf review** (sync spawn): compare delivered work to design/issues. Writes review to `docs/reviews/milestone-{number}-{slug}-review.md`. Verdict: PASS or FAIL.
    2. **Galadriel E2E test** (sync spawn, only if Step 1 = PASS): functional end-to-end verification. Writes report to `docs/reviews/milestone-{number}-{slug}-e2e.md`. Verdict: PASS or FAIL.
    3. **Teams notification** (coordinator executes, only if Step 2 = PASS): `.\scripts\notify-squad-event.ps1 -Event "feature-complete"` with feature name, summary, PR list, doc links.
    4. **Bilbo documentation** (sync spawn, only if Step 3 sent): document the completed feature.
    No step may be skipped. The pipeline does not send a Teams notification until both the review AND the E2E test pass.
21. **Feature review failure (remediation loop)** — if Gandalf's review (Rule 20, Step 1) returns FAIL: (1) Gandalf creates remediation issues labeled `squad` + `squad:{agent}`, assigned to the same milestone, (2) the coordinator re-opens the milestone via `gh api PATCH state=open`, (3) the pipeline pauses. When Ralph's next scan detects the milestone is complete again (all remediation issues closed), the full pipeline restarts from Step 1. If Galadriel's E2E test (Rule 20, Step 2) returns FAIL: (1) Galadriel creates fix issues assigned to the same milestone, (2) the coordinator re-opens the milestone, (3) the pipeline restarts from Step 2 (not Step 1) when Ralph detects completion again.
22. **Won't-fix transparency** — when Ralph detects milestone completion and the trigger includes `wont_fix_count > 0`, Gandalf's review (Rule 20, Step 1) MUST explicitly address every won't-fix issue in the review document. For each: was this a deliberate deferral or a missed task? Unintentional won't-fix closures (missed work) are automatic FAIL criteria. Deliberate deferrals must be documented with rationale and, if significant, a follow-up issue must be created.
23. **Post-merge issue verification** — after ANY merge to main — whether via `gh pr merge`, `git merge`, or direct commit — the coordinator MUST verify that all issues whose work was just landed are properly closed. If an issue remains open after its branch merged (e.g., direct `git merge` bypassed auto-close), close it with `gh issue close {N} --comment "Branch merged to main via direct merge. PR auto-close did not fire. Closing per Rule 23."` This catches recovery scenarios, manual conflict resolution, and any non-standard merge path.
24. **Worktree cleanup** — after a branch merges to main, the coordinator MUST clean up the associated worktree and local branch: (1) `git worktree remove ./worktrees/squad-{N}` to remove the worktree, (2) `git branch -d squad/{N}-{slug}` to delete the merged local branch. If the worktree contains uncommitted changes, the coordinator MUST warn and skip removal (do not use `--force`). **Periodic scan:** during Ralph's work-check cycle, Ralph MUST list worktrees via `git worktree list` and flag any whose branch has already been merged to main (i.e., branch no longer exists on remote or is fully merged). These are stale and should be cleaned up.
25. **Infrastructure failure notifications** — when ANY infrastructure or tool failure occurs during coordinator operations, the coordinator MUST immediately fire a Teams notification. No silent error swallowing. **Failure categories:** git operations (permissions, auth, push/pull failures), GitHub CLI errors (`gh` command failures), MCP tool disconnections or errors, notification script failures, worktree corruption, branch operations failures, merge conflicts that block automation. **Format:** `.\scripts\notify-squad-event.ps1 -Event "blocked" -What "Infrastructure failure: {category}" -Why "{error message}" -ActionNeeded "Investigate and resolve {category} failure" -Link "{relevant URL if any}" -Agent "coordinator"`. This fires IMMEDIATELY on detection — not batched, not deferred to Ralph's scan cycle. **Meta-failure exception:** if the notification script itself fails, log to console with `[CRITICAL]` prefix — this is the only case where silent handling is permitted (because the notification system itself is broken).
