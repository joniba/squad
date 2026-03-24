using System;
using System.IO;
using System.Threading;
using DgrepCli.Auth;
using DgrepCli.Config;

namespace DgrepCli.Commands
{
    /// <summary>
    /// Handles "dgrep auth status" and "dgrep auth test" commands.
    /// </summary>
    public class AuthCommand
    {
        private readonly ConfigManager _configManager;
        private readonly TextWriter _stdout;
        private readonly TextWriter _stderr;
        private readonly Func<DgrepConfig, string, IAuthProvider> _providerFactory;

        public AuthCommand(ConfigManager configManager)
            : this(configManager, Console.Out, Console.Error, null)
        {
        }

        public AuthCommand(ConfigManager configManager, TextWriter stdout, TextWriter stderr,
                           Func<DgrepConfig, string, IAuthProvider> providerFactory = null)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _stdout = stdout ?? Console.Out;
            _stderr = stderr ?? Console.Error;
            _providerFactory = providerFactory ?? AuthProviderFactory.Create;
        }

        public int Execute(AuthVerbOptions opts)
        {
            var action = opts.Action?.ToLowerInvariant()?.Trim();
            switch (action)
            {
                case "status": return ExecuteStatus(opts);
                case "test":   return ExecuteTest(opts);
                default:
                    _stderr.WriteLine($"Unknown auth action '{opts.Action}'. Use 'status' or 'test'.");
                    return 1;
            }
        }

        private int ExecuteStatus(AuthVerbOptions opts)
        {
            var config = _configManager.Load();
            IAuthProvider provider;
            try
            {
                provider = _providerFactory(config, opts.AuthMethod);
            }
            catch (AuthException ex)
            {
                _stderr.WriteLine($"Error: {ex.Message}");
                return 1;
            }

            _stdout.WriteLine($"Auth method:  {provider.Name}");
            _stdout.WriteLine($"Config file:  {_configManager.ConfigPath}");

            var configMethod = config.AuthMethod ?? "(not set, defaulting to azcli)";
            _stdout.WriteLine($"Configured:   {configMethod}");

            if (provider.Name == "certificate")
                _stdout.WriteLine($"Cert path:    {config.CertificatePath ?? "(not set)"}");

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
            {
                try
                {
                    var status = provider.ValidateAsync(cts.Token).GetAwaiter().GetResult();

                    _stdout.WriteLine($"Valid:        {(status.IsValid ? "yes" : "no")}");
                    if (status.Identity != null)
                        _stdout.WriteLine($"Identity:     {status.Identity}");
                    if (status.TokenExpiry.HasValue)
                        _stdout.WriteLine($"Token expiry: {status.TokenExpiry.Value:u}");
                    _stdout.WriteLine($"Status:       {status.Message}");

                    return status.IsValid ? 0 : 1;
                }
                catch (Exception ex)
                {
                    _stderr.WriteLine($"Error checking auth status: {ex.Message}");
                    return 1;
                }
            }
        }

        private int ExecuteTest(AuthVerbOptions opts)
        {
            var config = _configManager.Load();
            IAuthProvider provider;
            try
            {
                provider = _providerFactory(config, opts.AuthMethod);
            }
            catch (AuthException ex)
            {
                _stderr.WriteLine($"Error: {ex.Message}");
                return 1;
            }

            _stdout.WriteLine($"Testing authentication with method: {provider.Name}");

            var endpoint = config.DefaultEndpoint;
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                _stdout.WriteLine("No default endpoint configured. Using Azure Management resource URL for token test.");
                endpoint = "https://management.azure.com/";
            }
            else
            {
                _stdout.WriteLine($"Target endpoint: {endpoint}");
            }

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
            {
                try
                {
                    var token = provider.GetTokenAsync(endpoint, cts.Token).GetAwaiter().GetResult();
                    _stdout.WriteLine("Authentication successful!");
                    _stdout.WriteLine($"  Identity:     {token.DisplayIdentity}");
                    _stdout.WriteLine($"  Token expiry: {token.ExpiresOn:u}");
                    _stdout.WriteLine($"  Token length: {token.Token?.Length ?? 0} chars");
                    return 0;
                }
                catch (NotImplementedException ex)
                {
                    _stderr.WriteLine($"Auth method '{provider.Name}' is not fully implemented yet.");
                    _stderr.WriteLine($"  {ex.Message}");
                    return 1;
                }
                catch (AuthException ex)
                {
                    _stderr.WriteLine($"Authentication failed: {ex.Message}");
                    _stderr.WriteLine("Run 'dgrep auth status' for diagnostic information.");
                    return 1;
                }
                catch (OperationCanceledException)
                {
                    _stderr.WriteLine("Authentication test timed out after 30 seconds.");
                    return 1;
                }
                catch (Exception ex)
                {
                    _stderr.WriteLine($"Unexpected error during auth test: {ex.Message}");
                    return 1;
                }
            }
        }
    }
}
