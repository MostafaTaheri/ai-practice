using AIPractice.Application.Exceptions;

namespace AIPractice.Application.Features.Proposals.Exceptions;

public abstract class BaseBusinessProposalException : BusinessException
{
    public BaseBusinessProposalException(string code, string message, List<string> details) : base(
       code: $"Proposal-{code}",
       message: message,
       details: details)
    { }

    public sealed class ProposalValidationException : BaseBusinessProposalException
    {
        public ProposalValidationException(string message, List<string>? details = null) : base(code: "100", message: message, details: details ?? []) { }
    }
}
