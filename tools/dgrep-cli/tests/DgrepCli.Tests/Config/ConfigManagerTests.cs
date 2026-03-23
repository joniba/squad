using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using DgrepCli.Config;
using Xunit;

namespace DgrepCli.Tests.Config
{
    public class ConfigManagerTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _configPath;
        private readonly ConfigManager _manager;

        public ConfigManagerTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "dgrep-test-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(_tempDir);
            _configPath = Path.Combine(_tempDir, "config.json");
            _manager = new ConfigManager(_configPath);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        // --- Load tests ---

        [Fact]
        public void Load_MissingFile_ReturnsDefaultConfig()
        {
            var config = _manager.Load();

            Assert.NotNull(config);
            Assert.Null(config.DefaultNamespace);
            Assert.Null(config.DefaultCluster);
            Assert.Null(config.DefaultMaxRows);
            Assert.NotNull(config.SavedQueries);
            Assert.Empty(config.SavedQueries);
        }

        [Fact]
        public void Load_EmptyFile_ReturnsDefaultConfig()
        {
            File.WriteAllText(_configPath, "");
            var config = _manager.Load();

            Assert.NotNull(config);
            Assert.Null(config.DefaultNamespace);
        }

        [Fact]
        public void Load_ValidJson_DeserializesCorrectly()
        {
            var json = @"{
                ""defaultNamespace"": ""TestNS"",
                ""defaultMaxRows"": 100,
                ""outputFormat"": ""csv""
            }";
            File.WriteAllText(_configPath, json);

            var config = _manager.Load();

