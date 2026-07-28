# Graph Report - AdrPlus  (2026-07-28)

## Corpus Check
- 205 files · ~166,071 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 2913 nodes · 8944 edges · 138 communities (128 shown, 10 thin omitted)
- Extraction: 70% EXTRACTED · 30% INFERRED · 0% AMBIGUOUS · INFERRED: 2689 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `c19f9eab`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- .ParseArgs
- IAdrServices
- ValidateJsonConfigTests
- .ParseFileName
- IConfigPrompts
- .ExploreWizardAsync
- FileSystemServiceEnhancedTests
- PatternParserTests
- HelperTests
- TemplateResourcesTests
- [Kurzer Titel der Entscheidung]
- .StatusUpdateAdrAsync
- WizardCommandHandler
- Help command & router
- AdrServiceTests
- PromptConsole
- AdrPlusRepoConfigTests
- ValidateConfig
- AdrPlus README
- AdrPlus.Domain
- [Título breve de la decisión]
- AdrPlus.Infrastructure.FileSystem
- HelpCommandHandlerTests
- CancellationToken
- AdrRecordTests
- HelpUsageAttributeTests
- AdrPlus CHANGELOG
- Task
- [Titre bref de la décision]
- ADR record model tests
- ServiceCollectionExtensions.cs
- AdrHeaderTests
- [Titolo breve della decisione]
- IFileSystemService
- ADR discovery/query service
- IConsoleWriter
- .RouteAsync
- [決定の簡潔なタイトル]
- VersionCommandHandler
- .StatusChangeAdrAsync
- AdrPlus.Tests.csproj
- [결정에 대한 간단한 제목]
- .LogCommandException
- AdrPlusRepoConfig
- .ResolveAppVersion
- StringCaseExtensionsTests
- PathHelper
- .PromptGetArrayDomainsAdr
- LowercaseNamingPolicyTests
- [Korte titel van de beslissing]
- [Título breve da decisão]
- [Краткое название решения]
- AdrService
- IValidateConfig
- [决策简要标题]
- CommandAttribute
- [Kurzer Titel der Entscheidung]
- [Título breve de la decisión]
- [Titre bref de la décision]
- [Titolo breve della decisione]
- AdrStatusTests
- AppConstants
- .PromptWriteSuccess
- [決定の簡潔なタイトル]
- AdrPlus.Tests.Core
- [결정에 대한 간단한 제목]
- FormatMessages
- [Korte titel van de beslissing]
- AdrPlus
- Q: Why does IPromptConsole connect Migration wizard config to Init command handling, New ADR command, PromptConsole core UI, Approve command handling, Startup/DI services, App config wizard (editor), Repo config validation, Revise command handling, Migrate command handling, Explorer wizard prompts, Help command & router, Config command editing, Explorer report generation, Config field editing prompts, Infrastructure namespaces?
- Q: Should Init command handling be split into smaller, more focused modules?
- Q: Why does IFileSystemService connect ADR header parsing to Init command handling, New ADR command, ADR status change service, and most other commands?
- .ExecuteAsync
- [Título breve da decisão]
- [Краткое название решения]
- Test Architecture Guide
- Root CLAUDE.md (project instructions)
- [决策简要标题]
- [Kurzer Titel der Entscheidung]
- [Título breve de la decisión]
- [Titre bref de la décision]
- [Titolo breve della decisione]
- [決定の簡潔なタイトル]
- [결정에 대한 간단한 제목]
- [Korte titel van de beslissing]
- .PromptSelecAdrs
- [Título breve da decisão]
- [Краткое название решения]
- [决策简要标题]
- CultureData
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
- .PromptGetArrayDomainsAdr
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
- IMainProgram
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
- Translations
- RepoActions.cs
- CommandRouterTests.cs
- BehaviorWithoutArg.cs

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

## Communities (138 total, 10 thin omitted)

### Community 0 - ".ParseArgs"
Cohesion: 0.05
Nodes (47): InitCommandHandler, Arguments, CancellationToken, Dictionary, ILogger, List, Task, UndoStatusCommandHandler (+39 more)

### Community 1 - "IAdrServices"
Cohesion: 0.06
Nodes (34): Arguments, NewAdrCommandHandler, Arguments, CancellationToken, DateTime, Dictionary, ILogger, Task (+26 more)

### Community 2 - "ValidateJsonConfigTests"
Cohesion: 0.07
Nodes (4): ValidateJsonConfigTests, Dictionary, Fact, Task

### Community 3 - ".ParseFileName"
Cohesion: 0.15
Nodes (8): CancellationToken, Task, ReviseCommandHandlerTests, Dictionary, Fact, ILogger, string, Task

