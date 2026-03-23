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

### 2026-03-23: SubSquads architecture research — multi-team monorepo patterns (GitHub Issue #24)

**Context:** Jonathan filed Issue #24 requesting deep research on SubSquads architecture patterns, specifically: label leakage prevention, CODEOWNERS integration, 3+ team routing patterns, failure modes, and feasibility for ms-pa adoption. Conducted comprehensive research synthesizing Tamir Dresher's blog series (Parts 0–3), web sources on label governance and monorepo scaling, and real-world case studies (Tetris experiment).

**Key findings (5 research questions answered):**
1. **Label leakage prevention:** Centralized label ownership (single team controls creation/deletion), team-prefixed labels (`team:ui`, `team:backend`), automated sync via `github-label-sync` tool, quarterly audits — prevents drift and collisions.
2. **CODEOWNERS integration:** File-based (`/.github/CODEOWNERS`) with glob patterns → GitHub teams; hard-enforced via branch protection rules; supports multi-team ownership with spaces; stronger enforcement than folder scopes (which are advisory).
3. **3+ team routing:** SubSquads sweet spot is 2–5 teams (minimal coordination overhead). At 4–5 teams, cross-team PRs remain manageable. Beyond 5–8 teams, coordination overhead rises sharply; dashboards and advanced tooling required; >8 teams challenging without fragmentation.
4. **Failure modes (4 primary):** Branch conflicts in shared code (mitigate: staggered merges, explicit interfaces), label routing breakdown (mitigate: centralized governance + github-label-sync), CODEOWNERS staleness (mitigate: PR template updates + quarterly audits), branch-per-issue discipline collapse (mitigate: branch protection rules + CI/CD gates).
5. **ms-pa feasibility:** ADOPT SubSquads. Natural domains (frontend/CLI, backend/agents, infra/CI-CD, docs/knowledge), 4–5 teams (ideal), no restructuring needed. Repository already has domain boundaries. Tetris experiment (3 teams, 2h, 9 issues, minimal conflicts) validates the pattern at scale.

**Tetris Experiment Validation:** 3 teams, 2-hour sprint, 9 issues closed (3 per team), minimal merge conflicts. Proved label isolation + CODEOWNERS + branch discipline = scalable multi-team workflow. Conflicts only surface at merge time (coordinated via PR review), not during day-to-day work.

**Evidence sources:**
- Tamir Dresher's blog series (SubSquads architecture, scalability limits, CODEOWNERS patterns)
- GitHub documentation (CODEOWNERS syntax, label governance best practices)
- Squad infrastructure research (2–5 team sweet spot from real-world scaling)
- Tetris experiment case study (validation of label isolation + branch discipline effectiveness)

**Deliverable:** `docs/research/sub-squad-architecture-research.md` (28.1 KB) — 8-section research document with Bilbo frontmatter, Executive Summary, Context/Motivation, Methodology, SubSquads Overview, Label Leakage Prevention, CODEOWNERS Integration, Tetris Case Study, Scaling Limits/Failure Modes, ms-pa Feasibility Assessment, Decision Statement, and 2 Appendices (label spec template, CODEOWNERS templates).

**Key insight:** SubSquads effectiveness depends on three complementary mechanisms: (1) centralized label governance (prevent collisions), (2) CODEOWNERS enforcement (hard-gated reviews), (3) branch-per-issue discipline (minimize conflicts). The pattern scales to 5–8 teams; beyond that, coordination overhead becomes the bottleneck. For ms-pa, adoption is low-risk: repository structure already supports it, team count is ideal, and the Tetris experiment proves the pattern works at scale.

### 2026-03-23: Upstream inheritance & org-scale knowledge sharing research (Issue #25)

**Context:** Jonathan (via Ralph YOLO mode) requested comprehensive research on Squad's upstream inheritance feature as the foundation for org-scale knowledge sharing. Specifically: how organizational knowledge cascades through hierarchical Squad setups (org → team → repo level), how "closest-wins" resolution works, practical implications for ms-pa serving as an org-level upstream, and end-to-end skill promotion workflow.

