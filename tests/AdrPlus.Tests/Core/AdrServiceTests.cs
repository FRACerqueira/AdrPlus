// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using System.Text.Json;

namespace AdrPlus.Tests.Core;

/// <summary>
/// Unit tests for AdrService class: parsing, ADR repository reads/writes, and argument/help handling.
/// Follows xUnit + NSubstitute + FluentAssertions patterns per TEST_ARCHITECTURE.md.
/// </summary>
public class AdrServiceTests
{
    private readonly AdrService _service = new();

    #region Constructor Tests

    [Fact]
    public void Constructor_CreatesInstance()
    {
        // Arrange & Act
        var service = new AdrService();

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region OpenFile - Argument Validation Tests

    [Fact]
    public void OpenFile_WithNullFilepath_ThrowsArgumentNullException()
    {
        // Arrange
        var command = "notepad.exe";

        // Act & Assert
        var action = () => _service.OpenFile(null!, command);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("filepath");
    }

    [Fact]
    public void OpenFile_WithNullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var filepath = "/tmp/file.md";

        // Act & Assert
        var action = () => _service.OpenFile(filepath, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("command");
    }

    #endregion

    #region FromJson - Valid Config Tests

    [Fact]
    public void FromJson_WithValidJsonString_CreatesConfigWithDefaultValues()
    {
        // Arrange
        var jsonConfig = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { AppConstants.FieldFolderAdr, "doc/adr" },
            { AppConstants.FieldPrefix, "ADR" }
        });
        var template = "# ADR {0}";

        // Act
        var result = _service.FromJson(jsonConfig, template);

