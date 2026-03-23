using System;
using System.Threading;
using System.Threading.Tasks;
using DgrepCli.Auth;
using DgrepCli.Execution;
using Xunit;

namespace DgrepCli.Tests.Auth
{
    /// <summary>
    /// Tests that KustoQueryExecutor properly integrates with IAuthProvider.
    /// </summary>
    public class AuthExecutorIntegrationTests
    {
        [Fact]
        public async Task ExecuteAsync_WithAuthProvider_AttemptsAuthentication()
        {
            // Provider that returns a valid token — executor still throws NotImplemented
            // but only after successful auth
            var runner = new MockProcessRunner(new ProcessResult
            {
                ExitCode = 0,
                StdOut = @"{
                    ""accessToken"": ""test-token"",
                    ""expiresOn"": ""2030-01-01T00:00:00+00:00"",
                    ""subscription"": ""sub"",
                    ""tenant"": ""ten""
                }",
                StdErr = ""
            });
            var authProvider = new AzCliAuthProvider(runner);
            var executor = new KustoQueryExecutor(authProvider);
            var options = new QueryOptions { Cluster = "https://test.kusto.windows.net", Database = "TestDb" };

            var ex = await Assert.ThrowsAsync<NotImplementedException>(
                () => executor.ExecuteAsync("test query", options, CancellationToken.None));

            // Verifies auth happened before stub threw
            Assert.Contains("Auth provider 'azcli' is configured and ready", ex.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WithoutAuthProvider_ReportsNoAuth()
        {
            var executor = new KustoQueryExecutor(null);
            var options = new QueryOptions { Cluster = "https://test.kusto.windows.net", Database = "TestDb" };

            var ex = await Assert.ThrowsAsync<NotImplementedException>(
                () => executor.ExecuteAsync("test query", options, CancellationToken.None));

            Assert.Contains("No auth provider configured", ex.Message);
        }

        [Fact]
        public async Task ExecuteAsync_AuthFails_ThrowsQueryAuthException()
        {
            var runner = new MockProcessRunner(new ProcessResult
            {
                ExitCode = 1,
                StdOut = "",
                StdErr = "Please run 'az login' to setup account."
            });
            var authProvider = new AzCliAuthProvider(runner);
            var executor = new KustoQueryExecutor(authProvider);
            var options = new QueryOptions { Cluster = "https://test.kusto.windows.net", Database = "TestDb" };

            var ex = await Assert.ThrowsAsync<QueryAuthException>(
                () => executor.ExecuteAsync("test query", options, CancellationToken.None));

            Assert.Contains("Authentication failed", ex.Message);
            Assert.Contains("dgrep auth", ex.Message);
        }

        [Fact]
        public async Task ExecuteAsync_ExpiredToken_ThrowsQueryAuthException()
        {
            // Return a token that's already expired
            var runner = new MockProcessRunner(new ProcessResult
            {
                ExitCode = 0,
                StdOut = @"{
                    ""accessToken"": ""expired-token"",
                    ""expiresOn"": ""2020-01-01T00:00:00+00:00""
                }",
                StdErr = ""
            });
            var authProvider = new AzCliAuthProvider(runner);
            var executor = new KustoQueryExecutor(authProvider);
            var options = new QueryOptions { Cluster = "https://test.kusto.windows.net", Database = "TestDb" };

            var ex = await Assert.ThrowsAsync<QueryAuthException>(
                () => executor.ExecuteAsync("test query", options, CancellationToken.None));

            Assert.Contains("expired or null", ex.Message);
            Assert.Contains("dgrep auth status", ex.Message);
        }

        [Fact]
        public async Task ExecuteAsync_NoCluster_ThrowsBeforeAuth()
        {
            var executor = new KustoQueryExecutor(new ManagedIdentityAuthProvider());
            var options = new QueryOptions { Cluster = "", Database = "TestDb" };

            await Assert.ThrowsAsync<QueryConnectionException>(
                () => executor.ExecuteAsync("test query", options, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteAsync_NoDatabase_ThrowsBeforeAuth()
        {
            var executor = new KustoQueryExecutor(new ManagedIdentityAuthProvider());
            var options = new QueryOptions { Cluster = "https://test.kusto.windows.net", Database = "" };

            await Assert.ThrowsAsync<ArgumentException>(
                () => executor.ExecuteAsync("test query", options, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteAsync_EmptyQuery_ThrowsBeforeAuth()
        {
            var executor = new KustoQueryExecutor(new ManagedIdentityAuthProvider());
            var options = new QueryOptions { Cluster = "https://test.kusto.windows.net", Database = "TestDb" };

            await Assert.ThrowsAsync<QuerySyntaxException>(
                () => executor.ExecuteAsync("", options, CancellationToken.None));
        }
    }
}
