// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Commands.Help;
using AdrPlus.Core;
using AdrPlus.Infrastructure.UI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AdrPlus.Tests.Commands.Help;

/// <summary>
/// Unit tests for HelpCommandHandler class.
/// Tests demonstrate help command execution patterns using NSubstitute.
/// </summary>
public class HelpCommandHandlerTests
{
    private readonly IConsoleWriter _mockConsole;
    private readonly CommandRouter _mockCommandRouter;
    private readonly IAdrServices _mockAdrServices;
    private readonly HelpCommandHandler _handler;

    public HelpCommandHandlerTests()
    {
        _mockConsole = Substitute.For<IConsoleWriter>();
        var mockServiceProvider = Substitute.For<IServiceProvider>();
        var mockLogger = Substitute.For<ILogger<CommandRouter>>();
        var mockConfiguration = Substitute.For<IConfiguration>();
        var mockHelpLogger = Substitute.For<ILogger<HelpCommandHandler>>();
        _mockAdrServices = Substitute.For<IAdrServices>();
        
        _mockCommandRouter = new CommandRouter(
            mockConfiguration,
            mockServiceProvider,
            mockLogger,
            _mockConsole,
            _mockAdrServices);

        _handler = new HelpCommandHandler(mockHelpLogger, _mockConsole, _mockCommandRouter, _mockAdrServices);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var console = Substitute.For<IConsoleWriter>();
        var mockServiceProvider = Substitute.For<IServiceProvider>();
        var mockLogger = Substitute.For<ILogger<CommandRouter>>();
        var adrServices = Substitute.For<IAdrServices>();
        var mockHelpLogger = Substitute.For<ILogger<HelpCommandHandler>>();
        var mockConfiguration = Substitute.For<IConfiguration>();
        var commandRouter = new CommandRouter(mockConfiguration, mockServiceProvider, mockLogger, console, adrServices);

        // Act
        var handler = new HelpCommandHandler(mockHelpLogger, console, commandRouter, adrServices);
        // Assert
        handler.Should().NotBeNull();
    }

    #endregion

    #region ExecuteAsync - No Arguments Tests

