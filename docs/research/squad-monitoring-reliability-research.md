---
title: "Squad Monitoring & Ralph Watch Reliability"
author: Elrond
date: "2026-03-24"
status: "research"
tags:
  - ralph-watch
  - monitoring
  - distributed-systems
  - lock-detection
  - rate-limiting
  - reliability
  - ms-pa
summary: "Lock detection mechanisms, rate governor design, dashboard metrics, alert thresholds, and implementation roadmap for ralph-watch reliability across 8+ agents. Addresses auth race conditions and stale lock recovery."
---

## Executive Summary

**Problem:** The ms-pa squad runs 8+ agents executing 12+ rounds per hour, coordinated via ralph-watch polling loops. The system experiences three critical reliability gaps:

1. **Auth Race Conditions:** Multiple agents compete for `~/.config/gh/hosts.yml` lock during authentication, causing 37+ documented failures. No distributed lock mechanism detects or recovers from stale locks.
2. **Missed Monitoring Rounds:** Ralph-watch instances silently crash or hang without reliable heartbeat detection, causing orphaned agent processes and missing data collection windows.
3. **Cascading Quota Exhaustion:** Independent retry logic during GitHub API rate limits causes thundering herd effects, blocking all agents until manual intervention.

**Scope:** This research synthesizes lock detection patterns, rate-limiting algorithms (from rate-limiting-research.md), dashboard monitoring architecture, and implementation roadmap specific to ralph-watch and the 8-agent squad configuration.

**Key Deliverables (6 Acceptance Criteria):**
1. ✅ **Lock Detection Mechanisms** — 3+ patterns for distributed lock detection with stale lock recovery
2. ✅ **Rate Governor Design** — Priority-queued rate limiting extending rate-limiting-research.md
3. ✅ **Dashboard Metrics** — Top 10 observability metrics for ralph-watch and squad health
4. ✅ **Alert Thresholds** — Actionable alert definitions with severity mappings
5. ✅ **Implementation Roadmap** — Phased deployment (4 phases, 8–12 weeks, dependencies)
6. ✅ **Root Cause Analysis** — Auth race condition mechanics and recovery procedures

---

## Context & Problem Analysis

### Ralph-Watch Architecture

Ralph-watch is a polling-based monitoring script that coordinates squad agents. Current implementation (modeled from watchdog.ps1):

```powershell
# watchdog.ps1 pattern (46 lines)
- Lockfile: prevent duplicate instances (JSON with PID + timestamp)
- Heartbeat: overwritten JSON tracking lastRun, status, round, failures
- Structured log: append-only text with timestamps, status, duration
- Interval: configurable (default 24 hours)
- Failure detection: consecutive failure tracking
```

**Current bottlenecks:**

