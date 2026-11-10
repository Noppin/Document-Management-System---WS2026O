using System.Text.Json;
using DocumentManagementSystem.Application.Dtos;

namespace DocumentManagementSystem.Api.Tests;

public sealed class DocumentResponseSerializationTests
{
    [Theory]
    [InlineData(DocumentResponseStatus.Queued, 0)]
    [InlineData(DocumentResponseStatus.Processing, 1)]
    [InlineData(DocumentResponseStatus.Completed, 2)]
    [InlineData(DocumentResponseStatus.Failed, 3)]
    public void DocumentResponse_PreservesJsonContract(DocumentResponseStatus status, int expectedStatus)
    {
        var response = new DocumentResponse(
            Guid.NewGuid(), "invoice.pdf", "application/pdf", 8, status,
            new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero));

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var parsed = JsonDocument.Parse(json);
        var root = parsed.RootElement;

        Assert.Equal(new[] { "contentType", "createdAt", "fileName", "fileSize", "id", "status" },
            root.EnumerateObject().Select(property => property.Name).OrderBy(name => name));
        Assert.Equal(response.Id, root.GetProperty("id").GetGuid());
        Assert.Equal(response.FileName, root.GetProperty("fileName").GetString());
        Assert.Equal(response.ContentType, root.GetProperty("contentType").GetString());
        Assert.Equal(response.FileSize, root.GetProperty("fileSize").GetInt64());
        Assert.Equal(expectedStatus, root.GetProperty("status").GetInt32());
        Assert.Equal(response.CreatedAt, root.GetProperty("createdAt").GetDateTimeOffset());
    }
}
