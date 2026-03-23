using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using DgrepCli.Config;
using DgrepCli.Execution;
using DgrepCli.Formatters;

namespace DgrepCli.Commands
{
    /// <summary>
    /// Handles the "dgrep saved" command: list, add, remove, show, run saved queries.
    /// </summary>
    public class SavedCommand
    {
        private readonly IQueryExecutor _executor;
        private readonly ConfigManager _configManager;
        private readonly TextWriter _stdout;
        private readonly TextWriter _stderr;

        private static readonly Regex ParamPattern = new Regex(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

        public SavedCommand(IQueryExecutor executor, ConfigManager configManager)
            : this(executor, configManager, Console.Out, Console.Error)
        {
        }

        public SavedCommand(IQueryExecutor executor, ConfigManager configManager,
                            TextWriter stdout, TextWriter stderr)
        {
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _stdout = stdout ?? Console.Out;
            _stderr = stderr ?? Console.Error;
        }

        public int Execute(SavedOptions opts)
        {
            var config = _configManager.Load();
            EnsureBuiltInQueries(config);

            switch (opts.Action.ToLowerInvariant())
            {
                case "list": return ExecuteList(config);
                case "add": return ExecuteAdd(config, opts);
                case "remove": return ExecuteRemove(config, opts);
                case "show": return ExecuteShow(config, opts);
                case "run": return ExecuteRun(config, opts);
                default:
                    _stderr.WriteLine($"Unknown action '{opts.Action}'.");
                    return 1;
            }
        }

        private int ExecuteList(DgrepConfig config)
        {
            if (config.SavedQueries.Count == 0)
            {
                _stdout.WriteLine("No saved queries.");
                return 0;
            }

            _stdout.WriteLine($"{"Name",-25} {"Description"}");
            _stdout.WriteLine(new string('-', 70));

            foreach (var kvp in config.SavedQueries.OrderBy(q => q.Key))
            {
                var desc = kvp.Value.Description ?? "(no description)";
                _stdout.WriteLine($"{kvp.Key,-25} {desc}");
            }

            return 0;
        }

        private int ExecuteAdd(DgrepConfig config, SavedOptions opts)
        {
            if (config.SavedQueries.ContainsKey(opts.Name))
            {
                _stderr.WriteLine($"Error: A saved query named '{opts.Name}' already exists. Remove it first or choose a different name.");
                return 1;
            }

            var savedQuery = new SavedQuery
            {
                Query = opts.Query,
                Description = opts.Description,
                DefaultDatabase = opts.Database,
                DefaultCluster = opts.Cluster
            };

            config.SavedQueries[opts.Name] = savedQuery;
            _configManager.Save(config);

            _stdout.WriteLine($"Saved query '{opts.Name}' added.");
            return 0;
        }

        private int ExecuteRemove(DgrepConfig config, SavedOptions opts)
        {
            if (!config.SavedQueries.ContainsKey(opts.Name))
            {
                _stderr.WriteLine($"Error: No saved query named '{opts.Name}'.");
                return 1;
            }

            config.SavedQueries.Remove(opts.Name);
            _configManager.Save(config);

            _stdout.WriteLine($"Saved query '{opts.Name}' removed.");
            return 0;
        }

        private int ExecuteShow(DgrepConfig config, SavedOptions opts)
        {
            if (!config.SavedQueries.TryGetValue(opts.Name, out var query))
            {
                _stderr.WriteLine($"Error: No saved query named '{opts.Name}'.");
                return 1;
            }

            _stdout.WriteLine($"Name:        {opts.Name}");
            if (!string.IsNullOrEmpty(query.Description))
                _stdout.WriteLine($"Description: {query.Description}");
            if (!string.IsNullOrEmpty(query.DefaultCluster))
                _stdout.WriteLine($"Cluster:     {query.DefaultCluster}");
            if (!string.IsNullOrEmpty(query.DefaultDatabase))
                _stdout.WriteLine($"Database:    {query.DefaultDatabase}");

            // Show parameters found in template
            var paramNames = ExtractParameterNames(query.Query);
            if (paramNames.Count > 0)
                _stdout.WriteLine($"Parameters:  {string.Join(", ", paramNames)}");

            _stdout.WriteLine();
            _stdout.WriteLine("Query:");
            _stdout.WriteLine(query.Query);

            return 0;
        }

        private int ExecuteRun(DgrepConfig config, SavedOptions opts)
        {
            if (!config.SavedQueries.TryGetValue(opts.Name, out var savedQuery))
            {
                _stderr.WriteLine($"Error: No saved query named '{opts.Name}'.");
                return 1;
            }

            // Parse parameters
            var parameters = ParseParameters(opts.Parameters);

            // Substitute parameters in query template
            string query;
            try
            {
                query = SubstituteParameters(savedQuery.Query, parameters);
            }
            catch (ArgumentException ex)
            {
                _stderr.WriteLine($"Error: {ex.Message}");
                return 1;
            }

            // Resolve cluster/database: CLI override → saved query → global config → null
            var globalConfig = config;
            var cluster = ResolveString(opts.Cluster, savedQuery.DefaultCluster, globalConfig.DefaultCluster);
            var database = ResolveString(opts.Database, savedQuery.DefaultDatabase, globalConfig.DefaultDatabase);
            var outputFormat = ResolveString(opts.Output, globalConfig.OutputFormat, "table");
            var timeout = QueryCommand.ResolveTimeout(opts.Timeout, globalConfig);
            var maxRows = QueryCommand.ResolveMaxRows(opts.MaxRows ?? 0, globalConfig.DefaultMaxRows);

            if (string.IsNullOrWhiteSpace(cluster))
            {
                _stderr.WriteLine("Error: No cluster specified. Use --cluster, set it on the saved query, or set defaultCluster in config.");
                return 1;
            }
            if (string.IsNullOrWhiteSpace(database))
            {
                _stderr.WriteLine("Error: No database specified. Use --database, set it on the saved query, or set defaultDatabase in config.");
                return 1;
            }

            var queryOptions = new Execution.QueryOptions
            {
                Cluster = cluster,
                Database = database,
                Timeout = timeout,
                MaxRows = maxRows
            };

            try
            {
                using (var cts = new CancellationTokenSource(timeout))
                {
                    var result = _executor.ExecuteAsync(query, queryOptions, cts.Token)
                                         .GetAwaiter().GetResult();

                    var formatter = FormatterFactory.Create(outputFormat);
                    formatter.Format(result, _stdout);
                    return 0;
                }
            }
            catch (OperationCanceledException)
            {
                _stderr.WriteLine($"Error: Query timed out after {timeout.TotalSeconds:F0} seconds.");
                return 1;
            }
            catch (QueryException ex)
            {
                _stderr.WriteLine($"Error: {ex.Message}");
                return 1;
            }
            catch (NotImplementedException ex)
            {
                _stderr.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        /// <summary>
        /// Replaces {{param_name}} placeholders with provided parameter values.
        /// Throws ArgumentException if required parameters are missing.
        /// </summary>
        public static string SubstituteParameters(string template, Dictionary<string, string> parameters)
        {
            if (string.IsNullOrEmpty(template))
                return template;

            var requiredParams = ExtractParameterNames(template);
            if (requiredParams.Count == 0)
                return template;

            parameters = parameters ?? new Dictionary<string, string>();

            // Check for missing required parameters
            var missing = requiredParams.Where(p => !parameters.ContainsKey(p)).ToList();
            if (missing.Count > 0)
            {
                throw new ArgumentException(
                    $"Missing required parameter(s): {string.Join(", ", missing)}. " +
                    $"Provide with --param {missing[0]}=<value>");
            }

            return ParamPattern.Replace(template, match =>
            {
                var paramName = match.Groups[1].Value;
                return parameters.ContainsKey(paramName) ? parameters[paramName] : match.Value;
            });
        }

        /// <summary>
        /// Extracts unique parameter names from {{param_name}} placeholders in a template.
        /// </summary>
        public static List<string> ExtractParameterNames(string template)
        {
            if (string.IsNullOrEmpty(template))
                return new List<string>();

            return ParamPattern.Matches(template)
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Ensures built-in ICM investigation queries are seeded in the config.
        /// Only adds them if the config has no saved queries at all (fresh install).
        /// </summary>
        internal void EnsureBuiltInQueries(DgrepConfig config)
        {
            if (config.SavedQueries.Count > 0)
                return;

            foreach (var kvp in BuiltInQueries.GetAll())
            {
                config.SavedQueries[kvp.Key] = kvp.Value;
            }
            _configManager.Save(config);
        }

        private static Dictionary<string, string> ParseParameters(IEnumerable<string> parameters)
        {
            var dict = new Dictionary<string, string>();
            if (parameters == null) return dict;

            foreach (var param in parameters)
            {
                if (string.IsNullOrWhiteSpace(param)) continue;
                var eqIdx = param.IndexOf('=');
                if (eqIdx <= 0) continue;
                var key = param.Substring(0, eqIdx);
                var val = param.Substring(eqIdx + 1);
                dict[key] = val;
            }
            return dict;
        }

        private static string ResolveString(string cliValue, string savedValue, string configValue)
        {
            if (!string.IsNullOrEmpty(cliValue)) return cliValue;
            if (!string.IsNullOrEmpty(savedValue)) return savedValue;
            if (!string.IsNullOrEmpty(configValue)) return configValue;
            return null;
        }
    }
}
