using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using DgrepCli.Commands;
using DgrepCli.Config;
using DgrepCli.Execution;

namespace DgrepCli.Tests.Commands
{
    public class SavedCommandTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _configPath;
        private readonly ConfigManager _configManager;
        private readonly MockQueryExecutor _executor;
        private readonly StringWriter _stdout;
        private readonly StringWriter _stderr;

        public SavedCommandTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "dgrep-test-saved-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(_tempDir);
            _configPath = Path.Combine(_tempDir, "config.json");
            _configManager = new ConfigManager(_configPath);
            _executor = new MockQueryExecutor();
            _stdout = new StringWriter();
            _stderr = new StringWriter();
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        private SavedCommand CreateCommand()
        {
            return new SavedCommand(_executor, _configManager, _stdout, _stderr);
        }

        private void SeedQuery(string name, string query, string description = null,
                               string endpoint = null, string ns = null, string evt = null)
        {
            var config = _configManager.Load();
            config.SavedQueries[name] = new SavedQuery
            {
                Query = query,
                Description = description,
                Endpoint = endpoint,
                Namespace = ns,
                Event = evt
            };
            _configManager.Save(config);
        }

        // ============================================================
        // LIST
        // ============================================================

        [Fact]
        public void List_EmptyConfig_SeedsBuiltInQueriesAndShowsThem()
        {
            var cmd = CreateCommand();
            var exit = cmd.Execute(new SavedOptions { Action = "list" });

            Assert.Equal(0, exit);
            var output = _stdout.ToString();
            Assert.Contains("recent-errors", output);
            Assert.Contains("top-messages", output);
            Assert.Contains("sample-events", output);
        }

        [Fact]
        public void List_WithSavedQueries_ShowsAllWithDescriptions()
        {
            SeedQuery("my-query", "TestTable | take 10", "A test query");
            SeedQuery("another", "Logs | where Level == 'Error'", "Error logs");

            var cmd = CreateCommand();
            var exit = cmd.Execute(new SavedOptions { Action = "list" });

            Assert.Equal(0, exit);
            var output = _stdout.ToString();
            Assert.Contains("my-query", output);
            Assert.Contains("A test query", output);
            Assert.Contains("another", output);
            Assert.Contains("Error logs", output);
        }

        [Fact]
        public void List_QueryWithNoDescription_ShowsPlaceholder()
        {
            SeedQuery("nodesc", "TestTable | take 1");

            var cmd = CreateCommand();
            var exit = cmd.Execute(new SavedOptions { Action = "list" });

            Assert.Equal(0, exit);
            var output = _stdout.ToString();
            Assert.Contains("nodesc", output);
            Assert.Contains("(no description)", output);
        }

        // ============================================================
        // ADD
        // ============================================================

        [Fact]
        public void Add_NewQuery_SavesAndConfirms()
        {
            // Pre-seed a query so built-in queries don't auto-populate
            SeedQuery("existing", "X");

            var cmd = CreateCommand();
            var exit = cmd.Execute(new SavedOptions
            {
                Action = "add",
                Name = "new-query",
                Query = "source | where Status == '{{status}}'",
                Description = "Filter by status",
                Namespace = "MyNs",
                Event = "MyEvent",
                Endpoint = "https://production.diagnostics.monitoring.core.windows.net/"
            });

            Assert.Equal(0, exit);
            Assert.Contains("added", _stdout.ToString());

            // Verify persistence
            var config = _configManager.Load();
            Assert.True(config.SavedQueries.ContainsKey("new-query"));
            var saved = config.SavedQueries["new-query"];
            Assert.Equal("source | where Status == '{{status}}'", saved.Query);
            Assert.Equal("Filter by status", saved.Description);
            Assert.Equal("MyNs", saved.Namespace);
            Assert.Equal("MyEvent", saved.Event);
            Assert.Equal("https://production.diagnostics.monitoring.core.windows.net/", saved.Endpoint);
        }

