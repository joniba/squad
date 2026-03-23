### 2026-03-23T12:59:38Z: Process decision — Bilbo auto-docs enforcement
**By:** Coordinator (self-correction)
**What:** The "Bilbo auto-documents completed features" directive was captured but never enforced. Root cause: the directive is a note, not a gate. Fix: after every PR merge or feature completion, the coordinator MUST spawn Bilbo (lightweight mode) to evaluate if user-facing docs are needed. This is the same pattern as spawning Galadriel for PR review — a mandatory post-completion step, not optional.

**Enforcement rule:** After closing any issue that produced new scripts, tools, or user-facing capabilities:
1. Coordinator asks: "Does this feature need a user guide?"
2. If yes → spawn Bilbo to write it before marking the work complete
3. If no (pure research, internal refactoring, charter updates) → skip

**Why this was missed:** The directive was in the decisions inbox but the coordinator's workflow didn't include a Bilbo checkpoint. Three features shipped without guides (scheduler, email watchdog, Teams knowledge library).
