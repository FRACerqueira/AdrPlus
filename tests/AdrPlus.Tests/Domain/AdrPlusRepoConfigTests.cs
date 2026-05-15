// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;

namespace AdrPlus.Tests.Domain;

/// <summary>
/// Unit tests for AdrPlusRepoConfig class.
/// Tests demonstrate configuration initialization, property defaults, and utility methods using standard assertions.
/// </summary>
public class AdrPlusRepoConfigTests
{
    private const string TestFolderAdr = "docs/adr";
    private const string TestTemplate = "# ADR Template";

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var config = new AdrPlusRepoConfig(TestFolderAdr, TestTemplate);

        // Assert
        config.Should().NotBeNull();
        config.FolderAdr.Should().Be(TestFolderAdr);
        config.Template.Should().Be(TestTemplate);
    }

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var config = new AdrPlusRepoConfig(TestFolderAdr, TestTemplate);

        // Assert - check default values
        config.MigrationPattern.Should().Be(string.Empty);
        config.LenSeq.Should().Be(3);
        config.LenVersion.Should().Be(2);
        config.LenRevision.Should().Be(0);
        config.LenScope.Should().Be(0);
        config.Separator.Should().Be('-');
        config.CaseTransform.Should().Be(CaseFormat.KebabCase);
        config.FolderByScope.Should().BeFalse();
        config.Scopes.Should().Be(string.Empty);
        config.SkipDomain.Should().Be(string.Empty);
    }

    [Fact]
    public void Constructor_WithEmptyFolderAdr_Succeeds()
    {
        // Arrange & Act
        var config = new AdrPlusRepoConfig(string.Empty, TestTemplate);

        // Assert
        config.FolderAdr.Should().Be(string.Empty);
    }

    [Fact]
    public void Constructor_WithEmptyTemplate_Succeeds()
    {
        // Arrange & Act
        var config = new AdrPlusRepoConfig(TestFolderAdr, string.Empty);

        // Assert
        config.Template.Should().Be(string.Empty);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void FolderAdr_CanBeModified()
    {
        // Arrange
        var config = new AdrPlusRepoConfig(TestFolderAdr, TestTemplate);
        var newFolderAdr = "new/path/to/adr";

        // Act
        config.FolderAdr = newFolderAdr;

        // Assert
        config.FolderAdr.Should().Be(newFolderAdr);
    }

    [Fact]
    public void Template_CanBeModified()
    {
        // Arrange
        var config = new AdrPlusRepoConfig(TestFolderAdr, TestTemplate);
        var newTemplate = "# New Template";

        // Act
        config.Template = newTemplate;

        // Assert
        config.Template.Should().Be(newTemplate);
    }

    [Fact]
    public void LenSeq_CanBeSetToVariousValues()
    {
        // Arrange
        var config = new AdrPlusRepoConfig(TestFolderAdr, TestTemplate);

        // Act & Assert
        config.LenSeq = 4;
        config.LenSeq.Should().Be(4);

        config.LenSeq = 5;
        config.LenSeq.Should().Be(5);

        config.LenSeq = 3;
        config.LenSeq.Should().Be(3);
    }

    [Fact]
    public void LenVersion_CanBeSetToZeroToOmitVersion()
    {
        // Arrange
        var config = new AdrPlusRepoConfig(TestFolderAdr, TestTemplate);

        // Act
        config.LenVersion = 0;

        // Assert
        config.LenVersion.Should().Be(0);
    }

    [Fact]
    public void Separator_CanBeSetToValidValues()
    {
        // Arrange
        var config = new AdrPlusRepoConfig(TestFolderAdr, TestTemplate);

        // Act & Assert - test valid separators
        config.Separator = '-';
        config.Separator.Should().Be('-');

        config.Separator = '~';
        config.Separator.Should().Be('~');

        config.Separator = '.';
        config.Separator.Should().Be('.');
    }

    [Fact]
    public void CaseTransform_CanBeModified()
    {
        // Arrange
        var config = new AdrPlusRepoConfig(TestFolderAdr, TestTemplate);

        // Act & Assert
        config.CaseTransform = CaseFormat.PascalCase;
        config.CaseTransform.Should().Be(CaseFormat.PascalCase);

        config.CaseTransform = CaseFormat.SnakeCase;
        config.CaseTransform.Should().Be(CaseFormat.SnakeCase);

        config.CaseTransform = CaseFormat.KebabCase;
        config.CaseTransform.Should().Be(CaseFormat.KebabCase);
    }

    [Fact]
    public void MigrationPattern_CanBeSetAndModified()
    {
        // Arrange
        var config = new AdrPlusRepoConfig(TestFolderAdr, TestTemplate);
        var pattern = "ADR-\\d{4}";

        // Act
        config.MigrationPattern = pattern;

        // Assert
        config.MigrationPattern.Should().Be(pattern);
    }

    #endregion

    #region StatusMapping Tests

    [Fact]
    public void StatusMapping_ContainsAllAdrStatusValues()
    {
        // Arrange
        var config = new AdrPlusRepoConfig(TestFolderAdr, TestTemplate);

        // Act
        var mapping = config.StatusMapping;

        // Assert
        mapping.Should().NotBeEmpty();
        mapping.Should().ContainKey(AdrStatus.Unknown);
        mapping.Should().ContainKey(AdrStatus.Proposed);
        mapping.Should().ContainKey(AdrStatus.Accepted);
        mapping.Should().ContainKey(AdrStatus.Rejected);
        mapping.Should().ContainKey(AdrStatus.Superseded);
    }

    [Fact]
    public void StatusMapping_MapsProposedToStatusNew()
    {
        // Arrange
        var config = new AdrPlusRepoConfig(TestFolderAdr, TestTemplate);
        var newStatus = "New Status";
        config.StatusNew = newStatus;

        // Act
        var mapping = config.StatusMapping;

        // Assert
        mapping[AdrStatus.Proposed].Should().Be(newStatus);
    }

    [Fact]
    public void StatusMapping_MapsAcceptedToStatusAcc()
    {
        // Arrange
        var config = new AdrPlusRepoConfig(TestFolderAdr, TestTemplate);
        var accStatus = "Accepted";
        config.StatusAcc = accStatus;

        // Act
        var mapping = config.StatusMapping;

        // Assert
        mapping[AdrStatus.Accepted].Should().Be(accStatus);
    }

    [Fact]
    public void StatusMapping_MapsRejectedToStatusRej()
    {
        // Arrange
        var config = new AdrPlusRepoConfig(TestFolderAdr, TestTemplate);
        var rejStatus = "Rejected";
        config.StatusRej = rejStatus;

        // Act
        var mapping = config.StatusMapping;

        // Assert
        mapping[AdrStatus.Rejected].Should().Be(rejStatus);
    }

    [Fact]
    public void StatusMapping_MapsSupersededToStatusSup()
    {
        // Arrange
        var config = new AdrPlusRepoConfig(TestFolderAdr, TestTemplate);
        var supStatus = "Superseded";
        config.StatusSup = supStatus;

        // Act
        var mapping = config.StatusMapping;

        // Assert
        mapping[AdrStatus.Superseded].Should().Be(supStatus);
    }

    #endregion

    #region GetScopes Tests

    [Fact]
    public void GetScopes_WithEmptyScopes_ReturnsEmptyArray()
    {
        // Arrange
        var config = new AdrPlusRepoConfig(TestFolderAdr, TestTemplate);
        config.Scopes = string.Empty;

        // Act
        var scopes = config.GetScopes();

        // Assert
        scopes.Should().BeEmpty();
    }

    [Fact]
    public void GetScopes_WithNullScopes_ReturnsEmptyArray()
    {
        // Arrange
        var config = new AdrPlusRepoConfig(TestFolderAdr, TestTemplate);
        config.Scopes = null!;

        // Act
        var scopes = config.GetScopes();

        // Assert
        scopes.Should().BeEmpty();
    }

    [Fact]
    public void GetScopes_WithSingleScope_ReturnsSingleElementArray()
    {
        // Arrange
        var config = new AdrPlusRepoConfig(TestFolderAdr, TestTemplate);
        config.Scopes = "Enterprise";

        // Act
        var scopes = config.GetScopes();

        // Assert
        scopes.Should().HaveCount(1);
        scopes[0].Should().Be("Enterprise");
    }

    [Fact]
    public void GetScopes_WithMultipleScopes_ReturnsAllScopes()
    {
        // Arrange
        var config = new AdrPlusRepoConfig(TestFolderAdr, TestTemplate);
        config.Scopes = "Enterprise;Domain;Project";

        // Act
        var scopes = config.GetScopes();

        // Assert
        scopes.Should().HaveCount(3);
        scopes.Should().Contain("Enterprise");
        scopes.Should().Contain("Domain");
        scopes.Should().Contain("Project");
    }

    [Fact]
    public void GetScopes_TrimsWhitespace()
    {
        // Arrange
        var config = new AdrPlusRepoConfig(TestFolderAdr, TestTemplate);
        config.Scopes = "  Enterprise  ;  Domain  ;  Project  ";

        // Act
        var scopes = config.GetScopes();

        // Assert
        scopes.Should().HaveCount(3);
        scopes.Should().Contain("Enterprise");
        scopes.Should().Contain("Domain");
        scopes.Should().Contain("Project");
        scopes.Should().NotContain("  Enterprise  ");
    }

    [Fact]
    public void GetScopes_RemovesEmptyEntries()
    {
        // Arrange
        var config = new AdrPlusRepoConfig(TestFolderAdr, TestTemplate);
        config.Scopes = "Enterprise;;Domain;;Project";

        // Act
        var scopes = config.GetScopes();

        // Assert
        scopes.Should().HaveCount(3);
        scopes.Should().Contain("Enterprise");
        scopes.Should().Contain("Domain");
        scopes.Should().Contain("Project");
    }

    #endregion

    #region GetSkipDomains Tests

    [Fact]
    public void GetSkipDomains_WithEmptySkipDomain_ReturnsEmptyArray()
    {
        // Arrange
        var config = new AdrPlusRepoConfig(TestFolderAdr, TestTemplate);
        config.SkipDomain = string.Empty;

        // Act
        var skipDomains = config.GetSkipDomains();

        // Assert
        skipDomains.Should().BeEmpty();
    }

    [Fact]
    public void GetSkipDomains_WithNullSkipDomain_ReturnsEmptyArray()
    {
        // Arrange
        var config = new AdrPlusRepoConfig(TestFolderAdr, TestTemplate);
        config.SkipDomain = null!;

        // Act
        var skipDomains = config.GetSkipDomains();

        // Assert
        skipDomains.Should().BeEmpty();
    }

    [Fact]
    public void GetSkipDomains_WithSingleScope_ReturnsSingleElementArray()
    {
        // Arrange
        var config = new AdrPlusRepoConfig(TestFolderAdr, TestTemplate);
        config.SkipDomain = "Enterprise";

        // Act
        var skipDomains = config.GetSkipDomains();

        // Assert
        skipDomains.Should().HaveCount(1);
        skipDomains[0].Should().Be("Enterprise");
    }

    [Fact]
    public void GetSkipDomains_WithMultipleDomains_ReturnsAllDomains()
    {
        // Arrange
        var config = new AdrPlusRepoConfig(TestFolderAdr, TestTemplate);
        config.SkipDomain = "Enterprise;Global";

        // Act
        var skipDomains = config.GetSkipDomains();

        // Assert
        skipDomains.Should().HaveCount(2);
        skipDomains.Should().Contain("Enterprise");
        skipDomains.Should().Contain("Global");
    }

    [Fact]
    public void GetSkipDomains_TrimsWhitespace()
    {
        // Arrange
        var config = new AdrPlusRepoConfig(TestFolderAdr, TestTemplate);
        config.SkipDomain = "  Enterprise  ;  Global  ";

        // Act
        var skipDomains = config.GetSkipDomains();

        // Assert
        skipDomains.Should().HaveCount(2);
        skipDomains.Should().Contain("Enterprise");
        skipDomains.Should().Contain("Global");
        skipDomains.Should().NotContain("  Enterprise  ");
    }

    [Fact]
    public void GetSkipDomains_RemovesEmptyEntries()
    {
        // Arrange
        var config = new AdrPlusRepoConfig(TestFolderAdr, TestTemplate);
        config.SkipDomain = "Enterprise;;Global";

        // Act
        var skipDomains = config.GetSkipDomains();

        // Assert
        skipDomains.Should().HaveCount(2);
        skipDomains.Should().Contain("Enterprise");
        skipDomains.Should().Contain("Global");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void FullConfiguration_WithMultipleSettings_WorksTogether()
    {
        // Arrange & Act
        var config = new AdrPlusRepoConfig("docs/adr", "# ADR Template")
        {
            Prefix = "ADR",
            LenSeq = 4,
            LenVersion = 2,
            LenRevision = 1,
            LenScope = 1,
            Separator = '~',
            CaseTransform = CaseFormat.PascalCase,
            Scopes = "Enterprise;Domain;Project",
            FolderByScope = true,
            SkipDomain = "Enterprise",
            StatusNew = "Proposed",
            StatusAcc = "Accepted",
            StatusRej = "Rejected",
            StatusSup = "Superseded"
        };

        // Act & Assert
        config.FolderAdr.Should().Be("docs/adr");
        config.Prefix.Should().Be("ADR");
        config.LenSeq.Should().Be(4);
        config.Separator.Should().Be('~');
        config.CaseTransform.Should().Be(CaseFormat.PascalCase);
        config.FolderByScope.Should().BeTrue();
        config.GetScopes().Should().HaveCount(3);
        config.GetSkipDomains().Should().HaveCount(1);
        config.StatusMapping.Should().HaveCount(5);
    }

    [Fact]
    public void Configuration_CanBeCreatedWithDifferentDefaults()
    {
        // Arrange & Act - create multiple configs with different settings
        var config1 = new AdrPlusRepoConfig("docs/adr", "template1") { Separator = '-' };
        var config2 = new AdrPlusRepoConfig("src/decisions", "template2") { Separator = '~' };

        // Assert
        config1.FolderAdr.Should().Be("docs/adr");
        config1.Separator.Should().Be('-');
        config2.FolderAdr.Should().Be("src/decisions");
        config2.Separator.Should().Be('~');
    }

    #endregion
}
