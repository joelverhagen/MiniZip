using System;

namespace Knapcode.MiniZip
{
    public class BclZipEntry
    {
        public BclZipEntry(System.IO.Compression.ZipArchiveEntry entry)
        {
            CompressedLength = entry.CompressedLength;
            ExternalAttributes = entry.ExternalAttributes;
            FullName = entry.FullName;
            LastWriteTime = entry.LastWriteTime;
            Length = entry.Length;
            Name = entry.Name;
        }

        public long CompressedLength { get; }
        public int ExternalAttributes { get; }
        public string FullName { get; }
        public DateTimeOffset LastWriteTime { get; }
        public long Length { get; }
        public string Name { get; }
    }
}
