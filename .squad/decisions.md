# Squad Decisions

## Active Decisions

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

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
