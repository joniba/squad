using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DgrepCli.Execution;
using DgrepCli.Formatters;

namespace DgrepCli.Commands
{
    /// <summary>
    /// Handles the "dgrep tail" command: continuously polls DGrep for new results
    /// at a configurable interval, printing rows as they arrive.
    /// </summary>
    public class TailCommand
    {
        private readonly IQueryExecutor _executor;
        private readonly TextWriter _stdout;
        private readonly TextWriter _stderr;

        public TailCommand(IQueryExecutor executor)
            : this(executor, Console.Out, Console.Error)
        {
        }

        public TailCommand(IQueryExecutor executor, TextWriter stdout, TextWriter stderr)
        {
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _stdout = stdout ?? Console.Out;
            _stderr = stderr ?? Console.Error;
        }

        /// <summary>
        /// Run the tail loop. Returns 0 on graceful stop, 1 on error.
        /// </summary>
        public int Execute(TailOptions opts, CancellationToken ct)
        {
            if (opts == null) throw new ArgumentNullException(nameof(opts));

            IOutputFormatter formatter;
            try
            {
                formatter = FormatterFactory.Create(opts.Output);
            }
            catch (ArgumentException ex)
            {
                _stderr.WriteLine($"Error: {ex.Message}");
                return 1;
            }

            var totalResults = 0L;
            var currentFrom = opts.From;
            var pollInterval = TimeSpan.FromSeconds(Math.Max(opts.Interval, 1));

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var queryOptions = BuildQueryOptions(opts, currentFrom);

                    QueryResult result;
                    try
                    {
                        result = _executor.ExecuteAsync(opts.Query, queryOptions, ct)
                                          .GetAwaiter().GetResult();
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (QueryAuthException ex)
                    {
                        _stderr.WriteLine($"Error: {ex.Message}");
                        _stderr.WriteLine("Run 'dgrep auth' to configure authentication.");
                        return 1;
                    }
                    catch (QueryException ex)
                    {
                        _stderr.WriteLine($"Error: {ex.Message}");
                        return 1;
                    }

                    if (result != null && result.Rows.Count > 0)
                    {
                        formatter.Format(result, _stdout);
                        _stdout.Flush();
                        totalResults += result.Rows.Count;
                    }

                    // Advance the from time to now for the next poll
                    currentFrom = DateTime.UtcNow.ToString("o");

                    // Status line on stderr so it doesn't pollute piped stdout
                    _stderr.WriteLine($"Watching... (last check: {DateTime.Now:HH:mm:ss}, {totalResults} results so far)");

                    // Wait for next poll interval
                    try
                    {
                        Task.Delay(pollInterval, ct).GetAwaiter().GetResult();
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Graceful Ctrl+C — fall through to summary
            }

            _stderr.WriteLine($"\nStopped. Total results: {totalResults}");
            return 0;
        }

        /// <summary>
        /// Build QueryOptions from TailOptions, using Parameters to carry DGrep-specific fields.
        /// The real DgrepQueryExecutor reads these to construct QueryInput.
        /// </summary>
        internal static QueryOptions BuildQueryOptions(TailOptions opts, string currentFrom)
        {
            var qo = new QueryOptions
            {
                Cluster = opts.Endpoint,
                MaxRows = opts.MaxRows,
                Timeout = TimeSpan.FromMinutes(5),
                Parameters = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["_endpoint"] = opts.Endpoint ?? "",
                    ["_namespace"] = opts.Namespace ?? "",
                    ["_event"] = opts.Event ?? "",
                    ["_from"] = currentFrom ?? "",
                    ["_to"] = "now",
                    ["_queryType"] = opts.QueryType ?? "kql"
                }
            };

            if (!string.IsNullOrEmpty(opts.Version))
                qo.Parameters["_version"] = opts.Version;

            if (opts.Identity != null)
            {
                var identities = string.Join(";", opts.Identity.Where(i => !string.IsNullOrWhiteSpace(i)));
                if (!string.IsNullOrEmpty(identities))
                    qo.Parameters["_identity"] = identities;
            }

            return qo;
        }
    }
}
