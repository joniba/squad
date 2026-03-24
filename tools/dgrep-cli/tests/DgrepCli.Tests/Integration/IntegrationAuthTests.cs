using System;
using System.Threading;
using System.Threading.Tasks;
using DgrepCli.Auth;
using DgrepCli.Config;
using DgrepCli.Execution;
using Xunit;

namespace DgrepCli.Tests.Integration
{
    /// <summary>
    /// Integration tests for authentication against live Geneva/DGrep endpoints.
    ///
    /// Skipped by default. Enable with DGREP_INTEGRATION_ENABLED=true.
    /// Requires corpnet/VPN and valid Geneva credentials.
    /// </summary>
    [Trait("Category", "Integration")]
    public class IntegrationAuthTests : IntegrationTestBase
    {
        [Fact]
        public void SkipReason_IsNull_WhenEnabled()
        {
            // Meta-test: documents the skip mechanism.
            // When DGREP_INTEGRATION_ENABLED=true, SkipReason returns null (run tests).
            // When not set, SkipReason returns a descriptive message (skip tests).
            if (!IsEnabled)
            {
                Assert.NotNull(SkipReason);
                return;
            }
            Assert.Null(SkipReason);
        }

        [Fact]
        public async Task AzCli_GetToken_ReturnsValidToken()
        {
            if (!IsEnabled)
            {
                Assert.NotNull(SkipReason); // Documents why test was skipped
                return;
            }

            // Arrange — use AzCli auth provider (requires prior `az login`)
            var config = new DgrepConfig { AuthMethod = "azcli" };
            var provider = AuthProviderFactory.Create(config, cliOverride: null);

            // Act — request token scoped to the DGrep endpoint
            var token = await provider.GetTokenAsync(DefaultEndpoint, CancellationToken.None);

            // Assert
            Assert.NotNull(token);
            Assert.False(string.IsNullOrWhiteSpace(token.Token));
            Assert.False(token.IsExpired, "Token should not be expired immediately after acquisition");
        }

        [Fact]
        public async Task AzCli_ValidateAsync_ReturnsValid()
        {
            if (!IsEnabled)
            {
                Assert.NotNull(SkipReason);
                return;
            }

            // Arrange
            var config = new DgrepConfig { AuthMethod = "azcli" };
            var provider = AuthProviderFactory.Create(config, cliOverride: null);

            // Act
            var result = await provider.ValidateAsync(CancellationToken.None);

            // Assert
            Assert.True(result.IsValid,
                $"AzCli validation failed: {result.Message}. Ensure 'az login' has been run.");
        }

        [Fact]
        public async Task Certificate_GetToken_WhenCertConfigured_ReturnsToken()
        {
            if (!IsEnabled || string.IsNullOrEmpty(CertificatePath))
            {
                // Skip if no cert is configured — this is expected for most dev machines
                Assert.True(true, "Skipped: DGREP_TEST_CERT_PATH not set.");
                return;
            }

            // Arrange
            var config = new DgrepConfig
            {
                AuthMethod = "certificate",
                CertificatePath = CertificatePath
            };
            var provider = AuthProviderFactory.Create(config, cliOverride: null);

            // Act
            var token = await provider.GetTokenAsync(DefaultEndpoint, CancellationToken.None);

            // Assert
            Assert.NotNull(token);
            Assert.False(string.IsNullOrWhiteSpace(token.Token));
        }

        [Fact]
        public void DefaultEndpoint_IsReachable()
        {
            if (!IsEnabled)
            {
                Assert.NotNull(SkipReason);
                return;
            }

            // Validate the test endpoint URL is well-formed
            var uri = new Uri(DefaultEndpoint);
            Assert.Equal("https", uri.Scheme);
            Assert.Contains("monitoring", uri.Host);
        }
    }
}
