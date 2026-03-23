using System;

namespace DgrepCli.Auth
{
    /// <summary>
    /// Thrown when authentication fails for any reason.
    /// </summary>
    public class AuthException : Exception
    {
        public AuthException(string message) : base(message) { }
        public AuthException(string message, Exception inner) : base(message, inner) { }
    }
}
