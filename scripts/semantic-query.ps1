# semantic-query.ps1 — Queries .squad/semantic-model.json
# Usage: .\scripts\semantic-query.ps1 -Type <type> [-Filter <value>]
# Types: nodes, edges, decisions, docs-by-tag, files-by-agent, related-issues, skills, agents
param(
    [Parameter(Mandatory)][ValidateSet(
        "nodes","edges","decisions","docs-by-tag",
        "files-by-agent","related-issues","skills","agents"
    )][string]$Type, [string]$Filter
)
$ErrorActionPreference = "Stop"
$mp = Join-Path (git rev-parse --show-toplevel) ".squad\semantic-model.json"
if (-not (Test-Path $mp)) { Write-Error "No semantic model. Run semantic-index.ps1 first."; exit 1 }
$m = Get-Content $mp -Raw | ConvertFrom-Json

switch ($Type) {
    "nodes"  { $m.nodes | Where-Object { -not $Filter -or $_.type -eq $Filter } | Format-Table type,id -Auto }
    "edges"  { $m.edges | Where-Object { -not $Filter -or $_.type -eq $Filter } | Format-Table type,source,target -Auto }
    "decisions" { $m.nodes | Where-Object { $_.type -eq "decision" } | Format-Table id,title,author -Auto }
    "docs-by-tag" {
        if (-not $Filter) { Write-Error "-Filter <tag> required"; exit 1 }
        $ids = ($m.edges | Where-Object { $_.type -eq "tagged_with" -and $_.target -eq "tag:$Filter" }).source
        $m.nodes | Where-Object { $ids -contains $_.id } | Format-Table id,title,status -Auto
    }
    "files-by-agent" {
        if (-not $Filter) { Write-Error "-Filter <agent> required"; exit 1 }
        $tgt = ($m.edges | Where-Object { $_.source -eq "agent:$Filter" }).target
        $m.nodes | Where-Object { $tgt -contains $_.id } | Format-Table type,id -Auto
    }
    "related-issues" {
        if (-not $Filter) { Write-Error "-Filter <keyword> required"; exit 1 }
        $m.nodes | Where-Object { $_.type -eq "doc" -and ($_.title -match $Filter -or $_.id -match $Filter) } |
            Format-Table id,title,tags -Auto
    }
    "skills" { $m.nodes | Where-Object { $_.type -eq "skill" } | Format-Table id,path -Auto }
    "agents" { $m.nodes | Where-Object { $_.type -eq "agent" } | Format-Table id,role -Auto }
}
