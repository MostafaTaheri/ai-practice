namespace AIPractice.BuildingBlock;

public interface IGroundedStandardsClient
{
    Task<string> GenerateAsync(
        string proposal,
        string question,
        IReadOnlyList<SourceHit> hits,
        CancellationToken cancellationToken);
}
