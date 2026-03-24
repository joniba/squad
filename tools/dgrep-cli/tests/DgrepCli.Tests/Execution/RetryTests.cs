using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using DgrepCli.Execution;

namespace DgrepCli.Tests.Execution
{
    public class RetryPolicyTests
    {
        // -- Constructor --

        [Fact]
        public void Constructor_Defaults_3Retries1sBase30sMax()
        {
            var policy = new RetryPolicy();
            Assert.Equal(3, policy.MaxRetries);
            Assert.Equal(TimeSpan.FromSeconds(1), policy.BaseDelay);
            Assert.Equal(TimeSpan.FromSeconds(30), policy.MaxDelay);
        }

        [Fact]
        public void Constructor_CustomValues_Accepted()
        {
            var policy = new RetryPolicy(5, TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(1));
            Assert.Equal(5, policy.MaxRetries);
            Assert.Equal(TimeSpan.FromSeconds(2), policy.BaseDelay);
            Assert.Equal(TimeSpan.FromMinutes(1), policy.MaxDelay);
        }

        [Fact]
        public void Constructor_NegativeRetries_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RetryPolicy(-1));
        }

        [Fact]
        public void Constructor_ZeroRetries_Allowed()
        {
            var policy = new RetryPolicy(0);
            Assert.Equal(0, policy.MaxRetries);
        }

        // -- Static factories --

        [Fact]
        public void Default_Is3Retries()
        {
            Assert.Equal(3, RetryPolicy.Default.MaxRetries);
        }

        [Fact]
        public void None_IsZeroRetries()
        {
            Assert.Equal(0, RetryPolicy.None.MaxRetries);
        }

        // -- IsTransient --

        [Fact]
        public void IsTransient_RateLimitException_True()
        {
            Assert.True(RetryPolicy.IsTransient(new QueryRateLimitException()));
        }

        [Fact]
        public void IsTransient_ConnectionException_True()
        {
            Assert.True(RetryPolicy.IsTransient(new QueryConnectionException("url", "timeout")));
        }

        [Fact]
        public void IsTransient_TimeoutException_True()
        {
            Assert.True(RetryPolicy.IsTransient(new QueryTimeoutException(TimeSpan.FromSeconds(5))));
        }

        [Fact]
        public void IsTransient_AuthException_False()
        {
            Assert.False(RetryPolicy.IsTransient(new QueryAuthException("bad creds")));
        }

        [Fact]
        public void IsTransient_SyntaxException_False()
        {
            Assert.False(RetryPolicy.IsTransient(new QuerySyntaxException("q", "parse error")));
        }

        [Fact]
        public void IsTransient_NullException_False()
        {
            Assert.False(RetryPolicy.IsTransient(null));
        }

        [Fact]
        public void IsTransient_GenericException_False()
        {
            Assert.False(RetryPolicy.IsTransient(new InvalidOperationException("random")));
        }

        [Fact]
        public void IsTransient_ExceptionWith429Message_True()
        {
            Assert.True(RetryPolicy.IsTransient(new Exception("HTTP 429 Too Many Requests")));
        }

        [Fact]
        public void IsTransient_ExceptionWith503Message_True()
        {
            Assert.True(RetryPolicy.IsTransient(new Exception("HTTP 503 Service Unavailable")));
        }

        [Fact]
        public void IsTransient_InnerExceptionWith503_True()
        {
            var inner = new Exception("503 Service Unavailable");
            var outer = new QueryException("Wrapper", inner);
            Assert.True(RetryPolicy.IsTransient(outer));
        }

        [Fact]
        public void IsTransient_WebExceptionTimeout_True()
        {
            var ex = new WebException("timeout", WebExceptionStatus.Timeout);
            Assert.True(RetryPolicy.IsTransient(ex));
        }

        [Fact]
        public void IsTransient_WebExceptionConnectFailure_True()
        {
            var ex = new WebException("connect", WebExceptionStatus.ConnectFailure);
            Assert.True(RetryPolicy.IsTransient(ex));
        }

        // -- GetDelay --

        [Fact]
        public void GetDelay_Attempt0_ReturnsZero()
        {
            var policy = new RetryPolicy(3, TimeSpan.FromSeconds(1));
            Assert.Equal(TimeSpan.Zero, policy.GetDelay(0));
        }

        [Fact]
        public void GetDelay_Attempt1_ApproximatelyBaseDelay()
        {
            var policy = new RetryPolicy(3, TimeSpan.FromSeconds(1));
            var delay = policy.GetDelay(1);
            // With jitter (±25%), delay should be 0.75s–1.25s
            Assert.InRange(delay.TotalMilliseconds, 750, 1250);
        }

        [Fact]
        public void GetDelay_Attempt2_ApproximatelyDoubleBase()
        {
            var policy = new RetryPolicy(3, TimeSpan.FromSeconds(1));
            var delay = policy.GetDelay(2);
            // 2^1 * 1s = 2s, with jitter: 1.5s–2.5s
            Assert.InRange(delay.TotalMilliseconds, 1500, 2500);
        }

        [Fact]
        public void GetDelay_LargeAttempt_CappedAtMaxDelay()
        {
            var policy = new RetryPolicy(10, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
            var delay = policy.GetDelay(20);
            Assert.True(delay <= TimeSpan.FromMilliseconds(5000 * 1.25)); // Max with jitter overhead
        }

        // -- ExecuteAsync --

        [Fact]
        public async Task ExecuteAsync_SuccessOnFirstTry_ReturnsResult()
        {
            var policy = new RetryPolicy();
            var result = await policy.ExecuteAsync(
                ct => Task.FromResult(42),
                CancellationToken.None);
            Assert.Equal(42, result);
        }

        [Fact]
        public async Task ExecuteAsync_TransientThenSuccess_Retries()
        {
            var policy = new RetryPolicy(3, TimeSpan.FromMilliseconds(10));
            int attempt = 0;
            int retryCallbackCount = 0;

            var result = await policy.ExecuteAsync(
                ct =>
                {
                    attempt++;
                    if (attempt <= 2)
                        throw new QueryConnectionException("url", "timeout");
                    return Task.FromResult("ok");
                },
                CancellationToken.None,
                onRetry: (ex, a, d) => retryCallbackCount++);

            Assert.Equal("ok", result);
            Assert.Equal(3, attempt); // 2 failures + 1 success
            Assert.Equal(2, retryCallbackCount);
        }

        [Fact]
        public async Task ExecuteAsync_AllRetriesFail_ThrowsLastTransient()
        {
            var policy = new RetryPolicy(2, TimeSpan.FromMilliseconds(10));
            int attempt = 0;

            await Assert.ThrowsAsync<QueryRateLimitException>(() =>
                policy.ExecuteAsync<string>(
                    ct =>
                    {
                        attempt++;
                        throw new QueryRateLimitException();
                    },
                    CancellationToken.None));

            Assert.Equal(3, attempt); // initial + 2 retries
        }

        [Fact]
        public async Task ExecuteAsync_NonTransientError_FailsImmediately()
        {
            var policy = new RetryPolicy(3, TimeSpan.FromMilliseconds(10));
            int attempt = 0;

            await Assert.ThrowsAsync<QueryAuthException>(() =>
                policy.ExecuteAsync<string>(
                    ct =>
                    {
                        attempt++;
                        throw new QueryAuthException("bad creds");
                    },
                    CancellationToken.None));

            Assert.Equal(1, attempt); // No retries for non-transient
        }

        [Fact]
        public async Task ExecuteAsync_SyntaxError_FailsImmediately()
        {
            var policy = new RetryPolicy(3, TimeSpan.FromMilliseconds(10));
            int attempt = 0;

            await Assert.ThrowsAsync<QuerySyntaxException>(() =>
                policy.ExecuteAsync<string>(
                    ct =>
                    {
                        attempt++;
                        throw new QuerySyntaxException("bad | query", "parse error");
                    },
                    CancellationToken.None));

            Assert.Equal(1, attempt);
        }

        [Fact]
        public async Task ExecuteAsync_CancellationRequested_ThrowsImmediately()
        {
            var policy = new RetryPolicy(3, TimeSpan.FromMilliseconds(10));
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();

                await Assert.ThrowsAsync<OperationCanceledException>(() =>
                    policy.ExecuteAsync(
                        ct => Task.FromResult(42),
                        cts.Token));
            }
        }

        [Fact]
        public async Task ExecuteAsync_NoRetryPolicy_FailsOnFirstTransient()
        {
            var policy = RetryPolicy.None;
            int attempt = 0;

            await Assert.ThrowsAsync<QueryConnectionException>(() =>
                policy.ExecuteAsync<string>(
                    ct =>
                    {
                        attempt++;
                        throw new QueryConnectionException("url", "refused");
                    },
                    CancellationToken.None));

            Assert.Equal(1, attempt);
        }

        [Fact]
        public async Task ExecuteAsync_OnRetryCallback_ReceivesCorrectParams()
        {
            var policy = new RetryPolicy(2, TimeSpan.FromMilliseconds(10));
            Exception capturedEx = null;
            int capturedAttempt = 0;

            try
            {
                await policy.ExecuteAsync<string>(
                    ct => throw new QueryTimeoutException(TimeSpan.FromSeconds(5)),
                    CancellationToken.None,
                    onRetry: (ex, attempt, delay) =>
                    {
                        capturedEx = ex;
                        capturedAttempt = attempt;
                    });
            }
            catch { }

            Assert.IsType<QueryTimeoutException>(capturedEx);
            Assert.Equal(2, capturedAttempt); // Last retry was attempt 2
        }
    }

    public class RetryingQueryExecutorTests
    {
        [Fact]
        public async Task ExecuteAsync_SuccessOnFirstTry_ReturnsResult()
        {
            var inner = new MockQueryExecutor();
            var expected = MockQueryExecutor.CreateSimpleResult(
                new[] { "Id" }, new[] { "int" }, new[] { new object[] { 1 } });
            inner.WithResult(expected);

            var executor = new RetryingQueryExecutor(inner, RetryPolicy.Default, TextWriter.Null);
            var options = new QueryOptions { Cluster = "test", Database = "db" };
            var result = await executor.ExecuteAsync("query", options, CancellationToken.None);

            Assert.Same(expected, result);
            Assert.Equal(1, inner.ExecutionCount);
        }

        [Fact]
        public async Task ExecuteAsync_TransientThenSuccess_RetriesAndSucceeds()
        {
            int callCount = 0;
            var expected = MockQueryExecutor.CreateSimpleResult(
                new[] { "Id" }, new[] { "int" }, new[] { new object[] { 42 } });

            // Use a custom executor that fails twice then succeeds
            var failingExecutor = new ConfigurableExecutor(ct =>
            {
                callCount++;
                if (callCount <= 2)
                    throw new QueryConnectionException("endpoint", "network timeout");
                return Task.FromResult(expected);
            });

            var policy = new RetryPolicy(3, TimeSpan.FromMilliseconds(10));
            var stderr = new StringWriter();
            var executor = new RetryingQueryExecutor(failingExecutor, policy, stderr);

            var options = new QueryOptions { Cluster = "test", Database = "db" };
            var result = await executor.ExecuteAsync("query", options, CancellationToken.None);

            Assert.Same(expected, result);
            Assert.Equal(3, callCount);
            var output = stderr.ToString();
            Assert.Contains("[Retry 1/3]", output);
            Assert.Contains("[Retry 2/3]", output);
            Assert.Contains("Cannot reach endpoint", output);
        }

        [Fact]
        public async Task ExecuteAsync_RateLimitThenSuccess_RetriesWithMessage()
        {
            int callCount = 0;
            var expected = new QueryResult();

            var failingExecutor = new ConfigurableExecutor(ct =>
            {
                callCount++;
                if (callCount <= 1)
                    throw new QueryRateLimitException();
                return Task.FromResult(expected);
            });

            var policy = new RetryPolicy(3, TimeSpan.FromMilliseconds(10));
            var stderr = new StringWriter();
            var executor = new RetryingQueryExecutor(failingExecutor, policy, stderr);

            var options = new QueryOptions { Cluster = "test", Database = "db" };
            var result = await executor.ExecuteAsync("query", options, CancellationToken.None);

            Assert.Same(expected, result);
            Assert.Equal(2, callCount);
            Assert.Contains("Rate limited", stderr.ToString());
        }

        [Fact]
        public async Task ExecuteAsync_AuthFailure_FailsImmediately()
        {
            var inner = new MockQueryExecutor();
            inner.WithException(new QueryAuthException("expired"));

            var executor = new RetryingQueryExecutor(inner, RetryPolicy.Default, TextWriter.Null);
            var options = new QueryOptions { Cluster = "test", Database = "db" };

            await Assert.ThrowsAsync<QueryAuthException>(
                () => executor.ExecuteAsync("q", options, CancellationToken.None));

            Assert.Equal(1, inner.ExecutionCount); // No retries
        }

        [Fact]
        public async Task ExecuteAsync_SyntaxError_FailsImmediately()
        {
            var inner = new MockQueryExecutor();
            inner.WithException(new QuerySyntaxException("bad | query", "parse error"));

            var executor = new RetryingQueryExecutor(inner, RetryPolicy.Default, TextWriter.Null);
            var options = new QueryOptions { Cluster = "test", Database = "db" };

            var ex = await Assert.ThrowsAsync<QuerySyntaxException>(
                () => executor.ExecuteAsync("q", options, CancellationToken.None));

            Assert.Equal(1, inner.ExecutionCount);
            Assert.Contains("bad | query", ex.Message);
        }

        [Fact]
        public async Task ExecuteAsync_AllRetriesExhausted_ThrowsLastException()
        {
            var inner = new MockQueryExecutor();
            inner.WithException(new QueryConnectionException("endpoint", "down"));

            var policy = new RetryPolicy(2, TimeSpan.FromMilliseconds(10));
            var executor = new RetryingQueryExecutor(inner, policy, TextWriter.Null);
            var options = new QueryOptions { Cluster = "test", Database = "db" };

            await Assert.ThrowsAsync<QueryConnectionException>(
                () => executor.ExecuteAsync("q", options, CancellationToken.None));

            Assert.Equal(3, inner.ExecutionCount); // initial + 2 retries
        }

        [Fact]
        public async Task ExecuteAsync_Cancellation_StopsRetries()
        {
            var inner = new MockQueryExecutor();
            inner.WithException(new QueryConnectionException("url", "timeout"));
            inner.WithDelay(TimeSpan.FromSeconds(10));

            var policy = new RetryPolicy(3, TimeSpan.FromMilliseconds(100));
            var executor = new RetryingQueryExecutor(inner, policy, TextWriter.Null);

            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();
                var options = new QueryOptions { Cluster = "test", Database = "db" };

                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => executor.ExecuteAsync("q", options, cts.Token));
            }
        }

        [Fact]
        public void Constructor_NullInner_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new RetryingQueryExecutor(null));
        }

        // Helper: configurable executor for complex test scenarios
        private class ConfigurableExecutor : IQueryExecutor
        {
            private readonly Func<CancellationToken, Task<QueryResult>> _handler;

            public ConfigurableExecutor(Func<CancellationToken, Task<QueryResult>> handler)
            {
                _handler = handler;
            }

            public Task<QueryResult> ExecuteAsync(string query, QueryOptions options, CancellationToken ct)
                => _handler(ct);
        }
    }

    public class DgrepExitCodesTests
    {
        [Fact]
        public void ExitCodes_HaveExpectedValues()
        {
            Assert.Equal(0, DgrepExitCodes.Success);
            Assert.Equal(1, DgrepExitCodes.UserError);
            Assert.Equal(2, DgrepExitCodes.TransientFailure);
            Assert.Equal(3, DgrepExitCodes.AuthFailure);
        }
    }
}
