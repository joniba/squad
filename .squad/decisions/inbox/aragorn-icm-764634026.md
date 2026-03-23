### 2026-03-22: ICM 764634026 Resolution Walkthrough — MSPKI G1→G2 Migration
**Author:** Aragorn (Operator)
**Status:** Proposed
**Issue:** #38 | **PR:** #50

**Context:** Jonathan's team has ~15 in-scope items under ICM 764634026 (OceanView SR17) requiring MSPKI G1→G2 root CA migration. Items were excluded from central migration due to safety concerns.

**Decision:** Built a comprehensive resolution doc at `docs/icm-764634026-resolution.md` covering:
- Blocker assessment decision tree (Pinning, Torus, ClientAuth, SDP, Rollback)
- Two migration paths: Central (April 10 deadline) vs Self-migration (May 16 deadline)
- Ready-to-paste Kusto queries for scoping, blocker ID, cert renewal, and validation
- Verification commands (az CLI, PowerShell, OpenSSL, browser)
- Rollback procedures (global and per-region)
- Exit criteria and Red Flag tag reference

**Key risk:** G2 certificates do NOT include ClientAuth EKU. Any service using MSPKI certs for client authentication must use self-migration path and address the ClientAuth blocker separately.

**Open items for Jonathan:**
1. Run the Kusto scoping queries to get exact item list
2. Review Ligal Tuval meeting recording for service-specific guidance
3. Decide central vs self-migration per item
4. Set ETA tags on each IcM

**Reasoning:** Combined ICM incident data (details, AI summary, context, mitigation hints, similar incidents) with the full OceanView SR17 TSG from eng.ms to produce a self-contained walkthrough that doesn't require re-reading the TSG.
