### 2026-03-23T02:18:56Z: User directive — Corrupted PRs: redo with full review cycle
**By:** Jonathan (via Copilot)
**What:** Do NOT extract files from corrupted branches (Option C was wrong). Close corrupted PRs, create fresh branches from main, re-run each agent's task cleanly, open new PRs, run full Galadriel review cycle. New PRs may reference closed PRs for context. This applies to any future corrupted PRs as well.
**Why:** Jonathan wants the full quality pipeline (branch → PR → review → merge) even if it's slower. Shortcuts compromise the review process.
