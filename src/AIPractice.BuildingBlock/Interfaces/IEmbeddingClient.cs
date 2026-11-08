namespace AIPractice.BuildingBlock;

public interface IEmbeddingClient
{
    string Model { get; }

    Task<float[][]> EmbedAsync(
        string[] input,
        CancellationToken cancellationToken);
}
