<#
.SYNOPSIS
  Converts pa-squad orchestration log entries into squad-monitor compatible format.
.DESCRIPTION
  Reads existing .squad/orchestration-log/ markdown files and rewrites them with
  filenames and content that squad-monitor can parse (YYYY-MM-DDTHH-MM-SSZ-agent.md).
.PARAMETER TeamRoot
  Path to the squad repo root. Defaults to current directory.
#>
param(
    [string]$TeamRoot = (Get-Location).Path
)

$srcDir = Join-Path $TeamRoot ".squad\orchestration-log"
if (-not (Test-Path $srcDir)) {
    Write-Host "No orchestration-log directory found at $srcDir"
    exit 0
}

$files = Get-ChildItem "$srcDir\*.md" -ErrorAction SilentlyContinue
$seenKeys = @{}

foreach ($f in $files) {
    $name = $f.BaseName
    # Already in squad-monitor format? Skip.
    if ($name -match '^\d{4}-\d{2}-\d{2}T\d{2}-\d{2}-\d{2}Z-') { continue }

    # Extract timestamp + agent from pa-squad format: YYYY-MM-DDTHH-MM-{agent-description}.md
    if ($name -match '^(\d{4}-\d{2}-\d{2}T\d{2}-\d{2})-(.+)$') {
        $tsBase = $Matches[1]
        $desc = $Matches[2]
        $agent = ($desc -split '-')[0]

        # Deduplicate: increment seconds for same timestamp+agent combos
        $key = "$tsBase-$agent"
        if (-not $seenKeys.ContainsKey($key)) { $seenKeys[$key] = 0 }
        $sec = $seenKeys[$key].ToString("D2")
        $seenKeys[$key]++

        $ts = "$tsBase-${sec}Z"
        $newName = "$ts-$agent.md"
        $newPath = Join-Path $srcDir $newName

        $content = Get-Content $f.FullName -Raw
        $adapted = $content -replace '## Task\b', '## Assignment'
        if ($adapted -notmatch '## Outcome|## Result|\*\*Result:\*\*') {
            $adapted += "`n## Outcome`nPending`n"
        }

        Set-Content -Path $newPath -Value $adapted -NoNewline
        Write-Host "Adapted: $($f.Name) -> $newName"
    }
}
Write-Host "Orchestration log sync complete."
