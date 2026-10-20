using System.Text.Json;
using DocumentManagementSystem.Api.ExceptionHandling;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentManagementSystem.Api.Tests;

public sealed class ArgumentExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_ArgumentException_ReturnsBadRequestProblem()
    {
        var handler = new ArgumentExceptionHandler(NullLogger<ArgumentExceptionHandler>.Instance);
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        var handled = await handler.TryHandleAsync(
            context,
            new ArgumentException("A collection named 'Invoices' already exists."),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        context.Response.Body.Position = 0;
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(
            context.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem!.Status);
        Assert.Equal("Bad Request", problem.Title);
        Assert.Equal("A collection named 'Invoices' already exists.", problem.Detail);
    }

    [Fact]
    public async Task TryHandleAsync_UnrelatedException_IsNotHandled()
    {
        var handler = new ArgumentExceptionHandler(NullLogger<ArgumentExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(
            new DefaultHttpContext(),
            new InvalidOperationException("boom"),
            CancellationToken.None);

        Assert.False(handled);
    }
}
