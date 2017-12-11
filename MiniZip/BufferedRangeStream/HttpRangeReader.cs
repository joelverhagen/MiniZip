using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    public class HttpRangeReader : IRangeReader
    {
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
                    response.EnsureSuccessStatusCode();

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        return await stream.ReadToEndAsync(dst, dstOffset, count);
                    }
                }
            }
        }
    }
}
