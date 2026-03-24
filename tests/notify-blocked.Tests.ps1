<#
.SYNOPSIS
    Pester tests for scripts/notify-blocked.ps1 — blocked-on-human caller.
    Tests parameter validation, message formatting, and notify.ps1 integration.
#>

BeforeAll {
    $script:ScriptPath   = Join-Path $PSScriptRoot "..\scripts\notify-blocked.ps1"
    $script:NotifyScript = Join-Path $PSScriptRoot "..\scripts\notify.ps1"
    $script:TempDir      = Join-Path $TestDrive "blocked-tests"
    New-Item -Path $script:TempDir -ItemType Directory -Force | Out-Null
}

Describe "notify-blocked.ps1 — Parameter Validation" {

    It "declares Title as mandatory" {
        $cmd = Get-Command $script:ScriptPath
        $cmd.Parameters['Title'].Attributes |
            Where-Object { $_ -is [System.Management.Automation.ParameterAttribute] -and $_.Mandatory } |
            Should -Not -BeNullOrEmpty
    }

    It "declares Reason as mandatory" {
        $cmd = Get-Command $script:ScriptPath
        $cmd.Parameters['Reason'].Attributes |
            Where-Object { $_ -is [System.Management.Automation.ParameterAttribute] -and $_.Mandatory } |
            Should -Not -BeNullOrEmpty
    }

    It "declares ActionNeeded as mandatory" {
        $cmd = Get-Command $script:ScriptPath
        $cmd.Parameters['ActionNeeded'].Attributes |
            Where-Object { $_ -is [System.Management.Automation.ParameterAttribute] -and $_.Mandatory } |
            Should -Not -BeNullOrEmpty
    }

    It "validates Severity with ValidateSet" {
        $cmd = Get-Command $script:ScriptPath
        $validateSet = $cmd.Parameters['Severity'].Attributes |
            Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
        $validateSet | Should -Not -BeNullOrEmpty
        $validateSet.ValidValues | Should -Contain "blocking-feature"
        $validateSet.ValidValues | Should -Contain "livesite"
        $validateSet.ValidValues | Should -Contain "decision-needed"
    }

    It "accepts all valid Severity values" {
        foreach ($sev in @("blocking-feature", "livesite", "decision-needed")) {
            $output = & $script:ScriptPath `
                -Title "Sev test" -Reason "testing $sev" -ActionNeeded "None" `
                -Severity $sev -DryRun -Force *>&1 | Out-String
            $output | Should -Match "DRY RUN"
        }
    }

    It "defaults Severity to blocking-feature" {
        $output = & $script:ScriptPath `
            -Title "Default sev" -Reason "check default" -ActionNeeded "None" `
            -DryRun -Force *>&1 | Out-String
        $output | Should -Match "blocking-feature"
    }

    It "defaults BlockerLabel to View Blocker" {
        $output = & $script:ScriptPath `
            -Title "Default label" -Reason "check label" -ActionNeeded "None" `
            -BlockerUrl "https://example.com" `
            -DryRun -Force *>&1 | Out-String
        $output | Should -Match "View Blocker"
    }
}

Describe "notify-blocked.ps1 — Message Formatting (DryRun)" {

    It "produces an urgent card with red emoji and title" {
        $output = & $script:ScriptPath `
            -Title "Auth requires VPN access" `
            -Reason "SDK dSTS auth needs VPN tunnel." `
            -ActionNeeded "Confirm VPN access for build machine." `
            -DryRun -Force *>&1 | Out-String

        $output | Should -Match "DRY RUN.*urgent"
        $output | Should -Match "Auth requires VPN access"
    }

    It "includes reason with Why, Action needed, Agent, and Severity" {
        $output = & $script:ScriptPath `
            -Title "Test blocker" `
            -Reason "Something is broken." `
            -ActionNeeded "Fix the thing." `
            -Agent "Gimli" `
            -Severity "blocking-feature" `
            -DryRun -Force *>&1 | Out-String

        $output | Should -Match "Why.*Something is broken"
        $output | Should -Match "Action needed.*Fix the thing"
        $output | Should -Match "Agent.*Gimli"
        $output | Should -Match "Severity.*blocking-feature"
    }

    It "includes action URL button when BlockerUrl is provided" {
        $output = & $script:ScriptPath `
            -Title "With URL" `
            -Reason "Needs link." `
            -ActionNeeded "Click it." `
            -BlockerUrl "https://github.com/org/repo/issues/42" `
            -BlockerLabel "View Issue" `
            -DryRun -Force *>&1 | Out-String

        $output | Should -Match "Action.OpenUrl"
        $output | Should -Match "github.com/org/repo/issues/42"
        $output | Should -Match "View Issue"
    }

    It "works without optional parameters (no URL, no agent)" {
        $output = & $script:ScriptPath `
            -Title "Minimal blocker" `
            -Reason "Just the basics." `
            -ActionNeeded "Do something." `
            -DryRun -Force *>&1 | Out-String

        $output | Should -Match "Minimal blocker"
        $output | Should -Match "Just the basics"
        $output | Should -Match "Agent.*unknown"
    }

    It "uses custom BlockerLabel on the action button" {
        $output = & $script:ScriptPath `
            -Title "Custom label" `
            -Reason "Label test." `
            -ActionNeeded "Check." `
            -BlockerUrl "https://example.com" `
            -BlockerLabel "View Incident" `
            -DryRun -Force *>&1 | Out-String

        $output | Should -Match "View Incident"
    }

    It "shows livesite severity correctly" {
        $output = & $script:ScriptPath `
            -Title "LIVESITE: Aragorn blocked" `
            -Reason "Active customer-impacting incident." `
            -ActionNeeded "Take over investigation." `
            -Agent "Aragorn" `
            -Severity "livesite" `
            -DryRun -Force *>&1 | Out-String

        $output | Should -Match "Severity.*livesite"
        $output | Should -Match "Aragorn"
    }
}

Describe "notify-blocked.ps1 — Integration with notify.ps1" {

    It "sends through notify.ps1 urgent tier successfully" {
        $output = & $script:ScriptPath `
            -Title "Integration test" `
            -Reason "Verifying pipeline." `
            -ActionNeeded "None needed." `
            -DryRun -Force *>&1 | Out-String

        $output | Should -Match "DRY RUN.*urgent"
        $output | Should -Match "Integration test"
    }

    It "includes agent name in the card body" {
        $output = & $script:ScriptPath `
            -Title "EventId test" `
            -Reason "Check agent inclusion." `
            -ActionNeeded "Inspect." `
            -Agent "Elrond" `
            -DryRun -Force *>&1 | Out-String

        # Agent name appears in the reason body
        $output | Should -Match "Agent.*Elrond"
    }
}
