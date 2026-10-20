using System.Text.Json;
using DocumentManagementSystem.Api.ExceptionHandling;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentManagementSystem.Api.Tests;

public sealed class ValidationExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_ValidationException_ReturnsBadRequestProblemWithAllMessages()
    {
        var handler = new ValidationExceptionHandler(NullLogger<ValidationExceptionHandler>.Instance);
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        var exception = new ValidationException(new[]
        {
            new ValidationFailure("Name", "Name must be at least 2 characters long."),
            new ValidationFailure("Name", "Name must not exceed 200 characters.")
        });

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        context.Response.Body.Position = 0;
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(
            context.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem!.Status);
        Assert.Equal("Bad Request", problem.Title);
        Assert.Contains("Name must be at least 2 characters long.", problem.Detail);
        Assert.Contains("Name must not exceed 200 characters.", problem.Detail);
    }

    [Fact]
    public async Task TryHandleAsync_UnrelatedException_IsNotHandled()
    {
        var handler = new ValidationExceptionHandler(NullLogger<ValidationExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(
            new DefaultHttpContext(),
            new InvalidOperationException("boom"),
            CancellationToken.None);

        Assert.False(handled);
    }
}
