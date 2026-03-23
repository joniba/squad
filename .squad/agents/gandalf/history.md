# Project Context

- **Owner:** Jonathan
- **Project:** pa-squad — a personal-assistant squad for everyday tasks, research, documentation, tool building, and livesite support
- **Stack:** General-purpose (not a single-stack project)
- **Created:** 2026-03-22

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-03-22 — Triage Squad Feedback Issues #44-#49
- **Routing patterns:** Feedback on external squad product (cli, watch mode, defaults) routes to Gandalf (product feedback tracking). Tool fixes (PR flags, model defaults) route to Gimli. Documentation/charter patterns route to Bilbo.
- **Issue sources:** All 6 issues are Jonathan's onboarding feedback observations, filed on ms-pa repo because EMU tokens cannot write to bradygaster/squad directly.
- **Issues triaged:**
  - **#44** (PR draft flag) → `squad:gimli` (CLI fix)
  - **#45** (OAuth scopes) → `squad:gandalf` (external feedback)
  - **#46** (model defaults 4.6) → `squad:gimli` (config)
  - **#47** (watch UX) → `squad:gandalf` (external feedback)
  - **#48** (reviewer charter quality) → `squad:bilbo` (documentation patterns)
  - **#49** (default reviewer charter) → `squad:bilbo` (documentation patterns)

### 2026-03-22 — Teams Watchdog Decomposition
- **Architecture:** Teams watchdog is a 4-step pipeline (probe → filter → extract → format) + a watchdog orchestrator + delivery. Each step is a separate script under `.squad/skills/teams-watchdog/`.
- **Key constraint:** `gh copilot` doesn't work. Must use `copilot -p` or `copilot -i`. Cannot save Copilot output to a variable — use file-based I/O between steps.
- **Pattern:** Watchdog loop pattern from `ralph-watch.ps1` (lockfile, heartbeat, structured logging, Teams alerts on consecutive failures).
- **WorkIQ integration:** Teams messages are fetched via the `workiq-ask_work_iq` MCP tool, invoked through `copilot -p` prompts.
- **Repo:** Issues #1-#6 on `jbenami_microsoft/ms-pa`, labeled `squad` + `squad:gimli`.
- **Labels created:** `squad` (purple, 6f42c1) and `squad:gimli` (red, d73a49) on the ms-pa repo.

### 2026-03-22 — Failure Recovery Pipeline Architecture

- **Trigger:** Galadriel hit a wall trying to read ADO PR file contents — the `ado-repo_get_file_contents` tool doesn't exist. That near-miss made it clear the squad had no formal self-healing path.
- **Pipeline:** Failed Agent → Gandalf (triage) → Elrond (research, opus-4.6) → Gandalf (review) → Ralph (route) → Gimli/assigned (build) → Galadriel (review) → Original Agent (retry)
- **Signal format:** `.squad/decisions/inbox/{agent}-failure-{slug}.md` — structured failure report with slug convention, timestamp (ISO 8601 UTC), dedup check
- **Notification rule:** Jonathan only hears about it if Elrond can't find a solution, Gandalf rejects twice, retry still fails, OR Aragorn is blocked on a live livesite incident (livesite exception bypasses the pipeline)
- **All agents wired:** Elrond, Bilbo, Gimli, Aragorn, Scribe all updated with "On Failure" sections. Galadriel and Gandalf were already updated.
- **Key design principle:** Elrond writing "no solution found" IS the Jonathan notification trigger — it's the squad's final escalation gate.
- **Tracking:** `jbenami_microsoft/ms-pa#87`
- **Squad-infra audit:** Scanned coffee-ratings repo, found 11 squad-infra commits covering: ralph-watch watchdog (install, CLI args, config, pre-flight checks, Phase 1), spawn templates (lightweight spawn mode), governance consolidation, and model reference audits.
- **Audit pipeline:** Created 5 issues (#7–#11) as a dependency chain: Elrond researches → Bilbo documents → Gandalf decides PORT/SKIP/ADAPT → Gimli ports → Bilbo creates reusable squad template.
- **Bobbie charter study:** Found Bobbie's charter at `C:\code\dev\coffee-ratings\.squad\agents\bobbie\charter.md`. Role is Tester with strong PR review fix workflow, ownership-first model (original author fixes), and escalation after 3+ cycles.
- **Reviewer hire:** Created 2 issues (#12–#13) to research Bobbie's patterns and hire a LotR-cast Reviewer agent for ms-pa. Fills the gap — team has Lead, Researcher, Documentarian, Tool Builder, Operator but no Reviewer.
- **Labels created:** `squad:elrond` (blue, 1d76db), `squad:bilbo` (green, 0e8a16), `squad:gandalf` (yellow, fbca04) on ms-pa repo.
- **Issues created:** #7–#13 on `jbenami_microsoft/ms-pa`, all labeled `squad` + agent-specific labels.