### Community 4 - "IConfigPrompts"
Cohesion: 0.17
Nodes (11): JsonConfig, PrefixValue, IConfigPrompts, CancellationToken, Content, FieldsFromFileAdr, IEnumerable, IsAborted (+3 more)

### Community 5 - ".ExploreWizardAsync"
Cohesion: 0.14
Nodes (17): Fields, IOptions, ExploreCommandHandler, Arguments, CancellationToken, Dictionary, ILogger, Task (+9 more)

### Community 6 - "FileSystemServiceEnhancedTests"
Cohesion: 0.06
Nodes (20): AdrPlus.Tests.Infrastructure.FileSystem, FileSystemService, CancellationToken, IEnumerable, JsonSerializerOptions, Result, SearchOption, Success (+12 more)

### Community 7 - "PatternParserTests"
Cohesion: 0.06
Nodes (20): editorcmd, hasRider, hasVisualStudio, hasVSCode, Length, Position, PatternParser, Dictionary (+12 more)

### Community 8 - "HelperTests"
Cohesion: 0.05
Nodes (16): bool, date, Helper, DateTime, error, GeneratedRegex, int, JsonElement (+8 more)

### Community 9 - "TemplateResourcesTests"
Cohesion: 0.15
Nodes (10): AdrPlus.Tests.Localization, SatelliteResourcesTests, MemberData, Theory, TheoryData, TemplateResourcesTests, MemberData, string (+2 more)

### Community 10 - "[Kurzer Titel der Entscheidung]"
Cohesion: 0.13
Nodes (14): Annahmen, Argument, Einschränkungen, Entscheidung, Gruppe, Implikationen, [Kurzer Titel der Entscheidung], Notizen (+6 more)

### Community 11 - ".StatusUpdateAdrAsync"
Cohesion: 0.05
Nodes (35): ApproveCommandHandler, Arguments, CancellationToken, DateTime, Dictionary, ILogger, Task, RejectCommandHandler (+27 more)

### Community 12 - "WizardCommandHandler"
Cohesion: 0.18
Nodes (15): NotImplementedException, WizardCommandHandler, Arguments, CancellationToken, CommandRouter, CommandsAdr, IConfiguration, ILogger (+7 more)

### Community 14 - "AdrServiceTests"
Cohesion: 0.12
Nodes (3): AdrServiceTests, Fact, IConfiguration

### Community 15 - "PromptConsole"
Cohesion: 0.05
Nodes (15): Color, FrozenDictionary, PromptConsole, ConfirmYes, CountSelected, FieldsExplore, FieldsFromFileAdr, Filename (+7 more)

### Community 16 - "AdrPlusRepoConfigTests"
Cohesion: 0.13
Nodes (3): AdrPlusRepoConfigTests, Fact, string

### Community 17 - "ValidateConfig"
Cohesion: 0.11
Nodes (13): JsonNode, ValidateConfig, CancellationToken, Dictionary, ErrorReport, IConfiguration, IsValid, JsonElement (+5 more)

### Community 18 - "AdrPlus README"
Cohesion: 0.14
Nodes (26): Architecture Decision Record (ADR), FAQ (referenced, not read), AdrPlus Icon, Migration Guide, AdrPlus Migrated Header Table Format, ADR Migration Process, Migration Prerequisites, NuGet README (+18 more)

### Community 19 - "AdrPlus.Domain"
Cohesion: 0.12
Nodes (9): AdrPlus.Infrastructure.UI, AdrPlus.Extensions, AdrPlus.Domain, AdrPlus.Infrastructure.Logging, AdrPlus.Infrastructure.Configuration, AdrPlus, AdrPlus.Tests.Domain, AdrPlus.Infrastructure.Formatting (+1 more)

### Community 20 - "[Título breve de la decisión]"
Cohesion: 0.13
Nodes (14): Argumento, Artefactos relacionados, Decisiones relacionadas, Decisión, Grupo, Implicaciones, Notas, Posiciones (+6 more)

### Community 21 - "AdrPlus.Infrastructure.FileSystem"
Cohesion: 0.10
Nodes (15): AdrPlus.Tests.Commands.Reject, AdrPlus.Tests.Commands.NewAdr, AdrPlus.Tests.Commands.Explore, AdrPlus.Tests.Commands.Attributes, AdrPlus.Tests.Commands.Approve, AdrPlus.Tests.Commands.Revise, AdrPlus.Tests.Helpers, AdrPlus.Infrastructure.FileSystem (+7 more)

