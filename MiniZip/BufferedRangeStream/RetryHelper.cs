using System;
using System.IO;
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
                catch (Exception ex) when (ex is MiniZipHttpStatusCodeException || ex is IOException)
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
