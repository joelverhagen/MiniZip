using System.Linq;

namespace Knapcode.MiniZip
{
    public static class ZipEntryExtensions
    {
        public static ulong GetCompressedSize(this ZipEntry entry)
        {
            return entry.Zip64DataFields.SingleOrDefault()?.CompressedSize ?? entry.CompressedSize;
        }

        public static ulong GetUncompressedSize(this ZipEntry entry)
        {
            return entry.Zip64DataFields.SingleOrDefault()?.UncompressedSize ?? entry.UncompressedSize;
        }
    }
}
