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

        /// <summary>
        /// The prefix used in the version file name for ADR files, allowing users to specify a custom prefix that will be included in the generated version file names for ADRs. This prefix can help differentiate version files from other types of files in the ADR repository and provide a consistent naming convention for version information. 
        /// </summary>
        public const string VersionFilePrefix = "adrplus.version.";

        /// <summary>
        /// The length of the header section in generated ADR files, used to determine how many lines to read when parsing the header information from an existing ADR file. 
        /// </summary>
        public const int LenghtHeader = 12;

        /// <summary>
        /// The name of the directory where ADR templates are stored. This directory is used to store template files that define the structure and content of ADRs, allowing users to create new ADRs based on predefined templates.  
        /// </summary>
        public const string TemplateDirectoryName = "template";

        /// <summary>
        /// Represents the default file name for the ADR template file used by the application.
        /// </summary>
        /// <remarks>Use this constant when referencing or creating the ADR template file to ensure
        /// consistency across the application.</remarks>
        public const string AdrTemplateFileName = "adr-template.adrplus";

        /// <summary>
        /// Represents the default file name for the ADR configuration file.
        /// </summary>
        /// <remarks>Use this constant when referencing or creating the ADR configuration file to ensure
        /// consistency across the application.</remarks>
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

        /// <summary>
        /// The namespace where the application's embedded resources are located, used for loading templates, configuration files, and other resources that are compiled into the application assembly. 
        /// </summary>
        public const string ResourceNamespace = "AdrPlus.Resources";

        /// <summary>
        /// The name of the directory where ADR templates are stored. 
        /// </summary>
        public const string AppConfigfileName = "adrplus.json";
        
        /// <summary>
        /// The application banner text.
        /// </summary>
        public const string BannerText = "ADR-PLUS";

        /// <summary>
        /// Separator character for joining command arguments.
        /// </summary>
        public const char CommandArgsSeparator = (char)1;

        /// <summary>
        /// The default folder path for Architecture Decision Records (ADR).
        /// </summary>
        public const string DefaultFolderAdr = @"doc/adr";

        /// <summary>
        /// The application name.
        /// </summary>
        public const string NameApp = "AdrPlus";

        /// <summary>
        /// Configuration key for application version.
        /// </summary>
        public const string CfgNameVersionApp = "VersionApp";

        /// <summary>
        /// Configuration key for command name.
        /// </summary>
        public const string CfgCommandName = "CommandName";

        /// <summary>
        /// Configuration key for command arguments.
        /// </summary>
        public const string CfgCommandArgs = "CommandArgs";

        /// <summary>
        /// Configuration root section name.
        /// </summary>
        public const string DefaultSettingsRoot = "DefaultSettings";

        /// <summary>
        /// Configuration field name for language setting.
        /// </summary>
        public const string FieldLanguage = "language";


        /// <summary>
        /// Configuration field name for behavior without arguments, which defines the default behavior of the application when no command-line arguments are provided. This setting allows users to specify what action the application should take (e.g., show help, create a new ADR, list existing ADRs) when it is run without any specific commands or options. 
        /// </summary>
        public const string FieldWithoutArgs = "withoutargs";

        /// <summary>
        /// Configuration field name for open ADR command.
        /// </summary>
        public const string FieldOpenAdr = "comandopenadr";

        /// <summary>
        /// Configuration field name for the plugin allowlist setting.
        /// </summary>
        public const string FieldPluginAllowlist = "pluginallowlist";

        /// <summary>
        /// The name of the configuration migration pattern preferences.
        /// </summary>
        public const string FieldMigrationPattern = "migrationpattern";

        /// <summary>
        /// The name of the configuration file used by the application to store folder and preferences. 
        /// </summary>
        public const string FieldFolderAdr = "folderadr";

        /// <summary>
        /// The configuration key for the template used to generate ADR filenames, allowing users to define a custom format for how ADR files are named based on their metadata. 
        /// </summary>
        public const string FieldTemplate = "template";

        /// <summary>
        /// The configuration key for the prefix used in ADR filenames, allowing users to specify a custom prefix that will be included in the generated ADR file names. 
        /// </summary>
        public const string FieldPrefix = "prefix";

        /// <summary>
        /// The configuration key for the sequence number format used in ADR filenames, allowing users to define how the sequence number is formatted (e.g., with leading zeros) in the generated ADR file names. 
        /// </summary>
        public const string FieldLenSeq = "lenseq";

        /// <summary>
        /// The configuration key for the version format used in ADR filenames, allowing users to define how the version is formatted in the generated ADR file names. 
        /// </summary>
        public const string FieldLenVersion = "lenversion";

        /// <summary>
        /// The configuration key for the revision format used in ADR filenames, allowing users to define how the revision number is formatted in the generated ADR file names. 
        /// </summary>
        public const string FieldLenRevision = "lenrevision";

        /// <summary>
        /// Obsolete repository config field (removed in 1.0.0-rc5, see ADR006): used to control whether/how much of the
        /// scope was embedded in ADR filenames. Kept only so <see cref="ObsoleteRepoConfigFields"/> can recognize and
        /// tolerate it in configs written by older versions.
        /// </summary>
        public const string FieldLenScope = "lenscope";

        /// <summary>
        /// Obsolete repository config field (removed in 1.0.0-rc5, see ADR006): the semicolon-separated whitelist of
        /// valid scope values. Kept only so <see cref="ObsoleteRepoConfigFields"/> can recognize and tolerate it in
        /// configs written by older versions.
        /// </summary>
        public const string FieldScopes = "scopes";

        /// <summary>
        /// Obsolete repository config field (removed in 1.0.0-rc5, see ADR006): whether ADRs were organized into
        /// per-scope subfolders. Kept only so <see cref="ObsoleteRepoConfigFields"/> can recognize and tolerate it in
        /// configs written by older versions.
        /// </summary>
        public const string FieldFolderByScope = "folderbyscope";

        /// <summary>
        /// Obsolete repository config field (removed in 1.0.0-rc5, see ADR006): the scopes for which the domain
        /// prompt/segment was skipped. Kept only so <see cref="ObsoleteRepoConfigFields"/> can recognize and tolerate
        /// it in configs written by older versions.
        /// </summary>
        public const string FieldSkipDomain = "skipdomain";

        /// <summary>
        /// Repository config field names removed in 1.0.0-rc5 (ADR006: Scope/Domain became free-text header-only
        /// fields, no longer governed). <see cref="ValidateConfig.ValidateRepoStructure"/> tolerates these when
        /// present in a config written by an older version instead of reporting them as unexpected fields; they are
        /// dropped the next time that config is rewritten (host default on version-bump migration, or a repository
        /// config explicitly rewritten via <c>adrplus config --repository</c>), since the current
        /// <see cref="AdrPlusRepoConfig"/> type no longer declares them.
        /// </summary>
        public static readonly IReadOnlySet<string> ObsoleteRepoConfigFields = new HashSet<string>(
            [FieldLenScope, FieldScopes, FieldFolderByScope, FieldSkipDomain],
            StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The configuration key for the separator used to join multiple text values into a single string for storage in the configuration file, allowing users to specify a custom separator character or string.
        /// </summary>
        public const string FieldSeparator = "separator";

        /// <summary>
        /// The configuration key for the case transformation applied to certain fields in the generated ADR content, allowing users to specify how text should be transformed (e.g., to camelCase, PascalCase, snake_case, kebab-case) in the generated ADRs. 
        /// </summary>
        public const string FieldCaseTransform = "casetransform";

        /// <summary>
        /// The configuration keys for the different status values that can be assigned to ADRs, allowing users to define custom status values for Pupose.
        /// </summary>
        public const string FieldStatusNew = "statusnew";

        /// <summary>
        /// The configuration keys for the different status values that can be assigned to ADRs, allowing users to define custom status values for Accepted.
        /// </summary>
        public const string FieldStatusAccepted = "statusacc";

        /// <summary>
        /// The configuration keys for the different status values that can be assigned to ADRs, allowing users to define custom status values for Rejected.
        /// </summary>
        public const string FieldStatusRejected = "statusrej";

        /// <summary>
        /// The configuration keys for the different status values that can be assigned to ADRs, allowing users to define custom status values for Superseded.
        /// </summary>
        public const string FieldStatusSuperseded = "statussup";

        /// <summary>
        /// The configuration key for the disclaimer text that can be included in the header of generated ADR files, allowing users to specify a custom disclaimer message that will be included at the top of each generated ADR file.
        /// </summary>
        public const string FieldHeaderDisclaimer = "headerdisclaimer";

        /// <summary>
        /// The configuration key for the version text that can be included in the header of generated ADR files, allowing users to specify custom status information that will be included in the header section of each generated ADR file. 
        /// </summary>
        public const string FieldHeaderVersion = "headerversion";


        /// <summary>
        /// The configuration key for the revision text that can be included in the header of generated ADR files, allowing users to specify custom status information that will be included in the header section of each generated ADR file. 
        /// </summary>
        public const string FieldHeaderRevision = "headerrevision";

        /// <summary>
        /// The configuration key for the title file text that can be included in the header of generated ADR files, allowing users to specify custom title file information that will be included in the header section of each generated ADR file. 
        /// </summary>
        public const string FieldHeaderTitleFile = "headertitlefile";

        /// <summary>
        /// The configuration key for the domain text that can be included in the header of generated ADR files, allowing users to specify custom domain information that will be included in the header section of each generated ADR file. 
        /// </summary>
        public const string FieldHeaderDomain = "headerdomain";

        /// <summary>
        /// The configuration key for the scope text that can be included in the header of generated ADR files, allowing users to specify custom scope information that will be included in the header section of each generated ADR file. 
        /// </summary>
        public const string FieldHeaderScope = "headerscope";

        /// <summary>
        /// The configuration key for the title status created text that can be included in the header of generated ADR files, allowing users to specify custom title status created information that will be included in the header section of each generated ADR file. 
        /// </summary>
        public const string FieldHeaderStatusCreated = "headertitlestatuscreated";

        /// <summary>
        /// The configuration key for the title status changed text that can be included in the header of generated ADR files, allowing users to specify custom title status changed information that will be included in the header section of each generated ADR file. 
        /// </summary>
        public const string FieldHeaderStatusChanged = "headertitlestatuschanged";

        /// <summary>
        /// The configuration key for the title status superseded text that can be included in the header of generated ADR files, allowing users to specify custom title status superseded information that will be included in the header section of each generated ADR file. 
        /// </summary>
        public const string FieldHeaderStatusSuperseded = "headertitlestatussuperseded";

        /// <summary>
        /// The configuration key for the table fields text that can be included in the header of generated ADR files, allowing users to specify custom table fields information that will be included in the header section of each generated ADR file. 
        /// </summary>
        public const string FieldHeaderTableFields = "headertablefields";

        /// <summary>
        /// The configuration key for the table values text that can be included in the header of generated ADR files, allowing users to specify custom table values information that will be included in the header section of each generated ADR file. 
        /// </summary>
        public const string FieldHeaderTableValues = "headertablevalues";

        /// <summary>
        /// The configuration key for the migrated text that can be included in the header of generated ADR files, allowing users to specify custom migrated information that will be included in the header section of each generated ADR file.
        /// </summary>
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

        /// <summary>
        /// Neutral resource language for the assembly, defaulting to 'en-us'.
        /// </summary>
        private static readonly Lazy<string> neutralLanguage = new(() =>
        {
            var assembly = Assembly.GetExecutingAssembly();
            var attribute = assembly.GetCustomAttribute<NeutralResourcesLanguageAttribute>();
            return attribute?.CultureName ?? "en-us";
        }); 

    }
}
