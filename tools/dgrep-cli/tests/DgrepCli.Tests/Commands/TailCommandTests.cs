using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using DgrepCli.Commands;
using DgrepCli.Execution;

namespace DgrepCli.Tests.Commands
{
    public class TailCommandTests : IDisposable
    {
        private readonly MockQueryExecutor _executor;
        private readonly StringWriter _stdout;
        private readonly StringWriter _stderr;

        public TailCommandTests()
        {
            _executor = new MockQueryExecutor();
            _stdout = new StringWriter();
            _stderr = new StringWriter();
        }

        public void Dispose()
        {
            _stdout.Dispose();
            _stderr.Dispose();
        }

        private TailCommand CreateCommand()
        {
            return new TailCommand(_executor, _stdout, _stderr);
        }

        private static TailOptions DefaultOpts(int interval = 1)
        {
            return new TailOptions
            {
                Endpoint = "diag-prod",
                Namespace = "MyNamespace",
                Event = "MyEvent",
                From = "-5m",
                To = "now",
                Query = "where Level <= 2",
                QueryType = "kql",
                MaxRows = 500000,
                Output = "table",
                Interval = interval
            };
        }

        // ── Basic execution ──────────────────────────────────────────

        [Fact]
        public void Execute_SinglePollThenCancel_ReturnsZero()
        {
            _executor.WithResult(MockQueryExecutor.CreateSimpleResult(
                new[] { "Timestamp", "Message" },
                new[] { "string", "string" },
                new[] { new object[] { "2026-03-28T10:00:00Z", "test message" } }));

            var opts = DefaultOpts();
            using (var cts = new CancellationTokenSource())
            {
                // Cancel after first poll completes (before delay)
                _executor.WithResultFactory(callIndex =>
                {
                    if (callIndex > 0) cts.Cancel();
                    return MockQueryExecutor.CreateSimpleResult(
                        new[] { "Timestamp", "Message" },
                        new[] { "string", "string" },
                        callIndex == 0
                            ? new[] { new object[] { "2026-03-28T10:00:00Z", "first" } }
                            : new object[0][]);
                });

                var exitCode = CreateCommand().Execute(opts, cts.Token);
                Assert.Equal(0, exitCode);
            }
        }

        [Fact]
        public void Execute_EmptyResults_StillShowsStatusLine()
        {
            using (var cts = new CancellationTokenSource())
            {
                int callCount = 0;
                _executor.WithResultFactory(idx =>
                {
                    callCount++;
                    if (callCount >= 2) cts.Cancel();
                    return new QueryResult(); // empty
                });

                var exitCode = CreateCommand().Execute(DefaultOpts(), cts.Token);

                Assert.Equal(0, exitCode);
                Assert.Contains("Watching...", _stderr.ToString());
                Assert.Contains("0 results so far", _stderr.ToString());
            }
        }

        // ── Executor called with updated time ranges ────────────────

        [Fact]
        public void Execute_MultiplePollCycles_UpdatesFromTime()
        {
            using (var cts = new CancellationTokenSource())
            {
                int callCount = 0;
                _executor.WithResultFactory(idx =>
                {
                    callCount++;
                    if (callCount >= 3) cts.Cancel();
                    return MockQueryExecutor.CreateSimpleResult(
                        new[] { "Col" },
                        new[] { "string" },
                        new[] { new object[] { $"row-{idx}" } });
                });

                CreateCommand().Execute(DefaultOpts(), cts.Token);

                // Should have been called at least 2 times
                Assert.True(_executor.ExecutionCount >= 2,
                    $"Expected at least 2 executions but got {_executor.ExecutionCount}");

                // First call uses original from
                Assert.Equal("-5m", _executor.AllOptions[0].Parameters["_from"]);

                // Subsequent calls should have ISO 8601 timestamps (not the original relative time)
                for (int i = 1; i < _executor.AllOptions.Count; i++)
                {
                    var from = _executor.AllOptions[i].Parameters["_from"];
                    Assert.NotEqual("-5m", from);
                    // Should be parseable as DateTime (ISO 8601)
                    Assert.True(DateTime.TryParse(from, out _),
                        $"Expected ISO 8601 timestamp but got '{from}'");
                }
            }
        }

