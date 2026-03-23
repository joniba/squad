---
title: "Rate Limiting & Resource Coordination at Scale"
author: Elrond
date: "2026-03-24"
status: "research"
tags:
  - rate-limiting
  - multi-agent
  - quota-coordination
  - GitHub-API
  - scaling
  - ms-pa
summary: "6 coordination patterns for managing API quotas across 8+ agents; Rate Governor design; fairness analysis; implementation roadmap for ms-pa"
---

## Executive Summary

**Problem:** The ms-pa squad (8 agents × 12 rounds/hour ≈ 1,152–2,880 requests/hour) is constrained by GitHub API quotas (5,000 requests/hour core, 80 Copilot completions/hour). Independent per-agent retry logic causes cascading failures, thundering herd effects during quota resets, and unfair quota distribution.

**Solution:** Implement a **Rate Governor** — a centralized, file-based quota coordinator that tracks real-time X-RateLimit headers, predicts exhaustion, and routes requests through a shared token pool with priority queuing.

**Key Patterns Researched:**
1. **Token Bucket** — Emit tokens at steady rate; agents consume before request
2. **Shared Token Pool** — Centralized quota distribution across agents
3. **Predictive Circuit Breaker** — Proactive throttling via X-RateLimit-Remaining < threshold
4. **Priority Queuing** — Route high-value requests first (e.g., PRs) before routine syncs
5. **Adaptive Backoff + Jitter** — Exponential backoff with randomization breaks thundering herd
6. **Quota Recycling** — Dynamically reallocate quota based on per-agent consumption patterns

**Recommendation for ms-pa:** Implement Pattern 1 + Pattern 2 (Token Bucket + Shared Pool) with Pattern 3 (Predictive Circuit Breaker) as Phase 1. This addresses cascading failures with minimal complexity.

---

## Context & Problem Analysis

### Current State: Independent Rate Limiting Fails

Each ms-pa agent currently retries independently on 429 (rate limit exceeded):
- **Agent A** hits 429 → exponential backoff (1s, 2s, 4s...)
- **Agent B** hits 429 → same backoff pattern
- **Agents C–H** → all retry with correlated backoff intervals
- **Result:** All agents retry simultaneously at ~2^N interval → **thundering herd** → cascading service failures

### Quota Contention Scenario

**Setup:**
- 8 agents (Gandalf, Elrond, Bilbo, Gimli, Ralph watchdog, plus 3 others)
- 12 rounds/hour per agent
- 6–18 premium requests per watchdog run (depends on scope)
- GitHub API: 5,000 requests/hour; 80 Copilot completions/hour

**Peak Load Calculation:**
```
Baseline: 8 agents × 12 rounds/hour = 96 baseline polling requests/hour
High watchdog activity: +6–18 premium requests/hour = 102–114 requests/hour
Buffer capacity: 5,000 - 114 = 4,886 requests available
Margin: 97.7% of quota available... but distributed unevenly across agents
```

**Failure Mode #1: Uneven Distribution**
- Agent A makes 10 requests, exhausts quota fragment
- Agents B–H queue behind A
- If A then fails (network timeout, auth error), its stalled requests block queue
- Result: 7 agents idle, waiting for A's backoff to complete

**Failure Mode #2: Quota Reset Spike**
- GitHub resets quotas on rolling 1-hour window
- All agents see `X-RateLimit-Remaining: 5000` at reset
- All wake up simultaneously and burst
- First 100 requests from agent group → 4,900 remaining
- Next agent batch → fails at 429
- Agents 5–8 never get tokens that hour
- **Unfairness:** Early agents get full quota; later agents starve

**Failure Mode #3: Cascading 429s**
- Single 429 response → agent backoff → other agents detect latency → assume 429 → proactive backoff
- Cascade spreads through queue → all 8 agents throttled even though quota wasn't exhausted
- Actual quota: 2,000 requests unused; perceived quota: 0

---

## 6 Coordination Patterns for API Rate Limiting

### Pattern 1: Token Bucket Algorithm

