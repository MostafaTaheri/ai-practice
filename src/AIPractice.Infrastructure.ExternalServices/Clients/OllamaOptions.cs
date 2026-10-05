namespace AIPractice.Infrastructure.ExternalServices.Clients.Ollama;

public class OllamaOptions
{
    public const string SectionName = "Ollama";
    public string BaseUrl { get; set; } = null!;
    public string Model { get; set; } = "qwen2.5:3b";
}
