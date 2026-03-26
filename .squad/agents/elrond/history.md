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

