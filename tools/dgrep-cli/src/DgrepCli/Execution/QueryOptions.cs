using System;
using System.Collections.Generic;

namespace DgrepCli.Execution
{
    /// <summary>
    /// Options passed to a query executor: DGrep connection details, timeout, limits.
    /// </summary>
    public class QueryOptions
    {
        /// <summary>
        /// MDS endpoint URL (e.g. "https://production.diagnostics.monitoring.core.windows.net/").
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Namespace regex pattern to query (e.g. "MyServicePrd.*").
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Event name regex pattern (e.g. "Log", "Metric.*").
        /// </summary>
        public string Event { get; set; }

        /// <summary>
        /// Query language: "kql" or "mql". Default: "kql".
        /// </summary>
        public string QueryType { get; set; } = "kql";

        /// <summary>
        /// Query timeout. Default: 5 minutes.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Maximum rows to return. Default: 500,000.
        /// </summary>
        public int MaxRows { get; set; } = 500000;

        /// <summary>
        /// Optional identity column filters (e.g. Tenant=WUS, Role=FE).
        /// </summary>
        public Dictionary<string, string> IdentityColumns { get; set; }

        /// <summary>
        /// Optional query parameters (template substitution).
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; }
    }
}
