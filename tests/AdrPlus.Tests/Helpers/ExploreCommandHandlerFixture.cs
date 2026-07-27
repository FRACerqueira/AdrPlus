// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands.Explore;
using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.UI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdrPlus.Tests.Helpers;

/// <summary>
/// Test fixture that provides reusable mocks and handler instances for ExploreCommandHandler tests.
/// Centralizes mock creation and handler initialization to reduce test boilerplate.
/// </summary>
internal class ExploreCommandHandlerFixture
{
    private ILogger<ExploreCommandHandler>? _mockLogger;
    private IFileSystemService? _mockFileSystem;
    private IConsoleWriter? _mockConsole;
    private IExplorePrompts? _mockExplorePrompts;
    private IValidateConfig? _mockValidateConfig;
    private IAdrServices? _mockAdrServices;
    private AdrPlusConfig? _config;
    private ExploreCommandHandler? _handler;

    /// <summary>
    /// Gets the mock logger, creating it if necessary.
    /// </summary>
    public ILogger<ExploreCommandHandler> MockLogger
    {
        get
        {
            _mockLogger ??= Substitute.For<ILogger<ExploreCommandHandler>>();
            return _mockLogger;
        }
    }

    /// <summary>
    /// Gets the mock file system service, creating it if necessary.
    /// </summary>
    public IFileSystemService MockFileSystem
    {
        get
        {
            _mockFileSystem ??= Substitute.For<IFileSystemService>();
            return _mockFileSystem;
        }
    }

    /// <summary>
    /// Gets the mock console writer, creating it if necessary.
    /// </summary>
    public IConsoleWriter MockConsole
    {
        get
        {
            _mockConsole ??= Substitute.For<IConsoleWriter>();
            return _mockConsole;
        }
    }

    /// <summary>
    /// Gets the mock explore-specific prompts, creating it if necessary.
    /// </summary>
    public IExplorePrompts MockExplorePrompts
    {
        get
        {
            _mockExplorePrompts ??= Substitute.For<IExplorePrompts>();
            return _mockExplorePrompts;
        }
    }

    /// <summary>
    /// Gets the mock validate config service, creating it if necessary.
    /// </summary>
    public IValidateConfig MockValidateConfig
    {
        get
        {
            _mockValidateConfig ??= Substitute.For<IValidateConfig>();
            return _mockValidateConfig;
        }
    }

    /// <summary>
    /// Gets the mock ADR services, creating it if necessary.
    /// </summary>
    public IAdrServices MockAdrServices
    {
        get
        {
            _mockAdrServices ??= Substitute.For<IAdrServices>();
            return _mockAdrServices;
        }
    }

    /// <summary>
    /// Gets the AdrPlusConfig instance, creating it with defaults if necessary.
    /// </summary>
    public AdrPlusConfig Config
    {
        get
        {
            _config ??= new AdrPlusConfig
            {
                Language = "en-US",
                ComandOpenAdr = string.Empty
            };
            return _config;
        }
        set => _config = value;
    }

    /// <summary>
    /// Gets the ExploreCommandHandler instance, creating it if necessary.
    /// </summary>
    public ExploreCommandHandler Handler
    {
        get
        {
            _handler ??= new ExploreCommandHandler(
                MockLogger,
                Options.Create(Config),
                MockFileSystem,
                MockValidateConfig,
                MockConsole,
                MockExplorePrompts,
                MockAdrServices);
            return _handler;
        }
    }

    /// <summary>
    /// Reconfigures the handler with a new AdrPlusConfig.
    /// Clears the cached handler so a new one is created with the updated config.
    /// </summary>
    public ExploreCommandHandler CreateHandlerWithConfig(AdrPlusConfig customConfig)
    {
        Config = customConfig;
        _handler = null;
        return Handler;
    }

    /// <summary>
    /// Resets all mocks and handler to their initial state.
    /// Useful for test isolation or when a completely fresh fixture is needed.
    /// </summary>
    public void Reset()
    {
        _mockLogger = null;
        _mockFileSystem = null;
        _mockConsole = null;
        _mockExplorePrompts = null;
        _mockValidateConfig = null;
        _mockAdrServices = null;
        _config = null;
        _handler = null;
    }
}
