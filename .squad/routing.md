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

## Boromir Gate (Mandatory for Designs)

Every design doc Gandalf produces MUST be reviewed by Boromir before implementation begins. Boromir always rejects the first draft — not on details, but on the entire concept and approach. He demands:
- Alternative approaches that were considered and rejected (with reasons)
- Evidence/research backing the chosen approach
- Answers to "why not do this completely differently?"

**Flow:** Gandalf designs → Boromir reviews (REJECT expected) → Gandalf revises → Boromir re-reviews → approve or reject again. Implementation is BLOCKED until Boromir approves.

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
