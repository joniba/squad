# DGrep CLI — Project Plan

**Lead:** Gandalf  
**Research:** Elrond (`docs/research/geneva-dgrep-research.md`)  
**Implementation:** Gimli  
**Review:** Galadriel  
**Requested by:** Jonathan  
**Created:** 2026-03-23

---

## Overview

Build a cross-platform CLI tool for querying Geneva DGrep (Distributed Grep) — the unindexed log search engine used across Microsoft. Today, DGrep is only accessible via the Jarvis web UI or by writing .NET code against the DGrep SDK. There is **no CLI tool** anywhere in the ecosystem. Our CLI fills that gap.

### Goals

1. **`dgrep search`** — run KQL/MQL queries from the command line with structured output (table, JSON, CSV, TSV, JSONL)
2. **`dgrep stream`** — stream results in real-time as they arrive
3. **`dgrep export`** — export results to CSV via server-side blob export
4. **`dgrep config`** — manage endpoints, namespaces, defaults
5. **`dgrep saved`** — file-based saved query management
6. Cross-platform (Windows, Linux, macOS)
7. Auth via `az login` / `DefaultAzureCredential`, certificates, device code flow
8. Eventually publishable to its own repo and npm

### Non-Goals (for now)

- GUI or TUI
- Query editor with autocomplete
- Namespace/event discovery (future)

---

## Architecture Decisions

Based on Elrond's research:

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Language | **TypeScript** | Cross-platform, Azure Identity SDK parity, team already uses Node.js |
| CLI Framework | **Commander.js** | Mature, lightweight, well-documented (oclif is heavier than we need) |
| HTTP Client | **undici** (Node built-in fetch) | Modern, streaming support, no extra deps |
| Auth | **@azure/identity** | DefaultAzureCredential, device code, cert-based |
| Output | **cli-table3** + custom formatters | Table, JSON, CSV, TSV, JSONL |
| Config | **cosmiconfig** | Standard config discovery (.dgreprc, dgrep.config.json, etc.) |
| Testing | **vitest** | Fast, TypeScript-native, good mocking |
| Build | **tsup** | Fast bundling for CLI distribution |
| Package Manager | **npm** | Standard, publishable |

### Key Technical Risks

1. **REST API is undocumented** — DGrep SDK is the only documented interface. Must reverse-engineer the HTTP protocol. This is the **critical path blocker**.
2. **Auth token format** — Geneva uses dSTS. Unknown if `@azure/identity` tokens are accepted by the DGrep frontend.
3. **Streaming protocol** — Unknown mechanism (WebSocket? SSE? Chunked HTTP?).
4. **Rate limits** — 5 concurrent requests per user. CLI must guarantee cleanup on Ctrl+C / crash.

---

## Phased Work Items

### Phase 0: API Discovery Spikes (BLOCKED — research required)

These must complete before core query flow can be implemented. Assigned to **Elrond** (research with claude-opus-4.6).

| # | Item | Owner | Dependencies | Notes |
|---|------|-------|-------------|-------|
| 0.1 | **Spike: Capture DGrep REST API** | Elrond | None | Use .NET SDK + Fiddler/mitmproxy to capture HTTP traffic to `dgrepv2-frontend-prod.trafficmanager.net`. Document: endpoints, request format, headers, response format, streaming protocol. |
| 0.2 | **Spike: Auth token format** | Elrond | None | Determine if `@azure/identity` tokens work or if custom dSTS flow is needed. Test with `DefaultAzureCredential` against the DGrep frontend. |

### Phase 1: Foundation (CAN START NOW — no API dependency)

Everything here can be built with mock data and tested without hitting the real API. Assigned to **Gimli**.

