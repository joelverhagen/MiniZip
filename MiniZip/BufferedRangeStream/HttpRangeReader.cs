using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    public class HttpRangeReader : IRangeReader
    {
        private const string BytesUnit = "bytes";

        private readonly HttpClient _httpClient;
        private readonly Uri _requestUri;

        public HttpRangeReader(HttpClient httpClient, Uri requestUri)
        {
            _httpClient = httpClient;
            _requestUri = requestUri;
        }

        public async Task<int> ReadAsync(long srcOffset, byte[] dst, int dstOffset, int count)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, _requestUri))
            {
                request.Headers.Range = new RangeHeaderValue(srcOffset, (srcOffset + count) - 1);

                using (var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (response.StatusCode != HttpStatusCode.PartialContent)
                    {
                        throw new MiniZipException("The HTTP response did not have the expected status code HTTP " +
                            "206 Partial Content.");
                    }

                    if (response.Content.Headers.ContentRange == null)
                    {
                        throw new MiniZipException("The Content-Range header was expected but not found.");
                    }

                    if (!response.Content.Headers.ContentRange.HasRange
                        || response.Content.Headers.ContentRange.Unit != BytesUnit
                        || response.Content.Headers.ContentRange.From != srcOffset
                        || response.Content.Headers.ContentRange.To != (srcOffset + count) - 1)
                    {
                        throw new MiniZipException("The Content-Range header has an unexpected value.");
                    }
                    
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        return await stream.ReadToEndAsync(dst, dstOffset, count);
                    }
                }
            }
        }
    }
}
