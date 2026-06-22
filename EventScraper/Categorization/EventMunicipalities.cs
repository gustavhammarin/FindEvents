namespace EventScraper.Categorization;

public static class EventMunicipalities
{
    public static readonly IReadOnlyList<string> Municipalities =
    [
        "Aneby", "Eksjö", "Gislaved", "Gnosjö", "Habo",
        "Jönköping", "Mullsjö", "Nässjö", "Sävsjö", "Tranås",
        "Vaggeryd", "Vetlanda", "Värnamo"
    ];

    private const string Default = "Jönköping";

    public static string Normalize(string? raw, string? sourceHint = null)
    {
        var fallback = Municipalities.Contains(sourceHint ?? "") ? sourceHint! : Default;
        if (string.IsNullOrWhiteSpace(raw)) return fallback;

        var cleaned = raw.Trim().TrimEnd('s'); // "Jönköpings" → "Jönköping"

        var exact = Municipalities.FirstOrDefault(m =>
            string.Equals(m, cleaned, StringComparison.OrdinalIgnoreCase));
        if (exact is not null) return exact;

        // Also try without trimming the trailing 's'
        var exactRaw = Municipalities.FirstOrDefault(m =>
            string.Equals(m, raw.Trim(), StringComparison.OrdinalIgnoreCase));
        if (exactRaw is not null) return exactRaw;

        var partial = Municipalities.FirstOrDefault(m =>
            cleaned.StartsWith(m, StringComparison.OrdinalIgnoreCase) ||
            m.StartsWith(cleaned, StringComparison.OrdinalIgnoreCase));
        if (partial is not null) return partial;

        return fallback;
    }
}
