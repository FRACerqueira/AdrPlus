// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;

namespace AdrPlus.Tests.Domain;

public class AdrHeaderTests
{
    [Fact]
    public void AdrHeader_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var header = new AdrHeader();

        // Assert
        header.Disclaimer.Should().Be(string.Empty);
        header.Version.Should().Be(0);
        header.Revision.Should().BeNull();
        header.Scope.Should().Be(string.Empty);
        header.Domain.Should().Be(string.Empty);
        header.StatusCreate.Should().Be(AdrStatus.Unknown);
        header.DateCreate.Should().BeNull();
        header.StatusUpdate.Should().Be(AdrStatus.Unknown);
        header.DateUpdate.Should().BeNull();
        header.StatusChange.Should().Be(AdrStatus.Unknown);
        header.FileSuperSedes.Should().Be(string.Empty);
        header.DateChange.Should().BeNull();
        header.Title.Should().Be(string.Empty);
        header.IsValid.Should().BeFalse();
        header.ErrorMessage.Should().Be(string.Empty);
    }

    [Fact]
    public void AdrHeader_AllProperties_CanBeSet()
    {
        // Arrange
        var createDate = DateTime.UtcNow;
        var updateDate = createDate.AddDays(1);
        var changeDate = createDate.AddDays(2);

        var header = new AdrHeader
        {
            Disclaimer = "Test Disclaimer",
            Version = 1,
            Revision = 0,
            Scope = "API",
            Domain = "Backend",
            StatusCreate = AdrStatus.Proposed,
            DateCreate = createDate,
            StatusUpdate = AdrStatus.Accepted,
            DateUpdate = updateDate,
            StatusChange = AdrStatus.Superseded,
            FileSuperSedes = "ADR-0002.md",
            DateChange = changeDate,
            Title = "Use New Database",
            IsValid = true,
            ErrorMessage = string.Empty
        };

        // Assert
        header.Disclaimer.Should().Be("Test Disclaimer");
        header.Version.Should().Be(1);
        header.Revision.Should().Be(0);
        header.Scope.Should().Be("API");
        header.Domain.Should().Be("Backend");
        header.StatusCreate.Should().Be(AdrStatus.Proposed);
        header.DateCreate.Should().Be(createDate);
        header.StatusUpdate.Should().Be(AdrStatus.Accepted);
        header.DateUpdate.Should().Be(updateDate);
        header.StatusChange.Should().Be(AdrStatus.Superseded);
        header.FileSuperSedes.Should().Be("ADR-0002.md");
        header.DateChange.Should().Be(changeDate);
        header.Title.Should().Be("Use New Database");
        header.IsValid.Should().BeTrue();
        header.ErrorMessage.Should().Be(string.Empty);
    }

    [Fact]
    public void AdrHeader_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var date = DateTime.UtcNow;
        var header1 = new AdrHeader
        {
            Title = "Test",
            Version = 1,
            StatusCreate = AdrStatus.Proposed,
            DateCreate = date
        };

        var header2 = new AdrHeader
        {
            Title = "Test",
            Version = 1,
            StatusCreate = AdrStatus.Proposed,
            DateCreate = date
        };

        // Act & Assert
        header1.Should().Be(header2);
    }

    [Fact]
    public void AdrHeader_RecordEquality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var header1 = new AdrHeader { Title = "Test1", Version = 1 };
        var header2 = new AdrHeader { Title = "Test2", Version = 1 };

        // Act & Assert
        header1.Should().NotBe(header2);
    }
}
