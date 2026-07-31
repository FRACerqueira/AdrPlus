# Graph Report - AdrPlus  (2026-07-31)

## Corpus Check
- 224 files · ~176,269 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 3102 nodes · 9177 edges · 152 communities (140 shown, 12 thin omitted)
- Extraction: 71% EXTRACTED · 29% INFERRED · 0% AMBIGUOUS · INFERRED: 2685 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `197cd49c`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- .DirectoryExists
- .StatusUpdateAdrAsync
- ValidateJsonConfigTests
- .ParseFileName
- FieldsJson
- .ParseArgs
- FileSystemServiceEnhancedTests
- PatternParserTests
- HelperTests
- TemplateResourcesTests
- .ValidateRepoStructure
- IConsoleWriter
- .RouteAsync
- AdrFileNameComponentsTests
- AdrServiceTests
- PromptConsole
- AdrPlusRepoConfigTests
- ValidateConfig
- AdrPlus README
- AdrPlus.Domain
- AdrEventContext
- AdrPlus.Infrastructure.FileSystem
- HelpCommandHandlerTests
- UndoStatusCommandHandlerTests
- AdrRecordTests
- HelpUsageAttributeTests
- AdrPlus CHANGELOG
- AdrPlusRepoConfig
- ConfigCommandHandler
- ADR record model tests
- AdrPlus.Infrastructure.UI
- AdrHeaderTests
- IValidateConfig
- IFileSystemService
- ADR discovery/query service
- .GetFullNameFile
- WizardCommandHandler
- AdrFileNameComponents
- AdrPlus — Plugin Architecture Specification (Final)
- .StatusChangeAdrAsync
- AdrPlus.Abstractions.Tests.csproj
- AdrPlus Plugin System — Implementation Plan
- .LogCommandException
- AdrService
- .ResolveAppVersion
- StringCaseExtensionsTests
- PathHelper
- [Brief title of the decision]
- LowercaseNamingPolicyTests
- [Kurzer Titel der Entscheidung]
- [Título breve de la decisión]
- [Titre bref de la décision]
- .GetCommands
- ConfigCommandHandlerTests
- [Titolo breve della decisione]
- CommandAttribute
- [決定の簡潔なタイトル]
- [결정에 대한 간단한 제목]
- [Korte titel van de beslissing]
- [Título breve da decisão]
- AdrStatusTests
- AppConstants
- [Краткое название решения]
- [决策简要标题]
- Fact
- .IsValidCultureName
- FormatMessages
- [Brief title of the decision]
- AdrPlus
- Q: Why does IPromptConsole connect Migration wizard config to Init command handling, New ADR command, PromptConsole core UI, Approve command handling, Startup/DI services, App config wizard (editor), Repo config validation, Revise command handling, Migrate command handling, Explorer wizard prompts, Help command & router, Config command editing, Explorer report generation, Config field editing prompts, Infrastructure namespaces?
- Q: Should Init command handling be split into smaller, more focused modules?
- Q: Why does IFileSystemService connect ADR header parsing to Init command handling, New ADR command, ADR status change service, and most other commands?
- [Kurzer Titel der Entscheidung]
- [Título breve de la decisión]
- [Titre bref de la décision]
- Test Architecture Guide
- Root CLAUDE.md (project instructions)
- [Titolo breve della decisione]
- [決定の簡潔なタイトル]
- [결정에 대한 간단한 제목]
- [Korte titel van de beslissing]
- [Título breve da decisão]
- [Краткое название решения]
- [决策简要标题]
- [Brief title of the decision]
- [Kurzer Titel der Entscheidung]
- [Título breve de la decisión]
- [Titre bref de la décision]
- [Titolo breve della decisione]
- CultureData
- [決定の簡潔なタイトル]
- [결정에 대한 간단한 제목]
- [Korte titel van de beslissing]
- [Título breve da decisão]
- [Краткое название решения]
- [决策简要标题]
- AdrStatus
- IMainProgram
- [Brief title of the decision]
- [Kurzer Titel der Entscheidung]
- [Título breve de la decisión]
- [Titre bref de la décision]
- [Titolo breve della decisione]
- [決定の簡潔なタイトル]
- [결정에 대한 간단한 제목]
- [Korte titel van de beslissing]
- [Título breve da decisão]
- [Краткое название решения]
- [决策简要标题]
- .PromptSelectLogicalDrive
- ItemMenuWizardTests
- CommandRouterTests.cs
- .OpenFile
- Helper
- [Brief title of the decision]
- [Kurzer Titel der Entscheidung]
- [Título breve de la decisión]
- [Titre bref de la décision]
- [Titolo breve della decisione]
- [決定の簡潔なタイトル]
- [결정에 대한 간단한 제목]
- [Korte titel van de beslissing]
- [Título breve da decisão]
- [Краткое название решения]
- [决策简要标题]
- .ExecuteAsync
- [Brief title of the decision]
- [Kurzer Titel der Entscheidung]
- [Título breve de la decisión]
- [Titre bref de la décision]
- Select ADR templates based on configured UI language
- [Titolo breve della decisione]
- [決定の簡潔なタイトル]
- [결정에 대한 간단한 제목]
- [Korte titel van de beslissing]
- [Título breve da decisão]
- [Краткое название решения]
- [决策简要标题]
- [Brief title of the decision]
- [Kurzer Titel der Entscheidung]
- [Título breve de la decisión]
- [Titre bref de la décision]
- [Titolo breve della decisione]
- [決定の簡潔なタイトル]
- [결정에 대한 간단한 제목]
- [Korte titel van de beslissing]
- [Título breve da decisão]
- [Краткое название решения]
- [决策简要标题]

