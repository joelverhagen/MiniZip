using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    public class HttpClientDelegatingHandler : DelegatingHandler
    {
        private readonly HttpClient _httpClient;

        public HttpClientDelegatingHandler(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var clonedRequest = await CloneAsync(request);

            var response = await _httpClient.SendAsync(clonedRequest, cancellationToken);

            response.RequestMessage = request;

            return response;
        }

        private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request)
        {
            var clonedContent = await CloneAsync(request.Content);

            var clonedRequest = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Content = clonedContent,
                Version = request.Version,
            };

            foreach (var property in request.Properties)
            {
                clonedRequest.Properties.Add(property);
            }

            foreach (var header in request.Headers)
            {
                clonedRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clonedRequest;
        }

        private static async Task<HttpContent> CloneAsync(HttpContent content)
        {
            if (content == null)
            {
                return null;
            }

            var memoryStream = new MemoryStream();
            await content.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var clone = new StreamContent(memoryStream);

            foreach (var header in content.Headers)
            {
                clone.Headers.Add(header.Key, header.Value);
            }

            return clone;
        }
    }
}
