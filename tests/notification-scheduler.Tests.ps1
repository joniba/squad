<#
.SYNOPSIS
    Pester tests for scripts/notification-scheduler.ps1 — event-driven
    notification triggers, config management, and integration with notify.ps1.
#>

BeforeAll {
    $script:SchedulerScript = Join-Path $PSScriptRoot "..\scripts\notification-scheduler.ps1"
    $script:NotifyScript    = Join-Path $PSScriptRoot "..\scripts\notify.ps1"
    $script:TempDir         = Join-Path $TestDrive "scheduler-tests"
    New-Item -Path $script:TempDir -ItemType Directory -Force | Out-Null

    # Dot-source the scheduler to get exported functions
    . $script:SchedulerScript

    # Helper: create a temp triggers file path
    function New-TempTriggersFile {
        $path = Join-Path $script:TempDir "triggers-$(New-Guid).json"
        return $path
    }

    # Helper: create a temp webhook file
    function New-TempWebhookFile {
        param([string]$Url = "https://outlook.office.com/webhook/test-guid")
        $path = Join-Path $script:TempDir "webhook-$(New-Guid).url"
        Set-Content -Path $path -Value $Url
        return $path
    }

    # Helper: create a temp state file path
    function New-TempStateFile {
        $path = Join-Path $script:TempDir "state-$(New-Guid).json"
        return $path
    }

    # Helper: read triggers config from file
    function Read-TestTriggersFile {
        param([string]$Path)
        if (Test-Path $Path) {
            return Get-Content $Path -Raw | ConvertFrom-Json -AsHashtable
        }
        return $null
    }
}

Describe "Register-NotificationTrigger" {

    It "registers a new trigger and persists to file" {
        $tf = New-TempTriggersFile
        $result = Register-NotificationTrigger -Name "test-trigger" `
            -EventType "test-event" -Tier "urgent" -Template "test-template" `
            -TriggersFile $tf

        $result | Should -Not -BeNullOrEmpty
        $config = Read-TestTriggersFile -Path $tf
        $config.triggers.ContainsKey("test-trigger") | Should -Be $true
        $config.triggers["test-trigger"].tier | Should -Be "urgent"
        $config.triggers["test-trigger"].template | Should -Be "test-template"
        $config.triggers["test-trigger"].eventType | Should -Be "test-event"
        $config.triggers["test-trigger"].enabled | Should -Be $true
    }

    It "overwrites an existing trigger with same name" {
        $tf = New-TempTriggersFile
        Register-NotificationTrigger -Name "dup-trigger" `
            -EventType "ev1" -Tier "urgent" -Template "t1" -TriggersFile $tf | Out-Null

        Register-NotificationTrigger -Name "dup-trigger" `
            -EventType "ev2" -Tier "action" -Template "t2" -TriggersFile $tf | Out-Null

        $config = Read-TestTriggersFile -Path $tf
        $config.triggers["dup-trigger"].eventType | Should -Be "ev2"
        $config.triggers["dup-trigger"].tier | Should -Be "action"
    }

    It "creates the config file directory if missing" {
        $nestedPath = Join-Path $script:TempDir "nested\dir\triggers.json"
        Register-NotificationTrigger -Name "nested-test" `
            -EventType "ev" -Tier "feature" -Template "t" -TriggersFile $nestedPath | Out-Null

        Test-Path $nestedPath | Should -Be $true
    }

    It "supports disabled triggers" {
        $tf = New-TempTriggersFile
        Register-NotificationTrigger -Name "disabled-trigger" `
            -EventType "ev" -Tier "urgent" -Template "t" `
            -Enabled $false -TriggersFile $tf | Out-Null

        $config = Read-TestTriggersFile -Path $tf
        $config.triggers["disabled-trigger"].enabled | Should -Be $false
    }

    It "rejects invalid tier" {
        $tf = New-TempTriggersFile
        { Register-NotificationTrigger -Name "bad-tier" `
            -EventType "ev" -Tier "invalid" -Template "t" -TriggersFile $tf } |
            Should -Throw
    }
}

