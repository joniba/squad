using DgrepCli.Commands;

namespace DgrepCli.Config
{
    /// <summary>
    /// Resolves effective option values using priority: CLI flag > config file > hardcoded default.
    /// </summary>
    public class OptionResolver
    {
        private readonly DgrepConfig _config;

        // Hardcoded defaults
        public const string DefaultOutputFormat = "table";
        public const string DefaultQueryType = "kql";
        public const int DefaultMaxRows = 500000;
        public const string DefaultTimeRange = "1h";

        public OptionResolver(DgrepConfig config)
        {
            _config = config ?? new DgrepConfig();
        }

        /// <summary>
        /// Resolves SearchOptions by filling in config/default values for unset fields.
        /// CLI flags always win. Config values fill gaps. Hardcoded defaults are last resort.
        /// </summary>
        public ResolvedSearchOptions Resolve(SearchOptions opts)
        {
            return new ResolvedSearchOptions
            {
                Endpoint = ResolveString(opts.Endpoint, _config.DefaultEndpoint, null),
                Namespace = ResolveString(opts.Namespace, _config.DefaultNamespace, null),
                Event = opts.Event, // no config fallback for event
                From = ResolveString(opts.From, NegateTimeRange(_config.DefaultTimeRange), "-" + DefaultTimeRange),
                To = opts.To ?? "now",
                Query = opts.Query,
                QueryType = ResolveString(opts.QueryType, _config.DefaultQueryType, DefaultQueryType),
                MaxRows = ResolveMaxRows(opts.MaxRows, _config.DefaultMaxRows),
                Output = ResolveString(opts.Output, _config.OutputFormat, DefaultOutputFormat),
                CertPath = ResolveString(opts.CertPath, _config.CertificatePath, null),
                Identity = opts.Identity,
                Version = opts.Version
            };
        }

        private static string ResolveString(string cliValue, string configValue, string defaultValue)
        {
            if (!string.IsNullOrEmpty(cliValue))
                return cliValue;
            if (!string.IsNullOrEmpty(configValue))
                return configValue;
            return defaultValue;
        }

        private static int ResolveMaxRows(int cliValue, int? configValue)
        {
            // CommandLineParser sets the default to 500000 via attribute, so we can't
            // distinguish "user passed --max-rows 500000" from "user didn't pass it".
            // We treat the CLI value as authoritative if it differs from the attr default.
            if (cliValue != DefaultMaxRows)
                return cliValue;
            if (configValue.HasValue)
                return configValue.Value;
            return DefaultMaxRows;
        }

        /// <summary>
        /// Converts a config time range (e.g. "1h") to a negative relative value ("-1h")
        /// for use as a --from default.
        /// </summary>
        public static string NegateTimeRange(string timeRange)
        {
            if (string.IsNullOrEmpty(timeRange))
                return null;
            if (timeRange.StartsWith("-"))
                return timeRange;
            return "-" + timeRange;
        }
    }

    /// <summary>
    /// Fully resolved search options after applying config and defaults.
    /// </summary>
    public class ResolvedSearchOptions
    {
        public string Endpoint { get; set; }
        public string Namespace { get; set; }
        public string Event { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Query { get; set; }
        public string QueryType { get; set; }
        public int MaxRows { get; set; }
        public string Output { get; set; }
        public string CertPath { get; set; }
        public System.Collections.Generic.IEnumerable<string> Identity { get; set; }
        public string Version { get; set; }
    }
}