        [Fact]
        public void Execute_QueryPassedToExecutor()
        {
            using (var cts = new CancellationTokenSource())
            {
                _executor.WithResultFactory(idx =>
                {
                    cts.Cancel();
                    return new QueryResult();
                });

                var opts = DefaultOpts();
                opts.Query = "where Level == 1 | project Message";
                CreateCommand().Execute(opts, cts.Token);

                Assert.Equal("where Level == 1 | project Message", _executor.LastQuery);
                Assert.True(_executor.AllQueries.All(q => q == opts.Query));
            }
        }

        // ── Graceful cancellation ───────────────────────────────────

        [Fact]
        public void Execute_ImmediateCancel_ReturnsZeroWithSummary()
        {
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel(); // pre-cancel

                var exitCode = CreateCommand().Execute(DefaultOpts(), cts.Token);

                Assert.Equal(0, exitCode);
                Assert.Contains("Stopped.", _stderr.ToString());
                Assert.Contains("Total results: 0", _stderr.ToString());
            }
        }

        [Fact]
        public void Execute_CancelDuringDelay_StopsGracefully()
        {
            using (var cts = new CancellationTokenSource())
            {
                _executor.WithResultFactory(idx =>
                {
                    // Cancel after first poll — should break out during the delay
                    if (idx == 0)
                    {
                        Task.Run(async () =>
                        {
                            await Task.Delay(50);
                            cts.Cancel();
                        });
                    }
                    return MockQueryExecutor.CreateSimpleResult(
                        new[] { "X" }, new[] { "int" },
                        new[] { new object[] { idx } });
                });

                var exitCode = CreateCommand().Execute(DefaultOpts(interval: 60), cts.Token);

                Assert.Equal(0, exitCode);
                Assert.Contains("Stopped.", _stderr.ToString());
            }
        }

        [Fact]
        public void Execute_CancelDuringExecution_StopsGracefully()
        {
            using (var cts = new CancellationTokenSource())
            {
                // Add delay to the executor so cancellation happens mid-query
                _executor.WithDelay(TimeSpan.FromSeconds(5));
                _executor.WithResult(new QueryResult());

                // Cancel quickly
                Task.Run(async () =>
                {
                    await Task.Delay(50);
                    cts.Cancel();
                });

                var exitCode = CreateCommand().Execute(DefaultOpts(), cts.Token);

                Assert.Equal(0, exitCode);
                Assert.Contains("Stopped.", _stderr.ToString());
            }
        }

        // ── Poll interval configuration ─────────────────────────────

        [Fact]
        public void Execute_CustomInterval_RespectedBetweenPolls()
        {
            using (var cts = new CancellationTokenSource())
            {
                var timestamps = new System.Collections.Generic.List<DateTime>();
                _executor.WithResultFactory(idx =>
                {
                    timestamps.Add(DateTime.UtcNow);
                    if (idx >= 2) cts.Cancel();
                    return new QueryResult();
                });

                // 1-second interval — each poll should be ~1s apart
                CreateCommand().Execute(DefaultOpts(interval: 1), cts.Token);

                // At least 2 calls to measure gap
                if (timestamps.Count >= 2)
                {
                    var gap = timestamps[1] - timestamps[0];
                    // Should be approximately 1s (allow 0.5s–3s tolerance for test runner jitter)
                    Assert.True(gap.TotalMilliseconds >= 500,
                        $"Gap between polls was only {gap.TotalMilliseconds}ms, expected ~1000ms");
                }
            }
        }

        [Fact]
        public void Execute_MinimumInterval_ClampedToOneSecond()
        {
            using (var cts = new CancellationTokenSource())
            {
                _executor.WithResultFactory(idx =>
                {
                    cts.Cancel();
                    return new QueryResult();
                });

                var opts = DefaultOpts(interval: 0); // below minimum
                var exitCode = CreateCommand().Execute(opts, cts.Token);

                // Should not crash — interval is clamped to 1s internally
                Assert.Equal(0, exitCode);
            }
        }

