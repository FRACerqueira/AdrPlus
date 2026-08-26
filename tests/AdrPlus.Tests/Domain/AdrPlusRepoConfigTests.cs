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
        config.Separator.Should().Be('-');
        config.CaseTransform.Should().Be(CaseFormat.KebabCase);
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
            Separator = '~',
            CaseTransform = CaseFormat.PascalCase,
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
        config.StatusMapping.Should().HaveCount(5);
    }

    #endregion
}
