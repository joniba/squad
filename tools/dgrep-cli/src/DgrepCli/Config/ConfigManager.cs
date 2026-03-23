using System;
using System.IO;
using Newtonsoft.Json;

namespace DgrepCli.Config
{
    /// <summary>
    /// Reads, writes, and manages the dgrep config file at ~/.dgrep/config.json.
    /// </summary>
    public class ConfigManager
    {
        private readonly string _configPath;
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        public ConfigManager() : this(GetDefaultConfigPath()) { }

        public ConfigManager(string configPath)
        {
            _configPath = configPath ?? throw new ArgumentNullException(nameof(configPath));
        }

        public string ConfigPath => _configPath;

        /// <summary>
        /// Returns the default config file path: ~/.dgrep/config.json
        /// </summary>
        public static string GetDefaultConfigPath()
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".dgrep", "config.json");
        }

        /// <summary>
        /// Loads the config from disk. Returns a new default config if the file doesn't exist.
        /// Throws on malformed JSON.
        /// </summary>
        public DgrepConfig Load()
        {
            if (!File.Exists(_configPath))
                return new DgrepConfig();

            var json = File.ReadAllText(_configPath);
            if (string.IsNullOrWhiteSpace(json))
                return new DgrepConfig();

            try
            {
                return JsonConvert.DeserializeObject<DgrepConfig>(json, JsonSettings)
                       ?? new DgrepConfig();
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Config file '{_configPath}' contains invalid JSON: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Saves the config to disk, creating the directory if it doesn't exist.
        /// </summary>
        public void Save(DgrepConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            var dir = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonConvert.SerializeObject(config, JsonSettings);
            File.WriteAllText(_configPath, json);
        }

        /// <summary>
        /// Gets a config value by dot-separated key path.
        /// Returns null if the key doesn't exist or has no value.
        /// </summary>
        public string GetValue(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be empty.", nameof(key));

            var config = Load();
            return GetValueFromConfig(config, key);
        }

        /// <summary>
        /// Sets a config value by dot-separated key path and saves to disk.
        /// </summary>
        public void SetValue(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be empty.", nameof(key));

            var config = Load();
            SetValueOnConfig(config, key, value);
            Save(config);
        }

        internal static string GetValueFromConfig(DgrepConfig config, string key)
        {
            switch (key.ToLowerInvariant())
            {
                case "defaultnamespace": return config.DefaultNamespace;
                case "defaultcluster": return config.DefaultCluster;
                case "defaultdatabase": return config.DefaultDatabase;
                case "defaulttimerange": return config.DefaultTimeRange;
                case "defaultmaxrows": return config.DefaultMaxRows?.ToString();
                case "certificatepath": return config.CertificatePath;
                case "outputformat": return config.OutputFormat;
                case "defaultendpoint": return config.DefaultEndpoint;
                case "defaultquerytype": return config.DefaultQueryType;
                default: return null;
            }
        }

        internal static void SetValueOnConfig(DgrepConfig config, string key, string value)
        {
            switch (key.ToLowerInvariant())
            {
                case "defaultnamespace": config.DefaultNamespace = value; break;
                case "defaultcluster": config.DefaultCluster = value; break;
                case "defaultdatabase": config.DefaultDatabase = value; break;
                case "defaulttimerange": config.DefaultTimeRange = value; break;
                case "defaultmaxrows":
                    int maxRows;
                    if (!int.TryParse(value, out maxRows))
                        throw new ArgumentException($"Invalid integer value '{value}' for defaultMaxRows.");
                    config.DefaultMaxRows = maxRows;
                    break;
                case "certificatepath": config.CertificatePath = value; break;
                case "outputformat": config.OutputFormat = value; break;
                case "defaultendpoint": config.DefaultEndpoint = value; break;
                case "defaultquerytype": config.DefaultQueryType = value; break;
                default:
                    throw new ArgumentException($"Unknown config key '{key}'. Valid keys: defaultNamespace, defaultCluster, defaultDatabase, defaultTimeRange, defaultMaxRows, certificatePath, outputFormat, defaultEndpoint, defaultQueryType.");
            }
        }

        /// <summary>
        /// Returns all non-null config key/value pairs.
        /// </summary>
        public System.Collections.Generic.Dictionary<string, string> ListAll()
        {
            var config = Load();
            var result = new System.Collections.Generic.Dictionary<string, string>();

            void Add(string k, string v) { if (v != null) result[k] = v; }

            Add("defaultNamespace", config.DefaultNamespace);
            Add("defaultCluster", config.DefaultCluster);
            Add("defaultDatabase", config.DefaultDatabase);
            Add("defaultTimeRange", config.DefaultTimeRange);
            Add("defaultMaxRows", config.DefaultMaxRows?.ToString());
            Add("certificatePath", config.CertificatePath);
            Add("outputFormat", config.OutputFormat);
            Add("defaultEndpoint", config.DefaultEndpoint);
            Add("defaultQueryType", config.DefaultQueryType);

            if (config.SavedQueries != null && config.SavedQueries.Count > 0)
                Add("savedQueries", $"({config.SavedQueries.Count} saved)");

            return result;
        }
    }
}
