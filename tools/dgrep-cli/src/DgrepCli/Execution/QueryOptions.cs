using System;
using System.Collections.Generic;

namespace DgrepCli.Execution
{
    /// <summary>
    /// Options passed to a query executor: connection details, timeout, limits.
    /// </summary>
    public class QueryOptions
    {
        /// <summary>
        /// Kusto cluster connection string or URL (e.g. "https://mycluster.kusto.windows.net").
        /// </summary>
        public string Cluster { get; set; }

        /// <summary>
        /// Database name to query against.
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Query timeout. Default: 5 minutes.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Maximum rows to return. Default: 500,000.
        /// </summary>
        public int MaxRows { get; set; } = 500000;

        /// <summary>
        /// Optional query parameters (CLP substitution).
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; }
    }
}
