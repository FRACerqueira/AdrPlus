// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Commands.Help;
using System.Globalization;

namespace AdrPlus.Tests.Commands.Attributes;

/// <summary>
/// Unit tests for CommandAttribute class.
/// Tests demonstrate attribute instantiation, property access, and resource localization.
/// </summary>
public class CommandAttributeTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var alias = "test";
        var handlerType = typeof(HelpCommandHandler);
        var resourceKey = "TestResource";

        // Act
        var attribute = new CommandAttribute(alias, handlerType, resourceKey);

        // Assert
        attribute.Should().NotBeNull();
        attribute.AliasCommand.Should().Be(alias);
        attribute.HandlerCommand.Should().Be(handlerType);
    }

    [Fact]
    public void Constructor_WithNullAlias_CreatesInstanceWithNullAlias()
    {
        // Arrange
        var handlerType = typeof(HelpCommandHandler);
        var resourceKey = "TestResource";

        // Act
        var attribute = new CommandAttribute(null!, handlerType, resourceKey);

        // Assert
        attribute.Should().NotBeNull();
        attribute.AliasCommand.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithNullHandlerType_CreatesInstanceWithNullHandler()
    {
        // Arrange
        var alias = "test";
        var resourceKey = "TestResource";

        // Act
        var attribute = new CommandAttribute(alias, null!, resourceKey);

        // Assert
        attribute.Should().NotBeNull();
        attribute.HandlerCommand.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithNullResourceKey_CreatesInstanceWithNullResourceKey()
    {
        // Arrange
        var alias = "test";
        var handlerType = typeof(HelpCommandHandler);

        // Act
        var attribute = new CommandAttribute(alias, handlerType, null!);

        // Assert
        attribute.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithEmptyStrings_CreatesInstance()
    {
        // Arrange
        var alias = string.Empty;
        var handlerType = typeof(HelpCommandHandler);
        var resourceKey = string.Empty;

        // Act
        var attribute = new CommandAttribute(alias, handlerType, resourceKey);

        // Assert
        attribute.Should().NotBeNull();
        attribute.AliasCommand.Should().BeEmpty();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void AliasCommand_ReturnsConstructorValue()
    {
        // Arrange
        var expectedAlias = "help";
        var attribute = new CommandAttribute(expectedAlias, typeof(HelpCommandHandler), "CmdDescHelp");

        // Act
        var result = attribute.AliasCommand;

        // Assert
        result.Should().Be(expectedAlias);
    }

    [Fact]
    public void HandlerCommand_ReturnsConstructorValue()
    {
        // Arrange
        var expectedType = typeof(HelpCommandHandler);
        var attribute = new CommandAttribute("help", expectedType, "CmdDescHelp");

        // Act
        var result = attribute.HandlerCommand;

        // Assert
        result.Should().Be(expectedType);
    }

    [Fact]
    public void Description_WithValidResourceKey_ReturnsLocalizedString()
    {
        // Arrange
        var attribute = new CommandAttribute("help", typeof(HelpCommandHandler), "CmdDescHelp");

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
        var resourceKey = "NonExistentResourceKey12345";
        var attribute = new CommandAttribute("test", typeof(HelpCommandHandler), resourceKey);

        // Act
        var result = attribute.Description;

        // Assert
        result.Should().Be(resourceKey); // Falls back to resource key when not found
    }

    [Fact]
    public void Description_WithEmptyResourceKey_ReturnsEmptyString()
    {
        // Arrange
        var attribute = new CommandAttribute("test", typeof(HelpCommandHandler), string.Empty);

        // Act
        var result = attribute.Description;

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Description_UsesCurrentUICulture_ReturnsLocalizedString()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;

        try
        {
            // Set to English culture
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");

            var attribute = new CommandAttribute("help", typeof(HelpCommandHandler), "CmdDescHelp");

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
    public void Description_AccessedMultipleTimes_ReturnsConsistentValue()
    {
        // Arrange
        var attribute = new CommandAttribute("help", typeof(HelpCommandHandler), "CmdDescHelp");

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
        var attributeType = typeof(CommandAttribute);
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
        var attributeType = typeof(CommandAttribute);
        var attributeUsage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
            attributeType, typeof(AttributeUsageAttribute))!;

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage.AllowMultiple.Should().BeFalse();
    }

    #endregion

    #region Integration Tests with Enums

    [Fact]
    public void CommandAttribute_AppliedToEnum_CanBeRetrieved()
    {
        // Arrange
        var enumType = typeof(CommandsAdr);
        var field = enumType.GetField("Help");

        // Act
        var attribute = field?.GetCustomAttributes(typeof(CommandAttribute), false)
            .FirstOrDefault() as CommandAttribute;

        // Assert
        attribute.Should().NotBeNull();
        attribute!.AliasCommand.Should().NotBeNullOrEmpty();
        attribute.HandlerCommand.Should().NotBeNull();
    }

    [Fact]
    public void CommandAttribute_AllEnumFieldsHaveAttribute()
    {
        // Arrange
        var enumType = typeof(CommandsAdr);
        var fields = enumType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Act & Assert
        foreach (var field in fields)
        {
            var attribute = field.GetCustomAttributes(typeof(CommandAttribute), false)
                .FirstOrDefault() as CommandAttribute;

            attribute.Should().NotBeNull($"Field {field.Name} should have CommandAttribute");
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Constructor_WithSpecialCharactersInAlias_CreatesInstance()
    {
        // Arrange
        var alias = "test-command_123!@#";
        var handlerType = typeof(HelpCommandHandler);
        var resourceKey = "TestResource";

        // Act
        var attribute = new CommandAttribute(alias, handlerType, resourceKey);

        // Assert
        attribute.AliasCommand.Should().Be(alias);
    }

    [Fact]
    public void Constructor_WithWhitespaceAlias_CreatesInstance()
    {
        // Arrange
        var alias = "   ";
        var handlerType = typeof(HelpCommandHandler);
        var resourceKey = "TestResource";

        // Act
        var attribute = new CommandAttribute(alias, handlerType, resourceKey);

        // Assert
        attribute.AliasCommand.Should().Be(alias);
    }

    [Fact]
    public void HandlerCommand_WithAbstractType_StoresType()
    {
        // Arrange
        var handlerType = typeof(Attribute); // Using an abstract type
        var attribute = new CommandAttribute("test", handlerType, "TestResource");

        // Act
        var result = attribute.HandlerCommand;

        // Assert
        result.Should().Be(handlerType);
    }

    [Fact]
    public void HandlerCommand_WithInterfaceType_StoresType()
    {
        // Arrange
        var handlerType = typeof(ICommandHandler);
        var attribute = new CommandAttribute("test", handlerType, "TestResource");

        // Act
        var result = attribute.HandlerCommand;

        // Assert
        result.Should().Be(handlerType);
    }

    #endregion
}
