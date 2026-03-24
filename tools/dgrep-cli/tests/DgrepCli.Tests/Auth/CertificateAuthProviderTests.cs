using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DgrepCli.Auth;
using Xunit;

namespace DgrepCli.Tests.Auth
{
    public class CertificateAuthProviderTests
    {
        [Fact]
        public void Name_ReturnsCertificate()
        {
            var provider = new CertificateAuthProvider("/some/path.pfx");
            Assert.Equal("certificate", provider.Name);
        }

        [Fact]
        public void ValidateCertificatePath_NullPath_Throws()
        {
            var provider = new CertificateAuthProvider(null);
            var ex = Assert.Throws<AuthException>(() => provider.ValidateCertificatePath());
            Assert.Contains("No certificate path configured", ex.Message);
            Assert.Contains("dgrep config set certificatePath", ex.Message);
        }

        [Fact]
        public void ValidateCertificatePath_EmptyPath_Throws()
        {
            var provider = new CertificateAuthProvider("");
            var ex = Assert.Throws<AuthException>(() => provider.ValidateCertificatePath());
            Assert.Contains("No certificate path configured", ex.Message);
        }

        [Fact]
        public void ValidateCertificatePath_FileNotFound_Throws()
        {
            var provider = new CertificateAuthProvider("/nonexistent/cert.pfx");
            var ex = Assert.Throws<AuthException>(() => provider.ValidateCertificatePath());
            Assert.Contains("Certificate file not found", ex.Message);
            Assert.Contains("/nonexistent/cert.pfx", ex.Message);
        }

        [Fact]
        public void ValidateCertificatePath_WrongExtension_Throws()
        {
            // Create a temp file with wrong extension
            var tmpFile = Path.GetTempFileName(); // .tmp extension
            try
            {
                var provider = new CertificateAuthProvider(tmpFile);
                var ex = Assert.Throws<AuthException>(() => provider.ValidateCertificatePath());
                Assert.Contains("Unsupported certificate format", ex.Message);
                Assert.Contains(".pfx", ex.Message);
            }
            finally
            {
                File.Delete(tmpFile);
            }
        }

        [Theory]
        [InlineData(".pfx")]
        [InlineData(".pem")]
        [InlineData(".cer")]
        [InlineData(".crt")]
        public void ValidateCertificatePath_ValidExtension_Succeeds(string ext)
        {
            var tmpFile = Path.Combine(Path.GetTempPath(), $"test-cert{ext}");
            File.WriteAllText(tmpFile, "dummy cert content");
            try
            {
                var provider = new CertificateAuthProvider(tmpFile);
                provider.ValidateCertificatePath(); // should not throw
            }
            finally
            {
                File.Delete(tmpFile);
            }
        }

        [Fact]
        public async Task ValidateAsync_ValidCert_ReturnsValid()
        {
            var tmpFile = Path.Combine(Path.GetTempPath(), "test-validate.pfx");
            File.WriteAllText(tmpFile, "dummy");
            try
            {
                var provider = new CertificateAuthProvider(tmpFile);
                var status = await provider.ValidateAsync(CancellationToken.None);

                Assert.True(status.IsValid);
                Assert.Contains("Certificate found", status.Message);
                Assert.Contains("test-validate.pfx", status.Identity);
            }
            finally
            {
                File.Delete(tmpFile);
            }
        }

        [Fact]
        public async Task ValidateAsync_NoCert_ReturnsInvalid()
        {
            var provider = new CertificateAuthProvider(null);
            var status = await provider.ValidateAsync(CancellationToken.None);

            Assert.False(status.IsValid);
            Assert.Contains("No certificate path configured", status.Message);
        }

        [Fact]
        public async Task ValidateAsync_MissingFile_ReturnsInvalid()
        {
            var provider = new CertificateAuthProvider("/no/such/file.pfx");
            var status = await provider.ValidateAsync(CancellationToken.None);

            Assert.False(status.IsValid);
            Assert.Contains("Certificate file not found", status.Message);
        }

        [Fact]
        public async Task GetTokenAsync_ValidCert_ThrowsNotImplemented()
        {
            var tmpFile = Path.Combine(Path.GetTempPath(), "test-token.pfx");
            File.WriteAllText(tmpFile, "dummy");
            try
            {
                var provider = new CertificateAuthProvider(tmpFile);
                await Assert.ThrowsAsync<NotImplementedException>(
                    () => provider.GetTokenAsync("https://management.azure.com/", CancellationToken.None));
            }
            finally
            {
                File.Delete(tmpFile);
            }
        }

        [Fact]
        public async Task GetTokenAsync_NoCert_ThrowsAuthException()
        {
            var provider = new CertificateAuthProvider(null);
            await Assert.ThrowsAsync<AuthException>(
                () => provider.GetTokenAsync("https://management.azure.com/", CancellationToken.None));
        }
    }
}
