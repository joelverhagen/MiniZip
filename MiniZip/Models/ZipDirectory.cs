using System.Collections.Generic;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// Metadata about the whole ZIP directory.
    /// </summary>
    public class ZipDirectory
    {
        /// <summary>
        /// The offset after the end of central directory signature.
        /// </summary>
        public long OffsetAfterEndOfCentralDirectory { get; set; }

        /// <summary>
        /// The number of this disk.
        /// </summary>
        public ushort NumberOfThisDisk { get; set; }

        /// <summary>
        /// The number of the disk with the start of the central directory.
        /// </summary>
        public ushort DiskWithStartOfCentralDirectory { get; set; }

        /// <summary>
        /// The total number of entries in the central directory on this disk.
        /// </summary>
        public ushort EntriesInThisDisk { get; set; }

        /// <summary>
        /// The total number of entries in the central directory.
        /// </summary>
        public ushort EntriesForWholeCentralDirectory { get; set; }

        /// <summary>
        /// The size of the central directory.
        /// </summary>
        public uint CentralDirectorySize { get; set; }

        /// <summary>
        /// The offset of start of central directory with respect to the starting disk number.
        /// </summary>
        public uint OffsetOfCentralDirectory { get; set; }

        /// <summary>
        /// The length of the archive comment.
        /// </summary>
        public ushort CommentSize { get; set; }

        /// <summary>
        /// The ZIP archive comment bytes.
        /// </summary>
        public byte[] Comment { get; set; }

        /// <summary>
        /// Data in the Zip64 end of central directory record.
        /// </summary>
        public Zip64Directory Zip64 { get; set; }

        /// <summary>
        /// Data about the ZIP entries.
        /// </summary>
        public List<CentralDirectoryHeader> Entries { get; set; }
    }
}
