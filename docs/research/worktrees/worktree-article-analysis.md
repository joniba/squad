---
title: "Tamir Dresher's Git Worktrees Article: Comprehensive Analysis"
date: 2025-10-20
author: Elrond
documentarian: bilbo
category: research
tags:
  - research-method
  - git
  - workflow
  - worktree
  - agent-coordination
status: final
related_docs:
  - research/worktrees/worktree-parallelism-research.md
---

# Tamir Dresher's Git Worktrees Article: Comprehensive Analysis
**Analysis Date:** 2025  
**Analyst:** Elrond (Researcher)  
**Article URL:** https://www.tamirdresher.com/blog/2025/10/20/scaling-your-ai-development-team-with-git-worktrees

---

## 1. Article Summary

### The Problem Tamir Identifies
Tamir faced a critical productivity bottleneck during the Microsoft Global Hackathon 2025: **context switching kills developer productivity, and it's even worse with AI agents.**

The traditional Git workflow forces developers (and AI agents) to:
- Stop current work
- Commit or stash changes
- Switch branches
- Wait for IDE/agent context reload
- Repeat this cycle every time switching between features

This is compounded when using AI code agents (Roo, GitHub Copilot, Cursor) because **each branch switch resets the AI's understanding of what it's working on**—forcing re-contextualization repeatedly.

### The Proposed Solution: Git Worktrees as Virtual AI Teams
Instead of one working directory with constant branch switching, use **Git worktrees** to create **multiple independent working directories, each checked out to a different branch**. This allows:

1. **Multiple worktrees share the same `.git` directory** → commits and branches available everywhere
2. **No disk duplication** → smaller footprint than full repository clones
3. **Git handles coordination** → seamless sync between worktrees
4. **VS Code native support** → built-in worktree management UI

**The key insight:** Each worktree gets its own VS Code window with its own AI agent, working in parallel on different features, while you act as a tech lead reviewing and coordinating across all windows.

### Specific Workflow for Using Git Worktrees with AI Agents
Tamir's procedure creates a **team of AI agents working simultaneously on your machine**:

1. Set up multiple worktrees via VS Code's Repositories view
2. Open each worktree in its own VS Code window
3. Launch a separate AI agent (Roo, Copilot, Cursor) in each window
4. Assign each agent a specific feature/task
5. Switch between windows (Alt+Tab) to supervise progress
6. Review, guide, commit, and push from each window independently
7. No context loss—each agent maintains understanding of its isolated feature

Real-world benefits Tamir observed:
- **No Context Switching:** Each AI agent maintains full context within its window
- **Parallel Development:** Multiple features developed simultaneously
- **Tool Flexibility:** Mix different AI tools per window (e.g., Roo for features, Copilot with VS 2022 debugger for debugging)
- **Easy Progress Review:** Alt+Tab through windows for quick standups
- **Clean Branch Management:** If one feature needs abandonment, others are unaffected

---

## 2. Technical Details

### Git Commands and Operations

**Creating a worktree programmatically:**
```bash
git worktree add <path> <branch>
```

**Listing all worktrees:**
```bash
git worktree list --porcelain
```

**Discovering the main working tree:**
The first `worktree` line from `git worktree list --porcelain` is the main working tree root.

### Tamir's VS Code Workflow (Not Command-Line)

Tamir does **NOT** show raw git commands in his article—instead, he describes the VS Code UI workflow:

1. **Enable Repositories View** → Source Control panel (Ctrl+Shift+G) → •••  menu → Views → Repositories
2. **Access Worktrees** → Right-click repository in Repositories view → Worktrees menu
3. **Create Worktree** → Click "Create Worktree" → VS Code wizard prompts:
   - Select or create a branch
   - Choose worktree location (typically `{repo-name}.worktrees/{branch-name}`)
   - Open in new window
4. **Repeat for each feature**
5. **Launch AI agents** in each window independently

### How Agents Are Assigned to Worktrees

**Implicit assignment via window focus:**
- Each worktree is opened in its own VS Code window
- The user launches an AI agent (Roo, Copilot, Cursor) in each window
- The agent works within that window's directory
- No explicit coordination needed—window isolation provides natural agent assignment

### Branches and Worktrees Relationship

