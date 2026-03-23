using System;
using System.Linq;
using DgrepCli.Commands;

namespace DgrepCli.Config
{
    /// <summary>
    /// Executes config set/get/list commands using the ConfigManager.
    /// </summary>
    public class ConfigCommand
    {
        private readonly ConfigManager _manager;

        public ConfigCommand(ConfigManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        /// <summary>
        /// Runs the config subcommand. Returns exit code (0 = success, 1 = error).
        /// </summary>
        public int Execute(ConfigOptions opts)
        {
            switch (opts.Action.ToLowerInvariant())
            {
                case "get": return ExecuteGet(opts.Key);
                case "set": return ExecuteSet(opts.Key, opts.Value);
                case "list": return ExecuteList();
                default:
                    Console.Error.WriteLine($"Unknown config action '{opts.Action}'.");
                    return 1;
            }
        }

        private int ExecuteGet(string key)
        {
            try
            {
                var value = _manager.GetValue(key);
                if (value == null)
                {
                    Console.Error.WriteLine($"Config key '{key}' is not set.");
                    return 1;
                }
                Console.WriteLine(value);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error reading config: {ex.Message}");
                return 1;
            }
        }

        private int ExecuteSet(string key, string value)
        {
            try
            {
                _manager.SetValue(key, value);
                Console.WriteLine($"Set '{key}' = '{value}'");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error setting config: {ex.Message}");
                return 1;
            }
        }

        private int ExecuteList()
        {
            try
            {
                var values = _manager.ListAll();
                if (!values.Any())
                {
                    Console.WriteLine("No configuration values set.");
                    Console.WriteLine($"Config file: {_manager.ConfigPath}");
                    return 0;
                }

                var maxKeyLen = values.Keys.Max(k => k.Length);
                foreach (var kvp in values.OrderBy(k => k.Key))
                {
                    Console.WriteLine($"  {kvp.Key.PadRight(maxKeyLen)}  {kvp.Value}");
                }
                Console.WriteLine();
                Console.WriteLine($"Config file: {_manager.ConfigPath}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error listing config: {ex.Message}");
                return 1;
            }
        }
    }
}
