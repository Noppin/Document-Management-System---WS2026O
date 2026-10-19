using DocumentManagementSystem.Messaging;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManagementSystem.Api.ExceptionHandling;

public sealed class DocumentQueueExceptionHandler(ILogger<DocumentQueueExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DocumentQueueException queueException)
        {
            return false;
        }

        logger.LogError(
            queueException,
            "Failed to enqueue document for OCR processing: {Message}",
            queueException.Message);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status503ServiceUnavailable,
            Title = "Service Unavailable",
            Detail = "The document was uploaded but could not be queued for OCR processing. Please try again later."
        };

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
