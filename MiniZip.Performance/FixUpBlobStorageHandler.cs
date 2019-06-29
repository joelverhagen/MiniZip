using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    public class FixUpBlobStorageHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Convert the "x-ms-range" header to the "Range" header. The "Range" header is recognized by things other
            // than Azure Blob Storage.
            if (request.Headers.TryGetValues("x-ms-range", out var values))
            {
                request.Headers.TryAddWithoutValidation("Range", values);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
