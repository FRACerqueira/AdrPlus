// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;
using AdrPlus.Tests.Helpers;

namespace AdrPlus.Tests.Localization;

public class HeaderLocalizationTests
{
    public static readonly TheoryData<string, string> CultureAndExpectedCreatedLabel = new()
    {
        { "en-US", "Created" },
        { "pt-BR", "Criado" },
        { "de-DE", "Erstellt" },
        { "es-ES", "Creado" },
        { "fr-FR", "Créé" },
        { "it-IT", "Creato" },
        { "ru-RU", "Создано" }
    };

    [Theory]
    [MemberData(nameof(CultureAndExpectedCreatedLabel))]
    public void AdrPlusRepoConfig_HeaderLabels_ReflectCurrentUICultureAtConstruction(string cultureName, string expectedCreatedLabel)
    {
        // Arrange & Act
        AdrPlusRepoConfig? config = null;
        CultureData.WithCulture(cultureName, () =>
        {
            config = new AdrPlusRepoConfig("docs/adr", "template content");
        });

        // Assert
        config!.HeaderTitleStatusCreated.Should().Be(expectedCreatedLabel);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("pt-BR")]
    [InlineData("de-DE")]
    [InlineData("es-ES")]
    [InlineData("fr-FR")]
    [InlineData("it-IT")]
    [InlineData("ja-JP")]
    [InlineData("ko-KR")]
    [InlineData("nl-BE")]
    [InlineData("ru-RU")]
    [InlineData("zh-CN")]
    public void AdrPlusRepoConfig_StatusMapping_HasFiveDistinctValues(string cultureName)
    {
        // Arrange & Act
        AdrPlusRepoConfig? config = null;
        CultureData.WithCulture(cultureName, () =>
        {
            config = new AdrPlusRepoConfig("docs/adr", "template content");
        });

        // Assert
        config!.StatusMapping.Values.Distinct().Should().HaveCount(5);
    }
}
