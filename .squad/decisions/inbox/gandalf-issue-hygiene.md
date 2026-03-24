# Decision: Issue Hygiene and Worktree Cleanup (#142)

- **Date:** 2026-03-28
- **Author:** Gandalf
- **Type:** Governance
- **Issue:** #142

## Decision

Added three new rules (24–26) to `routing.md` addressing:

1. **Rule 24 — Governance issue closure:** After committing governance changes to main (Rule 15), the coordinator closes the tracking issue with `gh issue close` and a comment citing the commit SHA and Rule 15. This is an authorized exception to Rule 11.

2. **Rule 25 — Post-merge issue verification:** After any merge to main (PR merge, git merge, direct commit), the coordinator verifies all related issues are closed. Catches recovery merges and any non-standard path that bypasses PR auto-close.

3. **Rule 26 — Worktree cleanup:** After a branch merges, remove the worktree (`git worktree remove`) and delete the local branch (`git branch -d`). Ralph scans for stale worktrees during work-check cycles.

## Rationale

- Rule 24: Governance work should be tracked (Rule 17) but has no PR path (Rule 15). Explicit closure step fills the gap.
- Rule 25: Recovery scenarios (8-branch merge session) proved that `git merge` bypasses auto-close. Safety net catches edge cases.
- Rule 26: 9 stale worktrees found on disk. Post-merge cleanup + periodic scan prevents accumulation.

## Boromir Review

Explicitly skipped per Jonathan's request. This is governance hygiene, not architectural design.

## Design Doc

`docs/designs/issue-hygiene.md`
