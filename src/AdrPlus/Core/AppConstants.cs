// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Resources;

namespace AdrPlus.Core
{
    internal static class AppConstants
    {
        public const string VersionFilePrefix = "adrplus.version.";

        /// <summary>
        /// The length of the header section in generated ADR files, used to determine how many lines to read when parsing the header information from an existing ADR file.
        /// </summary>
        public const int LenghtHeader = 12;

        public const string TemplateDirectoryName = "template";

        public const string AdrTemplateFileName = "adr-template.adrplus";

        public const string AdrRepoConfigFileName = "adr-config.adrplus";

        /// <summary>
        /// The file name of the opt-in seed file that pre-provisions repository configuration for the
        /// first-install flow, letting a team seed automation/CI installs with pre-approved default values
        /// instead of the interactive wizard.
        /// </summary>
        public const string FirstInstallerFileName = "firstinstaller.adrplus";

        /// <summary>
        /// The suffix appended to <see cref="FirstInstallerFileName"/> once it has been applied, so a
        /// successful run cannot be re-applied and cannot be mistaken for a pending seed file.
        /// </summary>
        public const string FirstInstallerAppliedSuffix = ".applied";

        public const string ResourceNamespace = "AdrPlus.Resources";

        public const string AppConfigfileName = "adrplus.json";

        public const string BannerText = "ADR-PLUS";

        public const char CommandArgsSeparator = (char)1;

        public const string DefaultFolderAdr = @"doc/adr";

        public const string NameApp = "AdrPlus";

        public const string CfgNameVersionApp = "VersionApp";

        public const string CfgCommandName = "CommandName";

        public const string CfgCommandArgs = "CommandArgs";

        public const string DefaultSettingsRoot = "DefaultSettings";

        public const string FieldLanguage = "language";

        public const string FieldWithoutArgs = "withoutargs";

        public const string FieldOpenAdr = "comandopenadr";

        public const string FieldPluginAllowlist = "pluginallowlist";

        public const string FieldMigrationPattern = "migrationpattern";

        public const string FieldFolderAdr = "folderadr";

        public const string FieldTemplate = "template";

        public const string FieldPrefix = "prefix";

        public const string FieldLenSeq = "lenseq";

        public const string FieldLenVersion = "lenversion";

        public const string FieldLenRevision = "lenrevision";

        public const string FieldSeparator = "separator";

        public const string FieldCaseTransform = "casetransform";

        public const string FieldStatusNew = "statusnew";

        public const string FieldStatusAccepted = "statusacc";

        public const string FieldStatusRejected = "statusrej";

        public const string FieldStatusSuperseded = "statussup";

        public const string FieldHeaderDisclaimer = "headerdisclaimer";

        public const string FieldHeaderVersion = "headerversion";

        public const string FieldHeaderRevision = "headerrevision";

        public const string FieldHeaderTitleFile = "headertitlefile";

        public const string FieldHeaderDomain = "headerdomain";

        public const string FieldHeaderScope = "headerscope";

        public const string FieldHeaderStatusCreated = "headertitlestatuscreated";

        public const string FieldHeaderStatusChanged = "headertitlestatuschanged";

        public const string FieldHeaderStatusSuperseded = "headertitlestatussuperseded";

        public const string FieldHeaderTableFields = "headertablefields";

        public const string FieldHeaderTableValues = "headertablevalues";

        public const string FieldHeaderMigrated = "headermigrated";

        /// <summary>
        /// The configuration key for the list of plugin names expected to be active for this repository.
        /// </summary>
        public const string FieldActivePlugins = "activeplugins";

        /// <summary>
        /// The configuration key for the repository-wide plugin dispatch kill switch.
        /// </summary>
        public const string FieldDisablePlugins = "disableplugins";

        /// <summary>
        /// JSON serializer options configured for repository data with lowercase property naming, indented formatting,
        /// case-insensitive deserialization, and string-based enum conversion.
        /// The result is cached after the first call.
        /// </summary>
        /// <returns>The configured JSON serializer options.</returns>
        public static JsonSerializerOptions RepoSerializerOptions => serializerOptions.Value;

        private static readonly Lazy<JsonSerializerOptions> serializerOptions = new(() => new JsonSerializerOptions
        {
            IgnoreReadOnlyFields = true,
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = new LowercaseNamingPolicy(),
            Converters = { new JsonStringEnumConverter() }
        });

        /// <summary>
        /// JSON document options configured to allow trailing commas and skip comments during parsing.
        /// The result is cached after the first call.
        /// </summary>
        /// <returns>The configured JSON document options.</returns>
        public static JsonDocumentOptions DocumentOptions => documentOptions.Value;

        private static readonly Lazy<JsonDocumentOptions> documentOptions = new(() => new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });


        /// <summary>
        /// Gets the neutral language culture name from the assembly's NeutralResourcesLanguageAttribute.
        /// Returns "en-us" as the default if the attribute is not found.
        /// The result is cached after the first call.
        /// </summary>
        /// <returns>The neutral language culture name.</returns>
        public static string GetNeutralLanguage => neutralLanguage.Value;

        private static readonly Lazy<string> neutralLanguage = new(() =>
        {
            var assembly = Assembly.GetExecutingAssembly();
            var attribute = assembly.GetCustomAttribute<NeutralResourcesLanguageAttribute>();
            return attribute?.CultureName ?? "en-us";
        });

    }
}
