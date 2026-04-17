// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using System.Globalization;

namespace AdrPlus.Tests.Helpers;

/// <summary>
/// Provides culture theory data for parameterized tests that involve date parsing or formatting.
/// Supplies both "en-US" and "pt-BR" cultures so tests exercise culture-sensitive behavior.
/// </summary>
internal static class CultureData
{
    /// <summary>
    /// Culture names used for date and confirmation tests.
    /// </summary>
    public static readonly TheoryData<string> Cultures =
    [
        "en-US",
        "pt-BR"
    ];

    /// <summary>
    /// Runs <paramref name="action"/> with the thread's current culture and UI culture temporarily
    /// set to the culture identified by <paramref name="cultureName"/>.
    /// </summary>
    public static void WithCulture(string cultureName, Action action)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var savedCulture = CultureInfo.CurrentCulture;
        var savedUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            action();
        }
        finally
        {
            CultureInfo.CurrentCulture = savedCulture;
            CultureInfo.CurrentUICulture = savedUiCulture;
        }
    }

    /// <summary>
    /// Runs <paramref name="asyncAction"/> with the thread's current culture and UI culture temporarily
    /// set to the culture identified by <paramref name="cultureName"/>.
    /// </summary>
    public static async Task WithCultureAsync(string cultureName, Func<Task> asyncAction)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var savedCulture = CultureInfo.CurrentCulture;
        var savedUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            await asyncAction();
        }
        finally
        {
            CultureInfo.CurrentCulture = savedCulture;
            CultureInfo.CurrentUICulture = savedUiCulture;
        }
    }
}
