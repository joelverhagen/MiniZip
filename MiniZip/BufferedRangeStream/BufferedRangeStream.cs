using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// Allows you to seek and read a stream without having access to the whole stream of data. By providing a
    /// <see cref="IRangeReader"/>, you can fetch chunks of data on demand. The current implementation
    /// maintains a single contiguous buffer in memory so reading at both ends of the stream will result in buffering
    /// the entire inner data source. This implementation is designed to work well with reading a ZIP archive's central
    /// directory without reading the entire ZIP archive.
    /// </summary>
    public class BufferedRangeStream : Stream
    {
        private readonly IRangeReader _rangeReader;
        private readonly IBufferSizeProvider _bufferSizeProvider;
        private readonly BlockMemoryStream _buffer;
        private long _position;

        /// <summary>
        /// Initializes an instance of a buffered range reader.
        /// </summary>
        /// <param name="rangeReader">The interface used for reading ranges of bytes.</param>
        /// <param name="length">The total length of the file reader by <paramref name="rangeReader"/>.</param>
        /// <param name="bufferSizeProvider">The interface used to determine what buffer sizes to use.</param>
        public BufferedRangeStream(IRangeReader rangeReader, long length, IBufferSizeProvider bufferSizeProvider)
        {
            _rangeReader = rangeReader;
            _bufferSizeProvider = bufferSizeProvider;
            _buffer = new BlockMemoryStream();
            Length = length;
            BufferPosition = length;
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
        /// The current position in the underlying of the buffer. The initial value of this property is
        /// <see cref="Length"/>.
        /// </summary>
        public long BufferPosition { get; private set; }

        /// <summary>
        /// The size of the buffer.
        /// </summary>
        public long BufferSize => _buffer.Length;

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
        /// This method is not supported.
        /// </summary>
        public override void Flush()
        {
            throw new NotImplementedException();
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
            var bufferOffset = await GetBufferOffsetAsync(count);
            if (bufferOffset < 0)
            {
                return 0;
            }

            var read = (int) Math.Min(_buffer.Length - bufferOffset, count);
            _buffer.Position = bufferOffset;
            _buffer.ReadExactly(buffer, offset, read);

            Position += read;

            return read;
        }

        /// <summary>
        /// This method is not currently supported.
        /// </summary>
        /// <param name="buffer">The buffer to read bytes into.</param>
        /// <param name="offset">The offset in the buffer which is where bytes will be written to.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>The number of bytes read into the buffer.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        private async Task<int> GetBufferOffsetAsync(int count)
        {
            if (Position >= Length)
            {
                return -1;
            }

            if (Position < BufferPosition)
            {

                // Determine the read offset by setting a desired buffer size.
                var desiredBufferSize = Math.Max(count, _bufferSizeProvider.GetNextBufferSize());
                if (desiredBufferSize > Length)
                {
                    desiredBufferSize = (int)Length;
                }

                var available = Length - Position;

                long readOffset;
                if (available < desiredBufferSize)
                {
                    readOffset = Math.Max(0, Length - desiredBufferSize);
                }
                else
                {
                    readOffset = Position;
                }
                
                // Read up until the old position (or up to the end, if this is the first read).
                var readCount =  (int)(BufferPosition - readOffset);
                var newBuffer = new byte[readCount];
                var actualRead = await _rangeReader.ReadAsync(readOffset, newBuffer, 0, readCount);
                _buffer.Prepend(newBuffer);
                BufferPosition = readOffset;
            }

            return (int)(Position - BufferPosition);
        }

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
        /// <param name="value">The new length for the stream.</param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// This method is not supported.
        /// </summary>
        /// <param name="buffer">The buffer to read from.</param>
        /// <param name="offset">The offset in the buffer to start reading from.</param>
        /// <param name="count">The number of bytes to write.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
