using AutoMapper;
using DocumentManagementSystem.Application.Mapping;
using DocumentManagementSystem.Persistence.Mapping;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentManagementSystem.TestHelpers;

public static class TestMapper
{
    public static IMapper Create()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(configuration =>
        {
            configuration.AddProfile<MappingProfile>();
            configuration.AddProfile<PersistenceMappingProfile>();
        });
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }
}