## God Nodes (most connected - your core abstractions)
1. `ValidateJsonConfigTests` - 94 edges
2. `PromptConsole` - 85 edges
3. `IFileSystemService` - 80 edges
4. `AdrServiceTests` - 79 edges
5. `HelperTests` - 68 edges
6. `AdrPlus.Domain` - 65 edges
7. `AdrPlus.Core` - 61 edges
8. `ReviseCommandHandlerTests` - 60 edges
9. `AdrPlusRepoConfig` - 59 edges
10. `VersionCommandHandlerTests` - 59 edges

## Surprising Connections (you probably didn't know these)
- `NuGet README` --semantically_similar_to--> `AdrPlus CLI Tool`  [INFERRED] [semantically similar]
  NugetREADME.md → README.md
- `Release workflow (release.yml)` --shares_data_with--> `AdrPlus CHANGELOG`  [INFERRED]
  .github/workflows/release.yml → CHANGELOG.md
- `Migration Guide` --references--> `AdrPlus Icon`  [EXTRACTED]
  MigrationGuide.md → icon.png
- `NuGet README` --references--> `AdrPlus Icon`  [EXTRACTED]
  NugetREADME.md → icon.png
- `AdrPlus README` --references--> `AdrPlus Icon`  [EXTRACTED]
  README.md → icon.png

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **AdrPlus Contribution Workflow** — contributing_doc, github_pull_request_template_doc, github_issue_template_bug_report_doc, github_issue_template_feature_request_doc, changelog_doc [INFERRED 0.85]
- **ADR Status Lifecycle Commands** — changelog_new, changelog_version, changelog_review, changelog_supersede, changelog_approve, changelog_reject, changelog_undo [EXTRACTED 1.00]
- **AdrPlus Documentation Set** — readme, migrationguide, stepbystepguide, nugetreadme, security, faq [INFERRED 0.85]
- **Test Mock Helper Architecture** — tests_test_architecture_command_handler_pattern, tests_test_architecture_mock_helper_pattern, tests_test_architecture_supersede_case_study [EXTRACTED 1.00]

## Communities (152 total, 12 thin omitted)

### Community 0 - ".DirectoryExists"
Cohesion: 0.17
Nodes (14): InitCommandHandler, Arguments, CancellationToken, Dictionary, ILogger, List, Task, MaxNumber (+6 more)

### Community 1 - ".StatusUpdateAdrAsync"
Cohesion: 0.06
Nodes (32): ApproveCommandHandler, Arguments, CancellationToken, DateTime, Dictionary, ILogger, Task, RejectCommandHandler (+24 more)

### Community 2 - "ValidateJsonConfigTests"
Cohesion: 0.07
Nodes (4): ValidateJsonConfigTests, Dictionary, Fact, Task

### Community 3 - ".ParseFileName"
Cohesion: 0.06
Nodes (27): ReviseCommandHandler, Arguments, CancellationToken, DateTime, Dictionary, ILogger, Task, VersionCommandHandler (+19 more)

### Community 4 - "FieldsJson"
Cohesion: 0.17
Nodes (13): JsonConfig, PrefixValue, FieldsJson, JsonValueKind, IConfigPrompts, CancellationToken, Content, FieldsFromFileAdr (+5 more)

### Community 6 - "FileSystemServiceEnhancedTests"
Cohesion: 0.06
Nodes (20): AdrPlus.Tests.Infrastructure.FileSystem, FileSystemService, CancellationToken, IEnumerable, JsonSerializerOptions, Result, SearchOption, Success (+12 more)

### Community 7 - "PatternParserTests"
Cohesion: 0.06
Nodes (20): editorcmd, hasRider, hasVisualStudio, hasVSCode, Length, Position, PatternParser, Dictionary (+12 more)

### Community 9 - "TemplateResourcesTests"
Cohesion: 0.15
Nodes (10): AdrPlus.Tests.Localization, SatelliteResourcesTests, MemberData, Theory, TheoryData, TemplateResourcesTests, MemberData, string (+2 more)

### Community 10 - ".ValidateRepoStructure"
Cohesion: 0.20
Nodes (9): CancellationToken, ConfirmYes, Content, DateTime, Func, info, IsAborted, left (+1 more)

### Community 11 - "IConsoleWriter"
Cohesion: 0.08
Nodes (17): IOptionsMonitor, IServiceProvider, CommandRouter, Dictionary, IConfiguration, ILogger, Type, IConfigurationMigrator (+9 more)

### Community 12 - ".RouteAsync"
Cohesion: 0.29
Nodes (5): CancellationToken, Task, CommandRouterTests, Fact, Task

### Community 14 - "AdrServiceTests"
Cohesion: 0.12
Nodes (3): AdrServiceTests, Fact, IConfiguration

### Community 15 - "PromptConsole"
Cohesion: 0.05
Nodes (28): Color, FrozenDictionary, RepoActions, PromptConsole, CancellationToken, ConfirmYes, Content, CountSelected (+20 more)

### Community 16 - "AdrPlusRepoConfigTests"
Cohesion: 0.13
Nodes (3): AdrPlusRepoConfigTests, Fact, string

### Community 17 - "ValidateConfig"
Cohesion: 0.13
Nodes (12): JsonNode, ValidateConfig, CancellationToken, Dictionary, ErrorReport, IConfiguration, IsValid, List (+4 more)

### Community 18 - "AdrPlus README"
Cohesion: 0.20
Nodes (19): Architecture Decision Record (ADR), FAQ (referenced, not read), AdrPlus Icon, Migration Guide, AdrPlus Migrated Header Table Format, ADR Migration Process, Migration Prerequisites, NuGet README (+11 more)

