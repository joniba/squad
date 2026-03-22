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