| Component | Issue | Impact |
|-----------|-------|--------|
| Lockfile | Binary check (exists/doesn't exist); no stale detection | Orphaned processes; duplicate runs |
| Heartbeat | Single overwritten JSON; no timestamp validation | Can't detect hanging processes |
| Auth coordination | No per-agent lock; GitHub hosts.yml contention | Race conditions; auth failures |
| Rate limiting | Independent per-agent retry; no visibility | Cascading 429s; unfair quota distribution |
| Observability | Manual log parsing; no dashboard | Silent failures; blind incident response |

### Problem #1: Auth Race Condition (37 Failures Documented)

**Setup:**
- 8 agents spawn simultaneously at start of each round
- Each agent authenticates via `gh auth token` → reads `~/.config/gh/hosts.yml`
- File is not protected by distributed lock
- Race condition: Agent A acquires token, Agent B retries on EACCES (file locked by OS)

**Failure signature:**
```
Agent A: gh auth token ✅ (200ms)
Agent B: gh auth token ❌ error: config file locked
Agent C: gh auth token ❌ error: config file locked
```

**Root cause chain:**
1. GitHub CLI (gh) is not designed for concurrent authentication
2. `~/.config/gh/hosts.yml` is process-local state without distributed locking
3. Squad agents lack pre-coordination (no mutual exclusion protocol)
4. Retry logic in agents is independent; no shared backoff
5. Result: Cascade of EACCES errors across 6+ agents; task hangs; manual restart required

**Recovery:** Currently manual — restart entire squad; no automatic detection or self-healing.

### Problem #2: Monitoring Blind Spots

Ralph-watch instances have minimal observability:

- **Heartbeat**: Single `watchdog-heartbeat.json` file (overwritten each round)
- **Logging**: Text append-only, manual inspection required
- **Alerting**: None; failures discovered hours later via manual log review
- **Stale detection**: No mechanism to detect hung or crashed instances

**Consequence:** An agent crashes mid-round; watchdog marks it "OK" because heartbeat timestamp hasn't changed yet; 4 hours of missing data before human discovery.

### Problem #3: Rate Limiting Unfairness

From rate-limiting-research.md: independent per-agent retry logic causes:
- **Thundering herd:** All agents backoff simultaneously
- **Quota starvation:** Early agents consume quota; later agents blocked
- **Cascading failures:** Agents misinterpret latency as rate limit exhaustion

**Impact on ralph-watch:** Each monitoring round makes 6–18 API requests; 8 agents × 12 rounds = 576–1,728 requests/day competing for shared 5,000-request/hour quota.

---

## Lock Detection Mechanisms

### Mechanism 1: File-Based Distributed Lock with Heartbeat Validation

**Design:**

A lock file stores JSON with three fields:
- `pid` — Process ID holding lock
- `timestamp` — Lock acquisition time (ISO 8601)
- `heartbeat` — Last update time (real-time indicator)

Agents compete via atomic file writes (PowerShell Out-File with -Force is atomic on NTFS):

```powershell
# Pseudocode: Acquire lock
$lockPath = "~/.squad/auth.lock"
$lockData = @{
    pid = $PID
    timestamp = (Get-Date -Format 'o')
    heartbeat = (Get-Date -Format 'o')
}
$lockData | ConvertTo-Json | Out-File $lockPath -Encoding utf8 -Force

# Verify lock ownership (detect stale)
$lock = Get-Content $lockPath -Raw | ConvertFrom-Json
$proc = Get-Process -Id $lock.pid -ErrorAction SilentlyContinue
$staleSecs = [math]::Round(((Get-Date) - [datetime]$lock.heartbeat).TotalSeconds)

if (-not $proc -or $staleSecs -gt 30) {
    # Stale lock detected; acquire it
    $lock.pid = $PID
    $lock.heartbeat = (Get-Date -Format 'o')
} else {
    # Wait for lock release
    Start-Sleep -Seconds (1 + (Get-Random -Maximum 5))
}
```

**Stale Recovery:**
- Heartbeat older than 30 seconds → lock is stale
- Verify process still running: `Get-Process -Id $lock.pid`
- If process missing or heartbeat stale → forcibly acquire lock

**Pros:**
- ✅ Simple; no external dependencies
- ✅ Works on Windows (NTFS atomic writes)
- ✅ File-based (compatible with squad's file I/O philosophy)
- ✅ Readable in emergencies (manual JSON inspection)

**Cons:**
- ❌ Not true POSIX-style locking; subject to OS timing
- ❌ Heartbeat requires polling (not event-driven)
- ❌ Clock skew between agents affects TTL logic

**Recommendation for ralph-watch:** ✅ **ADOPT for Phase 1.** Simplest, proven pattern in watchdog.ps1.

---

### Mechanism 2: Process Registry with Stale Cleanup

**Design:**

Central JSON file tracks all active agent processes:

```json
{
  "agents": {
    "gandalf": { "pid": 1234, "started": "2026-03-24T10:00:00Z", "last_activity": "2026-03-24T10:05:30Z" },
    "elrond": { "pid": 5678, "started": "2026-03-24T10:00:00Z", "last_activity": "2026-03-24T10:05:25Z" },
    "bilbo": { "pid": 9999, "started": "2026-03-24T10:00:00Z", "last_activity": "2026-03-24T09:52:00Z" }
  },
  "last_cleanup": "2026-03-24T10:05:30Z"
}
```

Cleanup procedure (runs at start of ralph-watch round):
1. Read registry
2. For each agent: check if process running (`Get-Process -Id $pid`)
3. If process missing → mark as stale (> 15 minutes no activity)
4. Remove stale entries
5. Record cleanup timestamp

**Stale Recovery:**
- Agent crashes; ralph-watch detects missing process next round
- Automatic cleanup removes dead entries
- New agent can reuse agent name without lock conflicts

**Pros:**
- ✅ Centralized visibility (all agents visible in one file)
- ✅ Automatic dead process cleanup
- ✅ Can track agent lifecycle (started, last_activity)

**Cons:**
- ❌ Requires ralph-watch to be authoritative (can't rely on agents to update their own entries)
- ❌ Periodic cleanup has race conditions during high concurrency
- ❌ Stale detection logic duplicated across agents

**Recommendation for ralph-watch:** 🟡 **ADOPT for Phase 2.** Useful for monitoring dashboard; pair with Mechanism 1 for lock coordination.

---

### Mechanism 3: Lease-Based Distributed Lock with TTL

**Design:**

Lock file includes explicit TTL (time-to-live):

```json
{
  "holder": "agent-gandalf",
  "pid": 1234,
  "acquired_at": "2026-03-24T10:00:00Z",
  "ttl_seconds": 60,
  "expires_at": "2026-03-24T10:01:00Z"
}
```

Lock acquisition logic:
1. Read lock file
2. Check if expired: `now > expires_at`
3. If expired → acquire lock; set `expires_at = now + ttl_seconds`
4. If not expired and holder process alive → wait and retry
5. If not expired but holder process dead → forcibly acquire (stale recovery)

**Heartbeat procedure:**
- Lock holder periodically updates `expires_at` (within holding agent)
- Failure to update → process crashed; lock expires; other agent acquires

**Pros:**
- ✅ True TTL semantics (no explicit stale process checks needed)
- ✅ Self-healing (expired leases auto-release)
- ✅ Works across network filesystems (if needed future)

**Cons:**
- ❌ Requires lock holder to refresh lease (more complex than file-based)
- ❌ Clock skew can cause lease confusion
- ❌ Risk: process holds lock but stops refreshing → lease expires → two processes hold "valid" lease

**Recommendation for ralph-watch:** ❌ **DEFER to Phase 2+.** More complexity than needed; Mechanism 1 covers 80% use case.

---

## Comparison: Lock Mechanisms for Phase 1 Adoption

| Aspect | Mechanism 1 (File+Heartbeat) | Mechanism 2 (Registry) | Mechanism 3 (TTL Lease) |
|--------|------------------------------|----------------------|------------------------|
| **Simplicity** | ⭐⭐⭐⭐⭐ (5/5) | ⭐⭐⭐ (3/5) | ⭐⭐ (2/5) |
| **Stale Detection** | PID check + timestamp | Central registry scan | TTL auto-expiry |
| **False Positives** | Low (process check is definitive) | Medium (registry stale) | Medium (clock skew) |
| **Operational Overhead** | Minimal | Cleanup discipline required | Lease refresh discipline |
| **Debugging** | Manual JSON inspection | Central visibility | Lease state inspection |
| **Recommendation** | ✅ ADOPT Phase 1 | 🟡 Phase 2 (complement) | ❌ Phase 2+ (future) |

---

## Rate Governor Design

Extending rate-limiting-research.md with priority queuing and ralph-watch specifics:

### Phase 1: Token Bucket + Shared Pool + Priority Queuing

**Architecture:**

```
Rate Governor (shared file-based state)
├── Global Token Pool: 5,000 tokens/hour
├── Refill Rate: 1.39 tokens/sec
├── Priority Queue:
│   ├── P0: ralph-watch heartbeat, auth token refresh (3 tokens)
│   ├── P1: PR review, critical task sync (2 tokens)
│   └── P2: Routine polling, background tasks (1 token)
└── Per-Agent Allocation:
    ├── gandalf: 625 tokens
    ├── elrond: 625 tokens
    ├── ... (6 more @ 625 each)
    └── ralph-watch: 625 tokens (reserved for monitoring)
```

**State File** (`~/.squad/rate-governor.json`):

```json
{
  "global_tokens": 4500,
  "last_refill": "2026-03-24T10:05:30Z",
  "refill_rate_tokens_per_sec": 1.39,
  "agent_quotas": {
    "gandalf": { "tokens": 600, "reserved": 625, "last_request": "2026-03-24T10:05:28Z" },
    "elrond": { "tokens": 625, "reserved": 625, "last_request": "2026-03-24T10:05:20Z" },
    "ralph-watch": { "tokens": 625, "reserved": 625, "priority": "P0", "last_request": "2026-03-24T10:05:31Z" }
  },
  "queue": [
    { "agent": "gandalf", "priority": "P2", "cost": 1, "enqueued": "2026-03-24T10:05:40Z" },
    { "agent": "elrond", "priority": "P1", "cost": 2, "enqueued": "2026-03-24T10:05:41Z" }
  ]
}
```

**Request Handler Pseudocode:**

```powershell
function Request-WithRateGovernor {
    param(
        [string]$Agent,
        [string]$Priority = "P2",  # Default: routine
        [int]$TokenCost = 1
    )
    
    # 1. Refill global pool
    $state = Get-RateGovernorState
    RefillTokens -State $state
    
    # 2. Check agent quota
    if ($state.agent_quotas[$Agent].tokens -ge $TokenCost) {
        # Tokens available; consume and allow request
        $state.agent_quotas[$Agent].tokens -= $TokenCost
        $state.global_tokens -= $TokenCost
        Save-RateGovernorState -State $state
        return "ALLOW"
    }
    
    # 3. Check global pool (in case agent wants to borrow)
    if ($state.global_tokens -ge $TokenCost) {
        $state.agent_quotas[$Agent].tokens -= $TokenCost
        $state.global_tokens -= $TokenCost
        Save-RateGovernorState -State $state
        return "ALLOW"
    }
    
    # 4. Enqueue with priority
    $state.queue += @{
        agent = $Agent
        priority = $Priority
        cost = $TokenCost
        enqueued = Get-Date -Format 'o'
    }
    Save-RateGovernorState -State $state
    
    # 5. Wait for quota release
    do {
        Start-Sleep -Milliseconds 500
        $state = Get-RateGovernorState
        RefillTokens -State $state
        if ($state.agent_quotas[$Agent].tokens -ge $TokenCost -or $state.global_tokens -ge $TokenCost) {
            # Quota released; process request
            $state.agent_quotas[$Agent].tokens -= $TokenCost
            Save-RateGovernorState -State $state
            return "ALLOW"
        }
    } while ($true)
}
```

**Ralph-Watch Integration:**

```powershell
# In ralph-watch.ps1 heartbeat cycle:
Request-WithRateGovernor -Agent "ralph-watch" -Priority "P0" -TokenCost 3 -Wait

# In agent startup:
Request-WithRateGovernor -Agent "gandalf" -Priority "P1" -TokenCost 2 -Wait
```

**Fairness Analysis:**

- **Per-agent reserved quota:** Each agent gets 625 tokens (5,000 / 8). If all agents run equally, each gets fair share.
- **Global pool lending:** If an agent runs less frequently, remaining tokens available to others.
- **Priority queuing:** ralph-watch P0 requests jump queue; normal agents P2; avoids monitoring starvation.

**Pros:**
- ✅ Extends rate-limiting-research.md (Token Bucket + Shared Pool + Priority Queuing)
- ✅ Ralph-watch gets priority (P0); won't starve behind routine tasks
- ✅ Fair distribution if agents have similar activity
- ✅ Handles quota reset gracefully (reinitialize pool)

**Cons:**
- ❌ Requires synchronization across agents (JSON file contention)
- ❌ Priority queuing adds complexity; care needed to prevent priority inversion
- ❌ Doesn't account for endpoint-specific rate limits (may need per-endpoint tracking)

**Recommendation:** ✅ **ADOPT Phase 1.** Directly addresses thundering herd and unfairness from rate-limiting-research.md.

---

## Dashboard Metrics & Observability

### Top 10 Metrics for Ralph-Watch Monitoring

Ralph-watch health depends on four layers; top 10 metrics:

#### Layer 1: Monitoring Process Health (Ralph-Watch Itself)

1. **Heartbeat Age (seconds)**
   - Definition: Time since last ralph-watch heartbeat update
   - Source: `watchdog-heartbeat.json` → `lastRun` timestamp
   - Normal: < 10 seconds
   - Alert: > 30 seconds (watchdog hung/crashed)
   - Dashboard Panel: Live timer

2. **Uptime (days)**
   - Definition: Seconds since ralph-watch started (PID age)
   - Source: `watchdog-heartbeat.json` → `pid`, compare with process start time
   - Normal: > 7 days (continuous run)
   - Alert: < 1 hour (frequent restarts indicate crashes)
   - Dashboard Panel: Uptime gauge

3. **Round Count & Interval**
   - Definition: Number of monitoring rounds completed; interval duration
   - Source: `watchdog-heartbeat.json` → `round`
   - Normal: 1 round per 24 hours
   - Alert: No increment in 48 hours (process hung)
   - Dashboard Panel: Counter + rate

#### Layer 2: Rate Limiting State

4. **Token Availability (%)**
   - Definition: Percentage of hourly GitHub API quota remaining
   - Source: `~/.squad/rate-governor.json` → `global_tokens` / 5000 × 100
   - Normal: > 20% (safe margin)
   - Alert (Yellow): 10–20% (approaching limit)
   - Alert (Red): < 10% (imminent exhaustion)
   - Dashboard Panel: Horizontal bar gauge

5. **Rate Limit Events (count/hour)**
   - Definition: Count of 429 rate-limit responses received
   - Source: Squad agent logs aggregated; parse for "429"
   - Normal: 0 per hour
   - Alert: > 3 per hour (queuing ineffective; need investigation)
   - Dashboard Panel: Time series sparkline

6. **Queue Depth (agents waiting)**
   - Definition: Number of agents blocked on rate limit quota
   - Source: `~/.squad/rate-governor.json` → `queue` length
   - Normal: 0 agents
   - Alert: > 2 agents waiting > 60 seconds
   - Dashboard Panel: Counter

#### Layer 3: Agent Health & Coordination

7. **Active Agents (count)**
   - Definition: Number of agent processes currently running
   - Source: Process registry (Mechanism 2) OR heartbeat files in `.squad/`
   - Normal: 8 agents (full squad)
   - Alert: < 6 agents (1+ agent missing)
   - Dashboard Panel: Counter + health indicators

8. **Authentication Failures (count/hour)**
   - Definition: Count of `gh auth token` failures (EACCES, timeout)
   - Source: Squad agent stderr; parse for "error: config file locked"
   - Normal: 0
   - Alert: > 1 per hour (race condition active)
   - Dashboard Panel: Red counter

9. **Lock Contention Events (count/hour)**
   - Definition: Instances where agent had to wait for distributed lock
   - Source: `~/.squad/auth.lock` contention tracking (add counters to lock file)
   - Normal: 0–2 (occasional, expected)
   - Alert: > 5 per hour (lock is bottleneck; need sharding)
   - Dashboard Panel: Trend line

#### Layer 4: Observability & Reliability

10. **Task Completion Rate (%)**
    - Definition: Percentage of planned monitoring rounds completed successfully
    - Source: `watchdog.log` → count "status=OK" / total rounds × 100
    - Normal: > 99%
    - Alert (Yellow): 95–99% (intermittent failures)
    - Alert (Red): < 95% (systemic problem)
    - Dashboard Panel: Percentage gauge + trend

---

### Dashboard Layout Recommendation

**Physical Dashboard** (text-based Ralph-Watch Status):

```
┌─────────────────────────────────────────────────────────┐
│ Ralph-Watch Monitoring Dashboard (Live)                │
├─────────────────────────────────────────────────────────┤
│ Uptime: 42 days                                         │
│ Heartbeat Age: 2 seconds ✅                             │
│ Current Round: 1011 (started 10:05:00 UTC)            │
├─────────────────────────────────────────────────────────┤
│ API Quota: ████████████░░░░░░░░ 42% (2,100 / 5,000)   │
│ Queue Depth: 0 agents ✅                                │
│ Rate Limit Events (1h): 0 ✅                            │
├─────────────────────────────────────────────────────────┤
│ Active Agents: 8/8 ✅                                    │
│ Auth Failures (1h): 0 ✅                                │
│ Lock Contention Events (1h): 1                         │
├─────────────────────────────────────────────────────────┤
│ Task Completion: 99.8% ✅                               │
│ Last 10 Rounds: OK OK OK OK OK OK OK OK OK OK          │
└─────────────────────────────────────────────────────────┘
```

**Data Sources:**
- Heartbeat: `~/.squad/watchdog-heartbeat.json`
- Rate governor: `~/.squad/rate-governor.json`
- Agent registry: `~/.squad/agent-registry.json` (new in Phase 2)
- Logs: `~/.squad/watchdog.log`, agent stderr captured in `~/.squad/agent-logs/`

---

## Alert Thresholds

### Alert Definition Table

| Alert | Trigger | Severity | Recovery | Owner |
|-------|---------|----------|----------|-------|
| **Heartbeat Missing (> 30s)** | `now - heartbeat_time > 30s` | 🔴 Critical | Restart ralph-watch; check process status | Aragorn (on-call) |
| **Ralph-Watch Down** | No heartbeat > 5 minutes | 🔴 Critical | Kill stuck process; restart watchdog script | Aragorn |
| **Quota Exhaustion (< 5%)** | `global_tokens / 5000 < 0.05` | 🟠 High | Implement quota borrowing from next hour; alert Teams | Elrond |
| **Queue Backlog (> 2 agents, > 60s)** | `queue_length > 2 AND queue[0].wait_time > 60` | 🟡 Medium | Investigate rate limit source; may need quota reallocation | Elrond |
| **Auth Race Detected (> 2/hour)** | `auth_failures_1h > 2` | 🟠 High | Implement lock retry backoff; verify lock file present | Gimli |
| **Agent Missing (< 6/8)** | `active_agents < 6` | 🟠 High | Check if agent crashed; review agent logs; restart if needed | Gandalf |
| **Task Completion < 95%** | `success_rounds / total_rounds < 0.95` | 🟡 Medium | Review watchdog.log for failure pattern; escalate if systemic | Elrond |
| **Lock Contention (> 5/hour)** | `lock_events_1h > 5` | 🟡 Medium | Monitor lock file size; may indicate lock starvation | Gimli |
| **Consecutive Failures (≥ 3)** | `failure_streak >= 3` | 🟠 High | Manual investigation required; check for quota, auth, or network issues | Aragorn |
| **Rate Limit Events (> 3/hour)** | `rate_limit_events_1h > 3` | 🟡 Medium | Investigate source agent; may indicate API abuse or endpoint saturation | Elrond |

### Alert Distribution

- **Aragorn (Livesite):** Ralph-watch down, heartbeat missing, consecutive failures
- **Elrond (Researcher):** Quota exhaustion, queue backlog, task completion tracking
- **Gimli (Tool Builder):** Auth race, lock contention (implementation issues)
- **Gandalf (Triage):** Agent missing, rate limit investigation

### Alert Channels

1. **Teams Webhook** (Primary): Post to `#squad-alerts` channel
2. **Log File** (Secondary): Append to `~/.squad/alert.log` for historical tracking
3. **Email** (Escalation): Critical-only (heartbeat > 5 min) to squad DL

---

## Implementation Roadmap

### Phase 1: Foundation (Weeks 1–2, Effort: 8 days)

**Goal:** Implement distributed lock (Mechanism 1) + Rate Governor (Token Bucket) + Dashboard metrics

**Deliverables:**
- [ ] `auth.lock` file with heartbeat validation
- [ ] `rate-governor.json` state file with refill logic
- [ ] Ralph-watch integration: lock acquisition before `gh auth`
- [ ] Agent integration: Rate Governor request before GitHub API calls
- [ ] Basic metrics file: `~/.squad/ralph-watch.metrics.json`
- [ ] Alert: Heartbeat missing (30s threshold)

**Implementation Steps:**
1. Add lock acquisition helper to `.squad/scripts/lock-utils.ps1` (new)
2. Add rate governor helper to `.squad/scripts/rate-governor.ps1` (new)
3. Update `.squad/skills/teams-watchdog/watchdog.ps1` to use lock + heartbeat
4. Update agent spawn template to include Rate Governor request before auth
5. Create `.squad/dashboards/ralph-watch-status.ps1` (text-based status printer)
6. Wire Teams alerts to Aragorn for heartbeat threshold

**Success Criteria:**
- ✅ No auth race failures in 48 hours (0 consecutive EACCES errors)
- ✅ All 8 agents complete monitoring round without quota blocking
- ✅ Heartbeat updates every < 10 seconds during round
- ✅ Dashboard displays without manual parsing

**Dependencies:** None (builds on existing squad infrastructure)

---

### Phase 2: Observability & Monitoring (Weeks 3–4, Effort: 6 days)

**Goal:** Implement Process Registry (Mechanism 2) + comprehensive metrics + dashboard UI

**Deliverables:**
- [ ] `agent-registry.json` with process registry and cleanup
- [ ] 10 metrics collection (all 10 from dashboard section)
- [ ] Ralph-watch dashboard (HTML or interactive text)
- [ ] Metric history (7-day rolling window for graphing)
- [ ] Alert escalation: Team routing (Aragorn, Elrond, Gimli, Gandalf)

**Implementation Steps:**
1. Implement process registry scanning in ralph-watch
2. Add metrics collection: heartbeat, queue depth, auth failures, agent count
3. Create 7-day metric history file (JSON lines)
4. Build dashboard aggregator script (reads metrics, formats display)
5. Wire each alert threshold to appropriate owner (see Alert Distribution)
6. Create alerting script that posts to Teams by alert owner

**Success Criteria:**
- ✅ Dashboard updates every round; no manual intervention needed
- ✅ Alert routing sends messages to correct owner
- ✅ Metric history supports 7-day query (no data loss)
- ✅ Process registry cleanup removes dead agents correctly

**Dependencies:** Phase 1 (foundation must be stable)

---

### Phase 3: Stale Recovery & Self-Healing (Weeks 5–6, Effort: 5 days)

**Goal:** Implement automatic stale lock cleanup, lease-based locks, quota borrowing

**Deliverables:**
- [ ] Stale lock detection + forced acquisition (Mechanism 1 enhancement)
- [ ] Automatic dead process cleanup (Mechanism 2 full integration)
- [ ] Quota borrowing from next hour (Rate Governor enhancement)
- [ ] Automatic alert remediation (e.g., restart ralph-watch on heartbeat timeout)

**Implementation Steps:**
1. Add stale lock detection to `lock-utils.ps1` (PID check + TTL)
2. Implement aggressive cleanup in ralph-watch (remove dead agents)
3. Add quota borrowing logic to rate-governor (borrow against next hour's quota)
4. Create remediation script for Aragorn (auto-restart ralph-watch)
5. Add telemetry for recovery events (log all self-healing actions)

**Success Criteria:**
- ✅ Stale lock detected and automatically acquired within 30 seconds
- ✅ Dead agent cleanup prevents orphaned locks
- ✅ Quota borrowing prevents false exhaustion alerts
- ✅ Ralph-watch auto-restarts without human intervention

**Dependencies:** Phase 1 & 2 stable; good metric visibility

---

### Phase 4: Advanced Features & Hardening (Weeks 7–8, Effort: 4 days)

**Goal:** TTL-based leases, per-endpoint quota tracking, advanced failure analysis

**Deliverables:**
- [ ] Lease-based distributed lock (Mechanism 3, optional)
- [ ] Per-endpoint rate limit tracking (if GitHub API changes)
- [ ] Failure root cause automation (parse logs; categorize failures)
- [ ] Incident replay & replay capability (for RCA)

**Implementation Steps:**
1. Implement TTL-based lease in `lock-utils.ps1`
2. Add per-endpoint quota tracking if needed (inspect GitHub API responses)
3. Build failure categorization script (auth, quota, network, timeout, etc.)
4. Create incident replay tool (extract events; simulate conditions)

**Success Criteria:**
- ✅ TTL-based lease reduces stale lock impact to < 1s
- ✅ Failure categorization achieves 90%+ accuracy
- ✅ Incident replay allows RCA for past events
- ✅ No manual intervention needed for common failure modes

**Dependencies:** Phase 1–3 complete; foundation rock-solid

---

### Timeline & Resource Allocation

```
Week 1-2: Phase 1 (8 days)
  ├─ Day 1: Lock utils + rate governor scaffolding
  ├─ Day 2: Ralph-watch integration
  ├─ Day 3: Agent spawn template update
  ├─ Day 4: Basic metrics + dashboard
  ├─ Days 5-8: Testing, bug fixes, deployment

Week 3-4: Phase 2 (6 days)
  ├─ Day 1: Process registry implementation
  ├─ Day 2-3: Metrics collection + history
  ├─ Day 4: Dashboard UI + styling
  ├─ Day 5-6: Alert routing + testing

Week 5-6: Phase 3 (5 days)
  ├─ Day 1-2: Stale lock recovery + cleanup
  ├─ Day 3: Quota borrowing
  ├─ Day 4-5: Remediation + testing

Week 7-8: Phase 4 (4 days)
  ├─ Day 1: TTL leases (optional)
  ├─ Day 2-3: Failure analysis tooling
  ├─ Day 4: Polish + documentation

Total: 23 days (3.3 weeks full-time; or 8 weeks at 3 days/week)
```

**Ownership by Agent:**
- **Gimli (Tool Builder):** Implement lock utils, rate governor, metrics collection
- **Aragorn (Livesite):** Test reliability, define alert thresholds, handle remediation
- **Elrond (Researcher):** Define metrics strategy, analyze failure patterns, RCA tooling
- **Bilbo (Documentarian):** Maintenance guide, runbook, troubleshooting tips

---

## Root Cause Analysis: Auth Race Condition

### The Problem: 37 Documented Failures

**Failure Pattern:**

When 8 agents spawn simultaneously (start of ralph-watch round):

```
[10:00:00] Round 1 started
  Agent 1 (gandalf): gh auth token ✅ 200ms
  Agent 2 (elrond): gh auth token ❌ error: config file locked (EACCES)
  Agent 3 (bilbo): gh auth token ❌ error: config file locked (EACCES)
  Agent 4 (gimli): gh auth token ❌ error: config file locked (EACCES)
  Agent 5 (ralph-watch heartbeat): blocked waiting for token
  Agents 6-8: cascade failure → entire round blocked
  
Result: Round hangs 30+ seconds; manual restart required
```

### Root Cause Chain

**Layer 1: OS-Level File Locking**

GitHub CLI (gh) stores authentication state in `~/.config/gh/hosts.yml`:

```yaml
# ~/.config/gh/hosts.yml (protected by OS file lock)
github.com:
    oauth_token: ghu_...
    oauth_token_expiration: 2026-12-31T00:00:00Z
```

When `gh auth token` reads this file:
1. Process A opens file for read
2. OS acquires shared lock (advisory lock, not exclusive)
3. Process B tries to read same file
4. If Process A is also writing (reauth), OS might escalate to exclusive lock
5. Process B gets EACCES (access denied) because exclusive lock held by A

**Layer 2: Concurrent Authentication**

The 8 agents all run:

```powershell
$token = & gh auth token --hostname github.com
```

All 8 processes hit `~/.config/gh/hosts.yml` simultaneously:
- Process A: `gh auth token` → OS opens file
- Processes B-H: Also open same file → contention
- If any process's session expired → automatic re-auth trigger
- Re-auth writes to file → exclusive lock → all others blocked

**Layer 3: Squad Agent Concurrency Model**

`.squad/templates/squad.agent.md` spawns all agents in parallel:

```powershell
# spawn all agents at once
foreach ($agent in $agents) {
    Start-Process { & invoke-copilot ... }  # No wait; all fire together
}

# All 8 agents now executing in parallel
```

No staggering; no coordination. All hit `gh auth token` at T+0ms.

**Layer 4: Independent Retry Logic**

If agent gets EACCES, it doesn't know why. Current behavior:

```powershell
# In agent startup (pseudocode)
$maxRetries = 3
for ($i = 0; $i -lt $maxRetries; $i++) {
    try {
        $token = & gh auth token --hostname github.com
        break
    } catch {
        Write-Host "Retry $i failed: $_"
        Start-Sleep -Milliseconds (100 * [math]::Pow(2, $i))  # Exponential backoff
    }
}
```

Problem: **All 8 agents backoff independently**:
- Agent 1 fails → sleeps 100ms → retries
- Agent 2 fails → sleeps 100ms → retries
- ...
- Agent 8 fails → sleeps 100ms → retries
- **All retry simultaneously at 100ms → thundering herd → cascade**

**Layer 5: Lack of Distributed Coordination**

Squad agents have **no mutual exclusion protocol** for GitHub auth:
- No lock file saying "Agent 1 is authenticating"
- No queue saying "wait your turn"
- No single authority deciding which agent goes first

Result: Chaos.

### Why 37 Failures Occurred

**Incident Reconstruction:**

Assuming 8-agent squad running 12 rounds/day:
- 96 agent spawns per day
- ~5–10% hit auth race during concurrent spawn: 5–10 failures/day
- Over 7 days: 35–70 failures (37 observed fits this profile)

**Failure Severity:**

- **Round failure:** Entire monitoring round blocked; no data collected
- **Cascade:** Once one agent hits EACCES, others follow (cascade effect)
- **Manual recovery:** Human restarts ralph-watch or spawns agents serially
- **Data loss:** Round output missing from logs; blind spot in monitoring

### Recovery Procedures

#### Immediate (Manual Intervention)

1. **Stop ralph-watch:**
   ```powershell
   Stop-Process -Name powershell -ProcessName *ralph-watch* -Force -ErrorAction SilentlyContinue
   ```

2. **Clear stale lock files:**
   ```powershell
   Remove-Item ~/.config/gh/hosts.yml.lock -Force -ErrorAction SilentlyContinue
   Remove-Item ~/.squad/auth.lock -Force -ErrorAction SilentlyContinue
   ```

3. **Restart ralph-watch:**
   ```powershell
   & ~/.squad/skills/teams-watchdog/watchdog.ps1
   ```

#### Preventive (Phase 1 Implementation)

1. **Implement distributed lock (Mechanism 1):**
   - Before `gh auth token`, acquire `~/.squad/auth.lock`
   - Lock holds PID + heartbeat timestamp
   - Only one agent proceeds; others wait (staggered 100–500ms)

2. **Add heartbeat validation:**
   - If lock older than 30 seconds + process missing → forcibly acquire
   - No deadlock; automatic stale recovery

3. **Add exponential backoff + jitter:**
   - Agents retry with different delays (not synchronized)
   - Prevents thundering herd on retry

#### Automatic (Phase 3 Implementation)

1. **Stale lock cleanup:**
   - Ralph-watch runs at start of each round
   - Scans `auth.lock`; verifies holder process running
   - If stale → removes lock automatically
   - Next agent acquires without hang

2. **Process registry:**
   - Central registry tracks all agent PIDs
   - Automatic cleanup removes dead agents
   - No orphaned locks

3. **Alert escalation:**
   - If auth failures > 2/hour → alert Gimli + Aragorn
   - Automatic investigation: compare with lock file age, process status
   - May auto-restart ralph-watch if correlation detected

---

## Evidence & References

### Source Materials

1. **rate-limiting-research.md** — Phase 1 recommendation (Token Bucket + Shared Pool + Predictive Circuit Breaker); appendix on 6 coordination patterns
2. **teams-outlook-integration-research.md** — Ralph-watch script pattern; polling-based coordination
3. **watchdog.ps1** (in .squad/skills/teams-watchdog/) — Existing lockfile, heartbeat, structured log pattern
4. **squad-skills catalog** — Teams-monitor, news-broadcasting, secrets-management plugins relevant to monitoring infrastructure
5. **.squad/agents/elrond/history.md** — Learnings from 6 prior research efforts; squad maturity indicators

### Validation Approach

**Phase 1 Metrics (Success Criteria):**
- [ ] 48-hour test run: zero auth race failures
- [ ] All 8 agents complete round without quota blocking
- [ ] Heartbeat updates < 10 seconds
- [ ] Dashboard renders correctly

**Phase 2 Validation:**
- [ ] 7-day metric history without data loss
- [ ] Alert routing sends to correct owner
- [ ] Process registry cleanup works correctly
- [ ] 99.5%+ task completion rate

**Phase 3 Validation:**
- [ ] Stale lock detection triggers correctly
- [ ] Auto-restart ralph-watch on heartbeat timeout
- [ ] Zero manual interventions in 14-day run

**Phase 4 Validation (if TTL leases adopted):**
- [ ] TTL-based lock reduces stale lock recovery time to < 1s
- [ ] No false positives (legitimate process not evicted)

---

## Recommendations & Next Steps

### Immediate (Week 1)

- [ ] Assign Gimli to implement lock utils + rate governor (Phase 1 foundation)
- [ ] Brief Aragorn on alert thresholds + escalation procedure
- [ ] Schedule Phase 1 completion target: 2026-03-29

### Short-Term (Weeks 2–4)

- [ ] Complete Phase 1 testing; roll to production
- [ ] Begin Phase 2 (metrics + dashboard)
- [ ] Monitor for auth race regression

### Medium-Term (Weeks 5–8)

- [ ] Complete Phase 3 (stale recovery + self-healing)
- [ ] Implement Phase 4 if needed (TTL leases, advanced features)
- [ ] Update squad onboarding docs with lock + rate governor patterns

### Success Metrics (End of Phase 3)

- **Zero manual restarts** in 30-day production run
- **99.8%+ task completion** (compared to current 95–98%)
- **Alert response time** < 5 minutes (vs. hours for manual discovery)
- **Uptime**: Ralph-watch continuous > 60 days (compared to current weekly restarts)

---

## Appendix: Glossary & Definitions

- **ralph-watch** — Monitoring script that coordinates 8+ squad agents; polls for task completion, maintains heartbeat, manages rate limits
- **Heartbeat** — Periodic timestamp update indicating process is alive; stale heartbeat (> 30s old) indicates crash/hang
- **Distributed lock** — Mechanism for one-at-a-time resource access (e.g., GitHub auth) across multiple processes
- **Stale lock** — Lock acquired by process that is no longer running; detected by PID check or TTL expiration
- **Rate governor** — Centralized quota manager that coordinates API requests across 8 agents; prevents thundering herd
- **Thundering herd** — All agents retry simultaneously, causing cascade failure; classic distributed systems problem
- **Token bucket** — Rate limiting algorithm; tokens replenish at steady rate; each request consumes 1 token
- **Priority queuing** — Queue discipline where high-priority requests processed before low-priority
- **Lease** — Time-limited token/permit; expires automatically if not renewed; used in distributed systems
- **Quota reset** — GitHub API resets remaining requests on rolling 1-hour window; creates spike in demand

---

## Document Metadata

- **Written by:** Elrond (Researcher)
- **Date:** 2026-03-24
- **Status:** Research (ready for implementation planning by Gimli, Aragorn, Gandalf)
- **Next Step:** Assign Phase 1 to Gimli; schedule 2-week delivery
- **Related Issues:** GitHub Issue #30 (Squad Monitoring & Ralph Watch Reliability)
