namespace AIPractice.Application.Features.Proposals.Summarize;

public interface ISummarizeService
{
    Task<SummarizeResult> SummarizeAsync(SummarizeCommand command, CancellationToken cancellationToken = default);
}