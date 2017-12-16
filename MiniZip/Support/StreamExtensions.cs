using System.IO;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    internal static class StreamExtensions
    {
        public static async Task<int> ReadToEndAsync(this Stream stream, byte[] buffer, int offset, int count)
        {
            var currentCount = count;
            var currentOffset = 0;

            int read;
            do
            {
                read = await stream.ReadAsync(buffer, offset + currentOffset, currentCount);
                currentOffset += read;
                currentCount -= read;
            }
            while (read > 0 && currentCount > 0);

            return currentOffset;
        }

        public static async Task ReadExactlyAsync(this Stream stream, byte[] buffer, int count)
        {
            var read = await stream.ReadToEndAsync(buffer, 0, count);
            if (read < count)
            {
                throw new EndOfStreamException();
            }
        }
    }
}
