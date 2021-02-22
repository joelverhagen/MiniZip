using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly IThrottle _httpThrottle;

        /// <summary>
        /// Initializes an HTTP ZIP provider. This instance does not dispose the provided <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="httpClient">The HTTP client to use for HTTP requests.</param>
        public HttpZipProvider(HttpClient httpClient) : this(httpClient, httpThrottle: null)
        {
        }

        /// <summary>
        /// Initializes an HTTP ZIP provider. This instance does not dispose the provided <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="httpClient">The HTTP client to use for HTTP requests.</param>
        /// <param name="httpThrottle">The throttle to use for HTTP operations.</param>
        public HttpZipProvider(HttpClient httpClient, IThrottle httpThrottle)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _httpThrottle = httpThrottle ?? NullThrottle.Instance;
        }

        /// <summary>
        /// The buffer size provider to use when initializing the <see cref="BufferedRangeStream"/>. If the value of
        /// this property is set to null, <see cref="NullBufferSizeProvider"/> is used.
        /// </summary>
        public IBufferSizeProvider BufferSizeProvider { get; set; } = NullBufferSizeProvider.Instance;

        /// <summary>
        /// How to use ETags found on the ZIP endpoint.
        /// </summary>
        public ETagBehavior ETagBehavior { get; set; } = ETagBehavior.UseIfPresent;

        /// <summary>
        /// Require the ZIP endpoint to have the <c>Accept-Ranges: bytes</c> response header. If this setting is
        /// set to true and the expected response header is not found, an exception will be thrown.
        /// </summary>
        public bool RequireAcceptRanges { get; set; } = true;

        /// <summary>
        /// Whether or not to send the <c>x-ms-version: 2013-08-15</c> request header. This version or greater is
        /// required for Azure Blob Storage to return the <c>Accept-Ranges: bytes</c> response header.
        /// </summary>
        public bool SendXMsVersionHeader { get; set; } = true;

        /// <summary>
        /// Require the ZIP endpoint to have the <c>Content-Range</c> response header when performing range requests.
        /// If this setting is set to true and the expected response header is not found, an exception will be thrown.
        /// </summary>
        public bool RequireContentRange { get; set; } = true;

        /// <summary>
        /// Initialize the buffered range reader stream provided request URL.
        /// </summary>
        /// <param name="requestUri">The request URL.</param>
        /// <returns>The buffered range reader stream.</returns>
        public async Task<Stream> GetStreamAsync(Uri requestUri)
        {
            var tuple = await GetStreamAndHeadersAsync(requestUri);
            return tuple.Item1;
        }

        /// <summary>
        /// Initialize the ZIP directory reader for the provided request URL.
        /// </summary>
        /// <param name="requestUri">The request URL.</param>
        /// <returns>The ZIP directory reader.</returns>
        public async Task<ZipDirectoryReader> GetReaderAsync(Uri requestUri)
        {
            var tuple = await GetStreamAndHeadersAsync(requestUri);
            return new ZipDirectoryReader(tuple.Item1, leaveOpen: false, tuple.Item2);
        }

        private async Task<Tuple<Stream, ILookup<string, string>>> GetStreamAndHeadersAsync(Uri requestUri)
        {
            // Determine if the exists endpoint's length and whether it supports range requests.
            var info = await RetryHelper.RetryAsync(async () =>
            {
                using (var request = new HttpRequestMessage(HttpMethod.Head, requestUri))
                {
                    if (SendXMsVersionHeader)
                    {
                        request.Headers.TryAddWithoutValidation("x-ms-version", "2013-08-15");
                    }

                    await _httpThrottle.WaitAsync();
                    try
                    {
                        using (var response = await _httpClient.SendAsync(request))
                        {
                            if (!response.IsSuccessStatusCode)
                            {
                                throw await response.ToHttpExceptionAsync(
                                    string.Format(
                                        Strings.UnsuccessfulHttpStatusCodeWhenGettingLength,
                                        (int)response.StatusCode,
                                        response.ReasonPhrase));
                            }

                            if (response.Content?.Headers?.ContentLength == null)
                            {
                                throw await response.ToHttpExceptionAsync(Strings.ContentLengthHeaderNotFound);
                            }

                            if (RequireAcceptRanges
                                && (response.Headers.AcceptRanges == null
                                    || !response.Headers.AcceptRanges.Contains(HttpConstants.BytesUnit)))
                            {
                                throw await response.ToHttpExceptionAsync(string.Format(
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
                                throw await response.ToHttpExceptionAsync(string.Format(
                                    Strings.MissingETagHeader,
                                    nameof(MiniZip.ETagBehavior),
                                    nameof(ETagBehavior.Required)));
                            }

                            var headers = Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>()
                                .Concat(response.Headers)
                                .Concat(response.Content.Headers)
                                .SelectMany(x => x.Value.Select(y => new { x.Key, Value = y }))
                                .ToLookup(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

                            return new { Length = length, ETag = etag, Headers = headers };
                        }
                    }
                    finally
                    {
                        _httpThrottle.Release();
                    }
                }
            });

            var httpRangeReader = new HttpRangeReader(_httpClient, requestUri, info.Length, info.ETag, RequireContentRange);
            var bufferSizeProvider = BufferSizeProvider ?? NullBufferSizeProvider.Instance;
            var stream = new BufferedRangeStream(httpRangeReader, info.Length, bufferSizeProvider);

            return Tuple.Create<Stream, ILookup<string, string>>(stream, info.Headers);
        }
    }
}
