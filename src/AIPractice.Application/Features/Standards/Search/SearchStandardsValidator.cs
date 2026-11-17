using AIPractice.Application.Abstractions;
using AIPractice.Application.Exceptions;
using AIPractice.Application.Features.Proposals.ReviewWithStandards;
using static AIPractice.Application.Features.Proposals.Exceptions.BaseBusinessProposalException;

namespace AIPractice.Application.Features.Standards.Search;

public class SearchStandardsValidator(SearchStandardsCommand request) : BaseBusinessValidator
{
    public override Task<List<BusinessException>> ValidateAsync()
    {
        var exceptions = new List<BusinessException>();

        if (string.IsNullOrWhiteSpace(request.Question)
            || request.Question.Length > ReviewWithStandardsValidator.MaxQuestionLength)
        {
            exceptions.Add(new ProposalValidationException(
                message: $"Question must contain 1–{ReviewWithStandardsValidator.MaxQuestionLength} characters"));
        }

        return Task.FromResult(exceptions);
    }
}
