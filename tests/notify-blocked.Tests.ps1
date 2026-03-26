<#
.SYNOPSIS
    Pester tests for scripts/notify-blocked.ps1 — blocked-on-human caller.
    Tests parameter validation, message formatting, dedup, and notify.ps1 integration.
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

    It "Reason is optional (multi-issue mode uses -Issues instead)" {
        $cmd = Get-Command $script:ScriptPath
        $mandatoryAttr = $cmd.Parameters['Reason'].Attributes |
            Where-Object { $_ -is [System.Management.Automation.ParameterAttribute] -and $_.Mandatory }
        $mandatoryAttr | Should -BeNullOrEmpty
    }

    It "declares ActionNeeded as mandatory" {
        $cmd = Get-Command $script:ScriptPath
        $cmd.Parameters['ActionNeeded'].Attributes |
            Where-Object { $_ -is [System.Management.Automation.ParameterAttribute] -and $_.Mandatory } |
            Should -Not -BeNullOrEmpty
    }

    It "requires either -Reason or -Issues" {
        { & $script:ScriptPath -Title "No reason" -ActionNeeded "Fix" -DryRun -Force } |
            Should -Throw "*Either*Issues*Reason*"
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

    It "includes reason, action needed, agent, and severity in the card" {
        $output = & $script:ScriptPath `
            -Title "Test blocker" `
            -Reason "Something is broken." `
            -ActionNeeded "Fix the thing." `
            -Agent "Gimli" `
            -Severity "blocking-feature" `
            -DryRun -Force *>&1 | Out-String

        # Reason appears as a text block in the card body
        $output | Should -Match "Something is broken"
        # Action needed appears with bold prefix
        $output | Should -Match "Action needed.*Fix the thing"
        # Agent and Severity appear in FactSet rows
        $output | Should -Match '"title":\s*"Agent"'
        $output | Should -Match '"value":\s*"Gimli"'
        $output | Should -Match '"title":\s*"Severity"'
        $output | Should -Match '"value":\s*"blocking-feature"'
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
        # Agent defaults to "unknown" in FactSet
        $output | Should -Match '"value":\s*"unknown"'
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

        $output | Should -Match '"value":\s*"livesite"'
        $output | Should -Match "Aragorn"
    }
}

Describe "notify-blocked.ps1 — Multi-Issue Mode" {

    It "sends a card with per-issue rows" {
        $issues = @(
            @{ Number = 89; Title = "Credential setup"; Url = "https://github.com/org/repo/issues/89"; Reason = "dSTS creds not configured" },
            @{ Number = 90; Title = "VPN config"; Url = "https://github.com/org/repo/issues/90"; Reason = "VPN access needed" }
        )
        $output = & $script:ScriptPath `
            -Title "3 issues need credentials" `
            -ActionNeeded "Provide access credentials." `
            -Issues $issues `
            -Agent "Gimli" `
            -DryRun -Force *>&1 | Out-String

        $output | Should -Match "DRY RUN.*urgent"
        $output | Should -Match "#89"
        $output | Should -Match "#90"
        $output | Should -Match "dSTS creds not configured"
        $output | Should -Match "VPN access needed"
    }

    It "ignores -Reason when -Issues is provided" {
        $issues = @(
            @{ Number = 1; Title = "Test"; Url = "https://example.com/1"; Reason = "Issue reason" }
        )
        $output = & $script:ScriptPath `
            -Title "Multi-issue" `
            -Reason "This should be ignored" `
            -ActionNeeded "Fix." `
            -Issues $issues `
            -DryRun -Force *>&1 | Out-String

        # The per-issue reason appears, not the top-level Reason
        $output | Should -Match "Issue reason"
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

        $output | Should -Match '"value":\s*"Elrond"'
    }
}

