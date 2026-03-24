using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DgrepCli.Execution
{
    /// <summary>
    /// Decorator that wraps an IQueryExecutor with retry logic for transient failures.
    /// Non-transient errors (auth, syntax, bad request) fail immediately.
    /// </summary>
    public class RetryingQueryExecutor : IQueryExecutor
    {
        private readonly IQueryExecutor _inner;
        private readonly RetryPolicy _policy;
        private readonly TextWriter _stderr;

        /// <summary>
        /// Creates a retrying executor.
        /// </summary>
        /// <param name="inner">The actual executor to delegate to.</param>
        /// <param name="policy">Retry policy (default: 3 retries with exponential backoff).</param>
        /// <param name="stderr">Writer for retry status messages (default: Console.Error).</param>
        public RetryingQueryExecutor(IQueryExecutor inner, RetryPolicy policy = null, TextWriter stderr = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _policy = policy ?? RetryPolicy.Default;
            _stderr = stderr ?? Console.Error;
        }

        public async Task<QueryResult> ExecuteAsync(string query, QueryOptions options, CancellationToken ct)
        {
            return await _policy.ExecuteAsync(
                async (token) => await _inner.ExecuteAsync(query, options, token).ConfigureAwait(false),
                ct,
                onRetry: (ex, attempt, delay) =>
                {
                    var reason = GetRetryReason(ex);
                    _stderr.WriteLine(
                        $"[Retry {attempt}/{_policy.MaxRetries}] {reason} — waiting {delay.TotalSeconds:F1}s before retry...");
                }
            ).ConfigureAwait(false);
        }

        private static string GetRetryReason(Exception ex)
        {
            if (ex is QueryRateLimitException)
                return "Rate limited (max 5 concurrent queries per user)";
            if (ex is QueryTimeoutException)
                return "Query timed out";
            if (ex is QueryConnectionException connEx)
                return $"Cannot reach endpoint '{connEx.EndpointUrl}'";
            return $"Transient error: {ex.Message}";
        }
    }
}
