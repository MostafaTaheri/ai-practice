using AIPractice.Application.Abstractions;
using AIPractice.Application.Exceptions;
using static AIPractice.Application.Features.Proposals.Exceptions.BaseBusinessProposalException;

namespace AIPractice.Application.Features.Proposals.Summarize;

public class SummarizeValidator : BaseBusinessValidator
{
    private readonly SummarizeCommand _request;

    public SummarizeValidator(SummarizeCommand request)
    {
        _request = request;
    }

    public override async Task<List<BusinessException>> ValidateAsync()
    {
        var exceptions = new List<BusinessException>();

        if (string.IsNullOrWhiteSpace(_request.Proposal))
        {
            exceptions.Add(new ProposalValidationException(message: "Proposal must not be empty"));
        }

        return exceptions;
    }
}
