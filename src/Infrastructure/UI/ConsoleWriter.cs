// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.Formatting;
using PromptPlusLibrary;
using System.Globalization;

namespace AdrPlus.Infrastructure.UI
{
    /// <summary>
    /// Console writer implementation using PromptPlus library.
    /// </summary>
    internal sealed class ConsoleWriter(IAdrServices adrServices) : IConsoleWriter
    {
        private readonly IAdrServices _adrServices = adrServices;

        /// <inheritdoc/>
        public bool IsAbortedByCtrlC()
        { 
            return PromptPlus.AbortedByCtrlC;
        }

        /// <inheritdoc/>
        public void EnabledEscToAbort(bool enabled)
        { 
            PromptPlus.Config.EnabledAbortKey = enabled;
        }

        /// <inheritdoc/>
        public bool PressAnyKeyToContinue(string message, CancellationToken cancellationToken)
        {
            PromptPlus.Console.WriteLine("");
            PromptPlus.Controls.KeyPress(message)
                .Options(opt => opt.ShowTooltip(false))
                .Run(cancellationToken);
            return PromptPlus.AbortedByCtrlC;
        }

        /// <inheritdoc/>
        public (int left, int top) GetCursorPosition()
        {
            return PromptPlus.Console.GetCursorPosition();
        }

        /// <inheritdoc/>
        public void WriteWait(string message)
        {
            PromptPlus.Console.Write($"[{AppConstants.ColorWarning}]{message}[/]");
        }

        public void ClearWait((int left, int top) position)
        {
            PromptPlus.Console.ClearLine();
            PromptPlus.Console.SetCursorPosition(position.left, position.top);
        }

        /// <inheritdoc/>
        public void WriteSummary(string message)
        {
            PromptPlus.Console.WriteLine($"[{AppConstants.ColorSummary}]{message}[/]");
        }


        /// <inheritdoc/>
        public void WriteInfo(string message)
        {
            PromptPlus.Console.WriteLine($"[{AppConstants.ColorInfo}]{message}[/]");
        }

        /// <inheritdoc/>
        public void WriteSuccess(string message)
        {
            PromptPlus.Console.WriteLine($"[{AppConstants.ColorResult}]{message}[/]");
        }

        /// <inheritdoc/>
        public void WriteError(string message)
        {
            PromptPlus.Console.WriteLine($"[{AppConstants.ColorError}]{message}[/]");
        }

        /// <inheritdoc/>
        public void WriteHelp(string helpText)
        {
            PromptPlus.Console.WriteLine($"[{AppConstants.ColorHelp}]{helpText}[/]");
        }

        /// <inheritdoc/>
        public void WriteStartCommand(string text)
        {
            PromptPlus.Console.WriteLine(text, AppConstants.ColorWelcomeBanner);
            PromptPlus.Console.WriteLine("");
        }

        /// <inheritdoc/>
        public void WriteFinishedCommand(string text)
        {
            PromptPlus.Console.WriteLine("");
            PromptPlus.Console.WriteLine(text, AppConstants.ColorWelcomeBanner);
        }

        /// <summary>
        /// Displays an error message to the console using a static method.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        public static void ShowError(string message)
        {
            PromptPlus.Console.WriteLine($"[{AppConstants.ColorError}]{message}[/]");
        }

        /// <inheritdoc/>
        public void ShowWellcome(string appVersion)
        {
            PromptPlus.Console.WriteLine($"[{AppConstants.ColorInfo}]{string.Format(null, FormatMessages.WelcomeFormat, appVersion)}[/]");
            PromptPlus.Console.WriteLine("");
        }

