using System.Collections.Generic;

namespace Knapcode.MiniZip
{
    internal struct BlockAndOffset
    {
        public BlockAndOffset(LinkedListNode<byte[]> node, int offset) : this()
        {
            Node = node;
            Offset = offset;
        }

        public LinkedListNode<byte[]> Node { get; }
        public int Offset { get; }
    }
}
