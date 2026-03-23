using System;
using System.IO;
using System.Text;
using DgrepCli.Execution;

namespace DgrepCli.Formatters
{
    /// <summary>
    /// Formats query results as RFC 4180 compliant CSV.
    /// Fields containing commas, quotes, or newlines are quoted.
    /// </summary>
    public class CsvFormatter : IOutputFormatter
    {
        public void Format(QueryResult result, TextWriter writer)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            if (result.Columns == null || result.Columns.Count == 0)
                return;

            // Header row
            for (int c = 0; c < result.Columns.Count; c++)
            {
                if (c > 0) writer.Write(',');
                writer.Write(EscapeField(result.Columns[c].Name ?? ""));
            }
            writer.WriteLine();

            // Data rows
            foreach (var row in result.Rows)
            {
                for (int c = 0; c < result.Columns.Count; c++)
                {
                    if (c > 0) writer.Write(',');
                    var val = c < row.Length ? FormatValue(row[c]) : "";
                    writer.Write(EscapeField(val));
                }
                writer.WriteLine();
            }
        }

        /// <summary>
        /// RFC 4180: fields containing comma, double-quote, or newline must be
        /// enclosed in double-quotes. Double-quotes within are escaped as "".
        /// </summary>
        public static string EscapeField(string field)
        {
            if (field == null) return "";

            bool needsQuoting = field.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
            if (!needsQuoting) return field;

            var sb = new StringBuilder(field.Length + 4);
            sb.Append('"');
            foreach (var ch in field)
            {
                if (ch == '"') sb.Append('"'); // escape quote by doubling
                sb.Append(ch);
            }
            sb.Append('"');
            return sb.ToString();
        }

        private static string FormatValue(object value)
        {
            if (value == null) return "";
            return value.ToString();
        }
    }
}
