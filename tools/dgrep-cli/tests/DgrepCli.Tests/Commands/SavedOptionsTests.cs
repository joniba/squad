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
        public void Saved_Save_ParsesNameAndFlags()
        {
            var result = Parse("saved", "save", "my-query",
                "--endpoint", "diag-prod",
                "--namespace", "MyNs",
                "--event", "MyEvt",
                "--query", "where Level <= 2");

            result.WithParsed<SavedOptions>(opts =>
            {
                Assert.Equal("save", opts.Action);
                Assert.Equal("my-query", opts.Name);
                Assert.Equal("diag-prod", opts.Endpoint);
                Assert.Equal("MyNs", opts.Namespace);
                Assert.Equal("MyEvt", opts.Event);
                Assert.Equal("where Level <= 2", opts.Query);
            });

            Assert.IsType<Parsed<object>>(result);
        }

        [Fact]
        public void Saved_Run_ParsesNameWithOverrides()
        {
            var result = Parse("saved", "run", "my-query",
                "--from", "-2h",
                "--to", "-30m");

            result.WithParsed<SavedOptions>(opts =>
            {
                Assert.Equal("run", opts.Action);
                Assert.Equal("my-query", opts.Name);
                Assert.Equal("-2h", opts.From);
                Assert.Equal("-30m", opts.To);
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
        public void Saved_Delete_ParsesName()
        {
            var result = Parse("saved", "delete", "old-query");

            result.WithParsed<SavedOptions>(opts =>
            {
                Assert.Equal("delete", opts.Action);
                Assert.Equal("old-query", opts.Name);
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
