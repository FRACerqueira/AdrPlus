// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Tests;

/// <summary>
/// Unit tests for <see cref="Program.ResolveAppVersion"/>.
/// </summary>
public class ProgramTests
{
    [Fact]
    public void ResolveAppVersion_WithInformationalVersion_ReturnsItVerbatim()
    {
        var result = Program.ResolveAppVersion("1.0.0-beta", new Version(1, 0, 0));

        result.Should().Be("1.0.0-beta");
    }

    [Fact]
    public void ResolveAppVersion_WithSourceControlMetadataSuffix_StripsIt()
    {
        var result = Program.ResolveAppVersion("1.0.0-beta+abc1234", new Version(1, 0, 0));

        result.Should().Be("1.0.0-beta");
    }

    [Fact]
    public void ResolveAppVersion_WithoutInformationalVersion_FallsBackToAssemblyVersion()
    {
        var result = Program.ResolveAppVersion(null, new Version(1, 2, 3, 4));

        result.Should().Be("1.2.3");
    }

    [Fact]
    public void ResolveAppVersion_WithBlankInformationalVersion_FallsBackToAssemblyVersion()
    {
        var result = Program.ResolveAppVersion("   ", new Version(0, 6, 3));

        result.Should().Be("0.6.3");
    }

    [Fact]
    public void ResolveAppVersion_WithNeitherSource_ReturnsZeroVersion()
    {
        var result = Program.ResolveAppVersion(null, null);

        result.Should().Be("0.0.0");
    }
}
