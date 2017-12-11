using System;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;

namespace Knapcode.MiniZip
{
    public class SharpZipLibEntry
    {
        public SharpZipLibEntry(ICSharpCode.SharpZipLib.Zip.ZipEntry entry)
        {
            Comment = entry.Comment;
            CompressedSize = entry.CompressedSize;
            CompressionMethod = entry.CompressionMethod;
            Crc = entry.Crc;
            DateTime = entry.DateTime;
            DosTime = entry.DosTime;
            ExternalFileAttributes = entry.ExternalFileAttributes;
            ExtraData = entry.ExtraData?.ToArray();
            Flags = entry.Flags;
            Name = entry.Name;
            Offset = entry.Offset;
            Size = entry.Size;
            Version = entry.Version;
            VersionMadeBy = entry.VersionMadeBy;
            ZipFileIndex = entry.ZipFileIndex;
        }

        public string Comment { get; }
        public long CompressedSize { get; }
        public CompressionMethod CompressionMethod { get; }
        public long Crc { get; }
        public DateTime DateTime { get; }
        public long DosTime { get; }
        public int ExternalFileAttributes { get; }
        public byte[] ExtraData { get; }
        public int Flags { get; }
        public string Name { get; }
        public long Offset { get; }
        public long Size { get; }
        public int Version { get; }
        public int VersionMadeBy { get; }
        public long ZipFileIndex { get; }
    }
}