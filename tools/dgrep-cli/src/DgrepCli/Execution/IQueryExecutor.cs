using System.Threading;
using System.Threading.Tasks;

namespace DgrepCli.Execution
{
    /// <summary>
    /// Abstraction for query execution, enabling mock/real implementations.
    /// </summary>
    public interface IQueryExecutor
    {
        Task<QueryResult> ExecuteAsync(string query, QueryOptions options, CancellationToken ct);
    }
}
