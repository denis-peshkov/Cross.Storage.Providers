namespace Cross.Storage.Providers.Services;

public class AmazonS3StorageProvider : IStorageProvider
{
    private bool _disposed;

    private readonly AmazonS3Client _s3Client;

    private readonly StorageProviderOptions _storageProviderOptions;

    public AmazonS3StorageProvider(IOptions<StorageProviderOptions> storageProviderOptions, AmazonS3Client s3Client)
    {
        _s3Client = s3Client;
        _storageProviderOptions = storageProviderOptions.Value;
    }

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

    public Task<string> ReadAsync(string fileName, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<byte[]> ReadBinaryAsync(string fileName, CancellationToken cancellationToken = default)
    {
        if (!await IsFileExistAsync(fileName, cancellationToken))
        {
            throw new InvalidOperationException($"File {fileName} doesn`t exist.");
        }

        var request = new GetObjectRequest
        {
            BucketName = _storageProviderOptions.AmazonS3Storage?.BucketName,
            Key = fileName
        };

        // Execute request
        var response = await _s3Client.GetObjectAsync(request, cancellationToken);
        await using var responseStream = response.ResponseStream;

        var memoryStream = new MemoryStream();

        // Read the response stream into a memory stream
        await responseStream.CopyToAsync(memoryStream, cancellationToken);

        // Return the byte array
        return memoryStream.ToArray();
    }

    public Stream ReadStream(string fileName, CancellationToken cancellationToken = default)
    {
        if (!IsFileExist(fileName))
        {
            throw new InvalidOperationException($"File {fileName} doesn`t exist.");
        }

        var request = new GetObjectRequest
        {
            BucketName = _storageProviderOptions.AmazonS3Storage?.BucketName,
            Key = fileName,
        };

        // Execute request
        var response = _s3Client.GetObjectAsync(request, cancellationToken).GetAwaiter().GetResult();

        return response.ResponseStream;
    }

    public Task WriteAsync(string fileName, string content, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task WriteBinaryAsync(string fileName, byte[] content, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _storageProviderOptions.AmazonS3Storage?.BucketName,
            Key = fileName,
            InputStream = new MemoryStream(content),
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
    }

    public async Task WriteStreamAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _storageProviderOptions.AmazonS3Storage?.BucketName,
            Key = fileName,
            InputStream = content,
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
    }

    public async Task WriteStreamAsync(string fileName, IFormFile content, string mimetype, CancellationToken cancellationToken = default)
    {
        await using var stream = content.OpenReadStream();

        var request = new PutObjectRequest
        {
            BucketName = _storageProviderOptions.AmazonS3Storage?.BucketName,
            Key = fileName,
            InputStream = stream,
            ContentType = mimetype,
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
    }

    public Task<IEnumerable<string>> GetFilesByMaskAsync(string path, string fileMask, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteFileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _storageProviderOptions.AmazonS3Storage?.BucketName,
            Key = fileName
        };

        await _s3Client.DeleteObjectAsync(request, cancellationToken);
    }

    public void DeleteFile(string fileName)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _storageProviderOptions.AmazonS3Storage?.BucketName,
            Key = fileName
        };

