namespace App.Configuration;

public class MistralSettings
{
    public string BaseUrl { get; set; } = "https://api.mistral.ai/v1";
    public string CompletionModel { get; set; } = "mistral-small-latest";
    public string EmbeddingModel { get; set; } = "mistral-embed";
    public string? ApiKey { get; set; }
}
