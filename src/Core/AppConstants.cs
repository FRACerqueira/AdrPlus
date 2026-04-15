// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using PromptPlusLibrary;
using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Resources;

namespace AdrPlus.Core
{
    internal static class AppConstants
    {
        public const string AppConfigfileName = "adrplus.json";
        /// <summary>
        /// The application banner text.
        /// </summary>
        public const string BannerText = "ADR-PLUS";

        /// <summary>
        /// Folder name for storing application history data.
        /// </summary>
        public const string Folderhistory = "AdrPlus.History";

        /// <summary>
        /// Separator character for joining command arguments.
        /// </summary>
        public const char CommandArgsSeparator = (char)1;

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
        /// Console color for help messages.
        /// </summary>
        public const string ColorHelp = "skyblue2";

        /// <summary>
        /// Console color for welcome template messages.
        /// </summary>
        public const string ColorWelcomeTemplate = "yellow";

        /// <summary>
        /// Console color for the welcome banner.
        /// </summary>
        public static Color ColorWelcomeBanner => Color.DarkOrange;

        /// <summary>
        /// Console color for error messages.
        /// </summary>
        public const string ColorError = "red";

        /// <summary>
        /// Console color for informational messages.
        /// </summary>
        public const string ColorInfo = "grey";

        /// <summary>
        /// Console color for command results.
        /// </summary>
        public const string ColorResult = "white";

        /// <summary>
        /// Console color for warning messages.
        /// </summary>
        public const string ColorWarning = "gold1";

        /// <summary>
        /// Console color for summary messages.
        /// </summary>  
        public const string ColorSummary = "navajowhite1";

        /// <summary>
        /// Configuration root section name.
        /// </summary>
        public const string DefaultSettingsRoot = "DefaultSettings";

        /// <summary>
        /// Configuration field name for language setting.
        /// </summary>
        public const string FieldLanguage = "language";


        /// <summary>
        /// Configuration field name for open ADR command.
        /// </summary>
        public const string FieldOpenAdr = "comandopenadr";

        /// <summary>
        /// The name of the configuration file used by the application to store folder and preferences. 
        /// </summary>
        public const string FieldFolderRepo = "folderrepo";

        /// <summary>
        /// The name of the configuration file used by the application to store yes value and preferences. 
        /// </summary>
        public const string FieldYesValue = "yesvalue";

        /// <summary>
        /// The name of the configuration file used by the application to store no value and preferences. 
        /// </summary>
        public const string FieldNoValue = "novalue";

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
        /// The configuration key for the scope format used in ADR filenames, allowing users to define how the scope is formatted in the generated ADR file names.
        /// </summary>
        public const string FieldLenScope = "lenscope";

        /// <summary>
        /// The configuration key for the domain format used in ADR filenames, allowing users to define how the domain is formatted in the generated ADR file names.
        /// </summary>
        public const string FieldScopes = "scopes";

        /// <summary>
        /// The configuration key for the folder organization strategy based on ADR scope, allowing users to specify how ADR files should be organized into folders based on their scope.
        /// </summary>
        public const string FieldFolderByScope = "folderbyscope";

        /// <summary>
        /// Represents the field name for skipping domain information. Use scope values to determine which domain to skip in the generated ADR content, allowing users to customize the level of detail included in the ADRs based on their scope.
        /// </summary>
        public const string FieldSkipDomain = "skipdomain";

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
        /// The configuration key for the status text that can be included in the header of generated ADR files, allowing users to specify custom status information that will be included in the header section of each generated ADR file. 
        /// </summary>
        public const string FieldHeaderStatus = "headerstatus";

        /// <summary>
        /// The configuration key for the version text that can be included in the header of generated ADR files, allowing users to specify custom status information that will be included in the header section of each generated ADR file. 
        /// </summary>
        public const string FieldHeaderVersion = "headerversion";


        /// <summary>
        /// The configuration key for the revision text that can be included in the header of generated ADR files, allowing users to specify custom status information that will be included in the header section of each generated ADR file. 
        /// </summary>
        public const string FieldHeaderRevision = "headerrevision";

