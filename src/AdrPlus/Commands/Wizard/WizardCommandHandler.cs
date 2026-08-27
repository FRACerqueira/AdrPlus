// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.Formatting;
using AdrPlus.Infrastructure.Logging;
using AdrPlus.Infrastructure.UI;
using AdrPlus.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;


namespace AdrPlus.Commands.Wizard
{
    /// <summary>
    /// Handles the <c>wizard</c> command to provide a full interactive wizard experience for ADR operations.
    /// Presents a hierarchical menu to configure the application, manage ADRs, or access per-command help.
    /// Persists the last-selected menu item across sessions.
    /// </summary>
    /// <param name="builtinPluginsRoot">
    /// The folder containing plugins bundled with the adrplus package itself (e.g. <c>plugins-builtin</c> next
    /// to the tool's own assembly), or empty to skip showing them. Unlike the per-repo <c>ActivePlugins</c>
    /// summary shown by the ADR lifecycle commands, this is install-level and available before any repository
    /// is chosen, so it's shown on every wizard menu screen.
    /// </param>
    internal sealed partial class WizardCommandHandler(
        CommandRouter commandRouter,
        IConfiguration configuration,
        ILogger<WizardCommandHandler> logger,
        IFileSystemService fileSystem,
        IValidateConfig validateconfig,
        IConsoleWriter prompt,
        IWizardMenuPrompts wizardMenuPrompts,
        IAdrServices adrServices,
        string builtinPluginsRoot = "") : ICommandHandler
    {
        private readonly ILogger<WizardCommandHandler> _logger = logger;
        private readonly IFileSystemService _filesystem = fileSystem;
        private readonly IConsoleWriter _prompt = prompt;
        private readonly IWizardMenuPrompts _wizardMenuPrompts = wizardMenuPrompts;
        private readonly IValidateConfig _validateconfig = validateconfig;
        private readonly IConfiguration _configuration = configuration;
        private readonly CommandRouter _commandRouter = commandRouter;
        private readonly IAdrServices _adrServices = adrServices;
        private readonly string _builtinPluginsRoot = builtinPluginsRoot;
        private readonly (CommandsAdr Command, string Alias, Type ConfigCommandHandler, string Description)[] _commandsMap = adrServices.GetCommands();

        private static readonly Arguments[] ValidCommandArgs = [Arguments.Help];
        private const string StartMenuHistoryKey = "StartMenuWizard";
        private const string ConfigMenuHistoryKey = "DefaultConfigMenu";
        private const string AdrMenuHistoryKey = "DefaultAdrMenu";
        private const string HelpMenuHistoryKey = "DefaultHelpMenu";
        private const string PluginsMenuHistoryKey = "DefaultPluginsMenu";


        /// <summary>
        /// Executes the <c>wizard</c> command asynchronously, displaying the banner, welcome message,
        /// and looping through the main menu until the user exits.
        /// </summary>
        /// <param name="args">The raw command-line tokens. Supports <c>--help</c>/<c>-h</c>.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="args"/> is <see langword="null"/>.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the user exits the wizard via ESC or cancels a prompt.</exception>
        /// <exception cref="NotImplementedException">Thrown when an unrecognized top-level menu option is selected.</exception>
        public async Task ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(args);
                var parsedArgs = _adrServices.ParseArgs(args, ValidCommandArgs,"");
                if (parsedArgs.ContainsKey(Arguments.Help) && args.Length != 0)
                {
                    _prompt.PromptWriteHelp(_adrServices.GetHelpText(
                        "wizard",
                        ValidCommandArgs,
                            ["adrplus wizard"]));
                    return;
                }
                var currentMenu = await LoadOrInitializeStartMenuAsync(cancellationToken);
                if (currentMenu.Id![0] == '4')
                {
                    currentMenu = new ItemMenuWizard();
                }
                var builtinPluginsSummary = GetBuiltinPluginsSummary();
                while (true)
                {
                    _prompt.PromptEnabledEscToAbort(false);
                    _prompt.PromptShowBanner(AppConstants.BannerText);
                    // Reconstructs PromptShowWellcome's version line without its built-in trailing blank line,
                    // so the blank line can be moved below the builtin-plugins line instead (kept together
                    // with the banner/version at the top of every menu screen).
                    _prompt.PromptWriteInfo(string.Format(null, FormatMessages.MsgWelcome, _configuration[AppConstants.CfgNameVersionApp] ?? string.Empty));
                    if (builtinPluginsSummary.Count > 0)
                    {
                        _prompt.PromptWriteInfo(string.Format(null, FormatMessages.WizardBuiltinPluginsAvailable, string.Join(", ", builtinPluginsSummary)));
                    }
                    _prompt.PromptWriteInfo("");
                    _prompt.PromptWriteStartCommand(string.Format(null, FormatMessages.MsgCommandStarted, "wizard"));

                    var isRepoConfigured = _validateconfig.HasTemplateRepoFile();

                    if (string.IsNullOrEmpty(currentMenu.Id))
                    {
                        currentMenu = await HandleMainMenuAsync(isRepoConfigured, cancellationToken);
                        if (currentMenu.Id[0] == '0')
                        {
                            return;
                        }
                        continue;
                    }
                    switch (currentMenu.Id![0])
                    {
                        case '1':
                            try
                            {
                                currentMenu = await HandleConfigurationMenuAsync(isRepoConfigured, cancellationToken);
                            }
                            catch (OperationCanceledException)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    throw;
                                }
                            }
                            catch
                            {
                                // deliberately swallowed - a sub-menu command failure returns to the wizard loop instead of exiting it
                            }
                            break;
                        case '2':
                            try
                            {
                                currentMenu = await HandleAdrMenuAsync(isRepoConfigured, cancellationToken);
                            }
                            catch (OperationCanceledException)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    throw;
                                }
                            }
                            catch
                            {
                                // deliberately swallowed - a sub-menu command failure returns to the wizard loop instead of exiting it
                            }
                            break;
                        case '3':
                            try
                            {
                                currentMenu = await HandleHelpMenuAsync(isRepoConfigured, cancellationToken);
                            }
                            catch (OperationCanceledException)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    throw;
                                }
                            }
                            catch
                            {
                                // deliberately swallowed - a sub-menu command failure returns to the wizard loop instead of exiting it
                            }
                            break;
                        case '4':
                            try
                            {
                                currentMenu = new ItemMenuWizard();
                                _prompt.PromptEnabledEscToAbort(true);
                                await _commandRouter.RouteAsync(GetCommandAlias(CommandsAdr.Explore), ["-w"], cancellationToken);
                                _prompt.PromptEnabledEscToAbort(false);
                            }
                            catch (OperationCanceledException)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    throw;
                                }
                            }
                            catch
                            {
                                // deliberately swallowed - a sub-menu command failure returns to the wizard loop instead of exiting it
                            }
                            break;
                        case '5':
                            {
                                try
                                {
                                    currentMenu = await HandleInitMigrateMenuAsync(isRepoConfigured, cancellationToken);
                                }
                                catch (OperationCanceledException)
                                {
                                    if (cancellationToken.IsCancellationRequested)
                                    {
                                        throw;
                                    }
                                }
                                catch
                                {
                                    // deliberately swallowed - a sub-menu command failure returns to the wizard loop instead of exiting it
                                }
                                break;
                            }
                        case '6':
                            try
                            {
                                currentMenu = await HandlePluginsMenuAsync(isRepoConfigured, cancellationToken);
                            }
                            catch (OperationCanceledException)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    throw;
                                }
                            }
                            catch
                            {
                                // deliberately swallowed - a sub-menu command failure returns to the wizard loop instead of exiting it
                            }
                            break;
                        default:
                            await _filesystem.SaveHistoryAsync(StartMenuHistoryKey, new ItemMenuWizard(), cancellationToken);
                            throw new NotImplementedException(string.Format(null, FormatMessages.ErrInvalidMenuOption, $"{currentMenu.Id} {currentMenu.Title}"));
                    }
                    if (Helper.SkipWizardContinuePrompt)
                    {
                        Helper.SkipWizardContinuePrompt = false;
                        continue;
                    }
                    if (!string.IsNullOrEmpty(currentMenu.Id))
                    {
                        if (_prompt.PromptPressAnyKeyToContinue($"{Resources.AdrPlus.PressAnyKey}...", cancellationToken))
                        {
                            throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser, cancellationToken);
                        }
                        if (currentMenu.Id == "1.01")
                        {
                            Helper.HasAppConfigChange = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessages.LogCommandException(_logger, ex);
                throw;
            }
        }

        /// <summary>
        /// Loads the last-used start menu item from persisted history, falling back to the appropriate
        /// default group based on whether the repository template is already configured.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The <see cref="ItemMenuWizard"/> representing the start menu to display.</returns>
        private async Task<ItemMenuWizard> LoadOrInitializeStartMenuAsync(CancellationToken cancellationToken)
        {
            var isRepoConfigured = _validateconfig.HasTemplateRepoFile();
            var defaultMenuId = isRepoConfigured ? "2" : "1";

            var (success, savedMenu) = await _filesystem.ReadHistoryAsync<ItemMenuWizard>(StartMenuHistoryKey, cancellationToken);
            if (success && savedMenu is not null && isRepoConfigured)
            {
                return savedMenu;
            }
            var startMenu = GetGroupMenu().First(x => x.Id == defaultMenuId);
            await _filesystem.SaveHistoryAsync(StartMenuHistoryKey, startMenu, cancellationToken);
            return startMenu;
        }

        /// <summary>
        /// Presents the main group menu to the user, persists the selection to history,
        /// and returns the chosen <see cref="ItemMenuWizard"/>.
        /// </summary>
        /// <param name="isRepoConfigured">Whether the repository template file exists, used to enable/disable menu items.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The selected <see cref="ItemMenuWizard"/>.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the user cancels the prompt.</exception>
        private async Task<ItemMenuWizard> HandleMainMenuAsync(bool isRepoConfigured, CancellationToken cancellationToken)
        {
            var (isAborted, itemSelected) = _wizardMenuPrompts.PromptSelectMenu(isRepoConfigured, GetGroupMenu(), new ItemMenuWizard(), cancellationToken);
            if (isAborted)
            {
                throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
            }

            if (itemSelected!.Id != "0")
            {
                await _filesystem.SaveHistoryAsync(StartMenuHistoryKey, itemSelected, cancellationToken);
            }

            return itemSelected;
        }

        /// <summary>
        /// Presents the configuration sub-menu, routes to the appropriate <c>config</c> sub-command
        /// (<c>--application</c>, <c>--template</c>, or <c>--repository</c>), and returns the selected item.
        /// Selecting "Back" returns an empty <see cref="ItemMenuWizard"/> to return to the main menu.
        /// </summary>
        /// <param name="isRepoConfigured">Whether the repository template file exists, used to enable/disable menu items.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The selected <see cref="ItemMenuWizard"/> (or empty to navigate back).</returns>
        /// <exception cref="OperationCanceledException">Thrown when the user cancels the prompt.</exception>
        private async Task<ItemMenuWizard> HandleConfigurationMenuAsync(bool isRepoConfigured, CancellationToken cancellationToken)
        {
            var (_, defaultMenu) = await _filesystem.ReadHistoryAsync<ItemMenuWizard>(ConfigMenuHistoryKey, cancellationToken);
            var (isAborted, itemSelected) = _wizardMenuPrompts.PromptSelectMenu(isRepoConfigured, GetMenuConfigurations(), defaultMenu ?? new ItemMenuWizard(), cancellationToken);

            if (isAborted)
            {
                throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
            }

            if (itemSelected!.Id == "1.00")
            {
                return new ItemMenuWizard();
            }

            await _filesystem.SaveHistoryAsync(ConfigMenuHistoryKey, itemSelected, cancellationToken);

            var commandAlias = GetCommandAlias(CommandsAdr.Config);
            string[] args = itemSelected.Id switch
            {
                "1.01" => ["-a"],
                "1.02" => ["-t"],
                "1.03" => ["-m"],
                "1.04" => ["-r"],
                _ => throw await CreateInvalidMenuExceptionAsync(ConfigMenuHistoryKey, itemSelected, cancellationToken),
            };
            try
            {
                _prompt.PromptEnabledEscToAbort(true);
                await _commandRouter.RouteAsync(commandAlias, args, cancellationToken);
            }
            finally
            {
                _prompt.PromptEnabledEscToAbort(false);
            }
            return itemSelected;
        }


        private async Task<ItemMenuWizard> HandleInitMigrateMenuAsync(bool isRepoConfigured, CancellationToken cancellationToken)
        {
            var (isAborted, itemSelected) = _wizardMenuPrompts.PromptSelectMenu(isRepoConfigured, GetMenuInitMigrate(), new ItemMenuWizard(), cancellationToken);

            if (isAborted)
            {
                throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
            }

            if (itemSelected!.Id == "5.00")
            {
                return new ItemMenuWizard();
            }

            await _filesystem.SaveHistoryAsync(ConfigMenuHistoryKey, itemSelected, cancellationToken);

            var commandAlias = GetCommandAlias(CommandsAdr.Config);
            if (itemSelected!.Id == "5.01")
            {
                commandAlias = GetCommandAlias(CommandsAdr.Init);
            }
            else if (itemSelected!.Id == "5.02")
            {
                commandAlias = GetCommandAlias(CommandsAdr.Migrate);
            }
            string[] args = itemSelected.Id switch
            {
                "5.01" => ["-w"],
                "5.02" => ["-w"],
                _ => throw await CreateInvalidMenuExceptionAsync(ConfigMenuHistoryKey, itemSelected, cancellationToken),
            };
            try
            {
                _prompt.PromptEnabledEscToAbort(true);
                await _commandRouter.RouteAsync(commandAlias, args, cancellationToken);
            }
            finally
            {
                _prompt.PromptEnabledEscToAbort(false);
            }
            return itemSelected;
        }

        /// <summary>
        /// Presents the ADR operations sub-menu, routes to the appropriate ADR command
        /// (init, new, approve, reject, version, revise, supersede, undo) with <c>--wizard</c> mode,
        /// and returns the selected item.
        /// Selecting "Back" returns an empty <see cref="ItemMenuWizard"/> to return to the main menu.
        /// </summary>
        /// <param name="isRepoConfigured">Whether the repository template file exists, used to enable/disable menu items.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The selected <see cref="ItemMenuWizard"/> (or empty to navigate back).</returns>
        /// <exception cref="OperationCanceledException">Thrown when the user cancels the prompt.</exception>
        private async Task<ItemMenuWizard> HandleAdrMenuAsync(bool isRepoConfigured, CancellationToken cancellationToken)
        {
            var (_, defaultMenu) = await _filesystem.ReadHistoryAsync<ItemMenuWizard>(AdrMenuHistoryKey, cancellationToken);
            var (isAborted, itemSelected) = _wizardMenuPrompts.PromptSelectMenu(isRepoConfigured, GetMenuAdr(), defaultMenu ?? new ItemMenuWizard(), cancellationToken);

            if (isAborted)
            {
                throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
            }

            if (itemSelected!.Id == "2.00")
            {
                return new ItemMenuWizard();
            }

            await _filesystem.SaveHistoryAsync(AdrMenuHistoryKey, itemSelected, cancellationToken);

            CommandsAdr command;
            string[] args;

            (command, args) = itemSelected.Id switch
            {
                "2.01" => (CommandsAdr.New, new[] { "-w" }),
                "2.02" => (CommandsAdr.Approve, new[] { "-w" }),
                "2.03" => (CommandsAdr.Reject, new[] { "-w" }),
                "2.04" => (CommandsAdr.Version, new[] { "-w" }),
                "2.05" => (CommandsAdr.Revise, new[] { "-w" }),
                "2.06" => (CommandsAdr.Supersede, new[] { "-w" }),
                "2.07" => (CommandsAdr.UndoStatus, new[] { "-w" }),
                _ => throw await CreateInvalidMenuExceptionAsync(AdrMenuHistoryKey, itemSelected, cancellationToken),
            };
            try
            {
                _prompt.PromptEnabledEscToAbort(true);
                await _commandRouter.RouteAsync(GetCommandAlias(command), args, cancellationToken);
            }
            finally
            {
                _prompt.PromptEnabledEscToAbort(false);
            }
            return itemSelected;
        }

        /// <summary>
        /// Presents the plugins sub-menu, routes to <c>sync</c> or <c>plugins</c> in <c>--wizard</c> mode
        /// (each of which asks its own mode — default/backfill, list/validate — internally), and returns the
        /// selected item. Selecting "Back" returns an empty <see cref="ItemMenuWizard"/> to return to the main menu.
        /// </summary>
        /// <param name="isRepoConfigured">Whether the repository template file exists, used to enable/disable menu items.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The selected <see cref="ItemMenuWizard"/> (or empty to navigate back).</returns>
        /// <exception cref="OperationCanceledException">Thrown when the user cancels the prompt.</exception>
        private async Task<ItemMenuWizard> HandlePluginsMenuAsync(bool isRepoConfigured, CancellationToken cancellationToken)
        {
            var (_, defaultMenu) = await _filesystem.ReadHistoryAsync<ItemMenuWizard>(PluginsMenuHistoryKey, cancellationToken);
            var (isAborted, itemSelected) = _wizardMenuPrompts.PromptSelectMenu(isRepoConfigured, GetMenuPlugins(), defaultMenu ?? new ItemMenuWizard(), cancellationToken);

            if (isAborted)
            {
                throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
            }

            if (itemSelected!.Id == "6.00")
            {
                return new ItemMenuWizard();
            }

            await _filesystem.SaveHistoryAsync(PluginsMenuHistoryKey, itemSelected, cancellationToken);

            CommandsAdr command;
            string[] args;

            (command, args) = itemSelected.Id switch
            {
                "6.01" => (CommandsAdr.Sync, new[] { "-w" }),
                "6.02" => (CommandsAdr.Plugins, new[] { "-w" }),
                _ => throw await CreateInvalidMenuExceptionAsync(PluginsMenuHistoryKey, itemSelected, cancellationToken),
            };
            try
            {
                _prompt.PromptEnabledEscToAbort(true);
                await _commandRouter.RouteAsync(GetCommandAlias(command), args, cancellationToken);
            }
            finally
            {
                _prompt.PromptEnabledEscToAbort(false);
            }
            return itemSelected;
        }

        /// <summary>
        /// Presents the command help sub-menu, routes to the selected command handler with the <c>--help</c>
        /// flag to display its detailed usage, and returns the selected item.
        /// Selecting "Back" returns an empty <see cref="ItemMenuWizard"/> to return to the main menu.
        /// </summary>
        /// <param name="isRepoConfigured">Whether the repository template file exists, used to enable/disable menu items.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The selected <see cref="ItemMenuWizard"/> (or empty to navigate back).</returns>
        /// <exception cref="OperationCanceledException">Thrown when the user cancels the prompt.</exception>
        private async Task<ItemMenuWizard> HandleHelpMenuAsync(bool isRepoConfigured, CancellationToken cancellationToken)
        {
            var (_, defaultMenu) = await _filesystem.ReadHistoryAsync<ItemMenuWizard>(HelpMenuHistoryKey, cancellationToken);
            var (isAborted, itemSelected) = _wizardMenuPrompts.PromptSelectMenu(isRepoConfigured, GetMenuHelp(), defaultMenu ?? new ItemMenuWizard(), cancellationToken);

            if (isAborted)
            {
                throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
            }

            if (itemSelected!.Id == "3.00")
            {
                return new ItemMenuWizard();
            }

            await _filesystem.SaveHistoryAsync(HelpMenuHistoryKey, itemSelected, cancellationToken);
            var command = itemSelected.Id switch
            {
                "3.01" => CommandsAdr.Config,
                "3.02" => CommandsAdr.Init,
                "3.03" => CommandsAdr.Explore,
                "3.04" => CommandsAdr.Migrate,
                "3.05" => CommandsAdr.New,
                "3.06" => CommandsAdr.Approve,
                "3.07" => CommandsAdr.Reject,
                "3.08" => CommandsAdr.Version,
                "3.09" => CommandsAdr.Revise,
                "3.10" => CommandsAdr.Supersede,
                "3.11" => CommandsAdr.UndoStatus,
                "3.12" => CommandsAdr.Sync,
                "3.13" => CommandsAdr.Plugins,
                _ => throw await CreateInvalidMenuExceptionAsync(HelpMenuHistoryKey, itemSelected, cancellationToken),
            };
            await _commandRouter.RouteAsync(GetCommandAlias(command), ["-h"], cancellationToken);

            return itemSelected;
        }

        /// <summary>
        /// Returns the CLI alias for a given <see cref="CommandsAdr"/> enum value by looking it up in the commands map.
        /// </summary>
        /// <param name="command">The ADR command whose alias is needed.</param>
        /// <returns>The string alias (e.g. <c>"new"</c>, <c>"approve"</c>).</returns>
        private string GetCommandAlias(CommandsAdr command) =>
            _commandsMap.First(x => x.Command == command).Alias;

        /// <summary>
        /// Reads the name and version of every plugin bundled under <see cref="_builtinPluginsRoot"/>
        /// (e.g. <c>plugins-builtin/adr-indexer/plugin.json</c>), for display only — no manifest validation,
        /// assembly loading, or allowlist checks, since this is just informing the user what ships with this
        /// adrplus install, independent of any repository's <c>ActivePlugins</c>.
        /// </summary>
        /// <returns>A list of <c>"Name vVersion"</c> strings, one per bundled plugin found; empty when <see cref="_builtinPluginsRoot"/> is unset or absent.</returns>
        internal List<string> GetBuiltinPluginsSummary()
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(_builtinPluginsRoot) || !Directory.Exists(_builtinPluginsRoot))
            {
                return result;
            }

            foreach (var folderPath in Directory.EnumerateDirectories(_builtinPluginsRoot).OrderBy(path => path, StringComparer.Ordinal))
            {
                var manifestPath = Path.Combine(folderPath, "plugin.json");
                if (!File.Exists(manifestPath))
                {
                    continue;
                }
                var manifest = JsonSerializer.Deserialize<PluginManifest>(File.ReadAllText(manifestPath), PluginManifest.SerializerOptions);
                if (manifest?.Name is { Length: > 0 })
                {
                    result.Add($"{manifest.Name} v{manifest.Version}");
                }
            }
            return result;
        }

        /// <summary>
        /// Clears the menu history for <paramref name="historyKey"/> and returns a
        /// <see cref="NotImplementedException"/> describing the unrecognized menu option.
        /// </summary>
        /// <param name="historyKey">The persistence key of the menu whose history should be reset.</param>
        /// <param name="menu">The invalid menu item that was selected.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="NotImplementedException"/> ready to be thrown by the caller.</returns>
        private async Task<NotImplementedException> CreateInvalidMenuExceptionAsync(string historyKey, ItemMenuWizard menu, CancellationToken cancellationToken)
        {
            await _filesystem.SaveHistoryAsync(historyKey, new ItemMenuWizard(), cancellationToken);
            return new NotImplementedException(string.Format(null, FormatMessages.ErrInvalidMenuOption, $"{menu.Id} {menu.Title}"));
        }

        /// <summary>
        /// Returns the top-level group menu items (Configurations, Init/Migrate, ADRs, Command Help, Explore Report, Plugins, Exit).
        /// </summary>
        /// <returns>An array of <see cref="ItemMenuWizard"/> representing the top-level menu options.</returns>
        private static ItemMenuWizard[] GetGroupMenu()
        {
            return
            [
                new ItemMenuWizard
                {
                    Id = "1",
                    Title = Resources.AdrPlus.WizardGroupConfigurationsTitle,
                    Description = Resources.AdrPlus.WizardGroupConfigurationsDescription,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "5",
                    Title = Resources.AdrPlus.WizardGroupInitAndMigrate,
                    Description = Resources.AdrPlus.WizardGroupInitAndMigrateDescription,
                    EnabledWhenNotConfigured = false
                },
                new ItemMenuWizard
                {
                    Id = "2",
                    Title = Resources.AdrPlus.WizardGroupAdrsTitle,
                    Description = Resources.AdrPlus.WizardGroupAdrsDescription,
                    EnabledWhenNotConfigured = false
                },
                new ItemMenuWizard
                {
                    Id = "3",
                    Title = Resources.AdrPlus.WizardGroupCommandHelpTitle,
                    Description = Resources.AdrPlus.ShowHelpInfo,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "4",
                    Title = Resources.AdrPlus.WizardGroupExploreReportTitle,
                    Description = Resources.AdrPlus.WizardGroupExploreReportDescription,
                    EnabledWhenNotConfigured = false
                },
                new ItemMenuWizard
                {
                    Id = "6",
                    Title = Resources.AdrPlus.WizardGroupPluginsTitle,
                    Description = Resources.AdrPlus.WizardGroupPluginsDescription,
                    EnabledWhenNotConfigured = false
                },
                new ItemMenuWizard
                {
                    Id = "0",
                    Title = Resources.AdrPlus.WizardGroupExitTitle,
                    Description = Resources.AdrPlus.WizardGroupExitDescription,
                    EnabledWhenNotConfigured = true
                },

            ];
        }

        /// <summary>
        /// Returns the init/migrate sub-menu items (init, migrate, back).
        /// </summary>
        /// <returns>An array of <see cref="ItemMenuWizard"/> representing the init/migrate menu.</returns>
        private static ItemMenuWizard[] GetMenuInitMigrate()
        {
            return [
            new ItemMenuWizard
                {
                    Id = "5.00",
                    Title = Resources.AdrPlus.WizardMainMenu,
                    Description = Resources.AdrPlus.WizardHelpMainMenuDescription,
                    EnabledWhenNotConfigured = true
                },
            new ItemMenuWizard
            {
                Id = "5.01",
                Title = Resources.AdrPlus.WizardAdrInitTitle,
                Description = Resources.AdrPlus.EscForReturnWizard,
                EnabledWhenNotConfigured = false
            },
            new ItemMenuWizard
            {
                Id = "5.02",
                Title = Resources.AdrPlus.WizardConfigMigratedTitle,
                Description = Resources.AdrPlus.EscForReturnWizard,
                EnabledWhenNotConfigured = false
            }];
        }

        /// <summary>
        /// Returns the ADR operations sub-menu items (init, new, approve, reject, version, revise, supersede, undo, back).
        /// </summary>
        /// <returns>An array of <see cref="ItemMenuWizard"/> representing the ADR operations menu.</returns>
        private static ItemMenuWizard[] GetMenuAdr()
        {
            return
            [
                new ItemMenuWizard
                {
                    Id = "2.00",
                    Title = Resources.AdrPlus.WizardMainMenu,
                    Description = Resources.AdrPlus.WizardHelpMainMenuDescription,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "2.01",
                    Title = Resources.AdrPlus.WizardAdrNewTitle,
                    Description = Resources.AdrPlus.EscForReturnWizard,
                    EnabledWhenNotConfigured = false
                },
                new ItemMenuWizard
                {
                    Id = "2.02",
                    Title = Resources.AdrPlus.WizardAdrApproveTitle,
                    Description = Resources.AdrPlus.EscForReturnWizard,
                    EnabledWhenNotConfigured = false
                },
                new ItemMenuWizard
                {
                    Id = "2.03",
                    Title = Resources.AdrPlus.WizardAdrRejectTitle,
                    Description = Resources.AdrPlus.EscForReturnWizard,
                    EnabledWhenNotConfigured = false
                },
                new ItemMenuWizard
                {
                    Id = "2.04",
                    Title = Resources.AdrPlus.WizardAdrVersionTitle,
                    Description = Resources.AdrPlus.EscForReturnWizard,
                    EnabledWhenNotConfigured = false
                },
                new ItemMenuWizard
                {
                    Id = "2.05",
                    Title = Resources.AdrPlus.WizardAdrRevisionTitle,
                    Description = Resources.AdrPlus.EscForReturnWizard,
                    EnabledWhenNotConfigured = false
                },
                new ItemMenuWizard
                {
                    Id = "2.06",
                    Title = Resources.AdrPlus.WizardAdrSupersedeTitle,
                    Description = Resources.AdrPlus.EscForReturnWizard,
                    EnabledWhenNotConfigured = false
                },
                new ItemMenuWizard
                {
                    Id = "2.07",
                    Title = Resources.AdrPlus.WizardAdrUndoStatusTitle,
                    Description = Resources.AdrPlus.EscForReturnWizard,
                    EnabledWhenNotConfigured = false
                },
            ];
        }

        /// <summary>
        /// Returns the plugins sub-menu items (sync, plugins, back).
        /// </summary>
        /// <returns>An array of <see cref="ItemMenuWizard"/> representing the plugins menu.</returns>
        private static ItemMenuWizard[] GetMenuPlugins()
        {
            return
            [
                new ItemMenuWizard
                {
                    Id = "6.00",
                    Title = Resources.AdrPlus.WizardMainMenu,
                    Description = Resources.AdrPlus.WizardHelpMainMenuDescription,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "6.01",
                    Title = Resources.AdrPlus.WizardPluginsSyncTitle,
                    Description = Resources.AdrPlus.EscForReturnWizard,
                    EnabledWhenNotConfigured = false
                },
                new ItemMenuWizard
                {
                    Id = "6.02",
                    Title = Resources.AdrPlus.WizardPluginsDiagnosticsTitle,
                    Description = Resources.AdrPlus.EscForReturnWizard,
                    EnabledWhenNotConfigured = false
                },
            ];
        }

        /// <summary>
        /// Returns the configuration sub-menu items (application, template, repository, back).
        /// </summary>
        /// <returns>An array of <see cref="ItemMenuWizard"/> representing the configuration menu options.</returns>
        private static ItemMenuWizard[] GetMenuConfigurations()
        {
            return
            [
                new ItemMenuWizard
                {
                    Id = "1.00",
                    Title = Resources.AdrPlus.WizardMainMenu,
                    Description = Resources.AdrPlus.WizardHelpMainMenuDescription,
                    EnabledWhenNotConfigured = false
                },
                new ItemMenuWizard
                {
                    Id = "1.01",
                    Title = Resources.AdrPlus.WizardConfigApplicationTitle,
                    Description = Resources.AdrPlus.EscForReturnWizard,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "1.02",
                    Title = Resources.AdrPlus.WizardConfigTemplateTitle,
                    Description = Resources.AdrPlus.EscForReturnWizard,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "1.03",
                    Title = Resources.AdrPlus.WizardConfigMigration,
                    Description = Resources.AdrPlus.EscForReturnWizard,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "1.04",
                    Title = Resources.AdrPlus.WizardConfigRepositoryTitle,
                    Description = Resources.AdrPlus.EscForReturnWizard,
                    EnabledWhenNotConfigured = true
                }
            ];
        }

        /// <summary>
        /// Returns the command help sub-menu items, one entry per available command plus a back option.
        /// </summary>
        /// <returns>An array of <see cref="ItemMenuWizard"/> representing the help menu options.</returns>
        private static ItemMenuWizard[] GetMenuHelp()
        {
            return
            [
                new ItemMenuWizard
                {
                    Id = "3.00",
                    Title = Resources.AdrPlus.WizardMainMenu,
                    Description = Resources.AdrPlus.WizardHelpMainMenuDescription,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "3.01",
                    Title = Resources.AdrPlus.WizardHelpConfigTitle,
                    Description = Resources.AdrPlus.ShowHelpInfo,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "3.02",
                    Title = Resources.AdrPlus.WizardHelpInitTitle,
                    Description = Resources.AdrPlus.ShowHelpInfo,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "3.03",
                    Title = Resources.AdrPlus.WizardHelpExploreTitle,
                    Description = Resources.AdrPlus.ShowHelpInfo,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "3.04",
                    Title = Resources.AdrPlus.WizardHelpMigrateTitle,
                    Description = Resources.AdrPlus.ShowHelpInfo,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "3.05",
                    Title = Resources.AdrPlus.WizardHelpNewTitle,
                    Description = Resources.AdrPlus.ShowHelpInfo,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "3.06",
                    Title = Resources.AdrPlus.WizardHelpApproveTitle,
                    Description = Resources.AdrPlus.ShowHelpInfo,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "3.07",
                    Title = Resources.AdrPlus.WizardHelpRejectTitle,
                    Description = Resources.AdrPlus.ShowHelpInfo,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "3.08",
                    Title = Resources.AdrPlus.WizardHelpVersionTitle,
                    Description = Resources.AdrPlus.ShowHelpInfo,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "3.09",
                    Title = Resources.AdrPlus.WizardHelpRevisionTitle,
                    Description = Resources.AdrPlus.ShowHelpInfo,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "3.10",
                    Title = Resources.AdrPlus.WizardHelpSupersedeTitle,
                    Description = Resources.AdrPlus.ShowHelpInfo,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "3.11",
                    Title = Resources.AdrPlus.WizardHelpUndoTitle,
                    Description = Resources.AdrPlus.ShowHelpInfo,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "3.12",
                    Title = Resources.AdrPlus.WizardHelpSyncTitle,
                    Description = Resources.AdrPlus.ShowHelpInfo,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "3.13",
                    Title = Resources.AdrPlus.WizardHelpPluginsTitle,
                    Description = Resources.AdrPlus.ShowHelpInfo,
                    EnabledWhenNotConfigured = true
                },
            ];
        }

    }
}
