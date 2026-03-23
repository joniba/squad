# Squad Starter Template

A reusable template for bootstrapping a multi-agent squad — a team of specialized AI agents that collaborate through structured roles, routing rules, and ceremonies.

## What's Included

```
templates/squad-starter/
├── README.md                  ← You are here
├── team.md                    ← Squad roster with generic roles
├── routing.md                 ← Rules for routing work to the right agent
├── ceremonies.md              ← Team meetings (design review, retro)
└── charter-templates/         ← Role charters (copy per agent)
    ├── lead.md
    ├── developer.md
    ├── tester.md
    ├── documentarian.md
    └── reviewer.md
```

## Quick Start

### 1. Copy the template

```bash
cp -r templates/squad-starter/ .squad/
```

Or selectively copy the files you need into your existing `.squad/` directory.

### 2. Choose your cast

Each role has a placeholder `{Name}`. Replace it with your agent's name — could be a theme (mythology, sci-fi, animals) or just descriptive names (Alex, Sam, etc.).

| Role | Template | Example Name |
|------|----------|-------------|
| Lead | `charter-templates/lead.md` | Captain, Athena, Magnus |
| Developer | `charter-templates/developer.md` | Builder, Vulcan, Forge |
| Tester | `charter-templates/tester.md` | Sentinel, Argus, Scout |
| Documentarian | `charter-templates/documentarian.md` | Scribe, Clio, Archive |
| Reviewer | `charter-templates/reviewer.md` | Inspector, Minerva, Gate |

### 3. Set up agent folders

For each role you're using, create a folder under `.squad/agents/`:

```bash
mkdir -p .squad/agents/{lead,developer,tester,documentarian,reviewer}
```

Copy the relevant charter template into each folder as `charter.md`, and create an empty `history.md`:

```bash
cp charter-templates/lead.md .squad/agents/lead/charter.md
echo "# Project Context\n\n- **Owner:** {Your name}\n- **Project:** {Project name}\n- **Created:** $(date +%Y-%m-%d)\n\n## Learnings\n" > .squad/agents/lead/history.md
```

### 4. Customize

In each charter, replace these placeholders:

| Placeholder | Replace With |
|-------------|-------------|
| `{Name}` | Your agent's display name |
| `{Project}` | Your project name |
| `{project-slug}` | Your project's short identifier |

Then edit the **What I Own**, **Boundaries**, and **Voice** sections to fit your project's domain.

### 5. Update team.md and routing.md

- In `team.md`, fill in your cast names and remove roles you don't need.
- In `routing.md`, update the routing table with your agent names and project-specific work types.

## Roles Explained

| Role | Purpose | When You Need It |
|------|---------|-----------------|
| **Lead** | Triage, coordination, decision-making | Always — every squad needs a lead |
| **Developer** | Building features, scripts, tools, automation | When there's code to write |
| **Tester** | Quality assurance, test writing, edge case analysis | When you need quality gates on code |
| **Documentarian** | Writing docs, organizing knowledge, maintaining indexes | When documentation matters |
| **Reviewer** | Code review, PR quality gates, architecture alignment | When you want independent review |

### Optional Roles (Not Templated)

These roles aren't included as templates but are common additions:

- **Researcher** — Deep investigation, analysis, competitive research
- **Operator** — Livesite support, incident management, monitoring
- **Scribe** — Silent session logger, decision merging (infrastructure role)

## Design Principles

This template is based on patterns from a production squad. The key principles:

1. **Charters define identity** — each agent has clear ownership, boundaries, and delegation rules
2. **Routing prevents confusion** — explicit rules decide who handles what
3. **Ceremonies catch failures** — design reviews before complex work, retros after failures
4. **Collaboration protocol** — agents read shared decisions before starting, write decisions after making them
5. **Boundaries are explicit** — every charter says what the agent handles AND what it delegates

## Adapting the Template

### Minimal squad (2 roles)
Keep Lead + Developer. The lead handles triage and review; the developer builds.

### Standard squad (4 roles)
Lead + Developer + Documentarian + Reviewer. Covers build, document, and review.

### Full squad (5+ roles)
All templated roles plus Researcher, Operator, or custom domain roles.

### Adding custom roles
Copy any charter template, rename it, and adapt the sections. The structural pattern (Identity → Ownership → How I Work → Boundaries → Collaboration → Voice) works for any role.
