<#
.SYNOPSIS
    Pester tests for scripts/notify.ps1 — webhook notification router.
    Tests card formatting, dedup logic, batching, and state management.
#>

BeforeAll {
    $script:NotifyScript = Join-Path $PSScriptRoot "..\scripts\notify.ps1"
    $script:TempDir      = Join-Path $TestDrive "notify-tests"
    New-Item -Path $script:TempDir -ItemType Directory -Force | Out-Null

    # Helper: create a temp state file path
    function New-TempStateFile {
        $path = Join-Path $script:TempDir "state-$(New-Guid).json"
        return $path
    }

    # Helper: create a temp webhook file
    function New-TempWebhookFile {
        param([string]$Url = "https://outlook.office.com/webhook/test-guid")
        $path = Join-Path $script:TempDir "webhook-$(New-Guid).url"
        Set-Content -Path $path -Value $Url
        return $path
    }

    # Helper: run notify.ps1 with DryRun and capture output
    function Invoke-NotifyDryRun {
        param(
            [string]$Type,
            [hashtable]$Event,
            [string]$StateFile,
            [switch]$Force
        )
        $webhookFile = New-TempWebhookFile
        if (-not $StateFile) { $StateFile = New-TempStateFile }
        $params = @{
            Type        = $Type
            Event       = $Event
            StateFile   = $StateFile
            WebhookFile = $webhookFile
            DryRun      = $true
        }
        if ($Force) { $params.Force = $true }
        $output = & $script:NotifyScript @params *>&1
        return @{
            Output    = ($output | Out-String)
            StateFile = $StateFile
        }
    }

    # Helper: read state from file
    function Read-TestState {
        param([string]$Path)
        if (Test-Path $Path) {
            return Get-Content $Path -Raw | ConvertFrom-Json -AsHashtable
        }
        return $null
    }
}

Describe "notify.ps1 — Adaptive Card Formatting" {

    Context "Urgent (🔴) card" {
        It "produces a card with red emoji, title, reason, and action button" {
            $event = @{
                eventId     = "test-urgent-001"
                errorType   = "script-failure"
                title       = "icm-scan failed"
                reason      = "timeout querying incident"
                actionUrl   = "https://github.com/org/repo/issues/42"
                actionLabel = "View Investigation"
            }
            $result = Invoke-NotifyDryRun -Type "urgent" -Event $event -Force
            $result.Output | Should -Match "DRY RUN.*urgent"
            $result.Output | Should -Match "icm-scan failed"
            $result.Output | Should -Match "timeout querying incident"
            $result.Output | Should -Match "View Investigation"
            $result.Output | Should -Match "Action.OpenUrl"
            $result.Output | Should -Match "https://github.com/org/repo/issues/42"
        }

        It "includes 'What to do' section when actionLabel is provided" {
            $event = @{
                eventId     = "test-urgent-whatdo"
                title       = "Setup blocker"
                reason      = "Missing NuGet feed"
                actionLabel = "Configure Feed"
                actionUrl   = "https://example.com"
            }
            $result = Invoke-NotifyDryRun -Type "urgent" -Event $event -Force
            $result.Output | Should -Match "What to do"
            $result.Output | Should -Match "Configure Feed"
        }

        It "works without actionUrl (no action button)" {
            $event = @{
                eventId = "test-urgent-nourl"
                title   = "Unknown error"
                reason  = "Something broke"
            }
            $result = Invoke-NotifyDryRun -Type "urgent" -Event $event -Force
            $result.Output | Should -Match "Unknown error"
            $result.Output | Should -Not -Match "Action.OpenUrl"
        }
    }

    Context "Action (🟡) card" {
        It "produces a card with yellow emoji, title, reason, and time estimate" {
            $event = @{
                eventId       = "test-action-001"
                actionType    = "pr-review"
                title         = "PR #42: Ready for Review"
                reason        = "Galadriel review complete"
                actionUrl     = "https://github.com/org/repo/pull/42"
                actionLabel   = "Review PR"
                estimatedTime = "~5 min read"
            }
            $result = Invoke-NotifyDryRun -Type "action" -Event $event -Force
            $result.Output | Should -Match "PR #42"
            $result.Output | Should -Match "Galadriel review complete"
            $result.Output | Should -Match "~5 min read"
            $result.Output | Should -Match "Review PR"
        }

        It "omits time estimate when not provided" {
            $event = @{
                eventId    = "test-action-notime"
                title      = "Setup task"
                reason     = "Credentials needed"
            }
            $result = Invoke-NotifyDryRun -Type "action" -Event $event -Force
            $result.Output | Should -Not -Match "⏱️"
        }
    }

    Context "Feature (🔵) card" {
        It "produces a batched card with blue emoji, test instructions, and next action" {
            $event = @{
                eventId          = "test-feature-001"
                featureTitle     = "Auth Flow Complete"
                summary          = "Token caching works."
                testInstructions = "1. Run login\n2. Verify cache"
                issuesUrl        = "https://github.com/org/repo/issues"
                nextAction       = "Waiting for feedback"
                nextActionDue    = "2026-04-01"
            }
            $stateFile = New-TempStateFile
            $result = Invoke-NotifyDryRun -Type "feature" -Event $event -StateFile $stateFile -Force
            $result.Output | Should -Match "Auth Flow Complete"
            $result.Output | Should -Match "Token caching works"
            $result.Output | Should -Match "1\. Run login"
            $result.Output | Should -Match "View Issues"
        }
    }
}

