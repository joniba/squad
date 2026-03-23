---
title: "File Issue on Git Worktree Orchestration Bug"
date: 2026-03-23
author: yoni
documentarian: bilbo
category: decision
tags:
  - decision
  - teams
  - orchestration
  - git
  - squad-infra
  - yoni
  - active
status: active
---

# File Issue on Git Worktree Orchestration Bug

## Decision

An issue has been filed documenting the orchestration gap: squad orchestration falls back to `git checkout -b` instead of using git worktrees, which breaks parallel agent work.

## Rationale

- Parallel agents cannot work simultaneously with current orchestration approach
- Fallback to `git checkout -b` causes conflicts and coordination issues
- Git worktrees are designed for exactly this use case (parallel agent isolation)
- Issue documentation enables tracking and prioritization of the fix

## Implications

- Squad orchestration is currently blocked from true parallelism
- Workaround may exist (manual worktree setup) but is not automated
- Fix requires updating orchestration logic to prefer git worktrees
- Critical dependency for AI Squads adoption (see related decision)
- Related context: Orchestration gap documented in teams-knowledge/context

## Related Items

- Related decision: Endorse AI Squads (2026-03-23-ai-squads-endorsement.md)
- Related context: Orchestration gap (2026-03-23-orchestration-gap.md)
