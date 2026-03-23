# Semantic Model — Squad Knowledge Graph

> A JSON-based knowledge graph of everything the squad creates. Queryable by agents, browsable by humans.

## Data Model

All 8 node types and 8 edge types are implemented in `scripts/semantic-index.ps1`.

### Node Types (8/8 implemented)

| Type | Key | Source | Properties |
|------|-----|--------|------------|
| **agent** | name | `.squad/agents/*/charter.md` | role, charter_path |
| **decision** | slug | `.squad/decisions.md` | title, author |
| **skill** | name | `.squad/skills/*/SKILL.md` | path |
| **doc** | path | `docs/**/*.md` frontmatter | title, tags[], status |
| **commit** | short-sha | `git log -30` | author, message |
| **issue** | number | `gh issue list` | title, state, labels[] |
| **pr** | number | `gh pr list` | title, state |
| **session** | basename | `.squad/log/`, `.squad/orchestration-log/` | path |
| **file** | path | key `.squad/` config files | category |

### Edge Types (8/8 implemented)

| Relationship | From → To | Source | Example |
|-------------|-----------|--------|---------|
| **tagged_with** | doc → tag | doc frontmatter `tags:` | catalog tagged with "research" |
| **owns** | agent → skill/issue | SKILL.md `owner:` / issue `squad:X` label | gimli owns semantic-model |
| **authored** | agent → doc/decision | frontmatter `author:` / decisions.md | bilbo authored INDEX.md |
| **implements** | pr → issue | PR body `Resolves #N` | PR #53 implements #52 |
| **modifies** | commit → file | `git diff-tree` | commit abc modifies watchdog.ps1 |
| **depends_on** | issue → issue | issue body (future) | #36 depends on #37 |
| **decided_in** | decision → issue | decision title contains `#N` | PORT decided in #9 |
| **references** | doc → doc | markdown links `[](path.md)` | upgrade references catalog |

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
