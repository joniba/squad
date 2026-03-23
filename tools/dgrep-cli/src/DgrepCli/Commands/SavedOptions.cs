using CommandLine;
using System.Collections.Generic;

namespace DgrepCli.Commands
{
    [Verb("saved", HelpText = "Manage saved queries.\n\nSubcommands (pass as first positional arg):\n  list               — List all saved queries with descriptions\n  add <name>         — Save a new query template\n  remove <name>      — Remove a saved query\n  show <name>        — Display query details and template\n  run <name>         — Execute a saved query with parameter substitution\n\nExamples:\n  dgrep saved add my-query --query \"Table | where Status == '{{status}}'\" --description \"Filter by status\"\n  dgrep saved run my-query --param status=Error\n  dgrep saved list\n  dgrep saved show my-query\n  dgrep saved remove my-query")]
    public class SavedOptions
    {
        [Value(0, MetaName = "action", Required = true, HelpText = "Action to perform: list, add, remove, show, or run.")]
        public string Action { get; set; }

        [Value(1, MetaName = "name", Required = false, HelpText = "Saved query name.")]
        public string Name { get; set; }

        [Option("query", Required = false, HelpText = "KQL query template. Use {{param_name}} for parameters.")]
        public string Query { get; set; }

        [Option("description", Required = false, HelpText = "Human-readable description of the query.")]
        public string Description { get; set; }

        [Option("database", Required = false, HelpText = "Default database for this query.")]
        public string Database { get; set; }

        [Option("cluster", Required = false, HelpText = "Default cluster for this query.")]
        public string Cluster { get; set; }

        [Option("param", Separator = ',', Required = false, HelpText = "Parameter values for run (repeatable). Format: key=value.")]
        public IEnumerable<string> Parameters { get; set; }

        [Option("timeout", Required = false, HelpText = "Query timeout in seconds.")]
        public int Timeout { get; set; }

        [Option("max-rows", Required = false, HelpText = "Maximum result rows.")]
        public int? MaxRows { get; set; }

        [Option('o', "output", Required = false, HelpText = "Output format: table, json, csv, tsv, jsonl.")]
        public string Output { get; set; }
    }
}