### Community 19 - "AdrPlus.Domain"
Cohesion: 0.11
Nodes (10): AdrPlus.Extensions, AdrPlus.Domain, AdrPlus.Infrastructure.Configuration, AdrPlus, AdrPlus.Tests.Commands.Init, AdrPlus.Tests.Domain, AdrPlus.Tests.Core, AdrPlus.Core (+2 more)

### Community 20 - "AdrEventContext"
Cohesion: 0.06
Nodes (31): AdrPlus.Abstractions, AdrPlus.Abstractions.Domain, AdrPlus.Abstractions.Tests, IAsyncDisposable, IReadOnlyDictionary, IReadOnlyList, AdrEventContext, Func (+23 more)

### Community 21 - "AdrPlus.Infrastructure.FileSystem"
Cohesion: 0.10
Nodes (14): AdrPlus.Tests.Commands.Reject, AdrPlus.Tests.Commands.NewAdr, AdrPlus.Tests.Commands.Explore, AdrPlus.Tests.Commands.Attributes, AdrPlus.Tests.Commands.Approve, AdrPlus.Tests.Commands.Revise, AdrPlus.Tests.Helpers, AdrPlus.Infrastructure.FileSystem (+6 more)

### Community 22 - "HelpCommandHandlerTests"
Cohesion: 0.06
Nodes (33): Fields, ICommandHandler, IOptions, ExploreCommandHandler, Arguments, CancellationToken, Dictionary, ILogger (+25 more)

### Community 23 - "UndoStatusCommandHandlerTests"
Cohesion: 0.15
Nodes (11): UndoStatusCommandHandler, Arguments, CancellationToken, Dictionary, ILogger, Task, UndoStatusCommandHandlerTests, Dictionary (+3 more)

### Community 24 - "AdrRecordTests"
Cohesion: 0.14
Nodes (6): CancellationToken, Task, AdrRecord, DateTime, AdrRecordTests, Fact

### Community 26 - "AdrPlus CHANGELOG"
Cohesion: 0.12
Nodes (26): approve command, config command, AdrPlus CHANGELOG, explorer command, help command, init command, Keep a Changelog format, migrate command (+18 more)

### Community 27 - "AdrPlusRepoConfig"
Cohesion: 0.13
Nodes (5): Task, AdrPlusRepoConfig, Dictionary, SearchOption, Task

### Community 28 - "ConfigCommandHandler"
Cohesion: 0.13
Nodes (13): EditField, ConfigCommandHandler, Arguments, CancellationToken, Content, Func, ILogger, IsAborted (+5 more)

### Community 30 - "AdrPlus.Infrastructure.UI"
Cohesion: 0.10
Nodes (19): AdrPlus.Infrastructure.UI, AdrPlus.Commands.Migrate, AdrPlus.Commands.UndoStatus, AdrPlus.Commands.Config, AdrPlus.Commands.Explore, AdrPlus.Infrastructure.Logging, AdrPlus.Commands.Revise, AdrPlus.Tests.Commands.Help (+11 more)

### Community 32 - "IValidateConfig"
Cohesion: 0.15
Nodes (11): IValidateConfig, CancellationToken, ErrorReport, Task, ConfigVersionManager, CancellationToken, GeneratedRegex, IConfiguration (+3 more)

### Community 33 - "IFileSystemService"
Cohesion: 0.09
Nodes (9): content, header, content, header, IFileSystemService, IEnumerable, Result, Success (+1 more)

### Community 35 - ".GetFullNameFile"
Cohesion: 0.06
Nodes (37): Arguments, NewAdrCommandHandler, Arguments, CancellationToken, DateTime, Dictionary, ILogger, Task (+29 more)

### Community 36 - "WizardCommandHandler"
Cohesion: 0.18
Nodes (15): NotImplementedException, WizardCommandHandler, Arguments, CancellationToken, CommandRouter, CommandsAdr, IConfiguration, ILogger (+7 more)

### Community 38 - "AdrPlus — Plugin Architecture Specification (Final)"
Cohesion: 0.09
Nodes (22): 10. Security (D22), 11. Impact on the core (minimal, surgical), 12. End-to-end flow (Confluence example), 13. Accepted trade-offs, 14. Roadmap (v2, out of scope for v1), 15. Glossary, 1.1 What AdrPlus is, 1.2 Feature goal (+14 more)

### Community 39 - ".StatusChangeAdrAsync"
Cohesion: 0.13
Nodes (5): CancellationToken, DateTime, Error, Isvalid, IsValid

### Community 40 - "AdrPlus.Abstractions.Tests.csproj"
Cohesion: 0.08
Nodes (23): Microsoft.Extensions.Hosting (10.0.10), PromptPlus (6.0.0-Beta7), Serilog.Extensions.Logging.File (3.0.0), net10.0, Microsoft.NET.Sdk, net10.0, Microsoft.NET.Sdk, net10.0 (+15 more)

### Community 41 - "AdrPlus Plugin System — Implementation Plan"
Cohesion: 0.12
Nodes (16): AdrPlus Plugin System — Implementation Plan, Definition of Done for v1, Out of scope for this plan (do not build), Phase 10 — Plugin-author documentation, Phase 11 — Reference/example plugin, Phase 1 — `AdrPlus.Abstractions` project, Phase 2 — Domain snapshots, Phase 3 — Plugin discovery & loading (`IPluginManager` / `PluginLoader`) (+8 more)

### Community 42 - ".LogCommandException"
Cohesion: 0.27
Nodes (4): LoggerMessage, LogMessages, Exception, ILogger

