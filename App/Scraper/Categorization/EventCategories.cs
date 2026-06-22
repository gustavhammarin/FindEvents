namespace App.Scraper.Categorization;

public static class EventCategories
{
    public const string Default = "Övrigt";

    public static readonly IReadOnlyList<string> Categories =
    [
        "Musik & Konsert",
        "Teater & Show",
        "Konst & Utställning",
        "Föreläsning & Utbildning",
        "Workshop & Kurs",
        "Sport & Tävling",
        "Träning & Motion",
        "Natur & Friluftsliv",
        "Mat & Dryck",
        "Marknad & Loppis",
        "Familj & Barn",
        "Seniorer & Pensionärer",
        "Hälsa & Välmående",
        "Socialt & Träffpunkt",
        Default
    ];
}
