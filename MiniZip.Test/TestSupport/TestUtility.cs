using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    public static class TestUtility
    {
        private static readonly string TestDataDirectory = Path.GetFullPath("TestData");
        
        public static IReadOnlyList<string> TestDataPaths => Directory
            .EnumerateFiles(TestDataDirectory, "*", SearchOption.AllDirectories)
            .Select(x => GetRelativePath(x, TestDataDirectory))
            .ToList();

        public static MemoryStream BufferTestData(string path)
        {
            var fullPath = Path.Combine(TestDataDirectory, path);
            return new MemoryStream(File.ReadAllBytes(fullPath));
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
