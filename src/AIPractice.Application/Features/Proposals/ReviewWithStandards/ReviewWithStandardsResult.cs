using AIPractice.BuildingBlock;

namespace AIPractice.Application.Features.Proposals.ReviewWithStandards;

public class ReviewWithStandardsResult
{
    public StandardsAssessment Assessment { get; set; } = default!;
    public IReadOnlyList<SourceHit> RetrievedSources { get; set; } = default!;
    public string EmbeddingModel { get; set; } = default!;
    public string PromptVersion { get; set; } = default!;
}

public sealed class StandardsAssessment
{
    public required string Status { get; init; }
    public required StandardsFinding[] Findings { get; init; }
    public required string[] UnresolvedQuestions { get; init; }
}

public sealed class StandardsFinding
{
    public required string Guidance { get; init; }
    public required string SourceId { get; init; }
    public required string Quote { get; init; }
}
