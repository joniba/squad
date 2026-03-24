using System;

namespace DgrepCli.Execution
{
    /// <summary>
    /// Base exception for query execution failures.
    /// </summary>
    public class QueryException : Exception
    {
        public QueryException(string message) : base(message) { }
        public QueryException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Thrown when the executor cannot connect to an endpoint.
    /// </summary>
    public class QueryConnectionException : QueryException
    {
        public string EndpointUrl { get; }

        /// <summary>Backward-compat alias for EndpointUrl.</summary>
        public string ClusterUrl => EndpointUrl;

        public QueryConnectionException(string endpointUrl, string message)
            : base($"Cannot reach endpoint '{endpointUrl}': {message}. Check your network and endpoint configuration.")
        {
            EndpointUrl = endpointUrl;
        }

        public QueryConnectionException(string endpointUrl, string message, Exception inner)
            : base($"Cannot reach endpoint '{endpointUrl}': {message}. Check your network and endpoint configuration.", inner)
        {
            EndpointUrl = endpointUrl;
        }
    }

    /// <summary>
    /// Thrown when the query has a syntax error. Surfaces the original query and server error.
    /// </summary>
    public class QuerySyntaxException : QueryException
    {
        public string Query { get; }
        public string ServerError { get; }

        public QuerySyntaxException(string query, string serverError)
            : base($"Query syntax error: {serverError}\n  Query: {query}")
        {
            Query = query;
            ServerError = serverError;
        }
    }

    /// <summary>
    /// Thrown when a query exceeds the configured timeout.
    /// </summary>
    public class QueryTimeoutException : QueryException
    {
        public TimeSpan Timeout { get; }

        public QueryTimeoutException(TimeSpan timeout)
            : base($"Query timed out after {timeout.TotalSeconds:F0} seconds. " +
                   "Increase timeout with --timeout or config defaultTimeout.")
        {
            Timeout = timeout;
        }
    }

    /// <summary>
    /// Thrown when authentication fails. Points user to dgrep auth command.
    /// </summary>
    public class QueryAuthException : QueryException
    {
        public QueryAuthException(string message)
            : base($"Authentication failed. Run 'dgrep auth status' to check your credentials. Detail: {message}")
        {
        }

        public QueryAuthException(string message, Exception inner)
            : base($"Authentication failed. Run 'dgrep auth status' to check your credentials. Detail: {message}", inner)
        {
        }
    }

    /// <summary>
    /// Thrown when DGrep rate limit is hit (max 5 concurrent requests per user).
    /// This is a transient error that should be retried with backoff.
    /// </summary>
    public class QueryRateLimitException : QueryException
    {
        public int MaxConcurrent { get; }

        public QueryRateLimitException(int maxConcurrent = 5)
            : base($"Rate limited (max {maxConcurrent} concurrent queries per user). Wait and retry.")
        {
            MaxConcurrent = maxConcurrent;
        }

        public QueryRateLimitException(string message, Exception inner)
            : base(message, inner)
        {
            MaxConcurrent = 5;
        }
    }
}
