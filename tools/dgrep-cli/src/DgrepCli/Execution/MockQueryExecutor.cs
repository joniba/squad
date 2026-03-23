using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DgrepCli.Execution
{
    /// <summary>
    /// Mock query executor for testing. Returns canned results or throws configured exceptions.
    /// </summary>
    public class MockQueryExecutor : IQueryExecutor
    {
        private QueryResult _result;
        private Exception _exception;
        private TimeSpan _delay = TimeSpan.Zero;

        /// <summary>
        /// Captured arguments from the last ExecuteAsync call.
        /// </summary>
        public string LastQuery { get; private set; }
        public QueryOptions LastOptions { get; private set; }
        public int ExecutionCount { get; private set; }

        /// <summary>
        /// Configure the result to return on next execution.
        /// </summary>
        public MockQueryExecutor WithResult(QueryResult result)
        {
            _result = result;
            _exception = null;
            return this;
        }

        /// <summary>
        /// Configure an exception to throw on next execution.
        /// </summary>
        public MockQueryExecutor WithException(Exception ex)
        {
            _exception = ex;
            _result = null;
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
            ExecutionCount++;

            if (_delay > TimeSpan.Zero)
            {
                await Task.Delay(_delay, ct);
            }

            ct.ThrowIfCancellationRequested();

            if (_exception != null)
                throw _exception;

            return _result ?? new QueryResult();
        }
    }
}
