using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using Xunit;

namespace Knapcode.MiniZip
{
    public static class TestUtility
    {
        public static readonly string TestDataDirectory = Path.GetFullPath("TestData");
        public const string TestServerDirectory = "TestData";

        public static IReadOnlyList<string> TestDataPaths => Directory
            .EnumerateFiles(TestDataDirectory, "*", SearchOption.AllDirectories)
            .Select(x => GetRelativePath(x, TestDataDirectory))
            .ToList();

        public static IReadOnlyList<string> InvalidTestDataPaths => new[]
        {
            @"SharpZipLib\ZipFileHandling.EmbeddedArchive\1.zip",
            @"SharpZipLib\ZipFileHandling.FindEntriesInArchiveExtraData\0.zip",
            @"SharpZipLib\ZipFileHandling.Zip64Useage\1.zip",
            @"System.IO.Compression\badzipfiles\CDoffsetInBoundsWrong.zip",
            @"System.IO.Compression\badzipfiles\CDoffsetOutOfBounds.zip",
            @"System.IO.Compression\badzipfiles\EOCDmissing.zip",
            @"System.IO.Compression\badzipfiles\numberOfEntriesDifferent.zip",
            @"Custom\Spanning.z01",
            @"Custom\Spanning.z02",
            @"Custom\Spanning.zip",
        };

        public static IReadOnlyList<string> InvalidLocalFileHeaders => new[]
        {
            // mzip
            @"Custom\only-metadata-zip64.zip",
            @"Custom\only-metadata.zip",

            // encrypted
            @"SharpZipLib\FastZipHandling.Encryption\0.zip",
            @"SharpZipLib\FastZipHandling.NonAsciiPasswords\0.zip",
            @"SharpZipLib\GeneralHandling.StoredNonSeekableKnownSizeNoCrcEncrypted\0.zip",
            @"SharpZipLib\StreamHandling.StoredNonSeekableKnownSizeNoCrcEncrypted\0.zip",
            @"SharpZipLib\ZipFileHandling.AddEncryptedEntriesToExistingArchive\0.zip",
            @"SharpZipLib\ZipFileHandling.AddEncryptedEntriesToExistingArchive\1.zip",
            @"SharpZipLib\ZipFileHandling.BasicEncryption\0.zip",
            @"SharpZipLib\ZipFileHandling.BasicEncryptionToDisk\0.zip",
            @"SharpZipLib\ZipFileHandling.Crypto_AddEncryptedEntryToExistingArchiveDirect\1.zip",
            @"SharpZipLib\ZipFileHandling.Crypto_AddEncryptedEntryToExistingArchiveSafe\0.zip",
            @"SharpZipLib\ZipFileHandling.TestEncryptedDirectoryEntry\0.zip",
            @"SharpZipLib\ZipFileHandling.TestEncryptedDirectoryEntry\1.zip",

            // malformed
            @"System.IO.Compression\badzipfiles\localFileHeaderSignatureWrong.zip",
            @"System.IO.Compression\badzipfiles\localFileOffsetOutOfBounds.zip",

            // slow
            @"SharpZipLib\ZipFileHandling.Zip64Entries\0.zip",
        };

        public static IReadOnlyList<string> ValidTestDataPaths => TestDataPaths
            .Except(InvalidTestDataPaths)
            .ToList();

        public static void VerifyJsonEquals<T>(T expected, T actual)
        {
            var expectedJson = JsonConvert.SerializeObject(expected, Formatting.Indented);
            var actualJson = JsonConvert.SerializeObject(actual, Formatting.Indented);
            Assert.Equal(expectedJson, actualJson);
        }

        public static MemoryStream BufferTestData(string path)
        {
            var fullPath = Path.Combine(TestDataDirectory, path);
            return new MemoryStream(File.ReadAllBytes(fullPath));
        }

        public static TestServer GetTestServer(
            string directory,
            bool etags = true,
            Func<HttpContext, Func<Task>, Task> middleware = null)
        {
            return new TestServer(new WebHostBuilder().Configure(app =>
            {
                if (middleware != null)
                {
                    app.Use(middleware);
                }

                app.Use(async (context, next) =>
                {
                    await next.Invoke();

                    if (!etags)
                    {
                        context.Response.Headers.Remove("ETag");
                    }
                });

                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(directory),
                    RequestPath = new PathString("/" + TestServerDirectory),
                    ServeUnknownFileTypes = true,
                });
            }));
        }

        public static async Task<MiniZipResult> ReadWithMiniZipAsync(Stream stream)
        {
            stream.Position = 0;

            try
            {
                using (var reader = new ZipDirectoryReader(stream, leaveOpen: true))
                {
                    var data = await reader.ReadAsync();

                    return new MiniZipResult
                    {
                        Success = true,
                        Data = data,
                    };
                }
            }
            catch (Exception e)
            {
                return new MiniZipResult
                {
                    Success = false,
                    Exception = e,
                };
            }
        }

        public static BclZipResult ReadWithBcl(Stream stream)
        {
            stream.Position = 0;

            try
            {
                using (var archive = new System.IO.Compression.ZipArchive(
                    stream,
                    System.IO.Compression.ZipArchiveMode.Read,
                    leaveOpen: true))
                {
                    var data = archive
                        .Entries
                        .Select(x => new BclZipEntry(x))
                        .ToList();

                    return new BclZipResult
                    {
                        Success = true,
                        Data = data,
                    };
                }
            }
            catch (Exception e)
            {
                return new BclZipResult
                {
                    Success = false,
                    Exception = e,
                };
            }
        }

        public static SharpZipLibResult ReadWithSharpZipLib(MemoryStream stream)
        {
            stream.Position = 0;

            try
            {
                using (var file = new ICSharpCode.SharpZipLib.Zip.ZipFile(stream))
                {
                    file.IsStreamOwner = false;
                    var data = file
                        .Cast<ICSharpCode.SharpZipLib.Zip.ZipEntry>()
                        .Select(x => new SharpZipLibEntry(x))
                        .ToList();

                    return new SharpZipLibResult
                    {
                        Success = true,
                        Data = data,
                    };
                }
            }
            catch (Exception e)
            {
                return new SharpZipLibResult
                {
                    Success = false,
                    Exception = e,
                };
            }
        }

        private static string GetRelativePath(string filePath, string directoryPath)
        {
            var pathUri = new Uri(filePath);

            if (!directoryPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                directoryPath += Path.DirectorySeparatorChar;
            }

            var directoryUri = new Uri(directoryPath);

            return Uri.UnescapeDataString(directoryUri
                .MakeRelativeUri(pathUri)
                .ToString()
                .Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
