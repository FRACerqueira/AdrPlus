// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Tests.Helpers;
using System.Text.Json;

namespace AdrPlus.Tests.Commands;

public class AdrConfigMapperServiceTests
{
    private readonly AdrConfigMapperService _mapper = new();
    private readonly string _template = "# ADR {0}";
    private readonly string _defaultFolder = PathHelper.GetRepositoryAdrPath();

    #region Valid JSON Tests

    [Fact]
    public void FromJson_WithMinimalValidJson_ReturnsConfigWithDefaults()
    {
        // Arrange
        var json = CreateMinimalJson();

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert
        config.Should().NotBeNull();
        config.Template.Should().Be(_template);
        config.FolderRepo.Should().Be(_defaultFolder);
        config.Prefix.Should().Be(Resources.AdrPlus.DefaultPrefix);
        config.LenSeq.Should().Be(4);
        config.LenVersion.Should().Be(2);
        config.LenRevision.Should().Be(0);
        config.LenScope.Should().Be(0);
        config.Separator.Should().Be('-');
        config.CaseTransform.Should().Be(CaseFormat.PascalCase);
    }

    [Fact]
    public void FromJson_WithAllValidFields_ReturnsConfigWithCustomValues()
    {
        // Arrange
        var json = CreateFullJson(
            prefix: "DECISION",
            lenSeq: 5,
            lenVersion: 3,
            lenRevision: 2,
            lenScope: 1,
            separator: '_',
            caseTransform: "KebabCase",
            statusNew: "Proposed",
            statusAccepted: "Accepted",
            statusRejected: "Rejected",
            statusSuperseded: "Superseded",
            scopes: "Enterprise;Team;Project",
            folderByScope: true,
            skipDomain: "Team",
            headerDisclaimer: "# Confidential",
            headerStatus: "## Current Status",
            headerVersion: "## Version Info",
            headerRevision: "## Revision Info"
        );

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert
        config.Prefix.Should().Be("DECISION");
        config.LenSeq.Should().Be(5);
        config.LenVersion.Should().Be(3);
        config.LenRevision.Should().Be(2);
        config.LenScope.Should().Be(1);
        config.Separator.Should().Be('_');
        config.CaseTransform.Should().Be(CaseFormat.KebabCase);
        config.StatusNew.Should().Be("Proposed");
        config.StatusAcc.Should().Be("Accepted");
        config.StatusRej.Should().Be("Rejected");
        config.StatusSup.Should().Be("Superseded");
        config.Scopes.Should().Be("Enterprise;Team;Project");
        config.FolderByScope.Should().BeTrue();
        config.SkipDomain.Should().Be("Team");
        config.HeaderDisclaimer.Should().Be("# Confidential");
        config.HeaderStatus.Should().Be("## Current Status");
        config.HeaderVersion.Should().Be("## Version Info");
        config.HeaderRevision.Should().Be("## Revision Info");
    }

