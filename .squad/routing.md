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
