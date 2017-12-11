using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Knapcode.MiniZip
{
    public class ConsistencyTest
    {
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
                        Assert.Equal(bcl.Data.Sum(x => x.CompressedLength), miniZip.Data.Entries.Sum(x => (long) x.GetCompressedSize()));
                        Assert.Equal(bcl.Data.Sum(x => x.Length), miniZip.Data.Entries.Sum(x => (long) x.GetUncompressedSize()));
                    }
                }
            }
        }

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
                        Assert.Equal(sharpZipLib.Data.Sum(x => x.CompressedSize), miniZip.Data.Entries.Sum(x => (long)x.GetCompressedSize()));
                        Assert.Equal(sharpZipLib.Data.Sum(x => x.Size), miniZip.Data.Entries.Sum(x => (long)x.GetUncompressedSize()));
                    }
                }
            }
        }

        private static IReadOnlyDictionary<string, KnownException> CasesHandledByBcl = new Dictionary<string, KnownException>
        {
            {
                @"SharpZipLib\ZipFileHandling.FindEntriesInArchiveExtraData\0.zip",
                KnownException.Create<ZipException>("Cannot find central directory.")
            }
        };

        private static IReadOnlyDictionary<string, KnownException> CasesNotHandledByBcl = new Dictionary<string, KnownException>
        {
        };

        private static IReadOnlyDictionary<string, KnownException> CasesHandledBySharpZipLib = new Dictionary<string, KnownException>
        {
            {
                @"SharpZipLib\ZipFileHandling.EmbeddedArchive\1.zip",
                KnownException.Create<ZipException>("Wrong central directory signature.")
            },
            {
                @"SharpZipLib\ZipFileHandling.Zip64Useage\1.zip",
                KnownException.Create<ZipException>("Wrong central directory signature.")
            }
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
            }
        };

        public static IEnumerable<object[]> TestDataPaths => TestUtility
            .TestDataPaths
            .Select(x => new object[] { x });
    }
}