        [Fact]
        public void Add_DuplicateName_ReturnsError()
        {
            SeedQuery("dupe", "TestTable");

            var cmd = CreateCommand();
            var exit = cmd.Execute(new SavedOptions
            {
                Action = "add",
                Name = "dupe",
                Query = "Other"
            });

            Assert.Equal(1, exit);
            Assert.Contains("already exists", _stderr.ToString());
        }

        [Fact]
        public void Add_MinimalQuery_NoDescriptionOrDefaults()
        {
            SeedQuery("placeholder", "X");

            var cmd = CreateCommand();
            var exit = cmd.Execute(new SavedOptions
            {
                Action = "add",
                Name = "minimal",
                Query = "TestTable | take 1"
            });

            Assert.Equal(0, exit);
            var config = _configManager.Load();
            var saved = config.SavedQueries["minimal"];
            Assert.Equal("TestTable | take 1", saved.Query);
            Assert.Null(saved.Description);
            Assert.Null(saved.Namespace);
            Assert.Null(saved.Event);
            Assert.Null(saved.Endpoint);
        }

        // ============================================================
        // REMOVE
        // ============================================================

        [Fact]
        public void Remove_ExistingQuery_RemovesAndConfirms()
        {
            SeedQuery("to-remove", "TestTable");

            var cmd = CreateCommand();
            var exit = cmd.Execute(new SavedOptions { Action = "remove", Name = "to-remove" });

            Assert.Equal(0, exit);
            Assert.Contains("removed", _stdout.ToString());

            var config = _configManager.Load();
            Assert.False(config.SavedQueries.ContainsKey("to-remove"));
        }

        [Fact]
        public void Remove_NonExistent_ReturnsError()
        {
            SeedQuery("placeholder", "X");

            var cmd = CreateCommand();
            var exit = cmd.Execute(new SavedOptions { Action = "remove", Name = "ghost" });

            Assert.Equal(1, exit);
            Assert.Contains("No saved query named 'ghost'", _stderr.ToString());
        }

        // ============================================================
        // SHOW
        // ============================================================

        [Fact]
        public void Show_ExistingQuery_DisplaysAllDetails()
        {
            SeedQuery("detailed", "source | where Env == '{{environment}}'",
                       "Query by environment",
                       endpoint: "https://production.diagnostics.monitoring.core.windows.net/",
                       ns: "ProdNs", evt: "Log");

            var cmd = CreateCommand();
            var exit = cmd.Execute(new SavedOptions { Action = "show", Name = "detailed" });

            Assert.Equal(0, exit);
            var output = _stdout.ToString();
            Assert.Contains("detailed", output);
            Assert.Contains("Query by environment", output);
            Assert.Contains("https://production.diagnostics.monitoring.core.windows.net/", output);
            Assert.Contains("ProdNs", output);
            Assert.Contains("environment", output); // parameter name
            Assert.Contains("source | where Env == '{{environment}}'", output);
        }

        [Fact]
        public void Show_QueryWithMultipleParams_ListsAll()
        {
            SeedQuery("multi-param", "T | where A == '{{param1}}' and B == '{{param2}}' and C == '{{param1}}'");

            var cmd = CreateCommand();
            var exit = cmd.Execute(new SavedOptions { Action = "show", Name = "multi-param" });

            Assert.Equal(0, exit);
            var output = _stdout.ToString();
            Assert.Contains("param1", output);
            Assert.Contains("param2", output);
        }

        [Fact]
        public void Show_NonExistent_ReturnsError()
        {
            SeedQuery("placeholder", "X");

            var cmd = CreateCommand();
            var exit = cmd.Execute(new SavedOptions { Action = "show", Name = "nope" });

            Assert.Equal(1, exit);
            Assert.Contains("No saved query named 'nope'", _stderr.ToString());
        }

        // ============================================================
        // RUN
        // ============================================================

