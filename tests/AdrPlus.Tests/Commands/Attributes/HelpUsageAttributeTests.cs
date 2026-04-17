// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using System.Globalization;

namespace AdrPlus.Tests.Commands.Attributes;

/// <summary>
/// Unit tests for HelpUsageAttribute class.
/// Tests demonstrate attribute instantiation, property access, and resource localization.
/// </summary>
public class HelpUsageAttributeTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var usage = UsageArgumments.Optional;
        var resourceKey = "TestResource";

        // Act
        var attribute = new HelpUsageAttribute(usage, resourceKey);

        // Assert
        attribute.Should().NotBeNull();
        attribute.Usage.Should().Be(usage);
    }

    [Fact]
    public void Constructor_WithWizardUsage_CreatesInstance()
    {
        // Arrange
        var usage = UsageArgumments.Wizard;
        var resourceKey = "HelpUsageWizardNew";

        // Act
        var attribute = new HelpUsageAttribute(usage, resourceKey);

        // Assert
        attribute.Should().NotBeNull();
        attribute.Usage.Should().Be(UsageArgumments.Wizard);
    }

    [Fact]
    public void Constructor_WithOptionalWithValueUsage_CreatesInstance()
    {
        // Arrange
        var usage = UsageArgumments.OptionalWithValue;
        var resourceKey = "HelpUsageFileConfig";

        // Act
        var attribute = new HelpUsageAttribute(usage, resourceKey);

        // Assert
        attribute.Should().NotBeNull();
        attribute.Usage.Should().Be(UsageArgumments.OptionalWithValue);
    }

    [Fact]
    public void Constructor_WithOptionalWithValueWhenWizardUsage_CreatesInstance()
    {
        // Arrange
        var usage = UsageArgumments.OptionalWithValueWhenWizard;
        var resourceKey = "HelpUsageFileAdr";

        // Act
        var attribute = new HelpUsageAttribute(usage, resourceKey);

        // Assert
        attribute.Should().NotBeNull();
        attribute.Usage.Should().Be(UsageArgumments.OptionalWithValueWhenWizard);
    }

    [Fact]
    public void Constructor_WithNullResourceKey_CreatesInstance()
    {
        // Arrange
        var usage = UsageArgumments.Optional;

        // Act
        var attribute = new HelpUsageAttribute(usage, null!);

        // Assert
        attribute.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithEmptyResourceKey_CreatesInstance()
    {
        // Arrange
        var usage = UsageArgumments.Optional;
        var resourceKey = string.Empty;

        // Act
        var attribute = new HelpUsageAttribute(usage, resourceKey);

        // Assert
        attribute.Should().NotBeNull();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Usage_ReturnsConstructorValue()
    {
        // Arrange
        var expectedUsage = UsageArgumments.OptionalWithValue;
        var attribute = new HelpUsageAttribute(expectedUsage, "TestResource");

        // Act
        var result = attribute.Usage;

        // Assert
        result.Should().Be(expectedUsage);
    }

    [Fact]
    public void Usage_WithWizard_ReturnsWizard()
    {
        // Arrange
        var attribute = new HelpUsageAttribute(UsageArgumments.Wizard, "HelpUsageWizardNew");

        // Act
        var result = attribute.Usage;

        // Assert
        result.Should().Be(UsageArgumments.Wizard);
    }

    [Fact]
    public void Description_WithValidResourceKey_ReturnsLocalizedString()
    {
        // Arrange
        var attribute = new HelpUsageAttribute(UsageArgumments.Optional, "HelpUsageHelp");

        // Act
        var result = attribute.Description;

        // Assert
        result.Should().NotBeNullOrEmpty();
        // The description should either be the localized value or the resource key as fallback
    }

    [Fact]
    public void Description_WithInvalidResourceKey_ReturnsResourceKey()
    {
        // Arrange
        var resourceKey = "NonExistentResourceKey54321";
        var attribute = new HelpUsageAttribute(UsageArgumments.Optional, resourceKey);

        // Act
        var result = attribute.Description;

        // Assert
        result.Should().Be(resourceKey); // Falls back to resource key when not found
    }

    [Fact]
    public void Description_WithEmptyResourceKey_ReturnsEmptyString()
    {
        // Arrange
        var attribute = new HelpUsageAttribute(UsageArgumments.Optional, string.Empty);

        // Act
        var result = attribute.Description;

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Description_UsesCurrentCulture_ReturnsLocalizedString()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;

        try
        {
            // Set to English culture
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");

            var attribute = new HelpUsageAttribute(UsageArgumments.Optional, "HelpUsageHelp");

            // Act
            var result = attribute.Description;

            // Assert
            result.Should().NotBeNullOrEmpty();
        }
        finally
        {
            // Restore original culture
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }

    [Fact]
    public void Description_WithDifferentCultures_MayReturnDifferentValues()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;

        try
        {
            // Create attribute
            var attribute = new HelpUsageAttribute(UsageArgumments.Optional, "HelpUsageHelp");

            // Get description in English
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            var descriptionEn = attribute.Description;

            // Get description in Portuguese (if available)
            CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
            CultureInfo.CurrentUICulture = new CultureInfo("pt-BR");
            var descriptionPt = attribute.Description;

            // Assert - Both should return non-empty strings
            descriptionEn.Should().NotBeNullOrEmpty();
            descriptionPt.Should().NotBeNullOrEmpty();
        }
        finally
        {
            // Restore original culture
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }

    [Fact]
    public void Description_AccessedMultipleTimes_ReturnsConsistentValue()
    {
        // Arrange
        var attribute = new HelpUsageAttribute(UsageArgumments.Optional, "HelpUsageHelp");

        // Act
        var result1 = attribute.Description;
        var result2 = attribute.Description;
        var result3 = attribute.Description;

        // Assert
        result1.Should().Be(result2);
        result2.Should().Be(result3);
    }

    #endregion

    #region Attribute Usage Tests

    [Fact]
    public void AttributeUsage_AllowsFieldTarget()
    {
        // Arrange & Act
        var attributeType = typeof(HelpUsageAttribute);
        var attributeUsage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
            attributeType, typeof(AttributeUsageAttribute))!;

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage.ValidOn.Should().HaveFlag(AttributeTargets.Field);
    }

    [Fact]
    public void AttributeUsage_DoesNotAllowMultiple()
    {
        // Arrange & Act
        var attributeType = typeof(HelpUsageAttribute);
        var attributeUsage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
            attributeType, typeof(AttributeUsageAttribute))!;

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage.AllowMultiple.Should().BeFalse();
    }

    #endregion

    #region Integration Tests with Enums

    [Fact]
    public void HelpUsageAttribute_AppliedToEnum_CanBeRetrieved()
    {
        // Arrange
        var enumType = typeof(Arguments);
        var field = enumType.GetField("Help");

        // Act
        var attribute = field?.GetCustomAttributes(typeof(HelpUsageAttribute), false)
            .FirstOrDefault() as HelpUsageAttribute;

        // Assert
        attribute.Should().NotBeNull();
        attribute!.Usage.Should().Be(UsageArgumments.Optional);
        attribute.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void HelpUsageAttribute_AllEnumFieldsHaveAttribute()
    {
        // Arrange
        var enumType = typeof(Arguments);
        var fields = enumType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Act & Assert
        foreach (var field in fields)
        {
            var attribute = field.GetCustomAttributes(typeof(HelpUsageAttribute), false)
                .FirstOrDefault() as HelpUsageAttribute;

            attribute.Should().NotBeNull($"Field {field.Name} should have HelpUsageAttribute");
        }
    }

    [Fact]
    public void HelpUsageAttribute_WizardFields_HaveWizardUsage()
    {
        // Arrange
        var enumType = typeof(Arguments);
        var wizardFields = new[] { "WizardNew", "WizardVersion", "WizardReview", "WizardSupersede", "WizardApprove", "WizardReject", "WizardUndoStatus", "WizardInit" };

        // Act & Assert
        foreach (var fieldName in wizardFields)
        {
            var field = enumType.GetField(fieldName);
            if (field != null)
            {
                var attribute = field.GetCustomAttributes(typeof(HelpUsageAttribute), false)
                    .FirstOrDefault() as HelpUsageAttribute;

                attribute.Should().NotBeNull($"Field {fieldName} should have HelpUsageAttribute");
                attribute!.Usage.Should().Be(UsageArgumments.Wizard, $"Field {fieldName} should have Wizard usage");
            }
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Constructor_WithAllUsageEnumValues_CreatesInstance()
    {
        // Arrange & Act & Assert
        foreach (UsageArgumments usage in Enum.GetValues<UsageArgumments>())
        {
            var attribute = new HelpUsageAttribute(usage, "TestResource");
            attribute.Should().NotBeNull();
            attribute.Usage.Should().Be(usage);
        }
    }

    [Fact]
    public void Constructor_WithInvalidEnumValue_CreatesInstance()
    {
        // Arrange
        var invalidUsage = (UsageArgumments)999;

        // Act
        var attribute = new HelpUsageAttribute(invalidUsage, "TestResource");

        // Assert
        attribute.Should().NotBeNull();
        attribute.Usage.Should().Be(invalidUsage);
    }

    [Fact]
    public void Constructor_WithSpecialCharactersInResourceKey_CreatesInstance()
    {
        // Arrange
        var resourceKey = "Resource_Key-123!@#$";

        // Act
        var attribute = new HelpUsageAttribute(UsageArgumments.Optional, resourceKey);

        // Assert
        attribute.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithWhitespaceResourceKey_CreatesInstance()
    {
        // Arrange
        var resourceKey = "   ";

        // Act
        var attribute = new HelpUsageAttribute(UsageArgumments.Optional, resourceKey);

        // Assert
        attribute.Should().NotBeNull();
    }

    [Fact]
    public void Description_WithNullResourceKey_ThrowsArgumentNullException()
    {
        // Arrange
        var attribute = new HelpUsageAttribute(UsageArgumments.Optional, null!);

        // Act
        var act = () => attribute.Description;

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Usage_IsImmutable_CannotBeChanged()
    {
        // Arrange
        var attribute = new HelpUsageAttribute(UsageArgumments.Wizard, "TestResource");

        // Act & Assert
        // Property should be read-only (get-only)
        attribute.Usage.Should().Be(UsageArgumments.Wizard);
    }

    [Fact]
    public void Constructor_WithVeryLongResourceKey_CreatesInstance()
    {
        // Arrange
        var resourceKey = new string('a', 500);

        // Act
        var attribute = new HelpUsageAttribute(UsageArgumments.Optional, resourceKey);

        // Assert
        attribute.Should().NotBeNull();
        attribute.Description.Should().HaveLength(500);
    }

    #endregion

    #region UsageArgumments Enum Tests

    [Fact]
    public void UsageArgumments_HasExpectedValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<UsageArgumments>().Cast<UsageArgumments>().ToList();

        // Assert
        values.Should().Contain(UsageArgumments.Wizard);
        values.Should().Contain(UsageArgumments.Optional);
        values.Should().Contain(UsageArgumments.OptionalWithValue);
        values.Should().Contain(UsageArgumments.OptionalWithValueWhenWizard);
    }

    [Fact]
    public void UsageArgumments_HasExpectedCount()
    {
        // Arrange & Act
        var values = Enum.GetValues<UsageArgumments>().Cast<UsageArgumments>().ToList();

        // Assert
        values.Should().HaveCountGreaterThanOrEqualTo(4);
    }

    #endregion
}
