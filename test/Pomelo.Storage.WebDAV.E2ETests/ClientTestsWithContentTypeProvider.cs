namespace Pomelo.Storage.WebDAV.E2ETests
{
    public class ClientTestsWithContentTypeProvider : TestBase
    {
        public ClientTestsWithContentTypeProvider() : base(true) 
        { }

        [Fact]
        public async Task GetTest()
        {
            // Arrange
            using var client = new WebDAVHttpClient() { BaseAddress = new Uri("http://localhost:7000") };
            var testPath = Path.Combine(StoragePath, "client_tests");
            if (!Directory.Exists(testPath))
            {
                Directory.CreateDirectory(testPath);
            }
            File.WriteAllText(Path.Combine(testPath, "6.txt"), "1234567890");

            // Act
            var response = await client.GetAsync("/client_tests/6.txt");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal("1234567890", content);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.MediaType);
        }
    }
}
