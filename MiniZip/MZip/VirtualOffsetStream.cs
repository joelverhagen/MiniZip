using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// A read-only stream that offsets the position of an inner stream. The purpose of this stream is to support
    /// stream readers that only operate on the end of a stream. The inner stream contains the portion of the logical
    /// stream the the stream reader is expected to reader.
    /// </summary>
    internal class VirtualOffsetStream : Stream
    {
        private readonly Stream _innerStream;
        private long _position;

        /// <summary>
        /// Initializes an instance of a virtual offset stream.
        /// </summary>
        /// <param name="innerStream">The inner stream to read from.</param>
        /// <param name="virtualOffset">The virtual offset to apply to the inner stream.</param>
        public VirtualOffsetStream(Stream innerStream, long virtualOffset)
        {
            _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
            VirtualOffset = virtualOffset;
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
        /// The total length of this stream. This is the <see cref="VirtualOffset"/> plus the length of the inner
        /// stream.
        /// </summary>
        public override long Length => VirtualOffset + _innerStream.Length;

        /// <summary>
        /// The virtual offset.
        /// </summary>
        public long VirtualOffset { get; }

        /// <summary>
        /// The current position of this buffering stream. The next <see cref="Read(byte[], int, int)"/> will attempt
        /// to consume bytes here.
        /// </summary>
        public override long Position
        {
            get
            {
                return _position;
            }

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
        /// Disposes the inner stream.
        /// </summary>
        /// <param name="disposing">This parameter is unused.</param>
        protected override void Dispose(bool disposing)
        {
            _innerStream?.Dispose();
        }

        /// <summary>
        /// Asynchronously reads bytes from the stream into the provided buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read bytes into.</param>
        /// <param name="offset">The offset in the buffer which is where bytes will be written to.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The number of bytes read into the buffer.</returns>
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            SetInnerPosition();
            var read = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
            Position += read;
            return read;
        }

        /// <summary>
        /// Synchronously reads bytes from the stream into the provided buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read bytes into.</param>
        /// <param name="offset">The offset in the buffer which is where bytes will be written to.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>The number of bytes read into the buffer.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            SetInnerPosition();
            var read = _innerStream.Read(buffer, offset, count);
            Position += read;
            return read;
        }

        private void SetInnerPosition()
        {
            if (Position < VirtualOffset)
            {
                throw new InvalidOperationException(Strings.PositionBeforeVirtualOffset);
            }

            var innerPosition = Position - VirtualOffset;
            if (_innerStream.Position != innerPosition)
            {
                _innerStream.Position = innerPosition;
            }
        }

        /// <summary>
        /// This method is not supported.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.SeekUsingPosition(offset, origin);
        }

        /// <summary>
        /// This method is not supported.
        /// </summary>
        public override void Flush() => throw new NotSupportedException();

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
