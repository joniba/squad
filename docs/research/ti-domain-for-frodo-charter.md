# TI Domain Research — For Frodo Charter Redesign

**Researcher:** Elrond  
**Date:** 2026-03-23  
**Purpose:** Deep research into the Threat Intelligence (TI) domain to inform Gandalf's redesign of Frodo's charter. Jonathan's directive: Frodo owns the *entire* TI domain (`C:\dev\ti`), not just two repos.

---

## 1. Complete Repository Inventory (`C:\dev\ti`)

### Core TI Pipeline Services (Deployable)

| Repository | Stack | Purpose |
|---|---|---|
| **Sentinel-TiPipeline** | C# / .NET | **The heart of TI.** Customer Threat Intelligence Pipeline — services for ingesting indicators via APIs (GatewayService, IngestionAPI, StixAPIs), file imports (FileImportsApi/Service), connector management (TICS), and workspace management (ThreatIntelligenceManager). Azure Functions on Premium plan. |
| **Sentinel-TiPublishers** | C# / .NET | Publishers that distribute normalized TI to storage destinations: CosmosDbPublisher (→ Cosmos DB), LogAEventHubPublisher (→ Log Analytics via Scuba), LogARepublisher (periodic republish for Detections/Hunting). All EventHub-triggered Azure Functions. |
| **Sentinel-TiAutomation** | C# / .NET | Automation layer: TiNormalization (ingestion rules, STIX validation, consolidation), TiBulkActions (bulk edit/delete/export via Durable Functions), TiAutomationApis (REST APIs for bulk actions, STIX queries, ingestion rules). |
| **Sentinel-TiActionPipeline** | C# / .NET | Action pipeline processing (TSA bug filing configured). Azure Functions with deployment config. |

### STIX/Augusta Services (Deployable)

| Repository | Stack | Purpose |
|---|---|---|
| **Sentinel-Augusta** | C# / .NET | STIX/TAXII REST API (StixWebApi) for indicator CRUD. Multi-region EV2 deployment (App Service). Legacy but active — handles STIX filter, batch, TAXII client management. |
| **SecEng-Augusta** | C# / .NET | STIX pipeline infrastructure: BlackBoxConnectorService, DakotaFanOutService, ISGActor, TAXIIActor, MSFeedActor, MSFeedRealTimeService. Service Fabric actors processing feeds from various TI sources. Also contains STIX.NET and TAXII.NET libraries. |

### TI Matching (Deployable)

| Repository | Stack | Purpose |
|---|---|---|
| **Sentinel-ThreatIntelligenceMatching** | C# / .NET + Python (Synapse) | MLAP Synapse Spark jobs that compare customer security events against known threat indicators (IP, domain, file hash) to detect matches and generate alerts. |
| **Amba.TIMatching** | C# / .NET (K8s) | BBTIMatching.AR.Publisher.Service — processes Broad-Based TI matching events for MDE/MDATP. K8s Pod background worker with .NET Aspire support. |

### Watchlist (Deployable)

| Repository | Stack | Purpose |
|---|---|---|
| **Sentinel-Watchlist** | C# / .NET 8.0 | WatchlistRestApi (ARM-compatible CRUD for watchlists/items, confidential watchlists, large CSV processing) and WatchlistSyncEngineApp (syncs watchlist data to Log Analytics via Scuba). Azure Functions v4. |

### Shared Libraries (Non-deployable)

| Repository | Stack | Purpose |
|---|---|---|
| **Sentinel-TiCommon** | C# / .NET | Shared NuGet packages: Logging, CosmosDb, TestUtilities, HttpClientAuthentication, AzureFunctionUtilities, Recommendations. Consumed by all TI services. Also contains the AI-Generated documentation (Map.md, ServiceGraph.json). |
| **Sentinel-Common** | C# / .NET | Broader Sentinel shared NuGets (logging, CosmosDB, testing, auth, Azure Functions utilities). Cross-domain shared library. |
| **Sentinel-Synthetics** | C# / .NET | Common framework for building synthetic monitoring tests (logging, metrics, config, deployment patterns). |
| **SecEng-SOCML** | Python + C# | SOC Machine Learning — algorithms, libraries, and tools for anomaly detection, analytics settings, synthetic anomaly processing. |
| **SecEng-SOCML-Anomalies** | Python | Anomaly detection algorithms for SOC-ML (SOCML Python package). |

