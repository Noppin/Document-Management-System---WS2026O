using DocumentManagementSystem.Api.Controllers;
using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace DocumentManagementSystem.Api.Tests;

public sealed class CollectionsControllerTests
{
    private readonly ICollectionService _service = Substitute.For<ICollectionService>();
    private readonly CollectionsController _controller;

    public CollectionsControllerTests()
    {
        _controller = new CollectionsController(_service, NullLogger<CollectionsController>.Instance);
    }

    private static CollectionResponse CreateResponse() => new(
        Guid.NewGuid(),
        "Invoices",
        DateTimeOffset.UtcNow,
        0);

    [Fact]
    public void Create_ValidRequest_ReturnsCreatedAtActionForNewCollection()
    {
        var request = new CreateCollectionRequest { Name = "Invoices" };
        var response = CreateResponse();
        _service.Create(request).Returns(response);

        var result = _controller.Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(CollectionsController.GetById), created.ActionName);
        Assert.Equal<object>(response.Id, created.RouteValues!["id"]!);
        Assert.Same(response, created.Value);
    }

    [Fact]
    public void GetAll_ReturnsAllCollections()
    {
        IReadOnlyList<CollectionResponse> collections = new[] { CreateResponse() };
        _service.GetAll().Returns(collections);

        var result = _controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(collections, ok.Value);
    }

    [Fact]
    public void GetById_ExistingCollection_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var detail = new CollectionDetailResponse(id, "Invoices", DateTimeOffset.UtcNow, Array.Empty<DocumentResponse>());
        _service.GetById(id).Returns(detail);

        var result = _controller.GetById(id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(detail, ok.Value);
    }

    [Fact]
    public void GetById_MissingCollection_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _service.GetById(id).Returns((CollectionDetailResponse?)null);

        var result = _controller.GetById(id);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public void AddDocument_ExistingCollection_ReturnsNoContent()
    {
        var collectionId = Guid.NewGuid();
        var request = new AssignDocumentRequest(Guid.NewGuid());
        _service.AddDocument(collectionId, request.DocumentId).Returns(true);

        var result = _controller.AddDocument(collectionId, request);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public void AddDocument_MissingCollection_ReturnsNotFound()
    {
        var collectionId = Guid.NewGuid();
        var request = new AssignDocumentRequest(Guid.NewGuid());
        _service.AddDocument(collectionId, request.DocumentId).Returns(false);

        var result = _controller.AddDocument(collectionId, request);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void RemoveDocument_ExistingAssignment_ReturnsNoContent()
    {
        var collectionId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        _service.RemoveDocument(collectionId, documentId).Returns(true);

        var result = _controller.RemoveDocument(collectionId, documentId);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public void RemoveDocument_MissingAssignment_ReturnsNotFound()
    {
        var collectionId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        _service.RemoveDocument(collectionId, documentId).Returns(false);

        var result = _controller.RemoveDocument(collectionId, documentId);

        Assert.IsType<NotFoundResult>(result);
    }
}
