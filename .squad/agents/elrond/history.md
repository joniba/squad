# Project Context

- **Owner:** Jonathan
- **Project:** pa-squad — a personal-assistant squad for everyday tasks, research, documentation, tool building, and livesite support
- **Stack:** General-purpose (not a single-stack project)
- **Created:** 2026-03-22

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-03-22: Skills scan of tamirdresher/squad-skills plugins

**Context:** Scanned all 20 plugins in the squad-skills repo to identify which are relevant to the Teams watchdog project (Issues #1–#6).

**Key findings:**
- **teams-monitor** (🟢) — The closest existing skill to our watchdog. Uses `workiq-ask_work_iq` to query Teams messages, has working query templates, documents limitations (polling, not real-time; indexing delay). Critical reference for Probe/Filter steps.
- **news-broadcasting** (🟢) — Documents Teams webhook delivery pattern: read URL from `~\.squad\teams-webhook.url`, POST JSON via `Invoke-RestMethod`. Directly applicable to Deliver step.
- **secrets-management** (🟢) — Establishes how to store/access secrets (webhook URL, tokens) via Windows Credential Manager. Needed for production-ready scripts.
- **agency-optimal-config** (🟡) — Reference for MCP server configuration including Teams MCP and WorkIQ.
- 15 other plugins assessed as not relevant (blog-writing, chrome-devtools-mcp, cross-machine-coordination, gh-auth-isolation, github-distributed-coordination, github-multi-account, github-project-board, incident-response, restart-recovery, session-recovery, squad-email-headless, etc.)

**Key insight:** WorkIQ is poll-based with indexing delay — well-suited for our daily batch summary design. Rate-limit yourself to one WorkIQ query per agent cycle.

### 2026-03-22: ICM Investigator skill deep analysis & Aragorn gap assessment

**Context:** Jonathan was unhappy with Aragorn's investigation of ICM 766712513 — the report described the incident but didn't actually investigate it. Analyzed Jonathan's existing ICM Investigator skill (15 files: 6 sub-agents, 3 commands, 3 reference docs) to identify what Aragorn is missing.

**Key findings:**
- The ICM Investigator skill uses a **5-stage pipeline** (Context Loading → Enrichment → Triage → RCA → Remediation) with iterative refinement (max 9 iterations). Aragorn does a single ad-hoc pass.
- **Critical gap #1:** Aragorn never executes Kusto queries — he lists pre-built queries from incidents but doesn't run them. The `azure-mcp-kusto` tool is available and should be used.
- **Critical gap #2:** Aragorn never reads TSG content — he finds TSGs via `enghub-search` but doesn't use `enghub-fetch` to read them. TSGs are the single most valuable investigation resource.
- **Critical gap #3:** Aragorn doesn't classify incidents (true positive / false positive / noise), doesn't form hypotheses, doesn't build causal chains, and doesn't assign confidence levels to root cause determinations.
- **Critical gap #4:** Geneva MCP tools (`geneva-mcp-server-query_timeseries`) are available but unused — Aragorn can't verify the metric anomaly that triggered the incident.
- **Critical gap #5:** AppLens, Resource Health, and Azure Monitor tools are available but unused.
- **Critical gap #6:** Remediation recommendations are vague (no effort estimates, no owners, no verification criteria).

**Deliverables:**
- `docs/aragorn-icm-capability-upgrade.md` — 640-line exhaustive gap analysis with step-by-step example
- `.squad/decisions/inbox/elrond-aragorn-icm-upgrade.md` — Decision memo proposing charter upgrade

**Key insight:** The difference between a shallow and deep ICM investigation is not more tools — it's executing queries instead of listing them, reading TSGs instead of linking them, and forming hypotheses instead of transcribing metadata. The methodology matters more than the tool count.

### 2026-03-22: Worktree lifecycle research for parallel agent execution (Issue #59)

**Context:** Jonathan reported workspace corruption when 8 squad agents spawned in parallel on 2026-03-22 — despite successful PR pushes, local branches were corrupted. Investigated root cause and designed worktree-based isolation strategy.

**Problem:** Squad agents spawn via `.squad/templates/squad.agent.md` (line 609+) using generic prompt that does NOT include `WORKTREE_PATH`. All agents race in same working tree executing `git checkout -b squad/{issue}-{slug}`, final tree state corrupts with stashes and uncommitted files.

**Root cause:** Gap between documented strategy (squad.agent.md lines 562–600 document worktree awareness) and enforced behavior (spawn template lines 609–650 don't create worktrees).

**Key findings (8 research questions answered):**
1. **Current flow:** Coordinator → spawn prompt → agents infer branch creation, all in same tree → race condition
2. **Minimal change:** Pre-spawn worktree creation in Coordinator; pass `TEAM_ROOT` (main checkout for `.squad/` state) and `WORKTREE_PATH` (isolated working dir) to agents
3. **WORKTREE_PATH vs TEAM_ROOT:** Complementary paths — agents work in `WORKTREE_PATH`, read state from `TEAM_ROOT`
4. **Cleanup lifecycle:** After PR merge (most reliable, requires GitHub Actions webhook or post-merge hook automation)
5. **Edge cases:** Worktree collision detection, remote branch cleanup, locked worktree handling, branch switching prevention — all addressable with `--force` flags and pre-spawn cleanup
6. **Scribe integration:** Scribe operates from `TEAM_ROOT` (main), agents write to separate inbox files (`.squad/decisions/inbox/{agent}-{slug}.md`), union merge driver handles clean merges
7. **Existing patterns:** Squad already has infrastructure (union merge in `.gitattributes`, drop-box pattern documented, branch-per-issue naming) — just not enforced in spawn template
8. **Platform constraints:** `git worktree add` fully supported on Windows; VS Code has native worktree UI; no path-length issues; works identically to Unix/Mac

**Solution:** Implement Coordinator-managed worktrees with post-merge cleanup, main-checkout strategy for shared `.squad/` state.

**Deliverable:** `docs/research/worktree-lifecycle-research.md` (42.7 KB) — comprehensive research document with implementation priorities (4 phases), edge case strategies, cleanup automation options, and Windows platform notes.

**Key insight:** Squad has all the infrastructure (merge drivers, drop-box pattern, Scribe integration) but lacks enforcement in the spawn template. A minimal 2-line change to `.squad/templates/squad.agent.md` (add pre-spawn worktree creation) fixes 4+ corrupted PR incidents and branch-drift problems completely.

### 2025-01-16: Teams channel integration research — real-time squad notifications

**Context:** Jonathan requested comprehensive research on enabling real-time squad notifications in Teams channels for blocked tasks, stale items, and significant events. Conducted 6-question deep investigation covering existing plugins, Graph API, webhooks, WorkIQ capabilities, Power Automate feasibility, and EMU-specific constraints.

**Key findings (6 research questions answered):**
1. **Integration options:** Three primary channels: Incoming Webhooks (✅ simplest, already in use), Microsoft Graph API (✅ scalable, requires app registration), Power Automate (feasible but adds abstraction layer)
2. **WorkIQ for detection:** Poll-based (not event-driven), ~minutes to hours indexing delay, proven query patterns in teams-monitor plugin, recommended cadence: 1 query per agent cycle (per squad decisions)
3. **Webhook setup & security:** Minimal 5-minute setup; store URL securely in `~\.squad\teams-webhook.url` (not git-tracked); proven delivery pattern in news-broadcasting plugin (PowerShell `Invoke-RestMethod` + Adaptive Cards)
4. **Event types:** Blocked tasks (>12h), stale items (>5 days no activity), critical incidents (Sev2, outages), unreviewed PRs (>24h), decision announcements — filter via WorkIQ keywords + GitHub API
5. **Best for EMU:** Hybrid webhook + WorkIQ polling approach (MVP-ready, no tenant admin approval, no external credentials, low operational overhead)
6. **Implementation:** 4 steps — create webhook, build skill, register scheduler, test & iterate; Phase 1 ready for launch within days

**Plugin analysis (evidence source):**
- **teams-monitor:** WorkIQ query patterns (lines 28–52), actionable filtering (lines 54–70), rate-limit guidance, webhook URL storage pattern
- **news-broadcasting:** Webhook POST via PowerShell, Adaptive Card formatting, message style guidelines
- **teams-ui-automation:** Layer-based automation (Playwright → Keyboard → UIA); reference for future UI-based integrations

**Recommendation:** Hybrid webhook + WorkIQ polling approach — production-ready, leverages existing squad infrastructure (scheduler, WorkIQ, Teams integration), requires no tenant admin approval, iterative Phase 1 → Phase 2 refinement model.

**Deliverable:** `docs/research/teams-notification-research.md` (25.2 KB) — synthesizes all 6 questions with concrete implementation steps, phased rollout (Phase 1: MVP daily summary; Phase 2: real-time alerts; Phase 3: Graph API alternative), EMU-specific guidance, and success criteria.

**Key insight:** Real-time notifications are achievable via existing squad infrastructure (WorkIQ + webhooks) without new dependencies. The hybrid poll-based approach aligns with squad's event-driven architecture and WorkIQ's indexing characteristics. Implementation is a 4-step process; Phase 1 MVP can launch within days.
