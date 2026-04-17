// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Infrastructure.Process;

/// <summary>
/// Service for executing processes in a platform-appropriate way.
/// Abstracts process execution to allow for testing without actually launching processes.
/// </summary>
internal interface IProcessService
{
    /// <summary>
    /// Opens a file using the platform-appropriate shell command.
    /// On Windows uses <c>cmd.exe /c</c>; on Linux/macOS uses <c>sh -c</c>;
    /// on other platforms falls back to shell execute.
    /// </summary>
    /// <param name="filepath">The full path to the file to open.</param>
    /// <param name="command">The shell command string used to open the file.</param>
    /// <returns>An empty string on success; otherwise the stderr output or the exception message.</returns>
    string OpenFile(string filepath, string command);
}
