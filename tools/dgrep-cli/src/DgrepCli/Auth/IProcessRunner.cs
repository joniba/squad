using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DgrepCli.Auth
{
    /// <summary>
    /// Result from running an external process.
    /// </summary>
    public class ProcessResult
    {
        public int ExitCode { get; set; }
        public string StdOut { get; set; }
        public string StdErr { get; set; }
    }

    /// <summary>
    /// Abstraction for running external processes, enabling testability.
    /// </summary>
    public interface IProcessRunner
    {
        Task<ProcessResult> RunAsync(string fileName, string arguments, CancellationToken ct);
    }

    /// <summary>
    /// Default implementation that runs real processes.
    /// </summary>
    public class DefaultProcessRunner : IProcessRunner
    {
        public async Task<ProcessResult> RunAsync(string fileName, string arguments, CancellationToken ct)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = psi })
            {
                try
                {
                    process.Start();
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    return new ProcessResult
                    {
                        ExitCode = -1,
                        StdOut = "",
                        StdErr = $"'{fileName}' is not recognized as an internal or external command: {ex.Message}"
                    };
                }

                var stdout = await process.StandardOutput.ReadToEndAsync();
                var stderr = await process.StandardError.ReadToEndAsync();

                while (!process.HasExited)
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.Delay(100, ct);
                }

                return new ProcessResult
                {
                    ExitCode = process.ExitCode,
                    StdOut = stdout,
                    StdErr = stderr
                };
            }
        }
    }
}
