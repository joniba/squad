# semantic-index.ps1 — Builds .squad/semantic-model.json (8 node types, 8 edge types)
param([string]$Root = (git rev-parse --show-toplevel))
$ErrorActionPreference = "Stop"
$N = [System.Collections.ArrayList]::new(); $E = [System.Collections.ArrayList]::new()
function Add-N($t,$i,$p) { $N.Add((@{type=$t;id="$t`:$i"}+$p)) | Out-Null }
function Add-E($t,$s,$g) { $E.Add(@{type=$t;source=$s;target=$g}) | Out-Null }
$nr = $Root.Replace("/","\"); $repo = "jbenami_microsoft/ms-pa"

# 1. Agents
Get-ChildItem "$Root\.squad\agents" -Directory | ForEach-Object {
    $c = Get-Content "$($_.FullName)\charter.md" -Raw -EA SilentlyContinue
    $role = if ($c -match "Role:\*\*\s*(.+)") { $Matches[1].Trim() } else { "unknown" }
    Add-N "agent" $_.Name @{ role=$role; charter_path=".squad/agents/$($_.Name)/charter.md" }
}
# 2. Decisions — also emit decided_in and authored edges
$df = Get-Content "$Root\.squad\decisions.md" -Raw -EA SilentlyContinue
if ($df) { [regex]::Matches($df,'###\s+(.+?)\n\*\*(?:By|Author):\*\*\s*(.+?)(?:\s*\n)') | ForEach-Object {
    $t=$_.Groups[1].Value.Trim(); $a=$_.Groups[2].Value.Trim()
    $slug = (($t -replace '[^a-zA-Z0-9]+','-').ToLower().TrimEnd('-'))
    Add-N "decision" $slug @{title=$t;author=$a}
    $agentId = ($a -replace '\s.*','').ToLower()
    if ($N | Where-Object { $_.id -eq "agent:$agentId" }) { Add-E "authored" "agent:$agentId" "decision:$slug" }
    if ($t -match '#(\d+)') { Add-E "decided_in" "decision:$slug" "issue:$($Matches[1])" }
}}
# 3. Skills — emit owns edge from routing.md agent mapping
Get-ChildItem "$Root\.squad\skills" -Directory | ForEach-Object {
    Add-N "skill" $_.Name @{path=".squad/skills/$($_.Name)"}
    $sm = Get-Content "$($_.FullName)\SKILL.md" -Raw -EA SilentlyContinue
    if ($sm -match '(?i)owner:\s*(\w+)') { Add-E "owns" "agent:$($Matches[1].ToLower())" "skill:$($_.Name)" }
}
# 4. Docs — frontmatter parsing + authored & references edges
Get-ChildItem "$Root\docs" -Recurse -Filter "*.md" -EA SilentlyContinue | ForEach-Object {
    $ct = Get-Content $_.FullName -Raw -EA SilentlyContinue; if (-not $ct) { return }
    $rp = $_.FullName.Replace("$nr\","").Replace("\","/")
    $fm = if ($ct -match '(?s)^---\s*\n(.+?)\n---') { $Matches[1] } else { "" }
    $title = if ($ct -match 'title:\s*"?([^"\n]+)"?') { $Matches[1].Trim() } else { $_.BaseName }
    $tags = @()
    if ($fm -match 'tags:\s*\[([^\]]+)\]') { $tags = $Matches[1] -split ',\s*' | ForEach-Object { $_.Trim('" ') } }
    $st = if ($ct -match 'status:\s*(\S+)') { $Matches[1] } else { "unknown" }
    Add-N "doc" $rp @{title=$title;tags=$tags;status=$st}
    $tags | ForEach-Object { Add-E "tagged_with" "doc:$rp" "tag:$_" }
    if ($fm -match 'author:\s*(\w+)') { Add-E "authored" "agent:$($Matches[1].ToLower())" "doc:$rp" }
    [regex]::Matches($ct, '\]\((?!http)([^)]+\.md)\)') | ForEach-Object {
        $ref = $_.Groups[1].Value -replace '\\','/' -replace '^\./',''; Add-E "references" "doc:$rp" "doc:$ref"
    }
}
# 5. Commits — emit modifies edges for changed files
git -C $Root log --format="%H|%an|%s" -30 2>$null | ForEach-Object {
    $p = $_ -split '\|',3; if ($p.Count -ne 3) { return }
    $sh = $p[0].Substring(0,7); Add-N "commit" $sh @{author=$p[1];message=$p[2]}
    git -C $Root diff-tree --no-commit-id --name-only -r $p[0] 2>$null | Select-Object -First 10 | ForEach-Object {
        Add-E "modifies" "commit:$sh" "file:$_"
    }
}
# 6. Issues (GitHub)
try { (gh issue list --state all --json number,title,labels,state --limit 50 --repo $repo 2>$null | ConvertFrom-Json) | ForEach-Object {
    $lbl = @($_.labels | ForEach-Object { $_.name }); $num = $_.number
    Add-N "issue" $num @{title=$_.title;state=$_.state;labels=$lbl}
    $lbl | Where-Object { $_ -match '^squad:(.+)' } | ForEach-Object { Add-E "owns" "agent:$($Matches[1])" "issue:$num" }
}} catch {}
# 7. PRs (GitHub) — emit implements edges from "Resolves #N"
try { (gh pr list --state all --json number,title,state,body --limit 50 --repo $repo 2>$null | ConvertFrom-Json) | ForEach-Object {
    Add-N "pr" $_.number @{title=$_.title;state=$_.state}
    if ($_.body -match '(?i)(?:resolves?|closes?|fixes?)\s+#(\d+)') { Add-E "implements" "pr:$($_.number)" "issue:$($Matches[1])" }
}} catch {}
# 8. Sessions — scan log directories
foreach ($dir in @("$Root\.squad\log","$Root\.squad\orchestration-log")) {
    Get-ChildItem $dir -File -EA SilentlyContinue | ForEach-Object {
        $sn = $_.BaseName; Add-N "session" $sn @{path=$_.FullName.Replace("$nr\","").Replace("\","/")}
    }
}
# 9. Key squad files
@(".squad/routing.md",".squad/decisions.md",".squad/README.md") | ForEach-Object {
    if (Test-Path "$Root\$($_ -replace '/','\')" ) { Add-N "file" $_ @{category="squad-config"} }
}
# Write
$out = Join-Path $Root ".squad\semantic-model.json"
@{nodes=$N;edges=$E;metadata=@{updated=(Get-Date -Format "o");node_count=$N.Count;edge_count=$E.Count}} | ConvertTo-Json -Depth 5 | Set-Content $out -Encoding UTF8
Write-Host "Semantic model: $($N.Count) nodes, $($E.Count) edges -> $out"
