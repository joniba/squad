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
