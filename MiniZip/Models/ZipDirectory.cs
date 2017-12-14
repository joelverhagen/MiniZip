using System;
using System.Collections.Generic;

namespace Knapcode.MiniZip
{
    public class ZipDirectory : IEquatable<ZipDirectory>
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
        public Zip64Directory Zip64 { get; set; }
        public List<ZipEntry> Entries { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as ZipDirectory);
        }

        public bool Equals(ZipDirectory other)
        {
            return other != null &&
                   OffsetAfterEndOfCentralDirectory == other.OffsetAfterEndOfCentralDirectory &&
                   NumberOfThisDisk == other.NumberOfThisDisk &&
                   DiskWithStartOfCentralDirectory == other.DiskWithStartOfCentralDirectory &&
                   EntriesInThisDisk == other.EntriesInThisDisk &&
                   EntriesForWholeCentralDirectory == other.EntriesForWholeCentralDirectory &&
                   CentralDirectorySize == other.CentralDirectorySize &&
                   OffsetOfCentralDirectory == other.OffsetOfCentralDirectory &&
                   CommentSize == other.CommentSize &&
                   ListEqualityComparer<byte>.Default.Equals(Comment, other.Comment) &&
                   EqualityComparer<Zip64Directory>.Default.Equals(Zip64, other.Zip64) &&
                   ListEqualityComparer<ZipEntry>.Default.Equals(Entries, other.Entries);
        }

        public override int GetHashCode()
        {
            var hashCode = 2001994462;
            hashCode = hashCode * -1521134295 + OffsetAfterEndOfCentralDirectory.GetHashCode();
            hashCode = hashCode * -1521134295 + NumberOfThisDisk.GetHashCode();
            hashCode = hashCode * -1521134295 + DiskWithStartOfCentralDirectory.GetHashCode();
            hashCode = hashCode * -1521134295 + EntriesInThisDisk.GetHashCode();
            hashCode = hashCode * -1521134295 + EntriesForWholeCentralDirectory.GetHashCode();
            hashCode = hashCode * -1521134295 + CentralDirectorySize.GetHashCode();
            hashCode = hashCode * -1521134295 + OffsetOfCentralDirectory.GetHashCode();
            hashCode = hashCode * -1521134295 + CommentSize.GetHashCode();
            hashCode = hashCode * -1521134295 + ListEqualityComparer<byte>.Default.GetHashCode(Comment);
            hashCode = hashCode * -1521134295 + EqualityComparer<Zip64Directory>.Default.GetHashCode(Zip64);
            hashCode = hashCode * -1521134295 + ListEqualityComparer<ZipEntry>.Default.GetHashCode(Entries);
            return hashCode;
        }
    }
}
