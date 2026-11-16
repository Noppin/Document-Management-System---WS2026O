// Mit AI geschrieben.
using System.ComponentModel.DataAnnotations;
using DocumentManagementSystem.Api.Controllers;
using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManagementSystem.Api.Tests;

public sealed class SearchControllerTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Search_BlankQueryReturnsBadRequest(string query)
    {
        var service = new RecordingService();
        var result = await new SearchController(service).Search(query, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Null(service.Query);
    }

    [Fact]
    public void Search_ModelValidationRejectsOversizedQuery()
    {
        var parameter = typeof(SearchController).GetMethod(nameof(SearchController.Search))!.GetParameters()[0];
        var length = Assert.Single(parameter.GetCustomAttributes(typeof(StringLengthAttribute), false));
        Assert.False(((StringLengthAttribute)length).IsValid(new string('a', 201)));
    }

    [Fact]
    public async Task Search_ReturnsMatchingDocumentMetadata()
    {
        var service = new RecordingService();
        var result = await new SearchController(service).Search("Hello", CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var documents = Assert.IsAssignableFrom<IReadOnlyList<DocumentSearchResponse>>(ok.Value);
        Assert.Equal("HelloWorld.pdf", Assert.Single(documents).FileName);
        Assert.Equal("Hello", service.Query);
    }

    private sealed class RecordingService : IDocumentSearchService
    {
        public string? Query { get; private set; }
        public Task<IReadOnlyList<DocumentSearchResponse>> SearchAsync(string query, CancellationToken cancellationToken = default)
        {
            Query = query;
            return Task.FromResult<IReadOnlyList<DocumentSearchResponse>>(
                [new DocumentSearchResponse(Guid.NewGuid(), "HelloWorld.pdf")]);
        }
    }
}
