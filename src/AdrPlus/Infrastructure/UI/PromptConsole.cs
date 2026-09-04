// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.Formatting;
using ConsolePlusLibrary;
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
    internal sealed partial class PromptConsole(
        IConfiguration configuration, 
        IFileSystemService fileSystemService, 
        IValidateConfig validate, 
        IAdrServices adrServices) :
        IConsoleWriter,
        IConfigPrompts,
        IMigratePrompts,
        INewAdrPrompts,
        IExplorePrompts,
        IWizardMenuPrompts
    {
        private const string ColorHelp = "Skyblue";

        private static Color ColorWelcomeBanner => Color.Darkorange;

        private const string ColorError = "Red";

        private const string ColorInfo = "Grey";

        private const string ColorResult = "White";

        private const string ColorWarning = "Gold";

        private const string ColorSummary = "Navajowhite";

        private readonly IAdrServices _adrServices = adrServices;
        private readonly IConfiguration _configuration = configuration;
        private readonly IFileSystemService _fileSystemService = fileSystemService;
        private readonly IValidateConfig _validate = validate;

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
            var message = $"{Resources.AdrPlus.ConfigPromptChooseNewValue}";
            var enumlist = Enum.GetNames<BehaviorWithoutArg>();
            var result = PromptPlus.Controls
                .Select<string>(message, ShowDescField(fieldsJson))
                .Default(fieldsJson.Value)
                .AddItems(enumlist)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? fieldsJson.Value : result.Content!);
        }


        public (bool IsAborted, int Value) PromptSelectTitlePosition(string filename, int maxValue, int defaultValue, CancellationToken cancellationToken)
        {
            var result = PromptPlus.Controls.Slider($"{Resources.AdrPlus.PromptTitlePosition}")
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
                 .EnableHistory("AdrPlusMigrationTitlePosition")
                 .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? 0 : (int)result.Content!.Value);
        }

        public (bool IsAborted, int Value) PromptSelectRevisionPosition(string filename, int maxValue, int defaultValue, CancellationToken cancellationToken)
        {
            var result = PromptPlus.Controls.Slider($"{Resources.AdrPlus.PromptRevisionPosition}")
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
                 .EnableHistory("AdrPlusMigrationRevisionPosition")
                 .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? 0 : (int)result.Content!.Value);
        }

        public (bool IsAborted, int Value) PromptSelectRevisionLength(string filename, int position, int maxValue, int defaultValue, CancellationToken cancellationToken)
        {
            var result = PromptPlus.Controls.Slider($"{Resources.AdrPlus.PromptRevisionLength}")
               .ChangeDescription((item) =>
               {
                   var result = filename[position..][..(int)item];
                   return $"{Resources.AdrPlus.SampleResult}: {result}";
               })
               .Default(defaultValue, true)
               .EnableHistory("AdrPlusMigrationRevisionLength")
               .Range(2, maxValue)
               .Step(1)
               .LargeStep(1)
               .Layout(SliderLayout.UpDown)
               .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? 0 : (int)result.Content!.Value);
        }

        public (bool IsAborted, int Value) PromptSelectVersionPosition(string filename, int maxValue, int defaultValue, CancellationToken cancellationToken)
        {
            var result = PromptPlus.Controls.Slider($"{Resources.AdrPlus.PromptVersionPosition}")
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
                 .EnableHistory("AdrPlusMigrationVersionPosition")
                 .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? 0 : (int)result.Content!.Value);
        }

        public (bool IsAborted, int Value) PromptSelectVersionLength(string filename, int position, int maxValue, int defaultValue, CancellationToken cancellationToken)
        {
            var result = PromptPlus.Controls.Slider($"{Resources.AdrPlus.PromptVersionLength}")
               .ChangeDescription((item) =>
               {
                   var result = filename[position..][..(int)item];
                   return $"{Resources.AdrPlus.SampleResult}: {result}";
               })
               .Default(defaultValue, true)
               .EnableHistory("AdrPlusMigrationVersionLength")
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
            var result = PromptPlus.Controls.Slider($"{Resources.AdrPlus.PromptNumberPosition}")
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
                 .EnableHistory("AdrPlusMigrationNumberPosition")
                 .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? 0 : (int)result.Content!.Value);
        }


        /// <inheritdoc/>
        public (bool IsAborted, int Value) PromptSelectNumberLength(string filename, int position, int maxValue, int defaultValue, CancellationToken cancellationToken)
        {
            var result = PromptPlus.Controls.Slider($"{Resources.AdrPlus.PromptNumberLength}")
                .ChangeDescription((item) =>
                {
                    var result = filename[position..][..(int)item];
                    return $"{Resources.AdrPlus.SampleResult}: {result}";
                })
                .Default(defaultValue, true)
                .EnableHistory("AdrPlusMigrationNumberLength")
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
            var result = PromptPlus.Controls.MultiSelect<string>($"{Resources.AdrPlus.PromptFieldsMigrationTitle}")
                .AddItem(Resources.AdrPlus.Prefix)
                .AddItem(Resources.AdrPlus.Number, true, true)
                .AddItem(Resources.AdrPlus.Version)
                .AddItem(Resources.AdrPlus.Revision)
                .AddItem(Resources.AdrPlus.Title, true, true)
                .EnableHistory("AdrPlusMigrationFields")
                .UseDefaultHistory()
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? [] : result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, int Value,string PrefixValue) PromptSelectPrefixLength(string filename, int position, int maxValue, int defaultValue, CancellationToken cancellationToken)
        {
           var prefixValue = string.Empty;
            var result = PromptPlus.Controls.Slider($"{Resources.AdrPlus.PromptPrefixLength}")
                .ChangeDescription((item) =>
                {
                    var result = filename[position..][..(int)item];
                    prefixValue = result;
                    return $"{Resources.AdrPlus.SampleResult}: {result}";
                })
                .Default(3, true)
                .EnableHistory("AdrPlusMigrationPrefixLength")
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
            var result = PromptPlus.Controls.Slider($"{Resources.AdrPlus.PromptPrefixPosition}")
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
                 .EnableHistory("AdrPlusMigrationPrefixPosition")
                 .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? 0 : (int)result.Content!.Value);
        }


        /// <inheritdoc/>
        public (bool IsAborted, string SampleFileMigration) PromptSampleFileMigration(CancellationToken cancellationToken)
        {
            var result = PromptPlus.Controls.Input($"{Resources.AdrPlus.PromptFileSampleMigration}")
                .MaxLength(100)
                .EnableHistory("AdrPlusMigrationSampleFile")
                .Default("", true)
                .PredicateValid((input) =>
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

        /// <inheritdoc/>
        public void PromptClearRegionFromTop(int top)
        {
            var (_, bottom) = PromptPlus.Console.GetCursorPosition();
            for (var row = top; row <= bottom; row++)
            {
                PromptPlus.Console.ClearLine(row);
            }
            PromptPlus.Console.SetCursorPosition(0, top);
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
                .Options(opt => opt.ShowTooltip(false).SufixAfterPrompt(""))
                .Run(cancellationToken);
            return cancellationToken.IsCancellationRequested;
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
            PromptPlus.Console.WriteLine($"[{ColorError}]{message}[/]",true);
        }

        /// <inheritdoc/>
        public void PromptWriteHelp(string helpText)
        {
            PromptPlus.Console.WriteLine($"[{ColorHelp}]{helpText}[/]");
        }

        /// <inheritdoc/>
        public void PromptWriteStartCommand(string text)
        {
            // Color.Darkorange has no equivalent System.ConsoleColor member, so the
            // WriteLine(text, Color) overload throws under redirected/piped output
            // (no ANSI, falls back to legacy console colors). Only drop the color
            // when there's no real console; keep the original styling otherwise.
            if (Console.IsOutputRedirected)
            {
                PromptPlus.Console.WriteLine(text);
            }
            else
            {
                PromptPlus.Console.WriteLine(text, ColorWelcomeBanner);
            }
            PromptPlus.Console.WriteLine("");
        }

        /// <inheritdoc/>
        public void PromptWriteFinishedCommand(string text)
        {
            PromptPlus.Console.WriteLine("",true);
            if (Console.IsOutputRedirected)
            {
                PromptPlus.Console.WriteLine(text);
            }
            else
            {
                PromptPlus.Console.WriteLine(text, ColorWelcomeBanner, true);
            }
        }

        /// <summary>
        /// Displays an error message to the console.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        public static void PromptShowError(string message)
        {
            PromptPlus.Console.WriteLine($"[{ColorError}]{message}[/]",true);
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
            PromptPlus.Config.ShowMessageAbortKey = false;
            PromptPlus.Config.HideAfterFinish = true;
            PromptPlus.Config.HideOnAbort = true;
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
           if (Console.IsOutputRedirected)
           {
               // Console.Clear() and the banner widget require a real console buffer
               // (window size, cursor control). Under redirected/piped output - CI,
               // scripts, or an automation agent driving this CLI non-interactively -
               // there is no such buffer, and both throw "The handle is invalid".
               return;
           }
           PromptPlus.Console.Clear();
           PromptPlus.Widgets.Banner(bannerText, ColorWelcomeBanner, DashOptions.DoubleBorderUpDown);
        }

        /// <inheritdoc/>
        public void PromptWarnMissingActivePlugins(IReadOnlyList<string> missingPluginNames)
        {
            if (missingPluginNames.Count > 0)
            {
                PromptWriteInfo(string.Format(null, FormatMessages.PluginsActiveMissing, string.Join(", ", missingPluginNames)));
            }
        }

        /// <inheritdoc/>
        public (bool IsAborted, DateTime Content) PromptCalendar(string message, DateTime dateref, AdrPlusConfig config, DateTime minValue, DateTime maxValue, CancellationToken cancellationToken = default)
        {
            message =$"{message}";
            var result = PromptPlus.Controls
                .Calendar(message)
                .Culture(config.Language)
                .Default(dateref)
                .Range(minValue, maxValue)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? dateref : result.Content!.Value);
        }

        /// <inheritdoc/>
        public (bool IsAborted, bool ConfirmYes) PromptConfirm(string message, CancellationToken cancellationToken = default)
        {
            var result = PromptPlus.Controls
                .Confirm(message)
                .Run(cancellationToken);
            return (result.IsAborted, !result.IsAborted && char.ToUpperInvariant(result.Content!.Value.KeyChar) == char.ToUpperInvariant(PromptPlus.Config.YesChar));
        }

        /// <inheritdoc/>
        public (bool IsAborted, FieldsJson? Content) PromptConfigJsonAppSelect(FieldsJson defaultvalue, IEnumerable<FieldsJson> fields, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptSelectField}";
            var result = PromptPlus.Controls
                .Select<FieldsJson>(message, "")
                .Default(defaultvalue)
                .AddItem(new FieldsJson { Name = Resources.AdrPlus.ConfigActionSaveAndFinish, IsEndEdit = true })
                .AddItems(fields.Where(x => x.IsEnabled), false)
                .TextSelector(field => $"{GetTitleField(field.Name)} ")
                .ExtraInfo(field => field.IsEndEdit ? "" : field.Value)
                .ChangeDescription(field => ShowDescField(field))
                .DefaultMatchBy((a, b) => a.Name == b.Name)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? null : result.Content);
        }

        /// <summary>
        /// Gets the display title for a given configuration field name, returning a user-friendly title from resources if available, or the original field name if no title is defined.
        /// </summary>
        /// <param name="name">The configuration field name (one of the <c>AppConstants.Field*</c> keys, e.g. <c>"folderadr"</c>).</param>
        /// <returns>The display title for the specified configuration field name.</returns>
        private static string GetTitleField(string name)
        {
            return TitleFields.TryGetValue(name.ToLowerInvariant(), out var title) ? title : name;
        }

        /// <summary>
        /// Maps configuration field names to their display titles.
        /// </summary>
        private static FrozenDictionary<string, string> TitleFields => new Dictionary<string, string>
        {
                { AppConstants.FieldLanguage, Resources.AdrPlus.FieldTitleLanguage },
                { AppConstants.FieldWithoutArgs, Resources.AdrPlus.FieldTitleBehaviorWithoutArgs },
                { AppConstants.FieldOpenAdr, Resources.AdrPlus.FieldTitleOpenAdr },
                { AppConstants.FieldFolderAdr, Resources.AdrPlus.FieldTitleFolderRepo },
                { AppConstants.FieldPrefix, Resources.AdrPlus.FieldTitlePrefix },
                { AppConstants.FieldLenSeq, Resources.AdrPlus.FieldTitleLenSeq },
                { AppConstants.FieldLenVersion, Resources.AdrPlus.FieldTitleLenVersion },
                { AppConstants.FieldLenRevision, Resources.AdrPlus.FieldTitleLenRevision },
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
            var message = $"{Resources.AdrPlus.ConfigPromptSelectField}";
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
                .DefaultMatchBy((a, b) => a.Name == b.Name)
                .HideTipGroup()
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? null : result.Content);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptEditFieldLanguage(FieldsJson fieldsJson, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptEnterNewValue}";
            var result = PromptPlus.Controls
                .Input(message, ShowDescField(fieldsJson))
                .Default(fieldsJson.Value)
                .MaxLength(5)
                .AcceptInput(input => char.IsAsciiLetter(input) || input == '-')
                .SuggestionHandler(input => ["en-us", "pt-br", "de-de", "es-es", "fr-fr", "it-it", "ja-jp", "ko-kr", "nl-be", "ru-ru", "zh-cn"])
                .PredicateValid(input =>
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
            var message = $"{Resources.AdrPlus.ConfigPromptEnterNewValue}";
            var result = PromptPlus.Controls
                .Input(message, ShowDescField(fieldsJson))
                .Default(fieldsJson.Value)
                .MaxLength(50)
                .SuggestionHandler(input => [AppConstants.DefaultFolderAdr])
                .PredicateValid(input =>
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
            var message = $"{Resources.AdrPlus.ConfigPromptEnterNewValue}";
            var result = PromptPlus.Controls
                .Input(message, ShowDescField(fieldsJson))
                .Default(fieldsJson.Value)
                .MaxLength(100)
                .SuggestionHandler(input => ["code \"{0}\"", "devenv /edit \"{0}\"", "rider \"{0}\""])
                .PredicateValid(input =>
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
            var message = $"{Resources.AdrPlus.ConfigPromptEnterNewValue}";
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
            var message = $"{Resources.AdrPlus.ConfigPromptChooseNewValue}";
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
            var message = $"{Resources.AdrPlus.ConfigPromptChooseNewValue}";
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
            var message = $"{Resources.AdrPlus.ConfigPromptChooseNewValue}";
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
        public (bool IsAborted, bool Content) PromptEmptyTemplate(CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.PromptEmptyTemplate}";
            var result = PromptPlus.Controls
                .Switch(message, Resources.AdrPlus.HelpUsageEmptyAdr)
                .Run(cancellationToken);
            return (result.IsAborted, !result.IsAborted && (bool)result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, SyncWizardMode Mode) PromptSelectSyncMode(CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.WizardSyncModePrompt}";
            var result = PromptPlus.Controls
                .Select<string>(message, Resources.AdrPlus.WizardSyncModeDescription)
                .AddItems([Resources.AdrPlus.WizardSyncModeBack, Resources.AdrPlus.WizardSyncModeDefault, Resources.AdrPlus.WizardSyncModeBackfill])
                .Default(Resources.AdrPlus.WizardSyncModeDefault)
                .Run(cancellationToken);
            if (result.IsAborted)
            {
                return (true, SyncWizardMode.Default);
            }
            var mode = result.Content == Resources.AdrPlus.WizardSyncModeBackfill ? SyncWizardMode.Backfill
                : result.Content == Resources.AdrPlus.WizardSyncModeBack ? SyncWizardMode.Back
                : SyncWizardMode.Default;
            return (false, mode);
        }

        /// <inheritdoc/>
        public (bool IsAborted, PluginsWizardMode Mode) PromptSelectPluginsMode(CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.WizardPluginsModePrompt}";
            var result = PromptPlus.Controls
                .Select<string>(message, Resources.AdrPlus.WizardPluginsModeDescription)
                .AddItems([Resources.AdrPlus.WizardPluginsModeBack, Resources.AdrPlus.WizardPluginsModeInstall, Resources.AdrPlus.WizardPluginsModeList, Resources.AdrPlus.WizardPluginsModeListHost, Resources.AdrPlus.WizardPluginsModeValidate, Resources.AdrPlus.WizardPluginsModeManage, Resources.AdrPlus.WizardPluginsModeUninstall])
                .Default(Resources.AdrPlus.WizardPluginsModeList)
                .Run(cancellationToken);
            if (result.IsAborted)
            {
                return (true, PluginsWizardMode.List);
            }
            var mode = result.Content == Resources.AdrPlus.WizardPluginsModeValidate ? PluginsWizardMode.Validate
                : result.Content == Resources.AdrPlus.WizardPluginsModeManage ? PluginsWizardMode.Manage
                : result.Content == Resources.AdrPlus.WizardPluginsModeInstall ? PluginsWizardMode.Install
                : result.Content == Resources.AdrPlus.WizardPluginsModeUninstall ? PluginsWizardMode.Uninstall
                : result.Content == Resources.AdrPlus.WizardPluginsModeListHost ? PluginsWizardMode.ListHost
                : result.Content == Resources.AdrPlus.WizardPluginsModeBack ? PluginsWizardMode.Back
                : PluginsWizardMode.List;
            return (false, mode);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string ZipPath) PromptInputPluginZipPath(string root, CancellationToken cancellationToken = default)
        {
            var result = PromptPlus.Controls.File($"{Resources.AdrPlus.PromptPluginZipPath}: ")
                .SelectFilesOnly()
                .SearchPattern("*.zip")
                .EnableHistory("AdrPlusPluginsInstallZipPath")
                .Root(root)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? string.Empty : result.Content!.FullPath);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string[] SelectedNames) PromptSelectPluginsToUninstall(IReadOnlyList<string> installedNames, CancellationToken cancellationToken = default)
        {
            var result = PromptPlus.Controls.MultiSelect<string>($"{Resources.AdrPlus.PromptSelectPluginToUninstall}: ")
                .TextSelector(name => name)
                .Filter(FilterMode.Contains)
                .AddItems(installedNames)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? [] : [.. result.Content!]);
        }

        /// <inheritdoc/>
        public bool PromptShowPluginsListTable(IReadOnlyList<(string Status, string Name, string Version, string Events, string Allowlist, int Pending)> rows, CancellationToken cancellationToken = default)
        {
            var result = PromptPlus.Controls
                .TableSelect<(string Status, string Name, string Version, string Events, string Allowlist, int Pending)>(Resources.AdrPlus.WizardPluginsListTableTitle, string.Empty)
                .AddColumn(Resources.AdrPlus.TableColumnStatus, r => r.Status)
                .AddColumn(Resources.AdrPlus.TableColumnName, r => r.Name)
                .AddColumn(Resources.AdrPlus.TableColumnVersion, r => r.Version)
                .AddColumn(Resources.AdrPlus.TableColumnEvents, r => r.Events)
                .AddColumn(Resources.AdrPlus.TableColumnAllowlist, r => r.Allowlist)
                .AddColumn(Resources.AdrPlus.TableColumnPending, r => r.Pending)
                .AddItems(rows)
                .ViewOnly(true)
                .Run(cancellationToken);
            return result.IsAborted;
        }

        /// <inheritdoc/>
        public bool PromptShowPluginsHostListTable(IReadOnlyList<(string Name, string Version, string Events, string Allowlist)> rows, CancellationToken cancellationToken = default)
        {
            var result = PromptPlus.Controls
                .TableSelect<(string Name, string Version, string Events, string Allowlist)>(Resources.AdrPlus.WizardPluginsListTableTitle, string.Empty)
                .AddColumn(Resources.AdrPlus.TableColumnName, r => r.Name)
                .AddColumn(Resources.AdrPlus.TableColumnVersion, r => r.Version)
                .AddColumn(Resources.AdrPlus.TableColumnEvents, r => r.Events)
                .AddColumn(Resources.AdrPlus.TableColumnAllowlist, r => r.Allowlist)
                .AddItems(rows)
                .ViewOnly(true)
                .Run(cancellationToken);
            return result.IsAborted;
        }

        /// <inheritdoc/>
        public (bool IsAborted, string[] SelectedNames) PromptSelectActivePlugins(IReadOnlyList<string> pluginNames, IReadOnlySet<string> currentlyActive, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.WizardPluginsManagePrompt}";
            bool IsPreChecked(string name) => currentlyActive.Contains(name);
            var result = PromptPlus.Controls.MultiSelect<string>(message)
                .TextSelector(name => name)
                .Filter(FilterMode.Contains)
                .AddItems(pluginNames)
                .Default(pluginNames.Where(IsPreChecked), false)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? [] : [.. result.Content!]);
        }

        /// <inheritdoc/>
        public bool PromptShowPluginsValidateTable(IReadOnlyList<(string Status, string NameOrFolder, string Detail)> rows, CancellationToken cancellationToken = default)
        {
            var result = PromptPlus.Controls
                .TableSelect<(string Status, string NameOrFolder, string Detail)>(Resources.AdrPlus.WizardPluginsValidateTableTitle, string.Empty)
                .AddColumn(Resources.AdrPlus.TableColumnStatus, r => r.Status)
                .AddColumn(Resources.AdrPlus.TableColumnNameOrFolder, r => r.NameOrFolder)
                .AddColumn(Resources.AdrPlus.TableColumnDetail, r => r.Detail)
                .AddItems(rows)
                .ViewOnly(true)
                .Run(cancellationToken);
            return result.IsAborted;
        }


        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptEditFieldCaseTransform(FieldsJson fieldsJson, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptChooseNewValue}";
            var enumlist = Enum.GetNames<CaseFormat>();
            var result = PromptPlus.Controls
                .Select<string>(message, ShowDescField(fieldsJson))
                .Default(fieldsJson.Value)
                .AddItems(enumlist)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? fieldsJson.Value : result.Content!);
        }

        public (bool IsAborted, string FilePathAdrTemplate) PromptConfigTemplateAdrSelect(string root, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.PromptSelectAdrTemplatePath}";
            var result = PromptPlus.Controls
                .File(message)
                .SearchPattern("*.md")
                .SelectFilesOnly(true)
                .EnableHistory("AdrPlusAdrTemplatePathHistory")
                .Root(root)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? string.Empty : result.Content!.FullPath);
        }

        /// <inheritdoc/>
        public (bool IsAborted, AdrFileNameComponents? info) PromptSelecAdrs(AdrFileNameComponents[] adrFiles,AdrPlusRepoConfig adrPlusRepoConfig, Func<AdrFileNameComponents, (bool, string?)> validselect, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.NewVerChooseAdr}";
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
                    .PredicateValid((value) => string.IsNullOrWhiteSpace(value) ? (false, Resources.AdrPlus.ExceptionFilenameEmpty) : (true, string.Empty))
                    .EnableHistory("AdrPlusExploreReportFileName")
                    .Run(cancellationToken);
            return (inputfilename.IsAborted, inputfilename.IsAborted ? string.Empty : inputfilename.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, bool IsCreatingReport) PromptOptionShowOrCreateReport(CancellationToken cancellationToken)
        {
            var explorereport = PromptPlus.Controls.Switch($"{Resources.AdrPlus.PromptShowOrCreateReport}: ")
                .OffValue($"{Resources.AdrPlus.ShowAdrs}")
                .OnValue($"{Resources.AdrPlus.CreateReport}")
                .EnableHistory("AdrPlusExploreShowOrReport")
                .Default(false, true)
                .Run(cancellationToken);
            return (explorereport.IsAborted, !explorereport.IsAborted && (bool)explorereport.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string[] FieldsExplore) PromptFieldsExplore(CancellationToken cancellationToken)
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
                 .EnableHistory("AdrPlusExploreFields")
                 .UseDefaultHistory()
                 .TextSelector(item => item[2..])
                 .Run(cancellationToken);
            return (fields.IsAborted, fields.IsAborted ? [] : fields.Content!);
        }


        /// <inheritdoc/>
        public (bool IsAborted, string FileSelectd) PromptTableExplore(AdrFileNameComponents[] foundfiles, string[] fields, string folderrepoadr, AdrPlusRepoConfig adrPlusRepoConfig)
        {
            var onstart = true;
            var table = PromptPlus.Controls.TableSelect<AdrFileNameComponents>($"{Resources.AdrPlus.FilesExplored}")
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
                        ctx.AddColumn(Resources.AdrPlus.File, (item) => (object)$"{Path.GetFileName(item.FileName)} ({item.Number})", width: 40);
                        ctx.AddColumn(Resources.AdrPlus.CurrentStatus, (item) => (object)Helper.FmtStatus(item, adrPlusRepoConfig), width: 29);
                        if (fields.Any(x => x.StartsWith("3)",false,CultureInfo.InvariantCulture)))
                        {
                            ctx.AddColumn(Resources.AdrPlus.Folder, (item) => (object)Helper.FmtFolder(item, folderrepoadr), width: 20);
                        }
                        if (fields.Any(x => x.StartsWith("4)",false,CultureInfo.InvariantCulture)))
                        {
                            ctx.AddColumn(Resources.AdrPlus.Format, (item) => (object)Helper.FmtFormat(item), width: 20);
                        }
                        if (fields.Any(x => x.StartsWith("5)", false, CultureInfo.InvariantCulture)))
                        {
                            ctx.AddColumn(Resources.AdrPlus.Prefix, (item) => (object)item.Prefix, width: 5);
                        }
                        if (fields.Any(x => x.StartsWith("6)", false, CultureInfo.InvariantCulture)))
                        {
                            ctx.AddColumn(Resources.AdrPlus.Version, (item) => (object)item.Version.ToString(CultureInfo.InvariantCulture), width: 5);
                        }
                        if (fields.Any(x => x.StartsWith("7)", false, CultureInfo.InvariantCulture)))
                        {
                            ctx.AddColumn(Resources.AdrPlus.Revision, (item) => (object)(item.Revision??0).ToString(CultureInfo.InvariantCulture), width: 5);
                        }
                        if (fields.Any(x => x.StartsWith("8)", false, CultureInfo.InvariantCulture)))
                        {
                            ctx.AddColumn(Resources.AdrPlus.StatusCreated, (item) => (object)(item.Header.DateCreate == null ? string.Empty : $"{item.Header.DateCreate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}:{item.Header.StatusCreate}"), width: 25);
                        }
                        if (fields.Any(x => x.StartsWith("9)", false, CultureInfo.InvariantCulture)))
                        {
                            ctx.AddColumn(Resources.AdrPlus.StatusUpdated, (item) => (object)(item.Header.DateUpdate == null ? string.Empty : $"{item.Header.DateUpdate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}:{item.Header.StatusUpdate}"), width: 25);
                        }
                        if (fields.Any(x => x.StartsWith("10)", false, CultureInfo.InvariantCulture)))
                        {
                            ctx.AddColumn(Resources.AdrPlus.Scope, (item) => (object)(item.Header.Scope ?? string.Empty), width: 20);
                        }
                        if (fields.Any(x => x.StartsWith("11)", false, CultureInfo.InvariantCulture)))
                        {
                            ctx.AddColumn(Resources.AdrPlus.Domain, (item) => (object)(item.Header.Domain ?? string.Empty), width: 20);
                        }
                        ctx.Filter(FilterMode.Contains, FilterTableMode.ColumnFilters);
                        ctx.ChangeDescription((item) =>
                        {
                            return Path.GetDirectoryName(item.FileName) ?? string.Empty;
                        });
                    }
                })
                .Run();
                return (table.IsAborted, table.IsAborted ? string.Empty : table.Content.Value.FileName);
        }

        /// <inheritdoc/>
        public (bool IsAborted, ItemMenuWizard? Content) PromptSelectMenu(bool IsHasconfig, ItemMenuWizard[] itemMenus,ItemMenuWizard defaultvalue,  CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.SelectAnOption}";
            var result = PromptPlus.Controls
                .Select<ItemMenuWizard>(message,"")
                .Default(defaultvalue)
                .EnableHistory("AdrPlusMainMenuWizardSelection")
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
                .DefaultMatchBy((a, b) => a.Id == b.Id)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? null : result.Content!);
        }



        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptEditFieldSeparator(FieldsJson fieldsJson, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptChooseNewValue}";
            var opcsep = new[] { "-", "_", "." };
            var result = PromptPlus.Controls
                .Select<string>(message, ShowDescField(fieldsJson))
                .Default(fieldsJson.Value)
                .AddItems(opcsep)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? fieldsJson.Value : result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptEditFieldHeaderText(FieldsJson fieldsJson,int maxlength,string sugestion, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.ConfigPromptEnterNewValue}";
            var result = PromptPlus.Controls
                .Input(message, ShowDescField(fieldsJson))
                .Default(fieldsJson.Value)
                .MaxLength(maxlength)
                .PredicateValid(input => (input.Trim().Length > 0, Resources.AdrPlus.ErrMsgNotEmpty))
                .SuggestionHandler(input => [sugestion])
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? fieldsJson.Value : result.Content!);
        }

        public (bool IsAborted, int CountSelected) PromptShowAdrsMigrations(AdrFileNameComponents[] adrs, AdrPlusRepoConfig adrPlusRepo, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.PromptAdrToMigrate}";
            static bool IsReadyToMigrate(AdrFileNameComponents x) => x.IsValid && !x.Header.IsValid && !x.Header.IsMigrated;
            var result = PromptPlus.Controls.MultiSelect<AdrFileNameComponents>(message, Resources.AdrPlus.ViewOnlyPrompt)
                .TextSelector(x => $"{Path.GetFileName(x.FileName)} ")
                .ViewOnly()
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
                .PredicateChecked(IsReadyToMigrate)
                .Default(adrs.Where(IsReadyToMigrate), false)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? 0 : result.Content!.Length);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptSelectLogicalDrive(string message, IFileSystemService fileSystemService, CancellationToken cancellationToken = default)
        {
            message = $"{message}";
            string[] drives = fileSystemService.GetDrives();
            var result = PromptPlus.Controls
                .Select<string>(message)
                .UseDefaultHistory()
                .EnableHistory("AdrPlusRepoDriveSelection")
                .AddItems(drives)
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? string.Empty : result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptEditTitleAdr(string defaultTitle, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.PromptEnterAdrTitle}";
            var result = PromptPlus.Controls
                .Input(message)
                .Default(defaultTitle)
                .PredicateValid(input => (input.Trim().Length > 0, Resources.AdrPlus.ErrMsgNotEmpty))
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? defaultTitle : result.Content!);
        }

        /// <summary>
        /// Minimum Jaro-Winkler similarity for an existing value to be surfaced as a suggestion.
        /// Advisory only: never blocks or rejects what the user types.
        /// </summary>
        /// <remarks>Internal (not private) so <c>SuggestSimilar</c>'s behavior is directly unit-testable.</remarks>
        internal const double SimilaritySuggestionThreshold = 0.80;

        /// <summary>
        /// Ranks <paramref name="candidates"/> against <paramref name="input"/>, keeping those that either
        /// contain <paramref name="input"/> as a substring (case-insensitive, exact letters) or are similar
        /// enough by Jaro-Winkler distance (case- and diacritic-insensitive, tolerates typos) — the two checks
        /// are deliberately different: one is a literal substring test, the other a fuzzy one. Substring
        /// matches are listed first (a stronger signal), then the rest by descending similarity, so the best
        /// match always has priority instead of just filtering in file-scan order.
        /// </summary>
        internal static string[] SuggestSimilar(string input, string[] candidates)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return candidates;
            }
            return [.. candidates
                .Select(x => (Value: x, IsSubstringMatch: x.Contains(input, StringComparison.OrdinalIgnoreCase), Similarity: x.JaroWinklerSimilarity(input)))
                .Where(x => x.IsSubstringMatch || x.Similarity >= SimilaritySuggestionThreshold)
                .OrderByDescending(x => x.IsSubstringMatch)
                .ThenByDescending(x => x.Similarity)
                .Select(x => x.Value)];
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptEditScopeAdr(string defaultScope, string[] sugestscopes, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.PromptSelectAdrScope}";
            var result = PromptPlus.Controls
                .Input(message)
                .Default(defaultScope)
                .SuggestionHandler((input) => SuggestSimilar(input, sugestscopes))
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? defaultScope : result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptEditDomainAdr(string defaultdomain, string[] sugestdomains, CancellationToken cancellationToken = default)
        {
            var message = $"{Resources.AdrPlus.PromptEnterAdrDomain}";
            var result = PromptPlus.Controls
                .Input(message)
                .Default(defaultdomain)
                .SuggestionHandler((input) => SuggestSimilar(input, sugestdomains))
                .Run(cancellationToken);
            return (result.IsAborted, result.IsAborted ? defaultdomain : result.Content!);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string[] domains, Exception? Content) PromptGetArrayDomainsAdr(AdrFileNameComponents[] adrFiles, CancellationToken cancellationToken = default)
        {
            var defarrdomain = Array.Empty<string>();
            var message = $"{Resources.AdrPlus.PromptReadingRegisteredDomains}";
            var resuldefarrdomain = PromptPlus.Controls
                .Task(message)
                .Action(_ => defarrdomain = _adrServices.GetDomainsFrom(adrFiles))
                .Spinner(SpinnersType.Ascii)
                .Run(cancellationToken);
            return (resuldefarrdomain.IsAborted, defarrdomain, resuldefarrdomain.IsAborted ? null : resuldefarrdomain.Content!.Exception);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string[] scopes, Exception? Content) PromptGetArrayScopesAdr(AdrFileNameComponents[] adrFiles, CancellationToken cancellationToken = default)
        {
            var defarrscope = Array.Empty<string>();
            var message = $"{Resources.AdrPlus.PromptReadingRegisteredScopes}";
            var resuldefarrscope = PromptPlus.Controls
                .Task(message)
                .Action(_ => defarrscope = _adrServices.GetScopesFrom(adrFiles))
                .Spinner(SpinnersType.Ascii)
                .Run(cancellationToken);
            return (resuldefarrscope.IsAborted, defarrscope, resuldefarrscope.IsAborted ? null : resuldefarrscope.Content!.Exception);
        }

        /// <inheritdoc/>
        public (bool IsAborted, string Content) PromptSelectFolderPath(string message, bool checknitCmd, string root, IFileSystemService fileSystemService, IValidateConfig validateJsonConfig, CancellationToken cancellationToken = default)
        {
            var pronptmessage = $"{message}";
            while (true)
            {
                var result = PromptPlus.Controls
                    .File(pronptmessage)
                    .OnlyFolders()
                    .EnableHistory($"AdrPlusSelectFolderPath_{BitConverter.ToString(SHA256.HashData(Encoding.UTF8.GetBytes(message)))}")
                    .Root(root)
                    .Run(cancellationToken);
                if (result.IsAborted)
                {
                    return (true, string.Empty);
                }
                if (checknitCmd)
                {
                    var targetPath = Path.Combine(result.Content!.FullPath, validateJsonConfig.GetFileNameRepoConfig());
                    if (!fileSystemService.FileExists(targetPath))
                    {
                        PromptWriteError(Resources.AdrPlus.ErrorInitCommandNotExecuted);
                        continue;
                    }
                }
                return (false, result.Content!.FullPath);
            }
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
                .PredicateValid(input =>
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
                AppConstants.FieldWithoutArgs => Resources.AdrPlus.ConfigFieldDescBehaviorWithoutArgs,
                AppConstants.FieldFolderAdr => Resources.AdrPlus.ConfigFieldDescFolderRepo,
                AppConstants.FieldMigrationPattern => Resources.AdrPlus.ConfigFieldDescMigrationPattern,
                AppConstants.FieldOpenAdr => Resources.AdrPlus.ConfigFieldDescOpenAdr,
                AppConstants.FieldPrefix => Resources.AdrPlus.ConfigFieldDescPrefix,
                AppConstants.FieldLenSeq => Resources.AdrPlus.ConfigFieldDescLenSeq,
                AppConstants.FieldLenVersion => Resources.AdrPlus.ConfigFieldDescLenVersion,
                AppConstants.FieldLenRevision => Resources.AdrPlus.ConfigFieldDescLenRevision,
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
    }
}
