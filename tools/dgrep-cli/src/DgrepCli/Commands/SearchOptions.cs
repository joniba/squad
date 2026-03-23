using CommandLine;
using System.Collections.Generic;

namespace DgrepCli.Commands
{
    [Verb("search", HelpText = "Run a DGrep query and display results.\n\nExample:\n  dgrep search --endpoint diag-prod --namespace MyNamespace --event MyEvent --query \"where Level <= 2\" --from -1h")]
    public class SearchOptions
    {
        [Option("endpoint", Required = true, HelpText = "MDS endpoint (e.g. diag-prod, diag-int).")]
        public string Endpoint { get; set; }

        [Option("namespace", Required = true, HelpText = "Event namespace filter (regex supported, e.g. ^MyNamespace$).")]
        public string Namespace { get; set; }

        [Option("event", Required = true, HelpText = "Event name filter (regex supported, e.g. ^MyEvent$).")]
        public string Event { get; set; }

        [Option("version", Required = false, HelpText = "Event version filter (regex, e.g. ^(Ver2v0|Ver3v0)$).")]
        public string Version { get; set; }

        [Option("from", Required = true, HelpText = "Start time — ISO 8601 (2026-03-23T10:00:00Z) or relative (-30m, -1h, -4h, -7d).")]
        public string From { get; set; }

        [Option("to", Required = false, Default = "now", HelpText = "End time — ISO 8601 or relative. Defaults to now. Max range: 7 days.")]
        public string To { get; set; }

        [Option("identity", Required = false, Separator = ' ', HelpText = "Identity column scoping (repeatable). Format: Key=Value. Example: --identity Tenant=WUS --identity Role=Frontend")]
        public IEnumerable<string> Identity { get; set; }

        [Option("query", Required = true, HelpText = "Server-side query filter (KQL or MQL). Example: \"where Level <= 2\"")]
        public string Query { get; set; }

        [Option("query-type", Required = false, Default = "kql", HelpText = "Query language: kql (default, recommended) or mql (legacy).")]
        public string QueryType { get; set; }

        [Option("max-rows", Required = false, Default = 500000, HelpText = "Maximum result rows (1–1,000,000). Default: 500,000.")]
        public int MaxRows { get; set; }

        [Option('o', "output", Required = false, Default = "table", HelpText = "Output format: table (default), json, csv, tsv, jsonl.")]
        public string Output { get; set; }

        [Option("cert", Required = false, HelpText = "Path to certificate for cert-based authentication.")]
        public string CertPath { get; set; }
    }
}