**How it works:**
- Central token store initialized with `N` tokens (e.g., 5,000)
- Tokens replenish at steady rate: `rate = N / window_duration` (e.g., 5,000 / 3,600s ≈ 1.39 tokens/sec)
- Each request costs 1 token
- Empty bucket → request denied or queued

**Pseudocode:**
```
TokenBucket:
  max_tokens: 5000
  tokens: 5000
  refill_rate: 1.39 tokens/sec
  last_refill_time: now()
  
RequestHandler(agent):
  refill()
  if tokens >= 1:
    tokens -= 1
    return ALLOW
  else:
    return DENY_QUEUE
    
refill():
  now = current_time()
  elapsed = now - last_refill_time
  tokens = min(max_tokens, tokens + elapsed * refill_rate)
  last_refill_time = now
```

**Strengths:**
- ✅ Allows controlled bursts (tokens accumulate)
- ✅ Predictable behavior; mathematically proven fair over time
- ✅ Simple to implement in a single file (JSON state)
- ✅ Handles quota resets naturally (reinitialize bucket)

**Weaknesses:**
- ❌ Doesn't account for X-RateLimit-Remaining variance across API endpoints
- ❌ Requires synchronization if agents write to shared state file simultaneously

**Fairness Grade:** A (fair distribution if agents poll equally)

**Implementation Ease:** ⭐⭐⭐⭐ (4/5 — straightforward math)

---

### Pattern 2: Shared Token Pool with Per-Agent Quotas

**How it works:**
- Central pool allocates sub-quotas to each agent
- Each agent has local bucket (e.g., 625 tokens for 8 agents sharing 5,000)
- When local bucket empty, agent requests tokens from pool
- Pool tracks global remaining; denies new allocations if approaching limit

**Pseudocode:**
```
SharedTokenPool:
  global_tokens: 5000
  per_agent_quota: 625  # 5000 / 8 agents
  agent_buckets:
    gandalf: {tokens: 625, timestamp: now()}
    elrond:  {tokens: 625, timestamp: now()}
    ... (6 more agents)
  
RequestHandler(agent):
  if agent.bucket.tokens >= 1:
    agent.bucket.tokens -= 1
    return ALLOW
  elif global_tokens >= per_agent_quota * 0.5:  # Safety threshold
    refill_agent_bucket(agent)
    agent.bucket.tokens -= 1
    return ALLOW
  else:
    return DENY_QUEUE
```

**Strengths:**
- ✅ Prevents single agent from hogging quota
- ✅ Enables burst capacity locally (agent can use full 625 tokens rapidly)
- ✅ Graceful degradation: as quota approaches limit, refill requests denied
- ✅ Fair per-agent: each agent gets equal baseline allocation

**Weaknesses:**
- ❌ Requires coordination on refill thresholds
- ❌ If one agent unused, its quota blocked from other agents (waste)
- ❌ Slightly more complex state management

**Fairness Grade:** B+ (fair until quota exhaustion; unused quotas may be wasted)

**Implementation Ease:** ⭐⭐⭐⭐ (4/5 — moderate complexity)

---

### Pattern 3: Predictive Circuit Breaker (HTTP 429 Headers)

**How it works:**
- Every API response includes rate-limit headers: `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset`
- Governor tracks `Remaining` per endpoint
- When `Remaining < threshold` (e.g., 10% of limit), proactively throttle all agents **before** 429 occurs
- Circuit breaker trips on N consecutive 429s; reopens after backoff + probe success

**Pseudocode:**
```
PredictiveCircuitBreaker:
  threshold_percent: 0.10  # Open circuit at <10% quota remaining
  consecutive_429s: 0
  max_429_tolerance: 5
  state: CLOSED  # CLOSED | OPEN | HALF_OPEN
  
OnResponse(status, headers):
  remaining = parse_header(headers, 'X-RateLimit-Remaining')
  limit = parse_header(headers, 'X-RateLimit-Limit')
  
  if remaining / limit < threshold_percent:
    state = OPEN  # Throttle all agents proactively
    return THROTTLE_ALL
  
  if status == 429:
    consecutive_429s += 1
    if consecutive_429s >= max_429_tolerance:
      state = OPEN  # Circuit breaker tripped
      return REJECT_ALL
      retry_after = parse_header(headers, 'Retry-After')
      schedule_probe(retry_after)
  else:
    consecutive_429s = 0  # Reset counter on success
```

