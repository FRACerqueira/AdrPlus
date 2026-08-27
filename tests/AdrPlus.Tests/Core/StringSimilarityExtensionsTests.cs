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
    public void JaroWinklerSimilarity_FoldsDiacritics()
    {
        // Realistic pt-BR case: a user typing an unaccented variant of an existing scope/domain value.
        // Regression guard for a bug where this landed at exactly 0.7999999999999999 (one ULP under the
        // 0.80 suggestion threshold) because accented letters weren't folded before comparison.
        "Não".JaroWinklerSimilarity("Nao").Should().Be(1.0);
        "Autenticação".JaroWinklerSimilarity("Autenticacao").Should().Be(1.0);
    }

    [Fact]
    public void JaroWinklerSimilarity_EmojiPrefixDoesNotInflateScore_ViaSharedSurrogate()
    {
        // Regression guard: comparing by `char` (UTF-16 code unit) instead of Unicode codepoint splits an
        // astral-plane character (e.g. an emoji surrogate pair) into two independent "characters". "😀" and
        // "😻" share the same UTF-16 lead surrogate (both are outside the Basic Multilingual Plane), so a
        // code-unit comparison would count that lead surrogate as one matching "character" even though the
        // two emoji are completely unrelated. A codepoint-aware comparison must score this exactly the same
        // as swapping in two ASCII letters that share no such accidental relationship.
        var withSharedSurrogatePrefix = "😀Security".JaroWinklerSimilarity("😻Payments");
        var withUnrelatedAsciiPrefix = "ZSecurity".JaroWinklerSimilarity("QPayments");
        withSharedSurrogatePrefix.Should().Be(withUnrelatedAsciiPrefix);
    }

    [Fact]
    public void JaroWinklerSimilarity_UnrelatedSingleEmoji_ScoresZero()
    {
        // A codepoint-aware comparison of two completely different single characters (even ones that
        // happen to share a UTF-16 lead surrogate) must score 0, not a partial match.
        "😀".JaroWinklerSimilarity("😻").Should().Be(0.0);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("A", "")]
    [InlineData("A", "A")]
    [InlineData("Security", "Secuity")]
    [InlineData("Frontend", "Payments")]
    [InlineData("😀", "😻")]
    [InlineData("Não", "Nao")]
    public void JaroWinklerSimilarity_AlwaysStaysWithinZeroToOneRange(string a, string b)
    {
        var score = a.JaroWinklerSimilarity(b);
        score.Should().BeInRange(0.0, 1.0);
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
