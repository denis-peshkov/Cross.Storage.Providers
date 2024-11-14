namespace ExampleConsole;

internal static class Do
{
    private const string PLACEHOLDER_IMG_PATH = @"Assets/avatars/72e3700e-4a35-47f2-a575-383c2c8aa8e3/avatar-placeholder-generic.png";

    private const string MAIN_FILE_PATH = "test1.txt";

    private const string DESTINATION_FILE_PATH = "test3.txt";

    private const string TEST_DATA = "test data";

    private const string TARGET_IMG_PATH = "placeholder.png";

    public static async Task TestStorageProviderAsync(IStorageProvider storageProvider, CancellationToken cancellationToken)
    {
        var str1 = storageProvider.GetBaseUrl();
        Console.WriteLine($"Base Path: {str1}");

        await storageProvider.WriteAsync(MAIN_FILE_PATH, TEST_DATA, cancellationToken);
        Console.WriteLine($"WriteAsync: {TEST_DATA}");

        var str2 = await storageProvider.ReadAsync(MAIN_FILE_PATH, cancellationToken);
        Console.WriteLine($"ReadAsync: {str2}");

        var str3 = await storageProvider.ReadBinaryAsync(MAIN_FILE_PATH, cancellationToken);
        Console.WriteLine($"ReadBinaryAsync: {string.Join(",", str3)}");

        await storageProvider.WriteBinaryAsync("test2.txt", str3, cancellationToken);

        var fsSource = ReadStream(PLACEHOLDER_IMG_PATH);
        await storageProvider.WriteStreamAsync(TARGET_IMG_PATH, fsSource, cancellationToken);

        fsSource = await storageProvider.ReadStreamAsync(TARGET_IMG_PATH, cancellationToken);
        Console.WriteLine($"Num Bytes To Read: {fsSource.Length}");

        var sizeInBytes = storageProvider.GetFileSize(TARGET_IMG_PATH, SizeUnits.Byte);
        Console.WriteLine($"GetFileSize: {sizeInBytes}");

        var sizeInKb = storageProvider.GetFileSize(TARGET_IMG_PATH, SizeUnits.Kb);
        Console.WriteLine($"GetFileSize: {sizeInKb}");

        var sizeInTb = storageProvider.GetFileSize(TARGET_IMG_PATH, SizeUnits.Tb);
        Console.WriteLine($"GetFileSize: {sizeInTb}");

        await storageProvider.CopyFileAsync("test2.txt", DESTINATION_FILE_PATH, cancellationToken);

        await storageProvider.MoveFileAsync(DESTINATION_FILE_PATH, "sometest4.txt", cancellationToken);

        var fileExist = await storageProvider.IsFileExistAsync("sometest4.txt", cancellationToken);

        var fileNotExist = await storageProvider.IsFileExistAsync(DESTINATION_FILE_PATH, cancellationToken);

        var filesSearch = await storageProvider.SearchAsync("test", cancellationToken);
        Console.WriteLine($"SearchAsync: {filesSearch.Count}");
        foreach (var file in filesSearch)
        {
            Console.WriteLine(file);
        }

        var filesPaths = await storageProvider.GetFilePaths("./", "*", SearchOption.AllDirectories);
        Console.WriteLine($"GetFilePaths: {filesPaths.Length}");
        foreach (var file in filesPaths)
        {
            Console.WriteLine(file);
        }

        await storageProvider.DeleteFilesByPrefixAsync("some", cancellationToken);

        await storageProvider.DeleteFileAsync(DESTINATION_FILE_PATH, cancellationToken);

        await storageProvider.DeleteFilesExceptAsync("./", [TARGET_IMG_PATH], cancellationToken);
    }

    private static Stream ReadStream(string filename)
    {
        // Read the source file into a byte array.
        var fsSource = new FileStream(filename, FileMode.Open, FileAccess.Read);
        byte[] bytes = new byte[fsSource.Length];
        int numBytesToRead = (int)fsSource.Length;
        int numBytesRead = 0;
        while (numBytesToRead > 0)
        {
            // Read may return anything from 0 to numBytesToRead.
            int n = fsSource.Read(bytes, numBytesRead, numBytesToRead);

            // Break when the end of the file is reached.
            if (n == 0)
                break;

            numBytesRead += n;
            numBytesToRead -= n;
        }

        numBytesToRead = bytes.Length;
        Console.WriteLine($"Num Bytes To Read: {numBytesToRead}");
        return fsSource;
    }
}
