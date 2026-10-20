using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DocumentManagementSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("collections")]
public sealed class CollectionsController(ICollectionService service, ILogger<CollectionsController> logger)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CollectionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<CollectionResponse> Create(CreateCollectionRequest request)
    {
        logger.LogInformation("Received create-collection request for {CollectionName}", request.Name);

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
    public IActionResult AddDocument(Guid id, AssignDocumentRequest request)
    {
        logger.LogInformation(
            "Received request to add document {DocumentId} to collection {CollectionId}",
            request.DocumentId,
            id);

        return service.AddDocument(id, request.DocumentId) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}/documents/{documentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult RemoveDocument(Guid id, Guid documentId)
    {
        logger.LogInformation(
            "Received request to remove document {DocumentId} from collection {CollectionId}",
            documentId,
            id);

        return service.RemoveDocument(id, documentId) ? NoContent() : NotFound();
    }
}
