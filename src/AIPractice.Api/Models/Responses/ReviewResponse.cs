using AIPractice.Api.Models.Responses.Abstractions;

namespace AIPractice.Api.Models.Responses;

public sealed class ReviewResponse : BaseResponse<ReviewData>
{
    public ReviewResponse(ReviewData data) : base(data) { }

    public ReviewResponse(string errorCode, string errorMessage, List<string>? details = null)
        : base(errorCode, errorMessage, details) { }
}
