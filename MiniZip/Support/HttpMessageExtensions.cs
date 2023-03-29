using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    internal static class HttpMessageExtensions
    {
        public static async Task<MiniZipHttpException> ToHttpExceptionAsync(this HttpResponseMessage response, string message)
        {
            Exception innerException = null;

            string debugRequest = null;
            if (response.RequestMessage != null)
            {
                try
                {
                    debugRequest = await response.RequestMessage.GetDebugStringAsync();
                }
                catch (Exception ex)
                {
                    debugRequest = "<error>";
                    innerException = ex;
                }
            }

            string debugResponse;
            try
            {
                debugResponse = await response.GetDebugStringAsync();
            }
            catch (Exception ex)
            {
                debugResponse = "<error>";
                if (innerException == null)
                {
                    innerException = ex;
                }
                else
                {
                    innerException = new AggregateException(innerException, ex);
                }
            }

            return new MiniZipHttpException(
                message,
                debugRequest,
                response.StatusCode,
                response.ReasonPhrase,
                debugResponse,
                innerException);
        }

        public static async Task<string> GetDebugStringAsync(this HttpRequestMessage request)
        {
            var builder = new StringBuilder();

            builder.AppendFormat("{0} {1} HTTP/{2}\r\n", request.Method, request.RequestUri.AbsoluteUri, request.Version);

            await AppendHeadersAndBody(builder, request.Headers, request.Content);

            return builder.ToString();
        }

        public static async Task<string> GetDebugStringAsync(this HttpResponseMessage response)
        {
            var builder = new StringBuilder();

            builder.AppendFormat("HTTP/{0} {1} {2}\r\n", response.Version, (int)response.StatusCode, response.ReasonPhrase);

            await AppendHeadersAndBody(builder, response.Headers, response.Content);

            return builder.ToString();
        }

        private static async Task AppendHeadersAndBody(
            StringBuilder builder,
            IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers,
            HttpContent content)
        {
            if (content != null)
            {
                headers = headers.Concat(content.Headers);
            }

            // Write the headers.
            foreach (var header in headers)
            {
                foreach (var value in header.Value)
                {
                    builder.AppendFormat("{0}: {1}\r\n", header.Key, value);
                }
            }

            builder.Append("\r\n");

            // Write the request or response body.
            if (content != null)
            {
                using (var stream = await content.ReadAsStreamAsync())
                {
                    var buffer = new byte[1024 * 32 + 1];
                    var totalRead = 0;
                    int read;
                    do
                    {
                        read = await stream.ReadAsync(buffer, totalRead, buffer.Length - totalRead);
                        totalRead += read;
                    }
                    while (totalRead < buffer.Length && read > 0);

                    var hasMore = totalRead == buffer.Length;
                    var dataLength = Math.Min(totalRead, buffer.Length - 1);

                    try
                    {
                        using (var memoryStream = new MemoryStream(buffer, 0, dataLength))
                        using (var reader = new StreamReader(memoryStream))
                        {
                            // Write the response body as a string.
                            builder.Append(reader.ReadToEnd());
                        }
                    }
                    catch (Exception)
                    {
                        // Write the response body as base64 bytes.
                        builder.Append("<base64>\r\n");
                        builder.Append(Convert.ToBase64String(buffer, 0, dataLength));
                    }

                    if (hasMore)
                    {
                        builder.Append("\r\n<truncated>");
                    }
                }
            }
        }
    }
}
