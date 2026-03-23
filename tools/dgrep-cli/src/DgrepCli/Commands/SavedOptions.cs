using CommandLine;
using System.Collections.Generic;

namespace DgrepCli.Commands
{
    [Verb("saved", HelpText = "Manage saved queries.\n\nSubcommands (pass as first positional arg):\n  save <name>        — Save current flags as a named query\n  run <name>         — Run a saved query (override --from/--to)\n  list               — List all saved queries\n  delete <name>      — Delete a saved query\n\nExamples:\n  dgrep saved save my-query --endpoint diag-prod --namespace MyNs --event MyEvt --query \"where Level <= 2\"\n  dgrep saved run my-query --from -2h\n  dgrep saved list\n  dgrep saved delete my-query")]
    public class SavedOptions
    {
        [Value(0, MetaName = "action", Required = true, HelpText = "Action to perform: save, run, list, or delete.")]
        public string Action { get; set; }

        [Value(1, MetaName = "name", Required = false, HelpText = "Saved query name.")]
        public string Name { get; set; }

        // Flags for 'save' and 'run' override
        [Option("endpoint", Required = false, HelpText = "MDS endpoint.")]
        public string Endpoint { get; set; }

        [Option("namespace", Required = false, HelpText = "Event namespace filter.")]
        public string Namespace { get; set; }

        [Option("event", Required = false, HelpText = "Event name filter.")]
        public string Event { get; set; }

        [Option("version", Required = false, HelpText = "Event version filter.")]
        public string Version { get; set; }

        [Option("from", Required = false, HelpText = "Start time — ISO 8601 or relative.")]
        public string From { get; set; }

        [Option("to", Required = false, HelpText = "End time — ISO 8601 or relative.")]
        public string To { get; set; }

        [Option("identity", Required = false, Separator = ' ', HelpText = "Identity column scoping (repeatable). Format: Key=Value.")]
        public IEnumerable<string> Identity { get; set; }

        [Option("query", Required = false, HelpText = "Server-side query filter.")]
        public string Query { get; set; }

        [Option("query-type", Required = false, HelpText = "Query language: kql or mql.")]
        public string QueryType { get; set; }

        [Option("max-rows", Required = false, HelpText = "Maximum result rows.")]
        public int? MaxRows { get; set; }

        [Option('o', "output", Required = false, HelpText = "Output format: table, json, csv, tsv, jsonl.")]
        public string Output { get; set; }
    }
}
