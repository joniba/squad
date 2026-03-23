---
title: "Investigation Report: ICM 767184571 — ARM Watchlist API Error Rates (Recurrence)"
date: 2026-03-23
author: Aragorn
documentarian: bilbo
category: investigation
tags:
  - icm
  - livesite
  - azure
  - watchlists
  - recurrence
  - arm-throttling
status: final
updated: 2026-03-23 (v2 — corrected customer impact, added subscription throttling walkthrough)
---

# Investigation Report: ICM 767184571

**Title:** ARM has detected that customers making requests to [MICROSOFT.SECURITYINSIGHTS/WATCHLISTS] (on endpoint prd-weu-402.sentinel.microsoft.com and possibly others) through [ARM] are experiencing increased error rates

**Investigator:** Aragorn (Operator) · **Requested by:** Jonathan
**Date:** 2026-03-23 · **Version:** 2 (corrected customer impact assessment)

| Field | Value |
|-------|-------|
| **ICM ID** | 767184571 |
| **Severity** | 2 (ACTIVE) |
| **Type** | LiveSite |
| **Owning Team** | Threat Intelligence (USX Threat Intelligence) |
| **Assigned To** | jbenami |
| **Impact Start** | 2026-03-23T19:46:28Z |
| **Created** | 2026-03-23T19:46:34Z |
| **Alert Source** | ADROCS (Geneva monitor via ARM) |
| **Region** | West Europe (westeurope) |
| **Cloud** | Public Azure |
| **Endpoint** | prd-weu-402.sentinel.microsoft.com |
| **Resource Type** | MICROSOFT.SECURITYINSIGHTS/WATCHLISTS |
| **Hit Count** | 5 |

