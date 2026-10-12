using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Application.Mapping;
using DocumentManagementSystem.Application.Services;
using DocumentManagementSystem.Application.Validation;
using DocumentManagementSystem.Domain.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentManagementSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<ICollectionService, CollectionService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddAutoMapper(configuration => configuration.AddProfile<MappingProfile>());
        services.AddScoped<IValidator<UploadDocumentRequest>, UploadDocumentRequestValidator>();
        services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
        services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
        return services;
    }
}
