using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Knapcode.MiniZip
{
    public class ConsistencyTest
    {
        [Theory]
        [MemberData(nameof(TestDataPaths))]
        public async Task WithSelfUsingHttpRangeReader(string path)
        {
            // Arrange
            using (var memoryStream = TestUtility.BufferTestData(path))
            using (var server = TestUtility.GetTestServer(TestUtility.TestDataDirectory))
            using (var client = server.CreateClient())
            {
                var httpZipProvider = new HttpZipProvider(client)
                {
                    ETagBehavior = ETagBehavior.Required,
                    FirstBufferSize = 1,
                    SecondBufferSize = 1,
                    BufferGrowthExponent = 2,
                };

                var requestUri = new Uri(new Uri(server.BaseAddress, TestUtility.TestServerDirectory + "/"), path);
                using (var bufferedRangeStream = await httpZipProvider.GetStreamAsync(requestUri))
                {
                    // Act
                    var a = await TestUtility.ReadWithMiniZipAsync(memoryStream);
                    var b = await TestUtility.ReadWithMiniZipAsync(bufferedRangeStream);

                    // Assert
                    TestUtility.VerifyJsonEquals(a.Data, b.Data);
                    Assert.Equal(a.Success, b.Success);
                    Assert.Equal(a.Exception?.Message, b.Exception?.Message);
                    Assert.Equal(a.Exception?.GetType(), b.Exception?.GetType());
                }
            }
        }

        [Theory]
        [MemberData(nameof(TestDataPaths))]
        public async Task WithSelfUsingFileRangeReader(string path)
        {
            // Arrange
            using (var memoryStream = TestUtility.BufferTestData(path))
            {
                var fullPath = Path.Combine(TestUtility.TestDataDirectory, path);
                var length = new FileInfo(fullPath).Length;
                var fileRangeReader = new FileRangeReader(fullPath);
                var bufferSizeProvider = new ZipBufferSizeProvider(firstBufferSize: 1, secondBufferSize: 1, exponent: 2);
                using (var bufferedRangeStream = new BufferedRangeStream(fileRangeReader, length, bufferSizeProvider))
                {
                    // Act
                    var a = await TestUtility.ReadWithMiniZipAsync(memoryStream);
                    var b = await TestUtility.ReadWithMiniZipAsync(bufferedRangeStream);

                    // Assert
                    TestUtility.VerifyJsonEquals(a.Data, b.Data);
                    Assert.Equal(a.Success, b.Success);
                    Assert.Equal(a.Exception?.Message, b.Exception?.Message);
                    Assert.Equal(a.Exception?.GetType(), b.Exception?.GetType());
                }
            }
        }

        [Theory]
        [MemberData(nameof(TestDataPaths))]
        public async Task WithBcl(string path)
        {
            // Arrange
            using (var stream = TestUtility.BufferTestData(path))
            {
                // Act
                var miniZip = await TestUtility.ReadWithMiniZipAsync(stream);
                var bcl = TestUtility.ReadWithBcl(stream);

                // Assert
                if (CasesHandledByBcl.TryGetValue(path, out var knownException))
                {
                    Assert.Equal(knownException.Type, miniZip.Exception.GetType());
                    Assert.Equal(knownException.Message, miniZip.Exception.Message);
                    Assert.True(bcl.Success);
                }
                else if (CasesNotHandledByBcl.TryGetValue(path, out knownException))
                {
                    Assert.Equal(knownException.Type, bcl.Exception.GetType());
                    Assert.Equal(knownException.Message, bcl.Exception.Message);
                    Assert.True(miniZip.Success);
                }
                else
                {
                    Assert.Equal(bcl.Success, miniZip.Success);
                    if (miniZip.Success)
                    {
                        Assert.Equal(bcl.Data.Count, miniZip.Data.Entries.Count);

                        var nameToBcl = bcl
                            .Data
                            .OrderBy(x => x.FullName)
                            .ThenBy(x => x.CompressedLength)
                            .ThenBy(x => x.Length)
                            .ThenBy(x => x.ExternalAttributes)
                            .ThenBy(x => x.LastWriteTime)
                            .ToLookup(x => x.FullName);

                        var nameToMiniZip = miniZip
                            .Data
                            .Entries
                            .OrderBy(x => x.GetName())
                            .ThenBy(x => x.CompressedSize)
                            .ThenBy(x => x.UncompressedSize)
                            .ThenBy(x => x.ExternalAttributes)
                            .ThenBy(x => x.GetLastModified())
                            .ToLookup(x => x.GetName());

                        foreach (var name in nameToMiniZip.Select(x => x.Key))
                        {
                            var bclEntries = nameToBcl[name].ToList();
                            var miniZipEntries = nameToMiniZip[name].ToList();

                            Assert.Equal(bclEntries.Count, miniZipEntries.Count);
                            for (var i = 0; i < bclEntries.Count; i++)
                            {
                                Assert.Equal((ulong)bclEntries[i].CompressedLength, miniZipEntries[i].GetCompressedSize());
                                Assert.Equal((ulong)bclEntries[i].Length, miniZipEntries[i].GetUncompressedSize());
                                Assert.Equal((uint)bclEntries[i].ExternalAttributes, miniZipEntries[i].ExternalAttributes);
                                Assert.Equal(bclEntries[i].LastWriteTime.DateTime, miniZipEntries[i].GetLastModified());
                            }
                        }
                    }
                }
            }
        }

        private static IReadOnlyDictionary<string, KnownException> CasesHandledByBcl = new Dictionary<string, KnownException>
        {
            {
                @"SharpZipLib\ZipFileHandling.FindEntriesInArchiveExtraData\0.zip",
                KnownException.Create<MiniZipException>("Cannot find central directory.")
            },
            {
                @"Custom\Spanning.zip",
                KnownException.Create<MiniZipException>("Archives spanning multiple disks are not supported.")
            },
        };

        private static IReadOnlyDictionary<string, KnownException> CasesNotHandledByBcl = new Dictionary<string, KnownException>
        {
        };

        [Theory]
        [MemberData(nameof(TestDataPaths))]
        public async Task WithSharpZipLib(string path)
        {
            // Arrange
            using (var stream = TestUtility.BufferTestData(path))
            {
                // Act
                var miniZip = await TestUtility.ReadWithMiniZipAsync(stream);
                var sharpZipLib = TestUtility.ReadWithSharpZipLib(stream);

                // Assert
                if (CasesHandledBySharpZipLib.TryGetValue(path, out var knownException))
                {
                    Assert.Equal(knownException.Type, miniZip.Exception.GetType());
                    Assert.Equal(knownException.Message, miniZip.Exception.Message);
                    Assert.True(sharpZipLib.Success);
                }
                else if (CasesNotHandledBySharpZipLib.TryGetValue(path, out knownException))
                {
                    Assert.Equal(knownException.Type, sharpZipLib.Exception.GetType());
                    Assert.Equal(knownException.Message, sharpZipLib.Exception.Message);
                    Assert.True(miniZip.Success);
                }
                else
                {
                    Assert.Equal(sharpZipLib.Success, miniZip.Success);
                    if (miniZip.Success)
                    {
                        Assert.Equal(sharpZipLib.Data.Count, miniZip.Data.Entries.Count);

                        if (DifferencesFromSharpZipLib.TryGetValue(path, out var mutate))
                        {
                            mutate(miniZip.Data);
                        }

                        var nameToSharpZipLib = sharpZipLib
                            .Data
                            .OrderBy(x => x.Name)
                            .ThenBy(x => x.CompressedSize)
                            .ThenBy(x => x.Size)
                            .ThenBy(x => x.ExternalFileAttributes)
                            .ThenBy(x => x.DateTime)
                            .ToLookup(x => x.Name);

                        var nameToMiniZip = miniZip
                            .Data
                            .Entries
                            .OrderBy(x => x.GetName())
                            .ThenBy(x => x.CompressedSize)
                            .ThenBy(x => x.UncompressedSize)
                            .ThenBy(x => x.ExternalAttributes)
                            .ThenBy(x => x.GetLastModified())
                            .ToLookup(x => x.GetName());

                        foreach (var name in nameToMiniZip.Select(x => x.Key))
                        {
                            var sharpZipLibEntries = nameToSharpZipLib[name].ToList();
                            var miniZipEntries = nameToMiniZip[name].ToList();

                            Assert.Equal(sharpZipLibEntries.Count, miniZipEntries.Count);
                            for (var i = 0; i < sharpZipLibEntries.Count; i++)
                            {
                                Assert.Equal((ulong)sharpZipLibEntries[i].CompressedSize, miniZipEntries[i].GetCompressedSize());
                                Assert.Equal((ulong)sharpZipLibEntries[i].Size, miniZipEntries[i].GetUncompressedSize());
                                Assert.Equal((uint)sharpZipLibEntries[i].ExternalFileAttributes, miniZipEntries[i].ExternalAttributes);
                                Assert.Equal(sharpZipLibEntries[i].DateTime, miniZipEntries[i].GetLastModified());
                            }
                        }
                    }
                }
            }
        }

        private static IReadOnlyDictionary<string, Action<ZipDirectory>> DifferencesFromSharpZipLib = new Dictionary<string, Action<ZipDirectory>>
        {
            {
                @"System.IO.Compression\badzipfiles\invaliddate.zip",
                zipDirectory =>
                {
                    // SharpZipLib gives back 2064-02-29T23:10:42 instead of 2064-02-30T27:10:42.
                    var entry = zipDirectory.Entries.Single();

                    entry.LastModifiedDate &= 0b1111_1111_1110_0000;
                    entry.LastModifiedDate |= 29;

                    entry.LastModifiedTime &= 0b0000_0111_1111_1111;
                    entry.LastModifiedTime |= 23 << 11;
                }
            },
            {
                @"System.IO.Compression\compat\backslashes_FromUnix.zip",
                ConvertToForwardSlashes
            },
            {
                @"System.IO.Compression\compat\backslashes_FromWindows.zip",
                ConvertToForwardSlashes
            },
            {
                @"System.IO.Compression\StrangeZipFiles\dataDescriptor.zip",
                ConvertToForwardSlashes
            },
        };

        private static void ConvertToForwardSlashes(ZipDirectory zipDirectory)
        {
            foreach (var zipEntry in zipDirectory.Entries)
            {
                var fixedName = zipEntry.GetName().Replace(@"\", "/");
                zipEntry.Name = Encoding.ASCII.GetBytes(fixedName);
            }
        }

        private static IReadOnlyDictionary<string, KnownException> CasesHandledBySharpZipLib = new Dictionary<string, KnownException>
        {
            {
                @"SharpZipLib\ZipFileHandling.EmbeddedArchive\1.zip",
                KnownException.Create<MiniZipException>(Strings.InvalidCentralDirectorySignature)
            },
            {
                @"SharpZipLib\ZipFileHandling.Zip64Useage\1.zip",
                KnownException.Create<MiniZipException>(Strings.InvalidCentralDirectorySignature)
            },
            {
                @"Custom\Spanning.zip",
                KnownException.Create<MiniZipException>("Archives spanning multiple disks are not supported.")
            },
        };

        private static IReadOnlyDictionary<string, KnownException> CasesNotHandledBySharpZipLib = new Dictionary<string, KnownException>
        {
            {
                @"System.IO.Compression\compat\NullCharFileName_FromUnix.zip",
                KnownException.Create<ArgumentException>("Illegal characters in path.\r\nParameter name: path")
            },
            {
                @"System.IO.Compression\compat\NullCharFileName_FromWindows.zip",
                KnownException.Create<ArgumentException>("Illegal characters in path.\r\nParameter name: path")
            },
        };

        public static IEnumerable<object[]> TestDataPaths => TestUtility
            .TestDataPaths
            .Select(x => new object[] { x });
    }
}
