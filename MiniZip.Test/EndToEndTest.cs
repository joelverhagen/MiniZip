﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.Pkcs;
using System.Threading.Tasks;
using Xunit;

namespace Knapcode.MiniZip
{
    public class EndToEndTest
    {
        [Theory]
        [MemberData(nameof(Packages))]
        public async Task CanGatherAndRecreateNuGetPackageCentralDirectory(string id, string version)
        {
            using (var testDirectory = TestDirectory.Create())
            {
                var packageUri = await NuGetUtility.GetNupkgUrlAsync(id, version);

                ZipDirectory zipDirectoryA;
                string mzipPath;
                using (var httpClient = new HttpClient())
                {
                    var httpZipProvider = new HttpZipProvider(httpClient)
                    {
                        RequireAcceptRanges = false,
                    };
                    using (var reader = await httpZipProvider.GetReaderAsync(packageUri))
                    {
                        // Read the ZIP directory from the .nupkg URL.
                        zipDirectoryA = await reader.ReadAsync();

                        // Save the .mzip to the test directory.
                        mzipPath = Path.Combine(testDirectory, $"{id}.{version}.mzip");
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

        [Theory]
        [MemberData(nameof(Packages))]
        public async Task CanReadSignatureFile(string id, string version)
        {
            using (var testDirectory = TestDirectory.Create())
            {
                var packageUri = await NuGetUtility.GetNupkgUrlAsync(id, version);

                using (var httpClient = new HttpClient())
                {
                    var httpZipProvider = new HttpZipProvider(httpClient)
                    {
                        RequireAcceptRanges = false,
                    };
                    using (var reader = await httpZipProvider.GetReaderAsync(packageUri))
                    {
                        // Read the ZIP directory from the .nupkg URL.
                        var zipDirectory = await reader.ReadAsync();

                        // Find the signature entry
                        var entry = zipDirectory.Entries.Single(x => x.GetName() == ".signature.p7s");

                        // Read the signature file
                        var signatureBytes = await reader.ReadUncompressedFileDataAsync(zipDirectory, entry);

                        // Decode the signature file
                        var cms = new SignedCms();
                        cms.Decode(signatureBytes);
                        Assert.NotEmpty(cms.Certificates);
                    }
                }
            }
        }

        public static IEnumerable<object[]> Packages
        {
            get
            {
                yield return new object[] { "Newtonsoft.Json", "9.0.1" };
                yield return new object[] { "Microsoft.AspNet.Mvc", "5.2.7" };
                yield return new object[] { "Knapcode.MiniZip", "0.1.0" };
                yield return new object[] { "Microsoft.Extensions.Logging", "5.0.0" };
            }
        }
    }
}
