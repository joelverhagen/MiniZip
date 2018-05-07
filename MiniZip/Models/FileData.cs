using System.Collections.Generic;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// Fields shared between <see cref="LocalFileHeader"/> and <see cref="CentralDirectoryHeader"/>.
    /// </summary>
    public class FileData
    {
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
        /// The bytes of the name field.
        /// </summary>
        public byte[] Name { get; set; }

        /// <summary>
        /// The bytes of the extra field.
        /// </summary>
        public byte[] ExtraField { get; set; }

        /// <summary>
        /// The parsed split data fields.
        /// </summary>
        public List<ZipDataField> DataFields { get; set; }
    }
}