- **1:1 mapping:** Each worktree is checked out to exactly one branch
- **Branch lifecycle:** You can select existing branches or create new branches during worktree creation
- **Independence:** Each worktree's branch is independent; switching to another worktree does NOT affect the original branch
- **Shared `.git`:** All branches remain in the shared `.git` directory—commits on any branch are instantly available in all worktrees

### State Handling Across Worktrees

**Tamir does NOT explicitly address `.squad/` or agent state handling** in his article. This is a critical gap.

The article assumes:
- Each worktree is a fresh VS Code workspace
- Each AI agent works independently
- No explicit state synchronization is described
- The user manually reviews and coordinates across windows

### Scripts or Automation Provided

**Tamir provides NONE.** His article is a procedural walkthrough of VS Code's UI, not automation guidance. There are no scripts, no batch setup commands, and no coordination logic for spawning multiple agents simultaneously.

### Performance and Scaling Considerations

**Mentioned in article:**
- **Disk footprint:** Smaller than full clones (shared `.git`)
- **IDE overhead:** Each worktree gets its own VS Code window (can use multiple processes)
- **Context maintenance:** AI agents maintain full context within isolated windows

**Not mentioned:**
- How many concurrent worktrees/agents are practical
- Memory/CPU implications of multiple VS Code instances
- Network bandwidth for pushing multiple branches
- Coordination overhead when synchronizing results

---

## 3. Procedure — Step by Step

Tamir's exact procedure for setting up parallel AI agent work:

### Phase 1: Enable VS Code Worktree UI
1. Open Source Control panel → Ctrl+Shift+G
2. Click three dots (•••) menu
3. Find "Repositories" in Views submenu
4. Enable it

### Phase 2: Access Worktree Management
1. In Repositories view, locate your repository
2. Click three dots next to repository name
3. Navigate to "Worktrees" in context menu

### Phase 3: Create First Worktree
1. Click "Create Worktree"
2. VS Code launches worktree creation wizard
3. **Select or create a branch** — choose existing or create new
4. **Choose worktree location** — typically `{repo-name}.worktrees/{feature-name}`
5. **Open in New Window** — click three dots next to worktree, select "Open in New Window"

### Phase 4: Repeat for Each Feature
- Create worktree → Feature branch → VS Code window
- Create worktree → Bug fix branch → VS Code window
- Create worktree → Experiment branch → VS Code window

### Phase 5: Deploy AI Agents
1. In each VS Code window, start your AI agent (Roo, Copilot, Cursor)
2. Give each agent a clear, specific task ("Implement authentication", "Add API endpoints", "Fix bug #123")
3. Switch between windows to monitor progress
4. Review and guide each agent like a tech lead
5. Commit and push when satisfied

### Phase 6: Coordination and Review
- Use Alt+Tab to switch between windows
- Each window shows a different feature's progress
- No branch switching, no IDE reloading needed
- If you need to debug (VS 2022 + Copilot debugger), you can use a different tool in that window

### No Flow Diagram Provided
Tamir does not provide a diagram or formal flow description. The procedure is described narratively with screenshots of the VS Code UI.

---

## 4. Comparison with Current Squad Behavior

### What squad.agent.md Says (Aspirational vs. Operational)

**The "Worktree Awareness" section of squad.agent.md (lines 562-600) is ASPIRATIONAL:**

It documents **what SHOULD happen** if worktrees were properly used by Squad agents:

| Aspect | squad.agent.md | Reality |
|--------|----------------|---------|
| **Team Root Resolution** | Coordinator should detect worktree via `git rev-parse --show-toplevel` and `git worktree list --porcelain` | NOT currently implemented |
| **Two Strategies** | `worktree-local` (branch-local state) vs `main-checkout` (shared state) | NOT implemented; agents don't choose or adapt |
| **State Isolation** | Each worktree's `.squad/` is branch-local; no state races | NOT implemented; agents use checkout, clobber state |
| **Merge Driver** | `.gitattributes` configured with `merge=union` for conflict-free merges | Configured in template, but irrelevant if agents don't use worktrees |
| **Scribe Commit Strategy** | Scribe commits `.squad/` changes to the worktree's branch | Squad doesn't spawn Scribe or manage `.squad/` commits explicitly |

