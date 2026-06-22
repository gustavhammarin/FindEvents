namespace App.Configuration;

public class LlmSettings
{
    public string BaseUrl { get; set; } = "http://localhost:8080";
    public string Model { get; set; } = "mlx-community/Qwen3-8B-4bit";
    public string? ApiKey { get; set; }
}
