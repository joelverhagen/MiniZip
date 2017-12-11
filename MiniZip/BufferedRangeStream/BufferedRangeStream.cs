using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// Allows you to seek and read a stream without having access to the whole stream of data. By providing a
    /// <see cref="ReadRangeAsync"/> delegate, you can fetch chunks of data on demand. The current implementation
    /// maintains a single contiguous buffer in memory so reading at both ends of the stream will result in buffering
    /// the entire inner data source. This implementation is designed to work well with reading a ZIP archive's central
    /// directory without reading the entire ZIP archive.
    /// </summary>
    public class BufferedRangeStream : Stream
    {
        private byte[] _buffer;
        private readonly IRangeReader _rangeReader;
        private readonly IBufferSizeProvider _bufferSizeProvider;
        private long _position;

        public BufferedRangeStream(IRangeReader rangeReader, long length, IBufferSizeProvider bufferSizeProvider)
        {
            _rangeReader = rangeReader;
            _bufferSizeProvider = bufferSizeProvider;
            Length = length;
            BufferPosition = length;
            _position = 0;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length { get; }

        public long BufferPosition { get; private set; }
        public int BufferSize { get; private set; }

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
                    throw new ArgumentOutOfRangeException("The position must be a non-negative number.", nameof(value));
                }

                _position = value;
            }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var bufferOffset = await GetBufferOffsetAsync(count);
            if (bufferOffset < 0)
            {
                return 0;
            }

            var read = Math.Min(_buffer.Length, count);
            Buffer.BlockCopy(_buffer, bufferOffset, buffer, offset, read);

            Position += read;

            return read;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count).Result;
        }

        private async Task<int> GetBufferOffsetAsync(int count)
        {
            if ((_buffer == null
                 || Position < BufferPosition
                 || Position + count > BufferPosition + _buffer.Length))
            {
                if (Position >= Length)
                {
                    return -1;
                }

                if (_buffer != null && Position + count > BufferPosition + _buffer.Length)
                {
                    throw new NotSupportedException("Reading past the end of the buffer is not supported.");
                }

                // Determine the read offset by setting a desired buffer size.
                var desiredBufferSize = Math.Max(count, _bufferSizeProvider.GetNextBufferSize());
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

                // Read up until the old position.
                var readCount = (int)(BufferPosition - readOffset);
                var newBuffer = new byte[readCount + (_buffer?.Length ?? 0)];
                var actualRead = await _rangeReader.ReadAsync(readOffset, newBuffer, 0, readCount);

                if (_buffer == null)
                {
                    BufferSize = actualRead;
                    _buffer = newBuffer;
                }
                else
                {
                    // Append the old buff
                    Buffer.BlockCopy(_buffer, 0, newBuffer, actualRead, BufferSize);
                    BufferSize += actualRead;
                    _buffer = newBuffer;
                }
                
                BufferPosition = readOffset;
            }

            return (int)(Position - BufferPosition);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length - offset;
                    break;
                default:
                    throw new NotSupportedException();
            }

            return Position;
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
