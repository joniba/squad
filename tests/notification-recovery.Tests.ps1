<#
.SYNOPSIS
    Pester tests for scripts/notification-recovery.ps1 — dead letter queue,
    retry scheduler, health check, and cleanup.
#>

BeforeAll {
    $script:RecoveryScript = Join-Path $PSScriptRoot "..\scripts\notification-recovery.ps1"
    . $script:RecoveryScript

    $script:TempDir = Join-Path $TestDrive "recovery-tests"
    New-Item -Path $script:TempDir -ItemType Directory -Force | Out-Null

    # Helper: create isolated test directories
    function New-TestDirs {
        $id = (New-Guid).ToString().Substring(0, 8)
        $dlDir = Join-Path $script:TempDir "dead-letter-$id"
        $logFile = Join-Path $script:TempDir "retry-$id.log"
        $successFile = Join-Path $script:TempDir "last-success-$id.json"
        New-Item -Path $dlDir -ItemType Directory -Force | Out-Null
        return @{
            DeadLetterDir   = $dlDir
            RetryLog        = $logFile
            LastSuccessFile = $successFile
        }
    }

    # Helper: create a sample notification for dead letter
    function New-SampleNotification {
        param(
            [string]$EventId = "test-event-$(New-Guid)",
            [string]$Tier = "urgent"
        )
        return @{
            Type       = $Tier
            Event      = @{
                eventId = $EventId
                title   = "Test notification"
                reason  = "Unit test"
            }
            Card       = @{
                type        = "message"
                attachments = @(@{
                    contentType = "application/vnd.microsoft.card.adaptive"
                    content     = @{
                        type    = "AdaptiveCard"
                        version = "1.4"
                        body    = @(@{ type = "TextBlock"; text = "Test" })
                    }
                })
            }
            WebhookUrl = "https://outlook.office.com/webhook/test-guid"
            Error      = "Connection refused"
            Attempts   = 4
        }
    }
}

Describe "Write-DeadLetter" {

    It "creates a JSON file in the dead letter directory" {
        $dirs = New-TestDirs
        $notification = New-SampleNotification
        $path = Write-DeadLetter -Notification $notification -DeadLetterDir $dirs.DeadLetterDir

        $path | Should -Not -BeNullOrEmpty
        Test-Path $path | Should -Be $true
        $path | Should -Match "\.json$"
    }

    It "writes valid JSON with required fields" {
        $dirs = New-TestDirs
        $notification = New-SampleNotification -EventId "dl-fields-test"
        $path = Write-DeadLetter -Notification $notification -DeadLetterDir $dirs.DeadLetterDir

        $content = Get-Content $path -Raw | ConvertFrom-Json -AsHashtable
        $content.id | Should -Not -BeNullOrEmpty
        $content.createdAt | Should -Not -BeNullOrEmpty
        $content.tier | Should -Be "urgent"
        $content.eventId | Should -Be "dl-fields-test"
        $content.status | Should -Be "pending"
        $content.retryCycle | Should -Be 0
        $content.attempts | Should -Be 4
        $content.originalError | Should -Be "Connection refused"
    }

    It "includes timestamp and tier in filename" {
        $dirs = New-TestDirs
        $notification = New-SampleNotification -Tier "action"
        $path = Write-DeadLetter -Notification $notification -DeadLetterDir $dirs.DeadLetterDir

        $filename = Split-Path $path -Leaf
        $filename | Should -Match "^\d{8}-\d{6}-\d{3}_action\.json$"
    }

    It "creates the dead letter directory if it doesn't exist" {
        $dirs = New-TestDirs
        $newDir = Join-Path $dirs.DeadLetterDir "nested\subdir"
        $notification = New-SampleNotification
        $path = Write-DeadLetter -Notification $notification -DeadLetterDir $newDir

        Test-Path $newDir | Should -Be $true
        Test-Path $path | Should -Be $true
    }

    It "preserves event and card data in the dead letter entry" {
        $dirs = New-TestDirs
        $notification = New-SampleNotification -EventId "preserve-data-test"
        $path = Write-DeadLetter -Notification $notification -DeadLetterDir $dirs.DeadLetterDir

        $content = Get-Content $path -Raw | ConvertFrom-Json -AsHashtable
        $content.event.eventId | Should -Be "preserve-data-test"
        $content.event.title | Should -Be "Test notification"
        $content.card.type | Should -Be "message"
    }
}

