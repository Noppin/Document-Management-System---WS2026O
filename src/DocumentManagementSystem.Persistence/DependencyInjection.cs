using Microsoft.Extensions.DependencyInjection;
using DocumentManagementSystem.Domain.Repositories;
using DocumentManagementSystem.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagementSystem.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddLogging();
        services.AddAutoMapper(configuration => configuration.AddProfile<PersistenceMappingProfile>());
        services.AddDbContext<DocumentManagementDbContext>(options =>
            options.UseNpgsql(connectionString, postgres =>
                postgres.EnableRetryOnFailure(5, TimeSpan.FromSeconds(5), null)));
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<ICollectionRepository, CollectionRepository>();
        return services;
    }
}