### Community 22 - "HelpCommandHandlerTests"
Cohesion: 0.15
Nodes (11): CancellationToken, Task, Alias, Command, ConfigCommandHandler, Description, Dictionary, Type (+3 more)

### Community 23 - "CancellationToken"
Cohesion: 0.14
Nodes (9): FieldsJson, JsonValueKind, CancellationToken, Content, DateTime, IEnumerable, IsAborted, PrefixValue (+1 more)

### Community 24 - "AdrRecordTests"
Cohesion: 0.08
Nodes (17): Adrfiles, ArgsWizard, MigrateCommandHandler, Arguments, CancellationToken, Dictionary, IEnumerable, ILogger (+9 more)

### Community 26 - "AdrPlus CHANGELOG"
Cohesion: 0.12
Nodes (26): approve command, config command, AdrPlus CHANGELOG, explorer command, help command, init command, Keep a Changelog format, migrate command (+18 more)

### Community 27 - "Task"
Cohesion: 0.12
Nodes (3): Task, SearchOption, Task

### Community 28 - "[Titre bref de la décision]"
Cohesion: 0.13
Nodes (14): Argumentation, Artefacts connexes, Contraintes, Décision, Décisions connexes, Exigences connexes, Groupe, Hypothèses (+6 more)

### Community 30 - "ServiceCollectionExtensions.cs"
Cohesion: 0.14
Nodes (17): AdrPlus.Commands.Migrate, AdrPlus.Commands.UndoStatus, AdrPlus.Commands.Config, AdrPlus.Commands.Explore, AdrPlus.Commands.Revise, AdrPlus.Tests.Commands.Help, AdrPlus.Commands.Approve, AdrPlus.Commands.Init (+9 more)

### Community 32 - "[Titolo breve della decisione]"
Cohesion: 0.13
Nodes (14): Argomentazione, Artefatti correlati, Decisione, Decisioni correlate, Gruppo, Implicazioni, Note, Posizioni (+6 more)

### Community 33 - "IFileSystemService"
Cohesion: 0.11
Nodes (9): content, header, IFileSystemService, CancellationToken, IEnumerable, Result, Success, Task (+1 more)

### Community 35 - "IConsoleWriter"
Cohesion: 0.08
Nodes (17): IOptionsMonitor, IServiceProvider, CommandRouter, Dictionary, IConfiguration, ILogger, Type, IConfigurationMigrator (+9 more)

### Community 36 - ".RouteAsync"
Cohesion: 0.29
Nodes (5): CancellationToken, Task, CommandRouterTests, Fact, Task

### Community 37 - "[決定の簡潔なタイトル]"
Cohesion: 0.13
Nodes (14): グループ, 備考, 制約, 前提条件, 決定, [決定の簡潔なタイトル], 波及効果, 見解 (+6 more)

### Community 38 - "VersionCommandHandler"
Cohesion: 0.17
Nodes (9): ICommandHandler, HelpCommandHandler, CommandRouter, ILogger, VersionCommandHandler, Arguments, DateTime, Dictionary (+1 more)

### Community 39 - ".StatusChangeAdrAsync"
Cohesion: 0.13
Nodes (5): CancellationToken, DateTime, Error, Isvalid, IsValid

### Community 40 - "AdrPlus.Tests.csproj"
Cohesion: 0.10
Nodes (17): coverlet.collector (10.0.1), FluentAssertions (8.10.0), Microsoft.Extensions.Hosting (10.0.8), Microsoft.NET.Test.Sdk (18.6.0), NSubstitute (5.3.0), PromptPlus (6.0.0-Beta4), Serilog.Extensions.Logging.File (3.0.0), xunit.runner.visualstudio (3.1.5) (+9 more)

### Community 41 - "[결정에 대한 간단한 제목]"
Cohesion: 0.13
Nodes (14): 가정, 결정, [결정에 대한 간단한 제목], 관련 결정, 관련 산출물, 관련 요구사항, 관련 원칙, 그룹 (+6 more)

### Community 42 - ".LogCommandException"
Cohesion: 0.26
Nodes (4): LoggerMessage, LogMessages, Exception, ILogger

### Community 43 - "AdrPlusRepoConfig"
Cohesion: 0.16
Nodes (10): Result, Success, content, header, AdrFileNameComponents, AdrHeader, DateTime, AdrPlusRepoConfig (+2 more)

### Community 44 - ".ResolveAppVersion"
Cohesion: 0.19
Nodes (8): Assembly, CancellationTokenSource, AdrPlus.Tests, Program, Task, ProgramTests, Fact, Version

