using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
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
        /// How to use ETags found on the ZIP endpoint.
        /// </summary>
        public ETagBehavior ETagBehavior { get; set; } = ETagBehavior.UseIfPresent;

        /// <summary>
        /// Initialize the buffered range reader stream provided request URL.
        /// </summary>
        /// <param name="requestUri">The request URL.</param>
        /// <returns>The buffered range reader stream.</returns>
        public async Task<Stream> GetStreamAsync(Uri requestUri)
        {
            // Determine if the exists endpoint's length and whether it supports range requests.
            var info = await RetryHelper.RetryAsync(async () =>
            {
                using (var request = new HttpRequestMessage(HttpMethod.Head, requestUri))
                using (var response = await _httpClient.SendAsync(request))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new MiniZipHttpStatusCodeException(string.Format(
                            Strings.UnsuccessfulHttpStatusCodeWhenGettingLength,
                            (int)response.StatusCode,
                            response.ReasonPhrase));
                    }

                    if (response.Content?.Headers?.ContentLength == null)
                    {
                        throw new MiniZipException(Strings.ContentLengthHeaderNotFound);
                    }

                    if (response.Headers.AcceptRanges == null
                        || !response.Headers.AcceptRanges.Contains(HttpConstants.BytesUnit))
                    {
                        throw new MiniZipException(string.Format(
                            Strings.AcceptRangesBytesValueNotFoundFormat,
                            HttpConstants.BytesUnit));
                    }

                    var length = response.Content.Headers.ContentLength.Value;

                    var etagBehavior = ETagBehavior;
                    var etag = response.Headers?.ETag;
                    if (etag != null && (etag.IsWeak || etagBehavior == ETagBehavior.Ignore))
                    {
                        etag = null;
                    }

                    if (etag == null && etagBehavior == ETagBehavior.Required)
                    {
                        throw new MiniZipException(string.Format(
                            Strings.MissingETagHeader,
                            nameof(MiniZip.ETagBehavior),
                            nameof(ETagBehavior.Required)));
                    }

                    return new { Length = length, ETag = etag };
                }
            });

            var httpRangeReader = new HttpRangeReader(_httpClient, requestUri, info.Length, info.ETag);
            var bufferSizeProvider = new ZipBufferSizeProvider(FirstBufferSize, SecondBufferSize, BufferGrowthExponent);
            var stream = new BufferedRangeStream(httpRangeReader, info.Length, bufferSizeProvider);

            return stream;
        }

        /// <summary>
        /// Initialize the ZIP directory reader for the provided request URL.
        /// </summary>
        /// <param name="requestUri">The request URL.</param>
        /// <returns>The ZIP directory reader.</returns>
        public async Task<ZipDirectoryReader> GetReaderAsync(Uri requestUri)
        {
            var stream = await GetStreamAsync(requestUri);
            var reader = new ZipDirectoryReader(stream);

            return reader;
        }
    }
}
