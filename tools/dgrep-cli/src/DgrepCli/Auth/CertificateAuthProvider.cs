using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DgrepCli.Auth
{
    /// <summary>
    /// Authenticates using a client certificate. Reads cert path from config CertificatePath.
    /// </summary>
    public class CertificateAuthProvider : IAuthProvider
    {
        private readonly string _certificatePath;

        public string Name => "certificate";

        public CertificateAuthProvider(string certificatePath)
        {
            _certificatePath = certificatePath;
        }

        public Task<AuthToken> GetTokenAsync(string resource, CancellationToken ct)
        {
            ValidateCertificatePath();

            // Integration point: wire up X509Certificate2 + MSAL
            //   var cert = new X509Certificate2(_certificatePath);
            //   var app = ConfidentialClientApplicationBuilder.Create(clientId)
            //       .WithCertificate(cert).WithAuthority(authority).Build();
            //   var result = await app.AcquireTokenForClient(scopes).ExecuteAsync(ct);
            throw new NotImplementedException(
                "Certificate-based token acquisition requires MSAL integration. " +
                "The certificate path has been validated. Wire up " +
                "ConfidentialClientApplicationBuilder with the certificate to complete this provider.");
        }

        public Task<AuthProviderStatus> ValidateAsync(CancellationToken ct)
        {
            try
            {
                ValidateCertificatePath();
                return Task.FromResult(new AuthProviderStatus
                {
                    IsValid = true,
                    Message = $"Certificate found at: {_certificatePath}",
                    Identity = $"cert:{Path.GetFileName(_certificatePath)}"
                });
            }
            catch (AuthException ex)
            {
                return Task.FromResult(new AuthProviderStatus
                {
                    IsValid = false,
                    Message = ex.Message
                });
            }
        }

        internal void ValidateCertificatePath()
        {
            if (string.IsNullOrWhiteSpace(_certificatePath))
                throw new AuthException(
                    "No certificate path configured. Set 'certificatePath' in config: dgrep config set certificatePath /path/to/cert.pfx");

            if (!File.Exists(_certificatePath))
                throw new AuthException(
                    $"Certificate file not found: {_certificatePath}");

            var ext = Path.GetExtension(_certificatePath);
            if (ext != null)
                ext = ext.ToLowerInvariant();
            if (ext != ".pfx" && ext != ".pem" && ext != ".cer" && ext != ".crt")
                throw new AuthException(
                    $"Unsupported certificate format '{ext}'. Expected .pfx, .pem, .cer, or .crt.");
        }
    }
}
