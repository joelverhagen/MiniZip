namespace Knapcode.MiniZip
{
    /// <summary>
    /// A ZIP entry data field containing Zip64 information.
    /// APPNOTE.TXT: 4.5.3 -Zip64 Extended Information Extra Field (0x0001)
    /// </summary>
    public class Zip64DataField
    {
        /// <summary>
        /// The uncompressed size of the file.
        /// </summary>
        public ulong? UncompressedSize { get; set; }

        /// <summary>
        /// The compressed size of the file.
        /// </summary>
        public ulong? CompressedSize { get; set; }

        /// <summary>
        /// The offset to the entry's local header.
        /// </summary>
        public ulong? LocalHeaderOffset { get; set; }

        /// <summary>
        /// The disk number where the file starts.
        /// </summary>
        public ulong? DiskNumberStart { get; set; }
    }
}
