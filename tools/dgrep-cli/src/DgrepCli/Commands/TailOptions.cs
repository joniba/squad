using CommandLine;
using System.Collections.Generic;

namespace DgrepCli.Commands
{
    [Verb("tail", HelpText = "Stream DGrep results in real-time (continuous polling).\n\nExample:\n  dgrep tail --endpoint diag-prod --namespace MyNamespace --event MyEvent --query \"where Level <= 2\" --from -5m")]
    public class TailOptions
    {
        [Option("endpoint", Required = true, HelpText = "MDS endpoint (e.g. diag-prod, diag-int).")]
        public string Endpoint { get; set; }

        [Option("namespace", Required = true, HelpText = "Event namespace filter (regex supported).")]
        public string Namespace { get; set; }

        [Option("event", Required = true, HelpText = "Event name filter (regex supported).")]
        public string Event { get; set; }

        [Option("version", Required = false, HelpText = "Event version filter (regex).")]
        public string Version { get; set; }

        [Option("from", Required = true, HelpText = "Start time — ISO 8601 or relative (-5m, -30m, -1h).")]
        public string From { get; set; }

        [Option("to", Required = false, Default = "now", HelpText = "End time — ISO 8601 or relative. Defaults to now.")]
        public string To { get; set; }

        [Option("identity", Required = false, Separator = ' ', HelpText = "Identity column scoping (repeatable). Format: Key=Value.")]
        public IEnumerable<string> Identity { get; set; }

        [Option("query", Required = true, HelpText = "Server-side query filter (KQL or MQL).")]
        public string Query { get; set; }

        [Option("query-type", Required = false, Default = "kql", HelpText = "Query language: kql (default) or mql.")]
        public string QueryType { get; set; }

        [Option("max-rows", Required = false, Default = 500000, HelpText = "Maximum result rows per poll cycle (1–1,000,000).")]
        public int MaxRows { get; set; }

        [Option('o', "output", Required = false, Default = "table", HelpText = "Output format: table (default), json, csv, tsv, jsonl.")]
        public string Output { get; set; }

        [Option("cert", Required = false, HelpText = "Path to certificate for cert-based authentication.")]
        public string CertPath { get; set; }

        [Option("interval", Required = false, Default = 30, HelpText = "Polling interval in seconds. Default: 30.")]
        public int Interval { get; set; }
    }
}
