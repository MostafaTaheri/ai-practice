using AIPractice.Api.Models.Responses.Abstractions;
using AIPractice.Application.Features.Standards.Search;

namespace AIPractice.Api.Models.Responses;

public sealed class StandardsSearchData
{
    public required SourceHitModel[] RetrievedSources { get; init; }
    public required string EmbeddingModel { get; init; }

    public static StandardsSearchData From(SearchStandardsResult result) => new()
    {
        RetrievedSources = result.RetrievedSources
            .Select(h => new SourceHitModel
            {
                Id = h.Source.Id,
                Title = h.Source.Title,
                Text = h.Source.Text,
                Revision = h.Source.Revision,
                Score = h.Score
            })
            .ToArray(),
        EmbeddingModel = result.EmbeddingModel
    };
}

public sealed class StandardsSearchResponse : BaseResponse<StandardsSearchData>
{
    public StandardsSearchResponse(StandardsSearchData data) : base(data) { }

    public StandardsSearchResponse(string errorCode, string errorMessage, List<string>? details = null)
        : base(errorCode, errorMessage, details) { }
}
