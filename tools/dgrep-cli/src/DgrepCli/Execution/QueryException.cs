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
    /// Thrown when the executor cannot connect to the cluster.
    /// </summary>
    public class QueryConnectionException : QueryException
    {
        public string ClusterUrl { get; }

        public QueryConnectionException(string clusterUrl, string message)
            : base($"Failed to connect to cluster '{clusterUrl}': {message}")
        {
            ClusterUrl = clusterUrl;
        }

        public QueryConnectionException(string clusterUrl, string message, Exception inner)
            : base($"Failed to connect to cluster '{clusterUrl}': {message}", inner)
        {
            ClusterUrl = clusterUrl;
        }
    }

    /// <summary>
    /// Thrown when the query has a syntax error (pass through from Kusto).
    /// </summary>
    public class QuerySyntaxException : QueryException
    {
        public string Query { get; }

        public QuerySyntaxException(string query, string kustoError)
            : base($"Query syntax error: {kustoError}")
        {
            Query = query;
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
            : base($"Authentication failed: {message}. Run 'dgrep auth' to configure credentials.")
        {
        }

        public QueryAuthException(string message, Exception inner)
            : base($"Authentication failed: {message}. Run 'dgrep auth' to configure credentials.", inner)
        {
        }
    }
}