Describe "Get-DeadLetterItems" {

    It "returns empty array when directory is empty" {
        $dirs = New-TestDirs
        $items = Get-DeadLetterItems -DeadLetterDir $dirs.DeadLetterDir
        $items.Count | Should -Be 0
    }

    It "returns all pending items" {
        $dirs = New-TestDirs
        # Write 3 dead letters
        for ($i = 1; $i -le 3; $i++) {
            Write-DeadLetter -Notification (New-SampleNotification -EventId "read-$i") -DeadLetterDir $dirs.DeadLetterDir | Out-Null
            Start-Sleep -Milliseconds 20  # ensure unique filenames
        }

        $items = Get-DeadLetterItems -DeadLetterDir $dirs.DeadLetterDir
        $items.Count | Should -Be 3
    }

    It "filters by status" {
        $dirs = New-TestDirs
        # Write one pending, manually create one succeeded
        Write-DeadLetter -Notification (New-SampleNotification -EventId "filter-pending") -DeadLetterDir $dirs.DeadLetterDir | Out-Null

        $succeededPath = Join-Path $dirs.DeadLetterDir "succeeded-test.json"
        @{
            id        = "succ-1"
            createdAt = (Get-Date -Format 'o')
            tier      = "urgent"
            eventId   = "filter-succeeded"
            status    = "succeeded"
            retryCycle = 1
        } | ConvertTo-Json -Depth 5 | Set-Content $succeededPath -Encoding UTF8

        $pending = Get-DeadLetterItems -DeadLetterDir $dirs.DeadLetterDir -StatusFilter @("pending")
        $pending.Count | Should -Be 1
        $pending[0].eventId | Should -Be "filter-pending"
    }

    It "returns empty when directory doesn't exist" {
        $items = Get-DeadLetterItems -DeadLetterDir (Join-Path $script:TempDir "nonexistent")
        $items.Count | Should -Be 0
    }
}

