using CommandLine;

namespace DgrepCli.Commands
{
    [Verb("auth", HelpText = "Manage authentication.\n\nSubcommands:\n  status  — Show current auth method, token expiry, connected identity\n  test    — Attempt to connect to configured cluster, report success/failure\n\nExamples:\n  dgrep auth status\n  dgrep auth test")]
    public class AuthVerbOptions
    {
        [Value(0, MetaName = "action", Required = true, HelpText = "Action: status or test.")]
        public string Action { get; set; }

        [Option("auth-method", Required = false, HelpText = "Override auth method: azcli, certificate, managedidentity.")]
        public string AuthMethod { get; set; }
    }
}
