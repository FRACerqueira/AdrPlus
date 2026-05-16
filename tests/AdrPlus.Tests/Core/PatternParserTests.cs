// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Domain;

namespace AdrPlus.Tests.Core
{
    /// <summary>
    /// Unit tests for PatternParser class.
    /// Tests cover CreateMigratePattern, ParseAdrPattern, and ParseMigratePattern methods
    /// with various valid, invalid, and edge case scenarios.
    /// </summary>
    public class PatternParserTests
    {
        #region CreateMigratePattern Tests

        [Fact]
        public void CreateMigratePattern_WithLenNumberZero_ReturnsEmpty()
        {
            // Arrange
            var config = new ConfigMigration
            {
                LenNumber = 0,
                Title = 10
            };

            // Act
            var result = PatternParser.CreateMigratePattern(config);

            // Assert
            result.Should().Be(string.Empty);
        }

        [Fact]
        public void CreateMigratePattern_WithTitleZero_ReturnsEmpty()
        {
            // Arrange
            var config = new ConfigMigration
            {
                LenNumber = 5,
                Title = 0
            };

            // Act
            var result = PatternParser.CreateMigratePattern(config);

            // Assert
            result.Should().Be(string.Empty);
        }

        [Fact]
        public void CreateMigratePattern_WithMandatoryFieldsOnly_CreatesPattern()
        {
            // Arrange
            var config = new ConfigMigration
            {
                Number = 5,
                LenNumber = 4,
                Title = 30
            };

            // Act
            var result = PatternParser.CreateMigratePattern(config);

            // Assert
            result.Should().Be("N05:04T30");
        }

        [Fact]
        public void CreateMigratePattern_WithVersionIncluded_CreatesPatternWithVersion()
        {
            // Arrange
            var config = new ConfigMigration
            {
                Number = 10,
                LenNumber = 4,
                Title = 20,
                Version = 1,
                LenVersion = 2
            };

            // Act
            var result = PatternParser.CreateMigratePattern(config);

            // Assert
            result.Should().Be("N10:04T20V01:02");
        }

        [Fact]
        public void CreateMigratePattern_WithRevisionIncluded_CreatesPatternWithRevision()
        {
            // Arrange
            var config = new ConfigMigration
            {
                Number = 5,
                LenNumber = 4,
                Title = 30,
                Revision = 2,
                LenRevision = 1
            };

            // Act
            var result = PatternParser.CreateMigratePattern(config);

            // Assert
            result.Should().Be("N05:04T30R02:01");
        }

        [Fact]
        public void CreateMigratePattern_WithPrefixIncluded_CreatesPatternWithPrefix()
        {
            // Arrange
            var config = new ConfigMigration
            {
                Number = 5,
                LenNumber = 4,
                Title = 30,
                Prefix = 3,
                LenPrefix = 3
            };

            // Act
            var result = PatternParser.CreateMigratePattern(config);

            // Assert
            result.Should().Be("N05:04T30P03:03");
        }

        [Fact]
        public void CreateMigratePattern_WithAllFieldsIncluded_CreatesCompletePattern()
        {
            // Arrange
            var config = new ConfigMigration
            {
                Number = 5,
                LenNumber = 4,
                Title = 30,
                Version = 1,
                LenVersion = 2,
                Revision = 2,
                LenRevision = 1,
                Prefix = 3,
                LenPrefix = 3
            };

            // Act
            var result = PatternParser.CreateMigratePattern(config);

            // Assert
            result.Should().Be("N05:04T30V01:02R02:01P03:03");
        }

        [Fact]
        public void CreateMigratePattern_WithVersionZeroButRevision_OnlyIncludesRevision()
        {
            // Arrange
            var config = new ConfigMigration
            {
                Number = 5,
                LenNumber = 4,
                Title = 30,
                Version = 1,
                LenVersion = 0,  // Zero
                Revision = 2,
                LenRevision = 1
            };

            // Act
            var result = PatternParser.CreateMigratePattern(config);

            // Assert
            result.Should().Be("N05:04T30R02:01");
        }

        #endregion

        #region ParseAdrPattern Tests - Valid Cases

        [Fact]
        public void ParseAdrPattern_WithSimpleValidPattern_ReturnsComponents()
        {
            // Arrange
            var pattern = "1V2";

            // Act
            var result = PatternParser.ParseAdrPattern(pattern);

            // Assert
            result.Should().NotBeNull();
            result!["P"].Should().Be(string.Empty);
            result["N"].Should().Be("1");
            result["V"].Should().Be("2");
            result["R"].Should().Be(string.Empty);
            result["S"].Should().Be(string.Empty);
        }

