namespace Knapcode.MiniZip
{
    public class ZipEntry
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
    }
}
