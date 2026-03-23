using System.IO;
using Newtonsoft.Json.Linq;
using Xunit;
using DgrepCli.Execution;
using DgrepCli.Formatters;

namespace DgrepCli.Tests.Formatters
{
    public class JsonFormatterTests
    {
        private readonly JsonFormatter _formatter = new JsonFormatter(indented: true);

        private string Format(QueryResult result)
        {
            var sw = new StringWriter();
            _formatter.Format(result, sw);
            return sw.ToString();
        }

        [Fact]
        public void Format_SimpleResult_ValidJsonArray()
        {
            var result = MockQueryExecutor.CreateSimpleResult(
                new[] { "Name", "Age" },
                new[] { "string", "int" },
                new[] {
                    new object[] { "Alice", 30 },
                    new object[] { "Bob", 25 }
                });

            var output = Format(result);
            var arr = JArray.Parse(output);

            Assert.Equal(2, arr.Count);
            Assert.Equal("Alice", arr[0]["Name"].ToString());
            Assert.Equal(30, arr[0]["Age"].Value<int>());
            Assert.Equal("Bob", arr[1]["Name"].ToString());
        }

        [Fact]
        public void Format_EmptyResult_EmptyJsonArray()
        {
            var result = MockQueryExecutor.CreateSimpleResult(
                new[] { "Name" },
                new[] { "string" },
                new object[0][]);

            var output = Format(result);
            var arr = JArray.Parse(output);
            Assert.Empty(arr);
        }

        [Fact]
        public void Format_NullValues_JsonNull()
        {
            var result = MockQueryExecutor.CreateSimpleResult(
                new[] { "Key", "Value" },
                new[] { "string", "string" },
                new[] { new object[] { "test", null } });

            var output = Format(result);
            var arr = JArray.Parse(output);
            Assert.Equal(JTokenType.Null, arr[0]["Value"].Type);
        }

        [Fact]
        public void Format_SingleColumn_ObjectsWithOneKey()
        {
            var result = MockQueryExecutor.CreateSimpleResult(
                new[] { "Count" },
                new[] { "long" },
                new[] { new object[] { 42 } });

            var output = Format(result);
            var arr = JArray.Parse(output);
            Assert.Single(arr);
            Assert.Equal(42, arr[0]["Count"].Value<int>());
        }

        [Fact]
        public void Format_SpecialCharacters_EscapedInJson()
        {
            var result = MockQueryExecutor.CreateSimpleResult(
                new[] { "Msg" },
                new[] { "string" },
                new[] { new object[] { "Line1\nLine2\tTabbed \"quoted\"" } });

            var output = Format(result);
            var arr = JArray.Parse(output); // Should not throw
            Assert.Contains("Line1", arr[0]["Msg"].ToString());
        }

        [Fact]
        public void Format_CompactMode_NoIndentation()
        {
            var compact = new JsonFormatter(indented: false);
            var result = MockQueryExecutor.CreateSimpleResult(
                new[] { "A" },
                new[] { "string" },
                new[] { new object[] { "val" } });

            var sw = new StringWriter();
            compact.Format(result, sw);
            var output = sw.ToString().Trim();

            // Compact JSON should not contain newlines within the JSON body
            Assert.DoesNotContain("\n  ", output);
        }

        [Fact]
        public void Format_NoColumns_EmptyArray()
        {
            var result = new QueryResult();
            var output = Format(result);
            var arr = JArray.Parse(output);
            Assert.Empty(arr);
        }
    }
}
