#Requires -Modules Pester

<#
.SYNOPSIS
    Integration tests for scripts/notify-squad-event.ps1 — the dispatcher.
    Validates parameter routing, validation, and script-resolution logic.
#>

BeforeAll {
    $repoRoot = & git rev-parse --show-toplevel 2>$null
    if (-not $repoRoot) { $repoRoot = Split-Path $PSScriptRoot -Parent }
    $dispatcherPath = Join-Path $repoRoot "scripts" "notify-squad-event.ps1"
}

Describe "notify-squad-event.ps1 — Dispatcher" {

    # -----------------------------------------------------------------------
    # Existence & syntax
    # -----------------------------------------------------------------------
    Context "Script basics" {
        It "exists on disk" {
            $dispatcherPath | Should -Exist
        }

        It "parses without syntax errors" {
            $errors = $null
            [System.Management.Automation.Language.Parser]::ParseFile(
                $dispatcherPath, [ref]$null, [ref]$errors
            )
            $errors | Should -BeNullOrEmpty
        }
    }

    # -----------------------------------------------------------------------
    # Parameter validation — feature-complete
    # -----------------------------------------------------------------------
    Context "feature-complete validation" {
        It "rejects missing -FeatureName" {
            { & $dispatcherPath -Event "feature-complete" -Summary "stuff" -DryRun } |
                Should -Throw
        }

        It "rejects missing -Summary" {
            { & $dispatcherPath -Event "feature-complete" -FeatureName "X" -DryRun } |
                Should -Throw
        }

        It "rejects invalid -Event values" {
            { & $dispatcherPath -Event "invalid-event" } |
                Should -Throw
        }
    }

    # -----------------------------------------------------------------------
    # Parameter validation — blocked
    # -----------------------------------------------------------------------
    Context "blocked validation" {
        It "rejects missing -What" {
            { & $dispatcherPath -Event "blocked" -Why "reason" -ActionNeeded "fix" -DryRun } |
                Should -Throw
        }

        It "rejects missing -Why" {
            { & $dispatcherPath -Event "blocked" -What "thing" -ActionNeeded "fix" -DryRun } |
                Should -Throw
        }

        It "rejects missing -ActionNeeded" {
            { & $dispatcherPath -Event "blocked" -What "thing" -Why "reason" -DryRun } |
                Should -Throw
        }
    }

    # -----------------------------------------------------------------------
    # Routing — feature-complete (mock downstream)
    # -----------------------------------------------------------------------
    Context "feature-complete routing" {
        BeforeAll {
            # Create a mock notify-feature-complete.ps1 that captures params
            $mockDir = Join-Path $TestDrive "scripts"
            New-Item -Path $mockDir -ItemType Directory -Force | Out-Null

            # Mock caller: writes received params to a JSON file
            $mockCaller = @'
param(
    [string]$FeatureId,
    [string]$FeatureTitle,
    [string]$Summary,
    [string]$TestInstructions,
    [string]$IssuesUrl,
    [string]$PRList,
    [switch]$DryRun,
    [switch]$Force
)
$captured = @{
    FeatureId        = $FeatureId
    FeatureTitle     = $FeatureTitle
    Summary          = $Summary
    TestInstructions = $TestInstructions
    IssuesUrl        = $IssuesUrl
    PRList           = $PRList
    DryRun           = [bool]$DryRun
    Force            = [bool]$Force
}
$captured | ConvertTo-Json | Set-Content (Join-Path $PSScriptRoot "captured-fc.json")
'@
            Set-Content (Join-Path $mockDir "notify-feature-complete.ps1") $mockCaller

            # Copy dispatcher into mock dir so $PSScriptRoot resolves to mockDir
            Copy-Item $dispatcherPath (Join-Path $mockDir "notify-squad-event.ps1")
            $mockDispatcher = Join-Path $mockDir "notify-squad-event.ps1"
        }

        It "routes to notify-feature-complete.ps1 with correct params" {
            & $mockDispatcher -Event "feature-complete" `
                -FeatureName "Auth Flow" -Summary "Token caching shipped." `
                -PRs "55,56" -DocLinks "https://docs/auth.md" `
                -DryRun -Force

            $capturedFile = Join-Path (Split-Path $mockDispatcher -Parent) "captured-fc.json"
            $capturedFile | Should -Exist
            $c = Get-Content $capturedFile -Raw | ConvertFrom-Json
            $c.FeatureTitle | Should -Be "Auth Flow"
            $c.Summary      | Should -Be "Token caching shipped."
            $c.PRList        | Should -Match "#55"
            $c.PRList        | Should -Match "#56"
            $c.IssuesUrl     | Should -Be "https://docs/auth.md"
            $c.DryRun        | Should -BeTrue
            $c.Force         | Should -BeTrue
        }

        It "auto-generates FeatureId slug from FeatureName" {
            & $mockDispatcher -Event "feature-complete" `
                -FeatureName "DGrep CLI Phase 1" -Summary "Shipped." -DryRun

            $capturedFile = Join-Path (Split-Path $mockDispatcher -Parent) "captured-fc.json"
            $c = Get-Content $capturedFile -Raw | ConvertFrom-Json
            $c.FeatureId | Should -Be "dgrep-cli-phase-1"
        }

        It "uses explicit FeatureId when provided" {
            & $mockDispatcher -Event "feature-complete" `
                -FeatureName "X" -Summary "Y" -FeatureId "custom-id" -DryRun

            $capturedFile = Join-Path (Split-Path $mockDispatcher -Parent) "captured-fc.json"
            $c = Get-Content $capturedFile -Raw | ConvertFrom-Json
            $c.FeatureId | Should -Be "custom-id"
        }

        It "normalises PR numbers with hash prefix" {
            & $mockDispatcher -Event "feature-complete" `
                -FeatureName "X" -Summary "Y" -PRs "#10, 20, #30" -DryRun

            $capturedFile = Join-Path (Split-Path $mockDispatcher -Parent) "captured-fc.json"
            $c = Get-Content $capturedFile -Raw | ConvertFrom-Json
            $c.PRList | Should -Be "#10, #20, #30"
        }
    }

    # -----------------------------------------------------------------------
    # Routing — blocked (mock downstream)
    # -----------------------------------------------------------------------
    Context "blocked routing" {
        BeforeAll {
            $mockDir = Join-Path $TestDrive "scripts-blocked"
            New-Item -Path $mockDir -ItemType Directory -Force | Out-Null

            $mockCaller = @'
param(
    [string]$Title,
    [string]$Reason,
    [string]$ActionNeeded,
    [string]$BlockerUrl,
    [string]$BlockerLabel,
    [string]$Agent,
    [string]$Severity,
    [switch]$DryRun,
    [switch]$Force
)
$captured = @{
    Title        = $Title
    Reason       = $Reason
    ActionNeeded = $ActionNeeded
    BlockerUrl   = $BlockerUrl
    Agent        = $Agent
    Severity     = $Severity
    DryRun       = [bool]$DryRun
    Force        = [bool]$Force
}
$captured | ConvertTo-Json | Set-Content (Join-Path $PSScriptRoot "captured-blocked.json")
'@
            Set-Content (Join-Path $mockDir "notify-blocked.ps1") $mockCaller
            Copy-Item $dispatcherPath (Join-Path $mockDir "notify-squad-event.ps1")
            $mockDispatcher = Join-Path $mockDir "notify-squad-event.ps1"
        }

        It "routes to notify-blocked.ps1 with correct params" {
            & $mockDispatcher -Event "blocked" `
                -What "VPN needed" -Why "dSTS requires corp" `
                -ActionNeeded "Enable VPN" -Link "https://issue/42" `
                -Urgency "livesite" -Agent "Gimli" `
                -DryRun -Force

            $capturedFile = Join-Path (Split-Path $mockDispatcher -Parent) "captured-blocked.json"
            $capturedFile | Should -Exist
            $c = Get-Content $capturedFile -Raw | ConvertFrom-Json
            $c.Title        | Should -Be "VPN needed"
            $c.Reason       | Should -Be "dSTS requires corp"
            $c.ActionNeeded | Should -Be "Enable VPN"
            $c.BlockerUrl   | Should -Be "https://issue/42"
            $c.Severity     | Should -Be "livesite"
            $c.Agent        | Should -Be "Gimli"
            $c.DryRun       | Should -BeTrue
            $c.Force        | Should -BeTrue
        }

        It "defaults Urgency to blocking-feature" {
            & $mockDispatcher -Event "blocked" `
                -What "X" -Why "Y" -ActionNeeded "Z" -DryRun

            $capturedFile = Join-Path (Split-Path $mockDispatcher -Parent) "captured-blocked.json"
            $c = Get-Content $capturedFile -Raw | ConvertFrom-Json
            $c.Severity | Should -Be "blocking-feature"
        }
    }

    # -----------------------------------------------------------------------
    # Error: missing downstream caller script
    # -----------------------------------------------------------------------
    Context "missing caller scripts" {
        BeforeAll {
            $emptyDir = Join-Path $TestDrive "empty-scripts"
            New-Item -Path $emptyDir -ItemType Directory -Force | Out-Null
            Copy-Item $dispatcherPath (Join-Path $emptyDir "notify-squad-event.ps1")
            $emptyDispatcher = Join-Path $emptyDir "notify-squad-event.ps1"
        }

        It "errors when notify-feature-complete.ps1 is missing" {
            { & $emptyDispatcher -Event "feature-complete" `
                -FeatureName "X" -Summary "Y" -DryRun } |
                Should -Throw "*not found*"
        }

        It "errors when notify-blocked.ps1 is missing" {
            { & $emptyDispatcher -Event "blocked" `
                -What "X" -Why "Y" -ActionNeeded "Z" -DryRun } |
                Should -Throw "*not found*"
        }
    }
}
