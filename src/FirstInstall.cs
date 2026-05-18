// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.UI;
using PromptPlusLibrary;

namespace AdrPlus
{
    internal class FirstInstall(IFileSystemService fileSystemService, IValidateJsonConfig validate, IPromptConsole prompt) : IFirstInstall
    {
        private IFileSystemService _fileSystemService = fileSystemService;
        private IValidateJsonConfig _validate = validate;
        private IPromptConsole _prompt = prompt;

        public async Task<bool> Install(CancellationToken cancellationToken)
        {
            var baseDirectory = AppContext.BaseDirectory;
            var filePath = Path.GetFullPath(Path.Combine(baseDirectory, AppConstants.FileFirstInstall));
            if (!_fileSystemService.FileExists(filePath))
            {
                return true;
            }
            var result = await WizardFirstInstall(cancellationToken);
            return result;
        }

        private async Task<bool> WizardFirstInstall(CancellationToken cancellationToken)
        {
            PromptPlus.Widgets.DoubleDash("Let's start with the initial configuration", DashOptions.DoubleBorder,1, Color.Yellow);

            var languageinstall = PromptPlus.Controls.Select<SelectedItem>("Select the language for the ADR: ")
                .TextSelector(item => item.Name)
                .AddItem(new SelectedItem { Name = "English ", Code = "en-us" })
                .AddItem(new SelectedItem { Name = "Portuguese", Code = "pt-br" })
                .AddItem(new SelectedItem { Name = "Other ", Code = "" })
                .ExtraInfo(item => 
                { 
                    if (item.Code == "en-us")
                    {
                        return "Interface tool, status and header will be in English";
                    }
                    if (item.Code == "pt-br")
                    {
                        return "Interface tool, status and header will be in Portuguese";
                    }
                    return "Interface tool will be in English, status and header custom by user";
                })
                .Run(cancellationToken);
            if (languageinstall.IsAborted)
            {
                throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
            }
            if (languageinstall.Content.Code == "pt-br")
            {
                _prompt.PromptEnsureCulture(new AdrPlusConfig() { Language = "pt-br" });
            }

            var optionsMigrate = PromptPlus.Controls.Select<SelectedItem>("Do you want to migrate existing ADRs?: ")
                .TextSelector(item => item.Name)
                .AddItem(new SelectedItem { Name = "Yes", Code = "true" })
                .AddItem(new SelectedItem { Name = "No", Code = "false" })
                .Run(cancellationToken);
            if (optionsMigrate.IsAborted)
            {
                throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
            }

            (bool hasVisualStudio, bool hasVSCode, bool hasRider) = CheckEditorInPath();
            var items = new List<SelectedItem>();
            if (hasVisualStudio)
            {
                items.Add(new SelectedItem { Name = "Visual Studio", Code = "VST" });
            }
            else
            {
                items.Add(new SelectedItem { Name = "Visual Studio ", Code = "-" });
            }
            if (hasVSCode)
            {
                items.Add(new SelectedItem { Name = "VS Code", Code = "VSC" });
            }
            else
            {
                items.Add(new SelectedItem { Name = "VS Code ", Code = "-" });
            }
            if (hasRider)
            {
                items.Add(new SelectedItem { Name = "Rider", Code = "RDR" });
            }
            else
            {
                items.Add(new SelectedItem { Name = "Rider ", Code = "-" });
            }
            if (items.Count > 0)
            {
                items.Insert(0, new SelectedItem { Name = "None", Code = "" });
                var editorinstall = PromptPlus.Controls.Select<SelectedItem>("Select the editor to open ADR when creating/superseding a ADR: ")
                    .TextSelector(item => item.Name)
                    .Interaction(items, (item, ctx) =>
                        {
                            if (item.Code == "-")
                            {
                                ctx.AddItem(item, true);
                            }
                            else
                            {
                                ctx.AddItem(item);
                            }
                        })
                    .ExtraInfo(item => item.Code == "-"? "without global path" : "")
                    .Run(cancellationToken);
                if (editorinstall.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
            }

            var optionsPatternsADR = PromptPlus.Controls.MultiSelect<SelectedItem>("Select the desired patterns in the ADR files: ")
                    .TextSelector(item => item.Name)
                    .AddItem(new SelectedItem { Name = "Prefix", Code = "P" }, true)
                    .AddItem(new SelectedItem { Name = "Number", Code = "N" }, true, true)
                    .AddItem(new SelectedItem { Name = "Version", Code = "V" }, true, true)
                    .AddItem(new SelectedItem { Name = "Revision", Code = "R" })
                    .AddItem(new SelectedItem { Name = "Special cases (Scope,Folder by Scope)", Code = "S" })
                    .Run(cancellationToken);
            if (optionsPatternsADR.IsAborted)
            {
                throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
            }

            if (optionsPatternsADR.Content.Any(x => x.Code == "P"))
            { 
                var resultoption = PromptPlus.Controls.Input("Enter the prefix for the ADR file name (e.g. ADR): ")
                    .MaxLength(5)
                    .Default("ADR")
                    .AcceptInput(c => char.IsLetter(c))
                    .PredicateSelected(input => !string.IsNullOrWhiteSpace(input))
                    .Run(cancellationToken);
            }
            if (optionsPatternsADR.Content.Any(x => x.Code == "N"))
            {
                var resultoption = PromptPlus.Controls.Slider("Enter the length for number ADR: ")
                    .Range(3,6)
                    .Default(3)
                    .Step(1)
                    .LargeStep(1)
                    .Layout(SliderLayout.UpDown)
                    .Run(cancellationToken);
            }
            if (optionsPatternsADR.Content.Any(x => x.Code == "V"))
            {
                var resultoption = PromptPlus.Controls.Slider("Enter the length for version ADR: ")
                    .Range(2, 3)
                    .Default(2)
                    .Step(1)
                    .LargeStep(1)
                    .Layout(SliderLayout.UpDown)
                    .Run(cancellationToken);
            }
            if (optionsPatternsADR.Content.Any(x => x.Code == "R"))
            {
                var prefix = PromptPlus.Controls.Slider("Enter the length for revision ADR: ")
                    .Range(2, 3)
                    .Default(2)
                    .Step(1)
                    .LargeStep(1)
                    .Layout(SliderLayout.UpDown)
                    .Run(cancellationToken);
            }

            var optionstemplate = PromptPlus.Controls.Select<SelectedItem>("Select a file markdown template for new ADRs: ")
                .TextSelector(item => item.Name)
                .AddItem(new SelectedItem { Name = "Default by tool(madr)", Code = "D" })
                .AddItem(new SelectedItem { Name = "Other embedded templates", Code = "E" })
                .AddItem(new SelectedItem { Name = "Custom template", Code = "C" })
                .Run(cancellationToken);
            if (optionstemplate.IsAborted)
            {
                throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
            }

            var FilePathAdrTemplate = string.Empty;
            if (optionstemplate.Content.Code == "E")
            {
                var rootPath = Path.GetDirectoryName(_validate.GetConfigAdrTemplatePath())!;
                var (IsAborted, SelectedTemplate) = _prompt.PromptConfigTemplateAdrSelect(rootPath, cancellationToken);
                if (IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                FilePathAdrTemplate = SelectedTemplate;
            }
            else if (optionstemplate.Content.Code == "C")
            {
                string[] drives = _fileSystemService.GetDrives();
                var rootPath = drives[0];
                if (drives.Length > 1)
                {
                    var (IsAborted, Content) = _prompt.PromptSelectLogicalDrive(Resources.AdrPlus.NewAdrPromptSelectDrive, _fileSystemService, cancellationToken);
                    if (IsAborted)
                    {
                        throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                    }
                    rootPath = Content;
                }
                var Filetemplate = PromptPlus.Controls.FileSelect("Select a custom template file: ")
                    .SearchPattern("*.md")
                    .PredicateSelected(item =>
                    {
                        return (!item.IsFolder) && item.Name.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
                    })
                    .Root(rootPath)
                    .Run(cancellationToken);
                if (Filetemplate.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                FilePathAdrTemplate = Filetemplate.Content.FullPath;
            }
            return true;
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
        public static (bool hasVisualStudio, bool hasVSCode, bool hasRider) CheckEditorInPath()
        {
            var pathVariable = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrWhiteSpace(pathVariable))
            {
                return (false, false, false);
            }

            var pathDirectories = pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

            bool hasVisualStudio = false;
            bool hasVSCode = false;
            bool hasRider = false;

            foreach (var directory in pathDirectories)
            {
                try
                {
                    if (Directory.Exists(directory))
                    {
                        // Check for Visual Studio (devenv.exe)
                        if (!hasVisualStudio && File.Exists(Path.Combine(directory, "devenv.exe")))
                        {
                            hasVisualStudio = true;
                        }

                        // Check for VS Code (code.exe or code.cmd)
                        if (!hasVSCode && (File.Exists(Path.Combine(directory, "code.exe")) ||
                                           File.Exists(Path.Combine(directory, "code.cmd"))))
                        {
                            hasVSCode = true;
                        }

                        // Check for Rider (rider64.exe or rider.exe)
                        if (!hasRider && (File.Exists(Path.Combine(directory, "rider64.exe")) ||
                                          File.Exists(Path.Combine(directory, "rider.exe"))))
                        {
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

            return (hasVisualStudio, hasVSCode, hasRider);
        }

        private class SelectedItem
        {
            public string Name { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
        }
    }
}
