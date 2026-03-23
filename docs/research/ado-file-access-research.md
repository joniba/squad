---
title: "ADO PR File Content Access — Deep Research"
author: Elrond (Researcher)
date: 2026-03-23
requested_by: Jonathan (CRITICAL)
status: complete
tags: [ado, pr-review, file-access, mcp, galadriel]
---

# ADO PR File Content Access — Deep Research

## Problem Statement

Galadriel (Reviewer) attempted to review ADO PR 15064785 (Sentinel-TiPipeline) but could NOT read file contents. She could see **what changed** via `pull_request_read > get_diff` but couldn't read the **full files** to understand context. PR reviews without file access are incomplete — you can't evaluate a change without understanding the surrounding code.

## Executive Summary

**The gap is real but solvable.** There are **4 working approaches** to read ADO file contents, ranked by reliability:

| Rank | Method | Reliability | Speed | Works on PR branches? | Context tokens |
|------|--------|-------------|-------|----------------------|----------------|
| 🥇 | `az devops invoke` (Items API) | ★★★★★ | Fast | ✅ Yes (with version param) | Moderate |
| 🥈 | Local git clone + `git show` | ★★★★★ | Fastest | ✅ Yes (after fetch) | Low (filesystem) |
| 🥉 | `ado-search_code` MCP tool | ★★★☆☆ | Slow | ⚠️ Only indexed branches | High (returns extras) |
| 4 | `ado-repo_list_directory` | ❌ | N/A | N/A | N/A — no content |

**Recommended approach for Galadriel:** Use `az devops invoke` as the primary method. Fall back to local git clone when available.

---

## Detailed Findings

### 1. ADO MCP Tools Inventory

#### Tools Tested

**`ado-repo_list_directory`** — ❌ Does NOT return file content.
- Returns: path, gitObjectType, commitId, contentMetadata
- Useful for: discovering file structure (what files exist)
- Tested: `ado-repo_list_directory(path="/", project="One", repositoryId="Sentinel-TiPipeline")` → 21 items, no content
- Tested: `ado-repo_list_directory(path="/README.md", ...)` → metadata only, no content field

**`ado-search_code`** — ✅ Returns FULL file contents, but with caveats.
- When a file matches a search query, the response includes a `gitItem.content` field with the **complete file text**
- Tested: `ado-search_code(searchText="README", project=["One"], repository=["Sentinel-TiPipeline"])` → returned 11 results with full README.md contents embedded
- **Caveats:**
  - Only searches indexed branches (typically `master`/`main`). Branch filter `features/sagimarus/tiexpertagent` returned 0 results
  - Search is keyword-based — you need to know text IN the file, not just the filename
  - Returns extra results you don't want — noisy
  - Rate limited; not designed for "read this specific file"
- **Verdict:** Usable as a last resort. Not reliable for PR branch files.

**`ado-repo_get_pull_request_by_id`** — ✅ Returns PR metadata including source/target branches.
- Critical for: knowing which branch to read files from
- Tested: PR 15064785 → `sourceRefName: refs/heads/features/sagimarus/tiexpertagent`, `targetRefName: refs/heads/master`

**`ado-repo_list_pull_request_threads`** — Review comments only, no file content.

**`ado-repo_search_commits`** — Commit metadata only, no file content.

**No `ado-repo_get_file_contents` tool exists.** This is the confirmed gap.

#### Complete `ado-*` Tool Categories

- `ado-core_*` — Projects, teams, identity (no file access)
- `ado-work_*` — Iterations, capacity, team settings (no file access)
- `ado-pipelines_*` — Builds, runs, logs, artifacts (pipeline artifacts only)
- `ado-repo_*` — Repos, branches, PRs, commits, directories (**no file content**)
- `ado-wit_*` — Work items, queries, backlogs (no file access)
- `ado-wiki_*` — Wiki pages (wiki content only, not repo files)
- `ado-testplan_*` — Test plans, suites, cases (no file access)
- `ado-search_*` — Code search, wiki search, work item search (**code search returns content**)
- `ado-advsec_*` — Advanced security alerts (no file access)

### 2. `az devops invoke` — THE PRIMARY SOLUTION ✅

The Azure DevOps REST API "Items - Get" endpoint returns file contents when `includeContent=true`.

**Verified working command:**

```powershell
# Read a file from the default branch
az devops invoke `
  --area git `
  --resource items `
  --route-parameters project=One repositoryId=Sentinel-TiPipeline `
  --query-parameters "path=/global.json" "includeContent=true" `
  --org https://dev.azure.com/msazure