Describe "Retry-FailedNotifications" {

    Context "Empty queue" {
        It "reports nothing to retry" {
            $dirs = New-TestDirs
            $result = Retry-FailedNotifications -DeadLetterDir $dirs.DeadLetterDir -RetryLog $dirs.RetryLog
            $result.Processed | Should -Be 0
            $result.Succeeded | Should -Be 0
        }
    }

    Context "With mock webhook" {

        It "marks item as permanent-failure after max retry cycles" {
            $dirs = New-TestDirs
            # Create a dead letter with retryCycle already at max
            $dlPath = Join-Path $dirs.DeadLetterDir "max-retry.json"
            @{
                id          = "max-1"
                createdAt   = (Get-Date -Format 'o')
                tier        = "urgent"
                eventId     = "max-retry-test"
                event       = @{ eventId = "max-retry-test"; title = "Max"; reason = "test" }
                card        = @{ type = "message"; attachments = @() }
                webhookUrl  = "https://example.com/webhook"
                originalError = "timeout"
                attempts    = 4
                retryCycle  = 3  # already at max
                lastRetryAt = $null
                status      = "retrying"
            } | ConvertTo-Json -Depth 10 | Set-Content $dlPath -Encoding UTF8

            $result = Retry-FailedNotifications -DeadLetterDir $dirs.DeadLetterDir -RetryLog $dirs.RetryLog
            $result.PermanentFailures | Should -Be 1

            # Verify file updated
            $updated = Get-Content $dlPath -Raw | ConvertFrom-Json -AsHashtable
            $updated.status | Should -Be "permanent-failure"
        }

        It "logs retry attempts to the retry log" {
            $dirs = New-TestDirs
            $dlPath = Join-Path $dirs.DeadLetterDir "log-test.json"
            @{
                id          = "log-1"
                createdAt   = (Get-Date -Format 'o')
                tier        = "action"
                eventId     = "log-retry-test"
                event       = @{ eventId = "log-retry-test"; title = "Log"; reason = "test" }
                card        = @{ type = "message"; attachments = @() }
                webhookUrl  = "https://example.com/webhook"
                originalError = "refused"
                attempts    = 4
                retryCycle  = 3
                lastRetryAt = $null
                status      = "retrying"
            } | ConvertTo-Json -Depth 10 | Set-Content $dlPath -Encoding UTF8

            Retry-FailedNotifications -DeadLetterDir $dirs.DeadLetterDir -RetryLog $dirs.RetryLog | Out-Null

            Test-Path $dirs.RetryLog | Should -Be $true
            $logContent = Get-Content $dirs.RetryLog -Raw
            $logContent | Should -Match "PERMANENT_FAILURE"
            $logContent | Should -Match "log-retry-test"
        }

        It "respects backoff delay and skips items not yet due" {
            $dirs = New-TestDirs
            $dlPath = Join-Path $dirs.DeadLetterDir "backoff-test.json"
            @{
                id          = "back-1"
                createdAt   = (Get-Date -Format 'o')
                tier        = "urgent"
                eventId     = "backoff-test"
                event       = @{ eventId = "backoff-test"; title = "Backoff"; reason = "test" }
                card        = @{ type = "message"; attachments = @() }
                webhookUrl  = "https://example.com/webhook"
                originalError = "timeout"
                attempts    = 4
                retryCycle  = 0
                lastRetryAt = (Get-Date -Format 'o')  # just retried
                status      = "pending"
            } | ConvertTo-Json -Depth 10 | Set-Content $dlPath -Encoding UTF8

            $result = Retry-FailedNotifications -DeadLetterDir $dirs.DeadLetterDir -RetryLog $dirs.RetryLog
            # Should be skipped due to backoff (5 min hasn't passed)
            $result.Processed | Should -Be 0
        }

        It "DryRun logs but doesn't send" {
            $dirs = New-TestDirs
            $dlPath = Join-Path $dirs.DeadLetterDir "dryrun-test.json"
            @{
                id          = "dry-1"
                createdAt   = (Get-Date -Format 'o')
                tier        = "urgent"
                eventId     = "dryrun-event"
                event       = @{ eventId = "dryrun-event"; title = "DryRun"; reason = "test" }
                card        = @{ type = "message"; attachments = @() }
                webhookUrl  = "https://example.com/webhook"
                originalError = "timeout"
                attempts    = 4
                retryCycle  = 0
                lastRetryAt = $null
                status      = "pending"
            } | ConvertTo-Json -Depth 10 | Set-Content $dlPath -Encoding UTF8

            $output = Retry-FailedNotifications -DeadLetterDir $dirs.DeadLetterDir -RetryLog $dirs.RetryLog -DryRun *>&1 | Out-String
            $output | Should -Match "DRY RUN.*dryrun-event"

            # File should still be pending (not changed)
            $item = Get-Content $dlPath -Raw | ConvertFrom-Json -AsHashtable
            $item.status | Should -Be "pending"
        }

        It "increments retryCycle on failed retry attempt" {
            $dirs = New-TestDirs
            $dlPath = Join-Path $dirs.DeadLetterDir "increment-test.json"
            @{
                id          = "inc-1"
                createdAt   = (Get-Date -Format 'o')
                tier        = "urgent"
                eventId     = "increment-test"
                event       = @{ eventId = "increment-test"; title = "Inc"; reason = "test" }
                card        = @{ type = "message"; attachments = @() }
                webhookUrl  = "https://invalid.test.local/webhook-that-does-not-exist"
                originalError = "timeout"
                attempts    = 4
                retryCycle  = 0
                lastRetryAt = $null
                status      = "pending"
            } | ConvertTo-Json -Depth 10 | Set-Content $dlPath -Encoding UTF8

            # This will fail because the URL is invalid
            Retry-FailedNotifications -DeadLetterDir $dirs.DeadLetterDir -RetryLog $dirs.RetryLog -WebhookUrl "https://invalid.test.local/does-not-exist" 2>$null | Out-Null

            $updated = Get-Content $dlPath -Raw | ConvertFrom-Json -AsHashtable
            $updated.retryCycle | Should -Be 1
            $updated.status | Should -Be "retrying"
            $updated.lastRetryAt | Should -Not -BeNullOrEmpty
        }
    }
}

