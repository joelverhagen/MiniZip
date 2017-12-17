using System;
using System.IO;

namespace Knapcode.MiniZip
{
    internal class BlockMemoryStream : Stream
    {
        private readonly BlockList _blocks;
        private long _position;

        public BlockMemoryStream()
        {
            _blocks = new BlockList();
            _position = 0;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _blocks.Length;

        public void Prepend(byte[] block)
        {
            _blocks.Prepend(block);
            _position += block.Length;
        }

        public void Append(byte[] block)
        {
            _blocks.Append(block);
        }

        public override long Position
        {
            get => _position;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(Strings.PositionMustBeNonNegative, nameof(value));
                }

                _position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var remainingCount = count;
            var currentOffset = offset;
            var totalRead = 0;

            var blockAndOffset = _blocks.Search(Position);
            var node = blockAndOffset.Node;
            var blockOffset = blockAndOffset.Offset;

            while (remainingCount > 0 && node != null)
            {
                var read = Math.Min(remainingCount, node.Value.Length - blockOffset);

                Buffer.BlockCopy(node.Value, blockOffset, buffer, currentOffset, read);
                remainingCount -= read;
                currentOffset += read;
                totalRead += read;
                Position += read;

                node = node.Next;
                blockOffset = 0;
            }

            return totalRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.SeekUsingPosition(offset, origin);
        }

        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() => throw new NotSupportedException();
    }
}
