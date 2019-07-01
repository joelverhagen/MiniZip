using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    public class FixUpBlobStorageHandler : DelegatingHandler
    {
        private const string XMsBlobTypeHeader = "x-ms-blob-type";
        private const string XMsRangeHeader = "x-ms-range";
        private const string RangeHeader = "Range";
        private const string BlockBlob = "BlockBlob";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Convert the "x-ms-range" header to the "Range" header. The "Range" header is recognized by things other
            // than Azure Blob Storage.
            if (request.Headers.TryGetValues(XMsRangeHeader, out var values))
            {
                request.Headers.TryAddWithoutValidation(RangeHeader, values);
            }

            var response = await base.SendAsync(request, cancellationToken);

            if (!response.Headers.Contains(XMsBlobTypeHeader))
            {
                response.Headers.TryAddWithoutValidation(XMsBlobTypeHeader, BlockBlob);
            }

            return response;
        }
    }
}
