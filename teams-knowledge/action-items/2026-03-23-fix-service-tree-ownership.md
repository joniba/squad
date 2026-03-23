---
title: "Fix Service Tree Ownership Certificates"
date: 2026-03-23
author: yoni
documentarian: bilbo
category: action-item
tags:
  - action-item
  - teams
  - azure
  - arm-watchlist
  - yoni
  - active
status: active
---

# Fix Service Tree Ownership Certificates

## Action

Follow up with Ashish Syal and Tamar to fix service tree ownership certificates and missing configurations that are currently under the wrong service tree node.

## Context

- Certificates are misconfigured under the wrong service tree
- Related red flags on missing configs prevent proper monitoring
- This affects ARM watchlist request handling and alerting
- Service tree misalignment may contribute to SEV2 ARM watchlist issue (see related decision)

## Owner & Timeline

- Primary contact: Ashish Syal (escalation/triage)
- Secondary contact: Tamar (missing configs investigation)
- Target: As soon as possible (supports ARM watchlist SEV2 mitigation)

## Related Items

- Related decision: Prioritize SEV2 ARM watchlist issue (2026-03-23-arm-watchlist-priority.md)
