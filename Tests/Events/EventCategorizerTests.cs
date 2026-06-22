using App.Scraper.Categorization;
using Xunit;

namespace Tests.Events;

public class EventCategorizerTests
{
    [Theory]
    [InlineData("Musik & Konsert", "Musik & Konsert")]
    [InlineData("musik & konsert", "Musik & Konsert")]
    [InlineData("Musik och Konsert", "Musik & Konsert")]
    [InlineData("Musik", "Musik & Konsert")]
    [InlineData("Familj & Barn.", "Familj & Barn")]
    public void Normalize_MapsLlmAnswerToFixedList(string llmAnswer, string expected)
    {
        Assert.Equal(expected, EventCategorizer.Normalize(llmAnswer));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Hittepåkategori")]
    [InlineData("AB")]
    public void Normalize_ReturnsNullForInvalidAnswer(string? llmAnswer)
    {
        Assert.Null(EventCategorizer.Normalize(llmAnswer));
    }

    [Theory]
    [InlineData("Sommarkonsert i parken", "Liveband spelar klassiker", "Musik & Konsert")]
    [InlineData("Julmarknad på torget", "Hantverk och loppis", "Marknad & Loppis")]
    [InlineData("Sagostund för barn", "Pyssel och lek på biblioteket", "Familj & Barn")]
    [InlineData("Fotboll: hemmamatch", "Serieomgång mot grannlaget", "Sport & Tävling")]
    [InlineData("Vernissage", "Ny utställning på konsthallen", "Konst & Utställning")]
    public void Categorize_MatchesKeywords(string title, string description, string expected)
    {
        Assert.Equal(expected, EventCategorizer.Categorize(title, description));
    }

    [Fact]
    public void Categorize_FallsBackToDefault()
    {
        Assert.Equal(EventCategories.Default, EventCategorizer.Categorize("Xyzzy", "Qwerty"));
        Assert.Equal(EventCategories.Default, EventCategorizer.Categorize(null, null));
    }

    [Fact]
    public void Categorize_TitleHitOutweighsDescriptionHit()
    {
        // "konsert" in title (3p) beats "teater" in description (1p)
        Assert.Equal("Musik & Konsert",
            EventCategorizer.Categorize("Konsert i kyrkan", "Efter föreställningen på teatern"));
    }

    [Fact]
    public void Categorize_DoesNotMatchSubstrings()
    {
        // "mat" must not hit inside "matchen"
        Assert.NotEqual("Mat & Dryck", EventCategorizer.Categorize("Vi ses efter matchen", null));
    }
}