Describe "notify.ps1 — Deduplication" {

    Context "Urgent dedup" {
        It "sends first occurrence of an event" {
            $stateFile = New-TempStateFile
            $event = @{
                eventId = "dedup-urgent-first"
                title   = "First fire"
                reason  = "boom"
            }
            $result = Invoke-NotifyDryRun -Type "urgent" -Event $event -StateFile $stateFile
            $result.Output | Should -Match "DRY RUN.*urgent"
        }

        It "suppresses duplicate within 6h window" {
            $stateFile = New-TempStateFile
            $event = @{
                eventId = "dedup-urgent-dup"
                title   = "Dup test"
                reason  = "same event"
            }
            # First send
            Invoke-NotifyDryRun -Type "urgent" -Event $event -StateFile $stateFile | Out-Null

            # Second send — should be suppressed
            $result2 = Invoke-NotifyDryRun -Type "urgent" -Event $event -StateFile $stateFile
            $result2.Output | Should -Match "suppressed"
        }

        It "re-notifies when Force is used" {
            $stateFile = New-TempStateFile
            $event = @{
                eventId = "dedup-urgent-force"
                title   = "Force test"
                reason  = "forced"
            }
            Invoke-NotifyDryRun -Type "urgent" -Event $event -StateFile $stateFile | Out-Null
            $result = Invoke-NotifyDryRun -Type "urgent" -Event $event -StateFile $stateFile -Force
            $result.Output | Should -Match "DRY RUN.*urgent"
        }
    }

    Context "Action dedup" {
        It "suppresses duplicate action within 48h" {
            $stateFile = New-TempStateFile
            $event = @{
                eventId = "dedup-action-dup"
                title   = "PR review"
                reason  = "needs review"
            }
            Invoke-NotifyDryRun -Type "action" -Event $event -StateFile $stateFile | Out-Null
            $result = Invoke-NotifyDryRun -Type "action" -Event $event -StateFile $stateFile
            $result.Output | Should -Match "suppressed"
        }
    }

    Context "Feature dedup" {
        It "never re-sends same feature" {
            $stateFile = New-TempStateFile
            $event = @{
                eventId      = "dedup-feature-once"
                featureTitle = "One-shot feature"
                summary      = "Done."
            }
            Invoke-NotifyDryRun -Type "feature" -Event $event -StateFile $stateFile -Force | Out-Null
            $result = Invoke-NotifyDryRun -Type "feature" -Event $event -StateFile $stateFile
            $result.Output | Should -Match "suppressed"
        }
    }
}

Describe "notify.ps1 — State Management" {

    It "creates state file if it doesn't exist" {
        $stateFile = New-TempStateFile
        $event = @{
            eventId = "state-create-test"
            title   = "State test"
            reason  = "testing"
        }
        Invoke-NotifyDryRun -Type "urgent" -Event $event -StateFile $stateFile | Out-Null
        Test-Path $stateFile | Should -Be $true
    }

    It "persists events across invocations" {
        $stateFile = New-TempStateFile
        $event = @{
            eventId = "state-persist-test"
            title   = "Persist"
            reason  = "check"
        }
        Invoke-NotifyDryRun -Type "urgent" -Event $event -StateFile $stateFile | Out-Null
        $state = Read-TestState -Path $stateFile
        $state.events.Keys | Should -Contain "state-persist-test"
        $state.events["state-persist-test"].type | Should -Be "urgent"
    }

    It "records version in state" {
        $stateFile = New-TempStateFile
        $event = @{
            eventId = "state-version-test"
            title   = "Version"
            reason  = "check"
        }
        Invoke-NotifyDryRun -Type "urgent" -Event $event -StateFile $stateFile | Out-Null
        $state = Read-TestState -Path $stateFile
        $state.version | Should -Be 1
    }
}

