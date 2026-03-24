using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DgrepCli.Execution
{
    /// <summary>
    /// Mock query executor for testing. Returns canned results or throws configured exceptions.
    /// Supports single-result mode (WithResult) and sequential-results mode (WithResults).
    /// </summary>
    public class MockQueryExecutor : IQueryExecutor
    {
        private QueryResult _result;
        private Exception _exception;
        private TimeSpan _delay = TimeSpan.Zero;
        private Queue<QueryResult> _resultQueue;
        private Func<int, QueryResult> _resultFactory;

        /// <summary>
        /// Captured arguments from the last ExecuteAsync call.
        /// </summary>
        public string LastQuery { get; private set; }
        public QueryOptions LastOptions { get; private set; }
        public int ExecutionCount { get; private set; }

        /// <summary>
        /// All queries captured across every ExecuteAsync call (for tail/polling tests).
        /// </summary>
        public List<string> AllQueries { get; } = new List<string>();

        /// <summary>
        /// All options captured across every ExecuteAsync call (for tail/polling tests).
        /// </summary>
        public List<QueryOptions> AllOptions { get; } = new List<QueryOptions>();

        /// <summary>
        /// Configure the result to return on next execution.
        /// </summary>
        public MockQueryExecutor WithResult(QueryResult result)
        {
            _result = result;
            _exception = null;
            _resultQueue = null;
            _resultFactory = null;
            return this;
        }

        /// <summary>
        /// Configure a sequence of results. Each call to ExecuteAsync pops the next result.
        /// After exhaustion, returns an empty QueryResult.
        /// </summary>
        public MockQueryExecutor WithResults(params QueryResult[] results)
        {
            _resultQueue = new Queue<QueryResult>(results);
            _result = null;
            _exception = null;
            _resultFactory = null;
            return this;
        }

        /// <summary>
        /// Configure a factory that receives the 0-based call index and returns a result.
        /// </summary>
        public MockQueryExecutor WithResultFactory(Func<int, QueryResult> factory)
        {
            _resultFactory = factory;
            _result = null;
            _exception = null;
            _resultQueue = null;
            return this;
        }

        /// <summary>
        /// Configure an exception to throw on next execution.
        /// </summary>
        public MockQueryExecutor WithException(Exception ex)
        {
            _exception = ex;
            _result = null;
            _resultQueue = null;
            _resultFactory = null;
            return this;
        }

        /// <summary>
        /// Configure artificial delay before returning.
        /// </summary>
        public MockQueryExecutor WithDelay(TimeSpan delay)
        {
            _delay = delay;
            return this;
        }

        /// <summary>
        /// Creates a simple result set for testing.
        /// </summary>
        public static QueryResult CreateSimpleResult(string[] columnNames, string[] columnTypes, object[][] rows)
        {
            var columns = new List<ColumnDefinition>();
            for (int i = 0; i < columnNames.Length; i++)
            {
                columns.Add(new ColumnDefinition(columnNames[i],
                    i < columnTypes.Length ? columnTypes[i] : "string"));
            }
            var result = new QueryResult { Columns = columns, Rows = new List<object[]>() };
            if (rows != null)
            {
                foreach (var row in rows)
                    result.Rows.Add(row);
            }
            return result;
        }

        public async Task<QueryResult> ExecuteAsync(string query, QueryOptions options, CancellationToken ct)
        {
            LastQuery = query;
            LastOptions = options;
            AllQueries.Add(query);
            AllOptions.Add(options);
            ExecutionCount++;

            if (_delay > TimeSpan.Zero)
            {
                await Task.Delay(_delay, ct);
            }

            ct.ThrowIfCancellationRequested();

            if (_exception != null)
                throw _exception;

            if (_resultFactory != null)
                return _resultFactory(ExecutionCount - 1);

            if (_resultQueue != null && _resultQueue.Count > 0)
                return _resultQueue.Dequeue();

            return _result ?? new QueryResult();
        }
    }
}
