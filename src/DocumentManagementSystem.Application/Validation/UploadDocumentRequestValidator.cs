using DocumentManagementSystem.Application.Dtos;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace DocumentManagementSystem.Application.Validation;

public sealed class UploadDocumentRequestValidator : AbstractValidator<UploadDocumentRequest>
{
    private const long MaxFileSize = 20 * 1024 * 1024;

    public UploadDocumentRequestValidator()
    {
        RuleFor(request => request.File).NotEmpty().WithMessage("The uploaded file must not be empty.");

        When(request => request.File is not null, () =>
        {
            RuleFor(request => request.File.Length)
                .GreaterThan(0).WithMessage("The uploaded file must not be empty.");
            RuleFor(request => request.File.Length).LessThanOrEqualTo(MaxFileSize)
                .WithMessage("The uploaded file exceeds the 20 MB limit.");
            RuleFor(request => request.File.FileName)
                .Must(fileName => string.Equals(Path.GetExtension(fileName), ".pdf", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Only PDF documents are accepted.");
            RuleFor(request => request.File.ContentType)
                .Must(contentType => string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
                .WithMessage("The uploaded file must have content type application/pdf.");
            RuleFor(request => request.File).MustAsync(HasValidPdfHeader)
                .WithMessage("The uploaded file is not a valid PDF.");
        });
    }

    private static async Task<bool> HasValidPdfHeader(IFormFile file, CancellationToken cancellationToken)
    {
        var header = new byte[5];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(header, cancellationToken);
        return bytesRead == header.Length && header.SequenceEqual("%PDF-"u8.ToArray());
    }
}