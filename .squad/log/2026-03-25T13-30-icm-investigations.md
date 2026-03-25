# Session Log: 2026-03-25 — IcM Investigations (Aragorn)

**Timestamp:** 2026-03-25T13:30 UTC  
**Agents:** Aragorn (background, 2 parallel investigations)  
**Topic:** CosmosDbPublisher pipeline throttling incidents

---

## Summary

Two concurrent IcM investigations on recurring Cosmos DB RU throttling in the Threat Intelligence pipeline:

| Incident | Region | Status | Root Cause | Finding |
|----------|--------|--------|-----------|---------|
| IcM#768125338 | northeurope | ACTIVE (expected self-resolution) | RU spike (4×) on legacy indicator upserts | 31st recurrence; 0 customer impact |
| IcM#768125136 | westeurope | ACTIVE (3h 17min, no recovery) | Infinite retry + batch-terminating 429 handling | 2nd recurrence in 9 days; **YES customer impact** |

**Shared Pattern:** Cosmos DB throttling on burst write operations. IcM#768125136 represents a production defect (infinite retry policy) that extends a transient throttling event into sustained outage.

---

## Decisions Merged

- **6 unique decision items** across both investigations
- **Primary action:** Replace infinite retry policy in CosmosDbPublisher (P0)
- **Secondary actions:** Cosmos autoscale, 429 per-item handling, FCM change review step

---

## Outcomes

- IcM#768125338: Investigation complete. No action needed; expect auto-resolution.
- IcM#768125136: Investigation complete. **Requires immediate mitigation + P0 engineering fix.**

**Logs:** `.squad/orchestration-log/2026-03-25T13-30-aragorn-icm768*.md`  
**Reports:** `docs/investigations/icm-76812513{6,8}-investigation.md`

