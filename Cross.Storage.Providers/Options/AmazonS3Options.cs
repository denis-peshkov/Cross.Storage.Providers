namespace Cross.Storage.Providers.Options;

public class AmazonS3StorageOptions
{
    public string? AccessKey { get; set; }

    public string? SecretKey { get; set; }

    public RegionEndpoint? RegionSystemName { get; set; }

    public string? BucketName { get; set; }
}
