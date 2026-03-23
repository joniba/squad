using System;
using DgrepCli.Config;

namespace DgrepCli.Auth
{
    /// <summary>
    /// Creates the appropriate IAuthProvider based on config AuthMethod.
    /// </summary>
    public static class AuthProviderFactory
    {
        /// <summary>
        /// Create an auth provider from config. Defaults to AzCli if not specified.
        /// </summary>
        public static IAuthProvider Create(DgrepConfig config)
        {
            return Create(config, null);
        }

        /// <summary>
        /// Create an auth provider. CLI override takes precedence over config.
        /// </summary>
        public static IAuthProvider Create(DgrepConfig config, string cliOverride)
        {
            var method = !string.IsNullOrWhiteSpace(cliOverride)
                ? cliOverride
                : config?.AuthMethod;

            if (string.IsNullOrWhiteSpace(method))
                method = "azcli";

            switch (method.ToLowerInvariant().Trim())
            {
                case "azcli":
                case "az":
                    return new AzCliAuthProvider();

                case "certificate":
                case "cert":
                    return new CertificateAuthProvider(config?.CertificatePath);

                case "managedidentity":
                case "mi":
                    return new ManagedIdentityAuthProvider();

                default:
                    throw new AuthException(
                        $"Unknown auth method '{method}'. " +
                        "Valid values: azcli, certificate, managedidentity. " +
                        "Set via: dgrep config set authMethod <method>");
            }
        }
    }
}