**Strengths:**
- ✅ Proactive: stops 429s before they occur
- ✅ Uses data from API itself (X-RateLimit headers) — no guessing
- ✅ Breaks correlated retry storms (prevents thundering herd)
- ✅ Automatic recovery: circuit breaker reopens after cooldown

**Weaknesses:**
- ❌ Adds latency (proactive throttling) even if quota available
- ❌ Requires parsing multiple header formats (GitHub, Copilot use different schemes)

**Fairness Grade:** A (proactive throttling prevents unfair quota starvation)

**Implementation Ease:** ⭐⭐⭐ (3/5 — requires header parsing + state machine)

---

### Pattern 4: Priority Queuing

**How it works:**
- High-priority requests (e.g., PR reviews, critical metrics) queue separately
- Low-priority requests (e.g., routine syncs) queue separately
- Governor allocates quotas: high-priority first, then low-priority if quota available
- Prevents routine tasks from blocking critical work

**Pseudocode:**
```
PriorityQueue:
  HIGH_PRIORITY: []    # PRs, decision items, high-severity issues
  MEDIUM_PRIORITY: []  # Routine checks, metric polling
  LOW_PRIORITY: []     # Archive reads, background scans
  
Enqueue(request, priority):
  if priority == HIGH:
    HIGH_PRIORITY.push(request)
  elif priority == MEDIUM:
    MEDIUM_PRIORITY.push(request)
  else:
    LOW_PRIORITY.push(request)
    
Dequeue(available_tokens):
  dequeued = []
  
  # Drain HIGH first
  while HIGH_PRIORITY.not_empty() and available_tokens > 0:
    dequeued.push(HIGH_PRIORITY.pop())
    available_tokens -= 1
  
  # Then MEDIUM
  while MEDIUM_PRIORITY.not_empty() and available_tokens > 0:
    dequeued.push(MEDIUM_PRIORITY.pop())
    available_tokens -= 1
  
  # Finally LOW
  while LOW_PRIORITY.not_empty() and available_tokens > 0:
    dequeued.push(LOW_PRIORITY.pop())
    available_tokens -= 1
  
  return dequeued
```

**Strengths:**
- ✅ Ensures critical work completes even under quota pressure
- ✅ Degrades gracefully: low-priority work queues; high-priority work continues
- ✅ Operator can adjust priorities dynamically (e.g., "favor PRs at 3pm")

**Weaknesses:**
- ❌ Requires classification of requests by priority (not automatic)
- ❌ Low-priority work may starve indefinitely
- ❌ Adds queuing latency and complexity

**Fairness Grade:** C (not fair to low-priority agents, but fair to high-priority)

**Implementation Ease:** ⭐⭐⭐ (3/5 — requires priority classification + queue management)

---

### Pattern 5: Adaptive Backoff with Jitter

**How it works:**
- When 429 received, agent backs off with exponential delay: 1s, 2s, 4s, 8s, 16s...
- **Jitter** (randomization) added to each delay to break correlated retries
- Formula: `jitter_delay = random(0, exponential_backoff)`
- Prevents all agents from retrying simultaneously on quota reset

**Pseudocode:**
```
AdaptiveBackoffWithJitter:
  base_delay: 1
  max_delay: 64
  attempt: 0
  
OnRateLimited(headers):
  attempt += 1
  
  # Exponential backoff: 1, 2, 4, 8, 16, 32, 64
  exponential_delay = min(base_delay * 2^attempt, max_delay)
  
  # Full jitter: pick random value in [0, exponential_delay]
  jitter_delay = random(0, exponential_delay)
  
  # Honor Retry-After header if present (it's authoritative)
  retry_after = parse_header(headers, 'Retry-After')
  actual_delay = max(jitter_delay, retry_after)
  
  sleep(actual_delay)
  retry()
```