### Infrastructure / Resources (Non-deployable)

| Repository | Stack | Purpose |
|---|---|---|
| **Sentinel-TiSharedInfra-Resources** | IaC (ConfigGen) | Shared Azure infrastructure resources (ARM templates, ConfigGen). |
| **Amba.TIMatching.Resources** | IaC (ConfigGen) | Infrastructure configs for TI Matching (Event Hub namespaces, Storage, Key Vault, Managed Identities). |
| **Sentinel-TiIngestion** | Documentation | Documentation-only repo (README.md). |

### AI / Developer Tools

| Repository | Stack | Purpose |
|---|---|---|
| **Sentinel-TiAITools** | Python | MCP (Model Context Protocol) servers — notably a FastMCP server for Kusto queries (`mcp/kusto/`). Python-based AI tooling for the TI domain. |
| **AI.Workspace** | Node.js | Automated VS Code multi-repo workspace setup — fetches repos from Kusto, clones missing repos, generates `.code-workspace` files. Developer productivity tool. |
| **analysis-project** | Scripts | Code generation and analysis utilities (generators, analysis scripts, instructions). |

### Other

| Repository | Stack | Purpose |
|---|---|---|
| **Amba.SituationalAwarenessData** | C# / .NET (Docker) | Situational Awareness Data platform. Containerized .NET service with docker-compose support. |
| **Playground** | N/A | Sandbox/test repo (contains a single flowchart markdown file). |

**Total: 26 repositories** (11 deployable services, 5 shared libraries, 3 infrastructure, 3 developer tools, 4 other)

---

## 2. Domain Concepts and Vocabulary

### Core Standards

| Term | Definition |
|---|---|
| **STIX 2.1** | Structured Threat Information eXpression — the standard data format for TI. Objects include indicators, identities, attack-patterns, threat-actors, relationships, and sightings. Each has `type`, `spec_version`, `id`, `created`, `modified`. |
| **TAXII** | Trusted Automated eXchange of Indicator Information — protocol for exchanging STIX data. The TI platform supports TAXII 2.0/2.1 feeds as external connectors. |
| **Indicator** | The most common STIX object. Contains a `pattern` (e.g., `[ipv4-addr:value = '1.2.3.4']`), `pattern_type` (usually "stix"), `confidence` (0–100), `valid_from`/`valid_until`, `labels`, `threat_types`. Used for detection matching. |
| **STIX Domain Object (SDO)** | Broader category: identity, attack-pattern, threat-actor, relationship, sighting. Non-indicator STIX objects providing context. |

### Platform Concepts

| Term | Definition |
|---|---|
| **Document ID** | The unique identifier for a STIX object in the platform: `{base64(source)}---{stixId}`. Example: `Q29waWxvdFZhbGlkYXRvcg==---indicator--12345678-...`. Critical for all CRUD operations. |
| **Source / SourceSystem** | The origin of a TI object (e.g., "MDTI", "CopilotValidator", a TAXII feed name). Encoded in the document ID as base64. |
| **Workspace** | A customer's Log Analytics workspace — the scope boundary for all TI operations. Each workspace has its own set of indicators, connectors, and watchlists. |
| **Connector** | A configured link to an external TI data source (TAXII, Security Graph, MDTI, MDTI Premium). Managed by TICS (ThreatIntelligenceConnectorService). |
| **Ingestion Rules** | Per-workspace rules applied during normalization that transform or filter indicators as they enter the pipeline. |
| **Normalization** | The process of validating, enriching, and standardizing STIX objects before storage. Performed by TiNormalization. |
| **Consolidation** | Merging new indicators with existing database records during normalization — handles deduplication and update semantics. |

### Storage & Publishing

| Term | Definition |
|---|---|
| **StixStore** | Cosmos DB databases (`ti-*-stixstore`, `ti-*-stixstore-overflow`) that hold the canonical STIX indicator data. The system of record. |
| **Scuba** | The Event Hub pathway to Log Analytics. LogAEventHubPublisher and LogARepublisher push data via `tioutput-*-scubahub` → Scuba → customer Log Analytics workspace. |
| **Republish** | Periodic re-sending of TI data to Log Analytics for Detections (weekly, `ThreatIntelligence` table) and Hunting (monthly, `ThreatIntelligenceIndicator` table). Managed by LogARepublisher. |
| **Overflow** | Secondary Cosmos DB container for large STIX objects that exceed primary container limits. |

