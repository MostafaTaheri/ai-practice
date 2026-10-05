namespace AIPractice.BuildingBlock;

public interface IArchitectureModelClient
{
    Task<string> SummarizeAsync(
        string proposal,
        CancellationToken cancellationToken);
}