        [Fact]
        public void Run_SimpleQuery_ExecutesWithCorrectQuery()
        {
            SeedQuery("simple", "source | take 10",
                       endpoint: "https://prod.diagnostics.monitoring.core.windows.net/",
                       ns: "TestNs", evt: "Log");

            _executor.WithResult(MockQueryExecutor.CreateSimpleResult(
                new[] { "Name" }, new[] { "string" }, new[] { new object[] { "test" } }));

            var cmd = CreateCommand();
            var exit = cmd.Execute(new SavedOptions { Action = "run", Name = "simple" });

            Assert.Equal(0, exit);
            Assert.Equal("source | take 10", _executor.LastQuery);
            Assert.Equal("https://prod.diagnostics.monitoring.core.windows.net/", _executor.LastOptions.Endpoint);
            Assert.Equal("TestNs", _executor.LastOptions.Namespace);
        }

        [Fact]
        public void Run_WithParameterSubstitution_ReplacesPlaceholders()
        {
            SeedQuery("parameterized", "source | where Level == '{{level}}' and Region == '{{region}}'",
                       endpoint: "https://prod.diagnostics.monitoring.core.windows.net/",
                       ns: "TestNs", evt: "Log");

            _executor.WithResult(MockQueryExecutor.CreateSimpleResult(
                new[] { "Count" }, new[] { "long" }, new[] { new object[] { 42L } }));

            var cmd = CreateCommand();
            var exit = cmd.Execute(new SavedOptions
            {
                Action = "run",
                Name = "parameterized",
                Parameters = new[] { "level=Error", "region=WestUS2" }
            });

            Assert.Equal(0, exit);
            Assert.Equal("source | where Level == 'Error' and Region == 'WestUS2'", _executor.LastQuery);
        }

        [Fact]
        public void Run_MissingRequiredParam_ReturnsError()
        {
            SeedQuery("needs-param", "T | where X == '{{required}}'",
                       endpoint: "https://prod.diagnostics.monitoring.core.windows.net/",
                       ns: "TestNs", evt: "Log");

            var cmd = CreateCommand();
            var exit = cmd.Execute(new SavedOptions { Action = "run", Name = "needs-param" });

            Assert.Equal(1, exit);
            Assert.Contains("Missing required parameter(s): required", _stderr.ToString());
        }

        [Fact]
        public void Run_ExtraParams_IgnoredSilently()
        {
            SeedQuery("one-param", "T | where X == '{{x}}'",
                       endpoint: "https://prod.diagnostics.monitoring.core.windows.net/",
                       ns: "TestNs", evt: "Log");

            _executor.WithResult(new QueryResult());

            var cmd = CreateCommand();
            var exit = cmd.Execute(new SavedOptions
            {
                Action = "run",
                Name = "one-param",
                Parameters = new[] { "x=hello", "extra=ignored" }
            });

            Assert.Equal(0, exit);
            Assert.Equal("T | where X == 'hello'", _executor.LastQuery);
        }

        [Fact]
        public void Run_NoEndpoint_ReturnsError()
        {
            SeedQuery("no-endpoint", "T | take 1", ns: "TestNs", evt: "Log");

            var cmd = CreateCommand();
            var exit = cmd.Execute(new SavedOptions { Action = "run", Name = "no-endpoint" });

            Assert.Equal(1, exit);
            Assert.Contains("No endpoint specified", _stderr.ToString());
        }

        [Fact]
        public void Run_NoNamespace_ReturnsError()
        {
            SeedQuery("no-ns", "T | take 1",
                       endpoint: "https://prod.diagnostics.monitoring.core.windows.net/");

            var cmd = CreateCommand();
            var exit = cmd.Execute(new SavedOptions { Action = "run", Name = "no-ns" });

            Assert.Equal(1, exit);
            Assert.Contains("No namespace specified", _stderr.ToString());
        }

