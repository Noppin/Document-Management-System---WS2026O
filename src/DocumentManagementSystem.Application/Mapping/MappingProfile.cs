using AutoMapper;
using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Domain.Entities;

namespace DocumentManagementSystem.Application.Mapping;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<DocumentStatus, DocumentResponseStatus>()
            .ConvertUsing(status => (DocumentResponseStatus)status);
        CreateMap<Document, DocumentResponse>();
        CreateMap<Document, DocumentDownloadResponse>();
        CreateMap<CreateCollectionRequest, Collection>()
            .ForMember(destination => destination.Name, options => options.MapFrom(source => source.Name.Trim()))
            .ForMember(destination => destination.Id, options => options.Ignore())
            .ForMember(destination => destination.CreatedAt, options => options.Ignore())
            .ForMember(destination => destination.DocumentCount, options => options.Ignore())
            .ForMember(destination => destination.Documents, options => options.Ignore());
        CreateMap<Collection, CollectionResponse>();
        CreateMap<Collection, CollectionDetailResponse>();
    }
}
