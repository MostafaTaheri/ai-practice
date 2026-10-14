namespace AIPractice.Api.Models.Responses;

public sealed class ReviewData
{
    public required ReviewModel Review { get; init; }
    public required int Attempts { get; init; }
    public required string PromptVersion { get; init; }
}

public sealed class ReviewModel
{
    public required string Summary { get; init; }
    public required string[] Assumptions { get; init; }
    public required RiskModel[] Risks { get; init; }
    public required string[] Alternatives { get; init; }
    public required string[] Recommendations { get; init; }
    public required string[] MissingInformation { get; init; }
}

public sealed class RiskModel
{
    public required string Category { get; init; }
    public required string Severity { get; init; }
    public required string Description { get; init; }
    public required string EvidenceQuote { get; init; }
}
