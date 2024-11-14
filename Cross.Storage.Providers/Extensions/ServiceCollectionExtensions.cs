namespace Cross.Storage.Providers.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStorageProviders(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<StorageProviderOptions>()
            .Bind(configuration.GetSection("StorageProvider"))
            .ValidateDataAnnotations();

        var useStorage = configuration["StorageProvider:UseStorage"];
        switch (useStorage)
        {
            case "FileStorage":
                var directoryName = Directory.GetCurrentDirectory()
                    .Combine(configuration["StorageProvider:FileStorage:DirectoryName"])
                    .AbsolutePath;
                services.TryAddSingleton<IStorageProvider>(x => new FileStorageProvider(directoryName));
                break;

            case "AzureBlobStorage":
                var connectionString = configuration["StorageProvider:AzureBlobStorage:ConnectionString"];
                services.TryAddSingleton<IStorageProvider, AzureStorageProvider>();
                services.TryAddSingleton(_ => new BlobServiceClient(connectionString));
                break;

            case "AmazonS3Storage":
                var accessKey = configuration["StorageProvider:AmazonS3Storage:AccessKey"]!;
                var secretKey = configuration["StorageProvider:AmazonS3Storage:SecretKey"]!;
                var region = configuration["StorageProvider:AmazonS3Storage:Region"]!;
                services.TryAddSingleton<IStorageProvider, AmazonS3StorageProvider>();
                services.TryAddSingleton(_ => new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), RegionEndpoint.GetBySystemName(region)));
                break;

            default:
                throw new ArgumentException("Error while registering StorageProvider: wrong configuration.");
        }

        return services;
    }
}
