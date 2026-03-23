using System;
using System.Threading;
using System.Threading.Tasks;

namespace DgrepCli.Auth
{
    /// <summary>
    /// Authenticates using Azure Managed Identity. For running in Azure VMs, AKS, App Service.
    /// Stub with clear integration point — real implementation requires Azure.Identity SDK.
    /// </summary>
    public class ManagedIdentityAuthProvider : IAuthProvider
    {
        public string Name => "managedidentity";

        public Task<AuthToken> GetTokenAsync(string resource, CancellationToken ct)
        {
            // Integration point:
            //   var credential = new ManagedIdentityCredential();
            //   var tokenRequest = new TokenRequestContext(new[] { resource + "/.default" });
            //   var accessToken = await credential.GetTokenAsync(tokenRequest, ct);
            //   return new AuthToken { Token = accessToken.Token, ExpiresOn = accessToken.ExpiresOn, ... };
            throw new NotImplementedException(
                "Managed Identity authentication requires Azure.Identity SDK. " +
                "Install Microsoft.Azure.Identity NuGet and implement " +
                "ManagedIdentityCredential.GetTokenAsync(). " +
                "This provider is for Azure-hosted environments (VMs, AKS, App Service).");
        }

        public Task<AuthProviderStatus> ValidateAsync(CancellationToken ct)
        {
            return Task.FromResult(new AuthProviderStatus
            {
                IsValid = false,
                Message = "Managed Identity is only available in Azure-hosted environments " +
                          "(VMs, AKS, App Service). Cannot validate from this machine. " +
                          "Integration point ready — install Azure.Identity SDK to enable."
            });
        }
    }
}
