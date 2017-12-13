using System;
using System.Linq;
using System.Text;

namespace Knapcode.MiniZip
{
    public static class ZipEntryExtensions
    {
        private static readonly DateTime InvalidDateTime = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

        public static ulong GetCompressedSize(this ZipEntry entry)
        {
            return entry.Zip64DataFields.SingleOrDefault()?.CompressedSize ?? entry.CompressedSize;
        }

        public static ulong GetUncompressedSize(this ZipEntry entry)
        {
            return entry.Zip64DataFields.SingleOrDefault()?.UncompressedSize ?? entry.UncompressedSize;
        }

        public static string GetName(this ZipEntry entry)
        {
            return entry.GetName(encoding: null);
        }

        public static string GetName(this ZipEntry entry, Encoding encoding)
        {
            if (encoding == null)
            {
                encoding = Encoding.GetEncoding(codepage: 0);
            }
            else if (encoding != Encoding.UTF8 && (entry.Flags & (ushort)ZipEntryFlags.UTF8) != 0)
            {
                throw new ArgumentException(Strings.UTF8Mismatch);
            }

            return encoding.GetString(entry.Name);
        }

        public static DateTime GetLastModified(this ZipEntry entry)
        {
            var year   = 1980 + ((entry.LastModifiedDate & 0b1111_1110_0000_0000) >> 9 );
            var month  =        ((entry.LastModifiedDate & 0b0000_0001_1110_0000) >> 5 );
            var day    =        ((entry.LastModifiedDate & 0b0000_0000_0001_1111)      );
            var hour   =        ((entry.LastModifiedTime & 0b1111_1000_0000_0000) >> 11);
            var minute =        ((entry.LastModifiedTime & 0b0000_0111_1110_0000) >> 5 );
            var second =    2 * ((entry.LastModifiedTime & 0b0000_0000_0001_1111)      );

            try
            {
                return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified);
            }
            catch
            {
                return InvalidDateTime;
            }
        }
    }
}
