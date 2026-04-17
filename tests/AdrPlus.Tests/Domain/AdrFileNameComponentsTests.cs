// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;

namespace AdrPlus.Tests.Domain;

public class AdrFileNameComponentsTests
{
    [Fact]
    public void CreateUniqueTitle_WithTitleAndDomain_CombinesBoth()
    {
        // Arrange
        var title = "UseNewDatabase";
        var domain = "Backend";

        // Act
        var result = AdrFileNameComponents.CreateUniqueTitle(title, domain);

        // Assert
        result.Should().Be("UseNewDatabaseBackend");
    }

    [Fact]
    public void CreateUniqueTitle_WithTitleOnly_ReturnsTitle()
    {
        // Arrange
        var title = "UseNewDatabase";
        string? domain = null;

        // Act
        var result = AdrFileNameComponents.CreateUniqueTitle(title, domain);

        // Assert
        result.Should().Be("UseNewDatabase");
    }

    [Fact]
    public void CreateUniqueTitle_WithEmptyDomain_ReturnsTitle()
    {
        // Arrange
        var title = "UseNewDatabase";
        var domain = string.Empty;

        // Act
        var result = AdrFileNameComponents.CreateUniqueTitle(title, domain);

        // Assert
        result.Should().Be("UseNewDatabase");
    }

    [Fact]
    public void UniqueTitle_Property_CombinesTitleAndDomain()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Title = "UseNewDatabase",
            Domain = "Backend"
        };

        // Act
        var result = components.UniqueTitle;

        // Assert
        result.Should().Be("UseNewDatabaseBackend");
    }

    [Fact]
    public void UniqueTitle_Property_WithNullDomain_ReturnsTitle()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Title = "UseNewDatabase",
            Domain = null
        };

        // Act
        var result = components.UniqueTitle;

        // Assert
        result.Should().Be("UseNewDatabase");
    }

    [Fact]
    public void AdrFileNameComponents_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var components = new AdrFileNameComponents();

        // Assert
        components.Prefix.Should().Be(string.Empty);
        components.Number.Should().Be(0);
        components.Title.Should().Be(string.Empty);
        components.Version.Should().Be(0);
        components.Revision.Should().BeNull();
        components.Scope.Should().BeNull();
        components.Domain.Should().BeNull();
        components.SupersededValue.Should().BeNull();
        components.IsValid.Should().BeFalse();
        components.ErrorMessage.Should().Be(string.Empty);
        components.FileName.Should().Be(string.Empty);
        components.Header.Should().NotBeNull();
        components.ContentAdr.Should().BeNull();
    }

    [Fact]
    public void AdrFileNameComponents_AllProperties_CanBeSet()
    {
        // Arrange
        var header = new AdrHeader { Title = "Test" };
        var components = new AdrFileNameComponents
        {
            Prefix = "ADR",
            Number = 1,
            Title = "UseNewDatabase",
            Version = 1,
            Revision = 0,
            Scope = "API",
            Domain = "Backend",
            SupersededValue = 2,
            IsValid = true,
            ErrorMessage = "No errors",
            FileName = "ADR-0001.md",
            Header = header,
            ContentAdr = "Content here"
        };

        // Assert
        components.Prefix.Should().Be("ADR");
        components.Number.Should().Be(1);
        components.Title.Should().Be("UseNewDatabase");
        components.Version.Should().Be(1);
        components.Revision.Should().Be(0);
        components.Scope.Should().Be("API");
        components.Domain.Should().Be("Backend");
        components.SupersededValue.Should().Be(2);
        components.IsValid.Should().BeTrue();
        components.ErrorMessage.Should().Be("No errors");
        components.FileName.Should().Be("ADR-0001.md");
        components.Header.Should().Be(header);
        components.ContentAdr.Should().Be("Content here");
    }
}
