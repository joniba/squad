### 2026-03-23T02:12:30Z: User directive — Design review gate for new tooling
**By:** Jonathan (via Copilot)
**What:** New tools (or tools without detailed designs) require a design review before implementation. Workflow:
1. Gimli writes design in the issue or a linked doc
2. Gimli adds label: pending-design-review
3. Ralph routes to Gandalf for review
4. Gandalf reviews: if confident → removes pending-design-review, adds design-approved. If unsure → adds needs-jonathan-review and notifies Jonathan.
5. Implementation only starts AFTER design-approved label is present.
This does NOT apply to tools that already came with detailed designs or to trivial changes.
**Why:** Jonathan wants design oversight on new tooling to prevent low-quality implementations. Gandalf is the first gate; Jonathan is the escalation.
