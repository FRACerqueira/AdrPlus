// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;

namespace AdrPlus.Tests.Core;

public class AdrServiceTests
{
    private readonly IAdrStatusService _statusService = Substitute.For<IAdrStatusService>();
    private readonly IAdrFileParser _fileParser = Substitute.For<IAdrFileParser>();
    private readonly IAdrConfigMapper _configMapper = Substitute.For<IAdrConfigMapper>();
    private readonly IAdrQueryService _queryService = Substitute.For<IAdrQueryService>();
    private readonly ICommandMetadataService _commandMetadataService = Substitute.For<ICommandMetadataService>();
    private readonly AdrService _adrService;

    public AdrServiceTests()
    {
        _adrService = new AdrService(_statusService, _fileParser, _configMapper, _queryService, _commandMetadataService);
    }

    #region ReadAllAdr Tests

    [Fact]
    public async Task ReadAllAdr_WithValidInputs_DelegatesAndReturnsResult()
    {
        // Arrange
        var fileSystemService = Substitute.For<IFileSystemService>();
        const string directoryPath = "/path/to/adr";
        var config = new AdrPlusRepoConfig("# {0}", "doc/adr");
        const bool includeNotMatched = true;

        var expectedComponents = new[]
        {
            new AdrFileNameComponents { Number = 1, FileName = "ADR-0001-Decision.md" },
            new AdrFileNameComponents { Number = 2, FileName = "ADR-0002-Decision.md" }
        };

        _queryService.ReadAllAdrFiles(fileSystemService, directoryPath, config, includeNotMatched)
            .Returns(Task.FromResult(expectedComponents));

        // Act
        var result = await _adrService.ReadAllAdr(fileSystemService, directoryPath, config, includeNotMatched);

        // Assert
        result.Should().BeEquivalentTo(expectedComponents);
        await _queryService.Received(1).ReadAllAdrFiles(fileSystemService, directoryPath, config, includeNotMatched);
    }

    [Fact]
    public async Task ReadAllAdr_WithIncludeNotMatchedFalse_DelegatesCorrectly()
    {
        // Arrange
        var fileSystemService = Substitute.For<IFileSystemService>();
        const string directoryPath = "/path/to/adr";
        var config = new AdrPlusRepoConfig("# {0}", "doc/adr");
        const bool includeNotMatched = false;

        var expectedComponents = Array.Empty<AdrFileNameComponents>();

        _queryService.ReadAllAdrFiles(fileSystemService, directoryPath, config, includeNotMatched)
            .Returns(Task.FromResult(expectedComponents));

        // Act
        var result = await _adrService.ReadAllAdr(fileSystemService, directoryPath, config, includeNotMatched);

        // Assert
        result.Should().BeEmpty();
        await _queryService.Received(1).ReadAllAdrFiles(fileSystemService, directoryPath, config, includeNotMatched);
    }

    #endregion

    #region StatusUpdateAdrAsync Tests

    [Fact]
    public async Task StatusUpdateAdrAsync_WithValidInputs_DelegatesAndReturnsResult()
    {
        // Arrange
        const string fullpath = "/path/to/ADR-0001.md";
        var adrStatus = AdrStatus.Accepted;
        var dref = new DateTime(2024, 1, 1);
        var config = new AdrPlusRepoConfig("# {0}", "doc/adr");
        var fileSystemService = Substitute.For<IFileSystemService>();
        var cancellationToken = CancellationToken.None;

        _statusService.StatusUpdateAdrAsync(fullpath, adrStatus, dref, config, fileSystemService, cancellationToken)
            .Returns(Task.FromResult((true, "")));

        // Act
        var (isValid, error) = await _adrService.StatusUpdateAdrAsync(fullpath, adrStatus, dref, config, fileSystemService, cancellationToken);

        // Assert
        isValid.Should().BeTrue();
        error.Should().BeEmpty();
        await _statusService.Received(1).StatusUpdateAdrAsync(fullpath, adrStatus, dref, config, fileSystemService, cancellationToken);
    }

