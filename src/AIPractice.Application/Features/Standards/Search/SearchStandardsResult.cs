using AIPractice.BuildingBlock;

namespace AIPractice.Application.Features.Standards.Search;

public class SearchStandardsResult
{
    public IReadOnlyList<SourceHit> RetrievedSources { get; set; } = default!;
    public string EmbeddingModel { get; set; } = default!;
}