        // ── Output formatting ───────────────────────────────────────

        [Fact]
        public void Execute_TableOutput_PrintsResults()
        {
            using (var cts = new CancellationTokenSource())
            {
                _executor.WithResultFactory(idx =>
                {
                    cts.Cancel();
                    return MockQueryExecutor.CreateSimpleResult(
                        new[] { "Name", "Value" },
                        new[] { "string", "int" },
                        new[] { new object[] { "test-row", 42 } });
                });

                var opts = DefaultOpts();
                opts.Output = "table";
                CreateCommand().Execute(opts, cts.Token);

                Assert.Contains("test-row", _stdout.ToString());
                Assert.Contains("42", _stdout.ToString());
            }
        }

        [Fact]
        public void Execute_JsonOutput_PrintsJson()
        {
            using (var cts = new CancellationTokenSource())
            {
                _executor.WithResultFactory(idx =>
                {
                    cts.Cancel();
                    return MockQueryExecutor.CreateSimpleResult(
                        new[] { "Id" },
                        new[] { "int" },
                        new[] { new object[] { 99 } });
                });

                var opts = DefaultOpts();
                opts.Output = "json";
                CreateCommand().Execute(opts, cts.Token);

                Assert.Contains("\"Id\"", _stdout.ToString());
                Assert.Contains("99", _stdout.ToString());
            }
        }

        [Fact]
        public void Execute_CsvOutput_PrintsCsv()
        {
            using (var cts = new CancellationTokenSource())
            {
                _executor.WithResultFactory(idx =>
                {
                    cts.Cancel();
                    return MockQueryExecutor.CreateSimpleResult(
                        new[] { "A", "B" },
                        new[] { "string", "int" },
                        new[] { new object[] { "hello", 7 } });
                });

                var opts = DefaultOpts();
                opts.Output = "csv";
                CreateCommand().Execute(opts, cts.Token);

                Assert.Contains("A,B", _stdout.ToString());
                Assert.Contains("hello", _stdout.ToString());
            }
        }

        [Fact]
        public void Execute_InvalidOutputFormat_ReturnsError()
        {
            using (var cts = new CancellationTokenSource())
            {
                var opts = DefaultOpts();
                opts.Output = "xml"; // unsupported
                var exitCode = CreateCommand().Execute(opts, cts.Token);

                Assert.Equal(1, exitCode);
                Assert.Contains("Unknown", _stderr.ToString());
            }
        }

        // ── Status line ─────────────────────────────────────────────

        [Fact]
        public void Execute_StatusLine_ShowsResultCount()
        {
            using (var cts = new CancellationTokenSource())
            {
                int callCount = 0;
                _executor.WithResultFactory(idx =>
                {
                    callCount++;
                    if (callCount >= 2) cts.Cancel();
                    return MockQueryExecutor.CreateSimpleResult(
                        new[] { "X" }, new[] { "int" },
                        new[] { new object[] { 1 }, new object[] { 2 } });
                });

                CreateCommand().Execute(DefaultOpts(), cts.Token);

                var stderr = _stderr.ToString();
                Assert.Contains("Watching...", stderr);
                // First poll: 2 results
                Assert.Contains("2 results so far", stderr);
            }
        }

        [Fact]
        public void Execute_StatusLine_AccumulatesAcrossPolls()
        {
            using (var cts = new CancellationTokenSource())
            {
                _executor.WithResultFactory(idx =>
                {
                    if (idx >= 2) cts.Cancel();
                    return MockQueryExecutor.CreateSimpleResult(
                        new[] { "X" }, new[] { "int" },
                        new[] { new object[] { idx } }); // 1 row per poll
                });

                CreateCommand().Execute(DefaultOpts(), cts.Token);

                var stderr = _stderr.ToString();
                // After first poll: 1 result, after second: 2 results
                Assert.Contains("1 results so far", stderr);
                Assert.Contains("2 results so far", stderr);
            }
        }

