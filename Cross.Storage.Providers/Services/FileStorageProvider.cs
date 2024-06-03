namespace Cross.Storage.Providers.Services;

public class FileStorageProvider : IStorageProvider
{
    private bool _disposed;

    private readonly string _webRootPath;

    public FileStorageProvider(string webRootPath)
    {
        _webRootPath = webRootPath;
    }

    private void BuildFullPath(ref string filePath)
    {
        if (filePath.Contains(_webRootPath))
            return;

        filePath = _webRootPath + filePath;
    }


    public Task<string> ReadAsync(string fileName, CancellationToken stoppingToken = default)
    {
        BuildFullPath(ref fileName);

        if (!IsFileExist(fileName))
        {
            throw new InvalidOperationException($"File {fileName} doesn`t exist.");
        }

        return File.ReadAllTextAsync(fileName);
    }

    public Task<byte[]> ReadBinaryAsync(string fileName, CancellationToken stoppingToken = default)
    {
        BuildFullPath(ref fileName);

        if (!IsFileExist(fileName))
        {
            throw new InvalidOperationException($"File {fileName} doesn`t exist.");
        }

        return File.ReadAllBytesAsync(fileName);
    }

    public async Task WriteAsync(string fileName, string content, CancellationToken stoppingToken = default)
    {
        BuildFullPath(ref fileName);

        CreateDirectory(fileName);

        await DeleteFileAsync(fileName, stoppingToken);

        await File.WriteAllTextAsync(fileName, content);
    }

    public async Task WriteBinaryAsync(string fileName, byte[] content, CancellationToken stoppingToken = default)
    {
        BuildFullPath(ref fileName);

        CreateDirectory(fileName);

        await DeleteFileAsync(fileName, stoppingToken);

        await using var fileStream = File.OpenWrite(fileName);

        await fileStream.WriteAsync(content, 0, content.Length, stoppingToken);
    }

    public async Task WriteStreamAsync(string fileName, Stream content, CancellationToken stoppingToken = default)
    {
        BuildFullPath(ref fileName);

        CreateDirectory(fileName);

        await DeleteFileAsync(fileName, stoppingToken);

        await using var fileStream = File.OpenWrite(fileName);

        content.Position = 0;
        await content.CopyToAsync(fileStream, stoppingToken);
    }

    public async Task WriteStreamAsync(string fileName, IFormFile content, string mimetype, CancellationToken stoppingToken = default)
    {
        BuildFullPath(ref fileName);

        CreateDirectory(fileName);

        await DeleteFileAsync(fileName, stoppingToken);

        await using var fileStream = File.OpenWrite(fileName);

        await using var stream = content.OpenReadStream();

        stream.Position = 0;
        await stream.CopyToAsync(fileStream, stoppingToken);
    }

    public Task<IEnumerable<string>> GetFilesByMaskAsync(string path, string fileMask, CancellationToken stoppingToken = default)
    {
        BuildFullPath(ref path);

        var reg = new Regex(fileMask);

        return Task.FromResult(Directory.GetFiles(path).Where(fileName => reg.IsMatch(fileName)));
    }


    public void DeleteFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return;

        BuildFullPath(ref fileName);

        if (File.Exists(fileName))
            File.Delete(fileName);
    }

    public Task DeleteFilesByPrefixAsync(string? prefix, CancellationToken stoppingToken = default)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            return Task.CompletedTask;
        }

        BuildFullPath(ref prefix);

        var directoryPath = Path.GetDirectoryName(prefix);

        var filename = Path.GetFileName(prefix);

        if (!Directory.Exists(directoryPath))
        {
            return Task.CompletedTask;
        }

        var files = Directory.GetFiles(directoryPath, $"{filename}*");

        foreach (var file in files)
        {
            File.Delete(file);
        }

        return Task.CompletedTask;
    }

    public Task DeleteFilesExceptAsync(string directory, IReadOnlyCollection<string> filePaths, CancellationToken stoppingToken = default)
    {
        var images = new List<string>();
        foreach (var filePath in filePaths)
        {
            var path = filePath;
            BuildFullPath(ref path);
            images.Add(path);
        }

        var fileNames = GetFilePaths(directory, "*", SearchOption.AllDirectories);
        foreach (var fileName in fileNames)
        {
            if (!images.Contains(fileName))
            {
                File.Delete(fileName);
            }
        }

        return Task.CompletedTask;
    }

    public async Task DeleteFileAsync(string fileName, CancellationToken stoppingToken = default)
    {
        BuildFullPath(ref fileName);

        var fileExists = await IsFileExistAsync(fileName, stoppingToken);

        if (fileExists)
        {
            File.Delete(fileName);
        }
    }

    public Task MoveFileAsync(string sourceFileName, string destinationFileName, CancellationToken stoppingToken = default)
    {
        BuildFullPath(ref sourceFileName);
        BuildFullPath(ref destinationFileName);

        File.Move(sourceFileName, destinationFileName);

        return Task.CompletedTask;
    }

    public Task<bool> IsFileExistAsync(string fileName, CancellationToken stoppingToken = default)
        => Task.FromResult(IsFileExist(fileName));

    public bool IsFileExist(string fileName)
    {
        BuildFullPath(ref fileName);

        return File.Exists(fileName);
    }

    public string GetDirectoryName(string path)
    {
        BuildFullPath(ref path);

        var result = Path.GetDirectoryName(path);
        return !string.IsNullOrEmpty(result) ? result : string.Empty;
    }

    public string GetBaseUrl()
        => _webRootPath;

    public void CreateDirectory(string path)
    {
        BuildFullPath(ref path);

        var dir = GetDirectoryName(path);

        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    /// <summary>
    /// Удаляет директорию.
    /// </summary>
    /// <param name="path">Путь к директории.</param>
    /// <param name="recursive">Рекурсивное удаление.</param>
    public Task DeleteDirectory(string path, bool recursive = true)
    {
        BuildFullPath(ref path);

        var isDirectoryExists = Directory.Exists(path);
        if (isDirectoryExists)
        {
            var directory = new DirectoryInfo(path);
            directory.Delete(recursive);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Удаляет все файлы в директории.
    /// </summary>
    /// <param name="path">Путь к директории.</param>
    public Task DeleteAllFilesFromDirectory(string path)
    {
        BuildFullPath(ref path);

        var isDirectoryExists = Directory.Exists(path);
        if (isDirectoryExists)
        {
            var directory = new DirectoryInfo(path);
            foreach (var file in directory.GetFiles())
            {
                file.Delete();
            }
        }

        return Task.CompletedTask;
    }

    public Stream ReadStream(string fileName, CancellationToken stoppingToken = default)
    {
        BuildFullPath(ref fileName);

        return File.OpenRead(fileName);
    }

    public string[] GetFilePaths(string rootDirectory, string searchPattern, SearchOption searchOption)
    {
        BuildFullPath(ref rootDirectory);

        var directoryInfo = new DirectoryInfo(rootDirectory);

        if (!directoryInfo.Exists)
            return Array.Empty<string>();

        return Directory.GetFiles(rootDirectory, searchPattern, searchOption);
    }

    public string GetFileSize(string fileName, SizeUnits sizeUnit)
    {
        BuildFullPath(ref fileName);

        return (new FileInfo(fileName).Length / Math.Pow(1024, (long)sizeUnit)).ToString("0.00");
    }

    public Task UndeleteFile(string filePath)
    {
        throw new NotImplementedException();
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
}
