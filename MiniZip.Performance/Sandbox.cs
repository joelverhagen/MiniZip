using System;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Xunit;

namespace Knapcode.MiniZip
{
    public class Sandbox
    {
        [Fact]
        public async Task Test1()
        {
            var nupkgUrl = await NuGetUtility.GetNupkgUrlAsync("Newtonsoft.Json", "9.0.1");

            // Read the entries using Azure Blob Storage SDK and ZipArchive.
            var blobClient = new CloudBlobClient(new Uri("https://example"), new FixUpBlobStorageHandler());
            blobClient.DefaultRequestOptions.DisableContentMD5Validation = true;
            var blob = new CloudBlockBlob(nupkgUrl, blobClient);

            using (var stream = await blob.OpenReadAsync())
            using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                var entries = zipArchive.Entries.ToList();
            }

            // Read the entries using Azure Blob Storage SDK and MiniZip.
            using (var stream = await blob.OpenReadAsync())
            using (var zipDirectoryReader = new ZipDirectoryReader(stream))
            {
                var zipDirectory = await zipDirectoryReader.ReadAsync();
            }

            // Read the entries using HttpClient and MiniZip.
            using (var httpClient = new HttpClient())
            {
                var httpZipProvider = new HttpZipProvider(httpClient);
                using (var reader = await httpZipProvider.GetReaderAsync(nupkgUrl))
                {
                    var zipDirectory = await reader.ReadAsync();
                }
            }
        }
    }
}
