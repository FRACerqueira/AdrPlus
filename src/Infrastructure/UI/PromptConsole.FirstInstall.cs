// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.Formatting;
using PromptPlusLibrary;
using System.Text.Json;

namespace AdrPlus.Infrastructure.UI
{
    internal sealed partial class PromptConsole : IPromptConsole
    {
        private readonly JsonSerializerOptions _jsonopt = new()
        {
            WriteIndented = true
        };
        public async Task<bool> FistInstall(CancellationToken cancellationToken)
        {
            if (_fileSystemService.FileExists(_validate.GetDefaultConfigRepoFilePath()))
            {
                return false;
            }
            PromptEnabledEscToAbort(true);
            var result = await WizardFirstInstall(cancellationToken);
            PromptEnabledEscToAbort(false);
            return result;
        }

        private async Task<bool> WizardFirstInstall(CancellationToken cancellationToken)
        {
            var tryabort = false;
            PromptPlus.Widgets.DoubleDash(Resources.AdrPlus.InitConfigBannerMessage, DashOptions.DoubleBorder, 1, Color.Yellow);
            var languagesetting = "en-us";
            var openadrsetting = string.Empty;
            var folderadrsetting = string.Empty;
            var prefixsetting = string.Empty;
            var lenseqsetting = 0;
            var lenversetting = 0;
            var lenrevsetting = 0;
            var lenscopessetting = 0;
            var scopestextsetting = string.Empty;
            var skipdomaintextsetting = string.Empty;
            var folderbyscopessetting = false;
            var templatecontentsetting = string.Empty;
            var migrationpatternsetting = string.Empty;
            var casetransformsetting = CaseFormat.KebabCase;
            var otherlanguage = false;    
            while (true)
            {
                if (tryabort)
                {
                    PromptEnabledEscToAbort(false);
                    var resultabort = PromptPlus.Controls.Confirm($"{Resources.AdrPlus.InitConfigSkipConfirmation} ")
                        .Run(cancellationToken);
                    PromptEnabledEscToAbort(true);
                    if (resultabort.IsAborted || resultabort.Content!.Value.IsYesResponseKey())
                    {
                        return false;
                    }
                    tryabort = false;
                }

                //language
                var languageinstall = PromptPlus.Controls.Select<SelectedItem>($"{Resources.AdrPlus.InitConfigSelectLanguage}: ")
                    .TextSelector(item => item.Name)
                    .AddItem(new SelectedItem { Name = Resources.AdrPlus.InitConfigLanguageEnglish, Code = "en-us" })
                    .AddItem(new SelectedItem { Name = Resources.AdrPlus.InitConfigLanguagePortuguese, Code = "pt-br" })
                    .AddItem(new SelectedItem { Name = Resources.AdrPlus.InitConfigLanguageOther, Code = "" })
                    .ExtraInfo(item =>
                    {
                        if (item.Code == "en-us")
                        {
                            return Resources.AdrPlus.InitConfigLanguageEnglishDesc;
                        }
                        if (item.Code == "pt-br")
                        {
                            return Resources.AdrPlus.InitConfigLanguagePortugueseDesc;
                        }
                        return Resources.AdrPlus.InitConfigLanguageOtherDesc;
                    })
                    .Run(cancellationToken);
                if (languageinstall.IsAborted)
                {
                    tryabort = true;
                    continue;
                }
                if (languageinstall.Content.Code == "pt-br")
                {
                    PromptEnsureCulture(new AdrPlusConfig() { Language = "pt-br" });
                }
                if (languageinstall.Content.Code == "")
                {
                    otherlanguage = true;
                }
                languagesetting = languageinstall.Content.Code;

                //open adr with external editor
                (bool hasVisualStudio, bool hasVSCode, bool hasRider, string editorscmd) = CheckEditorInPath();
                var items = new List<SelectedItem>();
                if (hasVisualStudio)
                {
                    items.Add(new SelectedItem { Name = "Visual-Studio", Code = "VST" });
                }
                else
                {
                    items.Add(new SelectedItem { Name = "Visual-Studio ", Code = "" });
                }
                if (hasVSCode)
                {
                    items.Add(new SelectedItem { Name = "VS-Code", Code = "VSC" });
                }
                else
                {
                    items.Add(new SelectedItem { Name = "VS-Code ", Code = "" });
                }
                if (hasRider)
                {
                    items.Add(new SelectedItem { Name = "Rider", Code = "RDR" });
                }
                else
                {
                    items.Add(new SelectedItem { Name = "Rider ", Code = "" });
                }
                if (items.Count > 0)
                {
                    var editorinstall = PromptPlus.Controls.Select<SelectedItem>($"{Resources.AdrPlus.InitConfigSelectEditor}: ")
                        .TextSelector(item => item.Name)
                        .AddItem(new SelectedItem { Name = Resources.AdrPlus.InitConfigEditorNone, Code = "x" })
                        .Interaction(items.Where(x => x.Code.Length > 0), (item, ctx) =>
                        {
                            ctx.AddItem(item);
                        })
                        .AddItems(items.Where(x => x.Code.Length == 0), true)
                        .ExtraInfo(item => item.Code == "" ? Resources.AdrPlus.InitConfigEditorNoneDesc : "")
                        .Run(cancellationToken);
                    if (editorinstall.IsAborted)
                    {
                        tryabort = true;
                        continue;
                    }
                    var editorselected = editorinstall.Content.Code;
                    var editors = editorscmd.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    var index = Array.FindIndex(editors, x => x.StartsWith($"{editorselected}:", StringComparison.OrdinalIgnoreCase));
                    if (index >= 0)
                    {
                        openadrsetting = editors[index][(editors[index].IndexOf(':') + 1)..];
                    }
                }

                //default folder for ADR files
                var defaultfolderadr = PromptPlus.Controls
                    .Input($"{Resources.AdrPlus.InitConfigDefaultFolderADR}: ", Resources.AdrPlus.ConfigFieldDescFolderRepo)
                    .Default(AppConstants.DefaultFolderAdr)
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
                            return (false, string.Format(null, FormatMessages.ErrMsgFolderRepoMustBeRelativeFormat, input));
                        }
                        catch (NotSupportedException)
                        {
                            return (false, string.Format(null, FormatMessages.ErrMsgFolderRepoMustBeRelativeFormat, input));
                        }
                        return (true, string.Empty);
                    })
                    .Run(cancellationToken);
                if (defaultfolderadr.IsAborted)
                {
                    tryabort = true;
                    continue;
                }
                folderadrsetting = defaultfolderadr.Content.Trim();

