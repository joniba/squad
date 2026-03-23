# Semantic Model — Squad Knowledge Graph

> A JSON-based knowledge graph of everything the squad creates. Queryable by agents, browsable by humans.

## Data Model

### Node Types

| Type | Key | Properties |
|------|-----|------------|
| **agent** | name | role, charter_path |
| **doc** | path | title, category, tags[], status, date |
| **issue** | number | title, owner, status, labels[] |
| **pr** | number | title, author, status |
| **decision** | slug | title, author, date, status |
| **skill** | name | path, owner |
| **session** | id | agent, date, branch |
| **file** | path | last_modified_by, last_commit |

### Edge Types

| Relationship | From → To | Example |
|-------------|-----------|---------|
| owns | agent → issue | gimli owns #36 |
| authored | agent → doc | bilbo authored INDEX.md |
| implements | pr → issue | PR #53 implements #52 |
| modifies | session → file | session abc modifies watchdog.ps1 |
| depends_on | issue → issue | #36 depends on #37 |
| tagged_with | doc → tag | catalog tagged with "research" |
| decided_in | decision → issue | PORT decided in #9 |
| references | doc → doc | upgrade references catalog |

## Storage

- **File:** `.squad/semantic-model.json`
- **Format:** `{ "nodes": [...], "edges": [...], "metadata": { "updated": "ISO-8601" } }`
- Each node: `{ "type": "agent|doc|issue|...", "id": "unique-key", ...properties }`
- Each edge: `{ "type": "owns|authored|...", "source": "node-id", "target": "node-id" }`

## Scripts

| Script | Purpose |
|--------|---------|
| `scripts/semantic-index.ps1` | Build/rebuild the full graph from squad state, docs, and git |
| `scripts/semantic-query.ps1` | Query the graph by type, relationship, or filter |

## Querying

```powershell
# All issues owned by an agent
.\scripts\semantic-query.ps1 -Type files-by-agent -Filter gimli

# All decisions
.\scripts\semantic-query.ps1 -Type decisions

# Issues related to a topic
.\scripts\semantic-query.ps1 -Type related-issues -Filter "watchdog"

# All docs with a specific tag
.\scripts\semantic-query.ps1 -Type docs-by-tag -Filter "research"
```

## Update Ceremony

The semantic model is rebuilt by running `semantic-index.ps1` after any work session that produces files, closes issues, or merges PRs. This is triggered as part of the post-task pipeline (see #39).

## Design Rationale

JSON graph file was chosen over a full graph DB per Elrond's evaluation (#37). Reasons:
- Zero dependencies — no DB process, no installs beyond PowerShell
- Git-friendly — the model file is diffable and committable
- Fast — sub-second rebuilds for squad-scale data (~100s of nodes)
- Composable — any script/agent can read the JSON directly
- Visualization — JSON exports trivially to Cytoscape.js, D3, or Mermaid for human browsing
