using DocumentManagementSystem.Api.Controllers;
using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Application.Services;
using DocumentManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace DocumentManagementSystem.Api.Tests;

public sealed class DocumentsControllerTests
{
    private readonly IDocumentService _service = Substitute.For<IDocumentService>();
    private readonly DocumentsController _controller;

    public DocumentsControllerTests()
    {
        _controller = new DocumentsController(_service, NullLogger<DocumentsController>.Instance);
    }

    private static UploadDocumentRequest CreateUploadRequest()
    {
        var content = new MemoryStream(new byte[] { 1, 2, 3 });
        return new UploadDocumentRequest
        {
            File = new FormFile(content, 0, content.Length, "file", "report.pdf")
        };
    }

    private static DocumentResponse CreateResponse() => new(
        Guid.NewGuid(),
        "report.pdf",
        "application/pdf",
        3,
        DocumentStatus.Queued,
        DateTimeOffset.UtcNow);

    [Fact]
    public async Task Upload_ValidFile_ReturnsCreatedAtActionForNewDocument()
    {
        var request = CreateUploadRequest();
        var response = CreateResponse();
        _service.UploadAsync(request, Arg.Any<CancellationToken>()).Returns(response);

        var result = await _controller.Upload(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(DocumentsController.GetById), created.ActionName);
        Assert.Equal<object>(response.Id, created.RouteValues!["id"]!);
        Assert.Same(response, created.Value);
    }

    [Fact]
    public void GetAll_ReturnsAllDocuments()
    {
        IReadOnlyList<DocumentResponse> documents = new[] { CreateResponse() };
        _service.GetAll().Returns(documents);

        var result = _controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(documents, ok.Value);
    }

    [Fact]
    public void GetById_ExistingDocument_ReturnsOk()
    {
        var response = CreateResponse();
        _service.Get(response.Id).Returns(response);

        var result = _controller.GetById(response.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(response, ok.Value);
    }

    [Fact]
    public void GetById_MissingDocument_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _service.Get(id).Returns((DocumentResponse?)null);

        var result = _controller.GetById(id);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public void Download_ExistingDocument_ReturnsFileContent()
    {
        var id = Guid.NewGuid();
        var download = new DocumentDownloadResponse(new byte[] { 1, 2, 3 }, "application/pdf", "report.pdf");
        _service.GetDownload(id).Returns(download);

        var result = _controller.Download(id);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Same(download.Content, file.FileContents);
        Assert.Equal(download.ContentType, file.ContentType);
        Assert.Equal(download.FileName, file.FileDownloadName);
    }

    [Fact]
    public void Download_MissingDocument_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _service.GetDownload(id).Returns((DocumentDownloadResponse?)null);

        var result = _controller.Download(id);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void Delete_ExistingDocument_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _service.Delete(id).Returns(true);

        var result = _controller.Delete(id);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public void Delete_MissingDocument_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _service.Delete(id).Returns(false);

        var result = _controller.Delete(id);

        Assert.IsType<NotFoundResult>(result);
    }
}
