using AutoMapper;
using DocumentManagementSystem.Application.Mapping;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentManagementSystem.Persistence.Tests;

public static class TestMapper
{
    public static IMapper Create()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(configuration => configuration.AddProfile<MappingProfile>());
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }
}