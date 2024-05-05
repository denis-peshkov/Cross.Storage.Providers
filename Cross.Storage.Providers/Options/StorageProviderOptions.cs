namespace Cross.Storage.Providers.Options;

public class StorageProviderOptions
{
    public string? UseStorage { get; set; }

    public AzureBlobStorageOptions? AzureBlobStorage { get; set; }

    public FileStorageOptions? FileStorage { get; set; }

    public AmazonS3Options? AmazonS3Storage { get; set; }
}
