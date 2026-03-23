---
title: "Teams-Monitor Adaptation Research"
author: "Elrond"
date: "2026-03-23"
status: "draft"
tags: ["teams-watchdog", "workiq", "copilot-cli", "adaptation"]
issue: "#65"
---

# Teams-Monitor Adaptation Research

## Executive Summary

Tamir's teams-monitor skill demonstrates a **bridging pattern** (Teams → GitHub issues) rather than a **summarization pattern** (Teams → markdown summary). Adapting it for our watchdog requires **inverting the goal**: instead of asking Copilot to find and create issues, we ask it to find, filter, and summarize Jonathan's outgoing messages.

**Key finding:** A single-agent `copilot -p` call can replace our 3-step filter→extract→format pipeline if we construct the right prompt. The probe step remains separate (WorkIQ limitations demand isolation). Estimated cost: **4–6 premium requests per run** vs. current 3 (net savings ~50%).

---

## Question 1: What Does Tamir's teams-monitor Actually Tell the Agent to Do?

### teams-monitor SKILL.md Analysis

Tamir's skill is a **Teams-to-GitHub bridge**, not a Teams summarizer. The workflow is:

1. **Query WorkIQ** for recent Teams messages using topic-specific queries
   - Examples: "What did Tamir Dresher say recently?", "Are there incident mentions?"
2. **Filter for actionable content** — direct requests, decisions, escalations, squad mentions
3. **Create GitHub issues** with title `[Teams Bridge] <summary>`, label `teams-bridge`
4. **Deduplicate** — check existing issues to avoid duplicates
5. **Log activity** — record what was found

### How This Differs from Our Watchdog

| Aspect | teams-monitor | Our Watchdog |
|--------|---------------|-------------|
| **Goal** | Bridge tasks from Teams into GitHub | Summarize Jonathan's own messages |
| **Direction** | External → Internal (Teams → Issues) | Internal + External → Markdown |
| **Output** | GitHub issues | Markdown + Teams post |
| **Agent role** | Investigator/Classifier | Summarizer/Analyst |
| **Queries** | "What did Tamir say?", "Any incidents?" | "What did Jonathan send?" (fixed) |

### Key Reusable Patterns

✅ **Directly applicable:**
- WorkIQ query structure (use `workiq-ask_work_iq` tool)
- Rate-limiting guidance (one WorkIQ query per cycle, indexing delay expected)
- File-based secret management (webhook URL at `~\.squad\teams-webhook.url`)

⚠️ **Needs inversion:**
- Instead of "find actionable items for the team", use "find messages Jonathan sent"
- Instead of "create issues", use "extract and summarize"
- Instead of deduplicating against GitHub, deduplicate against previous summaries

---

## Question 2: How Can We Adapt It Into a Single-Agent Call?

### Current Pipeline (3 Steps)

```
probe-messages.ps1
  ↓ (raw text)
filter-my-messages.ps1
  ↓ (filtered text)
extract-insights.ps1
  ↓ (insights text)
format-summary.ps1
  → (markdown summary)
```

**Cost:** 3 calls to `copilot -p` (probe, filter, extract—format is post-processing)

### Proposed Single-Agent Adaptation

**Single prompt that does filter + extract + format in parallel:**

```
Use the workiq-ask_work_iq tool to:
1. Get all my Teams messages from the last 24 hours
2. From those messages, identify and extract:
   - Decisions I've made or communicated
   - Action items I've committed to
   - Important context or findings I've shared
3. Format output as markdown:
   - ## Decisions (3–5 bullets)
   - ## Action Items (3–5 bullets)
   - ## Key Context (2–3 paragraphs)
Keep it concise. If no messages found, say 'No Teams activity in last 24h.'
```

**Advantage:** Single agent call handles filter + extract + format atomically. No intermediate files or round-trips.

