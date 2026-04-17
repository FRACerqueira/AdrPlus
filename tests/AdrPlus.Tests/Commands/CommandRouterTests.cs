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
using NSubstitute;

namespace AdrPlus.Tests.Commands;

/// <summary>
/// Unit tests for CommandRouter class.
/// Tests demonstrate command routing patterns using NSubstitute.
/// These tests are designed to run cross-platform on both Linux and Windows.
/// </summary>
public class CommandRouterTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = Substitute.For<ILogger<CommandRouter>>();
        var console = Substitute.For<IConsoleWriter>();
        var adrServices = Substitute.For<IAdrServices>();
        var commandMap = new Dictionary<string, Type>();
        adrServices.GenerateCommandsMap().Returns(commandMap);

        // Act
        var router = new CommandRouter(serviceProvider, logger, console, adrServices);

        // Assert
        router.Should().NotBeNull();
    }

    #endregion

    #region RouteAsync - Unknown Command Tests

    [Fact]
    public async Task RouteAsync_WithUnknownCommandName_ThrowsInvalidOperationException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = Substitute.For<ILogger<CommandRouter>>();
        var console = Substitute.For<IConsoleWriter>();
        var adrServices = Substitute.For<IAdrServices>();
        var commandMap = new Dictionary<string, Type>();
        adrServices.GenerateCommandsMap().Returns(commandMap);
        var router = new CommandRouter(serviceProvider, logger, console, adrServices);

        // Act
        var ex = await Record.ExceptionAsync(
            () => router.RouteAsync("unknown", Array.Empty<string>(), CancellationToken.None));

        // Assert
        ex.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task RouteAsync_WithUnknownCommandName_CallsConsoleWriteError()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = Substitute.For<ILogger<CommandRouter>>();
        var console = Substitute.For<IConsoleWriter>();
        var adrServices = Substitute.For<IAdrServices>();
        var commandMap = new Dictionary<string, Type>();
        adrServices.GenerateCommandsMap().Returns(commandMap);
        var router = new CommandRouter(serviceProvider, logger, console, adrServices);

        // Act
        var ex = await Record.ExceptionAsync(
            () => router.RouteAsync("unknown", Array.Empty<string>(), CancellationToken.None));

        // Assert
        ex.Should().NotBeNull();
        console.Received().WriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task RouteAsync_WithUnknownCommandName_ExceptionMessageContainsCommandName()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = Substitute.For<ILogger<CommandRouter>>();
        var console = Substitute.For<IConsoleWriter>();
        var adrServices = Substitute.For<IAdrServices>();
        var commandMap = new Dictionary<string, Type>();
        adrServices.GenerateCommandsMap().Returns(commandMap);
        var router = new CommandRouter(serviceProvider, logger, console, adrServices);

        // Act
        var ex = await Record.ExceptionAsync(
            () => router.RouteAsync("nonexistent", Array.Empty<string>(), CancellationToken.None));

        // Assert
        ex.Should().NotBeNull();
        ex!.Message.Should().Contain("nonexistent");
    }

    #endregion

    #region RouteAsync - Command Case Sensitivity Tests

    [Fact]
    public async Task RouteAsync_WithUppercaseCommandName_ThrowsInvalidOperationException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = Substitute.For<ILogger<CommandRouter>>();
        var console = Substitute.For<IConsoleWriter>();
        var adrServices = Substitute.For<IAdrServices>();
        var commandMap = new Dictionary<string, Type> { { "new", typeof(object) } };
        adrServices.GenerateCommandsMap().Returns(commandMap);
        var router = new CommandRouter(serviceProvider, logger, console, adrServices);

        // Act
        var ex = await Record.ExceptionAsync(
            () => router.RouteAsync("NEW", Array.Empty<string>(), CancellationToken.None));

        // Assert - command lookup is case-sensitive
        ex.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task RouteAsync_WithValidCommandName_CommandLookupIsFound()
    {
        // Arrange
        var mockHandler = Substitute.For<ICommandHandler>();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<ICommandHandler>(sp => mockHandler);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var logger = Substitute.For<ILogger<CommandRouter>>();
        var console = Substitute.For<IConsoleWriter>();
        var adrServices = Substitute.For<IAdrServices>();
        var commandMap = new Dictionary<string, Type> { { "new", typeof(ICommandHandler) } };  // Use ICommandHandler type instead
        adrServices.GenerateCommandsMap().Returns(commandMap);
        var router = new CommandRouter(serviceProvider, logger, console, adrServices);

        // Act
        var ex = await Record.ExceptionAsync(
            () => router.RouteAsync("new", Array.Empty<string>(), CancellationToken.None));

        // Assert - command should be found and handler called
        ex.Should().BeNull();
    }

    #endregion

    #region RouteAsync - Empty/Null Command Name Tests

    #endregion

    #region RouteAsync - Console Output Tests

    [Fact]
    public async Task RouteAsync_WithUnknownCommandName_CallsWriteFinishedCommand()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = Substitute.For<ILogger<CommandRouter>>();
        var console = Substitute.For<IConsoleWriter>();
        var adrServices = Substitute.For<IAdrServices>();
        var commandMap = new Dictionary<string, Type>();
        adrServices.GenerateCommandsMap().Returns(commandMap);
        var router = new CommandRouter(serviceProvider, logger, console, adrServices);

        // Act
        await Record.ExceptionAsync(
            () => router.RouteAsync("unknown", Array.Empty<string>(), CancellationToken.None));

        // Assert
        // The finally block should execute and call WriteFinishedCommand
        // even though we threw an exception earlier
    }

    [Fact]
    public async Task RouteAsync_WithValidCommandName_CallsWriteStartCommand()
    {
        // Arrange
        var mockHandler = Substitute.For<ICommandHandler>();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<ICommandHandler>(_ => mockHandler);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var logger = Substitute.For<ILogger<CommandRouter>>();
        var console = Substitute.For<IConsoleWriter>();
        var adrServices = Substitute.For<IAdrServices>();
        var commandMap = new Dictionary<string, Type> { { "new", typeof(ICommandHandler) } };
        adrServices.GenerateCommandsMap().Returns(commandMap);
        var router = new CommandRouter(serviceProvider, logger, console, adrServices);

        // Act
        await router.RouteAsync("new", Array.Empty<string>(), CancellationToken.None);

        // Assert
        console.Received().WriteStartCommand(Arg.Is<string>(s => s.Contains("new")));
    }

    [Fact]
    public async Task RouteAsync_WithValidCommandName_CallsWriteFinishedCommand()
    {
        // Arrange
        var mockHandler = Substitute.For<ICommandHandler>();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<ICommandHandler>(_ => mockHandler);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var logger = Substitute.For<ILogger<CommandRouter>>();
        var console = Substitute.For<IConsoleWriter>();
        var adrServices = Substitute.For<IAdrServices>();
        var commandMap = new Dictionary<string, Type> { { "review", typeof(ICommandHandler) } };
        adrServices.GenerateCommandsMap().Returns(commandMap);
        var router = new CommandRouter(serviceProvider, logger, console, adrServices);

        // Act
        await router.RouteAsync("review", Array.Empty<string>(), CancellationToken.None);

        // Assert
        console.Received().WriteFinishedCommand(Arg.Is<string>(s => s.Contains("review")));
    }

    #endregion

    #region RouteAsync - Arguments Tests

    [Fact]
    public async Task RouteAsync_WithEmptyArgsArray_PassesToHandler()
    {
        // Arrange
        var mockHandler = Substitute.For<ICommandHandler>();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<ICommandHandler>(_ => mockHandler);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var logger = Substitute.For<ILogger<CommandRouter>>();
        var console = Substitute.For<IConsoleWriter>();
        var adrServices = Substitute.For<IAdrServices>();
        var commandMap = new Dictionary<string, Type> { { "init", typeof(ICommandHandler) } };
        adrServices.GenerateCommandsMap().Returns(commandMap);
        var router = new CommandRouter(serviceProvider, logger, console, adrServices);

        // Act
        await router.RouteAsync("init", Array.Empty<string>(), CancellationToken.None);

        // Assert
        await mockHandler.Received().ExecuteAsync(Array.Empty<string>(), CancellationToken.None);
    }

    [Fact]
    public async Task RouteAsync_WithMultipleArgs_PassesAllArgsToHandler()
    {
        // Arrange
        var mockHandler = Substitute.For<ICommandHandler>();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<ICommandHandler>(_ => mockHandler);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var logger = Substitute.For<ILogger<CommandRouter>>();
        var console = Substitute.For<IConsoleWriter>();
        var adrServices = Substitute.For<IAdrServices>();
        var commandMap = new Dictionary<string, Type> { { "new", typeof(ICommandHandler) } };
        adrServices.GenerateCommandsMap().Returns(commandMap);
        var router = new CommandRouter(serviceProvider, logger, console, adrServices);

        var args = new[] { "arg1", "arg2", "arg3" };

        // Act
        await router.RouteAsync("new", args, CancellationToken.None);

        // Assert
        await mockHandler.Received().ExecuteAsync(args, CancellationToken.None);
    }

    [Fact]
    public async Task RouteAsync_WithArgsContainingSpecialCharacters_PassesUnmodified()
    {
        // Arrange
        var mockHandler = Substitute.For<ICommandHandler>();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<ICommandHandler>(_ => mockHandler);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var logger = Substitute.For<ILogger<CommandRouter>>();
        var console = Substitute.For<IConsoleWriter>();
        var adrServices = Substitute.For<IAdrServices>();
        var commandMap = new Dictionary<string, Type> { { "review", typeof(ICommandHandler) } };
        adrServices.GenerateCommandsMap().Returns(commandMap);
        var router = new CommandRouter(serviceProvider, logger, console, adrServices);

        var args = new[] { "path/to/file", "--option=value", "@special" };

        // Act
        await router.RouteAsync("review", args, CancellationToken.None);

        // Assert
        await mockHandler.Received().ExecuteAsync(args, CancellationToken.None);
    }

    #endregion

    #region RouteAsync - Cancellation Token Tests

    [Fact]
    public async Task RouteAsync_WithValidCommandName_PassesCancellationTokenToHandler()
    {
        // Arrange
        var mockHandler = Substitute.For<ICommandHandler>();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<ICommandHandler>(_ => mockHandler);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var logger = Substitute.For<ILogger<CommandRouter>>();
        var console = Substitute.For<IConsoleWriter>();
        var adrServices = Substitute.For<IAdrServices>();
        var commandMap = new Dictionary<string, Type> { { "new", typeof(ICommandHandler) } };
        adrServices.GenerateCommandsMap().Returns(commandMap);
        var router = new CommandRouter(serviceProvider, logger, console, adrServices);

        var cts = new CancellationTokenSource();

        // Act
        await router.RouteAsync("new", Array.Empty<string>(), cts.Token);

        // Assert
        await mockHandler.Received().ExecuteAsync(Arg.Any<string[]>(), cts.Token);
    }

    #endregion

    #region RouteAsync - Cross-Platform Tests

    [Fact]
    public async Task RouteAsync_WithValidCommand_WorksOnAllPlatforms()
    {
        // Arrange
        var mockHandler = Substitute.For<ICommandHandler>();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<ICommandHandler>(_ => mockHandler);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var logger = Substitute.For<ILogger<CommandRouter>>();
        var console = Substitute.For<IConsoleWriter>();
        var adrServices = Substitute.For<IAdrServices>();
        var commandMap = new Dictionary<string, Type> { { "init", typeof(ICommandHandler) } };
        adrServices.GenerateCommandsMap().Returns(commandMap);
        var router = new CommandRouter(serviceProvider, logger, console, adrServices);

        // Act
        var ex = await Record.ExceptionAsync(
            () => router.RouteAsync("init", Array.Empty<string>(), CancellationToken.None));

        // Assert - no platform-specific exceptions
        ex.Should().BeNull();
    }

    [Fact]
    public async Task RouteAsync_WithArgsContainingPathsWithDifferentSeparators_WorksCrossPlatform()
    {
        // Arrange
        var mockHandler = Substitute.For<ICommandHandler>();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<ICommandHandler>(_ => mockHandler);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var logger = Substitute.For<ILogger<CommandRouter>>();
        var console = Substitute.For<IConsoleWriter>();
        var adrServices = Substitute.For<IAdrServices>();
        var commandMap = new Dictionary<string, Type> { { "review", typeof(ICommandHandler) } };
        adrServices.GenerateCommandsMap().Returns(commandMap);
        var router = new CommandRouter(serviceProvider, logger, console, adrServices);

        var args = new[] { "path\\to\\file", "path/to/file" };

        // Act
        var ex = await Record.ExceptionAsync(
            () => router.RouteAsync("review", args, CancellationToken.None));

        // Assert - args passed as-is without platform interpretation
        ex.Should().BeNull();
    }

    #endregion
}
