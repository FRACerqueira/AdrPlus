// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;

namespace AdrPlus.Tests.Domain;

public class CaseFormatTests
{
    [Fact]
    public void CaseFormat_AllValues_AreDefined()
    {
        // Arrange & Act
        var values = Enum.GetValues<CaseFormat>();

        // Assert
        values.Should().Contain(CaseFormat.CamelCase);
        values.Should().Contain(CaseFormat.PascalCase);
        values.Should().Contain(CaseFormat.SnakeCase);
        values.Should().Contain(CaseFormat.KebabCase);
    }

    [Fact]
    public void CaseFormat_HasExpectedCount()
    {
        // Arrange & Act
        var values = Enum.GetValues<CaseFormat>();

        // Assert
        values.Length.Should().Be(4);
    }

    [Theory]
    [InlineData(CaseFormat.CamelCase)]
    [InlineData(CaseFormat.PascalCase)]
    [InlineData(CaseFormat.SnakeCase)]
    [InlineData(CaseFormat.KebabCase)]
    internal void CaseFormat_ToString_ReturnsName(CaseFormat format)
    {
        // Act
        var result = format.ToString();

        // Assert
        result.Should().NotBeNullOrEmpty();
        Enum.IsDefined(format).Should().BeTrue();
    }

    [Fact]
    public void CaseFormat_DefaultValue_IsCamelCase()
    {
        // Arrange & Act
        var defaultFormat = default(CaseFormat);

        // Assert
        defaultFormat.Should().Be(CaseFormat.CamelCase);
    }

    [Theory]
    [InlineData(CaseFormat.CamelCase, "CamelCase")]
    [InlineData(CaseFormat.PascalCase, "PascalCase")]
    [InlineData(CaseFormat.SnakeCase, "SnakeCase")]
    [InlineData(CaseFormat.KebabCase, "KebabCase")]
    internal void CaseFormat_Name_MatchesExpected(CaseFormat format, string expectedName)
    {
        // Act
        var result = format.ToString();

        // Assert
        result.Should().Be(expectedName);
    }
}
