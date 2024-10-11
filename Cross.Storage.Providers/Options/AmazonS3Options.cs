namespace Cross.Storage.Providers.Options;

public class AmazonS3StorageOptions
{
    public string? AccessKey { get; set; }

    public string? SecretKey { get; set; }

    public string? Region { get; set; }

    public string? BucketName { get; set; }
}
