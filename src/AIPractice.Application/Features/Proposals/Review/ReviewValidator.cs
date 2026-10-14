using AIPractice.Application.Abstractions;
using AIPractice.Application.Exceptions;
using static AIPractice.Application.Features.Proposals.Exceptions.BaseBusinessProposalException;

namespace AIPractice.Application.Features.Proposals.Review;

public class ReviewValidator : BaseBusinessValidator
{
    public const int MaxProposalLength = 12_000;

    private readonly ReviewCommand _request;

    public ReviewValidator(ReviewCommand request)
    {
        _request = request;
    }

    public override Task<List<BusinessException>> ValidateAsync()
    {
        var exceptions = new List<BusinessException>();

        if (string.IsNullOrWhiteSpace(_request.Proposal)
            || _request.Proposal.Length > MaxProposalLength)
        {
            exceptions.Add(new ProposalValidationException(
                message: $"Proposal must contain 1–{MaxProposalLength} characters"));
        }

        return Task.FromResult(exceptions);
    }
}
