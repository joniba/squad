using System;
using System.Linq;
using System.Reflection;
using CommandLine;
using CommandLine.Text;
using DgrepCli.Auth;
using DgrepCli.Commands;
using DgrepCli.Config;
using DgrepCli.Execution;

namespace DgrepCli
{
    class Program
    {
        static int Main(string[] args)
        {
            // Handle --version at the top level
            if (args.Length == 1 && args[0] == "--version")
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                Console.WriteLine($"dgrep {version.Major}.{version.Minor}.{version.Build}");
                return 0;
            }

            var parser = new Parser(settings =>
            {
                settings.HelpWriter = null; // We handle help output ourselves
                settings.CaseInsensitiveEnumValues = true;
                settings.AutoVersion = false;
            });

            var result = parser.ParseArguments<SearchOptions, TailOptions, ConfigOptions, SavedOptions, QueryVerbOptions, AuthVerbOptions>(args);

            return result.MapResult(
                (SearchOptions opts) => RunSearch(opts),
                (TailOptions opts) => RunTail(opts),
                (ConfigOptions opts) => RunConfig(opts),
                (SavedOptions opts) => RunSaved(opts),
                (QueryVerbOptions opts) => RunQuery(opts),
                (AuthVerbOptions opts) => RunAuth(opts),
                errs => HandleParseErrors(result, errs)
            );
        }

        static int RunSearch(SearchOptions opts)
        {
            var errors = OptionValidator.ValidateSearchOptions(opts);
            if (errors.Any())
                return PrintValidationErrors(errors);

            // Phase 2: Wire up actual DGrep SDK query execution
            Console.WriteLine("Search command parsed successfully.");
            Console.WriteLine($"  Endpoint:   {opts.Endpoint}");
            Console.WriteLine($"  Namespace:  {opts.Namespace}");
            Console.WriteLine($"  Event:      {opts.Event}");
            Console.WriteLine($"  From:       {opts.From}");
            Console.WriteLine($"  To:         {opts.To}");
            Console.WriteLine($"  Query:      {opts.Query}");
            Console.WriteLine($"  Query Type: {opts.QueryType}");
            Console.WriteLine($"  Max Rows:   {opts.MaxRows}");
            Console.WriteLine($"  Output:     {opts.Output}");
            if (!string.IsNullOrEmpty(opts.Version))
                Console.WriteLine($"  Version:    {opts.Version}");
            if (opts.Identity != null && opts.Identity.Any())
                Console.WriteLine($"  Identity:   {string.Join(", ", opts.Identity)}");
            if (!string.IsNullOrEmpty(opts.CertPath))
                Console.WriteLine($"  Cert:       {opts.CertPath}");

            Console.Error.WriteLine("\nNote: Query execution not yet implemented (Phase 2).");
            return 0;
        }

        static int RunTail(TailOptions opts)
        {
            var errors = OptionValidator.ValidateTailOptions(opts);
            if (errors.Any())
                return PrintValidationErrors(errors);

            Console.WriteLine("Tail command parsed successfully.");
            Console.WriteLine($"  Endpoint:   {opts.Endpoint}");
            Console.WriteLine($"  Namespace:  {opts.Namespace}");
            Console.WriteLine($"  Event:      {opts.Event}");
            Console.WriteLine($"  From:       {opts.From}");
            Console.WriteLine($"  To:         {opts.To}");
            Console.WriteLine($"  Query:      {opts.Query}");
            Console.WriteLine($"  Interval:   {opts.Interval}s");
            Console.WriteLine($"  Output:     {opts.Output}");

            Console.Error.WriteLine("\nNote: Streaming not yet implemented (Phase 2).");
            return 0;
        }

        static int RunConfig(ConfigOptions opts)
        {
            var errors = OptionValidator.ValidateConfigOptions(opts);
            if (errors.Any())
                return PrintValidationErrors(errors);

            var manager = new ConfigManager();
            var command = new ConfigCommand(manager);
            return command.Execute(opts);
        }

