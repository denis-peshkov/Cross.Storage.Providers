namespace Cross.Storage.Providers.Services;

public interface IStorageProvider : IDisposable
{
    Task<string> ReadAsync(string fileName, CancellationToken stoppingToken = default);

    Task<byte[]> ReadBinaryAsync(string fileName, CancellationToken stoppingToken = default);

    Stream ReadStream(string fileName, CancellationToken stoppingToken = default);

    Task WriteAsync(string fileName, string content, CancellationToken stoppingToken = default);

    Task WriteBinaryAsync(string fileName, byte[] content, CancellationToken stoppingToken = default);

    Task WriteStreamAsync(string fileName, Stream content, CancellationToken stoppingToken = default);

    Task WriteStreamAsync(string fileName, IFormFile content, string mimetype, CancellationToken stoppingToken = default);

    Task<IEnumerable<string>> GetFilesByMaskAsync(string path, string fileMask, CancellationToken stoppingToken = default);

    Task DeleteFileAsync(string fileName, CancellationToken stoppingToken = default);

    void DeleteFile(string fileName);

    Task MoveFileAsync(string sourceFileName, string destinationFileName, CancellationToken stoppingToken = default);

    Task<bool> IsFileExistAsync(string fileName, CancellationToken stoppingToken = default);

    bool IsFileExist(string fileName);

    void CreateDirectory(string path);

    Task DeleteDirectory(string path, bool recursive = true);

    Task DeleteAllFilesFromDirectory(string path);

    string GetDirectoryName(string path);

    string GetUri(string fileName, string baseUrl);

    string[] GetFilePaths(string rootDirectory, string searchPattern, SearchOption searchOption);

    /// <summary>
    /// Получение размера файла.
    /// </summary>
    /// <param name="fileName">Имя файла.</param>
    /// <param name="sizeUnit">Единица измерения информации.</param>
    /// <returns>Размер файла.</returns>
    string GetFileSize(string fileName, SizeUnits sizeUnit);
}
