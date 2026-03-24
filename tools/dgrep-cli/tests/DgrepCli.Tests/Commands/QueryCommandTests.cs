using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using DgrepCli.Commands;
using DgrepCli.Config;
using DgrepCli.Execution;

namespace DgrepCli.Tests.Commands
{
    public class QueryCommandTests : IDisposable
    {
        private readonly string _tempConfigPath;
        private readonly ConfigManager _configManager;
        private readonly MockQueryExecutor _executor;
        private readonly StringWriter _stdout;
        private readonly StringWriter _stderr;

        public QueryCommandTests()
        {
            _tempConfigPath = Path.Combine(Path.GetTempPath(), $"dgrep-test-{Guid.NewGuid()}", "config.json");
            _configManager = new ConfigManager(_tempConfigPath);
            _executor = new MockQueryExecutor();
            _stdout = new StringWriter();
            _stderr = new StringWriter();
        }

        public void Dispose()
        {
            var dir = Path.GetDirectoryName(_tempConfigPath);
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }

        private QueryCommand CreateCommand()
        {
            return new QueryCommand(_executor, _configManager, _stdout, _stderr,
                                    new StringReader(""), stdinRedirected: false);
        }

        [Fact]
        public void Execute_SuccessfulQuery_ReturnsZeroAndFormatsOutput()
        {
            _executor.WithResult(MockQueryExecutor.CreateSimpleResult(
                new[] { "Name", "Value" },
                new[] { "string", "int" },
                new[] { new object[] { "test", 42 } }));

            var opts = new QueryVerbOptions
            {
                Query = "TestTable | take 1",
                Cluster = "https://cluster.kusto.windows.net",
                Database = "TestDB",
                Output = "table"
            };

            var cmd = CreateCommand();
            var exitCode = cmd.Execute(opts);

            Assert.Equal(0, exitCode);
            Assert.Contains("Name", _stdout.ToString());
            Assert.Contains("test", _stdout.ToString());
            Assert.Equal("TestTable | take 1", _executor.LastQuery);
        }

        [Fact]
        public void Execute_JsonOutput_WritesValidJson()
        {
            _executor.WithResult(MockQueryExecutor.CreateSimpleResult(
                new[] { "Id" },
                new[] { "int" },
                new[] { new object[] { 1 } }));

            var opts = new QueryVerbOptions
            {
                Query = "T | take 1",
                Cluster = "https://cluster",
                Database = "db",
                Output = "json"
            };

            var cmd = CreateCommand();
            var exitCode = cmd.Execute(opts);

            Assert.Equal(0, exitCode);
            var json = _stdout.ToString();
            Assert.Contains("\"Id\"", json);
        }

        [Fact]
        public void Execute_CsvOutput_WritesCsv()
        {
            _executor.WithResult(MockQueryExecutor.CreateSimpleResult(
                new[] { "A", "B" },
                new[] { "string", "int" },
                new[] { new object[] { "x", 1 } }));

            var opts = new QueryVerbOptions
            {
                Query = "T",
                Cluster = "https://cluster",
                Database = "db",
                Output = "csv"
            };

            var cmd = CreateCommand();
            var exitCode = cmd.Execute(opts);

            Assert.Equal(0, exitCode);
            Assert.Contains("A,B", _stdout.ToString());
        }

        [Fact]
        public void Execute_NoCluster_ReturnsError()
        {
            var opts = new QueryVerbOptions
            {
                Query = "T",
                Database = "db"
            };

            var cmd = CreateCommand();
            var exitCode = cmd.Execute(opts);

            Assert.Equal(1, exitCode);
            Assert.Contains("No cluster specified", _stderr.ToString());
        }

        [Fact]
        public void Execute_NoDatabase_ReturnsError()
        {
            var opts = new QueryVerbOptions
            {
                Query = "T",
                Cluster = "https://cluster"
            };

            var cmd = CreateCommand();
            var exitCode = cmd.Execute(opts);

            Assert.Equal(1, exitCode);
            Assert.Contains("No database specified", _stderr.ToString());
        }

        [Fact]
        public void Execute_ConnectionError_ShowsClusterUrl()
        {
            _executor.WithException(new QueryConnectionException(
                "https://bad.cluster", "Connection refused"));

            var opts = new QueryVerbOptions
            {
                Query = "T",
                Cluster = "https://bad.cluster",
                Database = "db"
            };

            var cmd = CreateCommand();
            var exitCode = cmd.Execute(opts);

            Assert.Equal(DgrepExitCodes.TransientFailure, exitCode);
            Assert.Contains("https://bad.cluster", _stderr.ToString());
        }

