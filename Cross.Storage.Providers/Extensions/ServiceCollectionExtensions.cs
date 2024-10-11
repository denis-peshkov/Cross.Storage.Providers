namespace Cross.Storage.Providers.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStorageProviders(this IServiceCollection services, IConfiguration configuration, string webRootPath)
    {
        services.AddOptions<StorageProviderOptions>()
            .Bind(configuration.GetSection("StorageProvider"))
            .ValidateDataAnnotations();

        var useStorage = configuration["StorageProvider:UseStorage"];

        switch (useStorage)
        {
            case "FileStorage":
                services.TryAddSingleton<IStorageProvider>(x => new FileStorageProvider(webRootPath));
                break;
            case "AzureBlobStorage":
                services.TryAddSingleton<IStorageProvider, AzureStorageProvider>();
                services.TryAddSingleton(_ => new BlobServiceClient(configuration["StorageProvider:AzureBlobStorage:ConnectionString"]));
                break;
            case "AmazonS3Storage":
                services.TryAddSingleton<IStorageProvider, AmazonS3StorageProvider>();
                services.TryAddSingleton(_ => new AmazonS3Client(new BasicAWSCredentials(
                    configuration["StorageProvider:AmazonS3Storage:AccessKey"],
                    configuration["StorageProvider:AmazonS3Storage:SecretKey"]),
                    RegionEndpoint.GetBySystemName(configuration["StorageProvider:AmazonS3Storage:RegionSystemName"])));
                break;
            default:
                throw new ArgumentException("Error while registering StorageProvider: wrong configuration.");
        }

        return services;
    }
}
