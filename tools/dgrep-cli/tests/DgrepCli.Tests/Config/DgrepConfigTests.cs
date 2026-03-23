using System.Collections.Generic;
using Newtonsoft.Json;
using DgrepCli.Config;
using Xunit;

namespace DgrepCli.Tests.Config
{
    public class DgrepConfigTests
    {
        [Fact]
        public void Serialize_EmptyConfig_ProducesValidJson()
        {
            var config = new DgrepConfig();
            var json = JsonConvert.SerializeObject(config, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            });

            Assert.Contains("savedQueries", json);
            Assert.DoesNotContain("defaultNamespace", json); // null, should be omitted
        }

        [Fact]
        public void Serialize_FullConfig_RoundTrips()
        {
            var config = new DgrepConfig
            {
                DefaultNamespace = "MyNamespace",
                DefaultCluster = "diag-prod",
                DefaultDatabase = "MyDB",
                DefaultTimeRange = "24h",
                DefaultMaxRows = 1000,
                CertificatePath = @"C:\certs\my.pfx",
                OutputFormat = "json",
                DefaultEndpoint = "diag-prod",
                DefaultQueryType = "kql",
                SavedQueries = new Dictionary<string, SavedQuery>
                {
                    ["errors"] = new SavedQuery
                    {
                        Query = "where Level <= 2",
                        Endpoint = "diag-prod",
                        Namespace = "MyNamespace",
                        Event = "Trace",
                        QueryType = "kql",
                        Description = "Find error-level logs"
                    }
                }
            };

            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            var deserialized = JsonConvert.DeserializeObject<DgrepConfig>(json);

            Assert.Equal("MyNamespace", deserialized.DefaultNamespace);
            Assert.Equal("diag-prod", deserialized.DefaultCluster);
            Assert.Equal("MyDB", deserialized.DefaultDatabase);
            Assert.Equal("24h", deserialized.DefaultTimeRange);
            Assert.Equal(1000, deserialized.DefaultMaxRows);
            Assert.Equal(@"C:\certs\my.pfx", deserialized.CertificatePath);
            Assert.Equal("json", deserialized.OutputFormat);
            Assert.Equal("diag-prod", deserialized.DefaultEndpoint);
            Assert.Equal("kql", deserialized.DefaultQueryType);
            Assert.Single(deserialized.SavedQueries);
            Assert.Equal("where Level <= 2", deserialized.SavedQueries["errors"].Query);
            Assert.Equal("Find error-level logs", deserialized.SavedQueries["errors"].Description);
        }

        [Fact]
        public void Deserialize_PartialJson_LeavesFieldsNull()
        {
            var json = @"{ ""defaultNamespace"": ""NS1"" }";
            var config = JsonConvert.DeserializeObject<DgrepConfig>(json);

            Assert.Equal("NS1", config.DefaultNamespace);
            Assert.Null(config.DefaultCluster);
            Assert.Null(config.DefaultTimeRange);
            Assert.Null(config.DefaultMaxRows);
            Assert.Null(config.OutputFormat);
        }

        [Fact]
        public void Deserialize_UnknownFields_AreIgnored()
        {
            var json = @"{ ""defaultNamespace"": ""NS1"", ""futureField"": true }";
            var config = JsonConvert.DeserializeObject<DgrepConfig>(json);

            Assert.Equal("NS1", config.DefaultNamespace);
        }

        [Fact]
        public void SavedQueries_DefaultsToEmptyDictionary()
        {
            var config = new DgrepConfig();
            Assert.NotNull(config.SavedQueries);
            Assert.Empty(config.SavedQueries);
        }

        [Fact]
        public void SavedQuery_RoundTrips()
        {
            var query = new SavedQuery
            {
                Query = "where RequestDuration > 5000",
                Endpoint = "prod-east",
                Namespace = "WebApp",
                Event = "Request",
                QueryType = "kql",
                Description = "Slow requests"
            };

            var json = JsonConvert.SerializeObject(query);
            var deserialized = JsonConvert.DeserializeObject<SavedQuery>(json);

            Assert.Equal(query.Query, deserialized.Query);
            Assert.Equal(query.Endpoint, deserialized.Endpoint);
            Assert.Equal(query.Namespace, deserialized.Namespace);
            Assert.Equal(query.Event, deserialized.Event);
            Assert.Equal(query.QueryType, deserialized.QueryType);
            Assert.Equal(query.Description, deserialized.Description);
        }
    }
}
