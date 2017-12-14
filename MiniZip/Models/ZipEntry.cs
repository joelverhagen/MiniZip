using System;
using System.Collections.Generic;

namespace Knapcode.MiniZip
{
    public class ZipEntry : IEquatable<ZipEntry>
    {
        public ushort VersionMadeBy { get; set; }
        public ushort VersionToExtract { get; set; }
        public ushort Flags { get; set; }
        public ushort CompressionMethod { get; set; }
        public ushort LastModifiedTime { get; set; }
        public ushort LastModifiedDate { get; set; }
        public uint Crc32 { get; set; }
        public uint CompressedSize { get; set; }
        public uint UncompressedSize { get; set; }
        public ushort NameSize { get; set; }
        public ushort ExtraFieldSize { get; set; }
        public ushort CommentSize { get; set; }
        public ushort DiskNumberStart { get; set; }
        public ushort InternalAttributes { get; set; }
        public uint ExternalAttributes { get; set; }
        public uint LocalHeaderOffset { get; set; }
        public byte[] Name { get; set; }
        public byte[] ExtraField { get; set; }
        public byte[] Comment { get; set; }
        public List<ZipDataField> DataFields { get; set; }
        public List<Zip64DataField> Zip64DataFields { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as ZipEntry);
        }

        public bool Equals(ZipEntry other)
        {
            return other != null &&
                   VersionMadeBy == other.VersionMadeBy &&
                   VersionToExtract == other.VersionToExtract &&
                   Flags == other.Flags &&
                   CompressionMethod == other.CompressionMethod &&
                   LastModifiedTime == other.LastModifiedTime &&
                   LastModifiedDate == other.LastModifiedDate &&
                   Crc32 == other.Crc32 &&
                   CompressedSize == other.CompressedSize &&
                   UncompressedSize == other.UncompressedSize &&
                   NameSize == other.NameSize &&
                   ExtraFieldSize == other.ExtraFieldSize &&
                   CommentSize == other.CommentSize &&
                   DiskNumberStart == other.DiskNumberStart &&
                   InternalAttributes == other.InternalAttributes &&
                   ExternalAttributes == other.ExternalAttributes &&
                   LocalHeaderOffset == other.LocalHeaderOffset &&
                   ListEqualityComparer<byte>.Default.Equals(Name, other.Name) &&
                   ListEqualityComparer<byte>.Default.Equals(ExtraField, other.ExtraField) &&
                   ListEqualityComparer<byte>.Default.Equals(Comment, other.Comment) &&
                   ListEqualityComparer<ZipDataField>.Default.Equals(DataFields, other.DataFields) &&
                   ListEqualityComparer<Zip64DataField>.Default.Equals(Zip64DataFields, other.Zip64DataFields);
        }

        public override int GetHashCode()
        {
            var hashCode = 458721257;
            hashCode = hashCode * -1521134295 + VersionMadeBy.GetHashCode();
            hashCode = hashCode * -1521134295 + VersionToExtract.GetHashCode();
            hashCode = hashCode * -1521134295 + Flags.GetHashCode();
            hashCode = hashCode * -1521134295 + CompressionMethod.GetHashCode();
            hashCode = hashCode * -1521134295 + LastModifiedTime.GetHashCode();
            hashCode = hashCode * -1521134295 + LastModifiedDate.GetHashCode();
            hashCode = hashCode * -1521134295 + Crc32.GetHashCode();
            hashCode = hashCode * -1521134295 + CompressedSize.GetHashCode();
            hashCode = hashCode * -1521134295 + UncompressedSize.GetHashCode();
            hashCode = hashCode * -1521134295 + NameSize.GetHashCode();
            hashCode = hashCode * -1521134295 + ExtraFieldSize.GetHashCode();
            hashCode = hashCode * -1521134295 + CommentSize.GetHashCode();
            hashCode = hashCode * -1521134295 + DiskNumberStart.GetHashCode();
            hashCode = hashCode * -1521134295 + InternalAttributes.GetHashCode();
            hashCode = hashCode * -1521134295 + ExternalAttributes.GetHashCode();
            hashCode = hashCode * -1521134295 + LocalHeaderOffset.GetHashCode();
            hashCode = hashCode * -1521134295 + ListEqualityComparer<byte>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + ListEqualityComparer<byte>.Default.GetHashCode(ExtraField);
            hashCode = hashCode * -1521134295 + ListEqualityComparer<byte>.Default.GetHashCode(Comment);
            hashCode = hashCode * -1521134295 + ListEqualityComparer<ZipDataField>.Default.GetHashCode(DataFields);
            hashCode = hashCode * -1521134295 + ListEqualityComparer<Zip64DataField>.Default.GetHashCode(Zip64DataFields);
            return hashCode;
        }
    }
}
