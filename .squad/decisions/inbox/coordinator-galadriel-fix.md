# Audit & Fix: Galadriel Hiring Process

**Author:** Squad (Coordinator)
**Date:** 2025-07-22
**Requested by:** Jonathan
**Type:** Audit + Fix

## Summary

Audited Galadriel's onboarding against the 8-step hiring process in `squad.agent.md`. Found two gaps — both in Step 7 (enforcement wiring). Fixed both.

## Step-by-Step Audit

### Step 1: Allocate name ✅
Galadriel — allocated from Lord of the Rings universe. Present in `casting/registry.json` with `created_at: 2026-03-23`.

### Step 2: Check plugin marketplaces ✅
Done at hire time. Cannot retroactively verify; no gap evident.

### Step 3: Generate charter.md + history.md ✅
- `charter.md` exists at `.squad/agents/galadriel/charter.md` — comprehensive reviewer identity, 3-stage pipeline, severity scale, verdict criteria, collaboration patterns.
- `history.md` exists at `.squad/agents/galadriel/history.md`.

### Step 4: Update casting registry ✅
Galadriel is in `.squad/casting/registry.json` with role "Reviewer", status "active".

### Step 5: Add to team.md roster ✅
Row exists: `| 👑 Galadriel | Reviewer | .squad/agents/galadriel/charter.md | ✅ Active |`

### Step 6: Add routing entries ❌ PARTIAL
- **Routing table:** ✅ Entry exists — `PR code review | 👑 Galadriel | docs/reviews/`
- **Issue routing:** ❌ MISSING — No `squad:galadriel` entry in the Issue Routing table.

**Fix applied:** Added `| squad:galadriel | PR code review, quality gates | 👑 Galadriel |` to the Issue Routing table.

### Step 7: Wire enforcement ❌ MISSING (critical gap)
This is the step the workflow-wiring-guide warns about: "Adding a reviewer to the roster ≠ enforcing reviews." No enforcement rules existed for Galadriel in `routing.md` Rules section. The routing table entry only handles explicit requests ("review PR #42") — it does NOT enforce automatic review of every PR.

**Fixes applied — added 4 numbered rules to `routing.md` → Rules section:**

| Rule # | Name | What it enforces |
|--------|------|-----------------|
| 9 | Issue lifecycle enforcement | All issue-linked work follows `issue-lifecycle.md` |
| 10 | Galadriel PR Gate | Every PR MUST be reviewed by Galadriel before merge |
| 11 | Issue closure restriction | File-producing issues close only via PR merge auto-close |
| 12 | Worktree for all file-producing work | All file changes require a worktree |

### Step 8: Announce ✅
Done at hire time. Cannot retroactively verify.

## Root Cause

The hiring process was followed through Step 6 but Step 7 (enforcement wiring) was skipped. This is the exact failure pattern documented in the workflow-wiring-guide: "Galadriel was on our roster from day one with 'Reviewer' as her role. She never reviewed a single PR because no RULE in routing.md told the coordinator to route PRs to her."

## Files Changed

| File | Change |
|------|--------|
| `.squad/routing.md` | Added `squad:galadriel` issue routing entry; added rules 9-12 (issue lifecycle, Galadriel PR gate, issue closure restriction, worktree requirement) |
| `.squad/decisions/inbox/coordinator-galadriel-fix.md` | This audit record |

## Verification

After these changes, a clean-session coordinator will:
- ✅ Know to route `squad:galadriel` labeled issues to Galadriel
- ✅ Route every PR to Galadriel before merge (Rule 10)
- ✅ Follow issue-lifecycle.md post-work steps (Rule 9)
- ✅ Never close file-producing issues via `gh issue close` (Rule 11)
- ✅ Require worktrees for all file-producing work (Rule 12)