            Assert.Equal("TestNS", config.DefaultNamespace);
            Assert.Equal(100, config.DefaultMaxRows);
            Assert.Equal("csv", config.OutputFormat);
        }

        [Fact]
        public void Load_InvalidJson_ThrowsInvalidOperationException()
        {
            File.WriteAllText(_configPath, "{ not valid json }}}");

            var ex = Assert.Throws<InvalidOperationException>(() => _manager.Load());
            Assert.Contains("invalid JSON", ex.Message);
        }

        // --- Save tests ---

        [Fact]
        public void Save_CreatesDirectoryAndFile()
        {
            var nestedPath = Path.Combine(_tempDir, "sub", "dir", "config.json");
            var manager = new ConfigManager(nestedPath);

            var config = new DgrepConfig { DefaultNamespace = "SavedNS" };
            manager.Save(config);

            Assert.True(File.Exists(nestedPath));
            var loaded = manager.Load();
            Assert.Equal("SavedNS", loaded.DefaultNamespace);
        }

        [Fact]
        public void Save_OverwritesExistingFile()
        {
            var config1 = new DgrepConfig { DefaultNamespace = "NS1" };
            _manager.Save(config1);

            var config2 = new DgrepConfig { DefaultNamespace = "NS2" };
            _manager.Save(config2);

            var loaded = _manager.Load();
            Assert.Equal("NS2", loaded.DefaultNamespace);
        }

        [Fact]
        public void Save_WithSavedQueries_Persists()
        {
            var config = new DgrepConfig();
            config.SavedQueries["my-query"] = new SavedQuery
            {
                Query = "where Level == 1",
                Endpoint = "diag-prod",
                Namespace = "NS",
                Event = "Evt"
            };
            _manager.Save(config);

            var loaded = _manager.Load();
            Assert.Single(loaded.SavedQueries);
            Assert.Equal("where Level == 1", loaded.SavedQueries["my-query"].Query);
        }

        // --- GetValue / SetValue tests ---

        [Fact]
        public void SetValue_ThenGetValue_RoundTrips()
        {
            _manager.SetValue("defaultNamespace", "RoundTripped");
            var value = _manager.GetValue("defaultNamespace");

            Assert.Equal("RoundTripped", value);
        }

        [Fact]
        public void SetValue_DefaultMaxRows_ParsesInt()
        {
            _manager.SetValue("defaultMaxRows", "5000");
            var value = _manager.GetValue("defaultMaxRows");
            Assert.Equal("5000", value);
        }

        [Fact]
        public void SetValue_DefaultMaxRows_InvalidInt_Throws()
        {
            Assert.Throws<ArgumentException>(() => _manager.SetValue("defaultMaxRows", "notANumber"));
        }

        [Fact]
        public void SetValue_UnknownKey_Throws()
        {
            Assert.Throws<ArgumentException>(() => _manager.SetValue("nonexistent", "val"));
        }

        [Fact]
        public void GetValue_UnsetKey_ReturnsNull()
        {
            var value = _manager.GetValue("defaultNamespace");
            Assert.Null(value);
        }

        [Fact]
        public void GetValue_EmptyKey_Throws()
        {
            Assert.Throws<ArgumentException>(() => _manager.GetValue(""));
        }

        [Fact]
        public void SetValue_MultipleKeys_PreservesAll()
        {
            _manager.SetValue("defaultNamespace", "NS");
            _manager.SetValue("outputFormat", "json");
            _manager.SetValue("defaultEndpoint", "prod");

            Assert.Equal("NS", _manager.GetValue("defaultNamespace"));
            Assert.Equal("json", _manager.GetValue("outputFormat"));
            Assert.Equal("prod", _manager.GetValue("defaultEndpoint"));
        }

        [Fact]
        public void SetValue_IsCaseInsensitiveOnKey()
        {
            _manager.SetValue("DefaultNamespace", "NS");
            Assert.Equal("NS", _manager.GetValue("defaultnamespace"));
        }

        // --- ListAll tests ---

        [Fact]
        public void ListAll_EmptyConfig_ReturnsEmptyDictionary()
        {
            var result = _manager.ListAll();
            Assert.Empty(result);
        }

        [Fact]
        public void ListAll_PopulatedConfig_ReturnsSetValues()
        {
            _manager.SetValue("defaultNamespace", "NS");
            _manager.SetValue("outputFormat", "csv");

            var result = _manager.ListAll();

            Assert.Equal("NS", result["defaultNamespace"]);
            Assert.Equal("csv", result["outputFormat"]);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void ListAll_WithSavedQueries_ShowsCount()
        {
            var config = new DgrepConfig();
            config.SavedQueries["q1"] = new SavedQuery { Query = "test" };
            config.SavedQueries["q2"] = new SavedQuery { Query = "test2" };
            _manager.Save(config);

            var result = _manager.ListAll();
            Assert.Contains("savedQueries", result.Keys);
            Assert.Contains("2 saved", result["savedQueries"]);
        }

        // --- Saved queries CRUD ---

        [Fact]
        public void SavedQueries_Add_Persists()
        {
            var config = _manager.Load();
            config.SavedQueries["errors"] = new SavedQuery
            {
                Query = "where Level <= 2",
                Endpoint = "diag-prod",
                Namespace = "MyNS",
                Event = "Trace",
                QueryType = "kql",
                Description = "Error-level logs"
            };
            _manager.Save(config);

            var loaded = _manager.Load();
            Assert.True(loaded.SavedQueries.ContainsKey("errors"));
            Assert.Equal("where Level <= 2", loaded.SavedQueries["errors"].Query);
        }

        [Fact]
        public void SavedQueries_Update_Persists()
        {
            var config = new DgrepConfig();
            config.SavedQueries["q"] = new SavedQuery { Query = "v1" };
            _manager.Save(config);

            var loaded = _manager.Load();
            loaded.SavedQueries["q"].Query = "v2";
            _manager.Save(loaded);

            var reloaded = _manager.Load();
            Assert.Equal("v2", reloaded.SavedQueries["q"].Query);
        }

        [Fact]
        public void SavedQueries_Delete_Persists()
        {
            var config = new DgrepConfig();
            config.SavedQueries["q1"] = new SavedQuery { Query = "test1" };
            config.SavedQueries["q2"] = new SavedQuery { Query = "test2" };
            _manager.Save(config);

            var loaded = _manager.Load();
            loaded.SavedQueries.Remove("q1");
            _manager.Save(loaded);

            var reloaded = _manager.Load();
            Assert.False(reloaded.SavedQueries.ContainsKey("q1"));
            Assert.True(reloaded.SavedQueries.ContainsKey("q2"));
        }

        [Fact]
        public void SavedQueries_List_ReturnsAll()
        {
            var config = new DgrepConfig();
            config.SavedQueries["a"] = new SavedQuery { Query = "q-a" };
            config.SavedQueries["b"] = new SavedQuery { Query = "q-b" };
            config.SavedQueries["c"] = new SavedQuery { Query = "q-c" };
            _manager.Save(config);

            var loaded = _manager.Load();
            Assert.Equal(3, loaded.SavedQueries.Count);
            Assert.Contains("a", (IDictionary<string, SavedQuery>)loaded.SavedQueries);
            Assert.Contains("b", (IDictionary<string, SavedQuery>)loaded.SavedQueries);
            Assert.Contains("c", (IDictionary<string, SavedQuery>)loaded.SavedQueries);
        }
    }
}
