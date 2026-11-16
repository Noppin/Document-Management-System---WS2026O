using DocumentManagementSystem.Storage;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManagementSystem.Api.ExceptionHandling;

public sealed class DocumentStorageExceptionHandler(
    ILogger<DocumentStorageExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DocumentStorageException)
        {
            return false;
        }

        logger.LogError(exception, "MinIO document storage operation failed");

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Document storage unavailable",
                Detail = "The document storage service is temporarily unavailable."
            },
            cancellationToken);

        return true;
    }
}
