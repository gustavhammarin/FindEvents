namespace EventScraper.Configuration;

public class MistralSettings
{
    public string BaseUrl { get; set; } = "https://api.mistral.ai/v1";
    public string Model { get; set; } = "mistral-small-latest";
    public string? ApiKey { get; set; }
}