    [Fact]
    public async Task StatusUpdateAdrAsync_WithFailure_ReturnsFalseWithError()
    {
        // Arrange
        const string fullpath = "/path/to/ADR-0001.md";
        var adrStatus = AdrStatus.Accepted;
        var dref = new DateTime(2024, 1, 1);
        var config = new AdrPlusRepoConfig("# {0}", "doc/adr");
        var fileSystemService = Substitute.For<IFileSystemService>();
        var cancellationToken = CancellationToken.None;
        const string errorMessage = "File not found";

        _statusService.StatusUpdateAdrAsync(fullpath, adrStatus, dref, config, fileSystemService, cancellationToken)
            .Returns(Task.FromResult((false, errorMessage)));

        // Act
        var (isValid, error) = await _adrService.StatusUpdateAdrAsync(fullpath, adrStatus, dref, config, fileSystemService, cancellationToken);

        // Assert
        isValid.Should().BeFalse();
        error.Should().Be(errorMessage);
    }

    #endregion

    #region StatusChangeSupersedeAdrAsync Tests

    [Fact]
    public async Task StatusChangeSupersedeAdrAsync_WithValidInputs_DelegatesAndReturnsResult()
    {
        // Arrange
        const string fullpath = "/path/to/ADR-0001.md";
        const string filename = "ADR-0002-NewDecision.md";
        var dref = new DateTime(2024, 1, 1);
        var config = new AdrPlusRepoConfig("# {0}", "doc/adr");
        var fileSystemService = Substitute.For<IFileSystemService>();
        var cancellationToken = CancellationToken.None;

        _statusService.StatusChangeSupersedeAdrAsync(fullpath, filename, dref, config, fileSystemService, cancellationToken)
            .Returns(Task.FromResult((true, "")));

        // Act
        var (isValid, error) = await _adrService.StatusChangeSupersedeAdrAsync(fullpath, filename, dref, config, fileSystemService, cancellationToken);

        // Assert
        isValid.Should().BeTrue();
        error.Should().BeEmpty();
        await _statusService.Received(1).StatusChangeSupersedeAdrAsync(fullpath, filename, dref, config, fileSystemService, cancellationToken);
    }

    [Fact]
    public async Task StatusChangeSupersedeAdrAsync_WithFailure_ReturnsFalseWithError()
    {
        // Arrange
        const string fullpath = "/path/to/ADR-0001.md";
        const string filename = "ADR-0002-NewDecision.md";
        var dref = new DateTime(2024, 1, 1);
        var config = new AdrPlusRepoConfig("# {0}", "doc/adr");
        var fileSystemService = Substitute.For<IFileSystemService>();
        var cancellationToken = CancellationToken.None;
        const string errorMessage = "Invalid file";

        _statusService.StatusChangeSupersedeAdrAsync(fullpath, filename, dref, config, fileSystemService, cancellationToken)
            .Returns(Task.FromResult((false, errorMessage)));

        // Act
        var (isValid, error) = await _adrService.StatusChangeSupersedeAdrAsync(fullpath, filename, dref, config, fileSystemService, cancellationToken);

        // Assert
        isValid.Should().BeFalse();
        error.Should().Be(errorMessage);
    }

    #endregion

    #region StatusChangeAdrAsync Tests

    [Fact]
    public async Task StatusChangeAdrAsync_WithValidInputs_DelegatesAndReturnsResult()
    {
        // Arrange
        const string fullpath = "/path/to/ADR-0001.md";
        var adrStatus = AdrStatus.Rejected;
        var dref = new DateTime(2024, 1, 1);
        var config = new AdrPlusRepoConfig("# {0}", "doc/adr");
        var fileSystemService = Substitute.For<IFileSystemService>();
        var cancellationToken = CancellationToken.None;

        _statusService.StatusChangeAdrAsync(fullpath, adrStatus, dref, config, fileSystemService, cancellationToken)
            .Returns(Task.FromResult((true, "")));

        // Act
        var (isValid, error) = await _adrService.StatusChangeAdrAsync(fullpath, adrStatus, dref, config, fileSystemService, cancellationToken);

        // Assert
        isValid.Should().BeTrue();
        error.Should().BeEmpty();
        await _statusService.Received(1).StatusChangeAdrAsync(fullpath, adrStatus, dref, config, fileSystemService, cancellationToken);
    }