Describe "Unregister-NotificationTrigger" {

    It "removes an existing trigger" {
        $tf = New-TempTriggersFile
        Register-NotificationTrigger -Name "remove-me" `
            -EventType "ev" -Tier "urgent" -Template "t" -TriggersFile $tf | Out-Null

        $result = Unregister-NotificationTrigger -Name "remove-me" -TriggersFile $tf
        $result | Should -Be $true

        $config = Read-TestTriggersFile -Path $tf
        $config.triggers.ContainsKey("remove-me") | Should -Be $false
    }

    It "returns false for non-existent trigger" {
        $tf = New-TempTriggersFile
        $result = Unregister-NotificationTrigger -Name "ghost" -TriggersFile $tf
        $result | Should -Be $false
    }

    It "does not affect other triggers when removing one" {
        $tf = New-TempTriggersFile
        Register-NotificationTrigger -Name "keep-me" `
            -EventType "ev" -Tier "action" -Template "t1" -TriggersFile $tf | Out-Null
        Register-NotificationTrigger -Name "remove-me" `
            -EventType "ev" -Tier "urgent" -Template "t2" -TriggersFile $tf | Out-Null

        Unregister-NotificationTrigger -Name "remove-me" -TriggersFile $tf | Out-Null

        $config = Read-TestTriggersFile -Path $tf
        $config.triggers.ContainsKey("keep-me") | Should -Be $true
        $config.triggers.ContainsKey("remove-me") | Should -Be $false
    }
}

Describe "Get-RegisteredTriggers" {

    It "lists all triggers" {
        $tf = New-TempTriggersFile
        Register-NotificationTrigger -Name "t1" -EventType "e1" -Tier "urgent" -Template "tpl1" -TriggersFile $tf | Out-Null
        Register-NotificationTrigger -Name "t2" -EventType "e2" -Tier "action" -Template "tpl2" -TriggersFile $tf | Out-Null
        Register-NotificationTrigger -Name "t3" -EventType "e3" -Tier "feature" -Template "tpl3" -Enabled $false -TriggersFile $tf | Out-Null

        $triggers = Get-RegisteredTriggers -TriggersFile $tf
        $triggers.Count | Should -Be 3
    }

    It "filters to enabled only when -EnabledOnly is set" {
        $tf = New-TempTriggersFile
        Register-NotificationTrigger -Name "enabled1" -EventType "e" -Tier "urgent" -Template "t" -Enabled $true -TriggersFile $tf | Out-Null
        Register-NotificationTrigger -Name "disabled1" -EventType "e" -Tier "action" -Template "t" -Enabled $false -TriggersFile $tf | Out-Null

        $triggers = Get-RegisteredTriggers -TriggersFile $tf -EnabledOnly
        $triggers.Count | Should -Be 1
        $triggers[0].Name | Should -Be "enabled1"
    }

    It "returns empty array when no triggers registered" {
        $tf = New-TempTriggersFile
        $triggers = Get-RegisteredTriggers -TriggersFile $tf
        @($triggers).Count | Should -Be 0
    }

    It "returns correct properties" {
        $tf = New-TempTriggersFile
        Register-NotificationTrigger -Name "props-test" -EventType "myevent" -Tier "action" -Template "mytemplate" -TriggersFile $tf | Out-Null

        $triggers = Get-RegisteredTriggers -TriggersFile $tf
        $t = $triggers | Where-Object { $_.Name -eq "props-test" }
        $t.EventType | Should -Be "myevent"
        $t.Tier | Should -Be "action"
        $t.Template | Should -Be "mytemplate"
        $t.Enabled | Should -Be $true
    }
}

Describe "Config load/save/hot-reload" {

    It "creates config file on first register" {
        $tf = New-TempTriggersFile
        Test-Path $tf | Should -Be $false
        Register-NotificationTrigger -Name "first" -EventType "e" -Tier "urgent" -Template "t" -TriggersFile $tf | Out-Null
        Test-Path $tf | Should -Be $true
    }

    It "persists version field" {
        $tf = New-TempTriggersFile
        Register-NotificationTrigger -Name "ver-test" -EventType "e" -Tier "urgent" -Template "t" -TriggersFile $tf | Out-Null
        $config = Read-TestTriggersFile -Path $tf
        $config.version | Should -Be 1
    }

    It "survives corrupt config file (returns empty)" {
        $tf = New-TempTriggersFile
        Set-Content -Path $tf -Value "NOT_JSON{{{{"
        $config = Read-TriggersConfig -TriggersFile $tf -Force
        $config.triggers.Count | Should -Be 0
    }

    It "hot-reloads when file changes between calls" {
        $tf = New-TempTriggersFile
        Register-NotificationTrigger -Name "before" -EventType "e" -Tier "urgent" -Template "t" -TriggersFile $tf | Out-Null

        # Directly modify the file to add another trigger (simulating external edit)
        $config = Read-TestTriggersFile -Path $tf
        $config.triggers["external"] = @{
            eventType = "ext"; tier = "action"; template = "ext-t"; enabled = $true
            createdAt = (Get-Date -Format 'o')
        }
        # Force a newer timestamp
        Start-Sleep -Milliseconds 100
        $config | ConvertTo-Json -Depth 10 | Set-Content $tf -Encoding UTF8

        # Read again — should pick up external change
        $reloaded = Read-TriggersConfig -TriggersFile $tf -Force
        $reloaded.triggers.ContainsKey("external") | Should -Be $true
    }

    It "returns empty triggers for missing config file" {
        $tf = Join-Path $script:TempDir "nonexistent-$(New-Guid).json"
        $config = Read-TriggersConfig -TriggersFile $tf
        $config.triggers.Count | Should -Be 0
    }
}

Describe "Build-EventFromTemplate" {

    Context "icm-urgent template" {
        It "builds urgent event with incident details" {
            $data = @{
                incidentCount = 2
                highlight     = "Sev2 recurring issue"
                incidents     = @(
                    @{ Sev = "Sev2"; Type = "LiveSite"; IcmId = "123"; Title = "API Errors" }
                    @{ Sev = "Sev2"; Type = "CRI"; IcmId = "456"; Title = "Data Loss" }
                )
                actionUrl = "https://example.com/incidents"
            }
            $result = Build-EventFromTemplate -Template "icm-urgent" -Tier "urgent" -EventData $data
            $result.title | Should -Match "2 new sev2\+"
            $result.reason | Should -Match "IcM#123"
            $result.reason | Should -Match "IcM#456"
            $result.errorType | Should -Be "livesite"
            $result.actionUrl | Should -Be "https://example.com/incidents"
        }
    }

    Context "icm-action template" {
        It "builds action event for ICM findings" {
            $data = @{ findingCount = 3; summary = "3 items need attention" }
            $result = Build-EventFromTemplate -Template "icm-action" -Tier "action" -EventData $data
            $result.title | Should -Match "3 finding"
            $result.reason | Should -Match "3 items"
        }
    }

    Context "pr-review template" {
        It "builds action event for PR review" {
            $data = @{
                reviewStatus = "approved"
                prNumber     = 42
                prTitle      = "Auth Flow"
                reviewer     = "Galadriel"
                actionUrl    = "https://github.com/org/repo/pull/42"
            }
            $result = Build-EventFromTemplate -Template "pr-review" -Tier "action" -EventData $data
            $result.title | Should -Match "PR #42.*approved.*Auth Flow"
            $result.reason | Should -Match "Galadriel"
            $result.currentState | Should -Be "approved"
        }
    }

    Context "build-failure template" {
        It "builds urgent event for build failure" {
            $data = @{
                buildId   = "build-789"
                buildName = "CI Pipeline"
                error     = "Test suite failed: 3 failures"
            }
            $result = Build-EventFromTemplate -Template "build-failure" -Tier "urgent" -EventData $data
            $result.title | Should -Match "Build/Test Failure.*CI Pipeline"
            $result.reason | Should -Match "3 failures"
            $result.errorType | Should -Be "script-failure"
        }
    }

    Context "feature-complete template" {
        It "builds feature event with test instructions" {
            $data = @{
                featureId        = "auth-flow"
                featureTitle     = "Auth Flow Complete"
                summary          = "Token caching works"
                testInstructions = "1. Run login"
            }
            $result = Build-EventFromTemplate -Template "feature-complete" -Tier "feature" -EventData $data
            $result.featureTitle | Should -Be "Auth Flow Complete"
            $result.summary | Should -Be "Token caching works"
            $result.testInstructions | Should -Be "1. Run login"
        }
    }

    Context "ralph-round template" {
        It "builds feature event with work items" {
            $data = @{
                roundId   = "round-5"
                workItems = @(
                    @{ status = "✅"; title = "Fix auth" }
                    @{ status = "🔄"; title = "Update docs" }
                )
            }
            $result = Build-EventFromTemplate -Template "ralph-round" -Tier "feature" -EventData $data
            $result.featureTitle | Should -Be "Ralph Round Complete"
            $result.summary | Should -Match "Fix auth"
            $result.summary | Should -Match "Update docs"
        }
    }

    Context "unknown template (passthrough)" {
        It "passes through raw event data with generated eventId" {
            $data = @{ title = "Custom"; reason = "Test" }
            $result = Build-EventFromTemplate -Template "custom-unknown" -Tier "urgent" -EventData $data
            $result.title | Should -Be "Custom"
            $result.reason | Should -Be "Test"
            $result.eventId | Should -Match "custom-unknown:"
        }
    }
}

Describe "Invoke-ScheduledNotification" {

    It "fires notification for registered enabled trigger via DryRun" {
        $tf = New-TempTriggersFile
        Register-NotificationTrigger -Name "invoke-test" `
            -EventType "test" -Tier "urgent" -Template "build-failure" `
            -TriggersFile $tf | Out-Null

        $result = Invoke-ScheduledNotification -EventName "invoke-test" `
            -EventData @{ buildId = "b1"; buildName = "CI"; error = "fail" } `
            -TriggersFile $tf -NotifyScript $script:NotifyScript -DryRun -Force
        $result.Sent | Should -Be $true
        $result.Tier | Should -Be "urgent"
        $result.Template | Should -Be "build-failure"
    }

    It "skips notification for disabled trigger" {
        $tf = New-TempTriggersFile
        Register-NotificationTrigger -Name "disabled-invoke" `
            -EventType "test" -Tier "action" -Template "pr-review" `
            -Enabled $false -TriggersFile $tf | Out-Null

        $result = Invoke-ScheduledNotification -EventName "disabled-invoke" `
            -EventData @{ prNumber = 1; prTitle = "T"; reviewStatus = "ok" } `
            -TriggersFile $tf -NotifyScript $script:NotifyScript -DryRun
        $result.Sent | Should -Be $false
        $result.Reason | Should -Be "disabled"
    }

    It "returns unknown-trigger for unregistered event" {
        $tf = New-TempTriggersFile
        $result = Invoke-ScheduledNotification -EventName "ghost-event" `
            -EventData @{ foo = "bar" } -TriggersFile $tf -NotifyScript $script:NotifyScript
        $result.Sent | Should -Be $false
        $result.Reason | Should -Be "unknown-trigger"
    }

    It "routes icm-scan-urgent to urgent tier" {
        $tf = New-TempTriggersFile
        Register-NotificationTrigger -Name "icm-scan-urgent" `
            -EventType "icm-scan" -Tier "urgent" -Template "icm-urgent" `
            -TriggersFile $tf | Out-Null

        $result = Invoke-ScheduledNotification -EventName "icm-scan-urgent" `
            -EventData @{
                incidentCount = 1
                highlight     = "New sev2"
                incidents     = @(@{ Sev = "Sev2"; Type = "LiveSite"; IcmId = "999"; Title = "Outage" })
            } `
            -TriggersFile $tf -NotifyScript $script:NotifyScript -DryRun -Force
        $result.Sent | Should -Be $true
        $result.Tier | Should -Be "urgent"
    }

    It "routes pr-review-complete to action tier" {
        $tf = New-TempTriggersFile
        Register-NotificationTrigger -Name "pr-review-complete" `
            -EventType "pr-review" -Tier "action" -Template "pr-review" `
            -TriggersFile $tf | Out-Null

        $result = Invoke-ScheduledNotification -EventName "pr-review-complete" `
            -EventData @{ prNumber = 42; prTitle = "Auth"; reviewStatus = "approved"; reviewer = "Bot" } `
            -TriggersFile $tf -NotifyScript $script:NotifyScript -DryRun -Force
        $result.Sent | Should -Be $true
        $result.Tier | Should -Be "action"
    }

    It "routes feature-complete to feature tier" {
        $tf = New-TempTriggersFile
        Register-NotificationTrigger -Name "feature-complete" `
            -EventType "feature" -Tier "feature" -Template "feature-complete" `
            -TriggersFile $tf | Out-Null

        $result = Invoke-ScheduledNotification -EventName "feature-complete" `
            -EventData @{ featureTitle = "Auth Flow"; summary = "Done"; featureId = "auth" } `
            -TriggersFile $tf -NotifyScript $script:NotifyScript -DryRun -Force
        $result.Sent | Should -Be $true
        $result.Tier | Should -Be "feature"
    }

    It "routes build-test-failure to urgent tier" {
        $tf = New-TempTriggersFile
        Register-NotificationTrigger -Name "build-test-failure" `
            -EventType "build" -Tier "urgent" -Template "build-failure" `
            -TriggersFile $tf | Out-Null

        $result = Invoke-ScheduledNotification -EventName "build-test-failure" `
            -EventData @{ buildName = "CI"; error = "compile error"; buildId = "b5" } `
            -TriggersFile $tf -NotifyScript $script:NotifyScript -DryRun -Force
        $result.Sent | Should -Be $true
        $result.Tier | Should -Be "urgent"
    }

    It "routes ralph-round-complete to feature tier" {
        $tf = New-TempTriggersFile
        Register-NotificationTrigger -Name "ralph-round-complete" `
            -EventType "ralph" -Tier "feature" -Template "ralph-round" `
            -TriggersFile $tf | Out-Null

        $result = Invoke-ScheduledNotification -EventName "ralph-round-complete" `
            -EventData @{
                roundId   = "r1"
                workItems = @(@{ status = "✅"; title = "Fix bug" })
            } `
            -TriggersFile $tf -NotifyScript $script:NotifyScript -DryRun -Force
        $result.Sent | Should -Be $true
        $result.Tier | Should -Be "feature"
    }
}