# Read a file from a specific PR branch
az devops invoke `
  --area git `
  --resource items `
  --route-parameters project=One repositoryId=Sentinel-TiPipeline `
  --query-parameters `
    "path=/.github/agents/tiexpert.agent.md" `
    "includeContent=true" `
    "versionDescriptor.version=features/sagimarus/tiexpertagent" `
    "versionDescriptor.versionType=branch" `
  --org https://dev.azure.com/msazure
```

**Response format:**
```json
{
  "commitId": "0bd6ead1eef5e3ef288c5989f31be0bd95265e2b",
  "content": "{\n  \"sdk\": {\n    \"version\": \"8.0.415\"\n  }\n}",
  "path": "/global.json",
  "gitObjectType": "blob"
}
```

**Key parameters:**
- `path` — file path in the repo (e.g., `/src/Program.cs`)
- `includeContent=true` — **REQUIRED** to get file text in the JSON response
- `versionDescriptor.version` — branch name (e.g., `features/sagimarus/tiexpertagent`)
- `versionDescriptor.versionType` — `branch`, `commit`, or `tag`

**Authentication:** Uses existing `az login` credentials. Already authenticated and tested.

**Limitations:**
- Large binary files may not include content (returns download URL instead)
- Very large text files may be truncated
- Requires shell execution — Galadriel must call PowerShell to run `az devops invoke`

### 3. Local Git Clone — THE FASTEST FALLBACK ✅

**Confirmed:** Sentinel-TiPipeline is cloned at `C:\dev\ti\Sentinel-TiPipeline`.

**Verified working commands:**

```powershell
# Fetch the PR branch
cd C:\dev\ti\Sentinel-TiPipeline
git fetch origin features/sagimarus/tiexpertagent
# Output: * branch features/sagimarus/tiexpertagent -> FETCH_HEAD

# Read a file from the PR branch without checking it out
git show FETCH_HEAD:.github/agents/tiexpert.agent.md

# Read a file from master (target branch)
git show master:src/SomeFile.cs

# Compare specific file between branches
git diff master...FETCH_HEAD -- path/to/file.cs
```

**Advantages:**
- Fastest — no network call per file (just initial fetch)
- Works offline after fetch
- Can read ANY file at ANY version
- Minimal context tokens (filesystem read, not JSON wrapper)