        [Fact]
        public void ParseAdrPattern_WithMultipleDigitNumber_ReturnsCorrectly()
        {
            // Arrange
            var pattern = "1234V56";

            // Act
            var result = PatternParser.ParseAdrPattern(pattern);

            // Assert
            result.Should().NotBeNull();
            result!["N"].Should().Be("1234");
            result["V"].Should().Be("56");
        }

        [Fact]
        public void ParseAdrPattern_WithPrefix_ReturnsComponents()
        {
            // Arrange
            var pattern = "ABC1V2";

            // Act
            var result = PatternParser.ParseAdrPattern(pattern);

            // Assert
            result.Should().NotBeNull();
            result!["P"].Should().Be("ABC");
            result["N"].Should().Be("1");
            result["V"].Should().Be("2");
        }

        [Fact]
        public void ParseAdrPattern_WithRevision_ReturnsComponents()
        {
            // Arrange
            var pattern = "1V2R3";

            // Act
            var result = PatternParser.ParseAdrPattern(pattern);

            // Assert
            result.Should().NotBeNull();
            result!["N"].Should().Be("1");
            result["V"].Should().Be("2");
            result["R"].Should().Be("3");
        }

        [Fact]
        public void ParseAdrPattern_WithScope_ReturnsComponents()
        {
            // Arrange
            var pattern = "1V2S";

            // Act
            var result = PatternParser.ParseAdrPattern(pattern);

            // Assert
            result.Should().NotBeNull();
            result!["N"].Should().Be("1");
            result["V"].Should().Be("2");
            result["S"].Should().Be("S");
        }

        [Fact]
        public void ParseAdrPattern_WithMultipleScopeLetters_ReturnsAllScope()
        {
            // Arrange
            var pattern = "1V2SCOPE";

            // Act
            var result = PatternParser.ParseAdrPattern(pattern);

            // Assert
            result.Should().NotBeNull();
            result!["S"].Should().Be("SCOPE");
        }

        [Fact]
        public void ParseAdrPattern_WithPrefixAndRevisionAndScope_ReturnsAllComponents()
        {
            // Arrange
            var pattern = "PREF1V2R3SCOPE";

            // Act
            var result = PatternParser.ParseAdrPattern(pattern);

            // Assert
            result.Should().NotBeNull();
            result!["P"].Should().Be("PREF");
            result["N"].Should().Be("1");
            result["V"].Should().Be("2");
            result["R"].Should().Be("3");
            result["S"].Should().Be("SCOPE");
        }

        #endregion

        #region ParseAdrPattern Tests - Invalid Cases

