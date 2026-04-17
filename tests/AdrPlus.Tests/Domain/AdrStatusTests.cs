// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;
using System.Runtime.InteropServices;

namespace AdrPlus.Tests.Domain;

public class AdrStatusTests
{
    private static string PlatformPath(params string[] segments) => Path.Combine(segments);
    private static string PlatformDrive => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "C:" : "/tmp";

    [Fact]
    public void AdrStatus_AllValues_AreDefined()
    {
        // Arrange & Act
        var values = Enum.GetValues<AdrStatus>();

        // Assert
        values.Should().Contain(AdrStatus.Unknown);
        values.Should().Contain(AdrStatus.Proposed);
        values.Should().Contain(AdrStatus.Accepted);
        values.Should().Contain(AdrStatus.Rejected);
        values.Should().Contain(AdrStatus.Superseded);
    }

    [Fact]
    public void AdrStatus_HasExpectedCount()
    {
        // Arrange & Act
        var values = Enum.GetValues<AdrStatus>();

        // Assert
        values.Length.Should().Be(5);
    }

    [Theory]
    [InlineData(AdrStatus.Unknown)]
    [InlineData(AdrStatus.Proposed)]
    [InlineData(AdrStatus.Accepted)]
    [InlineData(AdrStatus.Rejected)]
    [InlineData(AdrStatus.Superseded)]
    internal void AdrStatus_ToString_ReturnsName(AdrStatus status)
    {
        // Act
        var result = status.ToString();

        // Assert
        result.Should().NotBeNullOrEmpty();
        Enum.IsDefined(status).Should().BeTrue();
    }

    [Fact]
    public void AdrStatus_DefaultValue_IsUnknown()
    {
        // Arrange & Act
        var defaultStatus = default(AdrStatus);

        // Assert
        defaultStatus.Should().Be(AdrStatus.Unknown);
    }
}
