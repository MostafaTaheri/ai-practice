namespace AIPractice.Api.Models.Requests;

public sealed class ReviewWithStandardsRequest
{
    public required string Proposal { get; set; }
    public required string Question { get; set; }
}
