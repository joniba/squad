# Issue Lifecycle — PR-Gated Workflow

> Reference template for projects that require PR-gated issue completion.
> The coordinator reads this file when spawning agents for issue-linked work.
> Enforcement rules go in `routing.md` → Rules section — this file defines the WHAT, routing rules define the MUST.

## ISSUE CONTEXT Block (for spawn prompts)

When the coordinator spawns an agent for issue-linked work, add this block to the spawn prompt:

```
### Issue Context
ISSUE: #{number} — {title}
ISSUE_URL: {url}

AFTER completing your work:
1. Commit with message referencing the issue: "feat(scope): description (#N)"
2. Push your branch: `git push origin {branch_name}`
3. Create a PR: `gh pr create --title "{PR title}" --body "Closes #{N}" --base main --head {branch_name}`
4. Report: the PR number, files changed, and test results
DO NOT use `gh issue close`. The issue closes automatically when the PR merges.
```

## Coordinator Post-Work Steps (Issue-Linked)

After collecting agent results for issue-linked work:

1. **Verify push.** Check that the agent pushed: `git log origin/{branch} --oneline -1`
2. **Verify PR.** Check that a PR exists: `gh pr list --head {branch} --json number,title`
3. **If no PR:** Create one: `gh pr create --title "{title}" --body "Closes #{N}" --base main --head {branch}`
4. **Route to reviewer.** Spawn the project's designated reviewer (sync) with the PR diff for code review.
5. **On APPROVE:** Merge via `gh pr merge {pr_number} --squash --delete-branch`
6. **On REJECT:** Route reviewer's feedback to the original author agent. Re-spawn author to address feedback. After fixes are committed and pushed, repeat from step 4 (route to reviewer again). This cycle continues until the reviewer APPROVEs.
7. **Issue auto-closes** via "Closes #N" in PR body. Do NOT use `gh issue close` for file-producing work.

## Issue Closure Rules

| Issue Type | How It Closes |
|-----------|---------------|
| Code/scripts/tools | PR merge auto-close ("Closes #N") only |
| Documentation | PR merge auto-close only |
| Design docs | PR merge auto-close only |
| Research/investigation | PR merge auto-close only (report goes through PR) |
| Tracking/strategic (e.g., "Monitor X") | Coordinator may close with comment when condition is met |
| Superseded issues | Coordinator may close with comment linking to replacement issue |
| Recovery merge (direct `git merge`) | Coordinator closes with `gh issue close` + comment after verifying merge (Rule 23) |

**Never use `gh issue close` for any issue that produced files — EXCEPT** branches merged via direct `git merge` during recovery (Rule 23). This is the only authorized exception. The comment must cite the applicable rule.

## Worktree Requirement

ALL file-producing work requires a worktree — including documentation. The only exceptions:
- Read-only queries (explore agents)
- Scribe (writes to `.squad/` state on main — this is expected)
- Pure analysis that produces no files

## Activation

This template defines the lifecycle but does NOT enforce it. To activate enforcement, add numbered rules to `routing.md` → Rules section that reference this file. See the [Workflow Wiring Guide](workflow-wiring-guide.md) for how to wire enforcement rules.