### Community 45 - "StringCaseExtensionsTests"
Cohesion: 0.09
Nodes (12): CaseFormat, StringCaseExtensions, GeneratedRegex, Regex, StringCaseExtensionsTests, Fact, InlineData, Theory (+4 more)

### Community 46 - "PathHelper"
Cohesion: 0.10
Nodes (7): ExploreCommandHandlerTests, Fact, Task, ExploreCommandHandlerMockHelper, DateTime, Dictionary, PathHelper

### Community 47 - ".PromptGetArrayDomainsAdr"
Cohesion: 0.40
Nodes (3): domains, Exception, Task

### Community 48 - "LowercaseNamingPolicyTests"
Cohesion: 0.29
Nodes (6): JsonNamingPolicy, LowercaseNamingPolicy, LowercaseNamingPolicyTests, Fact, InlineData, Theory

### Community 49 - "[Korte titel van de beslissing]"
Cohesion: 0.13
Nodes (14): Aannames, Argument, Beperkingen, Beslissing, Gerelateerde artefacten, Gerelateerde beslissingen, Gerelateerde principes, Gerelateerde vereisten (+6 more)

### Community 50 - "[Título breve da decisão]"
Cohesion: 0.13
Nodes (14): Argumento, Artefatos relacionados, Decisão, Decisões relacionadas, Grupo, Implicações, Notas, Posições (+6 more)

### Community 51 - "[Краткое название решения]"
Cohesion: 0.13
Nodes (14): Аргументация, Группа, [Краткое название решения], Ограничения, Позиции, Предположения, Примечания, Проблема (+6 more)

### Community 52 - "AdrService"
Cohesion: 0.12
Nodes (12): IAdrServices, CommandsAdr, AdrService, Alias, Command, ConfigCommandHandler, Description, Dictionary (+4 more)

### Community 53 - "IValidateConfig"
Cohesion: 0.05
Nodes (29): EditField, ConfigCommandHandler, Arguments, CancellationToken, Content, Func, ILogger, IsAborted (+21 more)

### Community 54 - "[决策简要标题]"
Cohesion: 0.13
Nodes (14): 假设, 决策, [决策简要标题], 分组, 备注, 影响, 相关决策, 相关制品 (+6 more)

### Community 55 - "CommandAttribute"
Cohesion: 0.17
Nodes (8): Attribute, CommandArgumentAttribute, CommandAttribute, string, Type, HelpUsageAttribute, string, UsageArgumments

### Community 56 - "[Kurzer Titel der Entscheidung]"
Cohesion: 0.14
Nodes (13): Betrachtete Optionen, Entscheidungstreiber <!-- optional -->, Entscheidungsträger, Ergebnis der Entscheidung, Kontext und Problemstellung, [Kurzer Titel der Entscheidung], Links <!-- optional -->, Negative Konsequenzen <!-- optional --> (+5 more)

### Community 57 - "[Título breve de la decisión]"
Cohesion: 0.14
Nodes (13): Consecuencias Negativas <!-- opcional -->, Consecuencias Positivas <!-- opcional -->, Contexto y Enunciado del Problema, Decisores, Enlaces <!-- opcional -->, Impulsores de la Decisión <!-- opcional -->, Opciones Consideradas, [opción 1] (+5 more)

### Community 58 - "[Titre bref de la décision]"
Cohesion: 0.14
Nodes (13): Avantages et inconvénients des options <!-- optionnel -->, Conséquences négatives <!-- optionnel -->, Conséquences positives <!-- optionnel -->, Contexte et énoncé du problème, Décideurs, Facteurs de décision <!-- optionnel -->, Liens <!-- optionnel -->, [option 1] (+5 more)

### Community 59 - "[Titolo breve della decisione]"
Cohesion: 0.14
Nodes (13): Conseguenze Negative <!-- opzionale -->, Conseguenze Positive <!-- opzionale -->, Contesto e Definizione del Problema, Decisori, Driver della Decisione <!-- opzionale -->, Esito della Decisione, Link <!-- opzionale -->, [opzione 1] (+5 more)

### Community 61 - "AppConstants"
Cohesion: 0.25
Nodes (7): char, JsonDocumentOptions, Lazy, AppConstants, int, JsonSerializerOptions, string

### Community 62 - ".PromptWriteSuccess"
Cohesion: 0.18
Nodes (5): ReviseCommandHandler, Arguments, DateTime, Dictionary, ILogger

