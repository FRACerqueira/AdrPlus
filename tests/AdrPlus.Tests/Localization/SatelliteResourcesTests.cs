// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using System.Globalization;

namespace AdrPlus.Tests.Localization;

public class SatelliteResourcesTests
{
    public static readonly TheoryData<string> SupportedCultures = new()
    {
        "en-US",
        "pt-BR",
        "de-DE",
        "es-ES",
        "fr-FR",
        "it-IT",
        "ja-JP",
        "ko-KR",
        "nl-BE",
        "ru-RU",
        "zh-CN"
    };

    [Theory]
    [MemberData(nameof(SupportedCultures))]
    public void ResourceManager_ForSupportedCulture_ReturnsResourceSet(string cultureName)
    {
        // Arrange
        var culture = CultureInfo.GetCultureInfo(cultureName);

        // Act
        var resourceSet = global::AdrPlus.Resources.AdrPlus.ResourceManager.GetResourceSet(culture, true, false);

        // Assert
        resourceSet.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(SupportedCultures))]
    public void ResourceManager_ForSupportedCulture_ResolvesKnownKey(string cultureName)
    {
        // Arrange
        var culture = CultureInfo.GetCultureInfo(cultureName);

        // Act
        var value = global::AdrPlus.Resources.AdrPlus.ResourceManager.GetString(nameof(global::AdrPlus.Resources.AdrPlus.InitConfigLanguageEnglish), culture);

        // Assert
        value.Should().NotBeNullOrEmpty();
    }
}
