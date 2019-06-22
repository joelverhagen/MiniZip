using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Xunit;

namespace Knapcode.MiniZip
{
    public class EndToEndTest
    {
        [Theory]
        [InlineData("Newtonsoft.Json", "9.0.1")]
        [InlineData("Microsoft.AspNet.Mvc", "5.2.7")]
        [InlineData("Knapcode.MiniZip", "0.1.0")]
        public async Task CanGatherAndRecreateNuGetPackageCentralDirectory(string id, string version)
        {
            using (var testDirectory = TestDirectory.Create())
            {
                // Discover the .nupkg URL.
                var sourceRepository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
                var serviceIndex = await sourceRepository.GetResourceAsync<ServiceIndexResourceV3>();
                var packageBaseAddress = serviceIndex.GetServiceEntryUri(ServiceTypes.PackageBaseAddress);

                var lowerId = id.ToLowerInvariant();
                var lowerVersion = NuGetVersion.Parse(version).ToNormalizedString().ToLowerInvariant();
                var packageUri = new Uri(packageBaseAddress, $"{lowerId}/{lowerVersion}/{lowerId}.{lowerVersion}.nupkg");

                ZipDirectory zipDirectoryA;
                string mzipPath;
                using (var httpClient = new HttpClient())
                {
                    var httpZipProvider = new HttpZipProvider(httpClient);
                    using (var reader = await httpZipProvider.GetReaderAsync(packageUri))
                    {
                        // Read the ZIP directory from the .nupkg URL.
                        zipDirectoryA = await reader.ReadAsync();

                        // Save the .mzip to the test directory.
                        mzipPath = Path.Combine(testDirectory, $"{lowerId}.{lowerVersion}.mzip");
                        using (var fileStream = new FileStream(mzipPath, FileMode.Create))
                        {
                            var mzipFormat = new MZipFormat();
                            await mzipFormat.WriteAsync(reader.Stream, fileStream);
                        }
                    }
                }

                // Read the .mzip back from disk.
                ZipDirectory zipDirectoryB;
                using (var fileStream = new FileStream(mzipPath, FileMode.Open))
                {
                    var mzipFormat = new MZipFormat();
                    using (var mzipStream = await mzipFormat.ReadAsync(fileStream))
                    using (var reader = new ZipDirectoryReader(mzipStream))
                    {
                        zipDirectoryB = await reader.ReadAsync();
                    }
                }

                // Compare the results.
                TestUtility.VerifyJsonEquals(zipDirectoryA, zipDirectoryB);
            }
        }
    }
}
