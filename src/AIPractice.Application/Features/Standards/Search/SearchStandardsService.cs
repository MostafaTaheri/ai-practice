using AIPractice.BuildingBlock;

namespace AIPractice.Application.Features.Standards.Search;

public sealed class SearchStandardsService(
    IEmbeddingClient embeddings,
    IStandardsIndex standardsIndex) : ISearchStandardsService
{
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(30);

    public async Task<SearchStandardsResult> SearchAsync(
        SearchStandardsCommand command, CancellationToken cancellationToken = default)
    {
        await new SearchStandardsValidator(command).AssertValidationAsync();

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(Deadline);

        if (embeddings.Model != standardsIndex.EmbeddingModel)
            throw new InvalidDataException("Embedding model changed; restart.");

        var vectors = await embeddings.EmbedAsync([command.Question], deadline.Token);

        return new SearchStandardsResult
        {
            RetrievedSources = standardsIndex.Search(vectors[0]),
            EmbeddingModel = standardsIndex.EmbeddingModel
        };
    }
}
