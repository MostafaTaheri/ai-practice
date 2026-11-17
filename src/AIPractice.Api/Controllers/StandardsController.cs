using Asp.Versioning;
using AIPractice.Api.Models.Requests;
using AIPractice.Api.Models.Responses;
using AIPractice.Application.Features.Standards.Search;
using Microsoft.AspNetCore.Mvc;

namespace AIPractice.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/standards")]
public class StandardsController(ISearchStandardsService searchService) : ControllerBase
{
    /// <summary>
    /// Retrieval-only diagnostic: returns the ranked standards candidates without generating an answer
    /// </summary>
    [HttpPost("search")]
    [ProducesResponseType(typeof(StandardsSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(StandardsSearchResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(StandardsSearchResponse), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(StandardsSearchResponse), StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> Search(
        [FromBody] StandardsSearchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await searchService.SearchAsync(
            new SearchStandardsCommand { Question = request.Question },
            cancellationToken);

        return Ok(new StandardsSearchResponse(StandardsSearchData.From(result)));
    }
}
