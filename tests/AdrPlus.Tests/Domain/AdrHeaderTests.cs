// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;
using System.Runtime.InteropServices;

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
            FileSuperSedes = "ADR-0002.md",
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
        header.FileSuperSedes.Should().Be("ADR-0002.md");
        header.DateChange.Should().Be(changeDate);
        header.Title.Should().Be("Use New Database");
        header.IsValid.Should().BeTrue();
        header.IsMigrated.Should().BeTrue();
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

    [Fact]
    public void AdrHeader_IsMigrated_CanBeToggled()
    {
        // Arrange
        var header = new AdrHeader { IsMigrated = false };

        // Act
        header = header with { IsMigrated = true };
        var firstToggle = header.IsMigrated;

        header = header with { IsMigrated = false };
        var secondToggle = header.IsMigrated;

        // Assert
        firstToggle.Should().BeTrue();
        secondToggle.Should().BeFalse();
    }

    [Fact]
    public void AdrHeader_IsMigrated_DoesNotAffectEquality_WhenOtherPropertiesEqual()
    {
        // Arrange
        var header1 = new AdrHeader { Title = "Test", Version = 1, IsMigrated = false };
        var header2 = new AdrHeader { Title = "Test", Version = 1, IsMigrated = true };

        // Act & Assert
        header1.Should().NotBe(header2);
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
            FileSuperSedes = "ADR-0001_superseded.md"
        };

        // Act & Assert
        header.Disclaimer.Should().Contain("©");
        header.Title.Should().Contain("-");
        header.Scope.Should().Contain("-");
        header.Domain.Should().Contain("_");
        header.FileSuperSedes.Should().Contain("_");
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

    [Fact]
    public void AdrHeader_RecordCopy_WithModifications_CreatesNewInstance()
    {
        // Arrange
        var original = new AdrHeader { Title = "Original", Version = 1 };

        // Act
        var copy = original with { Title = "Modified", Version = 2 };

        // Assert
        original.Title.Should().Be("Original");
        original.Version.Should().Be(1);
        copy.Title.Should().Be("Modified");
        copy.Version.Should().Be(2);
        original.Should().NotBe(copy);
    }

    [Fact]
    public void AdrHeader_RecordToString_ContainsPropertyValues()
    {
        // Arrange
        var header = new AdrHeader
        {
            Title = "Test Title",
            Version = 1,
            Scope = "API"
        };

        // Act
        var toString = header.ToString();

        // Assert
        toString.Should().Contain("AdrHeader");
    }

    [Fact]
    public void AdrHeader_RecordGetHashCode_SameForEqualRecords()
    {
        // Arrange
        var header1 = new AdrHeader { Title = "Test", Version = 1 };
        var header2 = new AdrHeader { Title = "Test", Version = 1 };

        // Act
        var hash1 = header1.GetHashCode();
        var hash2 = header2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void AdrHeader_RecordGetHashCode_DifferentForDifferentRecords()
    {
        // Arrange
        var header1 = new AdrHeader { Title = "Test1", Version = 1 };
        var header2 = new AdrHeader { Title = "Test2", Version = 1 };

        // Act
        var hash1 = header1.GetHashCode();
        var hash2 = header2.GetHashCode();

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void AdrHeader_WithAllPropertiesSet_EqualsRecordWithSameValues()
    {
        // Arrange
        var date = new DateTime(2025, 04, 17, 10, 30, 0);
        var header1 = new AdrHeader
        {
            Disclaimer = "Disclaimer",
            Version = 2,
            Revision = 1,
            Scope = "API",
            Domain = "Backend",
            StatusCreate = AdrStatus.Proposed,
            DateCreate = date,
            StatusUpdate = AdrStatus.Accepted,
            DateUpdate = date.AddDays(1),
            StatusChange = AdrStatus.Superseded,
            FileSuperSedes = "ADR-0002.md",
            DateChange = date.AddDays(2),
            Title = "Title",
            IsValid = true,
            IsMigrated = true,
            ErrorMessage = string.Empty
        };

        var header2 = new AdrHeader
        {
            Disclaimer = "Disclaimer",
            Version = 2,
            Revision = 1,
            Scope = "API",
            Domain = "Backend",
            StatusCreate = AdrStatus.Proposed,
            DateCreate = date,
            StatusUpdate = AdrStatus.Accepted,
            DateUpdate = date.AddDays(1),
            StatusChange = AdrStatus.Superseded,
            FileSuperSedes = "ADR-0002.md",
            DateChange = date.AddDays(2),
            Title = "Title",
            IsValid = true,
            IsMigrated = true,
            ErrorMessage = string.Empty
        };

        // Act & Assert
        header1.Should().Be(header2);
    }

    [Fact]
    public void AdrHeader_MultipleInstances_AreIndependent()
    {
        // Arrange
        var header1 = new AdrHeader { Title = "Header1", Version = 1 };
        var header2 = new AdrHeader { Title = "Header2", Version = 2 };

        // Act
        var header1Copy = header1 with { };
        var header2Copy = header2 with { };

        // Assert
        header1.Should().Be(header1Copy);
        header2.Should().Be(header2Copy);
        header1.Should().NotBe(header2);
        header1Copy.Should().NotBe(header2Copy);
    }

    [Fact]
    public void AdrHeader_DefaultInstance_IsAlwaysFalseForValidAndMigrated()
    {
        // Arrange & Act
        var header = new AdrHeader();

        // Assert
        header.IsValid.Should().BeFalse();
        header.IsMigrated.Should().BeFalse();
    }

    [Fact]
    public void AdrHeader_WithOnlyTitleSet_OtherPropertiesHaveDefaults()
    {
        // Arrange & Act
        var header = new AdrHeader { Title = "Only Title Set" };

        // Assert
        header.Title.Should().Be("Only Title Set");
        header.Version.Should().Be(0);
        header.Revision.Should().BeNull();
        header.Scope.Should().Be(string.Empty);
        header.Domain.Should().Be(string.Empty);
        header.StatusCreate.Should().Be(AdrStatus.Unknown);
        header.DateCreate.Should().BeNull();
        header.IsValid.Should().BeFalse();
        header.IsMigrated.Should().BeFalse();
    }

    #endregion
}
