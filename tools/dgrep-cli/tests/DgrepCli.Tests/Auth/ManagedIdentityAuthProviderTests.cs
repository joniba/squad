using System;
using System.Threading;
using System.Threading.Tasks;
using DgrepCli.Auth;
using Xunit;

namespace DgrepCli.Tests.Auth
{
    public class ManagedIdentityAuthProviderTests
    {
        [Fact]
        public void Name_ReturnsManagedIdentity()
        {
            var provider = new ManagedIdentityAuthProvider();
            Assert.Equal("managedidentity", provider.Name);
        }

        [Fact]
        public async Task GetTokenAsync_ThrowsNotImplemented()
        {
            var provider = new ManagedIdentityAuthProvider();
            var ex = await Assert.ThrowsAsync<NotImplementedException>(
                () => provider.GetTokenAsync("https://kusto.kusto.windows.net", CancellationToken.None));
            Assert.Contains("Azure.Identity SDK", ex.Message);
            Assert.Contains("Azure-hosted environments", ex.Message);
        }

        [Fact]
        public async Task ValidateAsync_ReturnsInvalid_NotInAzure()
        {
            var provider = new ManagedIdentityAuthProvider();
            var status = await provider.ValidateAsync(CancellationToken.None);

            Assert.False(status.IsValid);
            Assert.Contains("Azure-hosted environments", status.Message);
            Assert.Contains("Integration point ready", status.Message);
        }
    }
}
