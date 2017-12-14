using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    public class Program
    {
        public static void Main(string[] args)
        {
            LargeAsync().GetAwaiter().GetResult();
        }

        private static async Task SmallAsync()
        {
            var url = "https://api.nuget.org/v3-flatcontainer/newtonsoft.json/10.0.3/newtonsoft.json.10.0.3.nupkg";
            using (var httpClient = new HttpClient())
            {
                var httpZipProvider = new HttpZipProvider(httpClient);

                using (var zipDirectoryReader = await httpZipProvider.GetReaderAsync(new Uri(url)))
                {
                    var zipDirectory = await zipDirectoryReader.ReadAsync();

                    Console.WriteLine("Top 5 ZIP entries by compressed size:");
                    var entries = zipDirectory
                        .Entries
                        .OrderByDescending(x => x.GetCompressedSize())
                        .Take(5)
                        .ToList();
                    for (var i = 0; i < entries.Count; i++)
                    {
                        Console.WriteLine($"{i + 1}. {entries[i].GetName()}");
                    }
                }
            }
        }

        private static async Task LargeAsync()
        {
            // Use the top 5 NuGet packages as an example.
            var urls = new[]
            {
                "https://api.nuget.org/v3-flatcontainer/newtonsoft.json/10.0.3/newtonsoft.json.10.0.3.nupkg",
                "https://api.nuget.org/v3-flatcontainer/nunit/3.9.0/nunit.3.9.0.nupkg",
                "https://api.nuget.org/v3-flatcontainer/entityframework/6.2.0/entityframework.6.2.0.nupkg",
                "https://api.nuget.org/v3-flatcontainer/jquery/3.2.1/jquery.3.2.1.nupkg",
                "https://api.nuget.org/v3-flatcontainer/htmlagilitypack/1.6.7/htmlagilitypack.1.6.7.nupkg",
            };

            // Set up and HTTP client that logs HTTP requests, to help clarify this example.
            using (var httpClientHandler = new HttpClientHandler())
            using (var loggingHandler = new LoggingHandler { InnerHandler = httpClientHandler })
            using (var httpClient = new HttpClient(loggingHandler))
            {
                // This provider uses and HTTP client and initializes a ZipDirectoryReader from a URL. This URL
                // must support HEAD method, Content-Length response header, and Range request header.
                var httpZipProvider = new HttpZipProvider(httpClient);

                foreach (var url in urls)
                {
                    Console.WriteLine(new string('=', 40));
                    Console.WriteLine();

                    // Initialize the reader. This performs a HEAD request to determine if the length of the
                    // ZIP file and whether the URL supports Range requests.
                    using (var reader = await httpZipProvider.GetReaderAsync(new Uri(url)))
                    {
                        // Read the ZIP file by requesting just the Central Directory part of the .zip.
                        var zipDirectory = await reader.ReadAsync();

                        // At this point, we known all about the entries of the .zip file include name, compressed
                        // size, and relative offset in the .zip file.
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

                // Summarize the work done. For NuGet packages, it is very common to download less than 1% of the
                // package content while determining the .zip entry metadata.
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
