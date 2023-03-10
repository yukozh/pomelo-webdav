// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System.Text;
using System.Xml.Linq;
using Pomelo.Storage.WebDAV.Http;

namespace Pomelo.Storage.WebDAV.E2ETests
{
    public class ClientTests : TestBase
    {
        [Fact]
        public async Task PropFindNotFoundTest()
        {
            // Arrange
            using var client = new WebDAVHttpClient() { BaseAddress = new Uri("http://localhost:7000") };

            // Act
            using var response = await client.PropFindAsync("/fake_path");

            // Assert
            Assert.Equal(404, (int)response.StatusCode);
        }

        [Fact]
        public async Task PropFindRootTest()
        {
            // Arrange
            using var client = new WebDAVHttpClient() { BaseAddress = new Uri("http://localhost:7000") };

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
            using var client = new WebDAVHttpClient() { BaseAddress = new Uri("http://localhost:7000") };
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
            using var client = new WebDAVHttpClient() { BaseAddress = new Uri("http://localhost:7000") };

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

        [Fact]
        public async Task HeadTest()
        {
            // Arrange
            using var client = new WebDAVHttpClient() { BaseAddress = new Uri("http://localhost:7000") };
            var testPath = Path.Combine(StoragePath, "client_tests");
            if (!Directory.Exists(testPath))
            {
                Directory.CreateDirectory(testPath);
            }
            File.WriteAllText(Path.Combine(testPath, "2.txt"), "Hello World");

            // Act
            using var response = await client.HeadAsync("/client_tests/2.txt");
            var result = response.ToHeadResult();

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.NotNull(result.LastModified);
            Assert.True(DateTime.UtcNow - result.LastModified.Value < new TimeSpan(0, 0, 10));
            Assert.Equal("bytes", result.AcceptRanges);
            Assert.Equal("Hello World".Length, result.ContentLength);
        }

        [Fact]
        public async Task PropPatchTest()
        {
            // Arrange
            using var client = new WebDAVHttpClient() { BaseAddress = new Uri("http://localhost:7000") };
            var testPath = Path.Combine(StoragePath, "client_tests");
            if (!Directory.Exists(testPath))
            {
                Directory.CreateDirectory(testPath);
            }
            File.WriteAllText(Path.Combine(testPath, "3.txt"), "Hello World");

            // Act
            using var response = await client.PropPatchAsync("/client_tests/3.txt", new List<XElement>
            {
                XElement.Parse(@"<D:prop xmlns:D=""DAV:"" xmlns:Z=""http://ns.example.com/standards/z39.50/""> 
    <Z:Authors> 
        <Z:Author>Yuko Zheng</Z:Author> 
    </Z:Authors> 
</D:prop> ")   
            }, new List<XElement>());
            var result = await response.ToPropPatchResultsAsync();

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task LockAndUnlockTest()
        {
            // Arrange
            using var client = new WebDAVHttpClient() { BaseAddress = new Uri("http://localhost:7000") };
            var testPath = Path.Combine(StoragePath, "client_tests");
            if (!Directory.Exists(testPath))
            {
                Directory.CreateDirectory(testPath);
            }
            File.WriteAllText(Path.Combine(testPath, "4.txt"), "Hello World");

            // Act
            using var response = await client.LockAsync("/client_tests/4.txt", Lock.LockType.Exclusive);
            var lockResult = await response.ToLockResultAsync();
            using var response2 = await client.UnlockAsync("/client_tests/4.txt", lockResult.LockToken);

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response2.IsSuccessStatusCode);
        }

        [Fact]
        public async Task MkcolTest()
        {
            // Arrange
            using var client = new WebDAVHttpClient() { BaseAddress = new Uri("http://localhost:7000") };
            var testPath = Path.Combine(StoragePath, "client_tests");
            if (!Directory.Exists(testPath))
            {
                Directory.CreateDirectory(testPath);
            }

            // Act
            using var response = await client.MkcolAsync("/client_tests/test_folder");

            // Assert
            var response2 = await client.PropFindAsync("/client_tests/test_folder", 0);
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response2.IsSuccessStatusCode);
        }

        [Fact]
        public async Task RefreshLockTest()
        {
            // Arrange
            using var client = new WebDAVHttpClient() { BaseAddress = new Uri("http://localhost:7000") };
            var testPath = Path.Combine(StoragePath, "client_tests");
            if (!Directory.Exists(testPath))
            {
                Directory.CreateDirectory(testPath);
            }
            File.WriteAllText(Path.Combine(testPath, "5.txt"), "Hello World");

            // Act
            var response1 = await client.LockAsync("/client_tests/5.txt", Lock.LockType.Exclusive, 60);
            var result1 = await response1.ToLockResultAsync();
            var response2 = await client.LockAsync("/client_tests/5.txt", Lock.LockType.Exclusive, refreshToken: result1.LockToken, timeoutSeconds: 120);
            var result2 = await response2.ToLockResultAsync();

            // Assert
            Assert.True(response1.IsSuccessStatusCode);
            Assert.True(response2.IsSuccessStatusCode);
            Assert.Equal(60, result1.TimeoutSeconds);
            Assert.Equal(120, result2.TimeoutSeconds);
        }

        [Fact]
        public async Task PropFindWithForwardTest()
        {
            // Arrange
            using var client = new WebDAVHttpClient() { BaseAddress = new Uri("http://localhost:7000") };
            client.DefaultRequestHeaders.Add("X-Forwarded-WebDAV-BaseUrl", "http://somehost/api/webdav");
            var testPath = Path.Combine(StoragePath, "forward_test");
            if (!Directory.Exists(testPath))
            {
                Directory.CreateDirectory(testPath);
            }

            // Act
            using var response = await client.PropFindAsync("/", 1);
            var result = await response.ToPropFindResultsAsync();

            // Assert
            Assert.Contains(result, x => x.Href == "http://somehost/api/webdav/forward_test");
        }

        [Fact]
        public async Task GetAndPutRangeTest()
        {
            // Arrange
            using var client = new WebDAVHttpClient() { BaseAddress = new Uri("http://localhost:7000") };
            var testPath = Path.Combine(StoragePath, "client_tests");
            if (!Directory.Exists(testPath))
            {
                Directory.CreateDirectory(testPath);
            }
            File.WriteAllText(Path.Combine(testPath, "7.txt"), "1234567890");

            // Act
            var response = await client.GetRangeAsync("/client_tests/7.txt", new System.Net.Http.Headers.RangeHeaderValue(1, 2));
            var content = await response.Content.ReadAsStringAsync();
            var response2 = await client.PutRangeAsync("/client_tests/7.txt", new MemoryStream(Encoding.UTF8.GetBytes("ab")), new System.Net.Http.Headers.RangeHeaderValue(1, 2));
            var response3 = await client.GetRangeAsync("/client_tests/7.txt", new System.Net.Http.Headers.RangeHeaderValue(0, 3));
            var content2 = await response3.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal("23", content);
            Assert.Equal("bytes 1-2/10", response.Content.Headers.GetValues("Content-Range").First());
            Assert.Equal("bytes 1-2/*", response2.Content.Headers.GetValues("Content-Range").First());
            Assert.Equal("bytes 0-3/10", response3.Content.Headers.GetValues("Content-Range").First());
            Assert.Equal("1ab4", content2);
        }

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
            Assert.Equal("application/octet-stream", response.Content.Headers.ContentType.MediaType);
        }

        [Fact]
        public async Task MoveTest()
        {
            // Arrange
            using var client = new WebDAVHttpClient() { BaseAddress = new Uri("http://localhost:7000") };
            var testPath = Path.Combine(StoragePath, "client_tests");
            if (!Directory.Exists(testPath))
            {
                Directory.CreateDirectory(testPath);
            }
            File.WriteAllText(Path.Combine(testPath, "7.txt"), "111");

            // Act
            var response = await client.MoveAsync("/client_tests/7.txt", "http://localhost:7000/client_tests/8.txt", false);

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(File.Exists(Path.Combine(testPath, "8.txt")));
        }
    }
}
