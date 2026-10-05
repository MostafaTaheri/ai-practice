using Asp.Versioning;
using AIPractice.Api.Models.Requests;
using AIPractice.Api.Models.Responses;
using AIPractice.Application.Features.Proposals.Summarize;
using Microsoft.AspNetCore.Mvc;

namespace AIPractice.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/proposals")]
public class ProposalController(ISummarizeService summarizeService) : ControllerBase
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
}
