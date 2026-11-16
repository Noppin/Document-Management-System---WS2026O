using DocumentManagementSystem.Search;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManagementSystem.Api.ExceptionHandling;

public sealed class SearchIndexExceptionHandler(
    ILogger<SearchIndexExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not SearchIndexException)
        {
            return false;
        }

        logger.LogError(exception, "Elasticsearch operation failed");

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Search unavailable",
                Detail = "The document search service is temporarily unavailable."
            },
            cancellationToken);

        return true;
    }
}
