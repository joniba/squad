using System.Collections.Generic;
using Newtonsoft.Json;

namespace DgrepCli.Config
{
    /// <summary>
    /// Root configuration model for dgrep CLI.
    /// Persisted as JSON at ~/.dgrep/config.json.
    /// </summary>
    public class DgrepConfig
    {
        [JsonProperty("defaultNamespace")]
        public string DefaultNamespace { get; set; }

        [JsonProperty("defaultCluster")]
        public string DefaultCluster { get; set; }

        [JsonProperty("defaultDatabase")]
        public string DefaultDatabase { get; set; }

        /// <summary>
        /// Default time range for queries (e.g. "1h", "24h", "30m").
        /// </summary>
        [JsonProperty("defaultTimeRange")]
        public string DefaultTimeRange { get; set; }

        /// <summary>
        /// Default maximum rows returned by queries.
        /// </summary>
        [JsonProperty("defaultMaxRows")]
        public int? DefaultMaxRows { get; set; }

        /// <summary>
        /// Path to certificate file for cert-based authentication.
        /// </summary>
        [JsonProperty("certificatePath")]
        public string CertificatePath { get; set; }

        /// <summary>
        /// Saved query templates keyed by name.
        /// </summary>
        [JsonProperty("savedQueries")]
        public Dictionary<string, SavedQuery> SavedQueries { get; set; }
            = new Dictionary<string, SavedQuery>();

        /// <summary>
        /// Default output format: table, json, csv, tsv, jsonl.
        /// </summary>
        [JsonProperty("outputFormat")]
        public string OutputFormat { get; set; }

        /// <summary>
        /// Default endpoint (e.g. diag-prod).
        /// </summary>
        [JsonProperty("defaultEndpoint")]
        public string DefaultEndpoint { get; set; }

        /// <summary>
        /// Default query type: kql or mql.
        /// </summary>
        [JsonProperty("defaultQueryType")]
        public string DefaultQueryType { get; set; }
    }

    /// <summary>
    /// A saved query template.
    /// </summary>
    public class SavedQuery
    {
        [JsonProperty("query")]
        public string Query { get; set; }

        [JsonProperty("endpoint")]
        public string Endpoint { get; set; }

        [JsonProperty("namespace")]
        public string Namespace { get; set; }

        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonProperty("queryType")]
        public string QueryType { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
}
