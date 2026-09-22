using DocumentManagementSystem.Api.Contracts;
using DocumentManagementSystem.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManagementSystem.Api.Controllers;

[ApiController]
[Route("collections")]
public sealed class CollectionsController(ICollectionService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CollectionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<CollectionResponse> Create(CreateCollectionRequest request)
    {
        var response = service.Create(request);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<CollectionResponse>> GetAll() => Ok(service.GetAll());

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CollectionDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<CollectionDetailResponse> GetById(Guid id)
    {
        var collection = service.GetById(id);
        return collection is null ? NotFound() : Ok(collection);
    }

    [HttpPost("{id:guid}/documents")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult AddDocument(Guid id, AssignDocumentRequest request) =>
        service.AddDocument(id, request.DocumentId) ? NoContent() : NotFound();

    [HttpDelete("{id:guid}/documents/{documentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult RemoveDocument(Guid id, Guid documentId) =>
        service.RemoveDocument(id, documentId) ? NoContent() : NotFound();
}