                //patterns in file name
                var optionsPatternsADR = PromptPlus.Controls.MultiSelect<SelectedItem>($"{Resources.AdrPlus.InitConfigSelectPatterns}: ")
                    .TextSelector(item => item.Name)
                    .AddItem(new SelectedItem { Name = Resources.AdrPlus.Prefix, Code = "P" }, true)
                    .AddItem(new SelectedItem { Name = Resources.AdrPlus.Number, Code = "N" }, true, true)
                    .AddItem(new SelectedItem { Name = Resources.AdrPlus.Version, Code = "V" }, true, true)
                    .AddItem(new SelectedItem { Name = Resources.AdrPlus.Revision, Code = "R" })
                    .AddItem(new SelectedItem { Name = Resources.AdrPlus.InitConfigEnterCaseTransform, Code = "T" })
                    .AddItem(new SelectedItem { Name = Resources.AdrPlus.InitConfigEnterSpecialCases, Code = "S" })
                    .Run(cancellationToken);
                if (optionsPatternsADR.IsAborted)
                {
                    tryabort = true;
                    continue;
                }

                if (optionsPatternsADR.Content.Any(x => x.Code == "P"))
                {
                    var resultoption = PromptPlus.Controls.Input($"{Resources.AdrPlus.InitConfigEnterPrefix}: ")
                        .MaxLength(5)
                        .Default("ADR")
                        .AcceptInput(c => char.IsLetter(c))
                        .PredicateSelected(input => !string.IsNullOrWhiteSpace(input))
                        .Run(cancellationToken);
                    if (resultoption.IsAborted)
                    {
                        tryabort = true;
                        continue;
                    }
                    prefixsetting = resultoption.Content.Trim();

                }
                if (optionsPatternsADR.Content.Any(x => x.Code == "N"))
                {
                    var resultoption = PromptPlus.Controls.Slider($"{Resources.AdrPlus.InitConfigEnterNumberLength}: ")
                        .Range(3, 6)
                        .Default(3)
                        .Step(1)
                        .LargeStep(1)
                        .Layout(SliderLayout.UpDown)
                        .Run(cancellationToken);
                    if (resultoption.IsAborted)
                    {
                        tryabort = true;
                        continue;
                    }
                    lenseqsetting = (int)resultoption.Content!;
                }
                if (optionsPatternsADR.Content.Any(x => x.Code == "V"))
                {
                    var resultoption = PromptPlus.Controls.Slider($"{Resources.AdrPlus.InitConfigEnterVersionLength}: ")
                        .Range(2, 3)
                        .Default(2)
                        .Step(1)
                        .LargeStep(1)
                        .Layout(SliderLayout.UpDown)
                        .Run(cancellationToken);
                    if (resultoption.IsAborted)
                    {
                        tryabort = true;
                        continue;
                    }
                    lenversetting = (int)resultoption.Content!;
                }
                if (optionsPatternsADR.Content.Any(x => x.Code == "R"))
                {
                    var resultoption = PromptPlus.Controls.Slider($"{Resources.AdrPlus.InitConfigEnterRevisionLength}: ")
                        .Range(2, 3)
                        .Default(2)
                        .Step(1)
                        .LargeStep(1)
                        .Layout(SliderLayout.UpDown)
                        .Run(cancellationToken);
                    if (resultoption.IsAborted)
                    {
                        tryabort = true;
                        continue;
                    }
                    lenrevsetting = (int)resultoption.Content!;
                }
                if (optionsPatternsADR.Content.Any(x => x.Code == "T"))
                {
                    var enumlist = Enum.GetNames<CaseFormat>();
                    var resultcase = PromptPlus.Controls
                        .Select<string>($"{Resources.AdrPlus.InitConfigEnterCaseTransform}: ", Resources.AdrPlus.ConfigFieldDescCaseTransform)
                        .Default(CaseFormat.KebabCase.ToString())
                        .MaxWidth(10)
                        .AddItems(enumlist)
                        .Run(cancellationToken);
                    if (resultcase.IsAborted)
                    {
                        tryabort = true;
                        continue;
                    }
                    casetransformsetting = Enum.Parse<CaseFormat>(resultcase.Content!);
                }
                if (optionsPatternsADR.Content.Any(x => x.Code == "S"))
                {
                    var resultoption = PromptPlus.Controls.Slider($"{Resources.AdrPlus.InitConfigEnterScopeLength}: ")
                        .Range(0, 5)
                        .Default(1)
                        .Step(1)
                        .LargeStep(1)
                        .Layout(SliderLayout.UpDown)
                        .Run(cancellationToken);
                    if (resultoption.IsAborted)
                    {
                        tryabort = true;
                        continue;
                    }
                    lenscopessetting = (int)resultoption.Content!;
                    if (resultoption.Content != 0)
                    {
                        var scopeslist = PromptPlus.Controls
                            .Input($"{Resources.AdrPlus.InitConfigListScopes}: ", Resources.AdrPlus.ConfigFieldDescScopes)
                            .Default(Resources.AdrPlus.DefaultScope)
                            .AcceptInput(input => char.IsAsciiLetter(input) || input == ';' || input == '*')
                            .SuggestionHandler(input => [Resources.AdrPlus.DefaultScope])
                            .PredicateSelected(input =>
                            {
                                if (input.Length == 0)
                                {
                                    return (false, Resources.AdrPlus.ErrorInvalidScopeFormat);
                                }
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
                        if (scopeslist.IsAborted)
                        {
                            tryabort = true;
                            continue;
                        }
                        var listscopes = scopeslist.Content
                            .Split(';', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Replace("*", "").Trim()).ToArray();

                        scopestextsetting = string.Join(";", listscopes.Distinct(StringComparer.OrdinalIgnoreCase));

                        var listskipdomain = scopeslist.Content
                                 .Split(';', StringSplitOptions.RemoveEmptyEntries)
                                 .Where(s => s.EndsWith("*", StringComparison.OrdinalIgnoreCase))
                                 .Select(s => s.Replace("*", "").Trim()).ToArray();
                        if (listskipdomain.Length > 0)
                        {
                            skipdomaintextsetting = string.Join(";", listskipdomain.Distinct(StringComparer.OrdinalIgnoreCase));
                        }


                        var optionsfolder = PromptPlus.Controls.Select<SelectedItem>($"{Resources.AdrPlus.InitConfigCreateFolderByScope} ")
                            .TextSelector(item => item.Name)
                            .AddItem(new SelectedItem { Name = Resources.AdrPlus.Yes, Code = "true" })
                            .AddItem(new SelectedItem { Name = Resources.AdrPlus.No, Code = "false" })
                            .Run(cancellationToken);
                        if (optionsfolder.IsAborted)
                        {
                            tryabort = true;
                            continue;
                        }
                        if (optionsfolder.Content.Code == "true")
                        {
                            folderbyscopessetting = true;
                        }
                    }
                }

                //template
                var optionstemplate = PromptPlus.Controls.Select<SelectedItem>($"{Resources.AdrPlus.InitConfigSelectTemplate}: ")
                    .TextSelector(item => item.Name)
                    .AddItem(new SelectedItem { Name = Resources.AdrPlus.InitConfigTemplateDefault, Code = "D" })
                    .AddItem(new SelectedItem { Name = Resources.AdrPlus.InitConfigTemplateEmbedded, Code = "E" })
                    .AddItem(new SelectedItem { Name = Resources.AdrPlus.InitConfigTemplateCustom, Code = "C" })
                    .Run(cancellationToken);
                if (optionstemplate.IsAborted)
                {
                    tryabort = true;
                    continue;
                }

                string FilePathAdrTemplate = string.Empty;
                if (optionstemplate.Content.Code == "D")
                {
                    templatecontentsetting = await _validate.GetConfigAdrTemplateAsync(cancellationToken);
                }
                else if (optionstemplate.Content.Code == "E")
                {
                    var rootPath = Path.GetDirectoryName(_validate.GetConfigAdrTemplatePath())!;
                    var (IsAborted, SelectedTemplate) = PromptConfigTemplateAdrSelect(rootPath, cancellationToken);
                    if (IsAborted)
                    {
                        tryabort = true;
                        continue;
                    }
                    FilePathAdrTemplate = SelectedTemplate;
                }
                else if (optionstemplate.Content.Code == "C")
                {
                    string[] drives = _fileSystemService.GetDrives();
                    var rootPath = drives[0];
                    if (drives.Length > 1)
                    {
                        var (IsAborted, Content) = PromptSelectLogicalDrive(Resources.AdrPlus.NewAdrPromptSelectDrive, _fileSystemService, cancellationToken);
                        if (IsAborted)
                        {
                            tryabort = true;
                            continue;
                        }
                        rootPath = Content;
                    }
                    var Filetemplate = PromptPlus.Controls.FileSelect($"{Resources.AdrPlus.InitConfigSelectCustomTemplate}: ")
                        .SearchPattern("*.md")
                        .PredicateSelected(item =>
                        {
                            return (!item.IsFolder) && item.Name.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
                        })
                        .Root(rootPath)
                        .Run(cancellationToken);
                    if (Filetemplate.IsAborted)
                    {
                        tryabort = true;
                        continue;
                    }
                    FilePathAdrTemplate = Filetemplate.Content.FullPath;
                }
                if (templatecontentsetting.Length == 0)
                {
                    templatecontentsetting = await _fileSystemService.ReadAllTextAsync(FilePathAdrTemplate, cancellationToken);
                }

                //Migration
                var optionsMigrate = PromptPlus.Controls.Select<SelectedItem>($"{Resources.AdrPlus.InitConfigMigrationQuestion} ")
                    .TextSelector(item => item.Name)
                    .AddItem(new SelectedItem { Name = Resources.AdrPlus.Yes, Code = "true" })
                    .AddItem(new SelectedItem { Name = Resources.AdrPlus.No, Code = "false" })
                    .Run(cancellationToken);
                if (optionsMigrate.IsAborted)
                {
                    tryabort = true;
                    continue;
                }

                PromptWriteSummary($"{Resources.AdrPlus.SummaryLanguage} : {languagesetting}");
                if (!string.IsNullOrEmpty(openadrsetting))
                {
                    PromptWriteSummary($"{Resources.AdrPlus.SummaryOpenAdrWith} : {openadrsetting}");
                }
                PromptWriteSummary($"{Resources.AdrPlus.SummaryFolderForAdrFiles} : {folderadrsetting}");
                if (prefixsetting.Length > 0)
                {
                    PromptWriteSummary($"{Resources.AdrPlus.SummaryPrefixInAdrFileName} : {prefixsetting}");
                }
                PromptWriteSummary($"{Resources.AdrPlus.SummaryLengthForNumberInAdrFileName} : {lenseqsetting}");
                PromptWriteSummary($"{Resources.AdrPlus.SummaryLengthForVersionInAdrFileName} : {lenversetting}");
                if (lenrevsetting.CompareTo(0) > 0)
                {
                    PromptWriteSummary($"{Resources.AdrPlus.SummaryLengthForRevisionInAdrFileName} : {lenrevsetting}");
                }
                if (lenscopessetting.CompareTo(0) > 0)
                {
                    PromptWriteSummary($"{Resources.AdrPlus.SummaryLengthForScopeInAdrFileName} : {lenscopessetting}");
                    PromptWriteSummary($"{Resources.AdrPlus.SummaryScopes} : {scopestextsetting}");
                    if (!string.IsNullOrEmpty(skipdomaintextsetting))
                    {
                        PromptWriteSummary($"{Resources.AdrPlus.SummarySkipDomainCheckForScopes} : {skipdomaintextsetting}");
                    }
                    if (folderbyscopessetting)
                    {
                        PromptWriteSummary($"{Resources.AdrPlus.SummaryFolderByScope} : {Resources.AdrPlus.Yes}");
                    }
                }
                PromptWriteSummary($"{Resources.AdrPlus.SummaryCaseTransformForAdrFileName} : {casetransformsetting}");
                if (optionsMigrate.Content.Code == "true")
                {
                    var (IsAborted, ConfigMigration) = WizardMigrationConfig(cancellationToken);
                    if (IsAborted)
                    {
                        tryabort = true;
                        continue;
                    }
                    PromptWriteSummary($"{Resources.AdrPlus.SummarySampleFilename}: {ConfigMigration.Sample}");
                    var indexpos = 0;
                    if (ConfigMigration.LenPrefix > 0)
                    {
                        PromptWriteSummary($"{Resources.AdrPlus.SummarySampleMigration} {Resources.AdrPlus.Prefix} : {ConfigMigration.Sample[..ConfigMigration.LenPrefix]}");
                        indexpos = ConfigMigration.LenPrefix + 1;
                    }
                    PromptWriteSummary($"{Resources.AdrPlus.SummarySampleMigration} {Resources.AdrPlus.Number} : {ConfigMigration.Sample.Substring(indexpos, ConfigMigration.LenNumber)}");
                    indexpos += ConfigMigration.LenNumber + 1;
                    if (ConfigMigration.LenVersion > 0)
                    {
                        PromptWriteSummary($"{Resources.AdrPlus.SummarySampleMigration} {Resources.AdrPlus.Version} : {ConfigMigration.Sample.Substring(indexpos, ConfigMigration.LenVersion)}");
                        indexpos += ConfigMigration.LenVersion + 1;
                    }
                    if (ConfigMigration.LenRevision > 0)
                    {
                        PromptWriteSummary($"{Resources.AdrPlus.SummarySampleMigration} {Resources.AdrPlus.ReadyToMigrate} : {ConfigMigration.Sample.Substring(indexpos, ConfigMigration.LenRevision)}");
                        indexpos += ConfigMigration.LenRevision + 1;
                    }
                    PromptWriteSummary($"{Resources.AdrPlus.SummarySampleMigration} {Resources.AdrPlus.Title} : {ConfigMigration.Sample[indexpos..]}");
                    migrationpatternsetting = PatternParser.CreateMigratePattern(ConfigMigration);
                }
                if (otherlanguage)
                {
                    PromptWriteSummary(Resources.AdrPlus.InitConfigRemindCustomLanguage);
                }
                PromptWriteSummary("");
                var resultok = PromptConfirm(Resources.AdrPlus.PromptConfirmParamMigration, cancellationToken);
                if (resultok.IsAborted)
                {
                    tryabort = true;
                    continue;
                }
                if (resultok.ConfirmYes)
                {
                    break;
                }
                PromptShowBanner(AppConstants.BannerText);
                PromptShowWellcome(_configuration[AppConstants.CfgNameVersionApp]!);
            }

            PromptShowBanner(AppConstants.BannerText);
            PromptShowWellcome(_configuration[AppConstants.CfgNameVersionApp]!);
            //write config file application
            var jsoncontent = $"{{\"DefaultSettings\":{{\"language\": \"{languagesetting}\",\"comandopenadr\": \"{openadrsetting}\"}}}}";

            using (var jsonDoc = JsonDocument.Parse(jsoncontent))
            {
                jsoncontent = JsonSerializer.Serialize(jsonDoc, _jsonopt);
            }
            var filepath = _validate.GetConfigAppFilePath();
            await _fileSystemService.WriteAllTextAsync(filepath, jsoncontent, cancellationToken);
            PromptWriteSuccess(filepath);

            //write config file for repository
            var configrecord = new AdrPlusRepoConfig(folderadrsetting, templatecontentsetting)
            {
                CaseTransform = casetransformsetting,
                LenScope = lenscopessetting,
                LenRevision = lenrevsetting,
                LenSeq = lenseqsetting,
                LenVersion = lenversetting,
                Prefix = prefixsetting,
                Scopes = scopestextsetting,
                SkipDomain = skipdomaintextsetting,
                MigrationPattern = migrationpatternsetting
            };
            jsoncontent = JsonSerializer.Serialize(configrecord, AppConstants.RepoSerializerOptions);
            filepath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, AppConstants.TemplateDirectoryName, AppConstants.AdrRepoConfigFileName));
            await _fileSystemService.WriteAllTextAsync(filepath, jsoncontent, cancellationToken);
            PromptWriteSuccess(filepath);
            PromptEnabledEscToAbort(false);
            var anykey = PromptPlus.Controls.KeyPress(Resources.AdrPlus.InitConfigPressAnyKeyToContinue)
                .Options(x => x.ShowTooltip(false))
                .Run(cancellationToken);
            if (anykey.IsAborted)
            {
                throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);

            }

            //set init seleceted for menu wizard
            var history = new ItemMenuWizard()
            {
                Id = "1.05",
                Title = Resources.AdrPlus.WizardAdrInitTitle,
                Description = Resources.AdrPlus.WizardAdrInitDescription,
                EnabledWhenNotConfigured = false
            };
            if (otherlanguage)
            {
                history = new ItemMenuWizard
                {
                    Id = "1.04",
                    Title = Resources.AdrPlus.WizardConfigRepositoryTitle,
                    Description = Resources.AdrPlus.WizardConfigRepositoryDescription,
                    EnabledWhenNotConfigured = true
                };
            }
            PromptPlus.Controls.History("AdrPlusMainMenuWizardSelection")
                .AddHistory(JsonSerializer.Serialize(history))
                .Save();

            return true;
        }

        private (bool IsAborted, ConfigMigration Content) WizardMigrationConfig(CancellationToken cancellationToken)
        {
            var configrecord = new ConfigMigration();
            ClearHistoryMigration();
            var prefixvalue = string.Empty;
            var (IsAborted, FieldsFromFileAdr) = PromptFieldsFromFileAdr(cancellationToken);
            if (IsAborted)
            {
                return (true, configrecord);
            }

            var sample = PromptSampleFileMigration(cancellationToken);
            if (sample.IsAborted)
            {
                return (true, configrecord);
            }

            var filename = Path.GetFileNameWithoutExtension(sample.SampleFileMigration);
            var maxlen = filename.Length;
            var largestep = 5;
            if (maxlen < largestep)
            {
                largestep = maxlen;
            }
            configrecord.Sample = filename;

            double defaulvalue = 0;

            if (FieldsFromFileAdr.Any(x => x.StartsWith('P')))
            {
                var elementprefix = PromptSelectPrefixPosition(filename, maxlen, 0, cancellationToken);
                if (elementprefix.IsAborted)
                {
                    return (true, configrecord);
                }

                defaulvalue = elementprefix.Value + 1;
                if (defaulvalue > maxlen - 1)
                {
                    defaulvalue = maxlen - 1;
                }

                var elementlenprefix = PromptSelectPrefixLength(filename, elementprefix.Value, maxlen, 3, cancellationToken);
                if (elementlenprefix.IsAborted)
                {
                    return (true, configrecord);
                }
                configrecord.Prefix = (int)elementprefix.Value;
                configrecord.LenPrefix = (int)elementlenprefix.Value;
                prefixvalue = elementlenprefix.PrefixValue;

                defaulvalue += elementlenprefix.Value;
                if (defaulvalue > maxlen - 1)
                {
                    defaulvalue = maxlen - 1;
                }
            }

            var elementnumber = PromptSelectNumberPosition(filename, maxlen, (int)defaulvalue, cancellationToken);
            if (elementnumber.IsAborted)
            {
                return (true, configrecord);
            }

            defaulvalue = elementnumber.Value + 1;
            if (defaulvalue > maxlen - 1)
            {
                defaulvalue = maxlen - 1;
            }

            var elementlennumber = PromptSelectNumberLength(filename, elementnumber.Value, 6, 3, cancellationToken);
            if (elementlennumber.IsAborted)
            {
                return (true, configrecord);
            }

            configrecord.Number = (int)elementnumber.Value;
            configrecord.LenNumber = (int)elementlennumber.Value;


            defaulvalue += elementlennumber.Value;
            if (defaulvalue > maxlen - 1)
            {
                defaulvalue = maxlen - 1;
            }

            if (FieldsFromFileAdr.Any(x => x.StartsWith('V')))
            {
                var elementversion = PromptSelectVersionPosition(filename, maxlen, (int)defaulvalue, cancellationToken);
                if (elementversion.IsAborted)
                {
                    return (true, configrecord);
                }

                defaulvalue = elementversion.Value + 1;
                if (defaulvalue > maxlen - 1)
                {
                    defaulvalue = maxlen - 1;
                }

                var elementlenversion = PromptSelectVersionLength(filename, elementversion.Value, 3, 2, cancellationToken);
                if (elementlenversion.IsAborted)
                {
                    return (true, configrecord);
                }

                configrecord.Version = elementversion.Value;
                configrecord.LenVersion = elementlenversion.Value;

                defaulvalue += elementlenversion.Value;
                if (defaulvalue > maxlen - 1)
                {
                    defaulvalue = maxlen - 1;
                }
            }
            if (FieldsFromFileAdr.Any(x => x.StartsWith('R')))
            {
                var elementrevision = PromptSelectRevisionPosition(filename, maxlen, (int)defaulvalue, cancellationToken);
                if (elementrevision.IsAborted)
                {
                    return (true, configrecord);
                }

                defaulvalue = elementrevision.Value + 1;
                if (defaulvalue > maxlen - 1)
                {
                    defaulvalue = maxlen - 1;
                }

                var elementlenrevision = PromptSelectRevisionLength(filename, elementrevision.Value, 3, 2, cancellationToken);
                if (elementlenrevision.IsAborted)
                {
                    return (true, configrecord);
                }

                configrecord.Revision = elementrevision.Value;
                configrecord.LenRevision = elementlenrevision.Value;

                defaulvalue += elementlenrevision.Value;
                if (defaulvalue > maxlen - 1)
                {
                    defaulvalue = maxlen - 1;
                }
            }

            var elementtitle = PromptSelectTitlePosition(filename, maxlen, (int)defaulvalue, cancellationToken);
            if (elementtitle.IsAborted)
            {
                return (true, configrecord);
            }
            configrecord.Title = elementtitle.Value;
            return (false, configrecord);
        }

        /// <summary>
        /// Checks if Visual Studio, VS Code, or Rider is available in the global PATH environment variable.
        /// </summary>
        /// <returns>
        /// A tuple containing:
        /// - hasVisualStudio: true if devenv.exe is found in PATH (Visual Studio)
        /// - hasVSCode: true if code.exe or code.cmd is found in PATH (VS Code)
        /// - hasRider: true if rider64.exe or rider.exe is found in PATH (JetBrains Rider)
        /// </returns>
        public static (bool hasVisualStudio, bool hasVSCode, bool hasRider, string editorcmd) CheckEditorInPath()
        {
            var pathVariable = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrWhiteSpace(pathVariable))
            {
                return (false, false, false, string.Empty);
            }

            var pathDirectories = pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

            bool hasVisualStudio = false;
            bool hasVSCode = false;
            bool hasRider = false;
            string editorcmd = string.Empty;
            foreach (var directory in pathDirectories)
            {
                try
                {
                    if (Directory.Exists(directory))
                    {
                        // Check for Visual Studio (devenv.exe)
                        if (!hasVisualStudio && File.Exists(Path.Combine(directory, "devenv.exe")))
                        {
                            editorcmd += "VST:devenv.exe {0};";
                            hasVisualStudio = true;
                        }

                        // Check for VS Code (code.exe or code.cmd)
                        if (!hasVSCode && (File.Exists(Path.Combine(directory, "code.exe"))))
                        {
                            editorcmd += "VSC:code.exe {0};";
                            hasVSCode = true;
                        }
                        else if (!hasVSCode && (File.Exists(Path.Combine(directory, "code.cmd"))))
                        {
                            editorcmd += "VSC:code.cmd {0};";
                            hasVSCode = true;
                        }
                        // Check for Rider (rider64.exe or rider.exe)
                        if (!hasRider && File.Exists(Path.Combine(directory, "rider64.exe")))
                        {
                            editorcmd += "RDR:rider64.exe {0};";
                            hasRider = true;
                        }
                        else if (!hasRider && File.Exists(Path.Combine(directory, "rider.exe")))
                        {
                            editorcmd += "RDR:rider.exe {0};";
                            hasRider = true;
                        }
                        // Early exit if all found
                        if (hasVisualStudio && hasVSCode && hasRider)
                        {
                            break;
                        }
                    }
                }
                catch
                {
                    // Ignore errors accessing directories (e.g., permissions issues)
                    continue;
                }
            }

            return (hasVisualStudio, hasVSCode, hasRider, editorcmd);
        }

        private class SelectedItem
        {
            public string Name { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
        }
    }
}
