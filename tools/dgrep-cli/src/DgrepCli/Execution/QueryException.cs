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
    /// Thrown when the executor cannot connect to the DGrep endpoint.
    /// </summary>
    public class QueryConnectionException : QueryException
    {
        public string Endpoint { get; }

        public QueryConnectionException(string endpoint, string message)
            : base($"Failed to connect to endpoint '{endpoint}': {message}")
        {
            Endpoint = endpoint;
        }

        public QueryConnectionException(string endpoint, string message, Exception inner)
            : base($"Failed to connect to endpoint '{endpoint}': {message}", inner)
        {
            Endpoint = endpoint;
        }
    }

    /// <summary>
    /// Thrown when the query has a syntax error (pass through from DGrep).
    /// </summary>
    public class QuerySyntaxException : QueryException
    {
        public string Query { get; }

        public QuerySyntaxException(string query, string serverError)
            : base($"Query syntax error: {serverError}")
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
