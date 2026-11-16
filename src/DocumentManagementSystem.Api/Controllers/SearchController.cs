using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace DocumentManagementSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("search")]
public sealed class SearchController(IDocumentSearchService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DocumentSearchResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentSearchResponse>>> Search(
        [FromQuery, Required, StringLength(200, MinimumLength = 1)] string query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "A non-empty search query is required."
            });
        }

        return Ok(await service.SearchAsync(query, cancellationToken));
    }
}
