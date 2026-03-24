using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using DgrepCli.Execution;

namespace DgrepCli.Tests.Execution
{
    public class MockQueryExecutorTests
    {
        [Fact]
        public async Task ExecuteAsync_WithResult_ReturnsConfiguredResult()
        {
            var executor = new MockQueryExecutor();
            var expected = MockQueryExecutor.CreateSimpleResult(
                new[] { "Id" }, new[] { "int" }, new[] { new object[] { 42 } });
            executor.WithResult(expected);

            var options = new QueryOptions
            {
                Endpoint = "https://test.diagnostics.monitoring.core.windows.net/",
                Namespace = "TestNs",
                Event = "Log"
            };
            var result = await executor.ExecuteAsync("test query", options, CancellationToken.None);

            Assert.Same(expected, result);
            Assert.Equal("test query", executor.LastQuery);
            Assert.Same(options, executor.LastOptions);
            Assert.Equal(1, executor.ExecutionCount);
        }

        [Fact]
        public async Task ExecuteAsync_NoResultConfigured_ReturnsEmptyResult()
        {
            var executor = new MockQueryExecutor();
            var options = new QueryOptions
            {
                Endpoint = "https://test.diagnostics.monitoring.core.windows.net/",
                Namespace = "TestNs",
                Event = "Log"
            };
            var result = await executor.ExecuteAsync("q", options, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Empty(result.Rows);
        }

        [Fact]
        public async Task ExecuteAsync_WithException_Throws()
        {
            var executor = new MockQueryExecutor();
            executor.WithException(new QueryConnectionException("https://endpoint", "refused"));

            var options = new QueryOptions
            {
                Endpoint = "https://endpoint",
                Namespace = "TestNs",
                Event = "Log"
            };
            await Assert.ThrowsAsync<QueryConnectionException>(
                () => executor.ExecuteAsync("q", options, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteAsync_CancellationToken_Honored()
        {
            var executor = new MockQueryExecutor();
            executor.WithDelay(TimeSpan.FromSeconds(10));
            executor.WithResult(new QueryResult());

            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();
                var options = new QueryOptions
                {
                    Endpoint = "https://test.diagnostics.monitoring.core.windows.net/",
                    Namespace = "TestNs",
                    Event = "Log"
                };
                await Assert.ThrowsAsync<TaskCanceledException>(
                    () => executor.ExecuteAsync("q", options, cts.Token));
            }
        }

        [Fact]
        public async Task ExecuteAsync_TracksExecutionCount()
        {
            var executor = new MockQueryExecutor();
            executor.WithResult(new QueryResult());
            var options = new QueryOptions
            {
                Endpoint = "https://test.diagnostics.monitoring.core.windows.net/",
                Namespace = "TestNs",
                Event = "Log"
            };

            await executor.ExecuteAsync("q1", options, CancellationToken.None);
            await executor.ExecuteAsync("q2", options, CancellationToken.None);
            await executor.ExecuteAsync("q3", options, CancellationToken.None);

            Assert.Equal(3, executor.ExecutionCount);
            Assert.Equal("q3", executor.LastQuery);
        }

        [Fact]
        public void CreateSimpleResult_BuildsCorrectStructure()
        {
            var result = MockQueryExecutor.CreateSimpleResult(
                new[] { "A", "B", "C" },
                new[] { "string", "int", "bool" },
                new[] {
                    new object[] { "x", 1, true },
                    new object[] { "y", 2, false }
                });

            Assert.Equal(3, result.Columns.Count);
            Assert.Equal("A", result.Columns[0].Name);
            Assert.Equal("int", result.Columns[1].Type);
            Assert.Equal(2, result.Rows.Count);
        }
    }

    public class DgrepQueryExecutorTests
    {
        [Fact]
        public async Task ExecuteAsync_EmptyEndpoint_ThrowsConnectionException()
        {
            var executor = new DgrepQueryExecutor();
            var options = new QueryOptions { Endpoint = "", Namespace = "Ns", Event = "Evt" };

            await Assert.ThrowsAsync<QueryConnectionException>(
                () => executor.ExecuteAsync("q", options, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteAsync_NullOptions_ThrowsArgumentNull()
        {
            var executor = new DgrepQueryExecutor();

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => executor.ExecuteAsync("q", null, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteAsync_EmptyQuery_ThrowsArgumentException()
        {
            var executor = new DgrepQueryExecutor();
            var options = new QueryOptions
            {
                Endpoint = "https://production.diagnostics.monitoring.core.windows.net/",
                Namespace = "Ns",
                Event = "Evt"
            };

            await Assert.ThrowsAsync<ArgumentException>(
                () => executor.ExecuteAsync("", options, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteAsync_MissingNamespace_ThrowsArgumentException()
        {
            var executor = new DgrepQueryExecutor();
            var options = new QueryOptions
            {
                Endpoint = "https://production.diagnostics.monitoring.core.windows.net/",
                Namespace = "",
                Event = "Evt"
            };

            await Assert.ThrowsAsync<ArgumentException>(
                () => executor.ExecuteAsync("source | take 10", options, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteAsync_ValidInputs_ThrowsNotImplemented()
        {
            var executor = new DgrepQueryExecutor();
            var options = new QueryOptions
            {
                Endpoint = "https://production.diagnostics.monitoring.core.windows.net/",
                Namespace = "TestNs",
                Event = "Log"
            };

            // The stub should throw NotImplementedException for valid inputs
            await Assert.ThrowsAsync<NotImplementedException>(
                () => executor.ExecuteAsync("source | take 10", options, CancellationToken.None));
        }
    }

    public class QueryExceptionTests
    {
        [Fact]
        public void QueryConnectionException_ContainsEndpoint()
        {
            var ex = new QueryConnectionException("https://prod.diagnostics.monitoring.core.windows.net/", "Connection refused");
            Assert.Contains("https://prod.diagnostics.monitoring.core.windows.net/", ex.Message);
            Assert.Contains("Connection refused", ex.Message);
            Assert.Equal("https://prod.diagnostics.monitoring.core.windows.net/", ex.Endpoint);
        }

        [Fact]
        public void QuerySyntaxException_ContainsQuery()
        {
            var ex = new QuerySyntaxException("bad query", "Unexpected token");
            Assert.Contains("Unexpected token", ex.Message);
            Assert.Equal("bad query", ex.Query);
        }

        [Fact]
        public void QueryTimeoutException_ContainsTimeout()
        {
            var ex = new QueryTimeoutException(TimeSpan.FromSeconds(30));
            Assert.Contains("30", ex.Message);
            Assert.Equal(TimeSpan.FromSeconds(30), ex.Timeout);
        }

        [Fact]
        public void QueryAuthException_PointsToAuthCommand()
        {
            var ex = new QueryAuthException("Token expired");
            Assert.Contains("dgrep auth", ex.Message);
            Assert.Contains("Token expired", ex.Message);
        }
    }
}
