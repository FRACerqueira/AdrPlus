// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.Formatting;
using Microsoft.Extensions.Configuration;
using PromptPlusLibrary;
using System.Collections.Frozen;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace AdrPlus.Infrastructure.UI
{
    /// <summary>
    /// Console writer implementation using PromptPlus library.
    /// </summary>
    internal sealed partial class PromptConsole(IConfiguration configuration, IFileSystemService fileSystemService, IValidateJsonConfig validate, IAdrServices adrServices) : IPromptConsole
    {
        /// <summary>
        /// Console color for help messages.
        /// </summary>
        private const string ColorHelp = "skyblue2";

            /// <summary>
        /// Console color for the welcome banner.
        /// </summary>
        private static Color ColorWelcomeBanner => Color.DarkOrange;

        /// <summary>
        /// Console color for error messages.
        /// </summary>
        private const string ColorError = "red";

        /// <summary>
        /// Console color for informational messages.
        /// </summary>
        private const string ColorInfo = "grey";

        /// <summary>
        /// Console color for command results.
        /// </summary>
        private const string ColorResult = "white";

        /// <summary>
        /// Console color for warning messages.
        /// </summary>
        private const string ColorWarning = "gold1";

        /// <summary>
        /// Console color for summary messages.
        /// </summary>  
        private const string ColorSummary = "navajowhite1";

        private readonly IAdrServices _adrServices = adrServices;
        private readonly IConfiguration _configuration = configuration;
        private readonly IFileSystemService _fileSystemService = fileSystemService;
        private readonly IValidateJsonConfig _validate = validate;

        /// <inheritdoc/>
        public async Task<bool> TryExecuteFistInstall(CancellationToken cancellationToken)
        {
            return await FistInstall(cancellationToken);
        }

        /// <inheritdoc/>
        public void ClearHistoryMigration()
        {
            PromptPlus.Controls.History("AdrPlusMigrationFields").Remove();
            PromptPlus.Controls.History("AdrPlusMigrationSampleFile").Remove();
            PromptPlus.Controls.History("AdrPlusMigrationPrefixPosition").Remove();
            PromptPlus.Controls.History("AdrPlusMigrationPrefixLength").Remove();
            PromptPlus.Controls.History("AdrPlusMigrationNumberPosition").Remove();
            PromptPlus.Controls.History("AdrPlusMigrationNumberLength").Remove();
            PromptPlus.Controls.History("AdrPlusMigrationVersionPosition").Remove();
            PromptPlus.Controls.History("AdrPlusMigrationVersionLength").Remove();
            PromptPlus.Controls.History("AdrPlusMigrationRevisionPosition").Remove();
            PromptPlus.Controls.History("AdrPlusMigrationRevisionLength").Remove();
            PromptPlus.Controls.History("AdrPlusMigrationTitlePosition").Remove();
        }

        public (bool IsAborted, string Content) PromptEditFieldBehaviorWithoutArgs(FieldsJson fieldsJson, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptChooseNewValue}: ";
            var enumlist = Enum.GetNames<BehaviorWithoutArg>();
            var result = PromptPlus.Controls
                .Select<string>(message, ShowDescField(fieldsJson))
                .Default(fieldsJson.Value)
                .MaxWidth(10)
                .AddItems(enumlist)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? fieldsJson.Value : result.Content!);
        }


        public (bool IsAborted, int Value) PromptSelectTitlePosition(string filename, int maxValue, int defaultValue, CancellationToken cancellationToken)
        {
            var result = PromptPlus.Controls.Slider($"{Resources.AdrPlus.PromptTitlePosition}: ")
                .ChangeDescription((item) =>
                {
                    var result = filename[(int)item..];
                    if (result.Length > 20)
                    {
                        result = result[..20] + "...";
                    }
                    return $"{Resources.AdrPlus.SampleResult}: {result}";
                })
                 .Range(0, maxValue)
                 .Default(defaultValue, true)
                 .Step(1)
                 .LargeStep(5)
                 .Layout(SliderLayout.UpDown)
                 .EnabledHistory("AdrPlusMigrationTitlePosition")
                 .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? 0 : (int)result.Content!.Value);
        }

        public (bool IsAborted, int Value) PromptSelectRevisionPosition(string filename, int maxValue, int defaultValue, CancellationToken cancellationToken)
        {
            var result = PromptPlus.Controls.Slider($"{Resources.AdrPlus.PromptRevisionPosition}: ")
                .ChangeDescription((item) =>
                {
                    var result = filename[(int)item..];
                    if (result.Length > 20)
                    {
                        result = result[..20] + "...";
                    }
                    return $"{Resources.AdrPlus.SampleResult}: {result}";
                })
                 .Range(0, maxValue)
                 .Default(defaultValue, true)
                 .Step(1)
                 .LargeStep(5)
                 .Layout(SliderLayout.UpDown)
                 .EnabledHistory("AdrPlusMigrationRevisionPosition")
                 .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? 0 : (int)result.Content!.Value);
        }

        public (bool IsAborted, int Value) PromptSelectRevisionLength(string filename, int position, int maxValue, int defaultValue, CancellationToken cancellationToken)
        {
            var result = PromptPlus.Controls.Slider($"{Resources.AdrPlus.PromptRevisionLength}: ")
               .ChangeDescription((item) =>
               {
                   var result = filename[position..][..(int)item];
                   return $"{Resources.AdrPlus.SampleResult}: {result}";
               })
               .Default(defaultValue, true)
               .EnabledHistory("AdrPlusMigrationRevisionLength")
               .Range(2, maxValue)
               .Step(1)
               .LargeStep(1)
               .Layout(SliderLayout.UpDown)
               .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? 0 : (int)result.Content!.Value);
        }

        public (bool IsAborted, int Value) PromptSelectVersionPosition(string filename, int maxValue, int defaultValue, CancellationToken cancellationToken)
        {
            var result = PromptPlus.Controls.Slider($"{Resources.AdrPlus.PromptVersionPosition}: ")
                .ChangeDescription((item) =>
                {
                    var result = filename[(int)item..];
                    if (result.Length > 20)
                    {
                        result = result[..20] + "...";
                    }
                    return $"{Resources.AdrPlus.SampleResult}: {result}";
                })
                 .Range(0, maxValue)
                 .Default(defaultValue, true)
                 .Step(1)
                 .LargeStep(5)
                 .Layout(SliderLayout.UpDown)
                 .EnabledHistory("AdrPlusMigrationVersionPosition")
                 .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? 0 : (int)result.Content!.Value);
        }

        public (bool IsAborted, int Value) PromptSelectVersionLength(string filename, int position, int maxValue, int defaultValue, CancellationToken cancellationToken)
        {
            var result = PromptPlus.Controls.Slider($"{Resources.AdrPlus.PromptVersionLength}: ")
               .ChangeDescription((item) =>
               {
                   var result = filename[position..][..(int)item];
                   return $"{Resources.AdrPlus.SampleResult}: {result}";
               })
               .Default(defaultValue, true)
               .EnabledHistory("AdrPlusMigrationVersionLength")
               .Range(2, maxValue)
               .Step(1)
               .LargeStep(1)
               .Layout(SliderLayout.UpDown)
               .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? 0 : (int)result.Content!.Value);
        }


        /// <inheritdoc/>
        public (bool IsAborted, int Value) PromptSelectNumberPosition(string filename, int maxValue, int defaultValue, CancellationToken cancellationToken)
        {
            var result = PromptPlus.Controls.Slider($"{Resources.AdrPlus.PromptNumberPosition}: ")
                .ChangeDescription((item) =>
                {
                    var result = filename[(int)item..];
                    if (result.Length > 20)
                    {
                        result = result[..20] + "...";
                    }
                    return $"{Resources.AdrPlus.SampleResult}: {result}";
                })
                 .Range(0, maxValue)
                 .Default(defaultValue, true)
                 .Step(1)
                 .LargeStep(5)
                 .Layout(SliderLayout.UpDown)
                 .EnabledHistory("AdrPlusMigrationNumberPosition")
                 .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? 0 : (int)result.Content!.Value);
        }


        /// <inheritdoc/>
        public (bool IsAborted, int Value) PromptSelectNumberLength(string filename, int position, int maxValue, int defaultValue, CancellationToken cancellationToken)
        {
            var result = PromptPlus.Controls.Slider($"{Resources.AdrPlus.PromptNumberLength}: ")
                .ChangeDescription((item) =>
                {
                    var result = filename[position..][..(int)item];
                    return $"{Resources.AdrPlus.SampleResult}: {result}";
                })
                .Default(defaultValue, true)
                .EnabledHistory("AdrPlusMigrationNumberLength")
                .Range(3, maxValue)
                .Step(1)
                .LargeStep(1)
                .Layout(SliderLayout.UpDown)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? 0 : (int)result.Content!.Value);
        }


        /// <inheritdoc/>
        public (bool IsAborted, string[] FieldsFromFileAdr) PromptFieldsFromFileAdr(CancellationToken cancellationToken)
        {
            var result = PromptPlus.Controls.MultiSelect<string>($"{Resources.AdrPlus.PromptFieldsMigrationTitle}: ")
                .AddItem(Resources.AdrPlus.Prefix)
                .AddItem(Resources.AdrPlus.Number, true, true)
                .AddItem(Resources.AdrPlus.Version)
                .AddItem(Resources.AdrPlus.Revision)
                .AddItem(Resources.AdrPlus.Title, true, true)
                .EnabledHistory("AdrPlusMigrationFields")
                .DefaultHistory()
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? [] : result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, int Value,string PrefixValue) PromptSelectPrefixLength(string filename, int position, int maxValue, int defaultValue, CancellationToken cancellationToken)
        {
           var prefixValue = string.Empty;
            var result = PromptPlus.Controls.Slider($"{Resources.AdrPlus.PromptPrefixLength}: ")
                .ChangeDescription((item) =>
                {
                    var result = filename[position..][..(int)item];
                    prefixValue = result;
                    return $"{Resources.AdrPlus.SampleResult}: {result}";
                })
                .Default(3, true)
                .EnabledHistory("AdrPlusMigrationPrefixLength")
                .Range(1, maxValue)
                .Step(1)
                .LargeStep(1)
                .Layout(SliderLayout.UpDown)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? 0 : (int)result.Content!.Value, prefixValue);
        }

        /// <inheritdoc/>
        public (bool IsAborted, int Value) PromptSelectPrefixPosition(string filename, int maxValue,int defaultValue, CancellationToken cancellationToken)
        {
            var result = PromptPlus.Controls.Slider($"{Resources.AdrPlus.PromptPrefixPosition}: ")
                .ChangeDescription((item) =>
                {
                    var result = filename[(int)item..];
                    if (result.Length > 20)
                    {
                        result = result[..20] + "...";
                    }
                    return $"{Resources.AdrPlus.SampleResult}: {result}";
                })
                 .Range(0, maxValue)
                 .Default(defaultValue, true)
                 .Step(1)
                 .LargeStep(5)
                 .Layout(SliderLayout.UpDown)
                 .EnabledHistory("AdrPlusMigrationPrefixPosition")
                 .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? 0 : (int)result.Content!.Value);
        }


        /// <inheritdoc/>
        public (bool IsAborted, string SampleFileMigration) PromptSampleFileMigration(CancellationToken cancellationToken)
        {
            var result = PromptPlus.Controls.Input($"{Resources.AdrPlus.PromptFileSampleMigration}: ")
                .MaxLength(100)
                .EnabledHistory("AdrPlusMigrationSampleFile")
                .Default("", true)
                .PredicateSelected((input) =>
                {
                    if (input.Length < 10)
                    {
                        return (false, string.Format(null, FormatMessages.ErrLenFileSampleMigration, 10));
                    }
                    return (true, string.Empty);
                })
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? string.Empty : result.Content!);
        }

        /// <inheritdoc/>
        public void FlushOutput()
        {
            Console.Out.Flush();
        }

        /// <inheritdoc/>
        public (int left, int top) PromptCursorPosition()
        { 
            return PromptPlus.Console.GetCursorPosition();
        }

        public void PromptMovePosition(int left, int top)
        { 
            PromptPlus.Console.SetCursorPosition(left, top);
        }

        /// <inheritdoc/>
        public bool PromptIsAbortedByCtrlC()
        { 
            return PromptPlus.AbortedByCtrlC;
        }

        /// <inheritdoc/>
        public void PromptEnabledEscToAbort(bool enabled)
        { 
            PromptPlus.Config.EnabledAbortKey = enabled;
        }

        /// <inheritdoc/>
        public bool PromptPressAnyKeyToContinue(string message, CancellationToken cancellationToken)
        {
            PromptPlus.Console.WriteLine("");
            PromptPlus.Controls.KeyPress(message)
                .Options(opt => opt.ShowTooltip(false))
                .Run(cancellationToken);
            return PromptPlus.AbortedByCtrlC;
        }

        /// <inheritdoc/>
        public (int left, int top) PromptGetCursorPosition()
        {
            return PromptPlus.Console.GetCursorPosition();
        }

        /// <inheritdoc/>
        public void PromptWriteWait(string message)
        {
            PromptPlus.Console.Write($"[{ColorWarning}]{message}[/]");
        }

        public void PromptClearWaitText((int left, int top) position)
        {
            PromptPlus.Console.ClearLine();
            PromptPlus.Console.SetCursorPosition(position.left, position.top);
        }

        /// <inheritdoc/>
        public void PromptWriteSummary(string message)
        {
            PromptPlus.Console.WriteLine($"[{ColorSummary}]{message}[/]");
        }


        /// <inheritdoc/>
        public void PromptWriteInfo(string message)
        {
            PromptPlus.Console.WriteLine($"[{ColorInfo}]{message}[/]");
        }

        /// <inheritdoc/>
        public void PromptWriteSuccess(string message)
        {
            PromptPlus.Console.WriteLine($"[{ColorResult}]{message}[/]");
        }

        /// <inheritdoc/>
        public void PromptWriteError(string message)
        {
            PromptPlus.Console.WriteLine($"[{ColorError}]{message}[/]");
        }

        /// <inheritdoc/>
        public void PromptWriteHelp(string helpText)
        {
            PromptPlus.Console.WriteLine($"[{ColorHelp}]{helpText}[/]");
        }

        /// <inheritdoc/>
        public void PromptWriteStartCommand(string text)
        {
            PromptPlus.Console.WriteLine(text, ColorWelcomeBanner);
            PromptPlus.Console.WriteLine("");
        }

        /// <inheritdoc/>
        public void PromptWriteFinishedCommand(string text)
        {
            PromptPlus.Console.WriteLine("");
            PromptPlus.Console.WriteLine(text, ColorWelcomeBanner);
        }

        /// <summary>
        /// Displays an error message to the console using a static method.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        public static void PromptShowError(string message)
        {
            PromptPlus.Console.WriteLine($"[{ColorError}]{message}[/]");
        }

        /// <inheritdoc/>
        public void PromptShowWellcome(string appVersion)
        {
            PromptPlus.Console.WriteLine($"[{ColorInfo}]{string.Format(null, FormatMessages.MsgWelcome, appVersion)}[/]");
            PromptPlus.Console.WriteLine("");
        }

        /// <inheritdoc/>
        public void PromptConfigure(AdrPlusConfig config)
        {
            PromptPlus.Config.DefaultCulture = new CultureInfo(config.Language);
            PromptPlus.Config.EnabledAbortKey = false;
            PromptPlus.Config.EnableMessageAbortCtrlC = false;
            PromptPlus.Config.HideAfterFinish = true;
            PromptPlus.Config.PageSize = 8;
        }

        public void PromptEnsureCulture(AdrPlusConfig config)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(config.Language);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(config.Language);
            CultureInfo.CurrentCulture = new CultureInfo(config.Language);
            CultureInfo.CurrentUICulture = new CultureInfo(config.Language);
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(config.Language);
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(config.Language);
            PromptPlus.Config.DefaultCulture = new CultureInfo(config.Language); 
        }

        /// <inheritdoc/>
        public void PromptShowBanner(string bannerText)
        {
           PromptPlus.Console.Clear();
           PromptPlus.Widgets
                .Banner(bannerText, ColorWelcomeBanner)
                .Border(BannerDashOptions.DoubleBorderDown)
                .Show();
        }

        /// <inheritdoc/>
        public (bool IsAborted, DateTime Content) PromptCalendar(string message, DateTime dateref, AdrPlusConfig config, CancellationToken cancellationToken = default)
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
                .AddItems(fields.Where(x => x.IsEnabled), false)
                .TextSelector(field => $"{GetTitleField(field.Name)} ")
                .ExtraInfo(field => field.IsEndEdit ? "" : field.Value)
                .ChangeDescription(field => ShowDescField(field))
                .EqualItems((a, b) => a.Name == b.Name)
                .MaxWidth(25)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? null : result.Content);
        }

        /// <summary>
        /// Gets the display title for a given configuration field name, returning a user-friendly title from resources if available, or the original field name if no title is defined. 
        /// </summary>
        /// <param name="name">
        /// The configuration field name for which to retrieve the display title. This should correspond to one of the defined configuration keys in AppConstants, such as "folderrepo", "dateformat", etc. 
        /// </param>
        /// <returns>The display title for the specified configuration field name.</returns>
        private static string GetTitleField(string name)
        {
            return TitleFields.TryGetValue(name.ToLowerInvariant(), out var title) ? title : name;
        }

        /// <summary>
        /// A frozen dictionary mapping configuration field names to their corresponding display titles, used for presenting user-friendly titles in the application's user interface when displaying configuration settings.
        /// Uses FrozenDictionary for optimal read performance.
        /// </summary>
        private static FrozenDictionary<string, string> TitleFields => new Dictionary<string, string>
        {
                { AppConstants.FieldLanguage, Resources.AdrPlus.FieldTitleLanguage },
                { AppConstants.FieldBehaviorWithoutArgs, Resources.AdrPlus.FieldTitleBehaviorWithoutArgs },
                { AppConstants.FieldOpenAdr, Resources.AdrPlus.FieldTitleOpenAdr },
                { AppConstants.FieldFolderAdr, Resources.AdrPlus.FieldTitleFolderRepo },
                { AppConstants.FieldPrefix, Resources.AdrPlus.FieldTitlePrefix },
                { AppConstants.FieldLenSeq, Resources.AdrPlus.FieldTitleLenSeq },
                { AppConstants.FieldLenVersion, Resources.AdrPlus.FieldTitleLenVersion },
                { AppConstants.FieldLenRevision, Resources.AdrPlus.FieldTitleLenRevision },
                { AppConstants.FieldLenScope, Resources.AdrPlus.FieldTitleLenScope },
                { AppConstants.FieldScopes, Resources.AdrPlus.FieldTitleScopes },
                { AppConstants.FieldFolderByScope, Resources.AdrPlus.FieldTitleFolderByScope },
                { AppConstants.FieldSkipDomain, Resources.AdrPlus.FieldTitleSkipDomain },
                { AppConstants.FieldSeparator, Resources.AdrPlus.FieldTitleSeparator },
                { AppConstants.FieldCaseTransform, Resources.AdrPlus.FieldTitleCaseTransform },
                { AppConstants.FieldStatusNew, Resources.AdrPlus.FieldTitleStatusNew },
                { AppConstants.FieldStatusAccepted, Resources.AdrPlus.FieldTitleStatusAccepted },
                { AppConstants.FieldStatusRejected, Resources.AdrPlus.FieldTitleStatusRejected },
                { AppConstants.FieldStatusSuperseded, Resources.AdrPlus.FieldTitleStatusSuperseded },
                { AppConstants.FieldHeaderDisclaimer, Resources.AdrPlus.ConfigFieldDescHeaderDisclaimer },
                { AppConstants.FieldHeaderTitleFile, Resources.AdrPlus.FieldTitleHeaderTitleFile },
                { AppConstants.FieldHeaderVersion, Resources.AdrPlus.FieldTitleHeaderVersion },
                { AppConstants.FieldHeaderRevision, Resources.AdrPlus.FieldTitleHeaderRevision },
                { AppConstants.FieldHeaderScope, Resources.AdrPlus.FieldTitleHeaderScope },
                { AppConstants.FieldHeaderDomain, Resources.AdrPlus.FieldTitleHeaderDomain },
                { AppConstants.FieldHeaderStatusCreated, Resources.AdrPlus.FieldTitleHeaderStatusCreated },
                { AppConstants.FieldHeaderStatusChanged, Resources.AdrPlus.FieldTitleHeaderStatusChanged },
                { AppConstants.FieldHeaderStatusSuperseded, Resources.AdrPlus.FieldTitleHeaderStatusSuperseded },
                { AppConstants.FieldHeaderTableFields, Resources.AdrPlus.FieldTitleHeaderTableFields },
                { AppConstants.FieldHeaderTableValues, Resources.AdrPlus.FieldTitleHeaderTableValues },
                { AppConstants.FieldHeaderMigrated, Resources.AdrPlus.FieldTitleHeaderMigrated },
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc/>
        public (bool IsAborted, FieldsJson? Content) PromptConfigJsonRepoSelect(FieldsJson defaultvalue, IEnumerable<FieldsJson> fields, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptSelectField}: ";
            var result = PromptPlus.Controls
                .Select<FieldsJson>(message, "")
                .Default(defaultvalue)
                .AddItem(new FieldsJson { Name = Resources.AdrPlus.ConfigActionSaveAndFinish, IsEndEdit = true })
                .Interaction(fields,(item,ctx) => 
                 {
                     ctx.AddItem(item, !item.IsEnabled);
                 })
                .AddItem(new FieldsJson { Name = Resources.AdrPlus.ConfigActionSaveAndFinish, IsEndEdit = true })
                .TextSelector(field => $"{GetTitleField(field.Name)} ")
                .ExtraInfo(field => field.IsEndEdit ? "" : field.Value)
                .ChangeDescription(field => ShowDescField(field))
                .EqualItems((a, b) => a.Name == b.Name)
                .MaxWidth(25)
                .HideTipGroup()
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
                        return (false, string.Format(null, FormatMessages.ValidationLanguageInvalid, input));
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
                .SuggestionHandler(input => [AppConstants.DefaultFolderAdr])
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
                        return (false, string.Format(null, FormatMessages.ErrFolderRepositoryMustBeRelativeFormat, input));
                    }
                    catch (NotSupportedException)
                    {
                        return (false, string.Format(null, FormatMessages.ErrFolderRepositoryMustBeRelativeFormat, input));
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
        public (bool IsAborted, int Content) PromptEditFieldLenSeq(FieldsJson fieldsJson, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptChooseNewValue}: ";
            var result = PromptPlus.Controls
                .Slider(message, ShowDescField(fieldsJson))
                .Default(int.TryParse(fieldsJson.Value, out int intValue) ? intValue : 3)
                .Layout(SliderLayout.UpDown)
                .Step(1)
                .LargeStep(1)
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
                .LargeStep(1)
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
                .LargeStep(1)
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
                .AcceptInput(input => char.IsAsciiLetter(input) || input == ';' || input == '*')
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
                .Default(int.TryParse(fieldsJson.Value, out int intValue) ? intValue : 1)
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
        public (bool IsAborted, bool Content) PromptEmptyTemplate(CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.PromptEmptyTemplate}: ";
            var result = PromptPlus.Controls
                .Switch(message, Resources.AdrPlus.HelpUsageEmptyAdr)
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
                .PredicateSelected(item =>
                    {
                        return (!item.IsFolder) && item.Name.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
                    })
                .EnabledHistory("AdrPlusAdrTemplatePathHistory")
                .Root(root)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? string.Empty : result.Content.FullPath);
        }

        /// <inheritdoc/>
        public (bool IsAborted, AdrFileNameComponents? info) PromptSelecAdrs(AdrFileNameComponents[] adrFiles,AdrPlusRepoConfig adrPlusRepoConfig, Func<AdrFileNameComponents, (bool, string?)> validselect, CancellationToken cancellationToken = default)
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
                        return Helper.GetResourceStatus(info.Header.StatusChange);
                    }
                    if (info.Header.StatusUpdate != AdrStatus.Unknown)
                    {
                        return Helper.GetResourceStatus(info.Header.StatusUpdate);
                    }
                    if (info.Header.StatusCreate != AdrStatus.Unknown)
                    {
                        return Helper.GetResourceStatus(info.Header.StatusCreate);
                    }
                    if (info.Header.IsMigrated)
                    {
                        return Resources.AdrPlus.Migrated;
                    }
                    return Helper.GetResourceStatus(AdrStatus.Unknown);
                })
                .AddItems(adrFiles.Where(x => x.IsValid))
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? null : result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Filename) PromptInputFileReport(CancellationToken cancellationToken)
        {
           var inputfilename = PromptPlus.Controls.Input($"{Resources.AdrPlus.PromptFileNameReport}: ")
                    .Default("AdrPlusReport")
                    .PredicateSelected((value) => string.IsNullOrWhiteSpace(value) ? (false, Resources.AdrPlus.ExceptionFilenameEmpty) : (true, string.Empty))
                    .EnabledHistory("AdrPlusExplorerReportFileName")
                    .Run(cancellationToken);
            return (inputfilename.IsAborted, inputfilename.IsAborted ? string.Empty : inputfilename.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, bool IsCreatingReport) PromptOptionShowOrCreateReport(CancellationToken cancellationToken)
        {
            var explorerreport = PromptPlus.Controls.Switch($"{Resources.AdrPlus.PromptShowOrCreateReport}: ")
                .OffValue($"{Resources.AdrPlus.ShowAdrs}")
                .OnValue($"{Resources.AdrPlus.CreateReport}")
                .EnabledHistory("AdrPlusExplorerShowOrReport")
                .Default(false, true)
                .Run(cancellationToken);
            return (explorerreport.IsAborted, !explorerreport.IsAborted && (bool)explorerreport.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string[] FieldsExplorer) PromptFieldsExplorer(CancellationToken cancellationToken)
        {
            string[] fieldsresources = Resources.AdrPlus.ListFieldReport.Split(',');
            var fields = PromptPlus.Controls.MultiSelect<string>($"{Resources.AdrPlus.PromptFieldsReport}: ")
                .Interaction(fieldsresources, (item, ctx) =>
                {
                    //1)File,
                    //2)Current Status,
                    //3)Folder,
                    //4)Format,
                    //5)Prefix,
                    //6)Version,
                    //7)Revision,
                    //8)Status Created,
                    //9)Status Updated,
                    //10)Scope,
                    //11)Domain
                    if (item.StartsWith("1)",false,CultureInfo.InvariantCulture) || item.StartsWith("2)",false  ,CultureInfo.InvariantCulture))
                    {
                        ctx.AddItem(item, true, true);
                    }
                    else
                    {
                        ctx.AddItem(item);
                    }
                })
                 .EnabledHistory("AdrPlusExplorerFields")
                 .DefaultHistory()
                 .TextSelector(item => item[2..])
                 .Run(cancellationToken);
            return (fields.IsAborted, fields.IsAborted ? [] : fields.Content!);
        }


        /// <inheritdoc/>
        public (bool IsAborted, string FileSelectd) PromptTableExplorer(AdrFileNameComponents[] foundfiles, string[] fields, string folderrepoadr, AdrPlusRepoConfig adrPlusRepoConfig)
        {
            var onstart = true;
            var table = PromptPlus.Controls.TableSelect<AdrFileNameComponents>($"{Resources.AdrPlus.FilesExplored}: ")
                .Interaction(foundfiles, (item, ctx) =>
                {
                    ctx.AddItem(item);
                    if (onstart)
                    {
                        onstart = false;
                        //1)File,
                        //2)Current Status,
                        //3)Folder,
                        //4)Format,
                        //5)Prefix,
                        //6)Version,
                        //7)Revision,
                        //8)Status Created,
                        //9)Status Updated,
                        //10)Scope,
                        //11)Domain
                        ctx.TextSelector((item) => Path.GetFileName(item.FileName));
                        ctx.AddColumn(Resources.AdrPlus.File, 40, (item) => 
                        {
                            return $"{Path.GetFileName(item.FileName)} ({item.Number})";
                        }, maxslidinglines: 3);
                        ctx.AddColumn(Resources.AdrPlus.CurrentStatus, 29, (item) => Helper.FmtStatus(item, adrPlusRepoConfig), maxslidinglines: 2);
                        if (fields.Any(x => x.StartsWith("3)",false,CultureInfo.InvariantCulture)))
                        {
                            ctx.AddColumn(Resources.AdrPlus.Folder, 20, (item) => Helper.FmtFolder(item, folderrepoadr), maxslidinglines: 2);
                        }
                        if (fields.Any(x => x.StartsWith("4)",false,CultureInfo.InvariantCulture)))
                        {
                            ctx.AddColumn(Resources.AdrPlus.Format, 20, (item) => Helper.FmtFormat(item), maxslidinglines: 2);
                        }
                        if (fields.Any(x => x.StartsWith("5)", false, CultureInfo.InvariantCulture)))
                        {
                            ctx.AddColumn(Resources.AdrPlus.Prefix, 5, (item) => item.Prefix, maxslidinglines: 2);
                        }
                        if (fields.Any(x => x.StartsWith("6)", false, CultureInfo.InvariantCulture)))
                        {
                            ctx.AddColumn(Resources.AdrPlus.Version, 5, (item) => item.Version.ToString(CultureInfo.InvariantCulture));
                        }
                        if (fields.Any(x => x.StartsWith("7)", false, CultureInfo.InvariantCulture)))
                        {
                            ctx.AddColumn(Resources.AdrPlus.Revision, 5, (item) => (item.Revision??0).ToString(CultureInfo.InvariantCulture));
                        }
                        if (fields.Any(x => x.StartsWith("8)", false, CultureInfo.InvariantCulture)))
                        {
                            ctx.AddColumn(Resources.AdrPlus.StatusCreated, 25, (item) => item.Header.DateCreate == null ? string.Empty : $"{item.Header.DateCreate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}:{item.Header.StatusCreate}");
                        }
                        if (fields.Any(x => x.StartsWith("9)", false, CultureInfo.InvariantCulture)))
                        {
                            ctx.AddColumn(Resources.AdrPlus.StatusUpdated, 25, (item) => item.Header.DateUpdate == null ? string.Empty : $"{item.Header.DateUpdate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}:{item.Header.StatusUpdate}");
                        }
                        if (fields.Any(x => x.StartsWith("10)", false, CultureInfo.InvariantCulture)))
                        {
                            ctx.AddColumn(Resources.AdrPlus.Scope, 20, (item) => item.Scope ?? string.Empty, maxslidinglines: 2);
                        }
                        if (fields.Any(x => x.StartsWith("11)", false, CultureInfo.InvariantCulture)))
                        {
                            ctx.AddColumn(Resources.AdrPlus.Domain, 20, (item) => item.Domain ?? string.Empty, maxslidinglines: 2);
                        }
                        ctx.Filter(FilterMode.Contains, true);
                        ctx.ChangeDescription((item, _, _) =>
                        {
                            return Path.GetDirectoryName(item.FileName) ?? string.Empty;
                        });
                    }
                })
                .Run();
                return (table.IsAborted, table.IsAborted ? string.Empty : table.Content.FileName);
        }

        /// <inheritdoc/>
        public (bool IsAborted, ItemMenuWizard? Content) PromptSelectMenu(bool IsHasconfig, ItemMenuWizard[] itemMenus,ItemMenuWizard defaultvalue,  CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.SelectAnOption}: ";
            var result = PromptPlus.Controls
                .Select<ItemMenuWizard>(message,"")
                .Default(defaultvalue)
                .EnabledHistory("AdrPlusMainMenuWizardSelection")
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
            var opcsep = new[] { "-", "_", "." };
            var result = PromptPlus.Controls
                .Select<string>(message, ShowDescField(fieldsJson))
                .Default(fieldsJson.Value)
                .MaxWidth(1)
                .AddItems(opcsep)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? fieldsJson.Value : result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptEditFieldHeaderText(FieldsJson fieldsJson,int maxlength,string sugestion, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptEnterNewValue}: ";
            var result = PromptPlus.Controls
                .Input(message, ShowDescField(fieldsJson))
                .Default(fieldsJson.Value)
                .MaxLength(maxlength)
                .PredicateSelected(input => (input.Trim().Length > 0, Resources.AdrPlus.ErrMsgNotEmpty))
                .SuggestionHandler(input => [sugestion])
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? fieldsJson.Value : result.Content!);
        }

        public (bool IsAborted, int CountSelected) PromptShowAdrsMigrations(AdrFileNameComponents[] adrs, AdrPlusRepoConfig adrPlusRepo, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.PromptAdrToMigrate}: ";
            var result = PromptPlus.Controls.MultiSelect<AdrFileNameComponents>(message, Resources.AdrPlus.ViewOnlyPrompt)
                .TextSelector(x => $"{Path.GetFileName(x.FileName)} ")
                .OnlyView()
                .Filter(FilterMode.Contains)
                .AddItems(adrs)
                .ExtraInfo(x => 
                 {
                     if (!x.IsValid)
                     {
                         return Resources.AdrPlus.MsgUnknownStructure;
                     }
                     else if (x.Header.IsMigrated)
                     {
                         if (x.Header.IsValid)
                         {
                             return Resources.AdrPlus.Migrated;
                         }
                         else
                         {
                             return Resources.AdrPlus.InvalidFormatHeader;
                         }
                     }
                     else if (x.Header.StatusCreate != AdrStatus.Unknown)
                     {
                         if (x.Header.IsValid)
                         {
                             return Resources.AdrPlus.AdrPlusFormat;
                         }
                         else
                         {
                             return Resources.AdrPlus.InvalidFormatHeader;
                         }
                     }
                     else if (x.Header.StatusCreate == AdrStatus.Unknown && !x.Header.IsMigrated && !x.Header.IsValid)
                     {
                         return Resources.AdrPlus.ReadyToMigrate;
                     }
                     else
                     {
                         return Resources.AdrPlus.MsgUnknownStructure;
                     }
                 })
                .PredicateSelected(x => false)
                .Default(adrs.Where(x => x.IsValid && !x.Header.IsValid && !x.Header.IsMigrated), false)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? 0 : result.Content!.Length);
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
        public (bool IsAborted, string[] domains, Exception? Content) PromptGetArrayDomainsAdr(IFileSystemService fileSystemService, string path, AdrPlusRepoConfig adrPlusRepo, CancellationToken cancellationToken = default)
        {
            var defarrdomain = Array.Empty<string>();
            var message = $"{Resources.AdrPlus.PromptReadingRegisteredDomains}: ";
            var resuldefarrdomain = PromptPlus.Controls
                .WaitCommand(message)
                .CommandHandler(() => defarrdomain = _adrServices.GetDomains(fileSystemService, path, adrPlusRepo).Result)
                .Spinner(SpinnersType.Ascii)
                .Run(cancellationToken);
            return (resuldefarrdomain.IsAborted, defarrdomain, resuldefarrdomain.IsAborted ? null : resuldefarrdomain.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptSelectFolderRepositoryAdr(string root, IFileSystemService fileSystemService, IValidateJsonConfig validateJsonConfig, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.PromptSelectAdrRepositoryAdr}: ";
            var result = PromptPlus.Controls
                .FileSelect(message)
                .OnlyFolders()
                .DefaultHistory()
                .EnabledHistory("AdrPlusRepoPathHistory")
                .PredicateSelected(input =>
                {
                    var targetconfigPath = Path.Combine(input.FullPath,validateJsonConfig.GetFileNameRepoConfig());
                    if (!fileSystemService.FileExists(targetconfigPath))
                    {
                        return (false, string.Format(null, FormatMessages.ErrFileNotFound, targetconfigPath));
                    }
                    return (true, "");
                })
                .Root(root)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? string.Empty : result.Content.FullPath);
        }


        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptSelectFolderPath(string message, bool checknitCmd, string root, IFileSystemService fileSystemService, IValidateJsonConfig validateJsonConfig, CancellationToken cancellationToken = default)
        {
            var pronptmessage = $"{message}: ";
            var result = PromptPlus.Controls
                .FileSelect(pronptmessage)
                .OnlyFolders()
                .DefaultHistory()
                .EnabledHistory($"AdrPlusSelectFolderPath_{BitConverter.ToString(SHA256.HashData(Encoding.UTF8.GetBytes(message)))}")
                .PredicateSelected(input =>
                {
                    if (!checknitCmd)
                    {
                        return (true, "");
                    }
                    var targetPath = Path.Combine(input.FullPath, validateJsonConfig.GetFileNameRepoConfig());
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
                AppConstants.FieldLanguage => Resources.AdrPlus.ConfigFieldDescLanguage,
                AppConstants.FieldBehaviorWithoutArgs => Resources.AdrPlus.ConfigFieldDescBehaviorWithoutArgs,
                AppConstants.FieldFolderAdr => Resources.AdrPlus.ConfigFieldDescFolderRepo,
                AppConstants.FieldMigrationPattern => Resources.AdrPlus.ConfigFieldDescMigrationPattern,
                AppConstants.FieldOpenAdr => Resources.AdrPlus.ConfigFieldDescOpenAdr,
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
                AppConstants.FieldHeaderDisclaimer => Resources.AdrPlus.FieldTitleHeaderDisclaimer,
                AppConstants.FieldHeaderTitleFile => Resources.AdrPlus.FieldTitleHeaderTitleFile,
                AppConstants.FieldHeaderVersion => Resources.AdrPlus.FieldTitleHeaderVersion,
                AppConstants.FieldHeaderRevision => Resources.AdrPlus.FieldTitleHeaderRevision,
                AppConstants.FieldHeaderScope => Resources.AdrPlus.FieldTitleHeaderScope,
                AppConstants.FieldHeaderDomain => Resources.AdrPlus.FieldTitleHeaderDomain,
                AppConstants.FieldHeaderStatusCreated => Resources.AdrPlus.FieldTitleHeaderStatusCreated,
                AppConstants.FieldHeaderStatusChanged => Resources.AdrPlus.FieldTitleHeaderStatusChanged,
                AppConstants.FieldHeaderStatusSuperseded => Resources.AdrPlus.FieldTitleHeaderStatusSuperseded,
                AppConstants.FieldHeaderTableFields => Resources.AdrPlus.FieldTitleHeaderTableFields,
                AppConstants.FieldHeaderTableValues => Resources.AdrPlus.FieldTitleHeaderTableValues,
                AppConstants.FieldHeaderMigrated => Resources.AdrPlus.FieldTitleHeaderMigrated,
                _ => string.Empty,
            };
        }

        private static string TextForRepoActions(RepoActions actions)
        {
            return actions switch
            {
                RepoActions.Template => Resources.AdrPlus.Template,
                RepoActions.Version => Resources.AdrPlus.Version,
                RepoActions.Revision => Resources.AdrPlus.Revision,
                RepoActions.Scope => Resources.AdrPlus.Scope,
                _ => actions.ToString(),
            };
        }
    }
}
