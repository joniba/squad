## Core Context

Archived history from elrond. Preserved core metadata and most recent activity entry below. For full history, refer to git log.

---

## Learnings

### 2025-07-24: Teams MCP Landscape Research
- **"Agency Teams MCP"** is `agency mcp teams` — part of Microsoft's "Work IQ" / "Agent 365" ecosystem. Server ID: `mcp_TeamsServer`. Provides ~26 deterministic Graph API operations for Teams chats, channels, messages, and members.
- **WorkIQ** (`@microsoft/workiq`, tool: `ask_work_iq`) is "Work IQ Copilot" — a natural language query interface to M365 Copilot. Broad scope (email, calendar, files, Teams) but read-only and non-deterministic.
- **They are complementary, not competing.** Agency Teams = structured action layer. WorkIQ = intelligence/query layer.
- The `agency` CLI (same one that hosts `agency mcp icm`) has a full M365 suite: `teams`, `mail`, `calendar`, `sharepoint`, `word`. All are Work IQ MCP servers accessed via HTTP proxy with EntraID auth.
- The third-party `floriscornel/teams-mcp` npm package exists but is community-built, not the Microsoft internal one. Agency Teams MCP is the official Microsoft equivalent.
- Microsoft Learn docs at `learn.microsoft.com/en-us/microsoft-agent-365/mcp-server-reference/teams` has full API reference for all Teams MCP tools.
- **Decision filed:** `.squad/decisions/inbox/elrond-teams-mcp-comparison.md` — recommends using both Agency Teams MCP (for actions) and WorkIQ (for intelligence), adding `agency mcp teams` to our MCP config.

### 2025-07-24: Deep Research — Work IQ Teams MCP (Agency Teams MCP)
- **Server identity confirmed:** Server ID `mcp_TeamsServer`, display name "Work IQ Teams", scope `McpServers.Teams.All`. NOT an npm package — cloud-hosted MCP server at `agent365.svc.cloud.microsoft` with HTTP/SSE transport + OAuth 2.0.
- **Full tool inventory documented:** 26 tools — 12 chat tools + 14 channel/team tools. All message ops are plain text only (no Adaptive Cards).
- **Auth model:** Entra ID OAuth 2.0 delegated (OBO), scope on Agent 365 API app `ea9ffc3e-8a23-4a7d-836d-234d7c7565c1`. Requires M365 Copilot license + Frontier program (preview).
- **No existing config in MDC-AI-Shared reference codebase.** Only `workiq.json` exists (different tool).
- **Cannot replace our webhook scripts today** — preview only, no Adaptive Cards, heavy auth. But enables new bidirectional capabilities.
- **Hybrid recommendation:** Keep webhooks for outbound, add MCP for inbound reading when GA. Filed to `.squad/decisions/inbox/elrond-agency-teams-deep.md`.

