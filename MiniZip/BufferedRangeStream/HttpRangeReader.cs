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
        private string _etag;
        private readonly string _etagQuotes;
        private readonly string _etagNoQuotes;
        private readonly bool _requireContentRange;
        private readonly bool _sendXMsVersionHeader;
        private readonly bool _allowETagVariants;
        private readonly IThrottle _httpThrottle;

        /// <summary>
        /// Initializes the HTTP range reader. The provided <see cref="HttpClient"/> is not disposed by this instance.
        /// </summary>
        /// <param name="httpClient">The HTTP client used to make the HTTP requests.</param>
        /// <param name="requestUri">The URL to request bytes from.</param>
        /// <param name="length">The length of the request URI, a size in bytes.</param>
        /// <param name="etag">The optional etag header to be used for follow-up requests.</param>
        /// <param name="requireContentRange">Whether or not to require and validate the Content-Range header.</param>
        /// <param name="sendXMsVersionHeader">Whether or not to send the <c>x-ms-version: 2013-08-15</c> request header.</param>
        /// <param name="allowETagVariants">Whether or not to allow different variants of the etag.</param>
        /// <param name="httpThrottle">The throttle to use for HTTP operations.</param>
        public HttpRangeReader(HttpClient httpClient, Uri requestUri, long length, string etag, bool requireContentRange, bool sendXMsVersionHeader, bool allowETagVariants, IThrottle httpThrottle)
        {
            _httpClient = httpClient;
            _requestUri = requestUri;
            _length = length;

            _etag = etag;
            if (etag != null && allowETagVariants)
            {
                if (etag.StartsWith("\"") && etag.EndsWith("\""))
                {
                    _etagQuotes = etag;
                    _etagNoQuotes = etag.Substring(1, etag.Length - 2);
                }
                else
                {
                    _etagQuotes = "\"" + etag + "\"";
                    _etagNoQuotes = etag;
                }
            }

            _requireContentRange = requireContentRange;
            _sendXMsVersionHeader = sendXMsVersionHeader;
            _allowETagVariants = allowETagVariants;
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
        /// <param name="httpThrottle">The throttle to use for HTTP operations.</param>
        public HttpRangeReader(HttpClient httpClient, Uri requestUri, long length, string etag, bool requireContentRange, IThrottle httpThrottle)
            : this(httpClient, requestUri, length, etag, requireContentRange, sendXMsVersionHeader: false, allowETagVariants: false, httpThrottle: httpThrottle)
        {
        }

        /// <summary>
        /// Initializes the HTTP range reader. The provided <see cref="HttpClient"/> is not disposed by this instance.
        /// </summary>
        /// <param name="httpClient">The HTTP client used to make the HTTP requests.</param>
        /// <param name="requestUri">The URL to request bytes from.</param>
        /// <param name="length">The length of the request URI, a size in bytes.</param>
        /// <param name="etag">The optional etag header to be used for follow-up requests.</param>
        /// <param name="requireContentRange">Whether or not to require and validate the Content-Range header.</param>
        public HttpRangeReader(HttpClient httpClient, Uri requestUri, long length, string etag, bool requireContentRange)
            : this(httpClient, requestUri, length, etag, requireContentRange, httpThrottle: null)
        {
        }

        /// <summary>
        /// Initializes the HTTP range reader. The provided <see cref="HttpClient"/> is not disposed by this instance.
        /// </summary>
        /// <param name="httpClient">The HTTP client used to make the HTTP requests.</param>
        /// <param name="requestUri">The URL to request bytes from.</param>
        /// <param name="length">The length of the request URI, a size in bytes.</param>
        /// <param name="etag">The optional etag header to be used for follow-up requests.</param>
        public HttpRangeReader(HttpClient httpClient, Uri requestUri, long length, string etag)
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
            return await RetryHelper.RetryAsync(async lastException =>
            {
                await _httpThrottle.WaitAsync();
                try
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Get, _requestUri))
                    {
                        if (_sendXMsVersionHeader)
                        {
                            request.Headers.TryAddWithoutValidation("x-ms-version", "2013-08-15");
                        }

                        if (_etag != null)
                        {
                            if (lastException is MiniZipHttpException ex
                                && ex.StatusCode == HttpStatusCode.PreconditionFailed
                                && _allowETagVariants == true)
                            {
                                // Swap out the etag version (quoted vs. unquoted, related to an old Azure Blob Storage
                                // bug) if there is an HTTP 412 Precondition Failed. This may be caused by the wrong
                                // version of the etag being cached by an intermediate layer.
                                _etag = _etag == _etagNoQuotes ? _etagQuotes : _etagNoQuotes;
                            }

                            request.Headers.TryAddWithoutValidation("If-Match", _etag);
                        }

                        request.Headers.Range = new RangeHeaderValue(srcOffset, (srcOffset + count) - 1);

                        using (var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                        {
                            if (response.StatusCode != HttpStatusCode.PartialContent)
                            {
                                throw await response.ToHttpExceptionAsync(string.Format(
                                    Strings.NonPartialContentHttpResponse,
                                    (int)response.StatusCode,
                                    response.ReasonPhrase));
                            }

                            if (_requireContentRange || response.Content.Headers.ContentRange != null)
                            {
                                if (response.Content.Headers.ContentRange == null)
                                {
                                    throw await response.ToHttpExceptionAsync(Strings.ContentRangeHeaderNotFound);
                                }

                                if (!response.Content.Headers.ContentRange.HasRange
                                    || response.Content.Headers.ContentRange.Unit != HttpConstants.BytesUnit
                                    || response.Content.Headers.ContentRange.From != srcOffset
                                    || response.Content.Headers.ContentRange.To != (srcOffset + count) - 1)
                                {
                                    throw await response.ToHttpExceptionAsync(Strings.InvalidContentRangeHeader);
                                }

                                if (response.Content.Headers.ContentRange.Length != _length)
                                {
                                    throw await response.ToHttpExceptionAsync(string.Format(
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
