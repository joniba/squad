# Boromir — History

## Project Context
- **Project:** pa-squad — personal-assistant squad
- **Owner:** Jonathan
- **Role:** Adversarial Design Reviewer — I challenge every design Gandalf produces
- **Joined:** 2026-03-24
- **Universe:** Lord of the Rings

## Core Context

Nobody asked for my opinion but I'm giving it anyway. I review every design that comes through this team. First drafts get rejected — not because I enjoy it (I do), but because the first idea is never the best idea. My job is to force better thinking.

## Learnings

### 2025-07-25: Enforcement V2 Review — REJECTED

**Design:** `docs/designs/enforcement-v2-pre-push-hook.md` (Gandalf)  
**Review:** `docs/reviews/enforcement-v2-boromir-review.md`  
**Decision:** `boromir-enforcement-v2.md` → inbox

**What happened:** Gandalf proposed a pre-push git hook to enforce path-based main branch protection — only `.squad/` files allowed direct-to-main, everything else must go through PRs. This was V2 after Jonathan killed V1 (branch protection alone can't do path-based enforcement because all agents share one GitHub identity).

**My verdict:** REJECT. Four issues:
1. No evidence the instruction layer was fixed first (cheaper intervention skipped)
2. Design overclaims — calls a bypassable client-side hook "enforcement" when `--no-verify` defeats it trivially
3. Local merges to main can trap Scribe commits behind a blocked push; recovery (`reset --hard`) is data-destructive
4. Structural alternatives not explored (separate push mechanism, separate `.squad/` branch)

**What the design got right:** V1 failure analysis was precise. Scribe compatibility analysis was thorough. Bypass taxonomy was honest. Rollback plan was clean. The design is 80% — it just needs to answer harder questions about its own assumptions.

**Lesson learned:** Good designs pre-answer objections (Gandalf's Section 13 did this). Great designs pre-answer objections they *don't want to hear*. The merge+Scribe trap and the instruction-layer gap were the questions the design didn't ask itself.

### 2025-07-25: Teams MCP Integration Review — REJECTED

**Design:** `docs/designs/teams-mcp-integration.md` (Gandalf)  
**Review:** `docs/reviews/design-review-teams-mcp-integration.md`  
**Decision:** `boromir-teams-design-review.md` → inbox

**What happened:** Gandalf designed a three-feature Teams MCP integration: hybrid notification routing (keep webhooks, add WorkIQ reading), bidirectional channel monitoring (poll Teams for Jonathan's messages, classify, route to agents), and broad chat intelligence (scan all Jonathan's chats for action items, commitments, deadlines). Built on Elrond's deep-dive into the Agency Teams MCP server (preview-only, 26 tools).

**My verdict:** REJECT. Four blocking issues:
1. WorkIQ's 5-60 min indexing delay means Feature 2's "bidirectional channel" has 15-75 min latency — destroys the value prop for urgent directives
2. Feature 3 scans ALL of Jonathan's private chats with zero privacy/scope model — no allowlist, no blocklist, raw excerpts committed to git
3. Dedup hash on LLM-generated summaries is non-deterministic — same message summarized differently across scans will create duplicates
4. Zero error handling for a system running 48+ times/day

**What the design got right:** Correct decision to keep webhooks for outbound (Adaptive Cards > plain text). Phased approach that doesn't block on MCP GA. Thorough task decomposition (15 issues). Good classifier taxonomy. Elrond's research was properly integrated. The routing decision matrix is clean and honest. Cost estimation with fallback. Design is 65% — it needs to answer the hard questions about latency, privacy, and reliability.

**Lesson learned:** Designs that depend on non-deterministic external systems (WorkIQ NL, LLM classification) need TWO levels of defense: the obvious one (dedup hashing) and the one the design doesn't want to admit (what if the source misses data entirely? what if the hash key itself is non-deterministic?). Also: when a design sells responsiveness ("Jonathan posts, squad reacts") but the underlying system adds 15-75 minutes of latency, call it out — overselling creates user expectations the system can't meet.

### 2025-07-25: Teams MCP Integration v2 Re-Review — APPROVED

**Design:** `docs/designs/teams-mcp-integration.md` (Gandalf, v2)  
**Original Review:** `docs/reviews/design-review-teams-mcp-integration.md`  
**Re-Review:** `docs/reviews/design-review-teams-mcp-integration-v2.md`  
**Decision:** `boromir-teams-design-v2.md` → inbox

**What happened:** Second review cycle. Gandalf revised the design addressing all four blockers and five non-blocking improvements from my first review. I re-reviewed each fix for substance vs. hand-waving.

**My verdict:** APPROVE. All four blockers genuinely fixed:
1. **Latency lie → honest latency table.** Feature rescoped to non-urgent directives only. Copilot CLI documented as the fast path. No more overselling.
2. **Privacy gap → default-deny allowlist.** `scope.json` with explicit opt-in, raw excerpts not committed to git, monthly consent review, audit trail. Structural fix.
3. **Broken dedup → message identity hashing.** `SHA256(source + sender + timestamp)` instead of LLM output. Correct root-cause fix.
4. **No error handling → comprehensive §10.** Retry policy, circuit breakers, state recovery, kill switch, dry-run mode, health monitoring. Production-grade.

Three minor new issues introduced (NL-based watchdog exclusion, scope.json chat name staleness, WorkIQ raw excerpt substring dedup), all low severity.

**Lesson learned:** A good revision answers the hard questions directly — it doesn't deflect or minimize. Gandalf's revision table explicitly cited each blocker ID and the structural change made. That's accountability. When reviewing second drafts, the question isn't "did they touch the right section?" but "did they change the architecture or just the words?" In this case, the architecture changed: new privacy model, new dedup key source, new error handling section, honest latency framing. That's why it earned an approve — not because the problems disappeared, but because the design now owns them.
