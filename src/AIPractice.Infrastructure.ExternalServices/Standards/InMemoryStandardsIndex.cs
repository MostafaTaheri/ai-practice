using System.Security.Cryptography;
using System.Text;
using AIPractice.BuildingBlock;

namespace AIPractice.Infrastructure.ExternalServices.Standards;

/// <summary>
/// Immutable exact-search index over the sample standards. Rebuilt only on restart.
/// </summary>
public sealed class InMemoryStandardsIndex : IStandardsIndex
{
    private readonly (StandardSource Source, float[] Vector)[] _entries;

    public string EmbeddingModel { get; }

    private InMemoryStandardsIndex(StandardSource[] sources, float[][] vectors, string model)
    {
        _entries = sources.Select((source, i) => (source, vectors[i])).ToArray();
        EmbeddingModel = model;
    }

    public static async Task<InMemoryStandardsIndex> BuildAsync(
        string directory,
        IEmbeddingClient embeddings,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(directory))
            throw new InvalidDataException($"Standards directory not found: {directory}");

        var files = Directory.GetFiles(directory, "*.md")
            .OrderBy(path => path, StringComparer.Ordinal).ToArray();

        if (files.Length != 10)
            throw new InvalidDataException("Expected 10 short sample standards.");

        var sources = new List<StandardSource>();
        foreach (var file in files)
        {
            var text = await File.ReadAllTextAsync(file, cancellationToken);
            if (string.IsNullOrWhiteSpace(text) || text.Length > 1200)
                throw new InvalidDataException("Invalid sample document size.");

            var title = text.Split('\n')[0].Trim().TrimStart('#').Trim();
            var revision = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

            sources.Add(new StandardSource(
                Path.GetFileNameWithoutExtension(file), title, text, revision));
        }

        if (sources.Select(source => source.Id)
            .Distinct(StringComparer.Ordinal).Count() != sources.Count)
            throw new InvalidDataException("Duplicate source IDs.");

        var vectors = await embeddings.EmbedAsync(
            sources.Select(source => source.Text).ToArray(), cancellationToken);

        return new InMemoryStandardsIndex(sources.ToArray(), vectors, embeddings.Model);
    }

    public IReadOnlyList<SourceHit> Search(float[] queryVector)
    {
        // Top three are candidates, not necessarily relevant evidence.
        return _entries.Select(entry => new SourceHit(
                entry.Source, VectorMath.Cosine(queryVector, entry.Vector)))
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.Source.Id, StringComparer.Ordinal)
            .Take(3).ToArray();
    }
}
