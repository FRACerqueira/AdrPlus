// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.Formatting;
using AdrPlus.Infrastructure.Logging;
using AdrPlus.Infrastructure.UI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;


namespace AdrPlus.Commands.Wizard
{
    /// <summary>
    /// Handles the <c>wizard</c> command to provide a full interactive wizard experience for ADR operations.
    /// Presents a hierarchical menu to configure the application, manage ADRs, or access per-command help.
    /// Persists the last-selected menu item across sessions.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="WizardCommandHandler"/> class.
    /// </remarks>
    /// <param name="commandRouter">The command router used to dispatch selected commands.</param>
    /// <param name="configuration">The application configuration for reading version and banner data.</param>
    /// <param name="logger">The logger for recording command execution and errors.</param>
    /// <param name="fileSystem">The file system service for persisting wizard history.</param>
    /// <param name="validateconfig">The service for checking whether the repository template is configured.</param>
    /// <param name="console">The console writer for displaying menus, banners, and prompts.</param>
    /// <param name="adrServices">The ADR services for argument parsing and command metadata.</param>
    internal sealed partial class WizardCommandHandler(
        CommandRouter commandRouter,
        IConfiguration configuration,
        IOptionsMonitor<AdrPlusConfig> config,
        ILogger<WizardCommandHandler> logger,
        IFileSystemService fileSystem,
        IValidateJsonConfig validateconfig,
        IConsoleWriter console,
        IAdrServices adrServices) : ICommandHandler
    {
        private readonly ILogger<WizardCommandHandler> _logger = logger;
        private readonly IFileSystemService _filesystem = fileSystem;
        private readonly IConsoleWriter _console = console;
        private readonly IValidateJsonConfig _validateconfig = validateconfig;
        private readonly IConfiguration _configuration = configuration;
        private readonly CommandRouter _commandRouter = commandRouter;
        private readonly IAdrServices _adrServices = adrServices;
        private readonly (CommandsAdr Command, string Alias, Type ConfigCommandHandler, string Description)[] _commandsMap = adrServices.GetCommands();
        private readonly IOptionsMonitor<AdrPlusConfig> _configMonitor = config;
        private AdrPlusConfig CurrentConfig  = config.CurrentValue;

        private static readonly Arguments[] ValidCommandArgs = [Arguments.Help];
        private const string StartMenuHistoryKey = "StartMenuWizard";
        private const string ConfigMenuHistoryKey = "DefaultConfigMenu";
        private const string AdrMenuHistoryKey = "DefaultAdrMenu";
        private const string HelpMenuHistoryKey = "DefaultHelpMenu";


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
            ArgumentNullException.ThrowIfNull(args);
            try
            {
                var parsedArgs = _adrServices.ParseArgs(args, ValidCommandArgs);
                if (parsedArgs.ContainsKey(Arguments.Help) && args.Length != 0)
                {
                    _console.WriteHelp(_adrServices.GetHelpText(
                        "wizard",
                        ValidCommandArgs,
                            ["adrplus wizard"]));
                    return;
                }
                var currentMenu = await LoadOrInitializeStartMenuAsync(cancellationToken);
                while (true)
                {
                    _console.ShowBanner(AppConstants.BannerText);
                    _console.ShowWellcome(_configuration[AppConstants.CfgNameVersionApp] ?? string.Empty);
                    _console.WriteStartCommand(string.Format(null, FormatMessages.MsgCommandStartedFormat, "wizard"));

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
                                if (_console.IsAbortedByCtrlC())
                                {
                                    throw;
                                }
                            }
                            catch
                            {
                                // If an exception occurs , skip excepion.
                            }
                            break;
                        case '2':
                            try
                            {
                                currentMenu = await HandleAdrMenuAsync(isRepoConfigured, cancellationToken);
                            }
                            catch (OperationCanceledException)
                            {
                                if (_console.IsAbortedByCtrlC())
                                {
                                    throw;
                                }
                            }
                            catch
                            {
                                // If an exception occurs , skip excepion.
                            }
                            break;
                        case '3':
                            try
                            {
                                currentMenu = await HandleHelpMenuAsync(isRepoConfigured, cancellationToken);
                            }
                            catch (OperationCanceledException)
                            {
                                if (_console.IsAbortedByCtrlC())
                                {
                                    throw;
                                }
                            }
                            catch
                            {
                                // If an exception occurs , skip excepion.
                            }
                            break;
                        default:
                            await _filesystem.SaveHistoryAsync(StartMenuHistoryKey, new ItemMenuWizard(), cancellationToken);
                            throw new NotImplementedException(string.Format(null, FormatMessages.InvalidMenuOption, $"{currentMenu.Id} {currentMenu.Title}"));
                    }
                    if (!string.IsNullOrEmpty(currentMenu.Id))
                    {
                        if (currentMenu.Id == "1.01" &&  !AppConstants.LanguageSetting.Equals(CultureInfo.CurrentCulture.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            break;
                        }
                        if (_console.PressAnyKeyToContinue($"{Resources.AdrPlus.PressAnyKey}...", cancellationToken))
                        {
                            throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser, cancellationToken);
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
            var (isAborted, itemSelected) = _console.PromptSelectMenu(isRepoConfigured, GetGroupMenu(), new ItemMenuWizard(), cancellationToken);
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
            var (isAborted, itemSelected) = _console.PromptSelectMenu(isRepoConfigured, GetMenuConfigurations(), defaultMenu ?? new ItemMenuWizard(), cancellationToken);

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
                "1.03" => ["-r"],
                _ => throw await CreateInvalidMenuExceptionAsync(ConfigMenuHistoryKey, itemSelected, cancellationToken),
            };
            try
            {
                _console.EnabledEscToAbort(true);
                await _commandRouter.RouteAsync(commandAlias, args, cancellationToken);
            }
            finally
            {
                _console.EnabledEscToAbort(false);
            }
            return itemSelected;
        }

        /// <summary>
        /// Presents the ADR operations sub-menu, routes to the appropriate ADR command
        /// (init, new, approve, reject, version, review, supersede, undo) with <c>--wizard</c> mode,
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
            var (isAborted, itemSelected) = _console.PromptSelectMenu(isRepoConfigured, GetMenuAdr(), defaultMenu ?? new ItemMenuWizard(), cancellationToken);

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
                "2.01" => (CommandsAdr.Init, new[] { "-w" }),
                "2.02" => (CommandsAdr.New, new[] { "-w" }),
                "2.03" => (CommandsAdr.Approve, new[] { "-w" }),
                "2.04" => (CommandsAdr.Reject, new[] { "-w" }),
                "2.05" => (CommandsAdr.Version, new[] { "-w" }),
                "2.06" => (CommandsAdr.Review, new[] { "-w" }),
                "2.07" => (CommandsAdr.Supersede, new[] { "-w" }),
                "2.08" => (CommandsAdr.UndoStatus, new[] { "-w" }),
                _ => throw await CreateInvalidMenuExceptionAsync(AdrMenuHistoryKey, itemSelected, cancellationToken),
            };
            try
            {
                _console.EnabledEscToAbort(true);
                await _commandRouter.RouteAsync(GetCommandAlias(command), args, cancellationToken);
            }
            finally
            {
                _console.EnabledEscToAbort(false);
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
            var (isAborted, itemSelected) = _console.PromptSelectMenu(isRepoConfigured, GetMenuHelp(), defaultMenu ?? new ItemMenuWizard(), cancellationToken);

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
                "3.03" => CommandsAdr.New,
                "3.04" => CommandsAdr.Approve,
                "3.05" => CommandsAdr.Reject,
                "3.06" => CommandsAdr.Version,
                "3.07" => CommandsAdr.Review,
                "3.08" => CommandsAdr.Supersede,
                "3.09" => CommandsAdr.UndoStatus,
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
            return new NotImplementedException(string.Format(null, FormatMessages.InvalidMenuOption, $"{menu.Id} {menu.Title}"));
        }

