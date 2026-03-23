using System;
using System.Threading;
using System.Threading.Tasks;
using DgrepCli.Auth;
using Xunit;

namespace DgrepCli.Tests.Auth
{
    /// <summary>
    /// Mock process runner for testing AzCliAuthProvider without actually running az CLI.
    /// </summary>
    public class MockProcessRunner : IProcessRunner
    {
        private readonly ProcessResult _result;
        private readonly Exception _exception;

        public string LastFileName { get; private set; }
        public string LastArguments { get; private set; }

        public MockProcessRunner(ProcessResult result)
        {
            _result = result;
        }

        public MockProcessRunner(Exception exception)
        {
            _exception = exception;
        }

        public Task<ProcessResult> RunAsync(string fileName, string arguments, CancellationToken ct)
        {
            LastFileName = fileName;
            LastArguments = arguments;

            if (_exception != null)
                throw _exception;

            return Task.FromResult(_result);
        }
    }

    public class AzCliAuthProviderTests
    {
        private const string ValidTokenJson = @"{
            ""accessToken"": ""eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9.test"",
            ""expiresOn"": ""2030-12-31T23:59:59+00:00"",
            ""subscription"": ""11111111-2222-3333-4444-555555555555"",
            ""tenant"": ""aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"",
            ""tokenType"": ""Bearer""
        }";

        [Fact]
        public void Name_ReturnsAzcli()
        {
            var provider = new AzCliAuthProvider();
            Assert.Equal("azcli", provider.Name);
        }

        [Fact]
        public async Task GetTokenAsync_SuccessfulResponse_ReturnsToken()
        {
            var runner = new MockProcessRunner(new ProcessResult
            {
                ExitCode = 0,
                StdOut = ValidTokenJson,
                StdErr = ""
            });
            var provider = new AzCliAuthProvider(runner);

            var token = await provider.GetTokenAsync("https://kusto.kusto.windows.net", CancellationToken.None);

            Assert.NotNull(token);
            Assert.StartsWith("eyJ0eXAi", token.Token);
            Assert.Contains("subscription=", token.DisplayIdentity);
            Assert.Contains("tenant=", token.DisplayIdentity);
            Assert.False(token.IsExpired);
        }

        [Fact]
        public async Task GetTokenAsync_CallsAzWithCorrectArgs()
        {
            var runner = new MockProcessRunner(new ProcessResult
            {
                ExitCode = 0,
                StdOut = ValidTokenJson,
                StdErr = ""
            });
            var provider = new AzCliAuthProvider(runner);

            await provider.GetTokenAsync("https://kusto.kusto.windows.net", CancellationToken.None);

            Assert.Equal("az", runner.LastFileName);
            Assert.Contains("account get-access-token", runner.LastArguments);
            Assert.Contains("--resource https://kusto.kusto.windows.net", runner.LastArguments);
            Assert.Contains("--output json", runner.LastArguments);
        }

        [Fact]
        public async Task GetTokenAsync_AzNotInstalled_ThrowsWithInstallHint()
        {
            var runner = new MockProcessRunner(new ProcessResult
            {
                ExitCode = -1,
                StdOut = "",
                StdErr = "'az' is not recognized as an internal or external command"
            });
            var provider = new AzCliAuthProvider(runner);

            var ex = await Assert.ThrowsAsync<AuthException>(
                () => provider.GetTokenAsync("https://kusto.kusto.windows.net", CancellationToken.None));
            Assert.Contains("not installed", ex.Message);
            Assert.Contains("https://aka.ms/installazurecli", ex.Message);
        }

        [Fact]
        public async Task GetTokenAsync_NotLoggedIn_ThrowsWithLoginHint()
        {
            var runner = new MockProcessRunner(new ProcessResult
            {
                ExitCode = 1,
                StdOut = "",
                StdErr = "Please run 'az login' to setup account."
            });
            var provider = new AzCliAuthProvider(runner);

            var ex = await Assert.ThrowsAsync<AuthException>(
                () => provider.GetTokenAsync("https://kusto.kusto.windows.net", CancellationToken.None));
            Assert.Contains("az login", ex.Message);
        }

        [Fact]
        public async Task GetTokenAsync_GenericFailure_ThrowsWithExitCode()
        {
            var runner = new MockProcessRunner(new ProcessResult
            {
                ExitCode = 2,
                StdOut = "",
                StdErr = "Some unexpected error occurred"
            });
            var provider = new AzCliAuthProvider(runner);

            var ex = await Assert.ThrowsAsync<AuthException>(
                () => provider.GetTokenAsync("https://kusto.kusto.windows.net", CancellationToken.None));
            Assert.Contains("exit 2", ex.Message);
        }

