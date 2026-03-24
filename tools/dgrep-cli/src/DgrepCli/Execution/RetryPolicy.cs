using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DgrepCli.Execution
{
    /// <summary>
    /// Configurable retry policy with exponential backoff for transient failures.
    /// DGrep rate limit: 5 concurrent requests per user.
    /// </summary>
    public class RetryPolicy
    {
        /// <summary>Maximum number of retry attempts (not counting the initial attempt).</summary>
        public int MaxRetries { get; }

        /// <summary>Base delay between retries. Actual delay = BaseDelay * 2^(attempt-1) + jitter.</summary>
        public TimeSpan BaseDelay { get; }

        /// <summary>Maximum delay cap for any single retry wait.</summary>
        public TimeSpan MaxDelay { get; }

        /// <summary>
        /// Creates a retry policy.
        /// </summary>
        /// <param name="maxRetries">Max retry attempts (default 3).</param>
        /// <param name="baseDelay">Base delay between retries (default 1s).</param>
        /// <param name="maxDelay">Maximum delay cap (default 30s).</param>
        public RetryPolicy(int maxRetries = 3, TimeSpan? baseDelay = null, TimeSpan? maxDelay = null)
        {
            if (maxRetries < 0)
                throw new ArgumentOutOfRangeException(nameof(maxRetries), "Max retries must be >= 0.");

            MaxRetries = maxRetries;
            BaseDelay = baseDelay ?? TimeSpan.FromSeconds(1);
            MaxDelay = maxDelay ?? TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Default policy: 3 retries, 1s base delay, 30s max delay.
        /// </summary>
        public static RetryPolicy Default => new RetryPolicy();

        /// <summary>
        /// No-retry policy for operations that should fail immediately.
        /// </summary>
        public static RetryPolicy None => new RetryPolicy(maxRetries: 0);

        /// <summary>
        /// Determines whether an exception is transient and should be retried.
        /// </summary>
        public static bool IsTransient(Exception ex)
        {
            if (ex == null) return false;

            // Rate limit (429)
            if (ex is QueryRateLimitException) return true;

            // Connection failures (network timeout, DNS, connection refused)
            if (ex is QueryConnectionException) return true;

            // Timeout
            if (ex is QueryTimeoutException) return true;

            // Check inner exceptions for HTTP status codes
            if (IsTransientHttpException(ex)) return true;
            if (ex.InnerException != null && IsTransientHttpException(ex.InnerException)) return true;

            // WebException with transient status
            if (ex is WebException webEx)
            {
                return webEx.Status == WebExceptionStatus.Timeout
                    || webEx.Status == WebExceptionStatus.ConnectFailure
                    || webEx.Status == WebExceptionStatus.NameResolutionFailure;
            }

            return false;
        }

        /// <summary>
        /// Calculates the delay for a given retry attempt (1-based) with jitter.
        /// </summary>
        public TimeSpan GetDelay(int attempt)
        {
            if (attempt <= 0) return TimeSpan.Zero;

            // Exponential backoff: BaseDelay * 2^(attempt-1)
            var exponent = Math.Min(attempt - 1, 10); // Cap to avoid overflow
            var delayMs = BaseDelay.TotalMilliseconds * Math.Pow(2, exponent);

            // Add jitter: ±25% randomization to avoid thundering herd
            var jitterFactor = 0.75 + (JitterRandom.NextDouble() * 0.5); // 0.75 to 1.25
            delayMs *= jitterFactor;

            // Cap at MaxDelay
            delayMs = Math.Min(delayMs, MaxDelay.TotalMilliseconds);

            return TimeSpan.FromMilliseconds(delayMs);
        }

        /// <summary>
        /// Executes an async operation with retry logic.
        /// </summary>
        public async Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct,
            Action<Exception, int, TimeSpan> onRetry = null)
        {
            Exception lastException = null;

            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    return await operation(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw; // Don't retry user cancellation
                }
                catch (Exception ex) when (attempt < MaxRetries && IsTransient(ex))
                {
                    lastException = ex;
                    var delay = GetDelay(attempt + 1);
                    onRetry?.Invoke(ex, attempt + 1, delay);
                    await Task.Delay(delay, ct).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    throw; // Non-transient: fail immediately
                }
            }

            // Should not reach here, but safety net
            throw lastException ?? new InvalidOperationException("Retry loop exited unexpectedly.");
        }

        private static bool IsTransientHttpException(Exception ex)
        {
            var message = ex.Message ?? string.Empty;
            // Match explicit "HTTP NNN" prefix (e.g. "HTTP 429 Too Many Requests")
            // or status-line style "NNN Service..." at the start of the message.
            // Avoids false positives from line numbers or timestamps containing these digits.
            return message.Contains("HTTP 429")
                || message.Contains("HTTP 503")
                || message.Contains("HTTP 502")
                || message.Contains("HTTP 504")
                || message.StartsWith("429 ")
                || message.StartsWith("503 ")
                || message.StartsWith("502 ")
                || message.StartsWith("504 ");
        }

        // Thread-safe random for jitter
        [ThreadStatic]
        private static Random _jitterRandom;

        private static Random JitterRandom
        {
            get
            {
                if (_jitterRandom == null)
                    _jitterRandom = new Random(Guid.NewGuid().GetHashCode());
                return _jitterRandom;
            }
        }
    }
}
