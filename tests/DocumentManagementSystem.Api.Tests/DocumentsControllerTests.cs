using DocumentManagementSystem.Api.Controllers;
using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Application.Services;
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
        DocumentResponseStatus.Queued,
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
    public async Task Download_ExistingDocument_ReturnsFileContent()
    {
        var id = Guid.NewGuid();
        var download = new DocumentDownloadResponse(new byte[] { 1, 2, 3 }, "application/pdf", "report.pdf");
        _service.GetDownloadAsync(id, Arg.Any<CancellationToken>()).Returns(download);

        var result = await _controller.Download(id, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Same(download.Content, file.FileContents);
        Assert.Equal(download.ContentType, file.ContentType);
        Assert.Equal(download.FileName, file.FileDownloadName);
    }

    [Fact]
    public async Task Download_MissingDocument_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _service.GetDownloadAsync(id, Arg.Any<CancellationToken>()).Returns((DocumentDownloadResponse?)null);

        var result = await _controller.Download(id, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_ExistingDocument_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _service.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _controller.Delete(id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_MissingDocument_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _service.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _controller.Delete(id, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
