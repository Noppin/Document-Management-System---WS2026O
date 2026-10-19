using System.Text.Json;
using DocumentManagementSystem.Api.ExceptionHandling;
using DocumentManagementSystem.Messaging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentManagementSystem.Api.Tests;

public sealed class DocumentQueueExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_QueueFailure_ReturnsServiceUnavailableProblem()
    {
        var handler = new DocumentQueueExceptionHandler(
            NullLogger<DocumentQueueExceptionHandler>.Instance);
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        var handled = await handler.TryHandleAsync(
            context,
            new DocumentQueueException(
                "Could not enqueue document 3a77cc6e for OCR processing.",
                new InvalidOperationException("RabbitMQ is unavailable.")),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);

        context.Response.Body.Position = 0;
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(
            context.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, problem!.Status);
        Assert.Equal("Service Unavailable", problem.Title);
        Assert.DoesNotContain("RabbitMQ", problem.Detail);
    }

    [Fact]
    public async Task TryHandleAsync_UnrelatedException_IsNotHandled()
    {
        var handler = new DocumentQueueExceptionHandler(
            NullLogger<DocumentQueueExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(
            new DefaultHttpContext(),
            new InvalidOperationException("boom"),
            CancellationToken.None);

        Assert.False(handled);
    }
}