### Community 63 - "[決定の簡潔なタイトル]"
Cohesion: 0.14
Nodes (13): リンク <!-- 任意 -->, 各選択肢の長所と短所 <!-- 任意 -->, 悪い影響 <!-- 任意 -->, 検討した選択肢, [決定の簡潔なタイトル], 決定内容, 決定者, 決定要因 <!-- 任意 --> (+5 more)

### Community 64 - "AdrPlus.Tests.Core"
Cohesion: 0.19
Nodes (3): AdrPlus.Tests.Core, ItemMenuWizardTests, Fact

### Community 65 - "[결정에 대한 간단한 제목]"
Cohesion: 0.14
Nodes (13): 결정 결과, 결정 동인 <!-- 선택 사항 -->, [결정에 대한 간단한 제목], 결정자, 고려된 옵션, 긍정적 결과 <!-- 선택 사항 -->, 링크 <!-- 선택 사항 -->, 부정적 결과 <!-- 선택 사항 --> (+5 more)

### Community 66 - "FormatMessages"
Cohesion: 0.40
Nodes (4): CompositeFormat, ConcurrentDictionary, FormatMessages, Func

### Community 67 - "[Korte titel van de beslissing]"
Cohesion: 0.14
Nodes (13): Beslissers, Beslissingsfactoren <!-- optioneel -->, Beslissingsresultaat, Context en probleemstelling, [Korte titel van de beslissing], Links <!-- optioneel -->, Negatieve gevolgen <!-- optioneel -->, [optie 1] (+5 more)

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

### Community 72 - ".ExecuteAsync"
Cohesion: 0.40
Nodes (3): ICommandHandler, CancellationToken, Task

### Community 73 - "[Título breve da decisão]"
Cohesion: 0.14
Nodes (13): Consequências Negativas <!-- opcional -->, Consequências Positivas <!-- opcional -->, Contexto e Declaração do Problema, Decisores, Drivers de Decisão <!-- opcional -->, Links <!-- opcional -->, [opção 1], [opção 2] (+5 more)

### Community 74 - "[Краткое название решения]"
Cohesion: 0.14
Nodes (13): [вариант 1], [вариант 2], [вариант 3], Итог решения, Контекст и постановка проблемы, [Краткое название решения], Отрицательные последствия <!-- необязательно -->, Плюсы и минусы вариантов <!-- необязательно --> (+5 more)

### Community 75 - "Test Architecture Guide"
Cohesion: 0.67
Nodes (4): Test Architecture Guide, CommandHandler Test Architecture, Mock Configuration Pattern, Supersede Test Refactoring Case Study

### Community 76 - "Root CLAUDE.md (project instructions)"
Cohesion: 0.67
Nodes (3): Default to --code-only for routine graphify builds on this repo, Root CLAUDE.md (project instructions), .graphifyignore (repo root)

### Community 77 - "[决策简要标题]"
Cohesion: 0.14
Nodes (13): [决策简要标题], 决策结果, 决策者, 决策驱动因素 <!-- 可选 -->, [备选方案 1], [备选方案 2], [备选方案 3], 备选方案的优缺点 <!-- 可选 --> (+5 more)

### Community 79 - "[Kurzer Titel der Entscheidung]"
Cohesion: 0.14
Nodes (13): Betrachtete Optionen, Entscheidungstreiber <!-- optional -->, Entscheidungsträger, Ergebnis der Entscheidung, Kontext und Problemstellung, [Kurzer Titel der Entscheidung], Links <!-- optional -->, Negative Konsequenzen <!-- optional --> (+5 more)

### Community 80 - "[Título breve de la decisión]"
Cohesion: 0.14
Nodes (13): Consecuencias Negativas <!-- opcional -->, Consecuencias Positivas <!-- opcional -->, Contexto y Enunciado del Problema, Decisores, Enlaces <!-- opcional -->, Impulsores de la Decisión <!-- opcional -->, Opciones Consideradas, [opción 1] (+5 more)

### Community 81 - "[Titre bref de la décision]"
Cohesion: 0.14
Nodes (13): Avantages et inconvénients des options <!-- optionnel -->, Conséquences négatives <!-- optionnel -->, Conséquences positives <!-- optionnel -->, Contexte et énoncé du problème, Décideurs, Facteurs de décision <!-- optionnel -->, Liens <!-- optionnel -->, [option 1] (+5 more)

### Community 82 - "[Titolo breve della decisione]"
Cohesion: 0.14
Nodes (13): Conseguenze Negative <!-- opzionale -->, Conseguenze Positive <!-- opzionale -->, Contesto e Definizione del Problema, Decisori, Driver della Decisione <!-- opzionale -->, Esito della Decisione, Link <!-- opzionale -->, [opzione 1] (+5 more)