### Community 43 - "AdrService"
Cohesion: 0.21
Nodes (7): IAdrServices, AdrService, IConfiguration, Result, string, Success, StringComparison

### Community 44 - ".ResolveAppVersion"
Cohesion: 0.19
Nodes (8): Assembly, CancellationTokenSource, AdrPlus.Tests, Program, Task, ProgramTests, Fact, Version

### Community 45 - "StringCaseExtensionsTests"
Cohesion: 0.09
Nodes (12): CaseFormat, StringCaseExtensions, GeneratedRegex, Regex, StringCaseExtensionsTests, Fact, InlineData, Theory (+4 more)

### Community 46 - "PathHelper"
Cohesion: 0.10
Nodes (7): ExploreCommandHandlerTests, Fact, Task, ExploreCommandHandlerMockHelper, DateTime, Dictionary, PathHelper

### Community 47 - "[Brief title of the decision]"
Cohesion: 0.13
Nodes (14): Argument, Assumptions, [Brief title of the decision], Constraints, Decision, Group, Implications, Issue (+6 more)

### Community 48 - "LowercaseNamingPolicyTests"
Cohesion: 0.29
Nodes (6): JsonNamingPolicy, LowercaseNamingPolicy, LowercaseNamingPolicyTests, Fact, InlineData, Theory

### Community 49 - "[Kurzer Titel der Entscheidung]"
Cohesion: 0.13
Nodes (14): Annahmen, Argument, Einschränkungen, Entscheidung, Gruppe, Implikationen, [Kurzer Titel der Entscheidung], Notizen (+6 more)

### Community 50 - "[Título breve de la decisión]"
Cohesion: 0.13
Nodes (14): Argumento, Artefactos relacionados, Decisiones relacionadas, Decisión, Grupo, Implicaciones, Notas, Posiciones (+6 more)

### Community 51 - "[Titre bref de la décision]"
Cohesion: 0.13
Nodes (14): Argumentation, Artefacts connexes, Contraintes, Décision, Décisions connexes, Exigences connexes, Groupe, Hypothèses (+6 more)

### Community 52 - ".GetCommands"
Cohesion: 0.15
Nodes (7): CommandsAdr, Alias, Command, ConfigCommandHandler, Description, Dictionary, Type

### Community 53 - "ConfigCommandHandlerTests"
Cohesion: 0.14
Nodes (5): FilePathAdrTemplate, ConfigCommandHandlerTests, Fact, ILogger, Task

### Community 54 - "[Titolo breve della decisione]"
Cohesion: 0.13
Nodes (14): Argomentazione, Artefatti correlati, Decisione, Decisioni correlate, Gruppo, Implicazioni, Note, Posizioni (+6 more)

### Community 55 - "CommandAttribute"
Cohesion: 0.17
Nodes (8): Attribute, CommandArgumentAttribute, CommandAttribute, string, Type, HelpUsageAttribute, string, UsageArgumments

### Community 56 - "[決定の簡潔なタイトル]"
Cohesion: 0.13
Nodes (14): グループ, 備考, 制約, 前提条件, 決定, [決定の簡潔なタイトル], 波及効果, 見解 (+6 more)

### Community 57 - "[결정에 대한 간단한 제목]"
Cohesion: 0.13
Nodes (14): 가정, 결정, [결정에 대한 간단한 제목], 관련 결정, 관련 산출물, 관련 요구사항, 관련 원칙, 그룹 (+6 more)

### Community 58 - "[Korte titel van de beslissing]"
Cohesion: 0.13
Nodes (14): Aannames, Argument, Beperkingen, Beslissing, Gerelateerde artefacten, Gerelateerde beslissingen, Gerelateerde principes, Gerelateerde vereisten (+6 more)

### Community 59 - "[Título breve da decisão]"
Cohesion: 0.13
Nodes (14): Argumento, Artefatos relacionados, Decisão, Decisões relacionadas, Grupo, Implicações, Notas, Posições (+6 more)

### Community 61 - "AppConstants"
Cohesion: 0.25
Nodes (7): char, JsonDocumentOptions, Lazy, AppConstants, int, JsonSerializerOptions, string

### Community 62 - "[Краткое название решения]"
Cohesion: 0.13
Nodes (14): Аргументация, Группа, [Краткое название решения], Ограничения, Позиции, Предположения, Примечания, Проблема (+6 more)

### Community 63 - "[决策简要标题]"
Cohesion: 0.13
Nodes (14): 假设, 决策, [决策简要标题], 分组, 备注, 影响, 相关决策, 相关制品 (+6 more)

### Community 64 - "Fact"
Cohesion: 0.18
Nodes (5): date, DateTime, error, status, Fact

### Community 65 - ".IsValidCultureName"
Cohesion: 0.24
Nodes (3): JsonElement, InlineData, Theory

### Community 66 - "FormatMessages"
Cohesion: 0.40
Nodes (4): CompositeFormat, ConcurrentDictionary, FormatMessages, Func

### Community 67 - "[Brief title of the decision]"
Cohesion: 0.14
Nodes (13): [Brief title of the decision], Considered Options, Context and Problem Statement, Deciders, Decision Drivers <!-- optional -->, Decision Outcome, Links <!-- optional -->, Negative Consequences <!-- optional --> (+5 more)

### Community 68 - "AdrPlus"
Cohesion: 0.40
Nodes (4): AdrPlus.Resources, CultureInfo, ResourceManager, AdrPlus