        /// <inheritdoc/>
        public void ConfigurePrompt(AdrPlusConfig config)
        {
            var cultureInfo = new CultureInfo(config.Language);
            PromptPlus.Config.DefaultCulture = cultureInfo;
            PromptPlus.Config.EnabledAbortKey = false;
            PromptPlus.Config.EnableMessageAbortCtrlC = false;
            PromptPlus.Config.HideAfterFinish = true;
            PromptPlus.Config.PageSize = 10;

            if (!string.IsNullOrWhiteSpace(config.YesValue))
            {
                PromptPlus.Config.YesChar = config.YesValue[0];
            }
            if (!string.IsNullOrWhiteSpace(config.NoValue))
            {
                PromptPlus.Config.NoChar = config.NoValue[0];
            }
        }

        public void EnsureCulture(AdrPlusConfig config)
        {
            var cultureInfo = new CultureInfo(config.Language);

            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
            CultureInfo.CurrentCulture = cultureInfo;
            CultureInfo.CurrentUICulture = cultureInfo;
            PromptPlus.Config.DefaultCulture = cultureInfo; 
            if (!string.IsNullOrWhiteSpace(config.YesValue))
            {
                PromptPlus.Config.YesChar = config.YesValue[0];
            }
            if (!string.IsNullOrWhiteSpace(config.NoValue))
            {
                PromptPlus.Config.NoChar = config.NoValue[0];
            }
        }

        /// <inheritdoc/>
        public void ShowBanner(string bannerText)
        {
           PromptPlus.Console.Clear();
           PromptPlus.Widgets
                .Banner(bannerText, AppConstants.ColorWelcomeBanner)
                .Border(BannerDashOptions.DoubleBorderDown)
                .Show();
        }

        /// <inheritdoc/>
        public (bool IsAborted, DateTime Content) PrompCalendar(string message, DateTime dateref, AdrPlusConfig config, CancellationToken cancellationToken = default)
        {
            message =$"{message}: ";
            var result = PromptPlus.Controls
                .Calendar(message)
                .Culture(config.Language)
                .Default(dateref)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? dateref : result.Content!.Value);
        }

        /// <inheritdoc/>
        public (bool IsAborted, bool ConfirmYes) PromptConfirm(string message, CancellationToken cancellationToken = default)
        {
            var result = PromptPlus.Controls
                .Confirm(message)
                .Run(cancellationToken);
            return (result.IsAborted, (result.IsAborted ? default : result.Content!.Value).IsYesResponseKey());
        }