| # | Item | Owner | Dependencies | Issue |
|---|------|-------|-------------|-------|
| 1.1 | **Project scaffolding** | Gimli | None | package.json, tsconfig, eslint, vitest config, tsup build, bin entry point |
| 1.2 | **CLI command structure** | Gimli | 1.1 | Commander.js setup with all commands (search, stream, export, config, saved, endpoints). Flags from Elrond's design. No implementation yet — just the command tree and --help output. |
| 1.3 | **TypeScript types & interfaces** | Gimli | 1.1 | QueryInput, QueryResult, RowSetResult, EventFilter, IdentityColumn, etc. Derived directly from SDK docs in Elrond's research. |
| 1.4 | **Output formatters** | Gimli | 1.3 | Table, JSON, CSV, TSV, JSONL formatters. Accept RowSetResult, emit formatted text to stdout. Full test coverage. |
| 1.5 | **Time range parser** | Gimli | 1.1 | Parse relative times (--from -30m, --from -2h, --from -1d) and absolute ISO timestamps. Full test coverage. |
| 1.6 | **Config management** | Gimli | 1.1 | cosmiconfig integration. Schema: endpoints map, default namespace, default endpoint. CRUD operations for `dgrep config set/get/list`. |
| 1.7 | **Saved query management** | Gimli | 1.3, 1.6 | File-based saved queries (.dgrep/queries/*.json). Save, list, run, delete. Each query stores: name, endpoint, namespace, event, identities, query text, query type. |

### Phase 2: Core Query Flow (BLOCKED on Phase 0)

| # | Item | Owner | Dependencies | Notes |
|---|------|-------|-------------|-------|
| 2.1 | **HTTP transport layer** | Gimli | 0.1, 1.3 | Implement the REST API calls based on Elrond's spike findings. QueryInput → HTTP POST → parse response. |
| 2.2 | **Auth layer** | Gimli | 0.2 | DefaultAzureCredential integration. Certificate support. Device code flow. Token caching. |
| 2.3 | **Search command (end-to-end)** | Gimli | 2.1, 2.2, 1.4, 1.5 | Wire everything together: parse CLI args → build QueryInput → authenticate → POST → format output. |
| 2.4 | **Stream command** | Gimli | 2.1, 2.2 | Real-time result streaming based on the protocol discovered in spike 0.1. |
| 2.5 | **Export command** | Gimli | 2.1, 2.2 | CSV blob export. Return SAS URI or download file. |
| 2.6 | **Graceful shutdown & cleanup** | Gimli | 2.1 | Ctrl+C handler. CloseAsync equivalent. Guarantee query cleanup to avoid quota exhaustion. |

### Phase 3: Polish (After Phase 2)

| # | Item | Owner | Dependencies | Notes |
|---|------|-------|-------------|-------|
| 3.1 | **KQL validation/linting** | Gimli | 1.3 | Warn on unsupported operators (has, let, mv-expand). Suggest DGrep equivalents. |
| 3.2 | **Endpoint discovery** | Gimli | 2.1 | `dgrep endpoints` — list known MDS endpoints with environment labels. |
| 3.3 | **Cross-platform testing** | Gimli | 2.3 | Verify on Windows, Linux, macOS. CI matrix. |
| 3.4 | **Documentation** | Bilbo | 2.3 | README with examples, KQL cheat sheet, troubleshooting guide. |
| 3.5 | **Standalone binary** | Gimli | 2.3 | Package with pkg or esbuild for single-binary distribution. |

---

## What Can Start NOW

Gimli can immediately begin Phase 1 items (1.1–1.7). These are pure TypeScript work with no API dependency. The types come from Elrond's SDK documentation, the output formatters work on mock data, and the CLI structure is fully defined by the research.

**Phase 0 spikes** (Elrond) can run in parallel — when they complete, Phase 2 unblocks.

---

## Issue Tracking

All issues on `jbenami_microsoft/ms-pa` repo, labeled `squad` + `squad:{agent}`.

---

## Directory Structure

```
tools/dgrep-cli/
├── PLAN.md              # This file
├── README.md            # Project description & usage
├── package.json         # Node.js project config
├── tsconfig.json        # TypeScript config
├── vitest.config.ts     # Test config
├── .gitignore           # node_modules, dist, etc.
├── src/
│   ├── index.ts         # CLI entry point
│   ├── commands/        # Command implementations
│   ├── types/           # TypeScript interfaces
│   ├── formatters/      # Output formatters
│   ├── auth/            # Authentication layer
│   ├── transport/       # HTTP client for DGrep API
│   ├── config/          # Config management
│   └── utils/           # Time parser, helpers
└── tests/
    ├── formatters/      # Output formatter tests
    ├── utils/           # Time parser tests
    ├── config/          # Config tests
    └── commands/        # Command integration tests (mocked)
```
