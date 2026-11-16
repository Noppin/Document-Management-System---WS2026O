using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManagementSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("search")]
public sealed class SearchController(IDocumentSearchService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DocumentSearchResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentSearchResponse>>> Search(
        [FromQuery] string query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Ok(Array.Empty<DocumentSearchResponse>());
        }

        return Ok(await service.SearchAsync(query, cancellationToken));
    }
}
