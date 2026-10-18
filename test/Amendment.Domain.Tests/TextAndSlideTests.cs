using Amendment.Domain.Approval;
using Amendment.Domain.Languages;
using Amendment.Domain.Model;

namespace Amendment.Domain.Tests;

public class AmendmentTextCleanupTests
{
    // Same goldens as legacy Characterization_CleanupAmendmentText_Golden (test/Amendment.Server.Tests).
    [Theory]
    [InlineData("We ~~shall~~ <u>will</u> proceed.", "We  will proceed.")]
    [InlineData("Slide one ~~drop\n**NEWSLIDE**\nthis~~ end\n**NEWSLIDE**\nSlide <u>two</u>", "Slide one  end\n**NEWSLIDE**\nSlide two")]
    [InlineData("Keep ~~unclosed", "Keep ~~unclosed")]
    [InlineData("Keep <u>unclosed", "Keep <u>unclosed")]
    [InlineData("a ~~b~~ c ~~d", "a  c ~~d")]
    [InlineData("x <u class=\"y\">t</u> z", "x <u class=\"y\">t</u> z")]
    [InlineData("x ~~<u>y</u>~~ z", "x  z")]
    [InlineData("x <u>a ~~b~~ c</u> z", "x a  c z")]
    [InlineData("<U>Up</U> and <u>low\nline</u>", "Up and low\nline")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Cleanup_MatchesLegacyGoldens(string? input, string expected) =>
        Assert.Equal(expected, AmendmentTextCleanup.Cleanup(input));

    [Theory]
    [InlineData("plain", false)]
    [InlineData("a ~~b~~", true)]
    [InlineData("a <U>b</U>", true)]
    [InlineData("a ~~b", false)]
    public void ContainsAmendmentFormatting(string input, bool expected) =>
        Assert.Equal(expected, AmendmentTextCleanup.ContainsAmendmentFormatting(input));

    [Fact]
    public void Property_CleanupNeverGrowsText_AndRemovesStrikethroughBeforeUnderline()
    {
        var random = new Random(1234);
        string[] tokens = ["a", " ", "~~", "<u>", "</u>", "\n", "**NEWSLIDE**", "b"];
        for (var i = 0; i < 500; i++)
        {
            var input = string.Concat(Enumerable.Range(0, random.Next(1, 20)).Select(_ => tokens[random.Next(tokens.Length)]));
            var once = AmendmentTextCleanup.Cleanup(input);
            Assert.True(once.Length <= input.Length, input);
            Assert.Equal(AmendmentTextCleanup.RemoveUnderline(AmendmentTextCleanup.RemoveStrikethrough(input)), once);
        }
    }
}

public class SlideDeckTests
{
    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("one", 1)]
    [InlineData(Fixtures.ThreeSlides, 3)]
    [InlineData("**NEWSLIDE**", 2)]
    [InlineData("a**NEWSLIDE****NEWSLIDE**b", 3)]
    public void PageCount_MatchesLegacySplit(string? markdown, int expected) =>
        Assert.Equal(expected, SlideDeck.PageCount(markdown));

    [Theory]
    [InlineData(0, 3, 0)]
    [InlineData(2, 3, 2)]
    [InlineData(3, 3, 0)]
    [InlineData(-1, 3, 0)]
    [InlineData(5, 0, 0)]
    public void NormalizePage_ResetsOutOfRangeToZero(int page, int count, int expected) =>
        Assert.Equal(expected, SlideDeck.NormalizePage(page, count));

    [Fact]
    public void Property_NormalizedPageIsAlwaysValid()
    {
        for (var count = 0; count < 10; count++)
            for (var page = -3; page < 13; page++)
            {
                var normalized = SlideDeck.NormalizePage(page, count);
                Assert.True(normalized == 0 || (normalized > 0 && normalized < count));
            }
    }

    [Fact]
    public void Split_PreservesSlideText() =>
        Assert.Equal(new[] { "Slide 1\n", "\nSlide 2\n", "\nSlide 3" }, SlideDeck.Split(Fixtures.ThreeSlides));
}

public class LanguageCodeTests
{
    [Theory]
    [InlineData("en", "en")]
    [InlineData(" EN ", "en")]
    [InlineData("pt_BR", "pt-br")]
    [InlineData("ENG", "en")]
    [InlineData("SPA", "es")]
    [InlineData("FRA", "fr")]
    [InlineData("Spanish", "es")]
    [InlineData("french", "fr")]
    public void Normalize(string input, string expected)
    {
        Assert.True(LanguageCode.TryNormalize(input, out var code));
        Assert.Equal(expected, code.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("e")]
    [InlineData("english!")]
    [InlineData("en--us")]
    public void Invalid(string? input) => Assert.False(LanguageCode.TryNormalize(input, out _));

    [Fact]
    public void SameLanguage_HasSameBodyKey() =>
        Assert.Equal(ItemKeys.BodySk(LanguageCode.Parse("ENG")), ItemKeys.BodySk(LanguageCode.Parse("en")));

    [Fact]
    public void Resolve_RejectsUnconfiguredLanguage() =>
        Assert.Equal(DomainErrorCode.ValidationError, SupportedLanguages.Default.Resolve("de").Errors.Single().Code);
}
