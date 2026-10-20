using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Application.Services;
using DocumentManagementSystem.Application.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentManagementSystem.Application.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_RegistersServicesValidatorsAndMapper()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IDocumentService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ICollectionService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IValidator<UploadDocumentRequest>));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IValidator<CreateCollectionRequest>));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(AutoMapper.IMapper));
    }

    [Fact]
    public void AddApplication_ResolvesCollectionValidator()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var validator = scope.ServiceProvider.GetRequiredService<IValidator<CreateCollectionRequest>>();

        Assert.IsType<CreateCollectionRequestValidator>(validator);
    }
}
