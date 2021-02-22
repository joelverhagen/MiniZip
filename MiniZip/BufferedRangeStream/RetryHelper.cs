using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    internal static class RetryHelper
    {
        public static async Task<T> RetryAsync<T>(Func<Task<T>> actAsync)
        {
            const int maxAttempts = 3;
            var attempt = 0;
            while (true)
            {
                try
                {
                    attempt++;
                    return await actAsync();
                }
                catch (Exception ex) when (ex is MiniZipHttpException || ex is IOException || ex is HttpRequestException)
                {
                    if (attempt >= maxAttempts)
                    {
                        throw;
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                }
            }
        }
    }
}
