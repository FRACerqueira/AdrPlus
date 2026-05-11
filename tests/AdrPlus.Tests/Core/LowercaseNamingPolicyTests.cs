// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;

namespace AdrPlus.Tests.Core
{
    public class LowercaseNamingPolicyTests
    {
        private readonly LowercaseNamingPolicy _policy = new();

        [Theory]
        [InlineData("TestProperty", "testproperty")]
        [InlineData("UPPERCASE", "uppercase")]
        [InlineData("lowercase", "lowercase")]
        [InlineData("CamelCase", "camelcase")]
        [InlineData("snake_case", "snake_case")]
        [InlineData("kebab-case", "kebab-case")]
        [InlineData("MixedUPPERandlower", "mixedupperandlower")]
        public void ConvertName_WithVaryingCases_ReturnsLowercase(string input, string expected)
        {
            // Act
            var result = _policy.ConvertName(input);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("a", "a")]
        [InlineData("A", "a")]
        [InlineData("1", "1")]
        [InlineData("_", "_")]
        [InlineData("-", "-")]
        public void ConvertName_WithSingleCharacters_ReturnsExpected(string input, string expected)
        {
            // Act
            var result = _policy.ConvertName(input);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void ConvertName_WithEmptyString_ReturnsEmptyString()
        {
            // Act
            var result = _policy.ConvertName(string.Empty);

            // Assert
            result.Should().Be(string.Empty);
        }

        [Theory]
        [InlineData("Name123", "name123")]
        [InlineData("Version2dot5", "version2dot5")]
        [InlineData("Test_123_Value", "test_123_value")]
        public void ConvertName_WithMixedAlphanumeric_ReturnsLowercase(string input, string expected)
        {
            // Act
            var result = _policy.ConvertName(input);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("HTTPResponseCode")]
        [InlineData("XMLData")]
        [InlineData("URL")]
        public void ConvertName_WithAcronyms_ReturnsLowercase(string input)
        {
            // Act
            var result = _policy.ConvertName(input);

            // Assert
            result.Should().Be(input.ToLowerInvariant());
        }

        [Fact]
        public void ConvertName_IsConsistent_WhenCalledMultipleTimes()
        {
            // Arrange
            var input = "TestValue";

            // Act
            var first = _policy.ConvertName(input);
            var second = _policy.ConvertName(input);
            var third = _policy.ConvertName(input);

            // Assert
            first.Should().Be(second).And.Be(third);
        }

        [Theory]
        [InlineData("PropertyWithNumbers123AndMore")]
        [InlineData("HTTPSConnectionData")]
        public void ConvertName_WithComplexNames_ReturnsFullyLowercase(string input)
        {
            // Act
            var result = _policy.ConvertName(input);

            // Assert
            result.Should().Be(input.ToLowerInvariant());
        }
    }
}
