# Triage: Squad Product Feedback Issues #44-#49

**Date:** 2026-03-22  
**Triaged by:** Gandalf  
**Source:** Jonathan onboarding feedback (filed on ms-pa repo since EMU tokens cannot write to bradygaster/squad directly)

## Summary
All 6 issues are observations about the squad CLI product surface, filed as feedback to track improvements. Routed based on ownership pattern: tool fixes → Gimli, documentation/patterns → Bilbo, product feedback → Gandalf.

## Routing Decisions

| Issue | Title | Owner | Rationale |
|-------|-------|-------|-----------|
| #44 | PRs stuck in draft — missing --ready flag | **Gimli** | CLI tool fix (add --ready flag or default behavior) |
| #45 | Project board not updating — missing OAuth scopes | **Gandalf** | External product feedback, tracked for squad project reference |
| #46 | Model defaults stuck on 4.5 — should use 4.6 when cost is equal | **Gimli** | CLI configuration change (model version selection) |
| #47 | squad-cli watch — can't take actions, confusing | **Gandalf** | External product UX/design feedback, tracked for squad project |
| #48 | Reviewer lockout design is wrong — authors should own fixes | **Bilbo** | Documentation of charter patterns and best practices |
| #49 | Default reviewer charter quality too low — needs better patterns | **Bilbo** | Documentation of reviewer charter templates and quality standards |

## Key Insight
These issues represent high-level product feedback from Jonathan's onboarding experience. They are NOT implementation tasks for this squad—rather, reference tracking for the upstream squad-skills project. Gandalf owns the product feedback meta-thread; Gimli and Bilbo own any local adaptations we choose to make.

## Next Steps
- Gimli reviews tool fixes (#44, #46) for applicability to ms-pa's ralph-watch implementation
- Bilbo documents reviewer charter best practices (#48, #49) for local Galadriel charter reference
- Gandalf maintains external feedback thread for upstream product communication
