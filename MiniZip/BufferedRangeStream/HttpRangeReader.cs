using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// A range reader which reads bytes from a specific URL. The URL must support range requests.
    /// </summary>
    public class HttpRangeReader : IRangeReader
    {
        private readonly HttpClient _httpClient;
        private readonly Uri _requestUri;
        private readonly long _length;
        private readonly EntityTagHeaderValue _etag;
        private readonly bool _requireContentRange;
        private readonly IThrottle _httpThrottle;

        /// <summary>
        /// Initializes the HTTP range reader. The provided <see cref="HttpClient"/> is not disposed by this instance.
        /// </summary>
        /// <param name="httpClient">The HTTP client used to make the HTTP requests.</param>
        /// <param name="requestUri">The URL to request bytes from.</param>
        /// <param name="length">The length of the request URI, a size in bytes.</param>
        /// <param name="etag">The optional etag header to be used for follow-up requests.</param>
        /// <param name="requireContentRange">Whether or not to require and validate the Content-Range header.</param>
        /// <param name="httpThrottle">The throttle to use for HTTP operations.</param>
        public HttpRangeReader(HttpClient httpClient, Uri requestUri, long length, EntityTagHeaderValue etag, bool requireContentRange, IThrottle httpThrottle)
        {
            _httpClient = httpClient;
            _requestUri = requestUri;
            _length = length;
            _etag = etag;
            _requireContentRange = requireContentRange;
            _httpThrottle = httpThrottle ?? NullThrottle.Instance;
        }

        /// <summary>
        /// Initializes the HTTP range reader. The provided <see cref="HttpClient"/> is not disposed by this instance.
        /// </summary>
        /// <param name="httpClient">The HTTP client used to make the HTTP requests.</param>
        /// <param name="requestUri">The URL to request bytes from.</param>
        /// <param name="length">The length of the request URI, a size in bytes.</param>
        /// <param name="etag">The optional etag header to be used for follow-up requests.</param>
        /// <param name="requireContentRange">Whether or not to require and validate the Content-Range header.</param>
        public HttpRangeReader(HttpClient httpClient, Uri requestUri, long length, EntityTagHeaderValue etag, bool requireContentRange)
            : this(httpClient, requestUri, length, etag, requireContentRange: true, httpThrottle: null)
        {
        }

        /// <summary>
        /// Initializes the HTTP range reader. The provided <see cref="HttpClient"/> is not disposed by this instance.
        /// </summary>
        /// <param name="httpClient">The HTTP client used to make the HTTP requests.</param>
        /// <param name="requestUri">The URL to request bytes from.</param>
        /// <param name="length">The length of the request URI, a size in bytes.</param>
        /// <param name="etag">The optional etag header to be used for follow-up requests.</param>
        public HttpRangeReader(HttpClient httpClient, Uri requestUri, long length, EntityTagHeaderValue etag)
            : this(httpClient, requestUri, length, etag, requireContentRange: true)
        {
        }

        /// <summary>
        /// Read bytes from the request URL.
        /// </summary>
        /// <param name="srcOffset">The position from the beginning of the request URL's response body to start reading.</param>
        /// <param name="dst">The destination buffer to write bytes to.</param>
        /// <param name="dstOffset">The offset in the destination buffer.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>The number of bytes read.</returns>
        public async Task<int> ReadAsync(long srcOffset, byte[] dst, int dstOffset, int count)
        {
            return await RetryHelper.RetryAsync(async () =>
            {
                await _httpThrottle.WaitAsync();
                try
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Get, _requestUri))
                    {
                        request.Headers.Range = new RangeHeaderValue(srcOffset, (srcOffset + count) - 1);

                        if (_etag != null)
                        {
                            request.Headers.IfMatch.Add(_etag);
                        }

                        using (var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                        {
                            if (response.StatusCode != HttpStatusCode.PartialContent)
                            {
                                throw new MiniZipHttpStatusCodeException(
                                    string.Format(
                                        Strings.NonPartialContentHttpResponse,
                                        (int)response.StatusCode,
                                        response.ReasonPhrase),
                                    response.StatusCode,
                                    response.ReasonPhrase);
                            }

                            if (_requireContentRange || response.Content.Headers.ContentRange != null)
                            {
                                if (response.Content.Headers.ContentRange == null)
                                {
                                    throw new MiniZipException(Strings.ContentRangeHeaderNotFound);
                                }

                                if (!response.Content.Headers.ContentRange.HasRange
                                    || response.Content.Headers.ContentRange.Unit != HttpConstants.BytesUnit
                                    || response.Content.Headers.ContentRange.From != srcOffset
                                    || response.Content.Headers.ContentRange.To != (srcOffset + count) - 1)
                                {
                                    throw new MiniZipException(Strings.InvalidContentRangeHeader);
                                }

                                if (response.Content.Headers.ContentRange.Length != _length)
                                {
                                    throw new MiniZipException(string.Format(
                                        Strings.LengthOfHttpContentChanged,
                                        response.Content.Headers.ContentRange.Length,
                                        _length));
                                }
                            }

                            using (var stream = await response.Content.ReadAsStreamAsync())
                            {
                                return await stream.ReadToEndAsync(dst, dstOffset, count);
                            }
                        }
                    }
                }
                finally
                {
                    _httpThrottle.Release();
                }
            });
        }
    }
}