### Community 83 - "[決定の簡潔なタイトル]"
Cohesion: 0.14
Nodes (13): リンク <!-- 任意 -->, 各選択肢の長所と短所 <!-- 任意 -->, 悪い影響 <!-- 任意 -->, 検討した選択肢, [決定の簡潔なタイトル], 決定内容, 決定者, 決定要因 <!-- 任意 --> (+5 more)

### Community 84 - "[결정에 대한 간단한 제목]"
Cohesion: 0.14
Nodes (13): 결정 결과, 결정 동인 <!-- 선택 사항 -->, [결정에 대한 간단한 제목], 결정자, 고려된 옵션, 긍정적 결과 <!-- 선택 사항 -->, 링크 <!-- 선택 사항 -->, 부정적 결과 <!-- 선택 사항 --> (+5 more)

### Community 85 - "[Korte titel van de beslissing]"
Cohesion: 0.14
Nodes (13): Beslissers, Beslissingsfactoren <!-- optioneel -->, Beslissingsresultaat, Context en probleemstelling, [Korte titel van de beslissing], Links <!-- optioneel -->, Negatieve gevolgen <!-- optioneel -->, [optie 1] (+5 more)

### Community 87 - "[Título breve da decisão]"
Cohesion: 0.14
Nodes (13): Consequências Negativas <!-- opcional -->, Consequências Positivas <!-- opcional -->, Contexto e Declaração do Problema, Decisores, Drivers da Decisão <!-- opcional -->, Links <!-- opcional -->, [opção 1], [opção 2] (+5 more)

### Community 88 - "[Краткое название решения]"
Cohesion: 0.14
Nodes (13): [вариант 1], [вариант 2], [вариант 3], Итог решения, Контекст и постановка проблемы, [Краткое название решения], Отрицательные последствия <!-- необязательно -->, Плюсы и минусы вариантов <!-- необязательно --> (+5 more)

### Community 89 - "[决策简要标题]"
Cohesion: 0.14
Nodes (13): [决策简要标题], 决策结果, 决策者, 决策驱动因素 <!-- 可选 -->, [备选方案 1], [备选方案 2], [备选方案 3], 备选方案的优缺点 <!-- 可选 --> (+5 more)

### Community 90 - "CultureData"
Cohesion: 0.18
Nodes (8): Action, CultureData, TheoryData, HeaderLocalizationTests, InlineData, MemberData, Theory, TheoryData

### Community 91 - "[Kurzer Titel der Entscheidung]"
Cohesion: 0.17
Nodes (11): Anforderung, Annahmen, Autor, Begründung, Definiert, [Kurzer Titel der Entscheidung], Kurzfassung, Priorität (+3 more)

### Community 92 - "[Título breve de la decisión]"
Cohesion: 0.17
Nodes (11): Autor, Definido, Esencia, Justificación, Partes Interesadas, Prioridad, Requisito, Responsable (+3 more)

### Community 93 - "[Titre bref de la décision]"
Cohesion: 0.17
Nodes (11): Auteur, Défini, Essence, Exigence, Hypothèses, Justification, Parties prenantes, Priorité (+3 more)

### Community 94 - "[Titolo breve della decisione]"
Cohesion: 0.17
Nodes (11): Autore, Definizione, Essenza, Motivazione, Presupposti, Priorità, Requisito, Responsabile (+3 more)

### Community 96 - "[決定の簡潔なタイトル]"
Cohesion: 0.17
Nodes (11): ステークホルダー, リスク, 作成者, 優先度, 前提条件, 定義, 担当者, 根拠 (+3 more)

### Community 97 - "[결정에 대한 간단한 제목]"
Cohesion: 0.17
Nodes (11): 가정, [결정에 대한 간단한 제목], 근거, 담당자, 리스크, 요구사항, 요지, 우선순위 (+3 more)

### Community 98 - "[Korte titel van de beslissing]"
Cohesion: 0.17
Nodes (11): Aannames, Auteur, Belanghebbenden, Eigenaar, Essentie, Gedefinieerd, [Korte titel van de beslissing], Onderbouwing (+3 more)

### Community 99 - "[Título breve da decisão]"
Cohesion: 0.17
Nodes (11): Autor, Definido, Essência, Justificativa, Partes Interessadas, Pressupostos, Prioridade, Requisito (+3 more)

### Community 100 - "[Краткое название решения]"
Cohesion: 0.17
Nodes (11): Автор, Заинтересованные стороны, [Краткое название решения], Обоснование, Определение, Ответственный, Предположения, Приоритет (+3 more)

