namespace Cross.Storage.Providers.Services;

public class AzureStorageProvider : IStorageProvider
{
    private bool _disposed;

    private readonly BlobContainerClient _blobContainerClient;

    public AzureStorageProvider(IOptions<StorageProviderOptions> storageProviderOptions, BlobServiceClient blobServiceClient)
    {
        var options = storageProviderOptions.Value;

        _blobContainerClient = blobServiceClient.GetBlobContainerClient(options.AzureBlobStorage?.ContainerName);
        _blobContainerClient.CreateIfNotExists();
    }

    public Task<string> ReadAsync(string fileName, CancellationToken stoppingToken = default)
        => throw new NotImplementedException();

    public async Task<byte[]> ReadBinaryAsync(string fileName, CancellationToken stoppingToken = default)
    {
        if (!await IsFileExistAsync(fileName, stoppingToken))
        {
            throw new InvalidOperationException($"File {fileName} doesn`t exist.");
        }

        var blockBlob = _blobContainerClient.GetBlockBlobClient(fileName);
        var result = await blockBlob.DownloadContentAsync(cancellationToken: stoppingToken);
        return result.Value.Content.ToArray();
    }

    public Stream ReadStream(string fileName, CancellationToken stoppingToken = default)
    {
        if (!IsFileExist(fileName))
        {
            throw new InvalidOperationException($"File {fileName} doesn`t exist.");
        }

        var blockBlob = _blobContainerClient.GetBlockBlobClient(fileName);
        var result = blockBlob.DownloadStreaming(cancellationToken: stoppingToken);
        return result.Value.Content;
    }

    public Task WriteAsync(string fileName, string content, CancellationToken stoppingToken = default)
        => throw new NotImplementedException();

    public async Task WriteBinaryAsync(string fileName, byte[] content, CancellationToken stoppingToken = default)
    {
        var blockBlob = _blobContainerClient.GetBlobClient(fileName);

        await blockBlob.UploadAsync(new BinaryData(content), cancellationToken: stoppingToken);
    }

    public async Task WriteStreamAsync(string fileName, Stream content, CancellationToken stoppingToken = default)
    {
        var blockBlob = _blobContainerClient.GetBlockBlobClient(fileName);

        await blockBlob.UploadAsync(content, cancellationToken: stoppingToken);
    }

