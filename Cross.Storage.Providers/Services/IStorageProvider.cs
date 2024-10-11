namespace Cross.Storage.Providers.Services;

public interface IStorageProvider : IDisposable
{
    Task<string> ReadAsync(string fileName, CancellationToken cancellationToken = default);

    Task<byte[]> ReadBinaryAsync(string fileName, CancellationToken cancellationToken = default);

    Stream ReadStream(string fileName, CancellationToken cancellationToken = default);

    Task WriteAsync(string fileName, string content, CancellationToken cancellationToken = default);

    Task WriteBinaryAsync(string fileName, byte[] content, CancellationToken cancellationToken = default);

    Task WriteStreamAsync(string fileName, Stream content, CancellationToken cancellationToken = default);

    Task WriteStreamAsync(string fileName, IFormFile content, string mimetype, CancellationToken cancellationToken = default);

    Task<IEnumerable<string>> GetFilesByMaskAsync(string path, string fileMask, CancellationToken cancellationToken = default);

    Task DeleteFileAsync(string fileName, CancellationToken cancellationToken = default);

    void DeleteFile(string fileName);

    Task DeleteFilesByPrefixAsync(string? prefix, CancellationToken cancellationToken = default);

    Task DeleteFilesExceptAsync(string directory, IReadOnlyCollection<string> filePaths, CancellationToken cancellationToken = default);

    Task MoveFileAsync(string sourceFileName, string destinationFileName, CancellationToken cancellationToken = default);

    Task<bool> IsFileExistAsync(string fileName, CancellationToken cancellationToken = default);

    bool IsFileExist(string fileName);

    void CreateDirectory(string path);

    Task DeleteDirectory(string path, bool recursive = true);

    Task DeleteAllFilesFromDirectory(string path);

    string GetDirectoryName(string path);

    string GetBaseUrl();

    string[] GetFilePaths(string rootDirectory, string searchPattern, SearchOption searchOption);

    /// <summary>
    /// Получение размера файла.
    /// </summary>
    /// <param name="fileName">Имя файла.</param>
    /// <param name="sizeUnit">Единица измерения информации.</param>
    /// <returns>Размер файла.</returns>
    string GetFileSize(string fileName, SizeUnits sizeUnit);

    Task UndeleteFile(string filePath);
}
