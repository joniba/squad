# Squad Decisions

## Active Decisions

### 2026-03-23T14:35:00Z: ICM Scan Watermark Design (Approved)
**Author:** Gandalf  
**Status:** Approved  
**Issue:** #92

**Context:** ICM scan currently queries ALL active incidents every time, wasting LLM calls on already-processed incidents. Needs watermark-driven query window to track scans and avoid reprocessing.

**Decision:**
- **Location:** `.squad/icm-scan-watermark.json` (gitignored, machine-local state)
- **Schema:** `{ lastScan (ISO 8601), seenIds (array, capped at 200), version: 1 }`
- **seenIds rotation:** FIFO cap at 200 entries (simple, keeps file ~7 KB)
- **Query window:** `now() - lastScan + 1h buffer`, capped at 7 days, 24h default on first run
- **Known IDs → LLM:** Merge watermark seenIds + investigation report IDs + GitHub issue IDs, pass to prompt to skip them
- **Corruption handling:** Log warning, delete corrupted file, start fresh (ephemeral state, safe to discard)
- **-Reset flag:** Clears watermark, next scan reverts to 24h default

**Rationale:**
- Gitignore: Machine-local tracking state, not shared config
- FIFO rotation: Simple, efficient, sufficient for deduplication window
- 1h buffer: Handles race conditions between scan cycles
- 7-day cap: Handles offline machines gracefully
- LLM-level dedup: Single source of truth in prompt prevents reprocessing

**Tracking:** Issue #92, assigned to Gimli (squad:gimli) for implementation, Galadriel for review.

### 2026-03-22T17:30:00Z: User directive — copilot CLI usage
**By:** Jonathan (via Copilot)  
**What:** `gh copilot` does not work. Must use vanilla `copilot -p` or `copilot -i` commands instead. Cannot save their result to a variable.  
**Why:** User request — captured for team memory

### 2026-03-22T17:29:59Z: User directive — scripting philosophy
**By:** Jonathan (via Copilot)  
**What:** Scripts should be short and concise and combined to create more complex functionality, rather than one big complex script. Build gradually, making sure each step works using real tests (no mocks) before adding additional complexity. Separate into small tasks.  
**Why:** User request — captured for team memory

### 2026-03-22T17:30:00Z: Teams Watchdog Architecture (Proposed)
**Author:** Gandalf  
**Status:** Proposed  

**Context:** Jonathan wants a daily Teams message watchdog that summarizes his decisions, action items, and important context from Teams conversations.

**Decision:** Decompose into a **6-step pipeline** of composable PowerShell scripts, not a monolithic watchdog:
1. **Probe** — Fetch raw Teams messages via WorkIQ MCP tool
2. **Filter** — Extract only Jonathan's sent messages
3. **Extract** — Identify decisions, action items, commitments
4. **Format** — Build a clean daily markdown summary
5. **Schedule** — Watchdog loop (based on ralph-watch.ps1 pattern)
6. **Deliver** — Post to Teams + archive to disk

Each step is a separate script under `.squad/skills/teams-watchdog/`, chained via file I/O (because `copilot -p` output cannot be captured in variables).

**Constraints Honored:**
- Scripts are short, composable, under 30-50 lines each
- Uses `copilot -p` (not `gh copilot`)
- File-based I/O between steps (no variable capture)
- Each step testable independently with real data

**Tracking:** Issues #1-#6 on `jbenami_microsoft/ms-pa`, labeled `squad` + `squad:gimli` (assigned to Gimli for implementation).

### 2026-03-22T17:35:00Z: Skills Scan: squad-skills Plugin Catalog (Proposed)
**Author:** Elrond (Researcher)  
**Status:** Proposed  

**Context:** Scanned all 20 plugins in tamirdresher/squad-skills to identify relevance to Teams message watchdog (Issues #1–#6).

**Finding — Directly Useful (🟢):**
1. **teams-monitor** — Demonstrates proven WorkIQ query patterns, filtering heuristics, and rate-limiting warnings. Critical reference for watchdog Probe/Filter steps.
2. **news-broadcasting** — Documents Teams webhook delivery pattern (URL at `~\.squad\teams-webhook.url`, POST via `Invoke-RestMethod`, Adaptive Cards formatting). Directly applicable to Deliver step.
3. **secrets-management** — Establishes security foundation: Windows Credential Manager for secrets, machine-local files (priority 2), `.env` (priority 3). Essential for production-ready watchdog scripts.

**Key Insight:** WorkIQ is poll-based with indexing delay—well-aligned with our daily summary design. Rate-limit to one WorkIQ query per agent cycle.

**Recommendation:** Install teams-monitor (P0), news-broadcasting (P0), secrets-management (P1) for watchdog work. Keep agency-optimal-config, teams-ui-automation, mail-mcp on radar for Phase 2.

**Tracking:** Findings documented in `docs/squad-skills-catalog.md` (Bilbo's catalog, 25.2 KB).

### 2026-03-22T17:40:00Z: User directive — work source policy
**By:** Jonathan (via Copilot)  
**What:** Only pull tasks from the board (GitHub issues), never from the chat. All work must be created as issues first.  
**Why:** User request — captured for team memory

### 2026-03-22T17:45:00Z: User directive — clean root chat
**By:** Jonathan (via Copilot)  
**What:** Whenever Jonathan mentions creating a task or mentions Gandalf, the Coordinator MUST pass the request to Gandalf via a subagent spawn to create an issue. The root chat must stay clean — only show handoffs to the squad, not planning or decomposition work.  
**Why:** User request — captured for team memory

### 2026-03-22T17:40:00Z: Coffee-Ratings Squad-Infra Audit & Port Plan (Proposed)
**Author:** Gandalf  
**Status:** Proposed  

**Context:** Jonathan requested an audit of coffee-ratings squad-infra work to identify reusable patterns for ms-pa, plus hiring a Reviewer agent based on the coffee-ratings "Bobbie" charter.

**Decision:** Two parallel initiatives tracked as dependent issue chains on `jbenami_microsoft/ms-pa`:

**Squad-Infra Audit (5-step pipeline, Issues #7–#11):**
1. **#7** — Elrond researches squad-infra git history + GitHub issues from coffee-ratings (11 known commits)
2. **#8** — Bilbo documents findings with links, diffs, and category groupings
3. **#9** — Gandalf decides PORT/SKIP/ADAPT for each item with reasoning
4. **#10** — Gimli ports approved changes to ms-pa
5. **#11** — Bilbo creates a reusable squad bootstrap template from ported patterns

**Reviewer Hire (2-step chain, Issues #12–#13):**
1. **#12** — Elrond studies Bobbie's charter and history, extracts generalizable patterns
2. **#13** — Gandalf hires new LotR-cast Reviewer agent adapted from Bobbie's patterns

**Reasoning:**
- Sequential dependency chain ensures full context before each step
- Bilbo's template (#11) is the most valuable long-term artifact
- Reviewer hire fills team gap (no dedicated quality gate) and applies proven patterns from mature squad

**Tracking:** Issues #7–#13 labeled with `squad` + agent-specific labels.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
