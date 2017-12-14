using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    public class HttpZipProvider : IHttpZipProvider
    {
        private readonly HttpClient _httpClient;

        public HttpZipProvider(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public int FirstBufferSize { get; set; } = ZipConstants.EndOfCentralRecordBaseSize;
        public int SecondBufferSize { get; set; } = 4096;
        public int BufferGrowthExponent { get; set; } = 2;

        public async Task<ZipDirectoryReader> GetReaderAsync(Uri requestUri)
        {
            // Determine if the exists endpoint's length and whether it supports range requests.
            using (var request = new HttpRequestMessage(HttpMethod.Head, requestUri))
            using (var response = await _httpClient.SendAsync(request))
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new MiniZipException(Strings.UnsuccessfulHttpStatusCodeWhenGettingLength);
                }

                if (response.Content?.Headers?.ContentLength == null)
                {
                    throw new MiniZipException(Strings.ContentLengthHeaderNotFound);
                }

                var length = response.Content.Headers.ContentLength.Value;

                if (response.Headers.AcceptRanges == null
                    || !response.Headers.AcceptRanges.Contains(HttpConstants.BytesUnit))
                {
                    throw new MiniZipException(string.Format(
                        Strings.AcceptRangesBytesValueNotFoundFormat,
                        HttpConstants.BytesUnit));
                }

                var httpRangeReader = new HttpRangeReader(_httpClient, requestUri);
                var bufferSizeProvider = new ZipBufferSizeProvider(FirstBufferSize, SecondBufferSize, BufferGrowthExponent);
                var stream = new BufferedRangeStream(httpRangeReader, length, bufferSizeProvider);
                var reader = new ZipDirectoryReader(stream);

                return reader;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
