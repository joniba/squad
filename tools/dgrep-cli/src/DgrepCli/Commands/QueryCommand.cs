using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DgrepCli.Config;
using DgrepCli.Execution;
using DgrepCli.Formatters;

namespace DgrepCli.Commands
{
    /// <summary>
    /// Handles the "dgrep query" command: resolves config defaults, executes query, formats output.
    /// </summary>
    public class QueryCommand
    {
        private readonly IQueryExecutor _executor;
        private readonly ConfigManager _configManager;
        private readonly TextWriter _stdout;
        private readonly TextWriter _stderr;
        private readonly TextReader _stdin;
        private readonly bool _stdinRedirected;

        public QueryCommand(IQueryExecutor executor, ConfigManager configManager)
            : this(executor, configManager, Console.Out, Console.Error, Console.In, Console.IsInputRedirected)
        {
        }

        public QueryCommand(IQueryExecutor executor, ConfigManager configManager,
                            TextWriter stdout, TextWriter stderr,
                            TextReader stdin = null, bool stdinRedirected = false)
        {
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _stdout = stdout ?? Console.Out;
            _stderr = stderr ?? Console.Error;
            _stdin = stdin ?? Console.In;
            _stdinRedirected = stdinRedirected;
        }

        public int Execute(QueryVerbOptions opts)
        {
            var config = _configManager.Load();

            // Resolve query: CLI arg or stdin
            var query = opts.Query;
            if (string.IsNullOrWhiteSpace(query))
            {
                if (_stdinRedirected)
                {
                    query = _stdin.ReadToEnd();
                }
                else
                {
                    _stderr.WriteLine("Error: No query provided. Pass as argument or pipe via stdin.");
                    return 1;
                }
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                _stderr.WriteLine("Error: Query is empty.");
                return 1;
            }

            // Resolve options from CLI flags → config → defaults
            var cluster = ResolveString(opts.Cluster, config.DefaultCluster, null);
            var database = ResolveString(opts.Database, config.DefaultDatabase, null);
            var outputFormat = ResolveString(opts.Output, config.OutputFormat, "table");
            var timeout = ResolveTimeout(opts.Timeout, config);
            var maxRows = ResolveMaxRows(opts.MaxRows, config.DefaultMaxRows);

            // Validate required fields
            if (string.IsNullOrWhiteSpace(cluster))
            {
                _stderr.WriteLine("Error: No cluster specified. Use --cluster or set defaultCluster in config.");
                return 1;
            }
            if (string.IsNullOrWhiteSpace(database))
            {
                _stderr.WriteLine("Error: No database specified. Use --database or set defaultDatabase in config.");
                return 1;
            }

            // Build query options
            var queryOptions = new Execution.QueryOptions
            {
                Cluster = cluster,
                Database = database,
                Timeout = timeout,
                MaxRows = maxRows,
                Parameters = ParseParameters(opts.Parameters)
            };

            // Execute
            try
            {
                using (var cts = new CancellationTokenSource(timeout))
                {
                    var result = _executor.ExecuteAsync(query, queryOptions, cts.Token)
                                         .GetAwaiter().GetResult();

                    // Format output
                    var formatter = FormatterFactory.Create(outputFormat);
                    formatter.Format(result, _stdout);
                    return 0;
                }
            }
            catch (OperationCanceledException)
            {
                _stderr.WriteLine($"Error: Query timed out after {timeout.TotalSeconds:F0} seconds. " +
                                  "Increase timeout with --timeout or config defaultTimeout.");
                return 1;
            }
            catch (QueryConnectionException ex)
            {
                _stderr.WriteLine($"Error: {ex.Message}");
                _stderr.WriteLine($"Check that the cluster URL is correct: {ex.ClusterUrl}");
                return 1;
            }
            catch (QuerySyntaxException ex)
            {
                _stderr.WriteLine($"Error: {ex.Message}");
                return 1;
            }
            catch (QueryTimeoutException ex)
            {
                _stderr.WriteLine($"Error: {ex.Message}");
                return 1;
            }
            catch (QueryAuthException ex)
            {
                _stderr.WriteLine($"Error: {ex.Message}");
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

        private static string ResolveString(string cliValue, string configValue, string defaultValue)
        {
            if (!string.IsNullOrEmpty(cliValue)) return cliValue;
            if (!string.IsNullOrEmpty(configValue)) return configValue;
            return defaultValue;
        }

        public static TimeSpan ResolveTimeout(int cliSeconds, DgrepConfig config)
        {
            if (cliSeconds > 0)
                return TimeSpan.FromSeconds(cliSeconds);
            // No timeout config field yet, use default
            return TimeSpan.FromSeconds(300);
        }

        public static int ResolveMaxRows(int cliValue, int? configValue)
        {
            if (cliValue > 0) return cliValue;
            if (configValue.HasValue && configValue.Value > 0) return configValue.Value;
            return 500000;
        }

        public static Dictionary<string, string> ParseParameters(IEnumerable<string> parameters)
        {
            if (parameters == null) return null;

            var dict = new Dictionary<string, string>();
            foreach (var param in parameters)
            {
                if (string.IsNullOrWhiteSpace(param)) continue;
                var eqIdx = param.IndexOf('=');
                if (eqIdx <= 0) continue;
                var key = param.Substring(0, eqIdx);
                var val = param.Substring(eqIdx + 1);
                dict[key] = val;
            }
            return dict.Count > 0 ? dict : null;
        }
    }
}
