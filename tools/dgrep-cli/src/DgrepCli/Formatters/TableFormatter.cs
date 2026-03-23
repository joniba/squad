using System;
using System.IO;
using System.Linq;
using DgrepCli.Execution;

namespace DgrepCli.Formatters
{
    /// <summary>
    /// Formats query results as an aligned text table with headers and row count.
    /// </summary>
    public class TableFormatter : IOutputFormatter
    {
        public void Format(QueryResult result, TextWriter writer)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            if (result.Columns == null || result.Columns.Count == 0)
            {
                writer.WriteLine("(no columns)");
                return;
            }

            var columnCount = result.Columns.Count;

            // Calculate column widths: max of header and all row values
            var widths = new int[columnCount];
            for (int c = 0; c < columnCount; c++)
            {
                widths[c] = (result.Columns[c].Name ?? "").Length;
            }

            foreach (var row in result.Rows)
            {
                for (int c = 0; c < columnCount && c < row.Length; c++)
                {
                    var val = FormatValue(row[c]);
                    if (val.Length > widths[c])
                        widths[c] = val.Length;
                }
            }

            // Ensure minimum column width of 3 for readability
            for (int c = 0; c < columnCount; c++)
            {
                if (widths[c] < 3) widths[c] = 3;
            }

            // Print header
            for (int c = 0; c < columnCount; c++)
            {
                if (c > 0) writer.Write("  ");
                writer.Write((result.Columns[c].Name ?? "").PadRight(widths[c]));
            }
            writer.WriteLine();

            // Print separator
            for (int c = 0; c < columnCount; c++)
            {
                if (c > 0) writer.Write("  ");
                writer.Write(new string('-', widths[c]));
            }
            writer.WriteLine();

            // Print rows
            foreach (var row in result.Rows)
            {
                for (int c = 0; c < columnCount; c++)
                {
                    if (c > 0) writer.Write("  ");
                    var val = c < row.Length ? FormatValue(row[c]) : "";
                    writer.Write(val.PadRight(widths[c]));
                }
                writer.WriteLine();
            }

            // Print row count
            writer.WriteLine();
            writer.WriteLine($"({result.Rows.Count} row{(result.Rows.Count == 1 ? "" : "s")})");
        }

        private static string FormatValue(object value)
        {
            if (value == null) return "(null)";
            return value.ToString();
        }
    }
}
