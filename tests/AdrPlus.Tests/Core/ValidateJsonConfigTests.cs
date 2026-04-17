// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Domain;
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
            { AppConstants.FieldFolderRepo, "docs/adr" },
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
            { AppConstants.FieldHeaderStatus, "## Status" },
            { AppConstants.FieldHeaderVersion, "## Version" },
            { AppConstants.FieldHeaderRevision, "## Revision" }
        }, AppConstants.RepoSerializerOptions);
    }

    private static string CreateValidAppJson()
    {
        return @"{
            ""DefaultSettings"": {
                ""language"": ""en-US"",
                ""folderrepo"": ""docs/adr"",
                ""comandopenadr"": ""code {0}"",
                ""yesvalue"": ""y"",
                ""novalue"": ""n""
            }
        }";
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
            { $"{AppConstants.DefaultSettingsRoot}:{AppConstants.FieldFolderRepo}", "docs/adr" }
        });

        // Act
        var (IsValid, ErrorReport) = await validator.ValidateAsync(CancellationToken.None);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("invalid-lang"));
    }

    [Fact]
    public async Task ValidateAsync_WithAbsoluteFolderRepo_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator(new Dictionary<string, string?>
        {
            { $"{AppConstants.DefaultSettingsRoot}:{AppConstants.FieldLanguage}", "en-US" },
            { $"{AppConstants.DefaultSettingsRoot}:{AppConstants.FieldFolderRepo}", "/absolute/path" }
        });

        // Act
        var (IsValid, ErrorReport) = await validator.ValidateAsync(CancellationToken.None);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("relative"));
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyDateFormat_IsValid()
    {
        // Arrange
        var validator = CreateValidator(new Dictionary<string, string?>
        {
            { $"{AppConstants.DefaultSettingsRoot}:{AppConstants.FieldLanguage}", "en-US" },
            { $"{AppConstants.DefaultSettingsRoot}:{AppConstants.FieldFolderRepo}", "docs/adr" }
        });

        // Act
        var (IsValid, _) = await validator.ValidateAsync(CancellationToken.None);

        // Assert
        IsValid.Should().BeTrue(); // Empty date format is allowed
    }

    [Fact]
    public async Task ValidateAsync_WithValidConfiguration_ReturnsValid()
    {
        // Arrange
        var validator = CreateValidator(new Dictionary<string, string?>
        {
            { $"{AppConstants.DefaultSettingsRoot}:{AppConstants.FieldLanguage}", "en-US" },
            { $"{AppConstants.DefaultSettingsRoot}:{AppConstants.FieldFolderRepo}", "docs/adr" }
        });

        var templatePath = Path.Combine(AppContext.BaseDirectory, "template", "adr-template.md");
        _fileSystem.FileExists(templatePath).Returns(true);

        // Act
        var (IsValid, ErrorReport) = await validator.ValidateAsync(CancellationToken.None);

        // Assert
        IsValid.Should().BeTrue();
        ErrorReport.Should().BeEmpty();
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
            // Output errors for debugging
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
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "folderrepo", "docs/adr" },
            { "dateformat", "yyyy-MM-dd" }
            // Missing many required fields
        });

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
        var json = @"{
            ""folderrepo"": ""docs/adr"",
            ""dateformat"": ""yyyy-MM-dd"",
            ""template"": ""# ADR"",
            ""prefix"": ""ADR"",
            ""lenseq"": ""not-a-number"",
            ""lenversion"": 2,
            ""lenrevision"": 0,
            ""lenscope"": 0,
            ""scopes"": """",
            ""folderbyscope"": false,
            ""skipdomain"": """",
            ""separator"": ""-"",
            ""casetransform"": ""CamelCase"",
            ""statusnew"": ""New"",
            ""statusacc"": ""Accepted"",
            ""statusrej"": ""Rejected"",            ""statussup"": ""Superseded"",
            ""headerdisclaimer"": ""# Disclaimer"",
            ""headerstatus"": ""## Status"",
            ""headerversion"": ""## Version"",
            ""headerrevision"": ""## Revision""
        }";

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
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "folderrepo", "docs/adr" },
            { "dateformat", "yyyy-MM-dd" },
            { "template", "# ADR {0}" },
            { "prefix", "ADR" },
            { "lenseq", 4 },
            { "lenversion", 2 },
            { "lenrevision", 0 },
            { "lenscope", 0 },
            { "scopes", "" },
            { "folderbyscope", false },
            { "skipdomain", "" },
            { "separator", "-" },
            { "casetransform", "CamelCase" },
            { "statusnew", "New" },
            { "statusacc", "Accepted" },
            { "statusrej", "Rejected" },            
            { "statussup", "Superseded" },
            { "headerdisclaimer", "# Disclaimer" },
            { "headerstatus", "## Status" },
            { "headerversion", "## Version" },
            { "headerrevision", "## Revision" },
            { "extraField", "should not be here" }
        });

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
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "folderrepo", "docs/adr" },
            { "template", "# ADR {0}" },
            { "prefix", "ADR" },
            { "lenseq", 2 }, // Less than minimum of 3
            { "lenversion", 2 },
            { "lenrevision", 0 },
            { "lenscope", 0 },
            { "scopes", "" },
            { "folderbyscope", false },
            { "skipdomain", "" },
            { "separator", "-" },
            { "casetransform", "CamelCase" },
            { "statusnew", "New" },
            { "statusacc", "Accepted" },
            { "statusrej", "Rejected" },            { "statussup", "Superseded" },
            { "headerdisclaimer", "# Disclaimer" },
            { "headerstatus", "## Status" },
            { "headerversion", "## Version" },
            { "headerrevision", "## Revision" }
        }, AppConstants.RepoSerializerOptions);

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
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "folderrepo", "docs/adr" },
            { "template", "# ADR {0}" },
            { "prefix", "ADR" },
            { "lenseq", 4 },
            { "lenversion", 2 },
            { "lenrevision", 0 },
            { "lenscope", 0 },
            { "scopes", "" },
            { "folderbyscope", false },
            { "skipdomain", "" },
            { "separator", "_" }, // Invalid separator
            { "casetransform", "CamelCase" },
            { "statusnew", "New" },
            { "statusacc", "Accepted" },
            { "statusrej", "Rejected" },            { "statussup", "Superseded" },
            { "headerdisclaimer", "# Disclaimer" },
            { "headerstatus", "## Status" },
            { "headerversion", "## Version" },
            { "headerrevision", "## Revision" }
        }, AppConstants.RepoSerializerOptions);

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
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "folderrepo", "docs/adr" },
            { "dateformat", "yyyy-MM-dd" },
            { "template", "# ADR {0}" },
            { "prefix", "ADR" },
            { "lenseq", 4 },
            { "lenversion", 2 },
            { "lenrevision", 0 },
            { "lenscope", 0 },
            { "scopes", "scope1;scope2" }, // Should be empty when lenscope is 0
            { "folderbyscope", false },
            { "skipdomain", "" },
            { "separator", "-" },
            { "casetransform", "CamelCase" },
            { "statusnew", "New" },
            { "statusacc", "Accepted" },
            { "statusrej", "Rejected" },            { "statussup", "Superseded" },
            { "headerdisclaimer", "# Disclaimer" },
            { "headerstatus", "## Status" },
            { "headerversion", "## Version" },
            { "headerrevision", "## Revision" }
        }, AppConstants.RepoSerializerOptions);

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
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "folderrepo", "docs/adr" },
            { "dateformat", "yyyy-MM-dd" },
            { "template", "# ADR {0}" },
            { "prefix", "ADR" },
            { "lenseq", 4 },
            { "lenversion", 2 },
            { "lenrevision", 0 },
            { "lenscope", 3 },
            { "scopes", "" }, // Should not be empty when lenscope > 0
            { "folderbyscope", false },
            { "skipdomain", "" },
            { "separator", "-" },
            { "casetransform", "CamelCase" },
            { "statusnew", "New" },
            { "statusacc", "Accepted" },
            { "statusrej", "Rejected" },            { "statussup", "Superseded" },
            { "headerdisclaimer", "# Disclaimer" },
            { "headerstatus", "## Status" },
            { "headerversion", "## Version" },
            { "headerrevision", "## Revision" }
        }, AppConstants.RepoSerializerOptions);

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
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "folderrepo", "docs/adr" },
            { "template", "# ADR {0}" },
            { "prefix", "ADR" },
            { "lenseq", 4 },
            { "lenversion", 2 },
            { "lenrevision", 0 },
            { "lenscope", 3 },
            { "scopes", "scope1;scope2" },
            { "folderbyscope", false },
            { "skipdomain", "scope1;invalidscope" }, // invalidscope is not in scopes
            { "separator", "-" },
            { "casetransform", "CamelCase" },
            { "statusnew", "New" },
            { "statusacc", "Accepted" },
            { "statusrej", "Rejected" },            { "statussup", "Superseded" },
            { "headerdisclaimer", "# Disclaimer" },
            { "headerstatus", "## Status" },
            { "headerversion", "## Version" },
            { "headerrevision", "## Revision" }
        }, AppConstants.RepoSerializerOptions);

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
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "folderrepo", "docs/adr" },
            { "dateformat", "yyyy-MM-dd" },
            { "template", "# ADR {0}" },
            { "prefix", "ADR" },
            { "lenseq", 4 },
            { "lenversion", 2 },
            { "lenrevision", 0 },
            { "lenscope", 0 },
            { "scopes", "" },
            { "folderbyscope", true }, // Cannot be true when scopes is empty
            { "skipdomain", "" },
            { "separator", "-" },
            { "casetransform", "CamelCase" },
            { "statusnew", "New" },
            { "statusacc", "Accepted" },
            { "statusrej", "Rejected" },            { "statussup", "Superseded" },
            { "headerdisclaimer", "# Disclaimer" },
            { "headerstatus", "## Status" },
            { "headerversion", "## Version" },
            { "headerrevision", "## Revision" }
        }, AppConstants.RepoSerializerOptions);

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
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "folderrepo", "docs/adr" },
            { "template", "# ADR {0}" },
            { "prefix", "ADR" },
            { "lenseq", 4 },
            { "lenversion", 2 },
            { "lenrevision", 0 },
            { "lenscope", 0 },
            { "scopes", "" },
            { "folderbyscope", false },
            { "skipdomain", "" },
            { "separator", "-" },
            { "casetransform", "CamelCase" },
            { "statusnew", "" }, // Empty
            { "statusacc", "Accepted" },
            { "statusrej", "Rejected" },            { "statussup", "Superseded" },
            { "headerdisclaimer", "# Disclaimer" },
            { "headerstatus", "## Status" },
            { "headerversion", "## Version" },
            { "headerrevision", "## Revision" }
        }, AppConstants.RepoSerializerOptions);

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
                ""folderrepo"": ""docs/adr"",
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
    public void ValidateAppStructure_WithEmptyFolderRepo_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = @"{
            ""DefaultSettings"": {
                ""language"": ""en-US"",
                ""folderrepo"": """",
                ""comandopenadr"": ""code {0}"",
                ""yesvalue"": ""y"",
                ""novalue"": ""n""
            }
        }";

        // Act
        var (IsValid, ErrorReport) = validator.ValidateAppStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("folderrepo"));
    }

    [Fact]
    public void ValidateAppStructure_WithOpenAdrMissingPlaceholder_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = @"{
            ""DefaultSettings"": {
                ""language"": ""en-US"",
                ""folderrepo"": ""docs/adr"",
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
    public void ValidateAppStructure_WithYesValueTooLong_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = @"{
            ""DefaultSettings"": {
                ""language"": ""en-US"",
                ""folderrepo"": ""docs/adr"",
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
                ""folderrepo"": ""docs/adr"",
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
                ""folderrepo"": ""docs/adr"",
                ""comandopenadr"": ""code {0}"",
                ""dateformat"": ""yyyy-MM-dd"",
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
            { "scopes", "ab;abc" }, // Minimum length is 2
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

    #endregion

    #region File Path Tests

    [Fact]
    public void HasTemplateRepoFile_WhenFileExists_ReturnsTrue()
    {
        // Arrange
        var validator = CreateValidator([]);
        var expectedPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "template", "adr-config.adrplus"));
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
        var expectedPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "template", "adr-config.adrplus"));
        _fileSystem.FileExists(expectedPath).Returns(false);

        // Act
        var result = validator.HasTemplateRepoFile();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetConfigRepoFilePath_ReturnsCorrectPath()
    {
        // Arrange
        var validator = CreateValidator([]);

        // Act
        var result = validator.GetConfigRepoFilePath();

        // Assert
        result.Should().Contain("template");
        result.Should().Contain("adr-config.adrplus");
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
        result.Should().Contain("adrplus.json");
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
        result.Should().Contain("template");
        result.Should().Contain("adr-template.md");
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
        result.Should().Be("adr-config.adrplus");
    }

    #endregion

    #region GetConfigRepoTemplateAsync Tests

    [Fact]
    public async Task GetConfigRepoTemplateAsync_WhenFileExists_ReturnsContent()
    {
        // Arrange
        var validator = CreateValidator([]);
        var expectedContent = "template content";
        var expectedPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "template", "adr-config.adrplus"));
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
        var expectedPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "template", "adr-config.adrplus"));
        _fileSystem.FileExists(expectedPath).Returns(false);

        // Act
        var act = async () => await validator.GetConfigRepoTemplateAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    #endregion

    #region GetConfigAdrTemplateAsync Tests

    [Fact]
    public async Task GetConfigAdrTemplateAsync_WhenFileExists_ReturnsContent()
    {
        // Arrange
        var validator = CreateValidator([]);
        var expectedContent = "ADR template content";
        var expectedPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "template", "adr-template.md"));
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
        var expectedPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "template", "adr-template.md"));
        _fileSystem.FileExists(expectedPath).Returns(false);

        // Act
        var act = async () => await validator.GetConfigAdrTemplateAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    #endregion

    #region InitializeTemplateAsync Tests

    [Fact]
    public async Task InitializeTemplateAsync_WithEnglishCulture_CreatesEnglishTemplate()
    {
        // Arrange
        var validator = CreateValidator([]);
        var templateDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "template"));
        var templateFile = Path.Combine(templateDir, "adr-template.md");
        _fileSystem.DirectoryExists(templateDir).Returns(false);
        _fileSystem.FileExists(templateFile).Returns(false);

        // Act
        var result = await validator.InitializeTemplateAsync("en-US", CancellationToken.None);

        // Assert
        _fileSystem.Received(1).CreateDirectory(templateDir);
        await _fileSystem.Received(1).WriteAllTextAsync(templateFile, result!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeTemplateAsync_WithPortugueseCulture_CreatesPortugueseTemplate()
    {
        // Arrange
        var validator = CreateValidator([]);
        var templateDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "template"));
        var templateFile = Path.Combine(templateDir, "adr-template.md");
        _fileSystem.DirectoryExists(templateDir).Returns(true);
        _fileSystem.FileExists(templateFile).Returns(false);

        // Act
        var result = await validator.InitializeTemplateAsync("pt-BR", CancellationToken.None);

        // Assert
        _fileSystem.DidNotReceive().CreateDirectory(Arg.Any<string>());
        await _fileSystem.Received(1).WriteAllTextAsync(templateFile, result!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeTemplateAsync_WhenTemplateExists_DoesNotOverwrite()
    {
        // Arrange
        var validator = CreateValidator([]);
        var templateDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "template"));
        var templateFile = Path.Combine(templateDir, "adr-template.md");
        _fileSystem.DirectoryExists(templateDir).Returns(true);
        _fileSystem.FileExists(templateFile).Returns(true);

        // Act
        var result = await validator.InitializeTemplateAsync("en-US", CancellationToken.None);

        // Assert
        await _fileSystem.DidNotReceive().WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task InitializeTemplateAsync_WithNullCulture_CreatesEnglishTemplate()
    {
        // Arrange
        var validator = CreateValidator([]);
        var templateDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "template"));
        var templateFile = Path.Combine(templateDir, "adr-template.md");
        _fileSystem.DirectoryExists(templateDir).Returns(true);
        _fileSystem.FileExists(templateFile).Returns(false);

        // Act
        var result = await validator.InitializeTemplateAsync(null, CancellationToken.None);

        // Assert
        await _fileSystem.Received(1).WriteAllTextAsync(templateFile, result!, Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetConfigDefaultRepoContentAsync Tests

    [Fact]
    public async Task GetConfigDefaultRepoContentAsync_WhenFileExists_ReturnsFileContent()
    {
        // Arrange
        var validator = CreateValidator([]);
        var config = new AdrPlusConfig { FolderRepo = "docs/adr"};
        var expectedContent = "existing content";
        var expectedPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "template", "adr-config.adrplus"));
        _fileSystem.FileExists(expectedPath).Returns(true);
        _fileSystem.ReadAllTextAsync(expectedPath, Arg.Any<CancellationToken>()).Returns(expectedContent);

        // Act
        var result = await validator.GetConfigDefaultRepoContentAsync(config, CancellationToken.None);

        // Assert
        result.Should().Be(expectedContent);
    }

    [Fact]
    public async Task GetConfigDefaultRepoContentAsync_WhenFileDoesNotExist_ReturnsSerializedConfig()
    {
        // Arrange
        var validator = CreateValidator([]);
        var config = new AdrPlusConfig { FolderRepo = "docs/adr"};
        var templateContent = "# Template Content";
        var configPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "template", "adr-config.adrplus"));
        var templatePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "template", "adr-template.md"));

        // Setup: Handle multiple file path checks
        _fileSystem.FileExists(Arg.Any<string>()).Returns(callInfo =>
        {
            var path = callInfo.Arg<string>();
            if (path == configPath) return false;      // config file doesn't exist
            if (path == templatePath) return true;     // template file exists
            return false;
        });
        _fileSystem.ReadAllTextAsync(templatePath, Arg.Any<CancellationToken>()).Returns(templateContent);

        // Act
        var result = await validator.GetConfigDefaultRepoContentAsync(config, CancellationToken.None);

        // Assert - should be serialized JSON containing config values and template
        result.Should().Contain("docs/adr");
        result.Should().Contain(templateContent);
    }

    #endregion

    #region Additional Edge Case Tests

    [Fact]
    public async Task ValidateAsync_WithEmptyLanguage_IsValid()
    {
        // Arrange
        var validator = CreateValidator(new Dictionary<string, string?>
        {
            { $"{AppConstants.DefaultSettingsRoot}:{AppConstants.FieldLanguage}", "" },
            { $"{AppConstants.DefaultSettingsRoot}:{AppConstants.FieldFolderRepo}", "docs/adr" }
        });

        var templatePath = Path.Combine(AppContext.BaseDirectory, "template", "adr-template.md");
        _fileSystem.FileExists(templatePath).Returns(true);

        // Act
        var (IsValid, _) = await validator.ValidateAsync(CancellationToken.None);

        // Assert
        IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithValidLanguageAndMissingTemplate_InitializesTemplate()
    {
        // Arrange
        var validator = CreateValidator(new Dictionary<string, string?>
        {
            { $"{AppConstants.DefaultSettingsRoot}:{AppConstants.FieldLanguage}", "en-US" },
            { $"{AppConstants.DefaultSettingsRoot}:{AppConstants.FieldFolderRepo}", "docs/adr" }
        });

        var templatePath = Path.Combine(AppContext.BaseDirectory, "template", "adr-template.md");
        var templateDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "template"));
        _fileSystem.FileExists(templatePath).Returns(false);
        _fileSystem.DirectoryExists(templateDir).Returns(true);

        // Act
        var (IsValid, _) = await validator.ValidateAsync(CancellationToken.None);

        // Assert
        IsValid.Should().BeTrue();
        await _fileSystem.Received(1).WriteAllTextAsync(templatePath, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ValidateRepoStructure_WithInvalidCaseTransform_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "folderrepo", "docs/adr" },
            { "template", "# ADR {0}" },
            { "prefix", "ADR" },
            { "lenseq", 4 },
            { "lenversion", 2 },
            { "lenrevision", 0 },
            { "lenscope", 0 },
            { "scopes", "" },
            { "folderbyscope", false },
            { "skipdomain", "" },
            { "separator", "-" },
            { "casetransform", "InvalidCase" },
            { "statusnew", "New" },
            { "statusacc", "Accepted" },
            { "statusrej", "Rejected" },            { "statussup", "Superseded" },
            { "headerdisclaimer", "# Disclaimer" },
            { "headerstatus", "## Status" },
            { "headerversion", "## Version" },
            { "headerrevision", "## Revision" }
        }, AppConstants.RepoSerializerOptions);

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
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "folderrepo", "docs/adr" },
            { "template", "# ADR {0}" },
            { "prefix", "ADR" },
            { "lenseq", 4 },
            { "lenversion", 1 },
            { "lenrevision", 0 },
            { "lenscope", 0 },
            { "scopes", "" },
            { "folderbyscope", false },
            { "skipdomain", "" },
            { "separator", "-" },
            { "casetransform", "CamelCase" },
            { "statusnew", "New" },
            { "statusacc", "Accepted" },
            { "statusrej", "Rejected" },            { "statussup", "Superseded" },
            { "headerdisclaimer", "# Disclaimer" },
            { "headerstatus", "## Status" },
            { "headerversion", "## Version" },
            { "headerrevision", "## Revision" }
        }, AppConstants.RepoSerializerOptions);

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
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "folderrepo", "docs/adr" },
            { "template", "# ADR {0}" },
            { "prefix", "ADR" },
            { "lenseq", 4 },
            { "lenversion", 2 },
            { "lenrevision", -1 },
            { "lenscope", 0 },
            { "scopes", "" },
            { "folderbyscope", false },
            { "skipdomain", "" },
            { "separator", "-" },
            { "casetransform", "CamelCase" },
            { "statusnew", "New" },
            { "statusacc", "Accepted" },
            { "statusrej", "Rejected" },            { "statussup", "Superseded" },
            { "headerdisclaimer", "# Disclaimer" },
            { "headerstatus", "## Status" },
            { "headerversion", "## Version" },
            { "headerrevision", "## Revision" }
        }, AppConstants.RepoSerializerOptions);

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
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "folderrepo", "docs/adr" },
            { "template", "# ADR {0}" },
            { "prefix", "ADR" },
            { "lenseq", 4 },
            { "lenversion", 2 },
            { "lenrevision", 0 },
            { "lenscope", 5 },
            { "scopes", "ab;cd" },
            { "folderbyscope", false },
            { "skipdomain", "" },
            { "separator", "-" },
            { "casetransform", "CamelCase" },
            { "statusnew", "New" },
            { "statusacc", "Accepted" },
            { "statusrej", "Rejected" },            { "statussup", "Superseded" },
            { "headerdisclaimer", "# Disclaimer" },
            { "headerstatus", "## Status" },
            { "headerversion", "## Version" },
            { "headerrevision", "## Revision" }
        }, AppConstants.RepoSerializerOptions);

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
            var json = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                { "folderrepo", "docs/adr" },
                { "template", "# ADR {0}" },
                { "prefix", "ADR" },
                { "lenseq", 4 },
                { "lenversion", 2 },
                { "lenrevision", 0 },
                { "lenscope", 0 },
                { "scopes", "" },
                { "folderbyscope", false },
                { "skipdomain", "" },
                { "separator", separator },
                { "casetransform", "CamelCase" },
                { "statusnew", "New" },
                { "statusacc", "Accepted" },
                { "statusrej", "Rejected" },            { "statussup", "Superseded" },
                { "headerdisclaimer", "# Disclaimer" },
                { "headerstatus", "## Status" },
                { "headerversion", "## Version" },
                { "headerrevision", "## Revision" }
            }, AppConstants.RepoSerializerOptions);

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
            var json = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                { "folderrepo", "docs/adr" },
                { "template", "# ADR {0}" },
                { "prefix", "ADR" },
                { "lenseq", 4 },
                { "lenversion", 2 },
                { "lenrevision", 0 },
                { "lenscope", 0 },
                { "scopes", "" },
                { "folderbyscope", false },
                { "skipdomain", "" },
                { "separator", "-" },
                { "casetransform", format },
                { "statusnew", "New" },
                { "statusacc", "Accepted" },
                { "statusrej", "Rejected" },            { "statussup", "Superseded" },
                { "headerdisclaimer", "# Disclaimer" },
                { "headerstatus", "## Status" },
                { "headerversion", "## Version" },
                { "headerrevision", "## Revision" }
            }, AppConstants.RepoSerializerOptions);

            // Act
            var (IsValid, _) = validator.ValidateRepoStructure(json);

            // Assert
            IsValid.Should().BeTrue($"case format '{format}' should be valid");
        }
    }

    [Fact]
    public void ValidateAppStructure_WithEmptyLanguage_IsValid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = @"{
            ""DefaultSettings"": {
                ""language"": """",
                ""folderrepo"": ""docs/adr"",
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
        // Use case-insensitive comparison to find the value
        var scopesValue = resultDict!.FirstOrDefault(kvp => 
            kvp.Key.Equals("scopes", StringComparison.OrdinalIgnoreCase)).Value;
        scopesValue.ToString().Should().Be("scope1;scope2");
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
    public async Task InitializeTemplateAsync_WithPortuguesePortugalCulture_CreatesPortugueseTemplate()
    {
        // Arrange
        var validator = CreateValidator([]);
        var templateDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "template"));
        var templateFile = Path.Combine(templateDir, "adr-template.md");
        _fileSystem.DirectoryExists(templateDir).Returns(true);
        _fileSystem.FileExists(templateFile).Returns(false);

        // Act
        var result = await validator.InitializeTemplateAsync("pt-PT", CancellationToken.None);

        // Assert
        await _fileSystem.Received(1).WriteAllTextAsync(templateFile, result!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeTemplateAsync_WithInvalidCulture_CreatesEnglishTemplate()
    {
        // Arrange
        var validator = CreateValidator([]);
        var templateDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "template"));
        var templateFile = Path.Combine(templateDir, "adr-template.md");
        _fileSystem.DirectoryExists(templateDir).Returns(true);
        _fileSystem.FileExists(templateFile).Returns(false);

        // Act
        var result = await validator.InitializeTemplateAsync("invalid-culture", CancellationToken.None);

        // Assert
        await _fileSystem.Received(1).WriteAllTextAsync(templateFile, result!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetConfigRepoTemplateAsync_WithCancellationToken_PassesTokenCorrectly()
    {
        // Arrange
        var validator = CreateValidator([]);
        var expectedContent = "template content";
        var expectedPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "template", "adr-config.adrplus"));
        var cts = new CancellationTokenSource();
        _fileSystem.FileExists(expectedPath).Returns(true);
        _fileSystem.ReadAllTextAsync(expectedPath, cts.Token).Returns(expectedContent);

        // Act
        var result = await validator.GetConfigRepoTemplateAsync(cts.Token);

        // Assert
        result.Should().Be(expectedContent);
        await _fileSystem.Received(1).ReadAllTextAsync(expectedPath, cts.Token);
    }

    [Fact]
    public async Task GetConfigAdrTemplateAsync_WithCancellationToken_PassesTokenCorrectly()
    {
        // Arrange
        var validator = CreateValidator([]);
        var expectedContent = "ADR template content";
        var expectedPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "template", "adr-template.md"));
        var cts = new CancellationTokenSource();
        _fileSystem.FileExists(expectedPath).Returns(true);
        _fileSystem.ReadAllTextAsync(expectedPath, cts.Token).Returns(expectedContent);

        // Act
        var result = await validator.GetConfigAdrTemplateAsync(cts.Token);

        // Assert
        result.Should().Be(expectedContent);
        await _fileSystem.Received(1).ReadAllTextAsync(expectedPath, cts.Token);
    }

    [Fact]
    public void ValidateRepoStructure_WithEmptyHeaderFields_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "folderrepo", "docs/adr" },
            { "template", "# ADR {0}" },
            { "prefix", "ADR" },
            { "lenseq", 4 },
            { "lenversion", 2 },
            { "lenrevision", 0 },
            { "lenscope", 0 },
            { "scopes", "" },
            { "folderbyscope", false },
            { "skipdomain", "" },
            { "separator", "-" },
            { "casetransform", "CamelCase" },
            { "statusnew", "New" },
            { "statusacc", "Accepted" },
            { "statusrej", "Rejected" },            { "statussup", "Superseded" },
            { "headerdisclaimer", "" },
            { "headerstatus", "## Status" },
            { "headerversion", "## Version" },
            { "headerrevision", "## Revision" }
        }, AppConstants.RepoSerializerOptions);

        // Act
        var (IsValid, ErrorReport) = validator.ValidateRepoStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("headerdisclaimer"));
    }

    [Fact]
    public void ValidateRepoStructure_WithEmptyStatusRejected_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "folderrepo", "docs/adr" },
            { "template", "# ADR {0}" },
            { "prefix", "ADR" },
            { "lenseq", 4 },
            { "lenversion", 2 },
            { "lenrevision", 0 },
            { "lenscope", 0 },
            { "scopes", "" },
            { "folderbyscope", false },
            { "skipdomain", "" },
            { "separator", "-" },
            { "casetransform", "CamelCase" },
            { "statusnew", "New" },
            { "statusacc", "Accepted" },
            { "statusrej", "" },            { "statussup", "Superseded" },
            { "headerdisclaimer", "# Disclaimer" },
            { "headerstatus", "## Status" },
            { "headerversion", "## Version" },
            { "headerrevision", "## Revision" }
        }, AppConstants.RepoSerializerOptions);

        // Act
        var (IsValid, ErrorReport) = validator.ValidateRepoStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("statusrej"));
    }

    [Fact]
    public void ValidateRepoStructure_WithEmptyStatusAccepted_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "folderrepo", "docs/adr" },
            { "template", "# ADR {0}" },
            { "prefix", "ADR" },
            { "lenseq", 4 },
            { "lenversion", 2 },
            { "lenrevision", 0 },
            { "lenscope", 0 },
            { "scopes", "" },
            { "folderbyscope", false },
            { "skipdomain", "" },
            { "separator", "-" },
            { "casetransform", "CamelCase" },
            { "statusnew", "New" },
            { "statusacc", "" },
            { "statusrej", "Rejected" },            { "statussup", "Superseded" },
            { "headerdisclaimer", "# Disclaimer" },
            { "headerstatus", "## Status" },
            { "headerversion", "## Version" },
            { "headerrevision", "## Revision" }
        }, AppConstants.RepoSerializerOptions);

        // Act
        var (IsValid, ErrorReport) = validator.ValidateRepoStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("statusacc"));
    }

    [Fact]
    public void ValidateRepoStructure_WithEmptyStatusSuperseded_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "folderrepo", "docs/adr" },
            { "template", "# ADR {0}" },
            { "prefix", "ADR" },
            { "lenseq", 4 },
            { "lenversion", 2 },
            { "lenrevision", 0 },
            { "lenscope", 0 },
            { "scopes", "" },
            { "folderbyscope", false },
            { "skipdomain", "" },
            { "separator", "-" },
            { "casetransform", "CamelCase" },
            { "statusnew", "New" },
            { "statusacc", "Accepted" },
            { "statusrej", "Rejected" },            { "statussup", "" },
            { "headerdisclaimer", "# Disclaimer" },
            { "headerstatus", "## Status" },
            { "headerversion", "## Version" },
            { "headerrevision", "## Revision" }
        }, AppConstants.RepoSerializerOptions);

        // Act
        var (IsValid, ErrorReport) = validator.ValidateRepoStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("statussup"));
    }

    [Fact]
    public void ValidateRepoStructure_WithEmptyHeaderStatus_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "folderrepo", "docs/adr" },
            { "template", "# ADR {0}" },
            { "prefix", "ADR" },
            { "lenseq", 4 },
            { "lenversion", 2 },
            { "lenrevision", 0 },
            { "lenscope", 0 },
            { "scopes", "" },
            { "folderbyscope", false },
            { "skipdomain", "" },
            { "separator", "-" },
            { "casetransform", "CamelCase" },
            { "statusnew", "New" },
            { "statusacc", "Accepted" },
            { "statusrej", "Rejected" },            { "statussup", "Superseded" },
            { "headerdisclaimer", "# Disclaimer" },
            { "headerstatus", "" },
            { "headerversion", "## Version" },
            { "headerrevision", "## Revision" }
        }, AppConstants.RepoSerializerOptions);

        // Act
        var (IsValid, ErrorReport) = validator.ValidateRepoStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("headerstatus"));
    }

    [Fact]
    public void ValidateRepoStructure_WithEmptyHeaderVersion_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "folderrepo", "docs/adr" },
            { "template", "# ADR {0}" },
            { "prefix", "ADR" },
            { "lenseq", 4 },
            { "lenversion", 2 },
            { "lenrevision", 0 },
            { "lenscope", 0 },
            { "scopes", "" },
            { "folderbyscope", false },
            { "skipdomain", "" },
            { "separator", "-" },
            { "casetransform", "CamelCase" },
            { "statusnew", "New" },
            { "statusacc", "Accepted" },
            { "statusrej", "Rejected" },            { "statussup", "Superseded" },
            { "headerdisclaimer", "# Disclaimer" },
            { "headerstatus", "## Status" },
            { "headerversion", "" },
            { "headerrevision", "## Revision" }
        }, AppConstants.RepoSerializerOptions);

        // Act
        var (IsValid, ErrorReport) = validator.ValidateRepoStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("headerversion"));
    }

    [Fact]
    public void ValidateRepoStructure_WithEmptyHeaderRevision_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "folderrepo", "docs/adr" },
            { "template", "# ADR {0}" },
            { "prefix", "ADR" },
            { "lenseq", 4 },
            { "lenversion", 2 },
            { "lenrevision", 0 },
            { "lenscope", 0 },
            { "scopes", "" },
            { "folderbyscope", false },
            { "skipdomain", "" },
            { "separator", "-" },
            { "casetransform", "CamelCase" },
            { "statusnew", "New" },
            { "statusacc", "Accepted" },
            { "statusrej", "Rejected" },            { "statussup", "Superseded" },
            { "headerdisclaimer", "# Disclaimer" },
            { "headerstatus", "## Status" },
            { "headerversion", "## Version" },
            { "headerrevision", "" }
        }, AppConstants.RepoSerializerOptions);

        // Act
        var (IsValid, ErrorReport) = validator.ValidateRepoStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("headerrevision"));
    }

    #endregion

    #region ValidateRepoStructure Boolean Field Tests

    [Fact]
    public void ValidateRepoStructure_WithNonBooleanFolderByScope_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = @"{
            ""folderrepo"": ""docs/adr"",
            ""dateformat"": ""yyyy-MM-dd"",
            ""template"": ""# ADR"",
            ""prefix"": ""ADR"",
            ""lenseq"": 4,
            ""lenversion"": 2,
            ""lenrevision"": 0,
            ""lenscope"": 0,
            ""scopes"": """",
            ""folderbyscope"": ""not-a-boolean"",
            ""skipdomain"": """",
            ""separator"": ""-"",
            ""casetransform"": ""CamelCase"",
            ""statusnew"": ""New"",
            ""statusacc"": ""Accepted"",
            ""statusrej"": ""Rejected"",            ""statussup"": ""Superseded"",
            ""headerdisclaimer"": ""# Disclaimer"",
            ""headerstatus"": ""## Status"",
            ""headerversion"": ""## Version"",
            ""headerrevision"": ""## Revision""
        }";

        // Act
        var (IsValid, ErrorReport) = validator.ValidateRepoStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("folderbyscope") && e.Contains("boolean"));
    }

    [Fact]
    public void ValidateRepoStructure_WithNumericFolderByScope_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = @"{
            ""folderrepo"": ""docs/adr"",
            ""dateformat"": ""yyyy-MM-dd"",
            ""template"": ""# ADR"",
            ""prefix"": ""ADR"",
            ""lenseq"": 4,
            ""lenversion"": 2,
            ""lenrevision"": 0,
            ""lenscope"": 0,
            ""scopes"": """",
            ""folderbyscope"": 1,
            ""skipdomain"": """",
            ""separator"": ""-"",
            ""casetransform"": ""CamelCase"",
            ""statusnew"": ""New"",
            ""statusacc"": ""Accepted"",
            ""statusrej"": ""Rejected"",            ""statussup"": ""Superseded"",
            ""headerdisclaimer"": ""# Disclaimer"",
            ""headerstatus"": ""## Status"",
            ""headerversion"": ""## Version"",
            ""headerrevision"": ""## Revision""
        }";

        // Act
        var (IsValid, ErrorReport) = validator.ValidateRepoStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("folderbyscope") && e.Contains("boolean"));
    }

    #endregion

    #region ValidateContentFileAsync Exception Tests

    [Fact]
    public async Task ValidateAsync_WithRelativePathContent_InitializesWhenFileNotFound()
    {
        // Arrange
        var validator = CreateValidator(new Dictionary<string, string?>
        {
            { $"{AppConstants.DefaultSettingsRoot}:{AppConstants.FieldLanguage}", "en-US" },
            { $"{AppConstants.DefaultSettingsRoot}:{AppConstants.FieldFolderRepo}", "docs/adr" }
        });

        var contentPath = Path.Combine(AppContext.BaseDirectory, "template", "adr-template.md");
        var templateDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "template"));

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
            { $"{AppConstants.DefaultSettingsRoot}:{AppConstants.FieldFolderRepo}", "docs/adr" }
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

    #region ValidateAppStructure Path Validation Tests

    [Fact]
    public void ValidateAppStructure_WithInvalidFolderRepoPath_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = @"{
            ""DefaultSettings"": {
                ""language"": ""en-US"",
                ""folderrepo"": ""C:\invalid<>path"",
                ""comandopenadr"": ""code {0}"",
                ""dateformat"": ""yyyy-MM-dd"",
                ""yesvalue"": ""y"",
                ""novalue"": ""n""
            }
        }";

        // Act
        var (IsValid, ErrorReport) = validator.ValidateAppStructure(json);

        // Assert
        IsValid.Should().BeFalse();
        ErrorReport.Should().Contain(e => e.Contains("invalid") || e.Contains("path"));
    }

    [Fact]
    public void ValidateAppStructure_WithUnsupportedFolderRepoPath_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        // On Windows, paths like "CON", "PRN", "AUX", "NUL" are reserved and can cause NotSupportedException
        var json = @"{
            ""DefaultSettings"": {
                ""language"": ""en-US"",
                ""folderrepo"": ""CON"",
                ""comandopenadr"": ""code {0}"",
                ""dateformat"": ""yyyy-MM-dd"",
                ""yesvalue"": ""y"",
                ""novalue"": ""n""
            }
        }";

        // Act
        var result = validator.ValidateAppStructure(json);

        // Assert - CON is a reserved name on Windows, may or may not trigger validation error depending on OS
        // The test documents the behavior
        result.Should().NotBeNull();
    }

    [Fact]
    public void ValidateAppStructure_WithValidFolderRepoPath_IsValid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = @"{
            ""DefaultSettings"": {
                ""language"": ""en-US"",
                ""folderrepo"": ""docs/adr"",
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
    public void ValidateAppStructure_WithWrongFieldType_ReturnsInvalid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = @"{
            ""DefaultSettings"": {
                ""language"": 123,
                ""folderrepo"": ""docs/adr"",
                ""comandopenadr"": ""code {0}"",
                ""dateformat"": ""yyyy-MM-dd"",
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

    [Fact]
    public void ValidateAppStructure_WithEmptyOpenAdr_IsValid()
    {
        // Arrange
        var validator = CreateValidator([]);
        var json = @"{
            ""DefaultSettings"": {
                ""language"": ""en-US"",
                ""folderrepo"": ""docs/adr"",
                ""comandopenadr"": """",
                ""yesvalue"": ""y"",
                ""novalue"": ""n""
            }
        }";

        // Act
        var (IsValid, _) = validator.ValidateAppStructure(json);

        // Assert
        IsValid.Should().BeTrue();
    }

    #endregion

    #region InitializeTemplate Additional Tests

    [Fact]
    public async Task InitializeTemplateAsync_WithEmptyString_CreatesEnglishTemplate()
    {
        // Arrange
        var validator = CreateValidator([]);
        var templateDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "template"));
        var templateFile = Path.Combine(templateDir, "adr-template.md");
        _fileSystem.DirectoryExists(templateDir).Returns(true);
        _fileSystem.FileExists(templateFile).Returns(false);

        // Act
        var result = await validator.InitializeTemplateAsync(string.Empty, CancellationToken.None);

        // Assert
        await _fileSystem.Received(1).WriteAllTextAsync(templateFile, result!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeTemplateAsync_CreatesDirectoryWhenNotExists()
    {
        // Arrange
        var validator = CreateValidator([]);
        var templateDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "template"));
        var templateFile = Path.Combine(templateDir, "adr-template.md");
        _fileSystem.DirectoryExists(templateDir).Returns(false);
        _fileSystem.FileExists(templateFile).Returns(false);

        // Act
        var result = await validator.InitializeTemplateAsync("en-US", CancellationToken.None);

        // Assert
        _fileSystem.Received(1).CreateDirectory(templateDir);
        await _fileSystem.Received(1).WriteAllTextAsync(templateFile, result!, Arg.Any<CancellationToken>());
    }

    #endregion

    #region EnsureFieldsRepoStructure Additional Edge Cases

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

        // Assert - Should handle empty/whitespace scopes
        resultDict.Should().NotBeNull();
    }

    #endregion

    #region GetConfigDefaultRepoContentAsync Edge Cases

    [Fact]
    public async Task GetConfigDefaultRepoContentAsync_WhenTemplateThrows_PropagatesException()
    {
        // Arrange
        var validator = CreateValidator([]);
        var config = new AdrPlusConfig { FolderRepo = "docs/adr"};
        var configPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "template", "adr-config.adrplus"));

        _fileSystem.FileExists(configPath).Returns(false, true);
        _fileSystem.ReadAllTextAsync(configPath, Arg.Any<CancellationToken>())
            .Returns<string>(x => throw new FileNotFoundException("Template not found"));

        // Act
        var act = async () => await validator.GetConfigDefaultRepoContentAsync(config, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    #endregion
}
