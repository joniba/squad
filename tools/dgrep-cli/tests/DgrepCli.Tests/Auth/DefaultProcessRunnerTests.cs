using System;
using System.Threading;
using System.Threading.Tasks;
using DgrepCli.Auth;
using Xunit;

namespace DgrepCli.Tests.Auth
{
    public class DefaultProcessRunnerTests
    {
        [Fact]
        public void ResolveFileName_WithExtension_ReturnsUnchanged()
        {
            var result = DefaultProcessRunner.ResolveFileName("notepad.exe");
            Assert.Equal("notepad.exe", result);
        }

        [Fact]
        public void ResolveFileName_WithDotInPath_ReturnsUnchanged()
        {
            var result = DefaultProcessRunner.ResolveFileName("my.tool.exe");
            Assert.Equal("my.tool.exe", result);
        }

        [Fact]
        public void ResolveFileName_OnWindows_ResolvesAzToFullPath()
        {
            // az.cmd is on PATH when Azure CLI is installed (required for this POC)
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                return; // Skip on non-Windows

            var result = DefaultProcessRunner.ResolveFileName("az");
            Assert.EndsWith("az.cmd", result);
            Assert.True(System.IO.File.Exists(result), $"Resolved path does not exist: {result}");
        }

        [Fact]
        public void ResolveFileName_UnknownCommand_ReturnsFallback()
        {
            // A command that definitely doesn't exist should return the original name
            var result = DefaultProcessRunner.ResolveFileName("zzz_nonexistent_tool_zzz");
            Assert.Equal("zzz_nonexistent_tool_zzz", result);
        }

        [Fact]
        public async Task RunAsync_OnWindows_CanInvokeAz()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                return; // Skip on non-Windows

            var runner = new DefaultProcessRunner();
            // Invoke 'az --version' — confirms .cmd resolution finds the process.
            // We only verify the process was found (not exit -1 "not recognized").
            // az itself may fail for env reasons (e.g. Python path) — that's OK.
            var result = await runner.RunAsync("az", "--version", CancellationToken.None);

            Assert.True(result.ExitCode != -1,
                $"az was not found (exit -1): {result.StdErr}");
            Assert.DoesNotContain("not recognized", result.StdErr);
        }
    }
}
