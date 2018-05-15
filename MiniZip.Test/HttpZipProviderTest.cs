using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Knapcode.MiniZip
{
    public class HttpZipProviderTest
    {
        public class GetReaderAsync : IDisposable
        {
            private Func<HttpRequestMessage, HttpResponseMessage> _getResponse;
            private readonly HttpClient _httpClient;
            private readonly HttpZipProvider _target;
            private readonly Uri _requestUri;

            public GetReaderAsync()
            {
                _getResponse = request => new HttpResponseMessage(HttpStatusCode.NotFound);
                _httpClient = new HttpClient(new TestMessageHandler(r => _getResponse(r)));
                _target = new HttpZipProvider(_httpClient);
                _requestUri = new Uri("http://example/foo.zip");
            }

            [Fact]
            public async Task ThrowsWhenRecievingNonSuccessStatusCode()
            {
                var exception = await Assert.ThrowsAsync<MiniZipException>(
                    () => _target.GetReaderAsync(_requestUri));
                Assert.Equal(
                    "The HTTP response did not have a success status code while trying to determine the content length. The response was 404 Not Found.",
                    exception.Message);
            }

            [Fact]
            public async Task ThrowsWhenMissingContentLength()
            {
                // Arrange
                _getResponse = r => new HttpResponseMessage(HttpStatusCode.OK);

                // Act & Assert
                var exception = await Assert.ThrowsAsync<MiniZipException>(
                    () => _target.GetReaderAsync(_requestUri));
                Assert.Equal(Strings.ContentLengthHeaderNotFound, exception.Message);
            }

            [Fact]
            public async Task ThrowsWhenMissingAcceptRanges()
            {
                // Arrange
                _getResponse = r => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("test content"),
                };

                // Act & Assert
                var exception = await Assert.ThrowsAsync<MiniZipException>(
                    () => _target.GetReaderAsync(_requestUri));
                Assert.Equal(
                    string.Format(Strings.AcceptRangesBytesValueNotFoundFormat, HttpConstants.BytesUnit),
                    exception.Message);
            }

            [Fact]
            public async Task ThrowsWhenMissingAcceptRangesBytes()
            {
                // Arrange
                _getResponse = r => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("test content"),
                    Headers =
                    {
                        AcceptRanges = { "none" },
                    },
                };

                // Act & Assert
                var exception = await Assert.ThrowsAsync<MiniZipException>(
                    () => _target.GetReaderAsync(_requestUri));
                Assert.Equal(
                    string.Format(Strings.AcceptRangesBytesValueNotFoundFormat, HttpConstants.BytesUnit),
                    exception.Message);
            }

            [Fact]
            public async Task ReturnsWorkingZipDirectoryReader()
            {
                // Arrange
                var fileName = "System.IO.Compression/refzipfiles/normal.zip";
                using (var server = TestUtility.GetTestServer(TestUtility.TestDataDirectory))
                using (var client = server.CreateClient())
                {
                    var requestUri = new Uri(new Uri(server.BaseAddress, TestUtility.TestServerDirectory + "/"), fileName);
                    var target = new HttpZipProvider(client);

                    // Act
                    var reader = await target.GetReaderAsync(requestUri);

                    // Assert
                    var actual = await reader.ReadAsync();
                    var expected = await TestUtility.ReadWithMiniZipAsync(TestUtility.BufferTestData(fileName));
                    TestUtility.VerifyJsonEquals(expected.Data, actual);
                } 
            }

            [Fact]
            public async Task UsesETagWhenRequiringAnETag()
            {
                // Arrange
                var fileName = "System.IO.Compression/refzipfiles/normal.zip";
                using (var server = TestUtility.GetTestServer(TestUtility.TestDataDirectory))
                using (var client = server.CreateClient())
                {
                    var requestUri = new Uri(new Uri(server.BaseAddress, TestUtility.TestServerDirectory + "/"), fileName);
                    var target = new HttpZipProvider(client) { ETagBehavior = ETagBehavior.Required };

                    // Act
                    var reader = await target.GetReaderAsync(requestUri);

                    // Assert
                    var actual = await reader.ReadAsync();
                    var expected = await TestUtility.ReadWithMiniZipAsync(TestUtility.BufferTestData(fileName));
                    TestUtility.VerifyJsonEquals(expected.Data, actual);
                }
            }

            [Fact]
            public async Task FailsWhenRequiredETagIsMissing()
            {
                // Arrange
                var fileName = "System.IO.Compression/refzipfiles/normal.zip";
                using (var server = TestUtility.GetTestServer(TestUtility.TestDataDirectory, etags: false))
                using (var client = server.CreateClient())
                {
                    var requestUri = new Uri(new Uri(server.BaseAddress, TestUtility.TestServerDirectory + "/"), fileName);
                    var target = new HttpZipProvider(client) { ETagBehavior = ETagBehavior.Required };

                    // Act & Assert
                    var ex = await Assert.ThrowsAsync<MiniZipException>(() => target.GetReaderAsync(requestUri));
                    Assert.Equal("An ETag header is required when using ETagBehavior.Required.", ex.Message);
                }
            }

            [Theory]
            [InlineData(ETagBehavior.Ignore, true)]
            [InlineData(ETagBehavior.Required, false)]
            [InlineData(ETagBehavior.UseIfPresent, false)]
            public async Task HandlesChangingETagProperly(ETagBehavior etagBehavior, bool success)
            {
                // Arrange
                using (var directory = TestDirectory.Create())
                {
                    var fileName = "normal.zip";
                    var serverDir = Path.Combine(directory, TestUtility.TestServerDirectory);
                    var filePath = Path.Combine(serverDir, fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                    using (var server = TestUtility.GetTestServer(
                        serverDir,
                        etags: true,
                        middleware: async (context, next) =>
                        {
                            await next.Invoke();

                            File.SetLastWriteTimeUtc(filePath, DateTime.UtcNow);
                        }))
                    using (var client = server.CreateClient())
                    {
                        File.Copy(
                            Path.Combine(TestUtility.TestDataDirectory, "System.IO.Compression/refzipfiles/normal.zip"),
                            filePath);
                        var requestUri = new Uri(new Uri(server.BaseAddress, TestUtility.TestServerDirectory + "/"), fileName);
                        var target = new HttpZipProvider(client) { ETagBehavior = etagBehavior };
                        var reader = await target.GetReaderAsync(requestUri);

                        // Act & Assert
                        if (success)
                        {
                            var actual = await reader.ReadAsync();
                            var expected = await TestUtility.ReadWithMiniZipAsync(TestUtility.BufferTestData(filePath));
                            TestUtility.VerifyJsonEquals(expected.Data, actual);
                        }
                        else
                        {
                            var ex = await Assert.ThrowsAsync<MiniZipException>(() => reader.ReadAsync());
                            Assert.Equal(
                                "The HTTP response did not have the expected status code HTTP 206 Partial Content. The response was 412 Precondition Failed.",
                                ex.Message);
                        }
                        
                    }
                }
            }

            [Fact]
            public async Task RejectsChangedLength()
            {
                // Arrange
                using (var directory = TestDirectory.Create())
                {
                    var fileName = "empty.zip";
                    var serverDir = Path.Combine(directory, TestUtility.TestServerDirectory);
                    var filePath = Path.Combine(serverDir, fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                    using (var server = TestUtility.GetTestServer(
                        serverDir,
                        etags: true,
                        middleware: async (context, next) =>
                        {
                            await next.Invoke();

                            File.Delete(filePath);
                            File.Copy(
                                Path.Combine(TestUtility.TestDataDirectory, "System.IO.Compression/refzipfiles/normal.zip"),
                                filePath);
                        }))
                    using (var client = server.CreateClient())
                    {
                        File.Copy(
                            Path.Combine(TestUtility.TestDataDirectory, "System.IO.Compression/refzipfiles/empty.zip"),
                            filePath);
                        var requestUri = new Uri(new Uri(server.BaseAddress, TestUtility.TestServerDirectory + "/"), fileName);
                        var target = new HttpZipProvider(client) { ETagBehavior = ETagBehavior.Ignore };
                        var reader = await target.GetReaderAsync(requestUri);

                        // Act & Assert
                        var ex = await Assert.ThrowsAsync<MiniZipException>(() => reader.ReadAsync());
                        Assert.Equal(
                            "The length of the ZIP file fetched over HTTP changed from the expected 2671162 bytes to 22 bytes.",
                            ex.Message);
                    }
                }
            }

            public void Dispose()
            {
                _httpClient?.Dispose();
            }
        }

        private class TestMessageHandler : HttpMessageHandler
        {
            private Func<HttpRequestMessage, HttpResponseMessage> _getResponse;

            public TestMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> getResponse)
            {
                _getResponse = getResponse;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_getResponse(request));
            }
        }
    }
}
