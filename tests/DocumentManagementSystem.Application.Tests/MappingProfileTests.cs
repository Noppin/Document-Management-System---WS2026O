using AutoMapper;
using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentManagementSystem.Application.Tests;

public sealed class MappingProfileTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IMapper _mapper;

    public MappingProfileTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        _provider = services.BuildServiceProvider();
        _mapper = _provider.GetRequiredService<IMapper>();
    }

    [Fact]
    public void Configuration_IsValid_WithoutPersistenceRegistration()
    {
        _mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }

    [Theory]
    [InlineData(DocumentStatus.Queued, DocumentResponseStatus.Queued)]
    [InlineData(DocumentStatus.Processing, DocumentResponseStatus.Processing)]
    [InlineData(DocumentStatus.Completed, DocumentResponseStatus.Completed)]
    [InlineData(DocumentStatus.Failed, DocumentResponseStatus.Failed)]
    public void DocumentResponse_PreservesMetadataAndStatus(
        DocumentStatus status, DocumentResponseStatus expectedStatus)
    {
        var document = CreateDocument(status);

        var response = _mapper.Map<DocumentResponse>(document);

        Assert.Equal(document.Id, response.Id);
        Assert.Equal(document.FileName, response.FileName);
        Assert.Equal(document.ContentType, response.ContentType);
        Assert.Equal(document.FileSize, response.FileSize);
        Assert.Equal(document.CreatedAt, response.CreatedAt);
        Assert.Equal(expectedStatus, response.Status);
    }

    [Fact]
    public void DocumentDownloadResponse_PreservesFileContentAndMetadata()
    {
        var document = CreateDocument();

        var response = _mapper.Map<DocumentDownloadResponse>(document);

        Assert.Equal(document.Content, response.Content);
        Assert.Equal(document.ContentType, response.ContentType);
        Assert.Equal(document.FileName, response.FileName);
    }

    [Fact]
    public void CreateCollectionRequest_TrimsNameAndInitializesBusinessDefaults()
    {
        var before = DateTimeOffset.UtcNow;
        var request = new CreateCollectionRequest { Name = "  Invoices  " };

        var first = _mapper.Map<Collection>(request);
        var second = _mapper.Map<Collection>(request);

        Assert.Equal("Invoices", first.Name);
        Assert.NotEqual(Guid.Empty, first.Id);
        Assert.NotEqual(first.Id, second.Id);
        Assert.InRange(first.CreatedAt, before, DateTimeOffset.UtcNow);
        Assert.Equal(0, first.DocumentCount);
        Assert.Empty(first.Documents);
    }

    [Fact]
    public void CollectionResponse_UsesDocumentCountWithoutLoadedDocuments()
    {
        var collection = new Collection { Name = "Invoices", DocumentCount = 3 };

        var response = _mapper.Map<CollectionResponse>(collection);

        Assert.Equal(collection.Id, response.Id);
        Assert.Equal(collection.Name, response.Name);
        Assert.Equal(collection.CreatedAt, response.CreatedAt);
        Assert.Equal(3, response.DocumentCount);
    }

    [Fact]
    public void CollectionDetailResponse_MapsNestedBusinessDocuments()
    {
        var document = CreateDocument(DocumentStatus.Completed);
        var collection = new Collection
        {
            Name = "Invoices",
            DocumentCount = 1,
            Documents = new[] { document }
        };

        var response = _mapper.Map<CollectionDetailResponse>(collection);

        Assert.Equal(collection.Id, response.Id);
        Assert.Equal(collection.Name, response.Name);
        Assert.Equal(collection.CreatedAt, response.CreatedAt);
        var nested = Assert.Single(response.Documents);
        Assert.Equal(document.Id, nested.Id);
        Assert.Equal(document.FileName, nested.FileName);
        Assert.Equal(DocumentResponseStatus.Completed, nested.Status);
    }

    private static Document CreateDocument(DocumentStatus status = DocumentStatus.Queued) => new()
    {
        FileName = "invoice.pdf",
        ContentType = "application/pdf",
        Content = "%PDF-1.4"u8.ToArray(),
        FileSize = 8,
        Status = status,
        CreatedAt = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero)
    };

    public void Dispose() => _provider.Dispose();
}