    [Theory]
    [InlineData("CamelCase", 0)] // CamelCase = 0
    [InlineData("camelcase", 0)]
    [InlineData("PascalCase", 1)] // PascalCase = 1
    [InlineData("PASCALCASE", 1)]
    [InlineData("SnakeCase", 2)] // SnakeCase = 2
    [InlineData("KebabCase", 3)] // KebabCase = 3
    public void FromJson_WithVariousCaseFormats_ParsesCaseTransformCorrectly(string caseFormatValue, int expectedFormatInt)
    {
        // Arrange
        var json = CreateJsonWithCaseTransform(caseFormatValue);

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert
        var expectedFormat = (CaseFormat)expectedFormatInt;
        config.CaseTransform.Should().Be(expectedFormat);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(100)]
    public void FromJson_WithValidLenSeq_ParsesCorrectly(int lenSeqValue)
    {
        // Arrange
        var json = CreateJsonWithField(AppConstants.FieldLenSeq, lenSeqValue);

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert
        config.LenSeq.Should().Be(lenSeqValue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(5)]
    public void FromJson_WithValidLenVersion_ParsesCorrectly(int lenVersionValue)
    {
        // Arrange
        var json = CreateJsonWithField(AppConstants.FieldLenVersion, lenVersionValue);

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert
        config.LenVersion.Should().Be(lenVersionValue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void FromJson_WithValidLenRevision_ParsesCorrectly(int lenRevisionValue)
    {
        // Arrange
        var json = CreateJsonWithField(AppConstants.FieldLenRevision, lenRevisionValue);

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert
        config.LenRevision.Should().Be(lenRevisionValue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void FromJson_WithValidLenScope_ParsesCorrectly(int lenScopeValue)
    {
        // Arrange
        var json = CreateJsonWithField(AppConstants.FieldLenScope, lenScopeValue);

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert
        config.LenScope.Should().Be(lenScopeValue);
    }

    [Theory]
    [InlineData("-")]
    [InlineData("_")]
    [InlineData("/")]
    [InlineData(".")]
    public void FromJson_WithVariousSeparators_ParsesCorrectly(string separatorValue)
    {
        // Arrange
        var json = CreateJsonWithStringField(AppConstants.FieldSeparator, separatorValue);

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert
        config.Separator.Should().Be(separatorValue[0]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FromJson_WithBooleanFolderByScope_ParsesCorrectly(bool folderByScopeValue)
    {
        // Arrange
        var json = CreateJsonWithField(AppConstants.FieldFolderByScope, folderByScopeValue);

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert
        config.FolderByScope.Should().Be(folderByScopeValue);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    public void FromJson_WithStringBooleanFolderByScope_ParsesCorrectly(string folderByScopeValue)
    {
        // Arrange
        var json = CreateJsonWithStringField(AppConstants.FieldFolderByScope, folderByScopeValue);

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert
        var expectedValue = bool.Parse(folderByScopeValue);
        config.FolderByScope.Should().Be(expectedValue);
    }

    [Fact]
    public void FromJson_WithCaseInsensitiveFieldNames_ParsesCorrectly()
    {
        // Arrange
        var json = CreateCaseInsensitiveJson();

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert
        config.Prefix.Should().Be("ADR-PLUS");
        config.StatusNew.Should().Be("Proposed");
    }

    #endregion

    #region Edge Cases - Invalid Values

    [Fact]
    public void FromJson_WithInvalidCaseTransform_IgnoresAndKeepsDefault()
    {
        // Arrange
        var json = CreateJsonWithStringField(AppConstants.FieldCaseTransform, "InvalidCaseFormat");

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert
        config.CaseTransform.Should().Be(CaseFormat.PascalCase);
    }

    [Fact]
    public void FromJson_WithLenSeqZeroOrNegative_IgnoresAndKeepsDefault()
    {
        // Arrange - Test with 0
        var jsonZero = CreateJsonWithField(AppConstants.FieldLenSeq, 0);
        var configZero = _mapper.FromJson(jsonZero, _template, _defaultFolder);
        
        configZero.LenSeq.Should().Be(4); // Default value

        // Arrange - Test with negative
        var jsonNegative = CreateJsonWithField(AppConstants.FieldLenSeq, -1);
        
        // Act
        var configNegative = _mapper.FromJson(jsonNegative, _template, _defaultFolder);

        // Assert
        configNegative.LenSeq.Should().Be(4); // Default value
    }

    [Fact]
    public void FromJson_WithLenVersionNegative_IgnoresAndKeepsDefault()
    {
        // Arrange
        var json = CreateJsonWithField(AppConstants.FieldLenVersion, -1);

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert
        config.LenVersion.Should().Be(2); // Default value
    }

    [Fact]
    public void FromJson_WithLenRevisionNegative_IgnoresAndKeepsDefault()
    {
        // Arrange
        var json = CreateJsonWithField(AppConstants.FieldLenRevision, -1);

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert
        config.LenRevision.Should().Be(0); // Default value
    }

    [Fact]
    public void FromJson_WithLenScopeNegative_IgnoresAndKeepsDefault()
    {
        // Arrange
        var json = CreateJsonWithField(AppConstants.FieldLenScope, -1);

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert
        config.LenScope.Should().Be(0); // Default value
    }

    [Fact]
    public void FromJson_WithEmptySeparator_IgnoresAndKeepsDefault()
    {
        // Arrange
        var json = CreateJsonWithStringField(AppConstants.FieldSeparator, "");

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert
        config.Separator.Should().Be('-'); // Default value
    }

    [Fact]
    public void FromJson_WithMultiCharacterSeparator_IgnoresAndKeepsDefault()
    {
        // Arrange
        var json = CreateJsonWithStringField(AppConstants.FieldSeparator, "--");

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert
        config.Separator.Should().Be('-'); // Default value
    }

    [Fact]
    public void FromJson_WithWhitespaceSeparator_IgnoresAndKeepsDefault()
    {
        // Arrange
        var json = CreateJsonWithStringField(AppConstants.FieldSeparator, "   ");

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert
        config.Separator.Should().Be('-'); // Default value
    }

    [Fact]
    public void FromJson_WithInvalidBooleanFolderByScope_IgnoresAndKeepsDefault()
    {
        // Arrange
        var json = CreateJsonWithStringField(AppConstants.FieldFolderByScope, "maybe");

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert - Should keep default (false)
        config.FolderByScope.Should().BeFalse();
    }

    [Fact]
    public void FromJson_WithWrongJsonTypes_IgnoresInvalidFields()
    {
        // Arrange
        var jsonObject = JsonSerializer.Serialize(new
        {
            prefix = 123, // Should be string, not number
            lenseq = "four", // Should be number, not string
            casetransform = 789 // Should be string, not number
        });

        // Act
        var config = _mapper.FromJson(jsonObject, _template, _defaultFolder);

        // Assert
        config.Prefix.Should().Be(Resources.AdrPlus.DefaultPrefix); // Default
        config.LenSeq.Should().Be(4); // Default
        config.CaseTransform.Should().Be(CaseFormat.PascalCase); // Default
    }

    #endregion

    #region Null/Empty Input Tests

    [Fact]
    public void FromJson_WithNullJsonString_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => _mapper.FromJson(null!, _template, _defaultFolder);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("jsonString");
    }

    [Fact]
    public void FromJson_WithEmptyJsonString_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => _mapper.FromJson(string.Empty, _template, _defaultFolder);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("jsonString");
    }

    [Fact]
    public void FromJson_WithWhitespaceJsonString_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => _mapper.FromJson("   ", _template, _defaultFolder);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("jsonString");
    }

    #endregion

    #region Cross-Platform Path Tests

    [Fact]
    public void FromJson_WithWindowsPath_CreatesConfigSuccessfully()
    {
        // Arrange
        var windowsPath = @"C:\repo\docs\adr";
        var json = CreateMinimalJson();

        // Act
        var config = _mapper.FromJson(json, _template, windowsPath);

        // Assert
        config.FolderRepo.Should().Be(windowsPath);
    }

    [Fact]
    public void FromJson_WithUnixPath_CreatesConfigSuccessfully()
    {
        // Arrange
        var unixPath = "/repo/docs/adr";
        var json = CreateMinimalJson();

        // Act
        var config = _mapper.FromJson(json, _template, unixPath);

        // Assert
        config.FolderRepo.Should().Be(unixPath);
    }

    [Fact]
    public void FromJson_WithCrossplatformPathHelper_WorksOnCurrentPlatform()
    {
        // Arrange
        var currentPlatformPath = PathHelper.GetRepositoryAdrPath();
        var json = CreateMinimalJson();

        // Act
        var config = _mapper.FromJson(json, _template, currentPlatformPath);

        // Assert
        config.FolderRepo.Should().Be(currentPlatformPath);
    }

    #endregion

    #region String Field Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FromJson_WithEmptyOrWhitespaceStringField_SetsTheValueAsIs(string emptyValue)
    {
        // Arrange
        var json = JsonSerializer.Serialize(new
        {
            prefix = emptyValue
        });

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert
        // The mapper accepts and sets the value as-is from JSON, even if empty/whitespace
        config.Prefix.Should().Be(emptyValue);
    }

    [Theory]
    [InlineData("New", "New")]
    [InlineData("Proposed", "Proposed")]
    [InlineData("Pending", "Pending")]
    public void FromJson_WithStatusNewValues_ParsesCorrectly(string statusValue, string expected)
    {
        // Arrange
        var json = CreateJsonWithStringField(AppConstants.FieldStatusNew, statusValue);

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert
        config.StatusNew.Should().Be(expected);
    }

    [Theory]
    [InlineData("Accepted", "Accepted")]
    [InlineData("Approved", "Approved")]
    [InlineData("Confirmed", "Confirmed")]
    public void FromJson_WithStatusAcceptedValues_ParsesCorrectly(string statusValue, string expected)
    {
        // Arrange
        var json = CreateJsonWithStringField(AppConstants.FieldStatusAccepted, statusValue);

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert
        config.StatusAcc.Should().Be(expected);
    }

    [Theory]
    [InlineData("Rejected", "Rejected")]
    [InlineData("Declined", "Declined")]
    public void FromJson_WithStatusRejectedValues_ParsesCorrectly(string statusValue, string expected)
    {
        // Arrange
        var json = CreateJsonWithStringField(AppConstants.FieldStatusRejected, statusValue);

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert
        config.StatusRej.Should().Be(expected);
    }

    [Theory]
    [InlineData("Superseded", "Superseded")]
    [InlineData("Replaced", "Replaced")]
    public void FromJson_WithStatusSupersededValues_ParsesCorrectly(string statusValue, string expected)
    {
        // Arrange
        var json = CreateJsonWithStringField(AppConstants.FieldStatusSuperseded, statusValue);

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert
        config.StatusSup.Should().Be(expected);
    }

    [Fact]
    public void FromJson_WithHeaderValues_ParsesAllHeaderFieldsCorrectly()
    {
        // Arrange
        var json = JsonSerializer.Serialize(new
        {
            headerdisclaimer = "# Disclaimer Text",
            headerstatus = "## Status Section",
            headerversion = "## Version Section",
            headerrevision = "## Revision Section"
        });

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert
        config.HeaderDisclaimer.Should().Be("# Disclaimer Text");
        config.HeaderStatus.Should().Be("## Status Section");
        config.HeaderVersion.Should().Be("## Version Section");
        config.HeaderRevision.Should().Be("## Revision Section");
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public void FromJson_WithMixedValidAndInvalidFields_ParsesValidFieldsAndIgnoresInvalid()
    {
        // Arrange
        var json = JsonSerializer.Serialize(new
        {
            prefix = "VALID-PREFIX",
            lenseq = -5, // Invalid
            lenversion = 3, // Valid
            lenrevision = "invalid", // Invalid (wrong type)
            lenscope = 2, // Valid
            separator = "---", // Invalid (too long)
            casetransform = "InvalidCase", // Invalid value
            statusnew = "Proposed" // Valid
        });

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert
        config.Prefix.Should().Be("VALID-PREFIX");
        config.LenSeq.Should().Be(4); // Kept default
        config.LenVersion.Should().Be(3); // Parsed successfully
        config.LenRevision.Should().Be(0); // Kept default
        config.LenScope.Should().Be(2); // Parsed successfully
        config.Separator.Should().Be('-'); // Kept default
        config.CaseTransform.Should().Be(CaseFormat.PascalCase); // Kept default
        config.StatusNew.Should().Be("Proposed"); // Parsed successfully
    }

    [Fact]
    public void FromJson_WithExtraUnknownFields_IgnoresThemAndParsesKnownFields()
    {
        // Arrange
        var json = JsonSerializer.Serialize(new
        {
            prefix = "ADR",
            unknownfield1 = "some value",
            lenseq = 5,
            unknownfield2 = 12345,
            statusnew = "New"
        });

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert
        config.Prefix.Should().Be("ADR");
        config.LenSeq.Should().Be(5);
        config.StatusNew.Should().Be("New");
    }

    [Fact]
    public void FromJson_WithEmptyJsonObject_ReturnsConfigWithAllDefaults()
    {
        // Arrange
        var json = JsonSerializer.Serialize(new { });

        // Act
        var config = _mapper.FromJson(json, _template, _defaultFolder);

        // Assert
        config.Prefix.Should().Be(Resources.AdrPlus.DefaultPrefix);
        config.LenSeq.Should().Be(4);
        config.LenVersion.Should().Be(2);
        config.LenRevision.Should().Be(0);
        config.LenScope.Should().Be(0);
        config.Separator.Should().Be('-');
        config.CaseTransform.Should().Be(CaseFormat.PascalCase);
    }

    [Fact]
    public void FromJson_PreservesTemplateAndFolderParameters()
    {
        // Arrange
        var customTemplate = "## Decision Record: {0}";
        var customFolder = PathHelper.GetAlternativeFolderPath();
        var json = CreateMinimalJson();

        // Act
        var config = _mapper.FromJson(json, customTemplate, customFolder);

        // Assert
        config.Template.Should().Be(customTemplate);
        config.FolderRepo.Should().Be(customFolder);
    }

    #endregion

    #region Helper Methods

    private static string CreateMinimalJson()
    {
        return JsonSerializer.Serialize(new { });
    }

    private static string CreateFullJson(
        string? prefix = null,
        int lenSeq = 0,
        int lenVersion = 0,
        int lenRevision = 0,
        int lenScope = 0,
        char separator = '-',
        string? caseTransform = null,
        string? statusNew = null,
        string? statusAccepted = null,
        string? statusRejected = null,
        string? statusSuperseded = null,
        string? scopes = null,
        bool? folderByScope = null,
        string? skipDomain = null,
        string? headerDisclaimer = null,
        string? headerStatus = null,
        string? headerVersion = null,
        string? headerRevision = null)
    {
        var dict = new Dictionary<string, object?>();

        if (prefix is not null) dict[AppConstants.FieldPrefix] = prefix;
        if (lenSeq > 0) dict[AppConstants.FieldLenSeq] = lenSeq;
        if (lenVersion >= 0) dict[AppConstants.FieldLenVersion] = lenVersion;
        if (lenRevision >= 0) dict[AppConstants.FieldLenRevision] = lenRevision;
        if (lenScope >= 0) dict[AppConstants.FieldLenScope] = lenScope;
        dict[AppConstants.FieldSeparator] = separator.ToString();
        if (caseTransform is not null) dict[AppConstants.FieldCaseTransform] = caseTransform;
        if (statusNew is not null) dict[AppConstants.FieldStatusNew] = statusNew;
        if (statusAccepted is not null) dict[AppConstants.FieldStatusAccepted] = statusAccepted;
        if (statusRejected is not null) dict[AppConstants.FieldStatusRejected] = statusRejected;
        if (statusSuperseded is not null) dict[AppConstants.FieldStatusSuperseded] = statusSuperseded;
        if (scopes is not null) dict[AppConstants.FieldScopes] = scopes;
        if (folderByScope is not null) dict[AppConstants.FieldFolderByScope] = folderByScope;
        if (skipDomain is not null) dict[AppConstants.FieldSkipDomain] = skipDomain;
        if (headerDisclaimer is not null) dict[AppConstants.FieldHeaderDisclaimer] = headerDisclaimer;
        if (headerStatus is not null) dict[AppConstants.FieldHeaderStatus] = headerStatus;
        if (headerVersion is not null) dict[AppConstants.FieldHeaderVersion] = headerVersion;
        if (headerRevision is not null) dict[AppConstants.FieldHeaderRevision] = headerRevision;

        return JsonSerializer.Serialize(dict);
    }

    private static string CreateJsonWithField(string fieldName, int value)
    {
        var dict = new Dictionary<string, object> { { fieldName, value } };
        return JsonSerializer.Serialize(dict);
    }

    private static string CreateJsonWithField(string fieldName, bool value)
    {
        var dict = new Dictionary<string, object> { { fieldName, value } };
        return JsonSerializer.Serialize(dict);
    }

    private static string CreateJsonWithStringField(string fieldName, string value)
    {
        var dict = new Dictionary<string, string> { { fieldName, value } };
        return JsonSerializer.Serialize(dict);
    }

    private static string CreateJsonWithCaseTransform(string caseFormat)
    {
        return JsonSerializer.Serialize(new
        {
            casetransform = caseFormat
        });
    }

    private static string CreateCaseInsensitiveJson()
    {
        return JsonSerializer.Serialize(new
        {
            PREFIX = "ADR-PLUS",
            STATUSNEW = "Proposed"
        });
    }

    #endregion
}
