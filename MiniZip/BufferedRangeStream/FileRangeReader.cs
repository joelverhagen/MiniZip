using System;
using System.IO;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    public class FileRangeReader : SeekableStreamRangeReader
    {
        public FileRangeReader(string path) : base(GetOpenStreamAsync(path))
        {
        }

        private static Func<Task<Stream>> GetOpenStreamAsync(string path)
        {
            return () => Task.FromResult<Stream>(new FileStream(path, FileMode.Open, FileAccess.Read));
        }
    }
}
