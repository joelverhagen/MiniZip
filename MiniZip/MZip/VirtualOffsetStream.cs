using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    internal class VirtualOffsetStream : Stream
    {
        private readonly Stream _innerStream;
        private long _position;
        private readonly long _virtualOffset;

        public VirtualOffsetStream(Stream innerStream, long virtualOffset)
        {
            _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
            _virtualOffset = virtualOffset;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _virtualOffset + _innerStream.Length;

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

        protected override void Dispose(bool disposing)
        {
            _innerStream?.Dispose();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            SetInnerPosition();
            var read = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
            Position += read;
            return read;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            SetInnerPosition();
            var read = _innerStream.Read(buffer, offset, count);
            Position += read;
            return read;
        }

        private void SetInnerPosition()
        {
            if (Position < _virtualOffset)
            {
                throw new InvalidOperationException(Strings.PositionBeforeVirtualOffset);
            }

            var innerPosition = Position - _virtualOffset;
            if (_innerStream.Position != innerPosition)
            {
                _innerStream.Position = innerPosition;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.SeekUsingPosition(offset, origin);
        }

        public override void Flush() => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