**Example with 8 agents hitting 429 simultaneously:**
```
Agent A: exponential_delay=4s, jitter_delay=2.3s, sleep(2.3s), retry at T+2.3s
Agent B: exponential_delay=4s, jitter_delay=3.8s, sleep(3.8s), retry at T+3.8s
Agent C: exponential_delay=4s, jitter_delay=0.2s, sleep(0.2s), retry at T+0.2s
... (no two agents retry at same time)
Result: Retries spread over 4-second window; no thundering herd
```

**Strengths:**
- ✅ Eliminates synchronized retry storms
- ✅ Proven technique (AWS, Google use it)
- ✅ Per-agent; no central coordination needed
- ✅ Handles quota resets naturally (jitter breaks synchronization at reset)

**Weaknesses:**
- ❌ Adds unpredictable latency
- ❌ Requires per-agent implementation (not a central policy)
- ❌ Doesn't prevent thundering herd if all agents use same seed

**Fairness Grade:** B+ (fair over time; latency variance increases)

**Implementation Ease:** ⭐⭐⭐⭐ (4/5 — simple math, but per-agent code needed)

---

### Pattern 6: Quota Recycling (Dynamic Reallocation)

**How it works:**
- Monitor per-agent quota consumption over time window (e.g., 1 hour)
- Agents that used <50% of allocation → quota reclaimed
- Reclaimed quota → reallocated to high-demand agents
- Unused quota not wasted; active agents get larger share

**Pseudocode:**
```
QuotaRecycling (hourly cycle):
  per_agent_used: {
    gandalf: 200,
    elrond: 400,
    bilbo: 100,
    gimli: 350,
    ralph: 300,
    others: 200
  }
  
  per_agent_quota: 625  # baseline
  
  # Phase 1: Calculate utilization
  utilization: {
    gandalf: 200/625 = 32%,
    elrond: 400/625 = 64%,
    bilbo: 100/625 = 16%,  <- BELOW THRESHOLD (50%)
    gimli: 350/625 = 56%,
    ralph: 300/625 = 48%,  <- BELOW THRESHOLD
    others: 200/625 = 32%  <- BELOW THRESHOLD
  }
  
  # Phase 2: Reclaim unused quota
  reclaimed = (100 + 300 + 200) = 600 tokens
  
  # Phase 3: Reallocate to high-demand agents
  high_demand_agents = [elrond, gimli]  # >50% utilization
  additional_quota = reclaimed / len(high_demand_agents) = 300
  
  new_allocation: {
    elrond: 625 + 300 = 925,
    gimli: 625 + 300 = 925,
    bilbo: 625 - 100 = 525,  # penalize underuse
    ... (others similarly)
  }
```

**Strengths:**
- ✅ Maximizes quota utilization (no waste)
- ✅ Rewards high-demand agents; incentivizes underperformers
- ✅ Adapts dynamically to workload changes
- ✅ Prevents quota starvation for latency-sensitive agents

**Weaknesses:**
- ❌ Complex state management; harder to reason about
- ❌ Agents may be penalized unfairly (e.g., offline maintenance = low utilization)
- ❌ Requires tuning thresholds and reallocation formula
- ❌ Fairness can decrease for low-utilization agents

**Fairness Grade:** B (fair to high-demand; unfair to low-utilization)

**Implementation Ease:** ⭐⭐ (2/5 — complex analytics + state transitions)

---

## Rate Governor: Proposed Design

### Data Structure

```json
{
  "version": "1.0",
  "created_at": "2026-03-24T14:30:00Z",
  "last_updated_at": "2026-03-24T14:30:00Z",
  "global_state": {
    "quota_limit": 5000,
    "quota_used": 240,
    "quota_remaining": 4760,
    "quota_reset_at": "2026-03-24T15:30:00Z",
    "circuit_breaker_state": "CLOSED",
    "circuit_breaker_open_at": null,
    "circuit_breaker_consecutive_429s": 0
  },
  "agent_quotas": {
    "gandalf": {
      "quota_baseline": 625,
      "quota_used": 40,
      "quota_remaining": 585,
      "priority": "HIGH",
      "request_queue": []
    },
    "elrond": {
      "quota_baseline": 625,
      "quota_used": 80,
      "quota_remaining": 545,
      "priority": "HIGH",
      "request_queue": []
    },
    ... (6 more agents)
  },
  "request_queue": {
    "HIGH_PRIORITY": [
      {"agent": "gandalf", "request_id": "pr-review-42", "created_at": "2026-03-24T14:29:00Z"}
    ],
    "MEDIUM_PRIORITY": [
      {"agent": "elrond", "request_id": "routine-sync-1", "created_at": "2026-03-24T14:28:00Z"}
    ],
    "LOW_PRIORITY": []
  },
  "rate_limit_headers": {
    "X-RateLimit-Limit": "5000",
    "X-RateLimit-Remaining": "4760",
    "X-RateLimit-Reset": "2026-03-24T15:30:00Z",
    "Retry-After": null,
    "last_parsed_at": "2026-03-24T14:30:00Z",
    "endpoint": "github-api"
  }
}
```

