using System.IO;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    public class FileRangeReader : IRangeReader
    {
        private readonly string _path;

        public FileRangeReader(string path)
        {
            _path = path;
        }

        public async Task<int> ReadAsync(long srcOffset, byte[] dst, int dstOffset, int count)
        {
            using (var stream = new FileStream(_path, FileMode.Open, FileAccess.Read))
            {
                stream.Position = srcOffset;

                return await stream.ReadToEndAsync(dst, dstOffset, count);
            }
        }
    }
}
