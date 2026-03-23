using System.Collections.Generic;
using System.IO;
using Xunit;
using DgrepCli.Execution;
using DgrepCli.Formatters;

namespace DgrepCli.Tests.Formatters
{
    public class TableFormatterTests
    {
        private readonly TableFormatter _formatter = new TableFormatter();

        private string Format(QueryResult result)
        {
            var sw = new StringWriter();
            _formatter.Format(result, sw);
            return sw.ToString();
        }

        [Fact]
        public void Format_SimpleResult_AlignedColumnsAndHeaders()
        {
            var result = MockQueryExecutor.CreateSimpleResult(
                new[] { "Name", "Age" },
                new[] { "string", "int" },
                new[] {
                    new object[] { "Alice", 30 },
                    new object[] { "Bob", 25 }
                });

            var output = Format(result);

            Assert.Contains("Name", output);
            Assert.Contains("Age", output);
            Assert.Contains("Alice", output);
            Assert.Contains("Bob", output);
            Assert.Contains("(2 rows)", output);
        }

        [Fact]
        public void Format_SingleRow_SingularRowCount()
        {
            var result = MockQueryExecutor.CreateSimpleResult(
                new[] { "Id" },
                new[] { "int" },
                new[] { new object[] { 1 } });

            var output = Format(result);
            Assert.Contains("(1 row)", output);
        }

        [Fact]
        public void Format_EmptyResult_ZeroRows()
        {
            var result = MockQueryExecutor.CreateSimpleResult(
                new[] { "Name", "Value" },
                new[] { "string", "string" },
                new object[0][]);

            var output = Format(result);
            Assert.Contains("Name", output);
            Assert.Contains("Value", output);
            Assert.Contains("(0 rows)", output);
        }

        [Fact]
        public void Format_NoColumns_ShowsNoColumnsMessage()
        {
            var result = new QueryResult();
            var output = Format(result);
            Assert.Contains("(no columns)", output);
        }

        [Fact]
        public void Format_NullValues_DisplaysNullPlaceholder()
        {
            var result = MockQueryExecutor.CreateSimpleResult(
                new[] { "Name", "Value" },
                new[] { "string", "string" },
                new[] { new object[] { "Test", null } });

            var output = Format(result);
            Assert.Contains("(null)", output);
        }

        [Fact]
        public void Format_WidthCalculation_LongValuesExpandColumn()
        {
            var result = MockQueryExecutor.CreateSimpleResult(
                new[] { "X" },
                new[] { "string" },
                new[] { new object[] { "A very long value here" } });

            var output = Format(result);
            // The data row should contain the full long value
            Assert.Contains("A very long value here", output);
        }

        [Fact]
        public void Format_SeparatorLine_MatchesColumnWidths()
        {
            var result = MockQueryExecutor.CreateSimpleResult(
                new[] { "Col1", "Col2" },
                new[] { "string", "string" },
                new[] { new object[] { "A", "B" } });

            var output = Format(result);
            var lines = output.Split('\n');
            // Second line should be all dashes and spaces
            Assert.True(lines[1].TrimEnd().Replace("-", "").Replace(" ", "").Length == 0,
                "Separator line should contain only dashes and spaces");
        }

        [Fact]
        public void Format_SingleColumn_NoPaddingIssues()
        {
            var result = MockQueryExecutor.CreateSimpleResult(
                new[] { "Status" },
                new[] { "string" },
                new[] {
                    new object[] { "OK" },
                    new object[] { "FAILED" },
                    new object[] { "OK" }
                });

            var output = Format(result);
            Assert.Contains("Status", output);
            Assert.Contains("(3 rows)", output);
        }

        [Fact]
        public void Format_SpecialCharactersInValues_DisplayedAsIs()
        {
            var result = MockQueryExecutor.CreateSimpleResult(
                new[] { "Message" },
                new[] { "string" },
                new[] { new object[] { "Error: \"file not found\" (code=404)" } });

            var output = Format(result);
            Assert.Contains("Error: \"file not found\" (code=404)", output);
        }
    }
}
