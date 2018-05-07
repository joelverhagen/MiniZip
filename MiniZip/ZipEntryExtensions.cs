using System;
using System.Linq;
using System.Text;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// Extension methods for <see cref="CentralDirectoryHeader"/>.
    /// </summary>
    public static class ZipEntryExtensions
    {
        private static readonly DateTime InvalidDateTime = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

        /// <summary>
        /// Determine the compressed size of the <see cref="CentralDirectoryHeader"/>, in bytes. This method takes all relevant
        /// details in the entry into account.
        /// </summary>
        /// <param name="entry">The ZIP entry.</param>
        public static ulong GetCompressedSize(this CentralDirectoryHeader entry)
        {
            return entry.Zip64DataFields.SingleOrDefault()?.CompressedSize ?? entry.CompressedSize;
        }

        /// <summary>
        /// Determine the uncompressed size of the <see cref="CentralDirectoryHeader"/>, in bytes. This method takes all relevant
        /// details in the entry into account.
        /// </summary>
        /// <param name="entry">The ZIP entry.</param>
        public static ulong GetUncompressedSize(this CentralDirectoryHeader entry)
        {
            return entry.Zip64DataFields.SingleOrDefault()?.UncompressedSize ?? entry.UncompressedSize;
        }

        /// <summary>
        /// Determines the full path name of the <see cref="CentralDirectoryHeader"/> by decoding the name bytes. Uses UTF-8
        /// encoding if the <see cref="FileData.Flags"/> indicate as such, otherwise uses the default code page.
        /// </summary>
        /// <param name="entry">The ZIP entry.</param>
        public static string GetName(this CentralDirectoryHeader entry)
        {
            return entry.GetName(encoding: null);
        }

        /// <summary>
        /// Determines the full path name of the <see cref="CentralDirectoryHeader"/> by decoding the name bytes with the provided
        /// encoding.
        /// </summary>
        /// <param name="entry">The ZIP entry.</param>
        /// <param name="encoding">The encoding to decode the bytes with.</param>
        public static string GetName(this CentralDirectoryHeader entry, Encoding encoding)
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

        /// <summary>
        /// Determine the last modified time of the <see cref="CentralDirectoryHeader"/>. The encoded format of the last modified time
        /// is MS-DOS format. There is no timezone information associated with the output. Defaults to 1980-01-01 if the
        /// last modified time is invalid.
        /// </summary>
        /// <param name="entry">The ZIP entry.</param>
        public static DateTime GetLastModified(this CentralDirectoryHeader entry)
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