### Community 101 - "[决策简要标题]"
Cohesion: 0.17
Nodes (11): 优先级, 作者, 假设, [决策简要标题], 利益相关方, 定义, 概要, 理由 (+3 more)

### Community 102 - ".PromptGetArrayDomainsAdr"
Cohesion: 0.39
Nodes (6): INewAdrPrompts, CancellationToken, Content, domains, Exception, IsAborted

### Community 103 - "[Kurzer Titel der Entscheidung]"
Cohesion: 0.29
Nodes (6): Bewertungskriterien, Empfehlung, [Kurzer Titel der Entscheidung], Recherche und Analyse jedes Kandidaten, Zu berücksichtigende Kandidaten, Zusammenfassung

### Community 104 - "[Título breve de la decisión]"
Cohesion: 0.29
Nodes (6): Candidatos a considerar, Criterios de evaluación, Investigación y análisis de cada candidato, Recomendación, Resumen, [Título breve de la decisión]

### Community 105 - "[Titre bref de la décision]"
Cohesion: 0.29
Nodes (6): Candidats à considérer, Critères d'évaluation, Recherche et analyse de chaque candidat, Recommandation, Résumé, [Titre bref de la décision]

### Community 106 - "[Titolo breve della decisione]"
Cohesion: 0.29
Nodes (6): Candidati da considerare, Criteri di valutazione, Raccomandazione, Ricerca e analisi di ciascun candidato, Riepilogo, [Titolo breve della decisione]

### Community 107 - "[決定の簡潔なタイトル]"
Cohesion: 0.29
Nodes (6): 各候補の調査と分析, 推奨事項, 検討する候補, 概要, [決定の簡潔なタイトル], 評価基準

### Community 108 - "[결정에 대한 간단한 제목]"
Cohesion: 0.29
Nodes (6): 각 후보에 대한 조사 및 분석, [결정에 대한 간단한 제목], 고려할 후보, 권장 사항, 요약, 평가 기준

### Community 109 - "[Korte titel van de beslissing]"
Cohesion: 0.29
Nodes (6): Aanbeveling, Evaluatiecriteria, [Korte titel van de beslissing], Onderzoek en analyse van elke kandidaat, Samenvatting, Te overwegen kandidaten

### Community 110 - "[Título breve da decisão]"
Cohesion: 0.29
Nodes (6): Candidatos a considerar, Critérios de avaliação, Pesquisa e análise de cada candidato, Recomendação, Resumo, [Título breve da decisão]

### Community 111 - "[Краткое название решения]"
Cohesion: 0.29
Nodes (6): Исследование и анализ каждого кандидата, Кандидаты для рассмотрения, [Краткое название решения], Критерии оценки, Резюме, Рекомендация

### Community 112 - "[决策简要标题]"
Cohesion: 0.29
Nodes (6): [决策简要标题], 各候选方案的研究与分析, 建议, 待考虑的候选方案, 摘要, 评估标准

### Community 113 - "IMainProgram"
Cohesion: 0.40
Nodes (3): IMainProgram, CancellationToken, Task

### Community 114 - "[Kurzer Titel der Entscheidung]"
Cohesion: 0.40
Nodes (4): Begründung, Entscheidung, Konsequenzen, [Kurzer Titel der Entscheidung]

### Community 115 - "[Título breve de la decisión]"
Cohesion: 0.40
Nodes (4): Consecuencias, Decisión, Justificación, [Título breve de la decisión]

### Community 116 - "[Titre bref de la décision]"
Cohesion: 0.40
Nodes (4): Conséquences, Décision, Justification, [Titre bref de la décision]

### Community 117 - "[Titolo breve della decisione]"
Cohesion: 0.40
Nodes (4): Conseguenze, Decisione, Motivazione, [Titolo breve della decisione]

### Community 118 - "[決定の簡潔なタイトル]"
Cohesion: 0.40
Nodes (4): 影響, 根拠, 決定, [決定の簡潔なタイトル]

### Community 119 - "[결정에 대한 간단한 제목]"
Cohesion: 0.40
Nodes (4): 결과, 결정, [결정에 대한 간단한 제목], 근거

### Community 120 - "[Korte titel van de beslissing]"
Cohesion: 0.40
Nodes (4): Beslissing, Gevolgen, [Korte titel van de beslissing], Onderbouwing

### Community 121 - "[Título breve da decisão]"
Cohesion: 0.40
Nodes (4): Consequências, Decisão, Justificativa, [Título breve da decisão]

### Community 122 - "[Краткое название решения]"
Cohesion: 0.40
Nodes (4): [Краткое название решения], Обоснование, Последствия, Решение

