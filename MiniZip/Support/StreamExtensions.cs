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

        public static async Task ReadFullyAsync(this Stream stream, byte[] buffer)
        {
            var read = await ReadToEndAsync(stream, buffer, 0, buffer.Length);
            if (read < buffer.Length)
            {
                throw new EndOfStreamException();
            }
        }

        public static async Task<byte> ReadU8Async(this Stream stream, byte[] byteBuffer)
        {
            if (await stream.ReadAsync(byteBuffer, 0, 1) == 0)
            {
                throw new EndOfStreamException();
            }

            return byteBuffer[0];
        }

        public static async Task<ushort> ReadLEU16Async(this Stream stream, byte[] byteBuffer)
        {
            int byteA = await stream.ReadU8Async(byteBuffer);
            int byteB = await stream.ReadU8Async(byteBuffer);

            return unchecked((ushort)((ushort)byteA | (ushort)(byteB << 8)));
        }

        public static async Task<uint> ReadLEU32Async(this Stream stream, byte[] byteBuffer)
        {
            var ushortA = await stream.ReadLEU16Async(byteBuffer);
            var ushortB = await stream.ReadLEU16Async(byteBuffer);

            return (uint)(ushortA | (ushortB << 16));
        }

        public static async Task<ulong> ReadLEU64Async(this Stream stream, byte[] byteBuffer)
        {
            var uintA = await stream.ReadLEU32Async(byteBuffer);
            var uintB = await stream.ReadLEU32Async(byteBuffer);

            return uintA | ((ulong)(uintB) << 32);
        }
    }
}