        [Fact]
        public void Execute_SyntaxError_ShowsErrorMessage()
        {
            _executor.WithException(new QuerySyntaxException("bad query", "Unexpected token 'xyz'"));

            var opts = new QueryVerbOptions
            {
                Query = "bad query",
                Cluster = "https://cluster",
                Database = "db"
            };

            var cmd = CreateCommand();
            var exitCode = cmd.Execute(opts);

            Assert.Equal(1, exitCode);
            Assert.Contains("syntax error", _stderr.ToString().ToLower());
        }

        [Fact]
        public void Execute_TimeoutError_ShowsTimeoutMessage()
        {
            _executor.WithException(new QueryTimeoutException(TimeSpan.FromSeconds(30)));

            var opts = new QueryVerbOptions
            {
                Query = "T",
                Cluster = "https://cluster",
                Database = "db"
            };

            var cmd = CreateCommand();
            var exitCode = cmd.Execute(opts);

            Assert.Equal(DgrepExitCodes.TransientFailure, exitCode);
            Assert.Contains("timed out", _stderr.ToString().ToLower());
        }

        [Fact]
        public void Execute_AuthError_PointsToAuthCommand()
        {
            _executor.WithException(new QueryAuthException("Token expired"));

            var opts = new QueryVerbOptions
            {
                Query = "T",
                Cluster = "https://cluster",
                Database = "db"
            };

            var cmd = CreateCommand();
            var exitCode = cmd.Execute(opts);

            Assert.Equal(DgrepExitCodes.AuthFailure, exitCode);
            Assert.Contains("dgrep auth", _stderr.ToString());
        }

        [Fact]
        public void Execute_ConfigDefaults_AppliedWhenCLIFlagsOmitted()
        {
            // Set config defaults
            var config = new DgrepConfig
            {
                DefaultCluster = "https://config-cluster",
                DefaultDatabase = "ConfigDB",
                DefaultMaxRows = 1000,
                OutputFormat = "json"
            };
            _configManager.Save(config);

            _executor.WithResult(new QueryResult
            {
                Columns = { new ColumnDefinition("X", "int") },
                Rows = { new object[] { 1 } }
            });

            var opts = new QueryVerbOptions { Query = "T | take 1" };

            var cmd = CreateCommand();
            var exitCode = cmd.Execute(opts);

            Assert.Equal(0, exitCode);
            // Config defaults should have been used
            Assert.Equal("https://config-cluster", _executor.LastOptions.Cluster);
            Assert.Equal("ConfigDB", _executor.LastOptions.Database);
            Assert.Equal(1000, _executor.LastOptions.MaxRows);
        }

        [Fact]
        public void Execute_CLIFlagsOverrideConfig()
        {
            var config = new DgrepConfig
            {
                DefaultCluster = "https://config-cluster",
                DefaultDatabase = "ConfigDB",
                DefaultMaxRows = 1000
            };
            _configManager.Save(config);

            _executor.WithResult(new QueryResult
            {
                Columns = { new ColumnDefinition("X", "int") },
                Rows = { new object[] { 1 } }
            });

            var opts = new QueryVerbOptions
            {
                Query = "T",
                Cluster = "https://cli-cluster",
                Database = "CliDB",
                MaxRows = 500
            };

            var cmd = CreateCommand();
            var exitCode = cmd.Execute(opts);

            Assert.Equal(0, exitCode);
            Assert.Equal("https://cli-cluster", _executor.LastOptions.Cluster);
            Assert.Equal("CliDB", _executor.LastOptions.Database);
            Assert.Equal(500, _executor.LastOptions.MaxRows);
        }

        [Fact]
        public void Execute_TimeoutCLIFlag_OverridesDefault()
        {
            _executor.WithResult(new QueryResult
            {
                Columns = { new ColumnDefinition("X", "int") },
                Rows = { new object[] { 1 } }
            });

            var opts = new QueryVerbOptions
            {
                Query = "T",
                Cluster = "https://cluster",
                Database = "db",
                Timeout = 60
            };

            var cmd = CreateCommand();
            cmd.Execute(opts);

            Assert.Equal(TimeSpan.FromSeconds(60), _executor.LastOptions.Timeout);
        }