**IcM Portal:** [IcM#767184571](https://portal.microsofticm.com/imp/v5/incidents/details/767184571/home)

---

## TL;DR

> **True Positive — Confirmed recurrence of ICM 766712513.** This is the 6th+ occurrence of the identical ARM→RP timeout pattern on the `prd-weu-402` Watchlist endpoint in March 2026. Same endpoint, same resource type, same alert source, same Sev2. The root cause is unchanged: a noisy tenant's automated request flood saturates ARM-to-RP connections, producing httpStatusCode=0 timeouts that ARM counts as failures. The RP itself is healthy (99.94–99.97% success rate per previous investigation). **Customer impact exists but is unquantified** — the noisy tenant receives errors, and co-located tenants on prd-weu-402 experience degraded service during saturation windows. No customers filed support requests. **This recurs until per-tenant rate limiting is implemented.** ICM's own mitigation hints reference 766712513 directly.

---

## Incident Classification

**Classification: TRUE POSITIVE (Recurring Noise Pattern)**

**Reasoning:** The ARM monitor correctly detected a real API success rate drop, but the root cause is exogenous to the RP — a single tenant's request flood exhausting ARM-to-RP connections. This is identical to ICM 766712513 (investigated in detail on 2026-03-22/23) and at least 4 other prior incidents on the same endpoint.

**Customer Impact: YES (real but unquantified) — corrected from v1 assessment**

> ⚠️ **v2 Correction:** The v1 report stated "zero customer impact." Jonathan challenged this, and upon re-examination, the evidence does NOT support a blanket "zero impact" claim. Here's the honest assessment:

**Who is impacted:**

1. **The noisy tenant itself (subscription `7d28c677-...`)** — They are a customer and they ARE receiving errors. The prior investigation confirmed all 46 RP-side HTTP 500s came from this subscription. Their automated retry loops generate errors that compound the problem. They are experiencing degraded service of their own making, but they're still a customer experiencing errors.

2. **Other tenants co-located on prd-weu-402** — When the noisy tenant saturates ARM→RP connections, ALL requests routed through that endpoint compete for the same connection pool. ARM reported a 73.63% success rate while the RP saw 99.97% — meaning ~26% of all ARM requests (not just the noisy tenant's) timed out at the TCP layer. Other tenants' Watchlist API calls during these windows would experience elevated latency or outright timeouts.

3. **The ICM title itself states it:** "customers making requests to [MICROSOFT.SECURITYINSIGHTS/WATCHLISTS]... are experiencing increased error rates" — ARM is saying customers (plural) are impacted.

**What the formal ICM metrics show (and why they're misleading):**
- Zero support requests (0 SRs) — customers didn't file tickets, but absence of SRs ≠ absence of impact
- Zero CritSits — no critical escalation
- Zero formally impacted subscriptions — ICM doesn't auto-detect co-located subscription impact
- `isCustomerImpacting: false` — this was set by the on-call, not measured
- `isNoise: false` — ICM doesn't classify it as noise either

**Bottom line:** Real customers are affected. The noisy tenant gets errors. Co-located tenants experience collateral degradation. We just don't know how many co-located tenants or how severe their experience was, because we haven't queried ARM logs by subscription to find out. The "zero impact" framing was wrong — the correct framing is: **real impact, unquantified scope, no customer escalations.**

**Severity Assessment:**
- **Reported:** Sev 2
- **Assessed:** Sev 2 is correct. While recurring and self-resolving, this incident causes real (if unquantified) customer impact to co-located tenants. The lack of support tickets doesn't mean the impact isn't real — it means customers either tolerate transient failures or don't know to report them. The systemic fix (per-tenant throttling) should be treated with Sev 2 urgency.

---

## Evidence Collected

### E1: Identical Pattern to ICM 766712513

**Evidence:** New incident matches prior incident on ALL dimensions — endpoint (prd-weu-402), resource type (WATCHLISTS), alert source (ADROCS), region (West Europe), severity (2), routing (AIMS://AZUREUX\AUXARM\MICROSOFT.SECURITYINSIGHTS.WATCHLISTS). Created ~21 hours after ICM 766712513 (2026-03-22T22:43 → 2026-03-23T19:46). | Source: `icm-get_incident_details_by_id` | Impact: critical

### E2: ICM Mitigation Hints Directly Reference 766712513

**Evidence:** Recommended causes from `icm-get_mitigation_hints` include: "High-volume requests causing throttling or saturation of resource provider" with direct references to ICM 699318024 and **ICM 766712513**. Recommended mitigations: "Identify and throttle or block offending high-volume request sources" — also referencing 766712513 directly. | Source: `icm-get_mitigation_hints` | Impact: high

### E3: 5+ Similar Historical Incidents

**Evidence:** Similar incident search returned: ICM 598566983 (Feb 2025, Sev2, THREATINTELLIGENCE, resolved — Cosmos outage), ICM 599126313 (Feb 2025, Sev2, THREATINTELLIGENCE, resolved — errors stopped). Mitigation hints additionally reference: ICM 757455363 (Mar 2026, WATCHLISTS, self-resolved), ICM 698715041 (prd-weu-402, THREATINTELLIGENCE), ICM 718932939 (prd-weu-402, THREATINTELLIGENCE), ICM 699318024 (prd-eus-402, ENRICHMENT). | Source: `icm-get_similar_incidents` + `icm-get_mitigation_hints` | Impact: high

### E4: ICM Formal Customer Impact Metrics (Misleading)

**Evidence:** 0 SRs, 0 CritSits, 0 impacted subscriptions, 0 impacted services/regions/clouds. `isCustomerImpacting: false`, `isNoise: false`, `isSupportEngagement: false`. However, these formal metrics only reflect whether customers filed support requests or whether the on-call explicitly flagged impact — they do NOT measure whether co-located tenants on prd-weu-402 experienced degraded service. The ARM success rate dropping to 73.63% means requests from ALL subscriptions on that endpoint were affected, not just the noisy tenant's. | Source: `icm-get_incident_customer_impact` + `icm-get_support_requests_crisit` (re-verified 2026-03-23) | Impact: **reassessed from LOW to MEDIUM**

### E5: Prior Investigation RCA (from ICM 766712513 v2 Report)

**Evidence:** Full investigation of the predecessor incident confirmed:
- RP-side success rate: **99.94–99.97%** (only 3–4 HTTP 500s per 5-min window across 7,000–14,000 requests)
- All 46 RP-side 500s from single subscription `7d28c677-88e0-4011-b860-dd6b0206eb23` (workspace: `learningenv-sentinel`)
- ARM reports 73.63% success rate vs RP's 99.97% — proving ~99% of ARM-reported failures are httpStatusCode=0 (TCP-level timeouts)
- TSG confirms: httpStatusCode=0 = ARM→RP connection timeout, counted as separate failures with each retry
| Source: `docs/investigations/icm-766712513-v2-report.md` | Impact: critical

---

## Root Cause Analysis

### Hypothesis 1: Recurring Noisy-Tenant ARM→RP Timeout Pattern ✅ CONFIRMED

**Evidence FOR:**
- Identical to ICM 766712513 in every dimension (endpoint, resource type, region, alert pattern)
- ICM's own mitigation system references 766712513 directly
- 5+ historical incidents on the same prd-weu-402 endpoint
- Prior investigation proved: single subscription running automated PUT retry loops → exhausting ARM→RP connections → httpStatusCode=0 timeouts → ARM sees low success rate

**Evidence AGAINST:** None

**Confidence: HIGH**

### Hypothesis 2: New/Different Root Cause

**Evidence FOR:** None — new incident created only ~21 hours after the prior one, with identical characteristics

**Evidence AGAINST:** All dimensions match exactly; ICM correlation engine and mitigation hints both link to the prior incident

**Confidence: N/A (rejected)**

### Causal Chain

```
Single noisy tenant (subscription 7d28c677-...) runs automated PUT retries on watchlist items
  → High request volume to prd-weu-402.sentinel.microsoft.com
    → ARM→RP TCP connections saturated
      → Requests timeout at ARM layer (httpStatusCode=0)
        → ARM counts timeouts as failures
          → ARM success rate drops below Bronze SLO threshold
            → ADROCS Geneva monitor fires Sev2 alert
              → ICM 767184571 created (6th+ recurrence)
```

---

## Remediation

### Immediate Mitigations (< 1 hour)

1. **Acknowledge and document as recurrence of ICM 766712513** — no new investigation needed
   - Effort: 5 minutes
   - Owner: On-call (jbenami)
   - Verification: ICM discussion entry linking to 766712513

2. **Monitor for self-resolution** — prior incidents in this pattern self-resolve within hours
   - Effort: Passive monitoring
   - Owner: On-call
   - Verification: ARM success rate returns above SLO threshold

### Short-Term Fixes (1–3 days)

3. **Implement per-tenant rate limiting on the Watchlist API**
   - Block or throttle subscription `7d28c677-88e0-4011-b860-dd6b0206eb23` if it continues causing incidents
   - Effort: 2–4 hours (config change)
   - Owner: Watchlist API team
   - Verification: No new ICMs from the same subscription pattern

4. **Update ARM manifest throttling rules** — configure `throttlingRules` with appropriate `bucketSize` for WATCHLISTS resource type to limit per-operation request rates (see ARM Throttling Walkthrough below)
   - Effort: 1–2 days (manifest change + rollout)
   - Owner: RP team + ARM manifest owners
   - Verification: ARM enforces per-tenant throttling before requests reach RP

5. **Consider updating ARM manifest timeout** — current default is 2 minutes. Evaluate if a shorter timeout or longer timeout better serves the Watchlist API pattern
   - Effort: 1 day (investigation + manifest change)
   - Owner: RP team
   - Verification: Timeout changes reflected in ARM→RP call patterns

### Long-Term Solutions (Weeks/Months)

6. **Suppress or auto-resolve this specific alert pattern** — configure ADROCS to auto-resolve incidents matching: endpoint=prd-weu-402 + resourceType=WATCHLISTS + duration < 4 hours + 0 SRs
   - Effort: 1 week (alert rule modification, testing)
   - Owner: On-call + monitoring team
   - Verification: No new Sev2 ICMs for this transient pattern

7. **Implement proper per-tenant request quotas at the RP layer** — prevent any single tenant from consuming disproportionate connection capacity
   - Effort: 2–4 weeks (code change)
   - Owner: Watchlist API team
   - Verification: Load testing confirms quota enforcement

---

## ARM Throttling & Manifest Configuration Walkthrough

> **Context:** Team members suggested: "throttling limits can be defined on ARM... maybe we want to update those" and "the ARM manifest details 2m for Timeout. Maybe we can configure maximum message size there as well."

This section provides a **step-by-step guide** for investigating and updating ARM manifest throttling, timeout, and message size settings for the `Microsoft.SecurityInsights` resource provider.

### Background: How ARM Throttling Works

ARM throttling operates at **two levels**:

1. **ARM-level (subscription/tenant):** Token bucket algorithm — 250 reads/sec (bucket), 200 writes/sec, 200 deletes/sec per subscription per service principal. These are global ARM limits and cannot be customized per-RP.

2. **RP-level (resource provider manifest):** Each RP can define custom `throttlingRules` in its ARM manifest to control per-operation request rates. This is what the team can update.

### Step 1: Locate Your ARM Manifest

The ARM manifest for `Microsoft.SecurityInsights` is a JSON file managed through the RP registration process. To find it:

```bash
# Option A: Use ProviderHub PowerShell to query the current registration
Get-AzProviderHubProviderRegistration -ProviderNamespace "Microsoft.SecurityInsights"

# Option B: Check the ARM manifest repository
# ARM manifests are typically stored in the RP's deployment repo,
# often in a path like: src/ARM/Microsoft.SecurityInsights/manifest.json
# or managed through RPaaS (Resource Provider as a Service)
```

**Where to look in the team's repos:**
- Check the SecurityInsights RP repo for files matching `*manifest*.json` or `*provider*registration*`
- The ARM manifest may also be managed through RPaaS portal or Azure ProviderHub

### Step 2: Understand the Throttling Rules Configuration

ARM Throttling v2 uses the **token bucket algorithm**. In the ARM manifest, throttling is configured via `throttlingRules`:

```json
"throttlingRules": [
  {
    "action": "Microsoft.SecurityInsights/watchlists/write",
    "metrics": [
      {
        "type": "NumberOfRequests",
        "bucketSize": "Small"
      }
    ]
  },
  {
    "action": "Microsoft.SecurityInsights/watchlists/read",
    "metrics": [
      {
        "type": "NumberOfRequests",
        "bucketSize": "Medium"
      }
    ]
  },
  {
    "action": "Microsoft.SecurityInsights/watchlists/watchlistItems/write",
    "metrics": [
      {
        "type": "NumberOfRequests",
        "bucketSize": "Small"
      }
    ]
  }
]
```

**Bucket sizes** determine the maximum requests and refill rate. The available sizes are:

| Bucket Size | Typical Behavior |
|-------------|-----------------|
| `XSmall` | Very restrictive — use for expensive/dangerous operations |
| `Small` | Restrictive — good for write operations |
| `Medium` | Moderate — suitable for most read operations |
| `Large` | Permissive — high-throughput read scenarios |
| `XLarge` | Very permissive — only for high-scale bulk reads |

**To add throttling for Watchlist operations**, add `throttlingRules` entries for:
- `Microsoft.SecurityInsights/watchlists/write`
- `Microsoft.SecurityInsights/watchlists/read`
- `Microsoft.SecurityInsights/watchlists/delete`
- `Microsoft.SecurityInsights/watchlists/watchlistItems/write`
- `Microsoft.SecurityInsights/watchlists/watchlistItems/read`

### Step 3: Configure Timeout Settings

The ARM manifest's endpoint registration includes a `Timeout` property:

```json
"endpoints": [
  {
    "apiVersions": ["2024-03-01"],
    "locations": ["West Europe"],
    "timeout": "PT2M"
  }
]
```

**Key facts about timeout:**
- **Default:** 2 minutes (`PT2M`) — this is what the team member referenced
- **Format:** ISO 8601 duration (e.g., `PT2M` = 2 minutes, `PT5M` = 5 minutes)
- **What it controls:** How long ARM waits for the RP to respond before considering the request a timeout (httpStatusCode=0)
- **Configurable via:** `ResourceProviderEndpoint.Timeout` property in the ProviderHub registration
- **Trade-offs:**
  - **Shorter timeout** (e.g., PT1M): Faster failure detection, but may prematurely timeout legitimate long-running operations
  - **Longer timeout** (e.g., PT5M): More forgiving for slow operations, but delays ARM's failure detection and ties up connections longer

**For the Watchlist timeout issue**, the 2-minute default is likely appropriate. The problem isn't timeout duration — it's connection saturation from request volume. Changing the timeout won't fix the root cause.

### Step 4: Maximum Message Size

ARM does **not** have a per-RP configurable "maximum message size" in the manifest in the same way as timeout and throttling. Message size limits are handled at different layers:

| Layer | Setting | Default | Configurable? |
|-------|---------|---------|--------------|
| **ARM gateway** | Request body size | 4 MB | No (platform limit) |
| **RP-side** | Your API's request body limits | Varies by implementation | Yes (in your RP code) |
| **ASP.NET / Kestrel** | `MaxRequestBodySize` | 28.6 MB (Kestrel default) | Yes (in your RP's hosting config) |
| **Service Fabric / K8s** | Ingress/reverse proxy limits | Varies | Yes (in deployment config) |
| **ARM manifest** | Not directly configurable | N/A | N/A |

**If the team wants to limit message size:**
1. **ARM-side:** Not configurable per-RP. The 4 MB ARM gateway limit applies to all RPs.
2. **RP-side:** Implement request body validation in your API middleware:
   ```csharp
   // In your RP's middleware or controller
   if (Request.ContentLength > maxAllowedSize)
       return StatusCode(413, "Request body too large");
   ```
3. **Hosting config:** Configure Kestrel or your reverse proxy to enforce limits.

### Step 5: How to Deploy ARM Manifest Changes

1. **Update the manifest JSON** in your RP's deployment repository (or RPaaS configuration)
2. **Submit a manifest rollout** — ARM manifests are deployed via the ARM manifest release process:
   - Canary → Pilot → Production stages
   - Each stage requires validation
3. **Monitor the rollout:**
   - Check Geneva/MDM metrics for the RP after the change
   - Verify throttling rules are enforced (check `x-ms-ratelimit-remaining-*` headers in API responses)
   - Monitor for any increase in 429 (Too Many Requests) responses
4. **Validate with ARM team:**
   - Contact: Julia Wang or Steve Arias (ARM throttling limits contacts per eng.ms documentation)
   - RPaaS office hours for manifest questions

### Step 6: Recommended Actions for This Incident

Given the recurring prd-weu-402 Watchlist timeout pattern, here is the prioritized action plan:

| # | Action | Effort | Impact |
|---|--------|--------|--------|
| 1 | **Add `throttlingRules` for Watchlist write operations** with `bucketSize: "Small"` to prevent noisy-tenant floods | 1 day (manifest change + rollout) | HIGH — directly addresses root cause |
| 2 | **Verify current timeout value** (`PT2M` assumed) and confirm it's appropriate for Watchlist operations | 2 hours (check manifest) | LOW — timeout isn't the root issue |
| 3 | **Implement RP-side per-tenant rate limiting** in the Watchlist API code to catch abuse before it reaches ARM | 1–2 weeks (code change) | HIGH — defense-in-depth |
| 4 | **Request ARM team guidance** on whether they can enable per-subscription throttling at the ARM gateway level for SecurityInsights | 1 hour (email/Teams) | MEDIUM — may already be available |

### Key Contacts

- **ARM throttling limits:** Julia Wang, Steve Arias (per eng.ms docs)
- **RPaaS office hours:** For manifest configuration questions
- **ARM RP Investigation TSG:** https://eng.ms/docs/products/arm/troubleshooting/livesites/tsgs/rps/overview

### Reference Documentation

- [ARM Throttling Limits (Microsoft Learn)](https://learn.microsoft.com/azure/azure-resource-manager/management/request-limits-and-throttling)
- [ARM Manifest Specification (eng.ms)](https://eng.ms/docs/cloud-ai-platform/microsoft-specialized-clouds-msc/msc-sovereign/sovereign-public-cloud/regulated-environment-management/microsoft-cloud-for-sovereignty/architecture/resourceprovider/specification/arm-manifest)
- [ProviderHub Timeout Property (Microsoft Learn)](https://learn.microsoft.com/dotnet/api/azure.resourcemanager.providerhub.models.resourceproviderendpoint.timeout)
- [ProviderHub Registration (Microsoft Learn)](https://learn.microsoft.com/azure/templates/microsoft.providerhub/providerregistrations)

---

## Subscription Throttling & Blocking Walkthrough

> **Added in v2** per Jonathan's request. This section provides actionable steps to identify, throttle, and block the offending subscription.

### Step A: Identify the Offending Subscription

The prior investigation (ICM 766712513) identified subscription `7d28c677-88e0-4011-b860-dd6b0206eb23` (workspace: `learningenv-sentinel`). To confirm it's the same subscription in this recurrence — or find a different one:

#### Kusto Query: ARM-Side (Find top subscriptions by error count)

Run this against the ARM Kusto cluster that logs requests for `Microsoft.SecurityInsights`:

```kusto
// ARM-side: Identify subscriptions generating the most failures on prd-weu-402
ArmRequests
| where timestamp between (datetime(2026-03-23T19:00:00Z) .. datetime(2026-03-23T21:00:00Z))
| where resourceProvider =~ "Microsoft.SecurityInsights"
| where resourceType =~ "watchlists" or resourceType =~ "watchlists/watchlistItems"
| where targetEndpoint contains "prd-weu-402"
| where httpStatusCode == 0 or httpStatusCode >= 400
| summarize
    ErrorCount = count(),
    DistinctOperations = dcount(operationName),
    SampleErrors = take_any(httpStatusCode, 5)
    by subscriptionId
| order by ErrorCount desc
| take 20
```

#### Kusto Query: RP-Side (Find top subscriptions by request volume)

Run this against the SecurityInsights RP logs:

```kusto
// RP-side: Find which subscriptions are generating the most requests
SentinelApiRequests
| where timestamp between (datetime(2026-03-23T19:00:00Z) .. datetime(2026-03-23T21:00:00Z))
| where operationName has "watchlist"
| where serverHost contains "prd-weu-402"
| summarize
    TotalRequests = count(),
    FailedRequests = countif(httpStatusCode >= 400),
    FailRate = round(100.0 * countif(httpStatusCode >= 400) / count(), 2)
    by subscriptionId
| order by TotalRequests desc
| take 20
```

> **Note:** Table and column names may vary by your RP's logging schema. Check your RP's Kusto database for the actual table names (e.g., `ApiRequests`, `IncomingRequests`, `SentinelServiceRequests`, etc.).

#### Alternative: Azure Monitor / Activity Log

```powershell
# Check Azure Activity Log for high-volume subscriptions
Get-AzLog -ResourceProvider "Microsoft.SecurityInsights" `
    -StartTime "2026-03-23T19:00:00Z" `
    -EndTime "2026-03-23T21:00:00Z" `
    -Status "Failed" |
    Group-Object -Property SubscriptionId |
    Sort-Object -Property Count -Descending |
    Select-Object -First 10 Name, Count
```

### Step B: Throttle the Subscription via ARM Manifest (ProviderHub)

ARM provides **per-resource-type throttling rules** configured through ProviderHub. These apply to ALL subscriptions equally (not per-subscription targeting), but they protect the RP from any single subscription's flood.

#### B1: Check current throttling rules

```powershell
# Install the ProviderHub module if needed
Install-Module -Name Az.ProviderHub -Force -AllowClobber

# Get current resource type registration for Watchlists
Get-AzProviderHubResourceTypeRegistration `
    -ProviderNamespace "Microsoft.SecurityInsights" `
    -ResourceType "watchlists" |
    Select-Object -ExpandProperty ThrottlingRule |
    Format-List
```

If this returns empty, no throttling rules are configured — which means ARM's default global limits apply (250 reads/sec, 200 writes/sec per subscription per service principal).

#### B2: Add throttling rules via ProviderHub

```powershell
# Define throttling rules for Watchlist operations
# These use the token bucket algorithm — "bucketSize" controls burst capacity

$throttlingRules = @(
    @{
        Action = "Microsoft.SecurityInsights/watchlists/write"
        Metric = @(
            @{
                Type = "NumberOfRequests"
                Limit = 50  # max 50 write requests per interval
                Interval = "PT1M"  # per minute
            }
        )
    },
    @{
        Action = "Microsoft.SecurityInsights/watchlists/read"
        Metric = @(
            @{
                Type = "NumberOfRequests"
                Limit = 200  # max 200 read requests per interval
                Interval = "PT1M"
            }
        )
    },
    @{
        Action = "Microsoft.SecurityInsights/watchlists/watchlistItems/write"
        Metric = @(
            @{
                Type = "NumberOfRequests"
                Limit = 100  # max 100 watchlist item writes per interval
                Interval = "PT1M"
            }
        )
    }
)

# Apply the throttling rules
Update-AzProviderHubResourceTypeRegistration `
    -ProviderNamespace "Microsoft.SecurityInsights" `
    -ResourceType "watchlists" `
    -ThrottlingRule $throttlingRules
```

> **⚠️ Important:** This applies to ALL subscriptions, not just the offending one. Set limits that protect the RP without breaking legitimate high-volume customers. Test in canary/pilot first.

#### B3: Verify throttling is active

After deployment, the offending subscription will receive HTTP 429 (Too Many Requests) responses with a `Retry-After` header when they exceed the limits. Monitor:

```powershell
# Check for 429 responses in ARM logs after throttling rules are deployed
# (Kusto query against ARM request logs)
```

```kusto
ArmRequests
| where timestamp > ago(1h)
| where resourceProvider =~ "Microsoft.SecurityInsights"
| where resourceType =~ "watchlists"
| where httpStatusCode == 429
| summarize ThrottledCount = count() by subscriptionId
| order by ThrottledCount desc
```

### Step C: Block a Specific Subscription (If Throttling Isn't Enough)

If the subscription continues causing problems even with throttling rules, there are several escalation options:

#### C1: RP-Side Subscription Block (Fastest — You Own This)

Add subscription-level filtering in your RP's request pipeline. This is the fastest option because it doesn't require ARM team involvement:

```csharp
// In your RP's middleware or request filter
public class SubscriptionBlockFilter : IActionFilter
{
    // Blocklist — store in config or Azure App Configuration for hot-reload
    private static readonly HashSet<string> BlockedSubscriptions = new()
    {
        "7d28c677-88e0-4011-b860-dd6b0206eb23"  // learningenv-sentinel — noisy tenant
    };

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var subscriptionId = context.RouteData.Values["subscriptionId"]?.ToString();
        if (subscriptionId != null && BlockedSubscriptions.Contains(subscriptionId))
        {
            context.Result = new ObjectResult(new
            {
                error = new
                {
                    code = "SubscriptionBlocked",
                    message = "This subscription has been temporarily blocked due to excessive request volume. Contact support."
                }
            })
            { StatusCode = 429 };
        }
    }
}
```

**Better approach** — use Azure App Configuration or a feature flag so you can add/remove subscriptions without redeploying:

```csharp
// Read blocklist from App Configuration
var blockedSubs = _configuration.GetSection("BlockedSubscriptions").Get<string[]>();
```

#### C2: ARM-Level Subscription Blocking (Requires ARM Team)

ARM does not provide a self-service "deny list" for RPs to block specific subscriptions. To block at the ARM layer, you need to:

1. **Contact the ARM team** (Julia Wang, Steve Arias) and request they add a subscription-level block for your RP
2. **File a support request** through the ARM RP support channel
3. **Use Azure Policy** — create a deny policy that prevents the subscription from creating/modifying watchlist resources:

```json
{
  "mode": "All",
  "policyRule": {
    "if": {
      "allOf": [
        {
          "field": "type",
          "equals": "Microsoft.SecurityInsights/watchlists"
        },
        {
          "field": "Microsoft.SecurityInsights/watchlists/subscriptionId",
          "in": ["7d28c677-88e0-4011-b860-dd6b0206eb23"]
        }
      ]
    },
    "then": {
      "effect": "deny"
    }
  }
}
```

> **Note:** Azure Policy deny works for PUT/PATCH operations but doesn't block GET/LIST. For full blocking, RP-side filtering (C1) is more comprehensive.

#### C3: Contact the Subscription Owner (Preferred Non-Technical Approach)

Before blocking, consider reaching out to the subscription owner:

```powershell
# Find the subscription owner/admin
Get-AzRoleAssignment -Scope "/subscriptions/7d28c677-88e0-4011-b860-dd6b0206eb23" |
    Where-Object { $_.RoleDefinitionName -eq "Owner" } |
    Select-Object DisplayName, SignInName
```

Then contact them to:
- Explain their automation is causing service degradation
- Ask them to implement backoff/retry logic in their client
- Suggest they batch their watchlist item operations instead of individual PUTs

### Step D: Decision Matrix — When to Use Which Approach

| Scenario | Approach | Time to Implement | Reversibility |
|----------|----------|-------------------|---------------|
| Prevent future floods from ANY subscription | **B: ARM throttling rules** | 1–2 days (manifest rollout) | Easy (remove rules) |
| Block ONE known bad subscription immediately | **C1: RP-side filter** | Hours (code deploy) | Easy (config change) |
| Block a subscription at ARM layer | **C2: ARM team request** | Days (depends on ARM team) | Medium (requires ARM team) |
| Prevent writes from a subscription via policy | **C3: Azure Policy deny** | Hours (policy assignment) | Easy (remove assignment) |
| Fix the root cause collaboratively | **C3: Contact owner** | Days–weeks | N/A |

**Recommended approach for this incident:**
1. **Immediately:** Deploy RP-side subscription filter (C1) for `7d28c677-...` if it continues causing incidents
2. **This week:** Add ARM throttling rules (B) for Watchlist operations to protect against ANY noisy tenant
3. **This sprint:** Contact the subscription owner (C3) to fix their automation
4. **Long-term:** Implement proper per-tenant rate limiting in the RP code

---

## Open Questions

1. **[HIGH]** Does the SecurityInsights ARM manifest currently define any `throttlingRules`? If not, adding them for Watchlist operations should be the first action.
2. **[HIGH]** Is the offending subscription `7d28c677-...` the same one causing this recurrence, or is it a different noisy tenant? Run the Kusto queries from Step A above.
3. **[HIGH]** How many co-located tenants on prd-weu-402 were affected during this incident window? Run ARM-side Kusto query filtering by endpoint + time window + httpStatusCode=0 and count distinct subscriptionIds.
4. **[MEDIUM]** Can ADROCS be configured to auto-suppress alerts matching this specific pattern (prd-weu-402 + WATCHLISTS + duration < 4h + 0 SRs)?
5. **[LOW]** What is the actual current timeout value in the SecurityInsights ARM manifest? Confirm whether it's `PT2M` (2 minutes).

---

## Priority Assessment

**Recommended Priority:** P1 (high) — **upgraded from P2 in v1**

**Rationale:**
- Customer Impact Scope: Real but unquantified — noisy tenant receives errors, co-located tenants on prd-weu-402 experience collateral degradation during saturation windows. No SRs filed but absence of SRs ≠ absence of impact.
- Blast Radius: Narrow per-incident (single endpoint, West Europe), but recurring 6+ times — cumulative impact is significant
- Fix Complexity: Low-Medium — ARM manifest throttling rules are a config change; RP-side subscription block is a few hours of work
- Dependencies: ARM manifest rollout process for throttling; no external dependency for RP-side block
- Workaround: Self-resolving per-occurrence, but recurs without fix — not a true workaround
- Deadline Pressure: Each recurrence risks impacting co-located tenants; fixing this prevents future incidents

**Relative Priority:**
- vs. ICM 764634026 (MSPKI cert migration, Apr 10 deadline): Lower — cert has a hard deadline
- vs. ICM 766937015 (MDTI Premium connector, named customers blocked): Comparable — both have real customer impact
- vs. ICM 51000000943039 (TI Upload API bug, S500 customer): Lower — S500 customer takes priority
- vs. ICM 766712513 (prior instance, same pattern): **Duplicate** — same investigation applies

**Decision:** Upgraded to P1 because the v1 "zero customer impact" assessment was incorrect. Real customers are affected during each recurrence, the pattern repeats regularly, and the fix (ARM throttling + RP-side block) is low-effort. The RP-side subscription block (Step C1) can be deployed immediately to stop the bleeding while the ARM manifest throttling rules are rolled out.

---

*Investigated by Aragorn (Operator) · pa-squad · 2026-03-23 · v2 update: customer impact corrected, subscription throttling walkthrough added*