**Risk points:**
- Token limit on very large message sets (100+ messages/day → token overflow)
- Output format consistency (markdown) — Copilot may not always structure correctly
- Loss of intermediate debugging (can't inspect filtered vs. extracted data if something goes wrong)

---

## Question 3: Exact `copilot -p` Command for Full Scan+Filter+Extract+Format

### Command Structure

```powershell
$prompt = @"
Use the workiq-ask_work_iq tool to answer this:

What Teams messages did I send in the last 24 hours?

From those messages, extract and summarize:
- Decisions I made or announced
- Action items I've committed to
- Important context I've shared with teams

Format your response as markdown with these sections:
## Decisions
(3–5 bullet points of decisions I communicated)

## Action Items  
(3–5 bullet points of commitments I made)

## Key Context
(2–3 sentences of important info I shared)

If there are no messages, respond: "No Teams activity in the last 24 hours."
"@

copilot -p $prompt --allow-tool='workiq'
```

### Rationale

1. **Single WorkIQ call** — Avoids multiple queries; WorkIQ filters by sender automatically
2. **Structured output** — Markdown sections match our delivery format
3. **No intermediate files** — Direct output to stdout
4. **Composable with watchdog.ps1** — Output piped directly to format-summary.ps1 or delivery

---

## Question 4: What Are the Risk Points?

### Risk 1: Token Limit on Large Message Sets
**Likelihood:** Low-Med | **Impact:** Medium  
**Details:** If Jonathan sends 100+ Teams messages/day, the full transcript may exceed Copilot token limits. WorkIQ may also return truncated results.  
**Mitigation:**
- Add `--max-messages=50` or similar to WorkIQ query (if supported)
- Fall back to time-windowed queries: "messages from the last 12 hours" if 24h fails
- Add explicit token budget guidance in prompt: "Keep the summary under 500 tokens"

### Risk 2: Output Format Reliability
**Likelihood:** Medium | **Impact:** Low-Med  
**Details:** Copilot may not always use exact markdown structure expected by format-summary.ps1 (e.g., uses `###` instead of `##`, or adds extra blank lines).  
**Mitigation:**
- Normalize output via regex in format-summary.ps1
- Add explicit format examples in prompt
- Test with diverse message sets before production

### Risk 3: Loss of Debugging Data
**Likelihood:** Low | **Impact:** Med-High (if needed for troubleshooting)  
**Details:** Current 3-step pipeline allows inspection of intermediate files. Single-agent approach loses that.  
**Mitigation:**
- Add `--debug` flag to POC script to save raw Copilot output to `.watchdog-debug.txt`
- Log the final markdown to watchdog.log for audit trail
- Add verbose output to stdout during development

### Risk 4: WorkIQ Indexing Delay
**Likelihood:** Medium | **Impact:** Low  
**Details:** Teams messages may have 5–60 minute indexing delay in WorkIQ (documented limitation).  
**Mitigation:**
- This is unavoidable; inherent to WorkIQ polling model
- Acceptable for daily summaries (already on 24h delay)
- Document in changelog/release notes

### Risk 5: Cost Not Meeting 6-Request Target
**Likelihood:** Low | **Impact:** Medium  
**Details:** If single-agent approach requires fallbacks or retries, may still cost ~9 requests.  
**Mitigation:**
- Current plan: Probe (1) + Format (1) + Delivery (1) = **3 requests**
- Old plan: Probe (1) + Filter (1) + Extract (1) + Format (1) + Delivery (1) = **5 requests**
- Single-agent plan: Probe (1) + Combined agent (1–2) + Delivery (1) = **3–4 requests**
- If fallback to 3-step: worst case 5 requests, still under budget

---

## Question 5: POC Script Ready to Test

See `.squad/skills/teams-watchdog/poc-single-agent-scan.ps1` (27 lines).

### How to Test

```powershell
# Test the POC directly (no watchdog loop)
& .\.squad\skills\teams-watchdog\poc-single-agent-scan.ps1 -Hours 24

# Capture output
& .\.squad\skills\teams-watchdog\poc-single-agent-scan.ps1 -Hours 24 > poc-output.txt

# Check format compliance
Get-Content poc-output.txt  # should have ## Decisions, ## Action Items, ## Key Context
```

### Success Criteria

✅ Script runs without errors  
✅ Output contains markdown sections (##, bullets)  
✅ Output runs within ~2 minutes  
✅ Token usage visible in output or logs  
✅ Can be piped to format-summary.ps1 without errors  

---

## Recommendation

**Proceed with single-agent adaptation.** The risk of output format variability is acceptable given the cost savings (50% reduction) and simplification (single call vs. 3-step pipeline). 

**Rollout plan:**
1. ✅ Test POC script with real Teams data (Jonathan validates manually)
2. → Replace filter/extract steps in run-pipeline.ps1 with combined-scan.ps1
3. → Keep probe and format steps (probe is isolation boundary; format is delivery-critical)
4. → Monitor first 5 runs for token errors or format anomalies
5. → If issues arise, add fallback logic (revert to 3-step pipeline)

---

## Appendix: Deliverables Checklist

- [x] Analyzed Tamir's teams-monitor SKILL.md
- [x] Read current watchdog architecture (probe/filter/extract/format)
- [x] Identified adaptation strategy (inversion: Teams→Issues becomes Teams→Summary)
- [x] Designed single-agent prompt
- [x] Identified risk points (token limit, format reliability, debugging loss, indexing delay, cost)
- [x] Created POC script at `.squad/skills/teams-watchdog/poc-single-agent-scan.ps1`
- [x] Documented this research in markdown with YAML frontmatter

**Next step:** Jonathan tests POC script with real Teams data to validate approach before Gimli integrates into run-pipeline.ps1.
