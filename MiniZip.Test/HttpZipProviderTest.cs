using System;
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
                Assert.Equal(Strings.UnsuccessfulHttpStatusCodeWhenGettingLength, exception.Message);
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
