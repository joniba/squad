using System;
using DgrepCli.Config;
using Xunit;

namespace DgrepCli.Tests.Auth
{
    /// <summary>
    /// Tests that the authMethod config key is properly handled by ConfigManager.
    /// </summary>
    public class AuthConfigTests
    {
        [Fact]
        public void GetValue_AuthMethod_ReturnsConfigured()
        {
            var config = new DgrepConfig { AuthMethod = "certificate" };
            var value = ConfigManager.GetValueFromConfig(config, "authMethod");
            Assert.Equal("certificate", value);
        }

        [Fact]
        public void GetValue_AuthMethod_ReturnsNullWhenNotSet()
        {
            var config = new DgrepConfig();
            var value = ConfigManager.GetValueFromConfig(config, "authMethod");
            Assert.Null(value);
        }

        [Theory]
        [InlineData("azcli")]
        [InlineData("certificate")]
        [InlineData("managedidentity")]
        public void SetValue_AuthMethod_ValidValues(string method)
        {
            var config = new DgrepConfig();
            ConfigManager.SetValueOnConfig(config, "authMethod", method);
            Assert.Equal(method, config.AuthMethod);
        }

        [Fact]
        public void SetValue_AuthMethod_InvalidValue_Throws()
        {
            var config = new DgrepConfig();
            var ex = Assert.Throws<ArgumentException>(
                () => ConfigManager.SetValueOnConfig(config, "authMethod", "kerberos"));
            Assert.Contains("Invalid auth method", ex.Message);
            Assert.Contains("kerberos", ex.Message);
        }

        [Fact]
        public void SetValue_AuthMethod_CaseInsensitive()
        {
            var config = new DgrepConfig();
            ConfigManager.SetValueOnConfig(config, "authMethod", "AzCli");
            Assert.Equal("azcli", config.AuthMethod);
        }

        [Fact]
        public void DgrepConfig_AuthMethod_RoundTrips()
        {
            var config = new DgrepConfig { AuthMethod = "certificate" };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(config);
            var deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<DgrepConfig>(json);
            Assert.Equal("certificate", deserialized.AuthMethod);
        }

        [Fact]
        public void DgrepConfig_AuthMethod_NullByDefault()
        {
            var config = new DgrepConfig();
            Assert.Null(config.AuthMethod);
        }
    }
}
