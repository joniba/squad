using System.IO;
using System.Linq;
using Xunit;
using DgrepCli.Execution;
using DgrepCli.Formatters;

namespace DgrepCli.Tests.Formatters
{
    public class CsvFormatterTests
    {
        private readonly CsvFormatter _formatter = new CsvFormatter();

        private string Format(QueryResult result)
        {
            var sw = new StringWriter();
            _formatter.Format(result, sw);
            return sw.ToString();
        }

        [Fact]
        public void Format_SimpleResult_HeaderAndDataRows()
        {
            var result = MockQueryExecutor.CreateSimpleResult(
                new[] { "Name", "Age" },
                new[] { "string", "int" },
                new[] {
                    new object[] { "Alice", 30 },
                    new object[] { "Bob", 25 }
                });

            var output = Format(result);
            var lines = output.TrimEnd().Split('\n').Select(l => l.TrimEnd('\r')).ToArray();

            Assert.Equal(3, lines.Length);
            Assert.Equal("Name,Age", lines[0]);
            Assert.Equal("Alice,30", lines[1]);
            Assert.Equal("Bob,25", lines[2]);
        }

        [Fact]
        public void Format_EmptyResult_HeaderOnly()
        {
            var result = MockQueryExecutor.CreateSimpleResult(
                new[] { "Name", "Value" },
                new[] { "string", "string" },
                new object[0][]);

            var output = Format(result);
            var lines = output.TrimEnd().Split('\n').Select(l => l.TrimEnd('\r')).ToArray();

            Assert.Single(lines);
            Assert.Equal("Name,Value", lines[0]);
        }

        [Fact]
        public void Format_NoColumns_EmptyOutput()
        {
            var result = new QueryResult();
            var output = Format(result);
            Assert.Equal("", output);
        }

        [Fact]
        public void Format_CommaInValue_QuotedField()
        {
            var result = MockQueryExecutor.CreateSimpleResult(
                new[] { "Msg" },
                new[] { "string" },
                new[] { new object[] { "hello, world" } });

            var output = Format(result);
            Assert.Contains("\"hello, world\"", output);
        }

        [Fact]
        public void Format_QuoteInValue_DoubleQuoteEscaped()
        {
            var result = MockQueryExecutor.CreateSimpleResult(
                new[] { "Msg" },
                new[] { "string" },
                new[] { new object[] { "say \"hi\"" } });

            var output = Format(result);
            // RFC 4180: quotes inside quoted field are doubled
            Assert.Contains("\"say \"\"hi\"\"\"", output);
        }

        [Fact]
        public void Format_NewlineInValue_QuotedField()
        {
            var result = MockQueryExecutor.CreateSimpleResult(
                new[] { "Msg" },
                new[] { "string" },
                new[] { new object[] { "line1\nline2" } });

            var output = Format(result);
            Assert.Contains("\"line1\nline2\"", output);
        }

        [Fact]
        public void Format_NullValues_EmptyField()
        {
            var result = MockQueryExecutor.CreateSimpleResult(
                new[] { "A", "B" },
                new[] { "string", "string" },
                new[] { new object[] { "val", null } });

            var output = Format(result);
            var lines = output.TrimEnd().Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
            Assert.Equal("val,", lines[1]);
        }

        [Fact]
        public void Format_SingleColumn_NoTrailingComma()
        {
            var result = MockQueryExecutor.CreateSimpleResult(
                new[] { "Status" },
                new[] { "string" },
                new[] { new object[] { "OK" } });

            var output = Format(result);
            var lines = output.TrimEnd().Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
            Assert.Equal("Status", lines[0]);
            Assert.Equal("OK", lines[1]);
        }

        [Fact]
        public void EscapeField_PlainValue_NoQuotes()
        {
            Assert.Equal("hello", CsvFormatter.EscapeField("hello"));
        }

        [Fact]
        public void EscapeField_NullValue_EmptyString()
        {
            Assert.Equal("", CsvFormatter.EscapeField(null));
        }
    }
}
