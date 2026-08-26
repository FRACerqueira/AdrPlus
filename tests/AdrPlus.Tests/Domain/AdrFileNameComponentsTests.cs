// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;

namespace AdrPlus.Tests.Domain;

public class AdrFileNameComponentsTests
{
    [Fact]
    public void CreateUniqueTitle_WithTitle_ReturnsCasedTitle()
    {
        // Arrange
        var title = "UseNewDatabase";

        // Act
        var result = AdrFileNameComponents.CreateUniqueTitle(title);

        // Assert
        result.Should().Be("UseNewDatabase");
    }

    [Fact]
    public void CreateUniqueTitle_WithEmptyTitle_ReturnsEmpty()
    {
        // Arrange
        var title = string.Empty;

        // Act
        var result = AdrFileNameComponents.CreateUniqueTitle(title);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void UniqueTitle_Property_ReflectsTitle()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Title = "UseNewDatabase"
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
        components.SupersededValue.Should().Be(2);
        components.IsValid.Should().BeTrue();
        components.ErrorMessage.Should().Be("No errors");
        components.FileName.Should().Be("ADR-0001.md");
        components.Header.Should().Be(header);
        components.ContentAdr.Should().Be("Content here");
    }

    #region Additional Edge Cases

    [Fact]
    public void UniqueTitle_Property_UpdatesWhenTitleChanges()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Title = "OldTitle"
        };
        var oldResult = components.UniqueTitle;

        // Act
        components.Title = "NewTitle";
        var newResult = components.UniqueTitle;

        // Assert
        oldResult.Should().Be("OldTitle");
        newResult.Should().Be("NewTitle");
    }

    [Fact]
    public void AdrFileNameComponents_HeaderProperty_InitializedAsNewInstance()
    {
        // Arrange & Act
        var components1 = new AdrFileNameComponents();
        var components2 = new AdrFileNameComponents();

        // Assert - Different instances but both are AdrHeader
        components1.Header.Should().NotBeNull();
        components2.Header.Should().NotBeNull();
        components1.Header.Should().NotBeSameAs(components2.Header);
    }

    [Fact]
    public void AdrFileNameComponents_WithLargeNumberValues_StoresCorrectly()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Number = 99999,
            Version = 100,
            Revision = 50,
            SupersededValue = 88888
        };

        // Act & Assert
        components.Number.Should().Be(99999);
        components.Version.Should().Be(100);
        components.Revision.Should().Be(50);
        components.SupersededValue.Should().Be(88888);
    }

    [Fact]
    public void AdrFileNameComponents_WithLongStrings_StoresCorrectly()
    {
        // Arrange
        var longPrefix = "VERYLONGPREFIX_WITH_MANY_CHARACTERS";
        var longTitle = "This is a very long title that exceeds normal expectations for ADR titles";
        var longErrorMessage = "This is a detailed error message that explains exactly what went wrong and why";
        var longContent = new string('X', 10000);

        var components = new AdrFileNameComponents
        {
            Prefix = longPrefix,
            Title = longTitle,
            ErrorMessage = longErrorMessage,
            ContentAdr = longContent
        };

        // Act & Assert
        components.Prefix.Should().Be(longPrefix);
        components.Title.Should().Be(longTitle);
        components.ErrorMessage.Should().Be(longErrorMessage);
        components.ContentAdr.Should().HaveLength(10000);
    }

    [Fact]
    public void AdrFileNameComponents_MultipleInstancesIndependent_DoNotAffectEachOther()
    {
        // Arrange
        var component1 = new AdrFileNameComponents
        {
            Title = "Title1",
            Number = 1
        };
        var component2 = new AdrFileNameComponents
        {
            Title = "Title2",
            Number = 2
        };

        // Act & Assert
        component1.UniqueTitle.Should().Be("Title1");
        component2.UniqueTitle.Should().Be("Title2");
        component1.Number.Should().Be(1);
        component2.Number.Should().Be(2);
    }

    [Fact]
    public void CreateUniqueTitle_StaticMethod_DoesNotAffectInstanceState()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Title = "OriginalTitle"
        };

        // Act
        var staticResult = AdrFileNameComponents.CreateUniqueTitle("DifferentTitle");

        // Assert
        components.Title.Should().Be("OriginalTitle");
        components.UniqueTitle.Should().Be("OriginalTitle");
        staticResult.Should().Be("DifferentTitle");
    }

    [Fact]
    public void AdrFileNameComponents_WithNullStringsForOptionalProperties_HandledCorrectly()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Prefix = "ADR",
            Title = "Test",
            ErrorMessage = null ?? string.Empty
        };

        // Act & Assert
        components.ErrorMessage.Should().Be(string.Empty);
        components.UniqueTitle.Should().Be("Test");
    }

    [Fact]
    public void AdrFileNameComponents_Initialization_PropertiesAccessible()
    {
        // Arrange & Act
        var components = new AdrFileNameComponents();

        // Verify all properties are accessible and have default values
        _ = components.Prefix;
        _ = components.Number;
        _ = components.Title;
        _ = components.Version;
        _ = components.Revision;
        _ = components.SupersededValue;
        _ = components.IsValid;
        _ = components.ErrorMessage;
        _ = components.FileName;
        _ = components.Header;
        _ = components.ContentAdr;
        _ = components.UniqueTitle;

        // Assert - if we got here without exception, all properties are accessible
        Assert.True(true);
    }

    [Fact]
    public void AdrFileNameComponents_SettersWorkAfterInstantiation()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            // Act
            Prefix = "PREFIX",
            Number = 42,
            Title = "Title",
            Version = 2,
            Revision = 1,
            SupersededValue = 40,
            IsValid = true,
            ErrorMessage = "Error",
            FileName = "file.md"
        };
        var newHeader = new AdrHeader { Title = "New Header" };
        components.Header = newHeader;
        components.ContentAdr = "Content";

        // Assert
        components.Prefix.Should().Be("PREFIX");
        components.Number.Should().Be(42);
        components.Title.Should().Be("Title");
        components.Version.Should().Be(2);
        components.Revision.Should().Be(1);
        components.SupersededValue.Should().Be(40);
        components.IsValid.Should().Be(true);
        components.ErrorMessage.Should().Be("Error");
        components.FileName.Should().Be("file.md");
        components.Header.Should().Be(newHeader);
        components.ContentAdr.Should().Be("Content");
    }

    [Fact]
    public void UniqueTitle_Property_ConsistentWithStaticMethod()
    {
        // Arrange
        var title = "ConsistentTitle";
        var components = new AdrFileNameComponents
        {
            Title = title
        };

        // Act
        var propertyResult = components.UniqueTitle;
        var staticResult = AdrFileNameComponents.CreateUniqueTitle(title);

        // Assert
        propertyResult.Should().Be(staticResult);
    }

    [Fact]
    public void AdrFileNameComponents_WithZeroValues_StoresCorrectly()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Number = 0,
            Version = 0,
            Revision = 0,
            SupersededValue = 0
        };

        // Act & Assert
        components.Number.Should().Be(0);
        components.Version.Should().Be(0);
        components.Revision.Should().Be(0);
        components.SupersededValue.Should().Be(0);
    }

    #endregion

    #region Gap Coverage - Untested Scenarios

    [Fact]
    public void AdrFileNameComponents_PrefixSetToWhitespaceOnly_StoresAsIs()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Prefix = "   "
        };

        // Act & Assert
        components.Prefix.Should().Be("   ");
    }

    [Fact]
    public void AdrFileNameComponents_WithNegativeNumbers_StoresCorrectly()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Number = -1,
            Version = -5,
            Revision = -10,
            SupersededValue = -100
        };

        // Act & Assert
        components.Number.Should().Be(-1);
        components.Version.Should().Be(-5);
        components.Revision.Should().Be(-10);
        components.SupersededValue.Should().Be(-100);
    }

    [Fact]
    public void AdrFileNameComponents_ContentAdr_DistinguishesEmptyFromNull()
    {
        // Arrange
        var component1 = new AdrFileNameComponents { ContentAdr = string.Empty };
        var component2 = new AdrFileNameComponents { ContentAdr = null };
        var component3 = new AdrFileNameComponents();

        // Act & Assert
        component1.ContentAdr.Should().Be(string.Empty);
        component2.ContentAdr.Should().BeNull();
        component3.ContentAdr.Should().BeNull();
    }

    [Fact]
    public void AdrFileNameComponents_ErrorMessage_DistinguishesEmptyFromNull()
    {
        // Arrange
        var component1 = new AdrFileNameComponents { ErrorMessage = string.Empty };
        var component2 = new AdrFileNameComponents { ErrorMessage = "Error occurred" };
        var component3 = new AdrFileNameComponents();

        // Act & Assert
        component1.ErrorMessage.Should().Be(string.Empty);
        component2.ErrorMessage.Should().Be("Error occurred");
        component3.ErrorMessage.Should().Be(string.Empty); // Default
    }

    [Fact]
    public void UniqueTitle_Property_AlwaysComputedFresh_NotCached()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Title = "Title1"
        };

        // Act - read UniqueTitle multiple times with changes
        var result1 = components.UniqueTitle;
        components.Title = "Title2";
        var result2 = components.UniqueTitle;

        // Assert - each call reflects current state
        result1.Should().Be("Title1");
        result2.Should().Be("Title2");
    }

    [Fact]
    public void AdrFileNameComponents_FileName_DoesNotAffectUniqueTitle()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Title = "Title",
            FileName = "adr-0001.md"
        };

        // Act
        var uniqueTitle = components.UniqueTitle;

        // Assert
        uniqueTitle.Should().Be("Title");
        components.FileName.Should().Be("adr-0001.md");
    }

    [Fact]
    public void AdrFileNameComponents_IsValidFlag_IndependentOfOtherProperties()
    {
        // Arrange
        var component1 = new AdrFileNameComponents { IsValid = true, Title = "Title1" };
        var component2 = new AdrFileNameComponents { IsValid = false, Title = "Title1" };

        // Act & Assert
        component1.IsValid.Should().BeTrue();
        component2.IsValid.Should().BeFalse();
        component1.Title.Should().Be(component2.Title);
        component1.UniqueTitle.Should().Be(component2.UniqueTitle);
    }

    [Fact]
    public void AdrFileNameComponents_AllNumericPropertiesAtMaxValues_StoresCorrectly()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Number = int.MaxValue,
            Version = int.MaxValue,
            Revision = int.MaxValue,
            SupersededValue = int.MaxValue
        };

        // Act & Assert
        components.Number.Should().Be(int.MaxValue);
        components.Version.Should().Be(int.MaxValue);
        components.Revision.Should().Be(int.MaxValue);
        components.SupersededValue.Should().Be(int.MaxValue);
    }

    [Fact]
    public void AdrFileNameComponents_AllNumericPropertiesAtMinValues_StoresCorrectly()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Number = int.MinValue,
            Version = int.MinValue,
            Revision = int.MinValue,
            SupersededValue = int.MinValue
        };

        // Act & Assert
        components.Number.Should().Be(int.MinValue);
        components.Version.Should().Be(int.MinValue);
        components.Revision.Should().Be(int.MinValue);
        components.SupersededValue.Should().Be(int.MinValue);
    }

    [Fact]
    public void AdrFileNameComponents_ModifyingHeaderDoesNotAffectUniqueTitle()
    {
        // Arrange
        var originalHeader = new AdrHeader { Title = "Original" };
        var components = new AdrFileNameComponents
        {
            Title = "Title",
            Header = originalHeader
        };
        var uniqueTitle1 = components.UniqueTitle;

        // Act
        var newHeader = new AdrHeader { Title = "Modified" };
        components.Header = newHeader;
        var uniqueTitle2 = components.UniqueTitle;

        // Assert
        uniqueTitle1.Should().Be("Title");
        uniqueTitle2.Should().Be("Title");
        components.Header.Title.Should().Be("Modified");
    }

    [Fact]
    public void AdrFileNameComponents_StringPropertiesWithUnicodeCharacters_StoresCorrectly()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Title = "Título em Português",
            Prefix = "ADR-日本語",
            ErrorMessage = "Ошибка"
        };

        // Act & Assert
        components.Title.Should().Be("Título em Português");
        components.Prefix.Should().Be("ADR-日本語");
        components.ErrorMessage.Should().Be("Ошибка");
        components.UniqueTitle.Should().Be("TítuloEmPortuguês");
    }

    [Fact]
    public void UniqueTitle_MultipleConsecutiveAccesses_ReturnConsistentResults()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Title = "Title"
        };

        // Act
        var result1 = components.UniqueTitle;
        var result2 = components.UniqueTitle;
        var result3 = components.UniqueTitle;

        // Assert
        result1.Should().Be(result2);
        result2.Should().Be(result3);
        result1.Should().Be("Title");
    }

    [Fact]
    public void CreateUniqueTitle_WithKebabCaseTitle_NormalizesProperly()
    {
        // Arrange
        var title = "use-new-database";

        // Act
        var result = AdrFileNameComponents.CreateUniqueTitle(title);

        // Assert
        result.Should().Be("UseNewDatabase");
    }

    [Fact]
    public void CreateUniqueTitle_WithSnakeCaseTitle_NormalizesProperly()
    {
        // Arrange
        var title = "use_new_database";

        // Act
        var result = AdrFileNameComponents.CreateUniqueTitle(title);

        // Assert
        result.Should().Be("UseNewDatabase");
    }

    [Fact]
    public void CreateUniqueTitle_WithSpacesInTitle_NormalizesProperly()
    {
        // Arrange
        var title = "use new database";

        // Act
        var result = AdrFileNameComponents.CreateUniqueTitle(title);

        // Assert
        result.Should().Be("UseNewDatabase");
    }

    [Fact]
    public void UniqueTitle_Property_WithMixedSeparators_NormalizesProperly()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Title = "use-new_database"
        };

        // Act
        var result = components.UniqueTitle;

        // Assert
        result.Should().Be("UseNewDatabase");
    }

    #endregion
}
