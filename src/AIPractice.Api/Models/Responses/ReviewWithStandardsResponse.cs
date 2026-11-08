using AIPractice.Api.Models.Responses.Abstractions;

namespace AIPractice.Api.Models.Responses;

public sealed class ReviewWithStandardsResponse : BaseResponse<ReviewWithStandardsData>
{
    public ReviewWithStandardsResponse(ReviewWithStandardsData data) : base(data) { }

    public ReviewWithStandardsResponse(string errorCode, string errorMessage, List<string>? details = null)
        : base(errorCode, errorMessage, details) { }
}
