using AIPractice.BuildingBlock;

namespace AIPractice.Application.Features.Proposals.Review;

public interface IReviewService
{
    Task<ReviewResult> ReviewAsync(ReviewCommand command, CancellationToken cancellationToken = default);
}
