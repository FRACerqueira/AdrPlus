// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Domain;

namespace AdrPlus.Tests.Core;

public class StringCaseExtensionsTests
{
    [Theory]
    [InlineData("hello world", "helloWorld")]
    [InlineData("Hello World", "helloWorld")]
    [InlineData("HELLO WORLD", "helloWorld")]
    [InlineData("hello", "hello")]
    [InlineData("Hello", "hello")]
    [InlineData("HelloWorld", "helloWorld")]
    [InlineData("hello_world", "helloWorld")]
    [InlineData("hello-world", "helloWorld")]
    [InlineData("XMLParser", "xmlParser")]
    [InlineData("use new database", "useNewDatabase")]
    public void ToCamelCase_WithVariousInputs_ReturnsCorrectCamelCase(string input, string expected)
    {
        // Act
        var result = input.ToCamelCase();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData(null, null)]
    public void ToCamelCase_WithEmptyOrWhitespace_ReturnsInput(string? input, string? expected)
    {
        // Act
        var result = input?.ToCamelCase();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("hello world", "HelloWorld")]
    [InlineData("Hello World", "HelloWorld")]
    [InlineData("HELLO WORLD", "HelloWorld")]
    [InlineData("hello", "Hello")]
    [InlineData("Hello", "Hello")]
    [InlineData("helloWorld", "HelloWorld")]
    [InlineData("hello_world", "HelloWorld")]
    [InlineData("hello-world", "HelloWorld")]
    [InlineData("XMLParser", "XmlParser")]
    [InlineData("use new database", "UseNewDatabase")]
    public void ToPascalCase_WithVariousInputs_ReturnsCorrectPascalCase(string input, string expected)
    {
        // Act
        var result = input.ToPascalCase();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData(null, null)]
    public void ToPascalCase_WithEmptyOrWhitespace_ReturnsInput(string? input, string? expected)
    {
        // Act
        var result = input?.ToPascalCase();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("hello world", "hello_world")]
    [InlineData("Hello World", "hello_world")]
    [InlineData("HELLO WORLD", "hello_world")]
    [InlineData("hello", "hello")]
    [InlineData("Hello", "hello")]
    [InlineData("HelloWorld", "hello_world")]
    [InlineData("helloWorld", "hello_world")]
    [InlineData("hello-world", "hello_world")]
    [InlineData("XMLParser", "xml_parser")]
    [InlineData("use new database", "use_new_database")]
    public void ToSnakeCase_WithVariousInputs_ReturnsCorrectSnakeCase(string input, string expected)
    {
        // Act
        var result = input.ToSnakeCase();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData(null, null)]
    public void ToSnakeCase_WithEmptyOrWhitespace_ReturnsInput(string? input, string? expected)
    {
        // Act
        var result = input?.ToSnakeCase();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("hello world", "hello-world")]
    [InlineData("Hello World", "hello-world")]
    [InlineData("HELLO WORLD", "hello-world")]
    [InlineData("hello", "hello")]
    [InlineData("Hello", "hello")]
    [InlineData("HelloWorld", "hello-world")]
    [InlineData("helloWorld", "hello-world")]
    [InlineData("hello_world", "hello-world")]
    [InlineData("XMLParser", "xml-parser")]
    [InlineData("use new database", "use-new-database")]
    public void ToKebabCase_WithVariousInputs_ReturnsCorrectKebabCase(string input, string expected)
    {
        // Act
        var result = input.ToKebabCase();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData(null, null)]
    public void ToKebabCase_WithEmptyOrWhitespace_ReturnsInput(string? input, string? expected)
    {
        // Act
        var result = input?.ToKebabCase();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("hello world", CaseFormat.CamelCase, "helloWorld")]
    [InlineData("hello world", CaseFormat.PascalCase, "HelloWorld")]
    [InlineData("hello world", CaseFormat.SnakeCase, "hello_world")]
    [InlineData("hello world", CaseFormat.KebabCase, "hello-world")]
    internal void ToCase_WithDifferentFormats_AppliesCorrectTransformation(string input, CaseFormat format, string expected)
    {
        // Act
        var result = input.ToCase(format);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToCase_WithInvalidFormat_ThrowsNotImplementedException()
    {
        // Arrange
        var input = "hello world";
        var invalidFormat = (CaseFormat)999;

        // Act
        var act = () => input.ToCase(invalidFormat);

        // Assert
        act.Should().Throw<NotImplementedException>();
    }

    [Theory]
    [InlineData("use_new_database", "useNewDatabase")]
    [InlineData("use-new-database", "useNewDatabase")]
    [InlineData("UseNewDatabase", "useNewDatabase")]
    public void ToCamelCase_FromDifferentFormats_ConvertsCorrectly(string input, string expected)
    {
        // Act
        var result = input.ToCamelCase();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("API", "api")]
    [InlineData("HTTPServer", "httpServer")]
    [InlineData("URLParser", "urlParser")]
    public void ToCamelCase_WithAcronyms_HandlesCorrectly(string input, string expected)
    {
        // Act
        var result = input.ToCamelCase();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("API", "Api")]
    [InlineData("HTTPServer", "HttpServer")]
    [InlineData("URLParser", "UrlParser")]
    public void ToPascalCase_WithAcronyms_HandlesCorrectly(string input, string expected)
    {
        // Act
        var result = input.ToPascalCase();

        // Assert
        result.Should().Be(expected);
    }
}
