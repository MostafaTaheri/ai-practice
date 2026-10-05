using AIPractice.Api.Models.Responses.Abstractions;

namespace AIPractice.Api.Models.Responses;

public sealed class SummaryResponse : BaseResponse<string>
{
    public SummaryResponse(string data) : base(data) { }

    public SummaryResponse(string errorCode, string errorMessage, List<string>? details = null)
        : base(errorCode, errorMessage, details) { }
}