### Community 69 - "Q: Why does IPromptConsole connect Migration wizard config to Init command handling, New ADR command, PromptConsole core UI, Approve command handling, Startup/DI services, App config wizard (editor), Repo config validation, Revise command handling, Migrate command handling, Explorer wizard prompts, Help command & router, Config command editing, Explorer report generation, Config field editing prompts, Infrastructure namespaces?"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: Why does IPromptConsole connect Migration wizard config to Init command handling, New ADR command, PromptConsole core UI, Approve command handling, Startup/DI services, App config wizard (editor), Repo config validation, Revise command handling, Migrate command handling, Explorer wizard prompts, Help command & router, Config command editing, Explorer report generation, Config field editing prompts, Infrastructure namespaces?, Source Nodes

### Community 70 - "Q: Should Init command handling be split into smaller, more focused modules?"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: Should Init command handling be split into smaller, more focused modules?, Source Nodes

### Community 71 - "Q: Why does IFileSystemService connect ADR header parsing to Init command handling, New ADR command, ADR status change service, and most other commands?"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: Why does IFileSystemService connect ADR header parsing to Init command handling, New ADR command, ADR status change service, and most other commands?, Source Nodes

### Community 72 - "[Kurzer Titel der Entscheidung]"
Cohesion: 0.14
Nodes (13): Betrachtete Optionen, Entscheidungstreiber <!-- optional -->, Entscheidungsträger, Ergebnis der Entscheidung, Kontext und Problemstellung, [Kurzer Titel der Entscheidung], Links <!-- optional -->, Negative Konsequenzen <!-- optional --> (+5 more)

### Community 73 - "[Título breve de la decisión]"
Cohesion: 0.14
Nodes (13): Consecuencias Negativas <!-- opcional -->, Consecuencias Positivas <!-- opcional -->, Contexto y Enunciado del Problema, Decisores, Enlaces <!-- opcional -->, Impulsores de la Decisión <!-- opcional -->, Opciones Consideradas, [opción 1] (+5 more)

### Community 74 - "[Titre bref de la décision]"
Cohesion: 0.14
Nodes (13): Avantages et inconvénients des options <!-- optionnel -->, Conséquences négatives <!-- optionnel -->, Conséquences positives <!-- optionnel -->, Contexte et énoncé du problème, Décideurs, Facteurs de décision <!-- optionnel -->, Liens <!-- optionnel -->, [option 1] (+5 more)

### Community 75 - "Test Architecture Guide"
Cohesion: 0.67
Nodes (4): Test Architecture Guide, CommandHandler Test Architecture, Mock Configuration Pattern, Supersede Test Refactoring Case Study

### Community 76 - "Root CLAUDE.md (project instructions)"
Cohesion: 0.67
Nodes (3): Default to --code-only for routine graphify builds on this repo, Root CLAUDE.md (project instructions), .graphifyignore (repo root)

### Community 77 - "[Titolo breve della decisione]"
Cohesion: 0.14
Nodes (13): Conseguenze Negative <!-- opzionale -->, Conseguenze Positive <!-- opzionale -->, Contesto e Definizione del Problema, Decisori, Driver della Decisione <!-- opzionale -->, Esito della Decisione, Link <!-- opzionale -->, [opzione 1] (+5 more)

### Community 79 - "[決定の簡潔なタイトル]"
Cohesion: 0.14
Nodes (13): リンク <!-- 任意 -->, 各選択肢の長所と短所 <!-- 任意 -->, 悪い影響 <!-- 任意 -->, 検討した選択肢, [決定の簡潔なタイトル], 決定内容, 決定者, 決定要因 <!-- 任意 --> (+5 more)

### Community 80 - "[결정에 대한 간단한 제목]"
Cohesion: 0.14
Nodes (13): 결정 결과, 결정 동인 <!-- 선택 사항 -->, [결정에 대한 간단한 제목], 결정자, 고려된 옵션, 긍정적 결과 <!-- 선택 사항 -->, 링크 <!-- 선택 사항 -->, 부정적 결과 <!-- 선택 사항 --> (+5 more)

### Community 81 - "[Korte titel van de beslissing]"
Cohesion: 0.14
Nodes (13): Beslissers, Beslissingsfactoren <!-- optioneel -->, Beslissingsresultaat, Context en probleemstelling, [Korte titel van de beslissing], Links <!-- optioneel -->, Negatieve gevolgen <!-- optioneel -->, [optie 1] (+5 more)

### Community 82 - "[Título breve da decisão]"
Cohesion: 0.14
Nodes (13): Consequências Negativas <!-- opcional -->, Consequências Positivas <!-- opcional -->, Contexto e Declaração do Problema, Decisores, Drivers de Decisão <!-- opcional -->, Links <!-- opcional -->, [opção 1], [opção 2] (+5 more)

### Community 83 - "[Краткое название решения]"
Cohesion: 0.14
Nodes (13): [вариант 1], [вариант 2], [вариант 3], Итог решения, Контекст и постановка проблемы, [Краткое название решения], Отрицательные последствия <!-- необязательно -->, Плюсы и минусы вариантов <!-- необязательно --> (+5 more)

### Community 84 - "[决策简要标题]"
Cohesion: 0.14
Nodes (13): [决策简要标题], 决策结果, 决策者, 决策驱动因素 <!-- 可选 -->, [备选方案 1], [备选方案 2], [备选方案 3], 备选方案的优缺点 <!-- 可选 --> (+5 more)

### Community 85 - "[Brief title of the decision]"
Cohesion: 0.14
Nodes (13): [Brief title of the decision], Considered Options, Context and Problem Statement, Deciders, Decision Drivers <!-- optional -->, Decision Outcome, Links <!-- optional -->, Negative Consequences <!-- optional --> (+5 more)

