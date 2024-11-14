namespace Cross.Storage.Providers.Services;

public class AmazonS3StorageProvider : DisposableBase, IStorageProvider
{
    private readonly AmazonS3Client _client;

    private readonly AmazonS3StorageOptions _amazonS3StorageOptions;

    public AmazonS3StorageProvider(IOptions<StorageProviderOptions> storageProviderOptions, AmazonS3Client client)
    {
        _client = client;
        _amazonS3StorageOptions = storageProviderOptions.Value.AmazonS3Storage ?? throw new ArgumentNullException(nameof(StorageProviderOptions.AmazonS3Storage));
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
            BucketName = _amazonS3StorageOptions.BucketName,
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
            BucketName = _amazonS3StorageOptions.BucketName,
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
        var request = new GetObjectRequest
        {
            BucketName = _amazonS3StorageOptions.BucketName,
            Key = fileName,
        };

        // Execute request
        var response = await _client.GetObjectAsync(request, cancellationToken);

        return response.ResponseStream;
    }

    public async Task WriteAsync(string fileName, string content, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _amazonS3StorageOptions.BucketName,
            Key = fileName,
            ContentBody = content,
        };

        await _client.PutObjectAsync(request, cancellationToken);
    }

    public async Task WriteBinaryAsync(string fileName, byte[] content, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _amazonS3StorageOptions.BucketName,
            Key = fileName,
            InputStream = new MemoryStream(content),
        };

        await _client.PutObjectAsync(request, cancellationToken);
    }

    public async Task WriteStreamAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _amazonS3StorageOptions.BucketName,
            Key = fileName,
            InputStream = content,
        };

        await _client.PutObjectAsync(request, cancellationToken);
    }

    public async Task WriteStreamAsync(string fileName, IFormFile content, string mimetype, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _amazonS3StorageOptions.BucketName,
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
            BucketName = _amazonS3StorageOptions.BucketName,
            Key = fileName,
        };

        await _client.DeleteObjectAsync(request, cancellationToken);
    }

    public async Task DeleteFilesByPrefixAsync(string? prefix, CancellationToken cancellationToken = default)
    {
        var request = new ListObjectsRequest {
            BucketName = _amazonS3StorageOptions.BucketName,
            Prefix = prefix,
        };

        var response = await _client.ListObjectsAsync(request, CancellationToken.None);
        foreach (var item in response.S3Objects)
        {
            var deleteObjectRequest = new DeleteObjectRequest
            {
                BucketName = _amazonS3StorageOptions.BucketName,
                Key = item.Key,
            };

            await _client.DeleteObjectAsync(deleteObjectRequest, cancellationToken);
        }
    }

    public async Task DeleteFilesExceptAsync(string directory, IReadOnlyCollection<string> filePaths, CancellationToken cancellationToken = default)
    {
        var fileNames = await GetFilePaths(directory, "*", SearchOption.AllDirectories);

        foreach (var fileName in fileNames)
        {
            if (!filePaths.Contains(fileName))
            {
                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = _amazonS3StorageOptions.BucketName,
                    Key = fileName,
                };

                await _client.DeleteObjectAsync(deleteRequest, cancellationToken);
            }
        }
    }

    public async Task CopyFileAsync(string sourceFileName, string destinationFileName, CancellationToken cancellationToken = default)
    {
        var request = new CopyObjectRequest
        {
            SourceBucket = _amazonS3StorageOptions.BucketName,
            SourceKey = sourceFileName,
            DestinationBucket = _amazonS3StorageOptions.BucketName,
            DestinationKey = destinationFileName,
        };

        await _client.CopyObjectAsync(request, cancellationToken);
    }


    public async Task MoveFileAsync(string sourceFileName, string destinationFileName, CancellationToken cancellationToken = default)
    {
        await CopyFileAsync(sourceFileName, destinationFileName, cancellationToken);
        await DeleteFileAsync(sourceFileName, cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> SearchAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var request = new ListObjectsRequest
        {
            BucketName = _amazonS3StorageOptions.BucketName,
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
                BucketName = _amazonS3StorageOptions.BucketName,
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

    public void CreateDirectory(string path)
    {
        // AmazonS3Storage stores files in a flat hierarchy. No need to create directory.
    }

    public async Task DeleteDirectoryAsync(string path, bool recursive = true)
    {
        path = Regex.Replace(path, @"\\+|/+", @"/");
        if (recursive)
        {
            var request = new ListObjectsV2Request
            {
                BucketName = _amazonS3StorageOptions.BucketName,
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
            await DeleteAllFilesFromDirectoryAsync(path);
        }
    }

    public async Task DeleteAllFilesFromDirectoryAsync(string path)
    {
        path = Regex.Replace(path, @"\\+|/+", @"/");

        if (!path.EndsWith('/'))
            path += "/";

        var request = new ListObjectsV2Request
        {
            BucketName = _amazonS3StorageOptions.BucketName,
            Prefix = path,
        };

        var response = await _client.ListObjectsV2Async(request);

        var tasks = response.S3Objects
            .Select(obj => new DeleteObjectRequest
                {
                    BucketName = _amazonS3StorageOptions.BucketName,
                    Key = obj.Key,
                }
            )
            .Select(deleteRequest => _client.DeleteObjectAsync(deleteRequest))
            .Cast<Task>()
            .ToList();

        await Task.WhenAll(tasks);
    }

    public string GetDirectoryName(string path)
    {
        var result = Path.GetDirectoryName(path);

        return !string.IsNullOrEmpty(result) ? result : string.Empty;
    }

    public string GetBaseUrl()
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _amazonS3StorageOptions.BucketName,
            Key = null,
            Expires = DateTime.UtcNow.AddDays(7),
        };

        return _client.GetPreSignedURL(request);
    }

    public async Task<string> GetUriAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _amazonS3StorageOptions.BucketName,
            Key = filePath,
            Expires = DateTime.UtcNow.AddDays(7),
        };

        return await _client.GetPreSignedURLAsync(request);
    }

    public async Task<string[]> GetFilePaths(string rootDirectory, string searchPattern, SearchOption searchOption)
    {
        rootDirectory = Regex.Replace(rootDirectory, @"\\+|/+", @"/");
        if (!rootDirectory.EndsWith('/'))
        {
            rootDirectory += "/";
        }

        // Create request to list objects in the root directory pattern
        var request = new ListObjectsV2Request
        {
            BucketName = _amazonS3StorageOptions.BucketName,
            Prefix = rootDirectory,
            Delimiter = "/",
        };

        var filePaths = new List<string>();
        ListObjectsV2Response response;
        do
        {
            response = await _client.ListObjectsV2Async(request);

            var files = response.S3Objects.Where(x => !x.Key.EndsWith('/'));
            if (!string.IsNullOrEmpty(searchPattern))
            {
                files = files.Where(x => Regex.IsMatch(x.Key, searchPattern));
            }

            switch (searchOption)
            {
                case SearchOption.AllDirectories:
                    filePaths.AddRange(files.Select(x => x.Key).ToList());
                    break;
                case SearchOption.TopDirectoryOnly:
                    var countSeparator = rootDirectory.Count(x => x.Equals('/'));
                    filePaths.AddRange(files.Where(b => b.Key.Count(c => c.Equals('/')) == countSeparator).Select(x => x.Key).ToList());
                    break;
            }

            if (string.IsNullOrEmpty(searchPattern))
            {
                filePaths.AddRange(response.S3Objects.Where(x => !x.Key.EndsWith('/')).Select(x => x.Key).ToList());
            }
            else
            {
                filePaths.AddRange(response.S3Objects.Where(x => !x.Key.EndsWith('/') && Regex.IsMatch(x.Key, searchPattern)).Select(x => x.Key).ToList());
            }

            request.ContinuationToken = response.NextContinuationToken;
        }
        while (response.IsTruncated);

        return filePaths.ToArray();
    }

    public string GetFileSize(string fileName, SizeUnits sizeUnit)
    {
        var request = new GetObjectMetadataRequest
        {
            BucketName = _amazonS3StorageOptions.BucketName,
            Key = fileName,
        };

        var response = _client.GetObjectMetadataAsync(request).GetAwaiter().GetResult();

        return (response.ContentLength / Math.Pow(1024, (int)sizeUnit)).ToString("0.00");
    }

    public Task UndeleteFile(string filePath)
        => throw new NotImplementedException();
}