        [Fact]
        public void Execute_QueryParameters_Parsed()
        {
            _executor.WithResult(new QueryResult
            {
                Columns = { new ColumnDefinition("X", "int") },
                Rows = { new object[] { 1 } }
            });

            var opts = new QueryVerbOptions
            {
                Query = "T | where Name == p_name",
                Cluster = "https://cluster",
                Database = "db",
                Parameters = new[] { "p_name=Alice", "p_age=30" }
            };

            var cmd = CreateCommand();
            cmd.Execute(opts);

            Assert.NotNull(_executor.LastOptions.Parameters);
            Assert.Equal("Alice", _executor.LastOptions.Parameters["p_name"]);
            Assert.Equal("30", _executor.LastOptions.Parameters["p_age"]);
        }

        [Fact]
        public void Execute_EmptyQuery_ReturnsError()
        {
            var opts = new QueryVerbOptions
            {
                Query = "   ",
                Cluster = "https://cluster",
                Database = "db"
            };

            var cmd = CreateCommand();
            var exitCode = cmd.Execute(opts);

            Assert.Equal(1, exitCode);
            Assert.Contains("No query provided", _stderr.ToString());
        }

        [Fact]
        public void ResolveTimeout_CLIValue_Used()
        {
            var timeout = QueryCommand.ResolveTimeout(60, new DgrepConfig());
            Assert.Equal(TimeSpan.FromSeconds(60), timeout);
        }

        [Fact]
        public void ResolveTimeout_ZeroCLI_DefaultUsed()
        {
            var timeout = QueryCommand.ResolveTimeout(0, new DgrepConfig());
            Assert.Equal(TimeSpan.FromSeconds(300), timeout);
        }

        [Fact]
        public void ResolveMaxRows_CLIValue_Used()
        {
            Assert.Equal(100, QueryCommand.ResolveMaxRows(100, 5000));
        }

        [Fact]
        public void ResolveMaxRows_ZeroCLI_ConfigUsed()
        {
            Assert.Equal(5000, QueryCommand.ResolveMaxRows(0, 5000));
        }

        [Fact]
        public void ResolveMaxRows_ZeroBoth_DefaultUsed()
        {
            Assert.Equal(500000, QueryCommand.ResolveMaxRows(0, null));
        }

        [Fact]
        public void ParseParameters_ValidPairs_Parsed()
        {
            var result = QueryCommand.ParseParameters(new[] { "key1=val1", "key2=val2" });
            Assert.Equal(2, result.Count);
            Assert.Equal("val1", result["key1"]);
        }

        [Fact]
        public void ParseParameters_Null_ReturnsNull()
        {
            Assert.Null(QueryCommand.ParseParameters(null));
        }

        [Fact]
        public void ParseParameters_EmptyStrings_ReturnsNull()
        {
            Assert.Null(QueryCommand.ParseParameters(new[] { "", "  " }));
        }

        [Fact]
        public void ParseParameters_ValueWithEquals_PreservesFullValue()
        {
            var result = QueryCommand.ParseParameters(new[] { "filter=Name==Test" });
            Assert.Equal("Name==Test", result["filter"]);
        }

        [Fact]
        public void Execute_StdinQuery_ReadsFromStdin()
        {
            _executor.WithResult(MockQueryExecutor.CreateSimpleResult(
                new[] { "Id" },
                new[] { "int" },
                new[] { new object[] { 1 } }));

            var stdinReader = new StringReader("StormEvents | take 5");
            var cmd = new QueryCommand(_executor, _configManager, _stdout, _stderr,
                                       stdinReader, stdinRedirected: true);

            var opts = new QueryVerbOptions
            {
                Cluster = "https://cluster",
                Database = "db"
            };

            var exitCode = cmd.Execute(opts);
            Assert.Equal(0, exitCode);
            Assert.Equal("StormEvents | take 5", _executor.LastQuery);
        }

        [Fact]
        public void Execute_EmptyStdin_ReturnsError()
        {
            var stdinReader = new StringReader("");
            var cmd = new QueryCommand(_executor, _configManager, _stdout, _stderr,
                                       stdinReader, stdinRedirected: true);

            var opts = new QueryVerbOptions
            {
                Cluster = "https://cluster",
                Database = "db"
            };

            var exitCode = cmd.Execute(opts);
            Assert.Equal(1, exitCode);
            Assert.Contains("empty", _stderr.ToString().ToLower());
        }
    }
}
