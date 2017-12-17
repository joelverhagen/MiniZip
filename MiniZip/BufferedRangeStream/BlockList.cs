using System.Collections.Generic;
using System.Linq;

namespace Knapcode.MiniZip
{
    internal class BlockList
    {
        private readonly LinkedList<byte[]> _blocks;
        private readonly SortedDictionary<long, LinkedListNode<byte[]>> _nodeLookup;
        private long _minimum;
        private long _maximum;

        public BlockList()
        {
            _blocks = new LinkedList<byte[]>();
            _nodeLookup = new SortedDictionary<long, LinkedListNode<byte[]>>();
        }

        public void Prepend(byte[] block)
        {
            if (HandleEmptyOrInitialBlock(block))
            {
                return;
            }

            _minimum -= block.Length;
            Length += block.Length;
            var node = _blocks.AddFirst(block);
            _nodeLookup.Add(_minimum, node);
        }

        public long Length { get; private set; }

        public void Append(byte[] block)
        {
            if (HandleEmptyOrInitialBlock(block))
            {
                return;
            }

            var node = _blocks.AddLast(block);
            _nodeLookup.Add(_maximum, node);
            _maximum += block.Length;
            Length += block.Length;
        }

        private bool HandleEmptyOrInitialBlock(byte[] block)
        {
            if (block.Length == 0)
            {
                return true;
            }

            if (_nodeLookup.Count == 0)
            {
                _minimum = 0;
                _maximum = block.Length;
                Length = block.Length;
                var node = _blocks.AddFirst(block);
                _nodeLookup.Add(_minimum, node);
                return true;
            }

            return false;
        }

        public BlockAndOffset Search(long position)
        {
            if (position < 0 || position >= Length)
            {
                return default(BlockAndOffset);
            }

            var adjustedPosition = _minimum + position;
            var keys = _nodeLookup.Keys.ToList();
            var keyIndex = keys.BinarySearch(adjustedPosition);

            long key;
            if (keyIndex < 0)
            {
                var indexOfNextKey = ~keyIndex;
                key = keys[indexOfNextKey - 1];
            }
            else
            {
                key = keys[keyIndex];
            }

            var node = _nodeLookup[key];
            var offset = (int)(adjustedPosition - key);

            return new BlockAndOffset(node, offset);
        }
    }
}
