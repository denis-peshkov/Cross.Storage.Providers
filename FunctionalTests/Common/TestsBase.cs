namespace FunctionalTests.Common;

[TestFixture]
public abstract class TestsBase
{
    protected IConfiguration Configuration;

    protected ServiceProvider ServiceProvider;

    protected IStorageProvider StorageProvider;

    [SetUp]
    public virtual void OneTimeSetUp()
    {
        Configuration = LoadConfiguration();

        ServiceProvider = new ServiceCollection()
            .AddStorageProviders(Configuration)
            .BuildServiceProvider();

        StorageProvider = ServiceProvider.GetService<IStorageProvider>()!;
    }

    [TearDown]
    // [OneTimeTearDown]
    public virtual void OneTimeTearDown()
    {
        ServiceProvider.Dispose();
    }

    private static IConfiguration LoadConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false, true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();
        return builder.Build();
    }
}
