using AutoMapper;
using DocumentManagementSystem.Domain.Entities;
using DocumentManagementSystem.Persistence.Entities;

namespace DocumentManagementSystem.Persistence.Mapping;

public sealed class PersistenceMappingProfile : Profile
{
    public PersistenceMappingProfile()
    {
        CreateMap<Document, DocumentEntity>()
            .ForMember(destination => destination.Status, options => options.MapFrom(source => (int)source.Status))
            .ForMember(destination => destination.CollectionDocuments, options => options.Ignore());
        CreateMap<DocumentEntity, Document>()
            .ForMember(destination => destination.Status, options => options.MapFrom(source => (DocumentStatus)source.Status))
            .ForMember(destination => destination.CollectionDocuments, options => options.Ignore());

        CreateMap<Collection, CollectionEntity>()
            .ForMember(destination => destination.CollectionDocuments, options => options.Ignore());
        CreateMap<CollectionEntity, Collection>();
        CreateMap<CollectionDocumentEntity, CollectionDocument>()
            .ForMember(destination => destination.Collection, options => options.Ignore());
    }
}
