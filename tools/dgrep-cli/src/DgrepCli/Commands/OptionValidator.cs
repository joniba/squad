using System;
using System.Collections.Generic;
using System.Linq;

namespace DgrepCli.Commands
{
    /// <summary>
    /// Validates parsed CLI options and returns human-readable error messages.
    /// </summary>
    public static class OptionValidator
    {
        private static readonly HashSet<string> ValidOutputFormats =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "table", "json", "csv", "tsv", "jsonl" };

        private static readonly HashSet<string> ValidQueryTypes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "kql", "mql" };

        private static readonly HashSet<string> ValidConfigActions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "set", "get", "list" };

        private static readonly HashSet<string> ValidSavedActions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "save", "run", "list", "delete" };

        public static List<string> ValidateSearchOptions(SearchOptions opts)
        {
            var errors = new List<string>();
            ValidateOutputFormat(opts.Output, errors);
            ValidateQueryType(opts.QueryType, errors);
            ValidateMaxRows(opts.MaxRows, errors);
            ValidateTimeRange(opts.From, "from", errors);
            if (opts.To != null && !opts.To.Equals("now", StringComparison.OrdinalIgnoreCase))
                ValidateTimeRange(opts.To, "to", errors);
            ValidateIdentities(opts.Identity, errors);
            return errors;
        }

        public static List<string> ValidateTailOptions(TailOptions opts)
        {
            var errors = new List<string>();
            ValidateOutputFormat(opts.Output, errors);
            ValidateQueryType(opts.QueryType, errors);
            ValidateMaxRows(opts.MaxRows, errors);
            ValidateTimeRange(opts.From, "from", errors);
            if (opts.To != null && !opts.To.Equals("now", StringComparison.OrdinalIgnoreCase))
                ValidateTimeRange(opts.To, "to", errors);
            ValidateIdentities(opts.Identity, errors);
            if (opts.Interval < 1)
                errors.Add("--interval must be at least 1 second.");
            return errors;
        }

        public static List<string> ValidateConfigOptions(ConfigOptions opts)
        {
            var errors = new List<string>();
            if (!ValidConfigActions.Contains(opts.Action))
                errors.Add($"Unknown config action '{opts.Action}'. Valid actions: set, get, list.");

            if (opts.Action != null)
            {
                var action = opts.Action.ToLowerInvariant();
                if (action == "set" && string.IsNullOrWhiteSpace(opts.Key))
                    errors.Add("'config set' requires a key.");
                if (action == "set" && string.IsNullOrWhiteSpace(opts.Value))
                    errors.Add("'config set' requires a value.");
                if (action == "get" && string.IsNullOrWhiteSpace(opts.Key))
                    errors.Add("'config get' requires a key.");
            }
            return errors;
        }

        public static List<string> ValidateSavedOptions(SavedOptions opts)
        {
            var errors = new List<string>();
            if (!ValidSavedActions.Contains(opts.Action))
                errors.Add($"Unknown saved-query action '{opts.Action}'. Valid actions: save, run, list, delete.");

            if (opts.Action != null)
            {
                var action = opts.Action.ToLowerInvariant();
                if ((action == "save" || action == "run" || action == "delete") && string.IsNullOrWhiteSpace(opts.Name))
                    errors.Add($"'saved {action}' requires a query name.");
                if (action == "save")
                {
                    if (string.IsNullOrWhiteSpace(opts.Endpoint))
                        errors.Add("'saved save' requires --endpoint.");
                    if (string.IsNullOrWhiteSpace(opts.Namespace))
                        errors.Add("'saved save' requires --namespace.");
                    if (string.IsNullOrWhiteSpace(opts.Event))
                        errors.Add("'saved save' requires --event.");
                    if (string.IsNullOrWhiteSpace(opts.Query))
                        errors.Add("'saved save' requires --query.");
                }
            }

            if (opts.Output != null)
                ValidateOutputFormat(opts.Output, errors);
            if (opts.QueryType != null)
                ValidateQueryType(opts.QueryType, errors);
            if (opts.MaxRows.HasValue)
                ValidateMaxRows(opts.MaxRows.Value, errors);

            return errors;
        }

        public static void ValidateOutputFormat(string format, List<string> errors)
        {
            if (!ValidOutputFormats.Contains(format))
                errors.Add($"Invalid output format '{format}'. Valid formats: table, json, csv, tsv, jsonl.");
        }

        public static void ValidateQueryType(string queryType, List<string> errors)
        {
            if (!ValidQueryTypes.Contains(queryType))
                errors.Add($"Invalid query type '{queryType}'. Valid types: kql, mql.");
        }

        public static void ValidateMaxRows(int maxRows, List<string> errors)
        {
            if (maxRows < 1 || maxRows > 1000000)
                errors.Add($"--max-rows must be between 1 and 1,000,000 (got {maxRows}).");
        }

        public static void ValidateTimeRange(string time, string flagName, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(time))
            {
                errors.Add($"--{flagName} cannot be empty.");
                return;
            }

            // Relative time: -30m, -1h, -4h, -7d, +5m
            if (time.Length >= 2 && (time[0] == '-' || time[0] == '+'))
            {
                var suffix = time[time.Length - 1];
                if (suffix != 'm' && suffix != 'h' && suffix != 'd' && suffix != 's')
                {
                    errors.Add($"--{flagName} relative time '{time}' must end with s, m, h, or d.");
                    return;
                }

                int numericPart;
                if (!int.TryParse(time.Substring(1, time.Length - 2), out numericPart) || numericPart <= 0)
                {
                    errors.Add($"--{flagName} relative time '{time}' has invalid numeric component.");
                }
                return;
            }

            // Absolute time: ISO 8601
            DateTimeOffset parsed;
            if (!DateTimeOffset.TryParse(time, out parsed))
            {
                errors.Add($"--{flagName} value '{time}' is not a valid ISO 8601 timestamp or relative duration (e.g. -30m, -1h).");
            }
        }

        public static void ValidateIdentities(IEnumerable<string> identities, List<string> errors)
        {
            if (identities == null) return;
            foreach (var id in identities)
            {
                if (string.IsNullOrWhiteSpace(id)) continue;
                if (!id.Contains("="))
                    errors.Add($"Invalid identity format '{id}'. Expected Key=Value (e.g. Tenant=WUS).");
            }
        }
    }
}