Describe "Initialize-DefaultTriggers" {

    It "registers all 6 default triggers" {
        $tf = New-TempTriggersFile
        Initialize-DefaultTriggers -TriggersFile $tf
        $config = Read-TestTriggersFile -Path $tf

        $config.triggers.ContainsKey("icm-scan-urgent") | Should -Be $true
        $config.triggers.ContainsKey("icm-scan-action") | Should -Be $true
        $config.triggers.ContainsKey("pr-review-complete") | Should -Be $true
        $config.triggers.ContainsKey("build-test-failure") | Should -Be $true
        $config.triggers.ContainsKey("feature-complete") | Should -Be $true
        $config.triggers.ContainsKey("ralph-round-complete") | Should -Be $true
    }

    It "does not overwrite existing triggers" {
        $tf = New-TempTriggersFile
        Register-NotificationTrigger -Name "icm-scan-urgent" `
            -EventType "custom" -Tier "action" -Template "custom-t" -TriggersFile $tf | Out-Null

        Initialize-DefaultTriggers -TriggersFile $tf

        $config = Read-TestTriggersFile -Path $tf
        # Original custom registration should be preserved
        $config.triggers["icm-scan-urgent"].eventType | Should -Be "custom"
        $config.triggers["icm-scan-urgent"].template | Should -Be "custom-t"
    }

    It "is idempotent (second call registers 0)" {
        $tf = New-TempTriggersFile
        Initialize-DefaultTriggers -TriggersFile $tf
        $output = Initialize-DefaultTriggers -TriggersFile $tf *>&1 | Out-String
        $output | Should -Match "All default triggers already registered"
    }
}

Describe "Edge cases" {

    It "handles duplicate trigger registration gracefully" {
        $tf = New-TempTriggersFile
        Register-NotificationTrigger -Name "dup" -EventType "e" -Tier "urgent" -Template "t1" -TriggersFile $tf | Out-Null
        Register-NotificationTrigger -Name "dup" -EventType "e" -Tier "action" -Template "t2" -TriggersFile $tf | Out-Null

        $config = Read-TestTriggersFile -Path $tf
        ($config.triggers.Keys | Where-Object { $_ -eq "dup" }).Count | Should -Be 1
        $config.triggers["dup"].tier | Should -Be "action"
    }

    It "handles missing config file gracefully on Get-RegisteredTriggers" {
        $tf = Join-Path $script:TempDir "missing-$(New-Guid).json"
        $triggers = Get-RegisteredTriggers -TriggersFile $tf
        @($triggers).Count | Should -Be 0
    }

    It "handles empty event data in Invoke-ScheduledNotification" {
        $tf = New-TempTriggersFile
        Register-NotificationTrigger -Name "empty-data" `
            -EventType "test" -Tier "urgent" -Template "build-failure" `
            -TriggersFile $tf | Out-Null

        # Should not throw — uses defaults from template
        $result = Invoke-ScheduledNotification -EventName "empty-data" `
            -EventData @{} `
            -TriggersFile $tf -NotifyScript $script:NotifyScript -DryRun -Force
        $result.Sent | Should -Be $true
    }

    It "handles concurrent register + unregister without data loss" {
        $tf = New-TempTriggersFile
        Register-NotificationTrigger -Name "a" -EventType "e" -Tier "urgent" -Template "t" -TriggersFile $tf | Out-Null
        Register-NotificationTrigger -Name "b" -EventType "e" -Tier "action" -Template "t" -TriggersFile $tf | Out-Null
        Register-NotificationTrigger -Name "c" -EventType "e" -Tier "feature" -Template "t" -TriggersFile $tf | Out-Null

        Unregister-NotificationTrigger -Name "b" -TriggersFile $tf | Out-Null

        $config = Read-TestTriggersFile -Path $tf
        $config.triggers.ContainsKey("a") | Should -Be $true
        $config.triggers.ContainsKey("b") | Should -Be $false
        $config.triggers.ContainsKey("c") | Should -Be $true
    }
}

Describe "Integration: event → notification sent (mock webhook)" {

    It "full pipeline: register trigger → invoke → notify.ps1 DryRun produces card" {
        $tf = New-TempTriggersFile
        Register-NotificationTrigger -Name "integration-test" `
            -EventType "build" -Tier "urgent" -Template "build-failure" `
            -TriggersFile $tf | Out-Null

        $result = Invoke-ScheduledNotification -EventName "integration-test" `
            -EventData @{
                buildId   = "int-build-1"
                buildName = "Integration CI"
                error     = "17 test failures in auth module"
                actionUrl = "https://github.com/org/repo/actions/runs/123"
            } `
            -TriggersFile $tf -NotifyScript $script:NotifyScript -DryRun -Force

        $result.Sent | Should -Be $true
        $result.Output | Should -Match "DRY RUN.*urgent"
        $result.Output | Should -Match "Integration CI"
        $result.Output | Should -Match "17 test failures"
    }

    It "full pipeline: ICM scan → urgent notification" {
        $tf = New-TempTriggersFile
        Register-NotificationTrigger -Name "icm-urgent-int" `
            -EventType "icm-scan" -Tier "urgent" -Template "icm-urgent" `
            -TriggersFile $tf | Out-Null

        $result = Invoke-ScheduledNotification -EventName "icm-urgent-int" `
            -EventData @{
                incidentCount = 2
                highlight     = "Sev2 API failures recurring"
                incidents     = @(
                    @{ Sev = "Sev2"; Type = "LiveSite"; IcmId = "111"; Title = "API Timeout" }
                    @{ Sev = "Sev2"; Type = "CRI"; IcmId = "222"; Title = "Data Corruption" }
                )
                actionUrl = "https://portal.microsofticm.com"
            } `
            -TriggersFile $tf -NotifyScript $script:NotifyScript -DryRun -Force

        $result.Sent | Should -Be $true
        $result.Output | Should -Match "2 new sev2\+"
        $result.Output | Should -Match "IcM#111"
        $result.Output | Should -Match "IcM#222"
    }

    It "full pipeline: PR review → action notification" {
        $tf = New-TempTriggersFile
        Register-NotificationTrigger -Name "pr-int" `
            -EventType "pr-review" -Tier "action" -Template "pr-review" `
            -TriggersFile $tf | Out-Null

        $result = Invoke-ScheduledNotification -EventName "pr-int" `
            -EventData @{
                prNumber     = 99
                prTitle      = "Scheduler Integration"
                reviewStatus = "approved"
                reviewer     = "Galadriel"
                actionUrl    = "https://github.com/org/repo/pull/99"
            } `
            -TriggersFile $tf -NotifyScript $script:NotifyScript -DryRun -Force

        $result.Sent | Should -Be $true
        $result.Output | Should -Match "PR #99.*approved"
        $result.Output | Should -Match "Galadriel"
    }

    It "full pipeline: Ralph round → feature notification" {
        $tf = New-TempTriggersFile
        Register-NotificationTrigger -Name "ralph-int" `
            -EventType "ralph" -Tier "feature" -Template "ralph-round" `
            -TriggersFile $tf | Out-Null

        $result = Invoke-ScheduledNotification -EventName "ralph-int" `
            -EventData @{
                roundId   = "r-99"
                workItems = @(
                    @{ status = "✅"; title = "Built scheduler" }
                    @{ status = "✅"; title = "Wrote tests" }
                    @{ status = "🔄"; title = "PR pending review" }
                )
                issuesUrl = "https://github.com/org/repo/issues"
            } `
            -TriggersFile $tf -NotifyScript $script:NotifyScript -DryRun -Force

        $result.Sent | Should -Be $true
        $result.Output | Should -Match "Ralph Round Complete"
        $result.Output | Should -Match "Built scheduler"
    }
}
