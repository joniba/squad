---
title: ".NET Aspire + MCP Integration for Squad Distributed System Observability"
date: "2025-01-16"
author: "Elrond (Researcher)"
status: "Research Complete"
tags: ["aspire", "mcp", "observability", "distributed-systems", "squad", "port-isolation"]
---

## Executive Summary

.NET Aspire is a transformational platform for AI-driven distributed system development. By combining Aspire's orchestration capabilities with the Model Context Protocol (MCP), Squad agents gain **superpowers**: they can orchestrate entire distributed systems (services, databases, message queues, frontends) from code, query resource health and telemetry via MCP tools, and reason about system-wide behavior in real-time.

**Key Finding:** The primary blocker for scaling Squad to parallel multi-worktree development is **port isolation**. Aspire's AppHost tries to bind to fixed ports (18888-18890 for Aspire infrastructure, plus application endpoints), causing conflicts when multiple worktrees run simultaneously. **Solution:** Dynamic port allocation via automation scripts + an MCP proxy indirection layer ensures each worktree instance has isolated infrastructure.

**Recommendation:** Adopt Aspire + MCP for Squad infrastructure observability, implementing Tamir Dresher's port isolation pattern. This enables true parallel multi-agent development workflows and provides Squad agents system-wide visibility into distributed tracing, logs, and health checks.

---

## 1. What is .NET Aspire?

### 1.1 Architecture Overview

.NET Aspire is a modern orchestration and observability platform that abstracts away environment complexity for distributed .NET applications. It consists of three layers:

1. **AppHost Project** (entry point): Developers declare component wiring in `Program.cs`
   - What services exist (APIs, databases, caches, queues, frontends)
   - How services reference each other
   - External service integrations

2. **Aspire Dashboard** (real-time monitoring): A web-based UI showing:
   - Resource status and health checks
   - Distributed tracing via OpenTelemetry
   - Structured logs and metrics
   - Service topology and dependencies

3. **MCP Server** (agent integration): Exposes Aspire internals to AI agents via Model Context Protocol
   - Query resource status, logs, and traces
   - Retrieve distributed tracing data
   - Run system-wide diagnostics
   - Execute orchestration commands

### 1.2 Why Aspire Transforms AI-Assisted Development

**Traditional AI-assisted coding:** Agent modifies a single file, runs a command, tests a component in isolation.

