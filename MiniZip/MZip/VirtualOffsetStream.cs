using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    internal class VirtualOffsetStream : Stream
    {
        private readonly Stream _innerStream;
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
                return _virtualOffset + _innerStream.Position;
            }

            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), Strings.PositionMustBeNonNegative);
                }
                else if (value < _virtualOffset)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), Strings.PositionBeforeVirtualOffset);
                }

                _innerStream.Position = value - _virtualOffset;
            }
        }

        protected override void Dispose(bool disposing)
        {
            _innerStream?.Dispose();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            VerifyPosition();
            return await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            VerifyPosition();
            return _innerStream.Read(buffer, offset, count);
        }

        private void VerifyPosition()
        {
            if (Position < _virtualOffset)
            {
                throw new InvalidOperationException(Strings.PositionBeforeVirtualOffset);
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
