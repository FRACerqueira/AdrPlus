// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Domain;
using System.Globalization;
using System.Text.Json;

namespace AdrPlus.Tests.Core;

/// <summary>
/// Unit tests for Helper static utility class.
/// Tests cover culture validation, ADR record creation, status line parsing, JSON operations, and file opening.
/// </summary>
public class HelperTests
{
    #region IsValidCultureName Tests

    [Theory]
    [InlineData("en-US")]
    [InlineData("pt-BR")]
    [InlineData("fr-FR")]
    [InlineData("de-DE")]
    [InlineData("es-ES")]
    [InlineData("ja-JP")]
    [InlineData("zh-CN")]
    public void IsValidCultureName_WithValidCultureNames_ReturnsTrue(string cultureName)
    {
        // Act
        var result = Helper.IsValidCultureName(cultureName);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("invalid-culture")]
    [InlineData("xx-YY")]
    [InlineData("invalid")]
    [InlineData("en-")]
    [InlineData("-US")]
    public void IsValidCultureName_WithInvalidCultureNames_ReturnsFalse(string cultureName)
    {
        // Act
        var result = Helper.IsValidCultureName(cultureName);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void IsValidCultureName_WithNullOrWhitespace_ReturnsFalse(string? cultureName)
    {
        // Act
        var result = Helper.IsValidCultureName(cultureName);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidCultureName_WithCultureNotFoundException_ReturnsFalse()
    {
        // Act
        var result = Helper.IsValidCultureName("zzz-ZZZ");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region CreateAdrRecord Tests

    [Fact]
    public void CreateAdrRecord_WithValidParameters_CreatesRecordWithAllProperties()
    {
        // Arrange
        var parseFile = new AdrFileNameComponents
        {
            Number = 1,
            FileName = "001-test-adr.md",
            Header = new AdrHeader
            {
                Title = "Test ADR",
                Scope = "System Design",
                Domain = "Architecture",
                StatusCreate = AdrStatus.Proposed,
                DateCreate = new DateTime(2024, 1, 1),
                StatusUpdate = AdrStatus.Accepted,
                DateUpdate = new DateTime(2024, 1, 15),
                StatusChange = AdrStatus.Unknown,
                DateChange = null,
                Version = 1,
                Revision = 1
            },
            ContentAdr = "# Context\n\n# Decision\n\n# Consequences"
        };

        var config = new AdrPlusRepoConfig("docs/adr", "# Context\n\n# Decision")
        {
            LenRevision = 1
        };

        // Act
        var record = Helper.CreateAdrRecord(parseFile, config);

        // Assert
        record.Should().NotBeNull();
        record.Number.Should().Be(1);
        record.Title.Should().Be("Test ADR");
        record.Scope.Should().Be("System Design");
        record.Domain.Should().Be("Architecture");
        record.StatusCreate.Should().Be(AdrStatus.Proposed);
        record.StatusUpdate.Should().Be(AdrStatus.Accepted);
        record.Version.Should().Be(1);
        record.Revision.Should().Be(1);
        record.Template.Should().Be("# Context\n\n# Decision\n\n# Consequences");
    }

    [Fact]
    public void CreateAdrRecord_WithNullScopeAndDomain_FillsWithEmptyString()
    {
        // Arrange
        var parseFile = new AdrFileNameComponents
        {
            Number = 1,
            FileName = "001-test-adr.md",
            Header = new AdrHeader
            {
                Title = "Test ADR",
                Scope = string.Empty,
                Domain = string.Empty,
                StatusCreate = AdrStatus.Proposed,
                DateCreate = new DateTime(2024, 1, 1),
                StatusUpdate = AdrStatus.Proposed,
                DateUpdate = new DateTime(2024, 1, 1),
                Version = 1,
                Revision = null
            },
            ContentAdr = "# Context"
        };

        var config = new AdrPlusRepoConfig("docs/adr", "# Context")
        {
            LenRevision = 0
        };

        // Act
        var record = Helper.CreateAdrRecord(parseFile, config);

        // Assert
        record.Scope.Should().Be(string.Empty);
        record.Domain.Should().Be(string.Empty);
    }

    [Fact]
    public void CreateAdrRecord_WhenRevisionLengthIsZero_SetsRevisionToNull()
    {
        // Arrange
        var parseFile = new AdrFileNameComponents
        {
            Number = 1,
            FileName = "001-test-adr.md",
            Header = new AdrHeader
            {
                Title = "Test",
                StatusCreate = AdrStatus.Proposed,
                DateCreate = new DateTime(2024, 1, 1),
                StatusUpdate = AdrStatus.Proposed,
                DateUpdate = new DateTime(2024, 1, 1),
                Version = 1,
                Revision = 1
            },
            ContentAdr = "Content"
        };

        var config = new AdrPlusRepoConfig("docs/adr", "Content")
        {
            LenRevision = 0
        };

        // Act
        var record = Helper.CreateAdrRecord(parseFile, config);

        // Assert
        record.Revision.Should().BeNull();
    }

    [Fact]
    public void CreateAdrRecord_WhenRevisionLengthIsPositive_PreservesRevision()
    {
        // Arrange
        var parseFile = new AdrFileNameComponents
        {
            Number = 1,
            FileName = "001-test-adr.md",
            Header = new AdrHeader
            {
                Title = "Test",
                StatusCreate = AdrStatus.Proposed,
                DateCreate = new DateTime(2024, 1, 1),
                StatusUpdate = AdrStatus.Proposed,
                DateUpdate = new DateTime(2024, 1, 1),
                Version = 1,
                Revision = 2
            },
            ContentAdr = "Content"
        };

        var config = new AdrPlusRepoConfig("docs/adr", "Content")
        {
            LenRevision = 1
        };

        // Act
        var record = Helper.CreateAdrRecord(parseFile, config);

        // Assert
        record.Revision.Should().Be(2);
    }

    [Fact]
    public void CreateAdrRecord_WithAllStatusTypes_CreatesRecordCorrectly()
    {
        // Arrange
        var parseFile = new AdrFileNameComponents
        {
            Number = 1,
            FileName = "001-test-adr.md",
            Header = new AdrHeader
            {
                Title = "Test",
                StatusCreate = AdrStatus.Proposed,
                DateCreate = new DateTime(2024, 1, 1),
                StatusUpdate = AdrStatus.Accepted,
                DateUpdate = new DateTime(2024, 1, 15),
                StatusChange = AdrStatus.Superseded,
                DateChange = new DateTime(2024, 2, 1),
                Version = 1,
                Revision = 1
            },
            ContentAdr = "Content"
        };

        var config = new AdrPlusRepoConfig("docs/adr", "Content")
        {
            LenRevision = 1
        };

        // Act
        var record = Helper.CreateAdrRecord(parseFile, config);

        // Assert
        record.StatusCreate.Should().Be(AdrStatus.Proposed);
        record.StatusUpdate.Should().Be(AdrStatus.Accepted);
        record.StatusChange.Should().Be(AdrStatus.Superseded);
        record.ChangeRef.Should().Be(new DateTime(2024, 2, 1));
    }

    [Fact]
    public void CreateAdrRecord_WithNullParseFile_ThrowsArgumentNullException()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("docs/adr", "Template") { LenRevision = 0 };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Helper.CreateAdrRecord(null!, config));
    }

    [Fact]
    public void CreateAdrRecord_WithNullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        var parseFile = new AdrFileNameComponents
        {
            Header = new AdrHeader { Title = "Test" }
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Helper.CreateAdrRecord(parseFile, null!));
    }