### Community 86 - "[Kurzer Titel der Entscheidung]"
Cohesion: 0.14
Nodes (13): Betrachtete Optionen, Entscheidungstreiber <!-- optional -->, Entscheidungsträger, Ergebnis der Entscheidung, Kontext und Problemstellung, [Kurzer Titel der Entscheidung], Links <!-- optional -->, Negative Konsequenzen <!-- optional --> (+5 more)

### Community 87 - "[Título breve de la decisión]"
Cohesion: 0.14
Nodes (13): Consecuencias Negativas <!-- opcional -->, Consecuencias Positivas <!-- opcional -->, Contexto y Enunciado del Problema, Decisores, Enlaces <!-- opcional -->, Impulsores de la Decisión <!-- opcional -->, Opciones Consideradas, [opción 1] (+5 more)

### Community 88 - "[Titre bref de la décision]"
Cohesion: 0.14
Nodes (13): Avantages et inconvénients des options <!-- optionnel -->, Conséquences négatives <!-- optionnel -->, Conséquences positives <!-- optionnel -->, Contexte et énoncé du problème, Décideurs, Facteurs de décision <!-- optionnel -->, Liens <!-- optionnel -->, [option 1] (+5 more)

### Community 89 - "[Titolo breve della decisione]"
Cohesion: 0.14
Nodes (13): Conseguenze Negative <!-- opzionale -->, Conseguenze Positive <!-- opzionale -->, Contesto e Definizione del Problema, Decisori, Driver della Decisione <!-- opzionale -->, Esito della Decisione, Link <!-- opzionale -->, [opzione 1] (+5 more)

### Community 90 - "CultureData"
Cohesion: 0.18
Nodes (8): Action, CultureData, TheoryData, HeaderLocalizationTests, InlineData, MemberData, Theory, TheoryData

### Community 91 - "[決定の簡潔なタイトル]"
Cohesion: 0.14
Nodes (13): リンク <!-- 任意 -->, 各選択肢の長所と短所 <!-- 任意 -->, 悪い影響 <!-- 任意 -->, 検討した選択肢, [決定の簡潔なタイトル], 決定内容, 決定者, 決定要因 <!-- 任意 --> (+5 more)

### Community 92 - "[결정에 대한 간단한 제목]"
Cohesion: 0.14
Nodes (13): 결정 결과, 결정 동인 <!-- 선택 사항 -->, [결정에 대한 간단한 제목], 결정자, 고려된 옵션, 긍정적 결과 <!-- 선택 사항 -->, 링크 <!-- 선택 사항 -->, 부정적 결과 <!-- 선택 사항 --> (+5 more)

### Community 93 - "[Korte titel van de beslissing]"
Cohesion: 0.14
Nodes (13): Beslissers, Beslissingsfactoren <!-- optioneel -->, Beslissingsresultaat, Context en probleemstelling, [Korte titel van de beslissing], Links <!-- optioneel -->, Negatieve gevolgen <!-- optioneel -->, [optie 1] (+5 more)

### Community 94 - "[Título breve da decisão]"
Cohesion: 0.14
Nodes (13): Consequências Negativas <!-- opcional -->, Consequências Positivas <!-- opcional -->, Contexto e Declaração do Problema, Decisores, Drivers da Decisão <!-- opcional -->, Links <!-- opcional -->, [opção 1], [opção 2] (+5 more)

### Community 95 - "[Краткое название решения]"
Cohesion: 0.14
Nodes (13): [вариант 1], [вариант 2], [вариант 3], Итог решения, Контекст и постановка проблемы, [Краткое название решения], Отрицательные последствия <!-- необязательно -->, Плюсы и минусы вариантов <!-- необязательно --> (+5 more)

### Community 96 - "[决策简要标题]"
Cohesion: 0.14
Nodes (13): [决策简要标题], 决策结果, 决策者, 决策驱动因素 <!-- 可选 -->, [备选方案 1], [备选方案 2], [备选方案 3], 备选方案的优缺点 <!-- 可选 --> (+5 more)

### Community 97 - "AdrStatus"
Cohesion: 0.14
Nodes (5): AdrHeader, DateTime, AdrStatus, InlineData, Theory

### Community 98 - "IMainProgram"
Cohesion: 0.40
Nodes (3): IMainProgram, CancellationToken, Task

### Community 99 - "[Brief title of the decision]"
Cohesion: 0.17
Nodes (11): Assumptions, Author, [Brief title of the decision], Defined, Gist, Owner, Priority, Rationale (+3 more)

### Community 100 - "[Kurzer Titel der Entscheidung]"
Cohesion: 0.17
Nodes (11): Anforderung, Annahmen, Autor, Begründung, Definiert, [Kurzer Titel der Entscheidung], Kurzfassung, Priorität (+3 more)

### Community 101 - "[Título breve de la decisión]"
Cohesion: 0.17
Nodes (11): Autor, Definido, Esencia, Justificación, Partes Interesadas, Prioridad, Requisito, Responsable (+3 more)

### Community 102 - "[Titre bref de la décision]"
Cohesion: 0.17
Nodes (11): Auteur, Défini, Essence, Exigence, Hypothèses, Justification, Parties prenantes, Priorité (+3 more)

### Community 103 - "[Titolo breve della decisione]"
Cohesion: 0.17
Nodes (11): Autore, Definizione, Essenza, Motivazione, Presupposti, Priorità, Requisito, Responsabile (+3 more)

### Community 104 - "[決定の簡潔なタイトル]"
Cohesion: 0.17
Nodes (11): ステークホルダー, リスク, 作成者, 優先度, 前提条件, 定義, 担当者, 根拠 (+3 more)