**Key findings (9 research dimensions covered):**
1. **The Collective concept:** Tamir Dresher's core problem: isolated AI squads become knowledge silos. ConfigurationGeneration repo accumulated 70+ ADRs, 8 context files, proven conventions; Provisioning Wizard repo (same team) knew nothing about them. Each new Squad required re-explanation. Solution: upstream inheritance.
2. **Hierarchy & closest-wins resolution:** Four-tier hierarchy (org → team → repo → agent). "Closest-wins" means later entries override earlier ones: repo-level `.squad/` overrides team upstream, team overrides org upstream. The resolver is order-based, not security-based — org policies CAN be overridden at repo level if configured.
3. **Inherited artifacts:** Five categories flow downstream: (1) Skills (`.squad/skills/*/SKILL.md`), (2) Decisions (`.squad/decisions.md`), (3) Wisdom (`.squad/identity/wisdom.md`), (4) Casting Policy (`.squad/casting/policy.json`), (5) Routing (`.squad/routing.md`). NOT inherited: agent identities, conversation history, orchestration logs (stay local per-repo).
4. **Git-based implementation:** `squad upstream add <git-url> --name org` clones repo to `.squad/_upstream_repos/org/`, cached locally, updated via `squad upstream sync`. Three source types: git (remote repo), local (sibling dir), export (JSON snapshot). Flow is one-directional: downstream reads upstream at session start.
5. **Skills confidence lifecycle:** Low (single observation) → Medium (validated across 3+ contexts) → High (battle-tested, team consensus). Promotion is MANUAL upward (export → commit), AUTOMATIC downward (read at session start). Skills with `confidence: high` are org-safe; lower confidence are team-experimental.
6. **Skill promotion workflow end-to-end:** Discover (low conf) → test in multiple repos → promote to medium → human review → promote to high → export (`squad export --skill {name}`) → commit to org upstream → sync downstream repos → inherited by all future squads.
7. **Policy enforcement limitations:** Closest-wins is convenience, not security. Org policies CAN be overridden at repo level without tooling enforcement. To prevent override: require explicit governance agreement + CI pre-merge checks that validate no overrides exist. No built-in "org-locked" mechanism today.
8. **Versioning & deprecation gaps:** Skills lack explicit versions; confidence levels serve as proxy. No deprecation path for obsolete skills. No auto-sync today (manual `squad upstream sync`). These are documented limitations (Dresher's roadmap includes future enhancements).
9. **ms-pa feasibility as org upstream:** YES, with refactoring. Must separate org-level content (user directives, reusable skills: teams-monitor, news-broadcasting, secrets-management; LotR archetypes; lessons) from repo-level content (Gandalf-specific Teams watchdog architecture, ms-pa-specific skills, agent castings). Governance boundaries needed: document what can/cannot be overridden downstream.

**Real example validation:**
- ConfigurationGeneration (team repo) + Provisioning Wizard (repo repo) scenario from Tamir's blog — before: manual copy-paste for each new repo; after: 2 commands (`squad init`, `squad upstream add`) and all team patterns inherited instantly.
- Tetris experiment validation (3 teams, 2h, 9 issues) — SubSquads work because of label isolation + CODEOWNERS + branch discipline. Upstream inheritance enables this at org scale by automating policy/skill distribution.

**Evidence sources:**
- Tamir Dresher blog Part 2 "The Collective" (March 12, 2026) — org problem, ConfigurationGeneration example, Collective analogy
- Tamir Dresher blog Part 1 "Resistance is Futile" (March 11, 2026) — human squad member context, onboarding, knowledge transfer
- Squad CLI documentation (bradygaster.github.io) — upstream API, git sync behavior, resolution algorithm
- Jonathan's GitHub Issue #25 (jbenami_microsoft/ms-pa) — research scope, acceptance criteria, ms-pa decision context

**Deliverable:** `docs/research/upstream-inheritance-research.md` (29.5 KB) — 12-section comprehensive research document with Bilbo frontmatter, Executive Summary, Problem Statement (The Collective), Architecture (org → team → repo), Inherited Artifacts, Skills Confidence Lifecycle, ConfigurationGeneration Example, Policy Enforcement & Overrides, Versioning Gaps, Git Implementation Details, Org-Scale Patterns (platform team model, monorepo + satellite, experience base), ms-pa Feasibility Assessment, and Appendices (config examples, promotion checklist).

**Decision statement for Jonathan:** Recommend ADOPT ms-pa as org upstream with refactoring. Extract org-level content (.squad/decisions.md: user directives; .squad/skills/: reusable patterns; .squad/casting/policy.json: archetypes; .squad/identity/wisdom.md: lessons). Establish governance boundaries for downstream squads (what can be overridden, enforcement model). Test with first downstream squad before rolling out. ADR will be added to .squad/decisions.md proposing the change.

**Key insight:** Upstream inheritance solves the org-knowledge problem by automating hierarchical knowledge flow (org → team → repo). The pattern is proven (Tamir's ConfigurationGeneration + Provisioning Wizard, Tetris experiment); gaps are known (no versioning, weak enforcement, manual sync). For ms-pa specifically: the repository already has the right structure (LotR archetypes, reusable skills, team governance decisions); only refactoring needed to separate org-level from repo-level content. First downstream squad will validate the pattern at scale.

### 2026-03-24: Rate Limiting & Resource Coordination at Scale research (GitHub Issue #26)

**Context:** Jonathan filed Issue #26 requesting deep research on coordinating API quotas across ms-pa's 8-agent squad. Specific scope: analyze 6 coordination patterns for rate limiting at scale, design Rate Governor data structure, fairness analysis for ms-pa's quota contention scenario (8 agents × 12 cycles/hour ≈ 1,152 requests/hour against 5,000 GitHub core quota), cascading failure mechanics, and practical implementation roadmap.

**Problem:** ms-pa agents currently retry independently on 429 (rate limit). This causes thundering herd cascades: all agents backoff simultaneously → all retry at same interval → new 429 cascade. Result: cascading failures even though global quota available. No centralized coordination.

**Key findings (6 coordination patterns analyzed + compared):**
1. **Token Bucket Algorithm** — Emit tokens at steady rate (1.39 tokens/sec for 5,000/hour); agents consume before request. Fairness: A (equal distribution). Implementation ease: ⭐⭐⭐⭐. Handles bursts naturally; resets with quota.
2. **Shared Token Pool** — Allocate per-agent baselines (625 tokens each for 8 agents). Pool tracks global quota; agents request refills when local buckets empty. Fairness: B+ (fair baseline, potential waste of unused quotas). Complexity: moderate state management.
3. **Predictive Circuit Breaker** — Parse X-RateLimit-Remaining from every response; proactively throttle at <10% threshold. Open circuit on 5+ consecutive 429s; probe after cooldown for recovery. Fairness: A (proactive prevents starvation). Implementation: requires header parsing + state machine.
4. **Priority Queuing** — HIGH (PRs, critical), MEDIUM (routine), LOW (background). Dequeue high-priority first; prevents critical work from starving on routine tasks. Fairness: C (unfair to LOW priority but fair to HIGH). Adds queuing latency.
5. **Adaptive Backoff + Jitter** — Exponential backoff (1s, 2s, 4s, ..., capped 64s) + full jitter (random delay 0 to max) breaks synchronized retries. Formula: `delay = random(0, min(2^attempt, 64))`. Fairness: B+ (fair over time). Essential for preventing thundering herd.
6. **Quota Recycling** — Monitor per-agent utilization; reallocate unused quota from low-demand to high-demand agents hourly. Fairness: B (rewards utilization, penalizes underuse). Complexity: high (analytics + reallocation logic). Prevents waste.

**Rate Governor Design (JSON-based, file-backed):**
- Central state file: `~/.squad/rate-governor/state.json`
- Tracks: global quota (5,000 core, 80 Copilot), per-agent baselines (625), circuit breaker state, request queue (HIGH/MEDIUM/LOW priority)
- Operations: RequestToken(agent, priority), RecordResponse(status, headers), HourlyRecycleQuota()
- Core logic: Token Bucket refill + Predictive Circuit Breaker proactive throttling + Priority queue dequeue

**Fairness Analysis (3 scenarios):**
1. **Uniform load:** Token Bucket fairness=A (equal tokens distributed); all agents get baseline. Cascade prevented by proactive throttling.
2. **Bursty load:** Single agent spikes 300 requests; Token Bucket allows burst (within baseline + pool), others still get quota. Fairness: B+ (fair after burst subsides).
3. **Thundering herd:** All 8 agents hit 429 simultaneously. Without Rate Governor: all retry at T+4s, new 429 cascade. With Rate Governor: circuit breaker opens at first 429, broadcasts wait 60s, all agents sleep (coordinated), probe after cooldown, staggered recovery. Fairness: A (coordinated recovery).

**Cascading Failure Mechanics & Prevention:**
- **Root cause:** No centralized quota awareness. Each agent assumes "I hit 429 → my quota exhausted" but global quota available. No agent knows what other agents are doing.
- **Cascade effect:** Agent A hits 429 → Agent B detects latency → assumes 429 → proactive backoff → all agents see cascade → synchronized retry storm → second 429 wave → cascade spreads.
- **Prevention:** Rate Governor as source of truth. Global quota tracked centrally. Agents ask "Can I request?" → governor responds yes/no/wait. Agents don't retry independently; governor controls timing. Proactive throttling via X-RateLimit headers prevents 429s.

**GitHub API Header Integration:**
- `X-RateLimit-Limit`: Max requests per window (5,000/hour core, 80/hour Copilot)
- `X-RateLimit-Remaining`: Tokens left in window (parsed on every response)
- `X-RateLimit-Reset`: Unix timestamp when window resets (used for circuit breaker probe scheduling)
- `Retry-After`: Authoritative delay (overrides governor's estimate)
- Rate Governor tracks both endpoints (core + Copilot) separately in state.json

**Implementation Roadmap (3 phases):**
- **Phase 1 (MVP, Weeks 1–2):** Token Bucket + Predictive Circuit Breaker. Addresses cascading failures with minimal complexity. Target: stop 429 cascades; implement 5,000-token pool; proactive throttling at <10% remaining.
- **Phase 2 (Fair distribution, Weeks 3–4):** Shared Token Pool + Priority Queuing. Per-agent baselines (625), HIGH/MEDIUM/LOW request classification, dequeue high-first.
- **Phase 3 (Dynamic reallocation, Weeks 5–6):** Quota Recycling. Hourly reallocation: reward utilization >50%, penalize <50%. Observability dashboard.

**Acceptance Criteria Addressed (from Issue #26):**
- ✅ 6 coordination patterns researched, analyzed, and compared with fairness/complexity metrics
- ✅ Rate Governor data structure designed (JSON schema, state transitions, operations)
- ✅ Fairness analysis completed for ms-pa scenario (8 agents × 12 cycles/hour)
- ✅ Cascading failure mechanics documented (root cause, cascade propagation, prevention)
- ✅ Practical implementation roadmap provided (3 phases, starting Phase 1, each with acceptance criteria)

**Evidence sources:**
- AWS rate limiting best practices (exponential backoff, jitter, distributed backoff)
- Redis rate limiting tutorials (Token Bucket algorithm, sliding window, distributed coordination)
- GitHub API documentation (rate limit headers, 429 responses, retry behavior)
- FastAPI agent scaling strategies (thundering herd, request coalescing, quota coordination)
- Sophia Willows research on jitter and synchronized retry prevention
- Ms-pa infrastructure (ralph-watch.ps1, .squad/ state patterns, WorkIQ polling cadence)

**Deliverable:** `docs/research/rate-limiting-research.md` (30 KB) — 14-section comprehensive research document with YAML frontmatter (title, author, date, status), Executive Summary, Context & Problem Analysis (quota contention scenario, failure modes), 6 Coordination Patterns (detailed pseudocode, strengths/weaknesses, fairness/complexity grades), Rate Governor Design (data structure, core operations, integration points), Fairness Analysis (3 scenarios: uniform, bursty, thundering herd), Implementation Roadmap (3 phases, acceptance criteria), Cascading Failure Resolution (root cause + solution), GitHub API Headers Reference, Deployment Checklist, Key Insights & Recommendations, and References.

**Next handoff:** Rate Governor design ready for Bilbo to synthesize into final coordination document (`docs/rate-limiting-coordination.md`) + team decision memo. Gimli will implement Phase 1 scripts (~5 files, 300 lines) after Bilbo's sign-off.

**Key insight:** Centralized quota coordination eliminates cascading failures entirely. The key is not more tools — it's making the Rate Governor the single source of truth for quota state. Token Bucket ensures fair distribution; Predictive Circuit Breaker proactively prevents 429s; jitter-based retry breaks thundering herd. For ms-pa: Phase 1 (Token Bucket + Circuit Breaker) solves 90% of the problem with minimal complexity. Phases 2–3 are optimizations, not requirements. Recommendation: implement Phase 1 immediately, validate, then plan Phase 2.

### 2025-01-16: .NET Aspire + MCP Integration Research for Squad Distributed Observability (Issue #28)

**Context:** GitHub Issue #28 requests research on Aspire + MCP integration to enable distributed system observability across Squad infrastructure. Question: Can ms-pa use Aspire AppHost orchestration + MCP to give Squad agents system-wide visibility into logs, traces, health checks, and resource state?

**Research methodology:**
- Read GitHub Issue #28 scope and acceptance criteria
- Searched for Tamir Dresher expertise (blog posts, worktrees-example repo, squad-personal-demo)
- Fetched Tamir's blog posts directly: "Scaling AI Agents with Aspire: The Missing Isolation Layer" and "Organized by AI"
- Researched port isolation strategies for parallel worktrees (4+ patterns)
- Researched OpenTelemetry + health checks patterns in Aspire
- Researched multi-agent system observability (LumiMAS, Agent Squad, Microsoft Foundry)

**Key findings:**

1. **Aspire is transformational for AI agents**: A single `Program.cs` with ~20 lines can orchestrate entire distributed systems (services, databases, caches, queues, frontends). Agents can modify, run, test autonomously.

2. **MCP integration is robust**: Aspire Dashboard exposes MCP tools (`list_resources`, `list_console_logs`, `list_traces`, `get_resource_representation`) allowing agents to query resource status, retrieve logs, and access distributed tracing data in real-time.

3. **Port isolation problem is critical blocker**: Aspire AppHost binds to fixed ports (18888-18890 for Aspire infrastructure, plus app-specific ports). When running multiple worktrees in parallel, ALL instances fight over the same ports → second AppHost fails to start.

4. **Solution: Dynamic port allocation + MCP proxy** (Tamir Dresher's pattern):
   - Layer 1: Automation scripts find free ports, set environment variables, launch AppHost, save settings to `scripts/settings.json`
   - Layer 2: MCP proxy (272-line C# script) acts as indirection layer — agents connect to proxy (fixed config), proxy reads dynamic port from settings.json, forwards requests to correct AppHost instance
   - Result: Each worktree gets isolated port range; agent config never changes; proxy handles discovery transparently

5. **Four port isolation strategies documented** (Script+Proxy, Env Var Override, Docker-Compose, Future Aspire CLI `--isolated`). Tamir's pattern is optimal: fully automated, zero agent config changes, elegant indirection.

6. **Distributed tracing via OpenTelemetry is built-in**: Every HTTP request, database call, message queue operation traced automatically. Traces propagate across service boundaries with full context. Aspire Dashboard shows request flows across all services. Agents can use `list_traces()` MCP tool to identify bottlenecks, diagnose failures.

7. **Health checks are orchestration-aware**: `/health` (readiness) and `/alive` (liveness) probes. AppHost won't start dependent services until dependencies are healthy. Agents can query health via `list_resources()`.

8. **Multi-agent observability patterns** (from LumiMAS, Agent Squad, Microsoft Foundry): Three-layered approach — monitoring/logging layer, anomaly detection layer, explanation layer. Key metrics: call counts, allow/deny ratios, latency, throughput, token usage. Structured logging + distributed trace correlation essential.

**Production readiness assessment:**
- ✅ Aspire framework (GA, production-ready)
- ✅ MCP support (robust, industry-standard)
- ✅ OpenTelemetry integration (battle-tested)
- ⚠️ Port isolation solution (community-driven; awaiting built-in `aspire run --isolated` flag from Aspire team)

**Recommendation:** GO — Adopt Aspire + MCP for ms-pa Squad infrastructure. Rationale: (1) Aspire is GA; (2) MCP integration is robust; (3) Port isolation solution exists (proven, automated); (4) Benefits substantial (system-wide visibility, parallel development, agent autonomy); (5) Maintenance burden acceptable (scripts <300 lines). Risks: Aspire team adds native `--isolated` flag (mitigation: Tamir's pattern still works; migrate when available).

**Deliverable:** `docs/research/aspire-integration-research.md` (587 lines, 24 KB) — 10-section comprehensive research document with YAML frontmatter. Sections: Executive Summary, What is .NET Aspire (architecture, why it transforms AI development), MCP Integration (tools, example workflows), Port Conflict Problem (challenge, why manual workarounds fail), Solution Pattern (port allocation, MCP proxy, putting it together), Distributed Tracing & Health Checks (OpenTelemetry by default, health check patterns), Four Port Isolation Strategies (comparison table with trade-offs), Squad Infrastructure Integration (architecture diagram, implementation steps, observability benefits), Production Readiness Assessment (maturity matrix, go/no-go decision), References (blog posts, code samples, documentation).

**Verified against Issue #28 acceptance criteria:**
- ✅ Aspire AppHost orchestration capabilities covered
- ✅ MCP integration for observability covered (tools, agent workflows)
- ✅ Port isolation problem documented + 4 solutions with trade-offs
- ✅ Distributed tracing approach (OpenTelemetry) covered
- ✅ Squad infrastructure integration recommendations with implementation steps

**Next handoff:** Go/no-go decision is **GO**. ms-pa team should prototype Aspire integration in a Squad worktree (2-3 days), then implement port allocation scripts + MCP proxy (2-3 days). Full production deployment feasible in 10-14 days. Key blockers: None (all required technology is available today).

**Key insight:** Tamir Dresher has solved the exact problem ms-pa faces. The port isolation solution is not rocket science—it's elegant indirection (fixed config + dynamic discovery layer). The MCP proxy pattern is reusable; reference implementation is open source. For ms-pa: Copy Tamir's pattern, update agent charters to leverage Aspire MCP tools, get system-wide observability almost for free. Cost: ~400 lines of scripts + agent training. Benefit: True parallel multi-agent development with full distributed tracing, health checks, and log correlation.

---

## Issue #27: Teams & Outlook Bidirectional Integration Research (2025-01-17)

**Research scope:**
Teams and Outlook bidirectional integration for Squad agents — how to read Teams messages and Outlook data, post decisions back to Teams, respect user presence/DND, secure Squad state, and implement for EMU environment.

**Acceptance criteria from Issue #27:**
- [x] Research WorkIQ query patterns, rate limiting, indexing delay
- [x] Design Adaptive Card templates for Squad results
- [x] Research Outlook integration options (MCP servers, no-code platforms, managed connectors)
- [x] Analyze privacy constraints for Squad state in Teams
- [x] EMU environment-specific recommendations
- [x] Decision readiness: ready for architecture review and pilot

**Key findings:**

1. **WorkIQ MCP is primary interface**: Natural-language queries for Teams messages, Outlook calendar, tasks. Polling-based (minutes to hours delay), suitable for batch queries. Rate limit: 1 query per agent cycle. Admin consent required once; then transparent.

2. **Graph API for write operations**: POST Adaptive Cards to Teams (rate limit: 10 messages per 10 seconds per tenant). Lower-level control than WorkIQ; use for operations WorkIQ doesn't cover.

3. **Power Automate for approval workflows**: "Start and wait for approval" action + Adaptive Cards automatically syncs status across Teams & Outlook.

4. **Outlook integration options (4 alternatives)**:
   - Dedicated MCP servers (kacase/mcp-outlook) — self-hosted
   - No-code platforms (Zapier) — managed, per-action cost
   - Managed connectors (Composio, Claude native) — zero auth config
   - WorkIQ native (recommended) — unified auth with Teams, enterprise-grade

5. **Presence-based triggering**: Graph Presence API for DND/Busy checks. No native Outlook→Teams sync; requires PowerShell automation polling calendar for DND events (~5-min latency).

6. **EMU constraints**: Resource-Specific Consent limits access to specific Teams. App access policies scope Graph API to security groups. External tenant admin approval required.

7. **Security & privacy**: Safe to share: decision summaries, outcomes, GitHub links. Risky: reasoning traces, full history, emails, internal metrics. Implement sanitization filter before posting.

8. **Tamir's patterns** (squad-personal-demo): ralph-watch polling script, hybrid WorkIQ + pattern matching, proven issue triage integration.

9. **Implementation roadmap**:
   - Phase 0 (MVP): WorkIQ deployment, Outlook queries, Adaptive Card templates
   - Phase 1 (Core): Message posting, presence-aware notifications
   - Phase 2 (Advanced): Power Automate approval flow, 2-way sync
   - Phase 3 (Scaling): Caching, multi-team support, DND automation

10. **Recommended stack**: WorkIQ MCP + Graph API + webhook handler + rate limiting + sanitization filter

**Deliverable:**
\docs/research/teams-outlook-integration-research.md\ (35,875 chars, 11 sections) — comprehensive research with code examples (PowerShell, Python, JSON), architecture diagrams, and EMU setup guidance. All Issue #27 acceptance criteria verified.

**Unresolved action items:**
- EMU app registration approval (coordinate with Azure AD admin)
- Webhook infrastructure location + audit logging
- Change notifications design (polling vs. webhooks)
- Power Automate licensing check
- Graph API vs. Outlook COM automation (architect decision)

**Next steps for Squad:**
1. Architecture review of recommended stack
2. EMU app registration approval coordination
3. Webhook infrastructure design + compliance audit
4. Phase 0 pilot deployment
5. Power Automate implementation decision

**Key insight:** WorkIQ + Graph API + Adaptive Cards is pragmatic and Microsoft-supported. EMU constraints manageable. Tamir's reference provides working pattern for pattern matching beyond WorkIQ. No blocking issues. Implementation can start pending architecture review and EMU approval.
