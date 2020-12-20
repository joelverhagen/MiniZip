using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Knapcode.MiniZip
{
    public class ZipDirectoryReaderTest
    {
        public class ReadLocalFileHeaderAsync
        {
            [Fact]
            public async Task RejectsEncryptedFiles()
            {
                // Arrange
                using (var stream = TestUtility.BufferTestData(@"SharpZipLib\FastZipHandling.Encryption\0.zip"))
                {
                    var reader = new ZipDirectoryReader(stream);
                    var directory = await reader.ReadAsync();
                    var entry = directory.Entries[0];

                    // Act & Assert
                    var ex = await Assert.ThrowsAsync<MiniZipException>(
                        () => reader.ReadLocalFileHeaderAsync(directory, entry));
                    Assert.Equal("Archives containing encrypted files are not supported.", ex.Message);
                }
            }

            [Theory]
            [InlineData(@"Custom\crc32collide_with_descriptor_and_signature.zip")]
            [InlineData(@"System.IO.Compression\StrangeZipFiles\dataDescriptor.zip")]
            public async Task ReadsDataDescriptorWithSignature(string resourceName)
            {
                // Arrange
                using (var stream = TestUtility.BufferTestData(resourceName))
                {
                    var reader = new ZipDirectoryReader(stream);
                    var directory = await reader.ReadAsync();
                    var entry = directory.Entries[0];

                    // Act
                    var localFileHeader = await reader.ReadLocalFileHeaderAsync(directory, entry);

                    // Assert
                    Assert.NotNull(localFileHeader.DataDescriptor);
                    Assert.True(localFileHeader.DataDescriptor.HasSignature);
                    Assert.Equal(entry.Crc32, localFileHeader.DataDescriptor.Crc32);
                    Assert.Equal(entry.CompressedSize, localFileHeader.DataDescriptor.CompressedSize);
                    Assert.Equal(entry.UncompressedSize, localFileHeader.DataDescriptor.UncompressedSize);
                }
            }

            [Theory]
            [InlineData(@"Custom\crc32collide_with_descriptor_no_signature.zip")]
            [InlineData(@"Custom\crc32collide_with_descriptor_no_signature_gap.zip")]
            public async Task ReadsDataDescriptorWithoutSignature(string resourceName)
            {
                // Arrange
                using (var stream = TestUtility.BufferTestData(resourceName))
                {
                    var reader = new ZipDirectoryReader(stream);
                    var directory = await reader.ReadAsync();
                    var entry = directory.Entries[0];

                    // Act
                    var localFileHeader = await reader.ReadLocalFileHeaderAsync(directory, entry);

                    // Assert
                    Assert.NotNull(localFileHeader.DataDescriptor);
                    Assert.False(localFileHeader.DataDescriptor.HasSignature);
                    Assert.Equal(entry.Crc32, localFileHeader.DataDescriptor.Crc32);
                    Assert.Equal(entry.CompressedSize, localFileHeader.DataDescriptor.CompressedSize);
                    Assert.Equal(entry.UncompressedSize, localFileHeader.DataDescriptor.UncompressedSize);
                }
            }

            [Theory]
            [MemberData(nameof(ReadsLocalHeaderOfAllFilesData))]
            public async Task ReadsLocalHeaderOfAllFiles(string resourceName)
            {
                // Arrange
                using (var stream = TestUtility.BufferTestData(resourceName))
                {
                    var reader = new ZipDirectoryReader(stream);
                    var directory = await reader.ReadAsync();

                    // Act
                    foreach (var entry in directory.Entries)
                    {
                        var localFileHeader = await reader.ReadLocalFileHeaderAsync(directory, entry);

                        // Assert
                        Assert.NotNull(localFileHeader);
                    }
                }
            }

            public static IEnumerable<object[]> ReadsLocalHeaderOfAllFilesData => TestUtility
                .ValidTestDataPaths
                .Except(TestUtility.InvalidLocalFileHeaders)
                .Select(x => new[] { x });

            [Fact]
            public async Task RejectsInvalidLocalFileHeaderSignature()
            {
                // Assert
                using (var stream = TestUtility.BufferTestData(@"System.IO.Compression\badzipfiles\localFileHeaderSignatureWrong.zip"))
                {
                    var reader = new ZipDirectoryReader(stream);
                    var directory = await reader.ReadAsync();
                    var entry = directory.Entries[0];

                    // Act & Assert
                    var ex = await Assert.ThrowsAsync<MiniZipException>(
                        () => reader.ReadLocalFileHeaderAsync(directory, entry));
                    Assert.Equal("Invalid local file header signature found.", ex.Message);
                }
            }

            [Fact]
            public async Task RejectsOffsetOutOfBounds()
            {
                // Assert
                using (var stream = TestUtility.BufferTestData(@"System.IO.Compression\badzipfiles\localFileOffsetOutOfBounds.zip"))
                {
                    var reader = new ZipDirectoryReader(stream);
                    var directory = await reader.ReadAsync();
                    var entry = directory.Entries[0];

                    // Act & Assert
                    await Assert.ThrowsAsync<EndOfStreamException>(
                        () => reader.ReadLocalFileHeaderAsync(directory, entry));
                }
            }
        }

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
                public override void EndWrite(IAsyncResult asyncResult) => throw new NotSupportedException();
                public override void Flush() => throw new NotSupportedException();
                public override void SetLength(long value) => throw new NotSupportedException();
                public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
                public override void WriteByte(byte value) => throw new NotSupportedException();
            }
        }
    }
}
