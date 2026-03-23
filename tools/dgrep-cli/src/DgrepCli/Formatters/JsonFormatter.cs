using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

using DgrepCli.Execution;

namespace DgrepCli.Formatters
{
    /// <summary>
    /// Formats query results as a structured JSON array of objects.
    /// Each row is an object with column names as keys.
    /// </summary>
    public class JsonFormatter : IOutputFormatter
    {
        private readonly bool _indented;

        public JsonFormatter(bool indented = true)
        {
            _indented = indented;
        }

        public void Format(QueryResult result, TextWriter writer)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            var rows = new List<Dictionary<string, object>>();

            foreach (var row in result.Rows)
            {
                var obj = new Dictionary<string, object>();
                for (int c = 0; c < result.Columns.Count && c < row.Length; c++)
                {
                    obj[result.Columns[c].Name ?? $"Column{c}"] = row[c];
                }
                rows.Add(obj);
            }

            var formatting = _indented ? Formatting.Indented : Formatting.None;
            var json = JsonConvert.SerializeObject(rows, formatting);
            writer.Write(json);
            writer.WriteLine();
        }
    }
}
