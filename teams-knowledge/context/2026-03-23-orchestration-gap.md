---
title: "Orchestration Gap — Parallel Agents Blocked by Git Checkout Fallback"
date: 2026-03-23
author: yoni
documentarian: bilbo
category: context
tags:
  - context
  - teams
  - orchestration
  - git
  - squad-infra
  - yoni
  - tracking
status: tracking
---

# Orchestration Gap — Parallel Agents Blocked by Git Checkout Fallback

## Situation

Squad orchestration currently falls back to `git checkout -b` instead of using git worktrees for agent isolation. This prevents parallel agents from working simultaneously, which is critical for true concurrent execution in squad workflows.

The fallback mechanism breaks parallelism because:
- `git checkout -b` modifies the working directory in-place
- Multiple agents cannot safely use the same working directory branch
- Coordination overhead and conflicts result
- Git worktrees are the proper solution for parallel, isolated branch contexts

## Why It Matters

**Impact:** Squad parallelism is blocked. Agents cannot work truly in parallel.

**Who's Affected:**
- Squad orchestration users (internal team workflows)
- Future AI Squads implementations (depends on proper parallelism)
- Multi-agent workflows requiring concurrent execution

**Current State:**
- Issue filed (2026-03-23-git-worktree-bug.md)
- Workaround: Manual git worktree setup by orchestration authors (not automated)
- No active mitigation

## Related Items

- Related decision: File issue on git worktree orchestration bug (2026-03-23-git-worktree-bug.md)
- Related decision: Endorse AI Squads (2026-03-23-ai-squads-endorsement.md)