### Log Analytics Tables

| Table | Contains |
|---|---|
| **ThreatIntelIndicators** | Indicator objects. Key columns: `Id`, `SourceSystem`, `Pattern`, `Confidence`, `ObservableKey`, `ObservableValue`, `IsDeleted`. |
| **ThreatIntelObjects** | All non-indicator STIX types (identity, attack-pattern, threat-actor, relationship, sighting). Key columns: `Id`, `SourceSystem`, `StixType`, `Data` (full STIX JSON). |

### Operations Concepts

| Term | Definition |
|---|---|
| **Bulk Actions** | Asynchronous batch operations (Edit, Delete) on STIX objects matching a condition. Uses Service Bus → Durable Functions orchestration. States: Created → Running → Done / Failed. |
| **File Import** | Alternative ingestion path: create import record → get SAS URI → upload JSON/CSV to blob → system parses and ingests. States: WaitingForUpload → Ingesting → Ingested / IngestedWithErrors / Failed. |
| **TI Matching** | Comparing customer security events (from Log Analytics) against known TI indicators to detect matches. Produces security alerts. Runs as MLAP Synapse Spark jobs. |
| **BBTI** | Broad-Based Threat Intelligence — matching at scale for MDE/MDATP. Distinct from per-workspace matching. Processed by Amba.TIMatching. |
| **Watchlists** | Customer-defined lists (CSV-based) of entities for hunting/detection. Separate from TI indicators but related domain. Synced to Log Analytics daily. |

### Infrastructure Concepts

| Term | Definition |
|---|---|
| **EV2** | Express V2 — Azure's safe deployment framework. Most TI services deploy via EV2 with multi-region rollout. |
| **ConfigGen** | Configuration Generation — IaC framework for Azure resource provisioning (used by Sentinel-TiSharedInfra-Resources and Amba.TIMatching.Resources). |
| **Dakota** | Internal codename for the Augusta/STIX pipeline infrastructure (Event Hubs, Key Vaults, storage). |
| **Augusta** | Internal codename for the MDTI (Microsoft Defender Threat Intelligence) backend platform. |
| **MLAP** | Machine Learning Analytics Platform — shared Synapse infrastructure that runs TI matching jobs. |

---

## 3. Architecture Patterns from AI-Generated Documentation

Source: `Sentinel-TiCommon/Documentation/AI-Generated/System-Understanding/Map.md` and `ServiceGraph.json`

### The Four-Layer Architecture

The TI platform follows a clear layered pipeline:

```
┌─────────────────────────────────────────────────────────────┐
│  LAYER 1: INGESTION (Sentinel-TiPipeline)                   │
│  External feeds → GatewayService → IngestionAPI             │
│  + FileImportsApi/Service (blob upload path)                │
│  + TICS (connector management for TAXII/MDTI feeds)         │
│  + StixAPIs (direct CRUD via ARM)                           │
│  Output: → dakota-*-stixeventhub                            │
├─────────────────────────────────────────────────────────────┤
│  LAYER 2: PROCESSING (Sentinel-TiAutomation)                │
│  TiNormalization: validate → check workspace → normalize    │
│  → apply ingestion rules → consolidate with DB              │
│  Output: → ti-*-normalizedeventhub                          │
├─────────────────────────────────────────────────────────────┤
│  LAYER 3: PUBLISHING (Sentinel-TiPublishers)                │
│  CosmosDbPublisher → ti-*-stixstore (system of record)      │
│  LogAEventHubPublisher → Scuba → Log Analytics              │
│  LogARepublisher → periodic Detection/Hunting refresh       │
├─────────────────────────────────────────────────────────────┤
│  LAYER 4: MATCHING (Sentinel-ThreatIntelligenceMatching)    │
│  MLAP Synapse: customer events vs indicators → alerts       │
│  Amba.TIMatching: BBTI events → MDE/MDATP                   │
└─────────────────────────────────────────────────────────────┘
```

### Key Architectural Patterns