    #endregion

    #region ParseStatusLine Tests

    [Fact]
    public void ParseStatusLine_WithValidStatusAndDate_ReturnsCorrectParsing()
    {
        // Arrange
        var statusLine = "Accepted (2024-01-15)";
        var config = new AdrPlusRepoConfig("docs/adr", "Template");

        // Act
        var (status, date, error) = Helper.ParseStatusLine(statusLine, config);

        // Assert
        status.Should().Be(AdrStatus.Accepted);
        date.Should().Be(new DateTime(2024, 1, 15));
        error.Should().BeEmpty();
    }

    [Fact]
    public void ParseStatusLine_WithCaseInsensitiveStatus_ReturnsCorrectParsing()
    {
        // Arrange
        var statusLine = "accepted (2024-01-15)"; // lowercase
        var config = new AdrPlusRepoConfig("docs/adr", "Template");

        // Act
        var (status, date, error) = Helper.ParseStatusLine(statusLine, config);

        // Assert
        status.Should().Be(AdrStatus.Accepted);
        date.Should().Be(new DateTime(2024, 1, 15));
        error.Should().BeEmpty();
    }

    [Fact]
    public void ParseStatusLine_WithMixedCaseStatus_ReturnsCorrectParsing()
    {
        // Arrange
        var statusLine = "ACCEPTED (2024-01-15)"; // uppercase
        var config = new AdrPlusRepoConfig("docs/adr", "Template");

        // Act
        var (status, date, error) = Helper.ParseStatusLine(statusLine, config);

        // Assert
        status.Should().Be(AdrStatus.Accepted);
        date.Should().Be(new DateTime(2024, 1, 15));
        error.Should().BeEmpty();
    }

