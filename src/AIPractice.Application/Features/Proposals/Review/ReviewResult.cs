namespace AIPractice.Application.Features.Proposals.Review;

public class ReviewResult
{
    public ReviewItem Review { get; set; } = default!;
    public int Attempts { get; set; }
    public string PromptVersion { get; set; } = default!;
}

public sealed class ReviewItem
{
    public required string Summary { get; init; }
    public required string[] Assumptions { get; init; }
    public required RiskItem[] Risks { get; init; }
    public required string[] Alternatives { get; init; }
    public required string[] Recommendations { get; init; }
    public required string[] MissingInformation { get; init; }
}

public sealed class RiskItem
{
    public required string Category { get; init; }
    public required string Severity { get; init; }
    public required string Description { get; init; }
    public required string EvidenceQuote { get; init; }
}
