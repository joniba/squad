# Review: `squad/scribe-scope-fix` — Scribe Scope Wiring Fix

**Reviewer:** Galadriel  
**Branch:** `squad/scribe-scope-fix`  
**Worktree:** `C:\dev\personal\pa-squad\worktrees\squad-scribe-scope`  
**Requested by:** Jonathan  
**Review Date:** 2026-03-25  
**Verdict:** ⛔ CHANGES_REQUESTED *(Cycle 1)*

**Re-Review Date:** 2026-03-25  
**Re-Review Commit:** `b5f74a2`  
**Verdict:** ✅ APPROVED *(Cycle 2)*

---

## What Was Reviewed

Two commits on `squad/scribe-scope-fix` branching from `main` at `9bef8ab`:

| Commit | Message |
|--------|---------|
| `a527080` | Add Rule 26: Scribe direct-commit scope — clarify governance boundary |
| `c791a45` | Add Rule 27: GitHub account auto-recovery to routing.md |

**Files changed (2):**
- `.squad/routing.md` — Rules 26 and 27 added
- `.squad/agents/scribe/charter.md` — Step 6 and "What I Own" updated

---

## Context: What Was Bypassed

Reviewed the two violating commits on `main` to understand what the rules must close:

**`1efb44d` (Elrond MCP catalog):** Scribe committed `docs/mcp-catalog.md` directly to main (added as a tracked file), along with legitimate state files.

**`9bef8ab` (Gandalf Kusto update):** Scribe committed `.squad/agents/aragorn/charter.md` and `docs/mcp-catalog.md` updates directly to main.

Both violations involved Scribe's `git add .squad/` command sweeping up out-of-scope files. Rule 26 targets this root cause.

---

## Findings

| # | Severity | File | Lines | Finding |
|---|----------|------|-------|---------|
| F1 | **HIGH** | `charter.md` | 28 | `decisions/inbox/` missing from Step 6 git add list — functional bug |
| F2 | **MEDIUM** | `routing.md` | 105–119 | `.squad/semantic-model.json` unaddressed — scope ambiguous |
| F3 | **LOW** | `routing.md` | 121 | Rule 27 trigger misses `401 Unauthorized` / `403 Forbidden` error class |
| F4 | **LOW** | `routing.md` | 123 | Rule 27 Step 2 doesn't specify URL parsing for SSH vs HTTPS remotes |

---

### F1 — HIGH: `decisions/inbox/` missing from Step 6 git add list (charter.md, line 28)

**Rule 26** (routing.md, line 109) explicitly includes `.squad/decisions/inbox/` in Scribe's allowed direct-commit scope:
> `.squad/decisions/inbox/` (cleanup after merge)

**Charter Step 6** (charter.md, line 28) lists the explicit git add paths for Scribe:
```
git add ONLY files within direct-commit scope (Rule 26):
  .squad/log/, .squad/orchestration-log/, .squad/decisions.md,
  .squad/agents/*/history.md, .squad/agents/*/history-archive.md
```

`.squad/decisions/inbox/` is **absent** from this list.

**Why this breaks things:** Scribe's Step 3 merges inbox files into `decisions.md` then **deletes** the inbox files from disk. Deletions must be staged to be committed. With the new explicit-path `git add`, `.squad/decisions.md` gets staged (correct), but the inbox file *deletions* are not staged. Result: `git status` shows stale deletions as unstaged; inbox files persist in git history as not-yet-deleted; downstream sessions see a dirty working tree or re-acquire already-merged decisions.

**Evidence from violating commit `1efb44d`:** That commit correctly staged inbox deletions (`D .squad/decisions/inbox/copilot-anti-assumption.md`, etc.). The new Step 6 breaks this.

**Required fix:** Add `.squad/decisions/inbox/` to the Step 6 explicit git add list:
```
git add .squad/log/ .squad/orchestration-log/ .squad/decisions.md \
        .squad/decisions/inbox/ .squad/agents/*/history.md \
        .squad/agents/*/history-archive.md
```

---

### F2 — MEDIUM: `.squad/semantic-model.json` not addressed (routing.md, lines 105–119)

Both violating commits included modifications to `.squad/semantic-model.json`. This file is:
- **Not listed** in Rule 26's allowed scope
- **Not listed** in Rule 26's prohibited examples
- **Not listed** in Scribe's "What I Own" section

The rule is silent on it. Two interpretations are possible:
- **Intentional exclusion:** Scribe should not commit it; it was incorrectly swept by `git add .squad/`. Any agent that updates it must do so via PR.
- **Gap:** It's session-level state and should be in the allowed list alongside history.md.

Given that `semantic-model.json` doesn't appear in the Scribe charter's "What I Own," the intentional-exclusion reading is stronger. But the rule should **explicitly call it out** either way, because:
1. It appeared in both violating commits — it's a concrete precedent
2. Without an explicit mention, a coordinator could reasonably re-add it to Scribe's git add and re-create the violation
3. The `commit-msg.txt` file also appears in the violating commit and is similarly unaddressed

