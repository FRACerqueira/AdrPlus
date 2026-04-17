// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Commands.Config;
using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.UI;
using AdrPlus.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static AdrPlus.Tests.Helpers.TestPathData;

namespace AdrPlus.Tests.Commands.Config;

/// <summary>
/// Unit tests for ConfigCommandHandler class.
/// Tests demonstrate config command execution, wizard flows, and validation patterns using NSubstitute.
/// </summary>
public class ConfigCommandHandlerTests
{
    private readonly ILogger<ConfigCommandHandler> _mockLogger;
    private readonly IFileSystemService _mockFileSystem;
    private readonly IConsoleWriter _mockConsole;
    private readonly IValidateJsonConfig _mockValidateConfig;
    private readonly IAdrServices _mockAdrServices;
    private readonly IOptionsMonitor<AdrPlusConfig> _mockConfigMonitor;
    private readonly AdrPlusConfig _config;
    private readonly ConfigCommandHandler _handler;

    public ConfigCommandHandlerTests()
    {
        _mockLogger = Substitute.For<ILogger<ConfigCommandHandler>>();
        _mockFileSystem = Substitute.For<IFileSystemService>();
        _mockConsole = Substitute.For<IConsoleWriter>();
        _mockValidateConfig = Substitute.For<IValidateJsonConfig>();
        _mockAdrServices = Substitute.For<IAdrServices>();
        _mockConfigMonitor = Substitute.For<IOptionsMonitor<AdrPlusConfig>>();

        _config = new AdrPlusConfig
        {
            FolderRepo = "docs/adr",
        };

        _mockConfigMonitor.CurrentValue.Returns(_config);

        _handler = new ConfigCommandHandler(
            _mockLogger,
            _mockFileSystem,
            _mockValidateConfig,
            _mockConsole,
            _mockConfigMonitor,
            _mockAdrServices);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var handler = new ConfigCommandHandler(
            _mockLogger,
            _mockFileSystem,
            _mockValidateConfig,
            _mockConsole,
            _mockConfigMonitor,
            _mockAdrServices);

        // Assert
        handler.Should().NotBeNull();
    }

    #endregion

    #region ExecuteAsync - Help Tests

