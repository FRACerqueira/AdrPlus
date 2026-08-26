// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;

namespace AdrPlus.Tests.Core;

public class StringSimilarityExtensionsTests
{
    [Fact]
    public void JaroWinklerSimilarity_WithIdenticalStrings_ReturnsOne()
    {
        "Security".JaroWinklerSimilarity("Security").Should().Be(1.0);
    }

    [Fact]
    public void JaroWinklerSimilarity_IsCaseInsensitive()
    {
        "Security".JaroWinklerSimilarity("security").Should().Be(1.0);
    }

    [Fact]
    public void JaroWinklerSimilarity_WithBothEmpty_ReturnsOne()
    {
        "".JaroWinklerSimilarity("").Should().Be(1.0);
    }

    [Theory]
    [InlineData("Security", "")]
    [InlineData("", "Security")]
    public void JaroWinklerSimilarity_WithOneEmpty_ReturnsZero(string a, string b)
    {
        a.JaroWinklerSimilarity(b).Should().Be(0.0);
    }

    [Theory]
    [InlineData("Security", "Secuity")]   // one character dropped
    [InlineData("Payment", "Payemnt")]    // adjacent transposition
    [InlineData("Backend", "Back-End")]   // punctuation/case variant
    [InlineData("Payments", "Payment")]   // trailing plural
    [InlineData("Auth", "Authentication")]// shared prefix, very different length
    public void JaroWinklerSimilarity_WithNearDuplicates_ScoresAboveSuggestionThreshold(string a, string b)
    {
        // 0.80 matches the suggestion threshold PromptConsole uses to surface a "did you mean" hint.
        a.JaroWinklerSimilarity(b).Should().BeGreaterThanOrEqualTo(0.80);
    }

    [Theory]
    [InlineData("Frontend", "Payments")]
    [InlineData("Billing", "UI")]
    public void JaroWinklerSimilarity_WithUnrelatedStrings_ScoresBelowSuggestionThreshold(string a, string b)
    {
        a.JaroWinklerSimilarity(b).Should().BeLessThan(0.80);
    }

    [Fact]
    public void JaroWinklerSimilarity_DoesNotUnderstandTranslation()
    {
        // Lexical similarity has no notion of meaning: a translation of the same concept scores low,
        // same as any other unrelated string. This is a known, accepted limitation (see ADR006 discussion).
        "Security".JaroWinklerSimilarity("Segurança").Should().BeLessThan(0.80);
    }

    [Theory]
    [InlineData("Backend", "Back-End")]
    [InlineData("Security", "Secuity")]
    [InlineData("Auth", "Authentication")]
    public void JaroWinklerSimilarity_IsSymmetric(string a, string b)
    {
        a.JaroWinklerSimilarity(b).Should().BeApproximately(b.JaroWinklerSimilarity(a), 0.0001);
    }

    [Fact]
    public void JaroWinklerSimilarity_NullSource_Throws()
    {
        string? source = null;
        var act = () => source!.JaroWinklerSimilarity("Security");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void JaroWinklerSimilarity_NullTarget_Throws()
    {
        var act = () => "Security".JaroWinklerSimilarity(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
