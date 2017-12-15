using System;
using System.IO;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// A range reader which reads from a stream. The stream must support reading and seeking.
    /// </summary>
    public class SeekableStreamRangeReader : IRangeReader
    {
        private readonly Func<Task<Stream>> _openStreamAsync;

        /// <summary>
        /// Initializes an instance of a seekable stream range reader.
        /// </summary>
        /// <param name="openStreamAsync">A function for opening a stream asynchronously.</param>
        public SeekableStreamRangeReader(Func<Task<Stream>> openStreamAsync)
        {
            _openStreamAsync = openStreamAsync;
        }

        /// <summary>
        /// Read a range of bytes from the opened stream.
        /// </summary>
        /// <param name="srcOffset">The position from the beginning of the opened stream to start reading.</param>
        /// <param name="dst">The destination buffer to write bytes to.</param>
        /// <param name="dstOffset">The offset in the destination buffer.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>The number of bytes read.</returns>
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
