using CommandLine;
using DgrepCli.Commands;
using Xunit;

namespace DgrepCli.Tests.Commands
{
    public class SavedOptionsTests
    {
        private static ParserResult<object> Parse(params string[] args)
        {
            var parser = new Parser(s => { s.HelpWriter = null; });
            return parser.ParseArguments<SearchOptions, TailOptions, ConfigOptions, SavedOptions>(args);
        }

        [Fact]
        public void Saved_Add_ParsesNameAndFlags()
        {
            var result = Parse("saved", "add", "my-query",
                "--query", "TestTable | where Status == '{{status}}'",
                "--description", "Filter by status",
                "--cluster", "https://c.kusto.windows.net",
                "--database", "MyDB");

            result.WithParsed<SavedOptions>(opts =>
            {
                Assert.Equal("add", opts.Action);
                Assert.Equal("my-query", opts.Name);
                Assert.Equal("TestTable | where Status == '{{status}}'", opts.Query);
                Assert.Equal("Filter by status", opts.Description);
                Assert.Equal("https://c.kusto.windows.net", opts.Cluster);
                Assert.Equal("MyDB", opts.Database);
            });

            Assert.IsType<Parsed<object>>(result);
        }

        [Fact]
        public void Saved_Run_ParsesNameWithParams()
        {
            var result = Parse("saved", "run", "my-query",
                "--param", "key1=val1,key2=val2",
                "--output", "json");

            result.WithParsed<SavedOptions>(opts =>
            {
                Assert.Equal("run", opts.Action);
                Assert.Equal("my-query", opts.Name);
                Assert.Equal("json", opts.Output);
            });

            Assert.IsType<Parsed<object>>(result);
        }

        [Fact]
        public void Saved_List_ParsesAction()
        {
            var result = Parse("saved", "list");

            result.WithParsed<SavedOptions>(opts =>
            {
                Assert.Equal("list", opts.Action);
            });

            Assert.IsType<Parsed<object>>(result);
        }

        [Fact]
        public void Saved_Remove_ParsesName()
        {
            var result = Parse("saved", "remove", "old-query");

            result.WithParsed<SavedOptions>(opts =>
            {
                Assert.Equal("remove", opts.Action);
                Assert.Equal("old-query", opts.Name);
            });

            Assert.IsType<Parsed<object>>(result);
        }

        [Fact]
        public void Saved_Show_ParsesName()
        {
            var result = Parse("saved", "show", "my-query");

            result.WithParsed<SavedOptions>(opts =>
            {
                Assert.Equal("show", opts.Action);
                Assert.Equal("my-query", opts.Name);
            });

            Assert.IsType<Parsed<object>>(result);
        }

        [Fact]
        public void Saved_MissingAction_Fails()
        {
            var result = Parse("saved");
            Assert.IsType<NotParsed<object>>(result);
        }
    }
}
