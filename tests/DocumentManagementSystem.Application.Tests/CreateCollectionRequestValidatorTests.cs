using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Application.Validation;
using FluentValidation.TestHelper;

namespace DocumentManagementSystem.Application.Tests;

public sealed class CreateCollectionRequestValidatorTests
{
    private readonly CreateCollectionRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidName_HasNoErrors()
    {
        var result = _validator.TestValidate(CreateRequest("Invoices"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_NameAtMinimumLength_IsAccepted()
    {
        var result = _validator.TestValidate(CreateRequest("ab"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_NameAtMaximumLength_IsAccepted()
    {
        var result = _validator.TestValidate(CreateRequest(new string('x', 200)));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyName_FailsNotEmpty()
    {
        var result = _validator.TestValidate(CreateRequest(string.Empty));

        result.ShouldHaveValidationErrorFor(request => request.Name)
            .WithErrorMessage("A collection name is required.");
    }

    [Fact]
    public void Validate_WhitespaceName_FailsNotEmpty()
    {
        var result = _validator.TestValidate(CreateRequest("   "));

        result.ShouldHaveValidationErrorFor(request => request.Name)
            .WithErrorMessage("A collection name is required.");
    }

    [Fact]
    public void Validate_TooShortName_Fails()
    {
        var result = _validator.TestValidate(CreateRequest("A"));

        result.ShouldHaveValidationErrorFor(request => request.Name)
            .WithErrorMessage("The collection name must use at least 2 characters.");
    }

    [Fact]
    public void Validate_TooLongName_Fails()
    {
        var result = _validator.TestValidate(CreateRequest(new string('x', 201)));

        result.ShouldHaveValidationErrorFor(request => request.Name)
            .WithErrorMessage("The collection name must use at most 200 characters.");
    }

    private static CreateCollectionRequest CreateRequest(string name) => new() { Name = name };
}