Describe "notify-blocked.ps1 — Dedup (No Duplicate Notifications)" {

    It "each call produces a card (timestamp-based eventId ensures uniqueness)" {
        # Each call generates a unique eventId (blocked:Agent:timestamp)
        # so separate calls don't collide in dedup state
        $output = & $script:ScriptPath `
            -Title "Unique event" `
            -Reason "Testing uniqueness." `
            -ActionNeeded "None." `
            -Agent "Gimli" `
            -DryRun -Force *>&1 | Out-String

        $output | Should -Match "DRY RUN.*urgent"
        $output | Should -Match "Unique event"
    }

    It "-Force bypasses any dedup state" {
        $output = & $script:ScriptPath `
            -Title "Force send" `
            -Reason "Must go through." `
            -ActionNeeded "Act now." `
            -Agent "Aragorn" `
            -DryRun -Force *>&1 | Out-String

        $output | Should -Match "DRY RUN.*urgent"
    }

    It "notify.ps1 suppresses re-send of same eventId without -Force" {
        # Call notify.ps1 directly with a fixed eventId to test dedup
        $notifyScript = Join-Path $PSScriptRoot "..\scripts\notify.ps1"
        $stateFile = Join-Path $TestDrive "dedup-state-$(New-Guid).json"
        $fixedEvent = @{
            eventId      = "blocked:TestAgent:fixed-id-for-dedup"
            errorType    = "needs-human"
            title        = "Dedup test"
            reason       = "Same blocker twice"
            actionNeeded = "Fix."
            agent        = "TestAgent"
            severity     = "blocking-feature"
        }

        # First send (with -Force to guarantee it goes, establishes state)
        $out1 = & $notifyScript -Type "urgent" -Event $fixedEvent `
            -StateFile $stateFile -DryRun -Force *>&1 | Out-String
        $out1 | Should -Match "DRY RUN.*urgent"

        # Second send without -Force — should be suppressed (dedup)
        $out2 = & $notifyScript -Type "urgent" -Event $fixedEvent `
            -StateFile $stateFile -DryRun *>&1 | Out-String
        $out2 | Should -Match "suppressed.*dedup"
    }
}

Describe "notify-blocked.ps1 — Escalation Scenario Tests" {

    It "Elrond exhausts research → notification includes research context" {
        $output = & $script:ScriptPath `
            -Title "Failure recovery exhausted: DGrep auth" `
            -Reason "Elrond found no viable solution after researching dSTS auth." `
            -ActionNeeded "Review research doc and provide guidance." `
            -BlockerUrl "https://github.com/org/repo/issues/42" `
            -BlockerLabel "View Issue" `
            -Agent "Elrond" `
            -Severity "decision-needed" `
            -DryRun -Force *>&1 | Out-String

        $output | Should -Match "DRY RUN.*urgent"
        $output | Should -Match "Failure recovery exhausted"
        $output | Should -Match '"value":\s*"Elrond"'
        $output | Should -Match '"value":\s*"decision-needed"'
        $output | Should -Match "dSTS auth"
        $output | Should -Match "github.com/org/repo/issues/42"
    }

    It "Aragorn blocked during livesite → immediate notification" {
        $output = & $script:ScriptPath `
            -Title "LIVESITE: Aragorn blocked on IcM investigation" `
            -Reason "Cannot query IcM API — auth token expired." `
            -ActionNeeded "Refresh IcM service principal credentials." `
            -BlockerUrl "https://github.com/org/repo/issues/99" `
            -Agent "Aragorn" `
            -Severity "livesite" `
            -DryRun -Force *>&1 | Out-String

        $output | Should -Match "DRY RUN.*urgent"
        $output | Should -Match "LIVESITE"
        $output | Should -Match '"value":\s*"Aragorn"'
        $output | Should -Match '"value":\s*"livesite"'
    }

    It "Gandalf double-rejection → escalation with multiple issues" {
        $issues = @(
            @{ Number = 42; Title = "Auth flow broken"; Url = "https://github.com/org/repo/issues/42"; Reason = "Gandalf rejected Elrond fix twice" },
            @{ Number = 43; Title = "Retry still fails"; Url = "https://github.com/org/repo/issues/43"; Reason = "Fix applied but original task still errors" }
        )
        $output = & $script:ScriptPath `
            -Title "Escalation: 2 blockers need human review" `
            -ActionNeeded "Review both issues and provide direction." `
            -Issues $issues `
            -Agent "Gandalf" `
            -Severity "blocking-feature" `
            -DryRun -Force *>&1 | Out-String

        $output | Should -Match "DRY RUN.*urgent"
        $output | Should -Match "#42"
        $output | Should -Match "#43"
        $output | Should -Match "Gandalf rejected"
        $output | Should -Match "still errors"
    }
}
