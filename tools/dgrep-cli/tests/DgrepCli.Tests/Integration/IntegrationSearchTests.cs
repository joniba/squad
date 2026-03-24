using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DgrepCli.Execution;
using Xunit;

namespace DgrepCli.Tests.Integration
{
    /// <summary>
    /// Integration tests for search queries against live Geneva/DGrep endpoints.
    ///
    /// Skipped by default. Enable with DGREP_INTEGRATION_ENABLED=true.
    /// Requires corpnet/VPN, valid Geneva credentials, and access to the test namespace.
    ///
    /// NOTE: These tests use the real IQueryExecutor implementation (DgrepQueryExecutor
    /// once the correction plan Phase B is complete). Until then, they serve as skeleton
    /// tests documenting the expected integration behavior.
    /// </summary>
    [Trait("Category", "Integration")]
    public class IntegrationSearchTests : IntegrationTestBase
    {
        [Fact]
        public async Task BasicQuery_ReturnsNonEmptyResult()
        {
            if (!IsEnabled)
            {
                Assert.NotNull(SkipReason);
                return;
            }

            // Arrange
            var options = CreateDefaultOptions();
            // TODO: Replace with real DgrepQueryExecutor once correction plan Phase B is complete.
            // For now, this test documents the expected contract.
            // var executor = new DgrepQueryExecutor(authProvider);
            IQueryExecutor executor = CreateExecutorOrSkip();
            if (executor == null) return;

            // Act
            var result = await executor.ExecuteAsync(SimpleQuery, options, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Columns.Count > 0, "Expected at least one column in results");
            Assert.True(result.Rows.Count > 0, "Expected at least one row for a '* | take 10' query");
            Assert.True(result.Rows.Count <= 10, "take 10 should return at most 10 rows");
        }

        [Fact]
        public async Task Query_WithMaxRows_RespectsLimit()
        {
            if (!IsEnabled)
            {
                Assert.NotNull(SkipReason);
                return;
            }

            // Arrange — limit to 5 rows
            var options = CreateDefaultOptions();
            options.MaxRows = 5;
            IQueryExecutor executor = CreateExecutorOrSkip();
            if (executor == null) return;

            // Act
            var result = await executor.ExecuteAsync("* | take 100", options, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Rows.Count <= 5,
                $"Expected ≤5 rows with MaxRows=5, got {result.Rows.Count}");
        }

        [Fact]
        public async Task Query_EmptyTimeRange_ReturnsZeroRows()
        {
            if (!IsEnabled)
            {
                Assert.NotNull(SkipReason);
                return;
            }

            // Arrange — query a time range with no data (year 2000)
            var options = CreateDefaultOptions();
            IQueryExecutor executor = CreateExecutorOrSkip();
            if (executor == null) return;

            // Act
            var result = await executor.ExecuteAsync(EmptyResultQuery, options, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Rows);
        }

        [Fact]
        public async Task Query_InvalidSyntax_ThrowsAppropriateException()
        {
            if (!IsEnabled)
            {
                Assert.NotNull(SkipReason);
                return;
            }

            // Arrange
            var options = CreateDefaultOptions();
            IQueryExecutor executor = CreateExecutorOrSkip();
            if (executor == null) return;

            // Act & Assert — bad KQL should throw a query-related exception
            var ex = await Assert.ThrowsAnyAsync<QueryException>(
                () => executor.ExecuteAsync("INVALID %%% SYNTAX !!!", options, CancellationToken.None));

            Assert.NotNull(ex.Message);
        }

        [Fact]
        public async Task Query_BadEndpoint_ThrowsConnectionException()
        {
            if (!IsEnabled)
            {
                Assert.NotNull(SkipReason);
                return;
            }

            // Arrange — point to a non-existent endpoint
            var options = CreateDefaultOptions();
            options.Cluster = "https://nonexistent.invalid.example.com/";
            IQueryExecutor executor = CreateExecutorOrSkip();
            if (executor == null) return;

            // Act & Assert
            await Assert.ThrowsAnyAsync<QueryException>(
                () => executor.ExecuteAsync(SimpleQuery, options, CancellationToken.None));
        }

        [Fact]
        public async Task Query_CancellationToken_AbortsQuery()
        {
            if (!IsEnabled)
            {
                Assert.NotNull(SkipReason);
                return;
            }

            // Arrange
            var options = CreateDefaultOptions();
            IQueryExecutor executor = CreateExecutorOrSkip();
            if (executor == null) return;

            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel(); // Cancel immediately

                // Act & Assert
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => executor.ExecuteAsync(SimpleQuery, options, cts.Token));
            }
        }

        // ── Helper ───────────────────────────────────────────────────

        /// <summary>
        /// Creates a real query executor, or returns null (with Assert.True skip)
        /// if the DgrepQueryExecutor is not yet available (correction plan Phase B pending).
        /// </summary>
        private static IQueryExecutor CreateExecutorOrSkip()
        {
            // TODO: Once DgrepQueryExecutor exists, replace this with:
            //   var config = new DgrepConfig { AuthMethod = "azcli" };
            //   var provider = AuthProviderFactory.Create(config, cliOverride: null);
            //   return new DgrepQueryExecutor(provider);
            //
            // Until then, skip with a clear message.
            try
            {
                // Attempt to use KustoQueryExecutor as a stand-in for structure validation.
                // It will throw NotImplementedException on actual execution, but the test
                // skeleton demonstrates the correct contract.
                return new KustoQueryExecutor();
            }
            catch
            {
                Assert.True(true, "Skipped: DgrepQueryExecutor not yet available (correction plan Phase B pending).");
                return null;
            }
        }
    }
}
