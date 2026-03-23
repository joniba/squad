using CommandLine;
using DgrepCli.Commands;
using Xunit;

namespace DgrepCli.Tests.Commands
{
    public class ConfigOptionsTests
    {
        private static ParserResult<object> Parse(params string[] args)
        {
            var parser = new Parser(s => { s.HelpWriter = null; });
            return parser.ParseArguments<SearchOptions, TailOptions, ConfigOptions, SavedOptions>(args);
        }

        [Fact]
        public void Config_Set_ParsesKeyAndValue()
        {
            var result = Parse("config", "set", "default.endpoint", "diag-prod");

            result.WithParsed<ConfigOptions>(opts =>
            {
                Assert.Equal("set", opts.Action);
                Assert.Equal("default.endpoint", opts.Key);
                Assert.Equal("diag-prod", opts.Value);
            });

            Assert.IsType<Parsed<object>>(result);
        }

        [Fact]
        public void Config_Get_ParsesKey()
        {
            var result = Parse("config", "get", "default.endpoint");

            result.WithParsed<ConfigOptions>(opts =>
            {
                Assert.Equal("get", opts.Action);
                Assert.Equal("default.endpoint", opts.Key);
            });

            Assert.IsType<Parsed<object>>(result);
        }

        [Fact]
        public void Config_List_ParsesAction()
        {
            var result = Parse("config", "list");

            result.WithParsed<ConfigOptions>(opts =>
            {
                Assert.Equal("list", opts.Action);
            });

            Assert.IsType<Parsed<object>>(result);
        }

        [Fact]
        public void Config_MissingAction_Fails()
        {
            var result = Parse("config");
            Assert.IsType<NotParsed<object>>(result);
        }
    }
}
