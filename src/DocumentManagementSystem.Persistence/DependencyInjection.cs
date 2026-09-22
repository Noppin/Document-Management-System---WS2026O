using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagementSystem.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<DocumentManagementDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<ICollectionRepository, CollectionRepository>();
        return services;
    }
}
