### 2026-03-23T02:18:56Z: User directive — Watchdog strategy shift
**By:** Jonathan (via Copilot)
**What:** Prefer Tamir's teams-monitor approach (single agent skill) over the 6-script composable pipeline. Reasons: simpler, cheaper (~6 req vs ~18 per run), will run more than once daily soon. IMPORTANT: do NOT use `gh copilot` — it doesn't work. Use `copilot -p` with `--allow-tool='workiq'`. Run POCs early to validate before building full solution. Prompt Jonathan for help if needed.
**Why:** Cost matters at higher frequency. Simplicity preferred. Known `gh copilot` issue from previous project.
