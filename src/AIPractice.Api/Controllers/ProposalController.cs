using Asp.Versioning;
using AIPractice.Api.Models.Requests;
using AIPractice.Api.Models.Responses;
using AIPractice.Application.Features.Proposals.Review;
using AIPractice.Application.Features.Proposals.ReviewWithStandards;
using AIPractice.Application.Features.Proposals.Summarize;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;

namespace AIPractice.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/proposals")]
public class ProposalController(ISummarizeService summarizeService, IReviewService reviewService, IReviewWithStandardsService reviewWithStandardsService, IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Summarize an architecture proposal
    /// </summary>
    [HttpPost("summarize")]
    [ProducesResponseType(typeof(SummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SummaryResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SummaryResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Summarize(
        [FromBody] SummaryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await summarizeService.SummarizeAsync(
            new SummarizeCommand { Proposal = request.Proposal },
            cancellationToken);

        return Ok(new SummaryResponse(result.Summary));
    }

    /// <summary>
    /// Generate a structured architecture review for a proposal
    /// </summary>
    [HttpPost("review")]
    [ProducesResponseType(typeof(ReviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ReviewResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ReviewResponse), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ReviewResponse), StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> Review(
        [FromBody] ReviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await reviewService.ReviewAsync(
            new ReviewCommand { Proposal = request.Proposal },
            cancellationToken);

        return Ok(new ReviewResponse(mapper.Map<ReviewData>(result)));
    }

    /// <summary>
    /// Assess a proposal against retrieved sample standards, with verified citations
    /// </summary>
    [HttpPost("review-with-standards")]
    [ProducesResponseType(typeof(ReviewWithStandardsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ReviewWithStandardsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ReviewWithStandardsResponse), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ReviewWithStandardsResponse), StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> ReviewWithStandards(
        [FromBody] ReviewWithStandardsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await reviewWithStandardsService.ReviewAsync(
            new ReviewWithStandardsCommand { Proposal = request.Proposal, Question = request.Question },
            cancellationToken);

        return Ok(new ReviewWithStandardsResponse(ReviewWithStandardsData.From(result)));
    }
}
