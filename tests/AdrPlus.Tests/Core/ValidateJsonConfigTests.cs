// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Infrastructure.FileSystem;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace AdrPlus.Tests.Core;

public class ValidateJsonConfigTests
{
    private readonly IFileSystemService _fileSystem;

    public ValidateJsonConfigTests()
    {
        _fileSystem = Substitute.For<IFileSystemService>();
    }

    private ValidateJsonConfig CreateValidator(Dictionary<string, string?> configValues)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues!)
            .Build();

        return new ValidateJsonConfig(_fileSystem, configuration);
    }

    private static string CreateValidRepoJson()
    {
        return JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { AppConstants.FieldFolderAdr, "doc/adr" },
            { AppConstants.FieldTemplate, "# ADR {0}" },
            { AppConstants.FieldPrefix, "ADR" },
            { AppConstants.FieldLenSeq, 4 },
            { AppConstants.FieldLenVersion, 2 },
            { AppConstants.FieldLenRevision, 0 },
            { AppConstants.FieldLenScope, 0 },
            { AppConstants.FieldScopes, "" },
            { AppConstants.FieldFolderByScope, false },
            { AppConstants.FieldSkipDomain, "" },
            { AppConstants.FieldSeparator, "-" },
            { AppConstants.FieldCaseTransform, "CamelCase" },
            { AppConstants.FieldStatusNew, "New" },
            { AppConstants.FieldStatusAccepted, "Accepted" },
            { AppConstants.FieldStatusRejected, "Rejected" },
            { AppConstants.FieldStatusSuperseded, "Superseded" },
            { AppConstants.FieldHeaderDisclaimer, "# Disclaimer" },
            { AppConstants.FieldHeaderTitleFile, "# Title" },
            { AppConstants.FieldHeaderVersion, "## Version" },
            { AppConstants.FieldHeaderRevision, "## Revision" },
            { AppConstants.FieldHeaderScope, "## Scope" },
            { AppConstants.FieldHeaderDomain, "## Domain" },
            { AppConstants.FieldHeaderStatusCreated, "## Status (Created)" },
            { AppConstants.FieldHeaderStatusChanged, "## Status (Changed)" },
            { AppConstants.FieldHeaderStatusSuperseded, "## Status (Superseded)" },
            { AppConstants.FieldHeaderTableFields, "## Fields" },
            { AppConstants.FieldHeaderTableValues, "## Values" },
            { AppConstants.FieldHeaderMigrated, "## Migrated" }
        }, AppConstants.RepoSerializerOptions);
    }

    private static string CreateValidAppJson()
    {
        return @"{
            ""DefaultSettings"": {
                ""language"": ""en-US"",
                ""comandopenadr"": ""code {0}"",
                ""yesvalue"": ""y"",
                ""novalue"": ""n""
            }
        }";
    }

    private static Dictionary<string, object> GetBaseRepoJsonDict()
    {
        return new Dictionary<string, object>
        {
            { AppConstants.FieldFolderAdr, "doc/adr" },
            { AppConstants.FieldTemplate, "# ADR {0}" },
            { AppConstants.FieldPrefix, "ADR" },
            { AppConstants.FieldLenSeq, 4 },
            { AppConstants.FieldLenVersion, 2 },
            { AppConstants.FieldLenRevision, 0 },
            { AppConstants.FieldLenScope, 0 },
            { AppConstants.FieldScopes, "" },
            { AppConstants.FieldFolderByScope, false },
            { AppConstants.FieldSkipDomain, "" },
            { AppConstants.FieldSeparator, "-" },
            { AppConstants.FieldCaseTransform, "CamelCase" },
            { AppConstants.FieldStatusNew, "New" },
            { AppConstants.FieldStatusAccepted, "Accepted" },
            { AppConstants.FieldStatusRejected, "Rejected" },
            { AppConstants.FieldStatusSuperseded, "Superseded" },
            { AppConstants.FieldHeaderDisclaimer, "# Disclaimer" },
            { AppConstants.FieldHeaderTitleFile, "# Title" },
            { AppConstants.FieldHeaderVersion, "## Version" },
            { AppConstants.FieldHeaderRevision, "## Revision" },
            { AppConstants.FieldHeaderScope, "## Scope" },
            { AppConstants.FieldHeaderDomain, "## Domain" },
            { AppConstants.FieldHeaderStatusCreated, "## Status (Created)" },
            { AppConstants.FieldHeaderStatusChanged, "## Status (Changed)" },
            { AppConstants.FieldHeaderStatusSuperseded, "## Status (Superseded)" },
            { AppConstants.FieldHeaderTableFields, "## Fields" },
            { AppConstants.FieldHeaderTableValues, "## Values" },
            { AppConstants.FieldHeaderMigrated, "## Migrated" }
        };
    }

    #region ValidateAsync Tests

    [Fact]
    public async Task ValidateAsync_WhenDefaultSettingsMissing_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);

        // Act
        var (IsValid, ErrorReport) = await validator.ValidateAsync(CancellationToken.None);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidLanguage_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator(new Dictionary<string, string?>
        {
            { $"{AppConstants.DefaultSettingsRoot}:{AppConstants.FieldLanguage}", "invalid-lang" },
        });

        // Act
        var (IsValid, ErrorReport) = await validator.ValidateAsync(CancellationToken.None);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("invalid-lang"));
    }

    [Fact]
    public async Task ValidateAsync_WithValidConfiguration_ReturnsValid()
    {
        // Arrange
        var validator = CreateValidator(new Dictionary<string, string?>
        {
            { $"{AppConstants.DefaultSettingsRoot}:{AppConstants.FieldLanguage}", "en-US" },
        });

        var templatePath = Path.Combine(AppContext.BaseDirectory, AppConstants.TemplateDirectoryName, AppConstants.AdrTemplateFileName);
        _fileSystem.FileExists(templatePath).Returns(true);

        // Act
        var (IsValid, ErrorReport) = await validator.ValidateAsync(CancellationToken.None);

        // Assert
        IsValid.Should().BeTrue();
        ErrorReport.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyLanguage_IsNotValid()
    {
        // Arrange
        var validator = CreateValidator(new Dictionary<string, string?>
        {
            { $"{AppConstants.DefaultSettingsRoot}:{AppConstants.FieldLanguage}", "" },
        });

        var templatePath = Path.Combine(AppContext.BaseDirectory, AppConstants.TemplateDirectoryName, AppConstants.AdrTemplateFileName);
        _fileSystem.FileExists(templatePath).Returns(true);

        // Act
        var (IsValid, _) = await validator.ValidateAsync(CancellationToken.None);

        // Assert
        IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WithValidLanguageAndMissingTemplate_InitializesTemplate()
    {
        // Arrange
        var validator = CreateValidator(new Dictionary<string, string?>
        {
            { $"{AppConstants.DefaultSettingsRoot}:{AppConstants.FieldLanguage}", "en-US" },
        });

        var templatePath = Path.Combine(AppContext.BaseDirectory, AppConstants.TemplateDirectoryName, AppConstants.AdrTemplateFileName);
        var templateDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, AppConstants.TemplateDirectoryName));
        _fileSystem.FileExists(templatePath).Returns(false);
        _fileSystem.DirectoryExists(templateDir).Returns(true);

        // Act
        var (IsValid, _) = await validator.ValidateAsync(CancellationToken.None);

        // Assert
        IsValid.Should().BeTrue();
        await _fileSystem.Received(1).WriteAllTextAsync(templatePath, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateAsync_WithRelativePathContent_InitializesWhenFileNotFound()
    {
        // Arrange
        var validator = CreateValidator(new Dictionary<string, string?>
        {
            { $"{AppConstants.DefaultSettingsRoot}:{AppConstants.FieldLanguage}", "en-US" },
        });

        var contentPath = Path.Combine(AppContext.BaseDirectory, AppConstants.TemplateDirectoryName, AppConstants.AdrTemplateFileName);
        var templateDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, AppConstants.TemplateDirectoryName));

        _fileSystem.FileExists(contentPath).Returns(false);
        _fileSystem.DirectoryExists(templateDir).Returns(true);

        // Act
        var (IsValid, _) = await validator.ValidateAsync(CancellationToken.None);

        // Assert
        IsValid.Should().BeTrue();
        await _fileSystem.Received(1).WriteAllTextAsync(contentPath, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateAsync_WithAbsolutePathContent_InitializesWhenFileNotFound()
    {
        // Arrange
        var validator = CreateValidator(new Dictionary<string, string?>
        {
            { $"{AppConstants.DefaultSettingsRoot}:{AppConstants.FieldLanguage}", "en-US" },
        });

        var templateDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "template"));

        _fileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _fileSystem.DirectoryExists(templateDir).Returns(true);

        // Act
        var (IsValid, _) = await validator.ValidateAsync(CancellationToken.None);

        // Assert
        IsValid.Should().BeTrue();
    }

    #endregion

    #region ValidateRepoStructure Tests

    [Fact]
    public void ValidateRepoStructure_WithValidJson_ReturnsValid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = CreateValidRepoJson();

        // Act
        var (IsValid, ErrorReport) = validator.ValidateRepoStructure(json);

        // Assert
        if (!IsValid)
        {
            foreach (var error in ErrorReport)
            {
                Console.WriteLine($"Validation Error: {error}");
            }
        }
        IsValid.Should().BeTrue();
        ErrorReport.Should().BeEmpty();
    }

    [Fact]
    public void ValidateRepoStructure_WithMissingRequiredField_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var dict = GetBaseRepoJsonDict();
        dict.Remove(AppConstants.FieldFolderAdr);
        var json = JsonSerializer.Serialize(dict, AppConstants.RepoSerializerOptions);

        // Act
        var (IsValid, ErrorReport) = validator.ValidateRepoStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().NotBeEmpty();
    }

    [Fact]
    public void ValidateRepoStructure_WithWrongFieldType_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var dict = GetBaseRepoJsonDict();
        dict[AppConstants.FieldLenSeq] = "not-a-number";
        var json = JsonSerializer.Serialize(dict, AppConstants.RepoSerializerOptions);

        // Act
        var (IsValid, _) = validator.ValidateRepoStructure(json);

        // Assert
        IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateRepoStructure_WithExtraFields_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var dict = GetBaseRepoJsonDict();
        dict["extraField"] = "should not be here";
        var json = JsonSerializer.Serialize(dict, AppConstants.RepoSerializerOptions);

        // Act
        var (IsValid, ErrorReport) = validator.ValidateRepoStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("extraField"));
    }

    [Fact]
    public void ValidateRepoStructure_WithInvalidLenSeq_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var dict = GetBaseRepoJsonDict();
        dict[AppConstants.FieldLenSeq] = 2;
        var json = JsonSerializer.Serialize(dict, AppConstants.RepoSerializerOptions);

        // Act
        var (IsValid, ErrorReport) = validator.ValidateRepoStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("lenseq"));
    }

    [Fact]
    public void ValidateRepoStructure_WithInvalidSeparator_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var dict = GetBaseRepoJsonDict();
        dict[AppConstants.FieldSeparator] = "_";
        var json = JsonSerializer.Serialize(dict, AppConstants.RepoSerializerOptions);

        // Act
        var (IsValid, ErrorReport) = validator.ValidateRepoStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("separator"));
    }

    [Fact]
    public void ValidateRepoStructure_WhenLenScopeZeroButScopesNotEmpty_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var dict = GetBaseRepoJsonDict();
        dict[AppConstants.FieldScopes] = "scope1;scope2";
        var json = JsonSerializer.Serialize(dict, AppConstants.RepoSerializerOptions);

        // Act
        var (IsValid, _) = validator.ValidateRepoStructure(json);

        // Assert
        IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateRepoStructure_WhenLenScopePositiveButScopesEmpty_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var dict = GetBaseRepoJsonDict();
        dict[AppConstants.FieldLenScope] = 3;
        dict[AppConstants.FieldScopes] = "";
        var json = JsonSerializer.Serialize(dict, AppConstants.RepoSerializerOptions);

        // Act
        var (IsValid, _) = validator.ValidateRepoStructure(json);

        // Assert
        IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateRepoStructure_WithInvalidskipdomainScopes_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var dict = GetBaseRepoJsonDict();
        dict[AppConstants.FieldLenScope] = 3;
        dict[AppConstants.FieldScopes] = "scope1;scope2";
        dict[AppConstants.FieldSkipDomain] = "scope1;invalidscope";
        var json = JsonSerializer.Serialize(dict, AppConstants.RepoSerializerOptions);

        // Act
        var (IsValid, ErrorReport) = validator.ValidateRepoStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("invalidscope"));
    }

    [Fact]
    public void ValidateRepoStructure_WhenFolderByScopeTrueButScopesEmpty_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var dict = GetBaseRepoJsonDict();
        dict[AppConstants.FieldFolderByScope] = true;
        var json = JsonSerializer.Serialize(dict, AppConstants.RepoSerializerOptions);

        // Act
        var (IsValid, _) = validator.ValidateRepoStructure(json);

        // Assert
        IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateRepoStructure_WithEmptyStatusFields_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var dict = GetBaseRepoJsonDict();
        dict[AppConstants.FieldStatusNew] = "";
        var json = JsonSerializer.Serialize(dict, AppConstants.RepoSerializerOptions);

        // Act
        var (IsValid, ErrorReport) = validator.ValidateRepoStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("statusnew"));
    }

    [Fact]
    public void ValidateRepoStructure_WithInvalidJson_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = "{ invalid json }";

        // Act
        var (IsValid, ErrorReport) = validator.ValidateRepoStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("JSON"));
    }

    [Fact]
    public void ValidateRepoStructure_WithInvalidCaseTransform_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var dict = GetBaseRepoJsonDict();
        dict[AppConstants.FieldCaseTransform] = "InvalidCase";
        var json = JsonSerializer.Serialize(dict, AppConstants.RepoSerializerOptions);

        // Act
        var (IsValid, ErrorReport) = validator.ValidateRepoStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("casetransform"));
    }

    [Fact]
    public void ValidateRepoStructure_WithLenVersionLessThan2_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var dict = GetBaseRepoJsonDict();
        dict[AppConstants.FieldLenVersion] = 1;
        var json = JsonSerializer.Serialize(dict, AppConstants.RepoSerializerOptions);

        // Act
        var (IsValid, ErrorReport) = validator.ValidateRepoStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("lenversion"));
    }

    [Fact]
    public void ValidateRepoStructure_WithNegativeLenRevision_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var dict = GetBaseRepoJsonDict();
        dict[AppConstants.FieldLenRevision] = -1;
        var json = JsonSerializer.Serialize(dict, AppConstants.RepoSerializerOptions);

        // Act
        var (IsValid, ErrorReport) = validator.ValidateRepoStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("lenrevision"));
    }

    [Fact]
    public void ValidateRepoStructure_WithScopeLengthLessThanLenScope_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var dict = GetBaseRepoJsonDict();
        dict[AppConstants.FieldLenScope] = 5;
        dict[AppConstants.FieldScopes] = "ab;cd";
        var json = JsonSerializer.Serialize(dict, AppConstants.RepoSerializerOptions);

        // Act
        var (IsValid, ErrorReport) = validator.ValidateRepoStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("lenscope"));
    }

    [Fact]
    public void ValidateRepoStructure_WithAllValidSeparators_ReturnsValid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var validSeparators = new[] { "-", "~", "." };

        foreach (var separator in validSeparators)
        {
            var dict = GetBaseRepoJsonDict();
            dict[AppConstants.FieldSeparator] = separator;
            var json = JsonSerializer.Serialize(dict, AppConstants.RepoSerializerOptions);

            // Act
            var (IsValid, _) = validator.ValidateRepoStructure(json);

            // Assert
            IsValid.Should().BeTrue($"separator '{separator}' should be valid");
        }
    }

    [Fact]
    public void ValidateRepoStructure_WithAllValidCaseFormats_ReturnsValid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var validFormats = new[] { "CamelCase", "PascalCase", "SnakeCase", "KebabCase" };

        foreach (var format in validFormats)
        {
            var dict = GetBaseRepoJsonDict();
            dict[AppConstants.FieldCaseTransform] = format;
            var json = JsonSerializer.Serialize(dict, AppConstants.RepoSerializerOptions);

            // Act
            var (IsValid, _) = validator.ValidateRepoStructure(json);

            // Assert
            IsValid.Should().BeTrue($"case format '{format}' should be valid");
        }
    }

    [Fact]
    public void ValidateRepoStructure_WithEmptyHeaderFields_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var dict = GetBaseRepoJsonDict();
        dict[AppConstants.FieldHeaderDisclaimer] = "";
        var json = JsonSerializer.Serialize(dict, AppConstants.RepoSerializerOptions);

        // Act
        var (IsValid, ErrorReport) = validator.ValidateRepoStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("headerdisclaimer"));
    }

    [Fact]
    public void ValidateRepoStructure_WithNonBooleanFolderByScope_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var dict = GetBaseRepoJsonDict();
        dict[AppConstants.FieldFolderByScope] = "not-a-boolean";
        var json = JsonSerializer.Serialize(dict, AppConstants.RepoSerializerOptions);

        // Act
        var (IsValid, ErrorReport) = validator.ValidateRepoStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("folderbyscope") && e.Contains("boolean"));
    }

    [Fact]
    public void ValidateRepoStructure_WithMixedCaseKeys_NormalizesAndValidates()
    {
        // Arrange
        var validator = CreateValidator([]);
        var dict = GetBaseRepoJsonDict();
        var json = JsonSerializer.Serialize(dict, AppConstants.RepoSerializerOptions);

        // Manually create JSON with mixed case keys
        var mixedCaseJson = json.Replace("\"folderadr\"", "\"FolderAdr\"")
                                .Replace("\"template\"", "\"Template\"")
                                .Replace("\"prefix\"", "\"Prefix\"");

        // Act
        var (IsValid, ErrorReport) = validator.ValidateRepoStructure(mixedCaseJson);

        // Assert
        IsValid.Should().BeTrue();
        ErrorReport.Should().BeEmpty();
    }

    [Fact]
    public void ValidateRepoStructure_WithAllLowercaseKeys_ValidatesCorrectly()
    {
        // Arrange
        var validator = CreateValidator([]);
        var dict = GetBaseRepoJsonDict();
        var json = JsonSerializer.Serialize(dict, AppConstants.RepoSerializerOptions);

        // Act
        var (IsValid, ErrorReport) = validator.ValidateRepoStructure(json);

        // Assert
        IsValid.Should().BeTrue();
        ErrorReport.Should().BeEmpty();
    }

    #endregion

    #region ValidateAppStructure Tests

    [Fact]
    public void ValidateAppStructure_WithValidJson_ReturnsValid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = CreateValidAppJson();

        // Act
        var (IsValid, ErrorReport) = validator.ValidateAppStructure(json);

        // Assert
        IsValid.Should().BeTrue();
        ErrorReport.Should().BeEmpty();
    }

    [Fact]
    public void ValidateAppStructure_WithMissingRequiredField_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = @"{
            ""DefaultSettings"": {
                ""language"": ""en-US""
            }
        }";

        // Act
        var (IsValid, ErrorReport) = validator.ValidateAppStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().NotBeEmpty();
    }

    [Fact]
    public void ValidateAppStructure_WithInvalidLanguage_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = @"{
            ""DefaultSettings"": {
                ""language"": ""invalid-culture"",
                ""comandopenadr"": ""code {0}"",
                ""yesvalue"": ""y"",
                ""novalue"": ""n""
            }
        }";

        // Act
        var (IsValid, ErrorReport) = validator.ValidateAppStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("invalid-culture"));
    }

    [Fact]
    public void ValidateAppStructure_WithEmptyLanguage_ReturnsValid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = @"{
            ""DefaultSettings"": {
                ""language"": """",
                ""comandopenadr"": ""code {0}"",
                ""yesvalue"": ""y"",
                ""novalue"": ""n""
            }
        }";

        // Act
        var (IsValid, _) = validator.ValidateAppStructure(json);

        // Assert
        IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateAppStructure_WithOpenAdrMissingPlaceholder_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = @"{
            ""DefaultSettings"": {
                ""language"": ""en-US"",
                ""comandopenadr"": ""code"",
                ""yesvalue"": ""y"",
                ""novalue"": ""n""
            }
        }";

        // Act
        var (IsValid, ErrorReport) = validator.ValidateAppStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("{0}"));
    }

    [Fact]
    public void ValidateAppStructure_WithEmptyOpenAdr_IsValid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = @"{
            ""DefaultSettings"": {
                ""language"": ""en-US"",
                ""comandopenadr"": """",
                ""yesvalue"": ""y"",
                ""novalue"": ""n""
            }
        }";

        // Act
        var (IsValid, _) = validator.ValidateAppStructure(json);

        // Assert - Empty comandopenadr is valid
        IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateAppStructure_WithYesValueTooLong_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = @"{
            ""DefaultSettings"": {
                ""language"": ""en-US"",
                ""comandopenadr"": ""code {0}"",
                ""yesvalue"": ""yes"",
                ""novalue"": ""n""
            }
        }";

        // Act
        var (IsValid, ErrorReport) = validator.ValidateAppStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("yesvalue"));
    }

    [Fact]
    public void ValidateAppStructure_WithNoValueTooLong_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = @"{
            ""DefaultSettings"": {
                ""language"": ""en-US"",
                ""comandopenadr"": ""code {0}"",
                ""yesvalue"": ""y"",
                ""novalue"": ""no""
            }
        }";

        // Act
        var (IsValid, ErrorReport) = validator.ValidateAppStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("novalue"));
    }

    [Fact]
    public void ValidateAppStructure_WithExtraFields_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = @"{
            ""DefaultSettings"": {
                ""language"": ""en-US"",
                ""comandopenadr"": ""code {0}"",
                ""yesvalue"": ""y"",
                ""novalue"": ""n"",
                ""extrafield"": ""should not exist""
            }
        }";

        // Act
        var (IsValid, ErrorReport) = validator.ValidateAppStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("extrafield"));
    }

    [Fact]
    public void ValidateAppStructure_WithWrongFieldType_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = @"{
            ""DefaultSettings"": {
                ""language"": 123,
                ""comandopenadr"": ""code {0}"",
                ""yesvalue"": ""y"",
                ""novalue"": ""n""
            }
        }";

        // Act
        var (IsValid, ErrorReport) = validator.ValidateAppStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("language"));
    }

    [Fact]
    public void ValidateAppStructure_WithInvalidJson_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = @"{ ""DefaultSettings"": { invalid json }";

        // Act
        var (IsValid, ErrorReport) = validator.ValidateAppStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("JSON"));
    }

    #endregion

    #region EnsureFieldsRepoStructure Tests

    [Fact]
    public void EnsureFieldsRepoStructure_WhenLenScopeZero_ClearsScopesAndskipdomain()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "lenscope", 0 },
            { "scopes", "scope1;scope2" },
            { "skipdomain", "scope1" },
            { "folderbyscope", true },
            { "lenversion", 2 },
            { "lenrevision", 0 }
        }, AppConstants.RepoSerializerOptions);

        // Act
        var result = validator.EnsureFieldsRepoStructure(json);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, object>>(result, AppConstants.RepoSerializerOptions);

        // Assert
        resultDict!["scopes"].ToString().Should().BeEmpty();
        resultDict["skipdomain"].ToString().Should().BeEmpty();
        resultDict["folderbyscope"].ToString().Should().Be("False");
    }

    [Fact]
    public void EnsureFieldsRepoStructure_WhenLenScopePositiveButScopesEmpty_SetsDefaults()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "lenscope", 3 },
            { "scopes", "" },
            { "skipdomain", "" },
            { "folderbyscope", false },
            { "lenversion", 2 },
            { "lenrevision", 0 }
        }, AppConstants.RepoSerializerOptions);

        // Act
        var result = validator.EnsureFieldsRepoStructure(json);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, object>>(result, AppConstants.RepoSerializerOptions);

        // Assert
        resultDict!["scopes"].ToString().Should().NotBeEmpty();
        resultDict["skipdomain"].ToString().Should().NotBeEmpty();
    }

    [Fact]
    public void EnsureFieldsRepoStructure_RemovesDuplicateScopes()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "lenscope", 3 },
            { "scopes", "scope1;scope2;SCOPE1;scope2" },
            { "skipdomain", "" },
            { "folderbyscope", false },
            { "lenversion", 2 },
            { "lenrevision", 0 }
        }, AppConstants.RepoSerializerOptions);

        // Act
        var result = validator.EnsureFieldsRepoStructure(json);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, object>>(result, AppConstants.RepoSerializerOptions);

        // Assert
        var scopes = resultDict!["scopes"].ToString()!.Split(';', StringSplitOptions.RemoveEmptyEntries);
        scopes.Should().HaveCount(2);
    }

    [Fact]
    public void EnsureFieldsRepoStructure_RemovesDuplicateskipdomains()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "lenscope", 3 },
            { "scopes", "scope1;scope2" },
            { "skipdomain", "scope1;scope2;SCOPE1" },
            { "folderbyscope", false },
            { "lenversion", 2 },
            { "lenrevision", 0 }
        }, AppConstants.RepoSerializerOptions);

        // Act
        var result = validator.EnsureFieldsRepoStructure(json);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, object>>(result, AppConstants.RepoSerializerOptions);

        // Assert
        var skipdomains = resultDict!["skipdomain"].ToString()!.Split(';', StringSplitOptions.RemoveEmptyEntries);
        skipdomains.Should().HaveCount(2);
    }

    [Fact]
    public void EnsureFieldsRepoStructure_RemovesInvalidskipdomains()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "lenscope", 3 },
            { "scopes", "scope1;scope2" },
            { "skipdomain", "scope1;invalidscope" },
            { "folderbyscope", false },
            { "lenversion", 2 },
            { "lenrevision", 0 }
        }, AppConstants.RepoSerializerOptions);

        // Act
        var result = validator.EnsureFieldsRepoStructure(json);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, object>>(result, AppConstants.RepoSerializerOptions);

        // Assert
        resultDict!["skipdomain"].ToString().Should().Be("scope1");
    }

    [Fact]
    public void EnsureFieldsRepoStructure_ClampsLenVersionBetween2And3()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "lenscope", 0 },
            { "scopes", "" },
            { "skipdomain", "" },
            { "folderbyscope", false },
            { "lenversion", 5 },
            { "lenrevision", 0 }
        }, AppConstants.RepoSerializerOptions);

        // Act
        var result = validator.EnsureFieldsRepoStructure(json);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, object>>(result, AppConstants.RepoSerializerOptions);

        // Assert
        int.Parse(resultDict!["lenversion"].ToString()!).Should().Be(3);
    }

    [Fact]
    public void EnsureFieldsRepoStructure_ClampsLenRevisionBetween0And3()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "lenscope", 0 },
            { "scopes", "" },
            { "skipdomain", "" },
            { "folderbyscope", false },
            { "lenversion", 2 },
            { "lenrevision", 5 }
        }, AppConstants.RepoSerializerOptions);

        // Act
        var result = validator.EnsureFieldsRepoStructure(json);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, object>>(result, AppConstants.RepoSerializerOptions);

        // Assert
        int.Parse(resultDict!["lenrevision"].ToString()!).Should().Be(3);
    }

    [Fact]
    public void EnsureFieldsRepoStructure_AdjustsLenScopeToMinScopeLength()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "lenscope", 5 },
            { "scopes", "ab;abc" },
            { "skipdomain", "" },
            { "folderbyscope", false },
            { "lenversion", 2 },
            { "lenrevision", 0 }
        }, AppConstants.RepoSerializerOptions);

        // Act
        var result = validator.EnsureFieldsRepoStructure(json);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, object>>(result, AppConstants.RepoSerializerOptions);

        // Assert
        int.Parse(resultDict!["lenscope"].ToString()!).Should().Be(2);
    }

    [Fact]
    public void EnsureFieldsRepoStructure_WithLenVersionOutOfRange_ClampsCorrectly()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "lenscope", 0 },
            { "scopes", "" },
            { "skipdomain", "" },
            { "folderbyscope", false },
            { "lenversion", 1 },
            { "lenrevision", 0 }
        }, AppConstants.RepoSerializerOptions);

        // Act
        var result = validator.EnsureFieldsRepoStructure(json);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, object>>(result, AppConstants.RepoSerializerOptions);

        // Assert
        int.Parse(resultDict!["lenversion"].ToString()!).Should().Be(2);
    }

    [Fact]
    public void EnsureFieldsRepoStructure_WithLenRevisionOutOfRange_ClampsCorrectly()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "lenscope", 0 },
            { "scopes", "" },
            { "skipdomain", "" },
            { "folderbyscope", false },
            { "lenversion", 2 },
            { "lenrevision", -1 }
        }, AppConstants.RepoSerializerOptions);

        // Act
        var result = validator.EnsureFieldsRepoStructure(json);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, object>>(result, AppConstants.RepoSerializerOptions);

        // Assert
        int.Parse(resultDict!["lenrevision"].ToString()!).Should().Be(0);
    }

    [Fact]
    public void EnsureFieldsRepoStructure_WithScopesContainingWildcard_ExtractsSkipdomainScopes()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "lenscope", 3 },
            { "scopes", "scope1*;scope2" },
            { "skipdomain", "" },
            { "folderbyscope", false },
            { "lenversion", 2 },
            { "lenrevision", 0 }
        }, AppConstants.RepoSerializerOptions);

        // Act
        var result = validator.EnsureFieldsRepoStructure(json);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, object>>(result, AppConstants.RepoSerializerOptions);

        // Assert
        resultDict!["scopes"].ToString().Should().Contain("scope1");
        resultDict["skipdomain"].ToString().Should().Contain("scope1");
    }

    [Fact]
    public void EnsureFieldsRepoStructure_WithMultipleScopesAndWildcard_HandlesCorrectly()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "lenscope", 3 },
            { "scopes", "api*;web;db*" },
            { "skipdomain", "" },
            { "folderbyscope", false },
            { "lenversion", 2 },
            { "lenrevision", 0 }
        }, AppConstants.RepoSerializerOptions);

        // Act
        var result = validator.EnsureFieldsRepoStructure(json);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, object>>(result, AppConstants.RepoSerializerOptions);

        // Assert
        var scopes = resultDict!["scopes"].ToString()!.Split(';');
        var skipdomains = resultDict["skipdomain"].ToString()!.Split(';', StringSplitOptions.RemoveEmptyEntries);
        scopes.Should().Contain("api");
        scopes.Should().Contain("db");
        skipdomains.Should().Contain("api");
        skipdomains.Should().Contain("db");
    }

    [Fact]
    public void EnsureFieldsRepoStructure_WithLenScopeGreaterThanMinScopeLength_AdjustsCorrectly()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "lenscope", 10 },
            { "scopes", "short;tiny" },
            { "skipdomain", "" },
            { "folderbyscope", false },
            { "lenversion", 2 },
            { "lenrevision", 0 }
        }, AppConstants.RepoSerializerOptions);

        // Act
        var result = validator.EnsureFieldsRepoStructure(json);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, object>>(result, AppConstants.RepoSerializerOptions);

        // Assert
        int.Parse(resultDict!["lenscope"].ToString()!).Should().BeLessThan(10);
    }

    [Fact]
    public void EnsureFieldsRepoStructure_WithOnlyWhitespaceScopes_HandlesCorrectly()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "lenscope", 3 },
            { "scopes", "  ;  ;  " },
            { "skipdomain", "" },
            { "folderbyscope", false },
            { "lenversion", 2 },
            { "lenrevision", 0 }
        }, AppConstants.RepoSerializerOptions);

        // Act
        var result = validator.EnsureFieldsRepoStructure(json);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, object>>(result, AppConstants.RepoSerializerOptions);

        // Assert
        resultDict.Should().NotBeNull();
    }

    [Fact]
    public void EnsureFieldsRepoStructure_WhenScopesEmpty_SetsDefaultScope()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "lenscope", 1 },
            { "scopes", "" },
            { "skipdomain", "" },
            { "folderbyscope", false },
            { "lenversion", 2 },
            { "lenrevision", 0 }
        }, AppConstants.RepoSerializerOptions);

        // Act
        var result = validator.EnsureFieldsRepoStructure(json);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, object>>(result, AppConstants.RepoSerializerOptions);

        // Assert
        resultDict!["scopes"].ToString().Should().NotBeEmpty();
        resultDict["skipdomain"].ToString().Should().NotBeEmpty();
    }

    [Fact]
    public void EnsureFieldsRepoStructure_WithCaseInsensitiveKeys_WorksCorrectly()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "LENSCOPE", 3 },
            { "SCOPES", "scope1;scope2" },
            { "skipdomain", "scope1" },
            { "FOLDERBYSCOPE", false },
            { "LENVERSION", 2 },
            { "LENREVISION", 0 }
        }, AppConstants.RepoSerializerOptions);

        // Act
        var result = validator.EnsureFieldsRepoStructure(json);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, object>>(result, AppConstants.RepoSerializerOptions);

        // Assert
        resultDict.Should().NotBeNull();
    }

    #endregion

    #region File Path Tests

    [Fact]
    public void HasTemplateRepoFile_WhenFileExists_ReturnsTrue()
    {
        // Arrange
        var validator = CreateValidator([]);
        var templateDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, AppConstants.TemplateDirectoryName));
        var expectedPath = Path.Combine(templateDir, AppConstants.AdrRepoConfigFileName);
        _fileSystem.FileExists(expectedPath).Returns(true);

        // Act
        var result = validator.HasTemplateRepoFile();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasTemplateRepoFile_WhenFileDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var validator = CreateValidator([]);
        var templateDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, AppConstants.TemplateDirectoryName));
        var expectedPath = Path.Combine(templateDir, AppConstants.AdrRepoConfigFileName);
        _fileSystem.FileExists(expectedPath).Returns(false);

        // Act
        var result = validator.HasTemplateRepoFile();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetDefaultConfigRepoFilePath_ReturnsCorrectPath()
    {
        // Arrange
        var validator = CreateValidator([]);

        // Act
        var result = validator.GetDefaultConfigRepoFilePath();

        // Assert
        result.Should().Contain(AppConstants.TemplateDirectoryName);
        result.Should().Contain(AppConstants.AdrRepoConfigFileName);
        Path.IsPathRooted(result).Should().BeTrue();
    }

    [Fact]
    public void GetConfigAppFilePath_ReturnsCorrectPath()
    {
        // Arrange
        var validator = CreateValidator([]);

        // Act
        var result = validator.GetConfigAppFilePath();

        // Assert
        result.Should().Contain(AppConstants.AppConfigfileName);
        Path.IsPathRooted(result).Should().BeTrue();
    }

    [Fact]
    public void GetConfigAdrTemplatePath_ReturnsCorrectPath()
    {
        // Arrange
        var validator = CreateValidator([]);

        // Act
        var result = validator.GetConfigAdrTemplatePath();

        // Assert
        result.Should().Contain(AppConstants.TemplateDirectoryName);
        result.Should().Contain(AppConstants.AdrTemplateFileName);
        Path.IsPathRooted(result).Should().BeTrue();
    }

    [Fact]
    public void GetFileNameRepoConfig_ReturnsCorrectFileName()
    {
        // Arrange
        var validator = CreateValidator([]);

        // Act
        var result = validator.GetFileNameRepoConfig();

        // Assert
        result.Should().Be(AppConstants.AdrRepoConfigFileName);
    }

    #endregion

    #region GetConfigRepoTemplateAsync Tests

    [Fact]
    public async Task GetConfigRepoTemplateAsync_WhenFileExists_ReturnsContent()
    {
        // Arrange
        var validator = CreateValidator([]);
        var expectedContent = "template content";
        var templateDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, AppConstants.TemplateDirectoryName));
        var expectedPath = Path.Combine(templateDir, AppConstants.AdrRepoConfigFileName);
        _fileSystem.FileExists(expectedPath).Returns(true);
        _fileSystem.ReadAllTextAsync(expectedPath, Arg.Any<CancellationToken>()).Returns(expectedContent);

        // Act
        var result = await validator.GetConfigRepoTemplateAsync(CancellationToken.None);

        // Assert
        result.Should().Be(expectedContent);
    }

    [Fact]
    public async Task GetConfigRepoTemplateAsync_WhenFileDoesNotExist_ThrowsFileNotFoundException()
    {
        // Arrange
        var validator = CreateValidator([]);
        var templateDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, AppConstants.TemplateDirectoryName));
        var expectedPath = Path.Combine(templateDir, AppConstants.AdrRepoConfigFileName);
        _fileSystem.FileExists(expectedPath).Returns(false);

        // Act
        var act = async () => await validator.GetConfigRepoTemplateAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task GetConfigRepoTemplateAsync_WithCancellationToken_PassesTokenCorrectly()
    {
        // Arrange
        var validator = CreateValidator([]);
        var expectedContent = "template content";
        var templateDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, AppConstants.TemplateDirectoryName));
        var expectedPath = Path.Combine(templateDir, AppConstants.AdrRepoConfigFileName);
        var cts = new CancellationTokenSource();
        _fileSystem.FileExists(expectedPath).Returns(true);
        _fileSystem.ReadAllTextAsync(expectedPath, cts.Token).Returns(expectedContent);

        // Act
        var result = await validator.GetConfigRepoTemplateAsync(cts.Token);

        // Assert
        result.Should().Be(expectedContent);
        await _fileSystem.Received(1).ReadAllTextAsync(expectedPath, cts.Token);
    }

    #endregion

    #region GetConfigAdrTemplateAsync Tests

    [Fact]
    public async Task GetConfigAdrTemplateAsync_WhenFileExists_ReturnsContent()
    {
        // Arrange
        var validator = CreateValidator([]);
        var expectedContent = "ADR template content";
        var expectedPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, AppConstants.TemplateDirectoryName, AppConstants.AdrTemplateFileName));
        _fileSystem.FileExists(expectedPath).Returns(true);
        _fileSystem.ReadAllTextAsync(expectedPath, Arg.Any<CancellationToken>()).Returns(expectedContent);

        // Act
        var result = await validator.GetConfigAdrTemplateAsync(CancellationToken.None);

        // Assert
        result.Should().Be(expectedContent);
    }

    [Fact]
    public async Task GetConfigAdrTemplateAsync_WhenFileDoesNotExist_ThrowsFileNotFoundException()
    {
        // Arrange
        var validator = CreateValidator([]);
        var expectedPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, AppConstants.TemplateDirectoryName, AppConstants.AdrTemplateFileName));
        _fileSystem.FileExists(expectedPath).Returns(false);

        // Act
        var act = async () => await validator.GetConfigAdrTemplateAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task GetConfigAdrTemplateAsync_WithCancellationToken_PassesTokenCorrectly()
    {
        // Arrange
        var validator = CreateValidator([]);
        var expectedContent = "ADR template content";
        var expectedPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, AppConstants.TemplateDirectoryName, AppConstants.AdrTemplateFileName));
        var cts = new CancellationTokenSource();
        _fileSystem.FileExists(expectedPath).Returns(true);
        _fileSystem.ReadAllTextAsync(expectedPath, cts.Token).Returns(expectedContent);

        // Act
        var result = await validator.GetConfigAdrTemplateAsync(cts.Token);

        // Assert
        result.Should().Be(expectedContent);
        await _fileSystem.Received(1).ReadAllTextAsync(expectedPath, cts.Token);
    }

    #endregion

    #region InitializeTemplateAsync Tests

    [Fact]
    public async Task InitializeTemplateAsync_WithEnglishCulture_CreatesEnglishTemplate()
    {
        // Arrange
        var validator = CreateValidator([]);
        var templateDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, AppConstants.TemplateDirectoryName));
        var templateFile = Path.Combine(templateDir, AppConstants.AdrTemplateFileName);
        _fileSystem.DirectoryExists(templateDir).Returns(false);
        _fileSystem.FileExists(templateFile).Returns(false);

        // Act
        await validator.InitializeTemplateAsync("en-US", CancellationToken.None);

        // Assert
        _fileSystem.Received(1).CreateDirectory(templateDir);
        await _fileSystem.Received().WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeTemplateAsync_WithPortugueseCulture_CreatesPortugueseTemplate()
    {
        // Arrange
        var validator = CreateValidator([]);
        var templateDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, AppConstants.TemplateDirectoryName));
        var templateFile = Path.Combine(templateDir, AppConstants.AdrTemplateFileName);
        _fileSystem.DirectoryExists(templateDir).Returns(true);
        _fileSystem.FileExists(templateFile).Returns(false);

        // Act
        await validator.InitializeTemplateAsync("pt-BR", CancellationToken.None);

        // Assert
        _fileSystem.DidNotReceive().CreateDirectory(Arg.Any<string>());
        await _fileSystem.Received().WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeTemplateAsync_WhenTemplateExists_DoesNotOverwrite()
    {
        // Arrange
        var validator = CreateValidator([]);
        var templateDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, AppConstants.TemplateDirectoryName));
        var templateFile = Path.Combine(templateDir, AppConstants.AdrTemplateFileName);
        _fileSystem.DirectoryExists(templateDir).Returns(true);
        _fileSystem.FileExists(templateFile).Returns(true);

        // Act
        await validator.InitializeTemplateAsync("en-US", CancellationToken.None);

        // Assert
        await _fileSystem.DidNotReceive().WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeTemplateAsync_WithNullCulture_CreatesEnglishTemplate()
    {
        // Arrange
        var validator = CreateValidator([]);
        var templateDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, AppConstants.TemplateDirectoryName));
        var templateFile = Path.Combine(templateDir, AppConstants.AdrTemplateFileName);
        _fileSystem.DirectoryExists(templateDir).Returns(true);
        _fileSystem.FileExists(templateFile).Returns(false);

        // Act
        await validator.InitializeTemplateAsync(null, CancellationToken.None);

        // Assert
        await _fileSystem.Received().WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeTemplateAsync_WithEmptyString_CreatesEnglishTemplate()
    {
        // Arrange
        var validator = CreateValidator([]);
        var templateDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, AppConstants.TemplateDirectoryName));
        var templateFile = Path.Combine(templateDir, AppConstants.AdrTemplateFileName);
        _fileSystem.DirectoryExists(templateDir).Returns(true);
        _fileSystem.FileExists(templateFile).Returns(false);

        // Act
        await validator.InitializeTemplateAsync(string.Empty, CancellationToken.None);

        // Assert
        await _fileSystem.Received().WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeTemplateAsync_CreatesDirectoryWhenNotExists()
    {
        // Arrange
        var validator = CreateValidator([]);
        var templateDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, AppConstants.TemplateDirectoryName));
        var templateFile = Path.Combine(templateDir, AppConstants.AdrTemplateFileName);
        _fileSystem.DirectoryExists(templateDir).Returns(false);
        _fileSystem.FileExists(templateFile).Returns(false);

        // Act
        await validator.InitializeTemplateAsync("en-US", CancellationToken.None);

        // Assert
        _fileSystem.Received(1).CreateDirectory(templateDir);
        await _fileSystem.Received().WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeTemplateAsync_WithPortuguesePortugalCulture_CreatesPortugueseTemplate()
    {
        // Arrange
        var validator = CreateValidator([]);
        var templateDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, AppConstants.TemplateDirectoryName));
        var templateFile = Path.Combine(templateDir, AppConstants.AdrTemplateFileName);
        _fileSystem.DirectoryExists(templateDir).Returns(true);
        _fileSystem.FileExists(templateFile).Returns(false);

        // Act
        await validator.InitializeTemplateAsync("pt-PT", CancellationToken.None);

        // Assert
        await _fileSystem.Received().WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeTemplateAsync_WithInvalidCulture_CreatesEnglishTemplate()
    {
        // Arrange
        var validator = CreateValidator([]);
        var templateDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, AppConstants.TemplateDirectoryName));
        var templateFile = Path.Combine(templateDir, AppConstants.AdrTemplateFileName);
        _fileSystem.DirectoryExists(templateDir).Returns(true);
        _fileSystem.FileExists(templateFile).Returns(false);

        // Act
        await validator.InitializeTemplateAsync("invalid-culture", CancellationToken.None);

        // Assert
        await _fileSystem.Received().WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetConfigDefaultRepoContentAsync Tests

    [Fact]
    public async Task GetConfigDefaultRepoContentAsync_WhenFileExists_ReturnsFileContent()
    {
        // Arrange
        var validator = CreateValidator([]);
        var expectedContent = "existing content";
        var configPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, AppConstants.TemplateDirectoryName, AppConstants.AdrRepoConfigFileName));
        _fileSystem.FileExists(configPath).Returns(true);
        _fileSystem.ReadAllTextAsync(configPath, Arg.Any<CancellationToken>()).Returns(expectedContent);

        // Act
        var result = await validator.GetConfigDefaultRepoContentAsync("doc/adr", CancellationToken.None);

        // Assert
        result.Should().Be(expectedContent);
    }

    [Fact]
    public async Task GetConfigDefaultRepoContentAsync_WhenFileDoesNotExist_ReturnsSerializedConfig()
    {
        // Arrange
        var validator = CreateValidator([]);
        var templateContent = "# Template Content";
        var templatePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, AppConstants.TemplateDirectoryName, AppConstants.AdrTemplateFileName));
        var configPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, AppConstants.TemplateDirectoryName, AppConstants.AdrRepoConfigFileName));

        _fileSystem.FileExists(Arg.Any<string>()).Returns(callInfo =>
        {
            var path = callInfo.Arg<string>();
            if (path == templatePath) return true;
            if (path == configPath) return false;
            return false;
        });
        _fileSystem.ReadAllTextAsync(templatePath, Arg.Any<CancellationToken>()).Returns(templateContent);

        // Act
        var result = await validator.GetConfigDefaultRepoContentAsync("doc/adr", CancellationToken.None);

        // Assert
        result.Should().Contain("folderadr");
        result.Should().Contain("template");
        await _fileSystem.Received(1).WriteAllTextAsync(configPath, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetConfigDefaultRepoContentAsync_WhenTemplateThrows_PropagatesException()
    {
        // Arrange
        var validator = CreateValidator([]);
        var configPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, AppConstants.TemplateDirectoryName, AppConstants.AdrRepoConfigFileName));

        _fileSystem.FileExists(configPath).Returns(false);
        _fileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<string>(x => throw new FileNotFoundException("Template not found"));

        // Act
        var act = async () => await validator.GetConfigDefaultRepoContentAsync("doc/adr", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    #endregion

    #region Validation Rules - App Config

    [Fact]
    public void ValidateAppStructure_WithInvalidLanguageCode_ReturnsError()
    {
        // Arrange
        var validator = CreateValidator(new Dictionary<string, string?> { });
        var jsonContent = """
        {
            "DefaultSettings": {
                "Language": "invalid-lang",
                "OpenAdr": "command {0}",
                "YesValue": "y",
                "NoValue": "n"
            }
        }
        """;

        // Act
        var result = validator.ValidateAppStructure(jsonContent);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorReport.Should().Contain(e => e.Contains("Language", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateAppStructure_WithYesValueTooLong_ReturnsError()
    {
        // Arrange
        var validator = CreateValidator(new Dictionary<string, string?> { });
        var jsonContent = """
        {
            "DefaultSettings": {
                "Language": "en",
                "OpenAdr": "command {0}",
                "YesValue": "yes",
                "NoValue": "n"
            }
        }
        """;

        // Act
        var result = validator.ValidateAppStructure(jsonContent);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorReport.Should().Contain(e => e.Contains("YesValue", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateAppStructure_WithNoValueTooLong_ReturnsError()
    {
        // Arrange
        var validator = CreateValidator(new Dictionary<string, string?> { });
        var jsonContent = """
        {
            "DefaultSettings": {
                "Language": "en",
                "OpenAdr": "command {0}",
                "YesValue": "y",
                "NoValue": "no"
            }
        }
        """;

        // Act
        var result = validator.ValidateAppStructure(jsonContent);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorReport.Should().Contain(e => e.Contains("NoValue", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Validation Rules - Repo Config

    [Fact]
    public void ValidateRepoStructure_WithInvalidSeparator_ReturnsError()
    {
        // Arrange
        var validator = CreateValidator(new Dictionary<string, string?> { });
        var invalidJson = CreateValidRepoJson();
        var jsonObj = JsonSerializer.Deserialize<Dictionary<string, object>>(invalidJson);
        jsonObj![AppConstants.FieldSeparator] = "|"; // Invalid separator
        var jsonContent = JsonSerializer.Serialize(jsonObj);

        // Act
        var result = validator.ValidateRepoStructure(jsonContent);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorReport.Should().Contain(e => e.Contains("Separator", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateRepoStructure_WithInvalidCaseTransform_ReturnsError()
    {
        // Arrange
        var validator = CreateValidator(new Dictionary<string, string?> { });
        var invalidJson = CreateValidRepoJson();
        var jsonObj = JsonSerializer.Deserialize<Dictionary<string, object>>(invalidJson);
        jsonObj![AppConstants.FieldCaseTransform] = "UPPERCASE"; // Invalid case transform
        var jsonContent = JsonSerializer.Serialize(jsonObj);

        // Act
        var result = validator.ValidateRepoStructure(jsonContent);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorReport.Should().Contain(e => e.Contains("CaseTransform", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateRepoStructure_WithLenSeqTooSmall_ReturnsError()
    {
        // Arrange
        var validator = CreateValidator(new Dictionary<string, string?> { });
        var invalidJson = CreateValidRepoJson();
        var jsonObj = JsonSerializer.Deserialize<Dictionary<string, object>>(invalidJson);
        jsonObj![AppConstants.FieldLenSeq] = 2; // Too small, minimum is 3
        var jsonContent = JsonSerializer.Serialize(jsonObj);

        // Act
        var result = validator.ValidateRepoStructure(jsonContent);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorReport.Should().Contain(e => e.Contains("LenSeq", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateRepoStructure_WithScopesMissingWhenLenScopeZero_IsValid()
    {
        // Arrange - Scopes is empty when LenScope is 0, which is valid
        var validator = CreateValidator(new Dictionary<string, string?> { });
        var validJson = CreateValidRepoJson();

        // Act
        var result = validator.ValidateRepoStructure(validJson);

        // Assert - should be valid because scopes is empty when lenscope = 0
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateRepoStructure_WithScopesEmptyWhenLenScopePositive_ReturnsError()
    {
        // Arrange - Scopes is empty when LenScope > 0, which is invalid
        var validator = CreateValidator(new Dictionary<string, string?> { });
        var invalidJson = CreateValidRepoJson();
        var jsonObj = JsonSerializer.Deserialize<Dictionary<string, object>>(invalidJson);
        jsonObj![AppConstants.FieldLenScope] = 2;
        jsonObj![AppConstants.FieldScopes] = ""; // Must not be empty when LenScope > 0
        var jsonContent = JsonSerializer.Serialize(jsonObj);

        // Act
        var result = validator.ValidateRepoStructure(jsonContent);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorReport.Should().Contain(e => e.Contains("Scopes", StringComparison.OrdinalIgnoreCase));
    }

    #endregion
}
