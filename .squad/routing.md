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
| Work review | 🏗️ Gandalf | — | Review output quality, check coherence |
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
11. **Issue closure restriction** — issues that produced files (code, docs, scripts, designs, tests) close ONLY via PR merge auto-close ("Closes #N" in PR body). Never use `gh issue close` for file-producing work. Exception: tracking/strategic issues and superseded issues may be closed with a comment.
12. **Worktree for all file-producing work** — every task that creates or modifies files (including documentation) requires a worktree. Exceptions: read-only queries, Scribe (.squad/ state), pure analysis producing no files.
13. **Boromir Design Gate** — every design doc Gandalf produces MUST be reviewed by Boromir before implementation begins. The coordinator spawns Boromir (sync) with the design after Gandalf completes it. Boromir always rejects the first draft on concept/approach — he demands alternative approaches considered and rejected, evidence backing the chosen approach, and answers to "why not do this completely differently?" On REJECT, Gandalf revises and resubmits. Repeat from step 4 (re-review) until Boromir approves. Implementation is BLOCKED until Boromir approves. Gandalf has authority to override Boromir after genuinely considering his objections, but must document the override rationale in the decisions inbox.
14. **Merge ordering via Gandalf** — when multiple PRs are pending merge (2+ approved PRs from the same work batch or related issues), the coordinator MUST consult Gandalf on merge order before merging. Gandalf determines sequencing based on dependency order, conflict risk, and logical progression. Single isolated PRs skip this gate and merge directly after Galadriel approval.
15. **Squad governance via Gandalf** — modifications to `.squad/` governance files (charters, routing.md, team.md, ceremonies.md, casting state) MUST be authored by Gandalf. No other agent may modify these files. Other agents write only to their own history.md and the decisions inbox. When the coordinator needs a charter updated, routing changed, or a member added/removed, it routes the modification task to Gandalf.
16. **Notification on significant completion** — After the coordinator completes a significant batch of work (feature completion, multi-branch merge, investigation conclusion, or PR merge resulting in committed artifacts), call the notification dispatcher from the repo root: `.\scripts\notify-squad-event.ps1 -Event "feature-complete" -What "<summary of what was completed>" -Why "<details and impact>" -Link "<relevant URL>" -Agent "<agent who did the work>"`. SCOPE: only for work producing committed artifacts (not status checks, questions, or dry-runs). FAILURE: if notification fails, log it but don't block the workflow.