    #endregion

    #region FromJson Tests

    [Fact]
    public void FromJson_WithValidJsonString_DelegatesAndReturnsConfig()
    {
        // Arrange
        const string jsonString = """{"FolderAdr": "doc/adr", "Prefix": "ADR"}""";
        const string template = "# ADR Template";
        var expectedConfig = new AdrPlusRepoConfig("# {0}", "doc/adr") { Prefix = "ADR" };

        _configMapper.FromJson(jsonString, template).Returns(expectedConfig);

        // Act
        var result = _adrService.FromJson(jsonString, template);

        // Assert
        result.Should().BeEquivalentTo(expectedConfig);
        _configMapper.Received(1).FromJson(jsonString, template);
    }

    #endregion

    #region ReadAllAdrByNumber Tests

    [Fact]
    public async Task ReadAllAdrByNumber_WithValidSequence_DelegatesAndReturnsResult()
    {
        // Arrange
        const int sequence = 1;
        var fileSystemService = Substitute.For<IFileSystemService>();
        const string directoryPath = "/path/to/adr";
        var config = new AdrPlusRepoConfig("# {0}", "doc/adr");

        var expectedComponents = new[]
        {
            new AdrFileNameComponents { Number = 1, Version = 1, FileName = "ADR-0001-Decision.md" },
            new AdrFileNameComponents { Number = 1, Version = 2, FileName = "ADR-0001-v02-Decision.md" }
        };

        _queryService.ReadAllAdrByNumber(sequence, fileSystemService, directoryPath, config)
            .Returns(Task.FromResult(expectedComponents));

        // Act
        var result = await _adrService.ReadAllAdrByNumber(sequence, fileSystemService, directoryPath, config);

        // Assert
        result.Should().BeEquivalentTo(expectedComponents);
        await _queryService.Received(1).ReadAllAdrByNumber(sequence, fileSystemService, directoryPath, config);
    }

    #endregion

    #region ReadLatestAdrFiles Tests

    [Fact]
    public async Task ReadLatestAdrFiles_WithValidInputs_DelegatesAndReturnsResult()
    {
        // Arrange
        var fileSystemService = Substitute.For<IFileSystemService>();
        const string directoryPath = "/path/to/adr";
        var config = new AdrPlusRepoConfig("# {0}", "doc/adr");

        var expectedComponents = new[]
        {
            new AdrFileNameComponents { Number = 2, Version = 1, FileName = "ADR-0002-Decision.md" },
            new AdrFileNameComponents { Number = 1, Version = 2, FileName = "ADR-0001-v02-Decision.md" }
        };

        _queryService.ReadLatestAdrFiles(fileSystemService, directoryPath, config)
            .Returns(Task.FromResult(expectedComponents));

        // Act
        var result = await _adrService.ReadLatestAdrFiles(fileSystemService, directoryPath, config);

        // Assert
        result.Should().BeEquivalentTo(expectedComponents);
        await _queryService.Received(1).ReadLatestAdrFiles(fileSystemService, directoryPath, config);
    }

    #endregion

    #region GetFileByUniqueTitle Tests

    [Fact]
    public async Task GetFileByUniqueTitle_WithMatchingTitle_DelegatesAndReturnsFilePath()
    {
        // Arrange
        const string title = "Decision";
        const string domain = "Enterprise";
        var fileSystemService = Substitute.For<IFileSystemService>();
        const string rootrepo = "/path/to/repo";
        var config = new AdrPlusRepoConfig("# {0}", "doc/adr");
        const string expectedFilePath = "ADR-0001-Decision.md";

        _queryService.GetFileByUniqueTitle(title, domain, fileSystemService, rootrepo, config)
            .Returns(Task.FromResult(expectedFilePath));

        // Act
        var result = await _adrService.GetFileByUniqueTitle(title, domain, fileSystemService, rootrepo, config);

        // Assert
        result.Should().Be(expectedFilePath);
        await _queryService.Received(1).GetFileByUniqueTitle(title, domain, fileSystemService, rootrepo, config);
    }

