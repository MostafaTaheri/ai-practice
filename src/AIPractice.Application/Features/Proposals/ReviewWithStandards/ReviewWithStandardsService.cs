using AIPractice.Application.Features.Proposals.Exceptions;
using AIPractice.BuildingBlock;

namespace AIPractice.Application.Features.Proposals.ReviewWithStandards;

public sealed class ReviewWithStandardsService(
    IEmbeddingClient embeddings,
    IStandardsIndex standardsIndex,
    IGroundedStandardsClient generator) : IReviewWithStandardsService
{
    public const string PromptVersion = "standards-review-v1";
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(90);

    public async Task<ReviewWithStandardsResult> ReviewAsync(
        ReviewWithStandardsCommand command, CancellationToken cancellationToken = default)
    {
        await new ReviewWithStandardsValidator(command).AssertValidationAsync();

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(Deadline);

        if (embeddings.Model != standardsIndex.EmbeddingModel)
            throw new InvalidDataException("Embedding model changed; restart.");

        var vectors = await embeddings.EmbedAsync([command.Question], deadline.Token);
        var hits = standardsIndex.Search(vectors[0]);

        var json = await generator.GenerateAsync(command.Proposal, command.Question, hits, deadline.Token);
        deadline.Token.ThrowIfCancellationRequested();

        StandardsAssessment assessment;
        try
        {
            assessment = GroundedAssessmentValidator.ParseAndValidate(json, hits);
        }
        catch (InvalidDataException ex)
        {
            throw new BaseBusinessProposalException.StandardsValidationException(
            [
                ex.Message,
                "Retrieved: " + string.Join(", ", hits.Select(h => $"{h.Source.Id}={h.Score:F3}")),
                "Model output: " + (json.Length > 800 ? json[..800] : json)
            ]);
        }

        return new ReviewWithStandardsResult
        {
            Assessment = assessment,
            RetrievedSources = hits,
            EmbeddingModel = standardsIndex.EmbeddingModel,
            PromptVersion = PromptVersion
        };
    }
}