**Limitations:**
- Requires the repo to be cloned locally (not all ADO repos are)
- Must know the local path (varies: `C:\dev\ti\`, `C:\dev\`, etc.)
- Requires `git fetch` first (network call, but one-time per PR)

**Local repo locations discovered:**
```
C:\dev\ti\Sentinel-TiPipeline          ← confirmed
C:\dev\ti\Sentinel-TiCommon            ← confirmed
C:\dev\ti\Sentinel-TiIngestion         ← confirmed
C:\dev\ti\Sentinel-TiPublishers        ← confirmed
C:\dev\ti\Sentinel-TiAutomation        ← confirmed
C:\dev\ti\Sentinel-TiAITools           ← confirmed
C:\dev\ti\Sentinel-TiActionPipeline    ← confirmed
C:\dev\ti\Sentinel-TiSharedInfra-Resources ← confirmed
(28 total repos under C:\dev\ti\)
```

### 4. `ado-search_code` Workaround — PARTIAL ⚠️

The ADO code search MCP tool returns full file contents in the `gitItem.content` field when files match a search.

**How to use it for file reading:**
```
ado-search_code(
  searchText="unique text from the file",
  project=["One"],
  repository=["Sentinel-TiPipeline"]
)
```

**Tested results:**
- `searchText="README"` → 11 results, with full file contents for each README.md
- Content includes the complete file text in `result[].gitItem.content`

**Critical limitations:**
- **Branch indexing is unreliable.** Searching with `branch=["features/sagimarus/tiexpertagent"]` returned 0 results even though the branch exists. ADO code search only indexes certain branches (typically default branch only).
- **You need to know text in the file.** You can't search by path alone — path filters narrow results but require matching text too.
- **Noisy results.** Returns multiple files, each with full content. Wastes context tokens.
- **Rate limited.** Not designed for sequential file reads.

**Verdict:** Useful for reading files on the default branch when you know some text in them. Not reliable for PR branch files.

### 5. REST API Documentation

**Endpoint:** `GET https://dev.azure.com/{org}/{project}/_apis/git/repositories/{repo}/items`

**Key parameters** (from Microsoft Learn docs):
- `path` (required) — file path
- `includeContent` — set to `true` to get file content in JSON
- `download` — set to `true` to get raw file download
- `versionDescriptor.version` — branch/tag/commit name
- `versionDescriptor.versionType` — `branch` | `commit` | `tag`
- `api-version` — `7.1` (latest stable)

**Source:** [Items - Get REST API (Azure DevOps Git)](https://learn.microsoft.com/en-us/rest/api/azure/devops/git/items/get?view=azure-devops-rest-7.1)

---

## Recommended Solution for Galadriel

### Primary Approach: `az devops invoke` via PowerShell

Galadriel should use PowerShell to call the ADO Items API:

```powershell
# Step 1: Get PR details (already available via MCP)
# sourceRefName = "refs/heads/features/sagimarus/tiexpertagent"
# targetRefName = "refs/heads/master"

# Step 2: Get list of changed files (already available via MCP)
# ado-repo pull_request_read > get_files

# Step 3: For each file, read content from BOTH branches
# Source (PR) version:
az devops invoke --area git --resource items `
  --route-parameters project=One repositoryId=Sentinel-TiPipeline `
  --query-parameters "path=/path/to/file.cs" "includeContent=true" `
    "versionDescriptor.version=features/sagimarus/tiexpertagent" `
    "versionDescriptor.versionType=branch" `
  --org https://dev.azure.com/msazure

# Target (master) version:
az devops invoke --area git --resource items `
  --route-parameters project=One repositoryId=Sentinel-TiPipeline `
  --query-parameters "path=/path/to/file.cs" "includeContent=true" `
    "versionDescriptor.version=master" `
    "versionDescriptor.versionType=branch" `
  --org https://dev.azure.com/msazure
```

### Fallback Approach: Local Git Clone

When the repo is cloned locally (most TI repos are at `C:\dev\ti\`):

```powershell
cd C:\dev\ti\Sentinel-TiPipeline
git fetch origin features/sagimarus/tiexpertagent
git show FETCH_HEAD:path/to/file.cs         # PR version
git show master:path/to/file.cs             # target version
```

### Galadriel Charter Update Needed

Galadriel's charter should include instructions to:
1. Use `az devops invoke` to read file contents (command template above)
2. Check `C:\dev\ti\{repo-name}` for local clones first (faster)
3. Extract branch names from `sourceRefName`/`targetRefName` (strip `refs/heads/` prefix)
4. For each changed file in the PR, read both source and target versions to understand context

---

## Appendix: Test Evidence

### Test 1: `ado-repo_list_directory` — No Content
```
Input:  ado-repo_list_directory(path="/README.md", project="One", repositoryId="Sentinel-TiPipeline")
Output: {path: "/README.md", gitObjectType: 3, commitId: "de29890c..."} — NO content field
```

### Test 2: `az devops invoke` with `includeContent=true` — ✅ Content Returned
```
Input:  az devops invoke --area git --resource items ... path=/global.json includeContent=true
Output: {"content": "{\n  \"sdk\": {\n    \"version\": \"8.0.415\"...}", "path": "/global.json"}
```

### Test 3: `az devops invoke` with branch version — ✅ PR Branch Content
```
Input:  ... path=/global.json includeContent=true versionDescriptor.version=features/sagimarus/tiexpertagent
Output: {"content": "...", "commitId": "0bd6ead1..."} — commit matches PR source commit
```

### Test 4: `ado-search_code` — ✅ Content in Results (default branch only)
```
Input:  ado-search_code(searchText="README", project=["One"], repository=["Sentinel-TiPipeline"])
Output: 11 results, each with gitItem.content containing full file text
Branch filter with PR branch: 0 results (not indexed)
```

### Test 5: Local Git Clone — ✅ Fetch + Show Works
```
Input:  git fetch origin features/sagimarus/tiexpertagent && git show FETCH_HEAD:.github/agents/tiexpert.agent.md
Output: Full file content (TI Expert agent definition, 20+ lines shown)
```

### Test 6: `az repos show` — ✅ Authenticated and Working
```
Input:  az repos show --project One --repository Sentinel-TiPipeline
Output: Repo metadata (id: 4d1d9ce9-5662-4e3a-bf71-7dd8ad674b33, defaultBranch: refs/heads/master)
```
