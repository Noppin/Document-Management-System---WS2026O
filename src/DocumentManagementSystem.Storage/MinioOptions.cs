namespace DocumentManagementSystem.Storage;

public sealed class MinioOptions
{
    public const string SectionName = "Minio";

    public string Endpoint { get; init; } = "minio:9000";
    public string AccessKey { get; init; } = "dms";
    public string SecretKey { get; init; } = "dmsminio123";
    public string BucketName { get; init; } = "documents";
    public bool UseSsl { get; init; }
}