        /// <summary>
        /// A frozen dictionary mapping configuration field names to their corresponding display titles, used for presenting user-friendly titles in the application's user interface when displaying configuration settings.
        /// Uses FrozenDictionary for optimal read performance.
        /// </summary>
        private static  FrozenDictionary<string, string> TitleFields => new Dictionary<string, string>
        {
                { FieldLanguage, Resources.AdrPlus.FieldTitleLanguage },
                { FieldOpenAdr, Resources.AdrPlus.FieldTitleOpenAdr },
                { FieldYesValue, Resources.AdrPlus.FieldTitleYesValue },
                { FieldNoValue, Resources.AdrPlus.FieldTitleNoValue },
                { FieldFolderRepo, Resources.AdrPlus.FieldTitleFolderRepo },
                { FieldTemplate, Resources.AdrPlus.FieldTitleTemplate },
                { FieldPrefix, Resources.AdrPlus.FieldTitlePrefix },
                { FieldLenSeq, Resources.AdrPlus.FieldTitleLenSeq },
                { FieldLenVersion, Resources.AdrPlus.FieldTitleLenVersion },
                { FieldLenRevision, Resources.AdrPlus.FieldTitleLenRevision },
                { FieldLenScope, Resources.AdrPlus.FieldTitleLenScope },
                { FieldScopes, Resources.AdrPlus.FieldTitleScopes },
                { FieldFolderByScope, Resources.AdrPlus.FieldTitleFolderByScope },
                { FieldSkipDomain, Resources.AdrPlus.FieldTitleSkipDomain },
                { FieldSeparator, Resources.AdrPlus.FieldTitleSeparator },
                { FieldCaseTransform, Resources.AdrPlus.FieldTitleCaseTransform },
                { FieldStatusNew, Resources.AdrPlus.FieldTitleStatusNew },
                { FieldStatusAccepted, Resources.AdrPlus.FieldTitleStatusAccepted },
                { FieldStatusRejected, Resources.AdrPlus.FieldTitleStatusRejected },
                { FieldStatusSuperseded, Resources.AdrPlus.FieldTitleStatusSuperseded },
                { FieldHeaderDisclaimer, Resources.AdrPlus.FieldTitleHeaderDisclaimer },
                { FieldHeaderStatus, Resources.AdrPlus.FieldTitleHeaderStatus },
                { FieldHeaderVersion, Resources.AdrPlus.FieldTitleHeaderVersion },
                { FieldHeaderRevision, Resources.AdrPlus.FieldTitleHeaderRevision },
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the display title for a given configuration field name, returning a user-friendly title from resources if available, or the original field name if no title is defined. 
        /// </summary>
        /// <param name="name">
        /// The configuration field name for which to retrieve the display title. This should correspond to one of the defined configuration keys in AppConstants, such as "folderrepo", "dateformat", etc. 
        /// </param>
        /// <returns>The display title for the specified configuration field name.</returns>
        public static string GetTitleField(string name)
        {
            return TitleFields.TryGetValue(name.ToLowerInvariant(), out var title) ? title : name;
        }

        /// <summary>
        /// Gets the cached JSON serializer options used for serializing and deserializing repository data, configured to ignore read-only fields, write indented JSON, be case-insensitive for property names, and include a converter for string enums. 
        /// </summary>
        private static readonly JsonSerializerOptions _repoSerializerOptions = new()
        {
            IgnoreReadOnlyFields = true,
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = new LowercaseNamingPolicy(),
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Gets the cached JSON serializer options used for serializing and deserializing repository data.
        /// </summary>
        public static JsonSerializerOptions RepoSerializerOptions => _repoSerializerOptions;

        /// <summary>
        /// Gets the cached JSON document options used for parsing JSON configuration files, configured to allow trailing commas and skip comments in the JSON content. 
        /// </summary>
        private static readonly JsonDocumentOptions _documentOptions = new()
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        };

        /// <summary>
        /// Gets the cached JSON document options used for parsing JSON configuration files.
        /// </summary>
        public static JsonDocumentOptions DocumentOptions => _documentOptions;

        private static readonly Lazy<string> _neutralLanguage = new(() =>
        {
            var assembly = Assembly.GetExecutingAssembly();
            var attribute = assembly.GetCustomAttribute<NeutralResourcesLanguageAttribute>();
            return attribute?.CultureName ?? "en-us";
        });

        /// <summary>
        /// Gets the neutral language culture name from the assembly's NeutralResourcesLanguageAttribute.
        /// Returns "en-us" as the default if the attribute is not found.
        /// The result is cached after the first call.
        /// </summary>
        /// <returns>The neutral language culture name.</returns>
        public static string GetNeutralLanguage() => _neutralLanguage.Value;
    }
}