**What squad.agent.md documents but doesn't enforce:**
```markdown
### Worktree Awareness

Squad and all spawned agents may be running inside a **git worktree** rather than the main checkout. 
All `.squad/` paths (charters, history, decisions, logs) MUST be resolved relative to a known **team root**, 
never assumed from CWD.
```

This is **documentation of a capability that was designed but never activated in practice.**

### How squad.agent.md Currently Handles Worktrees

The Coordinator is supposed to (on every session start):
1. Detect if it's running in a worktree
2. Resolve the team root via `git rev-parse --show-toplevel`
3. Check if `.squad/` exists at that root
4. If not, discover the main working tree via `git worktree list --porcelain`
5. Pass `TEAM_ROOT` to all spawned agents

**Status:** The logic is documented and described, but there's no evidence it's activated or enforced in the current implementation.

### What's Missing from the Squad Spawn Template

For worktrees to actually work with Squad agents, these pieces are needed:

1. **Agent Branch Creation Strategy**
   - Current: Agents do `git checkout -b squad/{issue}-{slug}` on existing worktree
   - Needed: Agents should do `git worktree add ../squad.{issue}.{slug} -b squad/{issue}-{slug}`
   - Problem: Creates sibling worktrees, coordination needed

2. **Worktree Creation Before Agent Spawn**
   - Current: Agents assume a worktree already exists
   - Needed: Coordinator should create worktree before spawning agent (if parallel execution is desired)
   - Command: `git worktree add {path} -b {branch}`

3. **Agent Awareness of Its Worktree**
   - Current: Agents don't know they're in a worktree; they assume single checkout
   - Needed: Agents should be told their worktree path and told NOT to switch branches

4. **Scribe Integration for State Management**
   - Current: Scribe writes to `.squad/` on the agent's current branch
   - Needed: Scribe should understand which worktree's `.squad/` to write to (especially if using `worktree-local` strategy)

5. **Cleanup/Pruning**
   - Current: No mechanism to clean up worktrees after agent finishes
   - Needed: Coordinator should run `git worktree remove {path}` after agent completes and PR is merged

6. **Coordination Between Parallel Agents**
   - Current: Each agent works independently; no shared coordination
   - Needed: Coordinator should track which agents are in which worktrees, prevent branch conflicts

---

## 5. The Gap — Why This Broke for Jonathan

### What Happened
Jonathan's team spawned 8 agents in parallel. Each agent:
1. Started in the same working tree
2. Did `git checkout -b squad/{issue}-{slug}` 
3. The second agent's checkout switched branches, which **clobbered the first agent's uncommitted work**
4. Each subsequent agent checkout switched branches again
5. Final state: workspace on a random branch with 4 stashes, uncommitted files, branch confusion

**But the work DID land on GitHub** because agents committed and pushed before being clobbered by the next agent's branch switch.

### Root Cause
**Single checkout with concurrent branch switches = state destruction.**

When Agent 1 is working on branch A and Agent 2 does `git checkout -b squad/issue-2 main`, the working directory switches to a new branch—**wiping uncommitted files on branch A**. Agent 1's in-progress work is lost locally (though it may have been pushed already).

### How Git Worktrees Would Have Prevented This

**If each agent had its own worktree:**
- Agent 1: `cd ../squad.issue-1/` (isolated worktree) → work happens here
- Agent 2: `cd ../squad.issue-2/` (different worktree) → work happens here
- Agent 3-8: Each has its own worktree directory
- **No checkout switches in the main worktree**
- Each agent works independently without stepping on other agents

### Specific Changes Needed to Squad Agent Spawn Template

#### Option A: Worktrees Created by Coordinator Before Spawn
**Recommended for truly parallel execution:**

**Coordinator (before spawning agents):**
```bash
# For each agent/issue:
git worktree add ./worktrees/squad.issue-X -b squad/issue-X main
```

**Agent Spawn Instructions:**
```
You are running in a dedicated git worktree at: {WORKTREE_PATH}
You do NOT need to create a branch or switch branches.
You ARE ALREADY on branch squad/issue-X.
Work only in {WORKTREE_PATH}—do not switch to other directories.
All agent operations (coding, commits, git push) must happen in {WORKTREE_PATH}.
When you finish: Coordinator will run: git worktree remove {WORKTREE_PATH}
```

