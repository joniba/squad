---
title: "Semantic Model: Build Index & Query Squad Knowledge"
date: 2024-12-19
author: Gimli
documentarian: Bilbo
category: guide
tags: [guide, architecture, tooling, bilbo, final]
status: final
---

# Semantic Model: Build Index & Query Squad Knowledge

Learn how to build and query the squad's semantic knowledge graph — a queryable index of agents, decisions, skills, docs, issues, PRs, and their relationships.

## What It Does

The semantic model is a JSON-based knowledge graph that indexes everything the squad creates and manages:

- **Agents** (Aragorn, Gandalf, Gimli, Elrond, Bilbo) and their roles
- **Decisions** from `.squad/decisions.md` with authors and dates
- **Skills** in `.squad/skills/` with ownership
- **Docs** from `docs/` with tags and status
- **GitHub Issues & PRs** with labels and state
- **Commits** from the last 30 with authors and changed files
- **Sessions** from orchestration logs
- **Relationships:** owns, authored, tagged_with, implements, modifies, and more

Use this to:
- Answer "who owns this skill?"
- Find all docs tagged with "research"
- Trace which PRs implement which issues
- Map agent responsibilities
- Build dashboards of squad state
- Understand decision lineage

## Prerequisites

- **PowerShell 7+** (Core or Desktop)
- **Git** installed and in PATH
- **GitHub CLI (`gh`)** installed and authenticated
- **Copilot CLI** installed (for session queries)
- **Repo root** accessible as the working directory

## Part 1: Building the Semantic Model Index

### First Build: Create the Model

From the repo root, run:

```powershell
.\scripts\semantic-index.ps1
```

This scans the entire repo and builds `.squad/semantic-model.json` with:
- All 8 node types (agent, decision, skill, doc, commit, issue, pr, session)
- All 8 relationship types (owns, authored, tagged_with, implements, etc.)
- Metadata (timestamps, counts)

Expected output:
```
Semantic model: 127 nodes, 342 edges -> .squad/semantic-model.json
```

### What Gets Indexed

| Category | Source | Example |
|----------|--------|---------|
| Agents | `.squad/agents/*/charter.md` | `{ id: "agent:gimli", role: "Tooling" }` |
| Decisions | `.squad/decisions.md` | `{ id: "decision:semantic-graph", author: "Elrond" }` |
| Skills | `.squad/skills/*/SKILL.md` | `{ id: "skill:teams-watchdog", path: ".squad/skills/teams-watchdog" }` |
| Docs | `docs/**/*.md` frontmatter | `{ id: "doc:docs/guides/index.md", tags: ["guide", "bilbo"], status: "final" }` |
| Commits | `git log -30` | `{ id: "commit:abc1234", author: "Gimli", message: "..." }` |
| Issues | `gh issue list --limit 50` | `{ id: "issue:42", title: "...", labels: ["squad:gimli", "blocked"] }` |
| PRs | `gh pr list --limit 50` | `{ id: "pr:53", title: "...", state: "open" }` |
| Sessions | `.squad/log/`, `.squad/orchestration-log/` | `{ id: "session:abc123", path: "..." }` |

### Rebuild After Changes

After creating new docs, decisions, issues, or PRs, rebuild to stay current:

```powershell
.\scripts\semantic-index.ps1
```

This is fast (~2 seconds for typical squad state) and replaces the previous index completely. No incremental updates needed.

**Best practice:** Rebuild after significant squad work — closes completed issues, merges PRs, adds decisions, creates docs.

## Part 2: Querying the Model

Use `scripts/semantic-query.ps1` to explore the model:

### Query Types

#### 1. List All Nodes of a Type

```powershell
# All agents
.\scripts\semantic-query.ps1 -Type agents

# All decisions
.\scripts\semantic-query.ps1 -Type decisions

# All skills
.\scripts\semantic-query.ps1 -Type skills
```

Output:
```
type              id
────              ──
agent             agent:aragorn
agent             agent:gandalf
agent             agent:gimli
agent             agent:elrond
agent             agent:bilbo
```

#### 2. List All Relationships of a Type

```powershell
# Who owns what?
.\scripts\semantic-query.ps1 -Type edges -Filter "owns"

# What is each doc tagged with?
.\scripts\semantic-query.ps1 -Type edges -Filter "tagged_with"
```

Output:
```
type    source                           target
────    ──────                           ──────
owns    agent:gimli                      skill:semantic-model
owns    agent:aragorn                    skill:icm-investigator
owns    agent:bilbo                      skill:knowledge-management
```

#### 3. Find Docs by Tag

