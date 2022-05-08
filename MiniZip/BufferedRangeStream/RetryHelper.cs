using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    internal static class RetryHelper
    {
        public static async Task<T> RetryAsync<T>(Func<Exception, Task<T>> actAsync)
        {
            const int maxAttempts = 3;
            var attempt = 0;
            Exception lastException = null;
            while (true)
            {
                try
                {
                    attempt++;
                    return await actAsync(lastException);
                }
                catch (Exception ex) when (ex is MiniZipHttpException || ex is IOException || ex is HttpRequestException)
                {
                    lastException = ex;

                    if (attempt >= maxAttempts ||
                        (ex is MiniZipHttpException mze && mze.StatusCode == HttpStatusCode.NotFound))
                    {
                        throw;
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                }
            }
        }
    }
}