    [Fact]
    public void ParseStatusLine_WithMissingOpenParentheses_ReturnsError()
    {
        // Arrange
        var statusLine = "Accepted 2024-01-15)"; // missing (
        var config = new AdrPlusRepoConfig("docs/adr", "Template");

        // Act
        var (status, date, error) = Helper.ParseStatusLine(statusLine, config);

        // Assert
        status.Should().Be(AdrStatus.Unknown);
        date.Should().BeNull();
        error.Should().NotBeEmpty();
        error.Should().Contain("format");
    }

    [Fact]
    public void ParseStatusLine_WithMissingCloseParentheses_ReturnsError()
    {
        // Arrange
        var statusLine = "Accepted (2024-01-15"; // missing )
        var config = new AdrPlusRepoConfig("docs/adr", "Template");

        // Act
        var (status, date, error) = Helper.ParseStatusLine(statusLine, config);

        // Assert
        status.Should().Be(AdrStatus.Unknown);
        date.Should().BeNull();
        error.Should().NotBeEmpty();
    }

    [Fact]
    public void ParseStatusLine_WithInvertedParentheses_ReturnsError()
    {
        // Arrange
        var statusLine = "Accepted )2024-01-15("; // inverted
        var config = new AdrPlusRepoConfig("docs/adr", "Template");

        // Act
        var (status, date, error) = Helper.ParseStatusLine(statusLine, config);

        // Assert
        status.Should().Be(AdrStatus.Unknown);
        date.Should().BeNull();
        error.Should().NotBeEmpty();
        error.Should().Contain("parentheses");
    }

    [Fact]
    public void ParseStatusLine_WithUnknownStatus_ReturnsError()
    {
        // Arrange
        var statusLine = "UnknownStatus (2024-01-15)";
        var config = new AdrPlusRepoConfig("docs/adr", "Template");

        // Act
        var (status, date, error) = Helper.ParseStatusLine(statusLine, config);

        // Assert
        status.Should().Be(AdrStatus.Unknown);
        date.Should().BeNull();
        error.Should().NotBeEmpty();
        error.Should().Contain("UnknownStatus");
    }

    [Fact]
    public void ParseStatusLine_WithInvalidDateFormat_ReturnsError()
    {
        // Arrange
        var statusLine = "Accepted (01/15/2024)"; // wrong format
        var config = new AdrPlusRepoConfig("docs/adr", "Template");

        // Act
        var (status, date, error) = Helper.ParseStatusLine(statusLine, config);

        // Assert
        status.Should().Be(AdrStatus.Unknown);
        date.Should().BeNull();
        error.Should().NotBeEmpty();
        error.Should().Contain("01/15/2024");
    }

    [Fact]
    public void ParseStatusLine_WithInvalidDateValue_ReturnsError()
    {
        // Arrange
        var statusLine = "Accepted (2024-13-45)"; // invalid month and day
        var config = new AdrPlusRepoConfig("docs/adr", "Template");

        // Act
        var (status, date, error) = Helper.ParseStatusLine(statusLine, config);

        // Assert
        status.Should().Be(AdrStatus.Unknown);
        date.Should().BeNull();
        error.Should().NotBeEmpty();
    }

    [Fact]
    public void ParseStatusLine_WithWhitespaceAroundStatus_ReturnsCorrectParsing()
    {
        // Arrange
        var statusLine = "  Accepted  (2024-01-15)"; // extra whitespace
        var config = new AdrPlusRepoConfig("docs/adr", "Template");

        // Act
        var (status, date, error) = Helper.ParseStatusLine(statusLine, config);

        // Assert
        status.Should().Be(AdrStatus.Accepted);
        date.Should().Be(new DateTime(2024, 1, 15));
        error.Should().BeEmpty();
    }

