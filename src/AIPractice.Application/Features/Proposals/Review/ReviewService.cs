using System.Text.Json;
using AIPractice.Application.Features.Proposals.Exceptions;
using AIPractice.BuildingBlock;

namespace AIPractice.Application.Features.Proposals.Review;

public sealed class ReviewService(IArchitectureModelClient modelClient) : IReviewService
{
    public const string PromptVersion = "architecture-review-v1";
    private const int MaxAttempts = 2;
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(90);

    public async Task<ReviewResult> ReviewAsync(ReviewCommand command, CancellationToken cancellationToken = default)
    {
        await new ReviewValidator(command).AssertValidationAsync();

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(Deadline);

        List<string> errors = [];
        string? feedback = null;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var json = await modelClient.GenerateReviewAsync(command.Proposal, feedback, deadline.Token);

            deadline.Token.ThrowIfCancellationRequested();

            try
            {
                var review = ReviewOutputValidator.Parse(json);
                errors = ReviewOutputValidator.Validate(review, command.Proposal);

                if (errors.Count == 0)
                    return new ReviewResult { Review = review, Attempts = attempt, PromptVersion = PromptVersion };
            }
            catch (JsonException)
            {
                errors = ["Return valid JSON with the exact required contract."];
            }

            feedback = "The previous attempt failed validation. "
                + "Generate a replacement review. Fix: "
                + string.Join(" ", errors);
        }

        throw new BaseBusinessProposalException.ReviewValidationException(errors);
    }
}