### Community 105 - "[결정에 대한 간단한 제목]"
Cohesion: 0.17
Nodes (11): 가정, [결정에 대한 간단한 제목], 근거, 담당자, 리스크, 요구사항, 요지, 우선순위 (+3 more)

### Community 106 - "[Korte titel van de beslissing]"
Cohesion: 0.17
Nodes (11): Aannames, Auteur, Belanghebbenden, Eigenaar, Essentie, Gedefinieerd, [Korte titel van de beslissing], Onderbouwing (+3 more)

### Community 107 - "[Título breve da decisão]"
Cohesion: 0.17
Nodes (11): Autor, Definido, Essência, Justificativa, Partes Interessadas, Pressupostos, Prioridade, Requisito (+3 more)

### Community 108 - "[Краткое название решения]"
Cohesion: 0.17
Nodes (11): Автор, Заинтересованные стороны, [Краткое название решения], Обоснование, Определение, Ответственный, Предположения, Приоритет (+3 more)

### Community 109 - "[决策简要标题]"
Cohesion: 0.17
Nodes (11): 优先级, 作者, 假设, [决策简要标题], 利益相关方, 定义, 概要, 理由 (+3 more)

### Community 110 - ".PromptSelectLogicalDrive"
Cohesion: 0.12
Nodes (19): Adrfiles, ArgsWizard, MigrateCommandHandler, Arguments, CancellationToken, Dictionary, IEnumerable, ILogger (+11 more)

### Community 116 - "Helper"
Cohesion: 0.38
Nodes (5): bool, Helper, GeneratedRegex, int, Regex

### Community 117 - "[Brief title of the decision]"
Cohesion: 0.29
Nodes (6): [Brief title of the decision], Candidates to consider, Evaluation criteria, Recommendation, Research and analysis of each candidate, Summary

### Community 118 - "[Kurzer Titel der Entscheidung]"
Cohesion: 0.29
Nodes (6): Bewertungskriterien, Empfehlung, [Kurzer Titel der Entscheidung], Recherche und Analyse jedes Kandidaten, Zu berücksichtigende Kandidaten, Zusammenfassung

### Community 119 - "[Título breve de la decisión]"
Cohesion: 0.29
Nodes (6): Candidatos a considerar, Criterios de evaluación, Investigación y análisis de cada candidato, Recomendación, Resumen, [Título breve de la decisión]

### Community 120 - "[Titre bref de la décision]"
Cohesion: 0.29
Nodes (6): Candidats à considérer, Critères d'évaluation, Recherche et analyse de chaque candidat, Recommandation, Résumé, [Titre bref de la décision]

### Community 121 - "[Titolo breve della decisione]"
Cohesion: 0.29
Nodes (6): Candidati da considerare, Criteri di valutazione, Raccomandazione, Ricerca e analisi di ciascun candidato, Riepilogo, [Titolo breve della decisione]

### Community 122 - "[決定の簡潔なタイトル]"
Cohesion: 0.29
Nodes (6): 各候補の調査と分析, 推奨事項, 検討する候補, 概要, [決定の簡潔なタイトル], 評価基準

### Community 123 - "[결정에 대한 간단한 제목]"
Cohesion: 0.29
Nodes (6): 각 후보에 대한 조사 및 분석, [결정에 대한 간단한 제목], 고려할 후보, 권장 사항, 요약, 평가 기준

### Community 124 - "[Korte titel van de beslissing]"
Cohesion: 0.29
Nodes (6): Aanbeveling, Evaluatiecriteria, [Korte titel van de beslissing], Onderzoek en analyse van elke kandidaat, Samenvatting, Te overwegen kandidaten

### Community 125 - "[Título breve da decisão]"
Cohesion: 0.29
Nodes (6): Candidatos a considerar, Critérios de avaliação, Pesquisa e análise de cada candidato, Recomendação, Resumo, [Título breve da decisão]

### Community 126 - "[Краткое название решения]"
Cohesion: 0.29
Nodes (6): Исследование и анализ каждого кандидата, Кандидаты для рассмотрения, [Краткое название решения], Критерии оценки, Резюме, Рекомендация

### Community 127 - "[决策简要标题]"
Cohesion: 0.29
Nodes (6): [决策简要标题], 各候选方案的研究与分析, 建议, 待考虑的候选方案, 摘要, 评估标准

### Community 128 - ".ExecuteAsync"
Cohesion: 0.40
Nodes (3): ICommandHandler, CancellationToken, Task

### Community 130 - "[Brief title of the decision]"
Cohesion: 0.40
Nodes (4): [Brief title of the decision], Consequences, Decision, Rationale

### Community 131 - "[Kurzer Titel der Entscheidung]"
Cohesion: 0.40
Nodes (4): Begründung, Entscheidung, Konsequenzen, [Kurzer Titel der Entscheidung]

### Community 132 - "[Título breve de la decisión]"
Cohesion: 0.40
Nodes (4): Consecuencias, Decisión, Justificación, [Título breve de la decisión]

### Community 133 - "[Titre bref de la décision]"
Cohesion: 0.40
Nodes (4): Conséquences, Décision, Justification, [Titre bref de la décision]

### Community 134 - "Select ADR templates based on configured UI language"
Cohesion: 0.13
Nodes (12): Considered Options, Context and Problem Statement, Decision Outcome, Links, Negative Consequences, Positive Consequences, Select ADR templates based on configured UI language, Accepted (+4 more)

### Community 135 - "[Titolo breve della decisione]"
Cohesion: 0.40
Nodes (4): Conseguenze, Decisione, Motivazione, [Titolo breve della decisione]

### Community 137 - "[決定の簡潔なタイトル]"
Cohesion: 0.40
Nodes (4): 影響, 根拠, 決定, [決定の簡潔なタイトル]

