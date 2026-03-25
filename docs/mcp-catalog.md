# MCP Server Catalog

> **Comprehensive reference for all Model Context Protocol (MCP) servers available in the Agency ecosystem and local environment.**
>
> Last updated: 2026-03-24 · Maintained by: Elrond (Researcher) · Owner: Jonathan

---

## 1. Overview

**Model Context Protocol (MCP)** is an open standard that connects AI agents (GitHub Copilot, Agency, Claude, etc.) to external tools and data sources via a structured JSON-RPC interface. Each MCP server exposes a set of **tools** that agents can invoke during conversation.

### How MCPs Work in Our Environment

```
┌─────────────┐     ┌──────────────┐     ┌──────────────────┐
│  Copilot CLI │────▶│  MCP Server  │────▶│  External System │
│  (Agent)     │◀────│  (stdio/http)│◀────│  (API/Service)   │
└─────────────┘     └──────────────┘     └──────────────────┘
```

**Three ways to provide MCPs:**

| Method | Config Location | Example |
|--------|----------------|---------|
| **Agency MCP** | `agency mcp <name>` or `agency copilot --mcp <name>` | `agency mcp ado`, `agency mcp icm` |
| **Standalone MCP** | `~/.copilot/mcp-config.json` | Azure MCP, Geneva MCP, ConfigGen MCP |
| **Platform-provided** | Built into Copilot CLI | GitHub MCP, IDE tools |

