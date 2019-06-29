using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// A base implementation of a read-only stream that only supports asynchronous reading. This stream supports
    /// reading and seeking but not writing.
    /// </summary>
    public abstract class ReadAsyncStream : Stream
    {
        /// <summary>
        /// The current position of the stream. This is the backing field of <see cref="Position"/>.
        /// </summary>
        protected long _position;

        /// <summary>
        /// Initializes and instance of a read-only asynchronous stream.
        /// </summary>
        /// <param name="length">The length of the stream.</param>
        protected ReadAsyncStream(long length)
        {
            Length = length;
            _position = 0;
        }

        /// <summary>
        /// Whether or not this stream supports reading. It does.
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// Whether or not this stream supports seeking. It does.
        /// </summary>
        public override bool CanSeek => true;

        /// <summary>
        /// Whether or not this stream supports write. It does not.
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        /// The total length of this stream.
        /// </summary>
        public override long Length { get; }

        /// <summary>
        /// The current position of this buffering stream. The next <see cref="Read(byte[], int, int)"/> will attempt
        /// to consume bytes here.
        /// </summary>
        public override long Position
        {
            get => _position;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), Strings.PositionMustBeNonNegative);
                }

                _position = value;
            }
        }

        /// <summary>
        /// Asynchronously reads bytes into a provided buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read bytes into.</param>
        /// <param name="offset">The offset in the buffer which is where bytes will be written to.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task returning the number of bytes read into the buffer.</returns>
        public abstract override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);

        /// <summary>
        /// Seek to an offset in the stream, based off of the provided origin.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <param name="origin">The origin.</param>
        /// <returns>The resulting absolute position (relative to the beginning).</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.SeekUsingPosition(offset, origin);
        }

        /// <summary>
        /// This method is not supported.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <summary>
        /// This method is not supported.
        /// </summary>
        public override void Flush() => throw new NotImplementedException();

        /// <summary>
        /// This method is not supported.
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <summary>
        /// This method is not supported.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
