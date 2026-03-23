### 2026-03-23T02:40:55Z: User directive — Push after merge
**By:** Jonathan (via Copilot)
**What:** Always push main to origin after a PR is approved and merged. Never let local main drift ahead of origin/main — it causes phantom diffs in future PRs.
**Why:** 11 unpushed commits caused every PR to show 25+ phantom files in the GitHub diff, triggering false scope-violation rejections from Galadriel.
