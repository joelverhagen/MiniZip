using System.Collections.Generic;

namespace Knapcode.MiniZip
{
    public class ZipDirectory
    {
        public long OffsetAfterEndOfCentralDirectory { get; set; }
        public ushort NumberOfThisDisk { get; set; }
        public ushort DiskWithStartOfCentralDirectory { get; set; }
        public ushort EntriesInThisDisk { get; set; }
        public ushort EntriesForWholeCentralDirectory { get; set; }
        public uint CentralDirectorySize { get; set; }
        public uint OffsetOfCentralDirectory { get; set; }
        public ushort CommentSize { get; set; }
        public byte[] Comment { get; set; }
        public bool IsZip64 { get; set; }
        public Zip64Directory Zip64 { get; set; }
        public List<ZipEntry> Entries { get; set; }
    }
}
