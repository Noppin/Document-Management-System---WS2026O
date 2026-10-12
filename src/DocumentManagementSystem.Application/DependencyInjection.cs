using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Application.Mapping;
using DocumentManagementSystem.Application.Services;
using DocumentManagementSystem.Application.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentManagementSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<ICollectionService, CollectionService>();
        services.AddAutoMapper(configuration => configuration.AddProfile<MappingProfile>());
        services.AddScoped<IValidator<UploadDocumentRequest>, UploadDocumentRequestValidator>();
        return services;
    }
}