        [Fact]
        public void ParseAdrPattern_WithNullInput_ReturnsNull()
        {
            // Act
            var result = PatternParser.ParseAdrPattern(null!);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseAdrPattern_WithEmptyString_ReturnsNull()
        {
            // Act
            var result = PatternParser.ParseAdrPattern(string.Empty);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseAdrPattern_WithWhitespaceOnly_ReturnsNull()
        {
            // Act
            var result = PatternParser.ParseAdrPattern("   ");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseAdrPattern_WithoutVersionMarker_ReturnsNull()
        {
            // Arrange
            var pattern = "1R2";  // Missing V

            // Act
            var result = PatternParser.ParseAdrPattern(pattern);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseAdrPattern_WithoutNumberBeforeVersion_ReturnsNull()
        {
            // Arrange
            var pattern = "V2";  // Missing N

            // Act
            var result = PatternParser.ParseAdrPattern(pattern);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseAdrPattern_WithoutAnyVersion_ReturnsNull()
        {
            // Arrange
            var pattern = "ABC1";  // Missing V entirely

            // Act
            var result = PatternParser.ParseAdrPattern(pattern);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseAdrPattern_WithInvalidCharacters_ReturnsNull()
        {
            // Arrange
            var pattern = "1V2-invalid";  // Invalid character '-'

            // Act
            var result = PatternParser.ParseAdrPattern(pattern);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseAdrPattern_WithOnlyLetters_ReturnsNull()
        {
            // Act
            var result = PatternParser.ParseAdrPattern("ABC");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseAdrPattern_WithOnlyNumbers_ReturnsNull()
        {
            // Act
            var result = PatternParser.ParseAdrPattern("12345");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseAdrPattern_WithRevisionWithoutV_ReturnsNull()
        {
            // Arrange
            var pattern = "1R2";  // R without V

            // Act
            var result = PatternParser.ParseAdrPattern(pattern);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region ParseMigratePattern Tests - Valid Cases

        [Fact]
        public void ParseMigratePattern_WithMinimalValidPattern_ReturnsComponents()
        {
            // Arrange
            var pattern = "N00:00T00";

            // Act
            var result = PatternParser.ParseMigratePattern(pattern);

            // Assert
            result.Should().NotBeNull();
            result!.Should().HaveCount(2);
            result["N"].Should().Be((0, 0));
            result["T"].Should().Be((0, 0));
            result.Should().NotContainKey("V");
            result.Should().NotContainKey("R");
            result.Should().NotContainKey("P");
        }

        [Fact]
        public void ParseMigratePattern_WithVersionIncluded_ReturnsComponents()
        {
            // Arrange
            var pattern = "N05:04T30V01:02";

            // Act
            var result = PatternParser.ParseMigratePattern(pattern);

            // Assert
            result.Should().NotBeNull();
            result!.Should().HaveCount(3);
            result["N"].Should().Be((5, 4));
            result["T"].Should().Be((30, 0));
            result["V"].Should().Be((1, 2));
        }

        [Fact]
        public void ParseMigratePattern_WithRevisionIncluded_ReturnsComponents()
        {
            // Arrange
            var pattern = "N05:04T30R02:01";

            // Act
            var result = PatternParser.ParseMigratePattern(pattern);

            // Assert
            result.Should().NotBeNull();
            result!.Should().HaveCount(3);
            result["N"].Should().Be((5, 4));
            result["T"].Should().Be((30, 0));
            result["R"].Should().Be((2, 1));
        }

        [Fact]
        public void ParseMigratePattern_WithPrefixIncluded_ReturnsComponents()
        {
            // Arrange
            var pattern = "N05:04T30P03:03";

            // Act
            var result = PatternParser.ParseMigratePattern(pattern);

            // Assert
            result.Should().NotBeNull();
            result!.Should().HaveCount(3);
            result["N"].Should().Be((5, 4));
            result["T"].Should().Be((30, 0));
            result["P"].Should().Be((3, 3));
        }

        [Fact]
        public void ParseMigratePattern_WithAllFieldsIncluded_ReturnsAllComponents()
        {
            // Arrange
            var pattern = "N05:04T30V01:02R02:01P03:03";

            // Act
            var result = PatternParser.ParseMigratePattern(pattern);

            // Assert
            result.Should().NotBeNull();
            result!.Should().HaveCount(5);
            result["N"].Should().Be((5, 4));
            result["T"].Should().Be((30, 0));
            result["V"].Should().Be((1, 2));
            result["R"].Should().Be((2, 1));
            result["P"].Should().Be((3, 3));
        }

        [Fact]
        public void ParseMigratePattern_WithLargeValues_ReturnsCorrectly()
        {
            // Arrange
            var pattern = "N99:99T99V99:99R99:99P99:99";

            // Act
            var result = PatternParser.ParseMigratePattern(pattern);

            // Assert
            result.Should().NotBeNull();
            result!["N"].Should().Be((99, 99));
            result["T"].Should().Be((99, 0));
            result["V"].Should().Be((99, 99));
            result["R"].Should().Be((99, 99));
            result["P"].Should().Be((99, 99));
        }

        [Fact]
        public void ParseMigratePattern_WithVersionAndRevision_ReturnsComponents()
        {
            // Arrange
            var pattern = "N10:05T25V02:03R01:01";

            // Act
            var result = PatternParser.ParseMigratePattern(pattern);

            // Assert
            result.Should().NotBeNull();
            result!.Should().HaveCount(4);
            result["N"].Should().Be((10, 5));
            result["T"].Should().Be((25, 0));
            result["V"].Should().Be((2, 3));
            result["R"].Should().Be((1, 1));
        }

        #endregion

        #region ParseMigratePattern Tests - Invalid Cases

        [Fact]
        public void ParseMigratePattern_WithNullInput_ReturnsNull()
        {
            // Act
            var result = PatternParser.ParseMigratePattern(null!);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseMigratePattern_WithEmptyString_ReturnsNull()
        {
            // Act
            var result = PatternParser.ParseMigratePattern(string.Empty);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseMigratePattern_WithWhitespaceOnly_ReturnsNull()
        {
            // Act
            var result = PatternParser.ParseMigratePattern("   ");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseMigratePattern_WithoutN_ReturnsNull()
        {
            // Arrange
            var pattern = "T00";  // Missing N

            // Act
            var result = PatternParser.ParseMigratePattern(pattern);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseMigratePattern_WithoutT_ReturnsNull()
        {
            // Arrange
            var pattern = "N00:00";  // Missing T

            // Act
            var result = PatternParser.ParseMigratePattern(pattern);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseMigratePattern_WithInvalidNFormat_ReturnsNull()
        {
            // Arrange
            var pattern = "N0:0T00";  // N with only 1 digit

            // Act
            var result = PatternParser.ParseMigratePattern(pattern);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseMigratePattern_WithInvalidLengthFormat_ReturnsNull()
        {
            // Arrange
            var pattern = "N00:0T00";  // Length with only 1 digit

            // Act
            var result = PatternParser.ParseMigratePattern(pattern);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseMigratePattern_WithMissingColon_ReturnsNull()
        {
            // Arrange
            var pattern = "N0000T00";  // No colon

            // Act
            var result = PatternParser.ParseMigratePattern(pattern);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseMigratePattern_WithoutColonInT_ReturnsNull()
        {
            // Arrange - T should have only position, no colon
            var pattern = "N00:00T00:00";  // T with colon (invalid)

            // Act
            var result = PatternParser.ParseMigratePattern(pattern);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseMigratePattern_WithTSingleDigit_ReturnsNull()
        {
            // Arrange
            var pattern = "N00:00T0";  // T with only 1 digit

            // Act
            var result = PatternParser.ParseMigratePattern(pattern);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseMigratePattern_WithInvalidCharacters_ReturnsNull()
        {
            // Arrange
            var pattern = "N00:00T00V-XX:XX";  // Invalid character

            // Act
            var result = PatternParser.ParseMigratePattern(pattern);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseMigratePattern_WithWrongOrder_ReturnsNull()
        {
            // Arrange
            var pattern = "T00N00:00";  // T before N

            // Act
            var result = PatternParser.ParseMigratePattern(pattern);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseMigratePattern_WithDuplicateN_ReturnsNull()
        {
            // Arrange
            var pattern = "N00:00N10:10T00";  // Duplicate N

            // Act
            var result = PatternParser.ParseMigratePattern(pattern);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseMigratePattern_WithRBeforeV_ReturnsNull()
        {
            // Arrange
            var pattern = "N00:00T00R01:01V02:02";  // R before V (wrong order)

            // Act
            var result = PatternParser.ParseMigratePattern(pattern);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseMigratePattern_WithInvalidTFormat_ReturnsNull()
        {
            // Arrange
            var pattern = "N00:00T0";  // T missing digit

            // Act
            var result = PatternParser.ParseMigratePattern(pattern);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region Edge Cases and Integration Tests

        [Fact]
        public void CreateAndParseMigratePattern_RoundTrip_Consistent()
        {
            // Arrange
            var original = new ConfigMigration
            {
                Number = 5,
                LenNumber = 4,
                Title = 30,
                Version = 1,
                LenVersion = 2,
                Revision = 2,
                LenRevision = 1,
                Prefix = 3,
                LenPrefix = 3
            };

            // Act
            var pattern = PatternParser.CreateMigratePattern(original);
            var parsed = PatternParser.ParseMigratePattern(pattern);

            // Assert
            parsed.Should().NotBeNull();
            parsed!["N"].Should().Be((original.Number, original.LenNumber));
            parsed["T"].Should().Be((original.Title, 0));
            parsed["V"].Should().Be((original.Version, original.LenVersion));
            parsed["R"].Should().Be((original.Revision, original.LenRevision));
            parsed["P"].Should().Be((original.Prefix, original.LenPrefix));
        }

        [Fact]
        public void ParseAdrPattern_VariousValidFormats_AllSucceed()
        {
            // Arrange
            var patterns = new[]
            {
                "1V1",
                "12V34",
                "1234V567890",
                "A1V2",
                "ABC1V2",
                "ABCDEF1V2",
                "1V1R1",
                "1V1S",
                "1V1R1S",
                "ABC1V2R3DEF",
                "PREFIX123V456R789SCOPE"
            };

            // Act & Assert
            foreach (var pattern in patterns)
            {
                var result = PatternParser.ParseAdrPattern(pattern);
                result.Should().NotBeNull($"Pattern '{pattern}' should be valid");
                result!.Keys.Should().Contain(new[] { "P", "N", "V", "R", "S" });
            }
        }

        [Fact]
        public void ParseMigratePattern_VariousValidFormats_AllSucceed()
        {
            // Arrange
            var patterns = new[]
            {
                "N00:00T00",
                "N01:02T03",
                "N10:05T25V02:03",
                "N10:05T25V02:03R01:01",
                "N10:05T25V02:03R01:01P03:03",
                "N99:99T99",
                "N50:10T40R20:05"
            };

            // Act & Assert
            foreach (var pattern in patterns)
            {
                var result = PatternParser.ParseMigratePattern(pattern);
                result.Should().NotBeNull($"Pattern '{pattern}' should be valid");
                result!.Should().ContainKey("N");
                result.Should().ContainKey("T");
            }
        }

        [Fact]
        public void ParseAdrPattern_CaseSensitive_ReturnsCorrectly()
        {
            // Arrange
            var pattern1 = "abc1V2";
            var pattern2 = "ABC1V2";

            // Act
            var result1 = PatternParser.ParseAdrPattern(pattern1);
            var result2 = PatternParser.ParseAdrPattern(pattern2);

            // Assert
            result1.Should().NotBeNull();
            result2.Should().NotBeNull();
            result1!["P"].Should().Be("abc");
            result2!["P"].Should().Be("ABC");
        }

        #endregion
    }
}
