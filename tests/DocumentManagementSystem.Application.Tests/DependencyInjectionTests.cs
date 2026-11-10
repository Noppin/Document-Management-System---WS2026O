using AutoMapper;
using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Application.Services;
using DocumentManagementSystem.Application.Validation;
using DocumentManagementSystem.Domain.Entities;
using DocumentManagementSystem.Persistence;
using DocumentManagementSystem.Persistence.Entities;
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MappingProfiles_AreAvailable_RegardlessOfRegistrationOrder(bool persistenceFirst)
    {
        var services = new ServiceCollection();
        const string connectionString = "Host=localhost;Database=dms;Username=test;Password=test";
        if (persistenceFirst)
        {
            services.AddPersistence(connectionString);
            services.AddApplication();
        }
        else
        {
            services.AddApplication();
            services.AddPersistence(connectionString);
        }
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        mapper.ConfigurationProvider.AssertConfigurationIsValid();

        var entity = new CollectionEntity { Name = "Invoices" };
        var model = mapper.Map<Collection>(entity);
        var response = mapper.Map<CollectionResponse>(model);

        Assert.Equal(entity.Id, response.Id);
        Assert.Equal(entity.Name, response.Name);
        Assert.IsType<CollectionService>(scope.ServiceProvider.GetRequiredService<ICollectionService>());
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
