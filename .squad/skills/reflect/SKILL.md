---
name: reflect
description: Learning capture system that extracts HIGH/MED/LOW confidence patterns from conversations to prevent repeating mistakes. Use after user corrections ("no", "wrong"), praise ("perfect", "exactly"), or when discovering edge cases.
license: MIT
metadata:
  version: 1.0.0
  adapted_from: Continuous improvement pattern library
---

# Reflect Skill

**Critical learning capture system** for agent continuous improvement. Prevents repeating mistakes and preserves successful patterns across sessions.

Analyze conversations and propose improvements to agent knowledge based on what worked, what didn't, and edge cases discovered. **Every correction is a learning opportunity.**

---

## Integration with Learning Systems

**Reflect complements existing learning mechanisms:**

1. **Agent history** — Permanent learnings from completed work
2. **Team decisions** — Shared guidelines all agents respect
3. **Skill improvements** — Recommendations to skill documentation
4. **Reflect skill** — Captures in-flight learnings that may graduate to permanent systems

**Workflow:**
- Use `reflect` during work to capture learnings
- At session end, review captured learnings
- Promote HIGH confidence patterns for team adoption
- Promote agent-specific patterns to agent history

---

## Triggers

### 🔴 HIGH Priority (Invoke Immediately)

| Trigger | Example | Why Critical |
|---------|---------|--------------|
| User correction | "no", "wrong", "not like that", "never do" | Captures mistakes to prevent repetition |
| Architectural insight | "you removed that without understanding why" | Documents design decisions |
| Immediate fixes | "debug", "root cause", "fix all" | Learns from errors in real-time |

### 🟡 MEDIUM Priority (Invoke After Multiple)

| Trigger | Example | Why Important |
|---------|---------|------------------|
| User praise | "perfect", "exactly", "great" | Reinforces successful patterns |
| Tool preferences | "use X instead of Y", "prefer" | Builds workflow preferences |
| Edge cases | "what if X happens?", "don't forget", "ensure" | Captures scenarios to handle |

### 🟢 LOW Priority (Invoke at Session End)

| Trigger | Example | Why Useful |
|---------|---------|------------|
| Repeated patterns | Frequent use of specific commands/tools | Identifies workflow preferences |
| Session end | After complex work | Consolidates all session learnings |

---

## Process

### Phase 1: Identify Learning Target

Determine what system should be updated:

1. **Agent-specific learning** — Agent's personal history and preferences
2. **Team-wide decision** — Guidelines for all agents
3. **Skill-specific improvement** — Document recommendation for skill owner

### Phase 2: Analyze Conversation

Scan for learning signals with confidence levels:

#### HIGH Confidence: Corrections

User actively steered or corrected output.

**Detection patterns:**
- Explicit rejection: "no", "not like that", "that's wrong"
- Strong directives: "never do", "always do", "don't ever"
- User provided alternative implementation

**Example:**
```text
User: "No, use the code search tool instead of raw API calls"
→ [HIGH] + Add constraint: "Prefer code search tools over direct API"
```

#### MEDIUM Confidence: Success Patterns

Output was accepted or praised.

**Detection patterns:**
- Explicit praise: "perfect", "great", "yes", "exactly"
- User built on output without modification
- Output was committed without changes

**Example:**
```text
User: "Perfect, that's exactly what I needed"
→ [MED] + Add preference: "Include usage examples in documentation"
```

#### MEDIUM Confidence: Edge Cases

Scenarios not anticipated.

**Detection patterns:**
- Questions not answered
- Workarounds user had to apply
- Error handling gaps discovered

**Example:**
```text
User: "What if the file doesn't exist?"
→ [MED] ~ Add edge case: "Handle missing file scenario"
```

#### LOW Confidence: Preferences

Accumulated patterns over time.

**Example:**
```text
User consistently uses --no-pager flag
→ [LOW] ~ Note for review: "User prefers --no-pager for commands"
```

### Phase 3: Propose Learnings

Present findings:

```text
┌─────────────────────────────────────────────────────────────┐
│ REFLECTION: {target}                                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│ [HIGH] + Add constraint: "{specific constraint}"            │
│   Source: "{quoted user correction}"                        │
│   Target: Agent knowledge base                              │
│                                                             │
│ [MED]  + Add preference: "{specific preference}"            │
│   Source: "{evidence from conversation}"                    │
│   Target: Shared guidelines                                 │
│                                                             │
│ [LOW]  ~ Note for review: "{observation}"                   │
│   Source: "{pattern observed}"                              │
│   Target: Session notes only                                │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│ Apply changes? [Y/n/edit]                                   │
└─────────────────────────────────────────────────────────────┘
```