**With Aspire:** Agent spins up entire distributed systems in code:
- Backend APIs (C#, Python, Node.js)
- Databases (PostgreSQL, MongoDB, etc.)
- Message brokers (RabbitMQ, Redis)
- Caches and in-memory stores
- Frontend applications (React, Vue, etc.)
- All networking and service discovery automatic

A single `Program.cs` with 15-20 lines creates a production-like environment. Agents can modify this, run it, test the whole system, iterate autonomously.

**Example from Tamir's worktrees-example:**

```csharp
var builder = DistributedApplication.CreateBuilder(args);
var cache = builder.AddRedis("cache");
var db = builder.AddPostgres("db").AddDatabase("notetakerdb");
var messaging = builder.AddRabbitMQ("messaging");

var backend = builder.AddProject<Projects.Backend>("backend")
    .WithReference(cache)
    .WithReference(db)
    .WithReference(messaging)
    .WithHttpEndpoint(name: "http")
    .WithExternalHttpEndpoints();

var aiService = builder.AddPythonApp("ai-service", "../ai-service", "main.py")
    .WithReference(db)
    .WithReference(messaging)
    .WithHttpEndpoint(env: "PORT", name: "http");

builder.AddJavaScriptApp("frontend", "../frontend")
    .WithReference(backend)
    .WithHttpEndpoint(env: "PORT");

builder.Build().Run();
```

This spins up: Redis, PostgreSQL, RabbitMQ, C# backend, Python AI service, JavaScript frontend—all orchestrated and networked together. An agent can test the entire system end-to-end without manual setup.

---

## 2. MCP Integration: System-Wide Visibility for Agents

### 2.1 What is MCP in the Aspire Context?

**Model Context Protocol (MCP)** is a communication standard that allows AI agents to interact with external systems. Aspire exposes its monitoring and orchestration capabilities via MCP tools.

### 2.2 MCP Tools Available from Aspire Dashboard

**Query Tools:**
- `list_resources` — get status of all services, containers, executables (healthy, degraded, stopped, etc.)
- `list_console_logs` — retrieve logs for a specific resource (great for debugging startup failures)
- `list_traces` — access distributed tracing data; trace request flows across all services
- `get_resource_representation` — get full config and current state of a resource

**Orchestration Tools:**
- Control resource lifecycle (start, stop, restart)
- Trigger health checks manually
- Update configuration in real-time

### 2.3 Example: Agent Troubleshooting Workflow

```
Agent: "Check if all resources are healthy"
  → Uses list_resources()
  → Response: {backend: "healthy", db: "healthy", cache: "healthy", ai_service: "degraded"}

Agent: "Why is the AI service degraded?"
  → Uses list_console_logs("ai_service")
  → Response: "Error: PYTHONPATH not set. Cannot import module 'transformers'."
  → Analyzes root cause: environment variable missing

Agent: "Fix the PYTHONPATH issue"
  → Modifies AppHost to add environment variable
  → Restarts ai_service
  → Uses list_resources() to confirm health → "healthy"

Agent: "Show traces for the last request"
  → Uses list_traces()
  → Response: Full distributed trace showing path through backend → cache → ai_service
  → Latency breakdown by service visible
```

This is transformative: instead of debugging one component at a time, agents reason about the **entire system**, following request flows across services, correlating failures.

---

## 3. The Port Conflict Problem: Blocker for Parallel Development

### 3.1 The Challenge

When running Aspire AppHost, it binds to fixed ports:
- **Port 18888** — Aspire Dashboard
- **Port 18889** — OTLP (OpenTelemetry Protocol) endpoint
- **Port 18890** — Resource Service endpoint  
- **Port 4317** — MCP server endpoint
- **Plus application-specific ports** (e.g., backend API on 5000, frontend on 3000)

When you try to run multiple worktrees in parallel (as Squad does for feature isolation), every AppHost instance fights over the same ports:

```
Terminal 1 (feature-auth worktree):
  cd worktrees/feature-auth/src/NoteTaker.AppHost
  dotnet run
  ✅ SUCCESS — Dashboard on 18888

Terminal 2 (feature-payments worktree):
  cd worktrees/feature-payments/src/NoteTaker.AppHost  
  dotnet run
  ❌ ERROR: Port 18888 already in use
  ❌ ERROR: Port 18889 already in use
  ❌ ERROR: Port 18890 already in use
```

**You cannot run the second AppHost at all.** This kills parallel development.

### 3.2 Why Manual Workarounds Don't Scale

Teams might try:
- Manually editing ports for each worktree (tedious, error-prone)
- Using environment variables per-worktree (requires tracking which port maps to which worktree)
- Running only one AppHost at a time (defeats the purpose of parallel development)

**Biggest problem:** Your agent's MCP connection needs to know which AppHost instance to connect to. If you hard-code MCP port 18890 in `.roo/mcp.json`, it only works for one worktree!

---

## 4. Solution: Port Isolation + MCP Proxy Pattern

Tamir Dresher's solution (detailed in his blog post) has two layers:

### 4.1 Layer 1: Automatic Port Allocation

**PowerShell/Bash scripts** that:
1. Find free ports using .NET's `IPGlobalProperties` API
2. Set environment variables for Aspire infrastructure components
3. Launch AppHost with those unique ports
4. Save port configuration to `scripts/settings.json` for later reference
5. Display dashboard URL and process ID for monitoring

**Output example:**
```
Starting Aspire AppHost with isolated ports...
✅ Dashboard:    http://localhost:54772/dashboard
✅ OTLP:         http://localhost:54773
✅ Resource SVC: http://localhost:54774
✅ MCP Server:   http://localhost:54775 (key: abc123...)
✅ Backend API:  http://localhost:54776
✅ AppHost PID:  12345
```

Each worktree gets unique ports. No conflicts.

### 4.2 Layer 2: The MCP Proxy Pattern

**Problem:** Your `.roo/mcp.json` agent configuration has fixed port references. How does it know which AppHost to connect to?

```json
{
  "mcpServers": {
    "aspire-dashboard": {
      "type": "http",
      "url": "http://localhost:18890/mcp",     // ❌ This port is wrong!
      "headers": {
        "x-mcp-api-key": "McpKey"              // ❌ This key is wrong!
      }
    }
  }
}
```

**Solution:** Introduce an indirection layer:

```
┌─────────────┐         ┌──────────────────┐         ┌─────────────────┐
│ Squad Agent │ stdio   │ aspire-mcp-proxy │  HTTP   │ Aspire AppHost  │
│             ├────────►│ (fixed config)   ├────────►│ (dynamic port)  │
│             │         │                  │         │                 │
└─────────────┘         └──────────────────┘         └─────────────────┘
                              ↓
                    reads from scripts/settings.json
                    (updated by start-apphost.ps1)
```

**How it works:**

1. **Agent connects via stdio** to `aspire-mcp-proxy.cs` (always the same configuration)
2. **Proxy reads `scripts/settings.json`** to discover current AppHost's MCP port and API key
3. **Proxy forwards MCP requests** to the correct dynamic port via HTTP
4. **Responses flow back** through the proxy to the agent

**In agent configuration (`.roo/mcp.json`):**
```json
{
  "mcpServers": {
    "aspire-mcp": {
      "command": "dotnet",
      "args": ["scripts/aspire-mcp-proxy.cs", "--no-build"],
      "description": "Aspire Dashboard MCP stdio proxy"
    }
  }
}
```

**The bridge file (`scripts/settings.json`) is updated every time you start AppHost:**
```json
{
  "port": "54775",
  "apiKey": "generatedKeyPerInstance",
  "lastUpdated": "2025-01-16T10:30:00Z"
}
```

**The proxy is written as a single, self-contained C# file (272 lines):**
- Acts as both an MCP client (talking to Aspire)
- Acts as both an MCP server (talking to Squad agents)
- Uses the official `ModelContextProtocol@0.4.1-preview.1` NuGet package
- Leverages .NET 10's single-file script feature (`dotnet run app.cs` directly)

**Result:** Agent configuration is **always fixed**. The proxy handles dynamic discovery transparently.

### 4.3 Putting It All Together

Workflow when you have multiple worktrees running in parallel:

```
1. Terminal 1: cd worktrees/feature-auth && ./scripts/start-apphost.ps1
   ✅ AppHost starts on ports 54772-54776
   ✅ settings.json updated with these ports
   ✅ Squad agent can connect via MCP proxy

2. Terminal 2: cd worktrees/feature-payments && ./scripts/start-apphost.ps1
   ✅ AppHost starts on ports 61450-61454 (next available range)
   ✅ settings.json updated with NEW ports
   ✅ Squad agent can still connect via proxy (proxy reads updated settings.json)

3. Now both worktrees run in parallel with NO port conflicts
```

The proxy pattern is elegant: it decouples agent configuration (fixed) from infrastructure deployment (dynamic).

---

## 5. Distributed Tracing & Health Checks in Aspire

### 5.1 Distributed Tracing: OpenTelemetry by Default

**Out-of-the-box integration:**
- Every HTTP request, database call, message queue operation is automatically traced
- Aspire uses OpenTelemetry Protocol (OTLP) for telemetry collection
- Traces propagate across service boundaries, correlating requests through all layers

**How it works:**
1. Service A handles a request
2. Service A calls Service B (trace context propagated via HTTP headers)
3. Service B calls the database (trace context propagated via database wire protocol)
4. Aspire Dashboard receives all traces and displays complete request flow

**Agent access via MCP:**
```
Agent: "Show me traces for requests over 500ms"
  → Uses list_traces() with filter
  → Response: All slow requests with full flame graphs
  → Agent can identify bottleneck (e.g., database query in Service B)
```

**Configuration:** Use `builder.AddServiceDefaults()` in your service's `Program.cs`:
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();  // ← Sets up OpenTelemetry automatically
builder.Services.AddScoped<MyBusinessLogic>();
var app = builder.Build();
app.MapDefaultEndpoints();     // ← Health checks, traces, metrics
app.Run();
```

### 5.2 Health Checks: Orchestration-Aware

**Two probe endpoints:**
- `/health` — Full readiness. Orchestrators use this to know when to route traffic.
- `/alive` — Liveness. Process is up, even if not ready.

**In AppHost (orchestration layer):**
```csharp
var db = builder.AddPostgres("db")
    .WithHealthCheck("postgres");  // ← Wait for DB to be healthy
    
var backend = builder.AddProject<Projects.Backend>("backend")
    .WithReference(db)             // ← Backend depends on DB
    .WithHealthCheck();            // ← Monitor backend health
```

Aspire won't start dependent services until their dependencies are healthy.

**Agent access:**
```
Agent: "Is the database ready?"
  → Uses list_resources()
  → Response: {db: "healthy", postgres: "healthy", backend: "healthy"}

Agent: "Why isn't the backend responding?"
  → Uses get_resource_representation("backend")
  → Response: Health probe details, error logs, current state
```

---

## 6. Four Strategies for Port Isolation (Trade-offs)

| Strategy | Pros | Cons | Effort |
|----------|------|------|--------|
| **Tamir's Script + MCP Proxy** | Fully automated, zero agent config changes, works with existing Aspire, elegant indirection | Requires external scripts + proxy binary, maintenance burden | Medium |
| **Environment Variable Override** | Simple, no external scripts | Each worktree must remember to set env vars, error-prone, agent config must know ports | Low |
| **Docker-Compose Per Worktree** | Containers ensure isolation, production-like | Heavy overhead, slow startup, requires Docker | High |
| **Aspire CLI `--isolated` Flag (Future)** | Native support, no scripts needed, Aspire team owns it | Not available yet, requires Aspire team implementation | N/A (proposed) |

**Recommendation:** Start with **Tamir's Script + MCP Proxy** pattern. It's fully automated and transparent to agents. When Aspire team adds built-in `--isolated` support, migrate to that.

---

## 7. Squad Infrastructure Integration Recommendations

### 7.1 Architecture

```
┌─────────────────────────────────────┐
│ Squad Worktree (feature-auth)       │
├─────────────────────────────────────┤
│ src/                                │
│   NoteTaker.AppHost/Program.cs      │
│   Backend/                          │
│   AI-Service/                       │
├─────────────────────────────────────┤
│ scripts/                            │
│   start-apphost.ps1 (port alloc)    │
│   aspire-mcp-proxy.cs               │
│   settings.json (dynamic ports)     │
└─────────────────────────────────────┘
          ↓ starts
┌─────────────────────────────────────┐
│ Aspire AppHost Runtime              │
│ (ports 54772-54776, isolated)       │
├─────────────────────────────────────┤
│ Resources:                          │
│  • PostgreSQL (port 54777)          │
│  • Redis (port 54778)               │
│  • Backend API (port 54779)         │
│  • Dashboard (port 54772)           │
│  • MCP Server (port 54775)          │
└─────────────────────────────────────┘
          ↓ exposes via
┌─────────────────────────────────────┐
│ MCP Protocol (stdio + HTTP)         │
├─────────────────────────────────────┤
│ Tools available to agents:          │
│  • list_resources                   │
│  • list_console_logs                │
│  • list_traces                      │
│  • get_resource_representation      │
└─────────────────────────────────────┘
          ↑ used by
┌─────────────────────────────────────┐
│ Squad Agents (Ralph, Data, Seven)   │
├─────────────────────────────────────┤
│ .roo/mcp.json configured with       │
│ aspire-mcp-proxy (fixed config)     │
└─────────────────────────────────────┘
```

### 7.2 Implementation Steps

1. **Add Aspire AppHost to Squad Worktrees**
   - Create `src/NoteTaker.AppHost/Program.cs`
   - Define services: backend APIs, Python agents, databases, caches, message queues
   - Use `builder.AddServiceDefaults()` for automatic observability

2. **Implement Port Allocation Scripts**
   - Copy `scripts/start-apphost.ps1` (Windows) and `start-apphost.sh` (macOS/Linux)
   - Scripts find free ports, set env vars, launch AppHost, save `settings.json`

3. **Deploy MCP Proxy**
   - Copy `scripts/aspire-mcp-proxy.cs` (or use Tamir's reference implementation)
   - Ensures agents connect to correct AppHost instance dynamically

4. **Configure Agent MCP in `.roo/mcp.json`**
   - Add `aspire-mcp` server pointing to proxy script
   - No need to know or hard-code port numbers

5. **Update Squad Agent Charters**
   - Teach Data (Code Expert), Ralph (Issue Monitor), Seven (Research) about Aspire capabilities
   - Add Aspire MCP tools to their skill sets

### 7.3 Observability Benefits for Squad

**Before (without Aspire):**
- Agents troubleshoot services individually
- No distributed tracing across services
- Health status opaque; manual checking required
- Logs scattered across separate files

**After (with Aspire + MCP):**
- Agents see entire system state via `list_resources()`
- Distributed traces show request paths across all services via `list_traces()`
- Health checks orchestrated automatically
- Centralized logging and metrics in Aspire Dashboard
- Agents can reason about system-wide bottlenecks, failures, and performance

**Concrete example:**
```
Ralph (Issue Monitor) sees a failing test in CI/CD.
Instead of waiting for logs, Ralph spins up the AppHost:
  1. Runs ./scripts/start-apphost.ps1
  2. Connects to MCP via proxy (fixed config)
  3. Calls list_resources() → Sees which service is unhealthy
  4. Calls list_console_logs("backend") → Finds error
  5. Calls list_traces() → Shows full request path to pinpoint latency
  6. Proposes fix based on full system context
```

---

## 8. Production Readiness Assessment for ms-pa

### 8.1 Aspire + MCP Maturity

| Dimension | Status | Notes |
|-----------|--------|-------|
| Aspire Framework | ✅ GA | .NET 8+, production-ready, Microsoft supported |
| MCP Support in Aspire | ✅ Production | Aspire Dashboard MCP server fully featured |
| OpenTelemetry Integration | ✅ Battle-tested | Industry standard, widely deployed |
| Port Isolation Solution | ⚠️ Community-driven | Tamir's workaround works; awaiting built-in Aspire CLI support |
| .NET Aspire CLI `--isolated` | 🔄 Proposed | GitHub issue #13932; community feedback strong |

### 8.2 What ms-pa Needs to Succeed

✅ **Already Available:**
- Aspire framework (GA)
- MCP protocol support in Aspire Dashboard
- OpenTelemetry integration
- Reference implementation from Tamir Dresher (open source)

⚠️ **Needs Implementation:**
- Port allocation scripts for your worktree structure
- MCP proxy binary (or use Tamir's reference)
- Squad agent charter updates (teach agents about Aspire tools)
- Integration tests (verify MCP tools work reliably)

❌ **Not Yet Available (But Not Blocking):**
- Built-in Aspire CLI `--isolated` flag (would simplify port allocation)
- Native support for multiple AppHost instances in Dashboard (workaround: separate dashboards per port)

### 8.3 Go/No-Go Decision

**GO: Adopt Aspire + MCP for ms-pa Squad infrastructure.**

**Rationale:**
1. Aspire is production-ready (GA, Microsoft-backed)
2. MCP integration is robust (industry standard)
3. Port isolation solution exists (Tamir's pattern proven, automated)
4. Benefits are substantial (system-wide visibility, parallel development, agent autonomy)
5. Maintenance burden is acceptable (scripts are <300 lines total)

**Risks & Mitigations:**
- **Risk:** Aspire team adds `--isolated` flag; current scripts become obsolete
  - **Mitigation:** Tamir's pattern will still work; migrate to native support when available
- **Risk:** MCP proxy requires maintenance
  - **Mitigation:** Use Tamir's reference implementation (open source); minimal changes needed
- **Risk:** Agents need training on new MCP tools
  - **Mitigation:** Update agent charters; tools are well-documented

---

## 9. References & Resources

### Blog Posts & Articles
- **Tamir Dresher: Scaling AI Agents with Aspire (Port Isolation Pattern)**
  - https://www.tamirdresher.com/blog/2025/12/16/scaling-ai-agents-with-aspire-isolation.html
  - Covers port allocation, MCP proxy, worktrees integration in detail

- **Tamir Dresher: Organized by AI (Squad Infrastructure Patterns)**
  - https://www.tamirdresher.com/blog/2026/03/10/organized-by-ai
  - Real-world Squad setup with Aspire, tooling, and workflows

### Code References
- **Tamir's Worktrees Example (Reference Implementation)**
  - https://github.com/tamirdresher/worktrees-example
  - `scripts/start-apphost.ps1` (port allocation)
  - `scripts/aspire-mcp-proxy.cs` (MCP proxy)
  - `src/NoteTaker.AppHost/Program.cs` (Aspire orchestration)

- **Tamir's Squad Personal Demo**
  - https://github.com/tamirdresher/squad-personal-demo
  - Full Squad setup with Aspire integration

- **Tamir's Squad Monitor**
  - https://github.com/tamirdresher/squad-monitor
  - Real-time monitoring dashboard for Squad agents

### Official Documentation
- **Aspire Dashboard & MCP Configuration**
  - https://aspire.dev/get-started/configure-mcp/
  - Official guide for enabling MCP in Aspire

- **Aspire Health Checks**
  - https://aspire.dev/fundamentals/health-checks/
  - Patterns for orchestration-aware health probes

- **GitHub Issue: Built-in Support for Isolated Multi-Instance AppHost**
  - https://github.com/dotnet/aspire/issues/13932
  - Aspire team's response to port isolation feedback

### Related Research
- **Multi-Agent System Observability (LumiMAS, Agent Squad, Microsoft Foundry)**
  - Three-layered monitoring: logging, anomaly detection, root-cause analysis
  - Structured logging, distributed tracing, parent trace linking essential
  - Key metrics: call counts, allow/deny ratios, latency, throughput, token usage

---

## 10. Conclusion

.NET Aspire, combined with Model Context Protocol, is the right foundational technology for scaling Squad to distributed system observability. The port isolation problem, which seemed blocking, has a proven solution (Tamir Dresher's pattern). Adopting this approach enables:

1. **True Parallel Development** — Multiple worktrees with full stacks running simultaneously
2. **System-Wide Agent Visibility** — Squad agents query logs, traces, and health across all services
3. **Autonomous Troubleshooting** — Agents reason about distributed failures without human intervention
4. **Production-Ready Observability** — OpenTelemetry tracing and metrics built-in, Aspire Dashboard provides real-time monitoring

**Next Steps for ms-pa:**
1. Prototype Aspire integration in a Squad worktree (2-3 days)
2. Implement port allocation scripts from Tamir's reference (1 day)
3. Deploy MCP proxy and test agent connectivity (1 day)
4. Update agent charters to leverage Aspire MCP tools (2 days)
5. Integration testing and documentation (3 days)

**Estimated Timeline to Production:** 10-14 days of focused development work.

---

*Research completed by Elrond, Squad Researcher, January 16, 2025*

*Sources: Tamir Dresher blog posts, official Aspire documentation, multi-agent observability research (LumiMAS, Agent Squad, Microsoft Foundry), community feedback on port isolation patterns.*
