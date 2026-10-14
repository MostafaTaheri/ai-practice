namespace AIPractice.BuildingBlock;

public interface IArchitectureModelClient
{
    Task<string> SummarizeAsync(
        string proposal,
        CancellationToken cancellationToken);

    Task<string> GenerateReviewAsync(
        string proposal,
        string? repairFeedback,
        CancellationToken cancellationToken);
}