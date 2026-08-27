// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Infrastructure.UI;

namespace AdrPlus.Tests.Infrastructure.UI;

/// <summary>
/// Covers <see cref="PromptConsole"/>'s pure, static <c>SuggestSimilar</c> helper (the wizard's Scope/Domain
/// "did you mean" suggestion filter). The interactive prompt methods themselves wrap <c>PromptPlus</c>
/// terminal controls and aren't covered here.
/// </summary>
public class PromptConsoleTests
{
    [Fact]
    public void SuggestSimilar_WithEmptyInput_ReturnsAllCandidatesUnfiltered()
    {
        string[] candidates = ["Backend", "Frontend", "Data"];

        var result = PromptConsole.SuggestSimilar("", candidates);

        result.Should().Equal(candidates);
    }

    [Fact]
    public void SuggestSimilar_ExcludesCandidatesBelowThresholdAndWithoutSubstringMatch()
    {
        string[] candidates = ["Backend", "Frontend", "Billing"];

        var result = PromptConsole.SuggestSimilar("Backand", candidates);

        result.Should().ContainSingle().Which.Should().Be("Backend");
    }

    [Fact]
    public void SuggestSimilar_NeverBlocksOrRejects_ReturnsEmptyRatherThanThrowingWhenNothingMatches()
    {
        string[] candidates = ["Backend", "Frontend"];

        var result = PromptConsole.SuggestSimilar("CompletelyUnrelatedTerm", candidates);

        result.Should().BeEmpty();
    }

    [Fact]
    public void SuggestSimilar_RanksSubstringMatchesBeforeFuzzyMatches()
    {
        // "Bakend" (typo, missing the 'c') is a literal substring of "MyBakendService" but not of
        // "Backend" - "Backend" only qualifies via Jaro-Winkler similarity to the typo itself. The
        // substring match must be ranked first regardless of similarity score.
        string[] candidates = ["Backend", "MyBakendService"];

        var result = PromptConsole.SuggestSimilar("Bakend", candidates);

        result.Should().Equal("MyBakendService", "Backend");
    }

    [Fact]
    public void SuggestSimilar_RanksHigherSimilarityFirstAmongNonSubstringMatches()
    {
        string[] candidates = ["Secuity", "Security"];

        // Both differ from "Security" only by an edit or two, but "Security" itself is the closer match
        // to a search for "Securty" than the already-mistyped "Secuity" is.
        var result = PromptConsole.SuggestSimilar("Securty", candidates);

        result.Should().HaveCount(2);
        result[0].Should().Be("Security");
    }

    [Fact]
    public void SimilaritySuggestionThreshold_Is080()
    {
        // Regression guard: StringSimilarityExtensionsTests' near-duplicate cases assume this exact value.
        PromptConsole.SimilaritySuggestionThreshold.Should().Be(0.80);
    }
}
