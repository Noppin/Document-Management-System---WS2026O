using Microsoft.Extensions.DependencyInjection;

namespace DocumentManagementSystem.Persistence.Tests;

public sealed class DependencyInjectionTests
{
    private const string ConnectionString = "Host=localhost;Database=dms;Username=dms;Password=dms";

    [Fact]
    public void AddPersistence_RegistersDbContextAndRepositories()
    {
        var services = new ServiceCollection();

        services.AddPersistence(ConnectionString);

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(DocumentManagementDbContext));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IDocumentRepository));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ICollectionRepository));
    }

    [Fact]
    public void AddPersistence_ResolvesRepositories()
    {
        var services = new ServiceCollection();
        services.AddPersistence(ConnectionString);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDocumentRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICollectionRepository>());
    }
}
