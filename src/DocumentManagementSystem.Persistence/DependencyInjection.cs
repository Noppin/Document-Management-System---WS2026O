using Microsoft.Extensions.DependencyInjection;

namespace DocumentManagementSystem.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services) => services;
}