### Community 138 - "[결정에 대한 간단한 제목]"
Cohesion: 0.40
Nodes (4): 결과, 결정, [결정에 대한 간단한 제목], 근거

### Community 139 - "[Korte titel van de beslissing]"
Cohesion: 0.40
Nodes (4): Beslissing, Gevolgen, [Korte titel van de beslissing], Onderbouwing

### Community 140 - "[Título breve da decisão]"
Cohesion: 0.40
Nodes (4): Consequências, Decisão, Justificativa, [Título breve da decisão]

### Community 141 - "[Краткое название решения]"
Cohesion: 0.40
Nodes (4): [Краткое название решения], Обоснование, Последствия, Решение

### Community 142 - "[决策简要标题]"
Cohesion: 0.40
Nodes (4): 决策, [决策简要标题], 后果, 理由

### Community 143 - "[Brief title of the decision]"
Cohesion: 0.40
Nodes (4): [Brief title of the decision], Consequences, Context, Decision

### Community 144 - "[Kurzer Titel der Entscheidung]"
Cohesion: 0.40
Nodes (4): Entscheidung, Konsequenzen, Kontext, [Kurzer Titel der Entscheidung]

### Community 145 - "[Título breve de la decisión]"
Cohesion: 0.40
Nodes (4): Consecuencias, Contexto, Decisión, [Título breve de la decisión]

### Community 146 - "[Titre bref de la décision]"
Cohesion: 0.40
Nodes (4): Conséquences, Contexte, Décision, [Titre bref de la décision]

### Community 147 - "[Titolo breve della decisione]"
Cohesion: 0.40
Nodes (4): Conseguenze, Contesto, Decisione, [Titolo breve della decisione]

### Community 148 - "[決定の簡潔なタイトル]"
Cohesion: 0.40
Nodes (4): 影響, 決定, [決定の簡潔なタイトル], 背景

### Community 149 - "[결정에 대한 간단한 제목]"
Cohesion: 0.40
Nodes (4): 결과, 결정, [결정에 대한 간단한 제목], 컨텍스트

### Community 150 - "[Korte titel van de beslissing]"
Cohesion: 0.40
Nodes (4): Beslissing, Context, Gevolgen, [Korte titel van de beslissing]

### Community 151 - "[Título breve da decisão]"
Cohesion: 0.40
Nodes (4): Consequências, Contexto, Decisão, [Título breve da decisão]

### Community 152 - "[Краткое название решения]"
Cohesion: 0.40
Nodes (4): Контекст, [Краткое название решения], Последствия, Решение

### Community 153 - "[决策简要标题]"
Cohesion: 0.40
Nodes (4): 决策, [决策简要标题], 后果, 背景

## Knowledge Gaps
- **698 isolated node(s):** `net10.0`, `Microsoft.NET.Sdk`, `net10.0`, `Microsoft.Extensions.Hosting (10.0.10)`, `PromptPlus (6.0.0-Beta7)` (+693 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **12 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `IFileSystemService` connect `IFileSystemService` to `.DirectoryExists`, `.StatusUpdateAdrAsync`, `ValidateJsonConfigTests`, `.ParseFileName`, `.ParseArgs`, `FileSystemServiceEnhancedTests`, `.ValidateRepoStructure`, `PromptConsole`, `ValidateConfig`, `AdrPlus.Infrastructure.FileSystem`, `HelpCommandHandlerTests`, `UndoStatusCommandHandlerTests`, `AdrPlusRepoConfig`, `ConfigCommandHandler`, `IValidateConfig`, `.GetFullNameFile`, `WizardCommandHandler`, `.StatusChangeAdrAsync`, `AdrService`, `PathHelper`, `ConfigCommandHandlerTests`, `.PromptSelectLogicalDrive`?**
  _High betweenness centrality (0.060) - this node is a cross-community bridge._
- **Why does `PromptConsole` connect `PromptConsole` to `IValidateConfig`, `IFileSystemService`, `.GetFullNameFile`, `FieldsJson`, `WizardCommandHandler`, `.ParseFileName`, `AdrFileNameComponents`, `IConsoleWriter`, `.PromptSelectLogicalDrive`, `HelpCommandHandlerTests`, `AdrPlus.Infrastructure.UI`?**
  _High betweenness centrality (0.046) - this node is a cross-community bridge._
- **Why does `IAdrServices` connect `.GetFullNameFile` to `.DirectoryExists`, `.StatusUpdateAdrAsync`, `IFileSystemService`, `.ParseFileName`, `WizardCommandHandler`, `.ParseArgs`, `ValidateJsonConfigTests`, `.ValidateRepoStructure`, `.PromptSelectLogicalDrive`, `PromptConsole`, `PathHelper`, `ValidateConfig`, `AdrPlus.Infrastructure.FileSystem`, `HelpCommandHandlerTests`, `UndoStatusCommandHandlerTests`, `ConfigCommandHandlerTests`, `ConfigCommandHandler`?**
  _High betweenness centrality (0.044) - this node is a cross-community bridge._
- **What connects `net10.0`, `Microsoft.NET.Sdk`, `net10.0` to the rest of the system?**
  _698 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `.StatusUpdateAdrAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.06030701754385965 - nodes in this community are weakly interconnected._
- **Should `ValidateJsonConfigTests` be split into smaller, more focused modules?**
  _Cohesion score 0.07211646136618141 - nodes in this community are weakly interconnected._
- **Should `.ParseFileName` be split into smaller, more focused modules?**
  _Cohesion score 0.058394160583941604 - nodes in this community are weakly interconnected._