    public async Task WriteStreamAsync(string fileName, IFormFile content, string mimetype, CancellationToken stoppingToken = default)
    {
        var blobClient = _blobContainerClient.GetBlobClient(fileName);
        await using var stream = content.OpenReadStream();

        await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = mimetype }, cancellationToken: stoppingToken);
    }

    public Task<IEnumerable<string>> GetFilesByMaskAsync(string path, string fileMask, CancellationToken stoppingToken = default)
        => throw new NotImplementedException();

    public async Task DeleteFileAsync(string fileName, CancellationToken stoppingToken = default)
        => await _blobContainerClient.DeleteBlobIfExistsAsync(fileName, cancellationToken: stoppingToken);

    public Task MoveFileAsync(string sourceFileName, string destinationFileName, CancellationToken stoppingToken = default)
        => throw new NotImplementedException();

    public async Task<bool> IsFileExistAsync(string fileName, CancellationToken stoppingToken = default)
    {
        var blockBlob = _blobContainerClient.GetBlockBlobClient(fileName);

        return (await blockBlob.ExistsAsync()).Value;
    }

    public bool IsFileExist(string fileName)
    {
        var blockBlob = _blobContainerClient.GetBlockBlobClient(fileName);

        return blockBlob.Exists().Value;
    }

    public string GetFileSize(string fileName, SizeUnits sizeUnit)
    {
        var blockBlob = _blobContainerClient.GetBlockBlobClient(fileName);
        return (blockBlob.GetProperties().Value.ContentLength / Math.Pow(1024, (long)sizeUnit)).ToString("0.00");
    }

    public async Task UndeleteFile(string filePath)
    {
        var blockBlob = _blobContainerClient.GetBlockBlobClient(filePath);
        await blockBlob.UndeleteAsync();
    }


    public string GetDirectoryName(string path)
    {
        var result = Path.GetDirectoryName(path);
        return !string.IsNullOrEmpty(result) ? result : string.Empty;
    }

    public string GetBaseUrl()
        => _blobContainerClient.Uri.ToString();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            return;
        }

        _disposed = true;
    }

    /// <summary>
    /// Возвращает BlobNames отфильтрованные по паттерну которые находятся в rootDirectory
    /// </summary>
    /// <param name="rootDirectory"></param>
    /// <param name="searchPattern">Supports ".*" or "*.*" or "*.jpeg|*.png"</param>
    /// <param name="searchOption"></param>
    /// <returns></returns>
    public string[] GetFilePaths(string rootDirectory, string searchPattern, SearchOption searchOption)
    {
        rootDirectory = Regex.Replace(rootDirectory, @"\\+|/+", @"/");
        if (!rootDirectory.EndsWith('/'))
            rootDirectory += "/";

        var searchExtensions = searchPattern.Replace("*", "").Split('|').Where(ext => ext != ".").ToArray();
        IEnumerable<BlobItem> blobs = _blobContainerClient.GetBlobs(traits: BlobTraits.Metadata, prefix: rootDirectory).Where(b => b.Metadata.Count == 0);

        if (searchExtensions.Any())
        {
            blobs = blobs.Where(b => searchExtensions.Any(ext => b.Name.EndsWith(ext))).ToArray();
        }

        var blobPathes = Array.Empty<string>();

        switch (searchOption)
        {
            case SearchOption.AllDirectories:
                blobPathes = blobs.Select(b => b.Name).ToArray();
                break;
            case SearchOption.TopDirectoryOnly:
                var countSeparator = rootDirectory.Count(x => x.Equals('/'));
                blobPathes = blobs.Where(b => b.Name.Count(c => c.Equals('/')) == countSeparator).Select(b => b.Name).ToArray();
                break;
        }

        return blobPathes;
    }

    public void DeleteFile(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        _blobContainerClient.DeleteBlobIfExists(fileName);
    }

    public async Task DeleteFilesByPrefixAsync(string? prefix, CancellationToken stoppingToken = default)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            return;
        }

        await foreach (var blobItem in _blobContainerClient.GetBlobsAsync(prefix: prefix, cancellationToken: stoppingToken))
        {
            // Delete each blob with the specified prefix
            var blobClient = _blobContainerClient.GetBlobClient(blobItem.Name);
            await blobClient.DeleteIfExistsAsync(cancellationToken: stoppingToken);
        }
    }

    public async Task DeleteFilesExceptAsync(string directory, IReadOnlyCollection<string> filePaths, CancellationToken stoppingToken = default)
    {
        var blobNames = GetFilePaths(directory, "*", SearchOption.AllDirectories);
        foreach (var blobName in blobNames)
        {
            if (!filePaths.Contains(blobName))
            {
                await _blobContainerClient.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: stoppingToken);
            }
        }
    }

    public void CreateDirectory(string path)
    {
        //AzureBlobStorage stores files in a flat hierarchy. No need to create directory.
    }

    public async Task DeleteDirectory(string path, bool recursive = true)
    {
        path = Regex.Replace(path, @"\\+|/+", @"/");
        if (recursive)
        {
            var blobs = _blobContainerClient.GetBlobs(prefix: path).OrderByDescending(x => x.Name).ToArray();
            foreach (var blob in blobs)
                await DeleteFileAsync(blob.Name);
        }
        else
        {
            await DeleteAllFilesFromDirectory(path);
        }
    }

    public Task DeleteAllFilesFromDirectory(string path)
    {
        path = Regex.Replace(path, @"\\+|/+", @"/");

        if (!path.EndsWith('/'))
            path += "/";

        var countSeparator = path.Count(x => x.Equals('/'));
        var topDirectoryBlobs = _blobContainerClient.GetBlobs(traits: BlobTraits.Metadata, prefix: path)
            .Where(b => b.Metadata.Count == 0 && b.Name.Count(c => c.Equals('/')) == countSeparator)
            .ToArray();

        foreach (var blob in topDirectoryBlobs)
            DeleteFile(blob.Name);

        return Task.CompletedTask;
    }
}
