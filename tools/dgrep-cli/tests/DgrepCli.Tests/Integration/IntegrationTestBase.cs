using System;
using System.Collections.Generic;
using DgrepCli.Execution;

namespace DgrepCli.Tests.Integration
{
    /// <summary>
    /// Base class for DGrep integration tests that run against live Geneva infrastructure.
    ///
    /// These tests are SKIPPED by default. To enable:
    ///   1. Set env var DGREP_INTEGRATION_ENABLED=true
    ///   2. Ensure corpnet/VPN connectivity
    ///   3. Ensure Geneva access for the test namespace
    ///
    /// Run with: dotnet test --filter Category=Integration
    /// </summary>
    public abstract class IntegrationTestBase
    {
        // ── Skip Logic ───────────────────────────────────────────────

        /// <summary>
        /// Returns a skip reason string if integration tests should be skipped,
        /// or null if they should run. Used by xUnit [Fact(Skip = ...)] pattern.
        /// </summary>
        protected static string SkipReason
        {
            get
            {
                var enabled = Environment.GetEnvironmentVariable("DGREP_INTEGRATION_ENABLED");
                if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
                {
                    return "Integration tests disabled. Set DGREP_INTEGRATION_ENABLED=true to run.";
                }
                return null; // null = do not skip
            }
        }

        /// <summary>
        /// Returns true if integration tests are enabled via environment variable.
        /// </summary>
        protected static bool IsEnabled =>
            string.Equals(
                Environment.GetEnvironmentVariable("DGREP_INTEGRATION_ENABLED"),
                "true",
                StringComparison.OrdinalIgnoreCase);

        // ── Test Endpoint Defaults ───────────────────────────────────

        /// <summary>Diagnostics PROD MDS endpoint.</summary>
        protected static string DefaultEndpoint =>
            Environment.GetEnvironmentVariable("DGREP_TEST_ENDPOINT")
            ?? "https://production.diagnostics.monitoring.core.windows.net/";

        /// <summary>Test namespace — Augusta production East US 2.</summary>
        protected static string DefaultNamespace =>
            Environment.GetEnvironmentVariable("DGREP_TEST_NAMESPACE")
            ?? "AugustaPrdEus2";

        /// <summary>Test event name.</summary>
        protected static string DefaultEvent =>
            Environment.GetEnvironmentVariable("DGREP_TEST_EVENT")
            ?? "Log";

        /// <summary>Optional certificate path for cert-based auth.</summary>
        protected static string CertificatePath =>
            Environment.GetEnvironmentVariable("DGREP_TEST_CERT_PATH");

        /// <summary>Optional certificate password.</summary>
        protected static string CertificatePassword =>
            Environment.GetEnvironmentVariable("DGREP_TEST_CERT_PASSWORD");

        // ── Helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Creates a QueryOptions configured for the default test endpoint.
        /// Note: QueryOptions currently has Kusto-centric properties (Cluster, Database).
        /// Once the correction plan (Phase A) renames these to MdsEndpoint/Namespace/Event,
        /// update this helper accordingly.
        /// </summary>
        protected static QueryOptions CreateDefaultOptions()
        {
            return new QueryOptions
            {
                // Cluster maps to MDS endpoint until QueryOptions is refactored
                Cluster = DefaultEndpoint,
                // Database maps to namespace until QueryOptions is refactored
                Database = DefaultNamespace,
                MaxRows = 100, // Keep small for integration tests
                Timeout = TimeSpan.FromMinutes(2)
            };
        }

        /// <summary>
        /// Returns a simple DGrep-compatible KQL query that returns a small number of rows.
        /// DGrep supports a KQL subset — no ago(), let, percentile(), etc.
        /// </summary>
        protected static string SimpleQuery => "* | take 10";

        /// <summary>
        /// Returns a query that should return zero results (filter on impossible value).
        /// </summary>
        protected static string EmptyResultQuery =>
            "* | where TIMESTAMP < datetime(2000-01-01) | take 1";

        /// <summary>
        /// Creates a result with test column definitions for formatter validation.
        /// Used when a real query result is available to pipe through formatters.
        /// </summary>
        protected static QueryResult CreateMinimalResult()
        {
            return MockQueryExecutor.CreateSimpleResult(
                new[] { "TIMESTAMP", "Level", "Message" },
                new[] { "datetime", "int", "string" },
                new[]
                {
                    new object[] { "2026-07-22T12:00:00Z", 2, "Test log entry" },
                    new object[] { "2026-07-22T12:01:00Z", 3, "Another entry" }
                });
        }
    }
}