**Agency version:** `agency 2026.3.21.1` (commit: 121f7024)
**Binary:** `C:\Users\jbenami\AppData\Roaming\agency\CurrentVersion\agency.exe`
**Docs:** [https://aka.ms/agency](https://aka.ms/agency)

---

## 2. Quick Reference Table

| # | Name | Source | Category | Status | Description |
|---|------|--------|----------|--------|-------------|
| 1 | **ado** | Agency + Standalone | DevOps | ✅ Configured (3 instances) | Azure DevOps — work items, repos, pipelines, PRs, wikis |
| 2 | **bluebird** | Agency | DevOps | ⚪ Available | Engineering Copilot Mini — code search/nav across ADO repos |
| 3 | **kusto** | Agency + Azure MCP | DevOps | ✅ Configured (via azure-mcp) | Azure Data Explorer — KQL queries |
| 4 | **icm** | Agency | DevOps | ✅ Configured | ICM — incident details, impact, similar incidents |
| 5 | **cloudbuild** | Agency | DevOps | ⚪ Available | CloudBuild — trigger and monitor builds |
| 6 | **enghub** | Agency + Standalone | Knowledge | ✅ Configured | EngHub (eng.ms) — docs, TSGs, ServiceTree |
| 7 | **teams** | Agency | M365 | ⚪ Available | Microsoft Teams — chat and channel messages |
| 8 | **mail** | Agency | M365 | ⚪ Available | Outlook — read, search, send emails |
| 9 | **calendar** | Agency | M365 | ⚪ Available | Calendar — view and manage events |
| 10 | **sharepoint** | Agency | M365 | ⚪ Available | SharePoint — sites, files, lists |
| 11 | **workiq** | Standalone | M365 | ✅ Configured | WorkIQ — M365 Copilot integration |
| 12 | **athena** | Agency | M365 | ⚪ Available | Microsoft Teams Athena Assistant |
| 13 | **msft-learn** | Agency | Knowledge | ⚪ Available | Microsoft Learn — official docs search |
| 14 | **es-chat** | Standalone | Knowledge | ✅ Configured | ES Chat — engineering systems KB, diagnostics |
| 15 | **security-context** | Agency | Security | ⚪ Available | Azure Security Context — security posture |
| 16 | **s360-breeze** | Agency | Security | ⚪ Available | S360 Breeze — compliance management |
| 17 | **calculator** | Agency | Utility | ⚪ Available | Simple calculator (demo/testing) |
| 18 | **stack** | Agency | Utility | ⚪ Available | FIFO stack for agent iteration state |
| 19 | **remote** | Agency | Utility | ⚪ Available | Proxy to any HTTP MCP with EntraID auth |
| 20 | **local** | Agency | Utility | ⚪ Available | Proxy to a local MCP server |
| 21 | **npx** | Agency | Utility | ⚪ Available | Launch stdio MCP via npx |
| 22 | **azure-mcp** | Standalone | Azure | ✅ Configured | Azure MCP — 100+ tools across 30+ Azure services |
| 23 | **Ev2-MCP-Server** | Standalone | Deployment | ✅ Configured | EV2 (ExpressV2) deployment service |
| 24 | **configgen** | Standalone | Infra | ✅ Configured | ConfigGen — library APIs, examples, breaking changes |
| 25 | **geneva-mcp-server** | Standalone | Monitoring | ✅ Configured | Geneva metrics — timeseries, dimensions, handbook |
| 26 | **yoni-configgen-mcp** | Standalone | Infra | ✅ Configured | ConfigGen semantic model — types, hierarchies, patterns |
| 27 | **github-mcp-server** | Platform | DevOps | ✅ Active | GitHub — actions, PRs, issues, code search, Copilot spaces |
| 28 | **ide** | Platform | Editor | ✅ Active | VS Code — selection, diagnostics |

**Legend:** ✅ Configured = in `mcp-config.json` or active · ⚪ Available = can be enabled via `agency mcp <name>`

---

## 3. Agency MCPs (Detailed)

These are available via `agency mcp <name>` and can be added to sessions with `agency copilot --mcp <name>`.

---

### 3.1 ADO (Azure DevOps)

**What it does:** Full Azure DevOps integration — work items CRUD, repository management, pull requests, pipelines, wikis, test plans, code search, and Advanced Security alerts.

**How to enable:**
```bash
# Via Agency
agency mcp ado
agency mcp ado --organization msazure

# Via mcp-config.json (currently configured — 3 instances)
```

**Key tools:** `core_list_projects`, `wit_get_work_item`, `wit_create_work_item`, `repo_create_pull_request`, `pipelines_get_builds`, `wiki_get_page_content`, `search_code`, `advsec_get_alerts` + 80 more

**Options:**
- `--organization <ORG>` — ADO organization (auto-detected from git remote)
- `--legacy` — Use legacy API mode

**Example usage:**
- "Create a PR from my branch to main, link work item #12345, add reviewer"
- "Show me failed builds in the last 24 hours for pipeline X"
- "Search code for 'DGrepClient' across all repos in the One project"

**Squad utility:** Foundational for everyone. Aragorn uses it for pipeline monitoring, Gandalf for triage, Galadriel for PR reviews, all agents for work item management.

---

### 3.2 Bluebird (Engineering Copilot Mini)

**What it does:** AI-powered code search and navigation across ADO repositories. Goes beyond text search — understands code semantics, finds implementations, traces call chains.

**How to enable:**
```bash
agency mcp bluebird
agency mcp bluebird --organization msazure --project One --repo MyRepo
```

**Options:**
- `--organization`, `--project`, `--repo`, `--branch` (all auto-detected)
- `--mini` — Lightweight mode
- `--full` — Full analysis mode
- `--local` — Search local checkout only

**Example usage:**
- "Find all implementations of IAlertProvider interface"
- "How does the TI enrichment pipeline process indicators?"

**Squad utility:** Elrond for deep code research across repos. Frodo for understanding TI backend code. Gimli for finding existing utilities before building new ones.

---

### 3.3 Kusto (Azure Data Explorer)

**What it does:** Execute KQL queries against any Azure Data Explorer cluster. Query logs, metrics, telemetry data.

**Production ICM Logs Endpoint:** `https://ti-prod-kusto-cluster.northeurope.kusto.windows.net` — primary source for incident log queries. See **Aragorn's Charter** (Stage 2: Data Enrichment) for ICM investigation workflow.

**How to enable:**
```bash
agency mcp kusto --service-uri https://mycluster.kusto.windows.net --database mydb
```

**Options:**
- `--service-uri <URI>` (required) — Kusto cluster URI
- `--database <DB>` — Default database
- `--known-services <JSON>` — Pre-configured service list

**Example usage:**
- "Show me top 10 longest-running incidents in the last 30 days"
- "Query error rates from ThreatIntelligence table, grouped by IndicatorType"

**Squad utility:** Aragorn for livesite telemetry investigation. Elrond for data-driven research. Frodo for TI pipeline health queries.

> **Note:** Also available via `azure-mcp` (Kusto tools: `kusto_query`, `kusto_table_list`, `kusto_table_schema`, etc.)

---

### 3.4 ICM (Incident Management)

**What it does:** Query and manage IcM incidents — get incident details, summaries, impact analysis, similar incidents, on-call schedules, mitigation hints.

**How to enable:**
```bash
# Via Agency (HTTP proxy)
agency mcp icm

# Currently configured in mcp-config.json
```

**Key tools (20+):** `get_incident_details_by_id`, `get_ai_summary`, `get_incident_context`, `get_impacted_s500_customers`, `get_impacted_ace_customers`, `get_impacted_services_regions_clouds`, `get_similar_incidents`, `get_mitigation_hints`, `get_on_call_schedule_by_team_id`, `get_teams_by_name`, `get_support_requests_crisit`, `get_incident_location`, `get_incident_customer_impact`, `get_outage_high_priority_events`, `is_specific_customer_impacted`

**Example usage:**
- "Summarize INC 766712513 — impact, status, who's engaged?"
- "Who is on-call for the TI Platform team right now?"
- "Find similar incidents to INC 12345678 in the last 90 days"

**Squad utility:** **Critical for Aragorn** — incident investigation, impact assessment, on-call lookup. Gandalf uses for triage decisions. Elrond uses for incident research and pattern analysis.

---

### 3.5 CloudBuild

**What it does:** Trigger and monitor CloudBuild builds — the Microsoft internal build system.

**How to enable:**
```bash
agency mcp cloudbuild
```

**Example usage:**
- "Trigger a build on my current branch and notify me when done"
- "What's the status of my last CloudBuild submission?"

**Squad utility:** Aragorn for build monitoring. Gimli for build pipeline tooling.

---

### 3.6 EngHub (eng.ms)

**What it does:** Search and browse Engineering Hub documentation, TSGs (Troubleshooting Guides), and ServiceTree hierarchy. Resolve services, list content nodes, fetch full page content.

**How to enable:**
```bash
# Via Agency
agency mcp enghub

# Currently configured standalone in mcp-config.json
```

**Key tools:** `search`, `fetch`, `get_source_link`, `resolve_service`, `get_service_nodes`, `get_node_tree`

**Binary:** `enghub-mcp` (npm global at `C:\Program Files\nodejs\`)

**Example usage:**
- "Find TSG for migration failures in TI service tree"
- "Search eng.ms for DGrep documentation"
- "Resolve the USX Threat Intelligence service and list its TSGs"

**Key docs:**
- [Agency MCP docs](https://eng.ms/docs/coreai/devdiv/one-engineering-system-1es/1es-jacekcz/startrightgitops/agency/tools/mcp/mcp)
- [GEAR-Core reference](https://eng.ms/docs/cloud-ai-platform/microsoft-specialized-clouds-msc/cai-silver/gear-silver/gear-core/gear-core/training/aitools/agencymcp)

**Squad utility:** **Critical for Elrond** — primary research tool for finding internal docs. Bilbo for sourcing documentation. Everyone for TSG lookup.

---

### 3.7 Teams (Microsoft Teams)

**What it does:** Read and post messages to Microsoft Teams chats and channels.

**How to enable:**
```bash
agency mcp teams
```

**Example usage:**
- "Find the TI Platform channel and post a status update"
- "Read the last 10 messages in the #livesite channel"
- "Send Jonathan a Teams message summarizing today's PR reviews"

**Squad utility:** The pa-squad notification channel. Aragorn can post incident alerts, Gandalf can post daily summaries, all agents can post status updates.

> **Note:** For programmatic Teams access, also consider WorkIQ MCP (poll-based, suitable for batch summaries).

---

### 3.8 Mail (Microsoft Outlook)

**What it does:** Read, search, and send emails via Outlook/Exchange.

**How to enable:**
```bash
agency mcp mail
```

**Example usage:**
- "Find the most recent email from security-review@ and summarize"
- "Search for emails about the DGrep migration in the last week"

**Squad utility:** Elrond for email-based research. Bilbo for finding documentation shared via email.

---

### 3.9 Calendar (Microsoft Calendar)

**What it does:** View and manage Outlook calendar events.

**How to enable:**
```bash
agency mcp calendar
```

**Example usage:**
- "What meetings does Jonathan have tomorrow? Flag conflicts."
- "Find the next TI team standup"

**Squad utility:** Gandalf for daily planning and scheduling awareness.

---

### 3.10 SharePoint

**What it does:** Access SharePoint sites, files, and lists.

**How to enable:**
```bash
agency mcp sharepoint
```

**Example usage:**
- "Search the TI Platform site for the latest onboarding runbook"
- "Find the most recent architecture decision doc in SharePoint"

**Squad utility:** Elrond for research on SharePoint-hosted docs. Bilbo for accessing team documentation.

---

### 3.11 WorkIQ (M365 Copilot)

**What it does:** Queries Microsoft 365 data (emails, meetings, files) through M365 Copilot. Poll-based with indexing delay.

**How to enable:**
```json
// In mcp-config.json (currently configured)
"workiq": {
  "type": "stdio",
  "command": "npx",
  "args": ["-y", "@microsoft/workiq", "mcp"],
  "tools": ["*"]
}
```

**Key tools:** `accept_eula`, `ask_work_iq`

**Example usage:**
- "What were the key discussion points from yesterday's meeting?"
- "Find recent emails about the ConfigGen migration"

**Squad utility:** Elrond for research across M365 data. Gandalf for meeting summaries.

> **Important:** Rate-limit to one WorkIQ query per agent cycle. Poll-based with indexing delay.

---

### 3.12 Athena (Teams Assistant)

**What it does:** Microsoft Teams Athena Assistant — advanced Teams interaction.

**How to enable:**
```bash
agency mcp athena
```

**Squad utility:** Enhanced Teams interaction beyond basic Teams MCP.

---

### 3.13 Microsoft Learn

**What it does:** Search official Microsoft documentation (docs.microsoft.com / learn.microsoft.com).

**How to enable:**
```bash
agency mcp msft-learn
```

**Example usage:**
- "Find documentation on Azure Sentinel threat intelligence APIs"
- "What are the latest features in Azure Data Explorer?"

**Squad utility:** Elrond for public documentation research. Frodo for Azure API reference.

> **Note:** Also available via `azure-mcp` as `azure-mcp-documentation`.

---

### 3.14 ES Chat (Engineering Systems)

**What it does:** AI-powered engineering systems support assistant. Searches internal knowledge bases (Engineering Hub, ADO wikis, work items, IcM incidents) and has custom diagnostic tools.

**How to enable:**
```json
// In mcp-config.json (currently configured)
"ask-es-chat": {
  "url": "https://eschat.microsoft.com/mcp",
  "type": "http"
}
```

**Key tools:** `es_resolve`, `es_ask`, `es_search`

**Example usage:**
- "How do I onboard a new service to the 1ES build pipeline?"
- "What is the process for requesting a new ADO repository?"

**Squad utility:** Elrond for engineering systems research. Gimli for tooling questions. Everyone for "how do I do X in the Microsoft engineering ecosystem?"

---

### 3.15 Security Context

**What it does:** Azure Security Context — provides security posture insights.

**How to enable:**
```bash
agency mcp security-context
```

**Docs:** [https://aka.ms/security-ai](https://aka.ms/security-ai)

**Squad utility:** Aragorn for security posture during incident response. Galadriel for security review context.

---

### 3.16 S360 Breeze (Compliance)

**What it does:** S360 compliance and security posture management.

**How to enable:**
```bash
agency mcp s360-breeze
```

**Example usage:**
- "Show current compliance status for the TI Platform service"

**Squad utility:** Aragorn for compliance monitoring. Gandalf for compliance-aware planning.

---

### 3.17–3.21 Utility MCPs

#### Calculator
```bash
agency mcp calculator
```
Simple calculator for testing and demos. Useful for validating MCP connectivity.

#### Stack
```bash
agency mcp stack
```
FIFO stack to help agents track iteration state across multi-step workflows.

#### Remote
```bash
agency mcp remote --url https://my-mcp-server.example.com
```
Proxy to any HTTP MCP server with automatic EntraID token injection. Use when connecting to custom team MCP servers.

#### Local
```bash
agency mcp local
```
Proxy to a local MCP server (e.g., for development).

#### npx
```bash
agency mcp npx
```
Launch any stdio-based MCP server via npx. Useful for trying npm-published MCP packages.

---

## 4. Standalone MCPs (Detailed)

These are configured directly in `~/.copilot/mcp-config.json` and run as independent processes.

---

### 4.1 Azure MCP (@azure/mcp)

**What it does:** The official Microsoft MCP server for Azure services. Provides 100+ tools across 30+ Azure service domains. This is the single largest MCP in the ecosystem.

**Binary:** `node "C:\Program Files\nodejs\node_modules\@azure\mcp\index.js" server start`
**Package:** `@azure/mcp` (npm)
**Docs:** [Azure MCP Server on Microsoft Learn](https://learn.microsoft.com/en-us/azure/developer/azure-mcp-server) · [EngHub: Azure MCP Server](https://eng.ms/docs/cloud-ai-platform/azure-core/one-fleet-platform/core-fundamentals-imrans/geneva-actions/geneva-actions-tsgs/ai-initiative/mcp-servers/azure-mcp-server)

**Config entry:**
```json
"azure-mcp": {
  "type": "stdio",
  "command": "node",
  "tools": ["*"],
  "args": ["C:\\Program Files\\nodejs\\node_modules\\@azure\\mcp\\index.js", "server", "start"]
}
```

**Service domains (30+):**

| Domain | Tool Prefix | Key Capabilities |
|--------|------------|------------------|
| Documentation | `documentation` | Search Microsoft Learn docs |
| Azure Developer CLI | `azd` | Project init, provisioning, deployment |
| Foundry | `foundry` | AI model/agent management |
| Best Practices | `get_azure_bestpractices` | Code generation best practices |
| AKS | `aks` | Kubernetes cluster management |
| App Configuration | `appconfig` | Key-value settings management |
| AppLens | `applens` | Diagnostics and troubleshooting |
| App Service | `appservice` | Web app management |
| Authorization | `role` | RBAC role assignments |
| Bicep Schema | `bicepschema` | IaC template management |
| Compute | `compute` | VMs, VMSS, Managed Disks |
| Container Apps | `containerapps` | Container app management |
| Container Registry | `acr` | ACR management |
| Cosmos DB | `cosmos` | Database CRUD and queries |
| Event Grid | `eventgrid` | Event subscriptions |
| Event Hubs | `eventhubs` | Namespace and hub management |
| Functions | `functions`, `functionapp` | Serverless code generation |
| Key Vault | `keyvault` | Secrets, keys, certificates |
| Kusto | `kusto` | KQL queries, schema, clusters |
| Monitor | `monitor` | Logs and metrics queries |
| MySQL | `mysql` | Flexible Server management |
| PostgreSQL | `postgres` | Flexible Server management |
| Redis | `redis` | Cache management |
| Search | `search` | AI Search indexes and queries |
| Service Bus | `servicebus` | Messaging queues/topics |
| SQL | `sql` | Azure SQL databases |
| Storage | `storage` | Blobs, tables, accounts |
| Subscriptions | `subscription_list` | List subscriptions |
| Resource Groups | `group_list` | List/manage resource groups |
| Well-Architected | `wellarchitectedframework` | WAF guidance |
| Cloud Architect | `cloudarchitect` | Architecture design |
| Deploy | `deploy` | Deployment plans, CI/CD |
| Pricing | `pricing` | Retail pricing lookup |

**Example usage:**
- "List all resource groups in my subscription"
- "Query Application Insights for errors in the last hour"
- "Generate Bicep template for a Cosmos DB account"
- "What are Azure best practices for building AI applications?"

**Squad utility:** Broad utility. Frodo for Azure resource management (TI backend). Gimli for infrastructure tooling. Aragorn for resource health monitoring. Gandalf for architecture decisions.

---

### 4.2 EV2 MCP Server (ExpressV2)

**What it does:** Interact with EV2 (ExpressV2) — the Microsoft-internal safe deployment service used for rolling out Azure services.

**Binary:** `dnx Ev2.McpServer` (NuGet-distributed)
**Package source:** `https://msazure.pkgs.visualstudio.com/_packaging/Official/nuget/v3/index.json`

**Config entry:**
```json
"Ev2-MCP-Server": {
  "type": "stdio",
  "command": "dnx",
  "tools": ["*"],
  "args": [
    "Ev2.McpServer",
    "--source", "https://msazure.pkgs.visualstudio.com/_packaging/Official/nuget/v3/index.json",
    "--interactive",
    "--yes"
  ],
  "env": {
    "ALLOWED_EV2_ENDPOINTS": "Test,Prod"
  }
}
```

**Example usage:**
- "Show current rollout status for my service"
- "What EV2 rollouts are in progress for the TI Platform?"

**Squad utility:** Aragorn for deployment monitoring. Frodo for TI backend rollout tracking.

---

### 4.3 ConfigGen MCP (Official)

**What it does:** Official ConfigGen MCP for exploring ConfigGen libraries — get Azure library examples, public APIs, search classes across packages, list breaking changes.

**Binary:** `ConfigurationGeneration.Mcp` (dotnet tool at `C:\Users\jbenami\.dotnet\tools\`)

**Config entry:**
```json
"configgen": {
  "type": "stdio",
  "command": "ConfigurationGeneration.Mcp",
  "args": [],
  "tools": ["*"]
}
```

**Key tools:** `get-azure-library-example`, `get-azure-library-publicapi`, `get-shellextension-library-publicapi`, `get-core-library-docs`, `get-core-library-publicapi`, `search-classes`, `search-publicapi`, `list-breaking-changes`, `get-breaking-change`

**Example usage:**
- "Show me the public API for ConfigurationGeneration.KeyVault"
- "Search for classes containing 'StorageAccount'"
- "What breaking changes were introduced in the latest ConfigGen release?"

**Squad utility:** Frodo for TI infrastructure ConfigGen work. Gimli for tooling around ConfigGen. Elrond for researching ConfigGen APIs.

---

### 4.4 Yoni ConfigGen MCP (Semantic Model)

**What it does:** Custom-built semantic model over the ConfigGen framework. Provides deep understanding: type descriptions, inheritance hierarchies, decompiled source, pattern guides, and deep behavioral guides synthesized from 20+ real repos.

**Binary:** Python MCP at `C:\dev\ti\Sentinel-TiAITools\mcp\configgen\server.py`
**Runtime:** `C:\dev\ti\Sentinel-TiAITools\mcp\configgen\.venv\Scripts\python.exe`

**Config entry:**
```json
"yoni-configgen-mcp": {
  "type": "stdio",
  "tools": ["*"],
  "command": "C:\\dev\\ti\\Sentinel-TiAITools\\mcp\\configgen\\.venv\\Scripts\\python.exe",
  "args": ["C:\\dev\\ti\\Sentinel-TiAITools\\mcp\\configgen\\server.py"]
}
```

**Key tools:** `describe_type`, `get_type_hierarchy`, `search_types`, `get_decompiled_source`, `generate_template`, `get_pattern_guide`, `ask_configgen`, `get_deep_guide`

**Guides available:**
- `resource_patterns` — Geo, RBAC, manifests, NSP
- `service_patterns` — K8S services, wrappers, scaling, telemetry
- `legacy_patterns` — Old API, migration, satellite repos
- `behavioral` — Deep framework logic, non-obvious behaviors

**Example usage:**
- "Describe the K8SServiceBase type and all its properties"
- "Show the inheritance hierarchy from TopologyBase"
- "Get the deep guide on resource patterns, section on Geo-Primary"
- "Generate a template for a new service called MyNewService"

**Squad utility:** **Critical for Frodo** — the primary tool for understanding and working with ConfigGen. Elrond for ConfigGen research. Gimli for template generation.

---

### 4.5 Geneva MCP Server

**What it does:** Query Geneva MDM (Metrics Data Mart) — timeseries data, metric discovery, dimension filtering, pre-aggregation validation.

**Binary:** `genevamcpserver.exe` (dotnet tool at `C:\Users\jbenami\.dotnet\tools\`)
**Docs:** [Geneva MDM MCP Server Reference](https://eng.ms/docs/cloud-ai-platform/cloud-operations-innovation/seci/ciapt/cinapt/cinapt/githubcopilot/github-copilot-geneva-mdm-mcp-server)

**Config entry:**
```json
"geneva-mcp-server": {
  "type": "stdio",
  "command": "genevamcpserver.exe",
  "args": [],
  "tools": ["*"]
}
```

**Key tools:**
- `query_timeseries` — Execute metric queries with dimension filters, time windows, aggregations
- `list_metrics` — List all metrics in an account/namespace
- `list_metric_preaggregations` — Get pre-aggregated dimension sets (validate before querying)
- `list_dimension_values` — Get possible values for a dimension
- `metrics_handbook` — Reference guide for accounts, namespaces, metrics, dimensions, query rules

**Query workflow (best practice):**
1. `metrics_handbook` — Understand query rules
2. `list_metrics` — Discover available metrics
3. `list_dimension_values` — Understand filtering options
4. `list_metric_preaggregations` — **MANDATORY** before complex queries
5. `query_timeseries` — Execute with validated combinations

**Example usage:**
- "List all metrics in the ThreatIntelligence namespace"
- "Query CPU usage for the TI ingestion service over the last 24 hours"
- "Show pre-aggregation configurations for the IndicatorProcessingLatency metric"

**Squad utility:** **Critical for Aragorn** — service health monitoring, metric investigation during incidents. Frodo for TI pipeline performance analysis.

---

### 4.6 EngHub MCP (Standalone)

**What it does:** Same as Agency EngHub but runs as a standalone process. Search Engineering Hub, fetch page content, resolve services, browse ServiceTree hierarchy.

**Binary:** `enghub-mcp start` (npm global at `C:\Program Files\nodejs\`)

**Config entry:**
```json
"enghub": {
  "type": "stdio",
  "command": "enghub-mcp",
  "args": ["start"],
  "tools": ["*"]
}
```

See [Section 3.6](#36-enghub-engms) for full details.

---

### 4.7 ES Chat (Standalone)

**What it does:** Engineering Systems Chat — AI-powered support for Microsoft engineering systems questions.

**Config entry:**
```json
"ask-es-chat": {
  "url": "https://eschat.microsoft.com/mcp",
  "type": "http"
}
```

See [Section 3.14](#314-es-chat-engineering-systems) for full details.

---

## 5. Platform-Provided MCPs

These are built into the Copilot CLI or VS Code and require no configuration.

---

### 5.1 GitHub MCP Server

**What it does:** Full GitHub integration — actions, commits, files, issues, pull requests, code search, branch management, Copilot spaces.

**Key tools:**
- **Actions:** `actions_get`, `actions_list`, `get_job_logs`
- **Code:** `get_file_contents`, `get_commit`, `list_commits`, `list_branches`, `search_code`
- **Issues:** `list_issues`, `issue_read`, `search_issues`
- **PRs:** `list_pull_requests`, `pull_request_read`, `search_pull_requests`
- **Discovery:** `search_repositories`, `search_users`, `list_copilot_spaces`, `get_copilot_space`

**Example usage:**
- "List open PRs in the pa-squad repo"
- "Search for 'MCP' across all repos in the microsoft org"
- "Get the diff for PR #42"

**Squad utility:** Everyone. Galadriel for PR reviews. Gimli for GitHub Actions. Gandalf for issue triage.

---

### 5.2 IDE Tools (VS Code)

**What it does:** Read from the active VS Code editor — current text selection and language diagnostics (errors, warnings).

**Key tools:** `get_selection`, `get_diagnostics`

**Squad utility:** Galadriel for live code review context. Gimli for debugging assistance.

---

## 6. Currently Configured

The following is the complete `~/.copilot/mcp-config.json` as of 2026-03-24:

```json
{
  "mcpServers": {
    "ado": {
      "tools": ["*"],
      "type": "stdio",
      "command": "npx",
      "args": ["-y", "@azure-devops/mcp", "msazure"],
      "env": {
        "ADO_ORG_NAME": "msazure",
        "ADO_DEFAULT_PROJECT": "One"
      }
    },
    "ado-wdatp": {
      "tools": ["*"],
      "type": "stdio",
      "command": "npx",
      "args": ["-y", "@azure-devops/mcp", "msazure"],
      "env": {
        "ADO_ORG_NAME": "microsoft",
        "ADO_DEFAULT_PROJECT": "WDATP"
      }
    },
    "azure-mcp": {
      "type": "stdio",
      "command": "node",
      "tools": ["*"],
      "args": ["C:\\Program Files\\nodejs\\node_modules\\@azure\\mcp\\index.js", "server", "start"]
    },
    "Ev2-MCP-Server": {
      "type": "stdio",
      "command": "dnx",
      "tools": ["*"],
      "args": [
        "Ev2.McpServer",
        "--source",
        "https://msazure.pkgs.visualstudio.com/_packaging/Official/nuget/v3/index.json",
        "--interactive", "--yes"
      ],
      "env": { "ALLOWED_EV2_ENDPOINTS": "Test,Prod" }
    },
    "ask-es-chat": {
      "url": "https://eschat.microsoft.com/mcp",
      "type": "http"
    },
    "yoni-configgen-mcp": {
      "type": "stdio",
      "tools": ["*"],
      "command": "C:\\dev\\ti\\Sentinel-TiAITools\\mcp\\configgen\\.venv\\Scripts\\python.exe",
      "args": ["C:\\dev\\ti\\Sentinel-TiAITools\\mcp\\configgen\\server.py"]
    },
    "workiq": {
      "type": "stdio",
      "command": "npx",
      "args": ["-y", "@microsoft/workiq", "mcp"],
      "tools": ["*"]
    },
    "icm": {
      "type": "stdio",
      "command": "agency",
      "args": ["mcp", "icm"],
      "tools": ["*"]
    },
    "geneva-mcp-server": {
      "type": "stdio",
      "command": "genevamcpserver.exe",
      "args": [],
      "tools": ["*"]
    },
    "configgen": {
      "type": "stdio",
      "command": "ConfigurationGeneration.Mcp",
      "args": [],
      "tools": ["*"]
    },
    "enghub": {
      "type": "stdio",
      "command": "enghub-mcp",
      "args": ["start"],
      "tools": ["*"]
    }
  }
}
```

**Summary:** 11 MCPs configured + 2 platform-provided (GitHub, IDE) = **13 active MCPs** providing **200+ tools**.

---

## 7. Configuration Guide

### Adding an Agency MCP to a Copilot Session

```bash
# One-time usage
agency copilot --mcp ado --mcp icm --mcp teams

# Multiple MCPs
agency copilot --mcp ado --mcp kusto --mcp icm --mcp enghub
```

### Adding a Standalone MCP to `~/.copilot/mcp-config.json`

**Step 1:** Open the config file:
```bash
code "$env:USERPROFILE\.copilot\mcp-config.json"
```

**Step 2:** Add a new entry under `mcpServers`. There are three types:

**stdio (local process):**
```json
"my-mcp": {
  "type": "stdio",
  "command": "path-to-binary",
  "args": ["arg1", "arg2"],
  "tools": ["*"],
  "env": { "MY_VAR": "value" }
}
```

**http (remote server):**
```json
"my-mcp": {
  "type": "http",
  "url": "https://my-mcp-server.example.com/mcp"
}
```

**npx (npm package):**
```json
"my-mcp": {
  "type": "stdio",
  "command": "npx",
  "args": ["-y", "@scope/package-name", "mcp"],
  "tools": ["*"]
}
```

**Step 3:** Reload the config (no restart needed):
- In Copilot CLI: The `mcp_reload` tool reloads configuration from disk
- Or restart the Copilot CLI session

### Validating Config

Use the `mcp_validate` tool with the path to your config file to check for schema errors before reloading.

### Adding Multiple ADO Organizations

Each ADO organization needs its own entry with a unique key:
```json
"ado-org1": {
  "type": "stdio",
  "command": "npx",
  "args": ["-y", "@azure-devops/mcp", "org1"],
  "env": { "ADO_ORG_NAME": "org1", "ADO_DEFAULT_PROJECT": "MyProject" }
},
"ado-org2": {
  "type": "stdio",
  "command": "npx",
  "args": ["-y", "@azure-devops/mcp", "org2"],
  "env": { "ADO_ORG_NAME": "org2", "ADO_DEFAULT_PROJECT": "OtherProject" }
}
```

Tool names are prefixed with the config key (e.g., `ado-org1-wit_get_work_item`, `ado-org2-wit_get_work_item`).

---

## 8. Squad Integration Notes

How each pa-squad member can leverage specific MCPs for maximum effectiveness.

### 🔍 Elrond (Researcher)

| MCP | Use Case |
|-----|----------|
| **enghub** | Primary research tool — search TSGs, docs, ServiceTree |
| **es-chat** | Engineering systems questions, onboarding processes |
| **azure-mcp** (documentation) | Official Azure/Microsoft Learn documentation |
| **msft-learn** | Public Microsoft documentation |
| **ado** (search_code, search_wiki) | Search code and wikis across ADO organizations |
| **bluebird** | Semantic code search when text search isn't enough |
| **workiq** | Research across M365 (emails, meetings, files) |
| **mail** / **sharepoint** | Access email threads and SharePoint docs |
| **kusto** | Data-driven research via KQL queries |
| **icm** (get_similar_incidents) | Pattern analysis across incident history |

**Power combo:** `enghub` + `es-chat` + `ado search_code` for comprehensive internal knowledge research.

### ⚔️ Aragorn (Operator / Livesite)

| MCP | Use Case |
|-----|----------|
| **icm** | Incident investigation, impact assessment, on-call lookup |
| **geneva-mcp-server** | Service health metrics, performance monitoring |
| **kusto** | Telemetry queries for incident diagnosis |
| **azure-mcp** (monitor, resourcehealth, applens) | Azure resource diagnostics |
| **ado** (pipelines) | Build/deployment status monitoring |
| **Ev2-MCP-Server** | Rollout monitoring and status |
| **security-context** | Security posture during incidents |
| **s360-breeze** | Compliance status checks |
| **teams** | Post incident updates to channels |

**Power combo:** `icm` + `geneva-mcp-server` + `kusto` for full incident triage pipeline.

### 🔨 Gimli (Tool Builder)

| MCP | Use Case |
|-----|----------|
| **azure-mcp** (functions, bicepschema, deploy) | Build Azure infrastructure tooling |
| **configgen** + **yoni-configgen-mcp** | ConfigGen template generation and API exploration |
| **github-mcp-server** (actions) | GitHub Actions CI/CD automation |
| **ado** (pipelines) | ADO pipeline management |
| **enghub** | Find existing tools/patterns before building new ones |
| **bluebird** | Semantic code search for reusable components |

**Power combo:** `configgen` + `yoni-configgen-mcp` + `azure-mcp` for infrastructure-as-code tooling.

### 🧙 Gandalf (Lead / Triage)

| MCP | Use Case |
|-----|----------|
| **ado** (wit, search_workitem) | Work item triage, sprint planning |
| **icm** | Incident severity assessment, escalation decisions |
| **azure-mcp** (cloudarchitect, wellarchitectedframework) | Architecture decisions |
| **github-mcp-server** (issues, PRs) | GitHub issue/PR triage |
| **calendar** | Meeting awareness for scheduling |
| **teams** | Post triage summaries, assign tasks |
| **enghub** | Architecture reference docs |

**Power combo:** `ado` + `icm` + `teams` for daily standup triage.

### 🧝 Frodo (TI Backend)

| MCP | Use Case |
|-----|----------|
| **yoni-configgen-mcp** | Deep ConfigGen type understanding, pattern guides |
| **configgen** | Official ConfigGen API exploration, breaking changes |
| **azure-mcp** (cosmos, storage, eventhubs, kusto) | TI backend Azure resources |
| **kusto** | TI pipeline telemetry queries |
| **geneva-mcp-server** | TI service metrics monitoring |
| **Ev2-MCP-Server** | TI backend deployment tracking |
| **ado** (repo, pipelines) | TI code repos and build pipelines |

**Power combo:** `yoni-configgen-mcp` + `configgen` + `azure-mcp` for full TI infrastructure work.

### 🌟 Galadriel (Reviewer)

| MCP | Use Case |
|-----|----------|
| **ado** (repo — PR threads, diffs) | ADO PR review comments |
| **github-mcp-server** (pull_request_read — reviews, diffs, files) | GitHub PR reviews |
| **ide** (get_diagnostics) | Live editor error checking |
| **azure-mcp** (get_azure_bestpractices) | Best practices validation |
| **configgen** (search-publicapi) | Validate ConfigGen API usage |
| **bluebird** | Understand code context across repos |

**Power combo:** `ado` or `github` PR tools + `ide` diagnostics + `azure-mcp` best practices for thorough reviews.

### 📜 Bilbo (Documentarian)

| MCP | Use Case |
|-----|----------|
| **enghub** | Source existing documentation, find gaps |
| **ado** (wiki) | Read/write ADO wiki pages |
| **github-mcp-server** (get_file_contents) | Read markdown docs from repos |
| **sharepoint** | Access SharePoint-hosted team docs |
| **es-chat** | Engineering systems process documentation |
| **msft-learn** | Reference official Microsoft docs |

**Power combo:** `enghub` + `ado wiki` + `sharepoint` for comprehensive documentation updates.

---

## 9. Multi-MCP Workflows

The real power is combining MCPs. Key workflows from the [GEAR-Core reference](https://eng.ms/docs/cloud-ai-platform/microsoft-specialized-clouds-msc/cai-silver/gear-silver/gear-core/gear-core/training/aitools/agencymcp):

### Incident Triage Pipeline
```
ICM (get incident) → Kusto (query telemetry) → Geneva (check metrics)
→ ADO (find related work items) → Teams (post summary)
```

### Code Review Workflow
```
ADO/GitHub (get PR diff) → Bluebird (understand context)
→ Azure MCP (check best practices) → ADO/GitHub (post review comments)
```

### Research & Documentation
```
EngHub (search docs) → ES Chat (ask questions) → ADO (search code)
→ SharePoint (find team docs) → ADO Wiki (write documentation)
```

### Deployment Monitoring
```
EV2 (check rollout) → Geneva (service metrics) → ICM (any incidents?)
→ ADO Pipelines (build status) → Teams (post status update)
```

---

## Appendix A: Tool Count Summary

| MCP | Approximate Tool Count |
|-----|----------------------|
| azure-mcp | 100+ |
| ado (×3 instances) | 80+ per instance |
| icm | 20+ |
| github-mcp-server | 18 |
| configgen (official) | 9 |
| yoni-configgen-mcp | 8 |
| enghub | 6 |
| geneva-mcp-server | 5 |
| es-chat | 3 |
| workiq | 2 |
| ide | 2 |
| **Total active** | **~200+ unique tools** |

## Appendix B: Key URLs

| Resource | URL |
|----------|-----|
| Agency landing page | https://aka.ms/agency |
| Agency MCP docs | https://eng.ms/docs/coreai/devdiv/one-engineering-system-1es/1es-jacekcz/startrightgitops/agency/tools/mcp/mcp |
| GEAR-Core MCP reference | https://eng.ms/docs/cloud-ai-platform/microsoft-specialized-clouds-msc/cai-silver/gear-silver/gear-core/gear-core/training/aitools/agencymcp |
| Agency general docs | https://eng.ms/docs/coreai/devdiv/one-engineering-system-1es/1es-gadecast/ospoost/ai-guidance-for-microsoft-developers/tools/agentic-engineering/agency/index |
| Azure MCP Server | https://learn.microsoft.com/en-us/azure/developer/azure-mcp-server |
| Azure MCP (npm) | https://www.npmjs.com/package/@azure/mcp |
| Azure MCP (GitHub) | https://github.com/microsoft/mcp |
| Geneva MDM MCP reference | https://eng.ms/docs/cloud-ai-platform/cloud-operations-innovation/seci/ciapt/cinapt/cinapt/githubcopilot/github-copilot-geneva-mdm-mcp-server |
| Azure MCP telemetry | https://eng.ms/docs/products/azure-developer-experience/telemetry/azure-mcp-telemetry |
| Security Context | https://aka.ms/security-ai |
| ES Chat | https://eschat.microsoft.com |
