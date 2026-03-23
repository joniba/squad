# Project Context

- **Owner:** Jonathan
- **Project:** pa-squad — a personal-assistant squad for everyday tasks, research, documentation, tool building, and livesite support
- **Stack:** General-purpose (not a single-stack project)
- **Created:** 2026-03-23

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-03-23 — Charter created
- Hired to handle TI domain backend work in SecurityInsights RP and Sentinel-TiPipeline — work that requires domain expertise Gimli doesn't have.
- First task: implement SubscriptionBlockFilter in SecurityInsights RP (from ICM 767184571 investigation).
- Need to confirm SecurityInsights RP repo local path with Jonathan before starting work.
- Key reference docs: `docs/guides/ti-pipeline-integration-guide.md`, `docs/reviews/pr-review-15064785-v2.md`, `docs/investigations/icm-767184571-investigation.md`.
- Conservative philosophy: minimal diffs, feature flags, config-driven, tested, human-reviewed.
