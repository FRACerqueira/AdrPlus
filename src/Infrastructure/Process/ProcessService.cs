// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Infrastructure.Process;

/// <summary>
/// Implementation of <see cref="IProcessService"/> for executing processes.
/// </summary>
internal sealed class ProcessService : IProcessService
{
    /// <summary>
    /// Opens a file using the platform-appropriate shell command.
    /// On Windows uses <c>cmd.exe /c</c>; on Linux/macOS uses <c>sh -c</c>;
    /// on other platforms falls back to <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute"/>.
    /// </summary>
    /// <param name="filepath">The full path to the file to open.</param>
    /// <param name="command">The shell command string used to open the file (may contain <c>{0}</c> placeholder substituted externally).</param>
    /// <returns>An empty string on success; otherwise the stderr output or the exception message.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filepath"/> or <paramref name="command"/> is <see langword="null"/>.</exception>
    public string OpenFile(string filepath, string command)
    {
        ArgumentNullException.ThrowIfNull(filepath);
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            if (OperatingSystem.IsWindows())
            {
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = $"/c \"{command}\"";
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                startInfo.FileName = "sh";
                startInfo.Arguments = $"-c \"{command}\"";
            }
            else
            {
                startInfo.FileName = filepath;
                startInfo.UseShellExecute = true;
                startInfo.RedirectStandardOutput = false;
                startInfo.RedirectStandardError = false;
            }

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null)
            {
                return Resources.AdrPlus.NewAdrErrorFailedToStartProcess;
            }

            if (startInfo.RedirectStandardError)
            {
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
                {
                    return error;
                }
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}
