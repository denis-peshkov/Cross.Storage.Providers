namespace Cross.Storage.Providers.Providers;

public class AmazonS3StorageProvider : StorageProviderBase, IStorageProvider
{
    private readonly AmazonS3Client _client;

    private readonly AmazonS3Options _amazonS3Options;

    public AmazonS3StorageProvider(IOptions<StorageProviderOptions> storageProviderOptions, AmazonS3Client client)
    {
        _client = client;
        _amazonS3Options = storageProviderOptions.Value.AmazonS3Storage ?? throw new ArgumentNullException(nameof(StorageProviderOptions.AmazonS3Storage));
    }

    protected override void Dispose(bool disposing)
    {
        _client.Dispose();

        base.Dispose(disposing);
    }

    public async Task<string> ReadAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var request = new GetObjectRequest
        {
            BucketName = _amazonS3Options.BucketName,
            Key = fileName,
        };

        var response = await _client.GetObjectAsync(request, cancellationToken);
        using var readStream = new StreamReader(response.ResponseStream);

        return await readStream.ReadToEndAsync(cancellationToken);
    }

    public async Task<byte[]> ReadBinaryAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var request = new GetObjectRequest
        {
            BucketName = _amazonS3Options.BucketName,
            Key = fileName,
        };

        // Execute request
        var response = await _client.GetObjectAsync(request, cancellationToken);

        await using var responseStream = response.ResponseStream;
        var memoryStream = new MemoryStream();

        // Read the response stream into a memory stream
        await responseStream.CopyToAsync(memoryStream, cancellationToken);

        // Return the byte array
        return memoryStream.ToArray();
    }

    public async Task<Stream> ReadStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        if (!await IsFileExistAsync(fileName, cancellationToken))
        {
            throw new InvalidOperationException($"File {fileName} doesn`t exist.");
        }

        var request = new GetObjectRequest
        {
            BucketName = _amazonS3Options.BucketName,
            Key = fileName,
        };

        // Execute request
        var response = await _client.GetObjectAsync(request, cancellationToken);
        response.ResponseStream.Position = 0;

        return response.ResponseStream;
    }

    public async Task WriteAsync(string fileName, string content, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _amazonS3Options.BucketName,
            Key = fileName,
            ContentBody = content,
        };

        await _client.PutObjectAsync(request, cancellationToken);
    }

    public async Task WriteBinaryAsync(string fileName, byte[] content, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _amazonS3Options.BucketName,
            Key = fileName,
            InputStream = new MemoryStream(content),
        };

        await _client.PutObjectAsync(request, cancellationToken);
    }

    public async Task WriteStreamAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _amazonS3Options.BucketName,
            Key = fileName,
            InputStream = content,
        };

        await _client.PutObjectAsync(request, cancellationToken);
    }

    public async Task WriteStreamAsync(string fileName, IFormFile content, string mimetype, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _amazonS3Options.BucketName,
            Key = fileName,
            InputStream = content.OpenReadStream(),
            ContentType = mimetype, // content.ContentType,
        };

        await _client.PutObjectAsync(request, cancellationToken);
    }

    public Task<IReadOnlyCollection<string>> GetFilesByMaskAsync(string path, string fileMask, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public async Task DeleteFileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _amazonS3Options.BucketName,
            Key = fileName,
        };

        await _client.DeleteObjectAsync(request, cancellationToken);
    }

    public void DeleteFile(string fileName)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _amazonS3Options.BucketName,
            Key = fileName,
        };

        _client.DeleteObjectAsync(request).GetAwaiter().GetResult();
    }

    public async Task DeleteFilesByPrefixAsync(string? prefix, CancellationToken cancellationToken = default)
    {
        var request = new ListObjectsRequest {
            BucketName = _amazonS3Options.BucketName,
            Prefix = prefix,
        };

        var response = await _client.ListObjectsAsync(request, CancellationToken.None);
        foreach (var item in response.S3Objects)
        {
            var deleteObjectRequest = new DeleteObjectRequest
            {
                BucketName = _amazonS3Options.BucketName,
                Key = item.Key,
            };

            await _client.DeleteObjectAsync(deleteObjectRequest, cancellationToken);
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
                    BucketName = _amazonS3Options.BucketName,
                    Key = fileName,
                };

                await _client.DeleteObjectAsync(deleteRequest, cancellationToken);
            }
        }
    }

    public async Task CopyFileAsync(string sourceFileName, string destinationFileName, CancellationToken cancellationToken = default)
        => await _client.CopyObjectAsync(new CopyObjectRequest()
        {
            SourceBucket = _amazonS3Options.BucketName,
            SourceKey = sourceFileName,
            DestinationBucket = _amazonS3Options.BucketName,
            DestinationKey = destinationFileName,
        }, cancellationToken);


    public async Task MoveFileAsync(string sourceFileName, string destinationFileName, CancellationToken cancellationToken = default)
    {
        await CopyFileAsync(sourceFileName, destinationFileName, cancellationToken);
        await DeleteFileAsync(sourceFileName, cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> SearchAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var request = new ListObjectsRequest {
            BucketName = _amazonS3Options.BucketName,
            Prefix = prefix,
            MaxKeys = 1,
        };

        var response = await _client.ListObjectsAsync(request, cancellationToken);

        var result =  response.S3Objects
            .Select(x=>  x.Key)
            .ToList();

        return result;
    }

    public async Task<bool> IsFileExistAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.GetObjectMetadataAsync(new()
            {
                BucketName = _amazonS3Options.BucketName,
                Key = fileName,
            }, cancellationToken);

            return true;
        }
        catch (AmazonS3Exception ex)
        {
            if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Object does not exist
                return false;
            }
            else
            {
                // Other error occurred
                throw;
            }
        }
    }

    public bool IsFileExist(string fileName)
    {
        return IsFileExistAsync(fileName).GetAwaiter().GetResult();
    }

    public void CreateDirectory(string path)
    {
        // AmazonS3Storage stores files in a flat hierarchy. No need to create directory.
    }

    public async Task DeleteDirectory(string path, bool recursive = true)
    {
        path = Regex.Replace(path, @"\\+|/+", @"/");
        if (recursive)
        {
            var request = new ListObjectsV2Request
            {
                BucketName = _amazonS3Options.BucketName,
                Prefix = path,
            };

            var response = _client.ListObjectsV2Async(request).GetAwaiter().GetResult();
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
            BucketName = _amazonS3Options.BucketName,
            Prefix = path,
        };

        var response = await _client.ListObjectsV2Async(request);

        var tasks = response.S3Objects
            .Select(obj =>
                new DeleteObjectRequest
                    { BucketName = _amazonS3Options.BucketName, Key = obj.Key }
            )
            .Select(deleteRequest =>
                _client.DeleteObjectAsync(deleteRequest)).Cast<Task>().ToList();

        await Task.WhenAll(tasks);
    }

    public string GetDirectoryName(string path)
        => throw new NotImplementedException();

    public string GetBaseUrl()
        => throw new NotImplementedException();

    public async Task<string> GetUriAsync(string filePath)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _amazonS3Options.BucketName,
            Key = filePath,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        return await _client.GetPreSignedURLAsync(request);
    }

    public string[] GetFilePaths(string rootDirectory, string searchPattern, SearchOption searchOption)
    {
        // Create request to list objects in the root directory pattern
        var request = new ListObjectsV2Request
        {
            BucketName = _amazonS3Options.BucketName,
            Prefix = rootDirectory,
            Delimiter = "/"
        };

        // Execute request
        var response = _client.ListObjectsV2Async(request).GetAwaiter().GetResult();

        return response.CommonPrefixes.Select(commonPrefix => commonPrefix.TrimEnd('/')).ToArray();
    }

    public string GetFileSize(string fileName, SizeUnits sizeUnit)
    {
        var request = new GetObjectMetadataRequest
        {
            BucketName = _amazonS3Options.BucketName,
            Key = fileName,
        };

        var response = _client.GetObjectMetadataAsync(request).GetAwaiter().GetResult();

        return (response.ContentLength / Math.Pow(1024, (long)sizeUnit)).ToString("0.00");
    }

    public Task UndeleteFile(string filePath)
        => throw new NotImplementedException();
}
