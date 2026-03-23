---
title: "Prioritize SEV2 ARM Watchlist Rate-Limiting Issue"
date: 2026-03-23
author: yoni
documentarian: bilbo
category: decision
tags:
  - decision
  - teams
  - azure
  - arm-watchlist
  - yoni
  - active
status: active
---

# Prioritize SEV2 ARM Watchlist Rate-Limiting Issue

## Decision

ARM watchlist request failures must be treated as SEV2 priority by the Flags Shiproom. The root cause is suspected to be missing rate limits on the ARM API.

## Rationale

- Watchlist failures are blocking dependent workflows
- Pattern suggests API rate limiting rather than transient failures
- SEV2 priority will trigger appropriate escalation and resource allocation
- Early action prevents downstream impact on customer-facing systems

## Implications

- Flags Shiproom should prioritize investigation and mitigation
- May require rate limit tuning on ARM API side
- Service tree ownership (see related action item) needs validation to confirm alerts are properly configured
