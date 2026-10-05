using AutoMapper;
using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Domain.Entities;

namespace DocumentManagementSystem.Application.Mapping;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Document, DocumentResponse>();
        CreateMap<Collection, CollectionResponse>()
            .ForCtorParam(nameof(CollectionResponse.DocumentCount),
                options => options.MapFrom(source => source.CollectionDocuments.Count));
    }
}
