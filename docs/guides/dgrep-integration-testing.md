# DGrep CLI — Integration Testing Guide

**Author:** Aragorn (Testing)
**Date:** 2026-07-22
**Status:** Active
**Related:** Issue #118, `docs/research/geneva-dgrep-research.md`, `.squad/decisions.md`

---

## 1. Overview

This guide covers end-to-end integration testing of the DGrep CLI against live Geneva infrastructure. These tests validate that the real `DgrepQueryExecutor` (backed by the DGrep SDK) can authenticate, execute queries, and return correctly formatted results.

Integration tests are **skipped by default**. They only run when explicitly enabled on a machine with corpnet access and valid Geneva credentials.

---

## 2. Prerequisites

### 2.1 Network Access

- **Corpnet or VPN** — DGrep endpoints are internal Microsoft services; they are not reachable from the public internet.
- Verify connectivity: `Test-NetConnection production.diagnostics.monitoring.core.windows.net -Port 443`

### 2.2 Geneva Access

- Your identity (AAD account or certificate SAN) must have **read access** to the target Geneva namespace.
- Geneva Account User Roles are managed at [aka.ms/jarvis](https://aka.ms/jarvis) → Account → User Roles.
- For the test namespace `AugustaPrdEus2`, ensure your team's role assignment includes read claims on the target events.

### 2.3 Authentication

Two auth modes are supported by the DGrep SDK:

| Mode | SDK Class | Requirements | CLI Context |
|------|-----------|--------------|-------------|
| **Interactive (dSTS)** | `DGrepUserAuthClient` | .NET Framework, GUI session, AAD account with MFA | Manual dev testing only — opens browser-based dSTS login dialog |
| **Certificate** | `DGrepClient` | X509 certificate (`.pfx`) with private key, cert SAN enrolled in Geneva Account User Roles | Automation / headless scenarios |

> **Known limitation:** `DGrepUserAuthClient` uses an interactive dSTS dialog that requires a desktop GUI session. It will **hang** in headless terminals (SSH, CI agents, Windows Terminal without desktop). For CI, use certificate-based auth.

### 2.4 Build Environment

- .NET Framework 4.7.2+ SDK (included with Visual Studio 2022 or Build Tools)
- `dotnet` CLI (MSBuild via `dotnet test` supports .NET Framework projects)
- DGrep SDK NuGet feed: `https://pkgs.dev.azure.com/msazure/_packaging/Official/nuget/v3/index.json`

---

## 3. Test Configuration

### 3.1 Environment Variables

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `DGREP_INTEGRATION_ENABLED` | Yes | `false` | Set to `true` to enable integration tests |
| `DGREP_TEST_ENDPOINT` | No | `https://production.diagnostics.monitoring.core.windows.net/` | MDS endpoint for test queries |
| `DGREP_TEST_NAMESPACE` | No | `AugustaPrdEus2` | Geneva namespace to query |
| `DGREP_TEST_EVENT` | No | `Log` | Event name to query |
| `DGREP_TEST_CERT_PATH` | No | *(none)* | Path to `.pfx` certificate for cert-based auth |
| `DGREP_TEST_CERT_PASSWORD` | No | *(none)* | Certificate password (if password-protected) |

### 3.2 Test Endpoint

- **Diagnostics PROD:** `https://production.diagnostics.monitoring.core.windows.net/`
- This is the standard MDS endpoint for internal Microsoft diagnostics. Use this for integration tests unless you have a specific reason to target a different environment.

### 3.3 Test Namespace & Events

| Setting | Value | Notes |
|---------|-------|-------|
| **Namespace** | `AugustaPrdEus2` | Augusta production namespace (East US 2) |
| **Events** | `Log`, `SentinelLogEntry` | Common log events available in the namespace |

---

## 4. Running Integration Tests

### 4.1 Quick Start

```powershell
# Enable integration tests (current session only)
$env:DGREP_INTEGRATION_ENABLED = "true"

# Run ONLY integration tests
cd tools/dgrep-cli
dotnet test --filter Category=Integration --nologo

# Run ALL tests (unit + integration)
dotnet test --nologo
```

### 4.2 Default Behavior (No Env Var)

When `DGREP_INTEGRATION_ENABLED` is not set or is not `true`, all integration tests are **skipped** with `Skip` reason. They do not count as failures — `dotnet test` still exits with code 0.

### 4.3 CI/CD

Integration tests should **not** run in standard CI pipelines. They require corpnet access and authenticated credentials. If you set up a dedicated integration test pipeline:

1. Run on a corpnet-connected build agent
2. Use certificate-based auth (set `DGREP_TEST_CERT_PATH`)
3. Set `DGREP_INTEGRATION_ENABLED=true` in pipeline variables
4. Run with `dotnet test --filter Category=Integration`

---

## 5. Manual Test Scenarios

The following scenarios validate the DGrep CLI end-to-end. Each corresponds to one or more automated integration tests.

### 5a. Auth Verification

**Goal:** Confirm that authentication succeeds against the live DGrep endpoint.

**Steps (interactive):**
1. Run `dgrep auth test --endpoint https://production.diagnostics.monitoring.core.windows.net/`
2. Complete the dSTS login dialog when it appears
3. Verify output shows a valid token with expiry time

**Steps (certificate):**
1. Run `dgrep auth test --auth-method certificate --cert <path-to-pfx>`
2. Verify output shows certificate-based auth success

**Expected:** Auth succeeds, token info displayed, no errors.

### 5b. Basic Query Returns Results

**Goal:** Confirm a simple query returns rows.

```powershell
dgrep search --endpoint https://production.diagnostics.monitoring.core.windows.net/ `
  --namespace AugustaPrdEus2 --event Log `
  --from -30m --query "* | take 10" --output table
```

**Expected:** Table output with column headers, ≤10 rows, row count displayed.

### 5c. Identity Scoping

**Goal:** Confirm identity column filtering restricts results.

```powershell
dgrep search --endpoint https://production.diagnostics.monitoring.core.windows.net/ `
  --namespace AugustaPrdEus2 --event Log `
  --from -1h --query "* | take 5" `
  --identity "Tenant=EUS2" --output table
```

**Expected:** Results filtered to specified tenant identity. Row count may be smaller than unfiltered query.

### 5d. Time Range Filtering

**Goal:** Confirm `--from` and `--to` parameters restrict the query window.

```powershell
# Query last 10 minutes only
dgrep search --endpoint https://production.diagnostics.monitoring.core.windows.net/ `
  --namespace AugustaPrdEus2 --event Log `
  --from -10m --to now --query "* | take 5" --output json
```

**Expected:** All returned rows have timestamps within the last 10 minutes.

### 5e. Output Formatters

**Goal:** Verify all output formats produce valid output with real data.

| Format | Flag | Validation |
|--------|------|------------|
| **Table** | `--output table` | Aligned columns, separator line, row count footer |
| **JSON** | `--output json` | Valid JSON array, parseable by `jq` or `ConvertFrom-Json` |
| **CSV** | `--output csv` | Valid CSV with header row, RFC 4180 escaping |

### 5f. Rate Limiting

**Goal:** Confirm the CLI respects the 5-concurrent-request limit.

**Manual test:** Fire 6+ parallel queries rapidly and observe that the 6th either queues or returns a quota error.

**Note:** The DGrep SDK manages `IDGrepQuery.CloseAsync()` for resource release. Integration tests must ensure queries are properly disposed to avoid leaking quota.

### 5g. Error Handling

| Scenario | Input | Expected |
|----------|-------|----------|
| Bad endpoint | `--endpoint https://nonexistent.example.com/` | `QueryConnectionException` with clear error |
| Bad namespace | `--namespace FakeNamespace123` | Error from DGrep backend about unknown namespace |
| Bad query | `--query "INVALID %%% SYNTAX"` | `QuerySyntaxException` or DGrep SDK error |
| Empty results | A query on a known-empty time range or filter | 0 rows returned, no crash, clean output |

---

## 6. Known Limitations

1. **Interactive auth requires GUI session** — `DGrepUserAuthClient` opens a dSTS browser dialog. This does not work in headless terminals, SSH sessions, or CI agents without a desktop.
2. **Certificate auth requires X509 with private key** — The `.pfx` must contain the private key. `.cer` and `.pem` (public-only) files will not work for client auth.
3. **Query range limited to 7 days** — DGrep enforces a maximum 7-day query window. Tests should use short time ranges (≤1 hour) for speed.
4. **1M row server limit** — Queries returning >1M rows are truncated server-side. Integration tests should use `take N` to keep results small.
5. **~5 minute ingestion latency** — Very recent logs (last 5 minutes) may not be queryable yet. Use `--from -30m` for reliable results.
6. **Corpnet required** — All DGrep/MDS endpoints are internal. Tests fail immediately if not on corpnet/VPN.
7. **Rate limit: 5 concurrent queries** — Orphaned queries (not closed) count against quota. The DGrep SDK's `IDGrepQuery.CloseAsync()` must be called; integration tests should ensure proper disposal.

---

## 7. Test File Reference

| File | Purpose |
|------|---------|
| `tests/DgrepCli.Tests/Integration/IntegrationTestBase.cs` | Base class — skip logic, test helpers, common `QueryOptions` |
| `tests/DgrepCli.Tests/Integration/IntegrationAuthTests.cs` | Auth flow validation against live endpoint |
| `tests/DgrepCli.Tests/Integration/IntegrationSearchTests.cs` | Basic query execution and result validation |
| `tests/DgrepCli.Tests/Integration/IntegrationFormatterTests.cs` | Output formatter validation with real query results |

All integration tests use `[Trait("Category", "Integration")]` and skip when `DGREP_INTEGRATION_ENABLED != "true"`.

**Run:** `dotnet test --filter Category=Integration`
