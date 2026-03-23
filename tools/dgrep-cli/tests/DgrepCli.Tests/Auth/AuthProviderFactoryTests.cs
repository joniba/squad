using System;
using System.Threading;
using System.Threading.Tasks;
using DgrepCli.Auth;
using DgrepCli.Config;
using Xunit;

namespace DgrepCli.Tests.Auth
{
    public class AuthProviderFactoryTests
    {
        [Fact]
        public void Create_NullConfig_DefaultsToAzCli()
        {
            var provider = AuthProviderFactory.Create(null);
            Assert.IsType<AzCliAuthProvider>(provider);
            Assert.Equal("azcli", provider.Name);
        }

        [Fact]
        public void Create_EmptyAuthMethod_DefaultsToAzCli()
        {
            var config = new DgrepConfig();
            var provider = AuthProviderFactory.Create(config);
            Assert.IsType<AzCliAuthProvider>(provider);
        }

        [Theory]
        [InlineData("azcli")]
        [InlineData("AzCli")]
        [InlineData("AZCLI")]
        [InlineData("az")]
        public void Create_AzCliVariants_ReturnsAzCliProvider(string method)
        {
            var config = new DgrepConfig { AuthMethod = method };
            var provider = AuthProviderFactory.Create(config);
            Assert.IsType<AzCliAuthProvider>(provider);
        }

        [Theory]
        [InlineData("certificate")]
        [InlineData("Certificate")]
        [InlineData("cert")]
        public void Create_CertificateVariants_ReturnsCertProvider(string method)
        {
            var config = new DgrepConfig { AuthMethod = method, CertificatePath = "test.pfx" };
            var provider = AuthProviderFactory.Create(config);
            Assert.IsType<CertificateAuthProvider>(provider);
        }

        [Theory]
        [InlineData("managedidentity")]
        [InlineData("ManagedIdentity")]
        [InlineData("mi")]
        public void Create_ManagedIdentityVariants_ReturnsMiProvider(string method)
        {
            var config = new DgrepConfig { AuthMethod = method };
            var provider = AuthProviderFactory.Create(config);
            Assert.IsType<ManagedIdentityAuthProvider>(provider);
        }

        [Fact]
        public void Create_UnknownMethod_Throws()
        {
            var config = new DgrepConfig { AuthMethod = "kerberos" };
            var ex = Assert.Throws<AuthException>(() => AuthProviderFactory.Create(config));
            Assert.Contains("kerberos", ex.Message);
            Assert.Contains("Valid values", ex.Message);
        }

        [Fact]
        public void Create_CliOverride_TakesPrecedence()
        {
            var config = new DgrepConfig { AuthMethod = "certificate", CertificatePath = "test.pfx" };
            var provider = AuthProviderFactory.Create(config, "azcli");
            Assert.IsType<AzCliAuthProvider>(provider);
        }

        [Fact]
        public void Create_CliOverrideEmpty_UsesConfig()
        {
            var config = new DgrepConfig { AuthMethod = "managedidentity" };
            var provider = AuthProviderFactory.Create(config, "");
            Assert.IsType<ManagedIdentityAuthProvider>(provider);
        }

        [Fact]
        public void Create_CliOverrideNull_UsesConfig()
        {
            var config = new DgrepConfig { AuthMethod = "managedidentity" };
            var provider = AuthProviderFactory.Create(config, null);
            Assert.IsType<ManagedIdentityAuthProvider>(provider);
        }
    }
}
