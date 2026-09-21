using DocumentManagementSystem.Api.Contracts;
using DocumentManagementSystem.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManagementSystem.Api.Controllers;

[ApiController]
[Route("documents")]
public sealed class DocumentsController(IDocumentService service) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<DocumentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentResponse>> Upload(
        [FromForm] UploadDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.UploadAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<DocumentResponse>> GetAll() => Ok(service.GetAll());

    [HttpGet("{id:guid}")]
    public ActionResult<DocumentResponse> GetById(Guid id)
    {
        var document = service.Get(id);
        return document is null ? NotFound() : Ok(document);
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id) => service.Delete(id) ? NoContent() : NotFound();
}
