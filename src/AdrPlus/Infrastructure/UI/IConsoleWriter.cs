// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;

namespace AdrPlus.Infrastructure.UI
{
    /// <summary>
    /// The mode chosen from <c>adrplus plugins --wizard</c>'s mode-selection prompt.
    /// </summary>
    internal enum PluginsWizardMode
    {
        Back,
        Install,
        List,
        ListHost,
        Validate,
        Manage,
        Uninstall
    }

    internal enum SyncWizardMode
    {
        Back,
        Default,
        Backfill
    }

    /// <summary>
    /// Core console I/O shared by every command: generic output, confirmation, cursor
    /// control, and the file/folder browsing primitives used across wizard flows.
    /// </summary>
    internal interface IConsoleWriter
    {
        /// <summary>
        /// Flushes any buffered output directly to the console window.
        /// </summary>
        void FlushOutput();

        /// <summary>
        /// Attempts to execute the first-time installation process.
        /// </summary>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains <see langword="true"/> if the
        /// installation was executed successfully; otherwise, <see langword="false"/>.</returns>
        Task<bool> TryExecuteFistInstall(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the current position of the cursor in the console.
        /// </summary>
        /// <returns>A tuple containing the left and top positions of the cursor.</returns>
        (int left, int top) PromptCursorPosition();

        /// <summary>
        /// Moves the cursor to a new position in the console.
        /// </summary>
        /// <param name="left">The left position to move the cursor to.</param>
        /// <param name="top">The top position to move the cursor to.</param>
        void PromptMovePosition(int left, int top);

        /// <summary>
        /// Ensures that the console culture settings are properly configured based on the provided application configuration.
        /// </summary>
        /// <param name="config">The application configuration containing the culture settings to apply.</param>
        void PromptEnsureCulture(AdrPlusConfig config);

        /// <summary>
        /// Gets the current position of the cursor.
        /// </summary>
        /// <returns>A tuple containing the left and top positions of the cursor.</returns>
        (int left, int top) PromptGetCursorPosition();

        void PromptWriteWait(string message);

        /// <summary>
        /// Clears the wait message from the console at current positions and set positions of the cursor.
        /// </summary>
        /// <param name="position">
        /// A tuple containing the left and top positions where the wait message was displayed, used to clear the message and reset the cursor position.
        /// </param>
        void PromptClearWaitText((int left, int top) position);

        /// <summary>
        /// Writes an informational message to the console.
        /// </summary>
        void PromptWriteInfo(string message);

        /// <summary>
        /// Writes a summary-styled message to the console.
        /// </summary>
        void PromptWriteSummary(string message);

        /// <summary>
        /// Writes a success message to the console.
        /// </summary>
        void PromptWriteSuccess(string message);

        /// <summary>
        /// Writes an error message to the console.
        /// </summary>
        void PromptWriteError(string message);

        /// <summary>
        /// Writes help information to the console.
        /// </summary>
        void PromptWriteHelp(string helpText);

        /// <summary>
        /// Writes the specified command to the output stream.
        /// </summary>
        /// <param name="command">The command string to write.</param>
        void PromptWriteStartCommand(string command);

        /// <summary>
        /// Writes a message indicating that the specified command has finished executing.
        /// </summary>
        /// <param name="command">The command string to write.</param>
        void PromptWriteFinishedCommand(string command);

        /// <summary>
        /// Displays a welcome message including the specified application version.
        /// </summary>
        /// <param name="appVersion">The version of the application to include in the welcome message.</param>
        void PromptShowWellcome(string appVersion);

        /// <summary>
        /// Configures the prompt settings.
        /// </summary>
        /// <param name="config">The configuration settings to apply to the prompt.</param>
        void PromptConfigure(AdrPlusConfig config);

        /// <summary>
        /// Displays a banner with the specified text.
        /// </summary>
        /// <param name="bannerText">The text to display in the banner.</param>
        void PromptShowBanner(string bannerText);

        /// <summary>
        /// Warns when a plugin listed in the repo's <c>ActivePlugins</c> baseline isn't currently loaded —
        /// the one drift case that wasn't deliberately chosen (see <c>PluginActivationGate</c>). Deliberately
        /// prints nothing on the happy path (no missing plugins) to avoid repeating status that's already
        /// available in full via <c>adrplus plugins --list</c>. Called once a command's own repository/plugin
        /// state is known. Callers should invoke this right before their own result message (not immediately
        /// after resolving the repo path) — writing any earlier can land on a cursor position a wizard flow has
        /// already repositioned (e.g. via <see cref="PromptMovePosition"/> after a confirm step), making the
        /// output invisible even though it was technically written.
        /// </summary>
        /// <param name="missingPluginNames">Names listed as active but not currently loaded; empty prints nothing.</param>
        void PromptWarnMissingActivePlugins(IReadOnlyList<string> missingPluginNames);

        /// <summary>
        /// Displays <paramref name="message"/> and waits for a keypress before continuing.
        /// </summary>
        /// <param name="message">The message to display to the user.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A boolean indicating whether the user pressed abort key.</returns>
        bool PromptPressAnyKeyToContinue(string message, CancellationToken cancellationToken);

        /// <summary>
        /// Enables or disables the ability for the user to abort an operation by pressing the Escape key during prompts.
        /// </summary>
        /// <param name="enabled">Whether pressing Escape should abort the current prompt.</param>
        void PromptEnabledEscToAbort(bool enabled);

        /// <summary>
        /// Prompts the user for confirmation with a yes/no question.
        /// </summary>
        /// <param name="message">The message to display to the user.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple indicating whether the operation was aborted and the user's response.</returns>
        (bool IsAborted, bool ConfirmYes) PromptConfirm(string message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to select a date using a calendar interface.
        /// </summary>
        /// <param name="message">The message to display to the user.</param>
        /// <param name="dateref">The reference date to display initially.</param>
        /// <param name="adrPlusRepo">The repository configuration.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the selected date.</returns>
        (bool IsAborted, DateTime Content) PromptCalendar(string message, DateTime dateref, AdrPlusConfig adrPlusRepo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to select an ADR from a list of latest ADR files.
        /// </summary>
        /// <param name="files">The array of ADR files to choose from.</param>
        /// <param name="adrPlusRepoConfig">The repository configuration.</param>
        /// <param name="validselect">Validation function that checks whether a selected ADR is valid and returns a message when invalid.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the selected ADR information.</returns>
        (bool IsAborted, AdrFileNameComponents? info) PromptSelecAdrs(AdrFileNameComponents[] files, AdrPlusRepoConfig adrPlusRepoConfig, Func<AdrFileNameComponents, (bool, string?)> validselect, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to select a logical drive from available drives.
        /// </summary>
        /// <param name="message">The message to display to the user.</param>
        /// <param name="fileSystemService">The file system service to enumerate available drives.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the selected drive path.</returns>
        (bool IsAborted, string Content) PromptSelectLogicalDrive(string message, IFileSystemService fileSystemService, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to select the repository folder.
        /// </summary>
        /// <param name="message">The message to display to the user.</param>
        /// <param name="checknitCmd">Whether to check for init command requirements.</param>
        /// <param name="root">The root directory path to start browsing from.</param>
        /// <param name="fileSystemService">The file system service to use for directory operations.</param>
        /// <param name="validateJsonConfig">The service to validate JSON configuration.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the selected folder path.</returns>
        (bool IsAborted, string Content) PromptSelectFolderPath(string message, bool checknitCmd, string root, IFileSystemService fileSystemService, IValidateConfig validateJsonConfig, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to confirm whether to use an empty template and returns the result along with an abort status.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the selected boolean value.</returns>
        (bool IsAborted, bool Content) PromptEmptyTemplate(CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to choose between <c>sync</c>'s default (re-drive pending) and <c>--backfill</c>
        /// (full repository sweep) modes, or <c>Back</c> to return to the previous wizard menu
        /// (<c>adrplus sync --wizard</c>).
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple indicating whether the operation was aborted and which mode was chosen.</returns>
        (bool IsAborted, SyncWizardMode Mode) PromptSelectSyncMode(CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to choose between <c>plugins</c>' <c>list</c> (repository-scoped), <c>list</c>
        /// (host-only, <see cref="PluginsWizardMode.ListHost"/>), <c>validate</c>, and <c>manage</c>
        /// (active-plugins selection) modes, or <c>Back</c> to return to the previous wizard menu
        /// (<c>adrplus plugins --wizard</c>).
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple indicating whether the operation was aborted and which mode was chosen.</returns>
        (bool IsAborted, PluginsWizardMode Mode) PromptSelectPluginsMode(CancellationToken cancellationToken = default);

        /// <summary>
        /// Displays every loaded plugin as a read-only, navigable table, cross-referenced against a repository's
        /// <c>activeplugins</c> (<c>adrplus plugins --list --path ... --wizard</c>).
        /// </summary>
        /// <param name="rows">
        /// One row per loaded plugin, plus a synthetic row for each name listed as active but not currently
        /// loaded (status, name, version, subscribed events, allowlist status, pending-item count). Must contain
        /// at least one row — the caller is responsible for falling back to plain text when there is nothing to show.
        /// </param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns><see langword="true"/> if the user aborted (Esc) instead of dismissing the table (Enter).</returns>
        bool PromptShowPluginsListTable(IReadOnlyList<(string Status, string Name, string Version, string Events, string Allowlist, int Pending)> rows, CancellationToken cancellationToken = default);

        /// <summary>
        /// Displays every loaded plugin as a read-only, navigable table, with no repository in scope
        /// (<c>adrplus plugins --list --wizard</c>, host-only). Unlike <see cref="PromptShowPluginsListTable"/>,
        /// rows carry no Active/Inactive/Missing status and no pending-item count — there's no repository's
        /// <c>activeplugins</c> or <c>plugins-state</c> folder to cross-reference against.
        /// </summary>
        /// <param name="rows">
        /// One row per loaded plugin (name, version, subscribed events, allowlist status). Must contain at
        /// least one row — the caller is responsible for falling back to plain text when there is nothing to show.
        /// </param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns><see langword="true"/> if the user aborted (Esc) instead of dismissing the table (Enter).</returns>
        bool PromptShowPluginsHostListTable(IReadOnlyList<(string Name, string Version, string Events, string Allowlist)> rows, CancellationToken cancellationToken = default);

        /// <summary>
        /// Shows a <c>MultiSelect</c> over <paramref name="pluginNames"/>, pre-checked per <paramref name="currentlyActive"/>,
        /// so the user can choose the new <c>activeplugins</c> baseline (<c>adrplus plugins --wizard</c>'s manage mode).
        /// </summary>
        /// <param name="pluginNames">Every currently loaded plugin's name.</param>
        /// <param name="currentlyActive">The repo's current <c>ActivePlugins</c> set, used to pre-check matching items.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple indicating whether the operation was aborted and, if not, the selected plugin names.</returns>
        (bool IsAborted, string[] SelectedNames) PromptSelectActivePlugins(IReadOnlyList<string> pluginNames, IReadOnlySet<string> currentlyActive, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts for the path to a plugin zip file to install (<c>adrplus plugins --wizard</c>'s install
        /// mode) via an interactive file browser, restricted to <c>.zip</c> files.
        /// </summary>
        /// <param name="root">The drive or folder the browser starts from, same as <see cref="PromptSelectFolderPath"/>.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple indicating whether the operation was aborted and the selected zip file path.</returns>
        (bool IsAborted, string ZipPath) PromptInputPluginZipPath(string root, CancellationToken cancellationToken = default);

        /// <summary>
        /// Shows a <c>MultiSelect</c> over every folder name currently under <c>./plugins/</c> so the user can
        /// choose one or more to uninstall in the same run (<c>adrplus plugins --wizard</c>'s uninstall mode) —
        /// each selected name is then uninstalled one at a time.
        /// </summary>
        /// <param name="installedNames">Every plugin folder name currently under <c>./plugins/</c>.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple indicating whether the operation was aborted and the selected folder names.</returns>
        (bool IsAborted, string[] SelectedNames) PromptSelectPluginsToUninstall(IReadOnlyList<string> installedNames, CancellationToken cancellationToken = default);

        /// <summary>
        /// Displays every loaded and rejected plugin candidate as a read-only, navigable table
        /// (<c>adrplus plugins --validate --wizard</c>).
        /// </summary>
        /// <param name="rows">
        /// One row per plugin candidate — <c>Status</c> is <c>"VALID"</c> or <c>"REJECTED"</c>, <c>NameOrFolder</c>
        /// is the plugin name (valid) or subfolder path (rejected), and <c>Detail</c> is its version (valid) or
        /// <c>"{Reason}: {Message}"</c> (rejected). Must contain at least one row — the caller is responsible for
        /// falling back to plain text when there is nothing to show.
        /// </param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns><see langword="true"/> if the user aborted (Esc) instead of dismissing the table (Enter).</returns>
        bool PromptShowPluginsValidateTable(IReadOnlyList<(string Status, string NameOrFolder, string Detail)> rows, CancellationToken cancellationToken = default);
    }
}
