// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands.Explorer;
using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.UI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdrPlus.Tests.Helpers;

/// <summary>
/// Test fixture that provides reusable mocks and handler instances for ExplorerCommandHandler tests.
/// Centralizes mock creation and handler initialization to reduce test boilerplate.
/// </summary>
internal class ExplorerCommandHandlerFixture
{
    private ILogger<ExplorerCommandHandler>? _mockLogger;
    private IFileSystemService? _mockFileSystem;
    private IPromptConsole? _mockConsole;
    private IValidateJsonConfig? _mockValidateConfig;
    private IAdrServices? _mockAdrServices;
    private AdrPlusConfig? _config;
    private ExplorerCommandHandler? _handler;

    /// <summary>
    /// Gets the mock logger, creating it if necessary.
    /// </summary>
    public ILogger<ExplorerCommandHandler> MockLogger
    {
        get
        {
            _mockLogger ??= Substitute.For<ILogger<ExplorerCommandHandler>>();
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
    public IPromptConsole MockConsole
    {
        get
        {
            _mockConsole ??= Substitute.For<IPromptConsole>();
            return _mockConsole;
        }
    }

    /// <summary>
    /// Gets the mock validate config service, creating it if necessary.
    /// </summary>
    public IValidateJsonConfig MockValidateConfig
    {
        get
        {
            _mockValidateConfig ??= Substitute.For<IValidateJsonConfig>();
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
    /// Gets the ExplorerCommandHandler instance, creating it if necessary.
    /// </summary>
    public ExplorerCommandHandler Handler
    {
        get
        {
            _handler ??= new ExplorerCommandHandler(
                MockLogger,
                Options.Create(Config),
                MockFileSystem,
                MockValidateConfig,
                MockConsole,
                MockAdrServices);
            return _handler;
        }
    }

    /// <summary>
    /// Reconfigures the handler with a new AdrPlusConfig.
    /// Clears the cached handler so a new one is created with the updated config.
    /// </summary>
    public ExplorerCommandHandler CreateHandlerWithConfig(AdrPlusConfig customConfig)
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
        _mockValidateConfig = null;
        _mockAdrServices = null;
        _config = null;
        _handler = null;
    }
}
