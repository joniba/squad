### 2026-03-23T14:42:40Z: User directive — Exhaust existing tools before researching alternatives
**By:** Jonathan (via Copilot)
**What:** Before any agent declares a tool/capability is missing and starts researching workarounds, they MUST first inventory all available MCP tools and test them. Elrond researched ADO file access workarounds without checking that search_code and repo_get_pull_request_by_id already existed in the ADO MCP. Galadriel did the same. Rule: try what you have → then research what you don't.
**Why:** Two agents wasted time researching workarounds for capabilities that already existed. The squad must be tool-aware before tool-seeking.
