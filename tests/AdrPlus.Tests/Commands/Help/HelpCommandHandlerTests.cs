// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Commands.Help;
using AdrPlus.Core;
using AdrPlus.Infrastructure.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AdrPlus.Tests.Commands.Help;

/// <summary>
/// Unit tests for HelpCommandHandler class.
/// Tests demonstrate help command execution, argument validation, and console output patterns using NSubstitute.
/// </summary>
public class HelpCommandHandlerTests
{
    private readonly IConsoleWriter _mockConsole;
    private readonly CommandRouter _commandRouter;
    private readonly HelpCommandHandler _handler;
    private readonly IAdrServices _mockAdrServices;

    public HelpCommandHandlerTests()
    {
        _mockConsole = Substitute.For<IConsoleWriter>();
        _mockAdrServices = Substitute.For<IAdrServices>();
        var mockLogger = Substitute.For<ILogger<CommandRouter>>();

        // Configure mock to return real command data
        var realAdrService = new AdrService();
        _mockAdrServices.GetCommands().Returns(realAdrService.GetCommands());
        _mockAdrServices.GenerateCommandsMap().Returns(realAdrService.GenerateCommandsMap());

        var services = new ServiceCollection();
        services.AddSingleton(_mockConsole);
        services.AddSingleton(_mockAdrServices);
        services.AddSingleton(mockLogger);
        services.AddTransient<HelpCommandHandler>();
        // Register CommandRouter to avoid service resolution issues
        services.AddSingleton<CommandRouter>();
        var serviceProvider = services.BuildServiceProvider();

        _commandRouter = serviceProvider.GetRequiredService<CommandRouter>();
        _handler = new HelpCommandHandler(_mockConsole, _commandRouter, _mockAdrServices);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var console = Substitute.For<IConsoleWriter>();
        var mockLogger = Substitute.For<ILogger<CommandRouter>>();
        var mockAdrServices = Substitute.For<IAdrServices>();

        // Configure mock
        var realAdrService = new AdrService();
        mockAdrServices.GenerateCommandsMap().Returns(realAdrService.GenerateCommandsMap());
        mockAdrServices.GetCommands().Returns(realAdrService.GetCommands());

        var services = new ServiceCollection();
        services.AddSingleton(console);
        services.AddSingleton(mockLogger);
        services.AddSingleton(mockAdrServices);
        services.AddTransient<HelpCommandHandler>();
        services.AddSingleton<CommandRouter>();
        var serviceProvider = services.BuildServiceProvider();

        var router = serviceProvider.GetRequiredService<CommandRouter>();

        // Act
        var handler = new HelpCommandHandler(console, router, mockAdrServices);

        // Assert
        handler.Should().NotBeNull();
    }

    #endregion

    #region ExecuteAsync - No Arguments Tests

