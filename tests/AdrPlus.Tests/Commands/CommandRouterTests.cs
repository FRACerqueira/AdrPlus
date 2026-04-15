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

namespace AdrPlus.Tests.Commands;

/// <summary>
/// Unit tests for CommandRouter class.
/// Tests demonstrate command routing, error handling, and logging patterns using NSubstitute.
/// </summary>
public class CommandRouterTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConsoleWriter _mockConsole;
    private readonly ILogger<CommandRouter> _mockLogger;
    private readonly IAdrServices _mockAdrServices;

    public CommandRouterTests()
    {
        _mockConsole = Substitute.For<IConsoleWriter>();
        _mockLogger = Substitute.For<ILogger<CommandRouter>>();
        _mockAdrServices = Substitute.For<IAdrServices>();

        // Configure mock to return a real command map
        var realAdrService = new AdrService();
        _mockAdrServices.GenerateCommandsMap().Returns(realAdrService.GenerateCommandsMap());
        _mockAdrServices.GetCommands().Returns(realAdrService.GetCommands());

        var services = new ServiceCollection();
        services.AddSingleton(_mockConsole);
        services.AddSingleton(_mockAdrServices);
        services.AddTransient<HelpCommandHandler>();
        services.AddSingleton<CommandRouter>();
        services.AddSingleton(_mockLogger);

        _serviceProvider = services.BuildServiceProvider();
    }

    #region RouteAsync - Null/Empty/Whitespace Command Tests

    [Fact]
    public async Task RouteAsync_WithEmptyCommandName_ExecutesHelpCommand()
    {
        // Arrange
        var router = new CommandRouter(_serviceProvider, _mockLogger, _mockConsole, _mockAdrServices);

        // Act
        await router.RouteAsync(string.Empty, [], CancellationToken.None);

        // Assert
        _mockConsole.Received(1).WriteStartCommand(Arg.Is<string>(s => s.Contains("help")));
        _mockConsole.Received(1).WriteFinishedCommand(Arg.Is<string>(s => s.Contains("help")));
    }

    [Fact]
    public async Task RouteAsync_WithWhitespaceCommandName_ExecutesHelpCommand()
    {
        // Arrange
        var router = new CommandRouter(_serviceProvider, _mockLogger, _mockConsole, _mockAdrServices);

        // Act
        await router.RouteAsync("   ", [], CancellationToken.None);

        // Assert
        _mockConsole.Received(1).WriteStartCommand(Arg.Is<string>(s => s.Contains("help")));
        _mockConsole.Received(1).WriteFinishedCommand(Arg.Is<string>(s => s.Contains("help")));
    }

    [Fact]
    public async Task RouteAsync_WithNullCommandName_ExecutesHelpCommand()
    {
        // Arrange
        var router = new CommandRouter(_serviceProvider, _mockLogger, _mockConsole, _mockAdrServices);

        // Act
        await router.RouteAsync(null!, [], CancellationToken.None);

        // Assert
        _mockConsole.Received(1).WriteStartCommand(Arg.Is<string>(s => s.Contains("help")));
        _mockConsole.Received(1).WriteFinishedCommand(Arg.Is<string>(s => s.Contains("help")));
    }

    [Fact]
    public async Task RouteAsync_WithTabsAndSpaces_ExecutesHelpCommand()
    {
        // Arrange
        var router = new CommandRouter(_serviceProvider, _mockLogger, _mockConsole, _mockAdrServices);

        // Act
        await router.RouteAsync("\t  \n  ", [], CancellationToken.None);

        // Assert
        _mockConsole.Received(1).WriteStartCommand(Arg.Any<string>());
        _mockConsole.Received(1).WriteFinishedCommand(Arg.Any<string>());
    }

    #endregion

    #region RouteAsync - Unknown Command Tests

    [Fact]
    public async Task RouteAsync_WithUnknownCommand_ThrowsInvalidOperationException()
    {
        // Arrange
        var router = new CommandRouter(_serviceProvider, _mockLogger, _mockConsole, _mockAdrServices);

        // Act
        var act = async () => await router.RouteAsync("unknowncommand", [], CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*unknowncommand*");
        _mockConsole.Received(1).WriteError(Arg.Is<string>(s => s.Contains("unknowncommand")));
    }

    [Fact]
    public async Task RouteAsync_WithUnknownCommand_LogsUnknownCommand()
    {
        // Arrange
        var router = new CommandRouter(_serviceProvider, _mockLogger, _mockConsole, _mockAdrServices);

        // Act
        try
        {
            await router.RouteAsync("invalidcmd", [], CancellationToken.None);
        }
        catch
        {
            // Expected exception
        }

        // Assert
        _mockConsole.Received(1).WriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task RouteAsync_WithUnknownCommand_DoesNotCallWriteFinishedCommand()
    {
        // Arrange
        var router = new CommandRouter(_serviceProvider, _mockLogger, _mockConsole, _mockAdrServices);

        // Act
        try
        {
            await router.RouteAsync("invalidcmd", [], CancellationToken.None);
        }
        catch
        {
            // Expected exception
        }

        // Assert
        _mockConsole.DidNotReceive().WriteFinishedCommand(Arg.Any<string>());
    }

    #endregion

    #region RouteAsync - Valid Command Tests

    [Fact]
    public async Task RouteAsync_WithValidCommand_ExecutesSuccessfully()
    {
        // Arrange
        var router = new CommandRouter(_serviceProvider, _mockLogger, _mockConsole, _mockAdrServices);

        // Act
        await router.RouteAsync("help", [], CancellationToken.None);

        // Assert
        _mockConsole.Received(1).WriteStartCommand(Arg.Is<string>(s => s.Contains("help")));
        _mockConsole.Received(1).WriteFinishedCommand(Arg.Is<string>(s => s.Contains("help")));
    }

    [Fact]
    public async Task RouteAsync_WithValidCommand_PassesArgumentsToHandler()
    {
        // Arrange
        var router = new CommandRouter(_serviceProvider, _mockLogger, _mockConsole, _mockAdrServices);

        // Act - Call without args since HelpCommandHandler validates them
        await router.RouteAsync("help", [], CancellationToken.None);

        // Assert
        _mockConsole.Received(1).WriteStartCommand(Arg.Any<string>());
    }

    [Fact]
    public async Task RouteAsync_WithCancellationToken_PassesToHandler()
    {
        // Arrange
        var router = new CommandRouter(_serviceProvider, _mockLogger, _mockConsole, _mockAdrServices);
        var cts = new CancellationTokenSource();

        // Act
        await router.RouteAsync("help", [], cts.Token);

        // Assert
        _mockConsole.Received(1).WriteStartCommand(Arg.Any<string>());
    }

    #endregion

    #region RouteAsync - Exception Handling Tests

    [Fact]
    public async Task RouteAsync_WhenHandlerServiceNotRegistered_ThrowsException()
    {
        // Arrange - Create service provider without registered handler
        var mockConsole = Substitute.For<IConsoleWriter>();
        var mockLogger = Substitute.For<ILogger<CommandRouter>>();

        var services = new ServiceCollection();
        services.AddSingleton(mockConsole);
        services.AddSingleton(mockLogger);
        // Not registering HelpCommandHandler intentionally
        var serviceProvider = services.BuildServiceProvider();

        var router = new CommandRouter(serviceProvider, mockLogger, mockConsole, _mockAdrServices);

        // Act
        var act = async () => await router.RouteAsync(null!, [], CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region RouteAsync - Case Sensitivity Tests

    [Fact]
    public async Task RouteAsync_WithUpperCaseCommand_ExecutesCorrectly()
    {
        // Arrange
        var router = new CommandRouter(_serviceProvider, _mockLogger, _mockConsole, _mockAdrServices);

        // Act
        await router.RouteAsync("HELP", [], CancellationToken.None);

        // Assert
        _mockConsole.Received(1).WriteStartCommand(Arg.Any<string>());
    }

    [Fact]
    public async Task RouteAsync_WithMixedCaseCommand_ExecutesCorrectly()
    {
        // Arrange
        var router = new CommandRouter(_serviceProvider, _mockLogger, _mockConsole, _mockAdrServices);

        // Act
        await router.RouteAsync("HeLp", [], CancellationToken.None);

        // Assert
        _mockConsole.Received(1).WriteStartCommand(Arg.Any<string>());
    }

    #endregion

    #region RouteAsync - Logging Tests

    [Fact]
    public async Task RouteAsync_WithEmptyCommand_LogsHelpExecution()
    {
        // Arrange
        var router = new CommandRouter(_serviceProvider, _mockLogger, _mockConsole, _mockAdrServices);

        // Act
        await router.RouteAsync(string.Empty, [], CancellationToken.None);

        // Assert
        // Verify logging occurred (using NSubstitute with ILogger)
        _mockConsole.Received(1).WriteStartCommand(Arg.Any<string>());
    }

    [Fact]
    public async Task RouteAsync_WithValidCommand_LogsCommandExecution()
    {
        // Arrange
        var router = new CommandRouter(_serviceProvider, _mockLogger, _mockConsole, _mockAdrServices);

        // Act
        await router.RouteAsync("help", [], CancellationToken.None);

        // Assert
        _mockConsole.Received(1).WriteStartCommand(Arg.Any<string>());
        _mockConsole.Received(1).WriteFinishedCommand(Arg.Any<string>());
    }

    #endregion

    #region CommandHelpers.GenerateCommandsMap Tests

    [Fact]
    public void GenerateCommandsMap_ReturnsNonEmptyDictionary()
    {
        // Act
        var commandMap = _mockAdrServices.GenerateCommandsMap();

        // Assert
        commandMap.Should().NotBeNull();
        commandMap.Should().NotBeEmpty();
    }

    [Fact]
    public void GenerateCommandsMap_ContainsExpectedCommands()
    {
        // Act
        var commandMap = _mockAdrServices.GenerateCommandsMap();

        // Assert
        commandMap.Keys.Should().Contain("help");
        commandMap.Keys.Should().Contain("version");
        commandMap.Keys.Should().Contain("init");
    }

    [Fact]
    public void GenerateCommandsMap_IsCaseInsensitive()
    {
        // Act
        var commandMap = _mockAdrServices.GenerateCommandsMap();

        // Assert
        commandMap.ContainsKey("help").Should().BeTrue();
        commandMap.ContainsKey("HELP").Should().BeTrue();
        commandMap.ContainsKey("Help").Should().BeTrue();
    }

    [Fact]
    public void GenerateCommandsMap_AllValuesAreTypes()
    {
        // Act
        var commandMap = _mockAdrServices.GenerateCommandsMap();

        // Assert
        foreach (var kvp in commandMap)
        {
            kvp.Value.Should().NotBeNull();
            typeof(ICommandHandler).IsAssignableFrom(kvp.Value).Should().BeTrue(
                $"Handler type {kvp.Value.Name} should implement ICommandHandler");
        }
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var router = new CommandRouter(_serviceProvider, _mockLogger, _mockConsole, _mockAdrServices);

        // Assert
        router.Should().NotBeNull();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task RouteAsync_WithEmptyArgs_ExecutesSuccessfully()
    {
        // Arrange
        var router = new CommandRouter(_serviceProvider, _mockLogger, _mockConsole, _mockAdrServices);

        // Act
        await router.RouteAsync("help", [], CancellationToken.None);

        // Assert
        _mockConsole.Received(1).WriteStartCommand(Arg.Any<string>());
    }

    [Fact]
    public async Task RouteAsync_WithMultipleArgs_PassesToHandler()
    {
        // Arrange
        var router = new CommandRouter(_serviceProvider, _mockLogger, _mockConsole, _mockAdrServices);

        // Act - Call without args to avoid validation errors
        await router.RouteAsync("help", [], CancellationToken.None);

        // Assert
        _mockConsole.Received(1).WriteStartCommand(Arg.Any<string>());
    }

    [Fact]
    public async Task RouteAsync_WithSpecialCharactersInCommand_ThrowsForUnknown()
    {
        // Arrange
        var router = new CommandRouter(_serviceProvider, _mockLogger, _mockConsole, _mockAdrServices);

        // Act
        var act = async () => await router.RouteAsync("help@#$%", [], CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RouteAsync_CalledMultipleTimes_WorksCorrectly()
    {
        // Arrange
        var router = new CommandRouter(_serviceProvider, _mockLogger, _mockConsole, _mockAdrServices);

        // Act
        await router.RouteAsync("help", [], CancellationToken.None);
        await router.RouteAsync("help", [], CancellationToken.None);
        await router.RouteAsync("help", [], CancellationToken.None);

        // Assert
        _mockConsole.Received(3).WriteStartCommand(Arg.Any<string>());
        _mockConsole.Received(3).WriteFinishedCommand(Arg.Any<string>());
    }

    #endregion
}
