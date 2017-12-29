using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    internal class BoundedStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly long _startPosition;
        private readonly long _endPosition;
        private bool _firstSeek = false;

        public BoundedStream(Stream innerStream, long startPosition, long endPosition)
        {
            _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
            _startPosition = startPosition;
            _endPosition = endPosition;

            if (!innerStream.CanSeek)
            {
                throw new ArgumentException(Strings.StreamMustSupportSeek, nameof(innerStream));
            }

            if (!innerStream.CanRead)
            {
                throw new ArgumentException(Strings.StreamMustSupportRead, nameof(innerStream));
            }

            if (endPosition >= innerStream.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(endPosition), Strings.EndPositionBeyondLength);
            }

            if (startPosition < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startPosition), Strings.StartPositionMustNotBeNegative);
            }

            if (startPosition > endPosition)
            {
                throw new ArgumentException(Strings.StartPositionGreaterThanEndPosition);
            }
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _endPosition - _startPosition;

        protected override void Dispose(bool disposing)
        {
            _innerStream?.Dispose();
        }

        private void Initialize()
        {
            if (!_firstSeek)
            {
                if (_innerStream.Position < _startPosition)
                {
                    _innerStream.Position = _startPosition;
                }
                else if (_innerStream.Position > _endPosition)
                {
                    _innerStream.Position = _endPosition;
                }

                _firstSeek = true;
            }
        }

        public override long Position
        {
            get
            {
                Initialize();
                return _innerStream.Position - _startPosition;
            }

            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), Strings.PositionMustBeNonNegative);
                }

                _innerStream.Position = _startPosition + value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var boundedCount = GetBoundedCount(count);
            return _innerStream.Read(buffer, offset, boundedCount);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var boundedCount = GetBoundedCount(count);
            return await _innerStream.ReadAsync(buffer, offset, boundedCount);
        }

        private int GetBoundedCount(int count)
        {
            Initialize();

            if (_innerStream.Position < _startPosition)
            {
                throw new InvalidOperationException(Strings.InnerStreamPositionBeforeBounds);
            }

            var remaining = (int)(_endPosition - _innerStream.Position);
            if (remaining <= 0)
            {
                return 0;
            }

            return Math.Min(count, remaining);
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
