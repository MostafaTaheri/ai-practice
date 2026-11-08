using AIPractice.Application.Abstractions;
using AIPractice.Application.Exceptions;
using static AIPractice.Application.Features.Proposals.Exceptions.BaseBusinessProposalException;

namespace AIPractice.Application.Features.Proposals.ReviewWithStandards;

public class ReviewWithStandardsValidator : BaseBusinessValidator
{
    public const int MaxProposalLength = 12_000;
    public const int MaxQuestionLength = 200;

    private readonly ReviewWithStandardsCommand _request;

    public ReviewWithStandardsValidator(ReviewWithStandardsCommand request)
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

        if (string.IsNullOrWhiteSpace(_request.Question)
            || _request.Question.Length > MaxQuestionLength)
        {
            exceptions.Add(new ProposalValidationException(
                message: $"Question must contain 1–{MaxQuestionLength} characters"));
        }

        return Task.FromResult(exceptions);
    }
}
