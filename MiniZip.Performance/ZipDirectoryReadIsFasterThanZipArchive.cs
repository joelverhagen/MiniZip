using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Azure.Storage.Blob;
using Xunit;
using Xunit.Abstractions;

namespace Knapcode.MiniZip
{
    public class ZipDirectoryReadIsFasterThanZipArchive : IDisposable
    {
        private static string _fileName = $"{nameof(ZipDirectoryReadIsFasterThanZipArchive)}.{DateTimeOffset.UtcNow:yyyyMMdd.HHmmss.FFFFFFF}.csv";
        private static bool _wroteHeader = false;

        private readonly ITestOutputHelper _output;
        private readonly TestServer _server;
        private readonly HttpClient _client;

        public ZipDirectoryReadIsFasterThanZipArchive(ITestOutputHelper output)
        {
            _output = output;
            _server = TestUtility.GetTestServer(TestUtility.TestDataDirectory);
            _client = _server.CreateClient();
        }

        public void Dispose()
        {
            _client?.Dispose();
            _server?.Dispose();
        }

        [Fact]
        public async Task Run()
        {
            var name1 = "CloudBlockBlob and ZipArchive";
            var average1 = await ExecuteTestsAsync(
                name1,
                async (url) =>
                {
                    var blobClient = new CloudBlobClient(
                        new Uri("https://example"),
                        new FixUpBlobStorageHandler
                        {
                            InnerHandler = new HttpClientDelegatingHandler(_client),
                        });
                    blobClient.DefaultRequestOptions.DisableContentMD5Validation = true;
                    var blob = new CloudBlockBlob(url, blobClient);

                    using (var stream = await blob.OpenReadAsync())
                    using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read))
                    {
                        var entries = zipArchive.Entries.ToList();
                    }
                });

            var name2 = "CloudBlockBlob and ZipDirectoryReader";
            var average2 = await ExecuteTestsAsync(
                name2,
                async (url) =>
                {
                    var blobClient = new CloudBlobClient(
                        new Uri("https://example"),
                        new FixUpBlobStorageHandler
                        {
                            InnerHandler = new HttpClientDelegatingHandler(_client),
                        });
                    blobClient.DefaultRequestOptions.DisableContentMD5Validation = true;
                    var blob = new CloudBlockBlob(url, blobClient);

                    using (var stream = await blob.OpenReadAsync())
                    using (var zipDirectoryReader = new ZipDirectoryReader(stream))
                    {
                        var zipDirectory = await zipDirectoryReader.ReadAsync();
                    }
                });

            var name3 = "HttpZipProvider";
            var average3 = await ExecuteTestsAsync(
                name3,
                async (url) =>
                {
                    // Read the entries using HttpClient and MiniZip.
                    var httpZipProvider = new HttpZipProvider(_client);
                    using (var reader = await httpZipProvider.GetReaderAsync(url))
                    {
                        var zipDirectory = await reader.ReadAsync();
                    }
                });

            Assert.True(average2 < average1, $"'{name2}' should be less than '{name1}'.");
            Assert.True(average3 < average1, $"'{name3}' should be less than '{name1}'.");

            // This is what we want. Ideally the stream provided by HttpZipProvider performs better than CloudBlockBlob.
            Assert.True(average2 < average3, $"'{name2}' should be less than '{name3}'.");
        }

        private async Task<double> ExecuteTestsAsync(string name, Func<Uri, Task> runAsync)
        {
            const int iterationCount = 5 + 1;
            var results = new ConcurrentBag<Result>();

            for (var iteration = 0; iteration < iterationCount; iteration++)
            {
                await RunIteration(name, iteration, runAsync, iteration > 0 ? results : null);
            }

            using (var fileStream = new FileStream(_fileName, FileMode.Append))
            using (var streamReader = new StreamWriter(fileStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)))
            using (var csvWriter = new CsvWriter(streamReader))
            {
                if (!_wroteHeader)
                {
                    csvWriter.WriteHeader<Result>();
                    csvWriter.NextRecord();
                    _wroteHeader = true;
                }

                foreach (var result in results.OrderBy(x => x.Name).ThenBy(x => x.Iteration).ThenBy(x => x.Sequence))
                {
                    csvWriter.WriteRecord(result);
                    csvWriter.NextRecord();
                }
            }

            var average = results.Average(x => x.DurationMs);
            _output.WriteLine($"{name}: average = {average}");

            return average;
        }

        private async Task RunIteration(string name, int iteration, Func<Uri, Task> runAsync, ConcurrentBag<Result> results)
        {
            var paths = TestUtility.ValidTestDataPaths;
            for (var sequence = 0; sequence < paths.Count; sequence++)
            {
                var url = new Uri(new Uri(_server.BaseAddress, TestUtility.TestServerDirectory + "/"), paths[sequence]);

                var stopwatch = Stopwatch.StartNew();
                await runAsync(url);
                stopwatch.Stop();

                if (results != null)
                {
                    results.Add(new Result(
                        name,
                        iteration,
                        sequence,
                        paths[sequence],
                        stopwatch.Elapsed));
                }
            }
        }

        private class Result
        {
            public Result(string name, int iteration, int sequence, string path, TimeSpan duration)
            {
                Name = name;
                Iteration = iteration;
                Sequence = sequence;
                Path = path;
                DurationMs = duration.TotalMilliseconds;
            }

            public string Name { get; set; }
            public int Iteration { get; set; }
            public int Sequence { get; set; }
            public string Path { get; set; }
            public double DurationMs { get; set; }
        }
    }
}
