using System.Collections.Generic;

namespace DgrepCli.Execution
{
    /// <summary>
    /// Structured result from a query execution: column definitions + row data.
    /// </summary>
    public class QueryResult
    {
        public List<ColumnDefinition> Columns { get; set; } = new List<ColumnDefinition>();
        public List<object[]> Rows { get; set; } = new List<object[]>();

        /// <summary>
        /// Total row count (may differ from Rows.Count if server truncated).
        /// </summary>
        public long TotalRowCount => Rows.Count;
    }

    /// <summary>
    /// Describes a single column in a query result.
    /// </summary>
    public class ColumnDefinition
    {
        public string Name { get; set; }
        public string Type { get; set; }

        public ColumnDefinition() { }

        public ColumnDefinition(string name, string type)
        {
            Name = name;
            Type = type;
        }
    }
}
