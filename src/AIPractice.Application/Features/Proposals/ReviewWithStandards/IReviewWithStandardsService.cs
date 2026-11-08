namespace AIPractice.Application.Features.Proposals.ReviewWithStandards;

public interface IReviewWithStandardsService
{
    Task<ReviewWithStandardsResult> ReviewAsync(ReviewWithStandardsCommand command, CancellationToken cancellationToken = default);
}
