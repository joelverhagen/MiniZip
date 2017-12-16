using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// A convenience type to simplying reading a ZIP archive over HTTP. This implementation requires that the provided
    /// URL supports HEAD requests, returns a Content-Length, and accepts Content-Length.
    /// </summary>
    public class HttpZipProvider : IHttpZipProvider
    {
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes an HTTP ZIP provider. This instance does not dispose the provided <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="httpClient">The HTTP client to use for HTTP requests.</param>
        public HttpZipProvider(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// The first buffer size in bytes to use when reading. This defaults to 22 bytes, which is the length of the
        /// "end of central directory" record.
        /// </summary>
        public int FirstBufferSize { get; set; } = ZipConstants.EndOfCentralDirectorySize;

        /// <summary>
        /// The second buffer size in bytes to use when reading. This defaults to 4096 bytes.
        /// </summary>
        public int SecondBufferSize { get; set; } = 4096;

        /// <summary>
        /// The exponent to determine the buffer growth rate. This defaults to 2.
        /// </summary>
        public int BufferGrowthExponent { get; set; } = 2;

        /// <summary>
        /// Initialize the ZIP directory reader for the provided request URL.
        /// </summary>
        /// <param name="requestUri">The request URL.</param>
        /// <returns>The ZIP directory reader.</returns>
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
    }
}