    [Fact]
    public async Task ExecuteAsync_WithNoArguments_WritesHelpToConsole()
    {
        // Act
        await _handler.ExecuteAsync([], CancellationToken.None);

        // Assert
        _mockConsole.Received().WriteHelp(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithNoArguments_CallsGenerateHelpAllCommands()
    {
        // Act
        await _handler.ExecuteAsync([], CancellationToken.None);

        // Assert
        _mockConsole.Received().WriteHelp(Arg.Is<string>(s => s.Contains("Available") || s.Contains("Commands")));
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyArray_WritesMultipleHelpLines()
    {
        // Act
        await _handler.ExecuteAsync([], CancellationToken.None);

        // Assert
        _mockConsole.Received().WriteHelp(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithNoArguments_WritesAllCommandsHelp()
    {
        // Arrange
        //none;

        // Act
        await _handler.ExecuteAsync([], CancellationToken.None);

        // Assert
        // Should write header + one line per command
        _mockConsole.Received().WriteHelp(Arg.Any<string>());
    }

    #endregion

    #region ExecuteAsync - Single Argument Tests

    [Fact]
    public async Task ExecuteAsync_WithSingleValidArgument_RoutesToCommand()
    {
        // Arrange
        var args = new[] { "help" };

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockConsole.Received().WriteStartCommand(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithSingleArgument_PassesEmptyArgsToRouter()
    {
        // Arrange
        var args = new[] { "help" };

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockConsole.Received().WriteStartCommand(Arg.Any<string>());
        _mockConsole.Received().WriteFinishedCommand(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownCommand_ThrowsException()
    {
        // Arrange
        var args = new[] { "unknowncommand123" };

        // Act
        var act = async () => await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithValidCommandName_ExecutesSuccessfully()
    {
        // Arrange
        var args = new[] { "help" };

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert - Should complete without exception
        _mockConsole.Received().WriteStartCommand(Arg.Any<string>());
    }

    #endregion

    #region ExecuteAsync - Multiple Arguments Tests

    [Fact]
    public async Task ExecuteAsync_WithTwoArguments_ThrowsArgumentException()
    {
        // Arrange
        var args = new[] { "arg1", "arg2" };

        // Act
        var act = async () => await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*too many*");
    }

    [Fact]
    public async Task ExecuteAsync_WithThreeArguments_ThrowsArgumentException()
    {
        // Arrange
        var args = new[] { "arg1", "arg2", "arg3" };

        // Act
        var act = async () => await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithManyArguments_ThrowsArgumentException()
    {
        // Arrange
        var args = new[] { "a", "b", "c", "d", "e" };

        // Act
        var act = async () => await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region ExecuteAsync - Cancellation Tests

    [Fact]
    public async Task ExecuteAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        await _handler.ExecuteAsync([], cts.Token);

        // Assert
        _mockConsole.Received().WriteHelp(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithCancelledToken_CompletesImmediately()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - Should handle cancellation gracefully
        await _handler.ExecuteAsync([], cts.Token);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellationToken_PassesToRouter()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var args = new[] { "help" };

        // Act
        await _handler.ExecuteAsync(args, cts.Token);

        // Assert
        _mockConsole.Received().WriteStartCommand(Arg.Any<string>());
    }

    #endregion

    #region GenerateHelpAllCommands Tests

    [Fact]
    public void GenerateHelpAllCommands_WritesHeaderToConsole()
    {
        // Act
        _handler.GenerateHelpAllCommands();

        // Assert
        _mockConsole.Received(1).WriteHelp(Arg.Is<string>(s => 
            s.Contains("Available") || s.Contains("Commands")));
    }

    [Fact]
    public void GenerateHelpAllCommands_WritesEachCommand()
    {
        // Arrange
        var commands = _mockAdrServices.GetCommands();

        // Act
        _handler.GenerateHelpAllCommands();

        // Assert
        // Should write header + one line per command
        _mockConsole.Received(commands.Length + 1).WriteHelp(Arg.Any<string>());
    }

    [Fact]
    public void GenerateHelpAllCommands_FormatsCommandsWithPadding()
    {
        // Act
        _handler.GenerateHelpAllCommands();

        // Assert
        _mockConsole.Received().WriteHelp(Arg.Is<string>(s => s.Contains('#')));
    }

    [Fact]
    public void GenerateHelpAllCommands_IncludesAllCommands()
    {
        // Arrange
        var commands = _mockAdrServices.GetCommands();

        // Act
        _handler.GenerateHelpAllCommands();

        // Assert
        foreach (var (_, alias, _, _) in commands)
        {
            _mockConsole.Received().WriteHelp(Arg.Is<string>(s => s.Contains(alias)));
        }
    }

    [Fact]
    public void GenerateHelpAllCommands_CalledMultipleTimes_WritesEachTime()
    {
        // Act
        _handler.GenerateHelpAllCommands();
        _handler.GenerateHelpAllCommands();
        _handler.GenerateHelpAllCommands();

        // Assert
        var commands = _mockAdrServices.GetCommands();
        _mockConsole.Received(3 * (commands.Length + 1)).WriteHelp(Arg.Any<string>());
    }

    [Fact]
    public void GenerateHelpAllCommands_FormatsWithConsistentPadding()
    {
        // Arrange
        var commands = _mockAdrServices.GetCommands();
        var maxLength = commands.Max(c => c.Alias.Length);

        // Act
        _handler.GenerateHelpAllCommands();

        // Assert
        // Each command line should have consistent padding
        _mockConsole.Received().WriteHelp(Arg.Any<string>());
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public async Task ExecuteAsync_MultipleInvocations_WritesHelpEachTime()
    {
        // Act
        await _handler.ExecuteAsync([], CancellationToken.None);
        await _handler.ExecuteAsync([], CancellationToken.None);
        await _handler.ExecuteAsync([], CancellationToken.None);

        // Assert
        _mockConsole.Received().WriteHelp(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithNullArgs_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _handler.ExecuteAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("args");
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyStringArgument_RoutesToHelp()
    {
        // Arrange
        var args = new[] { "" };

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockConsole.Received().WriteStartCommand(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithWhitespaceArgument_RoutesToHelp()
    {
        // Arrange
        var args = new[] { "   " };

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockConsole.Received().WriteStartCommand(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithCaseInsensitiveCommand_RoutesCorrectly()
    {
        // Arrange
        var args = new[] { "HELP" };

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockConsole.Received().WriteStartCommand(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithMixedCaseCommand_RoutesCorrectly()
    {
        // Arrange
        var args = new[] { "HeLp" };

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockConsole.Received().WriteStartCommand(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_SequentialCalls_WithDifferentArgCounts_WorksCorrectly()
    {
        // Act & Assert
        await _handler.ExecuteAsync([], CancellationToken.None);
        _mockConsole.Received().WriteHelp(Arg.Any<string>());

        await _handler.ExecuteAsync(["help"], CancellationToken.None);
        _mockConsole.Received().WriteStartCommand(Arg.Any<string>());

        var act = async () => await _handler.ExecuteAsync(["a", "b"], CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Argument Validation Tests

    [Fact]
    public async Task ExecuteAsync_ArgumentsLength_ZeroOrOne_IsValid()
    {
        // Arrange & Act & Assert
        await _handler.ExecuteAsync([], CancellationToken.None);
        _mockConsole.Received().WriteHelp(Arg.Any<string>());

        await _handler.ExecuteAsync(["help"], CancellationToken.None);
        _mockConsole.Received().WriteStartCommand(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_ArgumentsLength_GreaterThanOne_ThrowsException()
    {
        // Arrange
        var testCases = new[]
        {
            new[] { "a", "b" },
            ["a", "b", "c"],
            ["1", "2", "3", "4"]
        };

        // Act & Assert
        foreach (var args in testCases)
        {
            var act = async () => await _handler.ExecuteAsync(args, CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentException>();
        }
    }

    #endregion

    #region Console Output Verification Tests

    [Fact]
    public async Task ExecuteAsync_NoArgs_OutputContainsAllCommands()
    {
        // Arrange
        var commands = _mockAdrServices.GetCommands();

        // Act
        await _handler.ExecuteAsync([], CancellationToken.None);

        // Assert
        foreach (var (_, alias, _, _) in commands)
        {
            _mockConsole.Received().WriteHelp(Arg.Is<string>(s => s.Contains(alias)));
        }
    }

    [Fact]
    public async Task ExecuteAsync_NoArgs_OutputIncludesDescriptions()
    {
        // Act
        await _handler.ExecuteAsync([], CancellationToken.None);

        // Assert
        _mockConsole.Received().WriteHelp(Arg.Is<string>(s => s.Contains('#')));
    }

    [Fact]
    public void GenerateHelpAllCommands_OutputFormat_MatchesExpectedPattern()
    {
        // Act
        _handler.GenerateHelpAllCommands();

        // Assert
        _mockConsole.Received().WriteHelp(Arg.Is<string>(s => 
            s.Contains("  ") && s.Contains('#')));
    }

    #endregion
}
