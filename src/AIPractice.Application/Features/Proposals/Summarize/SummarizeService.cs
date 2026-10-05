using AIPractice.BuildingBlock;

namespace AIPractice.Application.Features.Proposals.Summarize;

public sealed class SummarizeService(IArchitectureModelClient modelClient) : ISummarizeService
{
    public async Task<SummarizeResult> SummarizeAsync(SummarizeCommand command, CancellationToken cancellationToken = default)
    {
        await new SummarizeValidator(command).AssertValidationAsync();

        var summary = await modelClient.SummarizeAsync(command.Proposal, cancellationToken);

        return new SummarizeResult { Summary = summary };
    }
}