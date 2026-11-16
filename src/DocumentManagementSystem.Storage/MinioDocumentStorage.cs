using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace DocumentManagementSystem.Storage;

public sealed class MinioDocumentStorage(
    IMinioClient client,
    IOptions<MinioOptions> options,
    ILogger<MinioDocumentStorage> logger) : IDocumentStorage
{
    private readonly MinioOptions _options = options.Value;

    public async Task StoreAsync(
        Guid documentId,
        Stream content,
        long contentLength,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureBucketAsync(cancellationToken);

            if (content.CanSeek)
            {
                content.Position = 0;
            }

            var objectName = DocumentObjectName.For(documentId);
            var args = new PutObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(objectName)
                .WithStreamData(content)
                .WithObjectSize(contentLength)
                .WithContentType(contentType);

            await client.PutObjectAsync(args, cancellationToken);

            logger.LogInformation(
                "Stored document {DocumentId} in MinIO bucket {BucketName} as {ObjectName}",
                documentId,
                _options.BucketName,
                objectName);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new DocumentStorageException(
                $"Could not store document {documentId} in MinIO.",
                exception);
        }
    }

    public async Task<byte[]> GetAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"dms-{documentId:N}-{Guid.NewGuid():N}.pdf");

        try
        {
            await EnsureBucketAsync(cancellationToken);

            var args = new GetObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(DocumentObjectName.For(documentId))
                .WithFile(tempPath);
            
            await client.GetObjectAsync(args, cancellationToken: cancellationToken);
            return await File.ReadAllBytesAsync(tempPath, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new DocumentStorageException(
                $"Could not retrieve document {documentId} from MinIO.",
                exception);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    public async Task DeleteAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureBucketAsync(cancellationToken);

            var args = new RemoveObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(DocumentObjectName.For(documentId));

            await client.RemoveObjectAsync(args, cancellationToken);

            logger.LogInformation(
                "Deleted document {DocumentId} from MinIO bucket {BucketName}",
                documentId,
                _options.BucketName);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new DocumentStorageException(
                $"Could not delete document {documentId} from MinIO.",
                exception);
        }
    }

    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        var bucketExists = await client.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_options.BucketName),
            cancellationToken);

        if (!bucketExists)
        {
            await client.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_options.BucketName),
                cancellationToken);
        }
    }
}
