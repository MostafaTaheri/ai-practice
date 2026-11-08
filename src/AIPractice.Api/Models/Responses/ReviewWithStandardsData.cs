using AIPractice.Application.Features.Proposals.ReviewWithStandards;

namespace AIPractice.Api.Models.Responses;

public sealed class ReviewWithStandardsData
{
    public required StandardsAssessmentModel Assessment { get; init; }
    public required SourceHitModel[] RetrievedSources { get; init; }
    public required string EmbeddingModel { get; init; }
    public required string PromptVersion { get; init; }

    public static ReviewWithStandardsData From(ReviewWithStandardsResult result) => new()
    {
        Assessment = new StandardsAssessmentModel
        {
            Status = result.Assessment.Status,
            Findings = result.Assessment.Findings
                .Select(f => new StandardsFindingModel { Guidance = f.Guidance, SourceId = f.SourceId, Quote = f.Quote })
                .ToArray(),
            UnresolvedQuestions = result.Assessment.UnresolvedQuestions
        },
        RetrievedSources = result.RetrievedSources
            .Select(h => new SourceHitModel
            {
                Id = h.Source.Id,
                Title = h.Source.Title,
                Text = h.Source.Text,
                Revision = h.Source.Revision,
                Score = h.Score
            })
            .ToArray(),
        EmbeddingModel = result.EmbeddingModel,
        PromptVersion = result.PromptVersion
    };
}

public sealed class StandardsAssessmentModel
{
    public required string Status { get; init; }
    public required StandardsFindingModel[] Findings { get; init; }
    public required string[] UnresolvedQuestions { get; init; }
}

public sealed class StandardsFindingModel
{
    public required string Guidance { get; init; }
    public required string SourceId { get; init; }
    public required string Quote { get; init; }
}

public sealed class SourceHitModel
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Text { get; init; }
    public required string Revision { get; init; }
    public required double Score { get; init; }
}
