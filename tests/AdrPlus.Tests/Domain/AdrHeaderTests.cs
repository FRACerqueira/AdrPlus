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
        header.NumberSuperSedes.Should().Be(string.Empty);
        header.DateChange.Should().BeNull();
        header.Title.Should().Be(string.Empty);
        header.IsValid.Should().BeFalse();
        header.IsMigrated.Should().BeFalse();
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
            NumberSuperSedes = "ADR-0002.md",
            DateChange = changeDate,
            Title = "Use New Database",
            IsValid = true,
            IsMigrated = true,
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
        header.NumberSuperSedes.Should().Be("ADR-0002.md");
        header.DateChange.Should().Be(changeDate);
        header.Title.Should().Be("Use New Database");
        header.IsValid.Should().BeTrue();
        header.IsMigrated.Should().BeTrue();
        header.ErrorMessage.Should().Be(string.Empty);
    }

    #region IsMigrated Property Tests

    [Fact]
    public void AdrHeader_IsMigrated_DefaultValue_IsFalse()
    {
        // Arrange & Act
        var header = new AdrHeader();

        // Assert
        header.IsMigrated.Should().BeFalse();
    }

    [Fact]
    public void AdrHeader_IsMigrated_CanBeSetToTrue()
    {
        // Arrange & Act
        var header = new AdrHeader { IsMigrated = true };

        // Assert
        header.IsMigrated.Should().BeTrue();
    }

    #endregion

    #region Additional Edge Cases

    [Fact]
    public void AdrHeader_WithSpecialCharactersInStrings_StoresCorrectly()
    {
        // Arrange
        var header = new AdrHeader
        {
            Disclaimer = "Test © 2024 - Special chars: !@#$%^&*()",
            Title = "Use New Database - v2.0",
            Scope = "Enterprise-Wide API",
            Domain = "Backend_Service.v1",
            NumberSuperSedes = "ADR-0001_superseded.md"
        };

        // Act & Assert
        header.Disclaimer.Should().Contain("©");
        header.Title.Should().Contain("-");
        header.Scope.Should().Contain("-");
        header.Domain.Should().Contain("_");
        header.NumberSuperSedes.Should().Contain("_");
    }

    [Fact]
    public void AdrHeader_WithLongStrings_StoresCorrectly()
    {
        // Arrange
        var longTitle = new string('A', 1000);
        var longDisclaimer = new string('B', 2000);
        var longError = new string('C', 3000);

        var header = new AdrHeader
        {
            Title = longTitle,
            Disclaimer = longDisclaimer,
            ErrorMessage = longError
        };

        // Act & Assert
        header.Title.Should().HaveLength(1000);
        header.Disclaimer.Should().HaveLength(2000);
        header.ErrorMessage.Should().HaveLength(3000);
    }

    [Fact]
    public void AdrHeader_AllStatuses_CanBeSet()
    {
        // Arrange & Act
        var statusValues = new[] 
        { 
            AdrStatus.Unknown, 
            AdrStatus.Proposed, 
            AdrStatus.Accepted, 
            AdrStatus.Rejected, 
            AdrStatus.Superseded 
        };

        foreach (var status in statusValues)
        {
            var header = new AdrHeader
            {
                StatusCreate = status,
                StatusUpdate = status,
                StatusChange = status
            };

            // Assert
            header.StatusCreate.Should().Be(status);
            header.StatusUpdate.Should().Be(status);
            header.StatusChange.Should().Be(status);
        }
    }

    [Fact]
    public void AdrHeader_WithNullableRevision_CanBeNull()
    {
        // Arrange & Act
        var header1 = new AdrHeader { Revision = null };
        var header2 = new AdrHeader { Revision = 5 };

        // Assert
        header1.Revision.Should().BeNull();
        header2.Revision.Should().Be(5);
    }

    [Fact]
    public void AdrHeader_WithNullableDates_CanBeNull()
    {
        // Arrange & Act
        var header1 = new AdrHeader { DateCreate = null, DateUpdate = null, DateChange = null };
        var now = DateTime.UtcNow;
        var header2 = new AdrHeader { DateCreate = now, DateUpdate = now, DateChange = now };

        // Assert
        header1.DateCreate.Should().BeNull();
        header1.DateUpdate.Should().BeNull();
        header1.DateChange.Should().BeNull();
        header2.DateCreate.Should().Be(now);
        header2.DateUpdate.Should().Be(now);
        header2.DateChange.Should().Be(now);
    }

    #endregion
}