```powershell
# All docs tagged as "research"
.\scripts\semantic-query.ps1 -Type docs-by-tag -Filter "research"

# All docs tagged as "guide"
.\scripts\semantic-query.ps1 -Type docs-by-tag -Filter "guide"
```

Output:
```
id                                           title                      status
──                                           ─────                      ──────
docs/research/teams-monitor-analysis.md     Teams Monitor Analysis     final
docs/research/squad-semantic-graph.md       Squad Semantic Graph RCA   draft
```

#### 4. Find What an Agent Owns

```powershell
# All skills/issues/docs owned by Gimli
.\scripts\semantic-query.ps1 -Type files-by-agent -Filter "gimli"
```

Output:
```
type     id
────     ──
skill    skill:semantic-model
skill    skill:squad-notifications
issue    issue:37
issue    issue:38
```

#### 5. Find Related Issues by Keyword

```powershell
# All docs mentioning "watchdog" or "monitor"
.\scripts\semantic-query.ps1 -Type related-issues -Filter "watchdog"
```

Output:
```
id                               title                              tags
──                               ─────                              ────
docs/guides/teams-watchdog.md   Teams Watchdog Daily Summary       guide,tooling,teams,bilbo
docs/research/teams-monitor.md  Teams Monitor Analysis             research,teams,elrond
```

## Part 3: Real-World Query Examples

### Example 1: "Who owns Semantic Model?"

```powershell
# Find the skill node
.\scripts\semantic-query.ps1 -Type skills | grep "semantic"

# Result: skill:semantic-model
# Now find who owns it:
.\scripts\semantic-query.ps1 -Type edges -Filter "owns" | Select-String "semantic-model"

# Result: agent:gimli owns skill:semantic-model
```

### Example 2: "What decisions did Elrond author?"

```powershell
# List all decisions (inspect authors)
.\scripts\semantic-query.ps1 -Type decisions | grep -i "elrond"

# Or manually inspect the JSON:
$model = Get-Content .\.squad\semantic-model.json | ConvertFrom-Json
$model.nodes | Where-Object { $_.type -eq "decision" -and $_.author -eq "Elrond" }
```

### Example 3: "Which PR implements which issue?"

```powershell
# Find the implements relationship
.\scripts\semantic-query.ps1 -Type edges -Filter "implements"

# Result:
# type       source    target
# ──────────────────────────────
# implements pr:53     issue:42
# implements pr:54     issue:51
```

### Example 4: "What docs describe semantic-model?"

```powershell
# Find all references to semantic-model
$model = Get-Content .\.squad\semantic-model.json | ConvertFrom-Json
$model.edges | Where-Object { $_.type -eq "references" -and $_.target -match "semantic" }
```

### Example 5: "Show the squad structure"

```powershell
# All agents and their roles
.\scripts\semantic-query.ps1 -Type agents

# All skills owned by Gimli
.\scripts\semantic-query.ps1 -Type files-by-agent -Filter "gimli"

# All issues assigned to Aragorn (via squad:aragorn label)
.\scripts\semantic-query.ps1 -Type edges -Filter "owns" | grep -i aragorn
```

## Part 4: Direct JSON Queries

For complex questions, query the JSON directly using PowerShell:

```powershell
$model = Get-Content .\.squad\semantic-model.json | ConvertFrom-Json

# Find all edges originating from Gimli
$gimliOwns = $model.edges | Where-Object { $_.source -eq "agent:gimli" }

# Find all nodes authored by Aragorn
$aragonAuthorships = $model.edges | Where-Object { $_.type -eq "authored" -and $_.source -eq "agent:aragorn" }

# Count docs per tag
$tagCounts = @{}
$model.edges | Where-Object { $_.type -eq "tagged_with" } | ForEach-Object {
    $tag = $_.target
    if (-not $tagCounts[$tag]) { $tagCounts[$tag] = 0 }
    $tagCounts[$tag]++
}
$tagCounts | Sort-Object Value -Descending
```

## Understanding the Data Model

### Node Types (8 total)

| Type | Key | Properties | Example |
|------|-----|------------|---------|
| **agent** | name | role, charter_path | `{ id: "agent:gimli", role: "Tooling", charter_path: ".squad/agents/gimli/charter.md" }` |
| **decision** | slug | title, author | `{ id: "decision:semantic-graph", title: "Build Semantic Graph", author: "Elrond" }` |
| **skill** | name | path | `{ id: "skill:teams-watchdog", path: ".squad/skills/teams-watchdog" }` |
| **doc** | path | title, tags[], status | `{ id: "doc:docs/guides/index.md", title: "Guides", tags: ["guide", "bilbo"], status: "final" }` |
| **commit** | short-sha | author, message | `{ id: "commit:abc1234", author: "Gimli", message: "docs: add semantic model guide" }` |
| **issue** | number | title, state, labels[] | `{ id: "issue:42", title: "...", state: "open", labels: ["squad:gimli"] }` |
| **pr** | number | title, state | `{ id: "pr:53", title: "...", state: "open" }` |
| **session** | basename | path | `{ id: "session:abc123", path: ".squad/orchestration-log/abc123.log" }` |