    [Fact]
    public async Task GetFileByUniqueTitle_WithNoMatch_DelegatesAndReturnsEmptyString()
    {
        // Arrange
        const string title = "NonExistent";
        const string domain = "Unknown";
        var fileSystemService = Substitute.For<IFileSystemService>();
        const string rootrepo = "/path/to/repo";
        var config = new AdrPlusRepoConfig("# {0}", "doc/adr");

        _queryService.GetFileByUniqueTitle(title, domain, fileSystemService, rootrepo, config)
            .Returns(Task.FromResult(string.Empty));

        // Act
        var result = await _adrService.GetFileByUniqueTitle(title, domain, fileSystemService, rootrepo, config);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetNextNumber Tests

    [Fact]
    public async Task GetNextNumber_WithExistingFiles_DelegatesAndReturnsNextNumber()
    {
        // Arrange
        var fileSystemService = Substitute.For<IFileSystemService>();
        const string directoryPath = "/path/to/adr";
        var config = new AdrPlusRepoConfig("# {0}", "doc/adr");
        const int expectedNextNumber = 5;

        _queryService.GetNextNumber(fileSystemService, directoryPath, config)
            .Returns(Task.FromResult(expectedNextNumber));

        // Act
        var result = await _adrService.GetNextNumber(fileSystemService, directoryPath, config);

        // Assert
        result.Should().Be(expectedNextNumber);
        await _queryService.Received(1).GetNextNumber(fileSystemService, directoryPath, config);
    }

    [Fact]
    public async Task GetNextNumber_WithEmptyDirectory_DelegatesAndReturnsOne()
    {
        // Arrange
        var fileSystemService = Substitute.For<IFileSystemService>();
        const string directoryPath = "/path/to/adr";
        var config = new AdrPlusRepoConfig("# {0}", "doc/adr");
        const int expectedNextNumber = 1;

        _queryService.GetNextNumber(fileSystemService, directoryPath, config)
            .Returns(Task.FromResult(expectedNextNumber));

        // Act
        var result = await _adrService.GetNextNumber(fileSystemService, directoryPath, config);

        // Assert
        result.Should().Be(expectedNextNumber);
    }

    #endregion

    #region GetLatestADRSequence Tests

    [Fact]
    public async Task GetLatestADRSequence_WithExistingSequence_DelegatesAndReturnsComponent()
    {
        // Arrange
        const int sequence = 1;
        var fileSystemService = Substitute.For<IFileSystemService>();
        const string rootPath = "/path/to/repo";
        var config = new AdrPlusRepoConfig("# {0}", "doc/adr");
        var expectedComponent = new AdrFileNameComponents 
        { 
            Number = 1, 
            Version = 2, 
            FileName = "ADR-0001-v02-Decision.md" 
        };

        _queryService.GetLatestADRSequence(sequence, fileSystemService, rootPath, config)
            .Returns(Task.FromResult((AdrFileNameComponents?)expectedComponent));

        // Act
        var result = await _adrService.GetLatestADRSequence(sequence, fileSystemService, rootPath, config);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedComponent);
        await _queryService.Received(1).GetLatestADRSequence(sequence, fileSystemService, rootPath, config);
    }