    [Fact]
    public void ParseStatusLine_WithWhitespaceInsideParentheses_ReturnsCorrectParsing()
    {
        // Arrange
        var statusLine = "Accepted (  2024-01-15  )"; // extra whitespace inside parentheses
        var config = new AdrPlusRepoConfig("docs/adr", "Template");

        // Act
        var (status, date, error) = Helper.ParseStatusLine(statusLine, config);

        // Assert
        status.Should().Be(AdrStatus.Accepted);
        date.Should().Be(new DateTime(2024, 1, 15));
        error.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Proposed")]
    [InlineData("Rejected")]
    [InlineData("Superseded")]
    public void ParseStatusLine_WithAllValidStatuses_ReturnsCorrectParsing(string statusText)
    {
        // Arrange
        var statusLine = $"{statusText} (2024-01-15)";
        var config = new AdrPlusRepoConfig("docs/adr", "Template");

        // Act
        var (status, date, error) = Helper.ParseStatusLine(statusLine, config);

        // Assert
        status.Should().NotBe(AdrStatus.Unknown);
        date.Should().Be(new DateTime(2024, 1, 15));
        error.Should().BeEmpty();
    }

    [Fact]
    public void ParseStatusLine_WithEmptyStatusString_ReturnsError()
    {
        // Arrange
        var statusLine = " (2024-01-15)"; // empty status before paren
        var config = new AdrPlusRepoConfig("docs/adr", "Template");

        // Act
        var (status, date, error) = Helper.ParseStatusLine(statusLine, config);

        // Assert
        status.Should().Be(AdrStatus.Unknown);
        date.Should().BeNull();
        error.Should().NotBeEmpty();
    }

    #endregion

    #region TryGetPropertyCaseInsensitive Tests

    [Fact]
    public void TryGetPropertyCaseInsensitive_WithExactMatch_ReturnsTrue()
    {
        // Arrange
        var json = JsonDocument.Parse(@"{""name"":""John"",""age"":30}").RootElement;

        // Act
        var result = Helper.TryGetPropertyCaseInsensitive(json, "name", out var value);

        // Assert
        result.Should().BeTrue();
        value.GetString().Should().Be("John");
    }

    [Fact]
    public void TryGetPropertyCaseInsensitive_WithDifferentCase_ReturnsTrue()
    {
        // Arrange
        var json = JsonDocument.Parse(@"{""name"":""John"",""age"":30}").RootElement;

        // Act
        var result = Helper.TryGetPropertyCaseInsensitive(json, "NAME", out var value);

        // Assert
        result.Should().BeTrue();
        value.GetString().Should().Be("John");
    }

    [Fact]
    public void TryGetPropertyCaseInsensitive_WithMixedCase_ReturnsTrue()
    {
        // Arrange
        var json = JsonDocument.Parse(@"{""FirstName"":""John"",""LastName"":""Doe""}").RootElement;

        // Act
        var result = Helper.TryGetPropertyCaseInsensitive(json, "firstname", out var value);

        // Assert
        result.Should().BeTrue();
        value.GetString().Should().Be("John");
    }

    [Fact]
    public void TryGetPropertyCaseInsensitive_WithNonexistentProperty_ReturnsFalse()
    {
        // Arrange
        var json = JsonDocument.Parse(@"{""name"":""John""}").RootElement;

        // Act
        var result = Helper.TryGetPropertyCaseInsensitive(json, "nonexistent", out _);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TryGetPropertyCaseInsensitive_WithComplexValue_ReturnsTrue()
    {
        // Arrange
        var json = JsonDocument.Parse(@"{""Address"":{""City"":""New York"",""Zip"":""10001""}}").RootElement;

        // Act
        var result = Helper.TryGetPropertyCaseInsensitive(json, "address", out var value);

        // Assert
        result.Should().BeTrue();
        value.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public void TryGetPropertyCaseInsensitive_WithArrayValue_ReturnsTrue()
    {
        // Arrange
        var json = JsonDocument.Parse(@"{""Items"":[""a"",""b"",""c""]}").RootElement;

        // Act
        var result = Helper.TryGetPropertyCaseInsensitive(json, "items", out var value);

        // Assert
        result.Should().BeTrue();
        value.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public void TryGetPropertyCaseInsensitive_WithNullValue_ReturnsTrue()
    {
        // Arrange
        var json = JsonDocument.Parse(@"{""Value"":null}").RootElement;

        // Act
        var result = Helper.TryGetPropertyCaseInsensitive(json, "value", out var value);

        // Assert
        result.Should().BeTrue();
        value.ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public void TryGetPropertyCaseInsensitive_WithNumericValue_ReturnsTrue()
    {
        // Arrange
        var json = JsonDocument.Parse(@"{""Count"":42,""Price"":19.99}").RootElement;

        // Act
        var result = Helper.TryGetPropertyCaseInsensitive(json, "price", out var value);

        // Assert
        result.Should().BeTrue();
        value.GetDouble().Should().Be(19.99);
    }

    [Fact]
    public void TryGetPropertyCaseInsensitive_WithBooleanValue_ReturnsTrue()
    {
        // Arrange
        var json = JsonDocument.Parse(@"{""Active"":true}").RootElement;

        // Act
        var result = Helper.TryGetPropertyCaseInsensitive(json, "active", out var value);

        // Assert
        result.Should().BeTrue();
        value.GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void TryGetPropertyCaseInsensitive_WithMultiplePropertiesSamePrefix_FindsExactMatch()
    {
        // Arrange
        var json = JsonDocument.Parse(@"{""Name"":""John"",""Name2"":""Jane"",""NameTest"":""Bob""}").RootElement;

        // Act
        var result = Helper.TryGetPropertyCaseInsensitive(json, "name", out var value);

        // Assert
        result.Should().BeTrue();
        value.GetString().Should().Be("John");
    }

    [Fact]
    public void TryGetPropertyCaseInsensitive_WithSpecialCharactersInValue_ReturnsTrue()
    {
        // Arrange
        var json = JsonDocument.Parse(@"{""Description"":""Hello\\nWorld\\t!""}").RootElement;

        // Act
        var result = Helper.TryGetPropertyCaseInsensitive(json, "description", out var value);

        // Assert
        result.Should().BeTrue();
        value.GetString().Should().Contain("Hello");
    }

    #endregion

    #region OpenFile Tests

    [Fact]
    public void OpenFile_WithNullFilepath_ThrowsArgumentNullException()
    {
        // Arrange
        string? filepath = null;
        var command = "echo test";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Helper.OpenFile(filepath!, command));
    }

    [Fact]
    public void OpenFile_WithNullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var filepath = "/path/to/file.txt";
        string? command = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Helper.OpenFile(filepath, command!));
    }

    [Fact]
    public void OpenFile_WithEmptyFilepath_DoesNotThrow()
    {
        // Arrange
        var filepath = string.Empty;
        var command = "echo test";

        // Act & Assert
        // Should not throw - empty string is not null, so validation passes
        var result = Helper.OpenFile(filepath, command);
        result.Should().BeOfType<string>();
    }

    [Fact]
    public void OpenFile_WithEmptyCommand_DoesNotThrow()
    {
        // Arrange
        var filepath = "/path/to/file.txt";
        var command = string.Empty;

        // Act & Assert
        // Should not throw - empty string is not null, so validation passes
        var result = Helper.OpenFile(filepath, command);
        result.Should().BeOfType<string>();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void OpenFile_WithValidParameters_ReturnsString()
    {
        // Arrange
        var filepath = "/path/to/file.txt";
        var command = "echo \"Opening file\"";

        // Act
        // This will attempt process execution but should return string (error or success)
        var result = Helper.OpenFile(filepath, command);

        // Assert
        result.Should().BeOfType<string>();
        // Result will contain either error message or empty string
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void OpenFile_WithWhitespaceFilepath_DoesNotThrow()
    {
        // Arrange
        var filepath = "   ";
        var command = "echo test";

        // Act & Assert
        // Whitespace is not validated by ArgumentNullException.ThrowIfNull
        var result = Helper.OpenFile(filepath, command);
        result.Should().BeOfType<string>();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void OpenFile_WithWhitespaceCommand_DoesNotThrow()
    {
        // Arrange
        var filepath = "/path/to/file.txt";
        var command = "   ";

        // Act & Assert
        var result = Helper.OpenFile(filepath, command);
        result.Should().BeOfType<string>();
    }

    #endregion

    #region GetResourceStatus Tests

    [Fact]
    public void GetResourceStatus_WithProposed_ReturnsNonEmptyString()
    {
        // Act
        var result = Helper.GetResourceStatus(AdrStatus.Proposed);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().BeOfType<string>();
    }

    [Fact]
    public void GetResourceStatus_WithAccepted_ReturnsNonEmptyString()
    {
        // Act
        var result = Helper.GetResourceStatus(AdrStatus.Accepted);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().BeOfType<string>();
    }

    [Fact]
    public void GetResourceStatus_WithRejected_ReturnsNonEmptyString()
    {
        // Act
        var result = Helper.GetResourceStatus(AdrStatus.Rejected);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().BeOfType<string>();
    }

    [Fact]
    public void GetResourceStatus_WithSuperseded_ReturnsNonEmptyString()
    {
        // Act
        var result = Helper.GetResourceStatus(AdrStatus.Superseded);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().BeOfType<string>();
    }

    [Fact]
    public void GetResourceStatus_WithUnknown_ReturnsNonEmptyString()
    {
        // Act
        var result = Helper.GetResourceStatus(AdrStatus.Unknown);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().BeOfType<string>();
    }

    #endregion
}
