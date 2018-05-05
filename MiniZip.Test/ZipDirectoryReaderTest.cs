using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Knapcode.MiniZip
{
    public class ZipDirectoryReaderTest
    {
        public class ReadAsync
        {
            [Fact]
            public async Task AllowsReadingTwice()
            {
                // Arrange
                using (var stream = TestUtility.BufferTestData(@"System.IO.Compression\refzipfiles\normal.zip"))
                {
                    var reader = new ZipDirectoryReader(stream);

                    // Act
                    var outputA = await reader.ReadAsync();
                    var outputB = await reader.ReadAsync();

                    // Assert
                    Assert.NotSame(outputA, outputB);
                    TestUtility.VerifyJsonEquals(outputA, outputB);
                }
            }

            [Fact]
            public async Task CanReadFromAChangedStream()
            {
                // Arrange
                using (var streamA = TestUtility.BufferTestData(@"System.IO.Compression\refzipfiles\normal.zip"))
                using (var streamB = TestUtility.BufferTestData(@"System.IO.Compression\refzipfiles\small.zip"))
                using (var sourceStream = new MemoryStream())
                {
                    var expected = (await TestUtility.ReadWithMiniZipAsync(streamB)).Data;

                    await streamA.CopyToAsync(sourceStream);
                    var reader = new ZipDirectoryReader(sourceStream);

                    await reader.ReadAsync();
                    sourceStream.SetLength(0);
                    streamB.Position = 0;
                    await streamB.CopyToAsync(sourceStream);

                    // Act
                    var actual = await reader.ReadAsync();

                    // Assert
                    Assert.NotSame(expected, actual);
                    TestUtility.VerifyJsonEquals(expected, actual);
                }
            }
            
            [Theory]
            [InlineData(@"System.IO.Compression\refzipfiles\fake64.zip", 770, 942)]
            [InlineData(@"System.IO.Compression\refzipfiles\normal.zip", 2670582, 2671162)]
            public async Task DoesNotReadBeforeCentralDirectory(string path, long minimum, long maximum)
            {
                // Arrange
                using (var originalStream = TestUtility.BufferTestData(path))
                using (var stream = new MinimumPositionStream(originalStream))
                {
                    var expected = await TestUtility.ReadWithMiniZipAsync(originalStream);

                    // Act
                    var actual = await TestUtility.ReadWithMiniZipAsync(stream);

                    // Assert
                    TestUtility.VerifyJsonEquals(expected, actual);
                    Assert.Equal(minimum, stream.MinimumPositionRead);
                    Assert.Equal(maximum, stream.MaximumPositionRead);
                }
            }

            private class MinimumPositionStream : Stream
            {
                private readonly Stream _innerStream;

                public MinimumPositionStream(Stream innerStream)
                {
                    _innerStream = innerStream;
                }

                public long? MinimumPosition { get; private set; }
                public long? MinimumPositionRead { get; private set; }
                public long? MaximumPosition { get; private set; }
                public long? MaximumPositionRead { get; private set; }

                public override long Length => _innerStream.Length;
                public override bool CanRead => _innerStream.CanRead;
                public override bool CanSeek => _innerStream.CanSeek;
                public override long Position
                {
                    get => _innerStream.Position;
                    set
                    {
                        _innerStream.Position = value;
                        UpdatePositions();
                    }
                }

                public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
                {
                    var initialPosition = Position;

                    var read = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);

                    MinimumPositionRead = Math.Min(MinimumPositionRead ?? long.MaxValue, initialPosition);
                    MaximumPositionRead = Math.Max(MaximumPositionRead ?? long.MinValue, initialPosition + read);

                    return read;
                }

                public override long Seek(long offset, SeekOrigin origin)
                {
                    var output = _innerStream.Seek(offset, origin);
                    UpdatePositions();
                    return output;
                }

                private void UpdatePositions()
                {
                    MinimumPosition = Math.Min(MinimumPosition ?? long.MaxValue, Position);
                    MaximumPosition = Math.Min(MaximumPosition ?? long.MinValue, Position);
                }

                public override bool CanWrite => throw new NotSupportedException();
                public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => throw new NotSupportedException();
                public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => throw new NotSupportedException();
                public override int EndRead(IAsyncResult asyncResult) => throw new NotSupportedException();
                public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
                public override int ReadByte() => throw new NotSupportedException();
                public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => throw new NotSupportedException();
                public override Task FlushAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
                public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new NotSupportedException();
                public override void CopyTo(Stream destination, int bufferSize) => throw new NotSupportedException();
                public override void EndWrite(IAsyncResult asyncResult) => throw new NotSupportedException();
                public override void Flush() => throw new NotSupportedException();
                public override void SetLength(long value) => throw new NotSupportedException();
                public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
                public override void WriteByte(byte value) => throw new NotSupportedException();
            }
        }
    }
}