### Edge Types (8 total)

| Relationship | From → To | Meaning | Source |
|-------------|-----------|---------|--------|
| **owns** | agent → skill/issue | Agent is responsible | SKILL.md owner / issue squad: label |
| **authored** | agent → doc/decision | Agent wrote | doc frontmatter author / decisions.md |
| **tagged_with** | doc → tag | Doc is classified | doc frontmatter tags |
| **implements** | pr → issue | PR addresses issue | PR body "Resolves #N" |
| **modifies** | commit → file | Commit changed file | `git diff-tree` |
| **references** | doc → doc | Doc links to doc | markdown `[](path.md)` link |
| **decided_in** | decision → issue | Decision made in issue | decision title contains `#N` |
| **depends_on** | issue → issue | Issue blocks issue | issue body (future) |

## File Location & Refresh Ceremony

**Location:** `.squad/semantic-model.json`

**Format:** JSON with three top-level keys:
```json
{
  "nodes": [ { "type": "...", "id": "...", ...properties } ],
  "edges": [ { "type": "...", "source": "...", "target": "..." } ],
  "metadata": { "updated": "2024-12-19T14:30:00Z", "node_count": 127, "edge_count": 342 }
}
```

**Refresh:** Run `semantic-index.ps1` after:
- Creating new docs or decisions
- Closing issues or merging PRs
- Updating agent charters
- Adding skills

Typically ~2 seconds to rebuild. No performance penalty.

## Troubleshooting

### "No semantic model. Run semantic-index.ps1 first"

The model hasn't been built. Run:

```powershell
.\scripts\semantic-index.ps1
```

### "gh issue list" Fails with Auth Error

GitHub CLI isn't authenticated. Run:

```powershell
gh auth login
```

Then retry the index build.

### Model Is Stale or Missing Recent Changes

Rebuild the index:

```powershell
.\scripts\semantic-index.ps1
```

The index reads fresh data on each run (no caching).

### Query Returns Unexpected Results

Verify the query type and filter are correct:

```powershell
# List valid query types
.\scripts\semantic-query.ps1 -Type nodes
.\scripts\semantic-query.ps1 -Type edges
.\scripts\semantic-query.ps1 -Type decisions
.\scripts\semantic-query.ps1 -Type skills
.\scripts\semantic-query.ps1 -Type agents
```

Check the JSON directly for unusual formatting:

```powershell
$model = Get-Content .\.squad\semantic-model.json | ConvertFrom-Json
$model.edges | Where-Object { $_.source -eq "agent:gimli" } | head -3
```

## Advanced: Building Dashboards & Reports

The semantic model is designed for composability. Use it to:

1. **Build a squad capability matrix:**
   ```powershell
   $model = Get-Content .\.squad\semantic-model.json | ConvertFrom-Json
   $agents = $model.nodes | Where-Object { $_.type -eq "agent" }
   $agents | ForEach-Object {
       $skills = $model.edges | Where-Object { $_.source -eq $_.id }
       [PSCustomObject]@{ Agent = $_.id; Skills = $skills.Count }
   }
   ```

2. **Generate a decision audit trail:**
   ```powershell
   $decisions = $model.nodes | Where-Object { $_.type -eq "decision" }
   $decisions | Sort-Object author | Group-Object author | Select-Object Name, @{L="Count";E={$_.Count}}
   ```

3. **Export to visualization (D3.js, Cytoscape, etc.):**
   The JSON structure is directly compatible with most graph visualization libraries.

## Related

- `.squad/semantic-model.json` — The model file (read-only, auto-generated)
- `scripts/semantic-index.ps1` — Builds the model
- `scripts/semantic-query.ps1` — Queries the model
- `.squad/skills/semantic-model/SKILL.md` — Architecture details
- `docs/guides/index.md` — Other documentation

## Next Steps

- **For Gimli (tooling):** Use semantic queries to auto-generate squad status reports
- **For Jonathan (team lead):** Query the model to understand agent workloads
- **For all agents:** Learn which decisions affect your work — run queries to trace decision lineage