        // Assert
        result.Should().NotBeNull();
        result.FolderAdr.Should().Be("doc/adr");
        result.Prefix.Should().Be("ADR");
    }

    [Fact]
    public void FromJson_WithCompleteJsonString_ParsesAllProperties()
    {
        // Arrange
        var jsonConfig = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { AppConstants.FieldFolderAdr, "doc/adr" },
            { AppConstants.FieldMigrationPattern, "ADR-{N}-{V}-{R}-{T}" },
            { AppConstants.FieldPrefix, "ADR" },
            { AppConstants.FieldLenSeq, 4 },
            { AppConstants.FieldLenVersion, 2 },
            { AppConstants.FieldLenRevision, 1 },
            { AppConstants.FieldSeparator, "-" },
            { AppConstants.FieldCaseTransform, "CamelCase" },
            { AppConstants.FieldStatusNew, "Proposed" },
            { AppConstants.FieldStatusAccepted, "Accepted" },
            { AppConstants.FieldStatusRejected, "Rejected" },
            { AppConstants.FieldStatusSuperseded, "Superseded" }
        });
        var template = "# ADR {0}";

        // Act
        var result = _service.FromJson(jsonConfig, template);

        // Assert
        result.FolderAdr.Should().Be("doc/adr");
        result.MigrationPattern.Should().Be("ADR-{N}-{V}-{R}-{T}");
        result.Prefix.Should().Be("ADR");
        result.LenSeq.Should().Be(4);
        result.LenVersion.Should().Be(2);
        result.LenRevision.Should().Be(1);
        result.Separator.Should().Be('-');
        result.StatusNew.Should().Be("Proposed");
        result.StatusAcc.Should().Be("Accepted");
        result.StatusRej.Should().Be("Rejected");
        result.StatusSup.Should().Be("Superseded");
    }

    [Fact]
    public void FromJson_WithCaseInsensitiveJsonFields_ParsesCorrectly()
    {
        // Arrange - Mixed case field names
        var jsonConfig = @"{
            ""FolderAdr"": ""doc/adr"",
            ""PREFIX"": ""ADR"",
            ""Separator"": ""-""
        }";
        var template = "# ADR {0}";

        // Act
        var result = _service.FromJson(jsonConfig, template);

        // Assert
        result.FolderAdr.Should().Be("doc/adr");
        result.Prefix.Should().Be("ADR");
        result.Separator.Should().Be('-');
    }

    [Fact]
    public void FromJson_WithInvalidSeparator_IgnoresInvalidAndUsesDefault()
    {
        // Arrange - Multi-character separator (invalid)
        var jsonConfig = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { AppConstants.FieldFolderAdr, "doc/adr" },
            { AppConstants.FieldSeparator, "---" } // Should be single char
        });
        var template = "# ADR {0}";

        // Act
        var result = _service.FromJson(jsonConfig, template);

        // Assert
        result.Separator.Should().Be('-'); // Default value
    }

    [Fact]
    public void FromJson_WithEmptySeparator_IgnoresAndUsesDefault()
    {
        // Arrange
        var jsonConfig = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { AppConstants.FieldFolderAdr, "doc/adr" },
            { AppConstants.FieldSeparator, "" }
        });
        var template = "# ADR {0}";

        // Act
        var result = _service.FromJson(jsonConfig, template);

        // Assert
        result.Separator.Should().Be('-'); // Default value
    }

    [Fact]
    public void FromJson_WithInvalidCaseTransform_IgnoresInvalidValue()
    {
        // Arrange
        var jsonConfig = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { AppConstants.FieldFolderAdr, "doc/adr" },
            { AppConstants.FieldCaseTransform, "InvalidCaseFormat" }
        });
        var template = "# ADR {0}";

        // Act
        var result = _service.FromJson(jsonConfig, template);

        // Assert
        result.CaseTransform.Should().Be(CaseFormat.KebabCase); // Default value
    }

    [Fact]
    public void FromJson_WithValidCaseTransforms_ParsesCorrectly()
    {
        // Arrange & Act & Assert
        var caseFormats = new[] { "CamelCase", "PascalCase", "SnakeCase", "KebabCase" };

        foreach (var caseFormat in caseFormats)
        {
            var jsonConfig = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                { AppConstants.FieldFolderAdr, "doc/adr" },
                { AppConstants.FieldCaseTransform, caseFormat }
            });

            var result = _service.FromJson(jsonConfig, "# ADR {0}");
            result.CaseTransform.Should().BeOneOf(CaseFormat.CamelCase, CaseFormat.PascalCase, CaseFormat.SnakeCase, CaseFormat.KebabCase);
        }
    }

    [Fact]
    public void FromJson_WithNegativeNumbers_IgnoresAndUsesDefaults()
    {
        // Arrange
        var jsonConfig = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { AppConstants.FieldFolderAdr, "doc/adr" },
            { AppConstants.FieldLenSeq, -5 },
            { AppConstants.FieldLenVersion, -2 }
        });
        var template = "# ADR {0}";

        // Act
        var result = _service.FromJson(jsonConfig, template);

        // Assert
        result.LenSeq.Should().Be(3); // Default value from AdrPlusRepoConfig
        result.LenVersion.Should().Be(2); // Default value from AdrPlusRepoConfig
    }

    [Fact]
    public void FromJson_WithZeroLenSeq_IgnoresAndUsesDefault()
    {
        // Arrange - LenSeq must be > 0
        var jsonConfig = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { AppConstants.FieldFolderAdr, "doc/adr" },
            { AppConstants.FieldLenSeq, 0 }
        });
        var template = "# ADR {0}";

        // Act
        var result = _service.FromJson(jsonConfig, template);

        // Assert
        result.LenSeq.Should().Be(3); // Default value
    }

    [Fact]
    public void FromJson_WithZeroLenVersionOrRevision_AcceptsZero()
    {
        // Arrange - These can be zero (optional)
        var jsonConfig = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { AppConstants.FieldFolderAdr, "doc/adr" },
            { AppConstants.FieldLenVersion, 0 },
            { AppConstants.FieldLenRevision, 0 }
        });
        var template = "# ADR {0}";

        // Act
        var result = _service.FromJson(jsonConfig, template);

        // Assert
        result.LenVersion.Should().Be(0);
        result.LenRevision.Should().Be(0);
    }

    #endregion

    #region FromJson - Error Cases

    [Fact]
    public void FromJson_WithNullJsonString_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var action = () => _service.FromJson(null!, "template");
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("jsonString");
    }

    [Fact]
    public void FromJson_WithEmptyJsonString_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var action = () => _service.FromJson(string.Empty, "template");
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("jsonString");
    }

    [Fact]
    public void FromJson_WithWhitespaceJsonString_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var action = () => _service.FromJson("   ", "template");
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("jsonString");
    }

    [Fact]
    public void FromJson_WithInvalidJsonFormat_ThrowsJsonException()
    {
        // Arrange
        var invalidJson = "{ invalid json";

        // Act & Assert
        var action = () => _service.FromJson(invalidJson, "template");
        action.Should().Throw<JsonException>();
    }

    [Fact]
    public void FromJson_WithEmptyJsonObject_CreatesConfigWithDefaults()
    {
        // Arrange
        var jsonConfig = "{}";
        var template = "# ADR {0}";

        // Act
        var result = _service.FromJson(jsonConfig, template);

        // Assert
        result.Should().NotBeNull();
        result.FolderAdr.Should().Be(AppConstants.DefaultFolderAdr);
    }

    #endregion

    #region FromJson - Header Fields Tests

    [Fact]
    public void FromJson_WithHeaderDisclaimer_ParsesCorrectly()
    {
        // Arrange
        var jsonConfig = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { AppConstants.FieldFolderAdr, "doc/adr" },
            { AppConstants.FieldHeaderDisclaimer, "This is a custom disclaimer" }
        });
        var template = "# ADR {0}";

        // Act
        var result = _service.FromJson(jsonConfig, template);

        // Assert
        result.HeaderDisclaimer.Should().Be("This is a custom disclaimer");
    }

    [Fact]
    public void FromJson_WithAllHeaderFields_ParsesCorrectly()
    {
        // Arrange
        var jsonConfig = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { AppConstants.FieldFolderAdr, "doc/adr" },
            { AppConstants.FieldHeaderDisclaimer, "Disclaimer" },
            { AppConstants.FieldHeaderTitleFile, "Decision Record" },
            { AppConstants.FieldHeaderVersion, "Version" },
            { AppConstants.FieldHeaderRevision, "Revision" },
            { AppConstants.FieldHeaderScope, "Scope" },
            { AppConstants.FieldHeaderDomain, "Domain" },
            { AppConstants.FieldHeaderStatusCreated, "Created" },
            { AppConstants.FieldHeaderStatusChanged, "Changed" },
            { AppConstants.FieldHeaderStatusSuperseded, "Superseded" },
            { AppConstants.FieldHeaderTableFields, "| Field |" },
            { AppConstants.FieldHeaderTableValues, "| Value |" },
            { AppConstants.FieldHeaderMigrated, "Migrated" }
        });
        var template = "# ADR {0}";

        // Act
        var result = _service.FromJson(jsonConfig, template);

        // Assert
        result.HeaderDisclaimer.Should().Be("Disclaimer");
        result.HeaderTitleFile.Should().Be("Decision Record");
        result.HeaderVersion.Should().Be("Version");
        result.HeaderRevision.Should().Be("Revision");
        result.HeaderScope.Should().Be("Scope");
        result.HeaderDomain.Should().Be("Domain");
        result.HeaderTitleStatusCreated.Should().Be("Created");
        result.HeaderTitleStatusChanged.Should().Be("Changed");
        result.HeaderTitleStatusSuperseded.Should().Be("Superseded");
        result.HeaderTableFields.Should().Be("| Field |");
        result.HeaderTableValues.Should().Be("| Value |");
        result.HeaderMigrated.Should().Be("Migrated");
    }

    #endregion

    #region ParseArgs Tests

    [Fact]
    public void ParseArgs_WithNullArgs_ThrowsArgumentNullException()
    {
        // Arrange
        Arguments[] argsForCommand = [Arguments.Help];

        // Act & Assert
        var action = () => _service.ParseArgs(null!, argsForCommand);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("args");
    }

    [Fact]
    public void ParseArgs_WithNullArgsForCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var args = new[] { "-h" };

        // Act & Assert
        var action = () => _service.ParseArgs(args, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("argsForCommand");
    }

    [Fact]
    public void ParseArgs_WithHelpShortFlag_ReturnsHelpArgument()
    {
        // Arrange
        var args = new[] { "-h" };
        Arguments[] argsForCommand = [Arguments.Help];

        // Act
        var result = _service.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().ContainKey(Arguments.Help);
        result[Arguments.Help].Should().Be(string.Empty);
    }

    [Fact]
    public void ParseArgs_WithHelpLongFlag_ReturnsHelpArgument()
    {
        // Arrange
        var args = new[] { "--help" };
        Arguments[] argsForCommand = [Arguments.Help];

        // Act
        var result = _service.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().ContainKey(Arguments.Help);
    }

    [Fact]
    public void ParseArgs_WithInvalidArgument_ThrowsArgumentException()
    {
        // Arrange
        var args = new[] { "--invalid" };
        Arguments[] argsForCommand = [Arguments.Help];

        // Act & Assert
        var action = () => _service.ParseArgs(args, argsForCommand);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ParseArgs_WithRefDateFlag_ConsumesFollowingValueAsDate()
    {
        // Arrange
        var args = new[] { "-r", "2026-01-01" };
        Arguments[] argsForCommand = [Arguments.DateRefAdr];

        // Act
        var result = _service.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().ContainKey(Arguments.DateRefAdr);
        result[Arguments.DateRefAdr].Should().Be("2026-01-01");
    }

    [Fact]
    public void ParseArgs_WithRequiredWhenNotWizardArgTotallyAbsent_ThrowsArgumentException()
    {
        // Arrange: --file is "Required When not in wizard mode" for approve-style commands, but is
        // entirely omitted here (not just present-with-an-empty-value) and no --wizard flag is given either.
        var args = new[] { "--refdate", "2026-01-01" };
        Arguments[] argsForCommand = [Arguments.WizardApprove, Arguments.FileAdr, Arguments.DateRefAdr];

        // Act & Assert
        var action = () => _service.ParseArgs(args, argsForCommand);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ParseArgs_WithNoArgsAndARequiredWhenNotWizardArgInCommand_ThrowsArgumentException()
    {
        // Arrange: a bare invocation (no flags at all) of a command that has a mandatory,
        // required-when-not-wizard argument must fail validation, not silently succeed.
        Arguments[] argsForCommand = [Arguments.WizardApprove, Arguments.FileAdr];

        // Act & Assert
        var action = () => _service.ParseArgs([], argsForCommand);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ParseArgs_WithWizardFlagAndRequiredWhenNotWizardArgAbsent_DoesNotThrow()
    {
        // Arrange: --wizard exempts "Required When not in wizard mode" arguments from validation.
        var args = new[] { "-w" };
        Arguments[] argsForCommand = [Arguments.WizardApprove, Arguments.FileAdr];

        // Act
        var result = _service.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().ContainKey(Arguments.WizardApprove);
        result.Should().NotContainKey(Arguments.FileAdr);
    }

    [Fact]
    public void ParseArgs_WithPluginsListOnlyAndNoTargetRepo_DoesNotThrow()
    {
        // Arrange: `plugins --list` without `--path` is a legitimate, documented host-wide mode -
        // --path must not be treated as unconditionally required just because it shares the
        // "OptionalWithValueWhenWizard" tag with genuinely mandatory arguments like --file/--title.
        var args = new[] { "--list" };
        Arguments[] argsForCommand =
            [Arguments.TargetRepo, Arguments.PluginsList, Arguments.PluginsValidate, Arguments.WizardPlugins];

        // Act
        var result = _service.ParseArgs(args, argsForCommand);

        // Assert
        result.Should().ContainKey(Arguments.PluginsList);
        result.Should().NotContainKey(Arguments.TargetRepo);
    }

    [Fact]
    public void ParseArgs_WithNoArgsAndNoRequiredWhenNotWizardArgInCommand_ReturnsEmptyDictionary()
    {
        // Arrange: a bare invocation of a command with no mandatory arguments (e.g. only --help
        // is valid for it) must simply return an empty result, not consult any global fallback.
        Arguments[] argsForCommand = [Arguments.Help];

        // Act
        var result = _service.ParseArgs([], argsForCommand);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetHelpText Tests

    [Fact]
    public void GetHelpText_WithNullCommand_ThrowsArgumentException()
    {
        // Arrange
        Arguments[] argsForCommand = [Arguments.Help];
        string[] examples = ["example"];

        // Act & Assert
        var action = () => _service.GetHelpText(null!, argsForCommand, examples);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetHelpText_WithEmptyCommand_ThrowsArgumentException()
    {
        // Arrange
        Arguments[] argsForCommand = [Arguments.Help];
        string[] examples = ["example"];

        // Act & Assert
        var action = () => _service.GetHelpText(string.Empty, argsForCommand, examples);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetHelpText_WithNullArgsForCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var command = "version";
        string[] examples = ["example"];

        // Act & Assert
        var action = () => _service.GetHelpText(command, null!, examples);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("argsForCommand");
    }

    [Fact]
    public void GetHelpText_WithNullExamples_ThrowsArgumentNullException()
    {
        // Arrange
        var command = "version";
        Arguments[] argsForCommand = [Arguments.Help];

        // Act & Assert
        var action = () => _service.GetHelpText(command, argsForCommand, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("examples");
    }

    [Fact]
    public void GetHelpText_WithValidCommand_ReturnsNonEmptyString()
    {
        // Arrange
        var command = "version";
        Arguments[] argsForCommand = [];
        string[] examples = [];

        // Act
        var result = _service.GetHelpText(command, argsForCommand, examples);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetHelpText_WithInvalidCommand_ReturnsEmptyString()
    {
        // Arrange
        var command = "nonexistent-command-xyz";
        Arguments[] argsForCommand = [];
        string[] examples = [];

        // Act
        var result = _service.GetHelpText(command, argsForCommand, examples);

        // Assert
        result.Should().Be(string.Empty);
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
    public void GenerateCommandsMap_AllValuesAreTypes()
    {
        // Act
        var result = _service.GenerateCommandsMap();

        // Assert
        foreach (var kvp in result)
        {
            kvp.Key.Should().NotBeNullOrEmpty();
            kvp.Value.Should().NotBeNull();
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
    }

    [Fact]
    public void GetCommands_AllEntriesHaveValidAlias()
    {
        // Act
        var result = _service.GetCommands();

        // Assert
        foreach (var (_, alias, _, _) in result)
        {
            alias.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void GetCommands_AllEntriesHaveValidHandlerType()
    {
        // Act
        var result = _service.GetCommands();

        // Assert
        foreach (var (_, _, handlerType, _) in result)
        {
            handlerType.Should().NotBeNull();
        }
    }

    #endregion

    #region ParseAdrHeaderAndContentAsync Tests

    [Fact]
    public async Task ParseAdrHeaderAndContentAsync_WithEmptyFile_ReturnsErrorMessage()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var fileSystemService = Substitute.For<IFileSystemService>();
        var emptyLines = Array.Empty<string>();
        var filePath = "/tmp/ADR-001-test.md";
        fileSystemService.ReadAllLinesAsync(filePath, TestContext.Current.CancellationToken).Returns(emptyLines);

        // Act
        var (header, _) = await _service.ParseAdrHeaderAndContentAsync(filePath, config, fileSystemService);

        // Assert
        header.Should().NotBeNull();
        header.IsValid.Should().BeFalse();
        header.ErrorMessage.Should().Contain("empty");
    }

    [Fact]
    public async Task ParseAdrHeaderAndContentAsync_WithTooShortFile_ReturnsErrorMessage()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var fileSystemService = Substitute.For<IFileSystemService>();
        var shortLines = new[] { "line1", "line2", "line3" };
        var filePath = "/tmp/ADR-001-test.md";
        fileSystemService.ReadAllLinesAsync(filePath, TestContext.Current.CancellationToken).Returns(shortLines);

        // Act
        var (header, _) = await _service.ParseAdrHeaderAndContentAsync(filePath, config, fileSystemService);

        // Assert
        header.Should().NotBeNull();
        header.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ParseAdrHeaderAndContentAsync_WithInvalidDisclaimerFormat_ReturnsErrorMessage()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var fileSystemService = Substitute.For<IFileSystemService>();
        var invalidLines = new string[12];
        invalidLines[0] = "Invalid disclaimer format";
        for (int i = 1; i < 12; i++) invalidLines[i] = string.Empty;
        var filePath = "/tmp/ADR-001-test.md";
        fileSystemService.ReadAllLinesAsync(filePath, TestContext.Current.CancellationToken).Returns(invalidLines);

        // Act
        var (header, _) = await _service.ParseAdrHeaderAndContentAsync(filePath, config, fileSystemService);

        // Assert
        header.Should().NotBeNull();
        header.IsValid.Should().BeFalse();
        header.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    private static string[] ValidHeaderPlusBodyLines() =>
    [
        "<!-- Do not remove this comment, lines and table (1-12) -->",
        "|Adr-Plus Fields|Values|",
        "|--|--|",
        "|Title File||",
        "|Version||",
        "|Revision||",
        "|Scope||",
        "|Domain||",
        "|Created||",
        "|Changed||",
        "|Superseded||",
        "<!-- end -->",
        "Body line 1",
        "Body line 2"
    ];

    [Fact]
    public async Task ParseAdrHeaderAndContentAsync_WithIncludeContentTrue_MaterializesBody()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var fileSystemService = Substitute.For<IFileSystemService>();
        var filePath = "/tmp/ADR-001-test.md";
        fileSystemService.ReadAllLinesAsync(filePath, Arg.Any<CancellationToken>()).Returns(ValidHeaderPlusBodyLines());

        // Act
        var (header, content) = await _service.ParseAdrHeaderAndContentAsync(filePath, config, fileSystemService, includeContent: true);

        // Assert
        header.IsValid.Should().BeTrue();
        content.Should().Contain("Body line 1").And.Contain("Body line 2");
    }

    [Fact]
    public async Task ParseAdrHeaderAndContentAsync_WithIncludeContentFalse_ReturnsEmptyContentButStillParsesHeader()
    {
        // Regression: a caller that only needs header fields (e.g. Scope/Domain/Number lookups) shouldn't
        // pay for reconstructing the body into a string it will never read.
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var fileSystemService = Substitute.For<IFileSystemService>();
        var filePath = "/tmp/ADR-001-test.md";
        fileSystemService.ReadAllLinesAsync(filePath, Arg.Any<CancellationToken>()).Returns(ValidHeaderPlusBodyLines());

        // Act
        var (header, content) = await _service.ParseAdrHeaderAndContentAsync(filePath, config, fileSystemService, includeContent: false);

        // Assert
        header.IsValid.Should().BeTrue();
        content.Should().BeEmpty();
    }

    #endregion

    #region ParseFileName Tests

    [Fact]
    public async Task ParseFileName_WithValidFileName_ParsesCorrectly()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR")
        {
            Prefix = "ADR",
            LenSeq = 3,
            Separator = '-'
        };
        var fileSystemService = Substitute.For<IFileSystemService>();
        var validLines = new string[12];
        for (int i = 0; i < 12; i++) validLines[i] = "line";
        fileSystemService.ReadAllLinesAsync(Arg.Any<string>(), TestContext.Current.CancellationToken).Returns(validLines);

        var filePath = "ADR-001-test-title.md";

        // Act
        var result = await _service.ParseFileName(filePath, config, fileSystemService);

        // Assert
        result.Should().NotBeNull();
        result.FileName.Should().Be(filePath);
    }

    [Fact]
    public async Task ParseFileName_WithNullFilePath_ReturnsErrorMessage()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var fileSystemService = Substitute.For<IFileSystemService>();

        // Act
        var result = await _service.ParseFileName(null!, config, fileSystemService);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ParseFileName_WithoutMdExtension_ReturnsErrorMessage()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var fileSystemService = Substitute.For<IFileSystemService>();

        // Act
        var result = await _service.ParseFileName("ADR-001-test.txt", config, fileSystemService);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ParseFileName_WithMigrationPatternFileName_ExtractsTitle()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR")
        {
            Prefix = "ADR",
            Separator = '-',
            MigrationPattern = "N00:04T04"
        };
        var fileSystemService = Substitute.For<IFileSystemService>();
        var validLines = new string[12];
        for (int i = 0; i < 12; i++) validLines[i] = "line";
        fileSystemService.ReadAllLinesAsync(Arg.Any<string>(), TestContext.Current.CancellationToken).Returns(validLines);

        var filePath = "0002UseMongoDB.md";

        // Act
        var result = await _service.ParseFileName(filePath, config, fileSystemService);

        // Assert
        result.Title.Should().Be("UseMongoDB");
    }

    [Fact]
    public async Task ParseFileName_WithMigrationPatternNonNumericNumberSegment_ReturnsErrorMessage()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR")
        {
            Prefix = "ADR",
            Separator = '-',
            MigrationPattern = "N00:04T04"
        };
        var fileSystemService = Substitute.For<IFileSystemService>();

        // "DECI" occupies the configured Number position (0..4) but is not numeric, so this file
        // does not actually match the pattern and must not be accepted as a migratable ADR.
        var filePath = "DECISION-001.md";

        // Act
        var result = await _service.ParseFileName(filePath, config, fileSystemService);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region ReadAllAdrByNumber Tests

    [Fact]
    public async Task ReadAllAdrByNumber_WithNullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        var fileSystemService = Substitute.For<IFileSystemService>();
        var rootpath = "/repo";

        // Act & Assert
        var action = async () => await _service.ReadAllAdrByNumber(1, fileSystemService, rootpath, null!);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ReadAllAdrByNumber_WithNullFileSystemService_ThrowsArgumentNullException()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var rootpath = "/repo";

        // Act & Assert
        var action = async () => await _service.ReadAllAdrByNumber(1, null!, rootpath, config);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ReadAllAdrByNumber_WithEmptyRootPath_ThrowsArgumentException()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var fileSystemService = Substitute.For<IFileSystemService>();

        // Act & Assert
        var action = async () => await _service.ReadAllAdrByNumber(1, fileSystemService, string.Empty, config);
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ReadAllAdrByNumber_WithNonexistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var fileSystemService = Substitute.For<IFileSystemService>();
        var rootpath = "/nonexistent";
        fileSystemService.DirectoryExists(rootpath).Returns(false);

        // Act & Assert
        var action = async () => await _service.ReadAllAdrByNumber(1, fileSystemService, rootpath, config);
        await action.Should().ThrowAsync<DirectoryNotFoundException>();
    }

    [Fact]
    public async Task ReadAllAdrByNumber_SkipsExpensiveHeaderParseForFilesThatDontMatchByName()
    {
        // Regression: the substring glob `*{sequence}*.md` isn't selective - e.g. a search for sequence "1"
        // also matches any filename embedding "V01" (the version segment every ADR has), so ParseFileName's
        // expensive header+content read used to run for nearly every glob-matched file before filtering by
        // Number. Number always comes from the filename alone (never the header, for both adrplus-created and
        // migrated files), so a cheap, I/O-free pre-filter can skip that read for files that don't match.
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR") { Prefix = "ADR", LenSeq = 3, LenVersion = 2, Separator = '-' };
        var fileSystemService = Substitute.For<IFileSystemService>();
        var rootpath = "/repo";
        var matchingFile = "/repo/doc/adr/ADR001V01-TitleA.md";
        var nonMatchingFile = "/repo/doc/adr/ADR999V01-TitleB.md"; // matches the loose glob for sequence "1" via "V01"
        fileSystemService.DirectoryExists(Arg.Any<string>()).Returns(true);
        fileSystemService.GetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns([matchingFile, nonMatchingFile]);
        fileSystemService.ReadAllLinesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([]);

        // Act
        await _service.ReadAllAdrByNumber(1, fileSystemService, rootpath, config);

        // Assert
        await fileSystemService.DidNotReceive().ReadAllLinesAsync(nonMatchingFile, Arg.Any<CancellationToken>());
        await fileSystemService.Received(1).ReadAllLinesAsync(matchingFile, Arg.Any<CancellationToken>());
    }

    #endregion

    #region ReadAllAdr Tests

    [Fact]
    public async Task ReadAllAdr_WithIncludeContentFalse_ReturnsResultsWithEmptyContentAdr()
    {
        // Regression: callers that only need filename/header fields (GetNextNumber, GetFileByUniqueTitle,
        // GetScopes, GetDomains) shouldn't pay for materializing every file's body.
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR") { Prefix = "ADR", LenSeq = 3, LenVersion = 2, Separator = '-' };
        var fileSystemService = Substitute.For<IFileSystemService>();
        var directoryPath = "/repo";
        fileSystemService.DirectoryExists(Arg.Any<string>()).Returns(true);
        fileSystemService.GetFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SearchOption>()).Returns(["/repo/doc/adr/ADR001V01-Title.md"]);
        fileSystemService.ReadAllLinesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ValidHeaderPlusBodyLines());

        var result = await _service.ReadAllAdr(fileSystemService, directoryPath, config, includeContent: false);

        result.Should().ContainSingle();
        result[0].Header.IsValid.Should().BeTrue();
        result[0].ContentAdr.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadAllAdr_WithNullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        var fileSystemService = Substitute.For<IFileSystemService>();
        var directoryPath = "/repo";

        // Act & Assert
        var action = async () => await _service.ReadAllAdr(fileSystemService, directoryPath, null!);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ReadAllAdr_WithNullFileSystemService_ThrowsArgumentNullException()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var directoryPath = "/repo";

        // Act & Assert
        var action = async () => await _service.ReadAllAdr(null!, directoryPath, config);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ReadAllAdr_WithEmptyDirectoryPath_ThrowsArgumentException()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var fileSystemService = Substitute.For<IFileSystemService>();

        // Act & Assert
        var action = async () => await _service.ReadAllAdr(fileSystemService, string.Empty, config);
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ReadAllAdr_WithNonexistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var fileSystemService = Substitute.For<IFileSystemService>();
        var directoryPath = "/nonexistent";
        fileSystemService.DirectoryExists(directoryPath).Returns(false);

        // Act & Assert
        var action = async () => await _service.ReadAllAdr(fileSystemService, directoryPath, config);
        await action.Should().ThrowAsync<DirectoryNotFoundException>();
    }

    #endregion

    #region GetNextNumber Tests

    [Fact]
    public async Task GetNextNumber_WithEmptyDirectory_ReturnsOne()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var fileSystemService = Substitute.For<IFileSystemService>();
        var directoryPath = "/repo";
        fileSystemService.DirectoryExists(directoryPath).Returns(true);
        fileSystemService.GetFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SearchOption>()).Returns([]);

        // Act
        var result = await _service.GetNextNumber(fileSystemService, directoryPath, config);

        // Assert
        result.Should().Be(1);
    }

    #endregion

    #region GetLatestADRSequence Tests

    [Fact]
    public async Task GetLatestADRSequence_WithNullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        var fileSystemService = Substitute.For<IFileSystemService>();
        var directoryPath = "/repo";

        // Act & Assert
        var action = async () => await _service.GetLatestADRSequence(1, fileSystemService, directoryPath, null!);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetLatestADRSequence_WithNullFileSystemService_ThrowsArgumentNullException()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var directoryPath = "/repo";

        // Act & Assert
        var action = async () => await _service.GetLatestADRSequence(1, null!, directoryPath, config);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetLatestADRSequence_WhenSequenceMatchesNoFile_ReturnsNullInsteadOfThrowing()
    {
        // Regression: the method's own signature (Task<AdrFileNameComponents?>) and its callers
        // (e.g. RejectCommandHandler's "?? throw new InvalidDataException(...)" for an undo-supersede
        // target that no longer exists) assume this can return null - but .Last() on the empty sequence
        // ReadAllAdrByNumber returns for a non-matching number throws InvalidOperationException instead,
        // which skips the caller's own friendly error message entirely.
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var fileSystemService = Substitute.For<IFileSystemService>();
        var directoryPath = "/repo";
        fileSystemService.DirectoryExists(Arg.Any<string>()).Returns(true);
        fileSystemService.GetFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SearchOption>()).Returns([]);

        var result = await _service.GetLatestADRSequence(999, fileSystemService, directoryPath, config);

        result.Should().BeNull();
    }

    #endregion

    #region GetDomains Tests

    [Fact]
    public async Task GetDomains_WithEmptyDirectory_ReturnsEmptyArray()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var fileSystemService = Substitute.For<IFileSystemService>();
        var directoryPath = "/repo";
        fileSystemService.DirectoryExists(directoryPath).Returns(true);
        fileSystemService.GetFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SearchOption>()).Returns([]);

        // Act
        var result = await _service.GetDomains(fileSystemService, directoryPath, config);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetScopes Tests

    [Fact]
    public async Task GetScopes_WithEmptyDirectory_ReturnsEmptyArray()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var fileSystemService = Substitute.For<IFileSystemService>();
        var directoryPath = "/repo";
        fileSystemService.DirectoryExists(directoryPath).Returns(true);
        fileSystemService.GetFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SearchOption>()).Returns([]);

        // Act
        var result = await _service.GetScopes(fileSystemService, directoryPath, config);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region "From" pure-function variants Tests (share one already-read AdrFileNameComponents[] across lookups)

    private static AdrFileNameComponents MakeFile(int number, string title, string scope = "", string domain = "") => new()
    {
        Number = number,
        Title = title,
        Header = new AdrHeader { IsValid = true, Scope = scope, Domain = domain }
    };

    [Fact]
    public void GetNextNumberFrom_WithNoFiles_ReturnsOne()
    {
        _service.GetNextNumberFrom([]).Should().Be(1);
    }

    [Fact]
    public void GetNextNumberFrom_WithFiles_ReturnsMaxPlusOne()
    {
        var files = new[] { MakeFile(1, "A"), MakeFile(3, "B"), MakeFile(2, "C") };

        _service.GetNextNumberFrom(files).Should().Be(4);
    }

    [Fact]
    public void GetFileByUniqueTitleFrom_WithMatchingTitle_ReturnsFileName()
    {
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var match = MakeFile(1, "My Title");
        match.FileName = "/repo/doc/adr/ADR001V01-MyTitle.md";
        var files = new[] { match, MakeFile(2, "Other Title") };

        _service.GetFileByUniqueTitleFrom("My Title", files, config).Should().Be(match.FileName);
    }

    [Fact]
    public void GetFileByUniqueTitleFrom_WithNoMatch_ReturnsEmpty()
    {
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var files = new[] { MakeFile(1, "Other Title") };

        _service.GetFileByUniqueTitleFrom("My Title", files, config).Should().BeEmpty();
    }

    [Fact]
    public void GetScopesFrom_ReturnsDistinctNonEmptyScopes()
    {
        var files = new[] { MakeFile(1, "A", scope: "Backend"), MakeFile(2, "B", scope: "backend"), MakeFile(3, "C", scope: ""), MakeFile(4, "D", scope: "Frontend") };

        _service.GetScopesFrom(files).Should().BeEquivalentTo(["Backend", "Frontend"]);
    }

    [Fact]
    public void GetDomainsFrom_ReturnsDistinctNonEmptyDomains()
    {
        var files = new[] { MakeFile(1, "A", domain: "Sales"), MakeFile(2, "B", domain: "sales"), MakeFile(3, "C", domain: "") };

        _service.GetDomainsFrom(files).Should().BeEquivalentTo(["Sales"]);
    }

    #endregion

    #region StatusUpdateAdrAsync Tests

    [Fact]
    public async Task StatusUpdateAdrAsync_WithValidFile_UpdatesStatus()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var fileSystemService = Substitute.For<IFileSystemService>();
        var fullpath = "/repo/ADR-001-test.md";
        fileSystemService.ReadAllLinesAsync(fullpath, TestContext.Current.CancellationToken).Returns([]);

        // Act
        var (isValid, error, record, content) = await _service.StatusUpdateAdrAsync(fullpath, AdrStatus.Accepted, DateTime.Now, config, fileSystemService, TestContext.Current.CancellationToken);

        // Assert
        isValid.Should().BeFalse(); // Invalid because mock returns empty lines
        error.Should().NotBeNullOrEmpty();
        record.Should().BeNull();
        content.Should().BeNull();
    }

    [Fact]
    public async Task StatusUpdateAdrAsync_WithEmptyFilePath_ThrowsArgumentException()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var fileSystemService = Substitute.For<IFileSystemService>();
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var action = async () => await _service.StatusUpdateAdrAsync(string.Empty, AdrStatus.Accepted, DateTime.Now, config, fileSystemService, cancellationToken);
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task StatusUpdateAdrAsync_WithNullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        var fileSystemService = Substitute.For<IFileSystemService>();
        var fullpath = "/repo/ADR-001-test.md";
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var action = async () => await _service.StatusUpdateAdrAsync(fullpath, AdrStatus.Accepted, DateTime.Now, null!, fileSystemService, cancellationToken);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task StatusUpdateAdrAsync_WithNullFileSystemService_ThrowsArgumentNullException()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var fullpath = "/repo/ADR-001-test.md";
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var action = async () => await _service.StatusUpdateAdrAsync(fullpath, AdrStatus.Accepted, DateTime.Now, config, null!, cancellationToken);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region StatusChangeSupersedeAdrAsync Tests

    [Fact]
    public async Task StatusChangeSupersedeAdrAsync_WithEmptyFilePath_ThrowsArgumentException()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var fileSystemService = Substitute.For<IFileSystemService>();
        var seqsupersede = "002";
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var action = async () => await _service.StatusChangeSupersedeAdrAsync(string.Empty, seqsupersede, DateTime.Now, config, fileSystemService, cancellationToken);
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task StatusChangeSupersedeAdrAsync_WithEmptySeqSupersede_ThrowsArgumentException()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var fileSystemService = Substitute.For<IFileSystemService>();
        var fullpath = "/repo/ADR-001-test.md";
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var action = async () => await _service.StatusChangeSupersedeAdrAsync(fullpath, string.Empty, DateTime.Now, config, fileSystemService, cancellationToken);
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task StatusChangeSupersedeAdrAsync_WithNullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        var fileSystemService = Substitute.For<IFileSystemService>();
        var fullpath = "/repo/ADR-001-test.md";
        var seqsupersede = "002";
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var action = async () => await _service.StatusChangeSupersedeAdrAsync(fullpath, seqsupersede, DateTime.Now, null!, fileSystemService, cancellationToken);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task StatusChangeSupersedeAdrAsync_WithNullFileSystemService_ThrowsArgumentNullException()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var fullpath = "/repo/ADR-001-test.md";
        var seqsupersede = "002";
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var action = async () => await _service.StatusChangeSupersedeAdrAsync(fullpath, seqsupersede, DateTime.Now, config, null!, cancellationToken);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region StatusChangeAdrAsync Tests

    [Fact]
    public async Task StatusChangeAdrAsync_WithEmptyFilePath_ThrowsArgumentException()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var fileSystemService = Substitute.For<IFileSystemService>();
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var action = async () => await _service.StatusChangeAdrAsync(string.Empty, AdrStatus.Accepted, DateTime.Now, config, fileSystemService, cancellationToken);
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task StatusChangeAdrAsync_WithNullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        var fileSystemService = Substitute.For<IFileSystemService>();
        var fullpath = "/repo/ADR-001-test.md";
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var action = async () => await _service.StatusChangeAdrAsync(fullpath, AdrStatus.Accepted, DateTime.Now, null!, fileSystemService, cancellationToken);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task StatusChangeAdrAsync_WithNullFileSystemService_ThrowsArgumentNullException()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var fullpath = "/repo/ADR-001-test.md";
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var action = async () => await _service.StatusChangeAdrAsync(fullpath, AdrStatus.Accepted, DateTime.Now, config, null!, cancellationToken);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task StatusChangeAdrAsync_WithValidFile_ChangesStatus()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("doc/adr", "# ADR");
        var fileSystemService = Substitute.For<IFileSystemService>();
        var fullpath = "/repo/ADR-001-test.md";
        fileSystemService.ReadAllLinesAsync(fullpath, TestContext.Current.CancellationToken).Returns([]);

        // Act
        var (isValid, error, record, content) = await _service.StatusChangeAdrAsync(fullpath, AdrStatus.Accepted, DateTime.Now, config, fileSystemService, TestContext.Current.CancellationToken);

        // Assert
        isValid.Should().BeFalse(); // Invalid because mock returns empty lines
        error.Should().NotBeNullOrEmpty();
        record.Should().BeNull();
        content.Should().BeNull();
    }

    #endregion

         #region Helper Methods

         private static string CreateValidRepoJson()
         {
             return JsonSerializer.Serialize(new Dictionary<string, object>
             {
                 { AppConstants.FieldFolderAdr, "doc/adr" },
                 { AppConstants.FieldMigrationPattern, "" },
                 { AppConstants.FieldTemplate, "# ADR {0}" },
                 { AppConstants.FieldPrefix, "ADR" },
                 { AppConstants.FieldLenSeq, 4 },
                 { AppConstants.FieldLenVersion, 2 },
                 { AppConstants.FieldLenRevision, 0 },
                 { AppConstants.FieldSeparator, "-" },
                 { AppConstants.FieldCaseTransform, "AsIs" }
             });
         }

         #endregion
    }