    [Fact]
    public async Task ExecuteAsync_WithHelpArgument_WritesHelpToConsole()
    {
        // Arrange
        var args = new[] { "--help" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.Help, string.Empty } };
        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockAdrServices.GetHelpText(Arg.Any<string>(), Arg.Any<Arguments[]>(), Arg.Any<string[]>())
            .Returns("Help text");

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockConsole.Received(1).WriteHelp("Help text");
    }

    #endregion

    #region ExecuteAsync - Application Config Tests

    [Fact]
    public async Task ExecuteAsync_WithApplicationConfigWithoutFile_ProcessesWizardFlow()
    {
        // Arrange
        var args = new[] { "--application" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardConfigApplication, string.Empty } };
        var configPath = AppConfigPath;
        var jsonContent = """{"DefaultSettings": {"Language": "en"}}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.GetConfigAppFilePath().Returns(configPath);
        _mockFileSystem.FileExists(configPath).Returns(true);
        _mockFileSystem.ReadAllTextAsync(configPath, Arg.Any<CancellationToken>()).Returns(jsonContent);
        _mockValidateConfig.ValidateAppStructure(jsonContent).Returns((true, []));

        var field = new FieldsJson { Name = "Language", Value = "pt", IsEndEdit = true };
        _mockConsole.PromptConfigJsonAppSelect(Arg.Any<FieldsJson>(), Arg.Any<List<FieldsJson>>(), Arg.Any<CancellationToken>())
            .Returns((false, field));

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockFileSystem.Received(1).WriteAllTextAsync(configPath, Arg.Any<string>(), Arg.Any<CancellationToken>());
        _mockConsole.Received(1).WriteSuccess(configPath);
    }

    [Fact]
    public async Task ExecuteAsync_WithApplicationConfigWithFile_ReadsFromSpecifiedFile()
    {
        // Arrange
        var args = new[] { "--application", "--file", "custom.json" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.WizardConfigApplication, string.Empty },
            { Arguments.FileConfig, "custom.json" }
        };
        var configPath = AppConfigPath;
        var jsonContent = """{"DefaultSettings": {"Language": "en"}}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.GetConfigAppFilePath().Returns(configPath);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(jsonContent);
        _mockValidateConfig.ValidateAppStructure(jsonContent).Returns((true, []));

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockFileSystem.Received(1).ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithApplicationConfigInvalidStructure_ThrowsInvalidDataException()
    {
        // Arrange
        var args = new[] { "--application" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardConfigApplication, string.Empty } };
        var configPath = AppConfigPath;
        var jsonContent = """{"DefaultSettings": {}}""";
        var errors = new[] { "Missing Language field" };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.GetConfigAppFilePath().Returns(configPath);
        _mockFileSystem.FileExists(configPath).Returns(true);
        _mockFileSystem.ReadAllTextAsync(configPath, Arg.Any<CancellationToken>()).Returns(jsonContent);
        _mockValidateConfig.ValidateAppStructure(jsonContent).Returns((false, errors));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();

        _mockConsole.Received(1).WriteError("Missing Language field");
    }

    [Fact]
    public async Task ExecuteAsync_WithApplicationConfigFileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var args = new[] { "--application" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardConfigApplication, string.Empty } };
        var configPath = AppConfigPath;

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.GetConfigAppFilePath().Returns(configPath);
        _mockFileSystem.FileExists(configPath).Returns(false);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithApplicationConfigCustomFileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var args = new[] { "--application", "--file", "missing.json" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.WizardConfigApplication, string.Empty },
            { Arguments.FileConfig, "missing.json" }
        };
        var configPath = AppConfigPath;

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.GetConfigAppFilePath().Returns(configPath);
        _mockFileSystem.FileExists(configPath).Returns(true);
        _mockFileSystem.FileExists("missing.json").Returns(false);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    #endregion

    #region ExecuteAsync - Repository Config Tests

    [Fact]
    public async Task ExecuteAsync_WithRepositoryConfigWithoutFile_ProcessesWizardFlow()
    {
        // Arrange
        var args = new[] { "--repository" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardConfigRepository, string.Empty } };
        var configPath = RepoConfigPath;
        var jsonContent = """{"Prefix": "ADR", "LenSeq": 4}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(false);
        _mockValidateConfig.GetConfigDefaultRepoContentAsync(_config, Arg.Any<CancellationToken>())
            .Returns(jsonContent);
        _mockValidateConfig.EnsureFieldsRepoStructure(jsonContent).Returns(jsonContent);
        _mockValidateConfig.ValidateRepoStructure(Arg.Any<string>()).Returns((true, []));
        _mockValidateConfig.GetConfigRepoFilePath().Returns(configPath);

        var field = new FieldsJson { Name = "Prefix", Value = "ADR", IsEndEdit = true };
        _mockConsole.PromptConfigJsonRepoSelect(Arg.Any<FieldsJson>(), Arg.Any<List<FieldsJson>>(), Arg.Any<CancellationToken>())
            .Returns((false, field));

        var repoConfig = new AdrPlusRepoConfig
        {
            Prefix = "ADR",
            LenSeq = 4,
            LenScope = 0,
            Scopes = "",
            SkipDomain = "",
            CaseTransform = CaseFormat.PascalCase,
            Separator = '-'
        };
        _mockAdrServices.FromJson(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockFileSystem.Received(1).WriteAllTextAsync(configPath, Arg.Any<string>(), Arg.Any<CancellationToken>());
        _mockConsole.Received(1).WriteSuccess(configPath);
    }

    [Fact]
    public async Task ExecuteAsync_WithRepositoryConfigExistingFile_PromptsForOverwrite()
    {
        // Arrange
        var args = new[] { "--repository" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardConfigRepository, string.Empty } };
        var configPath = RepoConfigPath;
        var jsonContent = """{"Prefix": "ADR"}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((false, true));
        _mockValidateConfig.GetConfigDefaultRepoContentAsync(_config, Arg.Any<CancellationToken>())
            .Returns(jsonContent);
        _mockValidateConfig.EnsureFieldsRepoStructure(jsonContent).Returns(jsonContent);
        _mockValidateConfig.ValidateRepoStructure(Arg.Any<string>()).Returns((true, []));
        _mockValidateConfig.GetConfigRepoFilePath().Returns(configPath);

        var field = new FieldsJson { Name = "Prefix", Value = "ADR", IsEndEdit = true };
        _mockConsole.PromptConfigJsonRepoSelect(Arg.Any<FieldsJson>(), Arg.Any<List<FieldsJson>>(), Arg.Any<CancellationToken>())
            .Returns((false, field));

        var repoConfig = new AdrPlusRepoConfig
        {
            Prefix = "ADR",
            LenSeq = 4,
            LenScope = 0,
            Scopes = "",
            SkipDomain = "",
            CaseTransform = CaseFormat.PascalCase,
            Separator = '-'
        };
        _mockAdrServices.FromJson(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockConsole.Received(1).PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithRepositoryConfigOverwriteDeclined_Returns()
    {
        // Arrange
        var args = new[] { "--repository" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardConfigRepository, string.Empty } };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((false, false));

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockFileSystem.DidNotReceive().WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithRepositoryConfigOverwriteAborted_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--repository" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardConfigRepository, string.Empty } };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((true, false));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithRepositoryConfigWithFile_ReadsFromSpecifiedFile()
    {
        // Arrange
        var args = new[] { "--repository", "--file", "custom.json" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.WizardConfigRepository, string.Empty },
            { Arguments.FileConfig, "custom.json" }
        };
        var configPath = RepoConfigPath;
        var jsonContent = """{"Prefix": "ADR"}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(jsonContent);
        _mockValidateConfig.ValidateRepoStructure(jsonContent).Returns((true, []));
        _mockValidateConfig.GetConfigRepoFilePath().Returns(configPath);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockFileSystem.Received(1).ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithRepositoryConfigCustomFileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var args = new[] { "--repository", "--file", "missing.json" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.WizardConfigRepository, string.Empty },
            { Arguments.FileConfig, "missing.json" }
        };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(false);
        _mockFileSystem.FileExists("missing.json").Returns(false);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithRepositoryConfigInvalidStructure_ThrowsInvalidDataException()
    {
        // Arrange
        var args = new[] { "--repository", "--file", "invalid.json" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.WizardConfigRepository, string.Empty },
            { Arguments.FileConfig, "invalid.json" }
        };
        var jsonContent = """{"Invalid": "data"}""";
        var errors = new[] { "Missing Prefix field" };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(jsonContent);
        _mockValidateConfig.ValidateRepoStructure(jsonContent).Returns((false, errors));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();

        _mockConsole.Received(1).WriteError("Missing Prefix field");
    }

    #endregion

    #region ExecuteAsync - Template Config Tests

    [Fact]
    public async Task ExecuteAsync_WithTemplateConfigWithFile_ReadsAndWritesTemplate()
    {
        // Arrange
        var args = new[] { "--template", "--file", "template.md" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.WizardConfigTemplate, string.Empty },
            { Arguments.FileConfig, "template.md" }
        };
        var configPath = AdrTemplateConfigPath;
        var templateContent = "# Template content";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith("template.md"))).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(templateContent);
        _mockValidateConfig.GetConfigAdrTemplatePath().Returns(configPath);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockFileSystem.Received(1).ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        _mockConsole.Received(1).WriteSuccess(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithTemplateConfigInvalidExtension_ThrowsInvalidOperationException()
    {
        // Arrange
        var args = new[] { "--template", "--file", "template.txt" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.WizardConfigTemplate, string.Empty },
            { Arguments.FileConfig, "template.txt" }
        };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithTemplateConfigFileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var args = new[] { "--template", "--file", "missing.md" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.WizardConfigTemplate, string.Empty },
            { Arguments.FileConfig, "missing.md" }
        };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithTemplateConfigWizard_ProcessesWizardFlow()
    {
        // Arrange
        var args = new[] { "--template" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardConfigTemplate, string.Empty } };
        var drives = MultipleTestDrives;
        var selectedDrive = drives[0];
        var templatePath = PathHelper.GetTemplateFilePath("custom.md");
        var configPath = AdrTemplateConfigPath;
        var templateContent = "# Template content";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectLogicalDrive(Arg.Any<string>(), _mockFileSystem, Arg.Any<CancellationToken>())
            .Returns((false, selectedDrive));
        _mockConsole.PromptConfigTemplateAdrSelect(selectedDrive, Arg.Any<CancellationToken>())
            .Returns((false, templatePath));
        _mockFileSystem.FileExists(templatePath).Returns(true);
        _mockFileSystem.ReadAllTextAsync(templatePath, Arg.Any<CancellationToken>()).Returns(templateContent);
        _mockValidateConfig.GetConfigAdrTemplatePath().Returns(configPath);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockFileSystem.Received(1).WriteAllTextAsync(configPath, templateContent, Arg.Any<CancellationToken>());
        _mockConsole.Received(1).WriteSuccess(configPath);
    }

    [Fact]
    public async Task ExecuteAsync_WithTemplateConfigWizardSingleDrive_SkipsDriveSelection()
    {
        // Arrange
        var args = new[] { "--template" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardConfigTemplate, string.Empty } };
        var drives = new[] { SingleTestDrive };
        var templatePath = PathHelper.GetTemplateFilePath("custom.md");
        var configPath = AdrTemplateConfigPath;
        var templateContent = "# Template content";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptConfigTemplateAdrSelect(SingleTestDrive, Arg.Any<CancellationToken>())
            .Returns((false, templatePath));
        _mockFileSystem.FileExists(templatePath).Returns(true);
        _mockFileSystem.ReadAllTextAsync(templatePath, Arg.Any<CancellationToken>()).Returns(templateContent);
        _mockValidateConfig.GetConfigAdrTemplatePath().Returns(configPath);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockConsole.DidNotReceive().PromptSelectLogicalDrive(Arg.Any<string>(), Arg.Any<IFileSystemService>(), Arg.Any<CancellationToken>());
        await _mockFileSystem.Received(1).WriteAllTextAsync(configPath, templateContent, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithTemplateConfigWizardAborted_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--template" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardConfigTemplate, string.Empty } };
        var drives = new[] { SingleTestDrive };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptConfigTemplateAdrSelect(SingleTestDrive, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithTemplateConfigWizardDriveSelectionAborted_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--template" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardConfigTemplate, string.Empty } };
        var drives = MultipleTestDrives;

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectLogicalDrive(Arg.Any<string>(), _mockFileSystem, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithTemplateConfigWizardSelectedFileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var args = new[] { "--template" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardConfigTemplate, string.Empty } };
        var drives = new[] { SingleTestDrive };
        var templatePath = PathHelper.GetTemplateFilePath("missing.md");

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptConfigTemplateAdrSelect(SingleTestDrive, Arg.Any<CancellationToken>())
            .Returns((false, templatePath));
        _mockFileSystem.FileExists(templatePath).Returns(false);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    #endregion

    #region ExecuteAsync - Invalid Arguments Tests

    [Fact]
    public async Task ExecuteAsync_WithNoValidArguments_ThrowsNotImplementedException()
    {
        // Arrange
        var args = Array.Empty<string>();
        var parsedArgs = new Dictionary<Arguments, string>();

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<NotImplementedException>();
    }

    #endregion

    #region ExecuteAsync - Cancellation Tests

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--application" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardConfigApplication, string.Empty } };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.GetConfigAppFilePath().Returns("app.json");
        _mockFileSystem.FileExists("app.json").Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<string>(callInfo => throw new OperationCanceledException());

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task ExecuteAsync_WhenExceptionOccurs_LogsException()
    {
        // Arrange
        var args = new[] { "--application" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardConfigApplication, string.Empty } };
        var exception = new InvalidOperationException("Test exception");

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.When(x => x.GetConfigAppFilePath()).Do(x => throw exception);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    #endregion
}
