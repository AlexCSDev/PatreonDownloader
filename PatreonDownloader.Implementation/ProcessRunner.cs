using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using PatreonDownloader.Implementation.Interfaces;

namespace PatreonDownloader.Implementation
{
    internal sealed class ProcessRunner : IProcessRunner
    {
        public async Task<ProcessRunResult> RunAsync(string fileName, string arguments)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using Process process = new Process { StartInfo = startInfo };
            StringBuilder stdout = new StringBuilder();
            StringBuilder stderr = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    stdout.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    stderr.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();

            return new ProcessRunResult(process.ExitCode, stdout.ToString(), stderr.ToString());
        }
    }
}
