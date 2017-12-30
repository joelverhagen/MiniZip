using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Knapcode.MiniZip.MZip
{
    public class MZipFormatTest
    {
        [Theory]
        [MemberData(nameof(CanRoundTripZipFileData))]
        public async Task CanRoundTripZipFile(string path)
        {
            // Arrange
            var srcStream = TestUtility.BufferTestData(path);
            var expected = (await TestUtility.ReadWithMiniZipAsync(srcStream)).Data;
            var target = new MZipFormat();
            var dstStream = new MemoryStream();

            // Act
            await target.WriteAsync(srcStream, dstStream);
            dstStream.Position = 0;
            var mzipStream = await target.ReadAsync(dstStream);
            var actual = (await TestUtility.ReadWithMiniZipAsync(mzipStream)).Data;

            // Assert
            TestUtility.VerifyJsonEquals(expected, actual);
        }

        public static IEnumerable<object[]> CanRoundTripZipFileData => TestUtility
            .ValidTestDataPaths
            .Select(x => new[] { x });
    }
}