    [Fact]
    public async Task GetLatestADRSequence_WithNoExistingSequence_DelegatesAndReturnsNull()
    {
        // Arrange
        const int sequence = 99;
        var fileSystemService = Substitute.For<IFileSystemService>();
        const string rootPath = "/path/to/repo";
        var config = new AdrPlusRepoConfig("# {0}", "doc/adr");

        _queryService.GetLatestADRSequence(sequence, fileSystemService, rootPath, config)
            .Returns(Task.FromResult((AdrFileNameComponents?)null));

        // Act
        var result = await _adrService.GetLatestADRSequence(sequence, fileSystemService, rootPath, config);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetDomains Tests

    [Fact]
    public async Task GetDomains_WithMultipleDomains_DelegatesAndReturnsDistinctDomains()
    {
        // Arrange
        var fileSystemService = Substitute.For<IFileSystemService>();
        const string directoryPath = "/path/to/adr";
        var config = new AdrPlusRepoConfig("# {0}", "doc/adr");
        var expectedDomains = new[] { "Enterprise", "Team", "Project" };

        _queryService.GetDomains(fileSystemService, directoryPath, config)
            .Returns(Task.FromResult(expectedDomains));

        // Act
        var result = await _adrService.GetDomains(fileSystemService, directoryPath, config);

        // Assert
        result.Should().BeEquivalentTo(expectedDomains);
        await _queryService.Received(1).GetDomains(fileSystemService, directoryPath, config);
    }

    [Fact]
    public async Task GetDomains_WithNoDomains_DelegatesAndReturnsEmptyArray()
    {
        // Arrange
        var fileSystemService = Substitute.For<IFileSystemService>();
        const string directoryPath = "/path/to/adr";
        var config = new AdrPlusRepoConfig("# {0}", "doc/adr");
        var expectedDomains = Array.Empty<string>();

        _queryService.GetDomains(fileSystemService, directoryPath, config)
            .Returns(Task.FromResult(expectedDomains));

        // Act
        var result = await _adrService.GetDomains(fileSystemService, directoryPath, config);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region ParseFileName Tests

    [Fact]
    public async Task ParseFileName_WithValidFilePath_DelegatesAndReturnsComponent()
    {
        // Arrange
        const string filePath = "/path/to/ADR-0001-Decision.md";
        var config = new AdrPlusRepoConfig("# {0}", "doc/adr");
        var fileSystemService = Substitute.For<IFileSystemService>();
        var expectedComponent = new AdrFileNameComponents 
        { 
            Number = 1, 
            FileName = "ADR-0001-Decision.md",
            IsValid = true
        };

        _fileParser.ParseFileName(filePath, config, fileSystemService)
            .Returns(Task.FromResult(expectedComponent));

        // Act
        var result = await _adrService.ParseFileName(filePath, config, fileSystemService);

        // Assert
        result.Should().BeEquivalentTo(expectedComponent);
        await _fileParser.Received(1).ParseFileName(filePath, config, fileSystemService);
    }

    [Fact]
    public async Task ParseFileName_WithInvalidFilePath_DelegatesAndReturnsInvalidComponent()
    {
        // Arrange
        const string filePath = "/path/to/Invalid.md";
        var config = new AdrPlusRepoConfig("# {0}", "doc/adr");
        var fileSystemService = Substitute.For<IFileSystemService>();
        var expectedComponent = new AdrFileNameComponents 
        { 
            IsValid = false,
            ErrorMessage = "Invalid filename format"
        };

        _fileParser.ParseFileName(filePath, config, fileSystemService)
            .Returns(Task.FromResult(expectedComponent));

        // Act
        var result = await _adrService.ParseFileName(filePath, config, fileSystemService);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid filename format");
    }

    #endregion

    #region GenerateCommandsMap Tests

    [Fact]
    public void GenerateCommandsMap_WithValidCommands_DelegatesAndReturnsMap()
    {
        // Arrange
        var expectedMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            { "init", typeof(object) },
            { "new", typeof(object) },
            { "list", typeof(object) }
        };

        _commandMetadataService.GenerateCommandsMap().Returns(expectedMap);

        // Act
        var result = _adrService.GenerateCommandsMap();

        // Assert
        result.Should().BeEquivalentTo(expectedMap);
        _commandMetadataService.Received(1).GenerateCommandsMap();
    }

    #endregion

    #region OpenFile Tests

    [Fact]
    public void OpenFile_WithValidFilePath_DelegatesAndReturnsEmptyStringOnSuccess()
    {
        // Arrange
        const string filepath = "/path/to/ADR-0001-Decision.md";
        const string command = "notepad";

        _commandMetadataService.OpenFile(filepath, command).Returns(string.Empty);

        // Act
        var result = _adrService.OpenFile(filepath, command);

        // Assert
        result.Should().BeEmpty();
        _commandMetadataService.Received(1).OpenFile(filepath, command);
    }

    [Fact]
    public void OpenFile_WithInvalidFilePath_DelegatesAndReturnsErrorMessage()
    {
        // Arrange
        const string filepath = "/path/to/nonexistent.md";
        const string command = "notepad";
        const string errorMessage = "File not found";

        _commandMetadataService.OpenFile(filepath, command).Returns(errorMessage);

        // Act
        var result = _adrService.OpenFile(filepath, command);

        // Assert
        result.Should().Be(errorMessage);
    }

    #endregion

    #region GetCommands Tests

    [Fact]
    public void GetCommands_WithValidCommands_DelegatesAndReturnsCommandArray()
    {
        // Arrange
        var expectedCommands = new[]
        {
            (CommandsAdr.Init, "init", typeof(object), "Initialize ADR"),
            (CommandsAdr.New, "new", typeof(object), "Create new ADR"),
            (CommandsAdr.Help, "help", typeof(object), "Show help")
        };

        _commandMetadataService.GetCommands().Returns(expectedCommands);

        // Act
        var result = _adrService.GetCommands();

        // Assert
        result.Should().HaveCount(3);
        result[0].Command.Should().Be(CommandsAdr.Init);
        result[1].Command.Should().Be(CommandsAdr.New);
        result[2].Command.Should().Be(CommandsAdr.Help);
        _commandMetadataService.Received(1).GetCommands();
    }

    #endregion

    #region ParseArgs Tests

    [Fact]
    public void ParseArgs_WithValidArgs_DelegatesAndReturnsParsedDictionary()
    {
        // Arrange
        var args = new[] { "-t", "my-title", "-d", "Enterprise" };
        var argsForCommand = new[] { Arguments.TitleAdr, Arguments.DomainAdr };
        var expectedParsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.TitleAdr, "my-title" },
            { Arguments.DomainAdr, "Enterprise" }
        };

