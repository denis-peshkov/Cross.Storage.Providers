namespace Cross.Storage.Providers.Services;

public interface IStorageProvider : IDisposable
{
    string GetDirectoryName(string path);

    void CreateDirectory(string path);

    Task DeleteDirectoryAsync(string path, bool recursive = true);

    Task<string> ReadAsync(string fileName, CancellationToken cancellationToken = default);

    Task<byte[]> ReadBinaryAsync(string fileName, CancellationToken cancellationToken = default);

    Task<Stream> ReadStreamAsync(string fileName, CancellationToken cancellationToken = default);

    Task WriteAsync(string fileName, string content, CancellationToken cancellationToken = default);

    Task WriteBinaryAsync(string fileName, byte[] content, CancellationToken cancellationToken = default);

    Task WriteStreamAsync(string fileName, Stream content, CancellationToken cancellationToken = default);

    Task WriteStreamAsync(string fileName, IFormFile content, string mimetype, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> GetFilesByMaskAsync(string path, string fileMask, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> SearchAsync(string prefix, CancellationToken cancellationToken = default);

    Task<bool> IsFileExistAsync(string fileName, CancellationToken cancellationToken = default);

    Task DeleteAllFilesFromDirectoryAsync(string path);

    Task DeleteFileAsync(string fileName, CancellationToken cancellationToken = default);

    Task DeleteFilesByPrefixAsync(string? prefix, CancellationToken cancellationToken = default);

    Task DeleteFilesExceptAsync(string directory, IReadOnlyCollection<string> filePaths, CancellationToken cancellationToken = default);

    Task UndeleteFile(string filePath);

    Task CopyFileAsync(string sourceFileName, string destinationFileName, CancellationToken cancellationToken = default);

    Task MoveFileAsync(string sourceFileName, string destinationFileName, CancellationToken cancellationToken = default);

    string GetBaseUrl();

    Task<string> GetUriAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns BlobNames filtered by pattern that exists in the root directory
    /// </summary>
    /// <param name="rootDirectory">Root directory</param>
    /// <param name="searchPattern">Supports ".*" or "*.*" or "*.jpeg|*.png"</param>
    /// <param name="searchOption">Search options</param>
    /// <returns></returns>
    Task<string[]> GetFilePaths(string rootDirectory, string searchPattern, SearchOption searchOption);

    /// <summary>
    /// Get file size
    /// </summary>
    /// <param name="fileName">File name</param>
    /// <param name="sizeUnit">Units of measurement</param>
    /// <returns>File size</returns>
    string GetFileSize(string fileName, SizeUnits sizeUnit);
}