        [Fact]
        public async Task GetTokenAsync_EmptyResponse_Throws()
        {
            var runner = new MockProcessRunner(new ProcessResult
            {
                ExitCode = 0,
                StdOut = "",
                StdErr = ""
            });
            var provider = new AzCliAuthProvider(runner);

            var ex = await Assert.ThrowsAsync<AuthException>(
                () => provider.GetTokenAsync("https://kusto.kusto.windows.net", CancellationToken.None));
            Assert.Contains("Empty response", ex.Message);
        }

        [Fact]
        public async Task GetTokenAsync_InvalidJson_Throws()
        {
            var runner = new MockProcessRunner(new ProcessResult
            {
                ExitCode = 0,
                StdOut = "not json",
                StdErr = ""
            });
            var provider = new AzCliAuthProvider(runner);

            var ex = await Assert.ThrowsAsync<AuthException>(
                () => provider.GetTokenAsync("https://kusto.kusto.windows.net", CancellationToken.None));
            Assert.Contains("Failed to parse", ex.Message);
        }

        [Fact]
        public async Task GetTokenAsync_MissingAccessToken_Throws()
        {
            var runner = new MockProcessRunner(new ProcessResult
            {
                ExitCode = 0,
                StdOut = @"{ ""subscription"": ""test"" }",
                StdErr = ""
            });
            var provider = new AzCliAuthProvider(runner);

            var ex = await Assert.ThrowsAsync<AuthException>(
                () => provider.GetTokenAsync("https://kusto.kusto.windows.net", CancellationToken.None));
            Assert.Contains("No accessToken", ex.Message);
        }

        [Fact]
        public void ParseTokenResponse_ValidJson_ParsesAllFields()
        {
            var token = AzCliAuthProvider.ParseTokenResponse(ValidTokenJson);

            Assert.Equal("eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9.test", token.Token);
            Assert.Contains("11111111-2222-3333-4444-555555555555", token.DisplayIdentity);
            Assert.Contains("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", token.DisplayIdentity);
            Assert.False(token.IsExpired); // 2030 expiry
        }

        [Fact]
        public void ParseTokenResponse_NoExpiry_DefaultsToOneHour()
        {
            var json = @"{ ""accessToken"": ""test-token"" }";
            var token = AzCliAuthProvider.ParseTokenResponse(json);

            Assert.Equal("test-token", token.Token);
            // Should default to ~1 hour from now
            Assert.True(token.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(50));
        }

        [Fact]
        public void ParseTokenResponse_NoSubscription_IdentityShowsTenant()
        {
            var json = @"{ ""accessToken"": ""test"", ""tenant"": ""t1"" }";
            var token = AzCliAuthProvider.ParseTokenResponse(json);
            Assert.Equal("tenant=t1", token.DisplayIdentity);
        }

        [Fact]
        public void ParseTokenResponse_NoSubOrTenant_IdentityUnknown()
        {
            var json = @"{ ""accessToken"": ""test"" }";
            var token = AzCliAuthProvider.ParseTokenResponse(json);
            Assert.Equal("unknown", token.DisplayIdentity);
        }

        [Fact]
        public async Task ValidateAsync_Success_ReturnsValid()
        {
            var runner = new MockProcessRunner(new ProcessResult
            {
                ExitCode = 0,
                StdOut = ValidTokenJson,
                StdErr = ""
            });
            var provider = new AzCliAuthProvider(runner);

            var status = await provider.ValidateAsync(CancellationToken.None);

            Assert.True(status.IsValid);
            Assert.Contains("working", status.Message);
            Assert.NotNull(status.Identity);
            Assert.True(status.TokenExpiry.HasValue);
        }

        [Fact]
        public async Task ValidateAsync_Failure_ReturnsInvalid()
        {
            var runner = new MockProcessRunner(new ProcessResult
            {
                ExitCode = 1,
                StdOut = "",
                StdErr = "Please run 'az login' to setup account."
            });
            var provider = new AzCliAuthProvider(runner);

            var status = await provider.ValidateAsync(CancellationToken.None);

            Assert.False(status.IsValid);
            Assert.Contains("az login", status.Message);
        }

        [Fact]
        public void AuthToken_IsExpired_WhenPast()
        {
            var token = new AuthToken
            {
                Token = "test",
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                DisplayIdentity = "test"
            };
            Assert.True(token.IsExpired);
        }

        [Fact]
        public void AuthToken_NotExpired_WhenFuture()
        {
            var token = new AuthToken
            {
                Token = "test",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
                DisplayIdentity = "test"
            };
            Assert.False(token.IsExpired);
        }
    }
}