1. **Event-driven pipeline via Event Hubs**: Data flows through Event Hubs with consumer groups for fan-out. The `dakota-*-stixeventhub` is the ingestion Event Hub; `ti-*-normalizedeventhub` is the post-processing hub; `tioutput-*-scubahub` is the Log Analytics output.

2. **Consumer group fan-out**: The normalized Event Hub feeds multiple consumers simultaneously — `cosmospublisher` group for CosmosDbPublisher, `logapublisher` group for LogAEventHubPublisher.

3. **Durable Functions orchestration**: Complex workflows (bulk actions, file import processing, republish) use Azure Durable Functions with fan-out/fan-in patterns.

4. **Service Bus for work queues**: Bulk actions use `tibulkactions-*-servicebus`; republish uses `ti-*-larepublish-sb`; TI Manager uses `generalActionQueue` and `durableActionQueue`.

5. **Cosmos DB as system of record**: `ti-*-stixstore` is the canonical store for all STIX data, partitioned by workspace. Overflow data in `ti-*-stixstore-overflow`.

6. **ARM API surface**: All customer-facing STIX APIs go through ARM (Azure Resource Manager) with standard subscription/resourceGroup/workspace scoping. API versions are date-based (e.g., `2025-01-01-preview`).

7. **Multi-region deployment**: Services deploy via EV2 across Azure regions. Augusta (StixWebApi) deploys to 10+ regions. BBTI Matching deploys to 16+ regions.

8. **Augusta/Dakota legacy + modern pipeline**: Two STIX API surfaces coexist — the modern ARM-based StixAPIs (Sentinel-TiPipeline) and the legacy StixWebApi (Sentinel-Augusta). Both write to the same Event Hub (`dakota-*-stixeventhub`).

### Service Connection Topology

From ServiceGraph.json (25 service nodes, 28 resource nodes, ~57 edges):

- **Cosmos DB connections**: 8 services → `ti-*-stixstore` (the most connected resource)
- **Event Hub connections**: 10+ services writing to or reading from various Event Hubs
- **Key Vault connections**: Nearly every service reads secrets from `ti-*-shared-kv` or `dakota-*-kv`
- **Service Bus**: 4 Service Bus namespaces for async work distribution
- **Storage**: File imports, checkpoint data, BBTI settings

---

## 4. What Sagi's TiExpert Agent Teaches Us

### Agent Structure

Sagi built a TiExpert agent (`tiexpert.agent.md`) in the Sentinel-TiPipeline repo with:

- **1 agent definition** (`.github/agents/tiexpert.agent.md`)
- **6 skill documents** (`.github/skills/*/SKILL.md`)
- **8 PowerShell scripts** (`.github/scripts/validate-*.ps1` + config/helpers)

### What the Agent Can Do

The TiExpert agent has **two modes**:

1. **Q&A mode**: Answer questions about STIX objects, API schemas, endpoint patterns, LA table structure
2. **Operations mode**: Perform real STIX operations against PPE:
   - STIX API CRUD (Create, Get, Update, Query, Count, Delete, Poll) for 6 STIX types
   - Upload via Ingestion API (bulk flat STIX)
   - Upload via File Import API (blob/SAS URI flow)
   - Legacy StixWebApi indicator CRUD
   - Bulk Actions (Edit, Delete)
   - Log Analytics queries (ThreatIntelIndicators, ThreatIntelObjects)
   - Validation flows (pre-built scripts exercising each API surface)

### Skills Architecture — What Each Skill Covers

| Skill | API Surface | Key Patterns |
|---|---|---|
| **stix-api-operations** | ARM STIX API (modern) | PUT with `x-ms-arm-resource-system-data` header, etag-based optimistic concurrency, document ID construction (`base64(source)---stixId`), query with filter conditions, poll for async arrival |
| **ingestion-api** | TI Ingestion API | POST bulk upload of flat STIX objects (not ARM-wrapped), async with 15–150s delay, uses different base URL (`api.ti-ppe.sentinel.azure.com`) |
| **file-import-api** | File Import API | Three-step flow: PUT to create import → upload blob via SAS URI → poll state machine (WaitingForUpload → Ingesting → Ingested). CRITICAL: use `ConvertTo-Json -InputObject` not pipe to avoid single-element array unwrap. |
| **bulk-actions-api** | Bulk Actions API | Async PUT with condition (stixObjectType + clauses) and operation (Edit/Delete). Mutator types: SetValue, SetTrue, SetFalse, AppendValue, RemoveValue. Poll until Done/Failed. |
| **la-query** | Log Analytics | Two tables: ThreatIntelIndicators (indicators), ThreatIntelObjects (all other STIX types). Poll-based verification with retries. LA token is separate from ARM token. |
| **stixwebapi-operations** | Legacy StixWebApi | Augusta-based indicator CRUD (filter, batch create/delete/tag, TAXII client management). Different API surface from modern ARM STIX API. |

