namespace Cross.Storage.Providers.Services;

public class AzureStorageProvider : DisposableBase, IStorageProvider
{
    private readonly BlobContainerClient _client;

    public AzureStorageProvider(IOptions<StorageProviderOptions> storageProviderOptions, BlobServiceClient blobServiceClient)
    {
        var azureBlobStorage = storageProviderOptions.Value.AzureBlobStorage ?? throw new ArgumentNullException(nameof(StorageProviderOptions.AzureBlobStorage));

        _client = blobServiceClient.GetBlobContainerClient(azureBlobStorage.ContainerName);
        _client.CreateIfNotExists();
    }

    public Task<string> ReadAsync(string fileName, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public async Task<byte[]> ReadBinaryAsync(string fileName, CancellationToken cancellationToken = default)
    {
        if (!await IsFileExistAsync(fileName, cancellationToken))
        {
            throw new InvalidOperationException($"File {fileName} doesn`t exist.");
        }

        var blockBlobClient = _client.GetBlockBlobClient(fileName);
        var result = await blockBlobClient.DownloadContentAsync(cancellationToken: cancellationToken);

        return result.Value.Content.ToArray();
    }

    public async Task<Stream> ReadStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        if (!await IsFileExistAsync(fileName, cancellationToken))
        {
            throw new InvalidOperationException($"File {fileName} doesn`t exist.");
        }

        var blockBlobClient = _client.GetBlockBlobClient(fileName);
        var result = await blockBlobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);

        var stream = new MemoryStream();
        await result.Value.Content.CopyToAsync(stream, cancellationToken);

        return stream;
    }

    public async Task WriteAsync(string fileName, string content, CancellationToken cancellationToken = default)
    {
        var blockBlobClient = _client.GetBlobClient(fileName);

        await blockBlobClient.UploadAsync(new BinaryData(content), overwrite: true, cancellationToken);
    }

    public async Task WriteBinaryAsync(string fileName, byte[] content, CancellationToken cancellationToken = default)
    {
        var blockBlobClient = _client.GetBlobClient(fileName);

        await blockBlobClient.UploadAsync(new BinaryData(content), cancellationToken: cancellationToken);
    }

    public async Task WriteStreamAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        var blockBlobClient = _client.GetBlockBlobClient(fileName);

        content.Position = 0;
        await blockBlobClient.UploadAsync(content, cancellationToken: cancellationToken);
    }

    public async Task WriteStreamAsync(string fileName, IFormFile content, string mimetype, CancellationToken cancellationToken = default)
    {
        var blobClient = _client.GetBlobClient(fileName);
        await using var stream = content.OpenReadStream();

        await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = mimetype }, cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyCollection<string>> GetFilesByMaskAsync(string path, string fileMask, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<IReadOnlyCollection<string>> SearchAsync(string prefix, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task CopyFileAsync(string sourceFileName, string destinationFileName, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task MoveFileAsync(string sourceFileName, string destinationFileName, CancellationToken cancellationToken = default)
    {
        await CopyFileAsync(sourceFileName, destinationFileName, cancellationToken);
        await DeleteFileAsync(sourceFileName, cancellationToken);
    }

    public async Task<bool> IsFileExistAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var blockBlobClient = _client.GetBlockBlobClient(fileName);
        var result = await blockBlobClient.ExistsAsync(cancellationToken);

        return result.Value;
    }

    public string GetFileSize(string fileName, SizeUnits sizeUnit)
    {
        var blockBlobClient = _client.GetBlockBlobClient(fileName);

        return (blockBlobClient.GetProperties().Value.ContentLength / Math.Pow(1024, (int)sizeUnit)).ToString("0.00");
    }

    public async Task UndeleteFile(string filePath)
    {
        var blockBlobClient = _client.GetBlockBlobClient(filePath);

        await blockBlobClient.UndeleteAsync();
    }

    public string GetDirectoryName(string path)
    {
        var result = Path.GetDirectoryName(path);

        return !string.IsNullOrEmpty(result) ? result : string.Empty;
    }

    public string GetBaseUrl()
        => _client.Uri.ToString();

    public Task<string> GetUriAsync(string filePath, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<string[]> GetFilePaths(string rootDirectory, string searchPattern, SearchOption searchOption)
    {
        rootDirectory = Regex.Replace(rootDirectory, @"\\+|/+", @"/");
        if (!rootDirectory.EndsWith('/'))
        {
            rootDirectory += "/";
        }

        var searchExtensions = searchPattern.Replace("*", "").Split('|').Where(ext => ext != ".").ToArray();
        IEnumerable<BlobItem> blobs = _client.GetBlobs(traits: BlobTraits.Metadata, prefix: rootDirectory).Where(b => b.Metadata.Count == 0);

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
            default:
                throw new ArgumentOutOfRangeException(nameof(searchOption), searchOption, null);
        }

        return Task.FromResult(blobPathes);
    }

    public async Task DeleteFileAsync(string fileName, CancellationToken cancellationToken = default)
        => await _client.DeleteBlobIfExistsAsync(fileName, cancellationToken: cancellationToken);

    public async Task DeleteFilesByPrefixAsync(string? prefix, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            return;
        }

        await foreach (var blobItem in _client.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
        {
            // Delete each blob with the specified prefix
            var blobClient = _client.GetBlobClient(blobItem.Name);
            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
    }

    public async Task DeleteFilesExceptAsync(string directory, IReadOnlyCollection<string> filePaths, CancellationToken cancellationToken = default)
    {
        var blobNames = await GetFilePaths(directory, "*", SearchOption.AllDirectories);
        foreach (var blobName in blobNames)
        {
            if (!filePaths.Contains(blobName))
            {
                await _client.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: cancellationToken);
            }
        }
    }

    public void CreateDirectory(string path)
    {
        // AzureBlobStorage stores files in a flat hierarchy. No need to create directory.
    }

    public async Task DeleteDirectoryAsync(string path, bool recursive = true)
    {
        path = Regex.Replace(path, @"\\+|/+", @"/");
        if (recursive)
        {
            var blobs = _client.GetBlobs(prefix: path).OrderByDescending(x => x.Name).ToArray();
            foreach (var blob in blobs)
                await DeleteFileAsync(blob.Name);
        }
        else
        {
            await DeleteAllFilesFromDirectoryAsync(path);
        }
    }

    public async Task DeleteAllFilesFromDirectoryAsync(string path)
    {
        path = Regex.Replace(path, @"\\+|/+", @"/");

        if (!path.EndsWith('/'))
            path += "/";

        var countSeparator = path.Count(x => x.Equals('/'));
        var topDirectoryBlobs = _client.GetBlobs(traits: BlobTraits.Metadata, prefix: path)
            .Where(b => b.Metadata.Count == 0 && b.Name.Count(c => c.Equals('/')) == countSeparator)
            .ToArray();

        foreach (var blob in topDirectoryBlobs)
            await DeleteFileAsync(blob.Name);
    }
}