        /// <summary>
        /// Returns the top-level group menu items (Configurations, ADRs, Command Help, Exit).
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
                    Id = "2",
                    Title = Resources.AdrPlus.WizardGroupAdrsTitle,
                    Description = Resources.AdrPlus.WizardGroupAdrsDescription,
                    EnabledWhenNotConfigured = false
                },
                new ItemMenuWizard
                {
                    Id = "3",
                    Title = Resources.AdrPlus.WizardGroupCommandHelpTitle,
                    Description = Resources.AdrPlus.WizardGroupCommandHelpDescription,
                    EnabledWhenNotConfigured = true
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
        /// Returns the ADR operations sub-menu items (init, new, approve, reject, version, review, supersede, undo, back).
        /// </summary>
        /// <returns>An array of <see cref="ItemMenuWizard"/> representing the ADR operations menu.</returns>
        private static ItemMenuWizard[] GetMenuAdr()
        {
            return
            [
                new ItemMenuWizard
                {
                    Id = "2.00",
                    Title = Resources.AdrPlus.WizardAdrMainMenuTitle,
                    Description = Resources.AdrPlus.WizardAdrMainMenuDescription,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "2.01",
                    Title = Resources.AdrPlus.WizardAdrInitTitle,
                    Description = Resources.AdrPlus.WizardAdrInitDescription,
                    EnabledWhenNotConfigured = false
                },
                new ItemMenuWizard
                {
                    Id = "2.02",
                    Title = Resources.AdrPlus.WizardAdrNewTitle,
                    Description = Resources.AdrPlus.WizardAdrNewDescription,
                    EnabledWhenNotConfigured = false
                },
                new ItemMenuWizard
                {
                    Id = "2.03",
                    Title = Resources.AdrPlus.WizardAdrApproveTitle,
                    Description = Resources.AdrPlus.WizardAdrApproveDescription,
                    EnabledWhenNotConfigured = false
                },
                new ItemMenuWizard
                {
                    Id = "2.04",
                    Title = Resources.AdrPlus.WizardAdrRejectTitle,
                    Description = Resources.AdrPlus.WizardAdrRejectDescription,
                    EnabledWhenNotConfigured = false
                },
                new ItemMenuWizard
                {
                    Id = "2.05",
                    Title = Resources.AdrPlus.WizardAdrVersionTitle,
                    Description = Resources.AdrPlus.WizardAdrVersionDescription,
                    EnabledWhenNotConfigured = false
                },
                new ItemMenuWizard
                {
                    Id = "2.06",
                    Title = Resources.AdrPlus.WizardAdrRevisionTitle,
                    Description = Resources.AdrPlus.WizardAdrRevisionDescription,
                    EnabledWhenNotConfigured = false
                },
                new ItemMenuWizard
                {
                    Id = "2.07",
                    Title = Resources.AdrPlus.WizardAdrSupersedeTitle,
                    Description = Resources.AdrPlus.WizardAdrSupersedeDescription,
                    EnabledWhenNotConfigured = false
                },
                new ItemMenuWizard
                {
                    Id = "2.08",
                    Title = Resources.AdrPlus.WizardAdrUndoStatusTitle,
                    Description = Resources.AdrPlus.WizardAdrUndoStatusDescription,
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
                    Title = Resources.AdrPlus.WizardConfigMainMenuTitle,
                    Description = Resources.AdrPlus.WizardConfigMainMenuDescription,
                    EnabledWhenNotConfigured = false
                },
                new ItemMenuWizard
                {
                    Id = "1.01",
                    Title = Resources.AdrPlus.WizardConfigApplicationTitle,
                    Description = Resources.AdrPlus.WizardConfigApplicationDescription,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "1.02",
                    Title = Resources.AdrPlus.WizardConfigTemplateTitle,
                    Description = Resources.AdrPlus.WizardConfigTemplateDescription,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "1.03",
                    Title = Resources.AdrPlus.WizardConfigRepositoryTitle,
                    Description = Resources.AdrPlus.WizardConfigRepositoryDescription,
                    EnabledWhenNotConfigured = true
                },
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
                    Title = Resources.AdrPlus.WizardHelpMainMenuTitle,
                    Description = Resources.AdrPlus.WizardHelpMainMenuDescription,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "3.01",
                    Title = Resources.AdrPlus.WizardHelpConfigTitle,
                    Description = Resources.AdrPlus.WizardHelpConfigDescription,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "3.02",
                    Title = Resources.AdrPlus.WizardHelpInitTitle,
                    Description = Resources.AdrPlus.WizardHelpInitDescription,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "3.03",
                    Title = Resources.AdrPlus.WizardHelpNewTitle,
                    Description = Resources.AdrPlus.WizardHelpNewDescription,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "3.04",
                    Title = Resources.AdrPlus.WizardHelpApproveTitle,
                    Description = Resources.AdrPlus.WizardHelpApproveDescription,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "3.05",
                    Title = Resources.AdrPlus.WizardHelpRejectTitle,
                    Description = Resources.AdrPlus.WizardHelpRejectDescription,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "3.06",
                    Title = Resources.AdrPlus.WizardHelpVersionTitle,
                    Description = Resources.AdrPlus.WizardHelpVersionDescription,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "3.07",
                    Title = Resources.AdrPlus.WizardHelpRevisionTitle,
                    Description = Resources.AdrPlus.WizardHelpRevisionDescription,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "3.08",
                    Title = Resources.AdrPlus.WizardHelpSupersedeTitle,
                    Description = Resources.AdrPlus.WizardHelpSupersedeDescription,
                    EnabledWhenNotConfigured = true
                },
                new ItemMenuWizard
                {
                    Id = "3.09",
                    Title = Resources.AdrPlus.WizardHelpUndoTitle,
                    Description = Resources.AdrPlus.WizardHelpUndoDescription,
                    EnabledWhenNotConfigured = true
                },
            ];
        }

    }
}