        _s3Client.DeleteObjectAsync(request).GetAwaiter().GetResult();
    }

    public async Task DeleteFilesByPrefixAsync(string? prefix, CancellationToken cancellationToken = default)
    {
        var request = new ListObjectsRequest {
            BucketName = _storageProviderOptions.AmazonS3Storage?.BucketName,
            Prefix = prefix
        };

        var response = await _s3Client.ListObjectsAsync(request, CancellationToken.None);
        foreach (var item in response.S3Objects)
        {
            var deleteObjectRequest = new DeleteObjectRequest
            {
                BucketName = _storageProviderOptions.AmazonS3Storage?.BucketName,
                Key = item.Key
            };

            await _s3Client.DeleteObjectAsync(deleteObjectRequest, cancellationToken);
        }
    }

    public async Task DeleteFilesExceptAsync(string directory, IReadOnlyCollection<string> filePaths, CancellationToken cancellationToken = default)
    {
        var fileNames = GetFilePaths(directory, "*", SearchOption.AllDirectories);
        foreach (var fileName in fileNames)
        {
            if (!filePaths.Contains(fileName))
            {
                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = _storageProviderOptions.AmazonS3Storage?.BucketName,
                    Key = fileName
                };

                await _s3Client.DeleteObjectAsync(deleteRequest, cancellationToken);
            }
        }
    }

    public Task MoveFileAsync(string sourceFileName, string destinationFileName, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> IsFileExistAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var request = new ListObjectsRequest {
            BucketName = _storageProviderOptions.AmazonS3Storage?.BucketName,
            Prefix = fileName,
            MaxKeys = 1
        };

        var response = await _s3Client.ListObjectsAsync(request, CancellationToken.None);

        return response.S3Objects.Any();
    }

    public bool IsFileExist(string fileName)
    {
        var request = new ListObjectsRequest {
            BucketName = _storageProviderOptions.AmazonS3Storage?.BucketName,
            Prefix = fileName,
            MaxKeys = 1
        };

        var response = _s3Client.ListObjectsAsync(request, CancellationToken.None).GetAwaiter().GetResult();

        return response.S3Objects.Any();
    }

    public void CreateDirectory(string path)
    {
        //AmazonS3Storage stores files in a flat hierarchy. No need to create directory.
    }

    public async Task DeleteDirectory(string path, bool recursive = true)
    {
        path = Regex.Replace(path, @"\\+|/+", @"/");
        if (recursive)
        {
            var request = new ListObjectsV2Request
            {
                BucketName = _storageProviderOptions.AmazonS3Storage?.BucketName,
                Prefix = path,
            };

            var response = _s3Client.ListObjectsV2Async(request).GetAwaiter().GetResult();
            foreach (var obj in response.S3Objects)
            {
                await DeleteFileAsync(obj.Key);
            }
        }
        else
        {
            await DeleteAllFilesFromDirectory(path);
        }
    }

    public async Task DeleteAllFilesFromDirectory(string path)
    {
        path = Regex.Replace(path, @"\\+|/+", @"/");

        if (!path.EndsWith('/'))
            path += "/";

        var request = new ListObjectsV2Request
        {
            BucketName = _storageProviderOptions.AmazonS3Storage?.BucketName,
            Prefix = path,
        };

        var response = await _s3Client.ListObjectsV2Async(request);

        var tasks = response.S3Objects
            .Select(obj =>
                new DeleteObjectRequest
                    { BucketName = _storageProviderOptions.AmazonS3Storage?.BucketName, Key = obj.Key }
            )
            .Select(deleteRequest =>
                _s3Client.DeleteObjectAsync(deleteRequest)).Cast<Task>().ToList();

        await Task.WhenAll(tasks);
    }

    public string GetDirectoryName(string path)
    {
        throw new NotImplementedException();
    }

    public string GetBaseUrl()
    {
        throw new NotImplementedException();
    }

    public string[] GetFilePaths(string rootDirectory, string searchPattern, SearchOption searchOption)
    {
        // Create request to list objects in the root directory pattern
        var request = new ListObjectsV2Request
        {
            BucketName = _storageProviderOptions.AmazonS3Storage?.BucketName,
            Prefix = rootDirectory,
            Delimiter = "/"
        };

        // Execute request
        var response = _s3Client.ListObjectsV2Async(request).GetAwaiter().GetResult();

        return response.CommonPrefixes.Select(commonPrefix => commonPrefix.TrimEnd('/')).ToArray();
    }

    public string GetFileSize(string fileName, SizeUnits sizeUnit)
    {
        var request = new GetObjectMetadataRequest
        {
            BucketName = _storageProviderOptions.AmazonS3Storage?.BucketName,
            Key = fileName,
        };

        var response = _s3Client.GetObjectMetadataAsync(request).GetAwaiter().GetResult();

        return (response.ContentLength / Math.Pow(1024, (long)sizeUnit)).ToString("0.00");
    }

    public Task UndeleteFile(string filePath)
    {
        throw new NotImplementedException();
    }
}
