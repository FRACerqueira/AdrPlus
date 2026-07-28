// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using System.Reflection;

namespace AdrPlus.Tests.Localization;

public class TemplateResourcesTests
{
    private static readonly string[] BaseTemplateNames =
    [
        AppConstants.AdrTemplateFileName,
        "madr-template.md",
        "alexandrian-template.md",
        "business-case-template.md",
        "merson-template.md",
        "nygard-template.md",
        "planguage-template.md",
        "tyree-ackerman-template.md"
    ];

    public static readonly TheoryData<string> LanguageSuffixes = new()
    {
        "",
        "-ptbr",
        "-de",
        "-es",
        "-fr",
        "-it",
        "-ja",
        "-ko",
        "-nl",
        "-ru",
        "-zh"
    };

    public static TheoryData<string, string> BaseTemplateNamesAndSuffixes()
    {
        var data = new TheoryData<string, string>();
        foreach (var suffix in LanguageSuffixes)
        {
            foreach (var name in BaseTemplateNames)
            {
                data.Add(name, suffix);
            }
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(BaseTemplateNamesAndSuffixes))]
    public void ManifestResourceStream_ForEveryTemplateAndLanguage_IsPresent(string baseTemplateName, string suffix)
    {
        // Arrange
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(baseTemplateName);
        var extension = Path.GetExtension(baseTemplateName);
        var resourceFileName = $"{nameWithoutExtension}{suffix}{extension}";
        var assembly = Assembly.GetAssembly(typeof(AppConstants))!;

        // Act
        using var stream = assembly.GetManifestResourceStream($"{AppConstants.ResourceNamespace}.{resourceFileName}");

        // Assert
        stream.Should().NotBeNull($"resource '{resourceFileName}' should be embedded for suffix '{suffix}'");
    }
}
