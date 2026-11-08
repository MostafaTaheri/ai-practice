using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIPractice.BuildingBlock;
using Microsoft.Extensions.Options;

namespace AIPractice.Infrastructure.ExternalServices.Clients.Ollama;

public sealed class OllamaEmbeddingClient(
    HttpClient httpClient,
    IOptions<OllamaOptions> options) : IEmbeddingClient
{
    public string Model => options.Value.EmbeddingModel;

    public async Task<float[][]> EmbedAsync(string[] input, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/embed",
            new EmbedRequest(Model, input, Truncate: false),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<EmbedResponse>(cancellationToken);

        var vectors = envelope?.Embeddings;
        if (vectors is null || vectors.Length != input.Length || vectors.Length == 0)
            throw new JsonException("Unexpected embedding count.");

        var dimension = vectors[0]?.Length ?? 0;
        if (dimension == 0 || vectors.Any(vector =>
            vector is null || vector.Length != dimension
            || vector.Any(value => !float.IsFinite(value))
            || vector.All(value => value == 0)))
            throw new JsonException("Invalid embedding vectors.");

        return vectors;
    }

    private sealed record EmbedRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] string[] Input,
        [property: JsonPropertyName("truncate")] bool Truncate);

    private sealed record EmbedResponse(
        [property: JsonPropertyName("embeddings")] float[][]? Embeddings);
}
