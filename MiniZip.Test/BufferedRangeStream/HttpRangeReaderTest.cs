using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Knapcode.MiniZip
{
    public class HttpRangeReaderTest
    {
        public class ReadAsync : IDisposable
        {
            private readonly TestDirectory _directory;
            private readonly TestServer _server;
            private readonly HttpClient _client;
            private readonly byte[] _content;
            private readonly string _fileName;
            private readonly byte[] _outputBuffer;
            private readonly Uri _requestUri;
            private readonly HttpRangeReader _target;

            public ReadAsync()
            {
                _directory = TestDirectory.Create();
                _fileName = "test.dat";
                _content = Enumerable.Range(0, 100).Select(x => (byte)x).ToArray();
                File.WriteAllBytes(Path.Combine(_directory, _fileName), _content);

                _server = TestUtility.GetTestServer(_directory);
                _client = _server.CreateClient();
                _requestUri = new Uri(new Uri(_server.BaseAddress, TestUtility.TestServerDirectory + "/"), _fileName);

                _outputBuffer = new byte[_content.Length];

                _target = new HttpRangeReader(_client, _requestUri, _content.Length, etag: null);
            }

            [Fact]
            public async Task ReadsRequestedRange()
            {
                // Arrange
                var expected = Enumerable
                    .Empty<byte>()
                    .Concat(Enumerable.Repeat((byte)0, 5))
                    .Concat(Enumerable.Range(15, 10).Select(x => (byte)x))
                    .Concat(Enumerable.Repeat((byte)0, 85))
                    .ToArray();

                // Act
                var read = await _target.ReadAsync(15, _outputBuffer, 5, 10);

                // Assert
                Assert.Equal(10, read);
                Assert.Equal(expected, _outputBuffer);
            }

            public void Dispose()
            {
                _client?.Dispose();
                _server?.Dispose();
                _directory?.Dispose();
            }
        }
    }
}
