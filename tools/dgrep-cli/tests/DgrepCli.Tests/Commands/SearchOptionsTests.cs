using System.Linq;
using CommandLine;
using DgrepCli.Commands;
using Xunit;

namespace DgrepCli.Tests.Commands
{
    public class SearchOptionsTests
    {
        private static ParserResult<object> Parse(params string[] args)
        {
            var parser = new Parser(s => { s.HelpWriter = null; });
            return parser.ParseArguments<SearchOptions, TailOptions, ConfigOptions, SavedOptions>(args);
        }

        [Fact]
        public void ValidSearch_ParsesAllRequiredFlags()
        {
            var result = Parse("search",
                "--endpoint", "diag-prod",
                "--namespace", "MyNamespace",
                "--event", "MyEvent",
                "--from", "-1h",
                "--query", "where Level <= 2");

            result.WithParsed<SearchOptions>(opts =>
            {
                Assert.Equal("diag-prod", opts.Endpoint);
                Assert.Equal("MyNamespace", opts.Namespace);
                Assert.Equal("MyEvent", opts.Event);
                Assert.Equal("-1h", opts.From);
                Assert.Equal("where Level <= 2", opts.Query);
            });

            Assert.IsType<Parsed<object>>(result);
        }

        [Fact]
        public void ValidSearch_DefaultValues()
        {
            var result = Parse("search",
                "--endpoint", "diag-prod",
                "--namespace", "MyNs",
                "--event", "MyEvt",
                "--from", "-30m",
                "--query", "where 1==1");

            result.WithParsed<SearchOptions>(opts =>
            {
                Assert.Equal("now", opts.To);
                Assert.Equal("kql", opts.QueryType);
                Assert.Equal(500000, opts.MaxRows);
                Assert.Equal("table", opts.Output);
                Assert.Null(opts.CertPath);
                Assert.Null(opts.Version);
            });
        }

        [Fact]
        public void ValidSearch_AllOptionalFlags()
        {
            var result = Parse("search",
                "--endpoint", "diag-int",
                "--namespace", "^MyNs$",
                "--event", "^MyEvt$",
                "--version", "^Ver2v0$",
                "--from", "2026-03-23T10:00:00Z",
                "--to", "2026-03-23T11:00:00Z",
                "--query", "where Level <= 2",
                "--query-type", "mql",
                "--max-rows", "1000",
                "-o", "json",
                "--cert", "C:\\certs\\my.pfx",
                "--identity", "Tenant=WUS", "--identity", "Role=Frontend");

            result.WithParsed<SearchOptions>(opts =>
            {
                Assert.Equal("diag-int", opts.Endpoint);
                Assert.Equal("^MyNs$", opts.Namespace);
                Assert.Equal("^MyEvt$", opts.Event);
                Assert.Equal("^Ver2v0$", opts.Version);
                Assert.Equal("2026-03-23T10:00:00Z", opts.From);
                Assert.Equal("2026-03-23T11:00:00Z", opts.To);
                Assert.Equal("mql", opts.QueryType);
                Assert.Equal(1000, opts.MaxRows);
                Assert.Equal("json", opts.Output);
                Assert.Equal("C:\\certs\\my.pfx", opts.CertPath);
                Assert.Contains("Tenant=WUS", opts.Identity);
                Assert.Contains("Role=Frontend", opts.Identity);
            });
        }

        [Fact]
        public void Search_MissingRequiredEndpoint_Fails()
        {
            var result = Parse("search",
                "--namespace", "MyNs",
                "--event", "MyEvt",
                "--from", "-1h",
                "--query", "where 1==1");

            Assert.IsType<NotParsed<object>>(result);
        }

        [Fact]
        public void Search_MissingRequiredQuery_Fails()
        {
            var result = Parse("search",
                "--endpoint", "diag-prod",
                "--namespace", "MyNs",
                "--event", "MyEvt",
                "--from", "-1h");

            Assert.IsType<NotParsed<object>>(result);
        }

        [Fact]
        public void Search_MissingRequiredFrom_Fails()
        {
            var result = Parse("search",
                "--endpoint", "diag-prod",
                "--namespace", "MyNs",
                "--event", "MyEvt",
                "--query", "where 1==1");

            Assert.IsType<NotParsed<object>>(result);
        }
    }
}
