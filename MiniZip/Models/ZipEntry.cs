using System.Collections.Generic;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// Metadata about a ZIP entry.
    /// </summary>
    public class ZipEntry
    {
        /// <summary>
        /// The version made by.
        /// </summary>
        public ushort VersionMadeBy { get; set; }

        /// <summary>
        /// The version needed to extract.
        /// </summary>
        public ushort VersionToExtract { get; set; }

        /// <summary>
        /// The general purpose bit flag.
        /// </summary>
        public ushort Flags { get; set; }

        /// <summary>
        /// The compression method.
        /// </summary>
        public ushort CompressionMethod { get; set; }

        /// <summary>
        /// The last modified file time (MS-DOS format).
        /// </summary>
        public ushort LastModifiedTime { get; set; }

        /// <summary>
        /// The last modified file data (MS-DOS format).
        /// </summary>
        public ushort LastModifiedDate { get; set; }

        /// <summary>
        /// The CRC-32 of the file.
        /// </summary>
        public uint Crc32 { get; set; }

        /// <summary>
        /// The compressed size of the file.
        /// </summary>
        public uint CompressedSize { get; set; }

        /// <summary>
        /// The uncompressed size of the file.
        /// </summary>
        public uint UncompressedSize { get; set; }

        /// <summary>
        /// The size of the name field.
        /// </summary>
        public ushort NameSize { get; set; }

        /// <summary>
        /// The size of the extra field.
        /// </summary>
        public ushort ExtraFieldSize { get; set; }

        /// <summary>
        /// The size of the comment field.
        /// </summary>
        public ushort CommentSize { get; set; }

        /// <summary>
        /// The disk number start.
        /// </summary>
        public ushort DiskNumberStart { get; set; }

        /// <summary>
        /// The internal file attributes.
        /// </summary>
        public ushort InternalAttributes { get; set; }

        /// <summary>
        /// The external file attributes.
        /// </summary>
        public uint ExternalAttributes { get; set; }

        /// <summary>
        /// The relative offset of local header.
        /// </summary>
        public uint LocalHeaderOffset { get; set; }

        /// <summary>
        /// The bytes of the name field.
        /// </summary>
        public byte[] Name { get; set; }

        /// <summary>
        /// The bytes of the extra field.
        /// </summary>
        public byte[] ExtraField { get; set; }

        /// <summary>
        /// The bytes of the comment field.
        /// </summary>
        public byte[] Comment { get; set; }

        /// <summary>
        /// The parsed split data fields.
        /// </summary>
        public List<ZipDataField> DataFields { get; set; }

        /// <summary>
        /// The data fields that are Zip64 extended information.
        /// </summary>
        public List<Zip64DataField> Zip64DataFields { get; set; }
    }
}
