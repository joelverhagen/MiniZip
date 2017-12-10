using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MiniZip
{
    public delegate Task<byte[]> ReadRangeAsync(long offset, int count);

    public class BufferedRangeStream : Stream
    {
        private byte[] _buffer;
        private long _bufferPosition;
        private readonly ReadRangeAsync _readRangeAsync;
        private readonly IBufferSizeProvider _bufferSizeProvider;
        private long _position;

        public BufferedRangeStream(ReadRangeAsync readRangeAsync, long length, IBufferSizeProvider bufferSizeProvider)
        {
            _readRangeAsync = readRangeAsync;
            _bufferSizeProvider = bufferSizeProvider;
            Length = length;
            _position = 0;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length { get; }


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
                 || Position < _bufferPosition
                 || Position + count > _bufferPosition + _buffer.Length))
            {
                if (Position >= Length)
                {
                    return -1;
                }

                var extraCount = Math.Max(count, _bufferSizeProvider.GetNextBufferSize());
                var available = Length - Position;
                long extraPosition;
                if (available < extraCount)
                {
                    extraPosition = Math.Max(0, Length - extraCount);
                }
                else
                {
                    extraPosition = Position;
                }

                _buffer = await _readRangeAsync(extraPosition, extraCount);
                _bufferPosition = extraPosition;
            }

            return (int)(Position - _bufferPosition);
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
