using System;
using System.IO;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    internal static class StreamExtensions
    {
        public static int ReadToEnd(this Stream stream, byte[] buffer, int offset, int count)
        {
            var currentCount = count;
            var currentOffset = 0;

            int read;
            do
            {
                read = stream.Read(buffer, offset + currentOffset, currentCount);
                currentOffset += read;
                currentCount -= read;
            }
            while (read > 0 && currentCount > 0);

            return currentOffset;
        }

        public static void ReadExactly(this Stream stream, byte[] buffer, int offset, int count)
        {
            var read = stream.ReadToEnd(buffer, offset, count);
            if (read < count)
            {
                throw new EndOfStreamException();
            }
        }

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

        public static async Task ReadExactlyAsync(this Stream stream, byte[] buffer, int offset, int count)
        {
            var read = await stream.ReadToEndAsync(buffer, offset, count);
            if (read < count)
            {
                throw new EndOfStreamException();
            }
        }

        public static long SeekUsingPosition(this Stream stream, long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    stream.Position = offset;
                    break;
                case SeekOrigin.Current:
                    stream.Position += offset;
                    break;
                case SeekOrigin.End:
                    stream.Position = stream.Length - offset;
                    break;
                default:
                    throw new NotImplementedException();
            }

            return stream.Position;
        }
    }
}
