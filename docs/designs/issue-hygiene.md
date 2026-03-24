# Design: Issue Hygiene and Worktree Cleanup Procedures

> **Issue:** #142
> **Author:** Gandalf (Lead)
> **Date:** 2026-03-28
> **Status:** Partially implemented, then refined. Original governance exception (Rule 15) was removed in favor of unified PR-based governance. Actual implementation: Rules 23–24 in routing.md (post-merge verification + worktree cleanup). See PR #147.

## Problem Statement

We have 17 open issues on the board, many representing completed work that was never closed. Three root causes identified:

### Problem 1: Rule 15 governance work leaves issues open

Rule 17 requires issue creation for all squad work. Rule 15 says governance changes go directly to main (no branch, no PR). Rule 11 says issues close only via PR merge auto-close ("Closes #N"). These three rules form an impossible triangle for governance work: the issue gets created (Rule 17), the work is committed directly to main (Rule 15), but no PR exists to auto-close the issue (Rule 11). The coordinator is supposed to close it manually, but there's no explicit step saying so — it gets forgotten.

### Problem 2: Direct merges don't close issues

During protocol recovery, 8 branches were merged to main via `git merge` instead of `gh pr merge`. Direct merges don't trigger GitHub's auto-close mechanism because there's no PR body containing "Closes #N." This is an edge case but it happened and will happen again during recovery scenarios, manual conflict resolution, or any non-standard merge path.

### Problem 3: Worktree accumulation

Every issue gets a worktree at `./worktrees/squad-{N}/`. After the branch merges to main, the worktree is never cleaned up. Currently 9 stale worktrees exist, consuming disk space and creating stale branch references.

## Solutions Chosen

> **2026-04-15 DECISION UPDATE:** After initial design approval, the governance exception approach (Rule 15) was reconsidered. The team decided that ALL work, including governance, should follow the standard PR lifecycle (branch → PR → review → merge) to maintain consistent oversight and audit trails. Therefore, Solution 1 below was NOT implemented. The governance work now follows Rule 11 like all other work. **Only Solutions 2 & 3 were implemented**, becoming Rules 23–24 in the current routing.md. The "Problem 1" no longer exists as a problem because governance work no longer has a special exception.

### Solution 1: Rule 24 — Governance issue closure (explicit step) [NOT IMPLEMENTED]

**Status:** This solution was rejected as part of PR #147 decision to remove governance exceptions. Governance work now follows the standard PR lifecycle.

**Original approach:** Add an explicit step to Rule 15's workflow: after committing governance changes, close the tracking issue with `gh issue close` and a comment citing the commit and Rule 15.

**Rejected because:** The team chose unified governance through PR-based review for all work types, eliminating the need for this exception.

### Solution 2: Rule 23 — Post-merge issue verification (catch-all) [IMPLEMENTED]

**Approach chosen:** After any merge to main, the coordinator verifies that all issues whose work just landed are properly closed. If an issue is still open after its branch merged, close it with `gh issue close` and a comment explaining why auto-close didn't fire.

**Alternative considered and rejected:** Require all merges to go through `gh pr merge`, even in recovery (create retroactive PRs). Rejected because:
- Recovery scenarios genuinely require `git merge` (branches already closed without PRs, merge conflicts needing manual resolution)
- Creating retroactive PRs for already-merged branches is ceremonial overhead with no review value
- The real problem is orphaned issues, not the merge mechanism

**Why this is the right fix:** It's a catch-all safety net. Regardless of how code reaches main, the coordinator checks that tracking issues reflect reality. This is idempotent (safe to run even if the issue is already closed) and handles future edge cases we haven't imagined yet.

### Solution 3: Rule 24 — Worktree cleanup (post-merge + periodic scan) [IMPLEMENTED]

**Approach chosen:** Two-pronged cleanup:
1. **Post-merge step:** After every branch merge, remove the worktree and delete the local branch.
2. **Periodic scan:** Ralph checks for stale worktrees during his work-check cycle and flags them.

**Alternative considered:** A standalone cleanup script. Rejected in favor of embedding it in the coordinator's existing post-merge flow and Ralph's existing scan cycle — this requires no new infrastructure and piggybacks on existing enforcement mechanisms.

**Safety:** The rule explicitly forbids `--force` removal. If a worktree has uncommitted changes, the coordinator warns and skips. This prevents data loss from premature cleanup.

## Implementation

- **Rules 23–24** (originally proposed as Rules 25–26) added to `.squad/routing.md`
- **Solution 1 (governance exception)** NOT implemented — the team decided to remove the governance exception entirely and require all work (including governance) to follow standard PR review
- **Issue closure table** in `.squad/templates/issue-lifecycle.md` updated to document the recovery merge exception only (governance exception removed)
- **This design doc** serves as the rationale record, with the 2026-04-15 decision note documenting why the approach was refined

## Impact (Actual)

Rule 23 (post-merge issue verification) and Rule 24 (worktree cleanup) close the recovery gap and accumulation gap respectively. The governance gap was closed by requiring governance work to follow the standard PR lifecycle (Rule 11) instead of creating a direct-commit exception. This simplifies the rule set and maintains consistent oversight for all work types.
