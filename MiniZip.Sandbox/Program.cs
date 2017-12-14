using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    public class Program
    {
        public static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        private static async Task MainAsync()
        {
            var urls = new[]
            {
                "https://api.nuget.org/v3-flatcontainer/newtonsoft.json/10.0.3/newtonsoft.json.10.0.3.nupkg",
                "https://api.nuget.org/v3-flatcontainer/nunit/3.9.0/nunit.3.9.0.nupkg",
                "https://api.nuget.org/v3-flatcontainer/entityframework/6.2.0/entityframework.6.2.0.nupkg",
                "https://api.nuget.org/v3-flatcontainer/jquery/3.2.1/jquery.3.2.1.nupkg",
                "https://api.nuget.org/v3-flatcontainer/htmlagilitypack/1.6.7/htmlagilitypack.1.6.7.nupkg",
            };

            using (var httpClientHandler = new HttpClientHandler())
            using (var loggingHandler = new LoggingHandler { InnerHandler = httpClientHandler })
            using (var httpClient = new HttpClient(loggingHandler))
            {
                var httpZipProvider = new HttpZipProvider(httpClient);

                foreach (var url in urls)
                {
                    Console.WriteLine(new string('=', 40));
                    Console.WriteLine();

                    using (var reader = await httpZipProvider.GetReaderAsync(new Uri(url)))
                    {
                        var zipDirectory = await reader.ReadAsync();

                        Console.WriteLine("Top 5 ZIP entries by compressed size:");
                        var entries = zipDirectory
                            .Entries
                            .OrderByDescending(x => x.GetCompressedSize())
                            .Take(5)
                            .ToList();
                        for (var i = 0; i < entries.Count; i++)
                        {
                            Console.WriteLine($"{i + 1}. {entries[i].GetName()} ({entries[i].GetCompressedSize():N0} bytes)");
                        }
                    }

                    Console.WriteLine();
                }

                Console.WriteLine(new string('=', 40));
                Console.WriteLine();

                var ratio = ((double)loggingHandler.TotalResponseBodyBytes) / loggingHandler.TotalContentLength;
                Console.WriteLine($"Total ZIP files checked:    {urls.Length:N0}");
                Console.WriteLine($"Total HTTP requests:        {loggingHandler.TotalRequests:N0}");
                Console.WriteLine($"Total Content-Length bytes: {loggingHandler.TotalContentLength:N0}");
                Console.WriteLine($"Actual downloaded bytes:    {loggingHandler.TotalResponseBodyBytes:N0}"); // well, sort of...
                Console.WriteLine($"Downloaded %:               {Math.Round(ratio * 100, 3):0.000}%");
            }

        }

        private class LoggingHandler : DelegatingHandler
        {
            public long _totalResponseBodyBytes = 0;
            public long _totalContentLength = 0;
            public int _totalRequests = 0;
            
            private static readonly HashSet<string> InterestingHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Range",
                "Accept-Ranges",
                "Content-Range",
                "Content-Length",
            };

            public long TotalResponseBodyBytes => _totalResponseBodyBytes;
            public long TotalContentLength => _totalContentLength;
            public int TotalRequests => _totalRequests;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Console.WriteLine($"==> {request.Method} {request.RequestUri}");
                OutputHeaders(request.Headers);
                Console.WriteLine();

                var response = await base.SendAsync(request, cancellationToken);

                Console.WriteLine($"<== {(int)response.StatusCode} {response.ReasonPhrase}");
                OutputHeaders(response.Headers.Concat(response.Content.Headers));
                Console.WriteLine();

                if (request.Method == HttpMethod.Get)
                {
                    Interlocked.Add(ref _totalResponseBodyBytes, response.Content.Headers.ContentLength.Value);
                }
                else if (request.Method == HttpMethod.Head)
                {
                    Interlocked.Add(ref _totalContentLength, response.Content.Headers.ContentLength.Value);
                }

                Interlocked.Increment(ref _totalRequests);

                return response;
            }

            private static void OutputHeaders(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
            {
                foreach (var header in headers)
                {
                    if (InterestingHeaders.Contains(header.Key))
                    {
                        Console.WriteLine($"    {header.Key}: {string.Join(", ", header.Value)}");
                    }
                }
            }
        }
    }
}
