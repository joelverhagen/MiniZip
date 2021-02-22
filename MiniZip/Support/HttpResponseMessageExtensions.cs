using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    internal static class HttpResponseMessageExtensions
    {
        public static async Task<MiniZipHttpException> ToHttpExceptionAsync(this HttpResponseMessage response, string message)
        {
            string debugResponse = null;
            Exception innerException = null;
            try
            {
                debugResponse = await response.GetDebugStringAsync();
            }
            catch (Exception ex)
            {
                debugResponse = "<error>";
                innerException = ex;
            }

            return new MiniZipHttpException(
                message,
                response.StatusCode,
                response.ReasonPhrase,
                debugResponse,
                innerException);
        }

        public static async Task<string> GetDebugStringAsync(this HttpResponseMessage response)
        {
            var builder = new StringBuilder();

            // Write the opening line.
            builder.AppendFormat("HTTP/{0} {1} {2}\r\n", response.Version, (int)response.StatusCode, response.ReasonPhrase);

            // Write the headers.
            var headers = response.Headers.AsEnumerable();
            if (response.Content != null)
            {
                headers = headers.Concat(response.Content.Headers);
            }

            foreach (var header in headers)
            {
                foreach (var value in header.Value)
                {
                    builder.AppendFormat("{0}: {1}\r\n", header.Key, value);
                }
            }

            builder.Append("\r\n");

            // Write the response body.
            if (response.Content != null)
            {
                using (var stream = await response.Content.ReadAsStreamAsync())
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

            return builder.ToString();
        }
    }
}
