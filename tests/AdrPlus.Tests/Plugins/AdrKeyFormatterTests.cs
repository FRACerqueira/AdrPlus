// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Plugins;

namespace AdrPlus.Tests.Plugins;

/// <summary>
/// Unit tests for <see cref="AdrKeyFormatter.Format"/> — the stable <c>adrKey</c> identity (spec §7) shared
/// between <see cref="PluginManager"/> and <c>SyncCommandHandler</c>.
/// </summary>
public class AdrKeyFormatterTests
{
    [Theory]
    [InlineData(7, 1, null, "0007-v1-r0")]
    [InlineData(7, 1, 0, "0007-v1-r0")]
    [InlineData(42, 2, 3, "0042-v2-r3")]
    [InlineData(1234, 1, null, "1234-v1-r0")]
    public void Format_ProducesExpectedKey(int number, int version, int? revision, string expected)
    {
        AdrKeyFormatter.Format(number, version, revision).Should().Be(expected);
    }
}
