using CommandLine;

namespace DgrepCli.Commands
{
    [Verb("config", HelpText = "Manage CLI configuration.\n\nSubcommands (pass as first positional arg):\n  set <key> <value>  — Set a config value\n  get <key>          — Get a config value\n  list               — List all config values\n\nExamples:\n  dgrep config set default.endpoint diag-prod\n  dgrep config get default.endpoint\n  dgrep config list")]
    public class ConfigOptions
    {
        [Value(0, MetaName = "action", Required = true, HelpText = "Action to perform: set, get, or list.")]
        public string Action { get; set; }

        [Value(1, MetaName = "key", Required = false, HelpText = "Config key (e.g. default.endpoint, default.namespace).")]
        public string Key { get; set; }

        [Value(2, MetaName = "value", Required = false, HelpText = "Config value to set.")]
        public string Value { get; set; }
    }
}
