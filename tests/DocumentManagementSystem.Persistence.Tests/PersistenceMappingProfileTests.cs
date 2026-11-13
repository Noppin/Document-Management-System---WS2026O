using AutoMapper;
using DocumentManagementSystem.Domain.Entities;
using DocumentManagementSystem.Persistence.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentManagementSystem.Persistence.Tests;

public sealed class PersistenceMappingProfileTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IMapper _mapper;

    public PersistenceMappingProfileTests()
    {
        var services = new ServiceCollection();
        services.AddPersistence("Host=localhost;Database=mapping_tests;Username=test;Password=test");
        _provider = services.BuildServiceProvider();
        _mapper = _provider.GetRequiredService<IMapper>();
    }

    [Fact]
    public void Configuration_IsValid_WithoutApplicationRegistration()
    {
        _mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }

    [Theory]
    [InlineData(DocumentStatus.Queued, 0)]
    [InlineData(DocumentStatus.Processing, 1)]
    [InlineData(DocumentStatus.Completed, 2)]
    [InlineData(DocumentStatus.Failed, 3)]
    public void Document_RoundTrip_PreservesAllFields(DocumentStatus status, int storedStatus)
    {
        var document = new Document
        {
            FileName = "invoice.pdf",
            ContentType = "application/pdf",
            FileSize = 8,
            Content = "%PDF-1.4"u8.ToArray(),
            Title = "Invoice",
            Description = "October invoice",
            Status = status,
            CreatedAt = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 10, 2, 12, 0, 0, TimeSpan.Zero)
        };

        var entity = _mapper.Map<DocumentEntity>(document);
        var result = _mapper.Map<Document>(entity);

        Assert.Equal(storedStatus, entity.Status);
        Assert.Empty(entity.CollectionDocuments);
        Assert.Equal(document.Id, result.Id);
        Assert.Equal(document.FileName, result.FileName);
        Assert.Equal(document.ContentType, result.ContentType);
        Assert.Equal(document.FileSize, result.FileSize);
        Assert.Equal(document.Content, result.Content);
        Assert.Equal(document.Title, result.Title);
        Assert.Equal(document.Description, result.Description);
        Assert.Equal(document.Status, result.Status);
        Assert.Equal(document.CreatedAt, result.CreatedAt);
        Assert.Equal(document.UpdatedAt, result.UpdatedAt);
    }

    [Fact]
    public void CollectionEntity_WithoutLoadedDocuments_PreservesCount()
    {
        var entity = new CollectionEntity { Name = "Invoices" };
        entity.CollectionDocuments.Add(new CollectionDocumentEntity
        {
            CollectionId = entity.Id,
            DocumentId = Guid.NewGuid()
        });

        var collection = _mapper.Map<Collection>(entity);

        Assert.Equal(entity.Id, collection.Id);
        Assert.Equal(entity.Name, collection.Name);
        Assert.Equal(entity.CreatedAt, collection.CreatedAt);
        Assert.Equal(1, collection.DocumentCount);
        Assert.Empty(collection.Documents);
    }

    [Fact]
    public void CollectionEntity_WithCyclicNavigation_MapsDocumentsWithoutJoinStructure()
    {
        var entity = new CollectionEntity { Name = "Invoices" };
        var document = new DocumentEntity
        {
            FileName = "invoice.pdf",
            ContentType = "application/pdf",
            FileSize = 8,
            Content = "%PDF-1.4"u8.ToArray(),
            Status = 2
        };
        var link = new CollectionDocumentEntity
        {
            CollectionId = entity.Id,
            Collection = entity,
            DocumentId = document.Id,
            Document = document
        };
        entity.CollectionDocuments.Add(link);
        document.CollectionDocuments.Add(link);

        var collection = _mapper.Map<Collection>(entity);

        Assert.Equal(1, collection.DocumentCount);
        var mappedDocument = Assert.Single(collection.Documents);
        Assert.Equal(document.Id, mappedDocument.Id);
        Assert.Equal(document.FileName, mappedDocument.FileName);
        Assert.Equal(DocumentStatus.Completed, mappedDocument.Status);
    }

    public void Dispose() => _provider.Dispose();
}