        _commandMetadataService.ParseArgs(args, argsForCommand).Returns(expectedParsedArgs);

        // Act
        var result = _adrService.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().BeEquivalentTo(expectedParsedArgs);
        _commandMetadataService.Received(1).ParseArgs(args, argsForCommand);
    }

    [Fact]
    public void ParseArgs_WithHelpFlag_DelegatesAndReturnsEmpty()
    {
        // Arrange
        var args = new[] { "-h" };
        var argsForCommand = new[] { Arguments.TitleAdr, Arguments.DomainAdr };
        var expectedParsedArgs = new Dictionary<Arguments, string>();

        _commandMetadataService.ParseArgs(args, argsForCommand).Returns(expectedParsedArgs);

        // Act
        var result = _adrService.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetHelpText Tests

    [Fact]
    public void GetHelpText_WithValidCommand_DelegatesAndReturnsHelpText()
    {
        // Arrange
        const string command = "new";
        var argsForCommand = new[] { Arguments.TitleAdr, Arguments.DomainAdr };
        var examples = new[] { "adr new -t MyTitle -d Enterprise", "adr new -t MyTitle" };
        const string expectedHelpText = "Usage: adr new [-t TITLE] [-d DOMAIN]\nExamples:\n  adr new -t MyTitle -d Enterprise\n  adr new -t MyTitle";

        _commandMetadataService.GetHelpText(command, argsForCommand, examples).Returns(expectedHelpText);

        // Act
        var result = _adrService.GetHelpText(command, argsForCommand, examples);

        // Assert
        result.Should().Be(expectedHelpText);
        _commandMetadataService.Received(1).GetHelpText(command, argsForCommand, examples);
    }

    #endregion
}
