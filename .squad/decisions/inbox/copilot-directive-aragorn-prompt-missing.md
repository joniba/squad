### 2026-03-23T01:45:51Z: User directive
**By:** Jonathan (via Copilot)
**What:** Aragorn (and the ICM investigator skill) must PROMPT Jonathan if any integrations are missing — missing tokens, MCP servers not configured, Kusto cluster access denied, Geneva account not found, eng.ms auth failures, etc. Never silently skip a tool or list queries for the user to run. If a tool fails, tell Jonathan what's missing and how to fix it.
**Why:** User request — Aragorn was listing Kusto queries instead of running them because he silently failed on access. Jonathan wants to be prompted so he can fix the integration, not work around it.
