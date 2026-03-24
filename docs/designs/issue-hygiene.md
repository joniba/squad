# Design: Issue Hygiene and Worktree Cleanup Procedures

> **Issue:** #142
> **Author:** Gandalf (Lead)
> **Date:** 2026-03-28
> **Status:** Implemented (Rules 24–26 in routing.md)

## Problem Statement

We have 17 open issues on the board, many representing completed work that was never closed. Three root causes identified:

### Problem 1: Rule 15 governance work leaves issues open

Rule 17 requires issue creation for all squad work. Rule 15 says governance changes go directly to main (no branch, no PR). Rule 11 says issues close only via PR merge auto-close ("Closes #N"). These three rules form an impossible triangle for governance work: the issue gets created (Rule 17), the work is committed directly to main (Rule 15), but no PR exists to auto-close the issue (Rule 11). The coordinator is supposed to close it manually, but there's no explicit step saying so — it gets forgotten.

### Problem 2: Direct merges don't close issues

During protocol recovery, 8 branches were merged to main via `git merge` instead of `gh pr merge`. Direct merges don't trigger GitHub's auto-close mechanism because there's no PR body containing "Closes #N." This is an edge case but it happened and will happen again during recovery scenarios, manual conflict resolution, or any non-standard merge path.

### Problem 3: Worktree accumulation

Every issue gets a worktree at `./worktrees/squad-{N}/`. After the branch merges to main, the worktree is never cleaned up. Currently 9 stale worktrees exist, consuming disk space and creating stale branch references.

## Solutions Chosen

### Solution 1: Rule 24 — Governance issue closure (explicit step)

**Approach chosen:** Add an explicit step to Rule 15's workflow: after committing governance changes, close the tracking issue with `gh issue close` and a comment citing the commit and Rule 15.

**Alternative considered and rejected:** Don't create issues for governance work at all (exempt from Rule 17). Rejected because governance work should be visible on the board — it's real work that takes time and affects the team. Exempting it from tracking means it becomes invisible, which contradicts the purpose of Rule 17 (all squad work appears on the GitHub board).

**Why this is the right fix:** The issue exists for tracking visibility. The closure mechanism is what's broken. Adding an explicit `gh issue close` step with a required comment (citing commit + Rule 15) preserves visibility while providing the missing closure path. The comment creates an audit trail equivalent to what a PR merge would provide.

### Solution 2: Rule 25 — Post-merge issue verification (catch-all)

**Approach chosen:** After any merge to main, the coordinator verifies that all issues whose work just landed are properly closed. If an issue is still open after its branch merged, close it with `gh issue close` and a comment explaining why auto-close didn't fire.

**Alternative considered and rejected:** Require all merges to go through `gh pr merge`, even in recovery (create retroactive PRs). Rejected because:
- Recovery scenarios genuinely require `git merge` (branches already closed without PRs, merge conflicts needing manual resolution)
- Creating retroactive PRs for already-merged branches is ceremonial overhead with no review value
- The real problem is orphaned issues, not the merge mechanism

**Why this is the right fix:** It's a catch-all safety net. Regardless of how code reaches main, the coordinator checks that tracking issues reflect reality. This is idempotent (safe to run even if the issue is already closed) and handles future edge cases we haven't imagined yet.

### Solution 3: Rule 26 — Worktree cleanup (post-merge + periodic scan)

**Approach chosen:** Two-pronged cleanup:
1. **Post-merge step:** After every branch merge, remove the worktree and delete the local branch.
2. **Periodic scan:** Ralph checks for stale worktrees during his work-check cycle and flags them.

**Alternative considered:** A standalone cleanup script. Rejected in favor of embedding it in the coordinator's existing post-merge flow and Ralph's existing scan cycle — this requires no new infrastructure and piggybacks on existing enforcement mechanisms.

**Safety:** The rule explicitly forbids `--force` removal. If a worktree has uncommitted changes, the coordinator warns and skips. This prevents data loss from premature cleanup.

## Implementation

- **Rules 24–26** added to `.squad/routing.md`
- **Issue closure table** updated in `.squad/templates/issue-lifecycle.md` to document the two new exceptions (governance and recovery merge)
- **This design doc** serves as the rationale record

## Impact

These three rules close the governance gap, the recovery gap, and the accumulation gap. They're additive — no existing rules are weakened. The two exceptions to Rule 11 are narrow, documented, and require comments citing the applicable rule.