Describe "Test-NotificationHealth" {

    It "returns 'unconfigured' when no webhook URL is available" {
        $dirs = New-TestDirs
        $missingFile = Join-Path $script:TempDir "no-webhook-$(New-Guid).url"
        $health = Test-NotificationHealth -WebhookFile $missingFile -DeadLetterDir $dirs.DeadLetterDir
        $health.status | Should -BeIn @("unconfigured", "degraded")
        $health.webhookReachable | Should -Be $false
    }

    It "reports dead letter queue depth" {
        $dirs = New-TestDirs
        # Write 2 dead letters
        Write-DeadLetter -Notification (New-SampleNotification -EventId "health-1") -DeadLetterDir $dirs.DeadLetterDir | Out-Null
        Start-Sleep -Milliseconds 20
        Write-DeadLetter -Notification (New-SampleNotification -EventId "health-2") -DeadLetterDir $dirs.DeadLetterDir | Out-Null

        $missingFile = Join-Path $script:TempDir "no-webhook-health-$(New-Guid).url"
        $health = Test-NotificationHealth -WebhookFile $missingFile -DeadLetterDir $dirs.DeadLetterDir
        $health.deadLetterCount | Should -Be 2
        $health.pendingRetries | Should -Be 2
    }

    It "reports last success timestamp when available" {
        $dirs = New-TestDirs
        $successTime = "2026-03-24T14:30:00.0000000Z"
        @{ lastSuccessAt = $successTime } | ConvertTo-Json | Set-Content $dirs.LastSuccessFile -Encoding UTF8

        $missingFile = Join-Path $script:TempDir "no-webhook-lastsuccess-$(New-Guid).url"
        $health = Test-NotificationHealth -WebhookFile $missingFile `
            -DeadLetterDir $dirs.DeadLetterDir -LastSuccessFile $dirs.LastSuccessFile
        $health.lastSuccessAt | Should -Not -BeNullOrEmpty
        # ConvertFrom-Json parses ISO to DateTime; verify it round-trips to original date
        ([datetime]$health.lastSuccessAt).Year | Should -Be 2026
        ([datetime]$health.lastSuccessAt).Month | Should -Be 3
        ([datetime]$health.lastSuccessAt).Day | Should -Be 24
    }

    It "reports permanent failures separately" {
        $dirs = New-TestDirs

        # Create one pending, one permanent failure
        Write-DeadLetter -Notification (New-SampleNotification -EventId "health-pending") -DeadLetterDir $dirs.DeadLetterDir | Out-Null

        $permPath = Join-Path $dirs.DeadLetterDir "perm-failure.json"
        @{
            id         = "perm-1"
            createdAt  = (Get-Date -Format 'o')
            tier       = "urgent"
            eventId    = "health-permanent"
            status     = "permanent-failure"
            retryCycle = 3
        } | ConvertTo-Json -Depth 5 | Set-Content $permPath -Encoding UTF8

        $missingFile = Join-Path $script:TempDir "no-webhook-pf-$(New-Guid).url"
        $health = Test-NotificationHealth -WebhookFile $missingFile -DeadLetterDir $dirs.DeadLetterDir
        $health.pendingRetries | Should -Be 1
        $health.permanentFailures | Should -Be 1
        $health.deadLetterCount | Should -Be 2
    }

    It "masks webhook URL in output" {
        $dirs = New-TestDirs
        $webhookFile = Join-Path $script:TempDir "mask-webhook-$(New-Guid).url"
        Set-Content -Path $webhookFile -Value "https://outlook.office.com/webhook/secret-guid-here"

        # This will fail to connect but we're testing URL masking
        $health = Test-NotificationHealth -WebhookFile $webhookFile -DeadLetterDir $dirs.DeadLetterDir 2>$null
        $health.webhookUrl | Should -Match "outlook\.office\.com"
        $health.webhookUrl | Should -Not -Match "secret-guid-here"
    }

    It "returns structured health object with all expected fields" {
        $dirs = New-TestDirs
        $missingFile = Join-Path $script:TempDir "no-webhook-struct-$(New-Guid).url"
        $health = Test-NotificationHealth -WebhookFile $missingFile -DeadLetterDir $dirs.DeadLetterDir

        $health.Keys | Should -Contain "timestamp"
        $health.Keys | Should -Contain "webhookReachable"
        $health.Keys | Should -Contain "deadLetterCount"
        $health.Keys | Should -Contain "pendingRetries"
        $health.Keys | Should -Contain "permanentFailures"
        $health.Keys | Should -Contain "lastSuccessAt"
        $health.Keys | Should -Contain "status"
        $health.Keys | Should -Contain "details"
    }
}

Describe "Invoke-DeadLetterCleanup" {

    It "removes succeeded items immediately" {
        $dirs = New-TestDirs
        $succPath = Join-Path $dirs.DeadLetterDir "succeeded-cleanup.json"
        @{
            id        = "cleanup-succ-1"
            createdAt = (Get-Date -Format 'o')
            status    = "succeeded"
            eventId   = "cleanup-succeeded"
        } | ConvertTo-Json -Depth 5 | Set-Content $succPath -Encoding UTF8

        $result = Invoke-DeadLetterCleanup -DeadLetterDir $dirs.DeadLetterDir
        $result.Removed | Should -Be 1
        Test-Path $succPath | Should -Be $false
    }

    It "removes permanent failures older than MaxAgeDays" {
        $dirs = New-TestDirs
        $oldPath = Join-Path $dirs.DeadLetterDir "old-permanent.json"
        @{
            id        = "cleanup-old-1"
            createdAt = (Get-Date).AddDays(-35).ToString('o')  # 35 days old
            status    = "permanent-failure"
            eventId   = "cleanup-old"
        } | ConvertTo-Json -Depth 5 | Set-Content $oldPath -Encoding UTF8

        $result = Invoke-DeadLetterCleanup -DeadLetterDir $dirs.DeadLetterDir -MaxAgeDays 30
        $result.Removed | Should -Be 1
        Test-Path $oldPath | Should -Be $false
    }

    It "keeps recent permanent failures" {
        $dirs = New-TestDirs
        $recentPath = Join-Path $dirs.DeadLetterDir "recent-permanent.json"
        @{
            id        = "cleanup-recent-1"
            createdAt = (Get-Date).AddDays(-5).ToString('o')  # 5 days old
            status    = "permanent-failure"
            eventId   = "cleanup-recent"
        } | ConvertTo-Json -Depth 5 | Set-Content $recentPath -Encoding UTF8

        $result = Invoke-DeadLetterCleanup -DeadLetterDir $dirs.DeadLetterDir -MaxAgeDays 30
        $result.Removed | Should -Be 0
        Test-Path $recentPath | Should -Be $true
    }

    It "keeps pending items untouched" {
        $dirs = New-TestDirs
        Write-DeadLetter -Notification (New-SampleNotification -EventId "keep-pending") -DeadLetterDir $dirs.DeadLetterDir | Out-Null

        $result = Invoke-DeadLetterCleanup -DeadLetterDir $dirs.DeadLetterDir
        $result.Removed | Should -Be 0

        $items = Get-DeadLetterItems -DeadLetterDir $dirs.DeadLetterDir
        $items.Count | Should -Be 1
    }

    It "returns zero removed when directory is empty" {
        $dirs = New-TestDirs
        $result = Invoke-DeadLetterCleanup -DeadLetterDir $dirs.DeadLetterDir
        $result.Removed | Should -Be 0
    }

    It "handles mixed statuses correctly" {
        $dirs = New-TestDirs

        # Succeeded → remove
        $s1 = Join-Path $dirs.DeadLetterDir "mix-succeeded.json"
        @{ id = "m1"; createdAt = (Get-Date -Format 'o'); status = "succeeded"; eventId = "mix-s" } |
            ConvertTo-Json | Set-Content $s1 -Encoding UTF8

        # Old permanent → remove
        $s2 = Join-Path $dirs.DeadLetterDir "mix-old-perm.json"
        @{ id = "m2"; createdAt = (Get-Date).AddDays(-40).ToString('o'); status = "permanent-failure"; eventId = "mix-op" } |
            ConvertTo-Json | Set-Content $s2 -Encoding UTF8

        # Recent permanent → keep
        $s3 = Join-Path $dirs.DeadLetterDir "mix-recent-perm.json"
        @{ id = "m3"; createdAt = (Get-Date).AddDays(-2).ToString('o'); status = "permanent-failure"; eventId = "mix-rp" } |
            ConvertTo-Json | Set-Content $s3 -Encoding UTF8

        # Pending → keep
        Write-DeadLetter -Notification (New-SampleNotification -EventId "mix-pending") -DeadLetterDir $dirs.DeadLetterDir | Out-Null

        $result = Invoke-DeadLetterCleanup -DeadLetterDir $dirs.DeadLetterDir -MaxAgeDays 30
        $result.Removed | Should -Be 2  # succeeded + old permanent

        $remaining = Get-ChildItem $dirs.DeadLetterDir -Filter "*.json" -File
        $remaining.Count | Should -Be 2  # recent permanent + pending
    }
}

Describe "Integration: Send failure → Dead letter → Retry" {

    It "end-to-end: write dead letter, read it, dry-run retry, cleanup" {
        $dirs = New-TestDirs

        # Step 1: Simulate send failure → write to dead letter
        $notification = New-SampleNotification -EventId "e2e-integration-test" -Tier "urgent"
        $dlPath = Write-DeadLetter -Notification $notification -DeadLetterDir $dirs.DeadLetterDir

        # Verify written
        Test-Path $dlPath | Should -Be $true
        $item = Get-Content $dlPath -Raw | ConvertFrom-Json -AsHashtable
        $item.status | Should -Be "pending"
        $item.eventId | Should -Be "e2e-integration-test"

        # Step 2: Check health — should show 1 pending
        $missingFile = Join-Path $script:TempDir "no-webhook-e2e-$(New-Guid).url"
        $health = Test-NotificationHealth -WebhookFile $missingFile -DeadLetterDir $dirs.DeadLetterDir
        $health.pendingRetries | Should -Be 1
        $health.status | Should -BeIn @("unconfigured", "degraded")

        # Step 3: Dry-run retry — should report the item
        $output = Retry-FailedNotifications -DeadLetterDir $dirs.DeadLetterDir `
            -RetryLog $dirs.RetryLog -DryRun *>&1 | Out-String
        $output | Should -Match "e2e-integration-test"

        # Step 4: Manually mark as succeeded (simulating successful retry)
        $item.status = "succeeded"
        $item | ConvertTo-Json -Depth 15 | Set-Content $dlPath -Encoding UTF8

        # Step 5: Cleanup removes succeeded items
        $cleanupResult = Invoke-DeadLetterCleanup -DeadLetterDir $dirs.DeadLetterDir
        $cleanupResult.Removed | Should -Be 1
        Test-Path $dlPath | Should -Be $false
    }

    It "full lifecycle: pending → retrying → permanent-failure → aged out" {
        $dirs = New-TestDirs

        # Create pending item
        $dlPath = Join-Path $dirs.DeadLetterDir "lifecycle.json"
        @{
            id          = "lc-1"
            createdAt   = (Get-Date).AddDays(-31).ToString('o')  # old enough to purge after permanent
            tier        = "urgent"
            eventId     = "lifecycle-test"
            event       = @{ eventId = "lifecycle-test"; title = "Lifecycle"; reason = "test" }
            card        = @{ type = "message"; attachments = @() }
            webhookUrl  = "https://example.com/webhook"
            originalError = "timeout"
            attempts    = 4
            retryCycle  = 3  # at max
            lastRetryAt = $null
            status      = "retrying"
        } | ConvertTo-Json -Depth 10 | Set-Content $dlPath -Encoding UTF8

        # Retry should mark as permanent-failure (cycle >= max)
        $result = Retry-FailedNotifications -DeadLetterDir $dirs.DeadLetterDir -RetryLog $dirs.RetryLog
        $result.PermanentFailures | Should -Be 1

        $item = Get-Content $dlPath -Raw | ConvertFrom-Json -AsHashtable
        $item.status | Should -Be "permanent-failure"

        # Cleanup should purge (createdAt is 31 days ago)
        $cleanupResult = Invoke-DeadLetterCleanup -DeadLetterDir $dirs.DeadLetterDir -MaxAgeDays 30
        $cleanupResult.Removed | Should -Be 1
        Test-Path $dlPath | Should -Be $false
    }
}

