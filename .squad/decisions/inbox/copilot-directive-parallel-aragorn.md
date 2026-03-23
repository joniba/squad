### 2026-03-23T02:22:11Z: User directive — Parallel Aragorn spawns for different ICMs
**By:** Jonathan (via Copilot)
**What:** Multiple Aragorn agents may be spawned in parallel without regard to other running agents, as long as each Aragorn is working on a different ICM incident. No contention expected — each writes to a unique file (docs/investigations/icm-{id}.md). To avoid git index conflicts, Aragorn writes the file but does NOT commit — Scribe batch-commits after all agents finish.
**Why:** ICM investigations are independent and time-sensitive. Parallelism is safe because outputs are unique files with no shared state.
