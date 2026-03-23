# semantic-index.ps1 — Builds .squad/semantic-model.json from squad state, docs, and git history.
# Usage: .\scripts\semantic-index.ps1
param([string]$Root = (git rev-parse --show-toplevel))
$ErrorActionPreference = "Stop"
$N = [System.Collections.ArrayList]::new(); $E = [System.Collections.ArrayList]::new()
function Add-N($t,$i,$p) { $N.Add((@{type=$t;id="$t`:$i"}+$p)) | Out-Null }
function Add-E($t,$s,$g) { $E.Add(@{type=$t;source=$s;target=$g}) | Out-Null }

# Agents
Get-ChildItem "$Root\.squad\agents" -Directory | ForEach-Object {
    $c = Get-Content "$($_.FullName)\charter.md" -Raw -EA SilentlyContinue
    $role = if ($c -match "Role:\*\*\s*(.+)") { $Matches[1].Trim() } else { "unknown" }
    Add-N "agent" $_.Name @{ role=$role; charter_path=".squad/agents/$($_.Name)/charter.md" }
}
# Decisions
$df = Get-Content "$Root\.squad\decisions.md" -Raw -EA SilentlyContinue
if ($df) { [regex]::Matches($df,'###\s+(.+?)\n\*\*(?:By|Author):\*\*\s*(.+?)(?:\s*\n)') | ForEach-Object {
    $t=$_.Groups[1].Value.Trim(); $a=$_.Groups[2].Value.Trim()
    Add-N "decision" (($t -replace '[^a-zA-Z0-9]+','-').ToLower().TrimEnd('-')) @{title=$t;author=$a}
}}
# Skills
Get-ChildItem "$Root\.squad\skills" -Directory | ForEach-Object { Add-N "skill" $_.Name @{path=".squad/skills/$($_.Name)"} }
# Docs (YAML frontmatter)
$nr = $Root.Replace("/","\")
Get-ChildItem "$Root\docs" -Recurse -Filter "*.md" -EA SilentlyContinue | ForEach-Object {
    $ct = Get-Content $_.FullName -Raw -EA SilentlyContinue
    $rp = $_.FullName.Replace("$nr\","").Replace("\","/")
    $title = if ($ct -match 'title:\s*"?([^"\n]+)"?') { $Matches[1].Trim() } else { $_.BaseName }
    $tags = @(); $fm = if ($ct -match '(?s)^---\s*\n(.+?)\n---') { $Matches[1] } else { "" }
    if ($fm -match 'tags:\s*\[([^\]]+)\]') { $tags = $Matches[1] -split ',\s*' | ForEach-Object { $_.Trim('" ') } }
    elseif ($fm -match '(?m)^tags:\s*$') {
        $in=$false; foreach ($ln in $fm -split '\n') {
            if ($ln -match '^\s*tags:\s*$') { $in=$true; continue }
            if ($in -and $ln -match '^\s+-\s+(.+)') { $tags += $Matches[1].Trim() }
            elseif ($in -and $ln -notmatch '^\s+-') { break }
        }
    }
    $st = if ($ct -match 'status:\s*(\S+)') { $Matches[1] } else { "unknown" }
    Add-N "doc" $rp @{title=$title;tags=$tags;status=$st}
    $tags | ForEach-Object { Add-E "tagged_with" "doc:$rp" "tag:$_" }
}
# Recent commits
git -C $Root log --format="%H|%an|%s" -30 2>$null | ForEach-Object {
    $p = $_ -split '\|',3
    if ($p.Count -eq 3) { Add-N "commit" $p[0].Substring(0,7) @{author=$p[1];message=$p[2]} }
}
# Write
$out = Join-Path $Root ".squad\semantic-model.json"
@{nodes=$N;edges=$E;metadata=@{updated=(Get-Date -Format "o");node_count=$N.Count;edge_count=$E.Count}} | ConvertTo-Json -Depth 5 | Set-Content $out -Encoding UTF8
Write-Host "Semantic model: $($N.Count) nodes, $($E.Count) edges -> $out"
