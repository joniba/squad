using System;
using System.Diagnostics;
using System.IO;
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
        /// <summary>
        /// On Windows, batch files (.cmd, .bat) cannot be executed directly with
        /// UseShellExecute=false. This resolves e.g. 'az' → full path to 'az.cmd'
        /// by searching PATH. The full path is needed because batch files often use
        /// %~dp0 to locate sibling files, which fails without an absolute path.
        /// </summary>
        internal static string ResolveFileName(string fileName)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                return fileName;

            if (!string.IsNullOrEmpty(Path.GetExtension(fileName)))
                return fileName;

            var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
            var dirs = pathVar.Split(';');

            foreach (var ext in new[] { ".cmd", ".bat" })
            {
                foreach (var dir in dirs)
                {
                    try
                    {
                        var fullPath = Path.Combine(dir, fileName + ext);
                        if (File.Exists(fullPath))
                            return fullPath;
                    }
                    catch
                    {
                        // Invalid path entry, skip
                    }
                }
            }

            return fileName;
        }

        public async Task<ProcessResult> RunAsync(string fileName, string arguments, CancellationToken ct)
        {
            fileName = ResolveFileName(fileName);

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