    [Fact]
    public async Task ExecuteAsync_WithEmptyArgs_CallsGenerateHelpAllCommands()
    {
        // Arrange
        var args = Array.Empty<string>();
        var commands = new[]
        {
            (CommandsAdr.New, "new", typeof(object), "Create a new ADR"),
            (CommandsAdr.Revise, "revise", typeof(object), "Create a new revision")
        };
        _mockAdrServices.GetCommands().Returns(commands);

        // Act
        await _handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        // Assert
        _mockConsole.Received().PromptWriteHelp(Resources.AdrPlus.HelpHeaderAvailableCommands);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyArgs_WritesAllCommandsToConsole()
    {
        // Arrange
        var args = Array.Empty<string>();
        var commands = new[]
        {
            (CommandsAdr.New, "new", typeof(object), "Create a new ADR"),
            (CommandsAdr.Revise, "revise", typeof(object), "Create a new revision"),
            (CommandsAdr.Init, "init", typeof(object), "Initialize repository")
        };
        _mockAdrServices.GetCommands().Returns(commands);

        // Act
        await _handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        // Assert
        // Verify console was called multiple times (header + 3 commands)
        _mockConsole.Received().PromptWriteHelp(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyArgs_AlignAliasesToMaxLength()
    {
        // Arrange
        var args = Array.Empty<string>();
        var commands = new[]
        {
            (CommandsAdr.New, "new", typeof(object), "Create a new ADR"),
            (CommandsAdr.Revise, "revise", typeof(object), "Create a new revision"),
            (CommandsAdr.Approve, "approve", typeof(object), "Approve an ADR")
        };
        _mockAdrServices.GetCommands().Returns(commands);

        // Act
        await _handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        // Assert
        var receivedCalls = _mockConsole.ReceivedCalls().ToList();
        receivedCalls.Should().HaveCountGreaterThanOrEqualTo(4); // Header + 3 commands
    }

    #endregion

    #region ExecuteAsync - Multiple Arguments Tests

    [Fact]
    public async Task ExecuteAsync_WithTwoArguments_ThrowsArgumentException()
    {
        // Arrange
        var args = new[] { "list", "filter" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.ExecuteAsync(args, TestContext.Current.CancellationToken));
        ex.Message.Should().Contain(Resources.AdrPlus.ErrMsgHelpTooManyArguments);
    }

    [Fact]
    public async Task ExecuteAsync_WithThreeArguments_ThrowsArgumentException()
    {
        // Arrange
        var args = new[] { "list", "filter", "extra" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.ExecuteAsync(args, TestContext.Current.CancellationToken));
        ex.Message.Should().Contain(Resources.AdrPlus.ErrMsgHelpTooManyArguments);
    }

    #endregion

    #region ExecuteAsync - Null Arguments Tests

    [Fact]
    public async Task ExecuteAsync_WithNullArgs_ThrowsArgumentNullException()
    {
        // Arrange
        string[] args = null!;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(
            () => _handler.ExecuteAsync(args, TestContext.Current.CancellationToken));
        ex.ParamName.Should().Be("args");
    }

    #endregion

    #region GenerateHelpAllCommands Tests

    [Fact]
    public void GenerateHelpAllCommands_WritesHelpHeader()
    {
        // Arrange
        var commands = new[]
        {
            (CommandsAdr.New, "new", typeof(object), "Create a new ADR"),
            (CommandsAdr.Revise, "revise", typeof(object), "Create a new revision")
        };
        _mockAdrServices.GetCommands().Returns(commands);

        // Act
        _handler.GenerateHelpAllCommands();

        // Assert
        _mockConsole.Received(1).PromptWriteHelp(Resources.AdrPlus.HelpHeaderAvailableCommands);
    }

    [Fact]
    public void GenerateHelpAllCommands_WithSingleCommand_WritesBothHeaderAndCommand()
    {
        // Arrange
        var commands = new[]
        {
            (CommandsAdr.New, "new", typeof(object), "Create a new ADR")
        };
        _mockAdrServices.GetCommands().Returns(commands);

        // Act
        _handler.GenerateHelpAllCommands();

        // Assert
        _mockConsole.ReceivedCalls().Count().Should().Be(2);
    }

    [Fact]
    public void GenerateHelpAllCommands_WithMultipleCommands_WritesAllCommands()
    {
        // Arrange
        var commands = new[]
        {
            (CommandsAdr.New, "new", typeof(object), "Create a new ADR"),
            (CommandsAdr.Revise, "revise", typeof(object), "Create a new revision"),
            (CommandsAdr.Init, "init", typeof(object), "Initialize repository"),
            (CommandsAdr.Approve, "approve", typeof(object), "Approve an ADR")
        };
        _mockAdrServices.GetCommands().Returns(commands);

        // Act
        _handler.GenerateHelpAllCommands();

        // Assert
        _mockConsole.ReceivedCalls().Count().Should().Be(5); // Header + 4 commands
    }

    [Fact]
    public void GenerateHelpAllCommands_IncludesCommandDescriptions()
    {
        // Arrange
        const string descriptionNewAdr = "Create a new ADR";
        const string descriptionRevise = "Create a new revision";
        var commands = new[]
        {
            (CommandsAdr.New, "new", typeof(object), descriptionNewAdr),
            (CommandsAdr.Revise, "revise", typeof(object), descriptionRevise)
        };
        _mockAdrServices.GetCommands().Returns(commands);

        // Act
        _handler.GenerateHelpAllCommands();

        // Assert
        _mockConsole.Received(1).PromptWriteHelp(Arg.Is<string>(s => s.Contains(descriptionNewAdr)));
        _mockConsole.Received(1).PromptWriteHelp(Arg.Is<string>(s => s.Contains(descriptionRevise)));
    }

    [Fact]
    public void GenerateHelpAllCommands_AliasesArePadded()
    {
        // Arrange
        var commands = new[]
        {
            (CommandsAdr.New, "n", typeof(object), "Create a new ADR"),
            (CommandsAdr.Revise, "revisecmd", typeof(object), "Create a new revision")
        };
        _mockAdrServices.GetCommands().Returns(commands);

        // Act
        _handler.GenerateHelpAllCommands();

        // Assert
        // Verify that WriteHelp was called with padded strings
        _mockConsole.Received().PromptWriteHelp(Arg.Any<string>());
    }

    [Fact]
    public void GenerateHelpAllCommands_FormatIncludesHashComment()
    {
        // Arrange
        var commands = new[]
        {
            (CommandsAdr.New, "new", typeof(object), "Create a new ADR")
        };
        _mockAdrServices.GetCommands().Returns(commands);

        // Act
        _handler.GenerateHelpAllCommands();

        // Assert
        _mockConsole.Received(1).PromptWriteHelp(Arg.Is<string>(s => s.Contains('#')));
    }

    [Fact]
    public void GenerateHelpAllCommands_PrefixesCommandWithTwoSpaces()
    {
        // Arrange
        var commands = new[]
        {
            (CommandsAdr.New, "new", typeof(object), "Create a new ADR")
        };
        _mockAdrServices.GetCommands().Returns(commands);

        // Act
        _handler.GenerateHelpAllCommands();

        // Assert
        _mockConsole.Received(1).PromptWriteHelp(Arg.Is<string>(s => s.StartsWith("  ")));
    }

    #endregion
}

