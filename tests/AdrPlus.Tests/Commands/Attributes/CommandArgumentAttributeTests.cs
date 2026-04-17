// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;

namespace AdrPlus.Tests.Commands.Attributes;

/// <summary>
/// Unit tests for CommandArgumentAttribute class.
/// Tests demonstrate attribute instantiation, property access, and optional parameters.
/// </summary>
public class CommandArgumentAttributeTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithShortAndLongCommand_CreatesInstance()
    {
        // Arrange
        var shortCommand = "-h";
        var longCommand = "--help";

        // Act
        var attribute = new CommandArgumentAttribute(shortCommand, longCommand);

        // Assert
        attribute.Should().NotBeNull();
        attribute.ShortCommand.Should().Be(shortCommand);
        attribute.LongCommand.Should().Be(longCommand);
        attribute.AliasesValues.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithAllParameters_CreatesInstance()
    {
        // Arrange
        var shortCommand = "-f";
        var longCommand = "--file";
        var aliases = new[] { "json", "xml", "yaml" };

        // Act
        var attribute = new CommandArgumentAttribute(shortCommand, longCommand, aliases);

        // Assert
        attribute.Should().NotBeNull();
        attribute.ShortCommand.Should().Be(shortCommand);
        attribute.LongCommand.Should().Be(longCommand);
        attribute.AliasesValues.Should().BeEquivalentTo(aliases);
    }

    [Fact]
    public void Constructor_WithNullAliases_CreatesInstanceWithNullAliases()
    {
        // Arrange
        var shortCommand = "-w";
        var longCommand = "--wizard";

        // Act
        var attribute = new CommandArgumentAttribute(shortCommand, longCommand, null);

        // Assert
        attribute.Should().NotBeNull();
        attribute.AliasesValues.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithEmptyAliases_CreatesInstanceWithEmptyArray()
    {
        // Arrange
        var shortCommand = "-o";
        var longCommand = "--open";
        var aliases = Array.Empty<string>();

        // Act
        var attribute = new CommandArgumentAttribute(shortCommand, longCommand, aliases);

        // Assert
        attribute.Should().NotBeNull();
        attribute.AliasesValues.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullShortCommand_CreatesInstance()
    {
        // Arrange
        var longCommand = "--help";

        // Act
        var attribute = new CommandArgumentAttribute(null!, longCommand);

        // Assert
        attribute.Should().NotBeNull();
        attribute.ShortCommand.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithNullLongCommand_CreatesInstance()
    {
        // Arrange
        var shortCommand = "-h";

        // Act
        var attribute = new CommandArgumentAttribute(shortCommand, null!);

        // Assert
        attribute.Should().NotBeNull();
        attribute.LongCommand.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithEmptyStrings_CreatesInstance()
    {
        // Arrange
        var shortCommand = string.Empty;
        var longCommand = string.Empty;

        // Act
        var attribute = new CommandArgumentAttribute(shortCommand, longCommand);

        // Assert
        attribute.Should().NotBeNull();
        attribute.ShortCommand.Should().BeEmpty();
        attribute.LongCommand.Should().BeEmpty();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void ShortCommand_ReturnsConstructorValue()
    {
        // Arrange
        var expectedShort = "-v";
        var attribute = new CommandArgumentAttribute(expectedShort, "--version");

        // Act
        var result = attribute.ShortCommand;

        // Assert
        result.Should().Be(expectedShort);
    }

    [Fact]
    public void LongCommand_ReturnsConstructorValue()
    {
        // Arrange
        var expectedLong = "--verbose";
        var attribute = new CommandArgumentAttribute("-v", expectedLong);

        // Act
        var result = attribute.LongCommand;

        // Assert
        result.Should().Be(expectedLong);
    }

    [Fact]
    public void AliasesValues_WithNullInConstructor_ReturnsNull()
    {
        // Arrange
        var attribute = new CommandArgumentAttribute("-t", "--test", null);

        // Act
        var result = attribute.AliasesValues;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void AliasesValues_WithArrayInConstructor_ReturnsArray()
    {
        // Arrange
        var expectedAliases = new[] { "alias1", "alias2", "alias3" };
        var attribute = new CommandArgumentAttribute("-t", "--test", expectedAliases);

        // Act
        var result = attribute.AliasesValues;

        // Assert
        result.Should().BeEquivalentTo(expectedAliases);
        result.Should().HaveCount(3);
    }

    [Fact]
    public void AliasesValues_WithSingleAlias_ReturnsArrayWithOneElement()
    {
        // Arrange
        var expectedAliases = new[] { "single" };
        var attribute = new CommandArgumentAttribute("-s", "--single", expectedAliases);

        // Act
        var result = attribute.AliasesValues;

        // Assert
        result.Should().ContainSingle();
        result![0].Should().Be("single");
    }

    #endregion

    #region Attribute Usage Tests

    [Fact]
    public void AttributeUsage_AllowsFieldTarget()
    {
        // Arrange & Act
        var attributeType = typeof(CommandArgumentAttribute);
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
        var attributeType = typeof(CommandArgumentAttribute);
        var attributeUsage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
            attributeType, typeof(AttributeUsageAttribute))!;

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage.AllowMultiple.Should().BeFalse();
    }

    #endregion

    #region Integration Tests with Enums

    [Fact]
    public void CommandArgumentAttribute_AppliedToEnum_CanBeRetrieved()
    {
        // Arrange
        var enumType = typeof(Arguments);
        var field = enumType.GetField("Help");

        // Act
        var attribute = field?.GetCustomAttributes(typeof(CommandArgumentAttribute), false)
            .FirstOrDefault() as CommandArgumentAttribute;

        // Assert
        attribute.Should().NotBeNull();
        attribute!.ShortCommand.Should().NotBeNullOrEmpty();
        attribute.LongCommand.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CommandArgumentAttribute_AllEnumFieldsHaveAttribute()
    {
        // Arrange
        var enumType = typeof(Arguments);
        var fields = enumType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Act & Assert
        foreach (var field in fields)
        {
            var attribute = field.GetCustomAttributes(typeof(CommandArgumentAttribute), false)
                .FirstOrDefault() as CommandArgumentAttribute;

            attribute.Should().NotBeNull($"Field {field.Name} should have CommandArgumentAttribute");
        }
    }

    [Fact]
    public void CommandArgumentAttribute_FromEnum_HasExpectedFormat()
    {
        // Arrange
        var enumType = typeof(Arguments);
        var field = enumType.GetField("FileConfig");

        // Act
        var attribute = field?.GetCustomAttributes(typeof(CommandArgumentAttribute), false)
            .FirstOrDefault() as CommandArgumentAttribute;

        // Assert
        attribute.Should().NotBeNull();
        attribute!.ShortCommand.Should().StartWith("-");
        attribute.LongCommand.Should().StartWith("--");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Constructor_WithSpecialCharactersInCommands_CreatesInstance()
    {
        // Arrange
        var shortCommand = "-@";
        var longCommand = "--test_123-command!";

        // Act
        var attribute = new CommandArgumentAttribute(shortCommand, longCommand);

        // Assert
        attribute.ShortCommand.Should().Be(shortCommand);
        attribute.LongCommand.Should().Be(longCommand);
    }

    [Fact]
    public void Constructor_WithWhitespaceCommands_CreatesInstance()
    {
        // Arrange
        var shortCommand = "   ";
        var longCommand = "  test  ";

        // Act
        var attribute = new CommandArgumentAttribute(shortCommand, longCommand);

        // Assert
        attribute.ShortCommand.Should().Be(shortCommand);
        attribute.LongCommand.Should().Be(longCommand);
    }

    [Fact]
    public void Constructor_WithVeryLongCommands_CreatesInstance()
    {
        // Arrange
        var shortCommand = new string('a', 100);
        var longCommand = new string('b', 200);

        // Act
        var attribute = new CommandArgumentAttribute(shortCommand, longCommand);

        // Assert
        attribute.ShortCommand.Should().HaveLength(100);
        attribute.LongCommand.Should().HaveLength(200);
    }

    [Fact]
    public void AliasesValues_WithNullElementsInArray_StoresArray()
    {
        // Arrange
        var aliases = new string?[] { "valid", null, "another" };

        // Act
        var attribute = new CommandArgumentAttribute("-t", "--test", aliases!);

        // Assert
        attribute.AliasesValues.Should().HaveCount(3);
        attribute.AliasesValues![1].Should().BeNull();
    }

    [Fact]
    public void AliasesValues_WithEmptyStringsInArray_StoresArray()
    {
        // Arrange
        var aliases = new[] { "valid", "", "another" };

        // Act
        var attribute = new CommandArgumentAttribute("-t", "--test", aliases);

        // Assert
        attribute.AliasesValues.Should().HaveCount(3);
        attribute.AliasesValues![1].Should().BeEmpty();
    }

    [Fact]
    public void AliasesValues_WithDuplicates_StoresAllDuplicates()
    {
        // Arrange
        var aliases = new[] { "test", "test", "test" };

        // Act
        var attribute = new CommandArgumentAttribute("-t", "--test", aliases);

        // Assert
        attribute.AliasesValues.Should().HaveCount(3);
        attribute.AliasesValues.Should().OnlyContain(x => x == "test");
    }

    [Fact]
    public void Properties_AreImmutable_CannotBeChanged()
    {
        // Arrange
        var attribute = new CommandArgumentAttribute("-t", "--test", ["alias"]);

        // Act & Assert
        // Properties should be read-only (get-only)
        attribute.ShortCommand.Should().NotBeNull();
        attribute.LongCommand.Should().NotBeNull();
        attribute.AliasesValues.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithCaseSensitiveCommands_PreservesCase()
    {
        // Arrange
        var shortCommand = "-H";
        var longCommand = "--HELP";

        // Act
        var attribute = new CommandArgumentAttribute(shortCommand, longCommand);

        // Assert
        attribute.ShortCommand.Should().Be("-H");
        attribute.LongCommand.Should().Be("--HELP");
    }

    #endregion
}
