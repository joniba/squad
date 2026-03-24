using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DgrepCli.Auth;
using DgrepCli.Commands;
using DgrepCli.Config;
using Xunit;

namespace DgrepCli.Tests.Auth
{
    public class AuthCommandTests
    {
        private const string ValidTokenJson = @"{
            ""accessToken"": ""eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9.test"",
            ""expiresOn"": ""2030-12-31T23:59:59+00:00"",
            ""subscription"": ""sub-id-123"",
            ""tenant"": ""tenant-id-456"",
            ""tokenType"": ""Bearer""
        }";

        private (AuthCommand cmd, StringWriter stdout, StringWriter stderr) CreateCommand(
            DgrepConfig config = null,
            Func<DgrepConfig, string, IAuthProvider> providerFactory = null)
        {
            var configDir = Path.Combine(Path.GetTempPath(), $"dgrep-test-{Guid.NewGuid()}");
            var configPath = Path.Combine(configDir, "config.json");
            var manager = new ConfigManager(configPath);
            if (config != null)
                manager.Save(config);

            var stdout = new StringWriter();
            var stderr = new StringWriter();

            var cmd = new AuthCommand(manager, stdout, stderr, providerFactory);
            return (cmd, stdout, stderr);
        }

        [Fact]
        public void Execute_UnknownAction_ReturnsError()
        {
            var (cmd, stdout, stderr) = CreateCommand();
            var result = cmd.Execute(new AuthVerbOptions { Action = "foobar" });

            Assert.Equal(1, result);
            Assert.Contains("Unknown auth action", stderr.ToString());
        }

        [Fact]
        public void Execute_StatusAction_WithAzCli_ShowsMethodAndConfig()
        {
            // Use a mock provider for status that always succeeds
            var factory = new Func<DgrepConfig, string, IAuthProvider>((cfg, cli) =>
                new AzCliAuthProvider(new MockProcessRunner(new ProcessResult
                {
                    ExitCode = 0,
                    StdOut = ValidTokenJson,
                    StdErr = ""
                })));

            var (cmd, stdout, stderr) = CreateCommand(new DgrepConfig(), factory);
            var result = cmd.Execute(new AuthVerbOptions { Action = "status" });

            Assert.Equal(0, result);
            var output = stdout.ToString();
            Assert.Contains("Auth method:", output);
            Assert.Contains("azcli", output);
            Assert.Contains("Valid:", output);
            Assert.Contains("yes", output);
        }

        [Fact]
        public void Execute_StatusAction_WithConfiguredMethod_ShowsConfigured()
        {
            var factory = new Func<DgrepConfig, string, IAuthProvider>((cfg, cli) =>
                new AzCliAuthProvider(new MockProcessRunner(new ProcessResult
                {
                    ExitCode = 0,
                    StdOut = ValidTokenJson,
                    StdErr = ""
                })));

            var config = new DgrepConfig { AuthMethod = "azcli" };
            var (cmd, stdout, stderr) = CreateCommand(config, factory);
            var result = cmd.Execute(new AuthVerbOptions { Action = "status" });

            Assert.Equal(0, result);
            var output = stdout.ToString();
            Assert.Contains("Configured:   azcli", output);
        }

        [Fact]
        public void Execute_StatusAction_NoConfiguredMethod_ShowsDefault()
        {
            var factory = new Func<DgrepConfig, string, IAuthProvider>((cfg, cli) =>
                new AzCliAuthProvider(new MockProcessRunner(new ProcessResult
                {
                    ExitCode = 0,
                    StdOut = ValidTokenJson,
                    StdErr = ""
                })));

            var (cmd, stdout, stderr) = CreateCommand(new DgrepConfig(), factory);
            cmd.Execute(new AuthVerbOptions { Action = "status" });

            var output = stdout.ToString();
            Assert.Contains("defaulting to azcli", output);
        }

        [Fact]
        public void Execute_StatusAction_CertMethod_ShowsCertPath()
        {
            var tmpCert = Path.Combine(Path.GetTempPath(), "auth-test.pfx");
            File.WriteAllText(tmpCert, "dummy");
            try
            {
                var factory = new Func<DgrepConfig, string, IAuthProvider>((cfg, cli) =>
                    new CertificateAuthProvider(cfg?.CertificatePath));

                var config = new DgrepConfig { AuthMethod = "certificate", CertificatePath = tmpCert };
                var (cmd, stdout, stderr) = CreateCommand(config, factory);
                cmd.Execute(new AuthVerbOptions { Action = "status" });

                var output = stdout.ToString();
                Assert.Contains("Cert path:", output);
            }
            finally
            {
                File.Delete(tmpCert);
            }
        }

