using AutoMapper;
using DocumentManagementSystem.Api.Contracts;
using DocumentManagementSystem.Domain.Entities;

namespace DocumentManagementSystem.Api.Mapping;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Document, DocumentResponse>();
        CreateMap<Collection, CollectionResponse>()
            .ForMember(destination => destination.DocumentCount,
                options => options.MapFrom(source => source.CollectionDocuments.Count));
    }
}