Describe "Update-LastSuccess" {

    It "creates a last-success marker file" {
        $dirs = New-TestDirs
        Update-LastSuccess -LastSuccessFile $dirs.LastSuccessFile

        Test-Path $dirs.LastSuccessFile | Should -Be $true
        $content = Get-Content $dirs.LastSuccessFile -Raw | ConvertFrom-Json
        $content.lastSuccessAt | Should -Not -BeNullOrEmpty
    }

    It "updates timestamp on subsequent calls" {
        $dirs = New-TestDirs
        Update-LastSuccess -LastSuccessFile $dirs.LastSuccessFile
        $first = (Get-Content $dirs.LastSuccessFile -Raw | ConvertFrom-Json).lastSuccessAt

        Start-Sleep -Milliseconds 50
        Update-LastSuccess -LastSuccessFile $dirs.LastSuccessFile
        $second = (Get-Content $dirs.LastSuccessFile -Raw | ConvertFrom-Json).lastSuccessAt

        $second | Should -Not -Be $first
    }
}

Describe "Write-RetryLog" {

    It "creates log file and writes entry" {
        $dirs = New-TestDirs
        Write-RetryLog -RetryLog $dirs.RetryLog -Message "TEST_LOG" -EventId "log-test-1" -Cycle 0

        Test-Path $dirs.RetryLog | Should -Be $true
        $content = Get-Content $dirs.RetryLog -Raw
        $content | Should -Match "TEST_LOG"
        $content | Should -Match "log-test-1"
    }

    It "appends multiple entries" {
        $dirs = New-TestDirs
        Write-RetryLog -RetryLog $dirs.RetryLog -Message "ENTRY_1" -EventId "multi-1" -Cycle 0
        Write-RetryLog -RetryLog $dirs.RetryLog -Message "ENTRY_2" -EventId "multi-2" -Cycle 1

        $lines = Get-Content $dirs.RetryLog
        $lines.Count | Should -Be 2
    }

    It "includes error when provided" {
        $dirs = New-TestDirs
        Write-RetryLog -RetryLog $dirs.RetryLog -Message "ERROR_ENTRY" -EventId "err-1" -Cycle 0 -Error "connection refused"

        $content = Get-Content $dirs.RetryLog -Raw
        $content | Should -Match "connection refused"
    }
}
