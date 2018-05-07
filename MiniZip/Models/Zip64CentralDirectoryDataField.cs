namespace Knapcode.MiniZip
{
    /// <summary>
    /// A ZIP entry data field containing Zip64 information found in the central directory header.
    /// </summary>
    public class Zip64CentralDirectoryDataField : Zip64DataField
    {
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
