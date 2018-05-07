using System.Collections.Generic;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// Metadata about a ZIP entry found in the central directory.
    /// </summary>
    public class CentralDirectoryHeader : FileData
    {
        /// <summary>
        /// The version made by.
        /// </summary>
        public ushort VersionMadeBy { get; set; }

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
        /// The bytes of the comment field.
        /// </summary>
        public byte[] Comment { get; set; }

        /// <summary>
        /// The data fields that are Zip64 extended information.
        /// </summary>
        public List<Zip64CentralDirectoryDataField> Zip64DataFields { get; set; }
    }
}
