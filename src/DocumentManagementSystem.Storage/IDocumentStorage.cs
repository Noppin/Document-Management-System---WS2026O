namespace DocumentManagementSystem.Storage;

public interface IDocumentStorage
{
    Task StoreAsync(
        Guid documentId,
        Stream content,
        long contentLength,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<byte[]> GetAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);
}
