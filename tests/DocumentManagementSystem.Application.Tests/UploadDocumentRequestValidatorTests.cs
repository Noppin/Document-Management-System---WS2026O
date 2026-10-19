using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Application.Validation;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;

namespace DocumentManagementSystem.Application.Tests;

public sealed class UploadDocumentRequestValidatorTests
{
    private const int MaxFileSize = 20 * 1024 * 1024;

    private readonly UploadDocumentRequestValidator _validator = new();

    [Fact]
    public async Task Validate_ValidPdf_HasNoErrors()
    {
        var result = await _validator.TestValidateAsync(CreateRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_NullFile_FailsNotEmpty()
    {
        var request = new UploadDocumentRequest { File = null! };

        var result = await _validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(upload => upload.File)
            .WithErrorMessage("The uploaded file must not be empty.");
    }

    [Fact]
    public async Task Validate_EmptyFile_FailsNotEmpty()
    {
        var result = await _validator.TestValidateAsync(CreateRequest(Array.Empty<byte>()));

        result.ShouldHaveValidationErrorFor(upload => upload.File.Length)
            .WithErrorMessage("The uploaded file must not be empty.");
    }

    [Fact]
    public async Task Validate_FileAtSizeLimit_IsAccepted()
    {
        var result = await _validator.TestValidateAsync(CreateRequest(CreatePdf(MaxFileSize)));

        result.ShouldNotHaveValidationErrorFor(upload => upload.File.Length);
    }

    [Fact]
    public async Task Validate_FileAboveSizeLimit_Fails()
    {
        var result = await _validator.TestValidateAsync(CreateRequest(CreatePdf(MaxFileSize + 1)));

        result.ShouldHaveValidationErrorFor(upload => upload.File.Length)
            .WithErrorMessage("The uploaded file exceeds the 20 MB limit.");
    }

    [Fact]
    public async Task Validate_NonPdfExtension_Fails()
    {
        var result = await _validator.TestValidateAsync(CreateRequest(fileName: "notes.txt"));

        result.ShouldHaveValidationErrorFor(upload => upload.File.FileName)
            .WithErrorMessage("Only PDF documents are accepted.");
    }

    [Fact]
    public async Task Validate_UppercasePdfExtension_IsAccepted()
    {
        var result = await _validator.TestValidateAsync(CreateRequest(fileName: "INVOICE.PDF"));

        result.ShouldNotHaveValidationErrorFor(upload => upload.File.FileName);
    }

    [Fact]
    public async Task Validate_WrongContentType_Fails()
    {
        var result = await _validator.TestValidateAsync(CreateRequest(contentType: "text/plain"));

        result.ShouldHaveValidationErrorFor(upload => upload.File.ContentType)
            .WithErrorMessage("The uploaded file must have content type application/pdf.");
    }

    [Fact]
    public async Task Validate_UppercaseContentType_IsAccepted()
    {
        var result = await _validator.TestValidateAsync(CreateRequest(contentType: "APPLICATION/PDF"));

        result.ShouldNotHaveValidationErrorFor(upload => upload.File.ContentType);
    }

    [Fact]
    public async Task Validate_MissingPdfHeader_Fails()
    {
        var result = await _validator.TestValidateAsync(CreateRequest("not-a-pdf"u8.ToArray()));

        result.ShouldHaveValidationErrorFor(upload => upload.File)
            .WithErrorMessage("The uploaded file is not a valid PDF.");
    }

    [Fact]
    public async Task Validate_HeaderShorterThanFiveBytes_Fails()
    {
        var result = await _validator.TestValidateAsync(CreateRequest("%PD"u8.ToArray()));

        result.ShouldHaveValidationErrorFor(upload => upload.File)
            .WithErrorMessage("The uploaded file is not a valid PDF.");
    }

    private static byte[] CreatePdf(int size)
    {
        var content = new byte[size];
        "%PDF-"u8.ToArray().CopyTo(content, 0);
        return content;
    }

    private static UploadDocumentRequest CreateRequest(
        byte[]? content = null,
        string fileName = "invoice.pdf",
        string contentType = "application/pdf")
    {
        var bytes = content ?? "%PDF-1.4"u8.ToArray();
        return new UploadDocumentRequest
        {
            File = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            }
        };
    }
}
