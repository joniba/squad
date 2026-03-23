# Frodo — History

## Core Context

- **Project:** pa-squad (personal-assistant squad)
- **Owner:** Jonathan
- **My Domain:** Threat Intelligence backend — SecurityInsights RP, Sentinel-TiPipeline
- **Operating Repos:** Sentinel-TiPipeline (ADO), SecurityInsights RP — NOT pa-squad app code
- **Philosophy:** Conservative. Minimal diffs. Production RP code. Human review required.

## Key Reference Material

- Sagi's TiExpert PR #15064785: agent definition + 6 SKILL.md files + 8 PowerShell scripts
- Galadriel's review: `docs/reviews/pr-review-15064785.md` and `pr-review-15064785-v2.md`
- TI Pipeline Integration Guide: `docs/guides/ti-pipeline-integration-guide.md`
- ICM 767184571 investigation: `docs/investigations/icm-767184571-investigation.md`
  - ARM Watchlist recurrence, subscription blocking walkthrough
  - RP-side SubscriptionBlockFilter pattern documented
  - ARM throttling via `Update-AzProviderHubResourceTypeRegistration`
- ICM 764634026 investigation: `docs/investigations/icm-764634026/`
  - MSPKI cert migration, mTLS in TAXIIRequestSender.cs

## Learnings

### 2026-03-23: Charter created
- Hired to handle TI domain backend work that Gimli isn't suited for
- First task: implement SubscriptionBlockFilter in SecurityInsights RP (from ICM 767184571)
- Need to confirm SecurityInsights RP repo path with Jonathan before starting work
- Conservative approach: feature flags, config-driven, tested, human-reviewed
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
