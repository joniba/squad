using System;

namespace DgrepCli.Formatters
{
    /// <summary>
    /// Factory that returns the correct formatter for a given output format string.
    /// </summary>
    public static class FormatterFactory
    {
        public static IOutputFormatter Create(string format)
        {
            switch ((format ?? "table").ToLowerInvariant())
            {
                case "table": return new TableFormatter();
                case "json": return new JsonFormatter(indented: true);
                case "csv": return new CsvFormatter();
                default:
                    throw new ArgumentException(
                        $"Unknown output format '{format}'. Supported: table, json, csv.");
            }
        }
    }
}
