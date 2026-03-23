<#
.SYNOPSIS
  Queries Copilot CLI session_store for completed tasks in recent sessions.
.DESCRIPTION
  Reads the session_store SQLite database to extract session task history with
  summaries. Outputs structured text suitable for terminal display.
.PARAMETER Hours
  How many hours back to search. Default: 24.
.PARAMETER Top
  Maximum number of sessions to return. Default: 20.
#>
param(
    [int]$Hours = 24,
    [int]$Top = 20
)

$dbPath = Join-Path $env:USERPROFILE ".copilot\session-store.db"
if (-not (Test-Path $dbPath)) {
    Write-Host "Session store not found at $dbPath"
    exit 1
}

$cutoff = (Get-Date).AddHours(-$Hours).ToString("yyyy-MM-ddTHH:mm:ss")

$pyScript = @"
import sqlite3, sys, os
db = os.path.join(os.path.expanduser('~'), '.copilot', 'session-store.db')
conn = sqlite3.connect(db)
cur = conn.execute('''
    SELECT s.id, s.summary, s.branch, s.created_at,
           substr(t.user_message, 1, 120) as task
    FROM sessions s
    LEFT JOIN turns t ON t.session_id = s.id AND t.turn_index = 0
    WHERE s.created_at >= ?
    ORDER BY s.created_at DESC
    LIMIT ?
''', ('$cutoff', $Top))
rows = cur.fetchall()
if not rows:
    print('No sessions found in the last $Hours hours.')
    sys.exit(0)
cols = [d[0] for d in cur.description]
widths = [max(len(str(c)), max((len(str(r[i])) for r in rows), default=0)) for i, c in enumerate(cols)]
widths = [min(w, 40) for w in widths]
hdr = ' | '.join(c.ljust(widths[i]) for i, c in enumerate(cols))
print(hdr)
print('-' * len(hdr))
for row in rows:
    vals = [str(v if v else '').ljust(widths[i])[:widths[i]] for i, v in enumerate(row)]
    print(' | '.join(vals))
conn.close()
"@

py -c $pyScript 2>&1
