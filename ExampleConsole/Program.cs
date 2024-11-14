// Build configuration
var basePath = Directory.GetCurrentDirectory();
IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(basePath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true, reloadOnChange: true)
    .Build();

// Read settings
var logLevelDefault = configuration["Logging:LogLevel:Default"];
Console.WriteLine($"Default Log Level: {logLevelDefault}");

//setup our DI
var serviceProvider = new ServiceCollection()
    .AddLogging()
    .AddStorageProviders(configuration)
    .BuildServiceProvider();

// //configure console logging
// serviceProvider
//     .GetService<ILoggerFactory>()
//     .AddConsole(LogLevel.Debug);
// var logger = serviceProvider.GetService<ILoggerFactory>()
//     .CreateLogger<Program>();
// logger.LogDebug("Starting application");

//do the actual work here
var storageProvider = serviceProvider.GetService<IStorageProvider>();
Do.TestStorageProviderAsync(storageProvider!, CancellationToken.None).GetAwaiter().GetResult();