### Core Operations

**1. RequestToken(agent, priority)**
```
input: agent (string), priority (HIGH|MEDIUM|LOW)
output: {allowed: bool, wait_ms: int, reason: string}

1. If circuit_breaker_state == OPEN:
   - If time_since_open > cooldown_period:
     - Send probe request
     - If probe succeeds: circuit_breaker_state = HALF_OPEN
     - Else: keep OPEN
   - Return {allowed: false, wait_ms: 5000, reason: "circuit breaker open"}

2. Enqueue(agent, priority)
3. Dequeue(available_tokens)
4. If dequeue succeeds:
   - Return {allowed: true, wait_ms: 0}
5. Else:
   - Return {allowed: false, wait_ms: estimate_queue_wait()}
```

**2. RecordResponse(agent, status, headers)**
```
input: agent (string), status (int), headers (dict)
output: void

1. Parse X-RateLimit-Limit, X-RateLimit-Remaining, X-RateLimit-Reset
2. Update global_state.quota_remaining from header
3. If remaining / limit < 0.1 (10% threshold):
   - circuit_breaker_state = OPEN
4. If status == 429:
   - circuit_breaker_consecutive_429s += 1
   - If consecutive_429s >= 5:
     - circuit_breaker_state = OPEN
   - Retry-After = parse_header(headers, 'Retry-After')
   - schedule_backoff_retry(agent, Retry-After)
5. Else:
   - circuit_breaker_consecutive_429s = 0
6. Save to disk (atomic write)
```

**3. HourlyRecycleQuota()**
```
input: none
output: void

1. For each agent:
   - utilization = quota_used / quota_baseline
   - If utilization < 0.5 (below 50%):
     - mark as LOW_DEMAND
   - Else:
     - mark as HIGH_DEMAND

2. reclaimed = sum(quota_baseline * (1 - utilization) for LOW_DEMAND agents)

3. For each HIGH_DEMAND agent:
   - additional = reclaimed / len(HIGH_DEMAND agents)
   - quota_baseline += additional

4. For each LOW_DEMAND agent:
   - penalty = quota_baseline * 0.1 (10% penalty)
   - quota_baseline -= penalty

5. Reset agent.quota_used = 0 (new hour)
6. Save to disk
```

### Integration Points

**Agent-side (ralph-watch.ps1 pattern):**
```powershell
# Before making request
$token_request = Invoke-Expression (Get-Content ~/.squad/rate-governor/request-token.ps1)
if ($token_request.allowed) {
  Make-Request
  Invoke-Expression (Get-Content ~/.squad/rate-governor/record-response.ps1)
} else {
  Start-Sleep -Milliseconds $token_request.wait_ms
  Retry-Request
}
```

**File-based sync (no variable capture):**
- Rate Governor writes JSON to `~/.squad/rate-governor/state.json`
- Agents read state, apply lock-free read
- Agents append request to queue file
- Rate Governor polls queue file, dequeues, writes response

---

## Fairness Analysis

### Scenario A: Uniform Load (All Agents Polling Equally)

| Pattern | Per-Agent Quota | Fairness | Notes |
|---------|-----------------|----------|-------|
| Token Bucket | 625 baseline | ✅ A | Equal tokens over time; agents that idle don't block others |
| Shared Pool | 625 baseline | ✅ A | Same as Token Bucket; fair baseline |
| Predictive Circuit Breaker | Varies | ✅ B+ | Throttles all agents equally when quota <10%; no starvation |
| Priority Queuing | Varies | ⚠️ C | Low-priority agents may never get tokens if high-priority queue full |
| Adaptive Backoff + Jitter | N/A (per-agent) | ✅ B+ | Fair over time; jitter breaks synchronization; no fairness violations |
| Quota Recycling | Varies | ⚠️ B | Penalizes underutilized agents; unfair for maintenance windows |

