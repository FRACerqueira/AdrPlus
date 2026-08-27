// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Infrastructure.UI;
using AdrPlus.Plugins;
using Microsoft.Extensions.Logging;

namespace AdrPlus.Tests.Plugins;

/// <summary>
/// Unit tests for <see cref="PluginActivationGate.WarnMissingActivePlugins"/> — the wrapper every dispatching
/// command now calls instead of <see cref="IConsoleWriter.PromptWarnMissingActivePlugins"/> directly, so a
/// configured-active plugin failing to load is also recorded in the log file, not just the console.
/// </summary>
/// <remarks>
/// The log half of this method isn't independently asserted here — this project has no existing convention or
/// helper for inspecting <see cref="ILogger"/> call content anywhere in its test suite, and introducing one for
/// a single Medium-severity finding wasn't judged worth the added complexity. Regression coverage focuses on
/// what these tests can assert reliably: that wrapping the existing console call didn't change its behavior.
/// </remarks>
public class PluginActivationGateTests
{
    [Fact]
    public void WarnMissingActivePlugins_WithMissingNames_StillCallsConsoleWithTheSameNames()
    {
        var logger = Substitute.For<ILogger>();
        var console = Substitute.For<IConsoleWriter>();

        PluginActivationGate.WarnMissingActivePlugins(logger, console, ["ghost-plugin"]);

        console.Received(1).PromptWarnMissingActivePlugins(Arg.Is<IReadOnlyList<string>>(names => names.Contains("ghost-plugin")));
    }

    [Fact]
    public void WarnMissingActivePlugins_WithNoMissingNames_StillCallsConsoleWithEmptyList()
    {
        var logger = Substitute.For<ILogger>();
        var console = Substitute.For<IConsoleWriter>();

        PluginActivationGate.WarnMissingActivePlugins(logger, console, []);

        console.Received(1).PromptWarnMissingActivePlugins(Arg.Is<IReadOnlyList<string>>(names => names.Count == 0));
    }
}