### Design Patterns Worth Emulating

1. **Skills as reusable building blocks**: Each API surface has its own skill doc with complete code patterns. The agent references skills rather than inlining everything.

2. **PPE config externalization**: Canonical config in `ti-config.ps1` with dot-sourcing pattern. Environment-specific values in one place.

3. **Token acquisition pattern**: ARM token for most APIs, separate LA token for Log Analytics. Cached per session.

4. **Validation as confidence builder**: Pre-built scripts that exercise each API surface end-to-end. Run `all` to validate the full pipeline. Good pattern for TI backend engineer confidence.

5. **Document ID as universal key**: The `{base64(source)}---{stixId}` pattern is used everywhere — STIX API, LA queries, bulk actions, file imports. Understanding this is fundamental.

### What Galadriel's Review Found

**Review v1** (couldn't read files — metadata only):
- Zero human reviewers assigned (process gap)
- Polling loops don't handle `Failed` state (spins for 75s per operation)
- `return` instead of `exit 1` on token failure (silent failures)
- Potential PPE secrets in config

**Review v2** (could read actual files):
- Confirmed BUG-1: `Run-BulkAction` poll loop wastes 15 minutes on full `Failed` run
- Confirmed BUG-2: Two files use `return` instead of `exit 1`; four others correctly use `exit 1`
- Found SEC-1: Hardcoded PPE subscription/workspace IDs (mitigated by being internal/PPE)
- Found BUG-3: `Poll-LAQuery` silently swallows all exceptions for 7.5 minutes
- Found undescribed scope: C# Sightings implementation + WorkspaceStatusUpdaterServices in same PR
- Positive: well-structured separation of concerns, shared infra, consistent output contract

---

## 5. What a TI Backend Engineer Actually Does Day-to-Day

Based on the repo inventory, service architecture, and the TiExpert agent's capabilities:

### Primary Work Areas

1. **STIX API Development** (Sentinel-TiPipeline)
   - Building/modifying REST APIs for STIX object CRUD
   - Adding new STIX object types (e.g., the Sightings type in the TiExpert PR)
   - API versioning and backward compatibility
   - ARM integration (subscription/resourceGroup/workspace scoping)

2. **Ingestion Pipeline** (Sentinel-TiPipeline + TiAutomation)
   - Processing flow: GatewayService → IngestionAPI → Event Hub → TiNormalization
   - Ingestion rules, validation, normalization
   - File import flow (SAS URI blob upload → parse → ingest)
   - Connector management for external feeds (TAXII, MDTI)

3. **Publishing & Storage** (Sentinel-TiPublishers)
   - CosmosDbPublisher: writing normalized STIX to Cosmos DB
   - LogAEventHubPublisher: forwarding to customer Log Analytics
   - LogARepublisher: periodic Detection/Hunting republish cycles
   - Managing consumer groups and Event Hub processing

4. **Automation & Bulk Operations** (Sentinel-TiAutomation)
   - Bulk edit/delete via Durable Functions
   - Ingestion rules CRUD
   - STIX query APIs

5. **TI Matching** (Sentinel-ThreatIntelligenceMatching + Amba.TIMatching)
   - Synapse Spark jobs comparing events vs indicators
   - BBTI matching for MDE/MDATP
   - Alert generation

6. **Watchlist Management** (Sentinel-Watchlist)
   - Watchlist CRUD APIs
   - Large CSV processing pipeline
   - Sync to Log Analytics
   - Confidential watchlists

7. **Augusta/Legacy STIX** (Sentinel-Augusta + SecEng-Augusta)
   - StixWebApi maintenance
   - Service Fabric actors (TAXII, ISG, MSFeed, BlackBox connectors)
   - STIX.NET / TAXII.NET library updates

8. **Infrastructure & Deployment** (shared infra repos)
   - EV2 deployments
   - ConfigGen resource provisioning
   - Multi-region rollout
   - Managed identities, Key Vault, Event Hub namespace management

### Key Technologies a TI Engineer Must Know

- **C# / .NET** (primary language for all services)
- **Azure Functions** (Premium plan, Durable Functions, EventHub/ServiceBus/Timer triggers)
- **Cosmos DB** (partitioned by workspace, change feed, overflow patterns)
- **Event Hubs** (consumer groups, checkpointing, ingestion flow)
- **Service Bus** (queues, topics, sessions for ordered processing)
- **Azure Blob Storage** (SAS URIs, file import staging)
- **Log Analytics / KQL** (querying TI tables, Scuba publishing)
- **ARM APIs** (Azure Resource Manager patterns, API versioning)
- **EV2** (deployment pipelines, multi-region rollout)
- **Service Fabric** (Augusta legacy services)
- **Synapse / Spark** (TI matching jobs, Python + PySpark)
- **Kubernetes** (BBTI matching K8s pods)
- **STIX 2.1** (the data model underlying everything)

### Cross-Cutting Concerns

- **Multi-tenancy**: Everything is scoped by workspace ID. All queries, storage, and APIs are workspace-partitioned.
- **Async processing**: Most operations are async — ingestion has 15–150s latency; bulk actions poll until Done; file imports have multi-stage state machines.
- **Secret management**: All services use Key Vault for secrets/certificates. Managed identities for service-to-service auth.
- **Monitoring**: Geneva (MDM/MDS) metrics, Application Insights, Kusto telemetry clusters.
- **Testing**: Synthetic monitoring (Sentinel-Synthetics), validation scripts (TiExpert), integration tests against PPE.

---

## 6. Implications for Frodo's Charter

Based on this research, Frodo's charter should:

1. **Cover all 26 repos under `C:\dev\ti`**, not just two. The domain is broad — from core pipeline services to matching engines to watchlists to ML anomaly detection.

2. **Understand the four-layer architecture**: Ingestion → Processing → Publishing → Matching. Code changes in any layer need awareness of upstream/downstream impacts.

3. **Know the STIX data model deeply**: Document ID construction, the 6 STIX object types, the ARM API surface, the difference between modern (StixAPIs) and legacy (StixWebApi) paths.

4. **Have operational skills modeled on TiExpert**: Sagi's agent shows the API patterns, token acquisition, and validation flows that define day-to-day TI engineering. Frodo should be able to help with STIX API operations, not just read code.

5. **Understand the infrastructure**: Event Hub topology, Cosmos DB partitioning, Service Bus work queues, EV2 deployment. These are not just "infra" — they're core to how the pipeline works.

6. **Be aware of the legacy/modern split**: Augusta (Service Fabric + StixWebApi) and the modern pipeline (Azure Functions + ARM STIX API) coexist. Both write to the same Event Hub. Understanding this is critical for debugging and development.

7. **Know the domain vocabulary**: Terms like "normalization", "consolidation", "republish", "Scuba", "BBTI", "Dakota", "Augusta" have specific meanings in this codebase. Frodo needs a glossary.

---

## Sources

- `C:\dev\ti\Sentinel-TiCommon\Documentation\AI-Generated\System-Understanding\Map.md` — 536-line service map
- `C:\dev\ti\Sentinel-TiCommon\Documentation\AI-Generated\System-Understanding\Repositories.md` — repo inventory
- `C:\dev\ti\Sentinel-TiCommon\Documentation\AI-Generated\System-Understanding\ServiceGraph.json` — 990-line service connection graph
- `C:\dev\ti\Sentinel-TiCommon\Documentation\AI-Generated\System-Understanding\ServiceGraph-Plan.md` — graph generation plan
- `C:\dev\ti\Sentinel-TiPipeline` branch `features/sagimarus/tiexpertagent` — TiExpert agent definition and 6 skills
- `docs/reviews/pr-review-15064785.md` — Galadriel review v1
- `docs/reviews/pr-review-15064785-v2.md` — Galadriel review v2
- Direct filesystem survey of all 26 repos under `C:\dev\ti`
