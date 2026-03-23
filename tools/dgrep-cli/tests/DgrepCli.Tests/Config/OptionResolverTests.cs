using System.Collections.Generic;
using DgrepCli.Commands;
using DgrepCli.Config;
using Xunit;

namespace DgrepCli.Tests.Config
{
    public class OptionResolverTests
    {
        private static SearchOptions MakeSearchOpts(
            string endpoint = null,
            string ns = null,
            string evt = "TestEvent",
            string from = null,
            string to = "now",
            string query = "where 1==1",
            string queryType = null,
            int maxRows = 500000,
            string output = null,
            string certPath = null)
        {
            return new SearchOptions
            {
                Endpoint = endpoint,
                Namespace = ns,
                Event = evt,
                From = from,
                To = to,
                Query = query,
                QueryType = queryType,
                MaxRows = maxRows,
                Output = output,
                CertPath = certPath,
                Identity = new List<string>()
            };
        }

        // --- CLI flags take priority ---

        [Fact]
        public void CliFlag_Wins_OverConfigAndDefault()
        {
            var config = new DgrepConfig
            {
                DefaultEndpoint = "config-endpoint",
                OutputFormat = "csv"
            };
            var opts = MakeSearchOpts(endpoint: "cli-endpoint", output: "json");
            var resolver = new OptionResolver(config);

            var resolved = resolver.Resolve(opts);

            Assert.Equal("cli-endpoint", resolved.Endpoint);
            Assert.Equal("json", resolved.Output);
        }

        [Fact]
        public void CliFlag_MaxRows_Wins_WhenDifferentFromDefault()
        {
            var config = new DgrepConfig { DefaultMaxRows = 100 };
            var opts = MakeSearchOpts(maxRows: 250);
            var resolver = new OptionResolver(config);

            var resolved = resolver.Resolve(opts);

            Assert.Equal(250, resolved.MaxRows);
        }

        // --- Config fills gaps ---

        [Fact]
        public void Config_FillsGaps_WhenCliFlagNotSet()
        {
            var config = new DgrepConfig
            {
                DefaultEndpoint = "config-endpoint",
                DefaultNamespace = "config-ns",
                OutputFormat = "csv",
                DefaultQueryType = "mql",
                CertificatePath = @"C:\cert.pfx"
            };
            var opts = MakeSearchOpts(); // all null/defaults
            var resolver = new OptionResolver(config);

            var resolved = resolver.Resolve(opts);

            Assert.Equal("config-endpoint", resolved.Endpoint);
            Assert.Equal("config-ns", resolved.Namespace);
            Assert.Equal("csv", resolved.Output);
            Assert.Equal("mql", resolved.QueryType);
            Assert.Equal(@"C:\cert.pfx", resolved.CertPath);
        }

        [Fact]
        public void Config_MaxRows_UsedWhenCliIsDefault()
        {
            var config = new DgrepConfig { DefaultMaxRows = 999 };
            var opts = MakeSearchOpts(); // maxRows = 500000 (attr default)
            var resolver = new OptionResolver(config);

            var resolved = resolver.Resolve(opts);

            Assert.Equal(999, resolved.MaxRows);
        }

        [Fact]
        public void Config_TimeRange_UsedAsFromDefault()
        {
            var config = new DgrepConfig { DefaultTimeRange = "4h" };
            var opts = MakeSearchOpts(); // from = null
            var resolver = new OptionResolver(config);

            var resolved = resolver.Resolve(opts);

            Assert.Equal("-4h", resolved.From);
        }

        // --- Hardcoded defaults are last resort ---

        [Fact]
        public void HardcodedDefaults_WhenNoCliOrConfig()
        {
            var config = new DgrepConfig(); // empty
            var opts = MakeSearchOpts();
            var resolver = new OptionResolver(config);

            var resolved = resolver.Resolve(opts);

            Assert.Equal("table", resolved.Output);
            Assert.Equal("kql", resolved.QueryType);
            Assert.Equal(500000, resolved.MaxRows);
            Assert.Equal("-1h", resolved.From);
        }

        [Fact]
        public void HardcodedDefaults_NullFieldsStayNull()
        {
            var config = new DgrepConfig();
            var opts = MakeSearchOpts();
            var resolver = new OptionResolver(config);

            var resolved = resolver.Resolve(opts);

            Assert.Null(resolved.Endpoint); // no default for endpoint
            Assert.Null(resolved.Namespace); // no default for namespace
            Assert.Null(resolved.CertPath);
        }

        // --- NegateTimeRange ---

        [Fact]
        public void NegateTimeRange_AddsMinusPrefix()
        {
            Assert.Equal("-1h", OptionResolver.NegateTimeRange("1h"));
            Assert.Equal("-30m", OptionResolver.NegateTimeRange("30m"));
            Assert.Equal("-7d", OptionResolver.NegateTimeRange("7d"));
        }

        [Fact]
        public void NegateTimeRange_AlreadyNegative_NoChange()
        {
            Assert.Equal("-2h", OptionResolver.NegateTimeRange("-2h"));
        }

        [Fact]
        public void NegateTimeRange_NullOrEmpty_ReturnsNull()
        {
            Assert.Null(OptionResolver.NegateTimeRange(null));
            Assert.Null(OptionResolver.NegateTimeRange(""));
        }

        // --- Null config safety ---

        [Fact]
        public void NullConfig_FallsBackToDefaults()
        {
            var resolver = new OptionResolver(null);
            var opts = MakeSearchOpts();

            var resolved = resolver.Resolve(opts);

            Assert.Equal("table", resolved.Output);
            Assert.Equal("kql", resolved.QueryType);
            Assert.Equal(500000, resolved.MaxRows);
        }

        // --- Full priority chain example ---

        [Fact]
        public void FullPriorityChain_CliOverridesConfigOverridesDefault()
        {
            var config = new DgrepConfig
            {
                DefaultEndpoint = "config-ep",
                OutputFormat = "csv",       // should be overridden by CLI
                DefaultQueryType = "mql",   // should be used (no CLI override)
                DefaultMaxRows = 100        // should be used (CLI is at attr default)
            };
            var opts = MakeSearchOpts(
                endpoint: "cli-ep",     // overrides config
                output: "jsonl"         // overrides config
            );
            var resolver = new OptionResolver(config);

            var resolved = resolver.Resolve(opts);

            Assert.Equal("cli-ep", resolved.Endpoint);      // CLI wins
            Assert.Equal("jsonl", resolved.Output);          // CLI wins
            Assert.Equal("mql", resolved.QueryType);         // config fills gap
            Assert.Equal(100, resolved.MaxRows);             // config fills gap
        }
    }
}