**Required fix:** Rule 26 should add one sentence explicitly naming files that are out of scope even though they live in `.squad/`:
> `.squad/semantic-model.json`, `.squad/config.json`, `.squad/team.md`, and other non-log state files are out of scope and require a PR.

Or, if `semantic-model.json` is considered session-level state, add it to the allowed list with a note on who maintains it.

---

### F3 — LOW: Rule 27 trigger condition too narrow (routing.md, line 121)

**Current:** "fails with a permissions or repository-not-found error (e.g., 'Could not resolve to a Repository')"

HTTP 401 Unauthorized and 403 Forbidden are also `gh` account-mismatch error classes. The `gh` CLI can surface these as "HTTP 401" or "authentication required" messages without using the phrase "permissions error." The trigger should cover these:

**Suggested addition:** Add to the example: `(e.g., "Could not resolve to a Repository", "HTTP 401", "Must have push access")`

**Severity rationale:** Low because the spirit of the rule clearly covers these cases and an AI coordinator would likely apply it; but the letter is narrower than needed.

---

### F4 — LOW: Rule 27 Step 2 doesn't specify URL parsing (routing.md, line 123)

**Current Step 2:** "Extract the expected owner from the git remote: `git remote get-url origin`"

`git remote get-url origin` returns a full URL, not just the owner. Format varies:
- HTTPS: `https://github.com/jbenami_microsoft/ms-pa.git` → owner = `jbenami_microsoft`
- SSH: `git@github.com:jbenami_microsoft/ms-pa.git` → owner = `jbenami_microsoft`

The rule doesn't specify how to extract the owner from either format. An AI coordinator will likely get this right, but an explicit extraction hint reduces ambiguity:

**Suggested addition:** Append to Step 2: "The owner is the path component immediately after `github.com/` (HTTPS) or `github.com:` (SSH)."

---

## What's Working Well

✅ **Rule 26 scope list is correct and complete** for the violations that occurred. `docs/**` and `.squad/agents/*/charter.md` are explicitly prohibited — both violations are now closed.

✅ **Rule 26 enforcement mechanism is clear:** "The coordinator MUST NOT include out-of-scope files in Scribe's git add." Actionable.

✅ **Charter "What I Own" update** is accurate and correctly references Rule 26 with a concise explanation.

✅ **Rule 27 5-step flow** is well-structured and closes the actual failure mode (wrong `gh` account active). Step 3 (`gh auth switch`) and Step 4 (single retry) are appropriately conservative before escalation.

✅ **Rule 26 "everything else requires worktree→PR→Galadriel"** explicitly closes the PR review gate bypass.

---

## Required Changes (for APPROVE)

1. **[F1 — Must Fix]** Add `.squad/decisions/inbox/` to Charter Step 6's explicit `git add` path list. Without it, inbox cleanup deletions won't be staged and the decision-merge workflow is broken.

2. **[F2 — Must Address]** Add an explicit statement in Rule 26 about `.squad/semantic-model.json` (and similar non-log `.squad/` state files). Either include them in the allowed scope or explicitly name them as requiring a PR. The current silence on a file that appeared in both violating commits is a gap.

3. **[F3+F4 — Suggested but not blocking]** Broaden the Rule 27 trigger to explicitly name HTTP 401/403 errors; clarify Step 2 URL owner extraction.

---

## Verdict

⛔ **CHANGES_REQUESTED** *(Cycle 1)*

F1 is a functional bug — the charter's explicit git add list in Step 6 does not include `.squad/decisions/inbox/`, which means Scribe will fail to stage inbox file deletions after merging. F2 is a governance gap: `.squad/semantic-model.json` appeared in both violating commits but Rule 26 is silent on it, leaving the door open for the same mistake.

Fix F1 (charter Step 6) and F2 (Rule 26 coverage of `semantic-model.json`), then resubmit.

---

## Cycle 2 Re-Review — Commit `b5f74a2`

**Re-Review Date:** 2026-03-25

### F1 Resolution — ✅ RESOLVED

Charter Step 6 now reads:

> `git add` ONLY files within direct-commit scope (Rule 26): `.squad/log/`, `.squad/orchestration-log/`, `.squad/decisions.md`, `.squad/decisions/inbox/`, `.squad/agents/*/history.md`, `.squad/agents/*/history-archive.md`.

`.squad/decisions/inbox/` is present. Inbox deletion staging is no longer broken.

### F2 Resolution — ✅ RESOLVED

Rule 26 (routing.md) now explicitly lists `.squad/semantic-model.json` in the allowed scope:

> `.squad/semantic-model.json` (derived state file maintained by Scribe)

The team chose to include it in the allowed list rather than prohibit it — a valid governance decision given that it appeared legitimately in both violating commits as a file Scribe manages. The ambiguity is closed.

### F3 + F4 (LOW, non-blocking)

These were suggested improvements only and were not required for approval. No change — acceptable.

### New Issues Introduced

None. The two targeted changes are surgical and correct. No scope creep observed.

---

## Final Verdict

✅ **APPROVED** *(Cycle 2)*

Both mandatory findings are resolved. F1 (functional bug in Step 6 git add list) and F2 (Rule 26 silence on `semantic-model.json`) are cleanly addressed. The branch is clear to merge.
