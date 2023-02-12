using Pomelo.Storage.WebDAV.Http;
using System.Xml.Linq;

namespace Pomelo.Storage.WebDAV.E2ETests
{
    public class ClientTests : TestBase
    {
        [Fact]
        public async Task PropFindNotFoundTest()
        {
            // Arrange
            using var client = new WebDAVClient() { BaseAddress = new Uri("http://localhost:7000") };

            // Act
            using var response = await client.PropFindAsync("/fake_path");

            // Assert
            Assert.Equal(404, (int)response.StatusCode);
        }

        [Fact]
        public async Task PropFindRootTest()
        {
            // Arrange
            using var client = new WebDAVClient() { BaseAddress = new Uri("http://localhost:7000") };

            // Act
            using var response = await client.PropFindAsync("/");
            var result = await response.ToPropFindResultsAsync();

            // Assert
            Assert.Equal(207, (int)response.StatusCode);
            Assert.Single(result);
            Assert.Equal("http://localhost:7000/", result.First().Href);
            Assert.Single(result.First().PropStat.Properties.DescendantsAndSelf("{DAV:}resourcetype").First().Descendants("{DAV:}collection"));
        }

        [Fact]
        public async Task PropFindFolderWithDepth1Test()
        {
            // Arrange
            using var client = new WebDAVClient() { BaseAddress = new Uri("http://localhost:7000") };
            var testPath = Path.Combine(StoragePath, "client_tests");
            if (!Directory.Exists(testPath))
            {
                Directory.CreateDirectory(testPath);
            }
            File.WriteAllText(Path.Combine(testPath, "1.txt"), "Hello World");

            // Act
            using var response = await client.PropFindAsync("/client_tests", 1);
            var result = await response.ToPropFindResultsAsync();

            // Assert
            Assert.Equal(207, (int)response.StatusCode);
            Assert.Equal(2, result.Count());
            Assert.Equal("http://localhost:7000/client_tests", result.First().Href);
            Assert.Equal("http://localhost:7000/client_tests/1.txt", result.Last().Href);
            Assert.Equal(Models.ItemType.Directory, result.First().Type);
            Assert.Equal(Models.ItemType.File, result.Last().Type);
        }

        [Fact]
        public async Task OptionsTest()
        {
            // Arrange
            using var client = new WebDAVClient() { BaseAddress = new Uri("http://localhost:7000") };

            // Act
            using var response = await client.OptionsAsync("/");
            var results = response.ToOptionsResult();

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.Contains("PUT", results);
            Assert.Contains("PROPFIND", results);
            Assert.Contains("PROPPATCH", results);
            Assert.Contains("LOCK", results);
            Assert.Contains("UNLOCK", results);
        }
    }
}