        [Fact]
        public void Execute_StopSummary_ShowsTotalResults()
        {
            using (var cts = new CancellationTokenSource())
            {
                _executor.WithResultFactory(idx =>
                {
                    // Return 3 rows on first call, then cancel on second call
                    if (idx >= 1)
                    {
                        cts.Cancel();
                        return new QueryResult();
                    }
                    return MockQueryExecutor.CreateSimpleResult(
                        new[] { "X" }, new[] { "int" },
                        new[] { new object[] { 1 }, new object[] { 2 }, new object[] { 3 } });
                });

                CreateCommand().Execute(DefaultOpts(), cts.Token);

                var stderr = _stderr.ToString();
                Assert.Contains("Stopped.", stderr);
                Assert.Contains("Total results: 3", stderr);
            }
        }

        // ── QueryOptions mapping ────────────────────────────────────

        [Fact]
        public void Execute_PassesDgrepParametersToExecutor()
        {
            using (var cts = new CancellationTokenSource())
            {
                _executor.WithResultFactory(idx =>
                {
                    cts.Cancel();
                    return new QueryResult();
                });

                var opts = DefaultOpts();
                opts.Endpoint = "diag-int";
                opts.Namespace = "TestNs";
                opts.Event = "TestEvent";
                opts.QueryType = "mql";
                CreateCommand().Execute(opts, cts.Token);

                var p = _executor.LastOptions.Parameters;
                Assert.Equal("diag-int", p["_endpoint"]);
                Assert.Equal("TestNs", p["_namespace"]);
                Assert.Equal("TestEvent", p["_event"]);
                Assert.Equal("mql", p["_queryType"]);
            }
        }

        [Fact]
        public void Execute_IdentityColumns_PassedAsParameter()
        {
            using (var cts = new CancellationTokenSource())
            {
                _executor.WithResultFactory(idx =>
                {
                    cts.Cancel();
                    return new QueryResult();
                });

                var opts = DefaultOpts();
                opts.Identity = new[] { "Tenant=WUS", "Role=FE" };
                CreateCommand().Execute(opts, cts.Token);

                Assert.Contains("Tenant=WUS", _executor.LastOptions.Parameters["_identity"]);
                Assert.Contains("Role=FE", _executor.LastOptions.Parameters["_identity"]);
            }
        }

        [Fact]
        public void Execute_VersionFilter_PassedAsParameter()
        {
            using (var cts = new CancellationTokenSource())
            {
                _executor.WithResultFactory(idx =>
                {
                    cts.Cancel();
                    return new QueryResult();
                });

                var opts = DefaultOpts();
                opts.Version = "^Ver2v0$";
                CreateCommand().Execute(opts, cts.Token);

                Assert.Equal("^Ver2v0$", _executor.LastOptions.Parameters["_version"]);
            }
        }

        // ── Error handling ──────────────────────────────────────────

        [Fact]
        public void Execute_QueryException_ReturnsError()
        {
            _executor.WithException(new QueryConnectionException("diag-prod", "Connection refused"));

            using (var cts = new CancellationTokenSource())
            {
                var exitCode = CreateCommand().Execute(DefaultOpts(), cts.Token);

                Assert.Equal(1, exitCode);
                Assert.Contains("Connection refused", _stderr.ToString());
            }
        }

        [Fact]
        public void Execute_AuthException_ReturnsError()
        {
            _executor.WithException(new QueryAuthException("Token expired"));

            using (var cts = new CancellationTokenSource())
            {
                var exitCode = CreateCommand().Execute(DefaultOpts(), cts.Token);

                Assert.Equal(1, exitCode);
                Assert.Contains("dgrep auth", _stderr.ToString());
            }
        }

        [Fact]
        public void Execute_NullOpts_ThrowsArgNull()
        {
            using (var cts = new CancellationTokenSource())
            {
                Assert.Throws<ArgumentNullException>(() =>
                    CreateCommand().Execute(null, cts.Token));
            }
        }

