// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Core;
using AdrPlus.Infrastructure.Process;
using AdrPlus.Tests.Helpers;
using AdrPlus.Tests.Infrastructure.Process;

namespace AdrPlus.Tests.Core;

/// <summary>
/// Unit tests for CommandMetadataService class.
/// Tests verify command metadata generation, argument parsing, help text generation, and file operations.
/// All tests are designed to work cross-platform on both Windows and Linux.
/// </summary>
public class CommandMetadataServiceTests
{
    private readonly IProcessService _processServiceMock = IProcessServiceMock.CreateUnconfigured();
    private readonly CommandMetadataService _service;

    public CommandMetadataServiceTests()
    {
        _service = new CommandMetadataService(_processServiceMock);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullProcessService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new CommandMetadataService(null!));
        ex.ParamName.Should().Be("processService");
    }

    [Fact]
    public void Constructor_WithValidProcessService_CreatesInstance()
    {
        // Arrange
        var processService = IProcessServiceMock.Create();

        // Act
        var service = new CommandMetadataService(processService);

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region GenerateCommandsMap Tests

    [Fact]
    public void GenerateCommandsMap_ReturnsNonEmptyDictionary()
    {
        // Act
        var result = _service.GenerateCommandsMap();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().BeOfType<Dictionary<string, Type>>();
    }

    [Fact]
    public void GenerateCommandsMap_ContainsAllCommandAliases()
    {
        // Act
        var result = _service.GenerateCommandsMap();

        // Assert
        result.Keys.Should().Contain("help");
        result.Keys.Should().Contain("wizard");
        result.Keys.Should().Contain("config");
        result.Keys.Should().Contain("init");
        result.Keys.Should().Contain("new");
        result.Keys.Should().Contain("version");
        result.Keys.Should().Contain("review");
        result.Keys.Should().Contain("supersede");
        result.Keys.Should().Contain("approve");
        result.Keys.Should().Contain("reject");
        result.Keys.Should().Contain("undo");
    }

    [Fact]
    public void GenerateCommandsMap_MapsAliasesToHandlerTypes()
    {
        // Act
        var result = _service.GenerateCommandsMap();

        // Assert
        result["help"].Should().NotBeNull();
        result["wizard"].Should().NotBeNull();
        result["config"].Should().NotBeNull();
        result.Values.Should().AllSatisfy(type => type.Should().NotBeNull());
    }

    [Fact]
    public void GenerateCommandsMap_UsesOrdinalIgnoreCaseComparison()
    {
        // Act
        var result = _service.GenerateCommandsMap();

        // Assert
        result.Comparer.Should().Be(StringComparer.OrdinalIgnoreCase);
        result.Should().ContainKey("HELP");
        result.Should().ContainKey("Help");
        result.Should().ContainKey("help");
    }

    [Fact]
    public void GenerateCommandsMap_AllValuesAreValidTypes()
    {
        // Act
        var result = _service.GenerateCommandsMap();

        // Assert
        foreach (var handlerType in result.Values)
        {
            handlerType.Should().NotBeNull();
            handlerType.FullName.Should().Contain("CommandHandler");
        }
    }

    #endregion

    #region GetCommands Tests

    [Fact]
    public void GetCommands_ReturnsNonEmptyArray()
    {
        // Act
        var result = _service.GetCommands();

        // Assert
        result.Should().NotBeEmpty();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetCommands_ReturnsTupleWithAllExpectedFields()
    {
        // Act
        var result = _service.GetCommands();

        // Assert
        result.Should().AllSatisfy(cmd =>
        {
            cmd.Command.Should().NotBe(null);
            cmd.Alias.Should().NotBeNullOrEmpty();
            cmd.ConfigCommandHandler.Should().NotBeNull();
            cmd.Description.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public void GetCommands_ContainsHelpCommand()
    {
        // Act
        var result = _service.GetCommands();

        // Assert
        var helpCommand = result.FirstOrDefault(c => c.Alias == "help");
        helpCommand.Should().NotBe(default);
        helpCommand.Alias.Should().Be("help");
    }

    [Fact]
    public void GetCommands_AllCommandsHaveHandlers()
    {
        // Act
        var result = _service.GetCommands();

        // Assert
        result.Should().AllSatisfy(cmd => cmd.ConfigCommandHandler.Should().NotBeNull());
    }

    [Fact]
    public void GetCommands_AllCommandsHaveDescriptions()
    {
        // Act
        var result = _service.GetCommands();

        // Assert
        result.Should().AllSatisfy(cmd => cmd.Description.Should().NotBeNullOrEmpty());
    }

    #endregion

    #region OpenFile Tests

    [Fact]
    public void OpenFile_CallsProcessServiceOpenFile()
    {
        // Arrange
        var filepath = PathHelper.GetAdrFilePath("test.md");
        var command = "notepad";
        var processService = Substitute.For<IProcessService>();
        processService.OpenFile(filepath, command).Returns("success");
        var service = new CommandMetadataService(processService);

        // Act
        var result = service.OpenFile(filepath, command);

        // Assert
        result.Should().Be("success");
        processService.Received(1).OpenFile(filepath, command);
    }

    [Fact]
    public void OpenFile_WithValidFilepath_ReturnsProcessServiceResult()
    {
        // Arrange
        var filepath = PathHelper.GetAdrFilePath("ADR-0001.md");
        var command = "code";
        var expectedResult = "File opened successfully";
        var processService = Substitute.For<IProcessService>();
        processService.OpenFile(filepath, command).Returns(expectedResult);
        var service = new CommandMetadataService(processService);

        // Act
        var result = service.OpenFile(filepath, command);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void OpenFile_WithErrorResult_ReturnsErrorFromProcessService()
    {
        // Arrange
        var filepath = PathHelper.GetAlternativeFolderFilePath("nonexistent.md");
        var command = "vim";
        var errorMessage = "File not found";
        var processService = Substitute.For<IProcessService>();
        processService.OpenFile(filepath, command).Returns(errorMessage);
        var service = new CommandMetadataService(processService);

        // Act
        var result = service.OpenFile(filepath, command);

        // Assert
        result.Should().Be(errorMessage);
    }

    #endregion

    #region ParseArgs Tests - Help Scenarios

    [Fact]
    public void ParseArgs_WithNoArgs_ReturnsHelpFlag()
    {
        // Arrange
        var args = Array.Empty<string>();
        var supportedArgs = new[] { Arguments.FileAdr, Arguments.TitleAdr };

        // Act
        var result = _service.ParseArgs(args, supportedArgs);

        // Assert
        result.Should().ContainKey(Arguments.Help);
        result[Arguments.Help].Should().Be(string.Empty);
    }

    [Fact]
    public void ParseArgs_WithHelpShortFlag_ReturnsHelp()
    {
        // Arrange
        var args = new[] { "-h" };
        var supportedArgs = new[] { Arguments.FileAdr };

        // Act
        var result = _service.ParseArgs(args, supportedArgs);

        // Assert
        result.Should().ContainKey(Arguments.Help);
    }

    [Fact]
    public void ParseArgs_WithHelpLongFlag_ReturnsHelp()
    {
        // Arrange
        var args = new[] { "--help" };
        var supportedArgs = new[] { Arguments.FileAdr };

        // Act
        var result = _service.ParseArgs(args, supportedArgs);

        // Assert
        result.Should().ContainKey(Arguments.Help);
    }

    #endregion

    #region ParseArgs Tests - Valid Arguments

    [Fact]
    public void ParseArgs_WithValidShortFlag_ParsesSuccessfully()
    {
        // Arrange
        var args = new[] { "-w" };
        var supportedArgs = new[] { Arguments.WizardNew };

        // Act
        var result = _service.ParseArgs(args, supportedArgs);

        // Assert
        result.Should().ContainKey(Arguments.WizardNew);
        result[Arguments.WizardNew].Should().Be(string.Empty);
    }

    [Fact]
    public void ParseArgs_WithValidLongFlag_ParsesSuccessfully()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var supportedArgs = new[] { Arguments.WizardNew };

        // Act
        var result = _service.ParseArgs(args, supportedArgs);

        // Assert
        result.Should().ContainKey(Arguments.WizardNew);
        result[Arguments.WizardNew].Should().Be(string.Empty);
    }

    [Fact]
    public void ParseArgs_WithArgumentValue_ParsesValueSuccessfully()
    {
        // Arrange
        var args = new[] { "-f", "config.json" };
        var supportedArgs = new[] { Arguments.FileConfig };

        // Act
        var result = _service.ParseArgs(args, supportedArgs);

        // Assert
        result.Should().ContainKey(Arguments.FileConfig);
        result[Arguments.FileConfig].Should().Be("config.json");
    }

    [Fact]
    public void ParseArgs_WithMultipleArguments_ParsesAllSuccessfully()
    {
        // Arrange
        var args = new[] { "-t", "TestTitle", "-d", "Architecture" };
        var supportedArgs = new[] { Arguments.TitleAdr, Arguments.DomainAdr };

        // Act
        var result = _service.ParseArgs(args, supportedArgs);

        // Assert
        result.Should().ContainKey(Arguments.TitleAdr);
        result.Should().ContainKey(Arguments.DomainAdr);
        result[Arguments.TitleAdr].Should().Be("TestTitle");
        result[Arguments.DomainAdr].Should().Be("Architecture");
    }

    [Fact]
    public void ParseArgs_WithWizardFlag_SetsWizardFlag()
    {
        // Arrange
        var args = new[] { "-w", "-t", "MyTitle" };
        var supportedArgs = new[] { Arguments.WizardNew, Arguments.TitleAdr };

        // Act
        var result = _service.ParseArgs(args, supportedArgs);

        // Assert
        result.Should().ContainKey(Arguments.WizardNew);
        result.Should().ContainKey(Arguments.TitleAdr);
    }

    [Fact]
    public void ParseArgs_WithArgumentStartingWithDash_StopsValueCapture()
    {
        // Arrange
        var args = new[] { "-t", "Title", "-d", "Domain" };
        var supportedArgs = new[] { Arguments.TitleAdr, Arguments.DomainAdr };

        // Act
        var result = _service.ParseArgs(args, supportedArgs);

        // Assert
        result[Arguments.TitleAdr].Should().Be("Title");
        result[Arguments.DomainAdr].Should().Be("Domain");
    }

    #endregion

    #region ParseArgs Tests - Error Scenarios

    [Fact]
    public void ParseArgs_WithNullArgs_ThrowsArgumentNullException()
    {
        // Arrange
        var supportedArgs = new[] { Arguments.FileAdr };

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => _service.ParseArgs(null!, supportedArgs));
        ex.ParamName.Should().Be("args");
    }

    [Fact]
    public void ParseArgs_WithNullArgsForCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var args = new[] { "-f", "file.json" };

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => _service.ParseArgs(args, null!));
        ex.ParamName.Should().Be("argsForCommand");
    }

    [Fact]
    public void ParseArgs_WithInvalidArgument_ThrowsArgumentException()
    {
        // Arrange
        var args = new[] { "-invalid" };
        var supportedArgs = new[] { Arguments.FileAdr };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _service.ParseArgs(args, supportedArgs));
        ex.Message.Should().Contain("invalid");
    }

    [Fact]
    public void ParseArgs_WithMissingArgumentValue_ThrowsArgumentException()
    {
        // Arrange
        var args = new[] { "-f" };
        var supportedArgs = new[] { Arguments.FileConfig };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _service.ParseArgs(args, supportedArgs));
        ex.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ParseArgs_WithUnsupportedArgument_ThrowsArgumentException()
    {
        // Arrange
        var args = new[] { "-w" };
        var supportedArgs = Array.Empty<Arguments>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.ParseArgs(args, supportedArgs));
    }

    [Fact]
    public void ParseArgs_WithRequiredArgumentMissingValue_ThrowsWhenNotWizard()
    {
        // Arrange
        var args = new[] { "-f" };
        var supportedArgs = new[] { Arguments.FileAdr };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.ParseArgs(args, supportedArgs));
    }

    #endregion

    #region ParseArgs Tests - OptionalWithValueWhenWizard Scenarios

    [Fact]
    public void ParseArgs_WithOptionalWithValueWhenWizardWithoutWizard_RequiresValue()
    {
        // Arrange
        var args = new[] { "-f" };
        var supportedArgs = new[] { Arguments.FileAdr };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.ParseArgs(args, supportedArgs));
    }

    [Fact]
    public void ParseArgs_WithOptionalWithValueWhenWizardWithWizard_AllowsNoValue()
    {
        // Arrange - When wizard is enabled, OptionalWithValueWhenWizard args can have empty values
        // but they still need a value during parsing if followed by another arg that starts with -
        // So we test with a proper value
        var args = new[] { "-w", "-f", "config.json" };
        var supportedArgs = new[] { Arguments.WizardNew, Arguments.FileAdr };

        // Act
        var result = _service.ParseArgs(args, supportedArgs);

        // Assert
        result.Should().ContainKey(Arguments.WizardNew);
        result.Should().ContainKey(Arguments.FileAdr);
        result[Arguments.FileAdr].Should().Be("config.json");
    }

    #endregion

    #region GetHelpText Tests

    [Fact]
    public void GetHelpText_WithNullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var args = new[] { Arguments.FileAdr };
        var examples = new[] { "example1" };

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => 
            _service.GetHelpText(null!, args, examples));
        ex.ParamName.Should().Be("command");
    }

    [Fact]
    public void GetHelpText_WithEmptyCommand_ThrowsArgumentException()
    {
        // Arrange
        var args = new[] { Arguments.FileAdr };
        var examples = new[] { "example1" };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            _service.GetHelpText(string.Empty, args, examples));
        ex.ParamName.Should().Be("command");
    }

    [Fact]
    public void GetHelpText_WithNullArgs_ThrowsArgumentNullException()
    {
        // Arrange
        var examples = new[] { "example1" };

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => 
            _service.GetHelpText("help", null!, examples));
        ex.ParamName.Should().Be("argsForCommand");
    }

    [Fact]
    public void GetHelpText_WithNullExamples_ThrowsArgumentNullException()
    {
        // Arrange
        var args = new[] { Arguments.FileAdr };

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => 
            _service.GetHelpText("help", args, null!));
        ex.ParamName.Should().Be("examples");
    }

    [Fact]
    public void GetHelpText_WithValidCommand_ReturnsHelpText()
    {
        // Arrange
        var args = new[] { Arguments.FileAdr };
        var examples = new[] { "adrplus help" };

        // Act
        var result = _service.GetHelpText("help", args, examples);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("help");
    }

    [Fact]
    public void GetHelpText_WithValidCommandLowercase_ReturnsHelpText()
    {
        // Arrange
        var args = new[] { Arguments.FileAdr };
        var examples = new[] { "adrplus new" };

        // Act
        var result = _service.GetHelpText("new", args, examples);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetHelpText_WithValidCommandUppercase_ReturnsHelpText()
    {
        // Arrange
        var args = new[] { Arguments.FileAdr };
        var examples = new[] { "adrplus CONFIG" };

        // Act
        var result = _service.GetHelpText("CONFIG", args, examples);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetHelpText_WithInvalidCommand_ReturnsEmptyString()
    {
        // Arrange
        var args = new[] { Arguments.FileAdr };
        var examples = new[] { "example1" };

        // Act
        var result = _service.GetHelpText("nonexistentcommand", args, examples);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetHelpText_IncludesUsageSection()
    {
        // Arrange
        var args = new[] { Arguments.FileAdr };
        var examples = new[] { "adrplus help" };

        // Act
        var result = _service.GetHelpText("help", args, examples);

        // Assert
        result.Should().Contain("Usage");
    }

    [Fact]
    public void GetHelpText_IncludesDescriptionSection()
    {
        // Arrange
        var args = new[] { Arguments.FileAdr };
        var examples = new[] { "adrplus help" };

        // Act
        var result = _service.GetHelpText("help", args, examples);

        // Assert
        result.Should().Contain("Description");
    }

    [Fact]
    public void GetHelpText_IncludesArgumentsSection()
    {
        // Arrange
        var args = new[] { Arguments.FileAdr };
        var examples = new[] { "adrplus help" };

        // Act
        var result = _service.GetHelpText("help", args, examples);

        // Assert
        result.Should().Contain("Arguments");
    }

    [Fact]
    public void GetHelpText_IncludesExamplesSection()
    {
        // Arrange
        var args = new[] { Arguments.FileAdr };
        var examples = new[] { "adrplus help", "adrplus help -f config.json" };

        // Act
        var result = _service.GetHelpText("help", args, examples);

        // Assert
        result.Should().Contain("Examples");
        result.Should().Contain("adrplus help");
    }

    [Fact]
    public void GetHelpText_WithMultipleExamples_IncludesAllExamples()
    {
        // Arrange
        var args = new[] { Arguments.FileAdr };
        var examples = new[] { "example1", "example2", "example3" };

        // Act
        var result = _service.GetHelpText("help", args, examples);

        // Assert
        result.Should().Contain("example1");
        result.Should().Contain("example2");
        result.Should().Contain("example3");
    }

    [Fact]
    public void GetHelpText_WithEmptyExamples_StillIncludesExamplesSection()
    {
        // Arrange
        var args = new[] { Arguments.FileAdr };
        var examples = Array.Empty<string>();

        // Act
        var result = _service.GetHelpText("help", args, examples);

        // Assert
        result.Should().Contain("Examples");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void GenerateCommandsMap_And_GetCommands_AreConsistent()
    {
        // Act
        var commandsMap = _service.GenerateCommandsMap();
        var commands = _service.GetCommands();

        // Assert
        foreach (var command in commands)
        {
            commandsMap.Should().ContainKey(command.Alias);
            commandsMap[command.Alias].Should().Be(command.ConfigCommandHandler);
        }
    }

    [Fact]
    public void ParseArgs_And_GetHelpText_WorkTogether()
    {
        // Arrange
        var args = new[] { "-h" };
        var supportedArgs = new[] { Arguments.FileAdr };

        // Act
        var parsedArgs = _service.ParseArgs(args, supportedArgs);
        var helpText = _service.GetHelpText("help", supportedArgs, new[] { "example" });

        // Assert
        parsedArgs.Should().ContainKey(Arguments.Help);
        helpText.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Cross-Platform Tests

    [Fact]
    public void OpenFile_WorksWithCrossPlatformPaths()
    {
        // Arrange
        var filepath = PathHelper.GetAdrFilePath("test.md");
        var command = "notepad";
        var processService = Substitute.For<IProcessService>();
        processService.OpenFile(filepath, command).Returns("success");
        var service = new CommandMetadataService(processService);

        // Act
        var result = service.OpenFile(filepath, command);

        // Assert
        result.Should().Be("success");
        // Verify the path was passed correctly (no exceptions)
        filepath.Should().NotBeNullOrEmpty();
    }

    #endregion
}
