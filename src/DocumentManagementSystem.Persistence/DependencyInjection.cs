using Microsoft.Extensions.DependencyInjection;

namespace DocumentManagementSystem.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddSingleton<IDocumentRepository, InMemoryDocumentRepository>();
        services.AddSingleton<ICollectionRepository, InMemoryCollectionRepository>();
        return services;
    }
}
