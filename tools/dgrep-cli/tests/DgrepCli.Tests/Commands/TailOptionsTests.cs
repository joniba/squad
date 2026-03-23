using System.Linq;
using CommandLine;
using DgrepCli.Commands;
using Xunit;

namespace DgrepCli.Tests.Commands
{
    public class TailOptionsTests
    {
        private static ParserResult<object> Parse(params string[] args)
        {
            var parser = new Parser(s => { s.HelpWriter = null; });
            return parser.ParseArguments<SearchOptions, TailOptions, ConfigOptions, SavedOptions>(args);
        }

        [Fact]
        public void ValidTail_ParsesAllRequiredFlags()
        {
            var result = Parse("tail",
                "--endpoint", "diag-prod",
                "--namespace", "MyNs",
                "--event", "MyEvt",
                "--from", "-5m",
                "--query", "where Level <= 2");

            result.WithParsed<TailOptions>(opts =>
            {
                Assert.Equal("diag-prod", opts.Endpoint);
                Assert.Equal("MyNs", opts.Namespace);
                Assert.Equal("MyEvt", opts.Event);
                Assert.Equal("-5m", opts.From);
                Assert.Equal("where Level <= 2", opts.Query);
            });

            Assert.IsType<Parsed<object>>(result);
        }

        [Fact]
        public void ValidTail_DefaultInterval()
        {
            var result = Parse("tail",
                "--endpoint", "diag-prod",
                "--namespace", "MyNs",
                "--event", "MyEvt",
                "--from", "-5m",
                "--query", "where 1==1");

            result.WithParsed<TailOptions>(opts =>
            {
                Assert.Equal(30, opts.Interval);
            });
        }

        [Fact]
        public void ValidTail_CustomInterval()
        {
            var result = Parse("tail",
                "--endpoint", "diag-prod",
                "--namespace", "MyNs",
                "--event", "MyEvt",
                "--from", "-5m",
                "--query", "where 1==1",
                "--interval", "10");

            result.WithParsed<TailOptions>(opts =>
            {
                Assert.Equal(10, opts.Interval);
            });
        }

        [Fact]
        public void Tail_MissingEndpoint_Fails()
        {
            var result = Parse("tail",
                "--namespace", "MyNs",
                "--event", "MyEvt",
                "--from", "-5m",
                "--query", "where 1==1");

            Assert.IsType<NotParsed<object>>(result);
        }
    }
}
