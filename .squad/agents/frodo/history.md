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