        /// <inheritdoc/>
        public (bool IsAborted, FieldsJson? Content) PromptConfigJsonAppSelect(FieldsJson defaultvalue, IEnumerable<FieldsJson> fields, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptSelectField}: ";
            var result = PromptPlus.Controls
                .Select<FieldsJson>(message, "")
                .Default(defaultvalue)
                .AddItem(new FieldsJson { Name = Resources.AdrPlus.ConfigActionSaveAndFinish, IsEndEdit = true })
                .AddGroupedItems(Resources.AdrPlus.Fields, fields.Where(x => x.IsEnabled), false)
                .AddItem(new FieldsJson { Name = Resources.AdrPlus.ConfigActionSaveAndFinish, IsEndEdit = true })
                .TextSelector(field => $"{AppConstants.GetTitleField(field.Name)} ")
                .ExtraInfo(field => field.IsEndEdit ? "" : field.Value)
                .ChangeDescription(field => ShowDescField(field))
                .EqualItems((a, b) => a.Name == b.Name)
                .MaxWidth(25)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? null : result.Content);
        }

        /// <inheritdoc/>
        public (bool IsAborted, FieldsJson? Content) PromptConfigJsonRepoSelect(FieldsJson defaultvalue, IEnumerable<FieldsJson> fields, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptSelectField}: ";
            var result = PromptPlus.Controls
                .Select<FieldsJson>(message, "")
                .Default(defaultvalue)
                .AddItem(new FieldsJson { Name = Resources.AdrPlus.ConfigActionSaveAndFinish, IsEndEdit = true })
                .AddGroupedItems(Resources.AdrPlus.Fields, fields.Where(x => x.IsEnabled), false)
                .AddItem(new FieldsJson { Name = Resources.AdrPlus.ConfigActionSaveAndFinish, IsEndEdit = true })
                .TextSelector(field => $"{AppConstants.GetTitleField(field.Name)} ")
                .ExtraInfo(field => field.IsEndEdit ? "" : field.Value)
                .ChangeDescription(field => ShowDescField(field))
                .EqualItems((a, b) => a.Name == b.Name)
                .MaxWidth(25)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? null : result.Content);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptEditFieldLanguage(FieldsJson fieldsJson, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptEnterNewValue}: ";
            var result = PromptPlus.Controls
                .Input(message, ShowDescField(fieldsJson))
                .Default(fieldsJson.Value)
                .MaxLength(5)
                .AcceptInput(input => char.IsAsciiLetter(input) || input == '-')
                .SuggestionHandler(input => ["en-us","pt-br"])
                .PredicateSelected(input =>
                {
                    if (input.Trim().Length == 0)
                    {
                        return (true, string.Empty);
                    }
                    var isvalid = true;
                    try
                    {
                        CultureInfo.GetCultureInfo(input, true);
                    }
                    catch (CultureNotFoundException)
                    {
                        isvalid = false;
                    }
                    if (!isvalid)
                    {
                        return (false, string.Format(null, FormatMessages.ValidationLanguageInvalidFormat, input));
                    }
                    return (true, string.Empty);
                })
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? fieldsJson.Value : result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptEditFieldFolderRepo(FieldsJson fieldsJson, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptEnterNewValue}: ";
            var result = PromptPlus.Controls
                .Input(message, ShowDescField(fieldsJson))
                .Default(fieldsJson.Value)
                .MaxLength(50)
                .SuggestionHandler(input => ["doc/adr"])
                .PredicateSelected(input =>
                {
                    if (input.Trim().Length == 0)
                    {
                        return (false, Resources.AdrPlus.ErrMsgNotEmpty);
                    }
                    try
                    {
                        // Validate that the path doesn't contain invalid characters
                        _ = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, input));
                    }
                    catch (ArgumentException)
                    {
                        return (false, string.Format(null, FormatMessages.ErrMsgFolderRepoMustBeRelativeFormat, input));
                    }
                    catch (NotSupportedException)
                    {
                        return (false, string.Format(null, FormatMessages.ErrMsgFolderRepoMustBeRelativeFormat, input));
                    }
                    return (true, string.Empty);
                })
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? fieldsJson.Value : result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptEditFielDateFormat(FieldsJson fieldsJson, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptEnterNewValue}: ";
            var result = PromptPlus.Controls
                .Input(message, ShowDescField(fieldsJson))
                .Default(fieldsJson.Value)
                .MaxLength(10)
                .SuggestionHandler(input => ["yyyy-MM-dd"])
                .PredicateSelected(input =>
                {
                    if (input.Trim().Length == 0)
                    {
                        return (true, string.Empty);
                    }
                    var isvalid = true;
                    try
                    {
                        var testDate = DateTime.UtcNow;
                        var formatted = testDate.ToString(input, CultureInfo.InvariantCulture);
                        if (!DateTime.TryParse(formatted, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                        { 
                            isvalid = false;
                        }
                    }
                    catch (FormatException)
                    {
                        isvalid = false;
                    }

                    if (!isvalid)
                    {
                        return (false, string.Format(null, FormatMessages.ValidationDateFormatInvalidFormat, input));
                    }

                    return (true, string.Empty);
                })
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? fieldsJson.Value : result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptEditFielOpenAdr(FieldsJson fieldsJson, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptEnterNewValue}: ";
            var result = PromptPlus.Controls
                .Input(message, ShowDescField(fieldsJson))
                .Default(fieldsJson.Value)
                .MaxLength(100)
                .SuggestionHandler(input => ["code \"{0}\"", "devenv /edit \"{0}\"", "rider \"{0}\""])
                .PredicateSelected(input =>
                {
                    if (input.Trim().Length == 0)
                    {
                        return (true, string.Empty);
                    }
                    if (!input.Contains("{0}", StringComparison.Ordinal))
                    {
                        return (false, Resources.AdrPlus.ErrMsgOpenAdrMustContainPlaceholder);
                    }
                    return (true, string.Empty);
                })
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? fieldsJson.Value : result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptEditFieldPrefix(FieldsJson fieldsJson, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptEnterNewValue}: ";
            var result = PromptPlus.Controls
                .Input(message, ShowDescField(fieldsJson))
                .Default(fieldsJson.Value)
                .InputToCase(CaseOptions.Uppercase)
                .MaxLength(5)
                .AcceptInput(input => char.IsAsciiLetter(input))
                .SuggestionHandler(input => [Resources.AdrPlus.DefaultPrefix])
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? fieldsJson.Value : result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptEditFieldYesNoChar(FieldsJson fieldsJson, IEnumerable<FieldsJson> fields, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptEnterNewValue}: ";
            var result = PromptPlus.Controls
                .Input(message, ShowDescField(fieldsJson))
                .Default(fieldsJson.Value)
                .InputToCase(CaseOptions.Uppercase)
                .MaxLength(1)
                .AcceptInput(input => char.IsAsciiLetter(input))
                .PredicateSelected(input =>
                {
                    if (input.Length == 0)
                    {
                        return (true, string.Empty);
                    }
                    var yesfield = fields.First(f => f.Name == AppConstants.FieldYesValue);
                    var nofield = fields.First(f => f.Name == AppConstants.FieldNoValue);
                    var isvalid = true;
                    if (fieldsJson.Name == AppConstants.FieldYesValue)
                    {
                        isvalid = input != nofield.Value;
                    }
                    if (fieldsJson.Name == AppConstants.FieldNoValue)
                    {
                        isvalid = input != yesfield.Value;
                    }
                    if (!isvalid)
                    {
                        return (false, Resources.AdrPlus.ErrMsgYesNoConflict);
                    }
                    return (true, string.Empty);
                })
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? fieldsJson.Value : result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, int Content) PromptEditFieldLenSeq(FieldsJson fieldsJson, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptChooseNewValue}: ";
            var result = PromptPlus.Controls
                .Slider(message, ShowDescField(fieldsJson))
                .Default(int.TryParse(fieldsJson.Value, out int intValue) ? intValue : 3)
                .Layout(SliderLayout.UpDown)
                .Step(1)
                .LargeStep(5)
                .Range(3, 5)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? 0 : (int)result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, int Content) PromptEditFieldRevision(FieldsJson fieldsJson, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptChooseNewValue}: ";
            var result = PromptPlus.Controls
                .Slider(message, ShowDescField(fieldsJson))
                .Default(int.TryParse(fieldsJson.Value, out int intValue) ? intValue : 0)
                .Layout(SliderLayout.UpDown)
                .Step(1)
                .LargeStep(3)
                .Range(0, 3)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? 0 : (int)result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, int Content) PromptEditFieldVersion(FieldsJson fieldsJson, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptChooseNewValue}: ";
            var result = PromptPlus.Controls
                .Slider(message, ShowDescField(fieldsJson))
                .Default(int.TryParse(fieldsJson.Value, out int intValue) ? intValue : 2)
                .Layout(SliderLayout.UpDown)
                .Step(1)
                .LargeStep(3)
                .Range(2, 3)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? 2 : (int)result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptEditFieldScopes(FieldsJson fieldsJson, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptEnterNewValue}: ";
            var result = PromptPlus.Controls
                .Input(message, ShowDescField(fieldsJson))
                .Default(fieldsJson.Value)
                .AcceptInput(input => char.IsAsciiLetter(input) || input == ';')
                .SuggestionHandler(input => [Resources.AdrPlus.DefaultScope])
                .PredicateSelected(input =>
                {
                    var scopes = input.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    var uniqueScopes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var scope in scopes)
                    {
                        if (!uniqueScopes.Add(scope))
                        {
                            return (false, Resources.AdrPlus.ErrMsgScopesDuplicateEntries);
                        }
                    }
                    return (true, string.Empty);
                })
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? fieldsJson.Value : result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptEditFieldskipdomain(FieldsJson fieldsJson, IEnumerable<FieldsJson> fields, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptSelectNewValues}: ";
            var result = PromptPlus.Controls
                .MultiSelect<string>(message, ShowDescField(fieldsJson))
                .Default(fieldsJson.Value.Split(';', StringSplitOptions.RemoveEmptyEntries))
                .AddItems(fields.First(f => f.Name == AppConstants.FieldScopes).Value.Split(';', StringSplitOptions.RemoveEmptyEntries))
                .PredicateSelected(input =>
                {
                    var scopes = input.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    var uniqueScopes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var scope in scopes)
                    {
                        if (!uniqueScopes.Add(scope))
                        {
                            return (false, Resources.AdrPlus.ErrMsgScopesDuplicateEntries);
                        }
                    }
                    return (true, string.Empty);
                })
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? fieldsJson.Value : string.Join(";", result.Content));
        }

        /// <inheritdoc/>
        public (bool IsAborted, int Content) PromptEditFieldLenScope(FieldsJson fieldsJson, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptChooseNewValue}: ";
            var result = PromptPlus.Controls
                .Slider(message, ShowDescField(fieldsJson))
                .Default(int.TryParse(fieldsJson.Value, out int intValue) ? intValue : 3)
                .Layout(SliderLayout.UpDown)
                .Step(1)
                .LargeStep(5)
                .Range(0, 5)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? 0 : (int)result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, bool Content) PromptEditFieldFolderByScope(FieldsJson fieldsJson, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptChooseNewValue}: ";
            var result = PromptPlus.Controls
                .Switch(message, ShowDescField(fieldsJson))
                .Default(bool.TryParse(fieldsJson.Value, out bool boolValue) && boolValue)
                .Run(cancellationToken);
            return (result.IsAborted, !result.IsAborted && (bool)result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptEditFieldCaseTransform(FieldsJson fieldsJson, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptChooseNewValue}: ";
            var enumlist = Enum.GetNames<CaseFormat>();
            var result = PromptPlus.Controls
                .Select<string>(message, ShowDescField(fieldsJson))
                .Default(fieldsJson.Value)
                .MaxWidth(10)
                .AddItems(enumlist)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? fieldsJson.Value : result.Content!);
        }

        public (bool IsAborted, string FilePathAdrTemplate) PromptConfigTemplateAdrSelect(string root, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.PromptSelectAdrTemplatePath}: ";
            var result = PromptPlus.Controls
                .FileSelect(message)
                .DefaultHistory()
                .SearchPattern("*.md")
                .EnabledHistory("AdrPlusAdrTemplatePathHistory")
                .Root(root)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? string.Empty : result.Content.FullPath);
        }

        /// <inheritdoc/>
        public (bool IsAborted, AdrFileNameComponents? info) PromptSelecLatesAdrs(AdrFileNameComponents[] adrFiles,AdrPlusRepoConfig adrPlusRepoConfig, Func<AdrFileNameComponents, (bool, string?)> validselect, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.NewVerChooseAdr}: ";
            var result = PromptPlus.Controls
                .Select<AdrFileNameComponents>(message, "")
                .TextSelector(info => $"{Path.GetFileName(info.FileName)} ")
                .PredicateSelected(validselect)
                .Filter(FilterMode.Contains)
                .ExtraInfo(info =>
                {
                    if (info.Header.StatusChange != AdrStatus.Unknown)
                    {
                        return adrPlusRepoConfig.StatusMapping[info.Header.StatusChange];
                    }
                    else if (info.Header.StatusUpdate != AdrStatus.Unknown)
                    {
                        return adrPlusRepoConfig.StatusMapping[info.Header.StatusUpdate];
                    }
                    return adrPlusRepoConfig.StatusMapping[info.Header.StatusCreate];
                })
                .AddItems(adrFiles.Where(x => x.IsValid))
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? null : result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, ItemMenuWizard? Content) PromptSelectMenu(bool IsHasconfig, ItemMenuWizard[] itemMenus,ItemMenuWizard defaultvalue,  CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.SelectAnOption}: ";
            var result = PromptPlus.Controls
                .Select<ItemMenuWizard>(message,"")
                .Default(defaultvalue)
                .Interaction(itemMenus, (item,opc) => 
                {
                    if (item.EnabledWhenNotConfigured || IsHasconfig)
                    {
                        opc.AddItem(item);
                    }
                    else
                    {
                        opc.AddItem(item, !IsHasconfig);
                    }
                })
                .TextSelector(item => item.Title)
                .ChangeDescription(field => field.Description)
                .EqualItems((a, b) => a.Id == b.Id)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? null : result.Content!);
        }



        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptEditFieldSeparator(FieldsJson fieldsJson, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptChooseNewValue}: ";
            var opcsep = new[] { "-", ".", "~" };
            var result = PromptPlus.Controls
                .Select<string>(message, ShowDescField(fieldsJson))
                .Default(fieldsJson.Value)
                .MaxWidth(1)
                .AddItems(opcsep)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? fieldsJson.Value : result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptEditFieldHeaderDisclaimer(FieldsJson fieldsJson, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptEnterNewValue}: ";
            var result = PromptPlus.Controls
                .Input(message, ShowDescField(fieldsJson))
                .Default(fieldsJson.Value)
                .MaxLength(200)
                .PredicateSelected(input => (input.Trim().Length > 0, Resources.AdrPlus.ErrMsgNotEmpty))
                .SuggestionHandler(input => [Resources.AdrPlus.DefaultHeaderDisclaimer])
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? fieldsJson.Value : result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptEditFieldHeaderStatus(FieldsJson fieldsJson, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptEnterNewValue}: ";
            var result = PromptPlus.Controls
                .Input(message, ShowDescField(fieldsJson))
                .Default(fieldsJson.Value)
                .MaxLength(30)
                .PredicateSelected(input => (input.Trim().Length > 0, Resources.AdrPlus.ErrMsgNotEmpty))
                .SuggestionHandler(input => [Resources.AdrPlus.DefaultTextStatus])
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? fieldsJson.Value : result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptEditFieldHeaderVersion(FieldsJson fieldsJson, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptEnterNewValue}: ";
            var result = PromptPlus.Controls
                .Input(message, ShowDescField(fieldsJson))
                .Default(fieldsJson.Value)
                .MaxLength(30)
                .PredicateSelected(input => (input.Trim().Length > 0, Resources.AdrPlus.ErrMsgNotEmpty))
                .SuggestionHandler(input => [Resources.AdrPlus.Version])
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? fieldsJson.Value : result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptEditFieldHeaderRevision(FieldsJson fieldsJson, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptEnterNewValue}: ";
            var result = PromptPlus.Controls
                .Input(message, ShowDescField(fieldsJson))
                .Default(fieldsJson.Value)
                .MaxLength(30)
                .PredicateSelected(input => (input.Trim().Length > 0, Resources.AdrPlus.ErrMsgNotEmpty))
                .SuggestionHandler(input => [Resources.AdrPlus.DefaultTextRevision])
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? fieldsJson.Value : result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptSelectLogicalDrive(string message, IFileSystemService fileSystemService, CancellationToken cancellationToken = default)
        {
            message = $"{message}: ";
            string[] drives = fileSystemService.GetDrives();
            var result = PromptPlus.Controls
                .Select<string>(message)
                .DefaultHistory()
                .EnabledHistory("AdrPlusRepoDriveSelection")
                .AddItems(drives)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? string.Empty : result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptEditTitleAdr(string defaultTitle, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.PromptEnterAdrTitle}: ";
            var result = PromptPlus.Controls
                .Input(message)
                .Default(defaultTitle)
                .MaxLength(100)
                .PredicateSelected(input => (input.Trim().Length > 0, Resources.AdrPlus.ErrMsgNotEmpty))
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? defaultTitle : result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptEditScopeAdr(string defaultScope, AdrPlusRepoConfig adrPlusRepo, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.PromptSelectAdrScope}: ";
            var result = PromptPlus.Controls
                .Select<string>(message)
                .Default(defaultScope)
                .AddItems(adrPlusRepo.GetScopes())
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? defaultScope : result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptEditDomainAdr(string defaultdomain, string[] sugestdomains, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.PromptEnterAdrDomain}: ";
            var result = PromptPlus.Controls
                .Input(message)
                .Default(defaultdomain)
                .PredicateSelected(input => (input.Trim().Length > 0, Resources.AdrPlus.ErrMsgNotEmpty))
                .SuggestionHandler((input) =>
                {
                    return string.IsNullOrWhiteSpace(input) ? sugestdomains : [.. sugestdomains.Where(x => x.Contains(input, StringComparison.OrdinalIgnoreCase)) ?? []];
                })
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? defaultdomain : result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string[] domains, Exception? Content) PromptGetArrayDomainsAdr(IFileSystemService fileSystemService, string path, AdrPlusConfig config, AdrPlusRepoConfig adrPlusRepo, CancellationToken cancellationToken = default)
        {
            var defarrdomain = Array.Empty<string>();
            var message = $"{Resources.AdrPlus.PromptReadingRegisteredDomains}: ";
            var resuldefarrdomain = PromptPlus.Controls
                .WaitCommand(message)
                .CommandHandler(() => defarrdomain = _adrServices.GetDomains(fileSystemService, Path.Combine(path, config.FolderRepo), adrPlusRepo).Result)
                .Spinner(SpinnersType.Ascii)
                .Run(cancellationToken);
            return (resuldefarrdomain.IsAborted, defarrdomain, resuldefarrdomain.IsAborted ? null : resuldefarrdomain.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptSelectFolderRepositoryAdr(bool checknitCmd, string root, IFileSystemService fileSystemService, IValidateJsonConfig validateJsonConfig, AdrPlusConfig repoConfig, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.PromptSelectAdrRepositoryPath}: ";
            var result = PromptPlus.Controls
                .FileSelect(message)
                .OnlyFolders()
                .DefaultHistory()
                .EnabledHistory("AdrPlusRepoPathHistory")
                .PredicateSelected(input =>
                {
                    if (!checknitCmd)
                    {
                        return (true, "");
                    }
                    var targetPath = Path.Combine(input.FullPath, repoConfig.FolderRepo, validateJsonConfig.GetFileNameRepoConfig());
                    if (!fileSystemService.FileExists(targetPath))
                    {
                        return (false, Resources.AdrPlus.ErrorInitCommandNotExecuted);
                    }
                    return (true, "");
                })
                .Root(root)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? string.Empty : result.Content.FullPath);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptEditFieldStatus(FieldsJson fieldsJson, CancellationToken cancellationToken = default)
        {
            var fieldName = fieldsJson.Name;
            var message = $"{Resources.AdrPlus.ConfigPromptChooseNewValue}: ";
            var result = PromptPlus.Controls
                .Input(message, ShowDescField(fieldsJson))
                .Default(fieldsJson.Value.ToPascalCase())
                .MaxLength(15)
                .SuggestionHandler(input =>
                {
                    var suggestions = new List<string>();
                    if (fieldName == AppConstants.FieldStatusNew)
                    {
                        suggestions.AddRange([Resources.AdrPlus.StatusNew]);
                    }
                    else if (fieldName == AppConstants.FieldStatusAccepted)
                    {
                        suggestions.AddRange([Resources.AdrPlus.StatusAcc]);
                    }
                    else if (fieldName == AppConstants.FieldStatusSuperseded)
                    {
                        suggestions.AddRange([Resources.AdrPlus.StatusSup]);
                    }
                    else if (fieldName == AppConstants.FieldStatusRejected)
                    {
                        suggestions.AddRange([Resources.AdrPlus.StatusRej]);
                    }
                    return [.. suggestions];
                })
                .AcceptInput(input => char.IsAsciiLetter(input))
                .PredicateSelected(input =>
                {
                    if (fieldName != AppConstants.FieldStatusRejected)
                    {
                        return (input.Length >= 3, Resources.AdrPlus.ConfigErrorMinThreeChars);
                    }
                    else
                    {
                        if (input.Length < 3 && input.Length != 0)
                        {
                            return (input.Length >= 3, Resources.AdrPlus.ConfigErrorMinThreeChars);
                        }
                    }
                    return (true, string.Empty);
                })
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? fieldsJson.Value : result.Content!);
        }

        /// <summary>
        /// Gets the description text for a field based on its name.
        /// </summary>
        /// <param name="field">The field metadata.</param>
        /// <returns>The localized description text for the field.</returns>
        private static string ShowDescField(FieldsJson field)
        {
            if (field.IsEndEdit)
            {
                return Resources.AdrPlus.ConfigActionSaveAndFinishDesc;
            }
            return field.Name switch
            {
                AppConstants.FieldLanguage=> Resources.AdrPlus.ConfigFieldDescLanguage,
                AppConstants.FieldFolderRepo => Resources.AdrPlus.ConfigFieldDescFolderRepo,
                AppConstants.FieldOpenAdr => Resources.AdrPlus.ConfigFieldDescOpenAdr,
                AppConstants.FieldYesValue => Resources.AdrPlus.ConfigFieldDescYesValue,
                AppConstants.FieldNoValue => Resources.AdrPlus.ConfigFieldDescNoValue,
                AppConstants.FieldPrefix => Resources.AdrPlus.ConfigFieldDescPrefix,
                AppConstants.FieldLenSeq => Resources.AdrPlus.ConfigFieldDescLenSeq,
                AppConstants.FieldLenVersion => Resources.AdrPlus.ConfigFieldDescLenVersion,
                AppConstants.FieldLenRevision => Resources.AdrPlus.ConfigFieldDescLenRevision,
                AppConstants.FieldScopes => Resources.AdrPlus.ConfigFieldDescScopes,
                AppConstants.FieldLenScope => Resources.AdrPlus.ConfigFieldDescLenScope,
                AppConstants.FieldSkipDomain => Resources.AdrPlus.ConfigFieldDescSkipDomain,
                AppConstants.FieldFolderByScope => Resources.AdrPlus.ConfigFieldDescFolderByScope,
                AppConstants.FieldCaseTransform => Resources.AdrPlus.ConfigFieldDescCaseTransform,
                AppConstants.FieldSeparator => Resources.AdrPlus.ConfigFieldDescSeparator,
                AppConstants.FieldStatusNew => Resources.AdrPlus.ConfigFieldDescStatusNew,
                AppConstants.FieldStatusAccepted => Resources.AdrPlus.ConfigFieldDescStatusAccepted,
                AppConstants.FieldStatusRejected => Resources.AdrPlus.ConfigFieldDescStatusRejected,
                AppConstants.FieldStatusSuperseded => Resources.AdrPlus.ConfigFieldDescStatusSuperseded,
                AppConstants.FieldHeaderDisclaimer => Resources.AdrPlus.ConfigFieldDescHeaderDisclaimer,
                AppConstants.FieldHeaderStatus => Resources.AdrPlus.FieldTitleHeaderStatus,
                AppConstants.FieldHeaderVersion => Resources.AdrPlus.FieldTitleHeaderVersion,
                AppConstants.FieldHeaderRevision => Resources.AdrPlus.FieldTitleHeaderRevision,
                _ => string.Empty,
            };
        }
    }
}