        static int RunSaved(SavedOptions opts)
        {
            var errors = OptionValidator.ValidateSavedOptions(opts);
            if (errors.Any())
                return PrintValidationErrors(errors);

            Console.WriteLine($"Saved query '{opts.Action}' parsed successfully.");
            if (!string.IsNullOrEmpty(opts.Name))
                Console.WriteLine($"  Name: {opts.Name}");

            Console.Error.WriteLine("\nNote: Saved query persistence not yet implemented (Phase 1.7).");
            return 0;
        }

        static int RunQuery(QueryVerbOptions opts)
        {
            var configManager = new ConfigManager();
            var config = configManager.Load();
            var authProvider = AuthProviderFactory.Create(config);
            var executor = new KustoQueryExecutor(authProvider);
            var command = new QueryCommand(executor, configManager);
            return command.Execute(opts);
        }

        static int RunAuth(AuthVerbOptions opts)
        {
            var configManager = new ConfigManager();
            var command = new AuthCommand(configManager);
            return command.Execute(opts);
        }

        static int PrintValidationErrors(System.Collections.Generic.List<string> errors)
        {
            Console.Error.WriteLine("Validation errors:");
            foreach (var error in errors)
                Console.Error.WriteLine($"  - {error}");
            return 1;
        }

        static int HandleParseErrors<T>(ParserResult<T> result, System.Collections.Generic.IEnumerable<Error> errs)
        {
            if (errs.Any(e => e.Tag == ErrorType.HelpRequestedError || e.Tag == ErrorType.HelpVerbRequestedError))
            {
                var helpText = HelpText.AutoBuild(result, h =>
                {
                    h.AdditionalNewLineAfterOption = false;
                    h.Heading = "dgrep — Geneva DGrep CLI";
                    h.Copyright = "";
                    h.AddPreOptionsLine("");
                    h.AddPreOptionsLine("Usage: dgrep <command> [options]");
                    h.AddPreOptionsLine("");
                    h.AddPreOptionsLine("Commands:");
                    h.AddPreOptionsLine("  search    Run a DGrep query and display results");
                    h.AddPreOptionsLine("  query     Execute a KQL query against a Kusto cluster");
                    h.AddPreOptionsLine("  auth      Manage authentication (status, test)");
                    h.AddPreOptionsLine("  tail      Stream DGrep results in real-time");
                    h.AddPreOptionsLine("  config    Manage CLI configuration");
                    h.AddPreOptionsLine("  saved     Manage saved queries");
                    h.AddPreOptionsLine("");
                    h.AddPreOptionsLine("Run 'dgrep <command> --help' for command-specific options.");
                    return h;
                }, e => e);
                Console.WriteLine(helpText);
                return 0;
            }

            if (errs.Any(e => e.Tag == ErrorType.VersionRequestedError))
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                Console.WriteLine($"dgrep {version.Major}.{version.Minor}.{version.Build}");
                return 0;
            }

            if (errs.Any(e => e.Tag == ErrorType.NoVerbSelectedError || e.Tag == ErrorType.BadVerbSelectedError))
            {
                var badVerb = errs.OfType<BadVerbSelectedError>().FirstOrDefault();
                if (badVerb != null)
                    Console.Error.WriteLine($"Unknown command '{badVerb.Token}'. Run 'dgrep --help' for available commands.");
                else
                    Console.Error.WriteLine("No command specified. Run 'dgrep --help' for available commands.");
                return 1;
            }

            // Generic parse errors
            var helpOnError = HelpText.AutoBuild(result, h =>
            {
                h.AdditionalNewLineAfterOption = false;
                h.Heading = "dgrep — Geneva DGrep CLI";
                h.Copyright = "";
                return h;
            }, e => e);
            Console.Error.Write(helpOnError);
            return 1;
        }
    }
}
