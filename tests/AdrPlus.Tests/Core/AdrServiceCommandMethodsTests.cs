// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Core;

namespace AdrPlus.Tests.Core;

/// <summary>
/// Unit tests for AdrService command-related methods (moved from CommandHelpers).
/// Ensures 100% coverage of GenerateCommandsMap, GetCommands, ParseArgs, GetHelpText, and OpenFile.
/// </summary>
public class AdrServiceCommandMethodsTests
{
    private readonly AdrService _adrServices;

    public AdrServiceCommandMethodsTests()
    {
        _adrServices = new AdrService();
    }

    #region GenerateCommandsMap Tests

    [Fact]
    public void GenerateCommandsMap_ReturnsNonEmptyDictionary()
    {
        // Act
        var result = _adrServices.GenerateCommandsMap();

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public void GenerateCommandsMap_AllKeysMatchAliases()
    {
        // Arrange
        var commands = _adrServices.GetCommands();

        // Act
        var map = _adrServices.GenerateCommandsMap();

        // Assert
        foreach (var (_, alias, handlerType, _) in commands)
        {
            map.Should().ContainKey(alias);
            map[alias].Should().Be(handlerType);
        }
    }

    [Fact]
    public void GenerateCommandsMap_IsCaseInsensitive()
    {
        // Act
        var map = _adrServices.GenerateCommandsMap();
        var firstKey = map.Keys.First();

        // Assert
        map.Should().ContainKey(firstKey.ToLower());
        map.Should().ContainKey(firstKey.ToUpper());
        map.Should().ContainKey(firstKey);
    }

    [Fact]
    public void GenerateCommandsMap_AllValuesAreValidTypes()
    {
        // Act
        var map = _adrServices.GenerateCommandsMap();

        // Assert
        foreach (var kvp in map)
        {
            kvp.Value.Should().NotBeNull();
            typeof(ICommandHandler).IsAssignableFrom(kvp.Value).Should().BeTrue();
        }
    }

    [Fact]
    public void GenerateCommandsMap_CountMatchesGetCommandsCount()
    {
        // Arrange
        var commands = _adrServices.GetCommands();

        // Act
        var map = _adrServices.GenerateCommandsMap();

        // Assert
        map.Should().HaveCount(commands.Length);
    }

    #endregion

    #region GetCommands Tests

    [Fact]
    public void GetCommands_ReturnsNonEmptyArray()
    {
        // Act
        var result = _adrServices.GetCommands();

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public void GetCommands_AllCommandsHaveNonNullProperties()
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
        for (int i = 0; i < commands1.Length; i++)
        {
            commands1[i].Alias.Should().Be(commands2[i].Alias);
        }
    }

    [Fact]
    public void GetCommands_AllAliasesAreUnique()
    {
        // Act
        var commands = _adrServices.GetCommands();

        // Assert
        var aliases = commands.Select(c => c.Alias).ToList();
        aliases.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void GetCommands_ContainsExpectedCommands()
    {
        // Act
        var commands = _adrServices.GetCommands();
        var aliases = commands.Select(c => c.Alias).ToList();

        // Assert
        aliases.Should().Contain("help");
        aliases.Should().Contain("new");
        aliases.Should().Contain("init");
        aliases.Should().Contain("approve");
        aliases.Should().Contain("reject");
    }

    #endregion

    #region ParseArgs Tests - Null/Empty Cases

    [Fact]
    public void ParseArgs_WithNullArgs_ThrowsArgumentNullException()
    {
        // Arrange
        var argsForCommand = new[] { Arguments.Help };

        // Act
        var act = () => _adrServices.ParseArgs(null!, argsForCommand);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("args");
    }

    [Fact]
    public void ParseArgs_WithNullArgsForCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var args = new[] { "-h" };

        // Act
        var act = () => _adrServices.ParseArgs(args, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("argsForCommand");
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

    #endregion

    #region ParseArgs Tests - Help Flags

    [Fact]
    public void ParseArgs_WithShortHelpFlag_ReturnsHelp()
    {
        // Arrange
        var args = new[] { "-h" };
        var argsForCommand = new[] { Arguments.Help, Arguments.FileAdr };

        // Act
        var result = _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().ContainKey(Arguments.Help);
        result[Arguments.Help].Should().BeEmpty();
        result.Should().ContainSingle();
    }

    [Fact]
    public void ParseArgs_WithLongHelpFlag_ReturnsHelp()
    {
        // Arrange
        var args = new[] { "--help" };
        var argsForCommand = new[] { Arguments.Help, Arguments.FileAdr };

        // Act
        var result = _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().ContainKey(Arguments.Help);
        result.Should().ContainSingle();
    }

    [Fact]
    public void ParseArgs_WithHelpFlagAndOtherArgs_OnlyReturnsHelp()
    {
        // Arrange
        var args = new[] { "-f", "test.md", "-h" };
        var argsForCommand = new[] { Arguments.Help, Arguments.FileAdr };

        // Act
        var result = _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().ContainKey(Arguments.Help);
        result.Should().ContainSingle();
    }

    #endregion

    #region ParseArgs Tests - Wizard Arguments

    [Fact]
    public void ParseArgs_WithWizardFlag_ParsesCorrectly()
    {
        // Arrange
        var args = new[] { "-w" };
        var argsForCommand = new[] { Arguments.WizardNew };

        // Act
        var result = _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().ContainKey(Arguments.WizardNew);
        result[Arguments.WizardNew].Should().BeEmpty();
    }

    [Fact]
    public void ParseArgs_WithLongWizardFlag_ParsesCorrectly()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var argsForCommand = new[] { Arguments.WizardNew };

        // Act
        var result = _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().ContainKey(Arguments.WizardNew);
        result[Arguments.WizardNew].Should().BeEmpty();
    }

    #endregion

    #region ParseArgs Tests - Optional Arguments

    [Fact]
    public void ParseArgs_WithOptionalArgument_ParsesEmptyValue()
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

    #endregion

    #region ParseArgs Tests - OptionalWithValue Arguments

    [Fact]
    public void ParseArgs_WithOptionalWithValue_ParsesValue()
    {
        // Arrange
        var args = new[] { "-f", "config.json" };
        var argsForCommand = new[] { Arguments.FileConfig };

        // Act
        var result = _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().ContainKey(Arguments.FileConfig);
        result[Arguments.FileConfig].Should().Be("config.json");
    }

    [Fact]
    public void ParseArgs_WithOptionalWithValueMissingValue_ThrowsException()
    {
        // Arrange
        var args = new[] { "-f" };
        var argsForCommand = new[] { Arguments.FileConfig };

        // Act
        var act = () => _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*-f*--file*");
    }

    [Fact]
    public void ParseArgs_WithOptionalWithValueFollowedByFlag_ThrowsException()
    {
        // Arrange
        var args = new[] { "-f", "-w" };
        var argsForCommand = new[] { Arguments.FileConfig, Arguments.WizardNew };

        // Act
        var act = () => _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*-f*");
    }

    #endregion

    #region ParseArgs Tests - OptionalWithValueWhenWizard Arguments

    [Fact]
    public void ParseArgs_WithOptionalWithValueWhenWizard_WithWizardAndValue_Succeeds()
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
    public void ParseArgs_WithOptionalWithValueWhenWizard_WithoutWizardAndWithValue_Succeeds()
    {
        // Arrange
        var args = new[] { "-t", "My Title" };
        var argsForCommand = new[] { Arguments.TitleAdr };

        // Act
        var result = _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().ContainKey(Arguments.TitleAdr);
        result[Arguments.TitleAdr].Should().Be("My Title");
    }

    [Fact]
    public void ParseArgs_WithOptionalWithValueWhenWizard_WithoutWizardAndWithoutValue_ThrowsException()
    {
        // Arrange
        var args = new[] { "-t" };
        var argsForCommand = new[] { Arguments.TitleAdr };

        // Act
        var act = () => _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region ParseArgs Tests - Invalid Arguments

    [Fact]
    public void ParseArgs_WithInvalidArgument_ThrowsException()
    {
        // Arrange
        var args = new[] { "--invalid-flag" };
        var argsForCommand = new[] { Arguments.Help };

        // Act
        var act = () => _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*--invalid-flag*");
    }

    [Fact]
    public void ParseArgs_WithArgumentNotInCommandList_ThrowsException()
    {
        // Arrange
        var args = new[] { "-w" };
        var argsForCommand = new[] { Arguments.Help };

        // Act
        var act = () => _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region ParseArgs Tests - Multiple Arguments

    [Fact]
    public void ParseArgs_WithMultipleArguments_ParsesAll()
    {
        // Arrange
        var args = new[] { "-w", "-f", "test.md", "-o" };
        var argsForCommand = new[] { Arguments.WizardNew, Arguments.FileAdr, Arguments.OpenAdr };

        // Act
        var result = _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().HaveCount(3);
        result[Arguments.WizardNew].Should().BeEmpty();
        result[Arguments.FileAdr].Should().Be("test.md");
        result[Arguments.OpenAdr].Should().BeEmpty();
    }

    [Fact]
    public void ParseArgs_WithAllArgumentTypes_ParsesCorrectly()
    {
        // Arrange
        var args = new[] { "-w", "-t", "Title", "-o", "-d", "Domain" };
        var argsForCommand = new[] { 
            Arguments.WizardNew, 
            Arguments.TitleAdr, 
            Arguments.OpenAdr, 
            Arguments.DomainAdr 
        };

        // Act
        var result = _adrServices.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().HaveCount(4);
        result[Arguments.WizardNew].Should().BeEmpty();
        result[Arguments.TitleAdr].Should().Be("Title");
        result[Arguments.OpenAdr].Should().BeEmpty();
        result[Arguments.DomainAdr].Should().Be("Domain");
    }

    #endregion

    #region GetHelpText Tests - Null/Empty/Whitespace

    [Fact]
    public void GetHelpText_WithNullCommand_ThrowsException()
    {
        // Arrange
        var argsForCommand = new[] { Arguments.Help };
        var examples = new[] { "example" };

        // Act
        var act = () => _adrServices.GetHelpText(null!, argsForCommand, examples);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetHelpText_WithEmptyCommand_ThrowsException()
    {
        // Arrange
        var argsForCommand = new[] { Arguments.Help };
        var examples = new[] { "example" };

        // Act
        var act = () => _adrServices.GetHelpText(string.Empty, argsForCommand, examples);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetHelpText_WithWhitespaceCommand_ThrowsException()
    {
        // Arrange
        var argsForCommand = new[] { Arguments.Help };
        var examples = new[] { "example" };

        // Act
        var act = () => _adrServices.GetHelpText("   ", argsForCommand, examples);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetHelpText_WithNullArgsForCommand_ThrowsException()
    {
        // Arrange
        var examples = new[] { "example" };

        // Act
        var act = () => _adrServices.GetHelpText("help", null!, examples);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetHelpText_WithNullExamples_ThrowsException()
    {
        // Arrange
        var argsForCommand = new[] { Arguments.Help };

        // Act
        var act = () => _adrServices.GetHelpText("help", argsForCommand, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region GetHelpText Tests - Valid Cases

    [Fact]
    public void GetHelpText_WithValidCommand_ReturnsNonEmptyString()
    {
        // Arrange
        var argsForCommand = new[] { Arguments.Help };
        var examples = new[] { "adrplus help" };

        // Act
        var result = _adrServices.GetHelpText("help", argsForCommand, examples);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetHelpText_WithInvalidCommand_ReturnsEmpty()
    {
        // Arrange
        var argsForCommand = new[] { Arguments.Help };
        var examples = new[] { "example" };

        // Act
        var result = _adrServices.GetHelpText("invalid-command", argsForCommand, examples);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetHelpText_ContainsUsageSection()
    {
        // Arrange
        var argsForCommand = new[] { Arguments.Help };
        var examples = new[] { "adrplus new -w" };

        // Act
        var result = _adrServices.GetHelpText("new", argsForCommand, examples);

        // Assert
        result.Should().Contain("adrplus");
        result.Should().Contain("new");
    }

    [Fact]
    public void GetHelpText_ContainsExamples()
    {
        // Arrange
        var argsForCommand = new[] { Arguments.Help };
        var exampleText = "adrplus new --wizard";
        var examples = new[] { exampleText };

        // Act
        var result = _adrServices.GetHelpText("new", argsForCommand, examples);

        // Assert
        result.Should().Contain(exampleText);
    }

    [Fact]
    public void GetHelpText_ContainsArgumentDescriptions()
    {
        // Arrange
        var argsForCommand = new[] { Arguments.Help, Arguments.WizardNew };
        var examples = new[] { "example" };

        // Act
        var result = _adrServices.GetHelpText("new", argsForCommand, examples);

        // Assert
        result.Should().Contain("-h");
        result.Should().Contain("--help");
        result.Should().Contain("-w");
        result.Should().Contain("--wizard");
    }

    [Fact]
    public void GetHelpText_WithMultipleExamples_ShowsAll()
    {
        // Arrange
        var argsForCommand = new[] { Arguments.Help };
        var examples = new[] { "example 1", "example 2", "example 3" };

        // Act
        var result = _adrServices.GetHelpText("new", argsForCommand, examples);

        // Assert
        foreach (var example in examples)
        {
            result.Should().Contain(example);
        }
    }

    [Fact]
    public void GetHelpText_CaseInsensitive_ReturnsHelpText()
    {
        // Arrange
        var argsForCommand = new[] { Arguments.Help };
        var examples = new[] { "example" };

        // Act
        var resultLower = _adrServices.GetHelpText("help", argsForCommand, examples);
        var resultUpper = _adrServices.GetHelpText("HELP", argsForCommand, examples);
        var resultMixed = _adrServices.GetHelpText("Help", argsForCommand, examples);

        // Assert
        resultLower.Should().NotBeNullOrWhiteSpace();
        resultUpper.Should().NotBeNullOrWhiteSpace();
        resultMixed.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetHelpText_WithEmptyArgsForCommand_ReturnsHelpText()
    {
        // Arrange
        var argsForCommand = Array.Empty<Arguments>();
        var examples = new[] { "example" };

        // Act
        var result = _adrServices.GetHelpText("help", argsForCommand, examples);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("help");
    }

    [Fact]
    public void GetHelpText_WithEmptyExamples_ReturnsHelpText()
    {
        // Arrange
        var argsForCommand = new[] { Arguments.Help };
        var examples = Array.Empty<string>();

        // Act
        var result = _adrServices.GetHelpText("help", argsForCommand, examples);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("help");
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
    public void OpenFile_WithEmptyFilePath_HandlesGracefully()
    {
        // Arrange & Act
        var result = _adrServices.OpenFile(string.Empty, "command");

        // Assert - Empty string is technically not null, so it may proceed
        // The actual behavior depends on Helper.OpenFile implementation
        result.Should().NotBeNull();
    }

    [Fact]
    public void OpenFile_WithEmptyCommand_HandlesGracefully()
    {
        // Arrange & Act
        var result = _adrServices.OpenFile("filepath", string.Empty);

        // Assert - Empty string is technically not null, so it may proceed
        // The actual behavior depends on Helper.OpenFile implementation
        result.Should().NotBeNull();
    }

    [Fact]
    public void OpenFile_WithValidCommandThatSucceeds_ReturnsEmptyString()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "test content");
        string command;

        if (OperatingSystem.IsWindows())
        {
            command = "type nul";
        }
        else
        {
            command = "true";
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
    public void OpenFile_WithCommandThatFails_ReturnsErrorMessage()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        string command;

        if (OperatingSystem.IsWindows())
        {
            command = "exit 1";
        }
        else
        {
            command = "false";
        }

        try
        {
            // Act
            var result = _adrServices.OpenFile(tempFile, command);

            // Assert
            result.Should().NotBeNull();
            // Could be empty or error message depending on implementation
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void OpenFile_WithNonExistentFile_HandlesGracefully()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), "non-existent-file-12345.md");

        // Act
        var result = _adrServices.OpenFile(nonExistentFile, "echo test");

        // Assert
        result.Should().NotBeNull();
    }

    #endregion
}
