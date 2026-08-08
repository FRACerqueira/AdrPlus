# Graph Report - AdrPlus  (2026-08-08)

## Corpus Check
- 288 files · ~214,250 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 3063 nodes · 10909 edges · 177 communities (88 shown, 89 thin omitted)
- Extraction: 71% EXTRACTED · 29% INFERRED · 0% AMBIGUOUS · INFERRED: 3114 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `9218a80b`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- NewAdrCommandHandlerTests
- ValidateJsonConfigTests
- HelperTests
- .ParseArgs
- FileSystemService
- PatternParserTests
- PluginManifest
- .ReadAllTextAsync
- .PromptSelectLogicalDrive
- AdrServiceTests
- .DirectoryExists
- .ParseFileName
- .ExecuteAsync
- FieldsJson
- IConfigPrompts
- AdrFileNameComponentsTests
- PluginsCommandHandlerTests
- PluginManagerRetryTests
- .PromptClearWaitText
- AdrPlusRepoConfigTests
- AdrEventContext record
- ValidateConfig
- .StatusUpdateAdrAsync
- AdrPlus.Domain
- CaseFormat & StringCaseExtensions
- HelpCommandHandlerTests
- UndoStatusCommandHandlerTests
- AdrPlus.Abstractions README (plain text)
- PluginManager
- .StatusChangeAdrAsync
- IValidateConfig
- AdrPlus.Infrastructure.FileSystem
- .RouteAsync
- IConsoleWriter
- AdrPlus.Infrastructure.Formatting
- HelpUsageAttributeTests
- IPluginLogger
- AdrRecordTests
- PromptConsole
- .ExecuteAsync
- CommandArgumentAttributeTests
- AdrPlus Csproj/NuGet Package Metadata
- LogMessages
- ConfigCommandHandler
- .LogCommandSuccessful
- MainProgram
- AdrHeaderTests
- PluginManagerBackfillTests
- TemplateResourcesTests
- CommandAttribute Tests
- IPluginManager
- .FileExists
- AdrPlus.Infrastructure.UI
- PathHelper
- .PromptWriteSuccess
- AdrPlus.Plugins
- AdrIndexerPlugin
- PluginManagerTests
- Alexandrian & Business Case ADR Templates
- Program Entry Point (+Tests)
- AdrPlusRepoConfig
- .PromptShowPluginsListTable
- NewAdrCommandHandler
- AdrRecordSnapshot
- .BuildAdrKey
- LowercaseNamingPolicyTests
- .Resolve
- PluginManagerDispatchTests
- SyncCommandHandler
- .ReadHistoryAsync
- .OnAdrEventAsync
- AdrPluginBaseTests
- .GetCommands
- Test Architecture Guide
- IAdrPlugin
- Tyree-Ackerman ADR Template (English)
- IAdrServices
- .PromptWriteError
- PluginManagerDisposalTests
- CommandAttribute
- AppConstants
- IPromptConsole (shared UI abstraction, #1 god node)
- AdrPlusConfig
- .GetHelpText
- .PromptEditTitleAdr
- .CreateHandler
- .ComputeDelay
- AdrStatusTests
- .InvokeOnceAsync
- PluginLoader.cs
- IFileSystemService
- ItemMenuWizardTests
- SatelliteResourcesTests
- FormatMessages
- AdrPlus
- .ExecuteAsync
- AttemptLoopOutcome.cs
- graphify Skill Trigger Reference
- Bug report issue template
- plugin.json Manifest Schema
- MADR Template (English/Neutral)
- Nygard ADR Template (English)
- CI Build & Test Workflow
- Protect Main Branch Workflow
- Release AdrPlus.Abstractions Workflow
- Release AdrPlus CLI Workflow
- CHANGELOG - 1.0.0-beta1 (command renames)
- CHANGELOG - 1.0.0-beta3 (fresh-install fixes)
- CHANGELOG - 1.0.0-beta5
- Pull request template
- AdrPlus Icon
- AdrPlus App Icon: Gear + ADR Document with Plus Sign
- NuGet Package README Overview
- adrKey vs Adr.Number Identity Rules
- AdrPluginBase Convenience Class
- Plugin Secrets Handling Guidance
- Alexandrian ADR Template — German translation (machine-translated, pending review)
- Alexandrian ADR Template — Spanish translation (machine-translated, pending review)
- Alexandrian ADR Template — French translation (machine-translated, pending review)
- Alexandrian ADR Template — Italian translation (machine-translated, pending review)
- Alexandrian ADR Template — Japanese translation (machine-translated, pending review)
- Alexandrian ADR Template — Korean translation (machine-translated, pending review)
- Alexandrian ADR Template — Dutch translation (machine-translated, pending review)
- Alexandrian ADR Template — Portuguese (Brazil) translation
- Alexandrian ADR Template — Russian translation (machine-translated, pending review)
- Alexandrian ADR Template — Chinese (Simplified) translation (machine-translated, pending review)
- Business Case ADR Template — German translation (machine-translated, pending review)
- Business Case ADR Template — Spanish translation (machine-translated, pending review)
- Business Case ADR Template — French translation (machine-translated, pending review)
- Business Case ADR Template — Italian translation (machine-translated, pending review)
- Business Case ADR Template — Japanese translation (machine-translated, pending review)
- Business Case ADR Template — Korean translation (machine-translated, pending review)
- Business Case ADR Template — Dutch translation (machine-translated, pending review)
- Business Case ADR Template — Portuguese (Brazil) translation
- Business Case ADR Template — Russian translation (machine-translated, pending review)
- Business Case ADR Template — Chinese (Simplified) translation (machine-translated, pending review)
- MADR Template (German)
- MADR Template (Spanish)
- MADR Template (French)
- MADR Template (Italian)
- MADR Template (Japanese)
- MADR Template (Korean)
- MADR Template (Dutch)
- MADR Template (Portuguese - Brazil)
- MADR Template (Russian)
- MADR Template (Chinese - Simplified)
- Merson Template (German)
- Merson Template (Spanish)
- Merson Template (French)
- Merson Template (Italian)
- Merson Template (Japanese)
- Merson Template (Korean)
- Merson Template (Dutch)
- Merson Template (Portuguese - Brazil)
- Merson Template (Russian)
- Merson Template (Chinese - Simplified)
- Nygard ADR Template (German)
- Nygard ADR Template (Spanish)
- Nygard ADR Template (French)
- Nygard ADR Template (Italian)
- Nygard ADR Template (Japanese)
- Nygard ADR Template (Korean)
- Nygard ADR Template (Dutch)
- Nygard ADR Template (Portuguese-Brazil)
- Nygard ADR Template (Russian)
- Nygard ADR Template (Chinese)
- Planguage Requirement Template (German)
- Planguage Requirement Template (Spanish)
- Planguage Requirement Template (French)
- Planguage Requirement Template (Italian)
- Planguage Requirement Template (Japanese)
- Planguage Requirement Template (Korean)
- Planguage Requirement Template (Dutch)
- Planguage Requirement Template (Portuguese-Brazil)
- Planguage Requirement Template (Russian)
- Planguage Requirement Template (Chinese)

## God Nodes (most connected - your core abstractions)
1. `ValidateJsonConfigTests` - 105 edges
2. `IFileSystemService` - 102 edges
3. `PromptConsole` - 93 edges
4. `AdrPlus.Domain` - 84 edges
5. `AdrServiceTests` - 81 edges
6. `IConsoleWriter` - 78 edges
7. `AdrPlus.Core` - 70 edges
8. `HelperTests` - 68 edges
9. `AdrPlusRepoConfig` - 67 edges
10. `ReviseCommandHandlerTests` - 61 edges

## Surprising Connections (you probably didn't know these)
- `AdrPlus.Abstractions README (Markdown)` --semantically_similar_to--> `AdrPlus.Abstractions README (plain text)`  [INFERRED] [semantically similar]
  AbstractionsREADME.md → AbstractionsREADME.txt
- `plugin.json manifest` --semantically_similar_to--> `plugin.json manifest (settings block)`  [INFERRED] [semantically similar]
  AbstractionsREADME.md → SECURITY.md
- `Host-global plugin storage (%UserProfile%/AdrPlus.Plugins/<name>/)` --semantically_similar_to--> `Host-global plugin storage (%UserProfile%/AdrPlus.Plugins/<name>/)`  [INFERRED] [semantically similar]
  AbstractionsREADME.md → AbstractionsREADME.txt
- `Host-global plugin storage (%UserProfile%/AdrPlus.Plugins/<name>/)` --semantically_similar_to--> `Host-global plugin storage (%UserProfile%/AdrPlus.Plugins/<name>/)`  [INFERRED] [semantically similar]
  AbstractionsREADME.md → SECURITY.md
- `Host-global plugin storage (%UserProfile%/AdrPlus.Plugins/<name>/)` --semantically_similar_to--> `Host-global plugin storage (%UserProfile%/AdrPlus.Plugins/)`  [INFERRED] [semantically similar]
  AbstractionsREADME.md → StepByStepGuide.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Host-global plugin storage model described across docs** — abstractionsreadme_hostglobalpluginstorage, abstractionsreadme_txt_hostglobalpluginstorage, security_hostglobalpluginstorage, stepbystepguide_hostglobalpluginstorage [INFERRED 0.85]
- **Plugin manifest and per-repository activation config flow** — abstractionsreadme_pluginjsonmanifest, security_pluginjsonmanifest, abstractionsreadme_adrconfigadrplus, stepbystepguide_adrconfigadrplus [INFERRED 0.75]
- **Plugin System: Design, Storage Pivot, Contract, and Release** — doc_adr_adr002v01_add_a_plugin_system_for_adr_lifecycle_event_integrations_decision, doc_adr_adr003v01_store_plugin_binaries_host_globally_instead_of_per_repository_decision, plugindevelopmentguide_iadrplugin_contract, changelog_unreleased [INFERRED 0.85]
- **11-Language UI and ADR Template Localization** — claude_project_status, translations_status, doc_adr_adr001v01_select_adr_templates_based_on_configured_ui_language_decision, changelog_beta4 [INFERRED 0.85]
- **Core plugin contract and event/result payload types** — doc_api_abstractions_adrplus_abstractions_iadrplugin_iadrplugin, doc_api_abstractions_adrplus_abstractions_adreventcontext_adreventcontext, doc_api_abstractions_adrplus_abstractions_pluginresult_pluginresult, doc_api_abstractions_adrplus_abstractions_adrpluginbase_adrpluginbase [EXTRACTED 1.00]
- **Plugin-author testing factory helpers** — doc_api_abstractions_adrplus_abstractions_testing_adreventcontextfactory_adreventcontextfactory, doc_api_abstractions_adrplus_abstractions_testing_adrrecordsnapshotfactory_adrrecordsnapshotfactory, doc_api_abstractions_adrplus_abstractions_testing_repoinfosnapshotfactory_repoinfosnapshotfactory [EXTRACTED 1.00]
- **Shared abstraction god-nodes referenced across most command handlers** — graphify_out_memory_query_20260727_174354_why_does_ipromptconsole_connect_migration_wizard_c_ipromptconsole, graphify_out_memory_query_20260727_175809_why_does_ifilesystemservice_connect_adr_header_par_ifilesystemservice, graphify_out_memory_query_20260727_175023_should_init_command_handling_be_split_into_smaller_initcommandhandler [INFERRED 0.85]
- **ADR Template & Index Ecosystem** — src_adrplus_plugins_adrindexer_indexadrs_template_adr_index, src_adrplus_resources_alexandrian_template_overview, src_adrplus_resources_business_case_template_overview [INFERRED 0.75]
- **Mock Helper Selection Strategy (Generic vs Domain-Specific vs Fixture)** — tests_test_architecture_commandhandlermockhelper, tests_test_architecture_supersedecommandhandlermockhelper, tests_test_architecture_explorecommandhandlerfixture [EXTRACTED 1.00]
- **ADR Structural Conceptual Framework (Template, REMAP, Ontology)** — src_adrplus_resources_tyree_ackerman_template, src_adrplus_resources_tyree_ackerman_template_remap_metamodel, src_adrplus_resources_tyree_ackerman_template_kyaruzi_van_katwijk_ontology [EXTRACTED 1.00]

## Communities (177 total, 89 thin omitted)

### Community 0 - "NewAdrCommandHandlerTests"
Cohesion: 0.17
Nodes (8): CancellationToken, Task, NewAdrCommandHandlerTests, Dictionary, Fact, ILogger, string, Task

### Community 1 - "ValidateJsonConfigTests"
Cohesion: 0.07
Nodes (4): ValidateJsonConfigTests, Dictionary, Fact, Task

### Community 2 - "HelperTests"
Cohesion: 0.05
Nodes (17): bool, date, Helper, DateTime, error, GeneratedRegex, int, JsonElement (+9 more)

### Community 3 - ".ParseArgs"
Cohesion: 0.13
Nodes (7): Task, ErrorReport, FilePathAdrTemplate, ConfigCommandHandlerTests, Fact, ILogger, Task

### Community 4 - "FileSystemService"
Cohesion: 0.06
Nodes (20): AdrPlus.Tests.Infrastructure.FileSystem, FileSystemService, CancellationToken, IEnumerable, JsonSerializerOptions, Result, SearchOption, Success (+12 more)

### Community 5 - "PatternParserTests"
Cohesion: 0.06
Nodes (20): editorcmd, hasRider, hasVisualStudio, hasVSCode, Length, Position, PatternParser, Dictionary (+12 more)

### Community 6 - "PluginManifest"
Cohesion: 0.06
Nodes (32): IDisposable, PluginAllowlistEntry, ManifestValidationOutcome, PluginLoader, PluginLoadOutcome, PluginRejection, Action, CancellationToken (+24 more)

### Community 7 - ".ReadAllTextAsync"
Cohesion: 0.19
Nodes (9): Dictionary, ConfirmYes, Content, DateTime, Func, info, IsAborted, left (+1 more)

### Community 8 - ".PromptSelectLogicalDrive"
Cohesion: 0.12
Nodes (18): Adrfiles, ArgsWizard, MigrateCommandHandler, Arguments, CancellationToken, Dictionary, Func, IEnumerable (+10 more)

### Community 9 - "AdrServiceTests"
Cohesion: 0.12
Nodes (3): AdrServiceTests, Fact, IConfiguration

### Community 10 - ".DirectoryExists"
Cohesion: 0.17
Nodes (16): InitCommandHandler, Arguments, CancellationToken, Dictionary, ILogger, List, Task, MaxNumber (+8 more)

### Community 11 - ".ParseFileName"
Cohesion: 0.08
Nodes (18): CancellationToken, Task, CancellationToken, Task, Task, ReviseCommandHandlerTests, Dictionary, Fact (+10 more)

### Community 12 - ".ExecuteAsync"
Cohesion: 0.25
Nodes (4): SupersedeCommandHandlerTests, Fact, ILogger, Task

### Community 13 - "FieldsJson"
Cohesion: 0.21
Nodes (4): FieldsJson, JsonValueKind, Content, IEnumerable

### Community 14 - "IConfigPrompts"
Cohesion: 0.17
Nodes (11): JsonConfig, PrefixValue, IConfigPrompts, CancellationToken, Content, FieldsFromFileAdr, IEnumerable, IsAborted (+3 more)

### Community 16 - "PluginsCommandHandlerTests"
Cohesion: 0.05
Nodes (43): Config, ConfigPath, PluginsCommandHandler, Allowlist, Arguments, CancellationToken, Detail, Dictionary (+35 more)

### Community 17 - "PluginManagerRetryTests"
Cohesion: 0.14
Nodes (20): PendingEntry, DateTime, PendingStateStore, CancellationToken, List, string, Task, PendingStateStoreTests (+12 more)

### Community 19 - "AdrPlusRepoConfigTests"
Cohesion: 0.10
Nodes (5): AdrPlusRepoConfigTests, Fact, string, PluginSnapshotExtensionsTests, Fact

### Community 20 - "AdrEventContext record"
Cohesion: 0.09
Nodes (42): CHANGELOG - 1.0.0-beta4 (11-language localization), CHANGELOG - Unreleased (Plugin System), CLAUDE.md Coding Principles (Think/Simplicity/Surgical/Goal-Driven), CLAUDE.md Current Project Status (beta3, 11 languages), Code of Conduct, Contributing Guide, ADR001: Select ADR Templates Based on Configured UI Language, ADR002: Add a Plugin System for ADR Lifecycle Event Integrations (+34 more)

### Community 21 - "ValidateConfig"
Cohesion: 0.10
Nodes (13): JsonNode, ValidateConfig, CancellationToken, Dictionary, ErrorReport, IConfiguration, IsValid, JsonElement (+5 more)

### Community 22 - ".StatusUpdateAdrAsync"
Cohesion: 0.05
Nodes (38): ApproveCommandHandler, Arguments, CancellationToken, DateTime, Dictionary, ILogger, Task, RejectCommandHandler (+30 more)

### Community 23 - "AdrPlus.Domain"
Cohesion: 0.11
Nodes (8): AdrPlus.Tests.Commands.Plugins, AdrPlus.Domain, AdrPlus.Commands.Plugins, AdrPlus.Tests.Extensions, AdrPlus.Tests.Domain, AdrPlus.Tests.Core, AdrPlus.Core, RepoActions

### Community 24 - "CaseFormat & StringCaseExtensions"
Cohesion: 0.09
Nodes (12): CaseFormat, StringCaseExtensions, GeneratedRegex, Regex, StringCaseExtensionsTests, Fact, InlineData, Theory (+4 more)

### Community 25 - "HelpCommandHandlerTests"
Cohesion: 0.13
Nodes (15): ICommandHandler, HelpCommandHandler, CancellationToken, CommandRouter, ILogger, Task, Alias, Command (+7 more)

### Community 26 - "UndoStatusCommandHandlerTests"
Cohesion: 0.16
Nodes (10): UndoStatusCommandHandler, Arguments, CancellationToken, ILogger, Task, UndoStatusCommandHandlerTests, Dictionary, Fact (+2 more)

### Community 27 - "AdrPlus.Abstractions README (plain text)"
Cohesion: 0.08
Nodes (38): AdrPlus.Abstractions README (Markdown), adr-config.adrplus (activeplugins/disableplugins), AdrEventContext, AdrPluginBase base class, doc/api-abstractions API reference, Host-global plugin storage (%UserProfile%/AdrPlus.Plugins/<name>/), IAdrPlugin interface, PluginDevelopmentGuide.md (external reference) (+30 more)

### Community 28 - "PluginManager"
Cohesion: 0.16
Nodes (20): HashSet, AdrEventContext, Func, AdrEventType, LoadedPlugin, PluginManager, Adr, CancellationToken (+12 more)

### Community 29 - ".StatusChangeAdrAsync"
Cohesion: 0.13
Nodes (7): Content, CancellationToken, DateTime, Error, Isvalid, Record, IsValid

### Community 30 - "IValidateConfig"
Cohesion: 0.12
Nodes (15): IValidateConfig, CancellationToken, Task, ConfigVersionManager, CancellationToken, GeneratedRegex, IConfiguration, ILogger (+7 more)

### Community 31 - "AdrPlus.Infrastructure.FileSystem"
Cohesion: 0.11
Nodes (14): AdrPlus.Tests.Commands.Reject, AdrPlus.Tests.Commands.NewAdr, AdrPlus.Tests.Commands.Explore, AdrPlus.Tests.Commands.Attributes, AdrPlus.Tests.Commands.Approve, AdrPlus.Tests.Commands.Revise, AdrPlus.Tests.Helpers, AdrPlus.Infrastructure.FileSystem (+6 more)

### Community 32 - ".RouteAsync"
Cohesion: 0.10
Nodes (21): NotImplementedException, CancellationToken, Task, WizardCommandHandler, Arguments, CancellationToken, CommandRouter, CommandsAdr (+13 more)

### Community 33 - "IConsoleWriter"
Cohesion: 0.17
Nodes (12): IConsoleWriter, Allowlist, CancellationToken, Events, IReadOnlyList, IReadOnlySet, Name, Pending (+4 more)

### Community 34 - "AdrPlus.Infrastructure.Formatting"
Cohesion: 0.09
Nodes (18): AdrPlus.Commands.Migrate, AdrPlus.Extensions, AdrPlus.Commands.UndoStatus, AdrPlus.Commands.Config, AdrPlus.Commands.Explore, AdrPlus.Infrastructure.Logging, AdrPlus.Commands.Revise, AdrPlus.Tests.Commands.Supersede (+10 more)

### Community 36 - "IPluginLogger"
Cohesion: 0.25
Nodes (4): IPluginContext, IPluginLogger, Exception, HostPluginContext

### Community 37 - "AdrRecordTests"
Cohesion: 0.14
Nodes (6): CancellationToken, Task, AdrRecord, DateTime, AdrRecordTests, Fact

### Community 38 - "PromptConsole"
Cohesion: 0.05
Nodes (29): Color, FrozenDictionary, PluginsWizardMode, SyncWizardMode, PromptConsole, CancellationToken, ConfirmYes, CountSelected (+21 more)

### Community 39 - ".ExecuteAsync"
Cohesion: 0.20
Nodes (5): Dictionary, SyncCommandHandlerTests, Fact, ILogger, Task

### Community 41 - "AdrPlus Csproj/NuGet Package Metadata"
Cohesion: 0.05
Nodes (36): DefaultDocumentation (1.2.5), Microsoft.Extensions.Hosting (10.0.10), PromptPlus (6.0.0-rc1), Serilog.Extensions.Logging.File (3.0.0), net10.0, net8.0, net9.0, Microsoft.NET.Sdk (+28 more)

### Community 42 - "LogMessages"
Cohesion: 0.13
Nodes (9): LoggerMessage, LogMessages, Exception, ILogger, CancellationToken, Task, HostPluginLogger, Exception (+1 more)

### Community 43 - "ConfigCommandHandler"
Cohesion: 0.15
Nodes (11): EditField, ConfigCommandHandler, Arguments, CancellationToken, Func, ILogger, IsAborted, JsonElement (+3 more)

### Community 44 - ".LogCommandSuccessful"
Cohesion: 0.14
Nodes (16): Fields, IOptions, ExploreCommandHandler, Arguments, CancellationToken, Dictionary, ILogger, Task (+8 more)

### Community 45 - "MainProgram"
Cohesion: 0.10
Nodes (16): IOptionsMonitor, IServiceProvider, CommandRouter, Dictionary, IConfiguration, ILogger, Type, IMainProgram (+8 more)

### Community 47 - "PluginManagerBackfillTests"
Cohesion: 0.30
Nodes (9): PluginManagerBackfillTests, Adr, EventType, Fact, FilePath, Func, GetContent, ILogger (+1 more)

### Community 48 - "TemplateResourcesTests"
Cohesion: 0.10
Nodes (14): AdrPlus.Tests.Localization, CultureData, Action, TheoryData, HeaderLocalizationTests, InlineData, MemberData, Theory (+6 more)

### Community 50 - "IPluginManager"
Cohesion: 0.23
Nodes (11): IPluginManager, Adr, CancellationToken, Content, EventType, FilePath, Func, GetContent (+3 more)

### Community 52 - "AdrPlus.Infrastructure.UI"
Cohesion: 0.10
Nodes (9): AdrPlus.Infrastructure.UI, AdrPlus.Tests.Commands, AdrPlus.Infrastructure.Configuration, AdrPlus.Tests.Commands.Help, AdrPlus.Commands.Init, AdrPlus, AdrPlus.Commands.Help, AdrPlus.Tests.Commands.Init (+1 more)

### Community 53 - "PathHelper"
Cohesion: 0.09
Nodes (7): ExploreCommandHandlerTests, Fact, Task, ExploreCommandHandlerMockHelper, DateTime, Dictionary, PathHelper

### Community 54 - ".PromptWriteSuccess"
Cohesion: 0.17
Nodes (5): VersionCommandHandler, Arguments, DateTime, Dictionary, ILogger

### Community 55 - "AdrPlus.Plugins"
Cohesion: 0.13
Nodes (9): AdrPlus.Tests.Commands.Sync, AdrPlus.Plugins, AdrPlus.Abstractions, AdrPlus.Abstractions.Domain, AdrPlus.Plugins.AdrIndexer, AdrPlus.Commands.Sync, AdrPlus.Tests.Plugins, AdrPlus.Abstractions.Tests (+1 more)

### Community 56 - "AdrIndexerPlugin"
Cohesion: 0.21
Nodes (9): Link, AdrIndexerPlugin, CancellationToken, List, Status, string, Task, Title (+1 more)

### Community 57 - "PluginManagerTests"
Cohesion: 0.36
Nodes (6): SearchOption, PluginManagerTests, Fact, ILogger, string, Task

### Community 58 - "Alexandrian & Business Case ADR Templates"
Cohesion: 0.12
Nodes (18): ADR Index Template, Considered Options, Context and Problem Statement, Deciders Section, Decision Drivers, Decision Outcome (with Positive/Negative Consequences), Links (ADR Cross-References), Alexandrian ADR Template (MADR-format, en-US canonical) (+10 more)

### Community 59 - "Program Entry Point (+Tests)"
Cohesion: 0.19
Nodes (8): CancellationTokenSource, AdrPlus.Tests, Program, Assembly, Task, Version, ProgramTests, Fact

### Community 60 - "AdrPlusRepoConfig"
Cohesion: 0.10
Nodes (13): IAdrServices, AdrService, content, header, IConfiguration, Result, string, Success (+5 more)

### Community 61 - ".PromptShowPluginsListTable"
Cohesion: 0.20
Nodes (9): Allowlist, Detail, Events, IReadOnlyList, Name, NameOrFolder, Pending, Status (+1 more)

### Community 62 - "NewAdrCommandHandler"
Cohesion: 0.35
Nodes (5): NewAdrCommandHandler, Arguments, DateTime, Dictionary, ILogger

### Community 63 - "AdrRecordSnapshot"
Cohesion: 0.08
Nodes (24): AdrPlus.Abstractions.Testing, AdrPlus.Abstractions.Tests.Testing, AdrRecordSnapshot, DateTime, AdrStatus, RepoInfoSnapshot, IReadOnlyDictionary, IReadOnlyList (+16 more)

### Community 64 - ".BuildAdrKey"
Cohesion: 0.25
Nodes (4): AdrKeyFormatter, AdrKeyFormatterTests, InlineData, Theory

### Community 65 - "LowercaseNamingPolicyTests"
Cohesion: 0.29
Nodes (6): JsonNamingPolicy, LowercaseNamingPolicy, LowercaseNamingPolicyTests, Fact, InlineData, Theory

### Community 66 - ".Resolve"
Cohesion: 0.33
Nodes (5): IsActive, MissingNames, PluginActivationGate, Func, IReadOnlyList

### Community 67 - "PluginManagerDispatchTests"
Cohesion: 0.42
Nodes (6): PluginManagerDispatchTests, Fact, IEnumerable, ILogger, string, Task

### Community 68 - "SyncCommandHandler"
Cohesion: 0.19
Nodes (9): SyncCommandHandler, Arguments, CancellationToken, Func, ILogger, IReadOnlyList, Task, AdrHeader (+1 more)

### Community 69 - ".ReadHistoryAsync"
Cohesion: 0.40
Nodes (4): CancellationToken, Result, Success, Task

### Community 70 - ".OnAdrEventAsync"
Cohesion: 0.25
Nodes (5): AdrPluginBase, CancellationToken, Task, PluginResult, PluginResultStatus

### Community 71 - "AdrPluginBaseTests"
Cohesion: 0.26
Nodes (6): AdrPluginBaseTests, TestPlugin, CancellationToken, Fact, Task, ValueTask

### Community 72 - ".GetCommands"
Cohesion: 0.15
Nodes (7): CommandsAdr, Alias, Command, ConfigCommandHandler, Description, Dictionary, Type

### Community 73 - "Test Architecture Guide"
Cohesion: 0.18
Nodes (14): Test Architecture Guide, Arrange-Act-Assert Pattern, CI/CD Pipeline (GitHub Actions), CommandHandlerMockHelper, Domain-Specific Mock Helper Pattern, ExploreCommandHandlerFixture, FluentAssertions, Generic Mock Helper Pattern (+6 more)

### Community 74 - "IAdrPlugin"
Cohesion: 0.16
Nodes (7): IAsyncDisposable, IAdrPlugin, CancellationToken, Task, IPluginConfiguration, HostPluginConfiguration, Dictionary

### Community 75 - "Tyree-Ackerman ADR Template (English)"
Cohesion: 0.41
Nodes (13): Tyree-Ackerman ADR Template (English), Tyree-Ackerman ADR Template (German), Tyree-Ackerman ADR Template (Spanish), Tyree-Ackerman ADR Template (French), Tyree-Ackerman ADR Template (Italian), Tyree-Ackerman ADR Template (Japanese), Tyree-Ackerman ADR Template (Korean), Kyaruzi and van Katwijk Architecture Ontology (+5 more)

### Community 76 - "IAdrServices"
Cohesion: 0.23
Nodes (6): SupersedeCommandHandler, Arguments, DateTime, Dictionary, ILogger, IAdrServices

### Community 77 - ".PromptWriteError"
Cohesion: 0.16
Nodes (5): ReviseCommandHandler, Arguments, DateTime, Dictionary, ILogger

### Community 78 - "PluginManagerDisposalTests"
Cohesion: 0.40
Nodes (5): PluginManagerDisposalTests, Fact, ILogger, string, Task

### Community 79 - "CommandAttribute"
Cohesion: 0.17
Nodes (8): Attribute, CommandArgumentAttribute, CommandAttribute, string, Type, HelpUsageAttribute, string, UsageArgumments

### Community 80 - "AppConstants"
Cohesion: 0.25
Nodes (7): char, JsonDocumentOptions, Lazy, AppConstants, int, JsonSerializerOptions, string

### Community 81 - "IPromptConsole (shared UI abstraction, #1 god node)"
Cohesion: 0.36
Nodes (10): CommandRouter, Graphify memory: why IPromptConsole connects command handlers, IPromptConsole (shared UI abstraction, #1 god node), MainProgram, PromptConsole (single implementation, split across partial classes), InitCommandHandler, Graphify memory: should Init command handling be split into smaller modules, Command-handler test scaffolding community (corrected label, was mislabeled 'Init command handling') (+2 more)

### Community 82 - "AdrPlusConfig"
Cohesion: 0.18
Nodes (5): AdrPlusConfig, List, BehaviorWithoutArg, ExploreCommandHandlerFixture, ILogger

### Community 83 - ".GetHelpText"
Cohesion: 0.31
Nodes (4): Arguments, Dictionary, SupersedeCommandHandlerMockHelper, Dictionary

### Community 84 - ".PromptEditTitleAdr"
Cohesion: 0.40
Nodes (6): INewAdrPrompts, CancellationToken, Content, domains, Exception, IsAborted

### Community 85 - ".CreateHandler"
Cohesion: 0.33
Nodes (3): InitCommandHandlerBuiltinPluginsTests, IReadOnlyList, string

### Community 86 - ".ComputeDelay"
Cohesion: 0.36
Nodes (4): ComputeDelayTests, Fact, InlineData, Theory

### Community 87 - "AdrStatusTests"
Cohesion: 0.28
Nodes (4): AdrStatusTests, Fact, InlineData, Theory

### Community 90 - "PluginLoader.cs"
Cohesion: 0.29
Nodes (5): AssemblyDependencyResolver, AssemblyLoadContext, AssemblyName, PluginAssemblyLoadContext, Assembly

### Community 91 - "IFileSystemService"
Cohesion: 0.12
Nodes (4): Task, IFileSystemService, IEnumerable, Task

### Community 95 - "SatelliteResourcesTests"
Cohesion: 0.38
Nodes (4): SatelliteResourcesTests, MemberData, Theory, TheoryData

### Community 96 - "FormatMessages"
Cohesion: 0.40
Nodes (4): CompositeFormat, ConcurrentDictionary, FormatMessages, Func

### Community 98 - "AdrPlus"
Cohesion: 0.40
Nodes (4): AdrPlus.Resources, CultureInfo, ResourceManager, AdrPlus

### Community 99 - ".ExecuteAsync"
Cohesion: 0.40
Nodes (3): ICommandHandler, CancellationToken, Task

## Knowledge Gaps
- **189 isolated node(s):** `net10.0`, `net9.0`, `net8.0`, `DefaultDocumentation (1.2.5)`, `Microsoft.NET.Sdk` (+184 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **89 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `IFileSystemService` connect `IFileSystemService` to `NewAdrCommandHandlerTests`, `ValidateJsonConfigTests`, `.ParseArgs`, `FileSystemService`, `PluginManifest`, `.ReadAllTextAsync`, `.PromptSelectLogicalDrive`, `.DirectoryExists`, `.ParseFileName`, `.ExecuteAsync`, `PluginsCommandHandlerTests`, `PluginManagerRetryTests`, `ValidateConfig`, `.StatusUpdateAdrAsync`, `UndoStatusCommandHandlerTests`, `PluginManager`, `.StatusChangeAdrAsync`, `IValidateConfig`, `.RouteAsync`, `PromptConsole`, `.ExecuteAsync`, `ConfigCommandHandler`, `.LogCommandSuccessful`, `PluginManagerBackfillTests`, `.FileExists`, `PathHelper`, `.PromptWriteSuccess`, `PluginManagerTests`, `AdrPlusRepoConfig`, `NewAdrCommandHandler`, `PluginManagerDispatchTests`, `SyncCommandHandler`, `.ReadHistoryAsync`, `IAdrServices`, `.PromptWriteError`, `PluginManagerDisposalTests`, `AdrPlusConfig`, `.GetHelpText`, `.PromptEditTitleAdr`, `.CreateHandler`?**
  _High betweenness centrality (0.150) - this node is a cross-community bridge._
- **Why does `AdrPlus.Domain` connect `AdrPlus.Domain` to `AdrPlus.Infrastructure.Formatting`, `SyncCommandHandler`, `PatternParserTests`, `FieldsJson`, `TemplateResourcesTests`, `AdrPlusConfig`, `AdrPlus.Infrastructure.UI`, `.StatusUpdateAdrAsync`, `AdrPlus.Plugins`, `CaseFormat & StringCaseExtensions`, `PluginLoader.cs`, `AdrPlusRepoConfig`, `AdrPlus.Infrastructure.FileSystem`?**
  _High betweenness centrality (0.109) - this node is a cross-community bridge._
- **Why does `PromptConsole` connect `PromptConsole` to `.RouteAsync`, `IConsoleWriter`, `AdrPlus.Infrastructure.Formatting`, `HelperTests`, `.PromptSelectLogicalDrive`, `IAdrServices`, `.LogCommandSuccessful`, `IConfigPrompts`, `FieldsJson`, `.PromptClearWaitText`, `AdrPlusConfig`, `.PromptEditTitleAdr`, `IFileSystemService`, `.PromptShowPluginsListTable`, `IValidateConfig`?**
  _High betweenness centrality (0.067) - this node is a cross-community bridge._
- **What connects `net10.0`, `net9.0`, `net8.0` to the rest of the system?**
  _189 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `ValidateJsonConfigTests` be split into smaller, more focused modules?**
  _Cohesion score 0.06594788858939803 - nodes in this community are weakly interconnected._
- **Should `HelperTests` be split into smaller, more focused modules?**
  _Cohesion score 0.05153576582148011 - nodes in this community are weakly interconnected._
- **Should `.ParseArgs` be split into smaller, more focused modules?**
  _Cohesion score 0.12594268476621417 - nodes in this community are weakly interconnected._