**Changes to squad.agent.md spawn template:**
- Add a "Pre-Spawn Setup" phase where coordinator creates worktrees
- Pass `WORKTREE_PATH` to each agent
- Instruct agents NOT to do branch creation or switching
- Coordinator cleans up worktrees after agent completes

#### Option B: Agents Create and Own Their Worktree
**Simpler for uncoordinated/async work, but requires more logic:**

**Agent Spawn Instructions:**
```
You are responsible for creating and managing your own worktree.
When you start:
1. Run: git worktree add {TEAM_ROOT}/worktrees/squad.{ISSUE_ID} -b squad/{ISSUE_SLUG} main
2. cd into that directory
3. Do ALL work (git add, git commit, git push) in that directory
4. Before you finish: Run: git worktree remove {TEAM_ROOT}/worktrees/squad.{ISSUE_ID}
```

**Advantages:**
- Agents are self-contained
- No coordinator orchestration needed
- Works with async spawn

**Disadvantages:**
- Agents must understand worktree lifecycle
- Error handling (what if worktree creation fails?)
- Coordination is implicit (each agent picks its own ID)

### Recommended Approach for Squad

**Use Option A (Coordinator-managed worktrees) for:**
- Parallel agent spawning (most common use case)
- Deterministic setup and cleanup
- Easier debugging and coordination

**Implementation steps:**

1. **Add to squad.agent.md "Full Mode" spawn section:**
   ```
   Pre-spawn setup (Coordinator):
   - For each agent in the parallel batch:
     - Run: git worktree add {TEAM_ROOT}/worktrees/squad.{issue-id} -b squad/{issue-slug} main
     - Verify worktree was created and branch exists
     - Pass WORKTREE_PATH to agent in spawn prompt
   ```

2. **Update agent spawn prompt template:**
   ```
   TEAM_ROOT: {path}
   WORKTREE_PATH: {path}/worktrees/squad.{issue-id}
   CURRENT_BRANCH: squad/{issue-slug}
   AGENT_TASK: [description]
   
   Instructions:
   - You are working in: {WORKTREE_PATH}
   - The branch squad/{issue-slug} is already created and active
   - Do NOT create branches or switch directories
   - All work (git add, git commit, git push) stays in {WORKTREE_PATH}
   - When complete, push your branch: git push origin squad/{issue-slug}
   ```

3. **Add post-agent cleanup:**
   ```
   After agent completes (PR merged or abandoned):
   - Run: git worktree remove {TEAM_ROOT}/worktrees/squad.{issue-id}
   - Verify worktree was removed
   ```

4. **Update documentation:**
   - Mark the existing "Worktree Awareness" section as "ACTIVE" (not just documented but enforced)
   - Add troubleshooting: "If worktree creation fails, check: disk space, branch name conflicts, existing worktrees"
   - Document the cleanup procedure

### Why This Solves Jonathan's Problem

- **8 agents spawned in parallel:** Each gets its own worktree in `worktrees/squad.issue-1/` through `worktrees/squad.issue-8/`
- **No branch switching in main checkout:** Each agent works in isolation
- **No stashes or uncommitted file loss:** Each agent's work is isolated by filesystem directory
- **Clean state after:** Coordinator runs cleanup to remove worktrees
- **Repeatable:** Same coordinator code works for 8 agents, 80 agents, or 1 agent

---

## Conclusion

**Tamir's article is a manual, UI-driven guide** for a developer to set up worktrees in VS Code and launch AI agents in parallel.

**Squad's template is an aspirational design** that documents worktree support but doesn't enforce or activate it in practice.

**The gap:** Squad agents are still using the old single-checkout pattern (`git checkout -b`), which breaks when multiple agents run in parallel.

**The fix:** Implement worktree-based agent spawning in the Squad coordinator—have the coordinator create isolated worktrees before spawning agents, and instruct agents to work only within their assigned worktree. This is the pattern Tamir describes, adapted for automated agent spawning.

**Tamir's core insight remains valid for Squad:** Worktrees solve the parallel execution problem elegantly and naturally. The implementation just needs to move from manual VS Code UI to automated coordinator setup.
