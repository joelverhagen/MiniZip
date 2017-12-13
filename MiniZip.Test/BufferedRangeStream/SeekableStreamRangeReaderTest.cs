using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Knapcode.MiniZip
{
    public class SeekableStreamRangeReaderTest
    {
        public class ReadAsync
        {
            [Fact]
            public async Task RejectsUnseekableStream()
            {
                // Arrange
                Func<Task<Stream>> openStreamAsync =
                    () => Task.FromResult<Stream>(new TestStream(canRead: true, canSeek: false));
                var target = new SeekableStreamRangeReader(openStreamAsync);

                // Act & Assert
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    target.ReadAsync(0, new byte[0], 0, 0));
                Assert.Equal(Strings.StreamMustSupportSeek, exception.Message);
            }

            [Fact]
            public async Task RejectsUnreadableStream()
            {
                // Arrange
                Func<Task<Stream>> openStreamAsync =
                    () => Task.FromResult<Stream>(new TestStream(canRead: false, canSeek: true));
                var target = new SeekableStreamRangeReader(openStreamAsync);

                // Act & Assert
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    target.ReadAsync(0, new byte[0], 0, 0));
                Assert.Equal(Strings.StreamMustSupportRead, exception.Message);
            }
        }

        private class TestStream : Stream
        {
            public TestStream(bool canRead, bool canSeek)
            {
                CanRead = canRead;
                CanSeek = canSeek;
            }

            public override bool CanRead { get; }

            public override bool CanSeek { get; }

            public override bool CanWrite => throw new NotImplementedException();

            public override long Length => throw new NotImplementedException();

            public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override void Flush()
            {
                throw new NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }
        }
    }
}
