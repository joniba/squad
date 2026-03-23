using System.IO;
using DgrepCli.Execution;

namespace DgrepCli.Formatters
{
    /// <summary>
    /// Formats a QueryResult for output to a TextWriter.
    /// </summary>
    public interface IOutputFormatter
    {
        void Format(QueryResult result, TextWriter writer);
    }
}