        [Fact]
        public void Run_CliOverridesSavedDefaults()
        {
            SeedQuery("overridable", "T | take 1",
                       endpoint: "https://saved.diagnostics.monitoring.core.windows.net/",
                       ns: "SavedNs", evt: "SavedEvt");

            _executor.WithResult(new QueryResult());

            var cmd = CreateCommand();
            var exit = cmd.Execute(new SavedOptions
            {
                Action = "run",
                Name = "overridable",
                Endpoint = "https://cli.diagnostics.monitoring.core.windows.net/",
                Namespace = "CliNs",
                Event = "CliEvt"
            });

            Assert.Equal(0, exit);
            Assert.Equal("https://cli.diagnostics.monitoring.core.windows.net/", _executor.LastOptions.Endpoint);
            Assert.Equal("CliNs", _executor.LastOptions.Namespace);
        }

        [Fact]
        public void Run_FallsBackToGlobalConfig()
        {
            // Saved query has no endpoint/namespace; global config provides them
            var config = _configManager.Load();
            config.DefaultEndpoint = "https://global.diagnostics.monitoring.core.windows.net/";
            config.DefaultNamespace = "GlobalNs";
            config.SavedQueries["fallback"] = new SavedQuery { Query = "T | take 1" };
            _configManager.Save(config);

            _executor.WithResult(new QueryResult());

            var cmd = CreateCommand();
            var exit = cmd.Execute(new SavedOptions { Action = "run", Name = "fallback" });

            Assert.Equal(0, exit);
            Assert.Equal("https://global.diagnostics.monitoring.core.windows.net/", _executor.LastOptions.Endpoint);
            Assert.Equal("GlobalNs", _executor.LastOptions.Namespace);
        }

        [Fact]
        public void Run_NonExistent_ReturnsError()
        {
            SeedQuery("placeholder", "X");

            var cmd = CreateCommand();
            var exit = cmd.Execute(new SavedOptions { Action = "run", Name = "missing" });

            Assert.Equal(1, exit);
            Assert.Contains("No saved query named 'missing'", _stderr.ToString());
        }

        [Fact]
        public void Run_ExecutorThrows_ReturnsError()
        {
            SeedQuery("will-fail", "T | take 1",
                       endpoint: "https://prod.diagnostics.monitoring.core.windows.net/",
                       ns: "TestNs", evt: "Log");

            _executor.WithException(new QueryConnectionException("https://prod.diagnostics.monitoring.core.windows.net/", "Connection refused"));

            var cmd = CreateCommand();
            var exit = cmd.Execute(new SavedOptions { Action = "run", Name = "will-fail" });

            Assert.Equal(1, exit);
            Assert.Contains("Connection refused", _stderr.ToString());
        }

        // ============================================================
        // PARAMETER SUBSTITUTION (static method tests)
        // ============================================================

        [Fact]
        public void SubstituteParameters_SingleParam_Replaces()
        {
            var result = SavedCommand.SubstituteParameters(
                "T | where X == '{{value}}'",
                new Dictionary<string, string> { { "value", "hello" } });

            Assert.Equal("T | where X == 'hello'", result);
        }

        [Fact]
        public void SubstituteParameters_MultipleParams_ReplacesAll()
        {
            var result = SavedCommand.SubstituteParameters(
                "T | where A == '{{a}}' and B == {{b}}",
                new Dictionary<string, string> { { "a", "x" }, { "b", "42" } });

            Assert.Equal("T | where A == 'x' and B == 42", result);
        }

        [Fact]
        public void SubstituteParameters_RepeatedParam_ReplacesAll()
        {
            var result = SavedCommand.SubstituteParameters(
                "{{x}} and {{x}} and {{x}}",
                new Dictionary<string, string> { { "x", "Y" } });

            Assert.Equal("Y and Y and Y", result);
        }

