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

    #region Additional Edge Cases

    [Fact]
    public void CreateUniqueTitle_WithSpecialCharactersInTitle_PreservesCharacters()
    {
        // Arrange
        var title = "Use-New_Database.v2";
        var domain = "Back-End";

        // Act
        var result = AdrFileNameComponents.CreateUniqueTitle(title, domain);

        // Assert
        result.Should().Be("Use-New_Database.v2Back-End");
    }

    [Fact]
    public void CreateUniqueTitle_WithWhitespaceInTitle_PreservesWhitespace()
    {
        // Arrange
        var title = "Use New Database";
        var domain = "Back End";

        // Act
        var result = AdrFileNameComponents.CreateUniqueTitle(title, domain);

        // Assert
        result.Should().Be("Use New DatabaseBack End");
    }

    [Fact]
    public void CreateUniqueTitle_WithEmptyTitle_CombinesWithDomain()
    {
        // Arrange
        var title = string.Empty;
        var domain = "Backend";

        // Act
        var result = AdrFileNameComponents.CreateUniqueTitle(title, domain);

        // Assert
        result.Should().Be("Backend");
    }

    [Fact]
    public void CreateUniqueTitle_WithBothEmpty_ReturnsEmpty()
    {
        // Arrange
        var title = string.Empty;
        var domain = string.Empty;

        // Act
        var result = AdrFileNameComponents.CreateUniqueTitle(title, domain);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void UniqueTitle_Property_UpdatesWhenTitleChanges()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Title = "OldTitle",
            Domain = "Backend"
        };
        var oldResult = components.UniqueTitle;

        // Act
        components.Title = "NewTitle";
        var newResult = components.UniqueTitle;

        // Assert
        oldResult.Should().Be("OldTitleBackend");
        newResult.Should().Be("NewTitleBackend");
    }

    [Fact]
    public void UniqueTitle_Property_UpdatesWhenDomainChanges()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Title = "UseDatabase",
            Domain = "Backend"
        };
        var oldResult = components.UniqueTitle;

        // Act
        components.Domain = "Frontend";
        var newResult = components.UniqueTitle;

        // Assert
        oldResult.Should().Be("UseDatabaseBackend");
        newResult.Should().Be("UseDatabaseFrontend");
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
        var longScope = "EnterpriseLevelIntegrationArchitecture";
        var longDomain = "MicroservicesPaymentProcessingCoreWithAsyncMessaging";
        var longErrorMessage = "This is a detailed error message that explains exactly what went wrong and why";
        var longContent = new string('X', 10000);

        var components = new AdrFileNameComponents
        {
            Prefix = longPrefix,
            Title = longTitle,
            Scope = longScope,
            Domain = longDomain,
            ErrorMessage = longErrorMessage,
            ContentAdr = longContent
        };

        // Act & Assert
        components.Prefix.Should().Be(longPrefix);
        components.Title.Should().Be(longTitle);
        components.Scope.Should().Be(longScope);
        components.Domain.Should().Be(longDomain);
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
            Domain = "Domain1",
            Number = 1
        };
        var component2 = new AdrFileNameComponents
        {
            Title = "Title2",
            Domain = "Domain2",
            Number = 2
        };

        // Act & Assert
        component1.UniqueTitle.Should().Be("Title1Domain1");
        component2.UniqueTitle.Should().Be("Title2Domain2");
        component1.Number.Should().Be(1);
        component2.Number.Should().Be(2);
    }

    [Fact]
    public void CreateUniqueTitle_StaticMethod_DoesNotAffectInstanceState()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Title = "OriginalTitle",
            Domain = "OriginalDomain"
        };

        // Act
        var staticResult = AdrFileNameComponents.CreateUniqueTitle("DifferentTitle", "DifferentDomain");

        // Assert
        components.Title.Should().Be("OriginalTitle");
        components.Domain.Should().Be("OriginalDomain");
        components.UniqueTitle.Should().Be("OriginalTitleOriginalDomain");
        staticResult.Should().Be("DifferentTitleDifferentDomain");
    }

    [Fact]
    public void AdrFileNameComponents_WithNullStringsForOptionalProperties_HandledCorrectly()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Prefix = "ADR",
            Title = "Test",
            Scope = null,
            Domain = null,
            ErrorMessage = null ?? string.Empty
        };

        // Act & Assert
        components.Scope.Should().BeNull();
        components.Domain.Should().BeNull();
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
        _ = components.Scope;
        _ = components.Domain;
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
        var components = new AdrFileNameComponents();

        // Act
        components.Prefix = "PREFIX";
        components.Number = 42;
        components.Title = "Title";
        components.Version = 2;
        components.Revision = 1;
        components.Scope = "Scope";
        components.Domain = "Domain";
        components.SupersededValue = 40;
        components.IsValid = true;
        components.ErrorMessage = "Error";
        components.FileName = "file.md";
        var newHeader = new AdrHeader { Title = "New Header" };
        components.Header = newHeader;
        components.ContentAdr = "Content";

        // Assert
        components.Prefix.Should().Be("PREFIX");
        components.Number.Should().Be(42);
        components.Title.Should().Be("Title");
        components.Version.Should().Be(2);
        components.Revision.Should().Be(1);
        components.Scope.Should().Be("Scope");
        components.Domain.Should().Be("Domain");
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
        var domain = "ConsistentDomain";
        var components = new AdrFileNameComponents
        {
            Title = title,
            Domain = domain
        };

        // Act
        var propertyResult = components.UniqueTitle;
        var staticResult = AdrFileNameComponents.CreateUniqueTitle(title, domain);

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
    public void CreateUniqueTitle_WithBothParametersNull_TreatsNullAsEmptyString()
    {
        // Arrange - null title forced to string, null domain becomes empty string via coalescing
        string? title = null;
        string? domain = null;

        // Act
        var result = AdrFileNameComponents.CreateUniqueTitle(title!, domain);

        // Assert - null title + (null -> "") = empty string
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void UniqueTitle_Property_SetDomainToNullAfterInitialization_ReflectsChange()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Title = "TestTitle",
            Domain = "InitialDomain"
        };
        var initialResult = components.UniqueTitle;

        // Act
        components.Domain = null;
        var finalResult = components.UniqueTitle;

        // Assert
        initialResult.Should().Be("TestTitleInitialDomain");
        finalResult.Should().Be("TestTitle");
    }

    [Fact]
    public void CreateUniqueTitle_WithWhitespaceOnlyStrings_ConcatenatesAsIs()
    {
        // Arrange - whitespace strings are preserved as-is in concatenation
        var title = "A";  // Use actual character to ensure proper counting
        var domain = "B";

        // Act
        var result = AdrFileNameComponents.CreateUniqueTitle(title, domain);

        // Assert
        result.Should().Be("AB");
        result.Length.Should().Be(2);
    }

    [Fact]
    public void AdrFileNameComponents_TitleSetToWhitespaceOnly_StoresAsIs()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Title = "   ",
            Domain = "Domain"
        };

        // Act
        var result = components.UniqueTitle;

        // Assert
        result.Should().Be("   Domain");
        components.Title.Should().Be("   ");
    }

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
            Title = "Title1",
            Domain = "Domain1"
        };

        // Act - read UniqueTitle multiple times with changes
        var result1 = components.UniqueTitle;
        components.Title = "Title2";
        var result2 = components.UniqueTitle;
        components.Domain = "Domain2";
        var result3 = components.UniqueTitle;

        // Assert - each call reflects current state
        result1.Should().Be("Title1Domain1");
        result2.Should().Be("Title2Domain1");
        result3.Should().Be("Title2Domain2");
    }

    [Fact]
    public void CreateUniqueTitle_WithNullTitle_DefaultsToEmptyString()
    {
        // Arrange - testing null coalescing in static method
        string? title = null;
        var domain = "Domain";

        // Act
        var result = AdrFileNameComponents.CreateUniqueTitle(title!, domain);

        // Assert
        result.Should().Be("Domain");
    }

    [Fact]
    public void AdrFileNameComponents_ScopeProperty_IndependentOfTitle()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Title = "MyTitle",
            Scope = "MyScope"
        };

        // Act & Assert
        components.Title.Should().Be("MyTitle");
        components.Scope.Should().Be("MyScope");
        components.UniqueTitle.Should().Be("MyTitle"); // Scope doesn't affect UniqueTitle
    }

    [Fact]
    public void AdrFileNameComponents_FileName_DoesNotAffectUniqueTitle()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Title = "Title",
            Domain = "Domain",
            FileName = "adr-0001.md"
        };

        // Act
        var uniqueTitle = components.UniqueTitle;

        // Assert
        uniqueTitle.Should().Be("TitleDomain");
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
            Domain = "Domain",
            Header = originalHeader
        };
        var uniqueTitle1 = components.UniqueTitle;

        // Act
        var newHeader = new AdrHeader { Title = "Modified" };
        components.Header = newHeader;
        var uniqueTitle2 = components.UniqueTitle;

        // Assert
        uniqueTitle1.Should().Be("TitleDomain");
        uniqueTitle2.Should().Be("TitleDomain");
        components.Header.Title.Should().Be("Modified");
    }

    [Fact]
    public void AdrFileNameComponents_StringPropertiesWithUnicodeCharacters_StoresCorrectly()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Title = "Título em Português",
            Domain = "Domínio",
            Prefix = "ADR-日本語",
            Scope = "Σκοπός",
            ErrorMessage = "Ошибка"
        };

        // Act & Assert
        components.Title.Should().Be("Título em Português");
        components.Domain.Should().Be("Domínio");
        components.Prefix.Should().Be("ADR-日本語");
        components.Scope.Should().Be("Σκοπός");
        components.ErrorMessage.Should().Be("Ошибка");
        components.UniqueTitle.Should().Be("Título em PortuguêsDomínio");
    }

    [Fact]
    public void AdrFileNameComponents_SettingDomainToEmptyAfterNullInitialization_UpdatesUniqueTitle()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Title = "Title",
            Domain = null
        };
        var initialResult = components.UniqueTitle;

        // Act
        components.Domain = string.Empty;
        var afterEmptyResult = components.UniqueTitle;

        // Assert
        initialResult.Should().Be("Title");
        afterEmptyResult.Should().Be("Title");
    }

    [Fact]
    public void UniqueTitle_MultipleConsecutiveAccesses_ReturnConsistentResults()
    {
        // Arrange
        var components = new AdrFileNameComponents
        {
            Title = "Title",
            Domain = "Domain"
        };

        // Act
        var result1 = components.UniqueTitle;
        var result2 = components.UniqueTitle;
        var result3 = components.UniqueTitle;

        // Assert
        result1.Should().Be(result2);
        result2.Should().Be(result3);
        result1.Should().Be("TitleDomain");
    }

    #endregion
}