**Recommendation:** Use Token Bucket + Shared Pool (fairness grade A) as baseline.

---

### Scenario B: Bursty Load (One Agent Dominates)

**Setup:** Gandalf watchdog makes 300 requests in 10 minutes; other agents idle.

| Pattern | Gandalf | Elrond | Fairness |
|---------|---------|--------|----------|
| Token Bucket (no limits) | 300 (bursts allowed) | 4700 remaining | ✅ Fair: Gandalf gets full burst; others have quota after |
| Shared Pool (625 baseline) | 625 local, then blocked | 625 remaining | ⚠️ Unfair: Gandalf forced to queue even if global quota available |
| Predictive Circuit Breaker | N/A (no 429s occurred) | N/A | ✅ Fair: Predictive only kicks in at <10% quota |
| Priority Queuing | HIGH requests proceed; LOW queued | Normal | ✅ Fair: Critical Gandalf work proceeds; routine work queues |
| Quota Recycling | Extra quota reallocated | Reduced quota | ⚠️ Unfair: Gandalf rewarded for burst; others penalized |

**Recommendation:** Token Bucket alone handles bursts fairly. Add Predictive Circuit Breaker for safety.

---

### Scenario C: Thundering Herd (All Agents Hit 429 Simultaneously)

**Setup:** All 8 agents hit 429 at T=0. Each uses independent exponential backoff with same seed.

| Pattern | Outcome | Fairness |
|---------|---------|----------|
| No coordination (independent backoff) | ❌ All retry at T+4s, T+8s, T+16s simultaneously → new 429s cascade | ❌ D |
| Adaptive Backoff + Jitter | ✅ Retries stagger over 4s window; quota recovers | ✅ A |
| Predictive Circuit Breaker | ✅ Circuit opens on first 429; blocks all agents; probes after cooldown | ✅ B+ |
| Shared Token Pool + Recycling | ✅ Pool exhausted; agents queue; recycling reallocates unused quotas | ✅ B |

**Recommendation:** Adaptive Backoff + Jitter is essential for handling thundering herd.

---

## Implementation Roadmap for ms-pa

### Phase 1: Minimal Viable Coordination (Weeks 1–2)

**Goals:**
- Stop independent per-agent retry logic
- Implement Token Bucket (centralized quota tracking)
- Add Predictive Circuit Breaker (X-RateLimit-Remaining monitoring)

**Files to create:**
1. `~/.squad/rate-governor/state.json` — Token bucket state
2. `.squad/bin/rate-governor-init.ps1` — Initialize Rate Governor
3. `.squad/bin/request-token.ps1` — Check quota; return allowed/deny
4. `.squad/bin/record-response.ps1` — Update state on 200/429
5. `docs/rate-governor-usage.md` — Operator guide

**Changes to agents:**
- Replace per-agent retry logic with Rate Governor calls
- Update ralph-watch.ps1 to integrate Rate Governor
- Add error handling for DENY responses (queue + retry with backoff)

**Acceptance Criteria:**
- ✅ Token Bucket initialized with 5,000 tokens
- ✅ Quota resets correctly on X-RateLimit-Reset timestamp
- ✅ Agents wait before making requests if quota <10%
- ✅ No cascading 429s under uniform load (8 agents × 12 cycles/hour)
- ✅ Thundering herd test: all 8 agents hit 429 → retries stagger (jitter test)

---

### Phase 2: Fair Quota Distribution (Weeks 3–4)

**Goals:**
- Prevent single agent from hogging quota
- Implement Shared Token Pool with per-agent baseline (625 tokens)
- Add Priority Queuing (HIGH, MEDIUM, LOW)

**Files to modify:**
1. `state.json` — Add per-agent quota tracking + priority queue
2. `.squad/bin/enqueue-request.ps1` — Classify priority; enqueue
3. `.squad/bin/dequeue-requests.ps1` — Drain high-priority first

