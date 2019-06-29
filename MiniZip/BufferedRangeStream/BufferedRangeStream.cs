using System;
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
    public class BufferedRangeStream : ReadAsyncStream
    {
        private readonly IRangeReader _rangeReader;
        private readonly IBufferSizeProvider _bufferSizeProvider;
        private readonly BlockMemoryStream _buffer;

        /// <summary>
        /// Initializes an instance of a buffered range reader stream.
        /// </summary>
        /// <param name="rangeReader">The interface used for reading ranges of bytes.</param>
        /// <param name="length">The total length of the file reader by <paramref name="rangeReader"/>.</param>
        /// <param name="bufferSizeProvider">The interface used to determine what buffer sizes to use.</param>
        public BufferedRangeStream(IRangeReader rangeReader, long length, IBufferSizeProvider bufferSizeProvider)
            : base(length)
        {
            _rangeReader = rangeReader;
            _bufferSizeProvider = bufferSizeProvider;
            _buffer = new BlockMemoryStream();
            BufferPosition = length;
        }

        /// <summary>
        /// The current position in the underlying of the buffer. The initial value of this property is
        /// the length of the stream.
        /// </summary>
        public long BufferPosition { get; private set; }

        /// <summary>
        /// The size of the buffer.
        /// </summary>
        public long BufferSize => _buffer.Length;

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
    }
}
