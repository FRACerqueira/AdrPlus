# Graph Report - AdrPlus  (2026-08-04)

## Corpus Check
- 288 files · ~211,173 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 3038 nodes · 10782 edges · 180 communities (90 shown, 90 thin omitted)
- Extraction: 71% EXTRACTED · 29% INFERRED · 0% AMBIGUOUS · INFERRED: 3086 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `cdbbef11`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- IAdrServices
- ValidateJsonConfigTests
- HelperTests
- IValidateConfig
- FileSystemServiceEnhancedTests
- PatternParserTests
- PluginManifest
- .ReadAllTextAsync
- .PromptSelectLogicalDrive
- AdrServiceTests
- .HasTemplateRepoFile
- .ParseFileName
- .ReadAllAdrByNumber
- CancellationToken
- FieldsJson
- AdrFileNameComponentsTests
- PluginsCommandHandlerTests
- PluginManagerRetryTests
- PromptConsole
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
- IFileSystemService
- ApproveCommandHandlerTests
- AdrPlus.Commands
- WizardCommandHandler
- IConsoleWriter
- ServiceCollectionExtensions.cs
- HelpUsageAttributeTests
- AdrPlus.Infrastructure.FileSystem
- AdrRecordTests
- PluginsCommandHandler
- .ParseArgs
- CommandArgumentAttribute Tests
- AdrPlus Csproj/NuGet Package Metadata
- LogMessages
- ConfigCommandHandler
- CommandRouter
- AdrHeaderTests
- PluginManagerBackfillTests
- TemplateResourcesTests
- CommandAttribute Tests
- IPluginManager
- .RouteAsync
- PluginManagerDispatchTests
- AdrPlusConfig
- VersionCommandHandler
- AdrPlus.Infrastructure.UI
- AdrIndexerPlugin
- AdrFileNameComponents
- Alexandrian & Business Case ADR Templates
- Program Entry Point (+Tests)
- MigrateCommandHandler
- PromptConsole Plugin/Event Fields
- .ExploreWizardAsync
- .Create
- .CreateHandler
- LowercaseNamingPolicyTests
- .OnAdrEventAsync
- AdrPluginBaseTests
- .ExecuteAsync
- .ParseAdrHeaderAndContentAsync
- IMainProgram
- IAdrPlugin & IPluginConfiguration Contracts
- .GetCommands
- Test Architecture Guide
- .LogError
- Tyree-Ackerman ADR Template (English)
- PluginManagerDisposalTests
- ReviseCommandHandler
- .ExecuteAsync
- .PromptShowPluginsListTable
- .Resolve
- IPromptConsole (shared UI abstraction, #1 god node)
- .PromptEditTitleAdr
- IPluginLogger
- .ComputeDelay
- AdrStatusTests
- .InvokeOnceAsync
- PluginAssemblyLoadContext
- .BuildAdrKey
- ItemMenuWizardTests
- SatelliteResourcesTests
- FormatMessages
- AdrPlus
- .ExecuteAsync
- .PromptGetArrayDomainsAdr
- .WriteAllTextAsync
- .PromptSelecAdrs
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
1. `IFileSystemService` - 101 edges
2. `ValidateJsonConfigTests` - 99 edges
3. `PromptConsole` - 92 edges
4. `AdrPlus.Domain` - 84 edges
5. `AdrServiceTests` - 80 edges
6. `IConsoleWriter` - 77 edges
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

## Communities (180 total, 90 thin omitted)

### Community 0 - "IAdrServices"
Cohesion: 0.06
Nodes (30): Arguments, NewAdrCommandHandler, Arguments, CancellationToken, DateTime, Dictionary, ILogger, Task (+22 more)

### Community 1 - "ValidateJsonConfigTests"
Cohesion: 0.07
Nodes (4): ValidateJsonConfigTests, Dictionary, Fact, Task

### Community 2 - "HelperTests"
Cohesion: 0.05
Nodes (16): bool, date, Helper, DateTime, error, GeneratedRegex, int, JsonElement (+8 more)

### Community 3 - "IValidateConfig"
Cohesion: 0.07
Nodes (21): IValidateConfig, CancellationToken, ErrorReport, Task, ConfigVersionManager, CancellationToken, GeneratedRegex, IConfiguration (+13 more)

### Community 4 - "FileSystemServiceEnhancedTests"
Cohesion: 0.06
Nodes (20): AdrPlus.Tests.Infrastructure.FileSystem, FileSystemService, CancellationToken, IEnumerable, JsonSerializerOptions, Result, SearchOption, Success (+12 more)

### Community 5 - "PatternParserTests"
Cohesion: 0.06
Nodes (20): editorcmd, hasRider, hasVisualStudio, hasVSCode, Length, Position, PatternParser, Dictionary (+12 more)

### Community 6 - "PluginManifest"
Cohesion: 0.06
Nodes (32): IDisposable, PluginAllowlistEntry, ManifestValidationOutcome, PluginLoader, PluginLoadOutcome, PluginRejection, Action, CancellationToken (+24 more)

### Community 7 - ".ReadAllTextAsync"
Cohesion: 0.20
Nodes (9): CancellationToken, ConfirmYes, Content, DateTime, Func, info, IsAborted, left (+1 more)

### Community 8 - ".PromptSelectLogicalDrive"
Cohesion: 0.14
Nodes (13): Adrfiles, ArgsWizard, CancellationToken, Dictionary, Task, IMigratePrompts, CancellationToken, CountSelected (+5 more)

### Community 9 - "AdrServiceTests"
Cohesion: 0.08
Nodes (4): AdrServiceTests, Fact, IConfiguration, Task

### Community 10 - ".HasTemplateRepoFile"
Cohesion: 0.17
Nodes (16): InitCommandHandler, Arguments, CancellationToken, Dictionary, ILogger, List, Task, MaxNumber (+8 more)

### Community 11 - ".ParseFileName"
Cohesion: 0.15
Nodes (8): CancellationToken, Task, ReviseCommandHandlerTests, Dictionary, Fact, ILogger, string, Task

### Community 12 - ".ReadAllAdrByNumber"
Cohesion: 0.14
Nodes (8): CancellationToken, Task, VersionCommandHandlerTests, Dictionary, Fact, ILogger, string, Task

### Community 13 - "CancellationToken"
Cohesion: 0.12
Nodes (9): CancellationToken, Content, DateTime, FieldsFromFileAdr, Filename, IEnumerable, IsAborted, PrefixValue (+1 more)

### Community 14 - "FieldsJson"
Cohesion: 0.17
Nodes (13): JsonConfig, PrefixValue, FieldsJson, JsonValueKind, IConfigPrompts, CancellationToken, Content, FieldsFromFileAdr (+5 more)

### Community 16 - "PluginsCommandHandlerTests"
Cohesion: 0.10
Nodes (15): PluginsWizardMode, PluginRejectionReason, PluginsCommandHandlerInstallTests, Dictionary, Fact, ILogger, string, Task (+7 more)

### Community 17 - "PluginManagerRetryTests"
Cohesion: 0.14
Nodes (20): PendingEntry, DateTime, PendingStateStore, CancellationToken, List, string, Task, PendingStateStoreTests (+12 more)

### Community 18 - "PromptConsole"
Cohesion: 0.05
Nodes (15): Color, FrozenDictionary, PromptConsole, ConfirmYes, CountSelected, FieldsExplore, FilePathAdrTemplate, IConfiguration (+7 more)

### Community 19 - "AdrPlusRepoConfigTests"
Cohesion: 0.10
Nodes (5): AdrPlusRepoConfigTests, Fact, string, PluginSnapshotExtensionsTests, Fact

### Community 20 - "AdrEventContext record"
Cohesion: 0.09
Nodes (42): CHANGELOG - 1.0.0-beta4 (11-language localization), CHANGELOG - Unreleased (Plugin System), CLAUDE.md Coding Principles (Think/Simplicity/Surgical/Goal-Driven), CLAUDE.md Current Project Status (beta3, 11 languages), Code of Conduct, Contributing Guide, ADR001: Select ADR Templates Based on Configured UI Language, ADR002: Add a Plugin System for ADR Lifecycle Event Integrations (+34 more)

### Community 21 - "ValidateConfig"
Cohesion: 0.11
Nodes (13): JsonNode, ValidateConfig, CancellationToken, Dictionary, ErrorReport, IConfiguration, IsValid, JsonElement (+5 more)

### Community 22 - ".StatusUpdateAdrAsync"
Cohesion: 0.14
Nodes (15): CancellationToken, Content, DateTime, Error, Isvalid, Record, RejectCommandHandlerTests, Dictionary (+7 more)

### Community 23 - "AdrPlus.Domain"
Cohesion: 0.08
Nodes (15): char, AdrPlus.Domain, AdrPlus.Commands.Explore, AdrPlus.Tests.Domain, AdrPlus.Infrastructure.Formatting, AdrPlus.Tests.Core, AdrPlus.Core, JsonDocumentOptions (+7 more)

### Community 24 - "CaseFormat & StringCaseExtensions"
Cohesion: 0.09
Nodes (12): CaseFormat, StringCaseExtensions, GeneratedRegex, Regex, StringCaseExtensionsTests, Fact, InlineData, Theory (+4 more)

### Community 25 - "HelpCommandHandlerTests"
Cohesion: 0.14
Nodes (14): HelpCommandHandler, CancellationToken, CommandRouter, ILogger, Task, Alias, Command, ConfigCommandHandler (+6 more)

### Community 26 - "UndoStatusCommandHandlerTests"
Cohesion: 0.14
Nodes (11): UndoStatusCommandHandler, Arguments, CancellationToken, Dictionary, ILogger, Task, UndoStatusCommandHandlerTests, Dictionary (+3 more)

### Community 27 - "AdrPlus.Abstractions README (plain text)"
Cohesion: 0.08
Nodes (38): AdrPlus.Abstractions README (Markdown), adr-config.adrplus (activeplugins/disableplugins), AdrEventContext, AdrPluginBase base class, doc/api-abstractions API reference, Host-global plugin storage (%UserProfile%/AdrPlus.Plugins/<name>/), IAdrPlugin interface, PluginDevelopmentGuide.md (external reference) (+30 more)

### Community 28 - "PluginManager"
Cohesion: 0.18
Nodes (18): HashSet, AdrEventContext, Func, LoadedPlugin, PluginManager, Adr, CancellationToken, Content (+10 more)

### Community 29 - "IFileSystemService"
Cohesion: 0.12
Nodes (23): IAdrServices, Content, AdrService, CancellationToken, DateTime, Error, IConfiguration, Isvalid (+15 more)

### Community 30 - "ApproveCommandHandlerTests"
Cohesion: 0.18
Nodes (9): CancellationToken, Task, ApproveCommandHandlerTests, Dictionary, Fact, ILogger, MemberData, Task (+1 more)

### Community 31 - "AdrPlus.Commands"
Cohesion: 0.07
Nodes (20): Attribute, AdrPlus.Tests.Commands.Reject, AdrPlus.Tests.Commands.NewAdr, AdrPlus.Tests.Commands.Explore, AdrPlus.Tests.Commands.Attributes, AdrPlus.Tests.Commands.Approve, AdrPlus.Tests.Commands.Revise, AdrPlus.Tests.Helpers (+12 more)

### Community 32 - "WizardCommandHandler"
Cohesion: 0.17
Nodes (16): NotImplementedException, WizardCommandHandler, Arguments, CancellationToken, CommandRouter, CommandsAdr, IConfiguration, ILogger (+8 more)

### Community 33 - "IConsoleWriter"
Cohesion: 0.10
Nodes (12): IOptionsMonitor, IConfigurationMigrator, CancellationToken, Task, IConsoleWriter, Task, ZipPath, MainProgram (+4 more)

### Community 34 - "ServiceCollectionExtensions.cs"
Cohesion: 0.10
Nodes (17): AdrPlus.Commands.Migrate, AdrPlus.Extensions, AdrPlus.Commands.UndoStatus, AdrPlus.Commands.Config, AdrPlus.Infrastructure.Logging, AdrPlus.Commands.Revise, AdrPlus.Tests.Extensions, AdrPlus.Commands.Approve (+9 more)

### Community 36 - "AdrPlus.Infrastructure.FileSystem"
Cohesion: 0.11
Nodes (13): AdrPlus.Tests.Commands.Sync, AdrPlus.Tests.Commands.Plugins, AdrPlus.Plugins, AdrPlus.Commands.Plugins, AdrPlus.Abstractions, AdrPlus.Infrastructure.FileSystem, AdrPlus.Commands.Init, AdrPlus.Abstractions.Domain (+5 more)

### Community 38 - "PluginsCommandHandler"
Cohesion: 0.11
Nodes (21): Config, ConfigPath, PluginsCommandHandler, Allowlist, Arguments, CancellationToken, Detail, Dictionary (+13 more)

### Community 39 - ".ParseArgs"
Cohesion: 0.13
Nodes (7): SyncWizardMode, Mode, SyncCommandHandlerTests, Fact, ILogger, Task, Dictionary

### Community 41 - "AdrPlus Csproj/NuGet Package Metadata"
Cohesion: 0.05
Nodes (36): DefaultDocumentation (1.2.5), Microsoft.Extensions.Hosting (10.0.10), PromptPlus (6.0.0-Beta9), Serilog.Extensions.Logging.File (3.0.0), net10.0, net8.0, net9.0, Microsoft.NET.Sdk (+28 more)

### Community 42 - "LogMessages"
Cohesion: 0.19
Nodes (6): LoggerMessage, LogMessages, Exception, ILogger, HostPluginLogger, ILogger

### Community 43 - "ConfigCommandHandler"
Cohesion: 0.15
Nodes (10): EditField, ConfigCommandHandler, Arguments, Func, ILogger, IsAborted, JsonElement, JsonValueKind (+2 more)

### Community 45 - "CommandRouter"
Cohesion: 0.29
Nodes (6): IServiceProvider, CommandRouter, Dictionary, IConfiguration, ILogger, Type

### Community 47 - "PluginManagerBackfillTests"
Cohesion: 0.30
Nodes (9): PluginManagerBackfillTests, Adr, EventType, Fact, FilePath, Func, GetContent, ILogger (+1 more)

### Community 48 - "TemplateResourcesTests"
Cohesion: 0.10
Nodes (14): AdrPlus.Tests.Localization, CultureData, Action, TheoryData, HeaderLocalizationTests, InlineData, MemberData, Theory (+6 more)

### Community 50 - "IPluginManager"
Cohesion: 0.14
Nodes (19): AdrRecordSnapshot, DateTime, AdrStatus, RepoInfoSnapshot, IReadOnlyDictionary, IReadOnlyList, PluginSnapshotExtensions, IPluginManager (+11 more)

### Community 51 - ".RouteAsync"
Cohesion: 0.29
Nodes (5): CancellationToken, Task, CommandRouterTests, Fact, Task

### Community 52 - "PluginManagerDispatchTests"
Cohesion: 0.42
Nodes (6): PluginManagerDispatchTests, Fact, IEnumerable, ILogger, string, Task

### Community 53 - "AdrPlusConfig"
Cohesion: 0.06
Nodes (18): AdrPlusConfig, List, BehaviorWithoutArg, SearchOption, ExploreCommandHandlerTests, Fact, Task, ExploreCommandHandlerFixture (+10 more)

### Community 54 - "VersionCommandHandler"
Cohesion: 0.24
Nodes (6): ICommandHandler, VersionCommandHandler, Arguments, DateTime, Dictionary, ILogger

### Community 55 - "AdrPlus.Infrastructure.UI"
Cohesion: 0.11
Nodes (8): AdrPlus.Infrastructure.UI, AdrPlus.Tests.Commands, AdrPlus.Infrastructure.Configuration, AdrPlus.Tests.Commands.Help, AdrPlus.Tests.Commands.Supersede, AdrPlus, AdrPlus.Commands.Supersede, AdrPlus.Tests.Infrastructure.Configuration

### Community 56 - "AdrIndexerPlugin"
Cohesion: 0.21
Nodes (9): Link, AdrIndexerPlugin, CancellationToken, List, Status, string, Task, Title (+1 more)

### Community 57 - "AdrFileNameComponents"
Cohesion: 0.10
Nodes (8): ApproveCommandHandler, Arguments, DateTime, Dictionary, ILogger, AdrFileNameComponents, AdrStatus, CommandHandlerMockHelper

### Community 58 - "Alexandrian & Business Case ADR Templates"
Cohesion: 0.12
Nodes (18): ADR Index Template, Considered Options, Context and Problem Statement, Deciders Section, Decision Drivers, Decision Outcome (with Positive/Negative Consequences), Links (ADR Cross-References), Alexandrian ADR Template (MADR-format, en-US canonical) (+10 more)

### Community 59 - "Program Entry Point (+Tests)"
Cohesion: 0.19
Nodes (8): CancellationTokenSource, AdrPlus.Tests, Program, Assembly, Task, Version, ProgramTests, Fact

### Community 60 - "MigrateCommandHandler"
Cohesion: 0.33
Nodes (5): MigrateCommandHandler, Arguments, Func, IEnumerable, ILogger

### Community 61 - "PromptConsole Plugin/Event Fields"
Cohesion: 0.13
Nodes (11): Allowlist, Detail, Events, IReadOnlyList, IReadOnlySet, Name, NameOrFolder, Pending (+3 more)

### Community 62 - ".ExploreWizardAsync"
Cohesion: 0.15
Nodes (16): Fields, IOptions, ExploreCommandHandler, Arguments, CancellationToken, Dictionary, ILogger, Task (+8 more)

### Community 63 - ".Create"
Cohesion: 0.10
Nodes (17): AdrPlus.Abstractions.Testing, AdrPlus.Abstractions.Tests.Testing, AdrEventContextFactory, AdrEventContext, AdrEventType, Func, AdrRecordSnapshotFactory, DateTime (+9 more)

### Community 64 - ".CreateHandler"
Cohesion: 0.33
Nodes (3): InitCommandHandlerBuiltinPluginsTests, IReadOnlyList, string

### Community 65 - "LowercaseNamingPolicyTests"
Cohesion: 0.29
Nodes (6): JsonNamingPolicy, LowercaseNamingPolicy, LowercaseNamingPolicyTests, Fact, InlineData, Theory

### Community 66 - ".OnAdrEventAsync"
Cohesion: 0.25
Nodes (5): AdrPluginBase, CancellationToken, Task, PluginResult, PluginResultStatus

### Community 67 - "AdrPluginBaseTests"
Cohesion: 0.26
Nodes (6): AdrPluginBaseTests, TestPlugin, CancellationToken, Fact, Task, ValueTask

### Community 68 - ".ExecuteAsync"
Cohesion: 0.24
Nodes (9): AdrEventType, SyncCommandHandler, Arguments, CancellationToken, Dictionary, Func, ILogger, IReadOnlyList (+1 more)

### Community 69 - ".ParseAdrHeaderAndContentAsync"
Cohesion: 0.14
Nodes (8): content, header, AdrHeader, DateTime, CancellationToken, Result, Success, Task

### Community 70 - "IMainProgram"
Cohesion: 0.40
Nodes (3): IMainProgram, CancellationToken, Task

### Community 71 - "IAdrPlugin & IPluginConfiguration Contracts"
Cohesion: 0.16
Nodes (7): IAsyncDisposable, IAdrPlugin, CancellationToken, Task, IPluginConfiguration, HostPluginConfiguration, Dictionary

### Community 72 - ".GetCommands"
Cohesion: 0.15
Nodes (7): CommandsAdr, Alias, Command, ConfigCommandHandler, Description, Dictionary, Type

### Community 73 - "Test Architecture Guide"
Cohesion: 0.18
Nodes (14): Test Architecture Guide, Arrange-Act-Assert Pattern, CI/CD Pipeline (GitHub Actions), CommandHandlerMockHelper, Domain-Specific Mock Helper Pattern, ExploreCommandHandlerFixture, FluentAssertions, Generic Mock Helper Pattern (+6 more)

### Community 75 - "Tyree-Ackerman ADR Template (English)"
Cohesion: 0.41
Nodes (13): Tyree-Ackerman ADR Template (English), Tyree-Ackerman ADR Template (German), Tyree-Ackerman ADR Template (Spanish), Tyree-Ackerman ADR Template (French), Tyree-Ackerman ADR Template (Italian), Tyree-Ackerman ADR Template (Japanese), Tyree-Ackerman ADR Template (Korean), Kyaruzi and van Katwijk Architecture Ontology (+5 more)

### Community 76 - "PluginManagerDisposalTests"
Cohesion: 0.40
Nodes (5): PluginManagerDisposalTests, Fact, ILogger, string, Task

### Community 77 - "ReviseCommandHandler"
Cohesion: 0.27
Nodes (5): ReviseCommandHandler, Arguments, DateTime, Dictionary, ILogger

### Community 78 - ".ExecuteAsync"
Cohesion: 0.23
Nodes (7): RejectCommandHandler, Arguments, CancellationToken, DateTime, Dictionary, ILogger, Task

### Community 79 - ".PromptShowPluginsListTable"
Cohesion: 0.15
Nodes (11): Allowlist, Detail, Events, IReadOnlyList, IReadOnlySet, Name, NameOrFolder, Pending (+3 more)

### Community 80 - ".Resolve"
Cohesion: 0.33
Nodes (5): IsActive, MissingNames, PluginActivationGate, Func, IReadOnlyList

### Community 81 - "IPromptConsole (shared UI abstraction, #1 god node)"
Cohesion: 0.36
Nodes (10): CommandRouter, Graphify memory: why IPromptConsole connects command handlers, IPromptConsole (shared UI abstraction, #1 god node), MainProgram, PromptConsole (single implementation, split across partial classes), InitCommandHandler, Graphify memory: should Init command handling be split into smaller modules, Command-handler test scaffolding community (corrected label, was mislabeled 'Init command handling') (+2 more)

### Community 84 - ".PromptEditTitleAdr"
Cohesion: 0.40
Nodes (6): INewAdrPrompts, CancellationToken, Content, domains, Exception, IsAborted

### Community 85 - "IPluginLogger"
Cohesion: 0.25
Nodes (4): IPluginContext, IPluginLogger, Exception, HostPluginContext

### Community 86 - ".ComputeDelay"
Cohesion: 0.36
Nodes (4): ComputeDelayTests, Fact, InlineData, Theory

### Community 87 - "AdrStatusTests"
Cohesion: 0.28
Nodes (4): AdrStatusTests, Fact, InlineData, Theory

### Community 90 - "PluginAssemblyLoadContext"
Cohesion: 0.33
Nodes (5): AssemblyDependencyResolver, AssemblyLoadContext, AssemblyName, PluginAssemblyLoadContext, Assembly

### Community 93 - ".BuildAdrKey"
Cohesion: 0.25
Nodes (4): AdrKeyFormatter, AdrKeyFormatterTests, InlineData, Theory

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

### Community 100 - ".PromptGetArrayDomainsAdr"
Cohesion: 0.40
Nodes (3): domains, Exception, Task

### Community 101 - ".WriteAllTextAsync"
Cohesion: 0.24
Nodes (6): CancellationToken, Task, ActivePluginsWriter, CancellationToken, IEnumerable, Task

## Knowledge Gaps
- **188 isolated node(s):** `net10.0`, `net9.0`, `net8.0`, `DefaultDocumentation (1.2.5)`, `Microsoft.NET.Sdk` (+183 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **90 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `IFileSystemService` connect `IFileSystemService` to `IAdrServices`, `ValidateJsonConfigTests`, `IValidateConfig`, `FileSystemServiceEnhancedTests`, `PluginManifest`, `.ReadAllTextAsync`, `.PromptSelectLogicalDrive`, `.HasTemplateRepoFile`, `.ParseFileName`, `.ReadAllAdrByNumber`, `CancellationToken`, `PluginsCommandHandlerTests`, `PluginManagerRetryTests`, `PromptConsole`, `ValidateConfig`, `.StatusUpdateAdrAsync`, `UndoStatusCommandHandlerTests`, `PluginManager`, `ApproveCommandHandlerTests`, `WizardCommandHandler`, `PluginsCommandHandler`, `.ParseArgs`, `ConfigCommandHandler`, `PluginManagerBackfillTests`, `PluginManagerDispatchTests`, `AdrPlusConfig`, `VersionCommandHandler`, `AdrFileNameComponents`, `MigrateCommandHandler`, `.ExploreWizardAsync`, `.CreateHandler`, `.ExecuteAsync`, `.ParseAdrHeaderAndContentAsync`, `PluginManagerDisposalTests`, `ReviseCommandHandler`, `.ExecuteAsync`, `.PromptEditTitleAdr`, `.ReadAllAdr`, `.ReadAllAdrByNumber`, `.PromptGetArrayDomainsAdr`, `.WriteAllTextAsync`?**
  _High betweenness centrality (0.144) - this node is a cross-community bridge._
- **Why does `AdrPlus.Domain` connect `AdrPlus.Domain` to `ServiceCollectionExtensions.cs`, `AdrPlus.Infrastructure.FileSystem`, `.ParseAdrHeaderAndContentAsync`, `PatternParserTests`, `FieldsJson`, `TemplateResourcesTests`, `AdrPlusConfig`, `AdrPlus.Infrastructure.UI`, `CaseFormat & StringCaseExtensions`, `AdrFileNameComponents`, `IFileSystemService`, `AdrPlus.Commands`?**
  _High betweenness centrality (0.103) - this node is a cross-community bridge._
- **Why does `AdrPlusRepoConfig` connect `IFileSystemService` to `IAdrServices`, `HelperTests`, `.ReadAllTextAsync`, `.PromptSelectLogicalDrive`, `AdrServiceTests`, `.HasTemplateRepoFile`, `.ParseFileName`, `.ReadAllAdrByNumber`, `CancellationToken`, `PromptConsole`, `AdrPlusRepoConfigTests`, `ValidateConfig`, `.StatusUpdateAdrAsync`, `CaseFormat & StringCaseExtensions`, `AdrRecordTests`, `PluginsCommandHandler`, `ConfigCommandHandler`, `IPluginManager`, `AdrFileNameComponents`, `MigrateCommandHandler`, `.ExploreWizardAsync`, `.ExecuteAsync`, `.ParseAdrHeaderAndContentAsync`, `.Resolve`, `.PromptEditTitleAdr`, `.ReadAllAdr`, `.ReadAllAdrByNumber`, `.PromptGetArrayDomainsAdr`, `.PromptSelecAdrs`?**
  _High betweenness centrality (0.070) - this node is a cross-community bridge._
- **What connects `net10.0`, `net9.0`, `net8.0` to the rest of the system?**
  _188 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `IAdrServices` be split into smaller, more focused modules?**
  _Cohesion score 0.06437346437346438 - nodes in this community are weakly interconnected._
- **Should `ValidateJsonConfigTests` be split into smaller, more focused modules?**
  _Cohesion score 0.06828282828282828 - nodes in this community are weakly interconnected._
- **Should `HelperTests` be split into smaller, more focused modules?**
  _Cohesion score 0.05241228070175439 - nodes in this community are weakly interconnected._