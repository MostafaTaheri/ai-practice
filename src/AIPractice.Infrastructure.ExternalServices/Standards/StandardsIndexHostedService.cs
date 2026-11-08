using AIPractice.BuildingBlock;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIPractice.Infrastructure.ExternalServices.Standards;

/// <summary>
/// Builds the index once at startup. A failure aborts startup so the API never
/// accepts requests with an empty knowledge base.
/// </summary>
public sealed class StandardsIndexHostedService(
    IEmbeddingClient embeddings,
    IOptions<StandardsOptions> options,
    ILogger<StandardsIndexHostedService> logger) : IStandardsIndex, IHostedService
{
    private static readonly TimeSpan InitializationDeadline = TimeSpan.FromSeconds(120);

    private volatile InMemoryStandardsIndex? _index;

    public string EmbeddingModel => Index.EmbeddingModel;

    public IReadOnlyList<SourceHit> Search(float[] queryVector) => Index.Search(queryVector);

    private InMemoryStandardsIndex Index =>
        _index ?? throw new InvalidOperationException("Standards index is not initialized.");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var directory = string.IsNullOrWhiteSpace(options.Value.Directory)
            ? Path.Combine(AppContext.BaseDirectory, "standards")
            : options.Value.Directory;

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linked.CancelAfter(InitializationDeadline);

        _index = await InMemoryStandardsIndex.BuildAsync(directory, embeddings, linked.Token);

        logger.LogInformation(
            "Standards index built from {Directory} using embedding model {Model}",
            directory, _index.EmbeddingModel);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
