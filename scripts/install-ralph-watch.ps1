# install-ralph-watch.ps1 — Install ralph-watch to ~/.squad/bin/
# Works both locally (from repo) and via one-liner (downloads from GitHub)
# Idempotent: safe to run multiple times

$destDir = Join-Path $env:USERPROFILE ".squad\bin"
$destFile = Join-Path $destDir "ralph-watch.ps1"
$repoRaw = "https://raw.githubusercontent.com/jbenami_microsoft/ms-pa/main/.squad/bin/ralph-watch.ps1"

# Create ~/.squad/bin/ if it doesn't exist
if (-not (Test-Path $destDir)) {
    New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    Write-Host "Created $destDir" -ForegroundColor Gray
}

# Try local copy first (if running from repo root), otherwise download
# $PSScriptRoot is empty when invoked via `irm | iex`, so guard for that
if ([string]::IsNullOrEmpty($PSScriptRoot)) {
    $localSource = $null
} else {
    $localSource = Join-Path $PSScriptRoot "..\.squad\bin\ralph-watch.ps1"
}
if ($localSource -and (Test-Path $localSource)) {
    Copy-Item $localSource $destFile -Force
    Write-Host "Copied ralph-watch.ps1 from local repo" -ForegroundColor Gray
} else {
    Write-Host "Downloading ralph-watch.ps1 from GitHub..." -ForegroundColor Gray
    try {
        $iwrParams = @{ Uri = $repoRaw; OutFile = $destFile }
        if ($PSVersionTable.PSVersion.Major -lt 6) {
            $iwrParams.UseBasicParsing = $true
        }
        Invoke-WebRequest @iwrParams
    } catch {
        Write-Host "❌ Download failed: $_" -ForegroundColor Red
        exit 1
    }
}

if (-not (Test-Path $destFile)) {
    Write-Host "❌ Installation failed — $destFile not found" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✅ ralph-watch installed to $destFile" -ForegroundColor Green
Write-Host ""
Write-Host "Usage:" -ForegroundColor Cyan
Write-Host "  cd <your-project-root>" -ForegroundColor White
Write-Host "  ~/.squad/bin/ralph-watch.ps1" -ForegroundColor White
Write-Host ""
Write-Host "Configuration (optional):" -ForegroundColor Cyan
Write-Host "  Create .squad/ralph-watch.config.json in your project root" -ForegroundColor White
Write-Host ""
Write-Host "To stop: Ctrl+C" -ForegroundColor Gray
