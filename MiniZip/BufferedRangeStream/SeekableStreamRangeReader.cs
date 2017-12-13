using System;
using System.IO;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    public class SeekableStreamRangeReader : IRangeReader
    {
        private readonly Func<Task<Stream>> _openStreamAsync;

        public SeekableStreamRangeReader(Func<Task<Stream>> openStreamAsync)
        {
            _openStreamAsync = openStreamAsync;
        }

        public async virtual Task<int> ReadAsync(long srcOffset, byte[] dst, int dstOffset, int count)
        {
            using (var stream = await _openStreamAsync())
            {
                if (!stream.CanSeek)
                {
                    throw new InvalidOperationException(Strings.StreamMustSupportSeek);
                }

                if (!stream.CanRead)
                {
                    throw new InvalidOperationException(Strings.StreamMustSupportRead);
                }

                stream.Position = srcOffset;

                return await stream.ReadToEndAsync(dst, dstOffset, count);
            }
        }
    }
}
