using DocumentManagementSystem.Application.Dtos;
using FluentValidation;

namespace DocumentManagementSystem.Application.Validation;

public sealed class CreateCollectionRequestValidator : AbstractValidator<CreateCollectionRequest>
{
    public CreateCollectionRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("A collection name is required.")
            .MinimumLength(2).WithMessage("The collection name must use at least 2 characters.")
            .MaximumLength(200).WithMessage("The collection name must use at most 200 characters.");
    }
}
