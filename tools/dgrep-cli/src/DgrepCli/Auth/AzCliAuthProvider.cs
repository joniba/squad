using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DgrepCli.Auth
{
    /// <summary>
    /// Authenticates using `az account get-access-token`. Default and simplest method.
    /// Requires Azure CLI installed and `az login` completed.
    /// </summary>
    public class AzCliAuthProvider : IAuthProvider
    {
        private readonly IProcessRunner _processRunner;

        public string Name => "azcli";

        public AzCliAuthProvider() : this(new DefaultProcessRunner()) { }

        public AzCliAuthProvider(IProcessRunner processRunner)
        {
            _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        }

        public async Task<AuthToken> GetTokenAsync(string resource, CancellationToken ct)
        {
            var args = $"account get-access-token --resource {resource} --output json";
            var result = await _processRunner.RunAsync("az", args, ct);

            if (result.ExitCode != 0)
            {
                var errorMsg = string.IsNullOrWhiteSpace(result.StdErr) ? result.StdOut : result.StdErr;
                if (errorMsg != null && errorMsg.IndexOf("not recognized", StringComparison.OrdinalIgnoreCase) >= 0)
                    throw new AuthException("Azure CLI (az) is not installed or not in PATH. Install from https://aka.ms/installazurecli");
                if (errorMsg != null && errorMsg.IndexOf("az login", StringComparison.OrdinalIgnoreCase) >= 0)
                    throw new AuthException("Not logged in. Run 'az login' first.");
                throw new AuthException($"az account get-access-token failed (exit {result.ExitCode}): {errorMsg}");
            }

            return ParseTokenResponse(result.StdOut);
        }

        public async Task<AuthProviderStatus> ValidateAsync(CancellationToken ct)
        {
            try
            {
                var token = await GetTokenAsync("https://kusto.kusto.windows.net", ct);
                return new AuthProviderStatus
                {
                    IsValid = true,
                    Message = "Azure CLI authentication is working.",
                    Identity = token.DisplayIdentity,
                    TokenExpiry = token.ExpiresOn
                };
            }
            catch (AuthException ex)
            {
                return new AuthProviderStatus
                {
                    IsValid = false,
                    Message = ex.Message
                };
            }
        }

        internal static AuthToken ParseTokenResponse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new AuthException("Empty response from az CLI.");

            JObject obj;
            try
            {
                obj = JObject.Parse(json);
            }
            catch (Exception ex)
            {
                throw new AuthException($"Failed to parse az CLI response: {ex.Message}");
            }

            var accessToken = obj.Value<string>("accessToken");
            if (string.IsNullOrEmpty(accessToken))
                throw new AuthException("No accessToken in az CLI response.");

            var expiresOn = DateTimeOffset.UtcNow.AddHours(1); // default fallback
            var expiresOnStr = obj.Value<string>("expiresOn");
            if (!string.IsNullOrEmpty(expiresOnStr))
            {
                if (DateTimeOffset.TryParse(expiresOnStr, out var parsed))
                    expiresOn = parsed;
            }

            var subscription = obj["subscription"]?.ToString();
            var tenant = obj["tenant"]?.ToString();
            var identity = subscription != null ? $"subscription={subscription}" : null;
            if (tenant != null)
                identity = identity != null ? $"{identity}, tenant={tenant}" : $"tenant={tenant}";

            return new AuthToken
            {
                Token = accessToken,
                ExpiresOn = expiresOn,
                DisplayIdentity = identity ?? "unknown"
            };
        }
    }
}
