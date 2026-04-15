// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Core;

namespace AdrPlus.Tests.Commands;

/// <summary>
/// Unit tests for AdrService command helper methods.
/// Tests demonstrate various patterns using direct method invocation and edge case handling.
/// </summary>
public class CommandHelpersTests
{
    private readonly AdrService _adrServices;

    public CommandHelpersTests()
    {
        _adrServices = new AdrService();
    }

    #region GetCommands Tests

    [Fact]
    public void GetCommands_ReturnsNonEmptyArray()
    {
        // Act
        var commands = _adrServices.GetCommands();

        // Assert
        commands.Should().NotBeNull();
        commands.Should().NotBeEmpty();
    }

    [Fact]
    public void GetCommands_AllCommandsHaveValidProperties()
    {
        // Act
        var commands = _adrServices.GetCommands();

        // Assert
        foreach (var (_, alias, handlerType, description) in commands)
        {
            alias.Should().NotBeNullOrWhiteSpace();
            handlerType.Should().NotBeNull();
            description.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void GetCommands_ReturnsConsistentResults()
    {
        // Act
        var commands1 = _adrServices.GetCommands();
        var commands2 = _adrServices.GetCommands();

        // Assert
        commands1.Should().HaveCount(commands2.Length);
    }

    #endregion

    #region GenerateCommandsMap Tests

    [Fact]
    public void GenerateCommandsMap_ContainsAllCommands()
    {
        // Arrange
        var expectedCommands = _adrServices.GetCommands();

        // Act
        var commandMap = _adrServices.GenerateCommandsMap();

        // Assert
        foreach (var (_, alias, _, _) in expectedCommands)
        {
            commandMap.Should().ContainKey(alias);
        }
    }

    [Fact]
    public void GenerateCommandsMap_MapsSameAliasToSameHandlerType()
    {
        // Arrange
        var commands = _adrServices.GetCommands();
        var commandMap = _adrServices.GenerateCommandsMap();

        // Act & Assert
        foreach (var (_, alias, handlerType, _) in commands)
        {
            commandMap[alias].Should().Be(handlerType);
        }
    }

    [Fact]
    public void GenerateCommandsMap_IsCaseInsensitive()
    {
        // Arrange
        var commandMap = _adrServices.GenerateCommandsMap();
        var commands = _adrServices.GetCommands();
        var (_, Alias, ConfigCommandHandler, _) = commands[0];

        // Act & Assert
        commandMap.Should().ContainKey(Alias.ToLower());
        commandMap.Should().ContainKey(Alias.ToUpper());
        commandMap[Alias.ToLower()].Should().Be(ConfigCommandHandler);
    }

    [Fact]
    public void GenerateCommandsMap_ReturnsNonEmptyDictionary()
    {
        // Act
        var commandMap = _adrServices.GenerateCommandsMap();

        // Assert
        commandMap.Should().NotBeNull();
        commandMap.Should().NotBeEmpty();
    }

    #endregion

    #region OpenFile Tests

    [Fact]
    public void OpenFile_WithNullFilePath_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _adrServices.OpenFile(null!, "command");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void OpenFile_WithNullCommand_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _adrServices.OpenFile("filepath", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void OpenFile_WithValidArguments_ReturnsString()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "test content");

        try
        {
            // Act
            var result = _adrServices.OpenFile(tempFile, $"echo {tempFile}");

            // Assert
            result.Should().NotBeNull();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void OpenFile_WithInvalidCommand_ReturnsErrorMessage()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            var result = _adrServices.OpenFile(tempFile, "invalid-command-that-does-not-exist-12345");

            // Assert
            result.Should().NotBeNullOrEmpty();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void OpenFile_WithNonExistentFile_ReturnsErrorOrEmpty()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), "non-existent-file-12345.txt");

        // Act
        var result = _adrServices.OpenFile(nonExistentFile, "echo test");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void OpenFile_WithCommandThatReturnsNonZeroExitCode_ReturnsError()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        string command;

        if (OperatingSystem.IsWindows())
        {
            command = "exit 1"; // Windows command that exits with code 1
        }
        else
        {
            command = "false"; // Unix command that exits with code 1
        }

        try
        {
            // Act
            var result = _adrServices.OpenFile(tempFile, command);

            // Assert
            result.Should().NotBeNull();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void OpenFile_WithCommandThatWritesToStdError_CapturesError()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        string command;

        if (OperatingSystem.IsWindows())
        {
            command = "echo error 1>&2 & exit 1"; // Write to stderr and exit with error
        }
        else
        {
            command = "echo error >&2 && exit 1"; // Write to stderr and exit with error
        }

        try
        {
            // Act
            var result = _adrServices.OpenFile(tempFile, command);

            // Assert
            result.Should().NotBeNull();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void OpenFile_WithValidCommand_ReturnsEmptyOnSuccess()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "test");
        string command;

        if (OperatingSystem.IsWindows())
        {
            command = "type nul"; // Windows command that succeeds silently
        }
        else
        {
            command = "true"; // Unix command that succeeds
        }

        try
        {
            // Act
            var result = _adrServices.OpenFile(tempFile, command);

            // Assert
            result.Should().BeEmpty();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void OpenFile_ExceptionDuringExecution_ReturnsExceptionMessage()
    {
        // Arrange - Use a command that will definitely fail
        var tempFile = Path.GetTempFileName();
        string invalidCommand;

        if (OperatingSystem.IsWindows())
        {
            // Use a path with invalid characters that will cause an exception
            invalidCommand = "\"C:\\><invalid>\\command.exe\"";
        }
        else
        {
            // Use an invalid shell command
            invalidCommand = "/bin/\0invalid";
        }

        try
        {
            // Act
            var result = _adrServices.OpenFile(tempFile, invalidCommand);

            // Assert - Should return an error message (either exception message or command error)
            result.Should().NotBeNull();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    #endregion

    #region ParseArgs Tests

    [Fact]
    public void ParseArgs_WithNullArgs_ThrowsArgumentNullException()
    {
        // Arrange
        var argsForCommand = new[] { Arguments.Help };

        // Act
        var act = () => _adrServices.ParseArgs(null!, argsForCommand);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ParseArgs_WithNullArgsForCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var args = new[] { "-h" };

        // Act
        var act = () => _adrServices.ParseArgs(args, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ParseArgs_WithEmptyArgs_ReturnsHelpArgument()
    {
        // Arrange
        var args = Array.Empty<string>();
        var argsForCommand = new[] { Arguments.Help };

        // Act
        var result = _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().ContainKey(Arguments.Help);
        result[Arguments.Help].Should().BeEmpty();
    }

    [Fact]
    public void ParseArgs_WithHelpShortFlag_ReturnsHelpArgument()
    {
        // Arrange
        var args = new[] { "-h" };
        var argsForCommand = new[] { Arguments.Help, Arguments.FileAdr };

        // Act
        var result = _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().ContainKey(Arguments.Help);
        result[Arguments.Help].Should().BeEmpty();
    }

    [Fact]
    public void ParseArgs_WithHelpLongFlag_ReturnsHelpArgument()
    {
        // Arrange
        var args = new[] { "--help" };
        var argsForCommand = new[] { Arguments.Help, Arguments.FileAdr };

        // Act
        var result = _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().ContainKey(Arguments.Help);
    }

    [Fact]
    public void ParseArgs_WithWizardFlag_ParsesCorrectly()
    {
        // Arrange
        var args = new[] { "-w" };
        var argsForCommand = new[] { Arguments.WizardNew, Arguments.Help };

        // Act
        var result = _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().ContainKey(Arguments.WizardNew);
        result[Arguments.WizardNew].Should().BeEmpty();
    }

    [Fact]
    public void ParseArgs_WithOptionalWithValue_ParsesValue()
    {
        // Arrange
        var args = new[] { "-f", "test.json" };
        var argsForCommand = new[] { Arguments.FileConfig };

        // Act
        var result = _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().ContainKey(Arguments.FileConfig);
        result[Arguments.FileConfig].Should().Be("test.json");
    }

    [Fact]
    public void ParseArgs_WithOptionalWithValueMissingValue_ThrowsArgumentException()
    {
        // Arrange
        var args = new[] { "-f" };
        var argsForCommand = new[] { Arguments.FileConfig };

        // Act
        var act = () => _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*-f*");
    }

    [Fact]
    public void ParseArgs_WithOptionalWithValueFollowedByFlag_ThrowsArgumentException()
    {
        // Arrange
        var args = new[] { "-f", "-w" };
        var argsForCommand = new[] { Arguments.FileConfig, Arguments.WizardNew };

        // Act
        var act = () => _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ParseArgs_WithInvalidArgument_ThrowsArgumentException()
    {
        // Arrange
        var args = new[] { "--invalid-arg" };
        var argsForCommand = new[] { Arguments.Help };

        // Act
        var act = () => _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*--invalid-arg*");
    }

    [Fact]
    public void ParseArgs_WithArgumentNotInCommandList_ThrowsArgumentException()
    {
        // Arrange
        var args = new[] { "-w" };
        var argsForCommand = new[] { Arguments.Help }; // Wizard not in list

        // Act
        var act = () => _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ParseArgs_WithMultipleArguments_ParsesAll()
    {
        // Arrange
        var args = new[] { "-w", "-f", "test.adr" };
        var argsForCommand = new[] { Arguments.WizardNew, Arguments.FileAdr };

        // Act
        var result = _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().ContainKey(Arguments.WizardNew);
        result.Should().ContainKey(Arguments.FileAdr);
        result[Arguments.FileAdr].Should().Be("test.adr");
    }

    [Fact]
    public void ParseArgs_WithOptionalWithValueWhenWizard_WithWizard_AllowsEmptyValue()
    {
        // Arrange
        var args = new[] { "-w", "-f" };
        var argsForCommand = new[] { Arguments.WizardNew, Arguments.FileAdr };

        // Act
        var act = () => _adrServices.ParseArgs(args, argsForCommand);

        // Assert - Should throw because -f needs a value
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ParseArgs_WithOptionalWithValueWhenWizard_WithoutWizard_RequiresValue()
    {
        // Arrange
        var args = new[] { "-f" };
        var argsForCommand = new[] { Arguments.FileAdr };

        // Act
        var act = () => _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ParseArgs_WithOptionalWithValueWhenWizard_WithoutWizardButWithValue_Succeeds()
    {
        // Arrange
        var args = new[] { "-f", "test.adr" };
        var argsForCommand = new[] { Arguments.FileAdr };

        // Act
        var result = _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().ContainKey(Arguments.FileAdr);
        result[Arguments.FileAdr].Should().Be("test.adr");
    }

    [Fact]
    public void ParseArgs_WithLongFormArguments_ParsesCorrectly()
    {
        // Arrange
        var args = new[] { "--file", "config.json" };
        var argsForCommand = new[] { Arguments.FileConfig };

        // Act
        var result = _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().ContainKey(Arguments.FileConfig);
        result[Arguments.FileConfig].Should().Be("config.json");
    }

    [Fact]
    public void ParseArgs_WithOpenFlag_ParsesCorrectly()
    {
        // Arrange
        var args = new[] { "-o" };
        var argsForCommand = new[] { Arguments.OpenAdr };

        // Act
        var result = _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().ContainKey(Arguments.OpenAdr);
        result[Arguments.OpenAdr].Should().BeEmpty();
    }

    [Fact]
    public void ParseArgs_WithWizardAndRequiredArg_WithoutValue_ThrowsException()
    {
        // Arrange
        var args = new[] { "-t" }; // title without wizard
        var argsForCommand = new[] { Arguments.TitleAdr };

        // Act
        var act = () => _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ParseArgs_WithSequenceArgument_ParsesValue()
    {
        // Arrange
        var args = new[] { "-s", "0001" };
        var argsForCommand = new[] { Arguments.SequenceAdr };

        // Act
        var result = _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().ContainKey(Arguments.SequenceAdr);
        result[Arguments.SequenceAdr].Should().Be("0001");
    }

    [Fact]
    public void ParseArgs_WithPathArgument_ParsesValue()
    {
        // Arrange
        var args = new[] { "-p", "docs/adr" };
        var argsForCommand = new[] { Arguments.TargetRepo };

        // Act
        var result = _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().ContainKey(Arguments.TargetRepo);
        result[Arguments.TargetRepo].Should().Be("docs/adr");
    }

    [Fact]
    public void ParseArgs_WithDomainArgument_ParsesValue()
    {
        // Arrange
        var args = new[] { "-d", "Architecture" };
        var argsForCommand = new[] { Arguments.DomainAdr };

        // Act
        var result = _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().ContainKey(Arguments.DomainAdr);
        result[Arguments.DomainAdr].Should().Be("Architecture");
    }

    [Fact]
    public void ParseArgs_WithScopeArgument_ParsesValue()
    {
        // Arrange
        var args = new[] { "-s", "backend" };
        var argsForCommand = new[] { Arguments.ScopeAdr };

        // Act
        var result = _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().ContainKey(Arguments.ScopeAdr);
        result[Arguments.ScopeAdr].Should().Be("backend");
    }

    [Fact]
    public void ParseArgs_WithRefDateArgument_ParsesCorrectly()
    {
        // Arrange
        var args = new[] { "-r" };
        var argsForCommand = new[] { Arguments.DateRefAdr };

        // Act
        var result = _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().ContainKey(Arguments.DateRefAdr);
        result[Arguments.DateRefAdr].Should().BeEmpty();
    }

    [Fact]
    public void ParseArgs_WithTitleArgument_ParsesValue()
    {
        // Arrange
        var args = new[] { "-w", "-t", "My Title" };
        var argsForCommand = new[] { Arguments.WizardNew, Arguments.TitleAdr };

        // Act
        var result = _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().ContainKey(Arguments.TitleAdr);
        result[Arguments.TitleAdr].Should().Be("My Title");
    }

    [Fact]
    public void ParseArgs_WithComplexScenario_ParsesAllArguments()
    {
        // Arrange
        var args = new[] { "-w", "-f", "test.adr", "-o", "-r" };
        var argsForCommand = new[] { Arguments.WizardNew, Arguments.FileAdr, Arguments.OpenAdr, Arguments.DateRefAdr };

        // Act
        var result = _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().HaveCount(4);
        result[Arguments.WizardNew].Should().BeEmpty();
        result[Arguments.FileAdr].Should().Be("test.adr");
        result[Arguments.OpenAdr].Should().BeEmpty();
        result[Arguments.DateRefAdr].Should().BeEmpty();
    }

    [Fact]
    public void ParseArgs_WithArgumentNotMatchingExpected_IgnoresIt()
    {
        // Arrange
        var args = new[] { "-f", "test.json", "-w" };
        var argsForCommand = new[] { Arguments.FileConfig }; // Wizard not in expected list

        // Act
        var act = () => _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region GetHelpText Tests

    [Fact]
    public void GetHelpText_WithNullCommand_ThrowsArgumentException()
    {
        // Arrange
        var argsForCommand = new[] { Arguments.Help };
        var examples = new[] { "example 1" };

        // Act
        var act = () => _adrServices.GetHelpText(null!, argsForCommand, examples);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetHelpText_WithEmptyCommand_ThrowsArgumentException()
    {
        // Arrange
        var argsForCommand = new[] { Arguments.Help };
        var examples = new[] { "example 1" };

        // Act
        var act = () => _adrServices.GetHelpText(string.Empty, argsForCommand, examples);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetHelpText_WithWhitespaceCommand_ThrowsArgumentException()
    {
        // Arrange
        var argsForCommand = new[] { Arguments.Help };
        var examples = new[] { "example 1" };

        // Act
        var act = () => _adrServices.GetHelpText("   ", argsForCommand, examples);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetHelpText_WithNullArgsForCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var examples = new[] { "example 1" };

        // Act
        var act = () => _adrServices.GetHelpText("help", null!, examples);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetHelpText_WithNullExamples_ThrowsArgumentNullException()
    {
        // Arrange
        var argsForCommand = new[] { Arguments.Help };

        // Act
        var act = () => _adrServices.GetHelpText("help", argsForCommand, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetHelpText_WithValidCommand_ReturnsNonEmptyString()
    {
        // Arrange
        var commands = _adrServices.GetCommands();
        var (_, Alias, _, _) = commands[0];
        var argsForCommand = new[] { Arguments.Help };
        var examples = new[] { $"adrplus {Alias} -h" };

        // Act
        var result = _adrServices.GetHelpText(Alias, argsForCommand, examples);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetHelpText_WithValidCommand_ContainsUsageSection()
    {
        // Arrange
        var commands = _adrServices.GetCommands();
        var (_, Alias, _, _) = commands[0];
        var argsForCommand = new[] { Arguments.Help };
        var examples = new[] { $"adrplus {Alias} -h" };

        // Act
        var result = _adrServices.GetHelpText(Alias, argsForCommand, examples);

        // Assert
        result.Should().Contain("adrplus");
        result.Should().Contain(Alias);
    }

    [Fact]
    public void GetHelpText_WithValidCommand_ContainsExamples()
    {
        // Arrange
        var commands = _adrServices.GetCommands();
        var (_, Alias, _, _) = commands[0];
        var argsForCommand = new[] { Arguments.Help };
        var exampleText = $"adrplus {Alias} -h";
        var examples = new[] { exampleText };

        // Act
        var result = _adrServices.GetHelpText(Alias, argsForCommand, examples);

        // Assert
        result.Should().Contain(exampleText);
    }

    [Fact]
    public void GetHelpText_WithInvalidCommand_ReturnsEmptyString()
    {
        // Arrange
        var argsForCommand = new[] { Arguments.Help };
        var examples = new[] { "example" };

        // Act
        var result = _adrServices.GetHelpText("invalid-command-xyz", argsForCommand, examples);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetHelpText_WithMultipleArguments_ShowsAllArguments()
    {
        // Arrange
        var commands = _adrServices.GetCommands();
        var (_, Alias, _, _) = commands[0];
        var argsForCommand = new[] { Arguments.Help, Arguments.WizardNew };
        var examples = new[] { "example 1", "example 2" };

        // Act
        var result = _adrServices.GetHelpText(Alias, argsForCommand, examples);

        // Assert
        result.Should().Contain("-h");
        result.Should().Contain("--help");
        result.Should().Contain("-w");
        result.Should().Contain("--wizard");
    }

    [Fact]
    public void GetHelpText_WithMultipleExamples_ShowsAllExamples()
    {
        // Arrange
        var commands = _adrServices.GetCommands();
        var (_, Alias, _, _) = commands[0];
        var argsForCommand = new[] { Arguments.Help };
        var examples = new[] { "example 1", "example 2", "example 3" };

        // Act
        var result = _adrServices.GetHelpText(Alias, argsForCommand, examples);

        // Assert
        result.Should().Contain("example 1");
        result.Should().Contain("example 2");
        result.Should().Contain("example 3");
    }

    [Fact]
    public void GetHelpText_CaseInsensitiveCommand_ReturnsHelpText()
    {
        // Arrange
        var commands = _adrServices.GetCommands();
        var (_, Alias, _, _) = commands[0];
        var argsForCommand = new[] { Arguments.Help };
        var examples = new[] { "example" };

        // Act
        var resultLower = _adrServices.GetHelpText(Alias.ToLower(), argsForCommand, examples);
        var resultUpper = _adrServices.GetHelpText(Alias.ToUpper(), argsForCommand, examples);

        // Assert
        resultLower.Should().NotBeNullOrWhiteSpace();
        resultUpper.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetHelpText_WithArgumentsHavingValidValues_ShowsValidValues()
    {
        // Arrange
        var commands = _adrServices.GetCommands();
        var (_, Alias, _, _) = commands[0];
        var argsForCommand = new[] { Arguments.Help, Arguments.WizardNew };
        var examples = new[] { "example" };

        // Act
        var result = _adrServices.GetHelpText(Alias, argsForCommand, examples);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("-h");
    }

    [Fact]
    public void GetHelpText_WithEmptyArgsForCommand_ReturnsHelpTextWithoutArguments()
    {
        // Arrange
        var commands = _adrServices.GetCommands();
        var (_, Alias, _, _) = commands[0];
        var argsForCommand = Array.Empty<Arguments>();
        var examples = new[] { "example" };

        // Act
        var result = _adrServices.GetHelpText(Alias, argsForCommand, examples);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain(Alias);
    }

    [Fact]
    public void GetHelpText_WithEmptyExamples_ReturnsHelpTextWithoutExamples()
    {
        // Arrange
        var commands = _adrServices.GetCommands();
        var (_, Alias, _, _) = commands[0];
        var argsForCommand = new[] { Arguments.Help };
        var examples = Array.Empty<string>();

        // Act
        var result = _adrServices.GetHelpText(Alias, argsForCommand, examples);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain(Alias);
    }

    [Fact]
    public void GetHelpText_WithOptionalWithValueWhenWizard_ShowsRequiredWhenNotWizard()
    {
        // Arrange
        var commands = _adrServices.GetCommands();
        var (_, Alias, _, _) = commands[0];
        var argsForCommand = new[] { Arguments.FileAdr }; // OptionalWithValueWhenWizard
        var examples = new[] { "example" };

        // Act
        var result = _adrServices.GetHelpText(Alias, argsForCommand, examples);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("-f");
        result.Should().Contain("--file");
    }

    [Fact]
    public void GetHelpText_VerifiesDescriptionIsIncluded()
    {
        // Arrange
        var commands = _adrServices.GetCommands();
        var (Command, Alias, ConfigCommandHandler, Description) = commands[0];
        var argsForCommand = new[] { Arguments.Help };
        var examples = new[] { "example" };

        // Act
        var result = _adrServices.GetHelpText(Alias, argsForCommand, examples);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        // Verify that usage, arguments, and examples sections exist
        result.Split('\n').Should().Contain(line => line.Contains("adrplus"));
    }

    #endregion
}