        // ── BuildQueryOptions unit tests ────────────────────────────

        [Fact]
        public void BuildQueryOptions_MapsAllFields()
        {
            var opts = DefaultOpts();
            opts.Endpoint = "my-endpoint";
            opts.Namespace = "my-ns";
            opts.Event = "my-event";
            opts.MaxRows = 1000;
            opts.QueryType = "mql";

            var qo = TailCommand.BuildQueryOptions(opts, "-5m");

            Assert.Equal("my-endpoint", qo.Cluster);
            Assert.Equal(1000, qo.MaxRows);
            Assert.Equal("-5m", qo.Parameters["_from"]);
            Assert.Equal("now", qo.Parameters["_to"]);
            Assert.Equal("my-endpoint", qo.Parameters["_endpoint"]);
            Assert.Equal("my-ns", qo.Parameters["_namespace"]);
            Assert.Equal("my-event", qo.Parameters["_event"]);
            Assert.Equal("mql", qo.Parameters["_queryType"]);
        }

        [Fact]
        public void BuildQueryOptions_NoVersion_OmitsVersionParam()
        {
            var opts = DefaultOpts();
            opts.Version = null;

            var qo = TailCommand.BuildQueryOptions(opts, "-5m");

            Assert.False(qo.Parameters.ContainsKey("_version"));
        }

        [Fact]
        public void BuildQueryOptions_WithVersion_IncludesVersionParam()
        {
            var opts = DefaultOpts();
            opts.Version = "^v1$";

            var qo = TailCommand.BuildQueryOptions(opts, "-5m");

            Assert.Equal("^v1$", qo.Parameters["_version"]);
        }

        // ── MockQueryExecutor enhancements ──────────────────────────

        [Fact]
        public async Task MockExecutor_WithResults_ReturnsSequentially()
        {
            var r1 = MockQueryExecutor.CreateSimpleResult(
                new[] { "A" }, new[] { "int" }, new[] { new object[] { 1 } });
            var r2 = MockQueryExecutor.CreateSimpleResult(
                new[] { "A" }, new[] { "int" }, new[] { new object[] { 2 } });

            _executor.WithResults(r1, r2);

            var result1 = await _executor.ExecuteAsync("q", new QueryOptions(), CancellationToken.None);
            var result2 = await _executor.ExecuteAsync("q", new QueryOptions(), CancellationToken.None);
            var result3 = await _executor.ExecuteAsync("q", new QueryOptions(), CancellationToken.None);

            Assert.Same(r1, result1);
            Assert.Same(r2, result2);
            Assert.Empty(result3.Rows); // exhausted → empty
        }

        [Fact]
        public async Task MockExecutor_WithResultFactory_CallsWithIndex()
        {
            _executor.WithResultFactory(idx =>
                MockQueryExecutor.CreateSimpleResult(
                    new[] { "Idx" }, new[] { "int" },
                    new[] { new object[] { idx * 10 } }));

            var r0 = await _executor.ExecuteAsync("q", new QueryOptions(), CancellationToken.None);
            var r1 = await _executor.ExecuteAsync("q", new QueryOptions(), CancellationToken.None);

            Assert.Equal(0, r0.Rows[0][0]);
            Assert.Equal(10, r1.Rows[0][0]);
        }

        [Fact]
        public async Task MockExecutor_AllQueries_TracksAllCalls()
        {
            _executor.WithResult(new QueryResult());

            await _executor.ExecuteAsync("q1", new QueryOptions(), CancellationToken.None);
            await _executor.ExecuteAsync("q2", new QueryOptions(), CancellationToken.None);

            Assert.Equal(2, _executor.AllQueries.Count);
            Assert.Equal("q1", _executor.AllQueries[0]);
            Assert.Equal("q2", _executor.AllQueries[1]);
            Assert.Equal(2, _executor.AllOptions.Count);
        }
    }
}
