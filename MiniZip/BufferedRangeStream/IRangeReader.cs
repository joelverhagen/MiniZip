using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// An interface for reading a range of bytes from an arbitrary source. This is used by
    /// <see cref="BufferedRangeStream"/> to read ranges of bytes on demand.
    /// </summary>
    public interface IRangeReader
    {
        /// <summary>
        /// Read bytes from an arbitrary location.
        /// </summary>
        /// <param name="srcOffset">The position from the beginning of the byte source.</param>
        /// <param name="dst">The destination buffer to write bytes to.</param>
        /// <param name="dstOffset">The offset in the destination buffer.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>The number of bytes read.</returns>
        Task<int> ReadAsync(long srcOffset, byte[] dst, int dstOffset, int count);
    }
}