Describe "notify.ps1 — Feature Batching" {

    It "enqueues feature events in the state file" {
        $stateFile = New-TempStateFile
        $event = @{
            eventId      = "batch-enqueue-1"
            featureTitle = "Feature A"
            summary      = "Summary A"
        }
        $result = Invoke-NotifyDryRun -Type "feature" -Event $event -StateFile $stateFile -Force
        $result.Output | Should -Match "Feature queued.*Feature A"
    }

    It "flushes when queue reaches 5 items" {
        $stateFile = New-TempStateFile
        $lastResult = $null
        for ($i = 1; $i -le 5; $i++) {
            $event = @{
                eventId      = "batch-flush-$i"
                featureTitle = "Feature $i"
                summary      = "Summary $i"
            }
            $lastResult = Invoke-NotifyDryRun -Type "feature" -Event $event -StateFile $stateFile -Force
        }
        # The 5th item triggers flush in DryRun
        $lastResult.Output | Should -Match "Batch Feature Card"
    }
}

Describe "notify.ps1 — Input Validation" {

    It "rejects urgent event missing 'title'" {
        $stateFile = New-TempStateFile
        $event = @{
            eventId = "val-no-title"
            reason  = "no title"
        }
        $webhookFile = New-TempWebhookFile
        { & $script:NotifyScript -Type "urgent" -Event $event -StateFile $stateFile -WebhookFile $webhookFile -DryRun *>&1 } |
            Should -Throw
    }

    It "rejects urgent event missing 'reason'" {
        $stateFile = New-TempStateFile
        $event = @{
            eventId = "val-no-reason"
            title   = "has title"
        }
        $webhookFile = New-TempWebhookFile
        { & $script:NotifyScript -Type "urgent" -Event $event -StateFile $stateFile -WebhookFile $webhookFile -DryRun *>&1 } |
            Should -Throw
    }

    It "rejects feature event missing 'featureTitle'" {
        $stateFile = New-TempStateFile
        $event = @{
            eventId = "val-no-feat-title"
            summary = "has summary"
        }
        $webhookFile = New-TempWebhookFile
        { & $script:NotifyScript -Type "feature" -Event $event -StateFile $stateFile -WebhookFile $webhookFile -DryRun *>&1 } |
            Should -Throw
    }

    It "rejects invalid Type" {
        $stateFile = New-TempStateFile
        $event = @{ title = "x"; reason = "y" }
        $webhookFile = New-TempWebhookFile
        { & $script:NotifyScript -Type "invalid" -Event $event -StateFile $stateFile -WebhookFile $webhookFile -DryRun *>&1 } |
            Should -Throw
    }
}

Describe "notify.ps1 — Webhook File Handling" {

    It "warns and exits gracefully when webhook file is missing (non-DryRun)" {
        $stateFile = New-TempStateFile
        $missingWebhook = Join-Path $script:TempDir "missing-webhook.url"
        $event = @{
            eventId = "webhook-missing"
            title   = "Test"
            reason  = "Check"
        }
        $output = & $script:NotifyScript -Type "urgent" -Event $event `
            -StateFile $stateFile -WebhookFile $missingWebhook -Force *>&1 | Out-String
        $output | Should -Match "Webhook URL file not found"
    }

    It "warns when webhook file is empty" {
        $stateFile = New-TempStateFile
        $emptyWebhook = Join-Path $script:TempDir "empty-webhook.url"
        Set-Content -Path $emptyWebhook -Value ""
        $event = @{
            eventId = "webhook-empty"
            title   = "Test"
            reason  = "Check"
        }
        $output = & $script:NotifyScript -Type "urgent" -Event $event `
            -StateFile $stateFile -WebhookFile $emptyWebhook -Force *>&1 | Out-String
        $output | Should -Match "Webhook URL file is empty"
    }
}