        [Fact]
        public void Execute_StatusAction_ValidationFails_ReturnsError()
        {
            var factory = new Func<DgrepConfig, string, IAuthProvider>((cfg, cli) =>
                new AzCliAuthProvider(new MockProcessRunner(new ProcessResult
                {
                    ExitCode = 1,
                    StdOut = "",
                    StdErr = "Please run 'az login' to setup account."
                })));

            var (cmd, stdout, stderr) = CreateCommand(new DgrepConfig(), factory);
            var result = cmd.Execute(new AuthVerbOptions { Action = "status" });

            Assert.Equal(1, result);
            var output = stdout.ToString();
            Assert.Contains("Valid:", output);
            Assert.Contains("no", output);
        }

        [Fact]
        public void Execute_TestAction_Success_ShowsTokenInfo()
        {
            var factory = new Func<DgrepConfig, string, IAuthProvider>((cfg, cli) =>
                new AzCliAuthProvider(new MockProcessRunner(new ProcessResult
                {
                    ExitCode = 0,
                    StdOut = ValidTokenJson,
                    StdErr = ""
                })));

            var config = new DgrepConfig { DefaultEndpoint = "https://production.diagnostics.monitoring.core.windows.net/" };
            var (cmd, stdout, stderr) = CreateCommand(config, factory);
            var result = cmd.Execute(new AuthVerbOptions { Action = "test" });

            Assert.Equal(0, result);
            var output = stdout.ToString();
            Assert.Contains("Authentication successful!", output);
            Assert.Contains("Identity:", output);
            Assert.Contains("Token expiry:", output);
            Assert.Contains("Token length:", output);
            Assert.Contains("Target endpoint:", output);
        }

        [Fact]
        public void Execute_TestAction_NoEndpoint_UsesDefaultResource()
        {
            var factory = new Func<DgrepConfig, string, IAuthProvider>((cfg, cli) =>
                new AzCliAuthProvider(new MockProcessRunner(new ProcessResult
                {
                    ExitCode = 0,
                    StdOut = ValidTokenJson,
                    StdErr = ""
                })));

            var (cmd, stdout, stderr) = CreateCommand(new DgrepConfig(), factory);
            cmd.Execute(new AuthVerbOptions { Action = "test" });

            var output = stdout.ToString();
            Assert.Contains("No default endpoint configured", output);
        }

        [Fact]
        public void Execute_TestAction_AuthFailure_ShowsError()
        {
            var factory = new Func<DgrepConfig, string, IAuthProvider>((cfg, cli) =>
                new AzCliAuthProvider(new MockProcessRunner(new ProcessResult
                {
                    ExitCode = 1,
                    StdOut = "",
                    StdErr = "Please run 'az login'"
                })));

            var (cmd, stdout, stderr) = CreateCommand(new DgrepConfig(), factory);
            var result = cmd.Execute(new AuthVerbOptions { Action = "test" });

            Assert.Equal(1, result);
            var errOutput = stderr.ToString();
            Assert.Contains("Authentication failed", errOutput);
            Assert.Contains("dgrep auth status", errOutput);
        }

        [Fact]
        public void Execute_TestAction_NotImplemented_ShowsMessage()
        {
            var factory = new Func<DgrepConfig, string, IAuthProvider>((cfg, cli) =>
                new ManagedIdentityAuthProvider());

            var (cmd, stdout, stderr) = CreateCommand(new DgrepConfig(), factory);
            var result = cmd.Execute(new AuthVerbOptions { Action = "test" });

            Assert.Equal(1, result);
            Assert.Contains("not fully implemented", stderr.ToString());
        }

        [Fact]
        public void Execute_StatusAction_InvalidProvider_ShowsError()
        {
            var factory = new Func<DgrepConfig, string, IAuthProvider>((cfg, cli) =>
            {
                throw new AuthException("Unknown auth method 'bogus'.");
            });

            var (cmd, stdout, stderr) = CreateCommand(new DgrepConfig(), factory);
            var result = cmd.Execute(new AuthVerbOptions { Action = "status" });

            Assert.Equal(1, result);
            Assert.Contains("Unknown auth method", stderr.ToString());
        }

        [Fact]
        public void Execute_TestAction_AuthMethodOverride_UsesOverride()
        {
            string capturedCli = null;
            var factory = new Func<DgrepConfig, string, IAuthProvider>((cfg, cli) =>
            {
                capturedCli = cli;
                return new AzCliAuthProvider(new MockProcessRunner(new ProcessResult
                {
                    ExitCode = 0,
                    StdOut = ValidTokenJson,
                    StdErr = ""
                }));
            });

            var (cmd, stdout, stderr) = CreateCommand(new DgrepConfig(), factory);
            cmd.Execute(new AuthVerbOptions { Action = "test", AuthMethod = "azcli" });

            Assert.Equal("azcli", capturedCli);
        }
    }
}
