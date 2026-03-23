---
title: "Squad Skills Catalog — tamirdresher/squad-skills"
date: 2026-03-22
author: Jonathan
documentarian: bilbo
category: catalogs
tags:
  - catalog
  - skills
  - squad-infra
  - tooling
status: final
related_docs: []
---

# Squad Skills Catalog — tamirdresher/squad-skills

**Source URL:** https://github.com/tamirdresher/squad-skills/tree/main/plugins  
**Cataloged:** 2026-03-22  
**Total Plugins:** 21  
**Status:** Complete reference for all available Squad skills

---

## Quick Reference Table

| Name | Description | Relevance to Teams Watchdog Project |
|------|-------------|--------------------------------------|
| [Agency Optimal Config](#agency-optimal-config) | Guidance on optimal Agency Copilot configuration and MCP setup | ✅ Highly relevant (config foundation) |
| [Blog Writing](#blog-writing) | Patterns for high-quality technical blog posts | ⚪ General documentation |
| [Chrome DevTools MCP](#chrome-devtools-mcp) | Browser debugging and inspection for web automation | 🟡 Useful for web-based issues |
| [Cross-Machine Coordination](#cross-machine-coordination) | Secure task queuing and work coordination across machines | ✅ Relevant to distributed workflows |
| [Fact Checking](#fact-checking) | Systematic review methodology and verification patterns | ⚪ General quality assurance |
| [GitHub Auth Isolation](#github-auth-isolation) | Handle multi-account GitHub workflows without auth conflicts | 🟡 Useful for multi-repo teams |
| [GitHub Distributed Coordination](#github-distributed-coordination) | Use GitHub issues and GraphQL for agent-to-agent coordination | ⚪ Infrastructure pattern |
| [GitHub Multi-Account](#github-multi-account) | PowerShell proxy for seamless GitHub multi-account CLI access | 🟡 Useful for split workflows |
| [GitHub Project Board](#github-project-board) | Manage GitHub Projects v2 boards programmatically | 🟡 May improve task tracking |
| [Incident Response](#incident-response) | Squad incident response patterns and automation | ✅ Highly relevant (livesite work) |
| [Mail MCP](#mail-mcp) | Access Outlook email via MCP protocol for agents | 🟡 Useful for communication automation |
| [News Broadcasting](#news-broadcasting) | Distribute updates and summaries to multiple channels | 🟡 Communication channel |
| [Outlook Automation](#outlook-automation) | Full Outlook COM automation (email, calendar, contacts) | ✅ Highly relevant (Teams integration) |
| [Reflect](#reflect) | End-of-session reflection and learning capture | ⚪ Session artifact generation |
| [Restart Recovery](#restart-recovery) | Handle agent crashes and restart failures gracefully | ✅ Highly relevant (reliability) |
| [Secrets Management](#secrets-management) | Secure credential handling and environment variable management | ✅ Highly relevant (security) |
| [Session Recovery](#session-recovery) | Find and resume closed Copilot CLI sessions from history | ⚪ Workflow recovery |
| [Squad Email Headless](#squad-email-headless) | Headless email sending for agents (PowerShell-based) | 🟡 Communication automation |
| [Teams Monitor](#teams-monitor) | Bridge Teams messages into GitHub issues via WorkIQ | ✅ Highly relevant (communication bridge) |
| [Teams UI Automation](#teams-ui-automation) | Hybrid Playwright + keyboard + UIA automation for Teams desktop | ✅ Highly relevant (core project) |
| [Teams Distributed Coordination](#teams-distributed-coordination) | *See: Cross-Machine Coordination* | ✅ Same as cross-machine |

---

## Detailed Plugin Reference

### Agency Optimal Config
**URL:** https://github.com/tamirdresher/squad-skills/tree/main/plugins/agency-optimal-config

**Description:** Reference guide for optimal Agency Copilot configuration. Documents how MCPs (Model Context Protocol servers) load from three sources: defaults, mcp-config.json, and --mcp flags. Provides concrete recommendations for adding missing email/calendar MCPs to Ralph's command line.

**Key Capabilities:**
- Document all available built-in MCPs and their status (loaded, missing, optional)
- Compare current vs. recommended mcp-config.json
- Guide on when to use --mcp flag vs. persistent config file entries
- Specific command line recommendations for Ralph

**Dependencies & Prerequisites:**
- Agency CLI installed
- Active mcp-config.json file (~/.copilot/mcp-config.json)
- Understanding of MCP protocol

**Relevance to Teams Watchdog:** Foundational configuration guidance for the entire squad infrastructure.

---

### Blog Writing
**URL:** https://github.com/tamirdresher/squad-skills/tree/main/plugins/blog-writing

**Description:** Codifies technical blog post quality patterns including storytelling structure, code block standards, series conventions, and pre-publish checklists. Emphasizes conversational voice over marketing language and real experiments over theory.

**Key Capabilities:**
- Writing voice and anti-patterns for technical content
- Blog post structure template (Hook → Context → Experiment → Insight → Solution → Next)
- Code block formatting and linking standards
- Series navigation and front matter conventions
- Pre-publish validation checklist

**Dependencies & Prerequisites:**
- Markdown editor
- Git for publishing workflow
- Blog platform understanding

**Relevance to Teams Watchdog:** General documentation quality—useful for writing incident postmortems and technical reports.

---

### Chrome DevTools MCP
**URL:** https://github.com/tamirdresher/squad-skills/tree/main/plugins/chrome-devtools-mcp

**Description:** MCP server integration enabling AI agents to connect directly to Chrome DevTools for remote debugging and live browser inspection. Provides programmatic access to DevTools capabilities like DOM inspection, network analysis, and performance metrics.

**Key Capabilities:**
- Live browser session debugging without separate profiles
- DOM element inspection by reference
- Network request/response analysis
- Performance metrics capture
- Console output and error reading
- JavaScript execution in page context
- Hybrid manual + AI debugging workflows

**Dependencies & Prerequisites:**
- Chrome browser (version 144+ for auto-connect)
- Node.js (for npx)
- Remote Debugging enabled in Chrome
- Playwright MCP for web automation

**Relevance to Teams Watchdog:** Useful for debugging web-based issues when Teams automation encounters problems. Complements Playwright for root cause analysis.

---

### Cross-Machine Coordination
**URL:** https://github.com/tamirdresher/squad-skills/tree/main/plugins/cross-machine-coordination

**Description:** Pattern for securely coordinating work execution across multiple machines (laptop, DevBox, cloud VMs) without manual intervention. Enables agents running on different machines to share tasks, coordinate execution, and pass results using Git-based task queuing.

**Key Capabilities:**
- Git-based task queuing and result management
- YAML task file format with schema validation
- Security model with command whitelisting and resource limits
- GitHub Issues supplement for urgent/ad-hoc tasks
- Audit trail with immutable git commits
- Support for GPU workloads and specialized resources
- Automatic task discovery by target machine

**Dependencies & Prerequisites:**
- Git repository with .squad/cross-machine/ directory structure
- GitHub Issues API (optional, for urgent tasks)
- YAML parsing and validation
- Whitelist validation for executable commands

**Relevance to Teams Watchdog:** Patterns for distributed workflows across development machines and CI/CD environments. Applicable if watchdog work spans DevBox and cloud resources.

---

### Fact Checking
**URL:** https://github.com/tamirdresher/squad-skills/tree/main/plugins/fact-checking

**Description:** Systematic review methodology and output format for fact-checking claims and deliverables. Codifies counter-hypothesis testing, evidence verification, and confidence level classification.

**Key Capabilities:**
- Structured review methodology (evidence + counter-hypotheses)
- URL, API endpoint, and package reference verification
- Confidence level classification (✅ Verified, ⚠️ Unverified, ❌ Contradicted)
- Standardized review output table format
- Integration point for agent-to-agent review workflows

**Dependencies & Prerequisites:**
- Access to documentation and APIs
- External reference verification capability
- Markdown or structured report output

**Relevance to Teams Watchdog:** Quality assurance for incident documentation and recommendations. Useful for verifying solutions before deployment.

---

### GitHub Auth Isolation
**URL:** https://github.com/tamirdresher/squad-skills/tree/main/plugins/gh-auth-isolation

**Description:** Prevents multiple Ralph instances from fighting over global gh CLI authentication state when running across repos with different GitHub accounts (personal vs. enterprise). Solution uses per-process GH_TOKEN environment variable instead of global auth switching.

**Key Capabilities:**
- Detect required GitHub account from git remote URL
- Extract token per-process without global state mutation
- Fallback to global switch for single-Ralph scenarios
- Integration with ralph-watch.ps1
- Safe concurrent execution

**Dependencies & Prerequisites:**
- GitHub CLI (gh) installed
- Multiple GitHub accounts configured
- PowerShell environment
- Git command access

**Relevance to Teams Watchdog:** Useful if squad switches between personal and enterprise GitHub accounts in same session. May be needed for multi-tenant scenarios.

---

### GitHub Distributed Coordination
**URL:** https://github.com/tamirdresher/squad-skills/tree/main/plugins/github-distributed-coordination

**Description:** Pattern for using GitHub issues and GraphQL as an agent-to-agent coordination mechanism. Enables asynchronous task assignment and status tracking without requiring shared infrastructure.

**Key Capabilities:**
- Agent-to-agent task assignment via GitHub issues
- GraphQL queries for cross-agent visibility
- Status tracking through issue labels and comments
- Incident escalation workflows
- Durable coordination (survives agent restarts)

**Dependencies & Prerequisites:**
- GitHub repository
- GitHub GraphQL API access
- gh CLI or REST API client

**Relevance to Teams Watchdog:** Infrastructure pattern for coordinating incident response tasks across multiple agents.

---

### GitHub Multi-Account
**URL:** https://github.com/tamirdresher/squad-skills/tree/main/plugins/github-multi-account

**Description:** PowerShell proxy utility providing seamless GitHub multi-account CLI access. Abstracts gh auth switching through configurable proxy functions that automatically select the correct account based on repository context.

**Key Capabilities:**
- PowerShell proxy wrapper for gh commands
- Automatic account selection based on repo mapping
- Configuration file for account/repo associations
- Shell profile integration
- Setup script for initialization

**Dependencies & Prerequisites:**
- PowerShell 5.1+
- GitHub CLI (gh) installed
- Multiple GitHub accounts with tokens configured

**Relevance to Teams Watchdog:** Operational convenience for teams managing multiple GitHub accounts. Simplifies daily workflows.

---

### GitHub Project Board
**URL:** https://github.com/tamirdresher/squad-skills/tree/main/plugins/github-project-board

**Description:** Programmatic management of GitHub Projects v2 boards including adding issues, moving items between columns, setting field values, and archiving completed work.

**Key Capabilities:**
- Add issues to project boards
- Move items between custom columns
- Set custom field values (priority, size, etc.)
- Archive completed items
- Query project structure
- Bulk operations support

**Dependencies & Prerequisites:**
- GitHub repository with Projects enabled
- GraphQL API access
- Project ID and field configuration knowledge

**Relevance to Teams Watchdog:** May improve task visibility and tracking if transitioning from standalone issue lists to structured project boards.

---

### Incident Response
**URL:** https://github.com/tamirdresher/squad-skills/tree/main/plugins/incident-response

**Description:** Squad incident response patterns and automation. Likely includes workflows for incident triage, escalation, communication, and postmortem generation.

**Key Capabilities:**
- Incident classification and severity assignment
- Escalation routing based on service/component
- Automated incident notifications
- Postmortem template and timeline tracking
- Root cause analysis patterns

**Dependencies & Prerequisites:**
- Incident management system or GitHub issues
- Alert/notification infrastructure
- Team on-call schedule access

**Relevance to Teams Watchdog:** **Highly relevant.** Core infrastructure for livesite incident response and investigation workflows.

---

### Mail MCP
**URL:** https://github.com/tamirdresher/squad-skills/tree/main/plugins/mail-mcp

**Description:** MCP server providing access to Outlook email for agents. Allows reading, searching, and composing email messages through Model Context Protocol.

**Key Capabilities:**
- Read recent emails from inbox
- Search emails by sender, subject, date, attachments
- Compose and send email
- Reply and forward operations
- Access to email metadata and attachments
- Folder navigation

**Dependencies & Prerequisites:**
- Outlook or equivalent MCP server running
- SMTP/IMAP configuration
- Authentication credentials

**Relevance to Teams Watchdog:** Communication automation—useful for automated incident notifications or escalations via email.

---

### News Broadcasting
**URL:** https://github.com/tamirdresher/squad-skills/tree/main/plugins/news-broadcasting

**Description:** Distribute updates, summaries, and announcements to multiple communication channels (Teams, email, Slack, etc.) from agent workflows.

**Key Capabilities:**
- Multi-channel message distribution
- Template support for consistent formatting
- Channel routing based on message type
- Rate limiting and batching
- Delivery status tracking

**Dependencies & Prerequisites:**
- Multiple chat/communication channel webhooks or API access
- Message template library
- Configuration for channel mappings

**Relevance to Teams Watchdog:** Useful for broadcasting incident summaries or watchdog status updates to stakeholders across Teams, email, and other channels.

---

### Outlook Automation
**URL:** https://github.com/tamirdresher/squad-skills/tree/main/plugins/outlook-automation

**Description:** Complete Windows COM automation for Microsoft Outlook. Enables agents to create/manage calendar meetings, send/read/search email, manage contacts, and manipulate Outlook data directly.

**Key Capabilities:**
- Create, update, delete calendar appointments and meetings
- Set meeting attendees, location, and reminders
- Send emails with attachments
- Read inbox, search email by multiple criteria
- Reply, forward, and organize emails
- Access contacts and task items
- Configure meeting status and response tracking

**Dependencies & Prerequisites:**
- Windows OS
- Microsoft Outlook installed and configured
- PowerShell 5.1+
- Outlook COM interface available

**Relevance to Teams Watchdog:** **Highly relevant.** Enables automated calendar management (syncing with incidents), email notifications to stakeholders, and integration with Outlook workflows.

---

### Reflect
**URL:** https://github.com/tamirdresher/squad-skills/tree/main/plugins/reflect

**Description:** End-of-session reflection framework for agents to capture learnings, document approaches, and generate session artifacts. Produces reflection summaries that feed into knowledge base.

**Key Capabilities:**
- Session outcome classification (success, partial, blocked)
- Approach documentation for future reference
- Lessons learned extraction
- Session timeline and artifact capture
- Knowledge base ingestion
- Cross-session pattern recognition

**Dependencies & Prerequisites:**
- Session history and turn-by-turn context
- Knowledge base or wiki system
- Markdown or structured report output

**Relevance to Teams Watchdog:** General quality—useful for capturing insights from incident investigations that can improve future responses.

---

### Restart Recovery
**URL:** https://github.com/tamirdresher/squad-skills/tree/main/plugins/restart-recovery

**Description:** Graceful handling of agent crashes, restart failures, and partial execution recovery. Enables agents to detect failure states and resume cleanly without manual intervention.

**Key Capabilities:**
- Crash detection and classification
- Partial state recovery
- Safe restart mechanism
- Retry logic with exponential backoff
- Health check before resuming work
- Error reporting and diagnostics

**Dependencies & Prerequisites:**
- Process monitoring capability
- Checkpoint/state saving mechanism
- Health check infrastructure

**Relevance to Teams Watchdog:** **Highly relevant.** Reliability infrastructure for ensuring watchdog agents recover from failures automatically.

---

### Secrets Management
**URL:** https://github.com/tamirdresher/squad-skills/tree/main/plugins/secrets-management

**Description:** Secure credential handling and environment variable management for agents. Prevents secrets from leaking into logs, files, or shared output.

**Key Capabilities:**
- Secret redaction in logs and output
- Secure environment variable injection
- Credential rotation and expiration
- Secret masking in error messages
- Audit logging of secret access
- Integration with credential managers (Windows Credential Manager, etc.)

**Dependencies & Prerequisites:**
- Secret storage mechanism (environment variables, credential manager, vault)
- Key material for encryption
- Audit logging infrastructure

**Relevance to Teams Watchdog:** **Highly relevant.** Critical for secure handling of authentication tokens, API keys, and service credentials used in incident response workflows.

---

### Session Recovery
**URL:** https://github.com/tamirdresher/squad-skills/tree/main/plugins/session-recovery

**Description:** Find and resume recently closed Copilot CLI sessions from historical records. Uses session_store database to query past sessions by topic, working directory, time range, or checkpoint.

**Key Capabilities:**
- Search recent sessions by keyword (FTS5 full-text search)
- Filter sessions by working directory and branch
- Exclude Ralph monitoring sessions
- Query checkpoint progress
- View file modifications during session
- Resume session with `--resume SESSION_ID`

**Dependencies & Prerequisites:**
- Copilot CLI session_store database accessible
- SQL query capability
- Knowledge of session search syntax

**Relevance to Teams Watchdog:** Workflow recovery—useful for resuming interrupted incident investigations without losing context.

---

### Squad Email Headless
**URL:** https://github.com/tamirdresher/squad-skills/tree/main/plugins/squad-email-headless

**Description:** Headless email sending for squad agents via PowerShell. Enables automated email notifications from agent workflows without requiring Outlook UI.

**Key Capabilities:**
- Send email via PowerShell with authentication
- Support for attachments
- Configure SMTP server and credentials
- Template-based message composition
- Batch email sending
- Delivery status tracking

**Dependencies & Prerequisites:**
- PowerShell 5.1+
- SMTP server access
- Email credentials
- Network connectivity

**Relevance to Teams Watchdog:** Communication automation for sending incident alerts or notifications without interactive Outlook.

---

### Teams Monitor
**URL:** https://github.com/tamirdresher/squad-skills/tree/main/plugins/teams-monitor

**Description:** Monitor Microsoft Teams channels via WorkIQ for actionable messages directed at the squad, and bridge them into GitHub issues for task tracking.

**Key Capabilities:**
- Query Teams messages using WorkIQ (WorkIQ service)
- Filter for actionable items (requests, decisions, escalations)
- Bridge relevant messages to GitHub issues
- Deduplication against existing issues
- Scheduled monitoring (20-minute intervals)
- Topic-specific query templates

**Dependencies & Prerequisites:**
- WorkIQ MCP configured
- GitHub repository access
- Regular scheduler (Ralph or external task scheduler)
- Team context (channel names, keywords)

**Relevance to Teams Watchdog:** **Highly relevant.** Bridges Teams communication into GitHub for centralized task tracking. Ensures watchdog team doesn't miss directives or escalations.

---

### Teams UI Automation
**URL:** https://github.com/tamirdresher/squad-skills/tree/main/plugins/teams-ui-automation

**Description:** Hybrid three-layer automation for Microsoft Teams desktop app and web. Combines Playwright for web automation (primary), keyboard shortcuts for navigation (secondary), and UIA for window management (tertiary).

**Key Capabilities:**
- Install apps to Teams channels (primary via Playwright)
- Add tabs to channels (Wiki, Planner, custom apps)
- Configure connectors (when Graph API unavailable)
- Keyboard shortcut navigation (Ctrl+/, etc.)
- Window state detection and focus management
- Known selectors and fallback patterns for Teams UI
- Version detection for cache validation

**Dependencies & Prerequisites:**
- Windows OS (for keyboard shortcuts and UIA)
- Microsoft Teams desktop app OR web browser
- PowerShell 5.1+
- Playwright MCP tools available
- Teams signed-in with appropriate permissions

**Relevance to Teams Watchdog:** **Highly relevant—core project.** This is the foundational skill enabling all Teams watchdog automation (monitoring, installation, configuration).

---

## Teams Watchdog Project Alignment

### Issues 1-6 Skill Mapping

| Issue | Feature | Primary Skill(s) | Supporting Skills |
|-------|---------|------------------|-------------------|
| #1 | Watchdog monitoring Teams channels | Teams Monitor, Teams UI Automation | Incident Response, Session Recovery |
| #2 | Detect message types and patterns | Teams Monitor, Fact Checking | Session Recovery, News Broadcasting |
| #3 | Bridge Teams messages to GitHub | Teams Monitor, GitHub Distributed Coordination | Incident Response |
| #4 | Automated incident triage | Incident Response, Cross-Machine Coordination | Secrets Management, Restart Recovery |
| #5 | Calendar/meeting integration | Outlook Automation, Agency Optimal Config | Secrets Management |
| #6 | Watchdog self-healing & recovery | Restart Recovery, Session Recovery, Secrets Management | Cross-Machine Coordination |

### Recommended Implementation Order

1. **Foundational:** Agency Optimal Config, Secrets Management
2. **Communication Bridge:** Teams Monitor, Teams UI Automation, Outlook Automation
3. **Reliability:** Restart Recovery, Session Recovery
4. **Operations:** Incident Response, Cross-Machine Coordination
5. **Quality:** Fact Checking, Reflect (post-incident)

---

## Installation & Configuration

### Adding Skills to Your Squad

Each skill is typically a directory under `plugins/` containing:
- `SKILL.md` — Main skill documentation
- `README.md` — Quick reference (if present)
- `SKILL.yaml` or `manifest.json` — Configuration metadata
- Supporting scripts or templates

**To integrate a skill:**

1. Reference the skill's SKILL.md in your agent prompt or charter
2. Follow prerequisites listed in the skill documentation
3. Add required MCPs to ~/.copilot/mcp-config.json (if applicable)
4. Test integration in isolated session before production use

### GitHub Integration

Clone the repository and reference plugins locally:

```bash
git clone https://github.com/tamirdresher/squad-skills.git
cd squad-skills/plugins
```

Or reference skills directly from URLs in your documentation.

---

## Contributing & Maintenance

**Repository:** https://github.com/tamirdresher/squad-skills  
**Issues/Discussions:** Use GitHub Issues for bug reports, enhancement requests, or to discuss new skill ideas.

---

## Appendix: Skill Domains

**Skill domains help categorize by function:**

| Domain | Skills |
|--------|--------|
| **Communication Bridge** | Teams Monitor, Teams UI Automation, News Broadcasting, Mail MCP, Squad Email Headless, Outlook Automation |
| **Coordination & Distributed Work** | Cross-Machine Coordination, GitHub Distributed Coordination, GitHub Multi-Account, GitHub Auth Isolation, GitHub Project Board |
| **Quality & Verification** | Fact Checking, Blog Writing, Reflect |
| **Reliability & Recovery** | Restart Recovery, Session Recovery, Cross-Machine Coordination |
| **Configuration & Infrastructure** | Agency Optimal Config, Secrets Management, Chrome DevTools MCP |
| **Incident Management** | Incident Response |

---

## License & Attribution

All skills are maintained by @tamirdresher. See individual SKILL.md files for specific details and examples.

**Last Updated:** 2026-03-22  
**Compiled by:** Bilbo (Documentarian)
