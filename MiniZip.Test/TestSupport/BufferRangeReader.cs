using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    public class BufferRangeReader : SeekableStreamRangeReader
    {
        public BufferRangeReader(byte[] buffer) : base(GetOpenStreamAsync(buffer.ToArray()))
        {
        }

        private static Func<Task<Stream>> GetOpenStreamAsync(byte[] buffer)
        {
            return () => Task.FromResult<Stream>(new MemoryStream(buffer));
        }
    }
}
