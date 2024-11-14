namespace FunctionalTests.Tests;

public class StorageProviderTests : TestsBase
{
    private const string MAIN_FILE_PATH = "test1.txt";

    private const string TEST_DATA = "test data";

    [Test]
    public async Task WriteAndRead_String_Successfully()
    {
        await StorageProvider.WriteAsync(MAIN_FILE_PATH, TEST_DATA);

        var str2 = await StorageProvider.ReadAsync(MAIN_FILE_PATH);
        str2.Should().Be(TEST_DATA);
    }
}
