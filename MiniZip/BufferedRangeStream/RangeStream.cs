using System;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// Allows you to seek and read a stream without having access to the whole stream of data. By providing a
    /// <see cref="IRangeReader"/>, you can fetch chunks of data on demand. This implementation does not buffer the
    /// reads and therefore may fetch the same bytes from the provided <see cref="IRangeReader"/> if requested multiple
    /// times. This is the unbuffered version of <see cref="BufferedRangeStream"/>.
    /// </summary>
    public class RangeStream : ReadAsyncStream
    {
        private readonly IRangeReader _rangeReader;

        /// <summary>
        /// Initializes an instance of a unbuffered range reader stream.
        /// </summary>
        /// <param name="rangeReader">The interface used for reading ranges of bytes.</param>
        /// <param name="length">The total length of the file reader by <paramref name="rangeReader"/>.</param>
        public RangeStream(IRangeReader rangeReader, long length)
            : base(length)
        {
            _rangeReader = rangeReader;
        }

        /// <summary>
        /// Asynchronously reads bytes from the stream into the provided buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read bytes into.</param>
        /// <param name="offset">The offset in the buffer which is where bytes will be written to.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The number of bytes read into the buffer.</returns>
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var actualCount = count;
            if (Position + count > Length)
            {
                actualCount = (int)(Length - Position);
            }

            if (actualCount <= 0)
            {
                return 0;
            }

            return await _rangeReader.ReadAsync(_position, buffer, offset, actualCount);
        }
    }
}