        [Fact]
        public void SubstituteParameters_MissingParam_Throws()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                SavedCommand.SubstituteParameters(
                    "T | where X == '{{missing}}'",
                    new Dictionary<string, string>()));

            Assert.Contains("missing", ex.Message);
        }

        [Fact]
        public void SubstituteParameters_MissingMultipleParams_ListsAll()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                SavedCommand.SubstituteParameters(
                    "{{a}} and {{b}}",
                    new Dictionary<string, string>()));

            Assert.Contains("a", ex.Message);
            Assert.Contains("b", ex.Message);
        }

        [Fact]
        public void SubstituteParameters_NullParams_ThrowsForRequired()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                SavedCommand.SubstituteParameters(
                    "T | where X == '{{needed}}'",
                    null));

            Assert.Contains("needed", ex.Message);
        }

        [Fact]
        public void SubstituteParameters_NoPlaceholders_ReturnsUnchanged()
        {
            var template = "T | take 10";
            var result = SavedCommand.SubstituteParameters(template, null);
            Assert.Equal(template, result);
        }

        [Fact]
        public void SubstituteParameters_EmptyTemplate_ReturnsEmpty()
        {
            Assert.Equal("", SavedCommand.SubstituteParameters("", null));
        }

        [Fact]
        public void SubstituteParameters_NullTemplate_ReturnsNull()
        {
            Assert.Null(SavedCommand.SubstituteParameters(null, null));
        }

        [Fact]
        public void SubstituteParameters_SpecialCharsInValue_PreservedLiterally()
        {
            var result = SavedCommand.SubstituteParameters(
                "T | where X == '{{val}}'",
                new Dictionary<string, string> { { "val", "O'Brien \"test\" $100" } });

            Assert.Equal("T | where X == 'O'Brien \"test\" $100'", result);
        }

        // ============================================================
        // EXTRACT PARAMETER NAMES
        // ============================================================

        [Fact]
        public void ExtractParameterNames_MultipleUnique_ReturnsDeduplicated()
        {
            var names = SavedCommand.ExtractParameterNames("{{a}} {{b}} {{a}} {{c}}");
            Assert.Equal(3, names.Count);
            Assert.Contains("a", names);
            Assert.Contains("b", names);
            Assert.Contains("c", names);
        }

        [Fact]
        public void ExtractParameterNames_NoParams_ReturnsEmpty()
        {
            var names = SavedCommand.ExtractParameterNames("T | take 10");
            Assert.Empty(names);
        }

        [Fact]
        public void ExtractParameterNames_NullInput_ReturnsEmpty()
        {
            var names = SavedCommand.ExtractParameterNames(null);
            Assert.Empty(names);
        }

        // ============================================================
        // BUILT-IN QUERIES
        // ============================================================

        [Fact]
        public void BuiltInQueries_ContainsAllThree()
        {
            var builtIn = SavedCommand.GetBuiltInQueries();

            Assert.True(builtIn.ContainsKey("recent-errors"));
            Assert.True(builtIn.ContainsKey("top-messages"));
            Assert.True(builtIn.ContainsKey("sample-events"));
        }

        [Fact]
        public void BuiltInQueries_RecentErrors_HasExpectedStructure()
        {
            var q = SavedCommand.GetBuiltInQueries()["recent-errors"];

            Assert.NotEmpty(q.Query);
            Assert.Equal("Recent error-level log entries", q.Description);
        }

        [Fact]
        public void BuiltInQueries_TopMessages_HasExpectedStructure()
        {
            var q = SavedCommand.GetBuiltInQueries()["top-messages"];

            Assert.NotEmpty(q.Query);
            Assert.Equal("Top messages by frequency", q.Description);
        }

        [Fact]
        public void BuiltInQueries_SampleEvents_HasExpectedStructure()
        {
            var q = SavedCommand.GetBuiltInQueries()["sample-events"];

            Assert.NotEmpty(q.Query);
            Assert.Equal("Sample N events from the source", q.Description);
        }

        [Fact]
        public void BuiltInQueries_SeededOnFreshConfig()
        {
            // Fresh config with no saved queries — built-ins should be seeded
            var cmd = CreateCommand();
            cmd.Execute(new SavedOptions { Action = "list" });

            var config = _configManager.Load();
            Assert.True(config.SavedQueries.ContainsKey("recent-errors"));
            Assert.True(config.SavedQueries.ContainsKey("top-messages"));
            Assert.True(config.SavedQueries.ContainsKey("sample-events"));
        }

        [Fact]
        public void BuiltInQueries_NotSeededWhenQueriesExist()
        {
            // Config already has queries — built-ins should NOT be added
            SeedQuery("my-query", "T | take 1");

            var cmd = CreateCommand();
            cmd.Execute(new SavedOptions { Action = "list" });

            var config = _configManager.Load();
            Assert.False(config.SavedQueries.ContainsKey("recent-errors"));
            Assert.True(config.SavedQueries.ContainsKey("my-query"));
        }

        // ============================================================
        // EDGE CASES
        // ============================================================

        [Fact]
        public void Add_NameWithSpecialChars_Works()
        {
            SeedQuery("placeholder", "X");

            var cmd = CreateCommand();
            var exit = cmd.Execute(new SavedOptions
            {
                Action = "add",
                Name = "my-query_v2.1",
                Query = "T | take 1"
            });

            Assert.Equal(0, exit);
            var config = _configManager.Load();
            Assert.True(config.SavedQueries.ContainsKey("my-query_v2.1"));
        }

        [Fact]
        public void RoundTrip_AddShowRemove_Works()
        {
            SeedQuery("placeholder", "X");

            var cmd = CreateCommand();

            // Add
            cmd.Execute(new SavedOptions
            {
                Action = "add",
                Name = "roundtrip",
                Query = "T | where X == '{{val}}'",
                Description = "Round trip test"
            });

            // Show
            _stdout.GetStringBuilder().Clear();
            cmd.Execute(new SavedOptions { Action = "show", Name = "roundtrip" });
            Assert.Contains("Round trip test", _stdout.ToString());

            // Remove
            _stdout.GetStringBuilder().Clear();
            cmd.Execute(new SavedOptions { Action = "remove", Name = "roundtrip" });
            Assert.Contains("removed", _stdout.ToString());

            // Verify gone
            _stderr.GetStringBuilder().Clear();
            var exit = cmd.Execute(new SavedOptions { Action = "show", Name = "roundtrip" });
            Assert.Equal(1, exit);
        }

        // ============================================================
        // VALIDATOR (updated actions)
        // ============================================================

        [Theory]
        [InlineData("list")]
        [InlineData("add")]
        [InlineData("remove")]
        [InlineData("show")]
        [InlineData("run")]
        public void Validator_ValidActions_NoErrors(string action)
        {
            var opts = new SavedOptions
            {
                Action = action,
                Name = action == "list" ? null : "test",
                Query = action == "add" ? "T | take 1" : null
            };
            var errors = OptionValidator.ValidateSavedOptions(opts);

            Assert.Empty(errors);
        }

        [Fact]
        public void Validator_InvalidAction_ReturnsError()
        {
            var opts = new SavedOptions { Action = "bogus" };
            var errors = OptionValidator.ValidateSavedOptions(opts);

            Assert.Single(errors);
            Assert.Contains("Unknown saved-query action", errors[0]);
        }

        [Theory]
        [InlineData("add")]
        [InlineData("remove")]
        [InlineData("show")]
        [InlineData("run")]
        public void Validator_ActionWithoutName_ReturnsError(string action)
        {
            var opts = new SavedOptions { Action = action, Query = action == "add" ? "Q" : null };
            var errors = OptionValidator.ValidateSavedOptions(opts);

            Assert.Contains(errors, e => e.Contains("requires a query name"));
        }

        [Fact]
        public void Validator_AddWithoutQuery_ReturnsError()
        {
            var opts = new SavedOptions { Action = "add", Name = "test" };
            var errors = OptionValidator.ValidateSavedOptions(opts);

            Assert.Contains(errors, e => e.Contains("requires --query"));
        }
    }
}
