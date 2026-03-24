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

            var options = new QueryOptions { Cluster = "test", Database = "db" };
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
            var options = new QueryOptions { Cluster = "test", Database = "db" };
            var result = await executor.ExecuteAsync("q", options, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Empty(result.Rows);
        }

        [Fact]
        public async Task ExecuteAsync_WithException_Throws()
        {
            var executor = new MockQueryExecutor();
            executor.WithException(new QueryConnectionException("cluster", "refused"));

            var options = new QueryOptions { Cluster = "cluster", Database = "db" };
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
                var options = new QueryOptions { Cluster = "test", Database = "db" };
                await Assert.ThrowsAsync<TaskCanceledException>(
                    () => executor.ExecuteAsync("q", options, cts.Token));
            }
        }

        [Fact]
        public async Task ExecuteAsync_TracksExecutionCount()
        {
            var executor = new MockQueryExecutor();
            executor.WithResult(new QueryResult());
            var options = new QueryOptions { Cluster = "test", Database = "db" };

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

    public class KustoQueryExecutorTests
    {
        [Fact]
        public async Task ExecuteAsync_EmptyCluster_ThrowsConnectionException()
        {
            var executor = new KustoQueryExecutor();
            var options = new QueryOptions { Cluster = "", Database = "db" };

            await Assert.ThrowsAsync<QueryConnectionException>(
                () => executor.ExecuteAsync("q", options, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteAsync_NullOptions_ThrowsArgumentNull()
        {
            var executor = new KustoQueryExecutor();

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => executor.ExecuteAsync("q", null, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteAsync_EmptyQuery_ThrowsSyntaxException()
        {
            var executor = new KustoQueryExecutor();
            var options = new QueryOptions { Cluster = "https://cluster", Database = "db" };

            await Assert.ThrowsAsync<QuerySyntaxException>(
                () => executor.ExecuteAsync("", options, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteAsync_ValidInputs_ThrowsNotImplemented()
        {
            var executor = new KustoQueryExecutor();
            var options = new QueryOptions
            {
                Cluster = "https://mycluster.kusto.windows.net",
                Database = "TestDB"
            };

            // The stub should throw NotImplementedException for valid inputs
            await Assert.ThrowsAsync<NotImplementedException>(
                () => executor.ExecuteAsync("StormEvents | take 10", options, CancellationToken.None));
        }
    }

    public class QueryExceptionTests
    {
        [Fact]
        public void QueryConnectionException_ContainsEndpointUrl()
        {
            var ex = new QueryConnectionException("https://test.endpoint.net", "Connection refused");
            Assert.Contains("https://test.endpoint.net", ex.Message);
            Assert.Contains("Connection refused", ex.Message);
            Assert.Contains("Check your network", ex.Message);
            Assert.Equal("https://test.endpoint.net", ex.EndpointUrl);
            Assert.Equal("https://test.endpoint.net", ex.ClusterUrl); // backward compat
        }

        [Fact]
        public void QuerySyntaxException_ContainsQueryAndServerError()
        {
            var ex = new QuerySyntaxException("bad query | where x", "Unexpected token");
            Assert.Contains("Unexpected token", ex.Message);
            Assert.Contains("bad query | where x", ex.Message);
            Assert.Equal("bad query | where x", ex.Query);
            Assert.Equal("Unexpected token", ex.ServerError);
        }

        [Fact]
        public void QueryTimeoutException_ContainsTimeout()
        {
            var ex = new QueryTimeoutException(TimeSpan.FromSeconds(30));
            Assert.Contains("30", ex.Message);
            Assert.Equal(TimeSpan.FromSeconds(30), ex.Timeout);
        }

        [Fact]
        public void QueryAuthException_PointsToAuthStatus()
        {
            var ex = new QueryAuthException("Token expired");
            Assert.Contains("dgrep auth status", ex.Message);
            Assert.Contains("Token expired", ex.Message);
        }

        [Fact]
        public void QueryRateLimitException_ShowsConcurrentLimit()
        {
            var ex = new QueryRateLimitException();
            Assert.Contains("5 concurrent", ex.Message);
            Assert.Contains("Wait and retry", ex.Message);
            Assert.Equal(5, ex.MaxConcurrent);
        }

        [Fact]
        public void QueryRateLimitException_CustomLimit()
        {
            var ex = new QueryRateLimitException(10);
            Assert.Contains("10 concurrent", ex.Message);
            Assert.Equal(10, ex.MaxConcurrent);
        }
    }
}
