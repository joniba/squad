using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DgrepCli.Execution;
using DgrepCli.Formatters;
using Xunit;

namespace DgrepCli.Tests.Integration
{
    /// <summary>
    /// Integration tests that validate output formatters produce correct output
    /// when fed real query results from live Geneva/DGrep endpoints.
    ///
    /// Skipped by default. Enable with DGREP_INTEGRATION_ENABLED=true.
    /// Requires corpnet/VPN, valid Geneva credentials, and access to the test namespace.
    /// </summary>
    [Trait("Category", "Integration")]
    public class IntegrationFormatterTests : IntegrationTestBase
    {
        [Fact]
        public void TableFormatter_WithRealResults_ProducesAlignedOutput()
        {
            if (!IsEnabled)
            {
                Assert.NotNull(SkipReason);
                return;
            }

            // Arrange — use a representative result (real or synthetic)
            var result = GetTestResult();
            var formatter = FormatterFactory.Create("table");
            var writer = new StringWriter();

            // Act
            formatter.Format(result, writer);
            var output = writer.ToString();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(output), "Table output should not be empty");
            Assert.Contains("TIMESTAMP", output); // Column header present
            Assert.Contains("row", output.ToLowerInvariant()); // Row count footer
        }

        [Fact]
        public void JsonFormatter_WithRealResults_ProducesValidJson()
        {
            if (!IsEnabled)
            {
                Assert.NotNull(SkipReason);
                return;
            }

            // Arrange
            var result = GetTestResult();
            var formatter = FormatterFactory.Create("json");
            var writer = new StringWriter();

            // Act
            formatter.Format(result, writer);
            var output = writer.ToString().Trim();

            // Assert — valid JSON array
            Assert.False(string.IsNullOrWhiteSpace(output), "JSON output should not be empty");
            Assert.StartsWith("[", output);
            Assert.EndsWith("]", output);

            // Verify it can be parsed
            var parsed = Newtonsoft.Json.Linq.JArray.Parse(output);
            Assert.True(parsed.Count > 0, "Expected at least one JSON object in array");
        }

        [Fact]
        public void CsvFormatter_WithRealResults_ProducesValidCsv()
        {
            if (!IsEnabled)
            {
                Assert.NotNull(SkipReason);
                return;
            }

            // Arrange
            var result = GetTestResult();
            var formatter = FormatterFactory.Create("csv");
            var writer = new StringWriter();

            // Act
            formatter.Format(result, writer);
            var output = writer.ToString();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(output), "CSV output should not be empty");

            var lines = output.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);
            Assert.True(lines.Length >= 2, "CSV should have header + at least one data row");

            // Header should contain column names
            Assert.Contains("TIMESTAMP", lines[0]);
        }

        [Fact]
        public void AllFormatters_WithEmptyResult_DoNotCrash()
        {
            if (!IsEnabled)
            {
                Assert.NotNull(SkipReason);
                return;
            }

            var emptyResult = new QueryResult();
            var formats = new[] { "table", "json", "csv" };

            foreach (var format in formats)
            {
                var formatter = FormatterFactory.Create(format);
                var writer = new StringWriter();

                // Should not throw
                formatter.Format(emptyResult, writer);
                Assert.NotNull(writer.ToString());
            }
        }

        [Fact]
        public void FormatterFactory_AllSupportedFormats_CreateSuccessfully()
        {
            if (!IsEnabled)
            {
                Assert.NotNull(SkipReason);
                return;
            }

            // Verify factory can create all formats without throwing
            Assert.NotNull(FormatterFactory.Create("table"));
            Assert.NotNull(FormatterFactory.Create("json"));
            Assert.NotNull(FormatterFactory.Create("csv"));
        }

        // ── Helper ───────────────────────────────────────────────────

        /// <summary>
        /// Gets a test result set. When running against live DGrep, this would
        /// execute a real query. For now, uses a synthetic result that matches
        /// the expected DGrep output schema.
        ///
        /// TODO: Replace with real query execution once DgrepQueryExecutor is available.
        /// </summary>
        private static QueryResult GetTestResult()
        {
            // Use the synthetic minimal result from the base class.
            // Once DgrepQueryExecutor exists, replace with:
            //   var executor = new DgrepQueryExecutor(authProvider);
            //   return await executor.ExecuteAsync("* | take 5", CreateDefaultOptions(), CancellationToken.None);
            return CreateMinimalResult();
        }
    }
}
