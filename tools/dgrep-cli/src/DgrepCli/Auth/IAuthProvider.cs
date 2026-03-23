using System;
using System.Threading;
using System.Threading.Tasks;

namespace DgrepCli.Auth
{
    /// <summary>
    /// Token returned by an auth provider.
    /// </summary>
    public class AuthToken
    {
        public string Token { get; set; }
        public DateTimeOffset ExpiresOn { get; set; }
        public string DisplayIdentity { get; set; }

        public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresOn;
    }

    /// <summary>
    /// Abstraction for authentication: obtain a bearer token for Kusto connections.
    /// </summary>
    public interface IAuthProvider
    {
        /// <summary>Name of this auth method (e.g. "azcli", "certificate", "managedidentity").</summary>
        string Name { get; }

        /// <summary>Obtain an access token for the given resource.</summary>
        Task<AuthToken> GetTokenAsync(string resource, CancellationToken ct);

        /// <summary>Validate that this provider is properly configured.</summary>
        Task<AuthProviderStatus> ValidateAsync(CancellationToken ct);
    }

    /// <summary>
    /// Result of validating an auth provider's configuration.
    /// </summary>
    public class AuthProviderStatus
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public string Identity { get; set; }
        public DateTimeOffset? TokenExpiry { get; set; }
    }
}
