using System;
using System.Threading;
using System.Threading.Tasks;
using DgrepCli.Auth;
using DgrepCli.Execution;
using Xunit;

namespace DgrepCli.Tests.Auth
{
    /// <summary>
    /// Tests that DgrepQueryExecutor validates inputs correctly.
    /// Note: The DGrep SDK handles its own auth (dSTS) internally,
    /// so the executor has no IAuthProvider dependency.
    /// These tests verify input validation before any SDK call would happen.
    /// </summary>
    public class AuthExecutorIntegrationTests
    {
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

            var ex = await Assert.ThrowsAsync<NotImplementedException>(
                () => executor.ExecuteAsync("source | take 10", options, CancellationToken.None));

            Assert.Contains("DGrep SDK", ex.Message);
        }

        [Fact]
        public async Task ExecuteAsync_NullOptions_ThrowsArgumentNull()
        {
            var executor = new DgrepQueryExecutor();

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => executor.ExecuteAsync("source | take 10", null, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteAsync_EmptyEndpoint_ThrowsConnectionException()
        {
            var executor = new DgrepQueryExecutor();
            var options = new QueryOptions
            {
                Endpoint = "",
                Namespace = "TestNs",
                Event = "Log"
            };

            await Assert.ThrowsAsync<QueryConnectionException>(
                () => executor.ExecuteAsync("source | take 10", options, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteAsync_NullEndpoint_ThrowsConnectionException()
        {
            var executor = new DgrepQueryExecutor();
            var options = new QueryOptions
            {
                Endpoint = null,
                Namespace = "TestNs",
                Event = "Log"
            };

            await Assert.ThrowsAsync<QueryConnectionException>(
                () => executor.ExecuteAsync("source | take 10", options, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteAsync_EmptyNamespace_ThrowsArgumentException()
        {
            var executor = new DgrepQueryExecutor();
            var options = new QueryOptions
            {
                Endpoint = "https://production.diagnostics.monitoring.core.windows.net/",
                Namespace = "",
                Event = "Log"
            };

            await Assert.ThrowsAsync<ArgumentException>(
                () => executor.ExecuteAsync("source | take 10", options, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteAsync_EmptyQuery_ThrowsArgumentException()
        {
            var executor = new DgrepQueryExecutor();
            var options = new QueryOptions
            {
                Endpoint = "https://production.diagnostics.monitoring.core.windows.net/",
                Namespace = "TestNs",
                Event = "Log"
            };

            await Assert.ThrowsAsync<ArgumentException>(
                () => executor.ExecuteAsync("", options, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteAsync_WhitespaceQuery_ThrowsArgumentException()
        {
            var executor = new DgrepQueryExecutor();
            var options = new QueryOptions
            {
                Endpoint = "https://production.diagnostics.monitoring.core.windows.net/",
                Namespace = "TestNs",
                Event = "Log"
            };

            await Assert.ThrowsAsync<ArgumentException>(
                () => executor.ExecuteAsync("   ", options, CancellationToken.None));
        }
    }
}