**Acceptance Criteria:**
- ✅ Each agent gets 625-token baseline
- ✅ Bursty agent (Gandalf) doesn't starve others
- ✅ PR reviews (HIGH) proceed even if routine syncs queued
- ✅ No unfair quota starvation under mixed load (bursty + idle)

---

### Phase 3: Dynamic Reallocation (Weeks 5–6)

**Goals:**
- Implement Quota Recycling
- Reward high-demand agents; penalize underutilized agents
- Add observability: quota utilization dashboard

**Files to add:**
1. `.squad/bin/recycle-quota-hourly.ps1` — Reallocation logic
2. `docs/rate-governor-observability.md` — Metrics definitions

**Acceptance Criteria:**
- ✅ Hourly quota reallocation working (test with synthetic low/high utilization)
- ✅ High-demand agents get +20% quota; low-demand agents -10%
- ✅ Quota allocation converges to steady state (no oscillations)
- ✅ Observability: per-agent utilization, circuit breaker state, queue depth logged

---

## Cascading Failure Resolution

### Root Cause: No Centralized Quota Awareness

Currently:
- Each agent retries independently
- No agent knows if other agents are also retrying
- Each agent assumes: "I hit 429 → my quota exhausted → wait"
- But global quota available; just local fragment exhausted

### Solution: Rate Governor as Source of Truth

With Rate Governor:
- Global quota tracked in one place: `~/.squad/rate-governor/state.json`
- Agents ask: "Can I make a request?" → governor responds "yes" or "wait N ms"
- Governor parses X-RateLimit headers → knows actual quota available
- Agents don't retry independently; governor controls retry timing

### Cascade Prevention Example

**Before (independent retry):**
```
T=0:00   Agent A hits 429 → backoff until T+4s
T=0:00   Agent B hits 429 → backoff until T+4s
T=0:00   Agent C hits 429 → backoff until T+3.8s (jitter)
T=0:03   Agent C retries → hits 429 (quota still 0)
T=0:03   Agent D sees latency → assumes 429 → proactive backoff
T=0:04   Agents A, B retry simultaneously → both hit 429
T=0:04   Agents E, F, G, H see cascade → cascade through team
T=0:08   Finally retry window clears quota; cascade subsides
RESULT: 8 seconds of cascading 429s; multiple agents affected
```

**After (Rate Governor):**
```
T=0:00   Agents A–H all hit 429 → Rate Governor sees X-RateLimit-Remaining: 0
T=0:00   Rate Governor: circuit_breaker_state = OPEN
T=0:00   Rate Governor broadcasts: wait 60 seconds
T=0:00   Agents A–H receive: {allowed: false, wait_ms: 60000}
T=0:00   Agents A–H sleep for 60s (coordinated, no retry storms)
T=1:00   Agents A–H wake; ask Rate Governor for token
T=1:00   Rate Governor checks X-RateLimit-Reset (now +1 hour)
T=1:00   Rate Governor: circuit_breaker_state = HALF_OPEN; send probe
T=1:01   Probe succeeds; circuit_breaker_state = CLOSED
T=1:01   Dequeue requests → agents proceed normally
RESULT: Cascading failure prevented; orderly recovery
```

---

## GitHub API Headers Reference

**Standard Rate Limit Headers:**
```
HTTP/1.1 200 OK
X-RateLimit-Limit: 5000
X-RateLimit-Remaining: 4760
X-RateLimit-Reset: 1711270200
```

**On 429 (Rate Limited):**
```
HTTP/1.1 429 Too Many Requests
Retry-After: 3600
X-RateLimit-Limit: 5000
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1711270200
```

**Copilot-Specific (if applicable):**
```
X-RateLimit-Limit: 80
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1711270200
```

**Rate Governor must track both endpoints separately:**
```json
{
  "endpoints": {
    "github-core-api": {
      "quota_limit": 5000,
      "quota_remaining": 4760,
      "quota_reset": "2026-03-24T15:30:00Z"
    },
    "github-copilot-api": {
      "quota_limit": 80,
      "quota_remaining": 0,
      "quota_reset": "2026-03-24T15:30:00Z"
    }
  }
}
```

