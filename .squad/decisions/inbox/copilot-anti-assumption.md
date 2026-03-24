### 2026-03-24T22:55:00Z: Coordinator anti-assumption rule
**By:** Jonathan (directive)
**What:** The coordinator MUST NOT declare issues "blocked on external access" without first attempting to use the required tools. When Ralph encounters an issue that needs MCP tools (IcM, WorkIQ, Geneva, etc.), the coordinator MUST:
1. Check if the MCP tool is available in the current session (scan tool list)
2. If available: spawn the agent immediately — it's NOT blocked
3. If unavailable: only THEN fire a blocked notification
Never assume a tool is unavailable. Verify first, report blocked second.

**Root cause:** Coordinator incorrectly blocked #136, #146, #111 for the entire session despite IcM MCP and WorkIQ MCP being available. Three issues sat idle for hours because of an assumption.

**Why:** Prevents the coordinator from inventing blocker states that don't exist. The rule is: try first, block second.
