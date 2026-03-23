# dgrep-cli

Cross-platform CLI for querying [Geneva DGrep](https://eng.ms/docs/products/geneva/logs/references/dgrepsdk/firstsdklogsearch) (Distributed Grep) — Microsoft's unindexed log search engine.

## Why?

Geneva DGrep is available on every Geneva Logs stream with zero setup, but the only ways to query it are:

1. **Jarvis web UI** — no scriptability, requires a browser
2. **DGrep .NET SDK** — requires writing C# code

There is **no CLI tool** anywhere in the ecosystem. `dgrep-cli` fills that gap.

## Usage

```bash
# Basic search
dgrep search --endpoint diag-prod --namespace MyNamespace --event MyEvent \
  --from -30m --query "source | where Level <= 2 | project PreciseTimeStamp, Message"

# With identity scoping
dgrep search --endpoint diag-prod --namespace MyNamespace --event MyEvent \
  --from -1h --identity "Tenant=WUS" --identity "Role=Frontend" \
  --query "source | where Message contains 'error'" --output json

# Stream results in real-time
dgrep stream --endpoint diag-prod --namespace MyNamespace --event MyEvent \
  --from -1h --query "source | where Level <= 2" --output jsonl

# Export to CSV
dgrep export --endpoint diag-prod --namespace MyNamespace --event MyEvent \
  --from -4h --query "source | where Status == 'Failed'"

# Manage configuration
dgrep config set endpoint.diag-prod https://production.diagnostics.monitoring.core.windows.net/
dgrep config list

# Saved queries
dgrep saved save my-error-query --endpoint diag-prod --namespace MyNS --event MyEvent \
  --query "source | where Level <= 2"
dgrep saved run my-error-query --from -1h
dgrep saved list
```

## Output Formats

| Format | Flag | Use Case |
|--------|------|----------|
| Table | `--output table` (default) | Human-readable terminal output |
| JSON | `--output json` | Structured data, piping to `jq` |
| CSV | `--output csv` | Spreadsheet import |
| TSV | `--output tsv` | Tab-separated for `awk`/`cut` |
| JSONL | `--output jsonl` | Streaming, line-by-line processing |

## Authentication

Supports multiple auth methods via `@azure/identity`:

1. **`az login`** — uses your existing Azure CLI session (default)
2. **Device code** — `--auth device-code` for headless environments
3. **Certificate** — `--cert /path/to/cert.pem` for automation
4. **Managed Identity** — automatic in Azure compute

## DGrep KQL Pitfalls

DGrep supports a **subset** of KQL. Common traps:

| ❌ Kusto | ✅ DGrep |
|----------|----------|
| `has "term"` | `contains "term"` |
| `mv-expand` | `mvexpand` |
| `column_ifexists` | `columnifexists` |
| `let x = ...;` | Not supported — inline your expressions |
| `iff(cond, a, b)` | `iif(cond, a, b)` |
| `summarize` in server query | ⚠️ Produces partial results — re-summarize in client query |

## Development

```bash
cd tools/dgrep-cli
npm install
npm run build
npm test
```

## Status

🚧 **Under active development** — Phase 1 (foundation) in progress.

See [PLAN.md](./PLAN.md) for the full project plan and phasing.

## License

MIT