### Community 123 - "[决策简要标题]"
Cohesion: 0.40
Nodes (4): 决策, [决策简要标题], 后果, 理由

### Community 124 - "[Kurzer Titel der Entscheidung]"
Cohesion: 0.40
Nodes (4): Entscheidung, Konsequenzen, Kontext, [Kurzer Titel der Entscheidung]

### Community 125 - "[Título breve de la decisión]"
Cohesion: 0.40
Nodes (4): Consecuencias, Contexto, Decisión, [Título breve de la decisión]

### Community 126 - "[Titre bref de la décision]"
Cohesion: 0.40
Nodes (4): Conséquences, Contexte, Décision, [Titre bref de la décision]

### Community 127 - "[Titolo breve della decisione]"
Cohesion: 0.40
Nodes (4): Conseguenze, Contesto, Decisione, [Titolo breve della decisione]

### Community 128 - "[決定の簡潔なタイトル]"
Cohesion: 0.40
Nodes (4): 影響, 決定, [決定の簡潔なタイトル], 背景

### Community 129 - "[결정에 대한 간단한 제목]"
Cohesion: 0.40
Nodes (4): 결과, 결정, [결정에 대한 간단한 제목], 컨텍스트

### Community 130 - "[Korte titel van de beslissing]"
Cohesion: 0.40
Nodes (4): Beslissing, Context, Gevolgen, [Korte titel van de beslissing]

### Community 131 - "[Título breve da decisão]"
Cohesion: 0.40
Nodes (4): Consequências, Contexto, Decisão, [Título breve da decisão]

### Community 132 - "[Краткое название решения]"
Cohesion: 0.40
Nodes (4): Контекст, [Краткое название решения], Последствия, Решение

### Community 133 - "[决策简要标题]"
Cohesion: 0.40
Nodes (4): 决策, [决策简要标题], 后果, 背景

### Community 134 - "Translations"
Cohesion: 0.50
Nodes (3): ADR templates, Translations, UI strings

## Knowledge Gaps
- **598 isolated node(s):** `net10.0`, `net9.0`, `net8.0`, `Microsoft.Extensions.Hosting (10.0.8)`, `PromptPlus (6.0.0-Beta4)` (+593 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **10 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `IFileSystemService` connect `IFileSystemService` to `.ParseArgs`, `IAdrServices`, `ValidateJsonConfigTests`, `.ParseFileName`, `.ExploreWizardAsync`, `FileSystemServiceEnhancedTests`, `.StatusUpdateAdrAsync`, `WizardCommandHandler`, `PromptConsole`, `ValidateConfig`, `AdrPlus.Infrastructure.FileSystem`, `CancellationToken`, `AdrRecordTests`, `Task`, `VersionCommandHandler`, `.StatusChangeAdrAsync`, `AdrPlusRepoConfig`, `PathHelper`, `.PromptGetArrayDomainsAdr`, `IValidateConfig`, `.PromptWriteSuccess`, `.PromptGetArrayDomainsAdr`?**
  _High betweenness centrality (0.076) - this node is a cross-community bridge._
- **Why does `AdrPlus.Domain` connect `AdrPlus.Domain` to `AdrPlus.Tests.Core`, `PatternParserTests`, `BehaviorWithoutArg.cs`, `.StatusUpdateAdrAsync`, `AdrPlusRepoConfig`, `StringCaseExtensionsTests`, `AdrPlus.Infrastructure.FileSystem`, `CancellationToken`, `CultureData`, `ServiceCollectionExtensions.cs`?**
  _High betweenness centrality (0.067) - this node is a cross-community bridge._
- **Why does `AdrPlus.Commands` connect `AdrPlus.Infrastructure.FileSystem` to `IAdrServices`, `.ExecuteAsync`, `CommandRouterTests.cs`, `AdrPlus.Domain`, `CommandAttribute`, `ServiceCollectionExtensions.cs`?**
  _High betweenness centrality (0.053) - this node is a cross-community bridge._
- **What connects `net10.0`, `net9.0`, `net8.0` to the rest of the system?**
  _598 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `.ParseArgs` be split into smaller, more focused modules?**
  _Cohesion score 0.05433506624182458 - nodes in this community are weakly interconnected._
- **Should `IAdrServices` be split into smaller, more focused modules?**
  _Cohesion score 0.05756302521008403 - nodes in this community are weakly interconnected._
- **Should `ValidateJsonConfigTests` be split into smaller, more focused modules?**
  _Cohesion score 0.07211646136618141 - nodes in this community are weakly interconnected._