using CommandLine;

namespace DgrepCli.Commands
{
    [Verb("query", HelpText = "Execute a KQL query against a Kusto cluster.\n\nExample:\n  dgrep query \"StormEvents | take 10\" --cluster https://help.kusto.windows.net --database Samples")]
    public class QueryVerbOptions
    {
        [Value(0, MetaName = "query", Required = false, HelpText = "KQL query to execute. If omitted, reads from stdin.")]
        public string Query { get; set; }

        [Option("cluster", Required = false, HelpText = "Kusto cluster URL (e.g. https://mycluster.kusto.windows.net).")]
        public string Cluster { get; set; }

        [Option("database", Required = false, HelpText = "Database name to query.")]
        public string Database { get; set; }

        [Option("timeout", Required = false, Default = 0, HelpText = "Query timeout in seconds. 0 = use config default (300s).")]
        public int Timeout { get; set; }

        [Option("max-rows", Required = false, Default = 0, HelpText = "Maximum rows to return. 0 = use config default (500,000).")]
        public int MaxRows { get; set; }

        [Option('o', "output", Required = false, Default = null, HelpText = "Output format: table (default), json, csv.")]
        public string Output { get; set; }

        [Option("param", Required = false, Separator = ' ', HelpText = "Query parameter. Format: Name=Value (repeatable).")]
        public System.Collections.Generic.IEnumerable<string> Parameters { get; set; }
    }
}
