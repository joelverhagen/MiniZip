using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Knapcode.MiniZip
{
    public class BoundedStreamTest
    {
        public class ReadAsync : Facts
        {
            [Theory]
            [MemberData(nameof(AllowsReadingOfZipCentralDirectoryData))]
            public async Task AllowsReadingOfZipCentralDirectory(string path)
            {
                // Arrange
                var originalStream = TestUtility.BufferTestData(path);
                var result = await TestUtility.ReadWithMiniZipAsync(originalStream);

                var expected = result.Data;
                var innerStream = new MemoryStream();

                var prefix = new byte[2345];
                var suffix = new byte[6789];
                innerStream.Write(prefix, 0, prefix.Length);
                originalStream.Position = 0;
                await originalStream.CopyToAsync(innerStream);
                innerStream.Write(suffix, 0, suffix.Length);

                var target = new BoundedStream(innerStream, prefix.Length, (prefix.Length + originalStream.Length) - 1);
                var reader = new ZipDirectoryReader(target);
                var expectedSize = originalStream.Length;

                // Act
                var actual = await reader.ReadAsync();

                // Assert
                TestUtility.VerifyJsonEquals(expected, actual);
                Assert.Equal(expectedSize, target.Length);
                Assert.Equal(prefix.Length + target.Length + suffix.Length, innerStream.Length);
            }

            public static IEnumerable<object[]> AllowsReadingOfZipCentralDirectoryData => TestUtility
                .ValidTestDataPaths
                .Select(x => new object[] { x });
        }

        public class Read : Facts
        {
            [Fact]
            public void ObservesBounds()
            {
                // Arrange
                var destination = new byte[_bytes.Length * 2];
                var expectedRead = (_endPosition - _startPosition) + 1;

                // Arrange & Act
                var read = _target.Read(destination, 0, destination.Length);

                // Assert
                Verify(_startPosition, _endPosition, destination, destination.Length - expectedRead);
                Assert.Equal(expectedRead, read);
            }

            [Fact]
            public void AllowsIntermediatePosition()
            {
                // Arrange
                var destination = new byte[_bytes.Length];
                var expectedRead = (_endPosition - _startPosition) - 1;
                _target.Position = 2;

                // Arrange & Act
                var read = _target.Read(destination, 0, destination.Length);

                // Assert
                Verify(_startPosition + 2, _endPosition, destination, _bytes.Length - expectedRead);
                Assert.Equal(expectedRead, read);
            }

            [Fact]
            public void ReadsNothingWhenPositionIsPastEndBound()
            {
                // Arrange
                var destination = new byte[_bytes.Length];
                var expectedRead = 0;
                _target.Position = 8;

                // Arrange & Act
                var read = _target.Read(destination, 0, destination.Length);

                // Assert
                Verify(0, -1, destination, destination.Length);
                Assert.Equal(expectedRead, read);
            }
        }

        public class Length : Facts
        {
            [Fact]
            public void IsBasedOffBounds()
            {
                Assert.Equal((_endPosition - _startPosition) + 1, _target.Length);
            }
        }

        public class Position : Facts
        {
            [Fact]
            public void ResetsInnerPositionStreamThatIsLessThanLowerBound()
            {
                // Arrange
                _innerStream.Position = _startPosition - 1;

                // Act
                var position = _target.Position;

                // Assert
                Assert.Equal(0, position);
                Assert.Equal(_startPosition, _innerStream.Position);
            }

            [Fact]
            public void DoesNotInnerPositionStreamThatIsGreaterThanUpperBound()
            {
                // Arrange
                _innerStream.Position = _endPosition + 2;

                // Act
                var position = _target.Position;

                // Assert
                Assert.Equal(_target.Length + 1, position);
                Assert.Equal(_endPosition + 2, _innerStream.Position);
            }

            [Fact]
            public void LeavesInnerPositionStreamThatIsEqualToUpperBound()
            {
                // Arrange
                _innerStream.Position = _endPosition;

                // Act
                var position = _target.Position;

                // Assert
                Assert.Equal(_endPosition - _startPosition, position);
                Assert.Equal(_endPosition, _innerStream.Position);
            }

            [Fact]
            public void LeavesInnerPositionStreamThatIsEqualToLowerBound()
            {
                // Arrange
                _innerStream.Position = _startPosition;

                // Act
                var position = _target.Position;

                // Assert
                Assert.Equal(0, position);
                Assert.Equal(_startPosition, _innerStream.Position);
            }

            [Fact]
            public void LeavesPositionThatIsInsideBounds()
            {
                // Arrange
                _innerStream.Position = _startPosition + 1;

                // Act
                var position = _target.Position;

                // Assert
                Assert.Equal(1, position);
                Assert.Equal(_startPosition + 1, _innerStream.Position);
            }
        }

        public class Facts
        {
            protected byte[] _bytes;
            protected MemoryStream _innerStream;
            protected int _startPosition;
            protected int _endPosition;
            internal BoundedStream _target;

            public Facts()
            {
                _bytes = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                _innerStream = new MemoryStream(_bytes);
                _startPosition = 2;
                _endPosition = 7;
                _target = new BoundedStream(_innerStream, _startPosition, _endPosition);
            }

            protected void Verify(int startPosition, int endPosition, byte[] actual, int extraBytes)
            {
                var count = (endPosition - startPosition) + 1;
                var expected = new byte[count  + extraBytes];
                Buffer.BlockCopy(_bytes, startPosition, expected, 0, count);
                Assert.Equal(expected, actual);
            }
        }
    }
}