---

## Deployment Checklist

- [ ] Rate Governor state file initialized (`.squad/rate-governor/state.json`)
- [ ] TokenBucket algorithm implemented (replenish logic, quota checks)
- [ ] Predictive Circuit Breaker logic working (429 detection, probes)
- [ ] All 8 agents updated to use Rate Governor (ralph-watch.ps1 integration)
- [ ] Jitter algorithm added to per-agent retry logic
- [ ] Per-agent quota baseline set (625 tokens each)
- [ ] Priority queue implemented (HIGH, MEDIUM, LOW classification)
- [ ] Hourly quota recycling scheduled (if Phase 3 enabled)
- [ ] Observability: rate-governor logs to `.squad/log/rate-governor.log`
- [ ] Operator guide written: `docs/rate-governor-usage.md`
- [ ] Test harness: simulate 429s, thundering herd, quota reset
- [ ] Integration test: all 8 agents run 12 cycles/hour without cascades

---

## Key Insights & Recommendations

### Insight 1: Centralized Coordination is Essential

Independent per-agent retry logic fails catastrophically at 8+ agents because **agents don't know what other agents are doing**. The thundering herd problem is unsolvable without a central source of truth.

### Insight 2: Token Bucket + Predictive Circuit Breaker is the Minimum Viable Solution

The combination handles:
- Fair quota distribution (Token Bucket)
- Cascading failure prevention (Predictive Circuit Breaker)
- Thundering herd breakage (Adaptive Backoff + Jitter)

All other patterns (Priority Queuing, Quota Recycling) are optimizations.

### Insight 3: X-RateLimit Headers Are Gold

GitHub's API includes `X-RateLimit-Remaining` in every response. Using this data proactively (instead of waiting for 429) prevents cascading failures entirely.

### Insight 4: File-Based State Is Sufficient for ms-pa

No need for Redis or external cache. A single JSON file (`.squad/rate-governor/state.json`) with atomic writes (PowerShell `-Force`) is sufficient for 8 agents.

### Insight 5: Fairness Requires Intentional Design

Fairness doesn't emerge from chaos. With Rate Governor:
- Token Bucket ensures equal baseline (625 tokens per agent)
- Shared Pool prevents hogging
- Predictive Circuit Breaker prevents unfair starvation during quota shortages
- Quota Recycling (Phase 3) rewards utilization

---

## References & Sources

- **Token Bucket Algorithm:** Redis tutorial "Build 5 Rate Limiters" (redis.io/tutorials)
- **Thundering Herd Problem:** singhajit.com, jaydipsatani.hashnode.dev, wikipedia
- **Exponential Backoff + Jitter:** AWS Architecture, Google Cloud best practices, Sophia Willows blog
- **Predictive Circuit Breaker:** Netflix's Hystrix library, Azure AppLens rate limiting guides
- **HTTP 429 Headers:** RFC 6585, GitHub API documentation, Atatus guides
- **Multi-Agent Coordination:** Tamir Dresher's blog series on scaling AI systems (referenced in Issue #26)

---

## Decision & Next Steps

**Decision:** Implement Phase 1 (Token Bucket + Predictive Circuit Breaker) as **minimum viable solution** to Issue #26.

**Next Steps:**
1. Bilbo: Document Rate Governor design in `docs/rate-limiting-coordination.md` (synthesis + final recommendations)
2. Gimli: Implement Rate Governor scripts (5 files, ~300 lines total)
3. Ralph: Integrate Rate Governor into watchdog loop
4. Gandalf: Schedule Phase 2 (fair quota distribution) for Week 3

**Acceptance Criteria Met:**
- ✅ Rate Governor data structure designed
- ✅ 6 coordination patterns analyzed + compared
- ✅ Fairness analysis completed
- ✅ Cascading failure mechanics understood
- ✅ Practical roadmap provided for ms-pa's 8-agent setup

---

**Document prepared by:** Elrond (Researcher)  
**Date:** 2026-03-24  
**Status:** Research Complete; Ready for Bilbo's synthesis → Gandalf's decision
