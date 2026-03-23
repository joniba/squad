### 2026-03-23T14:16:25Z: Process failure — Galadriel did not prompt on missing integration
**By:** Jonathan (via Copilot)
**What:** Galadriel reviewed ADO PR 15064785 but could not read file contents (ADO MCP lacks get_file_contents). She noted the limitation in her report but did NOT stop and prompt Jonathan. This violates the prerequisite check directive: "if a tool fails, STOP and report what's missing." The directive applies to ALL agents, not just Aragorn.
**Why:** PR reviews without file contents are incomplete. Jonathan must be prompted so the integration can be fixed, not worked around silently.