**Confidence Threshold:**

| Threshold | Action |
|-----------|--------|
| ≥1 HIGH signal | Always propose (user explicitly corrected) |
| ≥2 MED signals | Propose (sufficient pattern) |
| ≥3 LOW signals | Propose (accumulated evidence) |
| 1-2 LOW only | Skip (insufficient evidence) |

### Phase 4: Persist Learnings

**⚠️ HARD RULE: Insights MUST NOT stay only in session memory.** Every accepted learning MUST be persisted to at least one of these durable locations. If it's not written to a file, it didn't happen.

**ALWAYS show changes before applying.**

After user approval:

1. **For Agent Charters (HIGH priority corrections):**
   - If the learning reveals a gap in how an agent works, **fix the charter directly**
   - Write the fix to `.squad/decisions/inbox/{agent}-charter-fix-{slug}.md` for Scribe to apply
   - This is the strongest form of persistence — it changes agent behavior permanently

2. **For Team Decisions / Directives:**
   - Create directive in `.squad/decisions/inbox/copilot-directive-{slug}.md`
   - Scribe merges into `decisions.md` — all agents respect it going forward
   - Use for cross-cutting rules ("always X", "never Y")

3. **For Agent History (agent-specific patterns):**
   - Append to agent history under `## Learnings` section
   - Format: Date, assignment context, key learning
   - This is the minimum — use when the pattern is agent-specific, not team-wide

4. **For Skills (reusable patterns):**
   - Create or update `.squad/skills/{skill-name}/SKILL.md`
   - Use when the pattern applies across multiple agents or sessions

**Persistence hierarchy** (prefer higher over lower):
1. Charter fix (strongest — changes behavior)
2. Team directive (shared — all agents see it)
3. Skill update (reusable — any agent can read it)
4. Agent history (weakest — only that agent sees it)

**Memory Format Example:**

```markdown
### {Date}: {Assignment Title}

**Context:** {Brief description}

**Learnings:**
1. [HIGH] {Constraint learned} — Source: "{user quote}"
2. [MED] {Preference discovered} — Pattern observed across {N} instances
3. [MED] {Edge case} — Scenario: {description}
```

---

## Usage Examples

### Example 1: User Correction

**Conversation:**
```
Agent: "I'll use grep to search the repository"
User: "No, use the code search tools first, grep is too slow"
Agent: "Got it. Let me reflect on this correction..."
```

**Reflection Output:**
```
[HIGH] + Add constraint: "Use code search tools before grep"
  Source: "No, use the code search tools first, grep is too slow"
  Target: Agent knowledge base
```

### Example 2: Success Pattern

**Conversation:**
```
Agent: [Creates detailed documentation with examples]
User: "Perfect! This is exactly the format I want for all reports"
Agent: "Let me capture this success pattern..."
```

**Reflection Output:**
```
[MED] + Add preference: "Include examples in documentation"
  Source: User praised detailed format
  Target: Team guidelines
```

---

## Integration Notes

**Reflect provides a learning workflow for any agent:**
- No framework dependency
- Works with any knowledge storage system (Git, databases, files)
- Captures HIGH/MED/LOW confidence learnings
- Routes team-wide learnings to appropriate decision-makers
- Agent-specific learnings captured in agent history

**Core principle:** Every user correction is a learning opportunity.

---

## When to Use

✅ **Use reflect when:**
- User says "no", "wrong", "not like that" (HIGH priority)
- User says "perfect", "exactly", "great" (MED priority)
- You discover edge cases or gaps
- Complex work session with multiple learnings
- At end of sprint/milestone to consolidate patterns

❌ **Don't use reflect when:**
- Simple one-off questions with no pattern
- User is just exploring ideas (no concrete decisions)
- Learning is already captured in agent history
- Trivial preferences unlikely to recur

---

## See Also

- [News Broadcasting](../news-broadcasting/) — Share team updates
- [Fact Checking](../fact-checking/) — Verify accuracy
- [Distributed Coordination](../cross-machine-coordination/) — Coordinate across agents

