namespace DgrepCli.Execution
{
    /// <summary>
    /// Standardized exit codes for the DGrep CLI.
    /// </summary>
    public static class DgrepExitCodes
    {
        /// <summary>Query completed successfully.</summary>
        public const int Success = 0;

        /// <summary>User error: bad arguments, invalid query syntax, missing config.</summary>
        public const int UserError = 1;

        /// <summary>Transient failure: network timeout, 503, rate limit (after retries exhausted).</summary>
        public const int TransientFailure = 2;

        /// <summary>Authentication failure: expired cert, cancelled auth dialog, no credentials.</summary>
        public const int AuthFailure = 3;
    }
}
